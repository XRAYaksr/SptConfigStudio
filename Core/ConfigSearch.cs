using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

public sealed record Hit(string File, string Title, string Path, string Key, string Preview, string Value)
{
    public string Location => $"{Title} · {Path}";
}

/// <summary>Cross-file search over keys, raw values and localized item names.</summary>
public static class ConfigSearch
{
    public static List<Hit> Run(Workspace ws, string term, int cap = 200)
    {
        var hits = new List<Hit>();
        if (string.IsNullOrWhiteSpace(term) || term.Trim().Length < 2) return hits;
        var q = term.Trim();

        foreach (var entry in ws.Files)
        {
            var doc = ws.Open(entry.FileName);
            if (doc is null) continue;
            Walk(doc.Root, "", doc.FileName, entry.Title, q, hits, ws.Items, cap);
            if (hits.Count >= cap) break;
        }
        return hits;
    }

    private static void Walk(JsonNode? node, string path, string file, string title, string q,
        List<Hit> hits, ItemLookup items, int cap)
    {
        if (hits.Count >= cap || node is null) return;

        switch (node)
        {
            case JsonObject obj:
                foreach (var p in obj)
                {
                    var childPath = path.Length == 0 ? p.Key : path + "." + p.Key;
                    if (p.Key.Contains(q, StringComparison.OrdinalIgnoreCase))
                        Add(hits, file, title, childPath, p.Key, DiffEngine.Summarise(p.Value), Leaf(p.Value, items));
                    Walk(p.Value, childPath, file, title, q, hits, items, cap);
                    if (hits.Count >= cap) return;
                }
                break;
            case JsonArray arr:
                for (var i = 0; i < arr.Count; i++)
                {
                    var childPath = $"{path}[{i}]";
                    var text = Leaf(arr[i], items);
                    if (text.Contains(q, StringComparison.OrdinalIgnoreCase))
                        Add(hits, file, title, childPath, $"[{i}]", text, text);
                    Walk(arr[i], childPath, file, title, q, hits, items, cap);
                    if (hits.Count >= cap) return;
                }
                break;
            case JsonValue v:
                var leaf = Leaf(v, items);
                if (leaf.Contains(q, StringComparison.OrdinalIgnoreCase) && path.Length > 0 && DiffEngine.Summarise(v) != leaf)
                    Add(hits, file, title, path, Last(path), DiffEngine.Summarise(v), leaf);
                break;
        }
    }

    private static string Leaf(JsonNode? n, ItemLookup items)
    {
        if (n is not JsonValue v) return "";
        var raw = v.TryGetValue(out string? s) ? s ?? "" : v.ToJsonString().Trim('"');
        if (raw.Length == 0) return "";
        if (!ItemLookup.LooksLikeId(raw)) return raw;
        var name = items.Find(raw)?.Display;
        return items.IsReady && !string.IsNullOrEmpty(name) ? $"{raw} {name}" : raw;
    }

    private static string Last(string path)
    {
        var dot = path.LastIndexOf('.');
        var last = dot >= 0 ? path[(dot + 1)..] : path;
        var bracket = last.IndexOf('[');
        return bracket >= 0 ? last[..bracket] : last;
    }

    private static void Add(List<Hit> hits, string file, string title, string path, string key, string preview, string value)
    {
        if (hits.Any(h => h.File == file && h.Path == path)) return;
        hits.Add(new Hit(file, title, path, key, preview, value));
    }
}
