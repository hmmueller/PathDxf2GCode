namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using System.Globalization;
using System.Text.RegularExpressions;

public class Options : AbstractOptions {
    /// <summary>
    /// /d: search directories for DXF files
    /// </summary>
    private readonly List<string> _searchDirectories = new();

    /// <summary>
    /// Input DXF files
    /// </summary>    
    private readonly List<string> _dxfFilePaths = new();

    /// <summary>
    /// /f: Milling speed für G01, G02, G03
    /// TODO: https://diymachining.com/grbl-feed-rate/ 
    /// </summary>
    public double GlobalFeedRate_mmpmin { get; private set; } = -1;

    /// <summary>
    /// /v: Sweep speed for G00 (only necessary for statistics computations)
    /// TODO: https://diymachining.com/grbl-feed-rate/ 
    /// </summary>
    public double GlobalSweepRate_mmpmin { get; private set; } = -1;

    /// <summary>
    /// /z: Probe rate for G38
    /// </summary>
    public double GlobalProbeRate_mmpmin { get; private set; } = -1;

    /// <summary>
    /// /s: Sweep height for main path; must be higher than any obstacle
    /// the router bits might encounter.
    /// </summary>
    public double GlobalSweepHeight_mm { get; private set; } = -1;

    /// <summary>
    /// /c: Dry run for all paths of a DXF file, no gcode output
    /// </summary>
    public bool CheckModels { get; private set; } = false;

    /// <summary>
    /// /t: Write all texts matching this regexp; and the DXF objects to which they are assigned
    /// </summary>
    public Regex? ShowTextAssignments { get; private set; } = null;

    /// <summary>
    /// Directory of input file, and then all search directories for DXF files
    /// </summary>
    /// <param name="dir"></param>
    /// <returns></returns>
    public IEnumerable<string> DirAndSearchDirectories(string? dir) {
        if (dir != null) {
            yield return dir;
        }
        foreach (var d in _searchDirectories) {
            yield return d;
        }
    }

    /// <summary>
    /// /n: Regexp for paths in DXF texts. 
    /// Underscores (_) in path names read from the DXF file are replaced with dots
    /// (which I usually use in path names). Groups are used for sorting.
    /// Default pattern is ([0-9]{4})[.]([0-9]+)([A-Z]), with three groups.
    /// </summary>
    public string PathNamePattern { get; private set; } = "([0-9]{4})[.]([0-9]+)([A-Z])";

    /// <summary>
    /// /p: Regexp for paths in DXF filenames.
    /// Underscores (_) in path names read from the DXF file are replaced with dots
    /// (which I usually use in path names). Groups are used for comparing with path names.
    /// Default pattern is ([0-9]{4})(?:[.]([0-9]+))?, with one or two groups.
    /// </summary>
    public string PathFilePattern { get; private set; } = "([0-9]{4})(?:[.]([0-9]+))?";

    /// <summary>
    /// /dump: Flag for developer dumps
    /// </summary>
    public bool Dump { get; private set; }

    public IEnumerable<string> DxfFilePaths => _dxfFilePaths;

    public static void Usage(MessageHandlerForEntities messages) {
        messages.WriteLine(MessageHandler.InfoPrefix + Messages.Options_Help);
    }

    public static Options? Create(string[] args, MessageHandlerForEntities messages) {
        Options options = new();

        return FillOptions(args, options, messages,
            missingOptionAfter: a => messages.AddError("Options", Messages.Options_MissingOptionAfter_Name, a),
            unsupportedOption: a => messages.AddError("Options", Messages.Options_NotSupported_Name, a),
            handleOption: HandleOption,
            handleArgument: HandleArgument,
            checkOptions: CheckOptions) ? options : null;
    }

    private static bool HandleOption(string opt, string[] args, ref int i, Options options, MessageHandler messages) {
        string GetStringOption(ref int i) {
            return AbstractOptions.GetStringOption(args, ref i, Messages.Options_MissingValue_Name);
        }

        double GetDoubleOption(ref int i) {
            return AbstractOptions.GetDoubleOption(args, ref i, Messages.Options_MissingOptionAfter_Name,
                                                   Messages.Options_NaN_Name_Value, Messages.Options_LessThan0_Name_Value);
        }

        switch (opt) {
            case "d":
                options._searchDirectories.Add(GetStringOption(ref i));
                return true;
            case "c":
                options.CheckModels = true;
                return true;
            case "x":
                options.ShowTextAssignments = new Regex(GetStringOption(ref i));
                return true;
            case "n":
                options.PathNamePattern = GetStringOption(ref i);
                return true;
            case "p":
                options.PathFilePattern = GetStringOption(ref i);
                return true;
            case "f":
                options.GlobalFeedRate_mmpmin = GetDoubleOption(ref i);
                return true;
            case "z":
                options.GlobalProbeRate_mmpmin = GetDoubleOption(ref i);
                return true;
            case "v":
                options.GlobalSweepRate_mmpmin = GetDoubleOption(ref i);
                return true;
            case "s":
                options.GlobalSweepHeight_mm = GetDoubleOption(ref i);
                return true;
            case "l":
                Thread.CurrentThread.CurrentUICulture = new CultureInfo(GetStringOption(ref i));
                return true;
            default:
                return false;
        }
    }

    private static void HandleArgument(string a, Options options, MessageHandler messages) {
        options._dxfFilePaths.Add(a);
    }

    private static bool CheckOptions(Options options, MessageHandler messages) {
        bool result = true;
        if (options.GlobalFeedRate_mmpmin <= 0) {
            messages.AddError("Options", Messages.Options_MissingF);
            result = false;
        }
        if (options.GlobalSweepRate_mmpmin <= 0) {
            messages.AddError("Options", Messages.Options_MissingV);
            result = false;
        }
        if (options.GlobalSweepHeight_mm <= 0) {
            messages.AddError("Options", Messages.Options_MissingS);
            result = false;
        }
        if (options.GlobalProbeRate_mmpmin <= 0) {
            options.GlobalProbeRate_mmpmin = options.GlobalFeedRate_mmpmin;
        }
        return result;
    }
}
