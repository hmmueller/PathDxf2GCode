namespace de.hmmueller.PathGCodeAdjustZ;

using de.hmmueller.PathGCodeLibrary;
using System.Diagnostics;
using System.Globalization;

public class Options : AbstractOptions {
    public readonly List<string> GCodeFilePaths = new();

    /// <summary>
    /// /m: Maximum correction allowed. This is useful to
    /// avoid wrong values in the _Z.txt file; or wrong
    /// T settings in a DXF file.
    /// </summary>
    public double MaxCorrection_mm { get; private set; } = -1;

    public static void Usage(MessageHandler messages) {
        messages.WriteLine(Messages.Options_Help);
    }

    public static Options? Create(string[] args, MessageHandler messages) {
        bool doNotRun = false;

        Options options = new();
        for (int i = 0; i < args.Length; i++) {
            string a = args[i];
            try {
                if (a.StartsWith('/') || a.StartsWith('-')) {
                    if (a.Length == 1) {
                        doNotRun = true;
                        messages.AddError("Options", Messages.Options_MissingOptionAfter_Name, a);
                    } else if (a[1..] == "debug") {
                        Debugger.Launch();
                    } else {
                        switch (a.Substring(1, 1).ToLowerInvariant()) {
                            case "h":
                            case "?":
                                doNotRun = true;
                                break;
                            case "m":
                                options.MaxCorrection_mm = GetDoubleOption(args, ref i,
                                    Messages.Options_MissingOptionAfter_Name, Messages.Options_NaN_Name_Value, 
                                    Messages.Options_LessThan0_Name_Value);
                                break;
                            case "l":
                                Thread.CurrentThread.CurrentUICulture = new CultureInfo(GetStringOption(args, ref i, Messages.Options_MissingLocale));
                                break;
                            default:
                                doNotRun = true;
                                messages.AddError("Options", Messages.Options_NotSupported_Name, a);
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

        if (options.MaxCorrection_mm <= 0) {
            messages.AddError("Options", Messages.Options_MissingM);
            doNotRun = true;
        }

        if (doNotRun) {
            messages.WriteLine();
            return null;
        } else {
            return options;
        }
    }
}

