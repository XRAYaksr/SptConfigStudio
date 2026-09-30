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
    private readonly TabControl _tabs = new();
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
            Touched = () => { _dirty.Text = "有未保存修改"; _dirty.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Warn"); }
        };
        Root = Build();
        RebuildStructured();
    }

    private FrameworkElement Build()
    {
        var meta = MetadataCatalog.Meta(_doc.FileName);

        var title = Ui.Text(meta.Title, 20, weight: FontWeights.SemiBold);
        var sub = Ui.Mono("SPT_Data\\configs\\" + _doc.FileName, 11.5);
        var desc = Ui.Text(meta.Description, 12, "Brasa.TextDim", wrap: true);
        desc.Margin = Ui.T(0, 4, 0, 0);

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

        var actions = Ui.HBox(8, expandAll, collapseAll, reload);
        actions.HorizontalAlignment = HorizontalAlignment.Right;
        actions.VerticalAlignment = VerticalAlignment.Top;

        var headGrid = new Grid();
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        headGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var left = Ui.VBox(title, sub, desc, _dirty);
        _dirty.Margin = Ui.T(0, 4, 0, 0);
        Grid.SetColumn(left, 0);
        Grid.SetColumn(actions, 1);
        headGrid.Children.Add(left);
        headGrid.Children.Add(actions);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = Ui.T(0, 12, 0, 0),
            Content = _body
        };
        _body.Margin = Ui.T(0, 0, 4, 0);

        var structured = new DockPanel();
        DockPanel.SetDock(headGrid, Dock.Top);
        structured.Children.Add(headGrid);
        structured.Children.Add(scroll);

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

        var tabStructured = new TabItem { Header = "结构化编辑", Content = structured };
        var tabRaw = new TabItem { Header = "原始 JSON", Content = rawPanel };
        _tabs.Items.Add(tabStructured);
        _tabs.Items.Add(tabRaw);
        _tabs.SelectionChanged += (_, _) =>
        {
            if (ReferenceEquals(_tabs.SelectedItem, tabRaw)) LoadRaw();
        };

        var pad = new Border { Padding = Ui.T(22, 18, 22, 18), Child = _tabs };
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
        foreach (var child in root.Children) _body.Children.Add(_r.Render(child, 0));
        if (_ws.AnyDirty) { _dirty.Text = "有未保存修改"; _dirty.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Warn"); }
        else { _dirty.Text = ""; }
    }
}
