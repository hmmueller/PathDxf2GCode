namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using System.IO;

public static class GCodeHelpers {
    public static void DrillOrPullZFromTo(Vector2 pos, double currZ, double targetZ, double t_mm, 
                                          double f_mmpmin, Transformation3 zCorr, StreamWriter sw, Statistics stats) {
        if (targetZ.Near(currZ)) {
            // schon dort
        } else {
            sw.WriteLine($"DrillOrPullZFromTo {currZ.F3()} {targetZ.F3()}".AsComment(4));
            if (targetZ > t_mm || targetZ > currZ) {
                sw.WriteLine($"G00 Z{zCorr.Expr(targetZ, pos)}");
                stats.AddSweepLength(currZ, targetZ);
            } else {
                if (currZ > t_mm) {
                    sw.WriteLine($"G00 Z{zCorr.Expr(t_mm, pos)}");
                    stats.AddSweepLength(currZ, t_mm);
                }
                if (!targetZ.Near(t_mm)) {
                    // Ab t_mm wird gebohrt; TODO: Wenn tiefes Loch (> C*O), dann G81-Bohren
                    sw.WriteLine($"G01 Z{zCorr.Expr(targetZ, pos)}");
                    stats.AddDrillLength(t_mm, targetZ, f_mmpmin);
                }
            }
        }
    }

    public static Vector3 DrillOrPullZFromTo(Vector3 currPos, Vector3 target, double t_mm, double f_mmpmin, 
                                             Transformation3 zCorr, StreamWriter sw, Statistics stats) {
        DrillOrPullZFromTo(currPos.XY(), currPos.Z, target.Z, t_mm, f_mmpmin, zCorr, sw, stats);
        return target;
    }

    public static Vector3 SweepAndDrillSafelyFromTo(Vector3 from, Vector3 to, double t_mm, double sk_mm, double f_mmpmin, 
                                                    bool backtracking, Transformation3 zCorr, StreamWriter sw, Statistics stats) {
        sw.WriteLine($"SweepAndDrillSafelyFromTo {from.F3()} {to.F3()} s={sk_mm.F3()} bt={backtracking}".AsComment(2));
        Vector2 fromXY = from.XY();
        Vector2 toXY = to.XY();
        if (fromXY.Near(toXY)) {
            DrillOrPullZFromTo(fromXY, from.Z, to.Z, t_mm, f_mmpmin, zCorr, sw, stats);
        } else {
            DrillOrPullZFromTo(fromXY, from.Z, sk_mm, t_mm, f_mmpmin, zCorr, sw, stats);
            to = SweepFromTo(from, to, sw, stats);
            DrillOrPullZFromTo(toXY, sk_mm, to.Z, t_mm, f_mmpmin, zCorr, sw, stats);
        }
        return to;
    }

    public static Vector3 SweepFromTo(Vector3 from, Vector3 to, StreamWriter sw, Statistics stats) {
        double distance = (to - from).Modulus();
        if (!distance.Near(0)) {
            sw.WriteLine($"G00 X{to.X.F3()} Y{to.Y.F3()}");
            stats.AddSweepLength(distance);
        }

        return to;
    }
}

