namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using netDxf.Entities;
using netDxf;
using System.IO;

public class MessageHandlerForEntities : MessageHandler {
    public MessageHandlerForEntities(TextWriter error) : base(error) {
    }

    public void AddError(PathName name, string message, params object?[] pars) {
        AddError(name.ToString(), message, pars);
    }

    public void AddError(EntityObject errorObject, Vector2 position, string dxfFileName, string message, params object?[] pars) {
        AddError(Context(errorObject, position, dxfFileName), message, pars);
    }

    public static string Context(EntityObject errorObject, Vector2 position, string dxfFileName)
        => errorObject.CodeName + " @ " + position.F3() + " # " + new PathName(errorObject.Layer.Name, dxfFileName);
}