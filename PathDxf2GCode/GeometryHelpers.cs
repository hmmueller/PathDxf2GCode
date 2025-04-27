namespace de.hmmueller.PathDxf2GCode;

using static System.FormattableString;
using netDxf;

public static class GeometryHelpers {
    #region General

    public static void Assert(bool b, string errorContext, string message) {
        if (!b) {
            throw new EmitGCodeException(errorContext, message);
        }
    }

    #endregion

    #region double

    public const double RELATIVE_EPS = 1e-6;

    public static bool Near(this double d, double e, double relativeEps)
        => Math.Abs(d - e) <= relativeEps * (Math.Max(Math.Abs(d), Math.Abs(e)) + 0.001);

    public static bool Near(this double d, double e)
        => Near(d, e, RELATIVE_EPS);

    public static bool AbsNear(this double d, double e, double absoluteEps)
        => Math.Abs(d - e) <= absoluteEps;

    public static bool Between(this double a, double b, double c)
        => a >= b && a <= c;

    public static bool Ge(this double d, double e)
        => d >= e || d.Near(e);

    public static bool Gt(this double d, double e)
        => d > e && !d.Near(e);

    public static bool Le(this double d, double e)
        => e.Ge(d);

    #endregion

    #region Angles

    public static bool AngleIsInArc(this double angle, double startAngle, double endAngle)
        => startAngle < endAngle
            ? angle.Between(startAngle, endAngle)
            : angle >= startAngle || angle <= endAngle;

    #endregion

    #region Vectors

    public static bool Near(this Vector2 d, Vector2 e)
        => Vector2.SquareDistance(d, e).Near(0);

    public static bool AbsNear(this Vector2 d, Vector2 e, double absoluteEps)
        => Vector2.Distance(d, e).AbsNear(0, absoluteEps);

    public static Vector2 AsVector2(this Vector3 a)
        => a.Z.Near(0) ? new Vector2(a.X, a.Y) : throw new ArgumentOutOfRangeException($"**** ({a}).Z != 0 - cannot convert to Vector2");

    public static Vector2 Value(this Vector2? a)
        => a ?? throw new NullReferenceException();

    public static Vector2 Scaled(this Vector2 a, double f)
        => a / a.Modulus() * f;

    public static Vector2 XY(this Vector3 a)
        => new Vector2(a.X, a.Y);

    public static double Distance(this Vector2 a, Vector3 b)
        => a.Distance(b.AsVector2());

    public static double Distance(this Vector2 a, Vector2 b)
        => Vector2.Distance(a, b);

    public static double SquareDistance(this Vector2 a, Vector3 b)
        => Vector2.SquareDistance(a, b.AsVector2());

    public static double Distance(this Vector3 a, Vector3 b)
        => Vector3.Distance(a, b);

    public static Vector2 Rotate(this Vector2 a, double angle_rad)
        => Vector2.Rotate(a, angle_rad);

    public static Vector3 AsVector3(this Vector2 a, double z)
        => new(a.X, a.Y, z);

    #endregion

    #region Formatting

    public static string F3(this Vector3 a)
        => Invariant($"[{a.X:F3}|{a.Y:F3}|{a.Z:F3}]");

    public static string F3(this Vector2 a)
        => Invariant($"[{a.X:F3}|{a.Y:F3}]");

    public static string F3(this double d)
        => Invariant($"{d:F3}");

    public static string AsComment(this string s, int indent)
        => "(".PadLeft(indent + 1) + s.Trim().Replace(')', ']').Replace('\\', '/') + ")";

    #endregion
}
