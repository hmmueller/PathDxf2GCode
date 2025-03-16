namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using netDxf;
using netDxf.Entities;
using System.Text.RegularExpressions;

public class ZProbe {
    public Circle Source { get; }
    public ParamsText ParamsText { get; }
    private ZProbeParams? _params;
    private string? _name;

    public Vector2 Center { get; }

    public ZProbe(Circle source, ParamsText paramsText, Vector2 center) {
        Center = center;
        Source = source;
        ParamsText = paramsText;
    }

    public void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new ZProbeParams(ParamsText, MessageHandlerForEntities.Context(Source, Center, dxfFileName), pathParams, onError);
    }

    public double T_mm => _params!.T_mm;
    public string? L => _params!.L;
    public string Name => _name ?? throw new NullReferenceException("SetName was not called");

    public Vector3 EmitGCode(Vector3 currPos, Transformation2 t,
                             StreamWriter sw, Statistics stats, string dxfFileName, MessageHandlerForEntities messages) {
        Vector2 c = t.Transform(Center);
        PathSegment.AssertNear(currPos.XY(), c, MessageHandlerForEntities.Context(Source, Center, dxfFileName));

        // sw.WriteLine($"G00 Z{(_params!.T_mm + 2).F3()}  (Knapp über expected Z gehen)");
        sw.WriteLine("G38.3 Z0"); // Slow Z probe - I might have to place the probe below!
        sw.WriteLine("G04 P4"); // Wait 4s to allow reading the value
        sw.WriteLine($"G00 Z{currPos.Z.F3()}"); // Return to previous Z heigth

        return currPos;
    }

    internal void SetName(string name) {
        if (_name != null) {
            throw new Exception("name already set");
        }
        _name = name;
    }
}

public class Transformation3 : Transformation2 {
    private readonly (Vector2 Center, double T_mm, string Name)[] _zProbeData;

    public Transformation3(Vector2 fromStart, Vector2 fromEnd, Vector2 toStart, Vector2 toEnd, IEnumerable<ZProbe> zProbes)
        : base(fromStart, fromEnd, toStart, toEnd) {
        _zProbeData = zProbes.Select(z => (Center: Transform(z.Center), z.T_mm, z.Name)).ToArray();
    }

    private Transformation3(Transformation2 t, (Vector2 Center, double T_mm, string Name)[] zProbes) : base(t) {
        _zProbeData = zProbes;
    }

    public Transformation3 Transform3(Transformation2 t)
        => new Transformation3(Transform(t), _zProbeData);

    public string Expr(double z_mm, Vector2 xy) {
        if (_zProbeData.Any()) {
            (double Weight, double T_mm, string Name)[] ws = _zProbeData
                .Select(z => (
                    Weight: 1 / ((z.Center - xy).Modulus() + 1e-3), // 1e-3 avoids /0; but is small enough so that
                                          // typical distances to ZProbes (on the order of mm) are not distorted.
                    z.T_mm,
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
