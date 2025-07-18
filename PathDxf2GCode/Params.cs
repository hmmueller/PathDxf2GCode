﻿namespace de.hmmueller.PathDxf2GCode;

using System.Globalization;
using System.Text.RegularExpressions;
using netDxf;
using netDxf.Entities;

public class ParamsText {
    private readonly Dictionary<char, string> _rawStrings;
    public readonly Dictionary<char, string> VariableStrings;
    public string Text { get; }
    public string Context { get; }
    public string? LayerName { get; }
    public Vector2 Position { get; }
    public Vector2 TextCenter { get; }
    public double TextRadius { get; }
    private readonly HashSet<string> _uniqueErrors = new();

    public static readonly ParamsText EMPTY = new("", "", null, Vector2.Zero, Vector2.Zero, 0);

    public ParamsText(string text, EntityObject? source, Vector2? position, Vector2 textCenter, double textRadius)
        : this(text, source?.CodeName + " @ " + position?.F3(), source?.Layer.Name, position ?? Vector2.Zero, textCenter, textRadius) {
    }

    private ParamsText(string text, string context, string? layerName, Vector2 position, Vector2 textCenter, double textRadius)
        : this(text, context, layerName, position, Regex.Replace(text, @"~([\r\n]\s*)+", "")
            .Split(['\n', ' '])
            .Select(s => s.Trim())
            .Where(s => !string.IsNullOrEmpty(s))
            .Select(s => (Key: s[0], Value: s[1..]))
            .ToList(), textCenter, textRadius) {
    }

    private ParamsText(string text, string context, string? layerName, Vector2 position, List<(char Key, string Value)> rawStrings, Vector2 textCenter, double textRadius) {
        Text = text;
        Context = context;
        LayerName = layerName;
        Position = position;
        IEnumerable<(char Key, string Value)> nonVariableEntries = rawStrings.Where(kv => kv.Key != ':');
        string duplicates = new string(nonVariableEntries.GroupBy(kv => kv.Key)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray());
        if (duplicates != "") {
            throw new ArgumentException(string.Format(Messages.Params_DuplicateParametersFound_Text_Duplicates, text, duplicates));
        } else {
            _rawStrings = nonVariableEntries.ToDictionary(kv => kv.Key, kv => kv.Value);
            if (_rawStrings.TryGetValue('N', out string? n) && n.Contains("=")) {
                throw new ArgumentException(string.Format(Messages.Params_NCannotUseVariable_Text, n));
            }
            if (_rawStrings.TryGetValue('O', out string? o) && o.Contains("=")) {
                throw new ArgumentException(string.Format(Messages.Params_OCannotUseVariable_Text, o));
            }
        }
        VariableStrings = rawStrings.Where(kv => kv.Key == ':' && kv.Value.Length > 0)
                                             .ToDictionary(kv => kv.Value[0], kv => kv.Value[1..]);
        TextCenter = textCenter;
        TextRadius = textRadius;
    }

    public static bool IsNullOrEmpty(ParamsText? p) => p == null || !p.Keys.Any();

    public ParamsText LimitedTo(string keys) {
        if (keys.Contains('=')) {
            throw new ArgumentException(nameof(keys), "keys must not contain =, no support for limiting of Variables");
        }
        return new ParamsText(Text, Context, LayerName, Position,
            _rawStrings.Where(kvp => keys.Contains(kvp.Key)).Select(kvp => (kvp.Key, kvp.Value)).ToList(),
            TextCenter, TextRadius);
    }

    public IEnumerable<char> Keys
        => _rawStrings.Select(kv => kv.Key);

    public void AddError(MessageHandlerForEntities mh, string errorContext, string message) {
        if (_uniqueErrors.Add(message)) { // Each error should be output only once
            mh.AddError(errorContext, message);
        }
    }

    public void InterpolateVariablesAndCreateValues(ActualVariables variables,
        out Dictionary<char, string> stringValues, out Dictionary<char, double> doubleValues) {
        stringValues = _rawStrings.ToDictionary(kv => kv.Key, kv => variables.Interpolate(kv.Value));
        doubleValues = stringValues
            .Select(kvp => new {
                kvp.Key,
                Value = double.TryParse(kvp.Value.Replace(',', '.').Trim(),
                                        CultureInfo.InvariantCulture, out double d) ? d : (double?)null
            })
            .Where(kvp => kvp.Value != null)
            .ToDictionary(kvp => kvp.Key, kvp => (double)kvp.Value!);
    }

    internal int? GetN()
        => _rawStrings.TryGetValue('N', out string? n)
            && int.TryParse(n, CultureInfo.InvariantCulture, out int result) ? result : null;

    internal double? GetO()
        => _rawStrings.TryGetValue('O', out string? o)
            && double.TryParse(o.Replace(',', '.'), CultureInfo.InvariantCulture, out double result) ? result : null;
}

public interface IParams {
    double F_mmpmin { get; }
    double? RawB_mm { get; }
    double? RawD_mm { get; }
    double? RawI_mm { get; }
    double? RawP_mm { get; }
    double? RawU_mm { get; }
    int? RawC { get; }
    double V_mmpmin { get; }
    double T_mm { get; }
    double O_mm { get; }
    string M { get; }
    double Z_mmpmin { get; }
    double? W_mm { get; }

    double B_mm { get; }
    double D_mm { get; }
    double I_mm { get; }
    double P_mm { get; }
    double U_mm { get; }
    int C { get; }
    double S_mm { get; }
    double A_mm { get; }
}

public abstract class AbstractParams : IParams {
    public readonly ParamsText Text;
    private readonly string _errorContext;
    private readonly Action<string, string> _onError;
    private readonly HashSet<string> _uniqueErrors = new();

    private Dictionary<char, string>? _stringValues;
    private Dictionary<char, double>? _doubleValues;

    public string? GetString(char key)
        => _stringValues!.TryGetValue(key, out string? s) ? s : null;

    public double? GetDouble(char key)
        => _doubleValues!.TryGetValue(key, out double d) ? d : null;

    public double GetDouble(char key, Func<string, double> errorAndNull)
        => _doubleValues!.TryGetValue(key, out double d) ? d : errorAndNull($"{key}-Wert fehlt");

    protected void OnError(string msg) {
        Error(msg);
    }

    protected double OnErrorNaN(string msg) {
        Error(msg);
        return double.NaN;
    }

    protected AbstractParams(ParamsText text, ActualVariables superpathVariables, string errorContext, Action<string, string> onError) {
        text.InterpolateVariablesAndCreateValues(superpathVariables, out _stringValues, out _doubleValues);
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
        if (F_mmpmin.Le(0)) {
            Error(Messages.Params_FMustBeGtThan0_F, F_mmpmin);
        }
        if (RawI_mm.HasValue && RawI_mm.Value.Le(0)) {
            Error(Messages.Params_IMustBeGtThan0_I, RawI_mm);
        }
        if (RawC <= 0) {
            Error(Messages.Params_CMustBeGtThan0_C, RawC);
        }
        if (V_mmpmin.Le(0)) {
            Error(Messages.Params_VMustBeGtThan0_V, V_mmpmin);
        }
        if (O_mm.Le(0)) {
            Error(Messages.Params_OMustBeGtThan0_O, O_mm);
        }
        if (T_mm.Le(0)) {
            Error(Messages.Params_TMustBeGtThan0_T, T_mm);
        }
        if (S_mm.Le(T_mm)) {
            Error(Messages.Params_SMustBeGtThanT_S_T, S_mm, T_mm);
        }
        if (RawB_mm.HasValue && RawB_mm.Value.Ge(T_mm)) {
            Error(Messages.Params_BMustBeLessThanT_B_T, RawB_mm, T_mm);
        }
        if (RawD_mm.HasValue && RawD_mm.Value.Ge(T_mm)) {
            Error(Messages.Params_DMustBeLessThanT_D_T, RawD_mm, T_mm);
        }
        if (A_mm.Le(0)) {
            Error(Messages.Params_AMustBeGtThan0_A, A_mm);
        }
        if (W_mm.HasValue && W_mm.Value.Le(0)) {
            Error(Messages.Params_WMustBeGtThan0_W, W_mm);
        }
    }

    protected string GetString(ParamsText text, char key, Action<string> onError) {
        string MissingOnError(string s) {
            onError(s);
            return "***Missing***";
        }
        return GetString(key) ?? MissingOnError(string.Format(Messages.Params_MissingKey_Key, key));
    }

    public abstract double F_mmpmin { get; }
    public abstract double? RawB_mm { get; }
    public abstract double? RawD_mm { get; }
    public abstract double? RawI_mm { get; }
    public abstract double? RawP_mm { get; }
    public abstract double? RawU_mm { get; }
    public abstract int? RawC { get; }
    public abstract double V_mmpmin { get; }
    public abstract double T_mm { get; }
    public abstract double O_mm { get; }
    public abstract double S_mm { get; }
    public abstract double A_mm { get; }
    public abstract string M { get; }
    public abstract double Z_mmpmin { get; }
    public abstract double? W_mm { get; }

    public double B_mm => RawB_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'B');
    public double D_mm => RawD_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'D');
    public double I_mm => RawI_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'I');
    public double P_mm => RawP_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'P');
    public double U_mm => RawU_mm ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'U');
    public int C => RawC ?? throw new EmitGCodeException(_errorContext, Messages.Params_MissingKey_Key, 'C');
}

public class PathParams : AbstractParams {
    private readonly Options _options;

    public override double F_mmpmin => GetDouble('F') ?? _options.GlobalFeedRate_mmpmin;
    public override double? RawB_mm => GetDouble('B');
    public override double? RawD_mm => GetDouble('D');
    public override double? RawP_mm => GetDouble('P');
    public override double? RawU_mm => GetDouble('U');
    public override int? RawC => (int?)GetDouble('C');
    public override double? RawI_mm => GetDouble('I');
    public override double S_mm { get; }
    public override double A_mm { get; }
    public override double V_mmpmin => _options.GlobalSweepRate_mmpmin;
    public override double T_mm => GetDouble('T', OnErrorNaN);
    public override double O_mm => GetDouble('O', OnErrorNaN);
    public override string M => GetString(Text, 'M', OnError);
    public override double Z_mmpmin => GetDouble('Z') ?? _options.GlobalProbeRate_mmpmin;
    public override double? W_mm => GetDouble('W');
    public string OutFileSuffix => GetString('R') ?? "";
    public FormalVariables FormalVariables { get; }

    public PathParams(ParamsText text, ActualVariables superpathVariables, double? defaultSorNullForTplusO_mm, string errorContext, Options options, Action<string, string> onError) : base(text, superpathVariables, errorContext, onError) {
        _options = options;
        S_mm = GetDouble('S') ?? defaultSorNullForTplusO_mm ?? T_mm + O_mm;
        A_mm = GetDouble('A') ?? 4 * O_mm;

        CheckKeysAndValues(text, "FBDCISTOMPUZAWR");
        if (RawD_mm.HasValue && RawB_mm.HasValue && RawB_mm.Value.Ge(RawD_mm.Value)) {
            Error(Messages.Params_DMustBeGtThanB_D_B, RawD_mm, B_mm);
        }
        if (RawP_mm.HasValue && RawP_mm.Value.Le(0)) {
            Error(Messages.Params_PMustBeGtThan0_P, RawP_mm);
        }
        if (RawU_mm.HasValue && RawU_mm.Value.Le(0)) {
            Error(Messages.Params_UMustBeGtThan0_U, RawU_mm);
        }
        FormalVariables = new FormalVariables(text.VariableStrings);
    }
}

public abstract class AbstractChildParams : AbstractParams {
    private readonly IParams _parent;

    public override double F_mmpmin => _parent.F_mmpmin;
    public override double? RawB_mm => _parent.RawB_mm;
    public override double? RawD_mm => _parent.RawD_mm;
    public override double? RawP_mm => _parent.RawP_mm;
    public override double? RawU_mm => _parent.RawU_mm;
    public override int? RawC => _parent.RawC;
    public override double? RawI_mm => _parent.RawI_mm;
    public override double S_mm => GetDouble('S') ?? _parent.S_mm;
    public override double V_mmpmin => _parent.V_mmpmin;
    public override double T_mm => _parent.T_mm;
    public override double O_mm => _parent.O_mm;
    public override string M => _parent.M;
    public override double Z_mmpmin => _parent.Z_mmpmin;
    public override double A_mm => _parent.A_mm;
    public override double? W_mm => _parent.W_mm;

    protected AbstractChildParams(ParamsText text, ActualVariables superpathVariables, string errorContext, IParams parent, Action<string, string> onError) : base(text, superpathVariables, errorContext, onError) {
        _parent = parent;
    }
}

public class ChainParams : AbstractChildParams, IParams {
    public const string KEYS = "NICW";
    public override int? RawC => (int?)GetDouble('C') ?? base.RawC;
    public override double? RawI_mm => GetDouble('I') ?? base.RawI_mm;
    public override double? W_mm => GetDouble('W') ?? base.W_mm;

    public ChainParams(ParamsText text, ActualVariables superpathVariables, string errorContext, IParams pathParams, Action<string, string> onError) : base(text.LimitedTo(KEYS), superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, KEYS + MillParams.MILL_KEYS + MillParams.MARK_KEYS);
    }
}

public class SweepParams : AbstractChildParams {
    public SweepParams(ParamsText text, ActualVariables superpathVariables, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "SN");
    }
}

public class BackSweepParams : AbstractChildParams {
    public BackSweepParams(ParamsText text, ActualVariables superpathVariables, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "FBDCISTOMNPUAW>"); // Backsweeps are allowed for all sorts of objects, as they inherit their parents' full parameter set
    }
}

public class MillParams : AbstractChildParams {
    public const string MILL_KEYS = "FBSNQ";
    public const string MARK_KEYS = "FDSNQ";
    public const string SUPPORT_KEYS = "FDBSNPUQ";
    public override double F_mmpmin => GetDouble('F') ?? base.F_mmpmin;
    public override double? RawB_mm => GetDouble('B') ?? base.RawB_mm;
    public override double? RawD_mm => GetDouble('D') ?? base.RawD_mm;
    public override double? RawP_mm => GetDouble('P') ?? base.RawP_mm;
    public override double? RawU_mm => GetDouble('U') ?? base.RawU_mm;
    public string? Q => GetString('Q');

    public MillParams(ParamsText text, ActualVariables superpathVariables, MillType millType, string errorContext, IParams chainParams, Action<string, string> onError) : base(text.LimitedTo(
        millType switch {
            MillType.Mill => MILL_KEYS,
            MillType.Mark => MARK_KEYS,
            MillType.WithSupports => SUPPORT_KEYS,
            _ => throw new Exception("Internal error: Unexpected Milltype " + millType)
        }), superpathVariables, errorContext, chainParams, onError) {
        CheckKeysAndValues(text, MILL_KEYS + MARK_KEYS + SUPPORT_KEYS + ChainParams.KEYS);
    }
}

public class HelixParams : AbstractChildParams {
    public const string MILL_KEYS = "FBIQ";
    public const string MARK_KEYS = "FDIQ";
    public const string SUPPORT_KEYS = "FDBIPUQ";

    public override double F_mmpmin => GetDouble('F') ?? base.F_mmpmin;
    public override double? RawB_mm => GetDouble('B') ?? base.RawB_mm;
    public override double? RawD_mm => GetDouble('D') ?? base.RawD_mm;
    public override double? RawI_mm => GetDouble('I') ?? base.RawI_mm;
    public override double? RawP_mm => GetDouble('P') ?? base.RawP_mm;
    public override double? RawU_mm => GetDouble('U') ?? base.RawU_mm;
    public string? Q => GetString('Q');

    public HelixParams(ParamsText text, ActualVariables superpathVariables, MillType millType, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text,
                    millType switch {
                        MillType.Mill => MILL_KEYS,
                        MillType.Mark => MARK_KEYS,
                        MillType.WithSupports => SUPPORT_KEYS,
                        _ => throw new Exception("Internal error: Unexpected Milltype " + millType)
                    });
    }
}

public class DrillParams : AbstractChildParams {
    public override double F_mmpmin => GetDouble('F') ?? base.F_mmpmin;
    public override double? RawB_mm => GetDouble('B') ?? base.RawB_mm;
    public override double? RawD_mm => GetDouble('D') ?? base.RawD_mm;
    public override int? RawC => (int?)GetDouble('C') ?? base.RawC;
    public string? Q => GetString('Q');

    public DrillParams(ParamsText text, ActualVariables superpathVariables, bool isMark, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, isMark ? "FDCQ" : "FBCQ");
    }
}

public class SubpathParams : AbstractChildParams {
    public override double T_mm => GetDouble('T') ?? base.T_mm;
    public override double O_mm => GetDouble('O') ?? base.O_mm;
    public override string M => GetString('M') ?? base.M;
    public ActualVariables ActualVariables { get; }

    public SubpathParams(ParamsText text, ActualVariables superpathVariables, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "TOMN>");
        ActualVariables = new ActualVariables(superpathVariables.InterpolateInto(text.VariableStrings));
    }
}

public class ZProbeParams : AbstractChildParams {
    public override double T_mm => GetDouble('T') ?? base.T_mm;
    public string? L => GetString('L');
    public override double Z_mmpmin => GetDouble('Z') ?? base.Z_mmpmin;

    public ZProbeParams(ParamsText text, ActualVariables superpathVariables, string errorContext, IParams pathParams, Action<string, string> onError) : base(text, superpathVariables, errorContext, pathParams, onError) {
        CheckKeysAndValues(text, "TLZ");
    }
}
