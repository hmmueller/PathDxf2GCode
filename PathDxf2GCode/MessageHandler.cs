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

    public void Write(string s) {
        _error.Write(s);
    }

    public void WriteLine(string s) {
        _error.WriteLine(s);
    }

    public void WriteLine() {
        _error.WriteLine();
    }

    public void AddError(string context, string message) {
        string fullMsg = context.Replace("\r\n", "|") + ": " + message;
        if (!_errors.Contains(fullMsg)) {
            _errors.Add(fullMsg);
        }
    }

    public void AddError(PathName name, string message) {
        AddError(name.ToString(), message);
    }

    public void AddError(EntityObject errorObject, Vector2 position, string dxfFileName, string message) {
        AddError(Context(errorObject, position, dxfFileName), message);
    }

    public static string Context(EntityObject errorObject, Vector2 position, string dxfFileName)
        => errorObject.CodeName + " @ " + position.F3() + " # " + new PathName(errorObject.Layer.Name, dxfFileName);

    public IEnumerable<string> Errors => _errors;
}