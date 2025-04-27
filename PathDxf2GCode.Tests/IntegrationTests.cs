namespace de.hmmueller.PathDxf2GCode.Tests;

using System.Runtime.CompilerServices;

[TestClass]
public class IntegrationTests {
    // Yes, I know - this type of tests is brittle. Still, they are quick to create and worth their money. 

    private static string Truncate(string s, int lg)
        => lg >= s.Length ? s : s[..lg] + "...";

    private static string Max80From(string s, int i)
        => Truncate(s[i..], 80);

    private static int Count(string haystack, string needle) {
        return haystack.Split([needle], StringSplitOptions.None).Length - 1;
    }

    private static void AssertEqual(string expected, int firstLineNo, string actual) {
        int n = firstLineNo;
        int k = Math.Min(expected.Length, actual.Length);
        for (int i = 0; i < k; i++) {
            if (expected[i] != actual[i]) {
                Assert.Fail($"Expected != actual in Zeile {n}:\r\n...{Max80From(expected, i)}\r\nvs.\r\n...{Max80From(actual, i)}\r\n\r\nActual:\r\n{actual}");
            } else if (actual[i] == '\n') {
                n++;
            }
        }
        if (expected.Length > k) {
            Assert.Fail($"Expected ist länger ab Zeile {n}: {Max80From(expected, k)}");
        } else if (actual.Length > k) {
            Assert.Fail($"Actual ist länger ab Zeile {n}: {Max80From(actual, k)}\r\n\r\nActual:\r\n{actual}");
        }
    }

    private static void Compare(string filename, string? expected, Func<string, bool>? assert = null, [CallerLineNumber] int firstLineNo = 1) {
        using (StreamReader sr = new(filename)) {
            string actual = sr.ReadToEnd().Trim();
            if (expected != null) {
                AssertEqual(expected.Trim(), firstLineNo, actual.Trim());
            }
            if (assert != null) {
                Assert.IsTrue(assert(actual));
            }
        }
    }

    [TestMethod]
    public void TestMethod01() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s5", "8999.01P.dxf"]));
        Compare("8999.01P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.01P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.1P[8999.01P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [0.000|0.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[0.000|0.000] e=[57.687|0.000] h=0.800 bt=False)
G01 F150.000 X57.687 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [57.687|0.000|0.800] [57.687|0.000|-0.300] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.300)
G01 Z-0.300
  (MillLine s=[57.687|0.000] e=[0.000|0.000] h=-0.300 bt=False)
G01 F150.000 X0.000 Y0.000 Z-0.300
  (SweepAndDrillSafelyFromTo [0.000|0.000|-0.300] [57.687|0.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.300 5.000)
G00 Z5.000
G00 X57.687 Y0.000
G00 Z5.000
  (Fräslänge:     115 mm   ca.  2 min)
  (Bohrungen:       4 mm   ca.  1 min)
  (Leerfahrten:    66 mm   ca.  1 min)
  (Summe:         185 mm   ca.  2 min)
  (Befehlszahl: 8)
M30
%");
    }

    [TestMethod]
    public void TestMethod02() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.02P.dxf"]));
        Compare("8999.02P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.02P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|20.000])
G00 Z20.000
G00 X0.000 Y0.000
  (Model 8999.2P[8999.02P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|20.000] [5.948|5.963|20.000] s=20.000 bt=False)
    (DrillOrPullZFromTo 20.000 20.000)
G00 Z20.000
G00 X5.948 Y5.963
  (SweepAndDrillSafelyFromTo [5.948|5.963|20.000] [5.948|5.963|0.400] s=20.000 bt=False)
    (DrillOrPullZFromTo 20.000 0.400)
G00 Z1.000
G01 Z0.400
  (MillLine s=[5.948|5.963] e=[63.635|5.963] h=0.400 bt=False)
G01 F150.000 X63.635 Y5.963 Z0.400
  (SweepAndDrillSafelyFromTo [63.635|5.963|0.400] [63.635|5.963|-0.100] s=20.000 bt=False)
    (DrillOrPullZFromTo 0.400 -0.100)
G01 Z-0.100
  (MillLine s=[63.635|5.963] e=[5.948|5.963] h=-0.100 bt=False)
G01 F150.000 X5.948 Y5.963 Z-0.100
  (SweepAndDrillSafelyFromTo [5.948|5.963|-0.100] [63.635|5.963|20.000] s=20.000 bt=False)
    (DrillOrPullZFromTo -0.100 20.000)
G00 Z20.000
; G00 X63.635 Y5.963
  (SweepAndDrillSafelyFromTo [63.635|5.963|20.000] [76.907|14.144|20.000] s=20.000 bt=False)
G00 X76.907 Y14.144
G00 Z20.000
  (Fräslänge:     115 mm   ca.  2 min)
  (Bohrungen:       2 mm   ca.  1 min)
  (Leerfahrten:    63 mm   ca.  1 min)
  (Summe:         180 mm   ca.  2 min)
  (Befehlszahl: 10)
M30
%");
    }

    [TestMethod]
    public void TestMethod05() {
        Assert.AreNotEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.05P.dxf"]));
    }

    [TestMethod]
    public void TestMethod08() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.08P.dxf"]));
        Compare("8999.08P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.08P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.8P[8999.08P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [11.090|27.077|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 5.000)
G00 Z5.000
G00 X11.090 Y27.077
  (SweepAndDrillSafelyFromTo [11.090|27.077|5.000] [11.090|27.077|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 -0.100)
G00 Z1.000
G01 Z-0.100
  (MillArc l=[50.000|40.000] r=41.000 a0=198.373 a1=229.007 h=-0.100 p0=[11.090|27.077] p1=[23.105|9.054] bt=False)
G03 F150.000 I38.910 J12.923 X23.105 Y9.054 Z-0.100
  (SweepAndDrillSafelyFromTo [23.105|9.054|-0.100] [23.105|9.054|-0.100] s=5.000 bt=False)
  (MillArc l=[34.019|7.679] r=11.000 a0=172.824 a1=314.556 h=-0.100 p0=[23.105|9.054] p1=[41.737|-0.159] bt=False)
G03 F150.000 I10.914 J-1.374 X41.737 Y-0.159 Z-0.100
  (SweepAndDrillSafelyFromTo [41.737|-0.159|-0.100] [41.737|-0.159|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=41.000 a0=258.373 a1=289.007 h=-0.100 p0=[41.737|-0.159] p1=[63.353|1.235] bt=False)
G03 F150.000 I8.263 J40.159 X63.353 Y1.235 Z-0.100
  (SweepAndDrillSafelyFromTo [63.353|1.235|-0.100] [63.353|1.235|-0.100] s=5.000 bt=False)
  (MillArc l=[70.000|10.000] r=11.000 a0=232.824 a1=14.556 h=-0.100 p0=[63.353|1.235] p1=[80.647|12.765] bt=False)
G03 F150.000 I6.647 J8.765 X80.647 Y12.765 Z-0.100
  (SweepAndDrillSafelyFromTo [80.647|12.765|-0.100] [80.647|12.765|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=41.000 a0=318.373 a1=349.007 h=-0.100 p0=[80.647|12.765] p1=[90.248|32.182] bt=False)
G03 F150.000 I-30.647 J27.235 X90.248 Y32.182 Z-0.100
  (SweepAndDrillSafelyFromTo [90.248|32.182|-0.100] [90.248|32.182|-0.100] s=5.000 bt=False)
  (MillArc l=[85.981|42.321] r=11.000 a0=292.824 a1=74.556 h=-0.100 p0=[90.248|32.182] p1=[88.910|52.923] bt=False)
G03 F150.000 I-4.267 J10.139 X88.910 Y52.923 Z-0.100
  (SweepAndDrillSafelyFromTo [88.910|52.923|-0.100] [88.910|52.923|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=41.000 a0=18.373 a1=49.007 h=-0.100 p0=[88.910|52.923] p1=[76.895|70.946] bt=False)
G03 F150.000 I-38.910 J-12.923 X76.895 Y70.946 Z-0.100
  (SweepAndDrillSafelyFromTo [76.895|70.946|-0.100] [76.895|70.946|-0.100] s=5.000 bt=False)
  (MillArc l=[65.981|72.321] r=11.000 a0=352.824 a1=134.556 h=-0.100 p0=[76.895|70.946] p1=[58.263|80.159] bt=False)
G03 F150.000 I-10.914 J1.374 X58.263 Y80.159 Z-0.100
  (SweepAndDrillSafelyFromTo [58.263|80.159|-0.100] [58.263|80.159|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=41.000 a0=78.373 a1=109.007 h=-0.100 p0=[58.263|80.159] p1=[36.647|78.765] bt=False)
G03 F150.000 I-8.263 J-40.159 X36.647 Y78.765 Z-0.100
  (SweepAndDrillSafelyFromTo [36.647|78.765|-0.100] [36.647|78.765|-0.100] s=5.000 bt=False)
  (MillArc l=[30.000|70.000] r=11.000 a0=52.824 a1=194.556 h=-0.100 p0=[36.647|78.765] p1=[19.353|67.235] bt=False)
G03 F150.000 I-6.647 J-8.765 X19.353 Y67.235 Z-0.100
  (SweepAndDrillSafelyFromTo [19.353|67.235|-0.100] [19.353|67.235|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=41.000 a0=138.373 a1=169.007 h=-0.100 p0=[19.353|67.235] p1=[9.752|47.818] bt=False)
G03 F150.000 I30.647 J-27.235 X9.752 Y47.818 Z-0.100
  (SweepAndDrillSafelyFromTo [9.752|47.818|-0.100] [9.752|47.818|-0.100] s=5.000 bt=False)
  (MillArc l=[14.019|37.679] r=11.000 a0=112.824 a1=254.556 h=-0.100 p0=[9.752|47.818] p1=[11.090|27.077] bt=False)
G03 F150.000 I4.267 J-10.139 X11.090 Y27.077 Z-0.100
  (SweepAndDrillSafelyFromTo [11.090|27.077|-0.100] [11.090|27.077|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [11.090|27.077|5.000] [0.000|10.000|5.000] s=5.000 bt=False)
G00 X0.000 Y10.000
G00 Z5.000
  (Fräslänge:     295 mm   ca.  3 min)
  (Bohrungen:       1 mm   ca.  1 min)
  (Leerfahrten:    59 mm   ca.  1 min)
  (Summe:         355 mm   ca.  3 min)
  (Befehlszahl: 19)
M30
%");
    }

    [TestMethod]
    public void TestMethod09() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.09P.dxf"]));
        Compare("8999.09P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.09P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.9P[8999.09P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [20.962|29.147|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 5.000)
G00 Z5.000
G00 X20.962 Y29.147
  (SweepAndDrillSafelyFromTo [20.962|29.147|5.000] [20.962|29.147|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillArc l=[14.019|37.679] r=11.000 a0=309.135 a1=58.246 h=0.800 p0=[20.962|29.147] p1=[19.808|47.033] bt=False)
G02 F150.000 I-6.943 J8.532 X19.808 Y47.033 Z0.800
  (SweepAndDrillSafelyFromTo [19.808|47.033|0.800] [19.808|47.033|0.800] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=166.887 a1=140.493 h=0.800 p0=[19.808|47.033] p1=[26.082|59.721] bt=False)
G02 F150.000 I30.192 J-7.033 X26.082 Y59.721 Z0.800
  (SweepAndDrillSafelyFromTo [26.082|59.721|0.800] [26.082|59.721|0.800] s=5.000 bt=False)
  (MillArc l=[30.000|70.000] r=11.000 a0=249.135 a1=358.246 h=0.800 p0=[26.082|59.721] p1=[40.995|69.663] bt=False)
G02 F150.000 I3.918 J10.279 X40.995 Y69.663 Z0.800
  (SweepAndDrillSafelyFromTo [40.995|69.663|0.800] [40.995|69.663|0.800] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=106.887 a1=80.493 h=0.800 p0=[40.995|69.663] p1=[55.120|70.574] bt=False)
G02 F150.000 I9.005 J-29.663 X55.120 Y70.574 Z0.800
  (SweepAndDrillSafelyFromTo [55.120|70.574|0.800] [55.120|70.574|0.800] s=5.000 bt=False)
  (MillArc l=[65.981|72.321] r=11.000 a0=189.135 a1=298.246 h=0.800 p0=[55.120|70.574] p1=[71.187|62.630] bt=False)
G02 F150.000 I10.861 J1.746 X71.187 Y62.630 Z0.800
  (SweepAndDrillSafelyFromTo [71.187|62.630|0.800] [71.187|62.630|0.800] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=46.887 a1=20.493 h=0.800 p0=[71.187|62.630] p1=[79.038|50.853] bt=False)
G02 F150.000 I-21.187 J-22.630 X79.038 Y50.853 Z0.800
  (SweepAndDrillSafelyFromTo [79.038|50.853|0.800] [79.038|50.853|0.800] s=5.000 bt=False)
  (MillArc l=[85.981|42.321] r=11.000 a0=129.135 a1=238.246 h=0.800 p0=[79.038|50.853] p1=[80.192|32.967] bt=False)
G02 F150.000 I6.943 J-8.532 X80.192 Y32.967 Z0.800
  (SweepAndDrillSafelyFromTo [80.192|32.967|0.800] [80.192|32.967|0.800] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=346.887 a1=320.493 h=0.800 p0=[80.192|32.967] p1=[73.918|20.279] bt=False)
G02 F150.000 I-30.192 J7.033 X73.918 Y20.279 Z0.800
  (SweepAndDrillSafelyFromTo [73.918|20.279|0.800] [73.918|20.279|0.800] s=5.000 bt=False)
  (MillArc l=[70.000|10.000] r=11.000 a0=69.135 a1=178.246 h=0.800 p0=[73.918|20.279] p1=[59.005|10.337] bt=False)
G02 F150.000 I-3.918 J-10.279 X59.005 Y10.337 Z0.800
  (SweepAndDrillSafelyFromTo [59.005|10.337|0.800] [59.005|10.337|0.800] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=286.887 a1=260.493 h=0.800 p0=[59.005|10.337] p1=[44.880|9.426] bt=False)
G02 F150.000 I-9.005 J29.663 X44.880 Y9.426 Z0.800
  (SweepAndDrillSafelyFromTo [44.880|9.426|0.800] [44.880|9.426|0.800] s=5.000 bt=False)
  (MillArc l=[34.019|7.679] r=11.000 a0=9.135 a1=118.246 h=0.800 p0=[44.880|9.426] p1=[28.813|17.370] bt=False)
G02 F150.000 I-10.861 J-1.746 X28.813 Y17.370 Z0.800
  (SweepAndDrillSafelyFromTo [28.813|17.370|0.800] [28.813|17.370|0.800] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=226.887 a1=200.493 h=0.800 p0=[28.813|17.370] p1=[20.962|29.147] bt=False)
G02 F150.000 I21.187 J22.630 X20.962 Y29.147 Z0.800
  (SweepAndDrillSafelyFromTo [20.962|29.147|0.800] [20.962|29.147|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillArc l=[14.019|37.679] r=11.000 a0=309.135 a1=58.246 h=-0.100 p0=[20.962|29.147] p1=[19.808|47.033] bt=False)
G02 F150.000 I-6.943 J8.532 X19.808 Y47.033 Z-0.100
  (SweepAndDrillSafelyFromTo [19.808|47.033|-0.100] [19.808|47.033|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=166.887 a1=140.493 h=-0.100 p0=[19.808|47.033] p1=[26.082|59.721] bt=False)
G02 F150.000 I30.192 J-7.033 X26.082 Y59.721 Z-0.100
  (SweepAndDrillSafelyFromTo [26.082|59.721|-0.100] [26.082|59.721|-0.100] s=5.000 bt=False)
  (MillArc l=[30.000|70.000] r=11.000 a0=249.135 a1=358.246 h=-0.100 p0=[26.082|59.721] p1=[40.995|69.663] bt=False)
G02 F150.000 I3.918 J10.279 X40.995 Y69.663 Z-0.100
  (SweepAndDrillSafelyFromTo [40.995|69.663|-0.100] [40.995|69.663|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=106.887 a1=80.493 h=-0.100 p0=[40.995|69.663] p1=[55.120|70.574] bt=False)
G02 F150.000 I9.005 J-29.663 X55.120 Y70.574 Z-0.100
  (SweepAndDrillSafelyFromTo [55.120|70.574|-0.100] [55.120|70.574|-0.100] s=5.000 bt=False)
  (MillArc l=[65.981|72.321] r=11.000 a0=189.135 a1=298.246 h=-0.100 p0=[55.120|70.574] p1=[71.187|62.630] bt=False)
G02 F150.000 I10.861 J1.746 X71.187 Y62.630 Z-0.100
  (SweepAndDrillSafelyFromTo [71.187|62.630|-0.100] [71.187|62.630|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=46.887 a1=20.493 h=-0.100 p0=[71.187|62.630] p1=[79.038|50.853] bt=False)
G02 F150.000 I-21.187 J-22.630 X79.038 Y50.853 Z-0.100
  (SweepAndDrillSafelyFromTo [79.038|50.853|-0.100] [79.038|50.853|-0.100] s=5.000 bt=False)
  (MillArc l=[85.981|42.321] r=11.000 a0=129.135 a1=238.246 h=-0.100 p0=[79.038|50.853] p1=[80.192|32.967] bt=False)
G02 F150.000 I6.943 J-8.532 X80.192 Y32.967 Z-0.100
  (SweepAndDrillSafelyFromTo [80.192|32.967|-0.100] [80.192|32.967|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=346.887 a1=320.493 h=-0.100 p0=[80.192|32.967] p1=[73.918|20.279] bt=False)
G02 F150.000 I-30.192 J7.033 X73.918 Y20.279 Z-0.100
  (SweepAndDrillSafelyFromTo [73.918|20.279|-0.100] [73.918|20.279|-0.100] s=5.000 bt=False)
  (MillArc l=[70.000|10.000] r=11.000 a0=69.135 a1=178.246 h=-0.100 p0=[73.918|20.279] p1=[59.005|10.337] bt=False)
G02 F150.000 I-3.918 J-10.279 X59.005 Y10.337 Z-0.100
  (SweepAndDrillSafelyFromTo [59.005|10.337|-0.100] [59.005|10.337|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=286.887 a1=260.493 h=-0.100 p0=[59.005|10.337] p1=[44.880|9.426] bt=False)
G02 F150.000 I-9.005 J29.663 X44.880 Y9.426 Z-0.100
  (SweepAndDrillSafelyFromTo [44.880|9.426|-0.100] [44.880|9.426|-0.100] s=5.000 bt=False)
  (MillArc l=[34.019|7.679] r=11.000 a0=9.135 a1=118.246 h=-0.100 p0=[44.880|9.426] p1=[28.813|17.370] bt=False)
G02 F150.000 I-10.861 J-1.746 X28.813 Y17.370 Z-0.100
  (SweepAndDrillSafelyFromTo [28.813|17.370|-0.100] [28.813|17.370|-0.100] s=5.000 bt=False)
  (MillArc l=[50.000|40.000] r=31.000 a0=226.887 a1=200.493 h=-0.100 p0=[28.813|17.370] p1=[20.962|29.147] bt=False)
G02 F150.000 I21.187 J22.630 X20.962 Y29.147 Z-0.100
  (SweepAndDrillSafelyFromTo [20.962|29.147|-0.100] [20.962|29.147|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [20.962|29.147|5.000] [0.000|10.000|5.000] s=5.000 bt=False)
G00 X0.000 Y10.000
G00 Z5.000
  (Fräslänge:     749 mm   ca.  7 min)
  (Bohrungen:       3 mm   ca.  1 min)
  (Leerfahrten:    72 mm   ca.  1 min)
  (Summe:         825 mm   ca.  8 min)
  (Befehlszahl: 32)
M30
%");
    }

    [TestMethod]
    public void TestMethod10() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.10P.dxf"]));
        Compare("8999.10P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.10P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.10P[8999.10P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [0.000|0.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillArc l=[-14.142|-14.142] r=20.000 a0=45.000 a1=90.000 h=0.800 p0=[0.000|0.000] p1=[-14.142|5.858] bt=False)
G03 F150.000 I-14.142 J-14.142 X-14.142 Y5.858 Z0.800
  (SweepAndDrillSafelyFromTo [-14.142|5.858|0.800] [-14.142|5.858|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillArc l=[-14.142|-14.142] r=20.000 a0=90.000 a1=45.000 h=-0.100 p0=[-14.142|5.858] p1=[0.000|0.000] bt=False)
G02 F150.000 I-0.000 J-20.000 X0.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [0.000|0.000|-0.100] [-14.142|5.858|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X-14.142 Y5.858
G00 Z5.000
  (Fräslänge:      31 mm   ca.  1 min)
  (Bohrungen:       3 mm   ca.  1 min)
  (Leerfahrten:    23 mm   ca.  1 min)
  (Summe:          58 mm   ca.  1 min)
  (Befehlszahl: 8)
M30
%");
    }

    [TestMethod]
    public void TestMethod11() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.11P.dxf"]));
        Compare("8999.11P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.11P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.11P[8999.11P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [20.000|0.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 5.000)
G00 Z5.000
G00 X20.000 Y0.000
  (START Subpath 8998.2P[8999.11P.dxf] t=[ [120.000|170.000]=>[20.000|0.000] / [120.000|100.000]=>[20.000|70.000] ])
  (SweepAndDrillSafelyFromTo [20.000|0.000|5.000] [20.000|0.000|-0.200] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 -0.200)
G00 Z1.000
G01 Z-0.200
  (MillLine s=[20.000|0.000] e=[0.000|20.000] h=-0.200 bt=False)
G01 F150.000 X0.000 Y20.000 Z-0.200
  (SweepAndDrillSafelyFromTo [0.000|20.000|-0.200] [0.000|20.000|-0.200] s=5.000 bt=False)
  (MillLine s=[0.000|20.000] e=[0.000|50.000] h=-0.200 bt=False)
G01 F150.000 X0.000 Y50.000 Z-0.200
  (SweepAndDrillSafelyFromTo [0.000|50.000|-0.200] [0.000|50.000|-0.200] s=5.000 bt=False)
  (MillLine s=[0.000|50.000] e=[20.000|70.000] h=-0.200 bt=False)
G01 F150.000 X20.000 Y70.000 Z-0.200
  (SweepAndDrillSafelyFromTo [20.000|70.000|-0.200] [20.000|70.000|-0.300] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.200 -0.300)
G01 Z-0.300
  (MillLine s=[20.000|70.000] e=[0.000|50.000] h=-0.300 bt=False)
G01 F150.000 X0.000 Y50.000 Z-0.300
  (SweepAndDrillSafelyFromTo [0.000|50.000|-0.300] [0.000|50.000|-0.300] s=5.000 bt=False)
  (MillLine s=[0.000|50.000] e=[0.000|20.000] h=-0.300 bt=False)
G01 F150.000 X0.000 Y20.000 Z-0.300
  (SweepAndDrillSafelyFromTo [0.000|20.000|-0.300] [0.000|20.000|-0.300] s=5.000 bt=False)
  (MillLine s=[0.000|20.000] e=[20.000|0.000] h=-0.300 bt=False)
G01 F150.000 X20.000 Y0.000 Z-0.300
  (SweepAndDrillSafelyFromTo [20.000|0.000|-0.300] [20.000|70.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.300 5.000)
G00 Z5.000
; G00 X20.000 Y70.000
  (END Subpath 8998.2P[8999.11P.dxf] t=[ [120.000|170.000]=>[20.000|0.000] / [120.000|100.000]=>[20.000|70.000] ])
  (SweepAndDrillSafelyFromTo [20.000|70.000|5.000] [0.000|70.000|5.000] s=5.000 bt=False)
G00 X0.000 Y70.000
G00 Z5.000
  (Fräslänge:     173 mm   ca.  2 min)
  (Bohrungen:       2 mm   ca.  1 min)
  (Leerfahrten:    49 mm   ca.  1 min)
  (Summe:         225 mm   ca.  2 min)
  (Befehlszahl: 14)
M30
%");
    }

    [TestMethod]
    public void TestMethod12() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.12P.dxf"]));
        Compare("8999.12P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.12P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|2.000])
G00 Z2.000
G00 X0.000 Y0.000
  (Model 8999.12P[8999.12P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|2.000] [20.000|0.000|2.000] s=2.000 bt=False)
    (DrillOrPullZFromTo 2.000 2.000)
G00 Z2.000
G00 X20.000 Y0.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|2.000] [20.000|-0.000|-0.100] s=2.000 bt=False)
    (DrillOrPullZFromTo 2.000 -0.100)
G00 Z0.100
G01 Z-0.100
  (MillArc l=[40.000|-15.000] r=25.000 a0=143.130 a1=36.870 h=-0.100 p0=[20.000|-0.000] p1=[60.000|-0.000] bt=False)
G02 F150.000 I20.000 J-15.000 X60.000 Y-0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [60.000|-0.000|-0.100] [60.000|0.000|-0.100] s=2.000 bt=False)
  (MillArc l=[40.000|0.000] r=20.000 a0=0.000 a1=180.000 h=-0.100 p0=[60.000|0.000] p1=[20.000|0.000] bt=False)
G02 F150.000 I-20.000 J0.000 X20.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [20.000|0.000|-0.100] [20.000|0.000|2.000] s=2.000 bt=False)
    (DrillOrPullZFromTo -0.100 2.000)
G00 Z2.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|2.000] [60.000|0.000|2.000] s=2.000 bt=False)
; G00 X60.000 Y0.000
  (SweepAndDrillSafelyFromTo [60.000|0.000|2.000] [80.000|0.000|2.000] s=2.000 bt=False)
G00 X80.000 Y0.000
G00 Z2.000
  (Fräslänge:     109 mm   ca.  2 min)
  (Bohrungen:       0 mm   ca.  1 min)
  (Leerfahrten:    44 mm   ca.  1 min)
  (Summe:         153 mm   ca.  2 min)
  (Befehlszahl: 9)
M30
%");
    }

    [TestMethod]
    public void TestMethod13() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.13P.dxf"]));
        Compare("8999.13P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.13P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|25.000])
G00 Z25.000
G00 X0.000 Y0.000
  (Model 8999.13P[8999.13P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|25.000] [10.681|-13.420|25.000] s=25.000 bt=False)
    (DrillOrPullZFromTo 25.000 25.000)
G00 Z25.000
G00 X10.681 Y-13.420
  (SweepAndDrillSafelyFromTo [10.681|-13.420|25.000] [10.681|-13.420|18.800] s=25.000 bt=False)
    (DrillOrPullZFromTo 25.000 18.800)
G00 Z20.000
G01 Z18.800
  (MillArc l=[10.681|-23.420] r=10.000 a0=90.000 a1=180.000 h=18.800 p0=[10.681|-13.420] p1=[0.681|-23.420] bt=False)
G03 F150.000 I0.000 J-10.000 X0.681 Y-23.420 Z18.800
  (SweepAndDrillSafelyFromTo [0.681|-23.420|18.800] [0.681|-23.420|18.800] s=25.000 bt=False)
  (MillLine s=[0.681|-23.420] e=[0.681|-47.756] h=18.800 bt=False)
G01 F150.000 X0.681 Y-47.756 Z18.800
  (SweepAndDrillSafelyFromTo [0.681|-47.756|18.800] [0.681|-47.756|18.800] s=25.000 bt=False)
  (MillArc l=[10.681|-47.756] r=10.000 a0=180.000 a1=270.000 h=18.800 p0=[0.681|-47.756] p1=[10.681|-57.756] bt=False)
G03 F150.000 I10.000 J0.000 X10.681 Y-57.756 Z18.800
  (SweepAndDrillSafelyFromTo [10.681|-57.756|18.800] [10.681|-57.756|18.800] s=25.000 bt=False)
  (MillLine s=[10.681|-57.756] e=[110.512|-57.756] h=18.800 bt=False)
G01 F150.000 X110.512 Y-57.756 Z18.800
  (SweepAndDrillSafelyFromTo [110.512|-57.756|18.800] [110.512|-57.756|18.800] s=25.000 bt=False)
  (MillArc l=[110.512|-47.756] r=10.000 a0=270.000 a1=0.000 h=18.800 p0=[110.512|-57.756] p1=[120.512|-47.756] bt=False)
G03 F150.000 I0.000 J10.000 X120.512 Y-47.756 Z18.800
  (SweepAndDrillSafelyFromTo [120.512|-47.756|18.800] [120.512|-47.756|18.800] s=25.000 bt=False)
  (MillLine s=[120.512|-47.756] e=[120.512|-23.420] h=18.800 bt=False)
G01 F150.000 X120.512 Y-23.420 Z18.800
  (SweepAndDrillSafelyFromTo [120.512|-23.420|18.800] [120.512|-23.420|18.800] s=25.000 bt=False)
  (MillArc l=[110.512|-23.420] r=10.000 a0=0.000 a1=90.000 h=18.800 p0=[120.512|-23.420] p1=[110.512|-13.420] bt=False)
G03 F150.000 I-10.000 J0.000 X110.512 Y-13.420 Z18.800
  (SweepAndDrillSafelyFromTo [110.512|-13.420|18.800] [110.512|-13.420|18.800] s=25.000 bt=False)
  (MillLine s=[110.512|-13.420] e=[10.681|-13.420] h=18.800 bt=False)
G01 F150.000 X10.681 Y-13.420 Z18.800
  (SweepAndDrillSafelyFromTo [10.681|-13.420|18.800] [10.681|-13.420|18.500] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.800 18.500)
G01 Z18.500
  (MillArc l=[10.681|-23.420] r=10.000 a0=90.000 a1=180.000 h=18.500 p0=[10.681|-13.420] p1=[0.681|-23.420] bt=False)
G03 F150.000 I0.000 J-10.000 X0.681 Y-23.420 Z18.500
  (SweepAndDrillSafelyFromTo [0.681|-23.420|18.500] [0.681|-23.420|18.500] s=25.000 bt=False)
  (MillLine s=[0.681|-23.420] e=[0.681|-47.756] h=18.500 bt=False)
G01 F150.000 X0.681 Y-47.756 Z18.500
  (SweepAndDrillSafelyFromTo [0.681|-47.756|18.500] [0.681|-47.756|18.500] s=25.000 bt=False)
  (MillArc l=[10.681|-47.756] r=10.000 a0=180.000 a1=270.000 h=18.500 p0=[0.681|-47.756] p1=[10.681|-57.756] bt=False)
G03 F150.000 I10.000 J0.000 X10.681 Y-57.756 Z18.500
  (SweepAndDrillSafelyFromTo [10.681|-57.756|18.500] [10.681|-57.756|18.500] s=25.000 bt=False)
  (MillLine s=[10.681|-57.756] e=[110.512|-57.756] h=18.500 bt=False)
G01 F150.000 X110.512 Y-57.756 Z18.500
  (SweepAndDrillSafelyFromTo [110.512|-57.756|18.500] [110.512|-57.756|18.500] s=25.000 bt=False)
  (MillArc l=[110.512|-47.756] r=10.000 a0=270.000 a1=0.000 h=18.500 p0=[110.512|-57.756] p1=[120.512|-47.756] bt=False)
G03 F150.000 I0.000 J10.000 X120.512 Y-47.756 Z18.500
  (SweepAndDrillSafelyFromTo [120.512|-47.756|18.500] [120.512|-47.756|18.500] s=25.000 bt=False)
  (MillLine s=[120.512|-47.756] e=[120.512|-23.420] h=18.500 bt=False)
G01 F150.000 X120.512 Y-23.420 Z18.500
  (SweepAndDrillSafelyFromTo [120.512|-23.420|18.500] [120.512|-23.420|18.500] s=25.000 bt=False)
  (MillArc l=[110.512|-23.420] r=10.000 a0=0.000 a1=90.000 h=18.500 p0=[120.512|-23.420] p1=[110.512|-13.420] bt=False)
G03 F150.000 I-10.000 J0.000 X110.512 Y-13.420 Z18.500
  (SweepAndDrillSafelyFromTo [110.512|-13.420|18.500] [110.512|-13.420|18.500] s=25.000 bt=False)
  (MillLine s=[110.512|-13.420] e=[10.681|-13.420] h=18.500 bt=False)
G01 F150.000 X10.681 Y-13.420 Z18.500
  (SweepAndDrillSafelyFromTo [10.681|-13.420|18.500] [10.681|-13.420|25.000] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.500 25.000)
G00 Z25.000
  (SweepAndDrillSafelyFromTo [10.681|-13.420|25.000] [11.383|-20.908|25.000] s=25.000 bt=False)
G00 X11.383 Y-20.908
  (SweepAndDrillSafelyFromTo [11.383|-20.908|25.000] [11.383|-20.908|18.800] s=25.000 bt=False)
    (DrillOrPullZFromTo 25.000 18.800)
G00 Z20.000
G01 Z18.800
  (MillLine s=[11.383|-20.908] e=[4.251|-50.404] h=18.800 bt=False)
G01 F150.000 X4.251 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [4.251|-50.404|18.800] [4.251|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[4.251|-50.404] e=[8.804|-50.404] h=18.800 bt=False)
G01 F150.000 X8.804 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [8.804|-50.404|18.800] [8.804|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[8.804|-50.404] e=[12.173|-36.501] h=18.800 bt=False)
G01 F150.000 X12.173 Y-36.501 Z18.800
  (SweepAndDrillSafelyFromTo [12.173|-36.501|18.800] [12.173|-36.501|18.800] s=25.000 bt=False)
  (MillLine s=[12.173|-36.501] e=[29.805|-36.501] h=18.800 bt=False)
G01 F150.000 X29.805 Y-36.501 Z18.800
  (SweepAndDrillSafelyFromTo [29.805|-36.501|18.800] [29.805|-36.501|18.800] s=25.000 bt=False)
  (MillLine s=[29.805|-36.501] e=[26.459|-50.404] h=18.800 bt=False)
G01 F150.000 X26.459 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [26.459|-50.404|18.800] [26.459|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[26.459|-50.404] e=[31.013|-50.404] h=18.800 bt=False)
G01 F150.000 X31.013 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [31.013|-50.404|18.800] [31.013|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[31.013|-50.404] e=[38.121|-20.908] h=18.800 bt=False)
G01 F150.000 X38.121 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [38.121|-20.908|18.800] [38.121|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[38.121|-20.908] e=[33.568|-20.908] h=18.800 bt=False)
G01 F150.000 X33.568 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [33.568|-20.908|18.800] [33.568|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[33.568|-20.908] e=[30.618|-33.161] h=18.800 bt=False)
G01 F150.000 X30.618 Y-33.161 Z18.800
  (SweepAndDrillSafelyFromTo [30.618|-33.161|18.800] [30.618|-33.161|18.800] s=25.000 bt=False)
  (MillLine s=[30.618|-33.161] e=[12.962|-33.161] h=18.800 bt=False)
G01 F150.000 X12.962 Y-33.161 Z18.800
  (SweepAndDrillSafelyFromTo [12.962|-33.161|18.800] [12.962|-33.161|18.800] s=25.000 bt=False)
  (MillLine s=[12.962|-33.161] e=[15.913|-20.908] h=18.800 bt=False)
G01 F150.000 X15.913 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [15.913|-20.908|18.800] [15.913|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[15.913|-20.908] e=[11.383|-20.908] h=18.800 bt=False)
G01 F150.000 X11.383 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [11.383|-20.908|18.800] [11.383|-20.908|18.500] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.800 18.500)
G01 Z18.500
  (MillLine s=[11.383|-20.908] e=[4.251|-50.404] h=18.500 bt=False)
G01 F150.000 X4.251 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [4.251|-50.404|18.500] [4.251|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[4.251|-50.404] e=[8.804|-50.404] h=18.500 bt=False)
G01 F150.000 X8.804 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [8.804|-50.404|18.500] [8.804|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[8.804|-50.404] e=[12.173|-36.501] h=18.500 bt=False)
G01 F150.000 X12.173 Y-36.501 Z18.500
  (SweepAndDrillSafelyFromTo [12.173|-36.501|18.500] [12.173|-36.501|18.500] s=25.000 bt=False)
  (MillLine s=[12.173|-36.501] e=[29.805|-36.501] h=18.500 bt=False)
G01 F150.000 X29.805 Y-36.501 Z18.500
  (SweepAndDrillSafelyFromTo [29.805|-36.501|18.500] [29.805|-36.501|18.500] s=25.000 bt=False)
  (MillLine s=[29.805|-36.501] e=[26.459|-50.404] h=18.500 bt=False)
G01 F150.000 X26.459 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [26.459|-50.404|18.500] [26.459|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[26.459|-50.404] e=[31.013|-50.404] h=18.500 bt=False)
G01 F150.000 X31.013 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [31.013|-50.404|18.500] [31.013|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[31.013|-50.404] e=[38.121|-20.908] h=18.500 bt=False)
G01 F150.000 X38.121 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [38.121|-20.908|18.500] [38.121|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[38.121|-20.908] e=[33.568|-20.908] h=18.500 bt=False)
G01 F150.000 X33.568 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [33.568|-20.908|18.500] [33.568|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[33.568|-20.908] e=[30.618|-33.161] h=18.500 bt=False)
G01 F150.000 X30.618 Y-33.161 Z18.500
  (SweepAndDrillSafelyFromTo [30.618|-33.161|18.500] [30.618|-33.161|18.500] s=25.000 bt=False)
  (MillLine s=[30.618|-33.161] e=[12.962|-33.161] h=18.500 bt=False)
G01 F150.000 X12.962 Y-33.161 Z18.500
  (SweepAndDrillSafelyFromTo [12.962|-33.161|18.500] [12.962|-33.161|18.500] s=25.000 bt=False)
  (MillLine s=[12.962|-33.161] e=[15.913|-20.908] h=18.500 bt=False)
G01 F150.000 X15.913 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [15.913|-20.908|18.500] [15.913|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[15.913|-20.908] e=[11.383|-20.908] h=18.500 bt=False)
G01 F150.000 X11.383 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [11.383|-20.908|18.500] [11.383|-20.908|25.000] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.500 25.000)
G00 Z25.000
  (SweepAndDrillSafelyFromTo [11.383|-20.908|25.000] [45.804|-20.908|25.000] s=25.000 bt=False)
G00 X45.804 Y-20.908
  (SweepAndDrillSafelyFromTo [45.804|-20.908|25.000] [45.804|-20.908|18.800] s=25.000 bt=False)
    (DrillOrPullZFromTo 25.000 18.800)
G00 Z20.000
G01 Z18.800
  (MillLine s=[45.804|-20.908] e=[38.696|-50.404] h=18.800 bt=False)
G01 F150.000 X38.696 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [38.696|-50.404|18.800] [38.696|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[38.696|-50.404] e=[43.133|-50.404] h=18.800 bt=False)
G01 F150.000 X43.133 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [43.133|-50.404|18.800] [43.133|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[43.133|-50.404] e=[48.709|-27.248] h=18.800 bt=False)
G01 F150.000 X48.709 Y-27.248 Z18.800
  (SweepAndDrillSafelyFromTo [48.709|-27.248|18.800] [48.709|-27.248|18.800] s=25.000 bt=False)
  (MillLine s=[48.709|-27.248] e=[52.890|-50.404] h=18.800 bt=False)
G01 F150.000 X52.890 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [52.890|-50.404|18.800] [52.890|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[52.890|-50.404] e=[57.280|-50.404] h=18.800 bt=False)
G01 F150.000 X57.280 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [57.280|-50.404|18.800] [57.280|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[57.280|-50.404] e=[72.081|-27.888] h=18.800 bt=False)
G01 F150.000 X72.081 Y-27.888 Z18.800
  (SweepAndDrillSafelyFromTo [72.081|-27.888|18.800] [72.081|-27.888|18.800] s=25.000 bt=False)
  (MillLine s=[72.081|-27.888] e=[66.503|-50.404] h=18.800 bt=False)
G01 F150.000 X66.503 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [66.503|-50.404|18.800] [66.503|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[66.503|-50.404] e=[70.986|-50.404] h=18.800 bt=False)
G01 F150.000 X70.986 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [70.986|-50.404|18.800] [70.986|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[70.986|-50.404] e=[78.095|-20.908] h=18.800 bt=False)
G01 F150.000 X78.095 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [78.095|-20.908|18.800] [78.095|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[78.095|-20.908] e=[72.403|-20.908] h=18.800 bt=False)
G01 F150.000 X72.403 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [72.403|-20.908|18.800] [72.403|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[72.403|-20.908] e=[55.935|-46.104] h=18.800 bt=False)
G01 F150.000 X55.935 Y-46.104 Z18.800
  (SweepAndDrillSafelyFromTo [55.935|-46.104|18.800] [55.935|-46.104|18.800] s=25.000 bt=False)
  (MillLine s=[55.935|-46.104] e=[51.403|-20.908] h=18.800 bt=False)
G01 F150.000 X51.403 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [51.403|-20.908|18.800] [51.403|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[51.403|-20.908] e=[45.804|-20.908] h=18.800 bt=False)
G01 F150.000 X45.804 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [45.804|-20.908|18.800] [45.804|-20.908|18.500] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.800 18.500)
G01 Z18.500
  (MillLine s=[45.804|-20.908] e=[38.696|-50.404] h=18.500 bt=False)
G01 F150.000 X38.696 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [38.696|-50.404|18.500] [38.696|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[38.696|-50.404] e=[43.133|-50.404] h=18.500 bt=False)
G01 F150.000 X43.133 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [43.133|-50.404|18.500] [43.133|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[43.133|-50.404] e=[48.709|-27.248] h=18.500 bt=False)
G01 F150.000 X48.709 Y-27.248 Z18.500
  (SweepAndDrillSafelyFromTo [48.709|-27.248|18.500] [48.709|-27.248|18.500] s=25.000 bt=False)
  (MillLine s=[48.709|-27.248] e=[52.890|-50.404] h=18.500 bt=False)
G01 F150.000 X52.890 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [52.890|-50.404|18.500] [52.890|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[52.890|-50.404] e=[57.280|-50.404] h=18.500 bt=False)
G01 F150.000 X57.280 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [57.280|-50.404|18.500] [57.280|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[57.280|-50.404] e=[72.081|-27.888] h=18.500 bt=False)
G01 F150.000 X72.081 Y-27.888 Z18.500
  (SweepAndDrillSafelyFromTo [72.081|-27.888|18.500] [72.081|-27.888|18.500] s=25.000 bt=False)
  (MillLine s=[72.081|-27.888] e=[66.503|-50.404] h=18.500 bt=False)
G01 F150.000 X66.503 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [66.503|-50.404|18.500] [66.503|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[66.503|-50.404] e=[70.986|-50.404] h=18.500 bt=False)
G01 F150.000 X70.986 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [70.986|-50.404|18.500] [70.986|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[70.986|-50.404] e=[78.095|-20.908] h=18.500 bt=False)
G01 F150.000 X78.095 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [78.095|-20.908|18.500] [78.095|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[78.095|-20.908] e=[72.403|-20.908] h=18.500 bt=False)
G01 F150.000 X72.403 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [72.403|-20.908|18.500] [72.403|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[72.403|-20.908] e=[55.935|-46.104] h=18.500 bt=False)
G01 F150.000 X55.935 Y-46.104 Z18.500
  (SweepAndDrillSafelyFromTo [55.935|-46.104|18.500] [55.935|-46.104|18.500] s=25.000 bt=False)
  (MillLine s=[55.935|-46.104] e=[51.403|-20.908] h=18.500 bt=False)
G01 F150.000 X51.403 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [51.403|-20.908|18.500] [51.403|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[51.403|-20.908] e=[45.804|-20.908] h=18.500 bt=False)
G01 F150.000 X45.804 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [45.804|-20.908|18.500] [45.804|-20.908|25.000] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.500 25.000)
G00 Z25.000
  (SweepAndDrillSafelyFromTo [45.804|-20.908|25.000] [85.426|-20.908|25.000] s=25.000 bt=False)
G00 X85.426 Y-20.908
  (SweepAndDrillSafelyFromTo [85.426|-20.908|25.000] [85.426|-20.908|18.800] s=25.000 bt=False)
    (DrillOrPullZFromTo 25.000 18.800)
G00 Z20.000
G01 Z18.800
  (MillLine s=[85.426|-20.908] e=[78.317|-50.404] h=18.800 bt=False)
G01 F150.000 X78.317 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [78.317|-50.404|18.800] [78.317|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[78.317|-50.404] e=[82.754|-50.404] h=18.800 bt=False)
G01 F150.000 X82.754 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [82.754|-50.404|18.800] [82.754|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[82.754|-50.404] e=[88.330|-27.248] h=18.800 bt=False)
G01 F150.000 X88.330 Y-27.248 Z18.800
  (SweepAndDrillSafelyFromTo [88.330|-27.248|18.800] [88.330|-27.248|18.800] s=25.000 bt=False)
  (MillLine s=[88.330|-27.248] e=[92.511|-50.404] h=18.800 bt=False)
G01 F150.000 X92.511 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [92.511|-50.404|18.800] [92.511|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[92.511|-50.404] e=[96.902|-50.404] h=18.800 bt=False)
G01 F150.000 X96.902 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [96.902|-50.404|18.800] [96.902|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[96.902|-50.404] e=[111.703|-27.888] h=18.800 bt=False)
G01 F150.000 X111.703 Y-27.888 Z18.800
  (SweepAndDrillSafelyFromTo [111.703|-27.888|18.800] [111.703|-27.888|18.800] s=25.000 bt=False)
  (MillLine s=[111.703|-27.888] e=[106.124|-50.404] h=18.800 bt=False)
G01 F150.000 X106.124 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [106.124|-50.404|18.800] [106.124|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[106.124|-50.404] e=[110.608|-50.404] h=18.800 bt=False)
G01 F150.000 X110.608 Y-50.404 Z18.800
  (SweepAndDrillSafelyFromTo [110.608|-50.404|18.800] [110.608|-50.404|18.800] s=25.000 bt=False)
  (MillLine s=[110.608|-50.404] e=[117.716|-20.908] h=18.800 bt=False)
G01 F150.000 X117.716 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [117.716|-20.908|18.800] [117.716|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[117.716|-20.908] e=[112.025|-20.908] h=18.800 bt=False)
G01 F150.000 X112.025 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [112.025|-20.908|18.800] [112.025|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[112.025|-20.908] e=[95.556|-46.104] h=18.800 bt=False)
G01 F150.000 X95.556 Y-46.104 Z18.800
  (SweepAndDrillSafelyFromTo [95.556|-46.104|18.800] [95.556|-46.104|18.800] s=25.000 bt=False)
  (MillLine s=[95.556|-46.104] e=[91.024|-20.908] h=18.800 bt=False)
G01 F150.000 X91.024 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [91.024|-20.908|18.800] [91.024|-20.908|18.800] s=25.000 bt=False)
  (MillLine s=[91.024|-20.908] e=[85.426|-20.908] h=18.800 bt=False)
G01 F150.000 X85.426 Y-20.908 Z18.800
  (SweepAndDrillSafelyFromTo [85.426|-20.908|18.800] [85.426|-20.908|18.500] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.800 18.500)
G01 Z18.500
  (MillLine s=[85.426|-20.908] e=[78.317|-50.404] h=18.500 bt=False)
G01 F150.000 X78.317 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [78.317|-50.404|18.500] [78.317|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[78.317|-50.404] e=[82.754|-50.404] h=18.500 bt=False)
G01 F150.000 X82.754 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [82.754|-50.404|18.500] [82.754|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[82.754|-50.404] e=[88.330|-27.248] h=18.500 bt=False)
G01 F150.000 X88.330 Y-27.248 Z18.500
  (SweepAndDrillSafelyFromTo [88.330|-27.248|18.500] [88.330|-27.248|18.500] s=25.000 bt=False)
  (MillLine s=[88.330|-27.248] e=[92.511|-50.404] h=18.500 bt=False)
G01 F150.000 X92.511 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [92.511|-50.404|18.500] [92.511|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[92.511|-50.404] e=[96.902|-50.404] h=18.500 bt=False)
G01 F150.000 X96.902 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [96.902|-50.404|18.500] [96.902|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[96.902|-50.404] e=[111.703|-27.888] h=18.500 bt=False)
G01 F150.000 X111.703 Y-27.888 Z18.500
  (SweepAndDrillSafelyFromTo [111.703|-27.888|18.500] [111.703|-27.888|18.500] s=25.000 bt=False)
  (MillLine s=[111.703|-27.888] e=[106.124|-50.404] h=18.500 bt=False)
G01 F150.000 X106.124 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [106.124|-50.404|18.500] [106.124|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[106.124|-50.404] e=[110.608|-50.404] h=18.500 bt=False)
G01 F150.000 X110.608 Y-50.404 Z18.500
  (SweepAndDrillSafelyFromTo [110.608|-50.404|18.500] [110.608|-50.404|18.500] s=25.000 bt=False)
  (MillLine s=[110.608|-50.404] e=[117.716|-20.908] h=18.500 bt=False)
G01 F150.000 X117.716 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [117.716|-20.908|18.500] [117.716|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[117.716|-20.908] e=[112.025|-20.908] h=18.500 bt=False)
G01 F150.000 X112.025 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [112.025|-20.908|18.500] [112.025|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[112.025|-20.908] e=[95.556|-46.104] h=18.500 bt=False)
G01 F150.000 X95.556 Y-46.104 Z18.500
  (SweepAndDrillSafelyFromTo [95.556|-46.104|18.500] [95.556|-46.104|18.500] s=25.000 bt=False)
  (MillLine s=[95.556|-46.104] e=[91.024|-20.908] h=18.500 bt=False)
G01 F150.000 X91.024 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [91.024|-20.908|18.500] [91.024|-20.908|18.500] s=25.000 bt=False)
  (MillLine s=[91.024|-20.908] e=[85.426|-20.908] h=18.500 bt=False)
G01 F150.000 X85.426 Y-20.908 Z18.500
  (SweepAndDrillSafelyFromTo [85.426|-20.908|18.500] [85.426|-20.908|25.000] s=25.000 bt=False)
    (DrillOrPullZFromTo 18.500 25.000)
G00 Z25.000
  (SweepAndDrillSafelyFromTo [85.426|-20.908|25.000] [57.687|0.000|25.000] s=25.000 bt=False)
G00 X57.687 Y0.000
G00 Z25.000
  (Fräslänge:    1912 mm   ca. 18 min)
  (Bohrungen:      11 mm   ca.  1 min)
  (Leerfahrten:   179 mm   ca.  1 min)
  (Summe:        2102 mm   ca. 19 min)
  (Befehlszahl: 115)
M30
%");
    }

    [TestMethod]
    public void TestMethod14() { // Löcher
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.14P.dxf"]));
        Compare("8999.14P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.14P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.14P[8999.14P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [1.950|-3.302|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 5.000)
G00 Z5.000
G00 X1.950 Y-3.302
  (SweepAndDrillSafelyFromTo [1.950|-3.302|5.000] [1.950|-3.302|1.600] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (MillLine s=[1.950|-3.302] e=[1.950|-10.524] h=1.600 bt=False)
G01 F150.000 X1.950 Y-10.524 Z1.600
  (SweepAndDrillSafelyFromTo [1.950|-10.524|1.600] [1.950|-10.524|1.600] s=5.000 bt=False)
  (MillLine s=[1.950|-10.524] e=[6.159|-10.524] h=1.600 bt=False)
G01 F150.000 X6.159 Y-10.524 Z1.600
  (SweepAndDrillSafelyFromTo [6.159|-10.524|1.600] [6.159|-10.524|1.600] s=5.000 bt=False)
  (MillLine s=[6.159|-10.524] e=[6.159|-3.302] h=1.600 bt=False)
G01 F150.000 X6.159 Y-3.302 Z1.600
  (SweepAndDrillSafelyFromTo [6.159|-3.302|1.600] [6.159|-3.302|1.600] s=5.000 bt=False)
  (MillLine s=[6.159|-3.302] e=[1.950|-3.302] h=1.600 bt=False)
G01 F150.000 X1.950 Y-3.302 Z1.600
  (SweepAndDrillSafelyFromTo [1.950|-3.302|1.600] [1.950|-3.302|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [1.950|-3.302|5.000] [0.000|-13.877|5.000] s=5.000 bt=False)
G00 X0.000 Y-13.877
  (Drill l=[0.000|-13.877])
    (DrillOrPullZFromTo 5.000 -0.100)
G00 Z2.000
G01 Z-0.100
  (SweepAndDrillSafelyFromTo [0.000|-13.877|-0.100] [10.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X10.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[10.000|-13.877] r=1.050)
G00 X10.000 Y-13.927
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.050 X10.000 Y-13.827 Z1.400
G02 F150.000 I0 J-0.050 X10.000 Y-13.927 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.050 X10.000 Y-13.827 Z0.200
G02 F150.000 I0 J-0.050 X10.000 Y-13.927 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.050 X10.000 Y-13.827 Z-0.100
G02 F150.000 I0 J-0.050 X10.000 Y-13.927 Z-0.100
G00 X10.000 Y-13.877
  (SweepAndDrillSafelyFromTo [10.000|-13.877|-0.100] [20.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X20.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[20.000|-13.877] r=1.100)
G00 X20.000 Y-13.977
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.100 X20.000 Y-13.777 Z1.400
G02 F150.000 I0 J-0.100 X20.000 Y-13.977 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.100 X20.000 Y-13.777 Z0.200
G02 F150.000 I0 J-0.100 X20.000 Y-13.977 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.100 X20.000 Y-13.777 Z-0.100
G02 F150.000 I0 J-0.100 X20.000 Y-13.977 Z-0.100
G00 X20.000 Y-13.877
  (SweepAndDrillSafelyFromTo [20.000|-13.877|-0.100] [28.452|-3.302|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X28.452 Y-3.302
  (SweepAndDrillSafelyFromTo [28.452|-3.302|5.000] [28.452|-3.302|1.600] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (MillLine s=[28.452|-3.302] e=[31.497|-3.302] h=1.600 bt=False)
G01 F150.000 X31.497 Y-3.302 Z1.600
  (SweepAndDrillSafelyFromTo [31.497|-3.302|1.600] [31.497|-3.302|1.600] s=5.000 bt=False)
  (MillLine s=[31.497|-3.302] e=[28.612|-6.666] h=1.600 bt=False)
G01 F150.000 X28.612 Y-6.666 Z1.600
  (SweepAndDrillSafelyFromTo [28.612|-6.666|1.600] [28.612|-6.666|1.600] s=5.000 bt=False)
  (MillLine s=[28.612|-6.666] e=[31.817|-8.749] h=1.600 bt=False)
G01 F150.000 X31.817 Y-8.749 Z1.600
  (SweepAndDrillSafelyFromTo [31.817|-8.749|1.600] [31.817|-8.749|1.600] s=5.000 bt=False)
  (MillLine s=[31.817|-8.749] e=[28.452|-10.192] h=1.600 bt=False)
G01 F150.000 X28.452 Y-10.192 Z1.600
  (SweepAndDrillSafelyFromTo [28.452|-10.192|1.600] [28.452|-10.192|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [28.452|-10.192|5.000] [26.516|-10.341|5.000] s=5.000 bt=False)
G00 X26.516 Y-10.341
  (Drill l=[26.516|-10.341])
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (SweepAndDrillSafelyFromTo [26.516|-10.341|1.600] [30.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
G00 X30.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[30.000|-13.877] r=1.150)
G00 X30.000 Y-14.027
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.150 X30.000 Y-13.727 Z1.400
G02 F150.000 I0 J-0.150 X30.000 Y-14.027 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.150 X30.000 Y-13.727 Z0.200
G02 F150.000 I0 J-0.150 X30.000 Y-14.027 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.150 X30.000 Y-13.727 Z-0.100
G02 F150.000 I0 J-0.150 X30.000 Y-14.027 Z-0.100
G00 X30.000 Y-13.877
  (SweepAndDrillSafelyFromTo [30.000|-13.877|-0.100] [40.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X40.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[40.000|-13.877] r=1.200)
G00 X40.000 Y-14.077
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.200 X40.000 Y-13.677 Z1.400
G02 F150.000 I0 J-0.200 X40.000 Y-14.077 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.200 X40.000 Y-13.677 Z0.200
G02 F150.000 I0 J-0.200 X40.000 Y-14.077 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.200 X40.000 Y-13.677 Z-0.100
G02 F150.000 I0 J-0.200 X40.000 Y-14.077 Z-0.100
G00 X40.000 Y-13.877
  (SweepAndDrillSafelyFromTo [40.000|-13.877|-0.100] [51.497|-3.302|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X51.497 Y-3.302
  (SweepAndDrillSafelyFromTo [51.497|-3.302|5.000] [51.497|-3.302|1.600] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (MillLine s=[51.497|-3.302] e=[48.452|-3.302] h=1.600 bt=False)
G01 F150.000 X48.452 Y-3.302 Z1.600
  (SweepAndDrillSafelyFromTo [48.452|-3.302|1.600] [48.452|-3.302|1.600] s=5.000 bt=False)
  (MillLine s=[48.452|-3.302] e=[48.612|-6.666] h=1.600 bt=False)
G01 F150.000 X48.612 Y-6.666 Z1.600
  (SweepAndDrillSafelyFromTo [48.612|-6.666|1.600] [48.612|-6.666|1.600] s=5.000 bt=False)
  (MillLine s=[48.612|-6.666] e=[51.817|-8.749] h=1.600 bt=False)
G01 F150.000 X51.817 Y-8.749 Z1.600
  (SweepAndDrillSafelyFromTo [51.817|-8.749|1.600] [51.817|-8.749|1.600] s=5.000 bt=False)
  (MillLine s=[51.817|-8.749] e=[48.452|-10.192] h=1.600 bt=False)
G01 F150.000 X48.452 Y-10.192 Z1.600
  (SweepAndDrillSafelyFromTo [48.452|-10.192|1.600] [48.452|-10.192|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [48.452|-10.192|5.000] [46.516|-10.341|5.000] s=5.000 bt=False)
G00 X46.516 Y-10.341
  (Drill l=[46.516|-10.341])
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (SweepAndDrillSafelyFromTo [46.516|-10.341|1.600] [50.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
G00 X50.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[50.000|-13.877] r=1.250)
G00 X50.000 Y-14.127
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.250 X50.000 Y-13.627 Z1.400
G02 F150.000 I0 J-0.250 X50.000 Y-14.127 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.250 X50.000 Y-13.627 Z0.200
G02 F150.000 I0 J-0.250 X50.000 Y-14.127 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.250 X50.000 Y-13.627 Z-0.100
G02 F150.000 I0 J-0.250 X50.000 Y-14.127 Z-0.100
G00 X50.000 Y-13.877
  (SweepAndDrillSafelyFromTo [50.000|-13.877|-0.100] [60.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X60.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[60.000|-13.877] r=1.300)
G00 X60.000 Y-14.177
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.300 X60.000 Y-13.577 Z1.400
G02 F150.000 I0 J-0.300 X60.000 Y-14.177 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.300 X60.000 Y-13.577 Z0.200
G02 F150.000 I0 J-0.300 X60.000 Y-14.177 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.300 X60.000 Y-13.577 Z-0.100
G02 F150.000 I0 J-0.300 X60.000 Y-14.177 Z-0.100
G00 X60.000 Y-13.877
  (SweepAndDrillSafelyFromTo [60.000|-13.877|-0.100] [70.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X70.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[70.000|-13.877] r=1.350)
G00 X70.000 Y-14.227
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.350 X70.000 Y-13.527 Z1.400
G02 F150.000 I0 J-0.350 X70.000 Y-14.227 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.350 X70.000 Y-13.527 Z0.200
G02 F150.000 I0 J-0.350 X70.000 Y-14.227 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.350 X70.000 Y-13.527 Z-0.100
G02 F150.000 I0 J-0.350 X70.000 Y-14.227 Z-0.100
G00 X70.000 Y-13.877
  (SweepAndDrillSafelyFromTo [70.000|-13.877|-0.100] [80.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X80.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[80.000|-13.877] r=1.400)
G00 X80.000 Y-14.277
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.400 X80.000 Y-13.477 Z1.400
G02 F150.000 I0 J-0.400 X80.000 Y-14.277 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.400 X80.000 Y-13.477 Z0.200
G02 F150.000 I0 J-0.400 X80.000 Y-14.277 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.400 X80.000 Y-13.477 Z-0.100
G02 F150.000 I0 J-0.400 X80.000 Y-14.277 Z-0.100
G00 X80.000 Y-13.877
  (SweepAndDrillSafelyFromTo [80.000|-13.877|-0.100] [90.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X90.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[90.000|-13.877] r=1.450)
G00 X90.000 Y-14.327
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.450 X90.000 Y-13.427 Z1.400
G02 F150.000 I0 J-0.450 X90.000 Y-14.327 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.450 X90.000 Y-13.427 Z0.200
G02 F150.000 I0 J-0.450 X90.000 Y-14.327 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.450 X90.000 Y-13.427 Z-0.100
G02 F150.000 I0 J-0.450 X90.000 Y-14.327 Z-0.100
G00 X90.000 Y-13.877
  (SweepAndDrillSafelyFromTo [90.000|-13.877|-0.100] [94.057|-4.728|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X94.057 Y-4.728
  (SweepAndDrillSafelyFromTo [94.057|-4.728|5.000] [94.057|-4.728|1.600] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (MillLine s=[94.057|-4.728] e=[97.102|-4.728] h=1.600 bt=False)
G01 F150.000 X97.102 Y-4.728 Z1.600
  (SweepAndDrillSafelyFromTo [97.102|-4.728|1.600] [97.102|-4.728|1.600] s=5.000 bt=False)
  (MillLine s=[97.102|-4.728] e=[94.217|-8.093] h=1.600 bt=False)
G01 F150.000 X94.217 Y-8.093 Z1.600
  (SweepAndDrillSafelyFromTo [94.217|-8.093|1.600] [94.217|-8.093|1.600] s=5.000 bt=False)
  (MillLine s=[94.217|-8.093] e=[97.422|-10.176] h=1.600 bt=False)
G01 F150.000 X97.422 Y-10.176 Z1.600
  (SweepAndDrillSafelyFromTo [97.422|-10.176|1.600] [97.422|-10.176|1.600] s=5.000 bt=False)
  (MillLine s=[97.422|-10.176] e=[94.057|-11.618] h=1.600 bt=False)
G01 F150.000 X94.057 Y-11.618 Z1.600
  (SweepAndDrillSafelyFromTo [94.057|-11.618|1.600] [94.057|-11.618|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [94.057|-11.618|5.000] [100.000|-13.877|5.000] s=5.000 bt=False)
G00 X100.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[100.000|-13.877] r=1.500)
G00 X100.000 Y-14.377
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.500 X100.000 Y-13.377 Z1.400
G02 F150.000 I0 J-0.500 X100.000 Y-14.377 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.500 X100.000 Y-13.377 Z0.200
G02 F150.000 I0 J-0.500 X100.000 Y-14.377 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.500 X100.000 Y-13.377 Z-0.100
G02 F150.000 I0 J-0.500 X100.000 Y-14.377 Z-0.100
G00 X100.000 Y-13.877
  (SweepAndDrillSafelyFromTo [100.000|-13.877|-0.100] [110.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X110.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[110.000|-13.877] r=2.000)
G00 X110.000 Y-14.877
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J1.000 X110.000 Y-12.877 Z1.400
G02 F150.000 I0 J-1.000 X110.000 Y-14.877 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J1.000 X110.000 Y-12.877 Z0.200
G02 F150.000 I0 J-1.000 X110.000 Y-14.877 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J1.000 X110.000 Y-12.877 Z-0.100
G02 F150.000 I0 J-1.000 X110.000 Y-14.877 Z-0.100
G00 X110.000 Y-13.877
  (SweepAndDrillSafelyFromTo [110.000|-13.877|-0.100] [120.000|-13.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X120.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[120.000|-13.877] r=2.500)
G00 X120.000 Y-15.377
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J1.500 X120.000 Y-12.377 Z1.400
G02 F150.000 I0 J-1.500 X120.000 Y-15.377 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J1.500 X120.000 Y-12.377 Z0.200
G02 F150.000 I0 J-1.500 X120.000 Y-15.377 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J1.500 X120.000 Y-12.377 Z-0.100
G02 F150.000 I0 J-1.500 X120.000 Y-15.377 Z-0.100
G00 Z5.000
; G00 X120.000 Y-13.877
  (SweepAndDrillSafelyFromTo [120.000|-13.877|5.000] [130.000|-13.877|5.000] s=5.000 bt=False)
G00 X130.000 Y-13.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[130.000|-13.877] r=3.000)
G00 X130.000 Y-15.877
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J2.000 X130.000 Y-11.877 Z1.400
G02 F150.000 I0 J-2.000 X130.000 Y-15.877 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J2.000 X130.000 Y-11.877 Z0.200
G02 F150.000 I0 J-2.000 X130.000 Y-15.877 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J2.000 X130.000 Y-11.877 Z-0.100
G02 F150.000 I0 J-2.000 X130.000 Y-15.877 Z-0.100
G00 Z5.000
; G00 X130.000 Y-13.877
  (SweepAndDrillSafelyFromTo [130.000|-13.877|5.000] [135.501|-9.000|5.000] s=5.000 bt=False)
G00 X135.501 Y-9.000
  (SweepAndDrillSafelyFromTo [135.501|-9.000|5.000] [135.501|-9.000|1.600] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (MillLine s=[135.501|-9.000] e=[138.705|-11.083] h=1.600 bt=False)
G01 F150.000 X138.705 Y-11.083 Z1.600
  (SweepAndDrillSafelyFromTo [138.705|-11.083|1.600] [138.705|-11.083|1.600] s=5.000 bt=False)
  (MillLine s=[138.705|-11.083] e=[135.341|-12.525] h=1.600 bt=False)
G01 F150.000 X135.341 Y-12.525 Z1.600
  (SweepAndDrillSafelyFromTo [135.341|-12.525|1.600] [135.341|-12.525|1.600] s=5.000 bt=False)
  (MillLine s=[135.341|-12.525] e=[135.341|-5.635] h=1.600 bt=False)
G01 F150.000 X135.341 Y-5.635 Z1.600
  (SweepAndDrillSafelyFromTo [135.341|-5.635|1.600] [135.341|-5.635|1.600] s=5.000 bt=False)
  (MillLine s=[135.341|-5.635] e=[138.385|-5.635] h=1.600 bt=False)
G01 F150.000 X138.385 Y-5.635 Z1.600
  (SweepAndDrillSafelyFromTo [138.385|-5.635|1.600] [138.385|-5.635|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [138.385|-5.635|5.000] [130.000|-4.877|5.000] s=5.000 bt=False)
G00 X130.000 Y-4.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[130.000|-4.877] r=1.500)
G00 X130.000 Y-5.377
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.500 X130.000 Y-4.377 Z1.600
G02 F150.000 I0 J-0.500 X130.000 Y-5.377 Z1.600
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.500 X130.000 Y-4.377 Z1.600
G02 F150.000 I0 J-0.500 X130.000 Y-5.377 Z1.600
G00 X130.000 Y-4.877
    (DrillOrPullZFromTo 1.600 4.000)
G00 Z4.000
  (MillHelix l=[130.000|-4.877] r=3.000)
G00 X130.000 Y-6.877
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J2.000 X130.000 Y-2.877 Z1.600
G02 F150.000 I0 J-2.000 X130.000 Y-6.877 Z1.600
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J2.000 X130.000 Y-2.877 Z1.600
G02 F150.000 I0 J-2.000 X130.000 Y-6.877 Z1.600
G00 Z5.000
; G00 X130.000 Y-4.877
  (SweepAndDrillSafelyFromTo [130.000|-4.877|5.000] [120.000|-4.877|5.000] s=5.000 bt=False)
G00 X120.000 Y-4.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[120.000|-4.877] r=2.500)
G00 X120.000 Y-6.377
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J1.500 X120.000 Y-3.377 Z1.500
G02 F150.000 I0 J-1.500 X120.000 Y-6.377 Z1.500
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J1.500 X120.000 Y-3.377 Z1.500
G02 F150.000 I0 J-1.500 X120.000 Y-6.377 Z1.500
G00 Z5.000
; G00 X120.000 Y-4.877
  (SweepAndDrillSafelyFromTo [120.000|-4.877|5.000] [110.000|-4.877|5.000] s=5.000 bt=False)
G00 X110.000 Y-4.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[110.000|-4.877] r=2.000)
G00 X110.000 Y-5.877
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J1.000 X110.000 Y-3.877 Z1.750
G02 F150.000 I0 J-1.000 X110.000 Y-5.877 Z1.500
    (MillSemiCircle l=1.500)
G02 F150.000 I0 J1.000 X110.000 Y-3.877 Z1.250
G02 F150.000 I0 J-1.000 X110.000 Y-5.877 Z1.000
    (MillSemiCircle l=1.000)
G02 F150.000 I0 J1.000 X110.000 Y-3.877 Z0.800
G02 F150.000 I0 J-1.000 X110.000 Y-5.877 Z0.800
    (MillSemiCircle l=0.500)
G02 F150.000 I0 J1.000 X110.000 Y-3.877 Z0.800
G02 F150.000 I0 J-1.000 X110.000 Y-5.877 Z0.800
G00 X110.000 Y-4.877
  (SweepAndDrillSafelyFromTo [110.000|-4.877|0.800] [100.000|-4.877|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 5.000)
G00 Z5.000
G00 X100.000 Y-4.877
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[100.000|-4.877] r=1.500)
G00 X100.000 Y-5.377
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.500 X100.000 Y-4.377 Z1.600
G02 F150.000 I0 J-0.500 X100.000 Y-5.377 Z1.600
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.500 X100.000 Y-4.377 Z1.600
G02 F150.000 I0 J-0.500 X100.000 Y-5.377 Z1.600
G00 X100.000 Y-4.877
  (SweepAndDrillSafelyFromTo [100.000|-4.877|1.600] [100.000|0.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 5.000)
G00 Z5.000
G00 X100.000 Y0.000
G00 Z5.000
  (Fräslänge:     302 mm   ca.  3 min)
  (Bohrungen:       5 mm   ca.  1 min)
  (Leerfahrten:   398 mm   ca.  2 min)
  (Summe:         706 mm   ca.  4 min)
  (Befehlszahl: 241)
M30
%");
    }

    [TestMethod]
    public void TestMethod15() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.15P.dxf"]));
        Compare("8999.15P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.15P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.15P[8999.15P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [0.000|0.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[0.000|0.000] e=[100.000|0.000] h=0.800 bt=False)
G01 F150.000 X100.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [100.000|0.000|0.800] [100.000|0.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 1.000)
G00 Z1.000
  (MillLine s=[100.000|0.000] e=[100.000|-50.000] h=1.000 bt=False)
G01 F150.000 X100.000 Y-50.000 Z1.000
  (SweepAndDrillSafelyFromTo [100.000|-50.000|1.000] [100.000|-50.000|1.500] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 1.500)
G00 Z1.500
  (MillArc l=[130.000|-29.793] r=36.170 a0=213.962 a1=326.038 h=1.500 p0=[100.000|-50.000] p1=[160.000|-50.000] bt=False)
G03 F150.000 I30.000 J20.207 X160.000 Y-50.000 Z1.500
  (SweepAndDrillSafelyFromTo [160.000|-50.000|1.500] [160.000|-50.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.500 0.800)
G01 Z0.800
  (MillLine s=[160.000|-50.000] e=[160.000|25.000] h=0.800 bt=False)
G01 F150.000 X160.000 Y25.000 Z0.800
  (SweepAndDrillSafelyFromTo [160.000|25.000|0.800] [160.000|25.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[160.000|25.000] e=[160.000|-50.000] h=-0.100 bt=False)
G01 F150.000 X160.000 Y-50.000 Z-0.100
  (SweepAndDrillSafelyFromTo [160.000|-50.000|-0.100] [100.000|0.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X100.000 Y0.000
    (DrillOrPullZFromTo 5.000 -0.100)
G00 Z2.000
G01 Z-0.100
  (MillLine s=[100.000|0.000] e=[0.000|0.000] h=-0.100 bt=False)
G01 F150.000 X0.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [0.000|0.000|-0.100] [160.000|25.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
; G00 X160.000 Y25.000
  (SweepAndDrillSafelyFromTo [160.000|25.000|5.000] [200.000|0.000|5.000] s=5.000 bt=False)
G00 X200.000 Y0.000
  (SweepAndDrillSafelyFromTo [200.000|0.000|5.000] [200.000|0.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[200.000|0.000] e=[300.000|0.000] h=0.800 bt=False)
G01 F150.000 X300.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [300.000|0.000|0.800] [300.000|0.000|0.800] s=5.000 bt=False)
  (MillLine s=[300.000|0.000] e=[300.000|-50.000] h=0.800 bt=False)
G01 F150.000 X300.000 Y-50.000 Z0.800
  (SweepAndDrillSafelyFromTo [300.000|-50.000|0.800] [300.000|-50.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[300.000|-50.000] e=[300.000|0.000] h=-0.100 bt=False)
G01 F150.000 X300.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [300.000|0.000|-0.100] [300.000|0.000|-0.100] s=5.000 bt=False)
  (MillLine s=[300.000|0.000] e=[200.000|0.000] h=-0.100 bt=False)
G01 F150.000 X200.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [200.000|0.000|-0.100] [300.000|-50.000|1.800] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X300.000 Y-50.000
    (DrillOrPullZFromTo 5.000 1.800)
G00 Z2.000
G01 Z1.800
  (MillArc l=[330.000|-29.793] r=36.170 a0=213.962 a1=326.038 h=1.800 p0=[300.000|-50.000] p1=[360.000|-50.000] bt=False)
G03 F150.000 I30.000 J20.207 X360.000 Y-50.000 Z1.800
  (SweepAndDrillSafelyFromTo [360.000|-50.000|1.800] [360.000|-50.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.800 0.800)
G01 Z0.800
  (MillLine s=[360.000|-50.000] e=[360.000|25.000] h=0.800 bt=False)
G01 F150.000 X360.000 Y25.000 Z0.800
  (SweepAndDrillSafelyFromTo [360.000|25.000|0.800] [360.000|25.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[360.000|25.000] e=[360.000|-50.000] h=-0.100 bt=False)
G01 F150.000 X360.000 Y-50.000 Z-0.100
  (SweepAndDrillSafelyFromTo [360.000|-50.000|-0.100] [360.000|25.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X360.000 Y25.000
G00 Z5.000
  (Fräslänge:     992 mm   ca. 10 min)
  (Bohrungen:      13 mm   ca.  1 min)
  (Leerfahrten:   345 mm   ca.  1 min)
  (Summe:        1350 mm   ca. 11 min)
  (Befehlszahl: 37)
M30
%");
    }

    [TestMethod]
    public void TestMethod16() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.16P.dxf"]));
        Compare("8999.16P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.16P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.16P[8999.16P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [20.000|0.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 5.000)
G00 Z5.000
G00 X20.000 Y0.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|5.000] [20.000|0.000|1.600] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.600)
G00 Z2.000
G01 Z1.600
  (MillLine s=[20.000|0.000] e=[30.000|-20.000] h=1.600 bt=False)
G01 F150.000 X30.000 Y-20.000 Z1.600
  (SweepAndDrillSafelyFromTo [30.000|-20.000|1.600] [30.000|-20.000|1.600] s=5.000 bt=False)
  (MillLine s=[30.000|-20.000] e=[20.000|0.000] h=1.600 bt=True)
G01 F150.000 X20.000 Y0.000 Z1.600
  (SweepAndDrillSafelyFromTo [20.000|0.000|1.600] [20.000|0.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.600 1.000)
G01 Z1.000
  (MillLine s=[20.000|0.000] e=[45.000|-10.000] h=1.000 bt=False)
G01 F150.000 X45.000 Y-10.000 Z1.000
  (SweepAndDrillSafelyFromTo [45.000|-10.000|1.000] [30.000|-20.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 5.000)
G00 Z5.000
G00 X30.000 Y-20.000
    (DrillOrPullZFromTo 5.000 1.000)
G00 Z2.000
G01 Z1.000
  (MillLine s=[30.000|-20.000] e=[30.000|-35.000] h=1.000 bt=False)
G01 F150.000 X30.000 Y-35.000 Z1.000
  (SweepAndDrillSafelyFromTo [30.000|-35.000|1.000] [30.000|-35.000|1.000] s=5.000 bt=False)
  (MillLine s=[30.000|-35.000] e=[30.000|-20.000] h=1.000 bt=True)
G01 F150.000 X30.000 Y-20.000 Z1.000
  (SweepAndDrillSafelyFromTo [30.000|-20.000|1.000] [30.000|-20.000|1.000] s=5.000 bt=False)
  (MillLine s=[30.000|-20.000] e=[35.000|-30.000] h=1.000 bt=False)
G01 F150.000 X35.000 Y-30.000 Z1.000
  (SweepAndDrillSafelyFromTo [35.000|-30.000|1.000] [35.000|-30.000|1.000] s=5.000 bt=False)
  (MillLine s=[35.000|-30.000] e=[35.000|-45.000] h=1.000 bt=False)
G01 F150.000 X35.000 Y-45.000 Z1.000
  (SweepAndDrillSafelyFromTo [35.000|-45.000|1.000] [35.000|-45.000|1.000] s=5.000 bt=False)
  (MillLine s=[35.000|-45.000] e=[35.000|-30.000] h=1.000 bt=True)
G01 F150.000 X35.000 Y-30.000 Z1.000
  (SweepAndDrillSafelyFromTo [35.000|-30.000|1.000] [35.000|-30.000|1.000] s=5.000 bt=False)
  (MillLine s=[35.000|-30.000] e=[30.000|-20.000] h=1.000 bt=True)
G01 F150.000 X30.000 Y-20.000 Z1.000
  (SweepAndDrillSafelyFromTo [30.000|-20.000|1.000] [30.000|-20.000|0.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 0.000)
G01 Z0.000
  (MillLine s=[30.000|-20.000] e=[30.000|-35.000] h=0.000 bt=False)
G01 F150.000 X30.000 Y-35.000 Z0.000
  (SweepAndDrillSafelyFromTo [30.000|-35.000|0.000] [30.000|-35.000|0.000] s=5.000 bt=False)
  (MillLine s=[30.000|-35.000] e=[30.000|-20.000] h=0.000 bt=True)
G01 F150.000 X30.000 Y-20.000 Z0.000
  (SweepAndDrillSafelyFromTo [30.000|-20.000|0.000] [30.000|-20.000|0.000] s=5.000 bt=False)
  (MillLine s=[30.000|-20.000] e=[35.000|-30.000] h=0.000 bt=False)
G01 F150.000 X35.000 Y-30.000 Z0.000
  (SweepAndDrillSafelyFromTo [35.000|-30.000|0.000] [35.000|-30.000|0.000] s=5.000 bt=False)
  (MillLine s=[35.000|-30.000] e=[35.000|-45.000] h=0.000 bt=False)
G01 F150.000 X35.000 Y-45.000 Z0.000
  (SweepAndDrillSafelyFromTo [35.000|-45.000|0.000] [35.000|-45.000|0.000] s=5.000 bt=False)
  (MillLine s=[35.000|-45.000] e=[35.000|-30.000] h=0.000 bt=True)
G01 F150.000 X35.000 Y-30.000 Z0.000
  (SweepAndDrillSafelyFromTo [35.000|-30.000|0.000] [35.000|-30.000|0.000] s=5.000 bt=False)
  (MillLine s=[35.000|-30.000] e=[30.000|-20.000] h=0.000 bt=True)
G01 F150.000 X30.000 Y-20.000 Z0.000
  (SweepAndDrillSafelyFromTo [30.000|-20.000|0.000] [30.000|-20.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.000 -0.100)
G01 Z-0.100
  (MillLine s=[30.000|-20.000] e=[30.000|-35.000] h=-0.100 bt=False)
G01 F150.000 X30.000 Y-35.000 Z-0.100
  (SweepAndDrillSafelyFromTo [30.000|-35.000|-0.100] [30.000|-35.000|-0.100] s=5.000 bt=False)
  (MillLine s=[30.000|-35.000] e=[30.000|-20.000] h=-0.100 bt=True)
G01 F150.000 X30.000 Y-20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [30.000|-20.000|-0.100] [30.000|-20.000|-0.100] s=5.000 bt=False)
  (MillLine s=[30.000|-20.000] e=[35.000|-30.000] h=-0.100 bt=False)
G01 F150.000 X35.000 Y-30.000 Z-0.100
  (SweepAndDrillSafelyFromTo [35.000|-30.000|-0.100] [35.000|-30.000|-0.100] s=5.000 bt=False)
  (MillLine s=[35.000|-30.000] e=[35.000|-45.000] h=-0.100 bt=False)
G01 F150.000 X35.000 Y-45.000 Z-0.100
  (SweepAndDrillSafelyFromTo [35.000|-45.000|-0.100] [35.000|-45.000|-0.100] s=5.000 bt=False)
  (MillLine s=[35.000|-45.000] e=[35.000|-30.000] h=-0.100 bt=True)
G01 F150.000 X35.000 Y-30.000 Z-0.100
  (SweepAndDrillSafelyFromTo [35.000|-30.000|-0.100] [35.000|-30.000|-0.100] s=5.000 bt=False)
  (MillLine s=[35.000|-30.000] e=[30.000|-20.000] h=-0.100 bt=True)
G01 F150.000 X30.000 Y-20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [30.000|-20.000|-0.100] [45.000|-10.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X45.000 Y-10.000
  (Drill l=[45.000|-10.000])
    (DrillOrPullZFromTo 5.000 -0.100)
G00 Z2.000
G01 Z-0.100
  (SweepAndDrillSafelyFromTo [45.000|-10.000|-0.100] [45.000|-10.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 1.000)
G00 Z1.000
  (MillLine s=[45.000|-10.000] e=[55.000|-20.000] h=1.000 bt=False)
G01 F150.000 X55.000 Y-20.000 Z1.000
  (SweepAndDrillSafelyFromTo [55.000|-20.000|1.000] [55.000|-20.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 5.000)
G00 Z5.000
  (Drill l=[55.000|-20.000])
    (DrillOrPullZFromTo 5.000 -0.100)
G00 Z2.000
G01 Z-0.100
  (SweepAndDrillSafelyFromTo [55.000|-20.000|-0.100] [55.000|-20.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 1.000)
G00 Z1.000
  (MillLine s=[55.000|-20.000] e=[55.000|-35.000] h=1.000 bt=False)
G01 F150.000 X55.000 Y-35.000 Z1.000
  (SweepAndDrillSafelyFromTo [55.000|-35.000|1.000] [55.000|-35.000|1.000] s=5.000 bt=False)
  (MillLine s=[55.000|-35.000] e=[55.000|-20.000] h=1.000 bt=True)
G01 F150.000 X55.000 Y-20.000 Z1.000
  (SweepAndDrillSafelyFromTo [55.000|-20.000|1.000] [55.000|-20.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [55.000|-20.000|5.000] [55.000|-20.000|5.000] s=5.000 bt=True)
  (SweepAndDrillSafelyFromTo [55.000|-20.000|5.000] [55.000|-20.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.000)
G00 Z2.000
G01 Z1.000
  (MillLine s=[55.000|-20.000] e=[45.000|-10.000] h=1.000 bt=True)
G01 F150.000 X45.000 Y-10.000 Z1.000
  (SweepAndDrillSafelyFromTo [45.000|-10.000|1.000] [45.000|-10.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [45.000|-10.000|5.000] [45.000|-10.000|5.000] s=5.000 bt=True)
  (SweepAndDrillSafelyFromTo [45.000|-10.000|5.000] [45.000|-10.000|1.000] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 1.000)
G00 Z2.000
G01 Z1.000
  (MillLine s=[45.000|-10.000] e=[20.000|0.000] h=1.000 bt=True)
G01 F150.000 X20.000 Y0.000 Z1.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|1.000] [20.000|0.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 1.000 0.800)
G01 Z0.800
  (MillLine s=[20.000|0.000] e=[60.000|0.000] h=0.800 bt=False)
G01 F150.000 X60.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [60.000|0.000|0.800] [60.000|0.000|0.800] s=5.000 bt=False)
  (MillLine s=[60.000|0.000] e=[75.000|-10.000] h=0.800 bt=False)
G01 F150.000 X75.000 Y-10.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|-10.000|0.800] [75.000|-10.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[75.000|-10.000] e=[60.000|0.000] h=-0.100 bt=False)
G01 F150.000 X60.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [60.000|0.000|-0.100] [60.000|0.000|-0.100] s=5.000 bt=False)
  (MillLine s=[60.000|0.000] e=[20.000|0.000] h=-0.100 bt=False)
G01 F150.000 X20.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [20.000|0.000|-0.100] [75.000|-10.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X75.000 Y-10.000
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[75.000|-10.000] r=2.000)
G00 X75.000 Y-11.000
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J1.000 X75.000 Y-9.000 Z1.400
G02 F150.000 I0 J-1.000 X75.000 Y-11.000 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J1.000 X75.000 Y-9.000 Z0.200
G02 F150.000 I0 J-1.000 X75.000 Y-11.000 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J1.000 X75.000 Y-9.000 Z-0.100
G02 F150.000 I0 J-1.000 X75.000 Y-11.000 Z-0.100
G00 X75.000 Y-10.000
  (SweepAndDrillSafelyFromTo [75.000|-10.000|-0.100] [75.000|-10.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 0.800)
G00 Z0.800
  (MillLine s=[75.000|-10.000] e=[75.000|-20.000] h=0.800 bt=False)
G01 F150.000 X75.000 Y-20.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|-20.000|0.800] [75.000|-20.000|0.800] s=5.000 bt=False)
  (MillLine s=[75.000|-20.000] e=[85.000|-30.000] h=0.800 bt=False)
G01 F150.000 X85.000 Y-30.000 Z0.800
  (SweepAndDrillSafelyFromTo [85.000|-30.000|0.800] [85.000|-30.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[85.000|-30.000] e=[75.000|-20.000] h=-0.100 bt=False)
G01 F150.000 X75.000 Y-20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|-20.000|-0.100] [75.000|-20.000|-0.100] s=5.000 bt=False)
  (MillLine s=[75.000|-20.000] e=[75.000|-10.000] h=-0.100 bt=False)
G01 F150.000 X75.000 Y-10.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|-10.000|-0.100] [85.000|-30.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X85.000 Y-30.000
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[85.000|-30.000] r=4.000)
G00 X85.000 Y-33.000
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J3.000 X85.000 Y-27.000 Z1.400
G02 F150.000 I0 J-3.000 X85.000 Y-33.000 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J3.000 X85.000 Y-27.000 Z0.200
G02 F150.000 I0 J-3.000 X85.000 Y-33.000 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J3.000 X85.000 Y-27.000 Z-0.100
G02 F150.000 I0 J-3.000 X85.000 Y-33.000 Z-0.100
G00 Z5.000
G00 X85.000 Y-30.000
  (SweepAndDrillSafelyFromTo [85.000|-30.000|5.000] [85.000|-30.000|5.000] s=5.000 bt=True)
  (SweepAndDrillSafelyFromTo [85.000|-30.000|5.000] [85.000|-30.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[85.000|-30.000] e=[75.000|-20.000] h=0.800 bt=True)
G01 F150.000 X75.000 Y-20.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|-20.000|0.800] [75.000|-20.000|0.800] s=5.000 bt=False)
  (MillLine s=[75.000|-20.000] e=[75.000|-30.000] h=0.800 bt=False)
G01 F150.000 X75.000 Y-30.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|-30.000|0.800] [75.000|-30.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[75.000|-30.000] e=[75.000|-20.000] h=-0.100 bt=False)
G01 F150.000 X75.000 Y-20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|-20.000|-0.100] [75.000|-20.000|-0.100] s=5.000 bt=False)
  (MillLine s=[75.000|-20.000] e=[85.000|-30.000] h=-0.100 bt=True)
G01 F150.000 X85.000 Y-30.000 Z-0.100
  (SweepAndDrillSafelyFromTo [85.000|-30.000|-0.100] [75.000|-30.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X75.000 Y-30.000
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[75.000|-30.000] r=3.000)
G00 X75.000 Y-32.000
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J2.000 X75.000 Y-28.000 Z1.400
G02 F150.000 I0 J-2.000 X75.000 Y-32.000 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J2.000 X75.000 Y-28.000 Z0.200
G02 F150.000 I0 J-2.000 X75.000 Y-32.000 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J2.000 X75.000 Y-28.000 Z-0.100
G02 F150.000 I0 J-2.000 X75.000 Y-32.000 Z-0.100
G00 Z5.000
G00 X75.000 Y-30.000
  (SweepAndDrillSafelyFromTo [75.000|-30.000|5.000] [75.000|-30.000|5.000] s=5.000 bt=True)
  (SweepAndDrillSafelyFromTo [75.000|-30.000|5.000] [75.000|-30.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[75.000|-30.000] e=[75.000|-20.000] h=0.800 bt=True)
G01 F150.000 X75.000 Y-20.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|-20.000|0.800] [75.000|-20.000|0.800] s=5.000 bt=False)
  (MillLine s=[75.000|-20.000] e=[75.000|-10.000] h=0.800 bt=True)
G01 F150.000 X75.000 Y-10.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|-10.000|0.800] [75.000|-10.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[75.000|-10.000] e=[75.000|-20.000] h=-0.100 bt=True)
G01 F150.000 X75.000 Y-20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|-20.000|-0.100] [75.000|-20.000|-0.100] s=5.000 bt=False)
  (MillLine s=[75.000|-20.000] e=[75.000|-30.000] h=-0.100 bt=True)
G01 F150.000 X75.000 Y-30.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|-30.000|-0.100] [75.000|-10.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X75.000 Y-10.000
  (SweepAndDrillSafelyFromTo [75.000|-10.000|5.000] [75.000|-10.000|5.000] s=5.000 bt=True)
  (SweepAndDrillSafelyFromTo [75.000|-10.000|5.000] [75.000|-10.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[75.000|-10.000] e=[60.000|0.000] h=0.800 bt=True)
G01 F150.000 X60.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [60.000|0.000|0.800] [60.000|0.000|0.800] s=5.000 bt=False)
  (MillLine s=[60.000|0.000] e=[65.000|-10.000] h=0.800 bt=False)
G01 F150.000 X65.000 Y-10.000 Z0.800
  (SweepAndDrillSafelyFromTo [65.000|-10.000|0.800] [65.000|-10.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[65.000|-10.000] e=[60.000|0.000] h=-0.100 bt=False)
G01 F150.000 X60.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [60.000|0.000|-0.100] [60.000|0.000|-0.100] s=5.000 bt=False)
  (MillLine s=[60.000|0.000] e=[75.000|-10.000] h=-0.100 bt=True)
G01 F150.000 X75.000 Y-10.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|-10.000|-0.100] [65.000|-10.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
G00 X65.000 Y-10.000
    (DrillOrPullZFromTo 5.000 4.000)
G00 Z4.000
  (MillHelix l=[65.000|-10.000] r=1.500)
G00 X65.000 Y-10.500
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J0.500 X65.000 Y-9.500 Z1.400
G02 F150.000 I0 J-0.500 X65.000 Y-10.500 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J0.500 X65.000 Y-9.500 Z0.200
G02 F150.000 I0 J-0.500 X65.000 Y-10.500 Z-0.100
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J0.500 X65.000 Y-9.500 Z-0.100
G02 F150.000 I0 J-0.500 X65.000 Y-10.500 Z-0.100
G00 X65.000 Y-10.000
  (SweepAndDrillSafelyFromTo [65.000|-10.000|-0.100] [65.000|-10.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 0.800)
G00 Z0.800
  (MillLine s=[65.000|-10.000] e=[65.000|-25.000] h=0.800 bt=False)
G01 F150.000 X65.000 Y-25.000 Z0.800
  (SweepAndDrillSafelyFromTo [65.000|-25.000|0.800] [65.000|-25.000|0.800] s=5.000 bt=False)
  (MillLine s=[65.000|-25.000] e=[65.000|-10.000] h=0.800 bt=True)
G01 F150.000 X65.000 Y-10.000 Z0.800
  (SweepAndDrillSafelyFromTo [65.000|-10.000|0.800] [65.000|-10.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[65.000|-10.000] e=[65.000|-25.000] h=-0.100 bt=False)
G01 F150.000 X65.000 Y-25.000 Z-0.100
  (SweepAndDrillSafelyFromTo [65.000|-25.000|-0.100] [65.000|-25.000|-0.100] s=5.000 bt=False)
  (MillLine s=[65.000|-25.000] e=[65.000|-10.000] h=-0.100 bt=True)
G01 F150.000 X65.000 Y-10.000 Z-0.100
  (SweepAndDrillSafelyFromTo [65.000|-10.000|-0.100] [65.000|-10.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [65.000|-10.000|5.000] [65.000|-10.000|5.000] s=5.000 bt=True)
  (SweepAndDrillSafelyFromTo [65.000|-10.000|5.000] [65.000|-10.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[65.000|-10.000] e=[60.000|0.000] h=0.800 bt=True)
G01 F150.000 X60.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [60.000|0.000|0.800] [60.000|0.000|0.800] s=5.000 bt=False)
  (MillLine s=[60.000|0.000] e=[20.000|0.000] h=0.800 bt=True)
G01 F150.000 X20.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [20.000|0.000|0.800] [20.000|0.000|0.800] s=5.000 bt=False)
  (MillLine s=[20.000|0.000] e=[60.000|15.000] h=0.800 bt=False)
G01 F150.000 X60.000 Y15.000 Z0.800
  (SweepAndDrillSafelyFromTo [60.000|15.000|0.800] [60.000|15.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[60.000|15.000] e=[20.000|0.000] h=-0.100 bt=False)
G01 F150.000 X20.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [20.000|0.000|-0.100] [20.000|0.000|-0.100] s=5.000 bt=False)
  (MillLine s=[20.000|0.000] e=[60.000|0.000] h=-0.100 bt=True)
G01 F150.000 X60.000 Y0.000 Z-0.100
  (SweepAndDrillSafelyFromTo [60.000|0.000|-0.100] [60.000|0.000|-0.100] s=5.000 bt=False)
  (MillLine s=[60.000|0.000] e=[65.000|-10.000] h=-0.100 bt=True)
G01 F150.000 X65.000 Y-10.000 Z-0.100
  (SweepAndDrillSafelyFromTo [65.000|-10.000|-0.100] [60.000|15.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
; G00 X60.000 Y15.000
  (SweepAndDrillSafelyFromTo [60.000|15.000|5.000] [75.000|5.000|5.000] s=5.000 bt=False)
G00 X75.000 Y5.000
  (SweepAndDrillSafelyFromTo [75.000|5.000|5.000] [75.000|5.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[75.000|5.000] e=[105.000|5.000] h=0.800 bt=False)
G01 F150.000 X105.000 Y5.000 Z0.800
  (SweepAndDrillSafelyFromTo [105.000|5.000|0.800] [105.000|5.000|0.800] s=5.000 bt=False)
  (MillLine s=[105.000|5.000] e=[75.000|5.000] h=0.800 bt=True)
G01 F150.000 X75.000 Y5.000 Z0.800
  (SweepAndDrillSafelyFromTo [75.000|5.000|0.800] [75.000|5.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[75.000|5.000] e=[105.000|5.000] h=-0.100 bt=False)
G01 F150.000 X105.000 Y5.000 Z-0.100
  (SweepAndDrillSafelyFromTo [105.000|5.000|-0.100] [105.000|5.000|-0.100] s=5.000 bt=False)
  (MillLine s=[105.000|5.000] e=[75.000|5.000] h=-0.100 bt=True)
G01 F150.000 X75.000 Y5.000 Z-0.100
  (SweepAndDrillSafelyFromTo [75.000|5.000|-0.100] [75.000|5.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [75.000|5.000|5.000] [60.000|15.000|5.000] s=5.000 bt=True)
; G00 X60.000 Y15.000
  (SweepAndDrillSafelyFromTo [60.000|15.000|5.000] [70.000|20.000|5.000] s=5.000 bt=False)
G00 X70.000 Y20.000
  (SweepAndDrillSafelyFromTo [70.000|20.000|5.000] [70.000|20.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[70.000|20.000] e=[85.000|20.000] h=0.800 bt=False)
G01 F150.000 X85.000 Y20.000 Z0.800
  (SweepAndDrillSafelyFromTo [85.000|20.000|0.800] [85.000|20.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[85.000|20.000] e=[70.000|20.000] h=-0.100 bt=False)
G01 F150.000 X70.000 Y20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [70.000|20.000|-0.100] [85.000|20.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
; G00 X85.000 Y20.000
  (SweepAndDrillSafelyFromTo [85.000|20.000|5.000] [105.000|15.000|5.000] s=5.000 bt=False)
G00 X105.000 Y15.000
  (SweepAndDrillSafelyFromTo [105.000|15.000|5.000] [105.000|15.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[105.000|15.000] e=[115.000|15.000] h=0.800 bt=False)
G01 F150.000 X115.000 Y15.000 Z0.800
  (SweepAndDrillSafelyFromTo [115.000|15.000|0.800] [115.000|15.000|0.800] s=5.000 bt=False)
  (MillLine s=[115.000|15.000] e=[105.000|15.000] h=0.800 bt=True)
G01 F150.000 X105.000 Y15.000 Z0.800
  (SweepAndDrillSafelyFromTo [105.000|15.000|0.800] [105.000|15.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[105.000|15.000] e=[115.000|15.000] h=-0.100 bt=False)
G01 F150.000 X115.000 Y15.000 Z-0.100
  (SweepAndDrillSafelyFromTo [115.000|15.000|-0.100] [115.000|15.000|-0.100] s=5.000 bt=False)
  (MillLine s=[115.000|15.000] e=[105.000|15.000] h=-0.100 bt=True)
G01 F150.000 X105.000 Y15.000 Z-0.100
  (SweepAndDrillSafelyFromTo [105.000|15.000|-0.100] [105.000|15.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [105.000|15.000|5.000] [85.000|20.000|5.000] s=5.000 bt=True)
; G00 X85.000 Y20.000
  (SweepAndDrillSafelyFromTo [85.000|20.000|5.000] [105.000|25.000|5.000] s=5.000 bt=False)
G00 X105.000 Y25.000
  (SweepAndDrillSafelyFromTo [105.000|25.000|5.000] [105.000|25.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[105.000|25.000] e=[115.000|25.000] h=0.800 bt=False)
G01 F150.000 X115.000 Y25.000 Z0.800
  (SweepAndDrillSafelyFromTo [115.000|25.000|0.800] [115.000|25.000|0.800] s=5.000 bt=False)
  (MillLine s=[115.000|25.000] e=[105.000|25.000] h=0.800 bt=True)
G01 F150.000 X105.000 Y25.000 Z0.800
  (SweepAndDrillSafelyFromTo [105.000|25.000|0.800] [105.000|25.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[105.000|25.000] e=[115.000|25.000] h=-0.100 bt=False)
G01 F150.000 X115.000 Y25.000 Z-0.100
  (SweepAndDrillSafelyFromTo [115.000|25.000|-0.100] [115.000|25.000|-0.100] s=5.000 bt=False)
  (MillLine s=[115.000|25.000] e=[105.000|25.000] h=-0.100 bt=True)
G01 F150.000 X105.000 Y25.000 Z-0.100
  (SweepAndDrillSafelyFromTo [105.000|25.000|-0.100] [105.000|25.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
  (SweepAndDrillSafelyFromTo [105.000|25.000|5.000] [85.000|20.000|5.000] s=5.000 bt=True)
G00 X85.000 Y20.000
  (SweepAndDrillSafelyFromTo [85.000|20.000|5.000] [85.000|20.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[85.000|20.000] e=[70.000|20.000] h=0.800 bt=True)
G01 F150.000 X70.000 Y20.000 Z0.800
  (SweepAndDrillSafelyFromTo [70.000|20.000|0.800] [70.000|20.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[70.000|20.000] e=[85.000|20.000] h=-0.100 bt=True)
G01 F150.000 X85.000 Y20.000 Z-0.100
  (SweepAndDrillSafelyFromTo [85.000|20.000|-0.100] [70.000|20.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
; G00 X70.000 Y20.000
  (SweepAndDrillSafelyFromTo [70.000|20.000|5.000] [60.000|15.000|5.000] s=5.000 bt=True)
G00 X60.000 Y15.000
  (SweepAndDrillSafelyFromTo [60.000|15.000|5.000] [60.000|15.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000
G01 Z0.800
  (MillLine s=[60.000|15.000] e=[20.000|0.000] h=0.800 bt=True)
G01 F150.000 X20.000 Y0.000 Z0.800
  (SweepAndDrillSafelyFromTo [20.000|0.000|0.800] [20.000|0.000|-0.100] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.100)
G01 Z-0.100
  (MillLine s=[20.000|0.000] e=[60.000|15.000] h=-0.100 bt=True)
G01 F150.000 X60.000 Y15.000 Z-0.100
  (SweepAndDrillSafelyFromTo [60.000|15.000|-0.100] [20.000|0.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.100 5.000)
G00 Z5.000
; G00 X20.000 Y0.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|5.000] [50.000|30.000|5.000] s=5.000 bt=False)
G00 X50.000 Y30.000
  (START Subpath 8998.2P[8999.16P.dxf] t=[ [120.000|170.000]=>[50.000|30.000] / [120.000|100.000]=>[50.000|100.000] ])
  (SweepAndDrillSafelyFromTo [50.000|30.000|5.000] [50.000|30.000|-0.200] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 -0.200)
G00 Z1.000
G01 Z-0.200
  (MillLine s=[50.000|30.000] e=[30.000|50.000] h=-0.200 bt=False)
G01 F150.000 X30.000 Y50.000 Z-0.200
  (SweepAndDrillSafelyFromTo [30.000|50.000|-0.200] [30.000|50.000|-0.200] s=5.000 bt=False)
  (MillLine s=[30.000|50.000] e=[30.000|80.000] h=-0.200 bt=False)
G01 F150.000 X30.000 Y80.000 Z-0.200
  (SweepAndDrillSafelyFromTo [30.000|80.000|-0.200] [30.000|80.000|-0.200] s=5.000 bt=False)
  (MillLine s=[30.000|80.000] e=[50.000|100.000] h=-0.200 bt=False)
G01 F150.000 X50.000 Y100.000 Z-0.200
  (SweepAndDrillSafelyFromTo [50.000|100.000|-0.200] [50.000|100.000|-0.300] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.200 -0.300)
G01 Z-0.300
  (MillLine s=[50.000|100.000] e=[30.000|80.000] h=-0.300 bt=False)
G01 F150.000 X30.000 Y80.000 Z-0.300
  (SweepAndDrillSafelyFromTo [30.000|80.000|-0.300] [30.000|80.000|-0.300] s=5.000 bt=False)
  (MillLine s=[30.000|80.000] e=[30.000|50.000] h=-0.300 bt=False)
G01 F150.000 X30.000 Y50.000 Z-0.300
  (SweepAndDrillSafelyFromTo [30.000|50.000|-0.300] [30.000|50.000|-0.300] s=5.000 bt=False)
  (MillLine s=[30.000|50.000] e=[50.000|30.000] h=-0.300 bt=False)
G01 F150.000 X50.000 Y30.000 Z-0.300
  (SweepAndDrillSafelyFromTo [50.000|30.000|-0.300] [50.000|100.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.300 5.000)
G00 Z5.000
; G00 X50.000 Y100.000
  (END Subpath 8998.2P[8999.16P.dxf] t=[ [120.000|170.000]=>[50.000|30.000] / [120.000|100.000]=>[50.000|100.000] ])
  (SweepAndDrillSafelyFromTo [50.000|100.000|5.000] [65.000|35.000|5.000] s=5.000 bt=False)
G00 X65.000 Y35.000
  (START Subpath 8998.2P[8999.16P.dxf] t=[ [120.000|170.000]=>[65.000|35.000] / [120.000|100.000]=>[65.000|105.000] ])
  (SweepAndDrillSafelyFromTo [65.000|35.000|5.000] [65.000|35.000|-0.200] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 -0.200)
G00 Z1.000
G01 Z-0.200
  (MillLine s=[65.000|35.000] e=[45.000|55.000] h=-0.200 bt=False)
G01 F150.000 X45.000 Y55.000 Z-0.200
  (SweepAndDrillSafelyFromTo [45.000|55.000|-0.200] [45.000|55.000|-0.200] s=5.000 bt=False)
  (MillLine s=[45.000|55.000] e=[45.000|85.000] h=-0.200 bt=False)
G01 F150.000 X45.000 Y85.000 Z-0.200
  (SweepAndDrillSafelyFromTo [45.000|85.000|-0.200] [45.000|85.000|-0.200] s=5.000 bt=False)
  (MillLine s=[45.000|85.000] e=[65.000|105.000] h=-0.200 bt=False)
G01 F150.000 X65.000 Y105.000 Z-0.200
  (SweepAndDrillSafelyFromTo [65.000|105.000|-0.200] [65.000|105.000|-0.300] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.200 -0.300)
G01 Z-0.300
  (MillLine s=[65.000|105.000] e=[45.000|85.000] h=-0.300 bt=False)
G01 F150.000 X45.000 Y85.000 Z-0.300
  (SweepAndDrillSafelyFromTo [45.000|85.000|-0.300] [45.000|85.000|-0.300] s=5.000 bt=False)
  (MillLine s=[45.000|85.000] e=[45.000|55.000] h=-0.300 bt=False)
G01 F150.000 X45.000 Y55.000 Z-0.300
  (SweepAndDrillSafelyFromTo [45.000|55.000|-0.300] [45.000|55.000|-0.300] s=5.000 bt=False)
  (MillLine s=[45.000|55.000] e=[65.000|35.000] h=-0.300 bt=False)
G01 F150.000 X65.000 Y35.000 Z-0.300
  (SweepAndDrillSafelyFromTo [65.000|35.000|-0.300] [65.000|105.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.300 5.000)
G00 Z5.000
; G00 X65.000 Y105.000
  (END Subpath 8998.2P[8999.16P.dxf] t=[ [120.000|170.000]=>[65.000|35.000] / [120.000|100.000]=>[65.000|105.000] ])
  (SweepAndDrillSafelyFromTo [65.000|105.000|5.000] [65.000|35.000|5.000] s=5.000 bt=True)
; G00 X65.000 Y35.000
  (SweepAndDrillSafelyFromTo [65.000|35.000|5.000] [50.000|100.000|5.000] s=5.000 bt=True)
; G00 X50.000 Y100.000
  (SweepAndDrillSafelyFromTo [50.000|100.000|5.000] [50.000|30.000|5.000] s=5.000 bt=True)
; G00 X50.000 Y30.000
  (SweepAndDrillSafelyFromTo [50.000|30.000|5.000] [95.000|40.000|5.000] s=5.000 bt=False)
G00 X95.000 Y40.000
  (START Subpath 8998.2P[8999.16P.dxf] t=[ [120.000|170.000]=>[95.000|40.000] / [120.000|100.000]=>[95.000|110.000] ])
  (SweepAndDrillSafelyFromTo [95.000|40.000|5.000] [95.000|40.000|-0.200] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 -0.200)
G00 Z1.000
G01 Z-0.200
  (MillLine s=[95.000|40.000] e=[75.000|60.000] h=-0.200 bt=False)
G01 F150.000 X75.000 Y60.000 Z-0.200
  (SweepAndDrillSafelyFromTo [75.000|60.000|-0.200] [75.000|60.000|-0.200] s=5.000 bt=False)
  (MillLine s=[75.000|60.000] e=[75.000|90.000] h=-0.200 bt=False)
G01 F150.000 X75.000 Y90.000 Z-0.200
  (SweepAndDrillSafelyFromTo [75.000|90.000|-0.200] [75.000|90.000|-0.200] s=5.000 bt=False)
  (MillLine s=[75.000|90.000] e=[95.000|110.000] h=-0.200 bt=False)
G01 F150.000 X95.000 Y110.000 Z-0.200
  (SweepAndDrillSafelyFromTo [95.000|110.000|-0.200] [95.000|110.000|-0.300] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.200 -0.300)
G01 Z-0.300
  (MillLine s=[95.000|110.000] e=[75.000|90.000] h=-0.300 bt=False)
G01 F150.000 X75.000 Y90.000 Z-0.300
  (SweepAndDrillSafelyFromTo [75.000|90.000|-0.300] [75.000|90.000|-0.300] s=5.000 bt=False)
  (MillLine s=[75.000|90.000] e=[75.000|60.000] h=-0.300 bt=False)
G01 F150.000 X75.000 Y60.000 Z-0.300
  (SweepAndDrillSafelyFromTo [75.000|60.000|-0.300] [75.000|60.000|-0.300] s=5.000 bt=False)
  (MillLine s=[75.000|60.000] e=[95.000|40.000] h=-0.300 bt=False)
G01 F150.000 X95.000 Y40.000 Z-0.300
  (SweepAndDrillSafelyFromTo [95.000|40.000|-0.300] [95.000|110.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.300 5.000)
G00 Z5.000
; G00 X95.000 Y110.000
  (END Subpath 8998.2P[8999.16P.dxf] t=[ [120.000|170.000]=>[95.000|40.000] / [120.000|100.000]=>[95.000|110.000] ])
  (SweepAndDrillSafelyFromTo [95.000|110.000|5.000] [95.000|40.000|5.000] s=5.000 bt=True)
; G00 X95.000 Y40.000
  (SweepAndDrillSafelyFromTo [95.000|40.000|5.000] [50.000|30.000|5.000] s=5.000 bt=True)
; G00 X50.000 Y30.000
  (SweepAndDrillSafelyFromTo [50.000|30.000|5.000] [20.000|0.000|5.000] s=5.000 bt=True)
; G00 X20.000 Y0.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|5.000] [20.000|110.000|5.000] s=5.000 bt=False)
G00 X20.000 Y110.000
G00 Z5.000
  (Fräslänge:    1950 mm   ca. 19 min)
  (Bohrungen:      61 mm   ca.  1 min)
  (Leerfahrten:   737 mm   ca.  3 min)
  (Summe:        2748 mm   ca. 21 min)
  (Befehlszahl: 233)
M30
%");
    }

    [TestMethod]
    public void TestMethod17() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.17P.dxf"]));
        Compare("8999.17P_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.17P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|6.000])
G00 Z6.000
G00 X0.000 Y0.000
  (Model 8999.17P[8999.17P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|6.000] [20.000|0.000|6.000] s=6.000 bt=False)
    (DrillOrPullZFromTo 6.000 6.000)
G00 Z6.000
G00 X20.000 Y0.000
    (DrillOrPullZFromTo 6.000 7.000)
G00 Z7.000
  (MillHelix l=[20.000|0.000] r=5.000)
G00 X20.000 Y-4.000
    (MillSemiCircle l=5.000)
G02 F150.000 I0 J4.000 X20.000 Y4.000 Z4.500
G02 F150.000 I0 J-4.000 X20.000 Y-4.000 Z4.000
    (MillSemiCircle l=4.000)
G02 F150.000 I0 J4.000 X20.000 Y4.000 Z3.500
G02 F150.000 I0 J-4.000 X20.000 Y-4.000 Z3.000
    (MillSemiCircle l=3.000)
G02 F150.000 I0 J4.000 X20.000 Y4.000 Z3.000
G02 F150.000 I0 J-4.000 X20.000 Y-4.000 Z3.000
G00 Z6.000
G00 X20.000 Y0.000
    (DrillOrPullZFromTo 6.000 7.000)
G00 Z7.000
  (MillHelix l=[20.000|0.000] r=10.000)
G00 X20.000 Y-9.000
    (MillSemiCircle l=5.000)
G02 F150.000 I0 J9.000 X20.000 Y9.000 Z4.500
G02 F150.000 I0 J-9.000 X20.000 Y-9.000 Z4.000
    (MillSemiCircle l=4.000)
G02 F150.000 I0 J9.000 X20.000 Y9.000 Z3.500
G02 F150.000 I0 J-9.000 X20.000 Y-9.000 Z3.000
    (MillSemiCircle l=3.000)
G02 F150.000 I0 J9.000 X20.000 Y9.000 Z2.500
G02 F150.000 I0 J-9.000 X20.000 Y-9.000 Z2.000
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J9.000 X20.000 Y9.000 Z1.500
G02 F150.000 I0 J-9.000 X20.000 Y-9.000 Z1.000
    (MillSemiCircle l=1.000)
G02 F150.000 I0 J9.000 X20.000 Y9.000 Z1.000
G02 F150.000 I0 J-9.000 X20.000 Y-9.000 Z1.000
G00 Z6.000
; G00 X20.000 Y0.000
  (SweepAndDrillSafelyFromTo [20.000|0.000|6.000] [45.000|0.000|6.000] s=6.000 bt=False)
G00 X45.000 Y0.000
G00 Z6.000
  (Fräslänge:     358 mm   ca.  4 min)
  (Bohrungen:       0 mm   ca.  0 min)
  (Leerfahrten:    72 mm   ca.  1 min)
  (Summe:         430 mm   ca.  4 min)
  (Befehlszahl: 27)
M30
%");
    }

    [TestMethod]
    public void TestMethod18() {
        Assert.AreEqual(1, Program.Main(["/f150", "/v500", "/s15", "8999.18P.dxf"]));
    }

    [TestMethod]
    public void TestMethod19() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "8999.19P.dxf"]));
        Compare("8999.19P_Clean.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.19P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
  (Model 8999.19P[8999.19P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|5.000] [0.000|0.000|0.800] s=5.000 bt=False)
    (DrillOrPullZFromTo 5.000 0.800)
G00 Z2.000(=2.000+0.794*[#51-2.000]+0.206*[#52-2.000])
G01 Z0.800(=0.800+0.794*[#51-2.000]+0.206*[#52-2.000])
  (MillLine s=[0.000|0.000] e=[57.687|0.000] h=0.800 bt=False)
G01 F150.000 X57.687 Y0.000 Z0.800(=0.800+0.794*[#51-2.000]+0.206*[#52-2.000])
  (SweepAndDrillSafelyFromTo [57.687|0.000|0.800] [57.687|0.000|-0.300] s=5.000 bt=False)
    (DrillOrPullZFromTo 0.800 -0.300)
G01 Z-0.300(=-0.300+0.788*[#52-2.000]+0.212*[#51-2.000])
  (MillLine s=[57.687|0.000] e=[0.000|0.000] h=-0.300 bt=False)
G01 F150.000 X0.000 Y0.000 Z-0.300(=-0.300+0.788*[#52-2.000]+0.212*[#51-2.000])
  (SweepAndDrillSafelyFromTo [0.000|0.000|-0.300] [57.687|0.000|5.000] s=5.000 bt=False)
    (DrillOrPullZFromTo -0.300 5.000)
G00 Z5.000(=5.000+0.794*[#51-2.000]+0.206*[#52-2.000])
G00 X57.687 Y0.000
G00 Z5.000
  (Fräslänge:     115 mm   ca.  2 min)
  (Bohrungen:       4 mm   ca.  1 min)
  (Leerfahrten:    66 mm   ca.  1 min)
  (Summe:         185 mm   ca.  2 min)
  (Befehlszahl: 8)
M30
%");
        Compare("8999.19P_Z.txt", $@"([57.358|242.020]/T:2.000) #51=
([97.060|242.020]/T:2.000) #52=");
        Compare("8999.19P_Probing.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.19P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|5.000])
G00 Z5.000
G00 X0.000 Y0.000
G00 X8.429 Y9.528
G00 Z4.000
G38.3 Z0 F150.000
G04 P4
G00 Z5.000
G00 X48.131 Y9.528
G00 Z4.000
G38.3 Z0 F150.000
G04 P4
G00 Z5.000
G00 X0.000 Y0.000
G00 Z5.000
M30
%");
    }

    [TestMethod]
    public void TestMethod21() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s15", "/c", "8999.21P.dxf"]));
    }

    [TestMethod]
    public void TestMethod22() {
        Assert.AreEqual(1, Program.Main(["/f150", "/v500", "/s15", "/c", "8999.22P.dxf"]));
    }

    [TestMethod]
    public void TestMethod23() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s8", "8999.23 Pv.dxf"]));
        Compare("8999.23 Pv_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.23 Pv.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|8.000])
G00 Z8.000
G00 X0.000 Y0.000
  (Model 8999.23P[8999.23 Pv.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|8.000] [30.000|-10.000|8.000] s=8.000 bt=False)
    (DrillOrPullZFromTo 8.000 8.000)
G00 Z8.000
G00 X30.000 Y-10.000
    (DrillOrPullZFromTo 8.000 7.000)
G00 Z7.000
  (MillHelix l=[30.000|-10.000] r=15.000)
G00 X30.000 Y-24.000
    (MillSemiCircle l=5.000)
G02 F150.000 I0 J14.000 X30.000 Y4.000 Z4.000
G02 F150.000 I0 J-14.000 X30.000 Y-24.000 Z3.000
    (MillSemiCircle l=3.000)
G02 F150.000 I0 J14.000 X30.000 Y4.000 Z2.000
G02 F150.000 I0 J-14.000 X30.000 Y-24.000 Z1.000
    (MillSemiCircle l=1.000)
G02 F150.000 I0 J14.000 X30.000 Y4.000 Z1.000
G02 F150.000 I0 J-14.000 X30.000 Y-24.000 Z1.000
  (SupportBar)
  (MillArc l=[30.000|-10.000] r=14.000 a0=270.000 a1=241.352 h=1.000 p0=[30.000|-24.000] p1=[23.288|-22.286] bt=False)
G02 F150.000 I0.000 J14.000 X23.288 Y-22.286 Z1.000
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (MillArc l=[30.000|-10.000] r=14.000 a0=241.352 a1=118.648 h=-0.500 p0=[23.288|-22.286] p1=[23.288|2.286] bt=False)
G02 F150.000 I6.712 J12.286 X23.288 Y2.286 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillArc l=[30.000|-10.000] r=14.000 a0=118.648 a1=61.352 h=1.000 p0=[23.288|2.286] p1=[36.712|2.286] bt=False)
G02 F150.000 I6.712 J-12.286 X36.712 Y2.286 Z1.000
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (MillArc l=[30.000|-10.000] r=14.000 a0=61.352 a1=298.648 h=-0.500 p0=[36.712|2.286] p1=[36.712|-22.286] bt=False)
G02 F150.000 I-6.712 J-12.286 X36.712 Y-22.286 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillArc l=[30.000|-10.000] r=14.000 a0=298.648 a1=270.000 h=1.000 p0=[36.712|-22.286] p1=[30.000|-24.000] bt=False)
G02 F150.000 I-6.712 J12.286 X30.000 Y-24.000 Z1.000
G00 Z8.000
; G00 X30.000 Y-10.000
  (SweepAndDrillSafelyFromTo [30.000|-10.000|8.000] [5.000|-20.000|8.000] s=8.000 bt=False)
G00 X5.000 Y-20.000
G00 Z8.000
  (Fräslänge:     352 mm   ca.  4 min)
  (Bohrungen:      11 mm   ca.  1 min)
  (Leerfahrten:    85 mm   ca.  1 min)
  (Summe:         448 mm   ca.  4 min)
  (Befehlszahl: 22)
M30
%",
// lg=(30-2)*3.14=87.96 > 6+30+12+30+6=84 => 5 geometries, 3 bars
s => Count(s, "SupportBar") == 3);
    }

    [TestMethod]
    public void TestMethod24() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s8", "8999.24 Pv.dxf"]));
        Compare("8999.24 Pv_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.24 Pv.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|8.000])
G00 Z8.000
G00 X0.000 Y0.000
  (Model 8999.24P[8999.24 Pv.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|8.000] [30.000|-10.000|8.000] s=8.000 bt=False)
    (DrillOrPullZFromTo 8.000 8.000)
G00 Z8.000
G00 X30.000 Y-10.000
    (DrillOrPullZFromTo 8.000 7.000)
G00 Z7.000
  (MillHelix l=[30.000|-10.000] r=8.000)
G00 X30.000 Y-17.000
    (MillSemiCircle l=5.000)
G02 F150.000 I0 J7.000 X30.000 Y-3.000 Z4.000
G02 F150.000 I0 J-7.000 X30.000 Y-17.000 Z3.000
    (MillSemiCircle l=3.000)
G02 F150.000 I0 J7.000 X30.000 Y-3.000 Z2.000
G02 F150.000 I0 J-7.000 X30.000 Y-17.000 Z1.000
    (MillSemiCircle l=1.000)
G02 F150.000 I0 J7.000 X30.000 Y-3.000 Z1.000
G02 F150.000 I0 J-7.000 X30.000 Y-17.000 Z1.000
  (SupportBar)
  (MillArc l=[30.000|-10.000] r=7.000 a0=270.000 a1=212.704 h=1.000 p0=[30.000|-17.000] p1=[24.110|-13.782] bt=False)
G02 F150.000 I0.000 J7.000 X24.110 Y-13.782 Z1.000
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (MillArc l=[30.000|-10.000] r=7.000 a0=212.704 a1=327.296 h=-0.500 p0=[24.110|-13.782] p1=[35.890|-13.782] bt=False)
G02 F150.000 I5.890 J3.782 X35.890 Y-13.782 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillArc l=[30.000|-10.000] r=7.000 a0=327.296 a1=270.000 h=1.000 p0=[35.890|-13.782] p1=[30.000|-17.000] bt=False)
G02 F150.000 I-5.890 J3.782 X30.000 Y-17.000 Z1.000
G00 Z8.000
; G00 X30.000 Y-10.000
  (SweepAndDrillSafelyFromTo [30.000|-10.000|8.000] [5.000|-20.000|8.000] s=8.000 bt=False)
G00 X5.000 Y-20.000
G00 Z8.000
  (Fräslänge:     176 mm   ca.  2 min)
  (Bohrungen:       6 mm   ca.  1 min)
  (Leerfahrten:    77 mm   ca.  1 min)
  (Summe:         258 mm   ca.  2 min)
  (Befehlszahl: 18)
M30
%",
// lg=(16-2)*3.14=43.98 > 6+30+6=42 => 3 geometries, 2 (half) bars
s => Count(s, "SupportBar") == 2);
    }

    [TestMethod]
    public void TestMethod25() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s8", "8999.25 Pv.dxf"]));
        Compare("8999.25 Pv_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.25 Pv.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|8.000])
G00 Z8.000
G00 X0.000 Y0.000
  (Model 8999.25P[8999.25 Pv.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|8.000] [30.000|-10.000|8.000] s=8.000 bt=False)
    (DrillOrPullZFromTo 8.000 8.000)
G00 Z8.000
G00 X30.000 Y-10.000
    (DrillOrPullZFromTo 8.000 7.000)
G00 Z7.000
  (MillHelix l=[30.000|-10.000] r=5.000)
G00 X30.000 Y-14.000
    (MillSemiCircle l=5.000)
G02 F150.000 I0 J4.000 X30.000 Y-6.000 Z4.000
G02 F150.000 I0 J-4.000 X30.000 Y-14.000 Z3.000
    (MillSemiCircle l=3.000)
G02 F150.000 I0 J4.000 X30.000 Y-6.000 Z2.000
G02 F150.000 I0 J-4.000 X30.000 Y-14.000 Z1.000
    (MillSemiCircle l=1.000)
G02 F150.000 I0 J4.000 X30.000 Y-6.000 Z1.000
G02 F150.000 I0 J-4.000 X30.000 Y-14.000 Z1.000
G00 Z8.000
; G00 X30.000 Y-10.000
  (SweepAndDrillSafelyFromTo [30.000|-10.000|8.000] [5.000|-20.000|8.000] s=8.000 bt=False)
G00 X5.000 Y-20.000
G00 Z8.000
  (Fräslänge:      75 mm   ca.  1 min)
  (Bohrungen:       0 mm   ca.  0 min)
  (Leerfahrten:    71 mm   ca.  1 min)
  (Summe:         146 mm   ca.  1 min)
  (Befehlszahl: 13)
M30
%",
// R=5 < P/2=6
s => Count(s, "SupportBar") == 0);
    }

    [TestMethod]
    public void TestMethod26() {
        Assert.AreEqual(0, Program.Main(["/f150", "/v500", "/s8", "8999.26 Pv.dxf"]));
        Compare("8999.26 Pv_Milling.gcode", $@"%
(PathDxf2GCode - HMMüller 2024-2025 V.{Program.VERSION})
(8999.26 Pv.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|8.000])
G00 Z8.000
G00 X0.000 Y0.000
  (Model 8999.26P[8999.26 Pv.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|8.000] [0.000|0.000|3.000] s=8.000 bt=False)
    (DrillOrPullZFromTo 8.000 3.000)
G00 Z5.000
G01 Z3.000
  (MillLine s=[0.000|0.000] e=[100.000|0.000] h=3.000 bt=False)
G01 F150.000 X100.000 Y0.000 Z3.000
  (SweepAndDrillSafelyFromTo [100.000|0.000|3.000] [100.000|0.000|3.000] s=8.000 bt=False)
  (MillLine s=[100.000|0.000] e=[17.680|-56.775] h=3.000 bt=False)
G01 F150.000 X17.680 Y-56.775 Z3.000
  (SweepAndDrillSafelyFromTo [17.680|-56.775|3.000] [17.680|-56.775|1.000] s=8.000 bt=False)
    (DrillOrPullZFromTo 3.000 1.000)
G01 Z1.000
  (MillLine s=[17.680|-56.775] e=[100.000|0.000] h=1.000 bt=False)
G01 F150.000 X100.000 Y0.000 Z1.000
  (SweepAndDrillSafelyFromTo [100.000|0.000|1.000] [100.000|0.000|1.000] s=8.000 bt=False)
  (MillLine s=[100.000|0.000] e=[0.000|0.000] h=1.000 bt=False)
G01 F150.000 X0.000 Y0.000 Z1.000
  (SweepAndDrillSafelyFromTo [0.000|0.000|1.000] [0.000|0.000|-0.500] s=8.000 bt=False)
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillLine s=[0.000|0.000] e=[7.000|0.000] h=1.000 bt=False)
G01 F150.000 X7.000 Y0.000 Z1.000
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (MillLine s=[7.000|0.000] e=[93.000|0.000] h=-0.500 bt=False)
G01 F150.000 X93.000 Y0.000 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillLine s=[93.000|0.000] e=[100.000|0.000] h=1.000 bt=False)
G01 F150.000 X100.000 Y0.000 Z1.000
  (SweepAndDrillSafelyFromTo [100.000|0.000|1.000] [100.000|0.000|-0.500] s=8.000 bt=False)
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillLine s=[100.000|0.000] e=[94.238|-3.974] h=1.000 bt=False)
G01 F150.000 X94.238 Y-3.974 Z1.000
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (MillLine s=[94.238|-3.974] e=[64.602|-24.413] h=-0.500 bt=False)
G01 F150.000 X64.602 Y-24.413 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillLine s=[64.602|-24.413] e=[53.078|-32.362] h=1.000 bt=False)
G01 F150.000 X53.078 Y-32.362 Z1.000
    (DrillOrPullZFromTo 1.000 -0.500)
G01 Z-0.500
  (MillLine s=[53.078|-32.362] e=[23.442|-52.801] h=-0.500 bt=False)
G01 F150.000 X23.442 Y-52.801 Z-0.500
  (SupportBar)
    (DrillOrPullZFromTo -0.500 1.000)
G00 Z1.000
  (MillLine s=[23.442|-52.801] e=[17.680|-56.775] h=1.000 bt=False)
G01 F150.000 X17.680 Y-56.775 Z1.000
  (SweepAndDrillSafelyFromTo [17.680|-56.775|1.000] [17.680|-56.775|8.000] s=8.000 bt=False)
    (DrillOrPullZFromTo 1.000 8.000)
G00 Z8.000
G00 Z8.000
  (Fräslänge:     600 mm   ca.  6 min)
  (Bohrungen:      34 mm   ca.  1 min)
  (Leerfahrten:    18 mm   ca.  1 min)
  (Summe:         651 mm   ca.  6 min)
  (Befehlszahl: 27)
M30
%",
s => Count(s, "SupportBar") == 2 + 3);
    }
}
