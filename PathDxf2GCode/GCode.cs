namespace de.hmmueller.PathDxf2GCode;

public abstract class GCode {
    public abstract string AsString();
}

public class G0H : GCode {
    public bool AtOrAbovePathS { get; }
    public double X { get; }
    public double Y { get; }

    public G0H(double x, double y, bool atOrAbovePathS) {
        X = x;
        Y = y;
        AtOrAbovePathS = atOrAbovePathS;
    }

    public override string AsString() => $"G00 X{X.F3()} Y{Y.F3()}";
}

public class Comment : GCode {
    public string S { get; }

    public Comment(string s) {
        S = s;
    }

    public override string AsString() => S;
}

public class OtherGCode : GCode {
    public string G { get; }

    public OtherGCode(string g) {
        G = g;
    }

    public override string AsString() => G;
}
