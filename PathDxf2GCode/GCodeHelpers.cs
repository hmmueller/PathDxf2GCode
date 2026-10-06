namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using System.Text.RegularExpressions;

public static class GCodeHelpers {
    public static void AddComment(this List<GCode> gcodes, string comment, int indent) {
        gcodes.Add(new CommentGCode(comment.AsComment(indent)));
    }

    public static void AddHorizontalG00(this List<GCode> gcodes, Vector2 to, double lg_mm) {
        gcodes.Add(new HorizontalSweepGCode(to.X, to.Y, lg_mm));
    }

    public static void AddNonhorizontalG00(this List<GCode> gcodes, string g, double lg_mm) {
        gcodes.Add(new NonhorizontalSweepGCode(g, lg_mm));
    }

    public static void Add(this List<GCode> gcodes, string g) {
        gcodes.Add(new OtherGCode(g));
    }

    public static void AddMill(this List<GCode> gcodes, string g, double dist_mm, double fg_mmpmin) {
        gcodes.Add(new MillGCode(g, dist_mm, fg_mmpmin));
    }

    public static void AddDrill(this List<GCode> gcodes, string g, double dist_mm, double g_mmpmin) {
        gcodes.Add(new DrillGCode(g, dist_mm, g_mmpmin));
    }

    public static double Speed_mmpmin(this IParams pars, double dz_mm, double dxy_mm) {
        // with a = atan(dz/dxy):
        //   v_z = v sin a <= F_z
        //   v_xy = v cos a <= F_xy
        // =>
        //   v <= F_z / sin a
        //   v <= F_xy / cos a
        // =>
        //   v = min(F_z / sin a, F_xy / cos a)
        // I limit the speed to F_xy, because cos a is typically near 1, so
        // ramp speeds would only be marginally larger; and unexpected:
        //   v = min(F_z / sin a, F_xy)
        double angle_rad = Math.Atan2(dz_mm, dxy_mm);
        return Math.Min(pars.G_mmpmin / Math.Sin(angle_rad), pars.F_mmpmin);
    }

    public static Vector3 DrillOrPullZFromTo(Vector3 currPos, double toZ_mm, double th_mm,
                                             double g_mmpmin, Transformation3 zCorr, List<GCode> gcodes) {
        return MillOrPullZTo(currPos, toZ_mm, th_mm,
                          beforeDrillPos => DrillFromTo(beforeDrillPos, toZ_mm, g_mmpmin, zCorr, gcodes),
                          zCorr, gcodes);
    }

    public static Vector3 DrillFromTo(Vector3 currPos, double toZ_mm, double g_mmpmin, Transformation3 zCorr, List<GCode> gcodes) {
        gcodes.AddDrill($"G01 Z{zCorr.Expr(toZ_mm, currPos.XY())}", Math.Abs(currPos.Z - toZ_mm), g_mmpmin);
        return currPos.XY().AsVector3(toZ_mm);
    }

    public static Vector3 MillOrPullZTo(Vector3 currPos, double toZ_mm, double th_mm,
                                          Action<Vector3> millLastLeg, Transformation3 zCorr, List<GCode> gcodes) {
        Vector2 currPosXY = currPos.XY();
        double currZ_mm = currPos.Z;
        if (toZ_mm.Near(currZ_mm)) {
            // schon dort
        } else {
            gcodes.AddComment($"DrillOrPullZFromTo {currZ_mm.F3()} {toZ_mm.F3()}", 4);
            if (toZ_mm > th_mm || toZ_mm > currZ_mm) {
                gcodes.AddNonhorizontalG00($"G00 Z{zCorr.Expr(toZ_mm, currPosXY)}", Math.Abs(currZ_mm - toZ_mm));
            } else {
                if (currZ_mm > th_mm) {
                    gcodes.AddNonhorizontalG00($"G00 Z{zCorr.Expr(th_mm, currPosXY)}", Math.Abs(currZ_mm - th_mm));
                    currZ_mm = th_mm;
                }
                if (!toZ_mm.Near(th_mm)) {
                    millLastLeg(currPosXY.AsVector3(currZ_mm));
                }
            }
        }
        return currPosXY.AsVector3(toZ_mm);
    }

    public static Vector3 DrillOrPullZFromTo(Vector3 from, Vector3 target, double th_mm, double g_mmpmin,
                                             Transformation3 zCorr, List<GCode> gcodes) {
        return DrillOrPullZFromTo(from, target.Z, th_mm, g_mmpmin, zCorr, gcodes);
    }

    public static Vector3 SweepAndDrillSafelyFromTo(Vector3 from, Vector3 to, double th_mm, double s_mm,
            double g_mmpmin, bool backtracking, Transformation3 zCorr, List<GCode> gcodes) {
        Vector3 currPos = PullAndSweepHorizontallyFromTo(from, to.XY(), th_mm, s_mm, g_mmpmin, zCorr, gcodes);
        return DrillOrPullZFromTo(currPos, to.Z, th_mm, g_mmpmin, zCorr, gcodes);
    }

    public static Vector3 PullAndSweepHorizontallyFromTo(Vector3 from, Vector2 to,
            double th_mm, double s_mm, double g_mmpmin, Transformation3 zCorr, List<GCode> gcodes) {
        gcodes.AddComment($"PullAndSweepHorizontallyFromTo {from.F3()} {to.F3()} s={s_mm.F3()}", 2);
        if (from.XY().Near(to)) {
            return from;
        } else {
            Vector3 currPos = DrillOrPullZFromTo(from, s_mm, th_mm, g_mmpmin, zCorr, gcodes);
            return SweepHorizontallyFromTo(currPos, to, gcodes);
        }
    }

    public static Vector3 SweepHorizontallyFromTo(Vector3 from, Vector2 to, List<GCode> gcodes) {
        double distance = (to - from.XY()).Modulus();
        if (!distance.Near(0)) {
            gcodes.AddHorizontalG00(to, distance);
        }
        return to.AsVector3(from.Z);
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
