namespace de.hmmueller.PathGCodeLibrary;

using System.Diagnostics;
using System.Globalization;

public delegate bool HandleOption<TOptions>(string opt, string[] args, ref int i, TOptions options, MessageHandler messages)
    where TOptions : AbstractOptions;

public class AbstractOptions {
    protected static FormatException FormatException(string message, params object[] pars)
        => new FormatException(string.Format(message, pars));

    protected static string GetStringOption(string[] args, ref int i, string missingMessage_Name) {
        return args[i].Length > 2 ? args[i][2..]
            : i < args.Length - 1 ? args[++i]
            : throw FormatException(missingMessage_Name, args[i - 1]);
    }

    protected static double GetDoubleOption(string[] args, ref int i, string missingMessage_Name, string nanMessage_Name_Value, string lessThan0Message_Name_Value) {
        string a = args[i][2..];
        string v = GetStringOption(args, ref i, missingMessage_Name).Replace(',', '.');
        double result;
        try {
            result = double.Parse(v, CultureInfo.InvariantCulture);
        } catch (FormatException) {
            throw FormatException(nanMessage_Name_Value, v, a);
        }
        return result >= 0 ? result : throw FormatException(lessThan0Message_Name_Value, v, a);
    }

    protected static bool FillOptions<TOptions>(string[] args, TOptions options, MessageHandler messages,
        Action<string> missingOptionAfter, Action<string> unsupportedOption, HandleOption<TOptions> handleOption,
        Action<string, TOptions, MessageHandler> handleArgument, Func<TOptions, MessageHandler, bool> checkOptions)
            where TOptions : AbstractOptions {
        bool doNotRun = false;
        for (int i = 0; i < args.Length; i++) {
            string a = args[i];
            try {
                if (a.StartsWith('/') || a.StartsWith('-')) {
                    if (a.Length == 1) {
                        doNotRun = true;
                        missingOptionAfter(a);
                    } else if (a[1..] == "debug") {
                        Debugger.Launch();
                    } else {
                        string opt = a.Substring(1, 1).ToLowerInvariant();
                        if (opt == "h" || opt == "?") {
                            doNotRun = true;
                            break;
                        } else if (handleOption(opt, args, ref i, options, messages)) {
                            // ok
                        } else {
                            doNotRun = true;
                            unsupportedOption(a);
                        }
                    }
                } else {
                    handleArgument(a, options, messages);
                }
            } catch (Exception ex) {
                doNotRun = true;
                messages.WriteLine(ex.Message);
            }
        }

        if (!checkOptions(options, messages)) {
            doNotRun = true;
        }

        if (doNotRun) {
            messages.WriteLine();
            return false;
        } else {
            return true;
        }
    }
}

