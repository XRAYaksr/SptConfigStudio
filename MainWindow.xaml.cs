using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using SptConfigStudio.Core;
using SptConfigStudio.Theme;
using SptConfigStudio.Views;

namespace SptConfigStudio;

public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
{
    private Workspace? _ws;
    private readonly Dictionary<string, ConfigView> _views = new(StringComparer.OrdinalIgnoreCase);
    private NavPane? _navPane;
    private QuickView? _quick;
    private BackupView? _backup;
    private AppearanceView? _appearance;
    private DispatcherTimer? _searchTimer;
    private DispatcherTimer? _statusTimer;
    private string _current = "quick";
    private bool _itemIndexReady;
    private string? _startupNote;
    private static string? _pendingNote;

    private const double PaneOpenWidth = 240;
    private const double PaneCompactWidth = 48;
    private int _paneAnimGen;

    public MainWindow()
    {
        InitializeComponent();
        Ui.BaseChrome(this);
        Appearance.ApplyFromPrefs(this);
        Nav.IsPaneOpen = AppEnv.PaneOpen;
        PaneShell.Width = AppEnv.PaneOpen ? PaneOpenWidth : PaneCompactWidth;
        Nav.PaneOpened += (_, _) => OnPaneState(true);
        Nav.PaneClosed += (_, _) => OnPaneState(false);
        Activated += (_, _) => RefreshSystemAppearance();
        Loaded += OnLoaded;
        Closing += OnClosing;
        PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && SearchOverlay.Visibility == Visibility.Visible) SearchOverlay.Visibility = Visibility.Collapsed;
            if (e.Key == Key.F && Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) { Search.Focus(); e.Handled = true; }
        };
    }

    /// <summary>
    /// Two curves run side by side on a toggle: WPF-UI's own, which moves its pane column between 232 and a
    /// hard-coded 40 over 160ms at AccelerationRatio 0.4 with no easing token to redefine, and this one, which moves
    /// the shell around it. The rule to live by: at rest the shell is exactly pane + 8, and mid-flight it may only
    /// ever be narrower than that — the moment it is wider, the NavigationView's zero-width content column shows
    /// through as a band inside the sidebar. Narrower is harmless, the pane just gets clipped and the labels wipe in.
    /// Expanding therefore has to trail the library (E &lt;= A, hence the longer ease-in-out); collapsing has to
    /// outrun it (E &gt;= A, hence a short front-loaded ease-out). One symmetric curve cannot satisfy both: an
    /// ease-in-out collapse falls about 33px behind the pane and opens that band mid-flight.
    ///
    /// The page shares this grid as a star column, so an unfrozen Host re-measures the entire form on every frame —
    /// measured at ~85ms a frame, which left a 150ms collapse with three rendered frames and no curve to see. Freezing
    /// it at its final width costs one reflow at the start and buys back the ~10fps animation; what the user then
    /// watches is the sidebar wipe in over a page that has already settled.
    /// </summary>
    private void OnPaneState(bool open)
    {
        var target = open ? PaneOpenWidth : PaneCompactWidth;
        var gen = ++_paneAnimGen;
        if (PaneShell.Parent is Grid columns) Host.Width = columns.ActualWidth - target;
        var anim = new DoubleAnimation(PaneShell.ActualWidth, target,
            TimeSpan.FromMilliseconds(open ? 240 : 150))
        {
            EasingFunction = new CubicEase { EasingMode = open ? EasingMode.EaseInOut : EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        anim.Completed += (_, _) =>
        {
            // WPF hands a bare EventArgs to Completed, so a reversed toggle has to be detected by generation:
            // the run that got replaced must not release the new animation or write back its stale rest width.
            if (gen != _paneAnimGen) return;
            PaneShell.Width = target;
            PaneShell.BeginAnimation(WidthProperty, null);
            Host.Width = double.NaN;
        };
        PaneShell.BeginAnimation(WidthProperty, anim);
        _navPane?.SetPaneWide(open);
        AppEnv.SetPaneOpen(open);
    }

    /// <summary>Coming back to the foreground is when both follow-system settings get re-read.</summary>
    private void RefreshSystemAppearance()
    {
        // Ordered before the accent branch: the AccentKind early return below would otherwise short-circuit it.
        if (Appearance.ParseScheme(AppEnv.ThemeKind) == Scheme.System && Appearance.SystemPrefersLight() != Appearance.IsLight)
            Appearance.ApplyScheme(this, Scheme.System);

        if (AppEnv.AccentKind != "system") return;
        var sys = Appearance.ResolveSystemAccent();
        if (sys is not null && Appearance.AppliedAccent != sys.Value) Appearance.ApplyAccent(sys.Value);
    }

    // ------------------------------------------------------------------ startup

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var carried = _pendingNote;
        _pendingNote = null;

        var env = AppEnv.Detect();
        if (env.Error is null && AppEnv.WizardDone)
        {
            StartWorkspace(env, carried);
            return;
        }

        var outcome = SetupWizard.Run(this, env);
        if (outcome.GameRoot is null)
        {
            StatusLeft.Text = env.Error ?? "未完成首次设置，未定位到服务端。";
            ServerText.Text = "未定位到服务端";
            BtnSave.IsEnabled = BtnRevert.IsEnabled = BtnBackupNow.IsEnabled = BtnRestore.IsEnabled = false;
            return;
        }

        AppEnv.SetWizardDone(true);
        var note = outcome.SnapshotCreated
            ? "初始快照已创建，可在「备份与还原」页恢复。"
            : "尚未创建初始快照；首次「保存修改」前仍会自动创建备份。";

        if (env.Error is not null || !string.Equals(outcome.GameRoot, env.GameRoot, StringComparison.OrdinalIgnoreCase))
            RestartWith(outcome.GameRoot, note);
        else
            StartWorkspace(env, note);
    }

    private void StartWorkspace(AppEnv env, string? note)
    {
        _ws = new Workspace(env);
        _ws.DiscoverFiles();
        _ws.Changed += UpdateActions;
        env.SavePreferredRoot();

        BuildNav();
        Show("quick");

        _searchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(260) };
        _searchTimer.Tick += (_, _) => { _searchTimer.Stop(); _ = RunSearch(); };

        _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        _statusTimer.Tick += (_, _) => _ = RefreshStatus();
        _statusTimer.Start();

        _ = _ws.Items.EnsureLoadedAsync().ContinueWith(_ =>
        {
            Dispatcher.Invoke(() =>
            {
                _itemIndexReady = true;
                RefreshCurrent();
                StatusLeft.Text = _startupNote is null
                    ? "物品名称索引已就绪，搜索可命中中文物品名。"
                    : $"{_startupNote} 物品名称索引也已就绪，搜索可命中中文物品名。";
                _startupNote = null;
            });
        }, TaskScheduler.Default);

        _ = RefreshStatus();
        UpdateActions();
        Title = $"SPT 配置中心 — {env.GameRoot}";
        _startupNote = note;
        if (note is not null) StatusLeft.Text = note;
        if (_ws.Failed.Count > 0)
            StatusLeft.Text = "有配置文件解析失败：" + string.Join("；", _ws.Failed.Take(3));
    }

    private void RestartWith(string root, string? note)
    {
        new AppEnv { GameRoot = root }.SavePreferredRoot();
        _pendingNote = note;

        Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;
        var next = new MainWindow();
        Close();
        Application.Current.MainWindow = next;
        next.Show();
        Application.Current.ShutdownMode = ShutdownMode.OnLastWindowClose;
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_ws is null || !_ws.AnyDirty) return;
        var ask = MessageBox.Show("还有未保存的修改，直接关闭会丢失。要先保存吗？", "未保存的修改",
            MessageBoxButton.YesNoCancel, MessageBoxImage.Warning);
        if (ask == MessageBoxResult.Cancel) e.Cancel = true;
        else if (ask == MessageBoxResult.Yes) { OnSave(sender, new RoutedEventArgs()); e.Cancel = _ws.AnyDirty; }
    }

    // ------------------------------------------------------------------ navigation

    private void BuildNav()
    {
        _navPane = new NavPane(Nav, NavCursor, PaneLayer, Show);
        _navPane.WizardRequested += ReopenWizard;
        _navPane.OpenPaneRequested += () => Nav.IsPaneOpen = true;
        _navPane.Build(_ws!);
        _navPane.SetPaneWide(Nav.IsPaneOpen);
    }

    private void ReopenWizard()
    {
        if (_ws is null) return;
        var outcome = SetupWizard.Run(this, _ws.Env);
        if (outcome.GameRoot is null) return;

        if (!string.Equals(outcome.GameRoot, _ws.Env.GameRoot, StringComparison.OrdinalIgnoreCase))
            RestartWith(outcome.GameRoot, outcome.SnapshotCreated ? "已切换游戏目录并创建初始快照。" : "已切换游戏目录。");
        else if (outcome.SnapshotCreated)
        {
            _backup = null;
            if (_current == "backup") Show("backup");
            StatusLeft.Text = "初始快照已创建，可在「备份与还原」页查看或还原。";
        }
        _ = RefreshStatus();
    }

    private void Show(string key)
    {
        if (_ws is null) return;
        _current = key;
        _navPane?.MarkActive(key);
        SearchOverlay.Visibility = Visibility.Collapsed;

        Host.Content = key switch
        {
            "quick" => (_quick ??= new QuickView(_ws)).Root,
            "backup" => (_backup ??= new BackupView(_ws, RefreshCurrent)).Root,
            "appearance" => (_appearance ??= new AppearanceView(this, Nav)).Root,
            _ => FileView(key)
        };
        UpdateActions();
    }

    private FrameworkElement FileView(string fileName)
    {
        if (_views.TryGetValue(fileName, out var cached)) return cached.Root;
        var doc = _ws!.Open(fileName);
        if (doc is null) return Ui.Text($"读取 {fileName} 失败，文件可能不是合法 JSON。", 14, "Brasa.Bad", wrap: true);
        var view = new ConfigView(_ws, doc);
        _views[fileName] = view;
        return view.Root;
    }

    /// <summary>Rebuilds the visible editor from the (still in-memory) documents. Edits live in the JSON DOM, so nothing is lost.</summary>
    internal void RefreshCurrent()
    {
        _views.Clear();
        _quick = null;
        Show(_current);
    }

    // ------------------------------------------------------------------ toolbar

    private void UpdateActions()
    {
        if (_ws is null) return;
        var dirty = _ws.AnyDirty;
        BtnSave.IsEnabled = dirty;
        BtnRevert.IsEnabled = dirty;
        BtnSave.Content = dirty ? $"保存修改（{_ws.Dirty().Count} 个文件）" : "保存修改";
        StatusRight.Text = _ws.LastBackup is null
            ? "本次会话尚未写入，未创建备份"
            : $"最近备份 {_ws.LastBackup.Name}";
    }

    private void OnSave(object? sender, RoutedEventArgs e)
    {
        if (_ws is null) return;
        var targets = _ws.Dirty();
        if (targets.Count == 0)
        {
            StatusLeft.Text = "没有需要保存的修改。";
            return;
        }

        var (confirmed, scope) = SaveDialog.Review(this, _ws, targets);
        if (!confirmed)
        {
            StatusLeft.Text = "已取消，未写入任何文件。";
            return;
        }

        var result = _ws.Save(targets, "保存修改", scope);
        _backup = null;
        RefreshCurrent();

        if (result.Error is not null)
        {
            StatusLeft.Text = "写入未完成：" + result.Error;
            MessageBox.Show(result.Error, "保存结果", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
            var b = result.Backup!;
            StatusLeft.Text = $"已备份并写入：{b.Name}（{b.SizeText}，{b.Members.Length} 个文件，校验通过）→ {string.Join("、", result.Written)}";
        }
        _ = RefreshStatus();
    }

    private void OnRevert(object? sender, RoutedEventArgs e)
    {
        if (_ws is null) return;
        if (!_ws.AnyDirty)
        {
            StatusLeft.Text = "没有未保存的修改。";
            return;
        }
        if (MessageBox.Show("放弃所有未保存的修改，按磁盘内容重新载入？", "放弃修改",
                MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
        _ws.ReloadAll();
        RefreshCurrent();
        StatusLeft.Text = "已放弃未保存的修改。";
    }

    private async void OnBackupNow(object sender, RoutedEventArgs e)
    {
        if (_ws is null) return;
        BtnBackupNow.IsEnabled = false;
        StatusLeft.Text = "正在打包当前配置…";
        try
        {
            var entry = await Task.Run(() => _ws.Backups.Create("手动备份", BackupScope.Configs));
            _backup = null;
            StatusLeft.Text = $"已备份 {entry.Name}（{entry.SizeText}，{entry.Members.Length} 个文件，校验通过）";
            if (_current == "backup") { _backup = null; Show("backup"); }
            UpdateActions();
        }
        catch (Exception ex)
        {
            StatusLeft.Text = "备份失败：" + ex.Message;
            MessageBox.Show(ex.Message + "\n\n未对任何文件做改动。", "备份失败", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            BtnBackupNow.IsEnabled = true;
        }
    }

    private void OnRestore(object sender, RoutedEventArgs e)
    {
        if (_ws is null) return;
        BackupView.PickAndRestore(this, _ws, () => { _backup = null; RefreshCurrent(); });
    }

    private void OnOpenPanel(object sender, RoutedEventArgs e)
    {
        if (_ws is null) return;
        var url = _ws.Status.PanelUrl ?? $"https://127.0.0.1:{ServerStatus.ReadPort(_ws.Env)}";
        TryStart(url, "无法打开管理面板");
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        if (_ws is null) return;
        try
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"\"{_ws.Env.ConfigDir}\"") { UseShellExecute = true });
        }
        catch (Exception ex) { StatusLeft.Text = "无法打开目录：" + ex.Message; }
    }

    private void TryStart(string url, string failure)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch (Exception ex) { StatusLeft.Text = failure + "：" + ex.Message; }
    }

    // ------------------------------------------------------------------ search

    private void OnSearchChanged(object sender, TextChangedEventArgs e)
    {
        if (_ws is null) return;
        if (Search.Text.Trim().Length < 2)
        {
            SearchOverlay.Visibility = Visibility.Collapsed;
            return;
        }
        _searchTimer?.Stop();
        _searchTimer?.Start();
    }

    private async Task RunSearch()
    {
        if (_ws is null) return;
        var term = Search.Text.Trim();
        if (term.Length < 2) return;
        var hits = await Task.Run(() => ConfigSearch.Run(_ws, term, 80));
        if (Search.Text.Trim() != term) return;

        SearchResults.Children.Clear();
        SearchSummary.Text = _itemIndexReady
            ? $"“{term}” 命中 {hits.Count} 处（含中文物品名），点一条直接跳转"
            : $"“{term}” 命中 {hits.Count} 处，点一条直接跳转";
        foreach (var h in hits) SearchResults.Children.Add(SearchRow(h));
        SearchOverlay.Visibility = Visibility.Visible;
    }

    private FrameworkElement SearchRow(Hit h)
    {
        var box = new StackPanel { Margin = Ui.T(6, 5, 6, 5) };
        box.Children.Add(Ui.Text($"{h.Title} · {h.Key}", 12.5, weight: FontWeights.SemiBold));
        box.Children.Add(Ui.Mono(h.Path, 10.5));
        var preview = Ui.Text(h.Preview, 11.5, "Brasa.TextDim");
        preview.TextTrimming = TextTrimming.CharacterEllipsis;
        box.Children.Add(preview);

        var border = new Border
        {
            CornerRadius = new CornerRadius(3),
            Margin = Ui.T(0, 2, 0, 2),
            Child = box
        };
        border.SetResourceReference(Border.BackgroundProperty, "Brasa.SurfaceAlt");
        border.MouseEnter += (_, _) => border.SetResourceReference(Border.BackgroundProperty, "Brasa.SurfaceHi");
        border.MouseLeave += (_, _) => border.SetResourceReference(Border.BackgroundProperty, "Brasa.SurfaceAlt");
        border.MouseLeftButtonUp += (_, _) => Navigate(h);
        return border;
    }

    private void Navigate(Hit h)
    {
        Show(h.File);
        if (_views.TryGetValue(h.File, out var view)) view.FocusPath(h.Path);
        SearchOverlay.Visibility = Visibility.Collapsed;
        StatusLeft.Text = $"已定位 {h.Title} · {h.Path}";
    }

    // ------------------------------------------------------------------ server status

    private async Task RefreshStatus()
    {
        if (_ws is null) return;
        try { await _ws.Status.RefreshAsync(_ws.Env); }
        catch { /* a status probe must never surface as an error */ }

        var s = _ws.Status;
        ServerText.Text = s.Label;
        ServerDot.Fill = Ui.Res(s.PanelReachable ? "Brasa.Good" : s.ProcessRunning ? "Brasa.Warn" : "Brasa.TextDim");
        WarnStrip.IsOpen = s.ProcessRunning;
        WarnStrip.Message = s.PanelReachable
            ? "服务端正在运行：写入磁盘不会影响当前进程，需要重启服务端后新配置才会生效。本工具不会自动重启服务端。"
            : "服务端未运行：现在写入的配置会在下次启动时生效。";
    }
}
