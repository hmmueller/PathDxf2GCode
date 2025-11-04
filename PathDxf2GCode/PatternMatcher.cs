namespace de.hmmueller.PathDxf2GCode;

public class PatternMatcher<T> {
    public abstract class Pattern {
        internal abstract IEnumerable<int> Match(List<T> input, int i, Dictionary<string, Stack<T>> matched);
    }
 
    public class LeafPattern : Pattern {
        private readonly string _name;
        private readonly Func<T, Dictionary<string, Stack<T>>, bool> _condition;
        public LeafPattern(string name, Func<T, Dictionary<string, Stack<T>>, bool> condition) {
            _name = name;
            _condition = condition;
        }

        public LeafPattern(string name, Func<T, bool> condition) : this(name, (e, _) => condition(e)) {
        }

        internal override IEnumerable<int> Match(List<T> input, int i, Dictionary<string, Stack<T>> matched) {
            T e = input[i];
            if (_condition(e, matched)) {
                if (!matched.TryGetValue(_name, out Stack<T>? s)) {
                    matched.Add(_name, s = new Stack<T>());
                }
                s.Push(e);
                yield return i + 1;
                s.Pop();
            } else {
                yield break;
            }
        }
    }

    public class SeqPattern : Pattern {
        private readonly Pattern[] _innerPatterns;

        public SeqPattern(params Pattern[] innerPatterns) {
            _innerPatterns = innerPatterns;
        }

        internal override IEnumerable<int> Match(List<T> input, int i, Dictionary<string, Stack<T>> matched) {
            return Match(0, input, i, matched);
        }

        private IEnumerable<int> Match(int j, List<T> input, int i, Dictionary<string, Stack<T>> matched) {
            if (j >= _innerPatterns.Length) {
                yield return i;
            } else {
                foreach (var endOfJ in _innerPatterns[j].Match(input, i, matched)) {
                    foreach (var end in Match(j + 1, input, endOfJ, matched)) {
                        yield return end;
                    }
                }
            }
        }
    }
    
    public class LoopPattern : Pattern {
        private readonly int _from, _to;
        private readonly Pattern _innerPattern;

        public LoopPattern(Pattern inner, int from, int to) {
            _innerPattern = inner;
            _from = from;
            _to = to;
        }

        internal override IEnumerable<int> Match(List<T> input, int i, Dictionary<string, Stack<T>> matched) {
            return Match(0, input, i, matched);
        }

        private IEnumerable<int> Match(int j, List<T> input, int i, Dictionary<string, Stack<T>> matched) {
            if (j >= _from && j < _to) {
                yield return i;
            }
            foreach (var endOfJ in _innerPattern.Match(input, i, matched)) {
                foreach (var end in Match(j + 1, input, endOfJ, matched)) {
                    yield return end;
                }
            }
        }
    }

    public IEnumerable<(int from, int to, Dictionary<string, List<T>> matched)> Match(List<T> input, Pattern p, int start) {
        for (int i = start; i < input.Count; i++) {
            (int to, Dictionary<string, List<T>> matched)? bestMatch = null;
            Dictionary<string, Stack<T>> matched = new();
            foreach (var t in p.Match(input, i, matched)) { 
                // Find longest match
                if (bestMatch == null || t > bestMatch.Value.to) {
                    bestMatch = (t, matched.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList()));
                }
            }
            if (bestMatch != null) {
                yield return (i, bestMatch.Value.to, bestMatch.Value.matched);
            }
        }
    }
}
