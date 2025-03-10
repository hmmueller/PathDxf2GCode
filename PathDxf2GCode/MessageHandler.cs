namespace de.hmmueller.PathDxf2GCode;

using netDxf.Entities;
using netDxf;
using System.IO;

public class MessageHandler {
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
        string fullMsg = Messages.Error + context.Replace("\r\n", "|") + ": " + string.Format(message, pars);
        if (!_errors.Contains(fullMsg)) {
            _errors.Add(fullMsg);
        }
    }

    public void AddError(PathName name, string message, params object?[] pars) {
        AddError(name.ToString(), message, pars);
    }

    public void AddError(EntityObject errorObject, Vector2 position, string dxfFileName, string message, params object?[] pars) {
        AddError(Context(errorObject, position, dxfFileName), message, pars);
    }

    public static string Context(EntityObject errorObject, Vector2 position, string dxfFileName)
        => errorObject.CodeName + " @ " + position.F3() + " # " + new PathName(errorObject.Layer.Name, dxfFileName);

    public IEnumerable<string> Errors => _errors;
}