namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using System.Diagnostics;
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
    /// /p: Refexp for paths in DXF filenames and DXF texts. 
    /// Underscores (_) in path names read from the DXF file are replaced with dots
    /// (which I usually use in path names).
    /// Groups are used for sorting.
    /// </summary>
    public string PathNamePattern { get; private set; } = "([0-9]{4})(?:[.]([0-9]+[A-Z]))?";

    /// <summary>
    /// /dump: Flag for developer dumps
    /// </summary>
    public bool Dump { get; private set; }

    public IEnumerable<string> DxfFilePaths => _dxfFilePaths;

    public static void Usage(MessageHandlerForEntities messages) {
        messages.WriteLine(MessageHandler.InfoPrefix + Messages.Options_Help);
    }

    public static Options? Create(string[] args, MessageHandlerForEntities messages) {
        bool doNotRun = false;

        string GetStringOption(ref int i) {
            return AbstractOptions.GetStringOption(args, ref i, Messages.Options_MissingValue_Name);
        }

        double GetDoubleOption(ref int i) {
            string a = args[i][2..];
            string v = GetStringOption(ref i).Replace(',', '.');
            double result;
            try {
                result = double.Parse(v, CultureInfo.InvariantCulture);
            } catch (FormatException) {
                throw FormatException(Messages.Options_NaN_Name_Value, v, a);
            }
            return result >= 0 ? result : throw FormatException(Messages.Options_LessThan0_Name_Value, v, a);
        }

        Options options = new();
        for (int i = 0; i < args.Length; i++) {
            string a = args[i];
            try {
                if (a.StartsWith('/') || a.StartsWith('-')) {
                    if (a.Length == 1) {
                        doNotRun = true;
                        messages.AddError("Options", Messages.Options_MissingOptionAfter_Name, a);
                    } else if (a[1..] == "debug") {
                        Debugger.Launch();
                    } else if (a[1..] == "dump") {
                        options.Dump = true;
                    } else {
                        switch (a.Substring(1, 1).ToLowerInvariant()) {
                            case "h":
                            case "?":
                                doNotRun = true;
                                break;
                            case "d":
                                options._searchDirectories.Add(GetStringOption(ref i));
                                break;
                            case "c":
                                options.CheckModels = true;
                                break;
                            case "x":
                                options.ShowTextAssignments = new Regex(GetStringOption(ref i));
                                break;
                            case "p":
                                options.PathNamePattern = GetStringOption(ref i);
                                break;
                            case "f":
                                options.GlobalFeedRate_mmpmin = GetDoubleOption(ref i);
                                break;
                            case "z":
                                options.GlobalProbeRate_mmpmin = GetDoubleOption(ref i);
                                break;
                            case "v":
                                options.GlobalSweepRate_mmpmin = GetDoubleOption(ref i);
                                break;
                            case "s":
                                options.GlobalSweepHeight_mm = GetDoubleOption(ref i);
                                break;
                            case "l":
                                Thread.CurrentThread.CurrentUICulture = new CultureInfo(GetStringOption(ref i));
                                break;
                            default:
                                doNotRun = true;
                                messages.AddError("Options", Messages.Options_NotSupported_Name, a);
                                break;
                        }
                    }
                } else {
                    options._dxfFilePaths.Add(a);
                }
            } catch (Exception ex) {
                doNotRun = true;
                messages.AddError("Options", ex.Message);
            }
        }

        if (options.GlobalFeedRate_mmpmin <= 0) {
            messages.AddError("Options", Messages.Options_MissingF);
            doNotRun = true;
        }
        if (options.GlobalSweepRate_mmpmin <= 0) {
            messages.AddError("Options", Messages.Options_MissingV);
            doNotRun = true;
        }
        if (options.GlobalSweepHeight_mm <= 0) {
            messages.AddError("Options", Messages.Options_MissingS);
            doNotRun = true;
        }
        if (options.GlobalProbeRate_mmpmin <= 0) {
            options.GlobalProbeRate_mmpmin = options.GlobalFeedRate_mmpmin;
        }

        if (doNotRun) {
            messages.WriteLine();
            return null;
        } else {
            return options;
        }
    }
}
