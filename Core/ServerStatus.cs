using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace SptConfigStudio.Core;

/// <summary>Tells the user whether SPT.Server.exe is live, and whether the web panel answers.</summary>
public sealed class ServerStatus
{
    private static readonly HttpClient Http = new(new SocketsHttpHandler
    {
        SslOptions = new SslClientAuthenticationOptions { RemoteCertificateValidationCallback = (_, _, _, _) => true }
    })
    { Timeout = TimeSpan.FromMilliseconds(1200) };

    public bool ProcessRunning { get; private set; }
    public bool PanelReachable { get; private set; }
    public string? PanelUrl { get; private set; }
    public string Version { get; private set; } = "";

    public async Task<ServerStatus> RefreshAsync(AppEnv env, CancellationToken token = default)
    {
        ProcessRunning = Process.GetProcessesByName("SPT.Server").Length > 0
                         || Process.GetProcessesByName("SPT.Server.exe").Length > 0;

        var port = ReadPort(env);
        PanelUrl = $"https://127.0.0.1:{port}";
        try
        {
            using var resp = await Http.GetAsync(PanelUrl + "/health", token);
            if (resp.IsSuccessStatusCode)
            {
                PanelReachable = true;
                var json = await resp.Content.ReadAsStringAsync(token);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("version", out var v)) Version = v.GetString() ?? "";
            }
        }
        catch
        {
            PanelReachable = false;
        }
        return this;
    }

    public static int ReadPort(AppEnv env)
    {
        try
        {
            var path = Path.Combine(env.ConfigDir, "http.json");
            if (File.Exists(path) && ConfigDocument.Parse(File.ReadAllText(path)) is JsonNode n
                && n["port"]?.GetValue<int>() is int p) return p;
        }
        catch { /* fall back to the SPT default */ }
        return 6969;
    }

    public string Label => PanelReachable
        ? $"服务端运行中 {(Version.Length > 0 ? "v" + Version : "")}"
        : ProcessRunning ? "服务端进程存在，面板未响应" : "服务端未运行";
}
