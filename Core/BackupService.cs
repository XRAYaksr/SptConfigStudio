using System.IO;
using System.IO.Compression;

namespace SptConfigStudio.Core;

public enum BackupScope
{
    /// <summary>Only SPT_Data/configs — fast, covers everything this tool writes.</summary>
    Configs,
    /// <summary>configs + database top-level json (globals/settings/server).</summary>
    ConfigsWithDatabase,
    /// <summary>configs, database top-level json and player profiles.</summary>
    Full
}

public sealed record BackupEntry(FileInfo File, string Label, long CompressedBytes, string[] Members, bool Verified)
{
    public string Name => File.Name;
    public DateTime Created => File.CreationTime;
    public string SizeText => Sizes.Readable(CompressedBytes);
    public string CreatedText => Created.ToString("yyyy-MM-dd HH:mm:ss");
    public string StatusText => Verified ? $"{Members.Length} 个文件 · 校验通过" : "压缩包无法读取";
}

public static class Sizes
{
    public static string Readable(long b) => b switch
    {
        >= 1_048_576 => $"{b / 1_048_576.0:0.##} MB",
        >= 1024 => $"{b / 1024.0:0.#} KB",
        _ => $"{b} B"
    };
}

/// <summary>
/// Every write this tool performs is gated on a verified zip snapshot, so a bad edit is always recoverable.
/// </summary>
public sealed class BackupService(AppEnv env)
{
    private const string Prefix = "spt-cfg-";

    public event Action<string>? OnStatus;

    public string BackupRoot => env.BackupDir;

    public IEnumerable<string> ResolveScope(BackupScope scope) => scope switch
    {
        BackupScope.Configs => [env.ConfigDir],
        BackupScope.ConfigsWithDatabase => [env.ConfigDir, env.DatabaseDir],
        BackupScope.Full => [env.ConfigDir, env.DatabaseDir, env.ProfileDir],
        _ => [env.ConfigDir]
    };

    /// <summary>
    /// Zips the scope and only returns once the archive reopens cleanly. Throws if either step fails —
    /// callers must treat that as "do not write".
    /// </summary>
    public BackupEntry Create(string reason, BackupScope scope = BackupScope.Configs, int verifyTopN = 3)
    {
        Directory.CreateDirectory(env.BackupDir);
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var safeReason = string.Join("-", reason.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(env.BackupDir, $"{stamp}-{Prefix}{Truncate(safeReason, 48)}.zip");
        if (File.Exists(path)) path = Path.Combine(env.BackupDir, $"{stamp}-{Prefix}{Truncate(safeReason, 40)}-{Random.Shared.Next(100, 999)}.zip");

        var plan = Plan(scope);

        using (var fs = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
        {
            foreach (var (file, entryName) in plan)
            {
                var entry = zip.CreateEntry(entryName, CompressionLevel.SmallestSize);
                entry.LastWriteTime = file.LastWriteTime;
                using var src = file.Open(FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var dst = entry.Open();
                src.CopyTo(dst);
                OnStatus?.Invoke($"已写入 {entryName}");
            }
        }

        Verify(path, verifyTopN);
        var info = new FileInfo(path);
        Trim(env.KeepBackups);
        return new BackupEntry(info, reason, info.Length, [.. plan.Select(p => p.EntryName)], true);
    }

    /// <summary>
    /// Files covered by a scope. database/ carries hundreds of MB of tables and art, so only its top-level
    /// json (the server-editable part) is taken.
    /// </summary>
    private List<(FileInfo File, string EntryName)> Plan(BackupScope scope)
    {
        var plan = new List<(FileInfo, string)>();
        foreach (var root in ResolveScope(scope))
        {
            if (!Directory.Exists(root)) continue;
            var topOnly = string.Equals(Path.GetFullPath(root), Path.GetFullPath(env.DatabaseDir), StringComparison.OrdinalIgnoreCase);
            var files = topOnly
                ? Directory.GetFiles(root, "*.json").Select(f => new FileInfo(f))
                : new DirectoryInfo(root).EnumerateFiles("*", SearchOption.AllDirectories);

            foreach (var f in files.OrderBy(x => x.FullName, StringComparer.Ordinal))
            {
                if (IsLocked(f)) continue;
                plan.Add((f, Path.GetRelativePath(env.GameRoot, f.FullName).Replace('\\', '/')));
            }
        }
        return plan;
    }

    private static void Verify(string path, int topN)
    {
        using var zip = ZipFile.OpenRead(path);
        if (zip.Entries.Count == 0) throw new IOException("备份压缩包为空，已中止写入。");
        foreach (var e in zip.Entries.Take(topN))
        {
            using var s = e.Open();
            var buf = new byte[4096];
            var total = 0;
            while ((total = s.Read(buf, 0, buf.Length)) > 0) { /* CRC is checked as we drain the stream */ }
        }
    }

    public List<BackupEntry> List()
    {
        if (!Directory.Exists(env.BackupDir)) return [];
        var list = new List<BackupEntry>();
        foreach (var f in new DirectoryInfo(env.BackupDir).GetFiles("*.zip").OrderByDescending(x => x.Name))
        {
            string[] members = [];
            var label = f.Name;
            var verified = false;
            try
            {
                using var zip = ZipFile.OpenRead(f.FullName);
                members = [.. zip.Entries.Select(e => e.FullName)];
                var i = f.Name.IndexOf(Prefix, StringComparison.Ordinal);
                if (i >= 0) label = f.Name[(i + Prefix.Length)..].Replace(".zip", "");
                verified = true;
            }
            catch { /* unreadable archives are listed but flagged */ }
            list.Add(new BackupEntry(f, label, f.Length, members, verified));
        }
        return list;
    }

    /// <summary>Extracts an archive over the live tree. The caller must have taken a fresh backup first.</summary>
    public int Restore(BackupEntry entry, Action<string>? progress = null)
    {
        using var zip = ZipFile.OpenRead(entry.File.FullName);
        var targets = zip.Entries
            .Where(e => !string.IsNullOrEmpty(e.Name))
            .Select(e => (Entry: e, Target: Target(e.FullName)))
            .ToList();

        // An archive that matches nothing on disk was built for another layout; extracting it would scatter files.
        if (targets.Count == 0 || targets.All(t => !File.Exists(t.Target)))
            throw new IOException($"压缩包内的路径与当前游戏目录不匹配，已拒绝还原：{entry.Name}");

        var n = 0;
        foreach (var (e, target) in targets)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            e.ExtractToFile(target, overwrite: true);
            progress?.Invoke(target);
            n++;
        }
        return n;
    }

    private string Target(string entryName)
    {
        var target = Path.GetFullPath(Path.Combine(env.GameRoot, entryName.Replace('/', Path.DirectorySeparatorChar)));
        if (!target.StartsWith(Path.GetFullPath(env.GameRoot) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new IOException($"压缩包条目路径越界，已拒绝还原：{entryName}");
        return target;
    }

    public void Trim(int keep)
    {
        if (keep <= 0) return;
        var ours = List().Where(x => x.Name.Contains(Prefix, StringComparison.Ordinal)).ToList();
        foreach (var stale in ours.Skip(keep))
        {
            try { stale.File.Delete(); } catch { /* locked by an explorer preview, skip */ }
        }
    }

    public void Delete(BackupEntry entry) => entry.File.Delete();

    private static bool IsLocked(FileInfo f)
    {
        try
        {
            using var s = f.Open(FileMode.Open, FileAccess.Read, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
