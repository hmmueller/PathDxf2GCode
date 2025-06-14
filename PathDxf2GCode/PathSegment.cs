namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using netDxf.Entities;
using System.Collections.Generic;

public enum MillType { Mill, Mark, WithSupports }

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

    ILeafSegment CreateSegment();
}

public abstract class PathSegment {
    public abstract Vector2 Start { get; }
    public abstract Vector2 End { get; }

    public abstract void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError);

    public static void AssertNear(Vector2 a, Vector2 b, string errorContext) {
        GeometryHelpers.Assert(a.Near(b), errorContext, $"!{a.F3()}.Near({b.F3()})");
    }

    public abstract Vector3 EmitGCode(Vector3 currPos, Transformation3 zCorr, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages);
}

public abstract class PathSegment<TRawSegment> : PathSegment where TRawSegment : PathSegment<TRawSegment>.RawSegment {
    protected abstract TRawSegment Raw { get; }
    public abstract class RawSegment {
        public abstract Vector2 Start { get; }
        public abstract Vector2 End { get; }
    }
    public sealed override Vector2 Start => Raw.Start;
    public sealed override Vector2 End => Raw.End;

}

public abstract class PathSegmentWithParamsText<TRawSegment, TParams> : PathSegment<TRawSegment>
        where TRawSegment : PathSegmentWithParamsText<TRawSegment, TParams>.RawSegment
        where TParams : IParams {
    public new abstract class RawSegment : PathSegment<TRawSegment>.RawSegment {
        protected RawSegment(EntityObject source, ParamsText pars) {
            Source = source;
            ParamsText = pars;
        }

        public EntityObject Source { get; }
        public ParamsText ParamsText { get; }
    }

    public EntityObject Source => Raw.Source;
    public ParamsText ParamsText => Raw.ParamsText;
    protected TParams? _params;

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

    public override void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        // [0] looks innocent, but with branching mill chains the "first" segment may not be easily identifiable by the user ...
        _params = new ChainParams(_segments[0].ParamsText, superpathVariables,
            MessageHandlerForEntities.Context(_segments[0].Source, _segments[0].Start, dxfFileName), pathParams, onError);
        foreach (var s in _segments) {
            s.CreateParams(_params, superpathVariables, dxfFileName, onError);
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

    private static EdgeMilled AddEdgeForChainWithSlowMilling(List<Edge> edgesForThisRun, Edge e, Transformation3 t, ref Vector3 headPos) {
        Vector3 start = e.Start(t);
        Vector3 end = e.End(t);

        edgesForThisRun.Add(e);

        if (headPos.Vector2Near(start)) {
            headPos = end;
            return EdgeMilled.Start2End;
        } else if (headPos.Vector2Near(end)) {
            headPos = start;
            return EdgeMilled.End2Start;
        } else {
            throw new Exception("Internal error");
        }
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                                      List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        // A. Create milling movements ("edges") from segments
        List<List<Edge>> edgesBySegment = new();
        {
            double firstMillingBottom_mm = _params!.W_mm ?? _params!.T_mm - _params!.I_mm;
            foreach (var s in _segments) {
                List<Edge> edgesOfS = new();
                Edge? prev = default;
                for (double millingBottom_mm = firstMillingBottom_mm; millingBottom_mm.Ge(s.Bottom_mm); millingBottom_mm -= _params!.I_mm) {
                    edgesOfS.Add(prev = new Edge(s, millingBottom_mm, prev));
                }
                if (prev == null || !prev.MillingBottom_mm.Near(s.Bottom_mm)) {
                    edgesOfS.Add(new Edge(s, s.Bottom_mm, prev));
                }
                edgesBySegment.Add(edgesOfS);
            }
        }
        List<Edge> sortedEdges = new();

        // B. Create optimized order
        Vector3 headPos = edgesBySegment.First().First().Start(t);
        if (!headPos.XY().AbsNear(currPos.XY(), 1e-3)) {
            throw new Exception("Internal error");
        }

        if (_params.W_mm.HasValue) {
            // "Slow Milling", i.e. milling without sweeps; especially for T bits.

            int runCt = edgesBySegment.Max(es => es.Count);

            // "Backmilling": If there is more than one run, but Start is near End,
            // then Slow Milling can simply continue with the next layer of edges.
            // If, however, Start differs from End, milling has to return
            // to the start by tracing back all the milled edges.
            bool requiresBackMilling = runCt > 1 && !Start.Near(End);

            for (int run = 0; run < runCt; run++) {
                List<Edge> edgesForThisRun = new();
                foreach (var es in edgesBySegment) {
                    if (run < es.Count) {
                        // There is still an edge to be milled on this run for this segment.
                        Edge e = es[run];
                        e.Milled = AddEdgeForChainWithSlowMilling(edgesForThisRun, e, t, ref headPos);
                    } else {
                        // All edges have been milled for this segment; to continue, the
                        // bottom-most milled edge is re-traced.
                        Edge e = es.Last();
                        AddEdgeForChainWithSlowMilling(edgesForThisRun, e, t, ref headPos);
                        // e.Milled is already set
                    }
                }

                sortedEdges.AddRange(edgesForThisRun);

                if (requiresBackMilling && run < runCt - 1) {
                    foreach (var e in edgesForThisRun.Reverse<Edge>()) {
                        Edge reversedEdge = new(e.Segment, e.MillingBottom_mm, null) {
                            Milled = e.Milled == EdgeMilled.Start2End ? EdgeMilled.End2Start : EdgeMilled.Start2End
                        };
                        sortedEdges.Add(reversedEdge);
                        headPos = reversedEdge.Milled == EdgeMilled.Start2End ? reversedEdge.End(t) : reversedEdge.Start(t);
                    }
                }
            }

            // Create Gcode
            foreach (var e in sortedEdges) {
                currPos = GCodeHelpers.DrillOrPullZFromTo(currPos, target: e.Milled == EdgeMilled.Start2End ? e.Start(t) : e.End(t),
                    t_mm: _params!.T_mm, _params!.F_mmpmin, t, gcodes);
                currPos = e.Segment.EmitGCode(currPos, e.MillingBottom_mm, e.Milled == EdgeMilled.Start2End,
                    globalS_mm, t, gcodes, dxfFileName);
            }

        } else {
            // "Fast Milling"

            IEnumerable<Edge> edges = edgesBySegment.SelectMany(es => es);
            int remaining = edges.Count();
            while (remaining > 0) {
                Edge? nearest = null;
                bool nearestConnectAtStart = true;
                Vector3 NearestMillingTip() => nearestConnectAtStart ? nearest!.Start(t) : nearest!.End(t);

                foreach (var candidate in edges.Where(e => e.TopUnmilled)) {
                    double distToCandidateStart_mm = headPos.Distance(candidate.Start(t));
                    double distToCandidateEnd_mm = headPos.Distance(candidate.End(t));
                    bool candStartIsNearer = distToCandidateStart_mm.Lt(distToCandidateEnd_mm);
                    double distToNearerCandidateTip = candStartIsNearer ? distToCandidateStart_mm : distToCandidateEnd_mm;
                    if (nearest == null || distToNearerCandidateTip.Lt(headPos.Distance(NearestMillingTip()))) {
                        nearest = candidate;
                        nearestConnectAtStart = candStartIsNearer;
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

            // Create Gcode
            double s_mm = _params!.S_mm;
            foreach (var e in sortedEdges) {
                currPos = GCodeHelpers.SweepAndDrillSafelyFromTo(from: currPos,
                    to: e.Milled == EdgeMilled.Start2End ? e.Start(t) : e.End(t),
                    t_mm: _params!.T_mm, s_mm: s_mm, globalS_mm, _params!.F_mmpmin,
                    backtracking: false, t, gcodes);

                currPos = e.Segment.EmitGCode(currPos, e.MillingBottom_mm, e.Milled == EdgeMilled.Start2End,
                    globalS_mm, t, gcodes, dxfFileName);
            }

            Vector2 end = t.Transform(_segments.Last().End);
            currPos = GCodeHelpers.SweepAndDrillSafelyFromTo(from: currPos, to: end.AsVector3(s_mm),
                t_mm: _params!.T_mm, s_mm: s_mm, globalS_mm, _params!.F_mmpmin, backtracking: false, t, gcodes);
            AssertNear(currPos.XY(), end, MessageHandlerForEntities.Context(_segments.Last().Source, _segments.First().Start, dxfFileName));
        }

        return currPos;
    }
}

public interface ILeafSegment {
    // Marker interface
}

public class ChainSegment : ILeafSegment {
    protected RawSegment Raw { get; }

    public class RawSegment : IRawSegment {
        public EntityObject Source { get; }
        public ParamsText ParamsText { get; }
        public MillType MillType { get; }
        public double Order { get; }
        public IMillGeometry Geometry { get; private set; }
        public Vector2 Start => Geometry.Start;
        public Vector2 End => Geometry.End;

        public int Preference => 4;

        public RawSegment(IMillGeometry geometry, MillType millType, EntityObject source, ParamsText paramsText, double order) {
            Geometry = geometry;
            MillType = millType;
            Source = source;
            Order = order;
            ParamsText = paramsText;
        }

        public void Reverse() {
            Geometry = Geometry.CloneReversed();
        }

        public IRawSegment ReversedSegmentAfterTurn()
            => ReversedSegment();

        public RawSegment ReversedSegment()
            => new RawSegment(Geometry.CloneReversed(), MillType, Source, ParamsText, PathModel.BACKTRACK_ORDER);

        public ILeafSegment CreateSegment() => new ChainSegment(this);
    }

    private IParams? _params;
    private IMillGeometry[]? _supportGeometries;

    private ChainSegment(RawSegment raw) {
        Raw = raw;
    }

    public double Bottom_mm
        => MillType == MillType.Mark ? _params!.D_mm : _params!.B_mm;


    public Vector2 Start => Raw.Start;
    public Vector2 End => Raw.End;
    public MillType MillType => Raw.MillType;
    public EntityObject Source => Raw.Source;
    public ParamsText ParamsText => Raw.ParamsText;
    private IMillGeometry Geometry => Raw.Geometry;
    public double Order => Raw.Order;

    internal void CreateParams(ChainParams chainParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        _params = new MillParams(ParamsText, superpathVariables, MillType, MessageHandlerForEntities.Context(Source, Start, dxfFileName), chainParams, onError);

        // Also create geometries for support bars; done here once because
        // EmitGCode is called for each milling depth, but the geometries do not change.
        _supportGeometries = MillType == MillType.WithSupports
            ? Geometry.CreateSupportBarGeometries(_params.O_mm, _params.P_mm, _params.U_mm, _params.D_mm - _params.B_mm)
            : [];
    }

    internal Vector3 EmitGCode(Vector3 currPos, double millingLayer_mm, bool start2End, double globalS_mm,
                                Transformation3 t, List<GCode> gcodes, string dxfFileName) {
        IParams pars = _params!;
        double fullMillBottom = MillType == MillType.Mill ? pars.B_mm : pars.D_mm;
        bool backtracking = Order == PathModel.BACKTRACK_ORDER;
        string errorContext = MessageHandlerForEntities.Context(Source, Start, dxfFileName);
        currPos = _supportGeometries!.Any() && millingLayer_mm.Lt(fullMillBottom)
            ? (start2End
                ? _supportGeometries!.MillSupportsStart2End(currPos, millingLayer_mm,
                        globalS_mm, t, gcodes, dxfFileName, pars, backtracking)
                : _supportGeometries!.MillSupportsEnd2Start(currPos, millingLayer_mm,
                        globalS_mm, t, gcodes, dxfFileName, pars, backtracking))
            : (start2End ? Geometry : Geometry.CloneReversed()).EmitGCode(currPos, t, globalS_mm, gcodes,
                        fromZ_mm: Math.Max(millingLayer_mm, fullMillBottom),
                        toZ_mm: Math.Max(millingLayer_mm, fullMillBottom),
                        t_mm: pars.T_mm, f_mmpmin: pars.F_mmpmin, backtracking);
        return currPos;
    }
}

public abstract class AbstractSweepSegment<TRawSegment> : PathSegmentWithParamsText<TRawSegment, IParams>, ILeafSegment
    where TRawSegment : AbstractSweepSegment<TRawSegment>.RawSegment {

    public new abstract class RawSegment : PathSegmentWithParamsText<TRawSegment, IParams>.RawSegment {
        private Vector2 _start;
        private Vector2 _end;

        public override Vector2 Start => _start;
        public override Vector2 End => _end;

        protected RawSegment(EntityObject source, ParamsText pars, Vector2 start, Vector2 end) : base(source, pars) {
            _start = start;
            _end = end;
        }

        public void Reverse() {
            (_start, _end) = (_end, _start);
        }

        public abstract double Order { get; }

        public int Preference => 5;

        public IRawSegment ReversedSegmentAfterTurn()
            => new BackSweepSegment.RawSegment(Source, ParamsText, End, Start);
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
                                      List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        AssertNear(currPos.XY(), t.Transform(Start), MessageHandlerForEntities.Context(Source, Start, dxfFileName));

        Vector2 target = t.Transform(End);
        double s_mm = _params!.S_mm;
        GCodeHelpers.SweepAndDrillSafelyFromTo(currPos, target.AsVector3(s_mm), t_mm: _params!.T_mm,
                                               s_mm: s_mm, globalS_mm: globalS_mm, f_mmpmin: _params!.F_mmpmin,
                                               backtracking: Raw.Order == PathModel.BACKTRACK_ORDER, t, gcodes);
        return target.AsVector3(_params!.S_mm);
    }
}

public class SweepSegment : AbstractSweepSegment<SweepSegment.RawSegment> {
    public new class RawSegment : AbstractSweepSegment<RawSegment>.RawSegment, IRawSegment {
        public override double Order { get; }

        public RawSegment(EntityObject source, ParamsText pars, Vector2 start, Vector2 end, double order) : base(source, pars, start, end) {
            Order = order;
        }

        public ILeafSegment CreateSegment() => new SweepSegment(this);
    }

    protected override RawSegment Raw { get; }

    private SweepSegment(RawSegment raw) {
        Raw = raw;
    }

    public override void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        _params = new SweepParams(ParamsText, superpathVariables, MessageHandlerForEntities.Context(Source, Start, dxfFileName), pathParams, onError);
    }
}

public class BackSweepSegment : AbstractSweepSegment<BackSweepSegment.RawSegment> {
    public new class RawSegment : AbstractSweepSegment<RawSegment>.RawSegment, IRawSegment {
        public override double Order
        => PathModel.BACKTRACK_ORDER;

        public RawSegment(EntityObject source, ParamsText pars, Vector2 start, Vector2 end) : base(source, pars, start, end) {
        }

        public ILeafSegment CreateSegment() => new BackSweepSegment(this);
    }

    protected override RawSegment Raw { get; }

    private BackSweepSegment(RawSegment raw) {
        Raw = raw;
    }

    public override void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        _params = new BackSweepParams(ParamsText, superpathVariables, MessageHandlerForEntities.Context(Source, Start, dxfFileName), pathParams, onError);
    }
}

public abstract class MarkOrMillPathSegment<TRawSegment> : PathSegmentWithParamsText<TRawSegment, IParams>, ILeafSegment
        where TRawSegment : MarkOrMillPathSegment<TRawSegment>.RawSegment {
    public new abstract class RawSegment : PathSegmentWithParamsText<TRawSegment, IParams>.RawSegment {
        protected RawSegment(EntityObject source, ParamsText paramsText, bool isMark) : base(source, paramsText) {
            IsMark = isMark;
        }

        public bool IsMark { get; }
    }
}

public class HelixSegment : MarkOrMillPathSegment<HelixSegment.RawSegment> {
    public new class RawSegment : MarkOrMillPathSegment<RawSegment>.RawSegment, IRawSegment {
        public Vector2 Center { get; }
        public double Radius_mm { get; }
        public MillType MillType { get; }
        public override Vector2 Start => Center;
        public override Vector2 End => Center;

        // Multiple helixes at the same place must be milled from inside out (to avoid cores
        // that tumble around), thus those with a smaller radius must have a lower order.
        // Moreover, the order must be "very negative" so that they are far before the N orders.
        public double Order => -1000000 + Radius_mm;

        public int Preference => 2;

        public RawSegment(EntityObject source, ParamsText pars, Vector2 center, double radius_mm, MillType millType) : base(source, pars, millType == MillType.Mark) {
            Center = center;
            Radius_mm = radius_mm;
            MillType = millType;
        }

        public IRawSegment ReversedSegmentAfterTurn() => new BackSweepSegment.RawSegment(Source, ParamsText, Center, Center);

        public ILeafSegment CreateSegment() => new HelixSegment(this);

        public void Reverse() {
            // empty
        }
    }

    private IMillGeometry[]? _supportGeometries;

    private MillType MillType => Raw.MillType;
    private Vector2 Center => Raw.Center;
    private double Radius_mm => Raw.Radius_mm;

    protected override RawSegment Raw { get; }

    private HelixSegment(RawSegment raw) {
        Raw = raw;
    }

    public override void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        string errorContext = MessageHandlerForEntities.Context(Source, Center, dxfFileName);
        _params = new HelixParams(ParamsText, superpathVariables, MillType, errorContext, pathParams, onError);
        if ((2 * Radius_mm).Gt(_params.A_mm)) {
            onError(errorContext, string.Format(Messages.PathSegment_DiameterGtA_Diameter_A, 2 * Radius_mm, _params.A_mm));
        }
        // Also create geometries for support bars; done here once because
        // EmitGCode is called for each milling depth, but the geometries do not change.
        // The arc is oriented clockwise, as the helix is milled with G02, i.e. clockwise semicircles.
        // It starts and ends at 270 deg, as the semicircles also start there.
        IMillGeometry fullArc = new ArcGeometry(Center, Radius_mm - _params.O_mm / 2, 270,
                                                270 * (1 + GeometryHelpers.RELATIVE_EPS), counterclockwise: false);
        _supportGeometries = MillType == MillType.WithSupports
            ? fullArc.CreateSupportBarGeometries(_params.O_mm, _params.P_mm, _params.U_mm, _params.D_mm - _params.B_mm)
            : [];
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        Vector2 c = t.Transform(Center);
        string errorContext = MessageHandlerForEntities.Context(Source, Center, dxfFileName);
        AssertNear(currPos.XY(), c, errorContext);

        IParams pars = _params!;
        double fullMillBottom_mm = MillType == MillType.Mill ? pars.B_mm : pars.D_mm;

        double t_mm = pars.T_mm;
        double i_mm = pars.I_mm;
        double o_mm = pars.O_mm;
        double f_mmpmin = pars.F_mmpmin;

        double millingRadius_mm = Radius_mm - pars.O_mm / 2;
        // Milling many small semicircles
        double y0 = c.Y - millingRadius_mm;
        double y1 = c.Y + millingRadius_mm;

        // We lift to T+O, i.e. "somewhat above T".
        GCodeHelpers.DrillOrPullZFromTo(currPos.XY(), currPos.Z, t_mm + o_mm, t_mm: t_mm, f_mmpmin: f_mmpmin, t, gcodes);
        gcodes.AddComment($"MillHelix l={c.F3()} r={Radius_mm.F3()}", 2);
        gcodes.AddHorizontalG00(new Vector2(c.X, y0), Math.Abs(c.Y - y0));

        // First, we mill as long as we can mill complete circles (actually,two semicircles).
        double done_mm = t_mm;
        for (double d_mm = t_mm; done_mm.Gt(fullMillBottom_mm); d_mm -= i_mm) {
            gcodes.AddComment($"MillSemiCircle l={d_mm.F3()}", 4);

            double b1_mm = Math.Max(d_mm - i_mm / 2, fullMillBottom_mm);
            gcodes.AddMill($"G02 F{f_mmpmin.F3()} I0 J{millingRadius_mm.F3()} X{c.X.F3()} Y{y1.F3()} Z{t.Expr(b1_mm, c)}", millingRadius_mm * Math.PI, f_mmpmin);

            double b0_mm = Math.Max(b1_mm - i_mm / 2, fullMillBottom_mm);
            gcodes.AddMill($"G02 F{f_mmpmin.F3()} I0 J{(-millingRadius_mm).F3()} X{c.X.F3()} Y{y0.F3()} Z{t.Expr(b0_mm, c)}", millingRadius_mm * Math.PI, f_mmpmin);

            done_mm = d_mm; // We can only guarantee that depth d_mm has been reached;
                            // all lower depths might have been reached only in some parts of the semicircles.
            currPos = new(c.X, y0, b0_mm);
        }

        // Now, if necessary, we mill the support bar section.
        if (_supportGeometries!.Any()) {
            for (double d_mm = done_mm; done_mm.Gt(pars.B_mm); d_mm -= i_mm) {
                double b_mm = Math.Max(d_mm - i_mm, pars.B_mm);
                currPos = _supportGeometries!.MillSupportsStart2End(currPos, millingBottom_mm: b_mm,
                    globalS_mm, t, gcodes, errorContext, pars, backtracking: false);
                done_mm = b_mm;
            }
        }

        if (Radius_mm.Le(pars.O_mm)) {
            // If radius <= O, then there is no core in the center --> we can sweep straight to the center
            gcodes.AddHorizontalG00(c, lg_mm: millingRadius_mm);

            currPos = c.AsVector3(currPos.Z);
        } else {
            // Otherwise, first pull up to S (to avoid core), then sweep to lastPos.
            double s_mm = pars.S_mm;
            gcodes.AddNonhorizontalG00($"G00 Z{t.Expr(s_mm, c)}", lg_mm: s_mm - done_mm);
            gcodes.AddHorizontalG00(c, lg_mm: millingRadius_mm);

            currPos = c.AsVector3(s_mm);
        }

        return currPos;
    }
}

public class DrillSegment : MarkOrMillPathSegment<DrillSegment.RawSegment> {
    public new class RawSegment : MarkOrMillPathSegment<RawSegment>.RawSegment, IRawSegment {
        public Vector2 Center { get; }
        // See HelixSegment
        public double Order => -1000000;
        public int Preference => 1;

        public override Vector2 Start => Center;
        public override Vector2 End => Center;

        public RawSegment(EntityObject source, ParamsText pars, Vector2 center, bool isMark) : base(source, pars, isMark) {
            Center = center;
        }

        public void Reverse() {
            // empty
        }

        public IRawSegment ReversedSegmentAfterTurn() => new RawSegment(Source, ParamsText, Center, IsMark);

        public ILeafSegment CreateSegment() => new DrillSegment(this);
    }

    private Vector2 Center => Raw.Center;
    private bool IsMark => Raw.IsMark;

    protected override RawSegment Raw { get; }

    private DrillSegment(RawSegment raw) {
        Raw = raw;
    }

    public override void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        _params = new DrillParams(ParamsText, superpathVariables, IsMark, MessageHandlerForEntities.Context(Source, Center, dxfFileName), pathParams, onError);
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

public class SubPathSegment : PathSegmentWithParamsText<SubPathSegment.RawSegment, SubpathParams>, ILeafSegment {
    public new class RawSegment : PathSegmentWithParamsText<RawSegment, SubpathParams>.RawSegment, IRawSegment {
        public int Preference => 3;

        public readonly PathModel.Collection Models;
        private readonly string _dxfFilePath;

        public Options Options { get; }

        private Vector2 _start, _end;
        public override Vector2 Start => _start;
        public override Vector2 End => _end;
        public double Order { get; }

        public RawSegment(EntityObject source, ParamsText text, Vector2 start, Vector2 end, Options options,
            double order, PathModel.Collection models, string dxfFilePath) : base(source, text) {
            _start = start;
            _end = end;
            Models = models;
            _dxfFilePath = dxfFilePath;
            Options = options;
            Order = order;
        }

        public void Reverse() {
            (_start, _end) = (_end, _start);
        }

        public IRawSegment ReversedSegmentAfterTurn()
            => new RawSegment(Source, ParamsText, End, Start, Options, Order, Models, _dxfFilePath);

        public ILeafSegment CreateSegment() => new SubPathSegment(this);
    }

    protected override RawSegment Raw { get; }

    public SubPathSegment(RawSegment raw) {
        Raw = raw;
    }

    private Options Options => Raw.Options;

    private PathModel? _targetModel;

    public override void CreateParams(PathParams pathParams, ActualVariables superpathVariables, string dxfFileName, Action<string, string> onError) {
        string errorContext = MessageHandlerForEntities.Context(Source, Start, dxfFileName);
        _params = new SubpathParams(ParamsText, superpathVariables, errorContext, pathParams, onError);
    }

    public void ConnectModel(string dxfFileName, MessageHandlerForEntities messages, int nestingDepth) {
        string errorContext = MessageHandlerForEntities.Context(Source, Start, dxfFileName);

        string? path = _params!.GetString('>');
        PathName name;
        if (path == null) {
            throw new EmitGCodeException(errorContext, Messages.PathSegment_GtMissing);
        } else {
            name = path.AsPathReference(Options.PathNamePattern, dxfFileName)
                ?? throw new EmitGCodeException(errorContext,
                            string.Format(Messages.PathSegment_InvalidPathName_Dir_Path, '>', path));
        }

        if (nestingDepth > 9) {
            throw new EmitGCodeException(errorContext, string.Format(Messages.PathSegment_CallDepthGt9_Path, name));
        }

        _targetModel = Raw.Models.Load(name, _params!.ActualVariables, defaultSorNullForTplusO_mm: null, dxfFileName, Options, $"{Raw.ParamsText.Text} ({Raw.ParamsText.Context})", messages, nestingDepth + 1, out string searchedFiles);

        if (_targetModel == null) {
            messages.AddError(Source, End, dxfFileName, Messages.PathSegment_PathNotFound_Name_Files,
                              name.AsString(), searchedFiles);
        } else {
            double modelSize = _targetModel.Start.Distance(_targetModel.End);
            double referenceSize = Start.Distance(End);
            if (!modelSize.AbsNear(referenceSize, 1e-3)) {
                messages.AddError(Source, End, dxfFileName,
                    Messages.PathSegment_DistanceDiffers_CallerDist_ModelName_CalledDist,
                    referenceSize.F3(), _targetModel.Name, modelSize.F3());
            }

            if (_params!.M != _targetModel.Params.M) {
                messages.AddError(dxfFileName, Messages.PathSegment_DifferingM_Caller_Path_Called, _params.M, _targetModel.Name, _targetModel.Params.M);
            }
            if (_params.O_mm != _targetModel.Params.O_mm) {
                messages.AddError(dxfFileName, Messages.PathSegment_DifferingO_Caller_Path_Called, _params.O_mm, _targetModel.Name, _targetModel.Params.O_mm);
            }
            _targetModel.Params.FormalVariables.CheckActualVariables(_params.ActualVariables, msg => messages.AddError(errorContext, msg));
        }
    }

    public override Vector3 EmitGCode(Vector3 currPos, Transformation3 t, double globalS_mm,
        List<GCode> gcodes, string dxfFileName, MessageHandlerForEntities messages) {
        if (_targetModel != null) {
            PathName name = _targetModel.Name;
            Transformation3 compound = t.Transform3(new Transformation2(_targetModel!.Start, _targetModel.End, Start, End));
            gcodes.AddComment($"START Subpath {name} t={compound}", 2);
            currPos = _targetModel.EmitMillingGCode(currPos, compound, globalS_mm, gcodes, _targetModel.DxfFilePath, messages);
            gcodes.AddComment($"END Subpath {name} t={compound}", 2);
        }
        return currPos;
    }

    public IEnumerable<(ZProbe ZProbe, Vector2 Center)> CollectZProbes(Transformation2 t) {
        Transformation2 compound = t.Transform(new Transformation2(_targetModel!.Start, _targetModel.End, Start, End));
        return _targetModel?.CollectZProbes(compound) ?? Enumerable.Empty<(ZProbe, Vector2)>();
    }
}
