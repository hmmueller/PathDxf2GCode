namespace de.hmmueller.PathDxf2GCode.Tests;

using netDxf;

[TestClass]
public class MillGeometryTests {
    [TestMethod]
    [DataRow(25, 3)] // lg=25 >= 2.5+20+2.5 => 3
    [DataRow(50, 5)] // lg=50 >= 2.5+20+5+20+2.5 => 5
    public void TestCreateBarGeometriesOnLine(double lg_mm, int expectedGeometryCount) {
        IMillGeometry g = new LineGeometry(new Vector2(10, 10), new Vector2(10 + lg_mm, 10));
        IMillGeometry[] result = g.CreateBarGeometries(o_mm: 2, p_mm: 5, u_mm: 20);
        Assert.AreEqual(expectedGeometryCount, result.Length);
        CollectionAssert.AllItemsAreInstancesOfType(result, typeof(LineGeometry));
    }

    [TestMethod]
    [DataRow(24.9)] // lg=24.9 < 2.5+20+2.5 => n = 0 => Exception
    public void TestCreateBarGeometriesOnLineThrows(double lg_mm) {
        Assert.ThrowsException<ArgumentException>(() => {
            IMillGeometry g = new LineGeometry(new Vector2(10, 10), new Vector2(10 + lg_mm, 10));
            g.CreateBarGeometries(o_mm: 2, p_mm: 5, u_mm: 20);
        });
    }

    [TestMethod]
    [DataRow(10.7, 45, 180, 3)]    // lg=2*10.7*3.14*(3/8)=25.21   >= 2.5+20+2.5 => 3
    [DataRow(15, 45, 180, 3)]      // lg=2*15*3.14*(3/8)=35.34     >= 2.5+20+2.5 => 3
    [DataRow(15, 45, 45 + 190, 3)] // lg=2*15*3.14*(190/360)=49.74 <  2.5+20+5+20+2.5 => 3
    [DataRow(15, 45, 45 + 191, 5)] // lg=2*15*3.14*(191/360)=50.00 >= 2.5+20+5+20+2.5 => 5
    public void TestCreateBarGeometriesOnArc(double radius_mm, double startAngle_deg, double endAngle_deg, int expectedGeometryCount) {
        Vector2 center = new(10, 10);
        IMillGeometry g = new ArcGeometry(center, radius_mm, startAngle_deg, endAngle_deg, counterclockwise: true);
        IMillGeometry[] result = g.CreateBarGeometries(o_mm: 2, p_mm: 5, u_mm: 20);
        Assert.AreEqual(expectedGeometryCount, result.Length);
        Assert.IsTrue(result.All(h => h is ArcGeometry a && center == a.Center && radius_mm.Near(a.Radius_mm)));
    }

    [TestMethod]
    [DataRow(10.6, 45, 180)] // lg=2*10.6*3.14*(3/8)=24.98 < 2.5+20+2.5 => n = 0 => Exception
    public void TestCreateBarGeometriesOnArcThrows(double radius_mm, double startAngle_deg, double endAngle_deg) {
        Assert.ThrowsException<ArgumentException>(() => {
            Vector2 center = new(10, 10);
            IMillGeometry g = new ArcGeometry(center, radius_mm, startAngle_deg, endAngle_deg, counterclockwise: true);
            g.CreateBarGeometries(o_mm: 2, p_mm: 5, u_mm: 20);
        });
    }

}
