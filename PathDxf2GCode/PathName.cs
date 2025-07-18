namespace de.hmmueller.PathDxf2GCode;

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

public readonly struct PathName {
    private readonly string _name;
    private readonly string _fileNameForMessages;

    public PathName(string name, string fileNameForMessages) {
        // Paths in DXF have _ instead of . (e.g. in Caddy) ., * etc. -> transform back!
        _name = NameWithoutTildeSuffix(name);
        _fileNameForMessages = Path.GetFileName(fileNameForMessages);
    }

    public static string NameWithoutTildeSuffix(string name) {
        return Regex.Replace(name, DxfHelper.TILDE_SUFFIX_REGEX + "$", "", RegexOptions.IgnoreCase).Replace('_', '.');
    }

    public readonly string AsString()
        => _name;

    public override string ToString()
        => _name + "[" + _fileNameForMessages + "]";

    public override bool Equals([NotNullWhen(true)] object? other)
        => other != null && _name.Equals(((PathName)other)._name);

    public override int GetHashCode()
        => _name.GetHashCode();

    public static int CompareFileNameToPathName(string filename, PathName pathname, string pathFilePattern, string pathNamePattern) {
        string anchoredPathFilePattern = "^" + pathFilePattern + "$";
        Match m1 = Regex.Match(filename, anchoredPathFilePattern, RegexOptions.IgnoreCase);
        if (!m1.Success) {
            throw new ArgumentException(nameof(filename), $"'{filename}' does not match {anchoredPathFilePattern}");
        }
        string anchoredPathNamePattern = "^" + pathNamePattern + "$";
        Match m2 = Regex.Match(pathname.AsString(), anchoredPathNamePattern, RegexOptions.IgnoreCase);
        if (!m2.Success) {
            throw new ArgumentException(nameof(pathname), $"'{pathname}' does not match {anchoredPathNamePattern}");
        }
        int groupsWithValue1 = 0;
        int groupsWithValue2 = 0;
        for (int i = 1; i < m1.Groups.Count && i < m2.Groups.Count; i++) {
            string s1 = m1.Groups[i].Value;
            string s2 = m2.Groups[i].Value;
            if (s1 != "") {
                groupsWithValue1++;
            }
            if (s2 != "") {
                groupsWithValue2++;
            }
            if (s1 != "" && s2 != "") {
                // Compare parts only if both still "active"
                if (int.TryParse(s1, out int i1) && int.TryParse(s2, out int i2)) {
                    int c = i1 - i2;
                    if (c != 0) {
                        return c;
                    }
                } else {
                    int c = string.Compare(s1, s2, ignoreCase: true);
                    if (c != 0) {
                        return c;
                    }
                }
            } else {
                break; // Otherwise, only the group numbers are compared
            }
        }
        return groupsWithValue2 > groupsWithValue1 ? 0 : groupsWithValue1 - groupsWithValue2;
    }
}

