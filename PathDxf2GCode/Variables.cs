namespace de.hmmueller.PathDxf2GCode;

public class Variables {
    public static readonly Variables EMPTY = new Variables(new Dictionary<char, string>());

    private readonly Dictionary<char, string> _replacements;
    private readonly int _replacementsHash;

    public Variables(Dictionary<char, string> replacements) {
        _replacements = replacements;
        foreach (var kvp in _replacements) {
            _replacementsHash ^= kvp.Key.GetHashCode() ^ (kvp.Value.GetHashCode() >> 1);
        }
    }

    public override int GetHashCode() => _replacementsHash;

    public override bool Equals(object? obj) {
        return obj is Variables other
            && _replacements.Count == other._replacements.Count
            && !_replacements.Except(other._replacements).Any();
    }

    internal string Interpolate(string s) {
        bool changed;
        do {
            changed = false;
            foreach (var kvp in _replacements) {
                string v = "=" + kvp.Key;
                if (s.Contains(v)) {
                    s = s.Replace(v, kvp.Value);
                    changed = true;
                }
            }
        } while (changed);
        return s;
    }

    public void Interpolate(Variables variables) {
        foreach (var kvp in _replacements) {
            _replacements[kvp.Key] = variables.Interpolate(kvp.Value);
        }
    }
}
