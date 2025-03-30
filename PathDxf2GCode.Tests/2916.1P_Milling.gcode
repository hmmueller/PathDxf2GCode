%
(PathDxf2GCode - HMMüller 2024-2025 V.2025-03-24)
(PathDxf2GCode.Tests/2916.1P.dxf)
F150
G17 G21 G40 G49 G54 G80 G90 G94
T1
(SweepSafelyTo [0.000|0.000|15.000])
G00 Z15.000
G00 X0.000 Y0.000
  (Model 2916.1P[PathDxf2GCode.Tests/2916.1P.dxf])
  (SweepAndDrillSafelyFromTo [0.000|0.000|15.000] [8.000|-3.172|15.000] s=15.000 bt=False)
    (DrillOrPullZFromTo 15.000 15.000)
G00 Z15.000
G00 X8.000 Y-3.172
  (SweepAndDrillSafelyFromTo [8.000|-3.172|15.000] [8.000|-3.172|1.000] s=15.000 bt=False)
    (DrillOrPullZFromTo 15.000 1.000)
G00 Z2.000
G01 Z1.000
  (MillArc l=[9.000|-6.000] r=3.000 a0=109.471 a1=250.529 h=1.000 p0=[8.000|-3.172] p1=[8.000|-8.828] bt=False)
G02 F150.000 I1.000 J-2.828 X8.000 Y-8.828 Z1.000
  (SweepAndDrillSafelyFromTo [8.000|-8.828|1.000] [8.000|-8.828|15.000] s=15.000 bt=False)
    (DrillOrPullZFromTo 1.000 15.000)
G00 Z15.000
  (SweepAndDrillSafelyFromTo [8.000|-8.828|15.000] [9.000|-6.000|15.000] s=15.000 bt=False)
G00 X9.000 Y-6.000
    (DrillOrPullZFromTo 15.000 2.000)
G00 Z2.000
  (MillHelix l=[9.000|-6.000] r=2.000)
G01 F150.000 X9.000 Y-7.000
    (MillSemiCircle l=2.000)
G02 F150.000 I0 J1.000 X9.000 Y-5.000 Z1.400
G02 F150.000 I0 J-1.000 X9.000 Y-7.000 Z0.800
    (MillSemiCircle l=0.800)
G02 F150.000 I0 J1.000 X9.000 Y-5.000 Z0.200
G02 F150.000 I0 J-1.000 X9.000 Y-7.000 Z-0.300
    (MillSemiCircle l=-0.400)
G02 F150.000 I0 J1.000 X9.000 Y-5.000 Z-0.300
G02 F150.000 I0 J-1.000 X9.000 Y-7.000 Z-0.300
G00 X9.000 Y-6.000
  (SweepAndDrillSafelyFromTo [9.000|-6.000|-0.300] [0.000|-13.000|15.000] s=15.000 bt=False)
    (DrillOrPullZFromTo -0.300 15.000)
G00 Z15.000
G00 X0.000 Y-13.000
G00 Z15.000
  (Fräslänge:      31 mm   ca.  1 min)
  (Bohrungen:       1 mm   ca.  1 min)
  (Leerfahrten:    87 mm   ca.  1 min)
  (Summe:         119 mm   ca.  1 min)
  (Befehlszahl: 19)
M30
%
