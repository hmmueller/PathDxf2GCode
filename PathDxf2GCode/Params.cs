﻿namespace de.hmmueller.PathDxf2GCode;

using System.Globalization;
using netDxf;
using netDxf.Entities;

public class ParamsText {
    private readonly Dictionary<char, string> _strings;
    private readonly Dictionary<char, double> _values;
    public string Text { get; }
    public string Context { get; }
    public string? LayerName { get; }
    public Vector2 Position { get; }
    public Vector2 TextCenter { get; }
    public double TextRadius { get; }
    private readonly HashSet<string> _uniqueErrors = new();

    public readonly static ParamsText EMPTY = new("", "", null, Vector2.Zero, Vector2.Zero, 0);

    public ParamsText(string text, EntityObject? source, Vector2? position, Vector2 textCenter, double textRadius)
        : this(text, source?.CodeName + " @ " + position?.F3(), source?.Layer.Name, position ?? Vector2.Zero, textCenter, textRadius) {
    }

    private ParamsText(string text, string context, string? layerName, Vector2 position, Vector2 textCenter, double textRadius)
        : this(text, context, layerName, position, text
            .Split(['\n', ' '])
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .ToDictionary(s => s[0], s => s[1..]), textCenter, textRadius) {
    }

    private ParamsText(string text, string context, string? layerName, Vector2 position, Dictionary<char, string> strings, Vector2 textCenter, double textRadius) {
        Text = text;
        Context = context;
        LayerName = layerName;
        Position = position;
        _strings = strings;
        _values = strings
            .Select(kvp => new {
                kvp.Key,
                Value = double.TryParse(kvp.Value.Replace(',', '.').TrimStart('=').Trim(),
                                                        CultureInfo.InvariantCulture, out double d) ? d : (double?)null
            })
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value!);
        TextCenter = textCenter;
        TextRadius = textRadius;
    }

    public static bool IsNullOrEmpty(ParamsText? p) => p == null || !p.Keys.Any();

    public ParamsText LimitedTo(string keys) {
        return new ParamsText(Text, Context, LayerName, Position,
            _strings.Where(kvp => keys.Contains(kvp.Key)).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
            TextCenter, TextRadius);
    }

    public IEnumerable<char> Keys
        => _strings.Keys;

    public string? GetString(char key)
        => _strings.TryGetValue(key, out string? s) ? s : null;

    public double? GetDouble(char key)
        => _values.TryGetValue(key, out double d) ? d : null;

    public double GetDouble(char key, Func<string, double> errorAndNull)
        => _values.TryGetValue(key, out double d) ? d : errorAndNull($"{key}-Wert fehlt");

    public void AddError(MessageHandlerForEntities mh, string errorContext, string message) {
        if (_uniqueErrors.Add(message)) { // Each error should be output only once ausgeben
            mh.AddError(errorContext, message);
        }
    }
}

public interface IParams {
    double F_mmpmin { get; }
    double? RawB_mm { get; }
    double? RawD_mm { get; }
    double? RawI_mm { get; }
    int? RawC { get; }
    double? RawS_mm { get; }
    double V_mmpmin { get; }
    double? RawK_mm { get; }
    double T_mm { get; }
    double O_mm { get; }
    string M { get; }

    double B_mm { get; }
    double D_mm { get; }
    double I_mm { get; }
    int C { get; }
    double S_mm { get; }
    double K_mm { get; }
}

public abstract class AbstractParams : IParams {
    public readonly ParamsText Text;
    private readonly string _errorContext;
    private readonly Action<string, string> _onError;
    private readonly HashSet<string> _uniqueErrors = new();

    protected void OnError(string msg) {
        Error(msg);
    }

    protected double OnErrorNaN(string msg) {
        Error(msg);
        return double.NaN;
    }

    protected AbstractParams(ParamsText text, string errorContext, Action<string, string> onError) {
        Text = text;
        _errorContext = errorContext;
        _onError = onError;
    }

    protected void Error(string msg, params object[] pars) {
        string m = string.Format(msg, pars);
        if (_uniqueErrors.Add(m)) {
            _onError(_errorContext, m);
        }
    }

    protected void CheckKeysAndValues(ParamsText text, string expectedKeys) {
        foreach (char c in text.Keys.Except(expectedKeys)) {
            Error(Messages.Params_UnsupportedKey_Name_Context, c, text.Context);
        }
        if (F_mmpmin <= 0) {
            Error(Messages.Params_FMustBeGtThan0_F, F_mmpmin);
        }
        if (RawI_mm <= 0) {
            Error(Messages.Params_IMustBeGtThan0_I, RawI_mm);
        }
        if (RawC <= 0) {
            Error(Messages.Params_CMustBeGtThan0_C, RawC);
        }
        if (V_mmpmin <= 0) {
            Error(Messages.Params_VMustBeGtThan0_V, V_mmpmin);
        }
        if (O_mm <= 0) {
            Error(Messages.Params_OMustBeGtThan0_O, O_mm);
        }
        if (T_mm <= 0) {
            Error(Messages.Params_TMustBeGtThan0_T, T_mm);
        }
        if (RawS_mm <= T_mm) {
            Error(Messages.Params_SMustBeGtThanT_S_T, RawS_mm, T_mm);
        }
        if (RawB_mm.HasValue && RawB_mm > T_mm && !RawB_mm.Value.Near(T_mm)) {
            Error(Messages.Params_BMustBeLessThanT_B_T, RawB_mm, T_mm);
        }
        if (RawD_mm.HasValue && RawD_mm > T_mm && !RawD_mm.Value.Near(T_mm)) {
            Error(Messages.Params_DMustBeLessThanT_D_T, RawD_mm, T_mm);
        }
    }

    protected static string GetString(ParamsText text, char key, Action<string> onError) {
        string MissingOnError(string s) {
            onError(s);
            return "***Missing***";
        }
        return text.GetString(key) ?? MissingOnError(string.Format(Messages.Params_MissingKey_Key, key));
    }

    public abstract double F_mmpmin { get; }
    public abstract double? RawB_mm { get; }
    public abstract double? RawD_mm { get; }
    public abstract double? RawI_mm { get; }
    public abstract int? RawC { get; }
    public abstract double? RawS_mm { get; }
    public abstract double V_mmpmin { get; }
    public abstract double? RawK_mm { get; }
    public abstract double T_mm { get; }
    public abstract double O_mm { get; }
    public abstract string M { get; }

    public double B_mm => RawB_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'B');
    public double D_mm => RawD_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'D');
    public double I_mm => RawI_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'I');
    public double S_mm => RawS_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'S');
    public double K_mm => RawK_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'K');
    public int C => RawC ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'C');
}

public class PathParams : AbstractParams {
    private readonly Options _options;

    public override double F_mmpmin => Text.GetDouble('F') ?? _options.GlobalFeedRate_mmpmin;
    public override double? RawB_mm => Text.GetDouble('B');
    public override double? RawD_mm => Text.GetDouble('D');
    public override int? RawC => (int?)Text.GetDouble('C');
    public override double? RawI_mm => Text.GetDouble('I');
    public override double? RawK_mm => Text.GetDouble('K');
    public override double? RawS_mm => Text.GetDouble('S');
    public override double V_mmpmin => _options.GlobalSweepRate_mmpmin;
    public override double T_mm => Text.GetDouble('T', OnErrorNaN);
    public override double O_mm => Text.GetDouble('O', OnErrorNaN);
    public override string M => GetString(Text, 'M', OnError);

    public PathParams(ParamsText text, string errorContext, Options options, Action<string, string> onError) : base(text, errorContext, onError) {
        _options = options;
        CheckKeysAndValues(text, "FBDCIKSTOM");
        if (RawD_mm.HasValue && RawB_mm.HasValue && (RawD_mm < RawB_mm || RawD_mm.Value.Near(RawB_mm.Value))) {
            Error(Messages.Params_DMustBeGtThanB_D_B, RawD_mm, B_mm);
        }
    }
}

public abstract class AbstractChildParams : AbstractParams {
    private readonly IParams _parent;

    public override double F_mmpmin => _parent.F_mmpmin;
    public override double? RawB_mm => _parent.RawB_mm;
    public override double? RawD_mm => _parent.RawD_mm;
    public override int? RawC => _parent.RawC;
    public override double? RawI_mm => _parent.RawI_mm;
    public override double? RawK_mm => _parent.RawK_mm;
    public override double? RawS_mm => _parent.RawS_mm;
    public override double V_mmpmin => _parent.V_mmpmin;
    public override double T_mm => _parent.T_mm;
    public override double O_mm => _parent.O_mm;
    public override string M => _parent.M;

    protected AbstractChildParams(ParamsText text, string errorContext, IParams parent, Action<string, string> onError) : base(text, errorContext, onError) {
        _parent = parent;
    }
}

public class ChainParams : AbstractChildParams, IParams {
    public const string KEYS = "CIKN";
    public override int? RawC => (int?)Text.GetDouble('C') ?? base.RawC;
    public override double? RawI_mm => Text.GetDouble('I') ?? base.RawI_mm;
    public override double? RawK_mm => Text.GetDouble('K') ?? base.RawK_mm;

    public ChainParams(ParamsText text, string errorContext, IParams pathParams, Action<string, string> onError) : base(text.LimitedTo(KEYS), errorContext, pathParams, onError) {
        CheckKeysAndValues(text, KEYS + MillParams.MILL_KEYS + MillParams.MARK_KEYS);
    }
}

public class SweepParams : AbstractChildParams {
    public override double? RawS_mm => Text.GetDouble('S') ?? base.RawS_mm;

    public SweepParams(ParamsText text, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "SN");
    }
}

public class BackSweepParams : AbstractChildParams {
    public override double? RawS_mm => Text.GetDouble('S') ?? base.RawS_mm;
    public override double? RawK_mm => Text.GetDouble('K') ?? base.RawS_mm;

    public BackSweepParams(ParamsText text, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "FBDCIKSTOMN<>"); // Backsweeps are allowed for all sorts of objects, as they inherit their parents' fulll parameter set
    }
}

public class MillParams : AbstractChildParams {
    public const string MILL_KEYS = "FBSN";
    public const string MARK_KEYS = "FDSN";
    public override double F_mmpmin => Text.GetDouble('F') ?? base.F_mmpmin;
    public override double? RawB_mm => Text.GetDouble('B') ?? base.RawB_mm;
    public override double? RawD_mm => Text.GetDouble('D') ?? base.RawD_mm;
    public override double? RawS_mm => Text.GetDouble('S') ?? base.RawS_mm;

    public MillParams(ParamsText text, bool isMark, string errorContext, IParams chainParams, Action<string, string> onError) : base(text.LimitedTo(isMark ? MARK_KEYS : MILL_KEYS), errorContext, chainParams, onError) {
        CheckKeysAndValues(text, MILL_KEYS + MARK_KEYS + ChainParams.KEYS);
    }
}

public class HelixParams : AbstractChildParams {
    public override double F_mmpmin => Text.GetDouble('F') ?? base.F_mmpmin;
    public override double? RawB_mm => Text.GetDouble('B') ?? base.RawB_mm;
    public override double? RawD_mm => Text.GetDouble('D') ?? base.RawD_mm;
    public override double? RawI_mm => Text.GetDouble('I') ?? base.RawI_mm;

    public HelixParams(ParamsText text, bool isMark, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, isMark ? "FDI" : "FBI");
    }
}

public class DrillParams : AbstractChildParams {
    public override double F_mmpmin => Text.GetDouble('F') ?? base.F_mmpmin;
    public override double? RawB_mm => Text.GetDouble('B') ?? base.RawB_mm;
    public override double? RawD_mm => Text.GetDouble('D') ?? base.RawD_mm;
    public override int? RawC => (int?)Text.GetDouble('C') ?? base.RawC;

    public DrillParams(ParamsText text, bool isMark, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, isMark ? "FDC" : "FBC");
    }
}

public class SubpathParams : AbstractChildParams {
    public override double T_mm => Text.GetDouble('T') ?? base.T_mm;
    public override double O_mm => Text.GetDouble('O') ?? base.O_mm;
    public override string M => Text.GetString('M') ?? base.M;
    public override double? RawK_mm => Text.GetDouble('K') ?? base.RawK_mm;

    public SubpathParams(ParamsText text, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "TOMKN<>");
    }
}

public class ZProbeParams : AbstractChildParams {
    public override double T_mm => Text.GetDouble('T') ?? base.T_mm;
    public string? L => Text.GetString('L');

    public ZProbeParams(ParamsText text, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "TL");
    }
}
