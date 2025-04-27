namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using netDxf.Collections;
using netDxf.Entities;
using netDxf.Tables;
using System.Collections.Generic;

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
    private readonly IEnumerable<ZProbe> _zProbes;

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
        public readonly List<Vector2> Turns = new();
        public readonly List<IRawSegment> RawSegments = new();
        public readonly List<ZProbe> ZProbes = new();

        public RawPathModel(PathName p) {
            Name = p;
        }

        public bool HasTurnAt(Vector2 v) => Turns.Any(t => t.Near(v));
    }

    public static Dictionary<PathName, PathModel> TransformDxf2PathModel(
        string dxfFilePath, DrawingEntities entities, Dictionary<string, Linetype> layerLinetypes,
        double? defaultSorNullForTplusO_mm, Options options, MessageHandlerForEntities messages, PathModelCollection subPathDefs) {
        Dictionary<PathName, RawPathModel> models =
            CollectSegments(entities, layerLinetypes, subPathDefs, dxfFilePath, options, messages);
        Dictionary<PathName, PathModel> result = new();
        foreach (var kvp in models) {
            PathModel? m = CreatePathModel(kvp.Key, kvp.Value, defaultSorNullForTplusO_mm, dxfFilePath, options, messages);
            if (m != null) {
                result.Add(kvp.Key, m);
            }
        }
        return result;
    }

    private static Dictionary<PathName, RawPathModel> CollectSegments(DrawingEntities entities,
            Dictionary<string, Linetype> layerLinetypes, PathModelCollection subPathDefs,
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
        // Circle with 1.5 mm diameter und line type __ _ _ __ (PHANTOM) = Path reversal 
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
        bool IsTurnMarker(Circle c) => IsLineTypePhantomCircle(c) && c.Radius.Near(0.75);
        bool IsEndMarker(Circle c) => IsLineTypePhantomCircle(c) && c.Radius.Near(1);
        bool IsZProbe(Circle c) => IsLineTypePhantomCircle(c) && c.Radius.Near(3);

        // Algorithm:
        // 1. Collect all objects on path layers - circles, lines, arcs, and texts
        // 2. Connect texts with objects -> Dictionary<EntityObject, string>
        // 3. Handle special circles
        // 4. Handle geometry - non-special circles, lines and arcs
        // 5. Load subpaths at the very end (tail-recursion hopefully reduces problems)

        // 1. Collect all objects on path layers - circles, lines, and arcs
        List<Circle> circles = entities.Circles.Where(e => e.IsOnPathLayer(options.PathNamePattern,  dxfFilePath)).ToList();
        List<Line> lines = entities.Lines.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();
        List<Arc> arcs = entities.Arcs.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();

        HashSet<Circle> nonTextCircles = circles.Where(e => IsLineTypeHidden(e) || IsEndMarker(e) || IsTurnMarker(e)).ToHashSet();
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

            if (IsTurnMarker(circle)) { 
                rawModel.Turns.Add(center);
            } else if (IsStartMarker(circle)) {
                if (rawModel.Start != null) {
                    messages.AddError(circle, center, dxfFilePath, Messages.PathModel_TwoStarts_S1_S2, rawModel.Start.Value().F3(), center.F3());
                } else {
                    rawModel.Start = center;
                    rawModel.StartObject = circle;
                    rawModel.ParamsText = circleText;
                }
            } else if (IsEndMarker(circle)) { // End
                if (rawModel.End != null) {
                    messages.AddError(circle, center, dxfFilePath,
                        Messages.PathModel_TwoEnds_E1_E2, rawModel.End.Value().F3(), center.F3());
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
                double? bitRadius_mm = rawModel.ParamsText!.GetDouble('O') / 2;
                Vector2 center = circle.Center.AsVector2();
                if (bitRadius_mm == null) {
                    messages.AddError(MessageHandlerForEntities.Context(circle, center, dxfFilePath),
                        Messages.PathModel_MissingKey_Key, 'O');
                } else if (circle.Radius.Near(bitRadius_mm.Value)) {
                    bool isMark = IsLineType(layerLinetypes, circle, "DIVIDE");
                    if (isMark || IsLineType(layerLinetypes, circle, "CONTINUOUS")) {
                        rawModel.RawSegments.Add(new DrillSegment(circle, circleText, center, isMark));
                    } else {
                        messages.AddError(circle, center, dxfFilePath, Messages.PathModel_LineTypeNotSupported_LineType, LineTypeName(layerLinetypes, circle));
                    }
                } else if (circle.Radius > bitRadius_mm.Value) {
                    MillType? millType = MillTypeFromLineType(layerLinetypes, circle);
                    if (millType.HasValue) {
                        rawModel.RawSegments.Add(new HelixSegment(circle, circleText, center, circle.Radius, millType.Value));
                    } else {
                        messages.AddError(circle, center, dxfFilePath, Messages.PathModel_LineTypeNotSupported_LineType, LineTypeName(layerLinetypes, circle));
                    }
                } else {
                    messages.AddError(circle, center, dxfFilePath, Messages.PathModel_CircleTooSmall_D_O, (2 * circle.Radius).F3(), (2 * bitRadius_mm.Value).F3());
                }
            }
        }

        // 4b. Lines
        List<SubPathSegment> subPaths = new();
        foreach (var line in lines) {
            ParamsText lineText = GetText(line);
            double order = lineText.GetDouble('N') ?? LAST_ORDER;
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
            double order = arcText.GetDouble('N') ?? LAST_ORDER;

            HandleLineOrArc(layerLinetypes, dxfFilePath, subPathDefs, options, messages, subPaths, arc, arcGeometry.Start, arcGeometry.End, arcText, rawModel, order, arcGeometry);
        }

        return name2Model;
    }

    private static void HandleLineOrArc(Dictionary<string, Linetype> layerLinetypes, string dxfFilePath,
        PathModelCollection subPathDefs, Options options, MessageHandlerForEntities messages, 
        List<SubPathSegment> subPaths, EntityObject lineOrArc, Vector2 start, Vector2 end, 
        ParamsText text, RawPathModel rawModel, double order, IMillGeometry geometry) {
        void OnError(string s) {
            messages.AddError(rawModel.Name.AsString(), s);
        }

        MillType? millType = MillTypeFromLineType(layerLinetypes, lineOrArc);
        if (millType.HasValue) {
            rawModel.RawSegments.Add(new ChainSegment(geometry, millType.Value, lineOrArc, text, order));
        } else if (IsLineType(layerLinetypes, lineOrArc, "DASHDOT")) { // Subpath
            var s = new SubPathSegment(lineOrArc, text, start, end, options, order, subPathDefs, dxfFilePath, OnError);
            rawModel.RawSegments.Add(s);
            subPaths.Add(s);
        } else if (IsLineType(layerLinetypes, lineOrArc, "DASHED")
            || IsLineType(layerLinetypes, lineOrArc, "HIDDEN")) { // Sweep
            rawModel.RawSegments.Add(new SweepSegment(lineOrArc, text, start, end, order));
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
        if (text.IsBackward
            || text.IsUpsideDown
            || !text.Rotation.Near(0)
            || text.Alignment != TextAlignment.BaselineLeft && text.Alignment != TextAlignment.TopLeft) {
            messages.AddError(text, text.Position.AsVector2(), dxfFilePath, Messages.PathModel_TextLayout_Text, text.Value);
            return null;
        } else {
            double guessedWidth = text.Height * text.Value.Length * 0.6;
            Vector2 half = new Vector2(guessedWidth,
                text.Alignment == TextAlignment.BaselineLeft ? text.Height : -text.Height) / 2;
            return new Circle2(text.Position.AsVector2() + half, half.Modulus());
        }
    }

    private static Circle2? GetOverlapSurrounding(MText text, string dxfFilePath, MessageHandlerForEntities messages) {
        if (!text.Rotation.Between(-1, 1) && !text.Rotation.Between(359, 361)
            || text.AttachmentPoint != MTextAttachmentPoint.BottomLeft
               && text.AttachmentPoint != MTextAttachmentPoint.TopLeft) {
            messages.AddError(text, text.Position.AsVector2(), dxfFilePath, Messages.PathModel_TextLayout_Text, text.Value);
            return null;
        } else {
            string[] lines = text.PlainText().Split('\n');
            // TODO: \W for estimated width etc.?
            double guessedWidth = text.Height * lines.Max(s => s.Length) * 0.6;
            double guessedHeight = text.Height * lines.Length;

            Vector2 half = new Vector2(guessedWidth,
                text.AttachmentPoint == MTextAttachmentPoint.BottomLeft ? guessedHeight : -guessedHeight) / 2;

            double radius = Math.Min(half.Modulus(), guessedHeight * 0.7); // Circle should not be much larger than text

            return new Circle2(text.Position.AsVector2() + half, radius);
        }
    }

    private static PathModel? CreatePathModel(PathName name, RawPathModel rawModel, double? defaultSorNullForTplusO_mm,
        string dxfFilePath, Options options, MessageHandlerForEntities messages) {
        if (rawModel.Start == null) {
            messages.AddError(name, Messages.PathModel_MissingStart);
        }
        if (rawModel.End == null) {
            messages.AddError(name, Messages.PathModel_MissingEnd);
        }
        if (rawModel.Start == null || rawModel.End == null) {
            return null;
        }

        // A. Connect RawSegments to a long chain
        // Algorithm:
        // * repeatedly extend chain at currEnd;
        // - if there is more than one un-traversed candidate, continue by order (N);
        // - at a branch that ends with a turn return node by node and try to continue there;
        List<IRawSegment> orderedRawSegments = new();
        {
            Stack<IRawSegment> backtrackForTurns = new();
            HashSet<IRawSegment> traversed = new();

            bool ExistsNonTraversed(Func<IRawSegment, bool> filter)
                => rawModel.RawSegments.Any(s => filter(s) && !traversed.Contains(s));

            IEnumerable<IRawSegment> FindNonTraversedAnchoredAt(Vector2 p)
                => rawModel.RawSegments.Where(s => !traversed.Contains(s) && (s.Start.Near(p) || s.End.Near(p)));

            Vector2 currEnd = rawModel.Start.Value;
            while (ExistsNonTraversed(s => true)) {
                IEnumerable<IRawSegment> candidates = FindNonTraversedAnchoredAt(currEnd);
                if (!candidates.Any()) {
                    if (rawModel.HasTurnAt(currEnd)) {
                        while (backtrackForTurns.TryPop(out var s)) {
                            s.ReversedSegmentAfterTurn().AddTo(orderedRawSegments, traversed, null, ref currEnd);
                            IEnumerable<IRawSegment> candidatesWhileReversing = FindNonTraversedAnchoredAt(currEnd);
                            if (candidatesWhileReversing.Any()) {
                                break;
                            }
                        }
                    } else if (rawModel.End.Value.Near(currEnd)) {
                        if (ExistsNonTraversed(s => !(s is ZProbe))) {
                            IRawSegment example = rawModel.RawSegments.First(s => !traversed.Contains(s));
                            int ct = rawModel.RawSegments.Count(s => !traversed.Contains(s));

                            messages.AddError(example.Source, example.Start, dxfFilePath,Messages.PathModel_UnreachedSegments, ct);
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
                    candidates.Single().AddTo(orderedRawSegments, traversed, backtrackForTurns, ref currEnd);
                } else {
                    candidates.OrderBy(s => s.Order)
                        .ThenBy(s => s.Preference)
                        .ThenBy(s => s.Length)
                        .First()
                        .AddTo(orderedRawSegments, traversed, backtrackForTurns, ref currEnd);
                }
            }
        }
        {
            IRawSegment? last = orderedRawSegments.LastOrDefault();
            if (last != null && !last.End.Near(rawModel.End.Value)) {
                messages.AddError(last.Source, last.End, dxfFilePath, Messages.PathModel_LostEnd_End, rawModel.End.Value.F3());
            }
        }

        // B. Create MillChains
        List<PathSegment> segments = new();
        {
            List<ChainSegment> currChain = new();
            foreach (var s in orderedRawSegments) {
                if (s is ChainSegment c) {
                    currChain.Add(c);
                } else {
                    if (currChain.Any()) {
                        segments.Add(new MillChain(currChain.ToArray()));
                        currChain.Clear();
                    }
                    if (s is PathSegment p) {
                        segments.Add(p);
                    }
                }
            }
            if (currChain.Any()) {
                segments.Add(new MillChain(currChain.ToArray()));
            }
        }

        // C. Create Params
        void OnError(string context, string msg) {
            messages.AddError(context, msg);
        }
        PathParams pathParams = new(rawModel.ParamsText!, defaultSorNullForTplusO_mm,
            MessageHandlerForEntities.Context(rawModel.StartObject!, rawModel.Start.Value, dxfFilePath), options, OnError);
        {

            foreach (var s in segments) {
                s.CreateParams(pathParams, dxfFilePath, OnError);
            }

            foreach (var z in rawModel.ZProbes) {
                z.CreateParams(pathParams, dxfFilePath, OnError);
            }
        }

        // D. Sort _zProbes
        List<ZProbe> orderedZProbes = new();
        {
            Vector2 currZEnd = rawModel.Start.Value;
            while (rawModel.ZProbes.Count > orderedZProbes.Count) {
                ZProbe nearestZ = rawModel.ZProbes.Except(orderedZProbes).MinBy(z => (z.Center - currZEnd).Modulus())!;
                orderedZProbes.Add(nearestZ);
                currZEnd = nearestZ.Center;
            }
            int i = 51;
            foreach (var zProbe in orderedZProbes) {
                zProbe.SetName("#" + i++);
            }
        }

        // Z. Finally, create PathModel
        return new PathModel(rawModel.Name, pathParams, rawModel.Start.Value, rawModel.End.Value, segments, orderedZProbes, dxfFilePath);
    }

    public bool IsEmpty() => !_segments.Any();

    public Transformation3 CreateTransformation()
        => new Transformation3(Start, Start + Vector2.UnitX, Vector2.Zero, Vector2.UnitX, _zProbes);

    public bool HasZProbes => _zProbes.Any();

    public Vector3 EmitMillingGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages, int depth) {

        foreach (var s in _segments) {
            try {
                currPos = s.EmitGCode(currPos, t, globalS_mm, gcodes, dxfFileName, messages, depth);
            } catch (EmitGCodeException ex) {
                messages.AddError(ex.ErrorContext, ex.Message);
            }
        }
        return currPos;
    }

    public void WriteEmptyZ(StreamWriter sw) {
        foreach (var z in _zProbes) {
            sw.WriteLine((z.Center.F3() + (z.L == null ? "" : "/L:" + z.L) + "/T:" + z.T_mm.F3()).AsComment(0) + " " + z.Name + "=");
        }
    }

    public Vector3 EmitZProbingGCode(Vector3 currPos, double globalS_mm, List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        double sweepHeight = currPos.Z;
        var t = new Transformation2(Start, Start + Vector2.UnitX, Vector2.Zero, Vector2.UnitX);
        foreach (var z in _zProbes) {
            currPos = GCodeHelpers.SweepFromTo(currPos, t.Transform(z.Center).AsVector3(sweepHeight), globalS_mm, gcodes);
            try {
                currPos = z.EmitGCode(currPos, t, gcodes, dxfFileName, messages);
                if (!currPos.Z.Near(sweepHeight)) {
                    throw new Exception($"Internal Error - {currPos.Z} <> {sweepHeight}");
                }
            } catch (EmitGCodeException ex) {
                messages.AddError(ex.ErrorContext, ex.Message);
            }
        }
        return currPos;
    }
}
