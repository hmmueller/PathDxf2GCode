namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using netDxf;
using netDxf.Entities;
using System.Text.RegularExpressions;

public class ZProbe {
    public Circle Source { get; }
    public ParamsText ParamsText { get; }
    protected ZProbeParams? _params;

    public Vector2 Center { get; }

    public ZProbe(Circle source, ParamsText paramsText, Vector2 center) {
        Center = center;
        Source = source;
        ParamsText = paramsText;
    }

    public void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new ZProbeParams(ParamsText, MessageHandler.Context(Source, Center, dxfFileName), pathParams, onError);
    }

    public double T_mm => _params!.T_mm;
    public string? L => _params!.L;

    public Vector3 EmitGCode(Vector3 currPos, Transformation2 t,
                             StreamWriter sw, Statistics stats, string dxfFileName, MessageHandler messages) {
        Vector2 c = t.Transform(Center);
        PathSegment.AssertNear(currPos.XY(), c, MessageHandler.Context(Source, Center, dxfFileName));

        // sw.WriteLine($"G00 Z{(_params!.T_mm + 2).F3()}  (Knapp über expected Z gehen)");
        sw.WriteLine("G38.3 Z0"); // Langsame Probe auf Z - Zeit, um Fühler drunterzulegen
        sw.WriteLine("G04 P4"); // 4s zum Ablesen warten
        sw.WriteLine($"G00 Z{currPos.Z.F3()}"); // Wieder rauf auf initiale Höhe

        return currPos;
    }
}

public class Transformation3 : Transformation2 {
    private readonly (Vector2 Center, double T_mm)[] _zProbes;

    public Transformation3(Vector2 fromStart, Vector2 fromEnd, Vector2 toStart, Vector2 toEnd, IEnumerable<ZProbe> zProbes)
        : base(fromStart, fromEnd, toStart, toEnd) {
        _zProbes = zProbes.Select(z => (Center: Transform(z.Center), z.T_mm)).ToArray();
    }

    private Transformation3(Transformation2 t, (Vector2 Center, double T_mm)[] zProbeCenters) : base(t) {
        _zProbes = zProbeCenters;
    }

    public Transformation3 Transform3(Transformation2 t)
        => new Transformation3(Transform(t), _zProbes);

    public string Expr(double z_mm, Vector2 xy) {
        if (_zProbes.Any()) {
            (double Weight, double T_mm)[] ws = _zProbes.Select(z => (
                Weight: 1 / ((z.Center - xy).Modulus() + 1e-3), // 1e-3 verhindert /0; ist aber so klein, dass es normale Distanzen zu ZProbes (i.d.R. > 10mm) nicht verfälscht
                z.T_mm)).ToArray();
            double weightSum = ws.Sum(wt => wt.Weight);
            // Ergebnisbeispiel: 12.000(=12.000+0.318*#2001-1.592+0.682*#2002-3.408)
            // Dieses Format muss exakt mit der Regexp-Erkennung von MyAddZAdjustment zusammenpassen!
            // Weil dort keine Klammern gehen, ist hier die Formel weight/weightSum*(#200x - T_mm) ausmultipliziert.
            string result = z_mm.F3() + $"={z_mm.F3()}+{string.Join("+",
                ws.Select((wt, i) => $"{(wt.Weight / weightSum).F3()}*#{2001 + i}-{(wt.T_mm * wt.Weight / weightSum).F3()}"))}".AsComment(0);
            if (!Regex.IsMatch(result, "^" + GCodeConstants.ZAdjustmentExpressionRegex + "$")) {
                throw new Exception($"Internal Error: '{result}' does not match /^{GCodeConstants.ZAdjustmentExpressionRegex}$/");
            }
            return result;
        } else {
            return z_mm.F3();
        }
    }
}
