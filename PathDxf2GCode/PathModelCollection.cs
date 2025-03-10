﻿namespace de.hmmueller.PathDxf2GCode;

using netDxf;
using netDxf.Tables;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

public readonly struct PathName {
    private readonly string _name;
    private readonly string _fileNameForMessages;

    public PathName(string name, string fileNameForMessages) {
        // Pfade in DXF haben _ statt (wie in Caddy) ., * usw. -> zurücktransformieren!
        _name = ConvertDxfLayerToPathName(name);
        _fileNameForMessages = fileNameForMessages;
    }

    public static string ConvertDxfLayerToPathName(string name)
        => name.Replace('_', '.');

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
                // Nur wenn beide Teile noch "im Rennen sind", werden sie verglichen
                int c = string.Compare(s1, s2, ignoreCase: true);
                if (c != 0) {
                    return c;
                }
            } else {
                break; // Sonst brechen wir ab und vergleichen nun die Gruppenanzahlen
            }
        }
        return groupsWithValue2 > groupsWithValue1 ? 0 : groupsWithValue1 - groupsWithValue2;
    }
}

public class PathModelCollection {
    private readonly Dictionary<PathName, PathModel> _models = new();
    private readonly HashSet<string> _readFiles = new();

    public SortedDictionary<string, PathModel> Load(string dxfFilePath, Options options, string contextForErrors, MessageHandler messages) {
        var result = new SortedDictionary<string, PathModel>();
        if (_readFiles.Add(dxfFilePath)) {
            DxfDocument? d = DxfHelper.LoadDxfDocument(dxfFilePath, options.Dump, options.PathNamePattern,
                                                     out Dictionary<string, Linetype> layerLinetypes, messages);
            if (d != null) {
                Dictionary<PathName, PathModel> models = PathModel.TransformDxf2PathModel(dxfFilePath, d.Entities,
                    layerLinetypes, options, messages, this);
                foreach (var kvp in models) {
                    if (_models.TryAdd(kvp.Key, kvp.Value)) {
                        result.Add(kvp.Key.AsString(), kvp.Value);
                    } else {
                        messages.AddError(contextForErrors, $"Pfad {kvp.Key} schon einmal definiert in {kvp.Value.DxfFilePath}");
                    }
                }
            }
        }
        return result;
    }

    public PathModel Get(PathName path)
        => _models[path];

    public bool Contains(PathName path)
        => _models.ContainsKey(path);
}
