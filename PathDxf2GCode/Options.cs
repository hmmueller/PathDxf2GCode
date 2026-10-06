namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using System.Globalization;
using System.Text.RegularExpressions;

public class Options : AbstractOptions, IEmitParams {
    /// <summary>
    /// /d: search directories for DXF files
    /// </summary>
    private readonly List<string> _searchDirectories = new();

    /// <summary>
    /// Input DXF files
    /// </summary>    
    private readonly List<string> _dxfFilePaths = new();

    /// <summary>
    /// Restrictions on allowed subpaths.
    /// </summary>    
    private readonly List<(Regex Parent, Regex Child)> _subPathRestrictions = new();

    /// <summary>
    /// /f: Default milling speed (feed rate) for G01, G02, G03 in XY direction
    /// TODO: https://diymachining.com/grbl-feed-rate/ 
    /// </summary>
    public double? RawF_mmpmin { get; private set; }

    /// <summary>
    /// /j: Default maximum ramp angle
    /// </summary>
    public double? RawJ_deg { get; private set; }

    /// <summary>
    /// /g: Default milling speed for G01 in Z direction
    /// </summary>
    public double? RawG_mmpmin { get; private set; }

    /// <summary>
    /// /i: Default infeed
    /// </summary>
    public double? RawI_mm { get; private set; }

    /// <summary>
    /// /v: Sweep speed for G00 (only necessary for statistics computations)
    /// TODO: https://diymachining.com/grbl-feed-rate/ 
    /// </summary>
    public double GlobalSweepRate_mmpmin { get; private set; }

    /// <summary>
    /// /z: Default probe rate for G38
    /// </summary>
    public double? RawZ_mmpmin { get; private set; }

    /// <summary>
    /// /s: Default sweep height for main path; must be higher than any obstacle
    /// the router bits might encounter.
    /// </summary>
    public double S_mm { get; private set; }
    public double? RawS_mm => S_mm;

    /// <summary>
    /// /y: Default clamp height for local paths.
    /// </summary>
    public double? RawY_mm { get; private set; }

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
    /// /e: Check whether the child subpath matches the specified regex for the first parent subpath regex found in 
    /// the list of subpath restrictions. If no parent subpath regex matches, the restriction is considered to be ok.
    /// </summary>
    public bool SubPathRestrictionOk(string parent, string child)
        => _subPathRestrictions.FirstOrDefault(pc => pc.Parent.IsMatch(parent)).Child?.IsMatch(child) ?? true;

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
            case "e":
                string e = GetStringOption(ref i);
                string[] e2 = e.Split(':', 2);
                if (e2.Length != 2) {
                    messages.AddError("Options", Messages.Options_NotTwoPartsInSubPathRestriction_Value, e);
                } else {
                    try {
                        options._subPathRestrictions.Add((new Regex(e2[0]), new Regex(e2[1])));
                    } catch (ArgumentException ex) {
                        messages.AddError("Options", Messages.Options_InvalidRegexInSubPathRestriction_Value_Message, e, ex.Message);
                    }
                }
                return true;
            case "f":
                options.RawF_mmpmin = GetDoubleOption(ref i);
                return true;
            case "g":
                options.RawG_mmpmin = GetDoubleOption(ref i);
                return true;
            case "j":
                options.RawJ_deg = GetDoubleOption(ref i);
                return true;
            case "i":
                options.RawI_mm = GetDoubleOption(ref i);
                return true;
            case "z":
                options.RawZ_mmpmin = GetDoubleOption(ref i);
                return true;
            case "v":
                options.GlobalSweepRate_mmpmin = GetDoubleOption(ref i);
                return true;
            case "y":
                options.RawY_mm = GetDoubleOption(ref i);
                return true;
            case "s":
                options.S_mm = GetDoubleOption(ref i);
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
        if (options.S_mm <= 0) {
            messages.AddError("Options", Messages.Options_MissingS);
            result = false;
        }
        if (options.RawJ_deg <= 0 || options.RawJ_deg > 90) {
            messages.AddError("Options", Messages.Options_JNotBetween0And90);
            result = false;
        }
        return result;
    }
}
