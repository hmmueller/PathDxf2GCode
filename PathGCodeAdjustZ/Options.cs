namespace de.hmmueller.PathGCodeAdjustZ;

using de.hmmueller.PathGCodeLibrary;
using System.Globalization;

public class Options : AbstractOptions {
    public readonly List<string> GCodeFilePaths = new();

    /// <summary>
    /// /m: Maximum correction allowed. This is useful to
    /// avoid wrong values in the _Z.txt file; or wrong
    /// T settings in a DXF file.
    /// </summary>
    public double MaxCorrection_mm { get; private set; } = -1;

    /// <summary>
    /// /x: Pattern for lines that are to be written to stdout.
    /// </summary>
    public string? OutputPattern { get; private set; }

    public static void Usage(MessageHandler messages) {
        messages.WriteLine(Messages.Options_Help);
    }

    private static bool HandleOption(string opt, string[] args, ref int i, Options options, MessageHandler messages) {
        switch (opt) {
            case "m":
                options.MaxCorrection_mm = GetDoubleOption(args, ref i,
                    Messages.Options_MissingOptionAfter_Name, Messages.Options_NaN_Name_Value,
                    Messages.Options_LessThan0_Name_Value);
                return true;
            case "x":
                options.OutputPattern = GetStringOption(args, ref i, Messages.Options_MissingOptionAfter_Name);
                return true;
            case "l":
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(GetStringOption(args, ref i, Messages.Options_MissingLocale));
                return true;
            default:
                return false;
        }
    }

    private static void HandleArgument(string a, Options options, MessageHandler messages) {
        options.GCodeFilePaths.Add(a);
    }

    private static bool CheckOptions(Options options, MessageHandler messages) {
        if (options.MaxCorrection_mm <= 0) {
            messages.AddError("Options", Messages.Options_MissingM);
            return false;
        } else {
            return true;
        }
    }

    public static Options? Create(string[] args, MessageHandler messages) {
        Options options = new();

        return FillOptions(args, options, messages,
            missingOptionAfter: a => messages.AddError("Options", Messages.Options_MissingOptionAfter_Name, a),
            unsupportedOption: a => messages.AddError("Options", Messages.Options_NotSupported_Name, a),
            handleOption: HandleOption,
            handleArgument: HandleArgument,
            checkOptions: CheckOptions) ? options : null;
    }
}

