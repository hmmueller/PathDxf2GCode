namespace de.hmmueller.PathDxf2GCode;

using de.hmmueller.PathGCodeLibrary;
using netDxf;

public class Program {
    public const string VERSION = "2026-02-09";

    public static int Main(string[] args) {
        var messages = new MessageHandlerForEntities(Console.Error);

        messages.WriteLine(MessageHandler.InfoPrefix + "PathDxf2GCode (c) HMMüller 2024-2026 V.{0}", VERSION);
        messages.WriteLine();

        Options? options = Options.Create(args, messages);

        if (options == null) {
            messages.WriteErrors();
            Options.Usage(messages);
            return 2;
        } else if (!options.DxfFilePaths.Any()) {
            messages.WriteErrors();
            messages.WriteLine(MessageHandler.ErrorPrefix + Messages.Program_NoDxfFiles);
            return 3;
        } else {
            PathModel.Collection pathModels = new();

            foreach (var dxfFilePath in options.DxfFilePaths) {
                try {
                    GenerateGCode(dxfFilePath, pathModels, messages, options);
                } catch (EmitGCodeException ex) {
                    messages.AddError(ex.ErrorContext, ex.Message);
                } catch (Exception ex) {
                    messages.AddError(dxfFilePath, ex.Message);
                }
            }

            return messages.WriteErrors() ? 1 : 0;
        }
    }

    private static void Generate(string outputPath, MessageHandlerForEntities messages, Action<StreamWriter> write) {
        messages.WriteLine(MessageHandler.InfoPrefix + Messages.Program_Writing_Path, outputPath);

        using (StreamWriter sw = new(outputPath)) {
            write(sw);
        }
    }

    private static readonly IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> NO_ZPROBES = 
                                        Enumerable.Empty<(ZProbe ZProbe, Vector2 TransformedCenter)>();

    private static void GenerateGCode(string dxfFilePath, PathModel.Collection pathModels, MessageHandlerForEntities messages, Options options) {
        if (!dxfFilePath.EndsWith(".dxf", StringComparison.CurrentCultureIgnoreCase)) {
            dxfFilePath += ".dxf";
        }

        if (options.CheckModels) {
            SortedDictionary<string, PathModel> models = pathModels.LoadAllModels(dxfFilePath, globalSweepHeight_mm: null,
                paramsText => new FormalVariables(paramsText.VariableStrings).Example(msg => messages.AddError(dxfFilePath, msg)), 
                options, messages, nestingDepth: 0);
            foreach (var m in models) {
                messages.WriteLine(MessageHandler.InfoPrefix + Messages.Program_Checking_Path, m.Key);
                m.Value.CollectAndOrderAllZProbes(); // ZProbes need names in Transformation3, that's how they get them.
                using (StreamWriter sw = StreamWriter.Null) {
                    WriteMillingGCode(m.Value, NO_ZPROBES, sw, dxfFilePath, messages);
                }
            }
        } else {
            SortedDictionary<string, PathModel> models = pathModels.LoadAllModels(dxfFilePath,
                options.GlobalSweepHeight_mm, paramsText => ActualVariables.EMPTY, options, messages, nestingDepth: 0);
            if (models.Count > 1) {
                messages.AddError(dxfFilePath, Messages.Program_MoreThanOnePathLayer_File_Paths, dxfFilePath, string.Join(", ", models.Keys));
            } else if (models.Count == 0) {
                messages.AddError(dxfFilePath, Messages.Program_NoErrorFreePathLayer_File, dxfFilePath);
            } else {
                if (!messages.Errors.Any()) {
                    PathModel model = models.Single().Value;
                    string outFilePathPrefix = dxfFilePath[..^4] + model.Params.OutFileSuffix;
                    List<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes = model.CollectAndOrderAllZProbes();
                    if (orderedZProbes.Any()) {
                        Generate(outFilePathPrefix + "_Probing.gcode", messages, sw => WriteZProbingGCode(model, orderedZProbes, sw, dxfFilePath, messages));
                        Generate(outFilePathPrefix + "_Z.txt", messages, sw => WriteEmptyZ(orderedZProbes, sw, messages));
                        Generate(outFilePathPrefix + "_Clean.gcode", messages, sw => WriteMillingGCode(model, orderedZProbes, sw, dxfFilePath, messages));
                        // PathGCodeAdjustZ: _Clean.gcode + _Z.txt(man.) => _Milling.gcode
                    } else {
                        Generate(outFilePathPrefix + "_Milling.gcode", messages, sw => WriteMillingGCode(model, NO_ZPROBES, sw, dxfFilePath, messages));
                    }
                }
            }
        }
    }

    private static void WriteMillingGCode(PathModel m, IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes, StreamWriter sw, string dxfFilePath, MessageHandlerForEntities messages) {
        if (m.IsEmpty()) {
            messages.AddError(dxfFilePath, Messages.Program_NoSegmentsFound);
        } else {

            if (!messages.Errors.Any()) {
                // Z is a little bit different from S so that the first segment will definitely
                // emit a Z sweep - which is important because that Z sweep may include a Z adjustment
                // that would be missed otherwise.
                Vector3 init = new(0, 0, m.Params.S_mm * (1 + 2 * GeometryHelpers.RELATIVE_EPS));
                List<GCode> gcodes = new();

                Vector3 currpos = m.EmitMillingGCode(init, m.CreateTransformation(orderedZProbes), m.Params.S_mm, gcodes, dxfFilePath, messages);

                gcodes.AddNonhorizontalG00($"G00 Z{init.Z.F3()}", Math.Abs(currpos.Z - init.Z));

                gcodes = gcodes.Optimize();

                WritePrologue(init, sw, dxfFilePath);
                sw.WriteLine($"Model {m.Name}".AsComment(2));
                WriteGCodes(gcodes, sw);

                // See http://www.linuxcnc.org/docs/html/gcode/overview.html#_g_code_best_practices
                Statistics stats = new(m.Params.V_mmpmin);
                foreach (var g in gcodes) {
                    g.AddToStatistics(stats);
                }

                static string AsMin(TimeSpan t) => $"ca.{Math.Ceiling(t.TotalMinutes),3:F0}";
                void WriteStat(string s, string name) {
                    string m = string.Format(s, name);
                    messages.Write(m + ";");
                    sw.WriteLine(m.AsComment(2));
                }

                WriteStat($"  {{0,-12}} {stats.MillLength_mm,5:F0} mm   {AsMin(stats.RoughMillTime)} min", Messages.Program_MillingLength);
                WriteStat($"  {{0,-12}} {stats.DrillLength_mm,5:F0} mm   {AsMin(stats.RoughDrillTime)} min", Messages.Program_DrillingLength);
                messages.WriteLine();
                WriteStat($"  {{0,-12}} {stats.SweepLength_mm,5:F0} mm   {AsMin(stats.RoughSweepTime)} min", Messages.Program_SweepLength);
                WriteStat($"  {{0,-12}} {stats.TotalLength_mm,5:F0} mm   {AsMin(stats.TotalTime)} min", Messages.Program_SumLength);
                WriteStat($"  {{0,-12}} {stats.CommandCount}", Messages.Program_CommandCount);
                messages.WriteLine();

                WriteEpilogue(sw);
            }
        }
    }

    private static void WriteGCodes(List<GCode> gcodes, StreamWriter sw) {
        foreach (var g in gcodes) {
            sw.WriteLine(g.AsString());
        }
    }

    private static void WritePrologue(Vector3 init, StreamWriter sw, string dxfFilePath) {
        sw.WriteLine("%");
        sw.WriteLine($"PathDxf2GCode - HMMüller 2024-2025 V.{VERSION}".AsComment(0));

        // UGS does not like the following command, therefore I removed it.
        //sw.WriteLine($"O{Path.GetFileNameWithoutExtension(dxfFilePath)} {dxfFilePath.AsComment(0)}");

        sw.WriteLine(Path.GetFileName(dxfFilePath).AsComment(0));

        sw.WriteLine("F150"); // initial feed rate 150 mm/min - GRBL/µCNC wants this before the G-Commands
                              // G17 use XY plane, G21 mm mode, G40 cancel diameter compensation,
                              // G49 cancel length offset, G54 use coordinate system 1,
                              // G80 cancel canned cycles, G90 absolute distance mode, G94 feed/ minute mode.
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
    
    private static void WriteZProbingGCode(PathModel m, IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes, StreamWriter sw, string dxfFilePath, MessageHandlerForEntities messages) {
        if (!messages.Errors.Any()) {
            // See http://www.linuxcnc.org/docs/html/gcode/overview.html#_g_code_best_practices
            Vector3 init = new(0, 0, m.Params.S_mm);

            List<GCode> gcodes = new();
            Vector3 currpos = EmitZProbingGCode(init, orderedZProbes, m.Params.S_mm, gcodes, dxfFilePath, messages);
            GCodeHelpers.SweepFromTo(currpos, init, m.Params.S_mm, gcodes);
            gcodes.AddNonhorizontalG00($"G00 Z{init.Z.F3()}", Math.Abs(m.Params.S_mm - init.Z));

            WritePrologue(init, sw, dxfFilePath);
            WriteGCodes(gcodes, sw);
            WriteEpilogue(sw);
        }
    }

    private static Vector3 EmitZProbingGCode(Vector3 currPos, IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes, double globalS_mm, List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        double sweepHeight = currPos.Z;
        foreach (var zc in orderedZProbes) {
            currPos = GCodeHelpers.SweepFromTo(currPos, zc.TransformedCenter.AsVector3(sweepHeight), globalS_mm, gcodes);
            try {
                currPos = zc.ZProbe.EmitGCode(currPos, zc.TransformedCenter, gcodes, dxfFileName, messages);
                if (!currPos.Z.Near(sweepHeight)) {
                    throw new Exception($"Internal Error - {currPos.Z} <> {sweepHeight}");
                }
            } catch (EmitGCodeException ex) {
                messages.AddError(ex.ErrorContext, ex.Message);
            }
        }
        return currPos;
    }

    private static void WriteEmptyZ(IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes, StreamWriter sw, MessageHandlerForEntities messages) {
        if (!messages.Errors.Any()) {
            foreach (var zc in orderedZProbes) {
                ZProbe z = zc.ZProbe;
                // Line example - separators are # and =:
                // ([77.038 191.859]/L:ZA/T=5.000) #51=
                // <        0             > <  1  > <> < 3 >
                sw.WriteLine((z.Center.F3() + (z.L == null ? "" : "/L:" + z.L) + "/T=" + z.T_mm.F3()).AsComment(0) + " " + z.Name + "=");
            }
        }
    }
}