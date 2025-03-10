namespace de.hmmueller.PathDxf2GCode;

using netDxf;

public class Transformation2 {
    public static readonly Transformation2 Zero = new(Vector2.Zero, Vector2.UnitX, Vector2.Zero, Vector2.UnitX);

    private readonly Vector2 _fromStart;
    private readonly Vector2 _toStart;
    private readonly Vector2 _fromEnd;
    private readonly Vector2 _toEnd;
    private readonly double _rotation_rad;

    public double Rotation_deg { get; }

    protected Transformation2(Transformation2 t) : this(t._fromStart, t._fromEnd, t._toStart, t._toEnd) { }

    public Transformation2(Vector2 fromStart, Vector2 fromEnd, Vector2 toStart, Vector2 toEnd) {
        double fromDistance = Vector2.Distance(fromStart, fromEnd);
        double toDistance = Vector2.Distance(toStart, toEnd);
        if (!fromDistance.Near(toDistance)) {
            throw new ArgumentException($"**** Distanz {fromStart}...{fromEnd} = {fromDistance} ist nicht gleich Distanz {toStart}...{toEnd} = {toDistance}");
        }
        _fromStart = fromStart;
        _fromEnd = fromEnd;
        _toStart = toStart;
        _toEnd = toEnd;

        Vector2 from = fromEnd - fromStart;
        Vector2 to = toEnd - toStart;
        double a_rad = Vector2.AngleBetween(from, to);
        // cos ist für Links- und Rechtsdrehungen leider gleich - deshalb muss man schauen, welche Richtung korrekt ist.
        _rotation_rad = Vector2.Rotate(from, a_rad).Near(to) ? a_rad : Vector2.Rotate(from, -a_rad).Near(to) ? -a_rad : throw new Exception("**** cos liefert keine passende Drehung");
        Rotation_deg = MathHelper.NormalizeAngle(_rotation_rad * MathHelper.RadToDeg);
    }

    public Vector2 Transform(Vector2 p)
        => (p - _fromStart).Rotate(_rotation_rad) + _toStart;

    public Transformation2 Transform(Transformation2 t)
        => new(t._fromStart, t._fromEnd, Transform(t._toStart), Transform(t._toEnd));

    public override string ToString() {
        return $"[ {_fromStart.F3()}=>{_toStart.F3()} / {_fromEnd.F3()}=>{_toEnd.F3()} ]";
    }
}
