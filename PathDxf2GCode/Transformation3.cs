namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using netDxf;
using netDxf.Entities;
using System.Text.RegularExpressions;

public class ZProbe {
    public Vector2 Start { get; }
    private readonly Vector2 _end;

    private Vector2? _position;
    private ZProbeParams? _params;
    private string? _name;

    private double _segmentT_mm;
    private double _segmentH_mm;

    public EntityObject Source { get; }
    public Vector2 Position => _position!.Value;
    public ParamsText ParamsText { get; }
    public bool Disabled { get; private set; }

    public ZProbe(Vector2 start, Vector2 end, EntityObject source, ParamsText paramsText) {
        Start = start;
        _end = end;
        Source = source;
        ParamsText = paramsText;
    }

    public void TryAttachTo(ILeafSegmentWithTForZProbes segment) {
        void SetValues(ILeafSegmentWithTForZProbes segment) {
            _segmentT_mm = segment.T_mm;
            _segmentH_mm = segment.H_mm;
            Disabled = segment.Disabled;
        }

        if (segment.Contains(Start)) {
            _position = _end;
            SetValues(segment);
        } else if (segment.Contains(_end)) {
            _position = Start;
            SetValues(segment);
        } else {
            // ignore
        }
    }


    public bool IsAttached => _position != null;

    public void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        _params = new ZProbeParams(ParamsText, superpathVariables, MessageHandlerForEntities.Context(Source, Start, dxfFileName), pathParams, onError);
    }

    public double TH_mm(double h_mm) => (_params!.RawT_mm ?? _segmentT_mm) + _segmentH_mm + h_mm;
    public string? L => _params!.L;

    public string Name => _name ?? throw new NullReferenceException("SetName was not called");

    public Vector3 EmitGCode(Vector3 currPos, double h_mm, Vector2 transformedLocation,
                             List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        PathSegment.AssertNear(currPos.XY(), transformedLocation, MessageHandlerForEntities.Context(Source, Position, dxfFileName));

        double o_mm = _params!.O_mm;
        gcodes.AddNonhorizontalG00($"G00 Z{(TH_mm(h_mm) + o_mm).F3()}", currPos.Z - TH_mm(h_mm) - o_mm); // Go down quickly to T+O

        gcodes.Add($"G38.3 Z0 F{_params!.Z_mmpmin.F3()}"); // Slow Z probe - I have to place the probe below the lowering router bit!
        gcodes.Add("G04 P4"); // Wait 4s to allow reading the value
        // The previous two statements are currently not added to the statistics; maybe later.

        gcodes.AddNonhorizontalG00($"G00 Z{currPos.Z.F3()}", currPos.Z - TH_mm(h_mm)); // Return to previous Z height

        return currPos;
    }

    internal void SetName(string name) {
        if (_name != null && _name != name) {
            // The name will be set multiple times to the same when it is used in multiple superpaths
            throw new Exception($"Internal error - name of ZProbe already set to '{_name}', cannot be set to '{name}'");
        }
        _name = name;
    }
}

public class Transformation3 : Transformation2 {
    private readonly (Vector2 Position, double TH_mm, string Name)[] _zProbeData;

    public Transformation3(Vector2 fromStart, Vector2 fromEnd, Vector2 toStart, Vector2 toEnd, IEnumerable<(ZProbe ZProbe, Vector2 TransformedPosition, double H_mm)> orderedZProbes) : base(fromStart, fromEnd, toStart, toEnd) {
        _zProbeData = orderedZProbes.Select(z => (Position: z.TransformedPosition, z.ZProbe.TH_mm(z.H_mm), z.ZProbe.Name)).ToArray();
    }

    private Transformation3(Transformation2 t, (Vector2 Position, double T_mm, string Name)[] zProbes) : base(t) {
        _zProbeData = zProbes;
    }

    public Transformation3 Transform3(Transformation2 t)
        => new Transformation3(Transform(t), _zProbeData);

    public string Expr(double z_mm, Vector2 xy) {
        if (_zProbeData.Any()) {
            (double Weight, double T_mm, string Name)[] ws = _zProbeData
                .Select(z => (
                    Weight: 1 / ((z.Position - xy).Modulus() + 1e-3), // 1e-3 avoids /0; but is small enough so that
                                          // typical distances to ZProbes (on the order of mm) are not distorted.
                    z.TH_mm,
                    z.Name))
                .OrderByDescending(wt => wt.Weight)
                .Take(4)                  // Limited to the 4 nearest ZProbes to limit expression length.
                .ToArray();
            double weightSum = ws.Sum(wt => wt.Weight);
            // Example: 12.000(=12.000+0.318*[#51-5.000]+0.682*[#52-2.000])
            // This format is an interface to the regexp in PathGCodeAdjustZ;
            // this is (partially) checked by Regex.IsMatch() below.
            string result = z_mm.F3() + $"={z_mm.F3()}+{string.Join("+",
                ws.Select((wt, i) => $"{(wt.Weight / weightSum).F3()}*[{wt.Name}-{wt.T_mm.F3()}]"))}".AsComment(0);
            if (!Regex.IsMatch(result, "^" + GCodeConstants.ZAdjustmentExpressionRegex + "$")) {
                throw new Exception($"Internal Error: '{result}' does not match /^{GCodeConstants.ZAdjustmentExpressionRegex}$/");
            }
            return result;
        } else {
            return z_mm.F3();
        }
    }
}
