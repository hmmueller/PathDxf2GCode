namespace de.hmmueller.PathDxf2GCode;

using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

public class Options {
    /// <summary>
    /// /d: Suchverzeichnisse für referenzierte DXF-Dateien
    /// </summary>
    private readonly List<string> _searchDirectories = new();

    /// <summary>
    /// Zu verarbeitende DXF-Dateien
    /// </summary>    
    private readonly List<string> _dxfFilePaths = new();

    /// <summary>
    /// /f: Fräsgeschwindigkeit für G01, G02, G03
    /// TODO: https://diymachining.com/grbl-feed-rate/ 
    /// </summary>
    public double GlobalFeedRate_mmpmin { get; private set; } = -1;

    /// <summary>
    /// /v: Leerfahrtengeschwindigkeit für G00
    /// TODO: https://diymachining.com/grbl-feed-rate/ 
    /// </summary>
    public double GlobalSweepRate_mmpmin { get; private set; } = -1;

    /// <summary>
    /// /c: Prüflauf für alle Modelle in einer DXF-Datei, keine G-Code-Ausgabe
    /// </summary>
    public bool CheckModels { get; private set; } = false;

    /// <summary>
    /// /t: Gibt für alle auf diese Regex passenden Texte aus, welchem DXF-Objekt sie zugeordnet sind
    /// </summary>
    public Regex? ShowTextAssignments { get; private set; } = null;

    /// <summary>
    /// Verzeichnis einer Inputdatei und dann alle Suchverzeichnisse für DXF-Dateien
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
    /// /p: Pattern für Pfade in DXF-Dateinamen und DXF-Texten. 
    /// Die einzelnen Gruppen werden für Vergleiche (sowohl von-bis wie auch
    /// Gleichheit) als Stringwert verglichen.
    /// Underscore (_) in Pfadnamen aus der DXF-Datei werden vorher durch . ersetzt.
    /// </summary>
    public string PathNamePattern { get; private set; } = "([0-9]{4})(?:[.]([0-9]+[A-Z]))?";

    /// <summary>
    /// /dump: Flag für Entwicklerausgaben
    /// </summary>
    public bool Dump { get; private set; }

    public IEnumerable<string> DxfFilePaths => _dxfFilePaths;

    public static void Usage(MessageHandler messages) {
        messages.WriteLine("""
            Aufruf: PathDxf2GCode [Parameter] [DXF-Dateien]

            Parameter:
                /h     Hilfe-Anzeige
                /f 000 Fräsgeschwindigkeit in mm/min; Pflichtwert
                /v 000 Maximalgeschwindigkeit für Leerfahrten in mm/min; Pflichtwert
                /c     Überprüfen aller Pfade in der DXF-Datei ohne G-Code-Ausgabe; wenn /c nicht 
                       angegeben wird, dann darf die DXF-Datei nur einen Pfad enthalten
                /t zzz Gibt für alle auf diese Regex passenden Texte aus, welchem DXF-Objekt 
                       sie zugeordnet sind
                /d zzz Suchpfad für referenzierte DXF-Dateien
                /p zzz Reg.Ausdruck für Pfadbezeichnungen
            """);
    }

    public static Options? Create(string[] args, MessageHandler messages) {
        bool doNotRun = false;

        string GetStringOption(ref int i) {
            return args[i].Length > 2 ? args[i][2..] : i < args.Length - 1 ? args[++i] : throw new FormatException($"**** Fehlender Parameterwert für {args[i - 1]}");
        }

        double GetDoubleOption(ref int i) {
            string a = args[i][2..];
            string v = GetStringOption(ref i).Replace(',', '.');
            double result;
            try {
                result = double.Parse(v, CultureInfo.InvariantCulture);
            } catch (FormatException) {
                throw new FormatException($"**** Parameterwert {v} für {a} ist keine Zahl");
            }
            return result >= 0 ? result : throw new FormatException($"**** Parameterwert {v} für {a} ist nicht >= 0");
        }

        Options options = new();
        for (int i = 0; i < args.Length; i++) {
            string a = args[i];
            try {
                if (a.StartsWith('/') || a.StartsWith('-')) {
                    if (a.Length == 1) {
                        doNotRun = true;
                        messages.AddError("Options", $"Fehlende Option nach {a}");
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
                            case "t":
                                options.ShowTextAssignments = new Regex(GetStringOption(ref i));
                                break;
                            case "p":
                                options.PathNamePattern = GetStringOption(ref i);
                                break;
                            case "f":
                                options.GlobalFeedRate_mmpmin = GetDoubleOption(ref i);
                                break;
                            case "v":
                                options.GlobalSweepRate_mmpmin = GetDoubleOption(ref i);
                                break;
                            default:
                                doNotRun = true;
                                messages.AddError("Options", $"Option {a} nicht unterstützt");
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
            messages.AddError("Options", "/f nicht angegeben oder nicht größer als 0");
            doNotRun = true;
        }
        if (options.GlobalSweepRate_mmpmin <= 0) {
            messages.AddError("Options", "/v nicht angegeben oder nicht größer als 0");
            doNotRun = true;
        }

        if (doNotRun) {
            messages.WriteLine();
            return null;
        } else {
            return options;
        }
    }
}
