using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

public enum ChangeKind { Modified, Added, Removed }

public sealed record Change(string Path, ChangeKind Kind, string Old, string New);

/// <summary>
/// Compares the edited DOM against the text that was loaded, at leaf level. Used for the save confirmation,
/// so a "保存" click always shows what will actually change before a backup is taken.
/// </summary>
public static class DiffEngine
{
    public static List<Change> Compute(string baselineText, JsonNode current, int limit = 400)
    {
        var changes = new List<Change>();
        var baseline = ConfigDocument.Parse(baselineText);
        if (baseline is null) return changes;
        Walk("", baseline, current, changes, limit);
        return changes;
    }

    private static void Walk(string path, JsonNode? a, JsonNode? b, List<Change> outp, int limit)
    {
        if (outp.Count >= limit) return;
        if (JsonNode.DeepEquals(a, b)) return;

        if (SameShape(a, b))
        {
            switch (a)
            {
                case JsonObject oa:
                {
                    var ob = (JsonObject)b!;
                    foreach (var p in oa)
                    {
                        if (outp.Count >= limit) return;
                        var childPath = Child(path, p.Key);
                        if (ob.ContainsKey(p.Key)) Walk(childPath, p.Value, ob[p.Key], outp, limit);
                        else outp.Add(new Change(childPath, ChangeKind.Removed, Summarise(p.Value), ""));
                    }
                    foreach (var p in ob)
                        if (!oa.ContainsKey(p.Key))
                            outp.Add(new Change(Child(path, p.Key), ChangeKind.Added, "", Summarise(p.Value)));
                    return;
                }
                case JsonArray aa:
                {
                    var ab = (JsonArray)b!;
                    var common = Math.Min(aa.Count, ab.Count);
                    for (var i = 0; i < common; i++)
                    {
                        if (outp.Count >= limit) return;
                        Walk($"{path}[{i}]", aa[i], ab[i], outp, limit);
                    }
                    for (var i = common; i < aa.Count; i++)
                        outp.Add(new Change($"{path}[{i}]", ChangeKind.Removed, Summarise(aa[i]), ""));
                    for (var i = common; i < ab.Count; i++)
                        outp.Add(new Change($"{path}[{i}]", ChangeKind.Added, "", Summarise(ab[i])));
                    return;
                }
            }
        }

        outp.Add(new Change(path == "" ? "(根)" : path, ChangeKind.Modified, Summarise(a), Summarise(b)));
    }

    private static bool SameShape(JsonNode? a, JsonNode? b) =>
        a is JsonObject && b is JsonObject || a is JsonArray && b is JsonArray;

    private static string Child(string path, string key) => path.Length == 0 ? key : path + "." + key;

    public static string Summarise(JsonNode? n) => n switch
    {
        null => "null",
        JsonObject o => $"{{{o.Count} 项}}",
        JsonArray a => $"[{a.Count} 项]",
        JsonValue v => Short(v.ToJsonString()),
        _ => ""
    };

    private static string Short(string s)
    {
        s = s.Trim('"');
        return s.Length <= 64 ? s : s[..61] + "…";
    }
}
