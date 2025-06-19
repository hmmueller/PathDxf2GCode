namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Tables;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

public class PathModel {
    private class Circle2 {
        public Circle2(Vector2 center, double radius) {
            Center = center;
            Radius = radius;
        }
        public Vector2 Center { get; }
        public double Radius { get; }
    }

    public const int BACKTRACK_ORDER = 5555;
    private const int LAST_ORDER = 9998;

    public PathName Name { get; }
    public PathParams Params { get; }
    public Vector2 Start { get; }
    public Vector2 End { get; }
    public string DxfFilePath { get; }
    private readonly List<PathSegment> _segments;
    private readonly List<ZProbe> _zProbes;

    private PathModel(PathName name, PathParams pars, Vector2 start, Vector2 end, List<PathSegment> segments, List<ZProbe> zProbes, string dxfFilePath) {
        Name = name;
        Params = pars;
        Start = start;
        End = end;
        DxfFilePath = dxfFilePath;
        _segments = segments;
        _zProbes = zProbes;
    }

    private class RawPathModel {
        public Circle? StartObject;
        public Vector2? Start;
        public Vector2? End;

        public readonly PathName Name;
        public ParamsText? ParamsText;
        public readonly List<IRawSegment> RawSegments = new();
        public readonly List<ZProbe> ZProbes = new();

        public RawPathModel(PathName p) {
            Name = p;
        }
    }

    public class Collection {
        private readonly Dictionary<PathName, (RawPathModel RawModel, string DxfFilePath)> _rawModels = new();
        private readonly Dictionary<(PathName, Variables), PathModel> _models = new();

        public PathModel? Load(PathName name, ActualVariables variables, double? defaultSorNullForTplusO_mm, string currentDxfFile, Options options, string overlayTextForErrors, MessageHandlerForEntities messages, int nestingDepth, out string searchedFiles) {
            searchedFiles = "";
            if (!_models.ContainsKey((name, variables))) {
                (RawPathModel? rawModel, string? dxfFilePath) = LoadRawModel(name, Path.GetDirectoryName(Path.GetFullPath(currentDxfFile)), options, overlayTextForErrors, messages, out searchedFiles);
                if (rawModel != null) { // errors when rawModel==null were already registered
                    PathModel? m = CreatePathModel(name, rawModel, defaultSorNullForTplusO_mm: defaultSorNullForTplusO_mm, variables, dxfFilePath!, options, messages, nestingDepth);
                    if (m != null) { // errors when m==null were already registered
                        _models.Add((name, variables), m);
                    }
                }
            }
            return _models.TryGetValue((name, variables), out PathModel? result) ? result : null;
        }

        private (RawPathModel? rawModel, string? dxfFilePath) LoadRawModel(PathName name, string? currentDirectory, Options options, string overlayTextForErrors, MessageHandlerForEntities messages, out string searchedFiles) {
            searchedFiles = "";
            if (!_rawModels.ContainsKey(name)) {
                foreach (var directory in options.DirAndSearchDirectories(currentDirectory)) {
                    foreach (var f in Directory.GetFiles(directory, "*.dxf")) {
                        if (FileNameMatchesPathName(Path.GetFileNameWithoutExtension(f), name, options.PathFilePattern, options.PathNamePattern)) {
                            Dictionary<PathName, RawPathModel> newRawModels = LoadRawModels(f, options, messages);
                            foreach (var kvp in newRawModels) {
                                if (_rawModels.TryGetValue(kvp.Key, out var alreadyDefined)) {
                                    if (alreadyDefined.DxfFilePath != f) {
                                        messages.AddError(f, Messages.PathModel_PathDefinedTwice_Path_OtherFile, kvp.Key, alreadyDefined.DxfFilePath, f);
                                    }
                                } else {
                                    _rawModels.Add(kvp.Key, (kvp.Value, f));
                                }
                            }
                            searchedFiles += (searchedFiles == "" ? "" : ", ") + f;
                        }
                        // Continue loop, i.e. load all matching files! - we want to know whether
                        // the path might be defined more than once.
                    }
                }
            }
            return _rawModels.TryGetValue(name, out (RawPathModel RawModel, string DxfFilePath) result) ? result : (null, null);
        }


        public static bool FileNameMatchesPathName(string fileName, PathName path, string pathFilePattern, string pathNamePattern) {
            foreach (var m in Regex.Matches(fileName, $"(?<p1>{pathFilePattern})-(?<p2>{pathFilePattern})").ToArray()) {
                string from = m.Groups["p1"].Value;
                string to = m.Groups["p2"].Value;
                if (PathName.CompareFileNameToPathName(from, path, pathFilePattern, pathNamePattern) <= 0
                    && PathName.CompareFileNameToPathName(to, path, pathFilePattern, pathNamePattern) >= 0) {
                    return true;
                }
            }
            foreach (var m in Regex.Matches(fileName, pathFilePattern).ToArray()) {
                if (PathName.CompareFileNameToPathName(m.Value, path, pathFilePattern, pathNamePattern) == 0) {
                    return true;
                }
            }
            return false;
        }

        private Dictionary<PathName, RawPathModel> LoadRawModels(string dxfFilePath, Options options, MessageHandlerForEntities messages) {
            string fullDxfFilePath = Path.GetFullPath(dxfFilePath);

            var modelsInDxfFile = new SortedDictionary<string, PathModel>();
            DxfDocument? d = DxfHelper.LoadDxfDocument(fullDxfFilePath, options,
                                                     out Dictionary<string, Linetype> layerLinetypes, messages);
            return d == null ? new() : CollectSegments(d.Entities, layerLinetypes, this, dxfFilePath, options, messages);
        }

        public SortedDictionary<string, PathModel> LoadAllModels(string dxfFilePath, double? globalSweepHeight_mm,
            Func<ParamsText, ActualVariables> getVariables, Options options, MessageHandlerForEntities messages, int nestingDepth) {
            Dictionary<PathName, RawPathModel> rawModels = LoadRawModels(dxfFilePath, options, messages);
            SortedDictionary<string, PathModel> result = new();
            foreach (var kvp in rawModels) {
                PathModel? model = Load(kvp.Key, getVariables(kvp.Value.ParamsText!),
                                        globalSweepHeight_mm, dxfFilePath, options, "???", messages, nestingDepth, out _);
                if (model != null) {
                    result.Add(kvp.Key.AsString(), model);
                }
            }
            return result;
        }
    }

    private static Dictionary<PathName, RawPathModel> CollectSegments(DrawingEntities entities,
            Dictionary<string, Linetype> layerLinetypes, Collection subPathDefs,
            string dxfFilePath, Options options, MessageHandlerForEntities messages) {
        // Path lines:
        // DASHED:     __ __ __ __ = Sweep (G00)
        // HIDDEN:     ____ _____  = Sweep (G00) without parameters
        // CONTINUOUS: ___________ = Mill (G01/02) to full depth (B)
        // DIVIDE:     ___ . . ___ = Mill (G01/02) to mark depth (D)
        // BORDER:     __ __ . __  = Mill (G01/02) to full depth with support bars at mark depth
        // DASHDOT:    ___ . ___ . = Subpath

        // Special circles:
        // Circle with 1mm diameter und line type __ _ _ __ (PHANTOM) = Path start
        // Circle with 2mm diameter und line type  __ _ _ __ (PHANTOM) = Path end
        // Circle with 6mm diameter und line type  __ _ _ __ (PHANTOM) = ZProbe

        Dictionary<PathName, RawPathModel> name2Model = new();
        Dictionary<EntityObject, ParamsText> texts = new();

        ParamsText GetText(EntityObject e) {
            if (texts.TryGetValue(e, out ParamsText? t)) {
                if (e.Layer.Name != t.LayerName) {
                    messages.AddError(e, t.Position, dxfFilePath, Messages.PathModel_TextLayerDifferentFromElementLayer_TextLayer_ElementLayer, t.LayerName ?? "", e.Layer.Name);
                }
                return t;
            } else {
                return ParamsText.EMPTY;
            }
        }

        RawPathModel GetRawModel(EntityObject e, Vector2 position) {
            var p = new PathName(e.Layer.Name, dxfFilePath);
            return name2Model.TryGetValue(p, out RawPathModel? result)
                     ? result
                     : throw new KeyNotFoundException(MessageHandlerForEntities.Context(e, position, dxfFilePath) + ": " + string.Format(Messages.PathModel_MissingPathDefinition_PathName, p.AsString()));
        }

        RawPathModel GetOrCreateRawModel(EntityObject e) {
            var p = new PathName(e.Layer.Name, dxfFilePath);
            return name2Model.TryGetValue(p, out RawPathModel? result)
                     ? result
                     : name2Model[p] = new RawPathModel(p);
        }

        bool IsLineTypeHidden(EntityObject e) => DxfHelper.GetLinetype(e, layerLinetypes)?.Name == "HIDDEN";
        bool IsLineTypePhantomCircle(Circle c) => DxfHelper.GetLinetype(c, layerLinetypes)?.Name == "PHANTOM";
        bool IsStartMarker(Circle c) => IsLineTypePhantomCircle(c) && c.Radius.Near(0.5);
        bool IsEndMarker(Circle c) => IsLineTypePhantomCircle(c) && c.Radius.Near(1);
        bool IsZProbe(Circle c) => IsLineTypePhantomCircle(c) && c.Radius.Near(3);

        // Algorithm:
        // 1. Collect all objects on path layers - circles, lines, arcs, and texts
        // 2. Connect texts with objects -> Dictionary<EntityObject, string>
        // 3. Handle special circles
        // 4. Handle geometry - non-special circles, lines and arcs
        // 5. Load subpaths at the very end (tail-recursion hopefully reduces problems)

        // 1. Collect all objects on path layers - circles, lines, and arcs
        List<Circle> circles = entities.Circles.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();
        List<Line> lines = entities.Lines.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();
        List<Arc> arcs = entities.Arcs.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();

        HashSet<Circle> nonTextCircles = circles.Where(e => IsLineTypeHidden(e) || IsEndMarker(e)).ToHashSet();
        HashSet<Line> nonTextLines = lines.Where(IsLineTypeHidden).ToHashSet();
        HashSet<Arc> nonTextArcs = arcs.Where(IsLineTypeHidden).ToHashSet();

        // 2. Connect texts with objects -> Dictionary<EntityObject, string>
        foreach ((Circle2? TextCircle, string Text, EntityObject TextObject, Vector2 Position) in
            entities.Texts.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath))
                .Select(e => (TextCircle: GetOverlapSurrounding(e, dxfFilePath, messages), Text: e.Value, E: (EntityObject)e, Position: e.Position.AsVector2()))
            .Concat(entities.MTexts.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath))
                .Select(e => (TextCircle: GetOverlapSurrounding(e, dxfFilePath, messages), Text: e.PlainText(), E: (EntityObject)e, Position: e.Position.AsVector2()))
            ).Where(bs => bs.TextCircle != null)) {
            EntityObject? overlappingObject =
                // Order is important: Circles in pole position, then arcs, then lines.
                NearestOverlappingCircle(circles.Except(texts.Keys.OfType<Circle>()).Except(nonTextCircles), TextCircle!, Text, TextObject.Layer.Name)
                ?? NearestOverlappingArc(arcs.Except(texts.Keys.OfType<Arc>()).Except(nonTextArcs), TextCircle!, Text, TextObject.Layer.Name)
                ?? NearestOverlappingLine(lines.Except(texts.Keys.OfType<Line>()).Except(nonTextLines), TextCircle!, Text, TextObject.Layer.Name);
            if (overlappingObject == null) {
                messages.AddError(TextObject, Position, dxfFilePath, Messages.PathModel_NoObjectFound_Text_Center_Diameter, Text, TextCircle!.Center.F3(), (TextCircle!.Radius * 2).F3());
            } else {
                texts[overlappingObject] = new ParamsText(Text, TextObject, Position, TextCircle!.Center, TextCircle.Radius);
            }
        }

        if (options.ShowTextAssignments != null) {
            foreach (var kvp in texts) {
                ParamsText t = kvp.Value;
                if (options.ShowTextAssignments.IsMatch(t.Text)) {
                    messages.WriteLine();
                    messages.WriteLine(Messages.PathModel_TextAssignment_Obj_Text,
                        kvp.Key.ToLongString(layerLinetypes),
                        $"'{t.Text}' @ {t.TextCenter.F3()} d={(2 * t.TextRadius).F3()}");
                }
            }
            messages.WriteLine();
        }

        // 3. Handle special circles
        foreach (var circle in circles.Where(IsLineTypePhantomCircle)) {
            ParamsText circleText = GetText(circle);
            RawPathModel rawModel = GetOrCreateRawModel(circle);
            Vector2 center = circle.Center.AsVector2();

            if (IsStartMarker(circle)) {
                if (rawModel.Start != null) {
                    messages.AddError(circle, center, dxfFilePath, Messages.PathModel_TwoStarts_S1_S2, rawModel.Start.Value.F3(), center.F3());
                } else {
                    rawModel.Start = center;
                    rawModel.StartObject = circle;
                    rawModel.ParamsText = circleText;
                }
            } else if (IsEndMarker(circle)) { // End
                if (rawModel.End != null) {
                    messages.AddError(circle, center, dxfFilePath,
                        Messages.PathModel_TwoEnds_E1_E2, rawModel.End.Value.F3(), center.F3());
                } else {
                    rawModel.End = center;
                }
            } else if (IsZProbe(circle)) { // ZProbe
                rawModel.ZProbes.Add(new ZProbe(circle, circleText, center));
            } else {
                messages.AddError(circle, center, dxfFilePath, Messages.PathModel_NotSpecialCircle_Diameter, (2 * circle.Radius).F3());
            }
        }

        // 4. Handle geometry - non-special circles, lines and arcs
        // 4a. Circles
        foreach (var circle in circles.Where(c => !IsLineTypePhantomCircle(c))) {
            ParamsText circleText = GetText(circle);
            RawPathModel rawModel = GetRawModel(circle, circle.Center.AsVector2());
            if (ParamsText.IsNullOrEmpty(rawModel.ParamsText)) {
                messages.AddError(MessageHandlerForEntities.Context(rawModel.StartObject ?? circle,
                    rawModel.Start ?? circle.Center.AsVector2(), dxfFilePath),
                    Messages.PathModel_MissingParams_Path, rawModel.Name.AsString());
            } else {
                double? bitRadius_mm = rawModel.ParamsText!.GetO() / 2;
                Vector2 center = circle.Center.AsVector2();
                if (bitRadius_mm == null) {
                    messages.AddError(MessageHandlerForEntities.Context(circle, center, dxfFilePath),
                        Messages.PathModel_MissingKey_Key, 'O');
                } else if (circle.Radius.Near(bitRadius_mm.Value)) {
                    bool isMark = IsLineType(layerLinetypes, circle, "DIVIDE");
                    if (isMark || IsLineType(layerLinetypes, circle, "CONTINUOUS")) {
                        rawModel.RawSegments.Add(new DrillSegment.RawSegment(circle, circleText, center, isMark));
                    } else {
                        messages.AddError(circle, center, dxfFilePath, Messages.PathModel_LineTypeNotSupported_LineType, LineTypeName(layerLinetypes, circle));
                    }
                } else if (circle.Radius > bitRadius_mm.Value) {
                    MillType? millType = MillTypeFromLineType(layerLinetypes, circle);
                    if (millType.HasValue) {
                        rawModel.RawSegments.Add(new HelixSegment.RawSegment(circle, circleText, center, circle.Radius, millType.Value));
                    } else {
                        messages.AddError(circle, center, dxfFilePath, Messages.PathModel_LineTypeNotSupported_LineType, LineTypeName(layerLinetypes, circle));
                    }
                } else {
                    messages.AddError(circle, center, dxfFilePath, Messages.PathModel_CircleTooSmall_D_O, (2 * circle.Radius).F3(), (2 * bitRadius_mm.Value).F3());
                }
            }
        }

        // 4b. Lines
        List<SubPathSegment.RawSegment> subPaths = new();
        foreach (var line in lines) {
            ParamsText lineText = GetText(line);
            double order = lineText.GetN() ?? LAST_ORDER;
            Vector2 start = line.StartPoint.AsVector2();
            Vector2 end = line.EndPoint.AsVector2();
            RawPathModel rawModel = GetRawModel(line, start);
            LineGeometry geometry = new(start, end);

            HandleLineOrArc(layerLinetypes, dxfFilePath, subPathDefs, options, messages, subPaths, line, start, end, lineText, rawModel, order, geometry);
        }

        // 4c. Arcs
        foreach (var arc in arcs) {
            ParamsText arcText = GetText(arc);
            var arcGeometry = new ArcGeometry(arc.Center.AsVector2(), arc.Radius, arc.StartAngle, arc.EndAngle, counterclockwise: true);
            RawPathModel rawModel = GetRawModel(arc, arcGeometry.Start);
            double order = arcText.GetN() ?? LAST_ORDER;

            HandleLineOrArc(layerLinetypes, dxfFilePath, subPathDefs, options, messages, subPaths, arc, arcGeometry.Start, arcGeometry.End, arcText, rawModel, order, arcGeometry);
        }

        return name2Model;
    }

    private static void HandleLineOrArc(Dictionary<string, Linetype> layerLinetypes, string dxfFilePath,
        Collection subPathDefs, Options options, MessageHandlerForEntities messages,
        List<SubPathSegment.RawSegment> subPaths, EntityObject lineOrArc, Vector2 start, Vector2 end,
        ParamsText text, RawPathModel rawModel, double order, IMillGeometry geometry) {
       
        MillType? millType = MillTypeFromLineType(layerLinetypes, lineOrArc);
        if (millType.HasValue) {
            rawModel.RawSegments.Add(new ChainSegment.RawSegment(geometry, millType.Value, lineOrArc, text, order));
        } else if (IsLineType(layerLinetypes, lineOrArc, "DASHDOT")) { // Subpath
            var s = new SubPathSegment.RawSegment(lineOrArc, text, start, end, options, order, subPathDefs, dxfFilePath);
            rawModel.RawSegments.Add(s);
            subPaths.Add(s);
        } else if (IsLineType(layerLinetypes, lineOrArc, "DASHED")
            || IsLineType(layerLinetypes, lineOrArc, "HIDDEN")) { // Sweep
            rawModel.RawSegments.Add(new SweepSegment.RawSegment(lineOrArc, text, start, end, order));
        } else {
            messages.AddError(lineOrArc, start, dxfFilePath, Messages.PathModel_LineTypeNotSupported_LineType, LineTypeName(layerLinetypes, lineOrArc));
        }
    }

    private static bool IsLineType(Dictionary<string, Linetype> layerLinetypes, EntityObject eo, string prefix) {
        return LineTypeName(layerLinetypes, eo)?.StartsWith(prefix) ?? false;
    }

    private static MillType? MillTypeFromLineType(Dictionary<string, Linetype> layerLinetypes, EntityObject eo) {
        return IsLineType(layerLinetypes, eo, "CONTINUOUS") ? MillType.Mill
            : IsLineType(layerLinetypes, eo, "DIVIDE") ? MillType.Mark
            : IsLineType(layerLinetypes, eo, "BORDER") ? MillType.WithSupports
            : null;
    }

    private static string? LineTypeName(Dictionary<string, Linetype> layerLinetypes, EntityObject eo) {
        return DxfHelper.GetLinetype(eo, layerLinetypes)?.Name;
    }

    private static EntityObject? NearestOverlappingCircle(IEnumerable<Circle> circles, Circle2 textCircle, string text, string layerName) {
        return NearestOverlapping(circles, textCircle, text, layerName,
            textCircleOverLaps: c => textCircle.Center.Distance(c.Center) < textCircle.Radius + c.Radius,
            isNearerThan: (textCenter, c1, c2)
                => textCenter.SquareDistance(c1.Center) < textCenter.SquareDistance(c2.Center));
    }

    private static EntityObject? NearestOverlappingLine(IEnumerable<Line> lines, Circle2 textCircle, string text, string layerName) {
        return NearestOverlapping(lines, textCircle, text, layerName,
            textCircleOverLaps: c => CircleOverlapsLine(textCircle, c),
            isNearerThan: (textCenter, line1, line2)
                => MathHelper.PointLineDistance(textCenter, line1.StartPoint.AsVector2(), line1.Direction.AsVector2())
                    < MathHelper.PointLineDistance(textCenter, line2.StartPoint.AsVector2(), line2.Direction.AsVector2())
        );
    }

    private static bool CircleOverlapsLine(Circle2 textCircle, Line line) {
        Vector2 origin = line.StartPoint.AsVector2();
        Vector2 dir = Vector2.Normalize(line.Direction.AsVector2());
        double t = Vector2.DotProduct(dir, textCircle.Center - origin);
        Vector2 basePoint = origin + t * dir;
        double distance = (textCircle.Center - basePoint).Modulus();
        return distance < textCircle.Radius
            && MathHelper.PointInSegment(basePoint, origin, line.EndPoint.AsVector2()) == 0;
    }

    private static EntityObject? NearestOverlappingArc(IEnumerable<Arc> arcs, Circle2 textCircle, string text, string layerName) {
        return NearestOverlapping(arcs, textCircle, text, layerName,
            textCircleOverLaps: c => CircleOverlapsArc(textCircle, c, text),
            isNearerThan: (textCenter, arc1, arc2)
                => DistanceToArcCircle(textCenter, arc1) < DistanceToArcCircle(textCenter, arc2)
        );
    }

    private static bool CircleOverlapsArc(Circle2 textCircle, Arc arc, string text) {
        double? distance = DistanceToArcCircle(textCircle.Center, arc);
        return distance <= textCircle.Radius;
    }

    private static double DistanceToArcCircle(Vector2 textCenter, Arc arc) {
        Vector2 arcCenter = arc.Center.AsVector2();
        Vector2 arcCenter2textCenter = textCenter - arcCenter;
        Vector2 projOfTextCenterToArc = arcCenter + arcCenter2textCenter * (arc.Radius / arcCenter2textCenter.Modulus());

        bool projIsInArc = (Vector2.Angle(arcCenter2textCenter) * MathHelper.RadToDeg).AngleIsInArc(arc.StartAngle, arc.EndAngle);
        return projIsInArc ? (projOfTextCenterToArc - textCenter).Modulus() : double.PositiveInfinity;
    }

    private static EntityObject? NearestOverlapping<T>(IEnumerable<T> objects, Circle2 textCircle, string text,
        string layerName, Func<T, bool> textCircleOverLaps, Func<Vector2, T, T, bool> isNearerThan) where T : EntityObject {
        T? nearestOverlapping = null;
        foreach (var eo in objects.Where(eo => eo.Layer.Name == layerName && textCircleOverLaps(eo))) {
            if (nearestOverlapping == null || isNearerThan(textCircle.Center, eo, nearestOverlapping)) {
                nearestOverlapping = eo;
            }
        }
        return nearestOverlapping;
    }

    private static Circle2? GetOverlapSurrounding(Text text, string dxfFilePath, MessageHandlerForEntities messages) {
        Vector2 position = text.Position.AsVector2();
        if (text.IsBackward
            || text.IsUpsideDown
            || !text.Rotation.Near(0)) {
            messages.AddError(text, position, dxfFilePath, Messages.PathModel_TextLayout_Text, text.Value);
            return null;
        } else {
            double guessedWidth = text.Height * text.Value.Length * 0.6;
            double guessedHeight = text.Height;
            Vector2? offset =
                text.Alignment switch {
                    TextAlignment.BaselineLeft => new Vector2(guessedWidth, guessedHeight) / 2,
                    //TextAlignment.BaselineCenter =>
                    //TextAlignment.BaselineRight  =>
                    //TextAlignment.MiddleLeft     =>
                    TextAlignment.MiddleCenter => new Vector2(0, 0),
                    //TextAlignment.MiddleRight    =>
                    TextAlignment.TopLeft => new Vector2(guessedWidth, -guessedHeight) / 2,
                    //TextAlignment.TopCenter      =>
                    //TextAlignment.TopRight       =>
                    _ => null
                };
            if (offset == null) {
                messages.AddError(text, position, dxfFilePath, Messages.PathModel_TextLayout_Text, text.Value);
                return null;
            } else {
                return new Circle2(position + offset.Value,
                                   new Vector2(guessedWidth, guessedHeight).Modulus() / 2);
            }
        }
    }

    private static Circle2? GetOverlapSurrounding(MText text, string dxfFilePath, MessageHandlerForEntities messages) {
        Vector2 position = text.Position.AsVector2();
        if (!text.Rotation.Between(-1, 1) && !text.Rotation.Between(359, 361)) {
            messages.AddError(text, position, dxfFilePath, Messages.PathModel_TextLayout_Text, text.Value);
            return null;
        } else {
            string[] lines = text.PlainText().Split('\n');
            // TODO: \W for estimated width etc.?
            double guessedWidth = text.Height * lines.Max(s => s.Length) * 0.6;
            double guessedHeight = text.Height * lines.Length;

            Vector2? offset =
                text.AttachmentPoint switch {
                    MTextAttachmentPoint.BottomLeft => new Vector2(guessedWidth, guessedHeight) / 2,
                    //MTextAttachmentPoint.BaselineCenter =>
                    //MTextAttachmentPoint.BaselineRight  =>
                    //MTextAttachmentPoint.MiddleLeft     =>
                    MTextAttachmentPoint.MiddleCenter => new Vector2(0, 0),
                    //MTextAttachmentPoint.MiddleRight    =>
                    MTextAttachmentPoint.TopLeft => new Vector2(guessedWidth, -guessedHeight) / 2,
                    //MTextAttachmentPoint.TopCenter      =>
                    //MTextAttachmentPoint.TopRight       =>
                    _ => null
                };
            if (offset == null) {
                messages.AddError(text, position, dxfFilePath, Messages.PathModel_TextLayout_Text, text.Value);
                return null;
            } else {
                // Circle should not be much larger than text
                double radius = Math.Min(new Vector2(guessedWidth, guessedHeight).Modulus() / 2, guessedHeight * 0.7); 

                return new Circle2(position + offset.Value, radius);
            }
        }
    }

    private static PathModel? CreatePathModel(PathName name, RawPathModel rawModel, double? defaultSorNullForTplusO_mm,
        ActualVariables superpathVariables, string dxfFilePath, Options options, MessageHandlerForEntities messages, int nestingDepth) {
        if (rawModel.Start == null) {
            messages.AddError(name, Messages.PathModel_MissingStart);
        }
        if (rawModel.End == null) {
            messages.AddError(name, Messages.PathModel_MissingEnd);
        }
        if (rawModel.Start == null || rawModel.End == null) {
            return null;
        }

        // A. Connect RawSegments into a long chain
        // Algorithm:
        // * repeatedly extend chain at currEnd;
        // - if there is more than one un-traversed candidate, continue by order (N);
        // - at a branch that ends with a turn return node by node and try to continue there;
        List<IRawSegment> orderedRawSegments = new();
        {
            HashSet<IRawSegment> traversed = new();

            bool ExistsNonTraversed(Func<IRawSegment, bool> filter)
                => rawModel.RawSegments.Any(s => filter(s) && !traversed.Contains(s));

            IEnumerable<IRawSegment> FindNonTraversedAnchoredAt(Vector2 p)
                => rawModel.RawSegments.Where(s => !traversed.Contains(s) && (s.Start.Near(p) || s.End.Near(p)));

            Vector2 currEnd = rawModel.Start.Value;
            while (ExistsNonTraversed(s => true)) {
                IEnumerable<IRawSegment> candidates = FindNonTraversedAnchoredAt(currEnd);
                if (!candidates.Any()) {
                    if (rawModel.End.Value.Near(currEnd)) {
                        if (ExistsNonTraversed(s => !(s is ZProbe))) {
                            IRawSegment example = rawModel.RawSegments.First(s => !traversed.Contains(s));
                            int ct = rawModel.RawSegments.Count(s => !traversed.Contains(s));

                            messages.AddError(example.Source, example.Start, dxfFilePath, Messages.PathModel_UnreachedSegments, ct);
                        }
                        break;
                    } else {
                        IRawSegment? last = orderedRawSegments.LastOrDefault();
                        if (last != null) {
                            messages.AddError(last.Source, last.Start, dxfFilePath, Messages.PathModel_NoMoreSegmentsFound_P, currEnd.F3());
                        } else {
                            messages.AddError(name, Messages.PathModel_NoMoreSegmentsFound_P, currEnd.F3());
                        }
                        break;
                    }
                } else if (!candidates.Skip(1).Any()) { // Genau einer
                    candidates.Single().AddTo(orderedRawSegments, traversed, ref currEnd);
                } else {
                    candidates.OrderBy(s => s.Order)
                        .ThenBy(s => s.Preference)
                        .ThenBy(s => s.Length)
                        .First()
                        .AddTo(orderedRawSegments, traversed, ref currEnd);
                }
            }
        }
        {
            IRawSegment? last = orderedRawSegments.LastOrDefault();
            if (last != null && !last.End.Near(rawModel.End.Value)) {
                messages.AddError(last.Source, last.End, dxfFilePath, Messages.PathModel_LostEnd_End, rawModel.End.Value.F3());
            }
        }

        // B. Create PathSegments
        ILeafSegment[] bottomSegments = orderedRawSegments.Select(r => r.CreateSegment()).ToArray();

        // C. Create MillChains
        List<PathSegment> segments = new();
        {
            List<ChainSegment> currChain = new();
            foreach (var s in bottomSegments) {
                if (s is ChainSegment c) {
                    currChain.Add(c);
                } else {
                    if (currChain.Any()) {
                        segments.Add(new MillChain(currChain));
                        currChain.Clear();
                    }
                    if (s is PathSegment p) {
                        segments.Add(p);
                    }
                }
            }
            if (currChain.Any()) {
                segments.Add(new MillChain(currChain));
            }
        }

        // D. Create Params
        void OnError(string context, string msg) {
            messages.AddError(context, msg);
        }
        PathParams pathParams = new(rawModel.ParamsText!, superpathVariables, defaultSorNullForTplusO_mm,
            MessageHandlerForEntities.Context(rawModel.StartObject!, rawModel.Start.Value, dxfFilePath), options, OnError);
        {
            foreach (var s in segments) {
                s.CreateParams(pathParams, superpathVariables, dxfFilePath, OnError);
            }

            foreach (var z in rawModel.ZProbes) {
                z.CreateParams(pathParams, superpathVariables, dxfFilePath, OnError);
            }
        }

        // E. Connect Subpaths to PathModels
        foreach (var s in segments.OfType<SubPathSegment>()) {
            s.ConnectModel(dxfFilePath, messages, nestingDepth);
        }

        // Z. Create PathModel
        return new PathModel(rawModel.Name, pathParams, rawModel.Start.Value, rawModel.End.Value, segments, rawModel.ZProbes, dxfFilePath);
    }

    public bool IsEmpty() => !_segments.Any();

    public Transformation3 CreateTransformation(IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes)
        => new Transformation3(Start, Start + Vector2.UnitX, Vector2.Zero, Vector2.UnitX, orderedZProbes);

    public Vector3 EmitMillingGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {

        foreach (var s in _segments) {
            try {
                currPos = s.EmitGCode(currPos, t, globalS_mm, gcodes, dxfFileName, messages);
            } catch (EmitGCodeException ex) {
                messages.AddError(ex.ErrorContext, ex.Message);
            }
        }
        return currPos;
    }


    public List<(ZProbe ZProbe, Vector2 TransformedCenter)> CollectAndOrderAllZProbes() {
        // A. Collect all zProbes
        HashSet<(ZProbe ZProbe, Vector2 TransformedCenter)> openZProbes = CollectZProbes(new Transformation2(Start, Start + Vector2.UnitX, Vector2.Zero, Vector2.UnitX)).ToHashSet();

        // B. Order them
        List<(ZProbe ZProbe, Vector2 TransformedCenter)> orderedZProbes = new();
        {
            Vector2 currZEnd = Vector2.Zero;
            while (openZProbes.Any()) {
                (ZProbe ZProbe, Vector2 TransformedCenter) nearestZ = openZProbes.MinBy(z => (z.TransformedCenter - currZEnd).Modulus())!;
                orderedZProbes.Add(nearestZ);
                currZEnd = nearestZ.TransformedCenter;
                openZProbes.Remove(nearestZ);
            }
        }

        // C. Christen them
        int i = 51;
        foreach (var zc in orderedZProbes) {
            zc.ZProbe.SetName("#" + i++);
        }

        return orderedZProbes;
    }

    internal IEnumerable<(ZProbe ZProbe, Vector2 TransformedCenter)> CollectZProbes(Transformation2 t) {
        List<(ZProbe ZProbe, Vector2 TransformedCenter)> result = _zProbes.Select(z => (z, t.Transform(z.Center))).ToList();
        foreach (var s in _segments.OfType<SubPathSegment>()) {
            result.AddRange(s.CollectZProbes(t));
        }
        return result;
    }
}
