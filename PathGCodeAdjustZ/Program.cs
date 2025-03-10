namespace de.hmmueller.PathGCodeAdjustZ;

using de.hmmueller.PathGCodeLibrary;
using System.Globalization;
using System.Text.RegularExpressions;
using static System.FormattableString;

public class Program {
    private const string VERSION = "2025-02-28";

    private static int Main(string[] args) {
        Messages messages = new(Console.Error);

        messages.WriteLine($"---- PathGCodeAdjustZ (c) HMMüller 2024-2025 V.{VERSION}");

        Options? options = Options.Create(args, messages);

        if (options == null) {
            Options.Usage(messages);
            return 2;
        } else if (!options.GCodeFilePaths.Any()) {
            messages.Error($"**** Keine G-Code-Dateien angegeben");
            return 3;
        } else {
            foreach (var f in options.GCodeFilePaths) {
                Process(f, messages);
            }
            return 0; // TODO: Bei Fehlern 
        }
    }

    private static void Process(string f, Messages messages) {
        string basePath =
            f.EndsWith("_Z.txt", StringComparison.InvariantCultureIgnoreCase) ? f[..^6] :
            f.EndsWith("_Clean.gcode", StringComparison.InvariantCultureIgnoreCase) ? f[..^12] :
            f.EndsWith(".dxf", StringComparison.InvariantCultureIgnoreCase) ? f[..^4] :
            f.EndsWith(".gcode", StringComparison.InvariantCultureIgnoreCase) ? f[..^6] : f;
        var vars = new Dictionary<string, double>();
        string zFile = basePath + "_Z.txt";
        messages.WriteLine($"---- Einlesen von {zFile}");
        using (var sr = new StreamReader(zFile)) {
            int lineNo = 1;
            for (string? line = null; (line = sr.ReadLine()) != null; lineNo++) {
                string[] fields = line.Split([ '=', ' ', '\t' ], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                if (fields.Length == 0) {
                    // ignore - Leerzeile
                } else if (fields.Length < 3) {
                    messages.Error($"**** Z.{lineNo}: Zeile hat nicht das Format '(Kommentar) #...=Wert'");
                } else {
                    string varName = fields[1];
                    string value = fields[2];
                    try {
                        vars.Add(varName, double.Parse(value.Replace(',', '.'), CultureInfo.InvariantCulture));
                    } catch (FormatException) {
                        messages.Error($"**** Z.{lineNo}: Wert '{value}' für {varName} ist keine gültige Zahl");
                    }
                }
            }
        }

        if (!messages.HasErrors) {
            string cleanFile = basePath + "_Clean.gcode";
            messages.WriteLine($"---- Einlesen von {cleanFile}");
            using (var sr = new StreamReader(cleanFile)) {
                string millingFile = basePath + "_Milling.gcode";
                messages.WriteLine($"---- Schreiben von {millingFile}");
                using (var sw = new StreamWriter(millingFile)) {
                    int lineNo = 1;
                    for (string? line = null; (line = sr.ReadLine()) != null; lineNo++) {
                        Match m = Regex.Match(line, GCodeConstants.ZAdjustmentExpressionRegex);
                        if (m.Success) {
                            string expr = m.Groups[1].Value;
                            string replacement = Invariant($"{new ExprEval(vars, expr).Value:F3}");
                            sw.WriteLine(line[0..m.Index] + replacement + line[(m.Index + m.Length)..] + " (==" + expr + ")");
                        } else {
                            sw.WriteLine(line);
                        }
                    }
                }
            }
        }
    }
}

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
                throw new Exception($"')' expected at pos {_pos - 1}");
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
            throw new Exception($"Unexpected '{C}' at pos {_pos - 1}");
        }
        return d;
    }
}

public class Messages {
    private TextWriter _sw;

    public bool HasErrors { get; private set; }

    public Messages(TextWriter sw) {
        _sw = sw;
    }

    public void WriteLine() {
        _sw.WriteLine();
    }

    public void WriteLine(string msg) {
        _sw.WriteLine(msg);
    }

    public void Error(string msg) {
        if (!HasErrors) {
            WriteLine();
        }
        WriteLine(msg);
        HasErrors = true;
    }
}