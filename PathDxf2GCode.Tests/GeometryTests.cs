namespace de.hmmueller.PathDxf2GCode.Tests;

using netDxf;

[TestClass]
public class GeometryTests {
    [TestMethod]
    public void Assert0Near0() {
        Assert.IsTrue(0d.Near(0));
    }

    [TestMethod]
    [DataRow(-1000)]
    [DataRow(-0.001)]
    [DataRow(0.001)]
    [DataRow(1)]
    [DataRow(1000)]
    public void AssertXNearX(double d) {
        Assert.IsTrue(d.Near(d));
    }

    [TestMethod]
    public void AngleBetweenisNice() {
        for (double a = 5; a < 360; a += 20) {
            for (double c = a + 10; c < 360; c += 20) {
                double b = (a + c) / 2;
                Assert.IsTrue(b.AngleIsInArc(a, c), $"Wrong for {a} {b} {c}");
                Assert.IsTrue((b + 180).AngleIsInArc(c, a), $"Wrong for {c} {b} {a}");
            }
        }
    }

    [TestMethod]
    public void DistanceIsZero() {
        Assert.AreEqual(0, MathHelper.PointLineDistance(new Vector2(48, 200), new Vector2(80, 200), Vector2.Normalize(new Vector2(5, 0))));
    }

}

