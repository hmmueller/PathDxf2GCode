namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using netDxf.Tables;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

public readonly struct PathName {
    private readonly string _name;
    private readonly string _fileNameForMessages;

    public PathName(string name, string fileNameForMessages) {
        // Paths in DXF have _ instead of . (e.g. in Caddy) ., * etc. -> transform back!
        _name = Regex.Replace(name, DxfHelper.TILDE_SUFFIX_REGEX + "$", "", RegexOptions.IgnoreCase).Replace('_', '.');
        _fileNameForMessages = Path.GetFileName(fileNameForMessages);
    }

    public readonly string AsString()
        => _name;

    public override string ToString()
        => _name + "[" + _fileNameForMessages + "]";

    public override bool Equals([NotNullWhen(true)] object? other)
        => other != null && _name.Equals(((PathName)other)._name);

    public override int GetHashCode()
        => _name.GetHashCode();

    public static int CompareFileNameToPathName(string filename, PathName pathname, string pathNamePattern) {
        string anchoredPathNamePattern = "^" + pathNamePattern + "$";
        Match m1 = Regex.Match(filename, anchoredPathNamePattern, RegexOptions.IgnoreCase);
        if (!m1.Success) {
            throw new ArgumentException(nameof(filename), $"'{filename}' does not match {anchoredPathNamePattern}");
        }
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
                int c = string.Compare(s1, s2, ignoreCase: true);
                if (c != 0) {
                    return c;
                }
            } else {
                break; // Otherwise, only the group numbers are compared
            }
        }
        return groupsWithValue2 > groupsWithValue1 ? 0 : groupsWithValue1 - groupsWithValue2;
    }
}

public class PathModelCollection {
    private readonly Dictionary<PathName, PathModel> _models = new();
    private readonly Dictionary<string, SortedDictionary<string, PathModel>> _readFiles = new();

    public SortedDictionary<string, PathModel> Load(string dxfFilePath, double? defaultSorNullForTplusO_mm, Options options, 
                                                    string contextForErrors, MessageHandlerForEntities messages) {
        string fullDxfFilePath = Path.GetFullPath(dxfFilePath);
        if (!_readFiles.ContainsKey(fullDxfFilePath)) {
            var modelsInDxfFile = new SortedDictionary<string, PathModel>();
            _readFiles.Add(fullDxfFilePath, modelsInDxfFile);
            DxfDocument? d = DxfHelper.LoadDxfDocument(fullDxfFilePath, options,
                                                     out Dictionary<string, Linetype> layerLinetypes, messages);
            if (d != null) {
                Dictionary<PathName, PathModel> models = PathModel.TransformDxf2PathModel(fullDxfFilePath, d.Entities,
                    layerLinetypes, defaultSorNullForTplusO_mm, options, messages, this);
                foreach (var kvp in models) {
                    if (_models.TryAdd(kvp.Key, kvp.Value)) {
                        modelsInDxfFile.Add(kvp.Key.AsString(), kvp.Value);
                    } else {
                        messages.AddError(contextForErrors, Messages.PathModelCollection_PathDefinedTwice_Path_File, kvp.Key, kvp.Value.DxfFilePath);
                    }
                }
            }
        }
        return _readFiles[fullDxfFilePath];
    }

    public PathModel Get(PathName path)
        => _models[path];

    public bool Contains(PathName path)
        => _models.ContainsKey(path);
}
