namespace de.hmmueller.PathGCodeAdjustZ;

using System.Globalization;

internal class ExprEval {
    private readonly Dictionary<string, double> _vars;
    private readonly string _expr;
    private int _pos;
    private char C;
    private void Next() => C = _pos >= _expr.Length ? '=' : _expr[_pos++];
    public readonly double Value;

    public ExprEval(Dictionary<string, double> vars, string expr) {
        _vars = vars;
        _expr = expr;
        Next();
        Value = Expr();
        if (C != '=') {
            throw new Exception(string.Format(Messages.ExprEval_EndOfExprExpected_Pos, _pos - 1));
        }
    }

    private double Expr() {
        double d = Term();
        while (C == '+' || C == '-') {
            if (C == '+') {
                Next();
                d += Term();
            } else {
                Next();
                d -= Term();
            }
        }
        return d;
    }

    private double Term() {
        double d = Factor();
        while (C == '*' || C == '/') {
            if (C == '*') {
                Next();
                d *= Factor();
            } else {
                Next();
                d /= Factor();
            }
        }
        return d;
    }

    private double Factor() {
        double d;
        if (C == '#') {
            string name = "" + C;
            for (Next(); char.IsDigit(C); Next()) {
                name += C;
            }
            d = _vars[name];
        } else if (C == '(') {
            Next();
            d = Expr();
            if (C != ')') {
                throw new Exception(string.Format(Messages.ExprEval_RParExpected_Pos, _pos - 1));
            }
            Next();
        } else if (C == '[') {
            Next();
            d = Expr();
            if (C != ']') {
                throw new Exception(string.Format(Messages.ExprEval_RBrcktExpected_Pos, _pos - 1));
            }
            Next();
        } else if (C == '-') {
            Next();
            d = -Factor();
        } else if (char.IsDigit(C) || C == '.') {
            string v = "" + C;
            for (Next(); char.IsDigit(C) || C == '.'; Next()) {
                v += C;
            }
            d = double.Parse(v, CultureInfo.InvariantCulture);
        } else {
            throw new Exception(string.Format(Messages.ExprEval_Unexpected_Char_Pos, C, _pos - 1));
        }
        return d;
    }
}
