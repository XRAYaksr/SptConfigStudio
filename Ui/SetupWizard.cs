using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>
/// First-run setup: confirm the SPT root (walking up from the exe stops working once the tool lives outside the
/// game folder), explain the backup-before-write gate, then take the initial snapshot. Reads only — the wizard
/// never writes into the server tree.
/// </summary>
public sealed class SetupWizard
{
    public sealed record Outcome(string? GameRoot, bool SnapshotCreated);

    private const int StepCount = 3;
    private static readonly string[] Crumbs = ["确认游戏目录", "备份与生效机制", "创建初始快照"];

    private readonly Wpf.Ui.Controls.FluentWindow _win;
    private readonly ContentControl _body;
    private readonly TextBlock _crumb;
    private readonly Button _back;
    private readonly Button _next;
    private readonly Button _skip;
    private readonly TextBox _path;
    private readonly TextBlock _probe;
    private readonly TextBlock _result;
    private readonly TextBlock _scopeHint;
    private readonly TextBlock _archiveNote;
    private readonly ComboBox _scope;
    private FrameworkElement? _rootTree;
    private FrameworkElement? _snapshotTree;

    private int _step;
    private string? _confirmed;
    private bool _snapshot;
    private bool _busy;
    private bool _doneCreating;

    private SetupWizard(Window? owner, AppEnv env)
    {
        _win = new Wpf.Ui.Controls.FluentWindow
        {
            Width = 700,
            Height = 580,
            MinWidth = 580,
            MinHeight = 470,
            Owner = owner,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        _path = new TextBox { Text = env.GameRoot, Height = 30, VerticalContentAlignment = VerticalAlignment.Center };
        _probe = Ui.Text("", 12, "Brasa.TextDim", wrap: true);
        _result = Ui.Text("", 12, "Brasa.TextDim", wrap: true);
        _scopeHint = Ui.Text(BackupScopes.All[0].Hint, 11.5, "Brasa.TextDim", wrap: true);
        _archiveNote = Ui.Text("", 12, "Brasa.TextDim", wrap: true);
        _scope = new ComboBox { Width = 320, HorizontalAlignment = HorizontalAlignment.Left };

        var head = Ui.PageHeader(
            "首次设置", Icons.ForFile("wizard"), null,
            "共三步：确认服务端位置 → 了解备份与生效机制 → 创建初始快照。向导仅读取文件，不会修改任何配置。");
        head.Margin = Ui.T(0, 0, 0, 10);

        _crumb = Ui.Text("", 11.5, "Brasa.Accent", weight: FontWeights.SemiBold);
        _crumb.Margin = Ui.T(0, 0, 0, 10);
        _body = new ContentControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch
        };

        _back = Ui.Btn("上一步", (_, _) => Move(-1));
        _next = Ui.Btn("下一步", (_, _) => RunStep(MoveForwardAsync()), primary: true);
        _skip = Ui.Btn("跳过", (_, _) => _win.Close(), tip: "跳过剩余步骤；此后每次保存前仍会自动创建备份");

        var footer = new Grid();
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var nav = Ui.HBox(8, _back, _next);
        Grid.SetColumn(_skip, 0);
        Grid.SetColumn(nav, 1);
        _skip.HorizontalAlignment = HorizontalAlignment.Left;
        footer.Children.Add(_skip);
        footer.Children.Add(nav);

        var panel = new DockPanel { Margin = Ui.T(22, 18, 22, 18) };
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(_crumb, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        panel.Children.Add(head);
        panel.Children.Add(_crumb);
        panel.Children.Add(footer);
        panel.Children.Add(new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _body
        });
        Ui.Chrome(_win, "首次设置向导", panel);

        _path.TextChanged += (_, _) => Probe();
        Probe();
        Render();
    }

    public static Outcome Run(Window? owner, AppEnv env)
    {
        var wiz = new SetupWizard(owner, env);
        wiz._win.ShowDialog();
        return new Outcome(wiz._confirmed, wiz._snapshot);
    }

    // ------------------------------------------------------------------ steps

    private void Move(int delta)
    {
        _step = Math.Clamp(_step + delta, 0, StepCount - 1);
        Render();
    }

    private async Task MoveForwardAsync()
    {
        if (_step < StepCount - 1)
        {
            if (_step == 0 && _confirmed is null) return;
            Move(1);
            return;
        }
        if (_doneCreating) _win.Close();
        else await CreateSnapshotAsync();
    }

    /// <summary>Event handlers cannot await, and a discarded task swallows its exception, so surface failures here.</summary>
    private async void RunStep(Task work)
    {
        try { await work; }
        catch (Exception ex)
        {
            _busy = false;
            UpdateNav();
            MessageBox.Show(ex.Message, "向导操作失败", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void Render()
    {
        _crumb.Text = $"第 {_step + 1} / {StepCount} 步 · {Crumbs[_step]}";
        _body.Content = _step switch
        {
            0 => StepRoot(),
            1 => StepRules(),
            _ => StepSnapshot()
        };
        UpdateNav();
    }

    private void UpdateNav()
    {
        _back.IsEnabled = _step > 0 && !_busy;
        _skip.IsEnabled = _confirmed is not null && !_busy;
        _skip.Visibility = _step == StepCount - 1 && !_doneCreating ? Visibility.Visible : Visibility.Collapsed;
        _next.Content = _step switch
        {
            _ when _step < StepCount - 1 => "下一步",
            _ when _doneCreating => "完成",
            _ => "创建初始快照"
        };
        _next.IsEnabled = _busy ? _doneCreating : _step != 0 || _confirmed is not null;
    }

    private FrameworkElement StepRoot() => _rootTree ??= BuildRoot();

    private AppEnv TargetEnv() => new() { GameRoot = _confirmed ?? AppEnv.Detect().GameRoot };

    private FrameworkElement StepSnapshot() => _snapshotTree ??= BuildSnapshot();

    private FrameworkElement BuildSnapshot()
    {
        _scope.Items.Clear();
        foreach (var label in BackupScopes.All.Select(x => x.Label)) _scope.Items.Add(label);
        _scope.SelectedIndex = 0;
        _scope.SelectionChanged += (_, _) => _scopeHint.Text = BackupScopes.All[Math.Max(0, _scope.SelectedIndex)].Hint;

        RefreshArchiveNote();

        return Ui.VBox(
            Ui.Text("建议立即将当前配置打包为快照，作为后续回退的基准。若选择跳过，首次保存前仍会自动创建备份。",
                12.5, wrap: true),
            new Border { Height = 10 },
            Ui.Text("打包范围", 12, weight: FontWeights.SemiBold),
            _scope,
            _scopeHint,
            _archiveNote,
            new Border { Height = 12 },
            _result);
    }

    private void RefreshArchiveNote()
    {
        var existing = new BackupService(TargetEnv()).List();
        _archiveNote.Text = existing.Count == 0
            ? "· 备份目录当前为空。"
            : $"· 备份目录中已有 {existing.Count} 个压缩包，最新一份创建于 {existing[0].CreatedText}。";
    }

    private FrameworkElement BuildRoot()
    {
        var browse = Ui.Btn("浏览…", (_, _) => Browse());
        var entry = new Grid { Margin = Ui.T(0, 0, 0, 8) };
        entry.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        entry.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(_path, 0);
        Grid.SetColumn(browse, 1);
        browse.Margin = Ui.T(8, 0, 0, 0);
        entry.Children.Add(_path);
        entry.Children.Add(browse);

        _probe.Margin = Ui.T(0, 0, 0, 10);
        var lines = Ui.VBox(
            Ui.Text("请选择游戏根目录，即包含 SPT_Runtime 文件夹的目录层级。", 12.5, wrap: true),
            new Border { Height = 8 },
            entry,
            _probe,
            Bullet("本工具与游戏目录分离存放后无法自动定位，因此需在此确认一次，结果将记录于偏好设置。"),
            Bullet("误选目录不会造成文件损坏：本工具的读写范围仅限于已确认目录下的 SPT_Runtime\\SPT_Data\\configs。"));
        return lines;
    }

    private FrameworkElement StepRules()
    {
        var env = TargetEnv();
        return Ui.VBox(
            Rule("先备份，后写入",
                "执行「保存修改」时，本工具先将改动范围内的文件打包为 zip 并重新打开校验，校验通过后才写入磁盘；备份失败即中止写入，磁盘内容保持原样。"),
            Rule("备份位置",
                env.BackupDir + $"（默认保留最近 {env.KeepBackups} 份 spt-cfg-*.zip，保留数量可在「备份与还原」页调整）。"),
            Rule("还原前同样先备份",
                "还原任何一份备份之前，本工具会先为当前状态创建安全备份，因此还原操作本身也可以被还原。"),
            Rule("需重启服务端方可生效",
                "本工具不会自动重启 SPT。服务端运行期间写入磁盘不会影响当前进程，需停止并重新启动服务端后新配置方生效。"),
            Rule("仅修改配置，不涉及游戏本体",
                "写入范围仅限于 configs 目录下的 json 文件；database 与玩家存档仅纳入备份范围，编辑面板不会修改它们。"));
    }

    private static Border Rule(string title, string body)
    {
        var card = Ui.Card(Ui.VBox(
            Ui.Text(title, 13, weight: FontWeights.SemiBold),
            new Border { Height = 3 },
            Ui.Text(body, 12, "Brasa.TextDim", wrap: true)));
        card.Margin = Ui.T(0, 0, 0, 8);
        return card;
    }

    private static Border Bullet(string text)
    {
        var tb = Ui.Text("· " + text, 12, "Brasa.TextDim", wrap: true);
        tb.Margin = Ui.T(0, 3, 0, 0);
        return new Border { Child = tb };
    }

    // ------------------------------------------------------------------ step 1 logic

    private void Browse()
    {
        var dlg = new Microsoft.Win32.OpenFolderDialog { Title = "请选择包含 SPT_Runtime\\SPT_Data 的游戏根目录" };
        if (dlg.ShowDialog(_win) == true) _path.Text = dlg.FolderName;
    }

    private void Probe()
    {
        var dir = _path.Text.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(dir) || !AppEnv.IsSpt(dir))
        {
            _confirmed = null;
            _probe.Text = $"✗ 未找到 SPT_Runtime\\SPT_Data\\configs，请重新选择目录：{dir}";
            _probe.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Bad");
            UpdateNav();
            return;
        }

        var root = Path.GetFullPath(dir);
        var n = Directory.GetFiles(Path.Combine(root, "SPT_Runtime", "SPT_Data", "configs"), "*.json").Length;
        _confirmed = root;
        _probe.Text = $"✓ 已识别服务端：{n} 个配置文件；备份将写入 {Path.Combine(root, "Backups")}";
        _probe.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Good");
        UpdateNav();
    }

    private async Task CreateSnapshotAsync()
    {
        if (_confirmed is null) return;
        var env = TargetEnv();
        var scope = BackupScopes.All[_scope.SelectedIndex >= 0 ? _scope.SelectedIndex : 0].Scope;

        _busy = true;
        _result.Text = "正在创建备份并校验…";
        _result.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.TextDim");
        UpdateNav();

        try
        {
            var entry = await Task.Run(() => new BackupService(env).Create("初始快照", scope));
            _snapshot = true;
            _doneCreating = true;
            _result.Text = $"✓ 已创建 {entry.Name}（{entry.SizeText}，含 {entry.Members.Length} 个文件，校验通过）";
            _result.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Good");
            RefreshArchiveNote();
        }
        catch (Exception ex)
        {
            _result.Text = "✗ 备份失败：" + ex.Message + "\n未改动任何文件，可重试或跳过。";
            _result.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Bad");
        }
        finally
        {
            _busy = false;
            UpdateNav();
        }
    }
}
