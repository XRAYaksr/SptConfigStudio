using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>
/// Zip archive browser: take a snapshot on demand, restore one, or drop one. Restoring is itself a write to the
/// server tree, so it takes a fresh safety archive of the current state first.
/// </summary>
public sealed class BackupView
{
    private readonly Workspace _ws;
    private readonly Action _refreshHost;
    private readonly StackPanel _list = new();
    private readonly TextBlock _status;
    private readonly ComboBox _scope = new();
    private readonly TextBox _reason;
    private readonly Button _create;

    public FrameworkElement Root { get; }

    public BackupView(Workspace ws, Action refreshHost)
    {
        _ws = ws;
        _refreshHost = refreshHost;
        _reason = new TextBox { Text = "手动备份", Width = 220, Height = 28, VerticalContentAlignment = VerticalAlignment.Center };
        _status = Ui.Text("", 12, "Brasa.TextDim");
        _create = Ui.Btn("创建备份", (_, _) => _ = CreateAsync(), primary: true);

        foreach (var label in BackupScopes.All.Select(x => x.Label)) _scope.Items.Add(label);
        _scope.SelectedIndex = 0;
        _scope.Width = 300;

        Root = Build();
        Reload();
    }

    private FrameworkElement Build()
    {
        var head = new StackPanel { Margin = Ui.T(0, 0, 0, 12) };
        head.Children.Add(Ui.Text("备份与还原", 20, weight: FontWeights.SemiBold));
        head.Children.Add(Ui.Text(
            "每次点「保存修改」都会先自动打包一份 zip 并校验，成功后才写入磁盘。这里可以手动补一份，或回到任意一份备份。",
            12, "Brasa.TextDim", wrap: true));

        var scopeHint = Ui.Text(BackupScopes.All[0].Hint, 11.5, "Brasa.TextDim");
        scopeHint.VerticalAlignment = VerticalAlignment.Center;
        _scope.SelectionChanged += (_, _) => scopeHint.Text = BackupScopes.All[Math.Max(0, _scope.SelectedIndex)].Hint;

        var keep = new TextBox { Text = _ws.Env.KeepBackups.ToString(), Width = 60, Height = 28, VerticalContentAlignment = VerticalAlignment.Center, HorizontalContentAlignment = HorizontalAlignment.Center };
        keep.LostFocus += (_, _) =>
        {
            if (int.TryParse(keep.Text.Trim(), out var n) && n > 0) _ws.Env.SetKeepBackups(n);
            keep.Text = _ws.Env.KeepBackups.ToString();
        };
        var keepBox = Ui.HBox(6, Ui.Text("保留份数", 12), keep, Ui.Text("（仅清理本工具生成的 spt-cfg-* 压缩包）", 11, "Brasa.TextDim"));
        keepBox.VerticalAlignment = VerticalAlignment.Center;

        var bar = new StackPanel { Orientation = Orientation.Horizontal, Margin = Ui.T(0, 0, 0, 6) };
        bar.Children.Add(_scope);
        bar.Children.Add(new Border { Child = _reason, Margin = Ui.T(8, 0, 0, 0) });
        bar.Children.Add(new Border { Child = _create, Margin = Ui.T(8, 0, 0, 0) });
        bar.Children.Add(new Border { Child = Ui.Btn("打开备份目录", (_, _) => OpenFolder()), Margin = Ui.T(8, 0, 0, 0) });

        var actions = Ui.HBox(14, bar, _status);
        keepBox.Margin = Ui.T(0, 0, 0, 10);

        var panel = Ui.VBox(head, new Border { Child = actions }, keepBox, _list);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = Ui.T(22, 18, 22, 22),
            Content = panel
        };
        return scroll;
    }

    public void Reload()
    {
        _list.Children.Clear();
        var entries = _ws.Backups.List();
        _list.Children.Add(Ui.Text($"备份目录：{_ws.Env.BackupDir}（共 {entries.Count} 个压缩包）", 12, "Brasa.TextDim"));
        _list.Children.Add(new Border { Height = 8 });
        if (entries.Count == 0)
        {
            _list.Children.Add(Ui.Text("还没有备份。点上方「创建备份」先把当前配置打包。", 12.5, "Brasa.Warn"));
            return;
        }
        foreach (var e in entries) _list.Children.Add(Row(e));
    }

    private FrameworkElement Row(BackupEntry e)
    {
        var left = new StackPanel();
        left.Children.Add(Ui.Text(e.CreatedText + "  ·  " + e.Label, 13, weight: FontWeights.SemiBold));
        left.Children.Add(Ui.Mono(e.Name, 11));
        left.Children.Add(Ui.Text($"{e.SizeText} · {e.StatusText}", 11.5, e.Verified ? "Brasa.TextDim" : "Brasa.Bad"));

        var restore = Ui.Btn("还原这一份", (_, _) => _ = RestoreAsync(e));
        restore.IsEnabled = e.Verified;
        restore.ToolTip = e.Verified
            ? "先给当前状态补一份备份，再用这个压缩包覆盖文件"
            : "压缩包读取失败，无法还原";
        var delete = Ui.Btn("删除", (_, _) => Delete(e));
        delete.Padding = Ui.T(10, 4, 10, 4);
        var locate = Ui.Btn("定位", (_, _) => Locate(e));
        locate.Padding = Ui.T(10, 4, 10, 4);

        var right = Ui.HBox(6, locate, delete, restore);
        right.VerticalAlignment = VerticalAlignment.Center;

        var grid = new Grid();
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(left, 0);
        Grid.SetColumn(right, 1);
        grid.Children.Add(left);
        grid.Children.Add(right);

        var card = Ui.Card(grid);
        card.Margin = Ui.T(0, 0, 0, 8);
        return card;
    }

    private void Delete(BackupEntry e)
    {
        if (MessageBox.Show($"删除备份压缩包 {e.Name}？\n释放磁盘空间后无法再回到这份配置。", "删除备份",
                MessageBoxButton.OKCancel, MessageBoxImage.Warning) != MessageBoxResult.OK) return;
        try
        {
            _ws.Backups.Delete(e);
            Reload();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "删除失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Locate(BackupEntry e)
    {
        try { Process.Start("explorer.exe", $"/select,\"{e.File.FullName}\""); }
        catch (Exception ex) { _status.Text = "无法打开资源管理器：" + ex.Message; }
    }

    private void OpenFolder()
    {
        try
        {
            Directory.CreateDirectory(_ws.Env.BackupDir);
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_ws.Env.BackupDir}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { _status.Text = "无法打开备份目录：" + ex.Message; }
    }

    private async Task CreateAsync()
    {
        var scope = BackupScopes.All[_scope.SelectedIndex >= 0 ? _scope.SelectedIndex : 0].Scope;
        var reason = string.IsNullOrWhiteSpace(_reason.Text) ? "手动备份" : _reason.Text.Trim();
        await RunBusy($"备份中（{reason}）…", () => _ws.Backups.Create(reason, scope),
            e =>
            {
                _status.Text = $"已创建 {e.Name}（{e.SizeText}，{e.Members.Length} 个文件，校验通过）";
                Reload();
            });
    }

    private async Task RestoreAsync(BackupEntry e)
    {
        var preview = e.Members.Length > 12 ? string.Join("\n", e.Members.Take(12)) + $"\n… 共 {e.Members.Length} 个文件" : string.Join("\n", e.Members);
        var ask = MessageBox.Show(
            $"用 {e.CreatedText} 的备份覆盖当前文件？\n\n{e.Name}\n{preview}\n\n" +
            "会先给当前状态补一份安全备份，再覆盖解压。还原完成后需要重启服务端才会生效。",
            "还原备份", MessageBoxButton.OKCancel, MessageBoxImage.Warning);
        if (ask != MessageBoxResult.OK) return;

        await RunBusy("还原中…", () =>
        {
            var safety = _ws.Backups.Create("还原前安全备份", BackupScope.Configs);
            var n = _ws.Backups.Restore(e);
            return (safety, n);
        }, r =>
        {
            _ws.ReloadAll();
            _refreshHost();
            Reload();
            _status.Text = $"已还原 {r.n} 个文件；还原前的状态保存在 {r.safety.Name}";
        });
    }

    /// <summary>Runs a backup call off the UI thread with a busy label, reporting failures instead of throwing.</summary>
    private async Task RunBusy<T>(string label, Func<T> work, Action<T> done)
    {
        _create.IsEnabled = false;
        _status.Text = label;
        try
        {
            var result = await Task.Run(work);
            done(result);
        }
        catch (Exception ex)
        {
            _status.Text = "失败：" + ex.Message;
            MessageBox.Show(ex.Message, label + "失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _create.IsEnabled = true;
        }
    }

    /// <summary>Toolbar shortcut: pick an archive and restore it without leaving the current editor.</summary>
    public static void PickAndRestore(Window owner, Workspace ws, Action refreshHost)
    {
        var view = new BackupView(ws, refreshHost);
        var win = new Window
        {
            Title = "还原备份",
            Owner = owner,
            Width = 860,
            Height = 620,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = Ui.Res("Brasa.Background"),
            ShowInTaskbar = false
        };

        var note = Ui.Text("下面是本工具生成的压缩包。选择一份后确认，会先备份当前状态再覆盖。", 12, "Brasa.TextDim", wrap: true);
        note.Margin = Ui.T(0, 0, 0, 10);
        var content = new Border { Child = view.Root, Padding = Ui.T(0) };
        var root = new DockPanel { Margin = Ui.T(18, 14, 18, 14) };
        DockPanel.SetDock(note, Dock.Top);
        root.Children.Add(note);
        root.Children.Add(content);
        win.Content = root;
        win.ShowDialog();
    }
}
