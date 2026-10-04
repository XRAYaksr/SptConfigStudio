using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>Editor for one config file: generated structured form plus a raw JSON tab.</summary>
public sealed class ConfigView
{
    private readonly Workspace _ws;
    private readonly ConfigDocument _doc;
    private readonly FieldRenderer _r;
    private readonly StackPanel _body = new();
    private readonly TextBlock _dirty;
    private readonly ContentControl _pane = new();
    private TextBox? _raw;
    private TextBlock _rawStatus = new();

    public FrameworkElement Root { get; }

    public ConfigView(Workspace ws, ConfigDocument doc)
    {
        _ws = ws;
        _doc = doc;
        _dirty = Ui.Text("", 12, "Brasa.Good");
        _r = new FieldRenderer(ws)
        {
            Rebuild = RebuildStructured,
            Touched = RefreshDirty
        };
        Root = Build();
        RebuildStructured();
    }

    private FrameworkElement Build()
    {
        var meta = MetadataCatalog.Meta(_doc.FileName);

        var expandAll = Ui.Btn("展开全部", (_, _) => { SetExpanded(true); RebuildStructured(); });
        var collapseAll = Ui.Btn("折叠全部", (_, _) => { SetExpanded(false); RebuildStructured(); });
        var reload = Ui.Btn("从磁盘重新载入", (_, _) =>
        {
            if (MessageBox.Show("放弃该文件的未保存修改并重新读取磁盘内容？", "重新载入",
                    MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) return;
            _doc.Reload();
            RebuildStructured();
            LoadRaw();
        });

        var headGrid = Ui.PageHeader(
            meta.Title, Icons.ForFile(_doc.FileName),
            "SPT_Data\\configs\\" + _doc.FileName, meta.Description,
            Ui.HBox(8, expandAll, collapseAll, reload), _dirty);
        headGrid.Margin = Ui.T(0, 0, 0, 14);

        var structured = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = Ui.T(0, 12, 0, 0),
            Content = _body
        };
        _body.Margin = Ui.T(0, 0, 4, 0);

        _raw = new TextBox
        {
            Style = (Style)Application.Current.FindResource("Brasa.JsonBox"),
            Margin = Ui.T(0, 10, 0, 0)
        };
        var applyRaw = Ui.Btn("应用 JSON", (_, _) =>
        {
            if (_doc.TryApplyRawText(_raw!.Text, out var err))
            {
                _rawStatus.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Good");
                _rawStatus.Text = "已应用到内存中的配置，保存后写入磁盘。";
                RebuildStructured();
            }
            else
            {
                _rawStatus.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Bad");
                _rawStatus.Text = "解析失败：" + err;
            }
        });
        var format = Ui.Btn("仅格式化", (_, _) =>
        {
            _raw!.Text = _doc.Serialize();
            _rawStatus.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.TextDim");
            _rawStatus.Text = "已按 SPT 写法缩进，点「应用 JSON」才会生效。";
        });
        _rawStatus = Ui.Text("", 11.5, "Brasa.TextDim");
        _rawStatus.Margin = Ui.T(10, 0, 0, 0);
        _rawStatus.VerticalAlignment = VerticalAlignment.Center;

        var rawPanel = new DockPanel();
        var rawHead = new StackPanel { Margin = Ui.T(0, 0, 0, 0) };
        rawHead.Children.Add(Ui.Text($"直接编辑 {_doc.FileName} 的完整 JSON（结构化视图会同步刷新）。", 12, "Brasa.TextDim"));
        var rawBar = new StackPanel { Orientation = Orientation.Horizontal, Margin = Ui.T(0, 8, 0, 0) };
        rawBar.Children.Add(applyRaw);
        rawBar.Children.Add(format);
        rawBar.Children.Add(_rawStatus);
        DockPanel.SetDock(rawHead, Dock.Top);
        DockPanel.SetDock(rawBar, Dock.Bottom);
        rawPanel.Children.Add(rawHead);
        rawPanel.Children.Add(rawBar);
        rawPanel.Children.Add(_raw);

        var switcher = Ui.Segmented(new[] { "结构化编辑", "原始 JSON" }, 0, i =>
        {
            _pane.Content = i == 0 ? structured : rawPanel;
            if (i == 1) LoadRaw();
        });
        switcher.HorizontalAlignment = HorizontalAlignment.Left;
        switcher.Margin = Ui.T(0, 0, 0, 12);

        _pane.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _pane.VerticalContentAlignment = VerticalAlignment.Stretch;
        _pane.Content = structured;

        var dock = new DockPanel();
        DockPanel.SetDock(headGrid, Dock.Top);
        DockPanel.SetDock(switcher, Dock.Top);
        dock.Children.Add(headGrid);
        dock.Children.Add(switcher);
        dock.Children.Add(_pane);

        var pad = new Border { Padding = Ui.T(22, 18, 22, 18), Child = dock };
        return pad;
    }

    private void LoadRaw() => _raw!.Text = _doc.Serialize();

    private void SetExpanded(bool open)
    {
        Expansion.ResetFor(_doc.FileName);
        foreach (var n in Root_of().Flatten())
        {
            if (n.IsLeaf || n.Path.Length == 0) continue;
            Expansion.Set(_doc.FileName, n.Path, open);
        }
    }

    /// <summary>Opens every ancestor of <paramref name="path"/>, rebuilds, then scrolls the field into view.</summary>
    public void FocusPath(string path)
    {
        var prefix = "";
        for (var i = 0; i < path.Length; i++)
        {
            if (path[i] is '.' or '[' && prefix.Length > 0) Expansion.Set(_doc.FileName, prefix, true);
            prefix += path[i];
        }
        if (prefix.Length > 0) Expansion.Set(_doc.FileName, prefix, true);

        RebuildStructured();
        ScrollTo(path);
    }

    private FieldNode Root_of() => Factory.BuildRoot(_doc, _ws.Items);

    private void ScrollTo(string path)
    {
        if (FindByTag(_body, path) is not FrameworkElement hit) return;
        hit.Dispatcher.InvokeAsync(() =>
        {
            hit.BringIntoView();
            var anim = new System.Windows.Media.Animation.DoubleAnimation(0.3, 1.0, TimeSpan.FromMilliseconds(650));
            hit.BeginAnimation(UIElement.OpacityProperty, anim);
            anim.Completed += (_, _) => hit.BeginAnimation(UIElement.OpacityProperty, null);
        }, System.Windows.Threading.DispatcherPriority.Background);
    }

    private static FrameworkElement? FindByTag(DependencyObject node, string path)
    {
        if (node is FrameworkElement fe && fe.Tag as string == path) return fe;
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
        {
            var hit = FindByTag(child, path);
            if (hit is not null) return hit;
        }
        return null;
    }

    public void RebuildStructured()
    {
        _body.Children.Clear();
        var root = Root_of();
        var kids = root.Children;
        for (var i = 0; i < kids.Count; i++) _body.Children.Add(_r.Render(kids[i], 0, i == kids.Count - 1));
        RefreshDirty();
    }

    /// <summary>IsDirty compares content, so a commit that changed nothing leaves the header quiet.</summary>
    private void RefreshDirty()
    {
        if (_doc.IsDirty)
        {
            _dirty.Text = "有未保存修改";
            _dirty.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Warn");
        }
        else _dirty.Text = "";
    }
}
