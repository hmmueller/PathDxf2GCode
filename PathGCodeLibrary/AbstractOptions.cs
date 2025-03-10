namespace de.hmmueller.PathGCodeLibrary;

public class AbstractOptions {
    protected static FormatException FormatException(string message, params object[] pars)
        => new FormatException(string.Format(message, pars));

    protected static string GetStringOption3(string[] args, ref int i, string missingMessage) {
        return args[i].Length > 2 ? args[i][2..]
            : i < args.Length - 1 ? args[++i]
            : throw FormatException(missingMessage, args[i - 1]);
    }


}

