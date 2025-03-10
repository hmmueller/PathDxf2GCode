﻿namespace de.hmmueller.PathDxf2GCode;

using static System.FormattableString;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Entities;
using netDxf.Tables;

public static class GeometryHelpers {
    private const double EPS = 1e-7;

    public static bool Near(this double d, double e, double eps)
        => Math.Abs(d - e) / (Math.Abs(d) + Math.Abs(e) + eps) < eps;

    public static bool Near(this double d, double e)
        => Near(d, e, EPS);

    public static bool Near(this Vector2 d, Vector2 e)
        => Vector2.SquareDistance(d, e).Near(0);

    public static Vector2 AsVector2(this Vector3 a)
        => a.Z.Near(0) ? new Vector2(a.X, a.Y) : throw new ArgumentOutOfRangeException($"**** ({a}).Z != 0 - cannot convert to Vector2");

    public static Vector2 Value(this Vector2? a)
        => a ?? throw new NullReferenceException();

    public static Vector2 XY(this Vector3 a)
        => new Vector2(a.X, a.Y);

    public static string F3(this Vector3 a)
        => Invariant($"[{a.X:F3}|{a.Y:F3}|{a.Z:F3}]");

    public static string F3(this Vector2 a)
        => Invariant($"[{a.X:F3}|{a.Y:F3}]");

    public static string F3(this double d)
        => Invariant($"{d:F3}");

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

    public static bool Between(this double a, double b, double c)
        => a >= b && a <= c;

    public static bool AngleIsInArc(this double angle, double startAngle, double endAngle) 
        => startAngle < endAngle
            ? angle.Between(startAngle, endAngle)
            : angle >= startAngle || angle <= endAngle;

    public static bool IsOnPathLayer(this EntityObject e, string pathNamePattern, string fileNameForMessages)
        => Regex.IsMatch(new PathName(e.Layer.Name, fileNameForMessages).AsString(), "^" + pathNamePattern + "$");

    public static PathName? AsPathReference(this string text, string pathNamePattern, string fileNameForMessages) {
        Match m = Regex.Match(text, pathNamePattern);
        return m.Success ? new PathName(m.Value, fileNameForMessages) : null;
    }

    public static Linetype GetLinetype(this EntityObject e, Dictionary<string, Linetype> layerLinetypes)
        => e.Linetype.IsByLayer ? layerLinetypes[e.Layer.Name] : e.Linetype;

    public static Vector3 AsVector3(this Vector2 a, double z)
        => new(a.X, a.Y, z);

    public static string AsComment(this string s, int indent)
        => "(".PadLeft(indent + 1) + s.Trim().Replace(')', ']').Replace('\\', '/') + ")";
}
