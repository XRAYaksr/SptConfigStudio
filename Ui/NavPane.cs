using System.Windows.Controls;
using SptConfigStudio.Core;
using Wpf.Ui.Controls;

namespace SptConfigStudio.Views;

/// <summary>
/// Builds and highlights the sidebar. WPF-UI's NavigationView cannot be driven from here the usual way:
/// <c>SelectedItem</c> is read-only, and an item only raises SelectionChanged/ItemInvoked when it declares a
/// TargetPageType. So items stay page-less, navigation runs off ButtonBase.Click, and the accent bar follows
/// <c>IsActive</c>.
/// </summary>
public sealed class NavPane
{
    public static readonly string[] CategoryOrder = ["基础", "玩法", "世界", "高级", "其他"];

    private readonly NavigationView _nav;
    private readonly Action<string> _go;
    private readonly List<NavigationViewItem> _items = [];
    private readonly List<(NavigationViewItemHeader Header, string Text)> _headers = [];
    private bool _showHeaderText = true;

    public event Action? WizardRequested;

    public NavPane(NavigationView nav, Action<string> go)
    {
        _nav = nav;
        _go = go;
    }

    public void Build(Workspace ws)
    {
        _nav.MenuItems.Clear();
        _nav.FooterMenuItems.Clear();
        _items.Clear();
        _headers.Clear();

        AddItem("quick", "快速调整", "藏身处、空投、战局等常用项的中文面板", accent: true);

        var groups = ws.Files.GroupBy(f => f.Category).ToList();
        foreach (var cat in CategoryOrder.Concat(groups.Select(g => g.Key).Where(k => !CategoryOrder.Contains(k))))
        {
            var inCat = groups.FirstOrDefault(g => g.Key == cat);
            if (inCat is null) continue;
            _nav.MenuItems.Add(Header(cat));
            foreach (var f in inCat) AddItem(f.FileName, f.Title, f.Description, advanced: f.Advanced);
        }

        _nav.FooterMenuItems.Add(Header("工具"));
        AddItem("backup", "备份与还原", "查看、创建、还原压缩包备份", accent: true, footer: true);
        AddItem("appearance", "外观", "配色、侧边栏折叠、主题色与分区材质", footer: true);
        var wizard = AddItem("wizard", "重开首次向导", "再走一遍：确认目录 → 备份机制 → 初始快照", footer: true, navigate: false);
        wizard.Click += (_, _) => WizardRequested?.Invoke();
    }

    /// <summary>
    /// A compact pane clips its items with the scroll viewport, which leaves category headers cut mid-glyph.
    /// Blanking the text keeps the group icon and drops the fragment.
    /// </summary>
    public void SetHeaderVisible(bool showText)
    {
        _showHeaderText = showText;
        foreach (var (header, text) in _headers) header.Text = showText ? text : string.Empty;
    }

    /// <summary>Marks the entry matching <paramref name="key"/>; the template's IsActive trigger paints the bar and the pill.</summary>
    public void MarkActive(string? key)
    {
        foreach (var item in _items)
            item.IsActive = key is not null && string.Equals(item.Tag as string, key, StringComparison.OrdinalIgnoreCase);
    }

    private NavigationViewItem AddItem(string key, string title, string tip, bool advanced = false, bool accent = false, bool footer = false, bool navigate = true)
    {
        var item = new NavigationViewItem
        {
            Content = advanced ? title + " ·高级" : title,
            Tag = key,
            Icon = Icons.ForFile(key),
            ToolTip = tip.Length > 0 ? tip : title
        };
        if (advanced) item.SetResourceReference(Control.ForegroundProperty, "Brasa.TextDim");
        if (accent) item.SetResourceReference(Control.ForegroundProperty, "Brasa.AccentHi");
        if (navigate) item.Click += (_, _) => _go(key);
        _items.Add(item);
        if (footer) _nav.FooterMenuItems.Add(item);
        else _nav.MenuItems.Add(item);
        return item;
    }

    private NavigationViewItemHeader Header(string category)
    {
        var header = new NavigationViewItemHeader
        {
            Text = _showHeaderText ? category : string.Empty,
            Icon = Icons.ForCategory(category)
        };
        _headers.Add((header, category));
        return header;
    }
}
