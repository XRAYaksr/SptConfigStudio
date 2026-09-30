using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;

namespace SptConfigStudio.Core;

public sealed record ItemInfo(string Id, string Name, string NameEn, string ParentId, string Type, int? Price)
{
    public string Display => string.IsNullOrWhiteSpace(Name) ? (string.IsNullOrWhiteSpace(NameEn) ? Id : NameEn) : Name;
    public string PriceText => Price is > 0 ? $"{Price:N0} ₽" : "—";
}

/// <summary>
/// Resolves mongoids to readable Chinese names using the server database, so id lists in configs stop being
/// opaque hex. Built once, off the UI thread, on first use.
/// </summary>
public sealed class ItemLookup(AppEnv env)
{
    private readonly ConcurrentDictionary<string, ItemInfo> _byId = new(StringComparer.Ordinal);
    private volatile bool _loaded;
    private Task? _loading;

    public bool IsReady => _loaded;
    public event Action? Ready;

    public Task EnsureLoadedAsync() => _loading ??= Task.Run(Load);

    private void Load()
    {
        var itemsPath = Path.Combine(env.TemplateDir, "items.json");
        var pricesPath = Path.Combine(env.TemplateDir, "prices.json");
        var zhPath = Path.Combine(env.DatabaseDir, "locales", "global", "ch.json");
        var enPath = Path.Combine(env.DatabaseDir, "locales", "global", "en.json");

        using var items = File.Exists(itemsPath) ? JsonDocument.Parse(File.ReadAllText(itemsPath)) : null;
        using var prices = File.Exists(pricesPath) ? JsonDocument.Parse(File.ReadAllText(pricesPath)) : null;
        using var zh = File.Exists(zhPath) ? JsonDocument.Parse(File.ReadAllText(zhPath)) : null;
        using var en = File.Exists(enPath) ? JsonDocument.Parse(File.ReadAllText(enPath)) : null;

        var priceMap = new Dictionary<string, int>(StringComparer.Ordinal);
        if (prices is not null && prices.RootElement.ValueKind == JsonValueKind.Object)
            foreach (var p in prices.RootElement.EnumerateObject())
                if (p.Value.ValueKind == JsonValueKind.Number && p.Value.TryGetInt32(out var v)) priceMap[p.Name] = v;

        string Loc(JsonDocument? doc, string id, string suffix)
        {
            if (doc is null) return "";
            return doc.RootElement.TryGetProperty(id + " " + suffix, out var e) && e.ValueKind == JsonValueKind.String
                ? e.GetString() ?? "" : "";
        }

        if (items is not null && items.RootElement.ValueKind == JsonValueKind.Object)
        {
            foreach (var entry in items.RootElement.EnumerateObject())
            {
                var id = entry.Name;
                var r = entry.Value;
                string Get(string name) => r.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
                var parent = Get("_parent");
                var rawName = Get("_name");
                var type = Get("_type");
                _byId[id] = new ItemInfo(
                    id,
                    Loc(zh, id, "Name"),
                    Loc(en, id, "Name").Length > 0 ? Loc(en, id, "Name") : rawName,
                    parent,
                    type,
                    priceMap.TryGetValue(id, out var pr) ? pr : null);
            }
        }

        // Nodes (item families such as Weapon / Money) are not in items.json but do carry locale entries.
        foreach (var doc in new[] { zh })
        {
            if (doc is null) continue;
            foreach (var p in doc.RootElement.EnumerateObject())
            {
                if (!p.Name.EndsWith(" Name", StringComparison.Ordinal)) continue;
                if (p.Value.ValueKind != JsonValueKind.String) continue;
                var id = p.Name[..^" Name".Length];
                if (id.Length != 24 || _byId.ContainsKey(id)) continue;
                _byId[id] = new ItemInfo(id, p.Value.GetString() ?? "", "", "", "Node",
                    priceMap.TryGetValue(id, out var pr) ? pr : null);
            }
        }

        _loaded = true;
        Ready?.Invoke();
    }

    public static bool LooksLikeId(string? s) =>
        s is { Length: 24 } && s.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    public ItemInfo? Find(string? id) =>
        id is not null && _byId.TryGetValue(id, out var v) ? v : null;

    public string Describe(string? id)
    {
        var info = Find(id);
        return info?.Display ?? (LooksLikeId(id) ? "未知编号" : id ?? "");
    }

    public string FamilyOf(string id)
    {
        var chain = new List<string>();
        var cur = Find(id);
        var guard = 0;
        while (cur is not null && guard++ < 12)
        {
            if (chain.Count > 0 && chain[^1] == cur.Display) break;
            chain.Add(cur.Display);
            if (string.IsNullOrEmpty(cur.ParentId)) break;
            cur = Find(cur.ParentId);
        }
        return string.Join(" › ", chain);
    }

    public IEnumerable<ItemInfo> Search(string term, int take = 60)
    {
        if (string.IsNullOrWhiteSpace(term)) return [];
        var q = term.Trim();
        return _byId.Values
            .Where(x => x.Type == "Item" || x.Type == "Node")
            .Where(x => x.Display.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || x.NameEn.Contains(q, StringComparison.OrdinalIgnoreCase)
                        || x.Id.Equals(q, StringComparison.OrdinalIgnoreCase))
            .OrderBy(x => x.Display.Length)
            .Take(take);
    }
}
