using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

public enum FieldKind
{
    Toggle,
    Number,
    Percent,
    Duration,
    Text,
    Enum,
    IdRef,
    IdList,
    IdMap,
    IdMapRange,
    Range,          // { min, max }
    Group,         // generic object
    ScalarList,    // array of strings/numbers
    ObjectList,    // array of objects
    Raw            // anything unusual
}

/// <summary>
/// One editable row. Wraps a slot in the live JSON DOM (an object property or an array element) and knows
/// which editor the UI should show for it. Children are built lazily so 400 KB files stay responsive.
/// </summary>
public sealed class FieldNode
{
    public required string Key { get; init; }
    public required string Path { get; init; }
    public required JsonNode Container { get; init; }
    public FieldKind Kind { get; set; }
    public FieldNode? Owner { get; init; }
    public int Index { get; init; } = -1;

    /// <summary>Owning config file name, used to resolve curated overrides.</summary>
    public string File { get; init; } = "";

    public string Label { get; set; } = "";
    public string Hint { get; set; } = "";
    public string Unit { get; set; } = "";
    public double? Min { get; set; }
    public double? Max { get; set; }
    public string[]? EnumOptions { get; set; }

    /// <summary>Fallback used when the user has never toggled this node (top-level sections start open).</summary>
    public bool DefaultExpanded { get; set; }

    /// <summary>The whole-document node has no host property slot, so RawValue is null and its data is the container itself.</summary>
    public bool IsDocumentRoot { get; init; }

    public bool Expanded
    {
        get => Expansion.Get(File, Path) ?? DefaultExpanded;
        set => Expansion.Set(File, Path, value);
    }

    public bool IsLeaf => Kind is not (FieldKind.Group or FieldKind.ObjectList or FieldKind.IdMap
        or FieldKind.IdMapRange or FieldKind.Range or FieldKind.ScalarList or FieldKind.IdList);

    private List<FieldNode>? _children;
    public bool HasChildrenCache => _children is not null;
    public IReadOnlyList<FieldNode> Children
    {
        get
        {
            _children ??= BuildChildren();
            return _children;
        }
    }

    public ItemLookup? Lookup { get; set; }

    private bool IsArraySlot => Index >= 0;

    public JsonNode? RawValue => IsArraySlot
        ? ((JsonArray)Container)[Index]
        : ((JsonObject)Container).TryGetPropertyValue(Key, out var v) ? v : null;

    public void SetValue(JsonNode? node)
    {
        if (IsArraySlot) ((JsonArray)Container)[Index] = node;
        else ((JsonObject)Container)[Key] = node;
    }

    public void Remove()
    {
        if (IsArraySlot) ((JsonArray)Container).RemoveAt(Index);
        else ((JsonObject)Container).Remove(Key);
    }

    public JsonObject? AsObject() => RawValue as JsonObject;
    public JsonArray? AsArray() => RawValue as JsonArray;

    // ---------- scalar access ----------

    public bool GetBool() => RawValue is JsonValue v && v.TryGetValue(out bool b) && b;

    public void SetBool(bool value) => SetValue(JsonValue.Create(value));

    public double GetNumber()
    {
        if (RawValue is not JsonValue v) return 0;
        if (v.TryGetValue(out double d)) return d;
        if (v.TryGetValue(out long l)) return l;
        if (v.TryGetValue(out int i)) return i;
        var txt = v.ToJsonString();
        return double.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) ? p : 0;
    }

    public bool WasInteger()
    {
        if (RawValue is not JsonValue v) return false;
        if (v.TryGetValue(out long _)) return true;
        if (v.TryGetValue(out int _)) return true;
        var txt = v.ToJsonString();
        return long.TryParse(txt, NumberStyles.Any, CultureInfo.InvariantCulture, out _);
    }

    public void SetNumber(double value)
    {
        if (WasInteger() && Math.Abs(value - Math.Round(value)) < 1e-9 && Math.Round(value) is >= int.MinValue and <= int.MaxValue)
            SetValue(JsonValue.Create((long)Math.Round(value)));
        else
            SetValue(JsonValue.Create(value));
    }

    public string GetString()
    {
        if (RawValue is not JsonValue v) return "";
        if (v.TryGetValue(out string? s)) return s ?? "";
        var json = v.ToJsonString();
        return json.StartsWith('"') ? json.Trim('"') : json;
    }

    public void SetString(string value) => SetValue(JsonValue.Create(value));

    /// <summary>Replaces a node wholesale from pasted/raw JSON text. Returns false when the text is invalid.</summary>
    public bool TrySetRawJson(string text)
    {
        try
        {
            var node = ConfigDocument.Parse(text);
            if (node is null) return false;
            SetValue(node);
            _children = null;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    public string ValueSummary()
    {
        return Kind switch
        {
            FieldKind.Toggle => GetBool() ? "开" : "关",
            FieldKind.Number or FieldKind.Percent or FieldKind.Duration => NumberText(GetNumber()),
            FieldKind.Text or FieldKind.Enum => GetString(),
            FieldKind.IdRef => Lookup?.Describe(GetString()) ?? GetString(),
            FieldKind.Group => $"{Children.Count} 项",
            FieldKind.IdList or FieldKind.ScalarList => $"{AsArray()?.Count ?? 0} 项",
            FieldKind.IdMap or FieldKind.IdMapRange => $"{AsObject()?.Count ?? 0} 项",
            FieldKind.ObjectList => $"{AsArray()?.Count ?? 0} 条",
            FieldKind.Range => "min / max",
            _ => RawValue?.ToJsonString() ?? ""
        };
    }

    public static string NumberText(double d) =>
        Math.Abs(d - Math.Round(d)) < 1e-9 ? ((long)Math.Round(d)).ToString(CultureInfo.InvariantCulture)
                                           : d.ToString("0.######", CultureInfo.InvariantCulture);

    public static string DurationText(double seconds)
    {
        if (seconds < 0) return "不覆写";
        var s = seconds;
        if (s < 60) return $"{s:0.#} 秒";
        if (s < 3600) return $"{s / 60:0.#} 分钟";
        if (s < 86400) return $"{s / 3600:0.##} 小时";
        return $"{s / 86400:0.##} 天";
    }

    // ---------- tree building ----------

    public List<FieldNode> BuildChildren()
    {
        var list = new List<FieldNode>();
        switch (IsDocumentRoot ? Container : RawValue)
        {
            case JsonObject obj:
                foreach (var p in obj)
                {
                    var path = Path.Length == 0 ? p.Key : Path + "." + p.Key;
                    list.Add(Factory.Build(File, p.Key, path, obj, -1, p.Value, this, Lookup));
                }
                break;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    var el = arr[i];
                    var label = el is JsonValue ? (Factory.AsString(el) ?? $"第 {i + 1} 项") : $"第 {i + 1} 项";
                    list.Add(Factory.Build(File, label, $"{Path}[{i}]", arr, i, el, this, Lookup));
                }
                break;
        }
        return list;
    }

    public void Invalidate()
    {
        _children = null;
        Owner?.Invalidate();
    }

    /// <summary>Walks the subtree, materialising children, up to <paramref name="maxNodes"/> nodes.</summary>
    public IEnumerable<FieldNode> Flatten(int maxNodes = 20000)
    {
        var queue = new Queue<FieldNode>();
        queue.Enqueue(this);
        var seen = 0;
        while (queue.Count > 0 && seen < maxNodes)
        {
            var cur = queue.Dequeue();
            seen++;
            yield return cur;
            if (cur.IsLeaf) continue;
            foreach (var child in cur.Children) queue.Enqueue(child);
        }
    }
}

/// <summary>
/// Open/closed state for editor sections. FieldNode instances are rebuilt constantly, so this has to live
/// outside them or every save/reload would collapse the tree.
/// </summary>
public static class Expansion
{
    private static readonly Dictionary<string, bool> State = new(StringComparer.Ordinal);

    private static string Key(string file, string path) => file + "|" + path;

    public static bool? Get(string file, string path) => State.TryGetValue(Key(file, path), out var v) ? v : null;
    public static bool Has(string file, string path) => State.ContainsKey(Key(file, path));
    public static void Set(string file, string path, bool open) => State[Key(file, path)] = open;

    public static void ResetFor(string file)
    {
        var prefix = file + "|";
        foreach (var k in State.Keys.Where(x => x.StartsWith(prefix, StringComparison.Ordinal)).ToList())
            State.Remove(k);
    }
}

/// <summary>Infers an editor kind from a key + value shape, then applies curated overrides.</summary>
public static class Factory
{
    public static FieldNode Build(string file, string key, string path, JsonNode container, int index, JsonNode? value, FieldNode? owner, ItemLookup? lookup)
    {
        var node = new FieldNode
        {
            File = file,
            Key = key,
            Path = path,
            Container = container,
            Index = index,
            Owner = owner,
            Lookup = lookup,
            Label = key
        };

        node.Kind = Classify(key, value);
        MetadataCatalog.Apply(node);
        return node;
    }

    /// <summary>Root node for a whole config file; its children are the top-level sections.</summary>
    public static FieldNode BuildRoot(ConfigDocument doc, ItemLookup? lookup) => new()
    {
        File = doc.FileName,
        Key = doc.FileName,
        Path = "",
        Container = doc.Root,
        IsDocumentRoot = true,
        Lookup = lookup,
        Kind = FieldKind.Group,
        Label = MetadataCatalog.Meta(doc.FileName).Title
    };

    public static FieldKind Classify(string key, JsonNode? value)
    {
        switch (value)
        {
            case null:
                return FieldKind.Raw;
            case JsonObject obj:
                if (IsMinMax(obj)) return FieldKind.Range;
                if (obj.Count == 0) return FieldKind.Group;
                if (IsIdToNumber(obj)) return FieldKind.IdMap;
                if (IsIdToRange(obj)) return FieldKind.IdMapRange;
                return FieldKind.Group;
            case JsonArray arr:
                if (arr.Count == 0) return FieldKind.IdList;
                var allScalar = arr.All(x => x is JsonValue);
                if (allScalar)
                {
                    var allIds = arr.OfType<JsonValue>().All(v => ItemLookup.LooksLikeId(AsString(v)));
                    return allIds ? FieldKind.IdList : FieldKind.ScalarList;
                }
                if (arr.All(x => x is JsonObject)) return FieldKind.ObjectList;
                return FieldKind.Raw;
            case JsonValue jv:
                if (jv.TryGetValue(out bool _)) return FieldKind.Toggle;
                if (IsNumeric(jv))
                {
                    var lower = key.ToLowerInvariant();
                    if (lower.Contains("percent") || lower.EndsWith("chance")) return FieldKind.Percent;
                    if (lower.Contains("second") || lower.Contains("interval") && lower.EndsWith("seconds")) return FieldKind.Duration;
                    return FieldKind.Number;
                }
                if (ItemLookup.LooksLikeId(AsString(jv))) return FieldKind.IdRef;
                return FieldKind.Text;
        }
        return FieldKind.Raw;
    }

    public static string? AsString(JsonNode? n) => n is JsonValue v && v.TryGetValue(out string? s) ? s : null;

    public static bool IsNumeric(JsonValue v) =>
        v.TryGetValue(out double _) || v.TryGetValue(out long _) || v.TryGetValue(out int _) || v.TryGetValue(out decimal _);

    public static bool IsMinMax(JsonObject obj) =>
        obj.Count == 2 && obj.ContainsKey("min") && obj.ContainsKey("max")
        && obj["min"] is JsonValue && obj["max"] is JsonValue
        && IsNumeric((JsonValue)obj["min"]!) && IsNumeric((JsonValue)obj["max"]!);

    private static bool IsIdToNumber(JsonObject obj) =>
        obj.Count > 0 && obj.All(p => ItemLookup.LooksLikeId(p.Key) && p.Value is JsonValue jv && IsNumeric(jv));

    private static bool IsIdToRange(JsonObject obj) =>
        obj.Count > 0 && obj.All(p => ItemLookup.LooksLikeId(p.Key) && p.Value is JsonObject o && IsMinMax(o));

    /// <summary>Best-effort Chinese label from a camelCase key, keeping the raw key visible in the UI.</summary>
    public static (string label, bool full) Gloss(string key)
    {
        var words = SplitCamel(key);
        var parts = new List<string>(words.Count);
        var mapped = 0;
        foreach (var w in words)
        {
            if (MetadataCatalog.Words.TryGetValue(w.ToLowerInvariant(), out var zh))
            {
                parts.Add(zh);
                mapped++;
            }
            else parts.Add(w);
        }
        return (string.Concat(parts), mapped == words.Count);
    }

    private static List<string> SplitCamel(string s)
    {
        var outp = new List<string>();
        var cur = new System.Text.StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (c is '_' or '-' or ' ')
            {
                if (cur.Length > 0) { outp.Add(cur.ToString()); cur.Clear(); }
                continue;
            }
            if (char.IsUpper(c) && cur.Length > 0 && (i + 1 < s.Length && !char.IsUpper(s[i + 1]) || char.IsLower(s[i - 1])))
            {
                outp.Add(cur.ToString());
                cur.Clear();
            }
            cur.Append(c);
        }
        if (cur.Length > 0) outp.Add(cur.ToString());
        return outp.Count == 0 ? [s] : outp;
    }
}
