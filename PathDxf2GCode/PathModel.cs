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
        Options options, MessageHandler messages, PathModelCollection subPathDefs) {
        Dictionary<PathName, RawPathModel> models =
            CollectSegments(entities, layerLinetypes, subPathDefs, dxfFilePath, options, messages);
        Dictionary<PathName, PathModel> result = new();
        foreach (var kvp in models) {
            PathModel? m = CreatePathModel(kvp.Key, kvp.Value, dxfFilePath, options, messages);
            if (m != null) {
                result.Add(kvp.Key, m);
            }
        }
        return result;
    }

    private static Dictionary<PathName, RawPathModel> CollectSegments(DrawingEntities entities,
            Dictionary<string, Linetype> layerLinetypes, PathModelCollection subPathDefs,
            string dxfFilePath, Options options, MessageHandler messages) {
        // Pfadart:
        // strichlierte Linie = DASHED:           __ __ __ __ = Leerfahrt (G00)
        // langstrichlierte Linie = HIDDEN:       ____ _____  = Leerfahrt (G00) ohne Parameterangaben
        // durchgezogene Linie = CONTINUOUS:      ___________ = Fräsen mit voller Tiefe
        // strichdoppelpunktierte Linie = DIVIDE: ___ . . ___ = Fräsen mit Markierungstiefe
        // strichpunktierte Linie = DASHDOT:      ___ . ___ . = Subpfad 

        // Spezielle Formen:
        // Kreis mit 1mm Durchmesser und __ _ _ __ (PHANTOM)-Linie = Pfadstart
        // Kreis mit 1,5 mm Durchmesser und __ _ _ __ (PHANTOM)-Linie = Pfad-Wendepunkt 
        // Kreis mit 2mm Durchmesser und  __ _ _ __ (PHANTOM)-Linie = Pfadende

        Dictionary<PathName, RawPathModel> name2Model = new();
        Dictionary<EntityObject, ParamsText> texts = new();

        ParamsText GetText(EntityObject e) {
            if (texts.TryGetValue(e, out ParamsText? t)) {
                if (e.Layer.Name != t.LayerName) {
                    messages.AddError(e, t.Position, dxfFilePath, $"Text-Layername '{t.LayerName}' weicht von Element-Layername {e.Layer.Name} ab");
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
                                              : throw new Exception(MessageHandler.Context(e, position, dxfFilePath) + $": Pfaddefinition {p.AsString()} nicht gefunden");
        }

        RawPathModel GetOrCreateRawModel(EntityObject e) {
            var p = new PathName(e.Layer.Name, dxfFilePath);
            return name2Model.TryGetValue(p, out RawPathModel? result)
                                              ? result
                                              : name2Model[p] = new RawPathModel(p);
        }

        // Ablauf:
        // 1. Alle Objekte, die sich auf Layern befinden, einsammeln - Kreise, Strecken und Bögen
        // 2. Texte mit Objekten verbinden -> Dictionary<EntityObject, string>
        // 3. Kleine Kreise = Markierungen bearbeiten
        // 4. Geometrie = richtige Kreise, Strecken  und Bögen bearbeiten
        // 5. SubPaths ganz am Ende laden - Tail-Rekursion vermeidet hoffentlich manche Probleme

        // 1. Alle Objekte, die sich auf Layern befinden, einsammeln - Kreise, Strecken und Bögen
        List<Circle> circles = entities.Circles.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();
        List<Line> lines = entities.Lines.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();
        List<Arc> arcs = entities.Arcs.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath)).ToList();

        HashSet<Circle> hiddenCircles = circles.Where(e => LineTypeName(layerLinetypes, e) == "HIDDEN").ToHashSet();
        HashSet<Line> hiddenLines = lines.Where(e => LineTypeName(layerLinetypes, e) == "HIDDEN").ToHashSet();
        HashSet<Arc> hiddenArcs = arcs.Where(e => LineTypeName(layerLinetypes, e) == "HIDDEN").ToHashSet();

        // 2. Texte mit Objekten verbinden -> Dictionary<EntityObject, string>
        foreach ((Circle2? TextCircle, string Text, EntityObject TextObject, Vector2 Position) in
            entities.Texts.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath))
                .Select(e => (TextCircle: GetOverlapSurrounding(e, dxfFilePath, messages), Text: e.Value, E: (EntityObject)e, Position: e.Position.AsVector2()))
            .Concat(entities.MTexts.Where(e => e.IsOnPathLayer(options.PathNamePattern, dxfFilePath))
                .Select(e => (TextCircle: GetOverlapSurrounding(e, dxfFilePath, messages), Text: e.PlainText(), E: (EntityObject)e, Position: e.Position.AsVector2()))
            ).Where(bs => bs.TextCircle != null)) {
            EntityObject? overlappingObject =
                   // Reihenfolge wichtig: Kreise gewinnen; dann Arcs; dann Lines.
                   NearestOverlappingCircle(circles.Except(texts.Keys.OfType<Circle>()).Except(hiddenCircles), TextCircle!, Text)
                ?? NearestOverlappingArc(arcs.Except(texts.Keys.OfType<Arc>()).Except(hiddenArcs), TextCircle!, Text)
                ?? NearestOverlappingLine(lines.Except(texts.Keys.OfType<Line>()).Except(hiddenLines), TextCircle!, Text);
            if (overlappingObject == null) {
                messages.AddError(TextObject, Position, dxfFilePath, $"Kein überlappender Kreis, Bogen und keine überlappende Linie für '{Text}' gefunden; evtl. Textmitte nicht nahe genug (Textkreis: {TextCircle!.Center.F3()}, Durchm. {(TextCircle!.Radius * 2).F3()}) oder überlappender weiterer Text");
            } else {
                texts[overlappingObject] = new ParamsText(Text, TextObject, Position, TextCircle!.Center, TextCircle.Radius);
            }
        }

        if (options.ShowTextAssignments != null) {
            foreach (var kvp in texts) {
                ParamsText t = kvp.Value;
                if (options.ShowTextAssignments.IsMatch(t.Text)) {
                    messages.WriteLine();
                    messages.WriteLine("Objekt ...");
                    messages.WriteLine(kvp.Key.ToLongString(layerLinetypes));
                    messages.WriteLine("... hat zugeordneten Text");
                    messages.WriteLine($"'{t.Text}' @ {t.TextCenter.F3()} D={(2 * t.TextRadius).F3()}");
                }
            }
            messages.WriteLine();
        }

        bool IsPhantomCircle(Circle c) => GeometryHelpers.GetLinetype(c, layerLinetypes)?.Name == "PHANTOM";

        // 3. Spezialkreise bearbeiten
        foreach (var circle in circles.Where(c => IsPhantomCircle(c))) {
            ParamsText circleText = GetText(circle);
            RawPathModel rawModel = GetOrCreateRawModel(circle);
            Vector2 center = circle.Center.AsVector2();

            if (circle.Radius.Near(0.75)) { // Turn
                rawModel.Turns.Add(center);
            } else if (circle.Radius.Near(0.5)) { // Start
                if (rawModel.Start != null) {
                    messages.AddError(circle, center, dxfFilePath, $"Zwei Anfangspunkte definiert: {rawModel.Start.Value().F3()} und {center.F3()}");
                } else {
                    rawModel.Start = center;
                    rawModel.StartObject = circle;
                    rawModel.ParamsText = circleText;
                }
            } else if (circle.Radius.Near(1)) { // End
                if (rawModel.End != null) {
                    messages.AddError(circle, center, dxfFilePath, $"Zwei Endpunkte definiert: {rawModel.End.Value().F3()} und {center.F3()}");
                } else {
                    rawModel.End = center;
                }
            } else if (circle.Radius.Near(3)) { // ZProbe
                rawModel.ZProbes.Add(new ZProbe(circle, circleText, center));
            } else {
                messages.AddError(circle, center, dxfFilePath,
                    $"Kreis mit Linientyp PHANTOM (__ _ _ __) mit Durchmesser {2 * circle.Radius:F3} hat keine spezielle Bedeutung");
            }
        }

        //bool hasErrors = false;
        //foreach (var kvp in name2Model) {
        //    RawPathModel rawModel = kvp.Value;
        //    if (rawModel.Start == null) {
        //        messages.AddError(kvp.Key.AsString(), "Kein Anfangspunkt gefunden");
        //        hasErrors = true;
        //    } else if (rawModel.ParamsText == null) {
        //        messages.AddError(kvp.Key.AsString(), "Keine Parameter festgelegt");
        //        hasErrors = true;
        //    }
        //    if (rawModel.End == null) {
        //        messages.AddError(kvp.Key.AsString(), "Kein Endpunkt gefunden");
        //        hasErrors = true;
        //    }
        //}

        // 4. Geometrie = richtige Kreise, Strecken und Bögen bearbeiten
        // 4a. "richtige" Kreise
        foreach (var circle in circles.Where(c => !IsPhantomCircle(c))) {
            ParamsText circleText = GetText(circle);
            RawPathModel rawModel = GetRawModel(circle, circle.Center.AsVector2());
            double? bitRadius_mm = rawModel.ParamsText?.GetDouble('O') / 2;
            Vector2 center = circle.Center.AsVector2();
            if (bitRadius_mm == null) {
                messages.AddError(MessageHandler.Context(circle, center, dxfFilePath), "O-Wert fehlt");
            } else if (circle.Radius.Near(bitRadius_mm.Value)) {
                bool isMark = IsLineType(layerLinetypes, circle, "DIVIDE");
                if (isMark || IsLineType(layerLinetypes, circle, "CONTINUOUS")) {
                    rawModel.RawSegments.Add(new DrillSegment(circle, circleText, center, isMark));
                } else {
                    messages.AddError(circle, center, dxfFilePath, $"Linienart {LineTypeName(layerLinetypes, circle)} nicht unterstützt");
                }
            } else if (circle.Radius > bitRadius_mm.Value) {
                bool isMark = IsLineType(layerLinetypes, circle, "DIVIDE");
                if (isMark || IsLineType(layerLinetypes, circle, "CONTINUOUS")) {
                    rawModel.RawSegments.Add(new HelixSegment(circle, circleText, center, circle.Radius, isMark));
                } else {
                    messages.AddError(circle, center, dxfFilePath, $"Linienart {LineTypeName(layerLinetypes, circle)} nicht unterstützt");
                }
            } else {
                messages.AddError(circle, center, dxfFilePath, "O-Wert fehlt");
            }
        }

        // 4b. Strecken
        List<SubPathSegment> subPaths = new();
        foreach (var line in lines) {
            ParamsText lineText = GetText(line);
            double order = lineText.GetDouble('N') ?? LAST_ORDER;
            Vector2 start = line.StartPoint.AsVector2();
            Vector2 end = line.EndPoint.AsVector2();
            RawPathModel rawModel = GetRawModel(line, start);
            LineGeometry geometry = new(start, end);

            HandleLineOrArc(layerLinetypes, dxfFilePath, options, messages, subPaths, line, start, end, lineText, rawModel, order, geometry);
        }

        // 4c. Bögen
        foreach (var arc in arcs) {
            ParamsText arcText = GetText(arc);
            var arcGeometry = new ArcGeometry(arc.Center.AsVector2(), arc.Radius, arc.StartAngle, arc.EndAngle, counterclockwise: true);
            RawPathModel rawModel = GetRawModel(arc, arcGeometry.Start);
            double order = arcText.GetDouble('N') ?? LAST_ORDER;

            HandleLineOrArc(layerLinetypes, dxfFilePath, options, messages, subPaths, arc, arcGeometry.Start, arcGeometry.End,
                arcText, rawModel, order, arcGeometry);
        }

        // 5. SubPaths ganz am Ende laden - Tail-Rekursion vermeidet hoffentlich manche Probleme)
        foreach (var s in subPaths) {
            s.Load(subPathDefs, dxfFilePath, options, messages);
        }

        return name2Model;
    }

    private static void HandleLineOrArc(Dictionary<string, Linetype> layerLinetypes, string dxfFilePath,
        Options options, MessageHandler messages, List<SubPathSegment> subPaths, EntityObject lineOrArc,
        Vector2 start, Vector2 end, ParamsText text, RawPathModel rawModel, double order, MillGeometry geometry) {
        void OnError(string s) {
            messages.AddError(rawModel.Name.AsString(), s);
        }

        bool isMark = IsLineType(layerLinetypes, lineOrArc, "DIVIDE");
        if (isMark || IsLineType(layerLinetypes, lineOrArc, "CONTINUOUS")) {
            rawModel.RawSegments.Add(new ChainSegment(geometry, isMark, lineOrArc, text, order));
        } else if (IsLineType(layerLinetypes, lineOrArc, "DASHDOT")) { // Subpath
            var s = new SubPathSegment(lineOrArc, text, start, end, options.PathNamePattern, order, dxfFilePath, OnError);
            rawModel.RawSegments.Add(s);
            subPaths.Add(s);
        } else if (IsLineType(layerLinetypes, lineOrArc, "DASHED")
            || IsLineType(layerLinetypes, lineOrArc, "HIDDEN")) { // Sweep
            rawModel.RawSegments.Add(new SweepSegment(lineOrArc, text, start, end, order));
        } else {
            messages.AddError(lineOrArc, start, dxfFilePath, $"Linienart {LineTypeName(layerLinetypes, lineOrArc)} nicht unterstützt");
        }
    }

    private static bool IsLineType(Dictionary<string, Linetype> layerLinetypes, EntityObject eo, string prefix) {
        return LineTypeName(layerLinetypes, eo)?.StartsWith(prefix) ?? false;
    }

    private static string? LineTypeName(Dictionary<string, Linetype> layerLinetypes, EntityObject eo) {
        return GeometryHelpers.GetLinetype(eo, layerLinetypes)?.Name;
    }

    private static EntityObject? NearestOverlappingCircle(IEnumerable<Circle> circles, Circle2 textCircle, string text) {
        return NearestOverlapping(circles, textCircle, text,
            textCircleOverLaps: c => textCircle.Center.Distance(c.Center) < textCircle.Radius + c.Radius,
            isNearerThan: (textCenter, c1, c2)
                => textCenter.SquareDistance(c1.Center) < textCenter.SquareDistance(c2.Center));
    }

    private static EntityObject? NearestOverlappingLine(IEnumerable<Line> lines, Circle2 textCircle, string text) {
        return NearestOverlapping(lines, textCircle, text,
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
        double distance = (textCircle.Center - basePoint).Modulus(); // Länge
        return distance < textCircle.Radius
            && MathHelper.PointInSegment(basePoint, origin, line.EndPoint.AsVector2()) == 0;
    }

    private static EntityObject? NearestOverlappingArc(IEnumerable<Arc> arcs, Circle2 textCircle, string text) {
        return NearestOverlapping(arcs, textCircle, text,
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
        Func<T, bool> textCircleOverLaps, Func<Vector2, T, T, bool> isNearerThan) where T : EntityObject {
        T? nearestOverlapping = null;
        foreach (var eo in objects.Where(textCircleOverLaps)) {
            if (nearestOverlapping == null || isNearerThan(textCircle.Center, eo, nearestOverlapping)) {
                nearestOverlapping = eo;
            }
        }
        return nearestOverlapping;
    }

    private static Circle2? GetOverlapSurrounding(Text text, string dxfFilePath, MessageHandler messages) {
        if (text.IsBackward
            || text.IsUpsideDown
            || !text.Rotation.Near(0)
            || text.Alignment != TextAlignment.BaselineLeft && text.Alignment != TextAlignment.TopLeft) {
            messages.AddError(text, text.Position.AsVector2(), dxfFilePath, $"Text {text.Value} muss unrotiert mit Anker unten links oder oben links sein");
            return null;
        } else {
            double guessedWidth = text.Height * text.Value.Length * 0.6;
            Vector2 half = new Vector2(guessedWidth,
                text.Alignment == TextAlignment.BaselineLeft ? text.Height : -text.Height) / 2;
            return new Circle2(text.Position.AsVector2() + half, half.Modulus()); // Länge
        }
    }

    private static Circle2? GetOverlapSurrounding(MText text, string dxfFilePath, MessageHandler messages) {
        if (!text.Rotation.Between(-1, 1) && !text.Rotation.Between(359, 361)
            || text.AttachmentPoint != MTextAttachmentPoint.BottomLeft
               && text.AttachmentPoint != MTextAttachmentPoint.TopLeft) {
            messages.AddError(text, text.Position.AsVector2(), dxfFilePath, $"Text {text.Value} muss unrotiert mit Anker unten links oder oben links sein.");
            return null;
        } else {
            string[] lines = text.PlainText().Split('\n');
            // TODO: \W für geratene Breite usw. verwenden?
            double guessedWidth = text.Height * lines.Max(s => s.Length) * 0.6;
            double guessedHeight = text.Height * lines.Length;

            Vector2 half = new Vector2(guessedWidth,
                text.AttachmentPoint == MTextAttachmentPoint.BottomLeft ? guessedHeight : -guessedHeight) / 2;

            double radius = Math.Min(half.Modulus(), guessedHeight * 0.7); // Circle should not be much larger than text

            return new Circle2(text.Position.AsVector2() + half, radius);
        }
    }

    private static PathModel? CreatePathModel(PathName name, RawPathModel rawModel, string dxfFilePath,
        Options options, MessageHandler messages) {
        if (rawModel.Start == null) {
            messages.AddError(name, "Start-Markierung fehlt");
        }
        if (rawModel.End == null) {
            messages.AddError(name, "Ende-Markierung fehlt");
        }
        if (rawModel.Start == null || rawModel.End == null) {
            return null;
        }

        // A. RawSegments zu langer Kette verbinden
        // Algorithmus:
        // * Ab currEnd immer wieder verlängern; dabei
        // - Ast, der mit Turn endet, wieder zurückgehen und ab jedem Knoten versuchen, weiterzugehen
        // - bei mehreren Kandidaten (die noch nicht traversed sind) jenem mit geringster Order nachgehen.
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

                            messages.AddError(example.Source, example.Start, dxfFilePath,
                                $"{ct} Segmente (u.a. dieses hier) wurden nicht erreicht - evtl. fehlt N-Auszeichnung");
                        }
                        break;
                    } else {
                        IRawSegment? last = orderedRawSegments.LastOrDefault();
                        if (last != null) {
                            messages.AddError(last.Source, last.Start, dxfFilePath, $"Keine weiteren Segmente ab Punkt {currEnd.F3()} gefunden");
                        } else {
                            messages.AddError(name, $"Keine weiteren Segmente ab Punkt {currEnd.F3()} gefunden");
                        }
                        break;
                    }
                } else if (!candidates.Skip(1).Any()) { // Genau einer
                    candidates.Single().AddTo(orderedRawSegments, traversed, backtrackForTurns, ref currEnd);
                } else {
                    candidates.OrderBy(s => s.Order).ThenBy(s => s.Length).First().AddTo(orderedRawSegments, traversed, backtrackForTurns, ref currEnd);
                }
            }
        }
        // B. MillChains aufbauen
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
        // C. Params erzeugen
        void OnError(string context, string msg) {
            messages.AddError(context, msg);
        }
        PathParams pathParams = new(rawModel.ParamsText!,
            MessageHandler.Context(rawModel.StartObject!, rawModel.Start.Value, dxfFilePath), options, OnError);
        {

            foreach (var s in segments) {
                s.CreateParams(pathParams, dxfFilePath, OnError);
            }

            foreach (var z in rawModel.ZProbes) {
                z.CreateParams(pathParams, dxfFilePath, OnError);
            }
        }

        // D. _zProbes besser sortieren
        List<ZProbe> orderedZProbes = new();
        {
            Vector2 currZEnd = rawModel.Start.Value;
            while (rawModel.ZProbes.Count > orderedZProbes.Count) {
                ZProbe nearestZ = rawModel.ZProbes.Except(orderedZProbes).MinBy(z => (z.Center - currZEnd).Modulus())!;
                orderedZProbes.Add(nearestZ);
                currZEnd = nearestZ.Center;
            }
        }

        // Z. PathModel erzeugen
        return new PathModel(rawModel.Name, pathParams, rawModel.Start.Value, rawModel.End.Value, segments, orderedZProbes, dxfFilePath);
    }

    public bool IsEmpty() => !_segments.Any();

    public Transformation3 CreateTransformation()
        => new Transformation3(Start, Start + Vector2.UnitX, Vector2.Zero, Vector2.UnitX, _zProbes);

    public bool HasZProbes => _zProbes.Any();

    public Vector3 EmitMillingGCode(Vector3 currPos, Transformation3 t,
        StreamWriter sw, Statistics stats, string dxfFileName, MessageHandler messages) {
        foreach (var s in _segments) {
            try {
                currPos = s.EmitGCode(currPos, t, sw, stats, dxfFileName, messages);
            } catch (EmitGCodeException ex) {
                messages.AddError(ex.ErrorContext, ex.Message);
            }
        }
        return currPos;
    }

    public void WriteEmptyZ(StreamWriter sw) {
        int i = 2001;
        foreach (var z in _zProbes) {
            sw.WriteLine((z.Center.F3() + (z.L == null ? "" : "/L:" + z.L) + "/T:" + z.T_mm.F3()).AsComment(0) + $" #{i++}=");
        }
    }

    public Vector3 EmitZProbingGCode(Vector3 currPos, StreamWriter sw, Statistics stats, string dxfFileName, MessageHandler messages) {
        double sweepHeight = currPos.Z;
        var t = new Transformation2(Start, Start + Vector2.UnitX, Vector2.Zero, Vector2.UnitX);
        foreach (var z in _zProbes) {
            currPos = GCodeHelpers.SweepFromTo(currPos, t.Transform(z.Center).AsVector3(sweepHeight), sw, stats);
            try {
                currPos = z.EmitGCode(currPos, t, sw, stats, dxfFileName, messages);
                if (!currPos.Z.Near(sweepHeight)) {
                    throw new Exception($"Interner Fehler - {currPos.Z} <> {sweepHeight}");
                }
            } catch (EmitGCodeException ex) {
                messages.AddError(ex.ErrorContext, ex.Message);
            }
        }
        return currPos;
    }
}
