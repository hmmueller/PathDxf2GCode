namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using System.IO;

public abstract class MillGeometry {
    public abstract Vector2 Start { get; }
    public abstract Vector2 End { get; }

    public abstract MillGeometry CloneReversed();
    public abstract bool Equals(MillGeometry g);
    public abstract Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm, 
                                      List<GCode> gcodes, string dxfFileName, double millingTarget_mm, 
                                      double t_mm, double f_mmpmin, bool backtracking);
}

public class LineGeometry : MillGeometry {
    public override Vector2 Start { get; }
    public override Vector2 End { get; }

    public LineGeometry(Vector2 start, Vector2 end) {
        Start = start;
        End = end;
    }

    public override MillGeometry CloneReversed()
        => new LineGeometry(End, Start);

    public LineGeometry Transform(Transformation2 t)
        => new(t.Transform(Start), t.Transform(End));

    public override bool Equals(MillGeometry g)
        => g is LineGeometry li && li.Start.Near(Start) && li.End.Near(End);

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm, 
                                      List<GCode> gcodes, string dxfFileName, double millingTarget_mm, 
                                      double t_mm, double f_mmpmin, bool backtracking) {
        LineGeometry l = Transform(t);
        currPos = GCodeHelpers.DrillOrPullZFromTo(currPos, l.Start.AsVector3(millingTarget_mm), t_mm, f_mmpmin, t, gcodes);
        gcodes.AddComment($"MillLine s={l.Start.F3()} e={l.End.F3()} h={millingTarget_mm.F3()} bt={backtracking}", 2);

        gcodes.AddMill($"G01 F{f_mmpmin.F3()} X{l.End.X.F3()} Y{l.End.Y.F3()} Z{t.Expr(millingTarget_mm, l.Start)}",
            (l.End - l.Start).Modulus(), f_mmpmin);

        return l.End.AsVector3(millingTarget_mm);
    }
}

public class ArcGeometry : MillGeometry {
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
    public double StartAngle_deg { get; }
    public double EndAngle_deg { get; }
    public bool Counterclockwise { get; }

    public override Vector2 Start
        => Vector2.Polar(Center, Radius_mm, StartAngle_deg * MathHelper.DegToRad);
    public override Vector2 End
        => Vector2.Polar(Center, Radius_mm, EndAngle_deg * MathHelper.DegToRad);

    public override MillGeometry CloneReversed()
        => new ArcGeometry(Center, Radius_mm, EndAngle_deg, StartAngle_deg, !Counterclockwise);

    public ArcGeometry Transform(Transformation2 t)
        => new(t.Transform(Center), Radius_mm, StartAngle_deg + t.Rotation_deg, EndAngle_deg + t.Rotation_deg, Counterclockwise);

    public override bool Equals(MillGeometry g)
        => g is ArcGeometry arc && Center.Near(arc.Center) && Radius_mm.Near(arc.Radius_mm) 
            && StartAngle_deg.Near(arc.StartAngle_deg) && EndAngle_deg.Near(arc.EndAngle_deg) 
            && Counterclockwise == arc.Counterclockwise;

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm, 
                                      List<GCode> gcodes, string dxfFileName, double millingTarget_mm, 
                                      double t_mm, double f_mmpmin, bool backtracking) {
        ArcGeometry a = Transform(t);
        currPos = GCodeHelpers.DrillOrPullZFromTo(currPos, a.Start.AsVector3(millingTarget_mm), t_mm, f_mmpmin, t, gcodes);

        gcodes.AddComment($"MillArc l={a.Center.F3()} r={Radius_mm.F3()} a0={a.StartAngle_deg.F3()} a1={a.EndAngle_deg.F3()} h={millingTarget_mm.F3()} p0={a.Start.F3()} p1={a.End.F3()} bt={backtracking}", 2);
        string g = Counterclockwise ? "G03" : "G02";

        gcodes.AddMill($"{g} F{f_mmpmin.F3()} I{(a.Center.X - a.Start.X).F3()} J{(a.Center.Y - a.Start.Y).F3()} X{a.End.X.F3()} Y{a.End.Y.F3()} Z{t.Expr(millingTarget_mm, a.Start)}",
            Radius_mm * MathHelper.NormalizeAngle(Counterclockwise ? EndAngle_deg - StartAngle_deg : StartAngle_deg - EndAngle_deg) * MathHelper.DegToRad, f_mmpmin);

        return a.End.AsVector3(millingTarget_mm);
    }
}
