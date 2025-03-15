namespace de.hmmueller.PathDxf2GCode.Tests;

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
}

