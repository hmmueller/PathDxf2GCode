namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using netDxf.Entities;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public interface IRawSegment {
    EntityObject Source { get; }
    ParamsText ParamsText { get; }

    Vector2 Start { get; }
    Vector2 End { get; }
    void Reverse();
    double Order { get; }
    int Preference { get; }
    double Length
        => Vector2.Distance(Start, End);

    IRawSegment ReversedSegmentAfterTurn();

    public Vector2 OppositeEndOf(Vector2 v)
        => v.Near(Start) ? End : Start;

    public void AddTo(List<IRawSegment> result, HashSet<IRawSegment> traversed, Stack<IRawSegment>? backtrackForTurns, ref Vector2 currEnd) {
        if (End.Near(currEnd)) {
            Reverse();
        }
        result.Add(this);
        currEnd = OppositeEndOf(currEnd);
        traversed.Add(this);
        backtrackForTurns?.Push(this);
    }
}

public abstract class PathSegment {
    public abstract Vector2 Start { get; }
    public abstract Vector2 End { get; }

    public abstract void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError);

    public static void Assert(bool b, string errorContext, string message) {
        if (!b) {
            throw new EmitGCodeException(errorContext, message);
        }
    }

    public static void AssertNear(Vector2 a, Vector2 b, string errorContext) {
        Assert(a.Near(b), errorContext, $"!{a.F3()}.Near({b.F3()})");
    }

    public abstract Vector3 EmitGCode(Vector3 currPos, Transformation3 zCorr, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages);
}

public abstract class PathSegmentWithParamsText : PathSegment {
    public EntityObject Source { get; }
    public ParamsText ParamsText { get; }
    protected IParams? _params;

    protected PathSegmentWithParamsText(EntityObject source, ParamsText paramsText) {
        Source = source;
        ParamsText = paramsText;
    }

    protected string ErrorContext(string dxfFileName)
        => MessageHandlerForEntities.Context(Source, Start, dxfFileName);
}

public class MillChain : PathSegment {
    private readonly List<ChainSegment> _segments;
    private ChainParams? _params;

    public MillChain(IEnumerable<ChainSegment> segments) {
        _segments = segments.ToList();
    }

    public override Vector2 Start
        => _segments[0].Start;

    public override Vector2 End
        => _segments.Last().End;

    public override void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new ChainParams(_segments[0].ParamsText,
            MessageHandlerForEntities.Context(_segments[0].Source, _segments[0].Start, dxfFileName), pathParams, onError);
        foreach (var s in _segments) {
            s.CreateParams(_params, dxfFileName, onError);
        }
    }

    private enum EdgeMilled { Unknown, Start2End, End2Start }

    private class Edge {
        public readonly ChainSegment Segment;
        public readonly double MillingBottom_mm;
        public readonly Edge? Above;
        public EdgeMilled Milled;

        public Edge(ChainSegment segment, double millingBottom_mm, Edge? above) {
            Segment = segment;
            MillingBottom_mm = millingBottom_mm;
            Above = above;
            Milled = EdgeMilled.Unknown;
        }

        public Vector3 Start(Transformation3 t) => t.Transform(Segment.Start).AsVector3(MillingBottom_mm);
        public Vector3 End(Transformation3 t) => t.Transform(Segment.End).AsVector3(MillingBottom_mm);

        public bool TopUnmilled => Milled == EdgeMilled.Unknown && (Above == null || Above.Milled != EdgeMilled.Unknown);
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                                      List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        // Create actual edges
        List<Edge> edges = new();
        foreach (var s in _segments) {
            Edge? prev = default;
            for (double millingBottom_mm = _params!.T_mm - _params!.I_mm; millingBottom_mm >= s.Bottom_mm; millingBottom_mm -= _params!.I_mm) {
                edges.Add(prev = new Edge(s, millingBottom_mm, prev));
            }
            if (prev == null || !prev.MillingBottom_mm.Near(s.Bottom_mm)) {
                edges.Add(new Edge(s, s.Bottom_mm, prev));
            }
        }

        ISet<Edge> topEdges = edges.Where(e => e.Above == null).ToHashSet();

        int remaining = edges.Count;
        List<Edge> sortedEdges = new();

        // Optimized order
        Vector3 headPos = edges.First().Start(t);

        while (remaining > 0) {
            Edge? nearest = null;
            bool nearestConnectAtStart = true;
            Vector3 NearestMillingStart() => nearestConnectAtStart ? nearest!.Start(t) : nearest!.End(t);

            foreach (var candidate in edges.Where(e => e.TopUnmilled)) {
                double distToStart_mm = headPos.Distance(candidate.Start(t));
                double distToEnd_mm = headPos.Distance(candidate.End(t));
                if (nearest == null) {
                    nearest = candidate;
                    nearestConnectAtStart = distToStart_mm < distToEnd_mm;
                } else if (distToStart_mm < headPos.Distance(NearestMillingStart())) {
                    nearest = candidate;
                    nearestConnectAtStart = true;
                } else if (distToEnd_mm < headPos.Distance(NearestMillingStart())) {
                    nearest = candidate;
                    nearestConnectAtStart = false;
                }
            }

            sortedEdges.Add(nearest!);
            if (nearest!.Milled != EdgeMilled.Unknown) {
                throw new Exception("Internal Error");
            }
            nearest.Milled = nearestConnectAtStart ? EdgeMilled.Start2End : EdgeMilled.End2Start;
            headPos = nearestConnectAtStart ? nearest.End(t) : nearest.Start(t);
            remaining--;
        }

        // Create code
        double sk_mm = _params!.RawK_mm ?? _params!.S_mm;
        foreach (var e in sortedEdges) {
            currPos = GCodeHelpers.SweepAndDrillSafelyFromTo(from: currPos, 
                to: e.Milled == EdgeMilled.Start2End ? e.Start(t) : e.End(t), 
                t_mm: _params!.T_mm, sk_mm: sk_mm, globalS_mm, _params!.F_mmpmin, 
                backtracking: false, t, gcodes);

            currPos = e.Segment.EmitGCode(currPos, e.MillingBottom_mm, e.Milled == EdgeMilled.Start2End, 
                globalS_mm, t, gcodes, dxfFileName);
        }

        Vector2 end = t.Transform(_segments.Last().End);
        currPos = GCodeHelpers.SweepAndDrillSafelyFromTo(from: currPos, to: end.AsVector3(sk_mm),
            t_mm: _params!.T_mm, sk_mm: sk_mm, globalS_mm, _params!.F_mmpmin, backtracking: false, t, gcodes);
        AssertNear(currPos.XY(), end, MessageHandlerForEntities.Context(_segments.Last().Source, _segments.First().Start, dxfFileName));
        return currPos;
    }
}

public interface IMarkOrMillSegment {
    bool IsMark { get; }
    double Bottom_mm { get; }
}

public class ChainSegment : IRawSegment, IMarkOrMillSegment {
    public int Preference => 4;

    public EntityObject Source { get; }
    public ParamsText ParamsText { get; }
    public bool IsMark { get; }

    private MillGeometry _geometry;
    private IParams? _params;

    public double Bottom_mm
        => IsMark ? _params!.D_mm : _params!.B_mm;

    public ChainSegment(MillGeometry geometry, bool isMark, EntityObject source, ParamsText paramsText, double order) {
        _geometry = geometry;
        IsMark = isMark;
        Source = source;
        Order = order;
        ParamsText = paramsText;
    }

    public Vector2 Start => _geometry.Start;
    public Vector2 End => _geometry.End;

    public double Order { get; }

    public void Reverse() {
        _geometry = _geometry.CloneReversed();
    }

    public IRawSegment ReversedSegmentAfterTurn()
        => ReversedSegment();

    public ChainSegment ReversedSegment()
        => new ChainSegment(_geometry.CloneReversed(), IsMark, Source, ParamsText, PathModel.BACKTRACK_ORDER);

    protected string ErrorContext(string dxfFileName)
        => MessageHandlerForEntities.Context(Source, Start, dxfFileName);

    internal void CreateParams(ChainParams chainParams, string dxfFileName, Action<string, string> onError) {
        _params = new MillParams(ParamsText, IsMark, ErrorContext(dxfFileName), chainParams, onError);
    }

    internal Vector3 EmitGCode(Vector3 currPos, double millingLayer_mm, bool start2End, double globalS_mm, 
                                Transformation3 t, List<GCode> gcodes, string dxfFileName) {
        return 
            (start2End ? _geometry : _geometry.CloneReversed()).EmitGCode(currPos, t, globalS_mm, gcodes, dxfFileName,
            millingTarget_mm: Math.Max(millingLayer_mm, Bottom_mm),
            t_mm: _params!.T_mm, f_mmpmin: _params!.F_mmpmin, backtracking: Order == PathModel.BACKTRACK_ORDER);
    }
}

public abstract class AbstractSweepSegment : PathSegmentWithParamsText, IRawSegment {
    public int Preference => 5;

    private Vector2 _start;
    private Vector2 _end;

    protected AbstractSweepSegment(EntityObject source, ParamsText pars, Vector2 start, Vector2 end) : base(source, pars) {
        _start = start;
        _end = end;
    }

    public override Vector2 Start => _start;
    public override Vector2 End => _end;

    public abstract double Order { get; }

    public void Reverse() {
        (_start, _end) = (_end, _start);
    }

    public IRawSegment ReversedSegmentAfterTurn()
        => new BackSweepSegment(Source, ParamsText, _end, _start);

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                                      List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        AssertNear(currPos.XY(), t.Transform(Start), ErrorContext(dxfFileName));

        Vector2 target = t.Transform(End);
        double s_mm = _params!.S_mm;
        GCodeHelpers.SweepAndDrillSafelyFromTo(currPos, target.AsVector3(s_mm), t_mm: _params!.T_mm,
                                               sk_mm: s_mm, globalS_mm: globalS_mm, f_mmpmin: _params!.F_mmpmin,
                                               backtracking: Order == PathModel.BACKTRACK_ORDER, t, gcodes);
        return target.AsVector3(_params!.S_mm);
    }
}

public class SweepSegment : AbstractSweepSegment {
    public override double Order { get; }

    public SweepSegment(EntityObject source, ParamsText pars, Vector2 start, Vector2 end, double order) : base(source, pars, start, end) {
        Order = order;
    }

    public override void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new SweepParams(ParamsText, ErrorContext(dxfFileName), pathParams, onError);
    }
}

public class BackSweepSegment : AbstractSweepSegment {
    public override double Order
        => PathModel.BACKTRACK_ORDER;

    public BackSweepSegment(EntityObject source, ParamsText pars, Vector2 start, Vector2 end) : base(source, pars, start, end) {
    }

    public override void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new BackSweepParams(ParamsText, ErrorContext(dxfFileName), pathParams, onError);
    }
}

public abstract class PathMarkOrMillSegment : PathSegmentWithParamsText, IMarkOrMillSegment {
    public bool IsMark { get; }

    protected PathMarkOrMillSegment(EntityObject source, ParamsText paramsText, bool isMark) : base(source, paramsText) {
        IsMark = isMark;
    }

    public double Bottom_mm
        => IsMark ? _params!.D_mm : _params!.B_mm;
}

public class HelixSegment : PathMarkOrMillSegment, IRawSegment {
    public int Preference => 2;
    public Vector2 Center { get; }
    public double Radius_mm { get; }

    public HelixSegment(EntityObject source, ParamsText pars, Vector2 center, double radius_mm, bool isMark) : base(source, pars, isMark) {
        Center = center;
        Radius_mm = radius_mm;
    }

    // Multiple helixes at the same place must be milled from inside out (to avoid cores
    // that tumble around), this those with a smaller radius must have a lower order.
    // Moreover, the order must be "very negative" so that they are far before the N orders.
    public double Order => -1000000 + Radius_mm;

    public override Vector2 Start => Center;
    public override Vector2 End => Center;

    public void Reverse() {
        // empty
    }

    public IRawSegment ReversedSegmentAfterTurn() => new BackSweepSegment(Source, ParamsText, Center, Center);

    public override void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new HelixParams(ParamsText, IsMark, ErrorContext(dxfFileName), pathParams, onError);
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        Vector2 c = t.Transform(Center);
        AssertNear(currPos.XY(), c, MessageHandlerForEntities.Context(Source, Center, dxfFileName));
        double t_mm = _params!.T_mm;
        double i_mm = _params!.I_mm;
        double f_mmpmin = _params!.F_mmpmin;
        double bottom_mm = Bottom_mm;

        double millingRadius_mm = Radius_mm - _params!.O_mm / 2;
        // Milling many small semicircles
        double y0 = c.Y - millingRadius_mm;
        double y1 = c.Y + millingRadius_mm;

        GCodeHelpers.DrillOrPullZFromTo(currPos.XY(), currPos.Z, t_mm, t_mm: t_mm, f_mmpmin: f_mmpmin, t, gcodes);
        gcodes.AddComment($"MillHelix l={c.F3()} r={Radius_mm.F3()}", 2);
        gcodes.AddMill($"G01 F{f_mmpmin.F3()} X{c.X.F3()} Y{y0.F3()}", Math.Abs(c.Y - y0), f_mmpmin); // G01, as we touch the top

        // This will not create full holes - a core will remain; paths for full holes must be manually drawn.
        double done_mm = t_mm;
        for (double d_mm = t_mm; done_mm > bottom_mm; d_mm -= i_mm) {
            gcodes.AddComment($"MillSemiCircle l={d_mm.F3()}", 4);

            double b1_mm = Math.Max(d_mm - i_mm / 2, bottom_mm);
            gcodes.AddMill($"G02 F{f_mmpmin.F3()} I0 J{millingRadius_mm.F3()} X{c.X.F3()} Y{y1.F3()} Z{t.Expr(b1_mm, c)}", millingRadius_mm * Math.PI, f_mmpmin);

            double b0_mm = Math.Max(b1_mm - i_mm / 2, bottom_mm);
            gcodes.AddMill($"G02 F{f_mmpmin.F3()} I0 J{(-millingRadius_mm).F3()} X{c.X.F3()} Y{y0.F3()} Z{t.Expr(b0_mm, c)}", millingRadius_mm * Math.PI, f_mmpmin);

            done_mm = d_mm; // Spirale von millingLayer_mm nach b0_mm gefräst = nur bis millingLayer_mm ist Loch fertig!
        }
        if (Radius_mm <= _params!.O_mm) {
            // If radius <= O, then there is no core in the center --> we can sweep straight to the center
            gcodes.AddHorizontalG00(c, millingRadius_mm, bottom_mm, globalS_mm);

            return c.AsVector3(bottom_mm);
        } else {
            // Otherwise, first pull up to S (to avoid core), then sweep to lastPos.
            double s_mm = _params!.S_mm;
            gcodes.AddNonhorizontalG00($"G00 Z{t.Expr(s_mm, c)}", millingRadius_mm);
            gcodes.AddHorizontalG00(c, millingRadius_mm, s_mm, globalS_mm);

            return c.AsVector3(s_mm);
        }
    }
}

public class DrillSegment : PathMarkOrMillSegment, IRawSegment {
    public Vector2 Center { get; }
    // See HelixSegment
    public double Order => -1000000;
    public int Preference => 1;

    public DrillSegment(EntityObject source, ParamsText pars, Vector2 center, bool isMark) : base(source, pars, isMark) {
        Center = center;
    }

    public override Vector2 Start => Center;
    public override Vector2 End => Center;

    public void Reverse() {
        // empty
    }

    public IRawSegment ReversedSegmentAfterTurn() => new BackSweepSegment(Source, ParamsText, Center, Center);

    public override void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        _params = new DrillParams(ParamsText, IsMark, ErrorContext(dxfFileName), pathParams, onError);
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm, List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        Vector2 c = t.Transform(Center);
        AssertNear(currPos.XY(), c, MessageHandlerForEntities.Context(Source, Center, dxfFileName));

        gcodes.AddComment($"Drill l={c.F3()}", 2);
        double bottom_mm = IsMark ? _params!.D_mm : _params!.B_mm;
        GCodeHelpers.DrillOrPullZFromTo(currPos.XY(), currPos.Z, bottom_mm, t_mm: _params!.T_mm, f_mmpmin: _params.F_mmpmin, t, gcodes);
        return c.AsVector3(bottom_mm);
    }
}

public class SubPathSegment : PathSegmentWithParamsText, IRawSegment {
    public int Preference => 3;

    private readonly string _overlayTextForErrors;
    private readonly PathName _name;
    private PathModel? _model;
    private Vector2 _start;
    private Vector2 _end;

    public SubPathSegment(EntityObject source, ParamsText text, Vector2 start, Vector2 end, string pathNamePattern,
            double order, string dxfFilePath, Action<string> onError) : base(source, text) {
        _start = start;
        _end = end;
        _overlayTextForErrors = $"{text.Text} ({text.Context})";
        Order = order;
        string? path = text.GetString('>');
        if (path == null) {
            throw new ArgumentException(MessageHandlerForEntities.Context(source, start, dxfFilePath) + ": " + Messages.PathSegment_GtMissing);
        } else {
            _name = path.AsPathReference(pathNamePattern, dxfFilePath)
                ?? throw new ArgumentException(string.Format(MessageHandlerForEntities.Context(source, start, dxfFilePath) +
                    ": " + Messages.PathSegment_InvalidPathName_Dir_Path, '>', path));
        }
    }

    public override Vector2 Start => _start;
    public override Vector2 End => _end;

    public double Order { get; }

    public void Reverse() {
        (_start, _end) = (_end, _start);
    }

    public IRawSegment ReversedSegmentAfterTurn()
        => new BackSweepSegment(Source, ParamsText, _end, _start);

    internal void Load(PathModelCollection subModels, string currentDxfFile, Options options, MessageHandlerForEntities messages) {
        if (!subModels.Contains(_name)) {
            string searchedFiles = "";
            foreach (var directory in options.DirAndSearchDirectories(Path.GetDirectoryName(Path.GetFullPath(currentDxfFile)))) {
                string[] dxfFiles = Directory.GetFiles(directory, "*.dxf");
                foreach (var f in dxfFiles) {
                    if (FileNameMatchesPathName(Path.GetFileNameWithoutExtension(f), _name, options.PathNamePattern)) {
                        subModels.Load(f, options, _overlayTextForErrors, messages);
                        searchedFiles += (searchedFiles == "" ? "" : ", ") + f;
                        break;
                    }
                    // Load all matching files! - we want to know whether the path might be defined more than once.
                }
            }
            if (!subModels.Contains(_name)) {
                messages.AddError(_overlayTextForErrors, Messages.PathSegment_PathNotFound_Name_Files, _name.AsString(), searchedFiles);
                return;
            }
        }
        _model = subModels.Get(_name);

        double modelSize = _model.Start.Distance(_model.End);
        double referenceSize = Start.Distance(End);
        if (!modelSize.Near(referenceSize, 1e-3)) {
            messages.AddError(_overlayTextForErrors, Messages.PathSegment_DistanceDiffers_CallerDist_CalledDist, referenceSize.F3(), modelSize.F3());
        }
    }

    public static bool FileNameMatchesPathName(string fileName, PathName path, string pathNamePattern) {
        foreach (var m in Regex.Matches(fileName, $"(?<p1>{pathNamePattern})-(?<p2>{pathNamePattern})").ToArray()) {
            string from = m.Groups["p1"].Value;
            string to = m.Groups["p2"].Value;
            if (PathName.CompareFileNameToPathName(from, path, pathNamePattern) <= 0
                && PathName.CompareFileNameToPathName(to, path, pathNamePattern) >= 0) {
                return true;
            }
        }
        foreach (var m in Regex.Matches(fileName, pathNamePattern).ToArray()) {
            if (PathName.CompareFileNameToPathName(m.Value, path, pathNamePattern) == 0) {
                return true;
            }
        }
        return false;
    }

    public override void CreateParams(PathParams pathParams, string dxfFileName, Action<string, string> onError) {
        if (_model != null) { // Error "path not found" has already been emitted.
            string errorContext = ErrorContext(dxfFileName);
            _params = new SubpathParams(ParamsText, errorContext, pathParams, onError);
            if (_params.M != _model.Params.M) {
                onError(errorContext, string.Format(Messages.PathSegment_DifferingM_Caller_Path_Called, _params.M, _model.Name, _model.Params.M));
            }
            if (_params.O_mm != _model.Params.O_mm) {
                onError(errorContext, string.Format(Messages.PathSegment_DifferingO_Caller_Path_Called, _params.O_mm, _model.Name, _model.Params.O_mm));
            }
        }
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        Transformation3 compound = t.Transform3(new Transformation2(_model!.Start, _model.End, _start, _end));
        gcodes.AddComment($"START Subpath {_name} t={compound}", 2);
        currPos = _model.EmitMillingGCode(currPos, compound, globalS_mm, gcodes, _model.DxfFilePath, messages);
        gcodes.AddComment($"END Subpath {_name} t={compound}", 2);
        return currPos;
    }
}

