namespace de.hmmueller.PathDxf2GCode;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Tables;
using netDxf.Header;
using de.hmmueller.PathGCodeLibrary;

public static class DxfHelper {
    public const string TILDE_SUFFIX_REGEX = "(~[a-z]+)?";

    public static DxfDocument? LoadDxfDocument(string dxfFilePath, Options options,
            out Dictionary<string, Linetype> layerLinetypes, MessageHandlerForEntities messages) {
        messages.Write(MessageHandler.InfoPrefix + Messages.DxfHelper_ReadingFile__FileName, dxfFilePath);

        DxfVersion v = DxfDocument.CheckDxfFileVersion(dxfFilePath, out bool isBinary);
        messages.WriteLine(" (DXF-Version: {0})", v);

        DxfDocument d = DxfDocument.Load(dxfFilePath);
        string x = "a" + v + " c";

        if (d == null) {
            messages.AddError("Input", Messages.DxfHelper_CannotLoadFile_Path, dxfFilePath);
            layerLinetypes = new();
        } else {
            layerLinetypes = d.Layers.ToDictionary(layer => layer.Name, layer => layer.Linetype);

            if (options.Dump) {
                Dump(dxfFilePath, d.Entities, layerLinetypes, options.PathNamePattern);
            }
        }
        return d;
    }

    public static bool IsOnPathLayer(this EntityObject e, string pathNamePattern, string fileNameForMessages)
        => Regex.IsMatch(new PathName(e.Layer.Name, fileNameForMessages).AsString(), "^" + pathNamePattern + "$", RegexOptions.IgnoreCase);

    public static PathName? AsPathReference(this string text, string pathNamePattern, string fileNameForMessages) {
        Match m = Regex.Match(text, pathNamePattern, RegexOptions.IgnoreCase);
        return m.Success ? new PathName(m.Value, fileNameForMessages) : null;
    }

    public static Linetype GetLinetype(this EntityObject e, Dictionary<string, Linetype> layerLinetypes)
        => e.Linetype.IsByLayer ? layerLinetypes[e.Layer.Name] : e.Linetype;

    [ExcludeFromCodeCoverage]
    private static void Dump(string dxfFilePath, DrawingEntities d, Dictionary<string, Linetype> layerLinetypes, string pathNamePattern) {
        Console.WriteLine(MessageHandler.InfoPrefix + $"DUMP {dxfFilePath}");
        foreach (var e in d.All.Where(e => e.IsOnPathLayer(pathNamePattern, dxfFilePath))) {
            Console.WriteLine(e.ToLongString(layerLinetypes));
        }
        Console.WriteLine("----------------------------");
    }

    [ExcludeFromCodeCoverage]
    public static string ToLongString(this EntityObject e, Dictionary<string, Linetype> layerLinetypes)
        => $"{e.GetType().Name} LAYER={e.Layer.Name} CODENAME={e.CodeName} " +
           //$"TYPE={e.Type} COLOR={e.GetColor(layerColors)} LINETYPE={e.GetLinetype(layerLinetypes)}%{e.LinetypeScale} "
           $"TYPE={e.Type} LINETYPE={e.GetLinetype(layerLinetypes)}%{e.LinetypeScale} "
        + e switch {
            Line line => $"From={line.StartPoint.F3()} To={line.EndPoint.F3()} <{line.Lineweight},{line.Thickness.F3()}>",
            MLine mline => $"Points={string.Join(", ", mline.Vertexes)}",
            Circle circle => $"Center={circle.Center.F3()} Dia={(2 * circle.Radius).F3()}",
            Arc arc => $"Center={arc.Center.F3()} Dia={(2 * arc.Radius).F3()} Start={arc.StartAngle.F3()}° End={arc.EndAngle.F3()}°",
            //Insert ins => $"Pos={ins.Position.F3()} Rot={ins.Rotation.F3()} Sca={ins.Scale.F3()}",
            Text text => $"Pos={text.Position.F3()}  Rot={text.Rotation.F3()}  Hght={text.Height.F3()}  Wdth={text.Width.F3()} '{text.Value}'",
            MText mtext => $"Pos={mtext.Position.F3()}  Rot={mtext.Rotation.F3()}  Hght={mtext.Height.F3()}  '{mtext.Value}'",
            _ => "UNSUPPORTED"
        };
}
