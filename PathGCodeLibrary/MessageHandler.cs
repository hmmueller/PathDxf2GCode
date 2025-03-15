namespace de.hmmueller.PathGCodeLibrary;

using System.IO;

public class MessageHandler {
    public const string ErrorPrefix = "**** ";
    public const string InfoPrefix = "---- ";

    private readonly List<string> _errors = new();
    private readonly TextWriter _error;

    public MessageHandler(TextWriter error) {
        _error = error;
    }

    public void Write(string s, params object[] pars) {
        _error.Write(s, pars);
    }

    public void WriteLine(string s, params object[] pars) {
        _error.WriteLine(s, pars);
    }

    public void WriteLine() {
        _error.WriteLine();
    }

    public void AddError(string context, string message, params object?[] pars) {
        string fullMsg = ErrorPrefix + context.Replace("\r\n", "|") + ": " + string.Format(message, pars);
        if (!_errors.Contains(fullMsg)) {
            _errors.Add(fullMsg);
        }
    }

    public IEnumerable<string> Errors => _errors;

    public bool WriteErrors() {
        foreach (var e in Errors) {
            WriteLine(e);
        }
        return Errors.Any();
    }

}