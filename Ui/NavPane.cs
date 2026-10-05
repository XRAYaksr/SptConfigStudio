using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Windows.Threading;
using SptConfigStudio.Core;
using Wpf.Ui.Controls;

namespace SptConfigStudio.Views;

/// <summary>
/// Builds and highlights the sidebar. WPF-UI's NavigationView cannot be driven from here the usual way:
/// <c>SelectedItem</c> is read-only, and an item only raises SelectionChanged/ItemInvoked when it declares a
/// TargetPageType. So items stay page-less, navigation runs off ButtonBase.Click, and the pill follows
/// <c>IsActive</c>.
///
/// The amber bar is ours rather than the template's <c>ActiveRectangle</c>, because that one is a plain Opacity
/// setter inside the IsActive trigger and can only pop into place. Ours slides from row to row, so the library's
/// has to be switched off: NavigationViewSelectionIndicatorForeground is Transparent in both palettes and in
/// Theme/Appearance.cs, which re-pushes it on every accent change.
///
/// Categories are expandable rows, but the nesting is ours too: LeftMinimalCompactNavigationViewItemTemplate draws
/// its child ItemsControl unconditionally and never triggers on IsExpanded, so the library cannot collapse anything
/// in this pane mode. Children therefore stay top-level items whose Visibility follows their group.
/// </summary>
public sealed class NavPane
{
    public static readonly string[] CategoryOrder = ["基础", "玩法", "世界", "高级", "其他"];

    /// <summary>Menu rows and footer rows sit on opposite ends of the pane; sliding that far reads as a glitch.</summary>
    private const double LongJump = 200;

    /// <summary>How far a child sits inside its category row. The 40px rail has no room for it, so it goes to zero there.</summary>
    private const double ChildIndent = 16;

    private static readonly Thickness ChildMargin = new(ChildIndent, 0, 0, 0);

    private static readonly TimeSpan SlideTime = TimeSpan.FromMilliseconds(200);
    private static readonly TimeSpan FadeTime = TimeSpan.FromMilliseconds(140);

    private readonly NavigationView _nav;
    private readonly Rectangle _cursor;
    private readonly FrameworkElement _cursorHost;
    private readonly Action<string> _go;
    private readonly List<NavigationViewItem> _items = [];
    private readonly List<NavGroup> _groups = [];
    private readonly Dictionary<NavigationViewItem, NavGroup> _owner = [];
    private bool _wide = true;
    private string? _active;
    private NavigationViewItem? _activeItem;
    private double _cursorX, _cursorY;
    private bool _sliding, _pending, _placed;
    private int _slideGen, _fadeGen;

    public event Action? WizardRequested;

    /// <summary>Raised when a category row is clicked while the pane is folded into the icon rail.</summary>
    public event Action? OpenPaneRequested;

    public NavPane(NavigationView nav, Rectangle cursor, FrameworkElement cursorHost, Action<string> go)
    {
        _nav = nav;
        _cursor = cursor;
        _cursorHost = cursorHost;
        _go = go;
        // Collapsing the pane resizes the shell on every frame, and ScrollChanged bubbles up from both scroll
        // regions, so these re-sync the bar for free — no timer of our own. Loaded covers the very first paint,
        // which happens before anything is measured.
        _cursorHost.SizeChanged += (_, _) => Reposition();
        _cursorHost.Loaded += (_, _) => Reposition();
        _cursorHost.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler((_, _) => Reposition()));
    }

    public void Build(Workspace ws)
    {
        _nav.MenuItems.Clear();
        _nav.FooterMenuItems.Clear();
        _items.Clear();
        _groups.Clear();
        _owner.Clear();
        _active = null;
        _activeItem = null;
        Hide();

        AddItem("quick", "快速调整", "藏身处、空投、战局等常用项的中文面板", accent: true);

        var groups = ws.Files.GroupBy(f => f.Category).ToList();
        foreach (var cat in CategoryOrder.Concat(groups.Select(g => g.Key).Where(k => !CategoryOrder.Contains(k))))
        {
            var inCat = groups.FirstOrDefault(g => g.Key == cat);
            if (inCat is null) continue;
            var group = AddGroup(cat);
            foreach (var f in inCat)
                group.Children.Add(AddItem(f.FileName, f.Title, f.Description, advanced: f.Advanced, group: group));
        }

        var tools = AddGroup("工具", footer: true);
        tools.Children.Add(AddItem("backup", "备份与还原", "查看、创建、还原压缩包备份", accent: true, footer: true, group: tools));
        tools.Children.Add(AddItem("appearance", "外观", "配色、侧边栏折叠、主题色与分区材质", footer: true, group: tools));
        var wizard = AddItem("wizard", "重开首次向导", "再走一遍：确认目录 → 备份机制 → 初始快照", footer: true, navigate: false, group: tools);
        wizard.Click += (_, _) => WizardRequested?.Invoke();
        tools.Children.Add(wizard);

        // Groups are born collapsed, which is also what the user asked for: only the category holding the current
        // page opens, and 快速调整 is a top-level row, so a fresh pane shows nothing but the categories.
        ApplyVisibility();
    }

    /// <summary>
    /// The 40px rail has no room for a label or an indent, so both go. Children stay listed in the rail either way:
    /// collapsing a group is a wide-pane affordance only.
    /// </summary>
    public void SetPaneWide(bool wide)
    {
        _wide = wide;
        foreach (var item in _items) item.Margin = _owner.ContainsKey(item) && wide ? ChildMargin : new Thickness();
        ApplyVisibility();
        // Rows move, so the bar has to re-land once layout has caught up with the text and margin change.
        _ = _cursorHost.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Reposition));
    }

    /// <summary>Marks the entry matching <paramref name="key"/>: the template paints the pill, the bar slides over.</summary>
    public void MarkActive(string? key)
    {
        // RefreshCurrent re-enters with the same key after every save, revert, restore and palette switch.
        if (string.Equals(key, _active, StringComparison.OrdinalIgnoreCase))
        {
            // Folding the category by hand and then landing on its page again — a search jump to what is already
            // open — has to give that page back its row. Reposition re-places the bar without replaying the slide.
            if (_activeItem is { } current && _owner.TryGetValue(current, out var folded) && !folded.Expanded)
            {
                SetExpanded(folded, true);
                _ = _cursorHost.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Reposition));
            }
            return;
        }

        NavigationViewItem? hit = null;
        foreach (var item in _items)
        {
            var on = key is not null && string.Equals(item.Tag as string, key, StringComparison.OrdinalIgnoreCase);
            item.IsActive = on;
            if (on) hit = item;
        }
        _active = key;
        _activeItem = hit;
        // Arriving at a child by any route — click, search hit or a jump from the page itself — has to open its group,
        // and only that group: the pane would otherwise grow past its height as the user browses around.
        NavGroup? owner = null;
        if (hit is not null) _owner.TryGetValue(hit, out owner);
        foreach (var group in _groups)
            if (group.Expanded && group != owner) SetExpanded(group, false);
        if (owner is not null && !owner.Expanded) SetExpanded(owner, true);

        // A slide that starts before the new page finishes laying up loses a third of its clock to a frozen frame, so
        // it waits on Background and keeps the free resyncs out of the way meanwhile. The first appearance has no
        // clock to protect: Loaded lands after the pending layout pass, and until then Reposition is free to place
        // the bar as soon as the rows have a size.
        _pending = _placed;
        _ = _cursorHost.Dispatcher.BeginInvoke(_placed ? DispatcherPriority.Background : DispatcherPriority.Loaded, new Action(MoveCursor));
    }

    private void MoveCursor()
    {
        _pending = false;
        // With no live position there is nothing to slide from, and starting at the origin would drag the bar across
        // every row above the active one.
        if (!_placed) { Reposition(); return; }
        if (Bounds(_activeItem) is not { } box) { Hide(); return; }
        var spot = CursorSpot(box);
        if (Math.Abs(spot.Y - _cursorY) > LongJump)
        {
            Place(spot.X, spot.Y);
            _cursor.Opacity = 0;
            Fade(1, FadeTime);
            return;
        }
        Slide(spot.X, spot.Y);
    }

    private void Reposition()
    {
        if (_sliding || _pending) return;
        if (Bounds(_activeItem) is not { } box) { Hide(); return; }
        var spot = CursorSpot(box);
        Place(spot.X, spot.Y);
        if (_cursor.Opacity < 1) Fade(1, FadeTime);
    }

    /// <summary>Null until the row is actually in the visual tree and laid out.</summary>
    private Rect? Bounds(NavigationViewItem? item)
    {
        if (item is null || !item.IsVisible || item.ActualHeight <= 0) return null;
        try
        {
            return item.TransformToVisual(_cursorHost).TransformBounds(new Rect(0, 0, item.ActualWidth, item.ActualHeight));
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>The library's bar hugged MainBorder's inner edge, one pixel past its stroke, centred on the row.</summary>
    private Point CursorSpot(Rect b) => new(b.X + 1, b.Y + (b.Height - _cursor.Height) / 2);

    private void Place(double x, double y)
    {
        _sliding = false;
        _slideGen++;
        var move = (TranslateTransform)_cursor.RenderTransform;
        move.BeginAnimation(TranslateTransform.XProperty, null);
        move.BeginAnimation(TranslateTransform.YProperty, null);
        (_cursorX, _cursorY) = (x, y);
        move.X = x;
        move.Y = y;
        _placed = true;
    }

    private void Slide(double x, double y)
    {
        var move = (TranslateTransform)_cursor.RenderTransform;
        var ease = new CubicEase { EasingMode = EasingMode.EaseInOut };
        _sliding = true;
        var gen = ++_slideGen;

        var track = new DoubleAnimation(_cursorY, y, SlideTime) { EasingFunction = ease, FillBehavior = FillBehavior.Stop };
        track.Completed += (_, _) =>
        {
            if (gen != _slideGen) return;
            _sliding = false;
            move.BeginAnimation(TranslateTransform.XProperty, null);
            move.BeginAnimation(TranslateTransform.YProperty, null);
            (_cursorX, _cursorY) = (x, y);
            move.X = x;
            move.Y = y;
        };
        move.BeginAnimation(TranslateTransform.XProperty, new DoubleAnimation(_cursorX, x, SlideTime) { EasingFunction = ease, FillBehavior = FillBehavior.Stop });
        move.BeginAnimation(TranslateTransform.YProperty, track);
    }

    private void Fade(double to, TimeSpan time)
    {
        var gen = ++_fadeGen;
        var anim = new DoubleAnimation(_cursor.Opacity, to, time) { FillBehavior = FillBehavior.Stop };
        anim.Completed += (_, _) =>
        {
            if (gen != _fadeGen) return;
            _cursor.Opacity = to;
            _cursor.BeginAnimation(UIElement.OpacityProperty, null);
        };
        _cursor.BeginAnimation(UIElement.OpacityProperty, anim);
    }

    private void Hide()
    {
        _placed = false;
        _fadeGen++;
        _cursor.BeginAnimation(UIElement.OpacityProperty, null);
        _cursor.Opacity = 0;
    }

    private NavigationViewItem AddItem(string key, string title, string tip, bool advanced = false,
        bool accent = false, bool footer = false, bool navigate = true, NavGroup? group = null)
    {
        var item = new NavigationViewItem
        {
            Content = advanced ? title + " ·高级" : title,
            Tag = key,
            Icon = Icons.ForFile(key),
            ToolTip = tip.Length > 0 ? tip : title
        };
        if (group is not null)
        {
            _owner[item] = group;
            if (_wide) item.Margin = ChildMargin;
        }
        if (advanced) item.SetResourceReference(Control.ForegroundProperty, "Brasa.TextDim");
        if (accent) item.SetResourceReference(Control.ForegroundProperty, "Brasa.AccentHi");
        if (navigate) item.Click += (_, _) => _go(key);
        _items.Add(item);
        if (footer) _nav.FooterMenuItems.Add(item);
        else _nav.MenuItems.Add(item);
        return item;
    }

    /// <summary>
    /// A category as a normal row, so it picks up the same 14px text and primary colour as the rows under it.
    /// <paramref name="footer"/> only changes which collection the row lands in; the row itself is identical either way.
    /// </summary>
    private NavGroup AddGroup(string category, bool footer = false)
    {
        var chevron = new SymbolIcon
        {
            Symbol = SymbolRegular.ChevronRight24,
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center
        };
        var row = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        row.Children.Add(new System.Windows.Controls.TextBlock { Text = category });
        row.Children.Add(chevron);
        var parent = new NavigationViewItem
        {
            Content = row,
            Tag = "cat:" + category,
            Icon = Icons.ForCategory(category),
            ToolTip = category
        };
        var group = new NavGroup(chevron);
        parent.Click += (_, _) =>
        {
            // In the rail every child icon is on screen anyway, so toggling there would look like a dead click.
            if (_wide) Toggle(group);
            else OpenPaneRequested?.Invoke();
        };
        if (footer) _nav.FooterMenuItems.Add(parent);
        else _nav.MenuItems.Add(parent);
        _groups.Add(group);
        return group;
    }

    private void Toggle(NavGroup group)
    {
        SetExpanded(group, !group.Expanded);
        // Everything below shifts, so the bar has to re-land once the new heights are measured.
        _ = _cursorHost.Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(Reposition));
    }

    private void SetExpanded(NavGroup group, bool expanded)
    {
        group.Expanded = expanded;
        group.Chevron.Symbol = expanded ? SymbolRegular.ChevronDown24 : SymbolRegular.ChevronRight24;
        ApplyVisibility();
        if (!expanded && _activeItem is { } active && group.Children.Contains(active)) Hide();
    }

    /// <summary>The one place that decides whether a group's rows are on screen: collapsed hides them, the rail never does.</summary>
    private void ApplyVisibility()
    {
        foreach (var group in _groups)
        {
            var show = group.Expanded || !_wide;
            foreach (var child in group.Children) child.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private sealed class NavGroup(SymbolIcon chevron)
    {
        public SymbolIcon Chevron { get; } = chevron;
        public List<NavigationViewItem> Children { get; } = [];
        public bool Expanded { get; set; }
    }
}
