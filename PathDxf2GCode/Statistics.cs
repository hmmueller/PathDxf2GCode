namespace de.hmmueller.PathDxf2GCode;

public class Statistics {
    private readonly double _v_mmpmin;
    private int _cmdCt;
    private double _netMillTime_min;
    private double _netDrillTime_min;
    private double _netSweepTime_min;

    public Statistics(double v_mmpmin) {
        _v_mmpmin = v_mmpmin;
    }

    public double MillLength_mm { get; private set; }
    public TimeSpan RoughMillTime
        => TimeSpan.FromMinutes(_netMillTime_min * 1.4);

    public double DrillLength_mm { get; private set; }
    public TimeSpan RoughDrillTime
        => TimeSpan.FromMinutes(_netDrillTime_min * 1.4);

    public double SweepLength_mm { get; private set; }
    public TimeSpan RoughSweepTime
        => TimeSpan.FromMinutes(_netSweepTime_min * 1.4);

    public double TotalLength_mm
        => MillLength_mm + DrillLength_mm + SweepLength_mm;
    public TimeSpan TotalTime
        => RoughMillTime + RoughDrillTime + RoughSweepTime;

    public int CommandCount => _cmdCt;

    public void AddMillLength(double lg_mm, double f_mmpmin) {
        MillLength_mm += lg_mm;
        _netMillTime_min += lg_mm / f_mmpmin;
        _cmdCt++;
    }

    public void AddDrillLength(double lg_mm, double f_mmpmin) {
        DrillLength_mm += lg_mm;
        _netDrillTime_min += lg_mm / f_mmpmin;
        _cmdCt++;
    }

    public void AddSweepLength(double lg_mm) {
        SweepLength_mm += lg_mm;
        _netSweepTime_min += lg_mm / _v_mmpmin;
        _cmdCt++;
    }
}