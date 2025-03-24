namespace de.hmmueller.PathDxf2GCode;

public abstract class GCode {
    private readonly string _s;

    protected const char DEFAULT_LETTER = '_';

    public abstract char Letter { get; }
    public string AsString() => _s;
    public abstract void AddToStatistics(Statistics stats);

    protected GCode(string s) {
        _s = s;
    }
}

public class HorizontalSweepGCode : GCode {
    private readonly double _lg_mm;

    public override char Letter { get; }

    public HorizontalSweepGCode(double x_mm, double y_mm, double lg_mm, bool atOrAboveGlobalS) : base($"G00 X{x_mm.F3()} Y{y_mm.F3()}") {
        Letter = atOrAboveGlobalS ? 'H' : DEFAULT_LETTER;
        _lg_mm = lg_mm;
    }

    public override void AddToStatistics(Statistics stats) {
        stats.AddSweepLength(_lg_mm);
    }
}

public class NonhorizontalSweepGCode : GCode {
    private readonly double _lg_mm;

    public override char Letter => DEFAULT_LETTER;

    public NonhorizontalSweepGCode(string g, double lg_mm) : base(g) {
        _lg_mm = lg_mm;
    }

    public override void AddToStatistics(Statistics stats) {
        stats.AddSweepLength(_lg_mm);
    }
}

public class CommentGCode : GCode {
    public override char Letter => 'C';

    public CommentGCode(string s) : base(s) {
    }

    public override void AddToStatistics(Statistics stats) {
        // empty
    }
}

public class MillGCode : GCode {
    private readonly double _lg_mm;
    private readonly double _f_mmpmin;

    public override char Letter => DEFAULT_LETTER;

    public MillGCode(string g, double lg_mm, double f_mmpmin) : base(g) {
        _lg_mm = lg_mm;
        _f_mmpmin = f_mmpmin;
    }

    public override void AddToStatistics(Statistics stats) {
        stats.AddMillLength(_lg_mm, _f_mmpmin);
    }
}


public class DrillGCode : GCode {
    private readonly double _lg_mm;
    private readonly double _f_mmpmin;

    public override char Letter => DEFAULT_LETTER;

    public DrillGCode(string g, double lg_mm, double f_mmpmin) : base(g) {
        _lg_mm = lg_mm;
        _f_mmpmin = f_mmpmin;
    }

    public override void AddToStatistics(Statistics stats) {
        stats.AddDrillLength(_lg_mm, _f_mmpmin);
    }
}
public class OtherGCode : GCode {
    public override char Letter => DEFAULT_LETTER;

    public OtherGCode(string g) : base(g) {
    }

    public override void AddToStatistics(Statistics stats) {
        // empty
    }
}
