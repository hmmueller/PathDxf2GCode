namespace de.hmmueller.PathDxf2GCode.Tests;

using netDxf;

[TestClass]
public class TransformationTests {
    private readonly static Vector2 zero = Vector2.Zero;
    private readonly static Vector2 x0 = Vector2.UnitX;
    private readonly static Vector2 y0 = Vector2.UnitY;

    [TestMethod]
    public void ZeroDoesNothingToLineGeometry() {
        var geometry = new LineGeometry(x0, y0);
        MillGeometry transformed = geometry.Transform(Transformation2.Zero);
        Assert.IsTrue(geometry.Equals(transformed));
    }

    [TestMethod]
    public void ZeroDoesNothingToArcGeometry() {
        var geometry = new ArcGeometry(x0, 5, 200, 400, true);
        MillGeometry transformed = geometry.Transform(Transformation2.Zero);
        Assert.IsTrue(geometry.Equals(transformed));
    }

    [TestMethod]
    public void RotateLineBy90Deg() {
        var geometry = new LineGeometry(x0, y0);
        Transformation2 t = new(zero, y0, zero, x0);
        MillGeometry transformed = geometry.Transform(t);
        Assert.IsTrue(transformed.Equals(new LineGeometry(-y0, x0)));
    }

    [TestMethod]
    public void RotateArcBy90Deg() {
        var geometry = new ArcGeometry(-x0, 5, 200, 400, true);
        Transformation2 t = new(zero, y0, zero, x0);
        MillGeometry transformed = geometry.Transform(t);
        Assert.IsTrue(transformed.Equals(new ArcGeometry(y0, 5, 200 - 90, 400 - 90, true)));
    }
    
    [TestMethod]
    public void ShiftLineByUnitX() {
        var geometry = new LineGeometry(x0, y0);
        Transformation2 t = new(zero, y0, x0, x0 + y0);
        MillGeometry transformed = geometry.Transform(t);
        Assert.IsTrue(transformed.Equals(new LineGeometry(2 * x0, x0 + y0)));
    }

    [TestMethod]
    public void ShiftArcByUnitX() {
        var geometry = new ArcGeometry(-x0, 5, 200, 400, true);
        Transformation2 t = new(zero, y0, x0, x0 + y0);
        MillGeometry transformed = geometry.Transform(t);
        Assert.IsTrue(transformed.Equals(new ArcGeometry(zero, 5, 200, 400, true)));
    }
}