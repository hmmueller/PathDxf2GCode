using System.Globalization;

namespace de.hmmueller.PathGCodeLibrary;

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
}

