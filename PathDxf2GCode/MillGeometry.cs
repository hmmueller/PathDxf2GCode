namespace de.hmmueller.PathDxf2GCode;

using netDxf;

public interface IMillGeometry {
    Vector2 Start { get; }
    Vector2 End { get; }
    double Length_mm { get; }

    IMillGeometry CloneReversed();
    bool Equals(IMillGeometry g);
    Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                      List<GCode> gcodes, string dxfFileName, double millingTarget_mm,
                      double t_mm, double f_mmpmin, bool backtracking);
    IMillGeometry Section(double from_mm, double lg_mm);
}

public class LineGeometry : IMillGeometry {
    public Vector2 Start { get; }
    public Vector2 End { get; }

    public LineGeometry(Vector2 start, Vector2 end) {
        Start = start;
        End = end;
    }

    public double Length_mm
        => (End - Start).Modulus();

    public IMillGeometry CloneReversed()
        => new LineGeometry(End, Start);

    public LineGeometry Transform(Transformation2 t)
        => new(t.Transform(Start), t.Transform(End));

    public bool Equals(IMillGeometry g)
        => g is LineGeometry li && li.Start.Near(Start) && li.End.Near(End);

    public Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                                      List<GCode> gcodes, string dxfFileName, double millingTarget_mm,
                                      double t_mm, double f_mmpmin, bool backtracking) {
        LineGeometry l = Transform(t);
        currPos = GCodeHelpers.DrillOrPullZFromTo(currPos, l.Start.AsVector3(millingTarget_mm), t_mm, f_mmpmin, t, gcodes);
        gcodes.AddComment($"MillLine s={l.Start.F3()} e={l.End.F3()} h={millingTarget_mm.F3()} bt={backtracking}", 2);

        gcodes.AddMill($"G01 F{f_mmpmin.F3()} X{l.End.X.F3()} Y{l.End.Y.F3()} Z{t.Expr(millingTarget_mm, l.Start)}",
            (l.End - l.Start).Modulus(), f_mmpmin);

        return l.End.AsVector3(millingTarget_mm);
    }

    private Vector2 At(double f_mm)
        => Start + (End - Start).Scaled(f_mm);

    public IMillGeometry Section(double from_mm, double lg_mm)
        => new LineGeometry(At(from_mm), At(from_mm + lg_mm));
}

public class ArcGeometry : IMillGeometry {
    public ArcGeometry(Vector2 center, double radius_mm, double startAngle_deg, double endAngle_deg, bool counterclockwise) {
        Center = center;
        if (radius_mm <= 0) {
            throw new ArgumentException(nameof(radius_mm), "<= 0 nicht erlaubt");
        }
        Radius_mm = radius_mm;
        StartAngle_deg = MathHelper.NormalizeAngle(startAngle_deg);
        EndAngle_deg = MathHelper.NormalizeAngle(endAngle_deg);
        Counterclockwise = counterclockwise;
    }

    public Vector2 Center { get; }
    public double Radius_mm { get; }
    private double StartAngle_deg { get; }
    private double EndAngle_deg { get; }
    private bool Counterclockwise { get; }

    public Vector2 Start
        => Vector2.Polar(Center, Radius_mm, StartAngle_deg * MathHelper.DegToRad);
    public Vector2 End
        => Vector2.Polar(Center, Radius_mm, EndAngle_deg * MathHelper.DegToRad);

    public double Length_mm 
        => MathHelper.TwoPI * Radius_mm * MathHelper.NormalizeAngle(
             Direction * (EndAngle_deg - StartAngle_deg)) / 360;
 
    public IMillGeometry CloneReversed()
        => new ArcGeometry(Center, Radius_mm, EndAngle_deg, StartAngle_deg, !Counterclockwise);

    public ArcGeometry Transform(Transformation2 t)
        => new(t.Transform(Center), Radius_mm, StartAngle_deg + t.Rotation_deg, EndAngle_deg + t.Rotation_deg, Counterclockwise);

    public bool Equals(IMillGeometry g)
        => g is ArcGeometry arc && Center.Near(arc.Center) && Radius_mm.Near(arc.Radius_mm)
            && StartAngle_deg.Near(arc.StartAngle_deg) && EndAngle_deg.Near(arc.EndAngle_deg)
            && Counterclockwise == arc.Counterclockwise;

    public Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                                      List<GCode> gcodes, string dxfFileName, double millingTarget_mm,
                                      double t_mm, double f_mmpmin, bool backtracking) {
        ArcGeometry a = Transform(t);
        currPos = GCodeHelpers.DrillOrPullZFromTo(currPos, a.Start.AsVector3(millingTarget_mm), t_mm, f_mmpmin, t, gcodes);

        gcodes.AddComment($"MillArc l={a.Center.F3()} r={Radius_mm.F3()} a0={a.StartAngle_deg.F3()} a1={a.EndAngle_deg.F3()} h={millingTarget_mm.F3()} p0={a.Start.F3()} p1={a.End.F3()} bt={backtracking}", 2);
        string g = Counterclockwise ? "G03" : "G02";

        gcodes.AddMill($"{g} F{f_mmpmin.F3()} I{(a.Center.X - a.Start.X).F3()} J{(a.Center.Y - a.Start.Y).F3()} X{a.End.X.F3()} Y{a.End.Y.F3()} Z{t.Expr(millingTarget_mm, a.Start)}",
            Radius_mm * MathHelper.NormalizeAngle(Direction * (EndAngle_deg - StartAngle_deg)) * MathHelper.DegToRad, f_mmpmin);

        return a.End.AsVector3(millingTarget_mm);
    }

    private int Direction => Counterclockwise ? 1 : -1;

    private double AngleAt_deg(double f_mm)
        => StartAngle_deg + Direction * f_mm / (MathHelper.TwoPI * Radius_mm) * 360;

    public IMillGeometry Section(double from_mm, double lg_mm)
        => new ArcGeometry(Center, Radius_mm, AngleAt_deg(from_mm), AngleAt_deg(from_mm + lg_mm), Counterclockwise);
}

public static class MillGeometryHelper {
    public static IMillGeometry[] CreateBarGeometries(this IMillGeometry g, double o_mm, double p_mm, double u_mm) {
        double lg_mm = g.Length_mm;
        int nSections = (int)Math.Floor(lg_mm / (u_mm + p_mm));
        if (nSections == 0) {
            throw new ArgumentException("Internal error: nSections == 0; CreateBarGeometries must be called with large enough lg_mm!", nameof(lg_mm));
        }
        double v_mm = lg_mm / nSections - p_mm; // v is "u, extended for integral number of bars"

        // Relationship between P, v, edgeBarLg, betweenLg, and barLg:
        // ------+       +------------+       +------
        // ##### +-------+ ########## +-------+ #####
        // <P/2><    v    ><    P   ><    v    ><P/2>
        // <        vp         ><        vp         >
        //       <        vp         >
        // <eBLg><betw.Lg><  barLg   ><betw.Lg><eBLg>

        double vp_mm = v_mm + p_mm;
        double edgeBarMillLg_mm = p_mm / 2 + o_mm / 2;
        double betweenMillLg_mm = v_mm - o_mm;
        double barMillLg_mm = p_mm + o_mm;

        IMillGeometry[] result = [
                g.Section(0, edgeBarMillLg_mm), // left edge bar
                    ..Enumerable.Range(0, nSections - 1).SelectMany<int,IMillGeometry>(i => [
                        g.Section(edgeBarMillLg_mm + i * vp_mm, betweenMillLg_mm), // between
                        g.Section(edgeBarMillLg_mm + i * vp_mm + betweenMillLg_mm, barMillLg_mm) // bar
                    ]),
                    g.Section(lg_mm - edgeBarMillLg_mm - betweenMillLg_mm, betweenMillLg_mm), // last between
                    g.Section(lg_mm - edgeBarMillLg_mm, edgeBarMillLg_mm) // right edge bar
            ];
        return result;
    }

    public static Vector3 MillSupports(this IMillGeometry[] supportGeometries, Vector3 currPos, double millingBottom_mm, bool start2End, double globalS_mm, Transformation3 t, List<GCode> gcodes, string dxfFileName, IParams pars, bool backtracking) {
        bool inBar = true;
        foreach (var sg in start2End ? supportGeometries : supportGeometries.Reverse()) {
            if (inBar) {
                gcodes.AddComment("SupportBar", 2);
            }
            currPos = (start2End ? sg : sg.CloneReversed()).EmitGCode(currPos, t, globalS_mm, gcodes, dxfFileName,
                    millingTarget_mm: Math.Max(millingBottom_mm, inBar ? pars.D_mm : pars.B_mm),
                    t_mm: pars.T_mm, f_mmpmin: pars.F_mmpmin, backtracking);
            inBar = !inBar;
        }

        return currPos;
    }
}