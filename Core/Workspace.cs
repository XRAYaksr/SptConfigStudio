using System.IO;
using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

public sealed record ConfigFileEntry(string FileName, FileMeta Meta, string Path)
{
    public string Title => Meta.Title;
    public string Category => Meta.Category;
    public string Description => Meta.Description;
    public bool Advanced => Meta.Advanced;
}

/// <summary>Application state: discovered files, lazily parsed documents, and the backup-gated save path.</summary>
public sealed class Workspace
{
    public AppEnv Env { get; }
    public BackupService Backups { get; }
    public ItemLookup Items { get; }
    public ServerStatus Status { get; } = new();

    private readonly Dictionary<string, ConfigDocument> _loaded = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _failed = [];

    public event Action? Changed;

    public Workspace(AppEnv env)
    {
        Env = env;
        Backups = new BackupService(env);
        Items = new ItemLookup(env);
    }

    public List<ConfigFileEntry> Files { get; private set; } = [];

    public void DiscoverFiles()
    {
        Files = Directory.Exists(Env.ConfigDir)
            ? new DirectoryInfo(Env.ConfigDir).GetFiles("*.json")
                .Select(f => new ConfigFileEntry(f.Name, MetadataCatalog.Meta(f.Name), f.FullName))
                .OrderBy(x => MetadataCatalog.Meta(x.FileName).Category)
                .ThenBy(x => x.FileName)
                .ToList()
            : [];
    }

    public IReadOnlyList<string> Failed => _failed;

    public ConfigDocument? Open(string fileName)
    {
        if (_loaded.TryGetValue(fileName, out var cached)) return cached;
        try
        {
            var doc = new ConfigDocument(Path.Combine(Env.ConfigDir, fileName));
            doc.Changed += () => Changed?.Invoke();
            _loaded[fileName] = doc;
            return doc;
        }
        catch (Exception ex)
        {
            _failed.Add($"{fileName}: {ex.Message}");
            return null;
        }
    }

    public IEnumerable<ConfigDocument> Loaded => _loaded.Values;

    public List<ConfigDocument> Dirty() => _loaded.Values.Where(d => d.IsDirty).ToList();

    public bool AnyDirty => _loaded.Values.Any(d => d.IsDirty);

    public void MarkChanged() => Changed?.Invoke();

    /// <summary>Documents whose untouched DOM does not round-trip to the on-disk formatting.</summary>
    public List<string> ChurnRisk() => _loaded.Values.Where(d => !d.PreservesOriginalFormatting).Select(d => d.FileName).ToList();

    public List<ConfigDocument> WithFormatChurn(IEnumerable<ConfigDocument> saving) =>
        saving.Where(d => !d.PreservesOriginalFormatting).ToList();

    public BackupEntry? LastBackup { get; private set; }

    /// <summary>
    /// Takes a verified zip first, then writes. Returns null when the user's own gate (a confirm dialog) or the
    /// backup failed; callers must treat a null as "nothing was written".
    /// </summary>
    public SaveResult Save(IReadOnlyList<ConfigDocument> targets, string reason, BackupScope scope)
    {
        if (targets.Count == 0) return new SaveResult(null, [], 0);

        BackupEntry backup;
        try
        {
            backup = Backups.Create(reason, scope);
        }
        catch (Exception ex)
        {
            return new SaveResult(null, [], -1) { Error = "备份失败，已中止写入：" + ex.Message };
        }
        LastBackup = backup;

        var written = new List<string>();
        foreach (var doc in targets)
        {
            try
            {
                doc.WriteToDisk();
                written.Add(doc.FileName);
            }
            catch (Exception ex)
            {
                return new SaveResult(backup, written, written.Count)
                {
                    Error = $"已写入 {string.Join("、", written)}，但 {doc.FileName} 失败：{ex.Message}\n备份压缩包 {backup.Name} 可用于还原。"
                };
            }
        }
        Changed?.Invoke();
        return new SaveResult(backup, written, written.Count);
    }

    public sealed record SaveResult(BackupEntry? Backup, IReadOnlyList<string> Written, int Count)
    {
        public string? Error { get; init; }
        public bool Ok => Error is null && Count > 0;
    }

    /// <summary>Reloads every cached document from disk, dropping unsaved edits.</summary>
    public void ReloadAll()
    {
        foreach (var doc in _loaded.Values.ToList())
        {
            try { doc.Reload(); }
            catch (Exception ex) { _failed.Add($"{doc.FileName}: {ex.Message}"); }
        }
        Changed?.Invoke();
    }

    public List<Change> ChangesFor(ConfigDocument doc)
    {
        try { return DiffEngine.Compute(doc.BaselineText, doc.Root); }
        catch { return []; }
    }
}
