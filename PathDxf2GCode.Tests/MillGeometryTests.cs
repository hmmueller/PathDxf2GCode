namespace de.hmmueller.PathDxf2GCode.Tests;

using netDxf;
using System.Transactions;

[TestClass]
public class MillGeometryTests {
    [TestMethod]
    [DataRow(25, 5)] // lg=25 >= 2.5+i+20+i+2.5 => 5
    [DataRow(50, 9)] // lg=50 >= 2.5+i+20+i+5+i+20+i+2.5 => 9
    [DataRow(24.9, 0)] // lg=24.9 < 2.5+20+2.5 => n = 0 => no bar geometries
    public void TestCreateSupportBarGeometriesOnLine(double lg_mm, int expectedGeometryCount) {
        IMillGeometry g = new LineGeometry(new Vector2(10, 10), new Vector2(10 + lg_mm, 10));
        IMillGeometry[] result = g.CreateSupportBarGeometries(o_mm: 2, p_mm: 5, u_mm: 20, db_mm: 1);
        Assert.AreEqual(expectedGeometryCount, result.Length);
        CollectionAssert.AllItemsAreInstancesOfType(result, typeof(LineGeometry));
    }

    [TestMethod]
    [DataRow(10.7, 45, 180, 5)]    // lg=2*10.7*3.14*(3/8)=25.21   >= 2.5+i+20+i+2.5 => 5
    [DataRow(15, 45, 180, 5)]      // lg=2*15*3.14*(3/8)=35.34     >= 2.5+i+20+i+2.5 => 5
    [DataRow(15, 45, 45 + 190, 5)] // lg=2*15*3.14*(190/360)=49.74 <  2.5+i+20+i+5+i+20+i+2.5 => 5
    [DataRow(15, 45, 45 + 191, 9)] // lg=2*15*3.14*(191/360)=50.00 >= 2.5+i+20+i+5+i+20+i+2.5 => 9
    [DataRow(10.6, 45, 180, 0)] // lg=2*10.6*3.14*(3/8)=24.98 < 2.5+20+2.5 => n = 0 => no bar geometries
    public void TestCreateSupportBarGeometriesOnArc(double radius_mm, double startAngle_deg, double endAngle_deg, int expectedGeometryCount) {
        Vector2 center = new(10, 10);
        IMillGeometry g = new ArcGeometry(center, radius_mm, startAngle_deg, endAngle_deg, counterclockwise: true);
        IMillGeometry[] result = g.CreateSupportBarGeometries(o_mm: 2, p_mm: 5, u_mm: 20, db_mm: 1);
        Assert.AreEqual(expectedGeometryCount, result.Length);
        Assert.IsTrue(result.All(h => h is ArcGeometry a && center == a.Center && radius_mm.Near(a.Radius_mm)));
    }

    private static void AssertIsNear(Vector2 expected, Vector2 actual) {
        Assert.IsTrue(expected.Near(actual), expected + " vs. " + actual);
    }

    [TestMethod]
    [DataRow(0, 2.0 / 8, true, 45, 135)]
    [DataRow(0, 1.0 / 8, true, 45, 90)]
    [DataRow(0, -1.0 / 16, false, 45, 22.5)]
    [DataRow(2.0 / 8, -1.0 / 16, false, 135, 135 - 22.5)]
    [DataRow(2.0 / 8, -1.0 / 4, false, 135, 45)]
    public void TestArcCreateSections(double p, double d, bool c, double s, double e) {
        const double R = 10;
        const double U = 2 * R * Math.PI;

        ArcGeometry g = new ArcGeometry(new Vector2(), R, 45, 135, counterclockwise: true);
        ArcGeometry sect = (ArcGeometry)g.Section(p * U, d * U);

        Assert.AreEqual(c, sect.Counterclockwise);
        AssertIsNear(new Vector2(R, 0).Rotate(s * MathHelper.DegToRad), sect.Start);
        AssertIsNear(new Vector2(R, 0).Rotate(e * MathHelper.DegToRad), sect.End);
    }

    [TestMethod]
    [DataRow(0, 1.0, 0, 1)]
    [DataRow(0, 1.0 / 4, 0, 0.25)]
    [DataRow(1, -1.0 / 4, 1, 0.75)]
    public void TestCreateLineSections(double p, double d, double s, double e) {
        const double D = 10;
        double L = D * Math.Sqrt(2);
        Vector2 P = new Vector2(100, 0);
        LineGeometry g = new LineGeometry(P, P + new Vector2(D, D));
        LineGeometry sect = (LineGeometry)g.Section(p * L, d * L);

        AssertIsNear(P + new Vector2(D, D) * s, sect.Start);
        AssertIsNear(P + new Vector2(D, D) * e, sect.End);
    }

    private class TestParams : IParams {
        public double G_mmpmin { get; init; }
        public double F_mmpmin { get; init; }

        public double? RawB_mm => throw new NotImplementedException();
        public double? RawD_mm => throw new NotImplementedException();
        public double? RawI_mm => throw new NotImplementedException();
        public double? RawP_mm => throw new NotImplementedException();
        public double? RawU_mm => throw new NotImplementedException();
        public double T_mm => throw new NotImplementedException();
        public double O_mm => throw new NotImplementedException();
        public string M => throw new NotImplementedException();
        public double Z_mmpmin => throw new NotImplementedException();
        public double? W_mm => throw new NotImplementedException();
        public double B_mm => throw new NotImplementedException();
        public double D_mm => throw new NotImplementedException();
        public double I_mm => throw new NotImplementedException();
        public double J_deg => throw new NotImplementedException();
        public double P_mm => throw new NotImplementedException();
        public double U_mm => throw new NotImplementedException();
        public double S_mm => throw new NotImplementedException();
        public double A_mm => throw new NotImplementedException();
        public double? Y_mm => throw new NotImplementedException();
    }

    [TestMethod]
    [DataRow(0, 20, 100)]
    [DataRow(20, 0, 10)]
    [DataRow(20, 20, 14.142136)]
    [DataRow(1, 20, 100)] // 100.124922
    public void TestSpeed(double dz, double dxy, double expectedSpeed_mmpmin) {
        double speed_mmpmin = new TestParams() { G_mmpmin = 10, F_mmpmin = 100 }.Speed_mmpmin(dz, dxy);
        Assert.IsTrue(expectedSpeed_mmpmin.Near(speed_mmpmin), $"Expected {expectedSpeed_mmpmin} but got {speed_mmpmin}");
    }
}
