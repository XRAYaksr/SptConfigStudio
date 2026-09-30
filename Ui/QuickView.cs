using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Text.Json.Nodes;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>Curated dashboard: hand-picked fields grouped by what players actually tune.</summary>
public sealed class QuickView
{
    private readonly Workspace _ws;
    private readonly FieldRenderer _r;
    private readonly StackPanel _body = new();

    public FrameworkElement Root { get; }

    public QuickView(Workspace ws)
    {
        _ws = ws;
        _r = new FieldRenderer(ws) { Touched = () => _status.Text = "有未保存的修改", Rebuild = Rebuild };
        Root = Build();
        Rebuild();
    }

    private TextBlock _status = new();

    private FrameworkElement Build()
    {
        _status = Ui.Text("改动后记得点「保存修改」，保存前会自动生成 zip 备份。", 12, "Brasa.TextDim");

        var header = new StackPanel { Margin = Ui.T(0, 0, 0, 14) };
        header.Children.Add(Ui.Text("快速调整", 20, weight: FontWeights.SemiBold));
        header.Children.Add(Ui.Text("藏身处、空投、战局、掉落、经济等常用服务端开关。未列出的项目请到左侧对应配置文件里编辑。",
            12, "Brasa.TextDim", wrap: true));
        header.Children.Add(_status);
        header.Margin = Ui.T(0, 0, 0, 6);

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Padding = Ui.T(22, 18, 22, 22),
            Content = _body
        };
        var dock = new DockPanel();
        DockPanel.SetDock(header, Dock.Top);
        dock.Children.Add(header);
        dock.Children.Add(scroll);
        return dock;
    }

    public void Rebuild()
    {
        _body.Children.Clear();
        foreach (var group in MetadataCatalog.QuickGroups)
        {
            var card = new StackPanel();
            var head = new StackPanel { Margin = Ui.T(0, 0, 0, 8) };
            head.Children.Add(Ui.Text(group.Title, 15, "Brasa.AccentHi", weight: FontWeights.SemiBold));
            if (group.Subtitle.Length > 0) head.Children.Add(Ui.Text(group.Subtitle, 11.5, "Brasa.TextDim"));
            card.Children.Add(head);

            var rendered = 0;
            foreach (var entry in group.Entries)
            {
                var el = RenderEntry(entry);
                if (el is null) continue;
                card.Children.Add(el);
                rendered++;
            }
            if (rendered == 0) card.Children.Add(Ui.Text("当前配置文件中未找到这些项（可能已被 Mod 或版本变更移除）。", 12, "Brasa.Warn"));

            var border = new Border
            {
                Background = Ui.Res("Brasa.Surface"),
                BorderBrush = Ui.Res("Brasa.Border"),
                BorderThickness = Ui.T(1, 1, 1, 1),
                CornerRadius = new CornerRadius(6),
                Padding = Ui.T(16, 12, 16, 14),
                Margin = Ui.T(0, 0, 0, 14),
                Child = card
            };
            _body.Children.Add(border);
        }
    }

    private FrameworkElement? RenderEntry(QuickEntry entry)
    {
        var doc = _ws.Open(entry.File);
        if (doc is null) return null;
        var located = MetadataCatalog.Locate(doc.Root, entry.Path);
        if (located is null) return null;

        var node = Factory.Build(entry.File, located.Key, located.Path, located.Container, located.Index, located.Value, null, _ws.Items);
        if (node.Kind is FieldKind.Group or FieldKind.IdMap or FieldKind.IdMapRange or FieldKind.ObjectList
            or FieldKind.ScalarList or FieldKind.IdList)
        {
            var size = located.Value switch
            {
                JsonObject o => o.Count,
                JsonArray a => a.Count,
                _ => 0
            };
            node.DefaultExpanded = size <= 10;
            node.Label = string.IsNullOrEmpty(node.Label) || node.Label == node.Key ? entry.Path.Split('.').Last() : node.Label;
        }
        return _r.Render(node, 1);
    }
}
