namespace de.hmmueller.PathDxf2GCode;

using System.Globalization;

public class VariableDefinitionException : Exception {
    public VariableDefinitionException(string message, params object[] pars) : base(string.Format(message, pars)) {
    }
}

public abstract class Variables {
    protected readonly IReadOnlyDictionary<char, string> _assignments;
    private readonly int _valuesHash;

    public Variables(IReadOnlyDictionary<char, string> replacements) {
        _assignments = replacements;
        foreach (var kvp in _assignments) {
            _valuesHash ^= kvp.Key.GetHashCode() ^ (kvp.Value.GetHashCode() >> 1);
        }
    }

    public override int GetHashCode() => _valuesHash;

    public override bool Equals(object? obj) {
        return obj is Variables other
            && _assignments.Count == other._assignments.Count
            && !_assignments.Except(other._assignments).Any();
    }

}

public class FormalVariables : Variables {
    public FormalVariables(IReadOnlyDictionary<char, string> replacements) : base(replacements) {
    }

    public ActualVariables Example(Action<string> onError) {
        int k = 0;
        try {
            return new ActualVariables(_assignments.ToDictionary(kvp => kvp.Key, kvp => Parse(kvp.Key, kvp.Value).Example(ref k)));
        } catch (VariableDefinitionException ex) {
            onError(string.Format(Messages.Variables_Error_Message, ex.Message));
            return ActualVariables.EMPTY;
        }
    }

    private interface IDefinition {
        bool Accepts(string v);
        string Example(ref int k);
    }

    private class AlternativeDefinition : IDefinition {
        private readonly string[] _alternatives;

        public AlternativeDefinition(string[] alternatives) {
            _alternatives = alternatives;
        }

        public bool Accepts(string v) => _alternatives.Contains(v);

        public string Example(ref int k) => _alternatives[k++ % _alternatives.Length];
    }

    private class RangeDefinition : IDefinition {
        private readonly double _from, _to;

        public RangeDefinition(double from, double to) {
            _from = from;
            _to = to;
        }

        public bool Accepts(string v) 
            => double.TryParse(v.Replace(',', '.'), CultureInfo.InvariantCulture, out double d)
                && d >= _from && d <= _to;

        public string Example(ref int k) => (k++ % 2 == 0 ? _from : _to).ToString(CultureInfo.InvariantCulture);
    }

    private IDefinition Parse(char name, string def) {
        string d = def.Trim();
        if (d.Contains(',')) {
            return new AlternativeDefinition(d.Replace("?", "").Split(',', StringSplitOptions.TrimEntries));
        } else if (d.StartsWith('?')) {
            return new AlternativeDefinition(d[1..].Select(t => "" + t).ToArray());
        } else if (d.Contains('~')) {
            string[] n = d.Replace(',', '.').Split('~', StringSplitOptions.TrimEntries);
            if (n.Length != 2) {
                throw new VariableDefinitionException(Messages.Variables_RangeHasMoreThanTwoNumbers_Variable_Range, name, d);
            } else {
                try {
                    return new RangeDefinition(double.Parse(n[0], CultureInfo.InvariantCulture),
                        double.Parse(n[1], CultureInfo.InvariantCulture));
                } catch (FormatException fe) {
                    throw new VariableDefinitionException(Messages.Variables_RangeFormatError_Variable_Message, name, fe.Message);
                }
            }
        } else {
            throw new VariableDefinitionException(Messages.Variables_DefinitionNeitherListNorLettersNorRange_Variable_Definition, name, d);
        }
    }

    public void CheckActualVariables(ActualVariables values, Action<string> onError) {
        char[] missingValues = _assignments.Keys.Except(values.Assignments.Keys).ToArray();
        if (missingValues.Any()) {
            onError(string.Format(Messages.Variables_MissingValues_Variables, new string(missingValues)));
        }
        char[] missingDefinitions = values.Assignments.Keys.Except(_assignments.Keys).ToArray();
        if (missingDefinitions.Any()) {
            onError(string.Format(Messages.Variables_MissingDefinitions_Variables, new string(missingDefinitions)));
        }
        foreach (var k in _assignments.Keys.Intersect(values.Assignments.Keys)) {
            try {
                if (!Parse(k, _assignments[k]).Accepts(values.Assignments[k])) {
                    onError(string.Format(Messages.Variables_ValueDoesNotMatchDefinition_Value_Definition_Variable,
                        _assignments[k], values.Assignments[k], k));
                }
            } catch (VariableDefinitionException ex) {
                onError(string.Format(ex.Message));
            }
        }
    }
}

public class ActualVariables : Variables {
    internal IReadOnlyDictionary<char, string> Assignments => _assignments;

    public static readonly ActualVariables EMPTY = new ActualVariables(new Dictionary<char, string>());

    public ActualVariables(Dictionary<char, string> replacements) : base(replacements) {
    }

    internal string Interpolate(string s) {
        bool changed;
        do {
            changed = false;
            foreach (var kvp in _assignments) {
                string v = "=" + kvp.Key;
                if (s.Contains(v)) {
                    s = s.Replace(v, kvp.Value);
                    changed = true;
                }
            }
        } while (changed);
        return s;
    }

    public Dictionary<char, string> InterpolateInto(IReadOnlyDictionary<char, string> variableStrings) {
        return variableStrings.ToDictionary(kvp => kvp.Key, kvp => Interpolate(kvp.Value));
    }
}