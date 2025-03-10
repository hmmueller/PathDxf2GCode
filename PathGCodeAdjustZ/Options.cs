namespace de.hmmueller.PathGCodeAdjustZ;

using de.hmmueller.PathGCodeLibrary;
using System.Diagnostics;
using System.Globalization;

public class Options : AbstractOptions {
    /// <summary>
    /// Zu verarbeitende GCode-Dateien
    /// </summary>    
    public readonly List<string> GCodeFilePaths = new();

    public static void Usage(Messages messages) {
        messages.WriteLine("""
            Aufruf: PathGCodeAdjustZ [Parameter] [GCode-Dateien]

            Parameter:
                /h     Hilfe-Anzeige
            """);
    }

    public static Options? Create(string[] args, Messages messages) {
        bool doNotRun = false;

        Options options = new();
        for (int i = 0; i < args.Length; i++) {
            string a = args[i];
            try {
                if (a.StartsWith('/') || a.StartsWith('-')) {
                    if (a.Length == 1) {
                        doNotRun = true;
                        messages.WriteLine("Fehlende Option nach {a}");
                    } else if (a[1..] == "debug") {
                        Debugger.Launch();
                    } else {
                        switch (a.Substring(1, 1).ToLowerInvariant()) {
                            case "h":
                            case "?":
                                doNotRun = true;
                                break;
                            case "l":
                                Thread.CurrentThread.CurrentUICulture = new CultureInfo(GetStringOption3(args, ref i, "**** Missing locale"));
                                break;
                            default:
                                doNotRun = true;
                                messages.WriteLine($"Option {a} nicht unterstützt");
                                break;
                        }
                    }
                } else {
                    options.GCodeFilePaths.Add(a);
                }
            } catch (Exception ex) {
                doNotRun = true;
                messages.WriteLine(ex.Message);
            }
        }

        if (doNotRun) {
            messages.WriteLine();
            return null;
        } else {
            return options;
        }
    }
}

