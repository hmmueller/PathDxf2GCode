namespace de.hmmueller.PathDxf2GCode;

using netDxf;

public class Program {
    public const string VERSION = "2025-03-08";

    public static int Main(string[] args) {
        var messages = new MessageHandler(Console.Error);

        messages.WriteLine($"---- PathDxf2GCode (c) HMMüller 2024-2025 V.{VERSION}");
        messages.WriteLine();

        Options? options = Options.Create(args, messages);

        if (options == null) {
            Options.Usage(messages);
            return 2;
        } else if (!options.DxfFilePaths.Any()) {
            messages.WriteLine($"**** Keine DXF-Dateien angegeben");
            return 3;
        } else {
            PathModelCollection pathModels = new();

            foreach (var dxfFilePath in options.DxfFilePaths) {
                try {
                    GenerateGCode(dxfFilePath, pathModels, messages, options);
                } catch (EmitGCodeException ex) {
                    messages.AddError(ex.ErrorContext, ex.Message);
                } catch (Exception ex) {
                    messages.AddError(dxfFilePath, ex.Message);
                }
            }

            if (messages.Errors.Any()) {
                messages.WriteLine("Fehler:");
                foreach (var e in messages.Errors) {
                    messages.WriteLine($"**** {e}");
                }
                return 1;
            } else {
                return 0;
            }
        }
    }

    private static void Generate(string outputPath, MessageHandler messages, Action<StreamWriter> write) {
        messages.WriteLine($"---- Schreiben von {outputPath}");

        using (StreamWriter sw = new(outputPath)) {
            write(sw);
        }
    }

    private static void GenerateGCode(string dxfFilePath, PathModelCollection pathModels, MessageHandler messages, Options options) {
        if (!dxfFilePath.EndsWith(".dxf", StringComparison.CurrentCultureIgnoreCase)) {
            dxfFilePath += ".dxf";
        }

        var models = pathModels.Load(dxfFilePath, options, dxfFilePath, messages);
        if (options.CheckModels) {
            foreach (var m in models) {
                messages.WriteLine($"Überprüfung von {m.Key}");
                using (StreamWriter sw = StreamWriter.Null) {
                    WriteMillingGCode(m.Value, sw, dxfFilePath, messages);
                }
            }
        } else {
            if (models.Count > 1) {
                messages.AddError(dxfFilePath, $"DXF-Datei {dxfFilePath} enthält mehr als einen CNC-Pfad-LayerName: {string.Join(", ", models.Keys)}");
            } else if (models.Count == 0) {
                messages.AddError(dxfFilePath, $"DXF-Datei {dxfFilePath} enthält keinen CNC-Pfad-LayerName ohne Fehler");
            } else {
                if (!messages.Errors.Any()) {
                    PathModel model = models.Single().Value;
                    if (model.HasZProbes) {
                        Generate(dxfFilePath[..^4] + "_Probing.gcode", messages, sw => WriteZProbingGCode(model, sw, dxfFilePath, messages));
                        Generate(dxfFilePath[..^4] + "_Z.txt", messages, sw => WriteEmptyZ(model, sw, dxfFilePath, messages));
                        Generate(dxfFilePath[..^4] + "_Clean.gcode", messages, sw => WriteMillingGCode(model, sw, dxfFilePath, messages));
                        // PathGCodeAdjustZ: _Clean.gcode + _Z.txt(man.) => _Milling.gcode
                    } else {
                        Generate(dxfFilePath[..^4] + "_Milling.gcode", messages, sw => WriteMillingGCode(model, sw, dxfFilePath, messages));
                    }
                }
            }
        }
    }

    private static void WriteMillingGCode(PathModel m, StreamWriter sw, string dxfFilePath, MessageHandler messages) {
        if (m.IsEmpty()) {
            messages.AddError(dxfFilePath, "Keine Segmente gefunden");
        } else {

            if (!messages.Errors.Any()) {
                // See http://www.linuxcnc.org/docs/html/gcode/overview.html#_g_code_best_practices
                Statistics stats = new(m.Params.V_mmpmin);

                Vector3 init = new(0, 0, m.Params.S_mm);
                WritePrologue(init, sw, dxfFilePath);
                sw.WriteLine($"Model {m.Name}".AsComment(2));
                Vector3 currpos = m.EmitMillingGCode(init, m.CreateTransformation(), sw, stats, dxfFilePath, messages);

                sw.WriteLine($"G00 Z{init.Z.F3()}");
                stats.AddSweepLength(currpos.Z, init.Z);

                static string AsMin(TimeSpan t) => $"ca.{Math.Ceiling(t.TotalMinutes),3:F0}";
                void WriteStat(string s) {
                    messages.Write(s + ";");
                    sw.WriteLine(s.AsComment(2));
                }

                WriteStat($"  Fräslänge:   {stats.MillLength_mm,5:F0} mm   {AsMin(stats.RoughMillTime)} min");
                WriteStat($"  Bohrungen:   {stats.DrillLength_mm,5:F0} mm   {AsMin(stats.RoughDrillTime)} min");
                WriteStat($"  Leerfahrten: {stats.SweepLength_mm,5:F0} mm   {AsMin(stats.RoughSweepTime)} min");
                messages.WriteLine();
                WriteStat($"  Summe:       {stats.TotalLength_mm,5:F0} mm   {AsMin(stats.TotalTime)} min");
                WriteStat($"  Befehlszahl: {stats.CommandCount}");
                messages.WriteLine();

                WriteEpilogue(sw);
            }
        }
    }

    private static void WritePrologue(Vector3 init, StreamWriter sw, string dxfFilePath) {
        sw.WriteLine("%");
        sw.WriteLine($"PathDxf2GCode - HMMüller 2024-2025 V.{VERSION}".AsComment(0));

        // UGS mag das folgende O-Command nicht, daher weggelassen.
        //sw.WriteLine($"O{Path.GetFileNameWithoutExtension(dxfFilePath)} {dxfFilePath.AsComment(0)}");
        sw.WriteLine(dxfFilePath.AsComment(0));

        sw.WriteLine("F150"); // initial feed rate 150 mm/min - GRBL/µCNC will das vor den G-Commands
                              // G17 use XY plane, G21 mm mode, G40 cancel diameter compensation, G49 cancel length offset, G54 use
                              // coordinate system 1, G80 cancel canned cycles, G90 absolute distance mode, G94 feed/ minute mode.
        sw.WriteLine("G17 G21 G40 G49 G54 G80 G90 G94");

        //sw.WriteLine("M6 T1"); // select tool 1
        sw.WriteLine("T1"); // select tool 1; GRBL/µCNC mag kein M6

        sw.WriteLine($"SweepSafelyTo {init.F3()}".AsComment(0));
        sw.WriteLine($"G00 Z{init.Z.F3()}");
        sw.WriteLine($"G00 X{init.X.F3()} Y{init.Y.F3()}");
    }

    private static void WriteEpilogue(StreamWriter sw) {
        sw.WriteLine("M30");
        sw.WriteLine("%");
    }

    private static void WriteZProbingGCode(PathModel m, StreamWriter sw, string dxfFilePath, MessageHandler messages) {
        if (!messages.Errors.Any()) {
            // See http://www.linuxcnc.org/docs/html/gcode/overview.html#_g_code_best_practices
            Statistics _ = new(m.Params.V_mmpmin);
            Vector3 init = new(0, 0, m.Params.S_mm);
            WritePrologue(init, sw, dxfFilePath);

            Vector3 currpos = m.EmitZProbingGCode(init, sw, _, dxfFilePath, messages);
            GCodeHelpers.SweepFromTo(currpos, init, sw, _);

            sw.WriteLine($"G00 Z{init.Z.F3()}");

            WriteEpilogue(sw);
        }
    }

    private static void WriteEmptyZ(PathModel m, StreamWriter sw, string dxfFilePath, MessageHandler messages) {
        if (!messages.Errors.Any()) {
            m.WriteEmptyZ(sw);
        }
    }
}