namespace de.hmmueller.PathDxf2GCode;

using netDxf;

public static class GCodeHelpers {
    public static void AddComment(this List<GCode> gcodes, string comment, int indent) {
        gcodes.Add(new Comment(comment.AsComment(indent)));
    }

    public static void AddG0H(this List<GCode> gcodes, double x_mm, double y_mm, double minZ_mm, double globalS_mm) {
        gcodes.Add(new G0H(x_mm, y_mm, minZ_mm >= globalS_mm || minZ_mm.Near(globalS_mm)));
    }

    public static void Add(this List<GCode> gcodes, string g) {
        gcodes.Add(new OtherGCode(g));
    }

    public static void DrillOrPullZFromTo(Vector2 pos, double currZ, double targetZ, double t_mm,
                                          double f_mmpmin, Transformation3 zCorr, List<GCode> gcodes, Statistics stats) {
        if (targetZ.Near(currZ)) {
            // schon dort
        } else {
            gcodes.AddComment($"DrillOrPullZFromTo {currZ.F3()} {targetZ.F3()}", 4);
            if (targetZ > t_mm || targetZ > currZ) {
                gcodes.Add($"G00 Z{zCorr.Expr(targetZ, pos)}");
                stats.AddSweepLength(currZ, targetZ);
            } else {
                if (currZ > t_mm) {
                    gcodes.Add($"G00 Z{zCorr.Expr(t_mm, pos)}");
                    stats.AddSweepLength(currZ, t_mm);
                }
                if (!targetZ.Near(t_mm)) {
                    // From t_mm downwards, we drill; TODO: deep holes could be drilled with G81
                    gcodes.Add($"G01 Z{zCorr.Expr(targetZ, pos)}");
                    stats.AddDrillLength(t_mm, targetZ, f_mmpmin);
                }
            }
        }
    }

    public static Vector3 DrillOrPullZFromTo(Vector3 currPos, Vector3 target, double t_mm, double f_mmpmin,
                                             Transformation3 zCorr, List<GCode> gcodes, Statistics stats) {
        DrillOrPullZFromTo(currPos.XY(), currPos.Z, target.Z, t_mm, f_mmpmin, zCorr, gcodes, stats);
        return target;
    }

    public static Vector3 SweepAndDrillSafelyFromTo(Vector3 from, Vector3 to, double t_mm, double sk_mm, 
            double globalS_mm, double f_mmpmin, bool backtracking, 
            Transformation3 zCorr, List<GCode> gcodes, Statistics stats) {
        gcodes.AddComment($"SweepAndDrillSafelyFromTo {from.F3()} {to.F3()} s={sk_mm.F3()} bt={backtracking}", 2);
        Vector2 fromXY = from.XY();
        Vector2 toXY = to.XY();
        if (fromXY.Near(toXY)) {
            DrillOrPullZFromTo(fromXY, from.Z, to.Z, t_mm, f_mmpmin, zCorr, gcodes, stats);
        } else {
            DrillOrPullZFromTo(fromXY, from.Z, sk_mm, t_mm, f_mmpmin, zCorr, gcodes, stats);
            to = SweepFromTo(from, to, globalS_mm, gcodes, stats);
            DrillOrPullZFromTo(toXY, sk_mm, to.Z, t_mm, f_mmpmin, zCorr, gcodes, stats);
        }
        return to;
    }

    public static Vector3 SweepFromTo(Vector3 from, Vector3 to, double globalS_mm, List<GCode> gcodes, Statistics stats) {
        double distance = (to - from).Modulus();
        if (!distance.Near(0)) {
            gcodes.AddG0H(to.X, to.Y, Math.Min(from.Z, to.Z), globalS_mm);
            stats.AddSweepLength(distance);
        }

        return to;
    }

    public static List<GCode> Optimize(this List<GCode> gcodes) {
        // TODO: Optimize sweeps >= globalS_mm
        return gcodes;
    }
}
