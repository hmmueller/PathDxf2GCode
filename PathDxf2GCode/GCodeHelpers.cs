namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using System.Text.RegularExpressions;

public static class GCodeHelpers {
    public static void AddComment(this List<GCode> gcodes, string comment, int indent) {
        gcodes.Add(new CommentGCode(comment.AsComment(indent)));
    }

    public static void AddHorizontalG00(this List<GCode> gcodes, Vector2 to, double lg_mm, double minZ_mm, double globalS_mm) {
        gcodes.Add(new HorizontalSweepGCode(to.X, to.Y, lg_mm));
    }

    public static void AddNonhorizontalG00(this List<GCode> gcodes, string g, double lg_mm) {
        gcodes.Add(new NonhorizontalSweepGCode(g, lg_mm));
    }

    public static void Add(this List<GCode> gcodes, string g) {
        gcodes.Add(new OtherGCode(g));
    }

    public static void AddMill(this List<GCode> gcodes, string g, double dist_mm, double f_mmpmin) {
        gcodes.Add(new MillGCode(g, dist_mm, f_mmpmin));
    }

    public static void AddDrill(this List<GCode> gcodes, string g, double dist_mm, double f_mmpmin) {
        gcodes.Add(new DrillGCode(g, dist_mm, f_mmpmin));
    }

    public static void DrillOrPullZFromTo(Vector2 pos, double currZ, double targetZ, double t_mm,
                                          double f_mmpmin, Transformation3 zCorr, List<GCode> gcodes) {
        if (targetZ.Near(currZ)) {
            // schon dort
        } else {
            gcodes.AddComment($"DrillOrPullZFromTo {currZ.F3()} {targetZ.F3()}", 4);
            if (targetZ > t_mm || targetZ > currZ) {
                gcodes.AddNonhorizontalG00($"G00 Z{zCorr.Expr(targetZ, pos)}", Math.Abs(currZ -targetZ));
            } else {
                if (currZ > t_mm) {
                    gcodes.AddNonhorizontalG00($"G00 Z{zCorr.Expr(t_mm, pos)}", Math.Abs(currZ- t_mm));
                }
                if (!targetZ.Near(t_mm)) {
                    // From t_mm downwards, we drill; TODO: deep holes could be drilled with G81
                    gcodes.AddDrill($"G01 Z{zCorr.Expr(targetZ, pos)}", Math.Abs(t_mm - targetZ), f_mmpmin);
                }
            }
        }
    }

    public static Vector3 DrillOrPullZFromTo(Vector3 currPos, Vector3 target, double t_mm, double f_mmpmin,
                                             Transformation3 zCorr, List<GCode> gcodes) {
        DrillOrPullZFromTo(currPos.XY(), currPos.Z, target.Z, t_mm, f_mmpmin, zCorr, gcodes);
        return target;
    }

    public static Vector3 SweepAndDrillSafelyFromTo(Vector3 from, Vector3 to, double t_mm, double s_mm,
            double globalS_mm, double f_mmpmin, bool backtracking,
            Transformation3 zCorr, List<GCode> gcodes) {
        gcodes.AddComment($"SweepAndDrillSafelyFromTo {from.F3()} {to.F3()} s={s_mm.F3()} bt={backtracking}", 2);
        Vector2 fromXY = from.XY();
        Vector2 toXY = to.XY();
        if (fromXY.Near(toXY)) {
            DrillOrPullZFromTo(fromXY, from.Z, to.Z, t_mm, f_mmpmin, zCorr, gcodes);
        } else {
            DrillOrPullZFromTo(fromXY, from.Z, s_mm, t_mm, f_mmpmin, zCorr, gcodes);
            SweepFromTo(fromXY.AsVector3(s_mm), to, globalS_mm, gcodes);
            DrillOrPullZFromTo(toXY, s_mm, to.Z, t_mm, f_mmpmin, zCorr, gcodes);
        }
        return to;
    }

    public static Vector3 SweepFromTo(Vector3 from, Vector3 to, double globalS_mm, List<GCode> gcodes) {
        double distance = (to - from).Modulus();
        if (!distance.Near(0)) {
            gcodes.AddHorizontalG00(to.XY(), distance, Math.Min(from.Z, to.Z), globalS_mm);
        }

        return to;
    }

    private static bool IsMatch(List<GCode> gcodes, string pattern, out Match match) {
        string s = new string(gcodes.Select(g => g.Letter).ToArray());
        match = Regex.Match(s, pattern);
        return match.Success;
    }

    public static List<GCode> Optimize(this List<GCode> gcodes) {
        for (int n = 0; ; n++) {
            if (n > gcodes.Count) {
                throw new Exception($"Internal error - Optimize ran for more than {gcodes.Count} iterations");
            }
            if (IsMatch(gcodes, "(HC*)+H", out Match match)) {
                int lastIndex = match.Index + match.Length - 1; // -1, because the last G00 should survive
                IEnumerable<GCode> replacement = gcodes[match.Index..lastIndex]
                    .Select(g => g is CommentGCode ? g : g is HorizontalSweepGCode ? new CommentGCode("; " + g.AsString()) : g);
                gcodes = gcodes[..match.Index].Concat(replacement).Concat(gcodes[lastIndex..]).ToList();
            } else {
                break;
            }
        }
        return gcodes;
    }
}
