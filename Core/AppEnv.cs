using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

/// <summary>Locates the SPT installation and persists user preferences.</summary>
public sealed class AppEnv
{
    public required string GameRoot { get; init; }

    public string DataDir => Path.Combine(GameRoot, "SPT_Runtime", "SPT_Data");
    public string ConfigDir => Path.Combine(DataDir, "configs");
    public string DatabaseDir => Path.Combine(DataDir, "database");
    public string UserDir => Path.Combine(GameRoot, "SPT_Runtime", "user");
    public string ProfileDir => Path.Combine(UserDir, "profiles");
    public string BackupDir => Path.Combine(GameRoot, "Backups");
    public string TemplateDir => Path.Combine(DatabaseDir, "templates");

    public static string PrefPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SptConfigStudio", "prefs.json");

    public string? Error { get; init; }

    /// <summary>Walks up from the exe (and from the CWD) looking for SPT_Runtime/SPT_Data/configs.</summary>
    public static AppEnv Detect()
    {
        var fromPrefs = TryLoadPreferredRoot();
        if (fromPrefs is not null)
        {
            var e = new AppEnv { GameRoot = fromPrefs };
            if (IsSpt(e.GameRoot)) return e;
        }

        var candidates = new List<string>();
        foreach (var start in new[] { AppContext.BaseDirectory, Directory.GetCurrentDirectory() })
        {
            var dir = new DirectoryInfo(start);
            while (dir is not null)
            {
                candidates.Add(dir.FullName);
                dir = dir.Parent;
            }
        }

        foreach (var c in candidates.Where(IsSpt))
            return new AppEnv { GameRoot = Path.GetFullPath(c) };

        return new AppEnv
        {
            GameRoot = candidates.Count > 0 ? candidates[0] : @"C:\",
            Error = "未能自动定位 SPT 安装目录，请在设置中手动选择包含 SPT_Runtime\\SPT_Data 的游戏根目录。"
        };
    }

    public static bool IsSpt(string root) =>
        Directory.Exists(Path.Combine(root, "SPT_Runtime", "SPT_Data", "configs"));

    private static string? TryLoadPreferredRoot()
    {
        try { return LoadPrefs()["gameRoot"]?.GetValue<string>(); }
        catch { return null; }
    }

    public void SavePreferredRoot() => SetPref("gameRoot", GameRoot);

    public int KeepBackups
    {
        get { try { return LoadPrefs()["keepBackups"]?.GetValue<int>() ?? 30; } catch { return 30; } }
    }

    public void SetKeepBackups(int n) => SetPref("keepBackups", n);

    public static bool WizardDone
    {
        get { try { return LoadPrefs()["wizardDone"]?.GetValue<bool>() ?? false; } catch { return false; } }
    }

    public static void SetWizardDone(bool value) => SetPref("wizardDone", value);

    private static void SetPref(string key, JsonNode? value)
    {
        var prefs = LoadPrefs();
        prefs[key] = value;
        Directory.CreateDirectory(Path.GetDirectoryName(PrefPath)!);
        File.WriteAllText(PrefPath, JsonSerializer.Serialize(prefs, Prefs));
    }

    /// <summary>Prefs are read-modify-write: a corrupt file falls back to empty rather than throwing.</summary>
    private static JsonObject LoadPrefs()
    {
        try
        {
            if (File.Exists(PrefPath) && JsonNode.Parse(File.ReadAllText(PrefPath)) is JsonObject obj) return obj;
        }
        catch { /* corrupt prefs are not worth crashing over */ }
        return new JsonObject();
    }

    private static readonly JsonSerializerOptions Prefs = new() { WriteIndented = true };
}
