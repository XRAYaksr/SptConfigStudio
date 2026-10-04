using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>Shared display names for what a backup zip covers.</summary>
public static class BackupScopes
{
    public static readonly (BackupScope Scope, string Label, string Hint)[] All =
    [
        (BackupScope.Configs, "仅服务端配置（configs）", "覆盖本工具写入的全部内容，最快。"),
        (BackupScope.ConfigsWithDatabase, "配置 + 数据库全局表", "额外打包 database 顶层 json（globals/settings/server 等）。"),
        (BackupScope.Full, "配置 + 数据库全局表 + 玩家存档", "把 user/profiles 一并打包，改动前后都留一份存档。")
    ];

    public static string LabelOf(BackupScope s) => All.First(x => x.Scope == s).Label;
}

/// <summary>
/// Review gate shown before anything touches the disk: every pending change, the scope of the zip that will be
/// taken first, and a warning when a file would come back reformatted.
/// </summary>
public static class SaveDialog
{
    public static (bool Confirmed, BackupScope Scope) Review(Window owner, Workspace ws, IReadOnlyList<ConfigDocument> targets)
    {
        var win = new Wpf.Ui.Controls.FluentWindow
        {
            Owner = owner,
            Width = 820,
            Height = 640,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ShowInTaskbar = false
        };

        var head = Ui.PageHeader(
            $"准备写入 {targets.Count} 个配置文件", Icons.ForFile("backup"), null,
            $"流程：先把当前磁盘内容打包成 zip 并校验完整性（备份目录 {ws.Env.BackupDir}），备份成功后才写入；备份失败则一律不写。");
        head.Margin = Ui.T(0, 0, 0, 10);

        var scope = new ComboBox { ItemsSource = BackupScopes.All.Select(x => x.Label).ToList(), SelectedIndex = 0, Width = 300, HorizontalAlignment = HorizontalAlignment.Left };
        var scopeHint = Ui.Text(BackupScopes.All[0].Hint, 11.5, "Brasa.TextDim");
        scopeHint.VerticalAlignment = VerticalAlignment.Center;
        scope.SelectionChanged += (_, _) =>
            scopeHint.Text = BackupScopes.All[Math.Max(0, scope.SelectedIndex)].Hint;
        var scopeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = Ui.T(0, 0, 0, 4) };
        scopeRow.Children.Add(Ui.Text("备份范围", 12.5));
        scopeRow.Children.Add(new Border { Child = scope, Margin = Ui.T(10, 0, 10, 0) });
        scopeRow.Children.Add(scopeHint);

        var churn = targets.Where(d => !d.PreservesOriginalFormatting).Select(d => d.FileName).ToList();
        var body = new StackPanel();
        if (churn.Count > 0)
        {
            var note = Ui.Text(
                $"注意：{string.Join("、", churn)} 的原始排版与本工具的输出不完全一致（换行符、缩进、空行或转义），写入后可能附带纯空白变化。功能不受影响，但 git 对比会变大。",
                11.5, "Brasa.Warn", wrap: true);
            note.Margin = Ui.T(0, 0, 0, 10);
            body.Children.Add(note);
        }

        foreach (var doc in targets)
        {
            var changes = ws.ChangesFor(doc);
            var card = new StackPanel();
            var title = Ui.Text($"{MetadataCatalog.Meta(doc.FileName).Title}  ·  {doc.FileName}", 13.5, "Brasa.AccentHi", weight: FontWeights.SemiBold);
            card.Children.Add(title);
            card.Children.Add(Ui.Text($"{changes.Count} 处改动", 11, "Brasa.TextDim"));

            foreach (var c in changes.Take(150))
                card.Children.Add(ChangeRow(c));
            if (changes.Count > 150)
                card.Children.Add(Ui.Text($"… 另有 {changes.Count - 150} 处未列出。", 11.5, "Brasa.Warn"));

            var box = Ui.Card(card, Ui.T(14, 10, 14, 12));
            box.Margin = Ui.T(0, 0, 0, 10);
            body.Children.Add(box);
        }

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = body,
            Margin = Ui.T(0, 6, 0, 6)
        };

        var confirmed = false;
        var save = Ui.Btn("备份并写入", (_, _) => { confirmed = true; win.Close(); }, primary: true);
        save.Padding = Ui.T(18, 7, 18, 7);
        var cancel = Ui.Btn("取消", (_, _) => win.Close());
        cancel.IsCancel = true;
        var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        footer.Children.Add(save);
        footer.Children.Add(new Border { Child = cancel, Margin = Ui.T(8, 0, 0, 0) });

        var root = new DockPanel { Margin = Ui.T(20, 16, 20, 16) };
        DockPanel.SetDock(head, Dock.Top);
        DockPanel.SetDock(scopeRow, Dock.Top);
        DockPanel.SetDock(footer, Dock.Bottom);
        root.Children.Add(head);
        root.Children.Add(scopeRow);
        root.Children.Add(footer);
        root.Children.Add(scroll);
        Ui.Chrome(win, "保存前确认", root, maximizable: true);
        win.ShowDialog();

        var chosen = scope.SelectedIndex >= 0 ? BackupScopes.All[scope.SelectedIndex].Scope : BackupScope.Configs;
        return (confirmed, chosen);
    }

    private static FrameworkElement ChangeRow(Change c)
    {
        var grid = new Grid { Margin = Ui.T(0, 2, 0, 2) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var path = Ui.Mono(c.Path, 11.5);
        path.TextTrimming = TextTrimming.CharacterEllipsis;
        Grid.SetColumn(path, 0);
        grid.Children.Add(path);

        var what = c.Kind switch
        {
            ChangeKind.Modified => $"{Shorten(c.Old)} → {Shorten(c.New)}",
            ChangeKind.Added => "新增 " + Shorten(c.New),
            _ => "删除 " + Shorten(c.Old)
        };
        var value = Ui.Text(what, 11.5, c.Kind == ChangeKind.Removed ? "Brasa.Bad" : "Brasa.Good");
        value.HorizontalAlignment = HorizontalAlignment.Right;
        value.MaxWidth = 380;
        value.TextTrimming = TextTrimming.CharacterEllipsis;
        Grid.SetColumn(value, 1);
        grid.Children.Add(value);
        return grid;
    }

    private static string Shorten(string s)
    {
        s = s.Replace("\r", "").Replace("\n", " ");
        return s.Length <= 40 ? s : s[..37] + "…";
    }
}
