using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using SptConfigStudio.Core;
using SptConfigStudio.Theme;
using Wpf.Ui.Controls;

namespace SptConfigStudio.Views;

/// <summary>
/// 外观 page: colour scheme, pane collapse, accent colour, per-region material. Rebuilt on every visit instead of
/// cached, so the controls always read back the live state — including a pane the user collapsed with the hamburger.
/// </summary>
public sealed class AppearanceView
{
    private static readonly string[] SchemeLabels = ["深色", "浅色", "跟随系统"];
    private static readonly string[] MaterialLabels = ["云母", "亚克力", "不透明"];
    private static readonly Material[] Materials = [Material.Mica, Material.Acrylic, Material.Opaque];

    private readonly MainWindow _win;
    private readonly NavigationView _nav;
    private readonly StackPanel _body = new();

    private Scheme _scheme = Appearance.ParseScheme(AppEnv.ThemeKind);
    private Material _pane = Appearance.ParseMaterial(AppEnv.PaneMaterial);
    private Material _content = Appearance.ParseMaterial(AppEnv.ContentMaterial);

    private Border _swatch = new();
    private StackPanel _custom = new();
    private Wpf.Ui.Controls.TextBox _hex = new();
    private readonly List<Slider> _rgb = [];
    private bool _suppress;

    public FrameworkElement Root { get; }

    public AppearanceView(MainWindow win, NavigationView nav)
    {
        _win = win;
        _nav = nav;
        Root = Build();
        Rebuild();
    }

    private FrameworkElement Build()
    {
        var header = Ui.PageHeader(
            "外观", Icons.ForFile("appearance"), null,
            "界面配色、侧边栏折叠、主题色与分区材质。改动即时生效，并记住到下次启动。", null, null);
        header.Margin = Ui.T(0, 0, 0, 14);

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

    private void Rebuild()
    {
        _rgb.Clear();
        _body.Children.Clear();
        _body.Children.Add(SchemeCard());
        _body.Children.Add(PaneCard());
        _body.Children.Add(AccentCard());
        _body.Children.Add(MaterialCard());
        _body.Children.Add(Ui.Text(
            "Windows 只允许每个窗口存在一种桌面材质。当侧边栏与主界面选择不同材质时，窗口采用亚克力，云母一侧改用更浓的着色层近似；两者都选不透明时关闭桌面材质。",
            12, "Brasa.TextDim", wrap: true));
    }

    // ------------------------------------------------------------------ scheme

    private UIElement SchemeCard() => Card("配色",
        Row(new SymbolIcon { Symbol = SymbolRegular.BrightnessHigh24 }, "界面配色",
            "深色为默认。浅色下强调色会按对比度自动压深，以保持正文与控件可读。",
            Ui.Segmented(SchemeLabels, (int)_scheme, i =>
            {
                _scheme = (Scheme)i;
                Appearance.SaveScheme(_scheme);
                Appearance.ApplyScheme(_win, _scheme);
                // Cached pages still hold brushes resolved against the previous palette; this view rebuilds itself
                // because Ui.Segmented cannot move its selection from outside.
                _win.RefreshCurrent();
                Rebuild();
            }), last: true));

    // ------------------------------------------------------------------ pane

    private UIElement PaneCard() => Card("侧边栏",
        Row(new SymbolIcon { Symbol = SymbolRegular.PanelLeft24 }, "侧边栏",
            "收起后只保留图标列；也可点侧栏顶部的菜单按钮切换。",
            Ui.Segmented(["展开", "收起"], _nav.IsPaneOpen ? 0 : 1, i => _nav.IsPaneOpen = i == 0), last: true));

    // ------------------------------------------------------------------ accent

    private UIElement AccentCard()
    {
        var items = new List<string> { "跟随系统" };
        items.AddRange(Appearance.Presets.Select(p => $"{p.Label} ({p.Hex})"));
        items.Add("自定义…");

        var current = Current();
        var followSystem = AppEnv.AccentKind == "system";
        var preset = IndexOfPreset(Appearance.ToHex(current));
        var selected = followSystem ? 0 : preset >= 0 ? preset + 1 : items.Count - 1;

        var combo = new ComboBox
        {
            ItemsSource = items,
            SelectedIndex = selected,
            MinWidth = 200,
            VerticalAlignment = VerticalAlignment.Center
        };
        combo.SelectionChanged += (_, _) =>
        {
            var i = combo.SelectedIndex;
            if (i < 0) return;

            if (i == 0)
            {
                SetAccent("system", Appearance.ResolveSystemAccent() ?? Appearance.FromHex(Appearance.DefaultHex));
            }
            else if (i <= Appearance.Presets.Length)
            {
                var hex = Appearance.Presets[i - 1].Hex;
                SetAccent("preset", Appearance.FromHex(hex));
            }
            else
            {
                SetAccent("custom", Current());
            }

            _custom.Visibility = combo.SelectedIndex == items.Count - 1 ? Visibility.Visible : Visibility.Collapsed;
        };

        _swatch = Swatch(current);
        var row = Row(new SymbolIcon { Symbol = SymbolRegular.ColorFill24 }, "主题色",
            "「跟随系统」只取 Windows 的强调色，深色还是浅色请在上方「界面配色」中选择。",
            Ui.HBox(10, _swatch, combo), last: false);

        _hex = new Wpf.Ui.Controls.TextBox { Width = 120, Text = Appearance.ToHex(current), VerticalAlignment = VerticalAlignment.Center };
        _hex.LostFocus += (_, _) => CommitHex();
        _hex.PreviewKeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter) CommitHex();
        };

        _custom = new StackPanel { Visibility = combo.SelectedIndex == items.Count - 1 ? Visibility.Visible : Visibility.Collapsed };
        _custom.Children.Add(Row(null, "自定义颜色", "十六进制写作 E08A2E 或 #E08A2E，回车应用；也可直接用滑块调色。", _hex, last: false));

        for (var i = 0; i < 3; i++)
        {
            var index = i;
            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 255,
                Width = 220,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                Value = Channel(current, index)
            };
            slider.ValueChanged += (_, _) =>
            {
                if (_suppress) return;
                SetAccent("custom", WithChannel(Current(), index, slider.Value));
            };
            _rgb.Add(slider);
            _custom.Children.Add(Row(null, new[] { "红", "绿", "蓝" }[index], null, slider, last: index == 2));
        }

        return Card("主题色", row, _custom);
    }

    private void CommitHex()
    {
        if (!Appearance.TryHex(_hex.Text, out var c))
        {
            _hex.Text = Appearance.ToHex(Current());
            return;
        }
        if (c.Equals(Current())) return;
        SetAccent("custom", c);
    }

    /// <summary>Applies, persists and repaints the row's own editors. The sliders are synced under a guard so the
    /// hex field and the sliders cannot echo each other.</summary>
    private void SetAccent(string kind, Color color)
    {
        Appearance.SaveAccent(kind, Appearance.ToHex(color));
        Appearance.ApplyAccent(color);
        _suppress = true;
        _swatch.Background = new SolidColorBrush(color);
        _hex.Text = Appearance.ToHex(color);
        for (var i = 0; i < _rgb.Count; i++) _rgb[i].Value = Channel(color, i);
        _suppress = false;
    }

    private static Border Swatch(Color c)
    {
        var b = new Border
        {
            Width = 22,
            Height = 22,
            CornerRadius = new CornerRadius(4),
            BorderThickness = Ui.T(1, 1, 1, 1),
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(c)
        };
        b.SetResourceReference(Border.BorderBrushProperty, "Brasa.BorderHi");
        return b;
    }

    private static Color Current() => Appearance.AppliedAccent ?? Appearance.FromHex(Appearance.DefaultHex);

    private static byte Channel(Color c, int index) => index switch { 0 => c.R, 1 => c.G, _ => c.B };

    private static Color WithChannel(Color baseColor, int index, double value)
    {
        var v = (byte)Math.Clamp(Math.Round(value), 0, 255);
        return index switch
        {
            0 => Color.FromRgb(v, baseColor.G, baseColor.B),
            1 => Color.FromRgb(baseColor.R, v, baseColor.B),
            _ => Color.FromRgb(baseColor.R, baseColor.G, v)
        };
    }

    private static int IndexOfPreset(string hex)
    {
        for (var i = 0; i < Appearance.Presets.Length; i++)
            if (string.Equals(Appearance.Presets[i].Hex, hex.TrimStart('#'), StringComparison.OrdinalIgnoreCase)) return i;
        return -1;
    }

    // ------------------------------------------------------------------ material

    private UIElement MaterialCard() => Card("分区材质",
        Row(new SymbolIcon { Symbol = SymbolRegular.ColorBackground24 }, "侧边栏材质",
            "云母透出桌面色调，亚克力带桌面模糊，不透明为实底。",
            Ui.Segmented(MaterialLabels, Array.IndexOf(Materials, _pane), i =>
            {
                _pane = Materials[i];
                ApplyMaterials();
            }), last: false),
        Row(new SymbolIcon { Symbol = SymbolRegular.DarkTheme24 }, "主界面材质",
            "两个分区各选一种；请求冲突时以窗口实际采用的材质为准。",
            Ui.Segmented(MaterialLabels, Array.IndexOf(Materials, _content), i =>
            {
                _content = Materials[i];
                ApplyMaterials();
            }), last: true));

    private void ApplyMaterials()
    {
        Appearance.SaveMaterials(_pane, _content);
        Appearance.ApplyMaterials(_win, _pane, _content);
    }

    // ------------------------------------------------------------------ scaffolding

    private UIElement Row(SymbolIcon? icon, string title, string? subtitle, FrameworkElement editor, bool last)
    {
        if (icon is not null) icon.FontSize = 20;
        var label = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        label.Children.Add(Ui.Text(title, 13.5, weight: FontWeights.SemiBold));
        if (!string.IsNullOrEmpty(subtitle)) label.Children.Add(Ui.Text(subtitle!, 11.5, "Brasa.TextDim", wrap: true));
        return Ui.Line(icon, label, editor, last);
    }

    private static Border Card(string title, params UIElement[] rows)
    {
        var panel = new StackPanel();
        var head = new StackPanel { Margin = Ui.T(12, 0, 12, 6) };
        head.Children.Add(Ui.Text(title, 15, "Brasa.AccentHi", weight: FontWeights.SemiBold));
        panel.Children.Add(head);
        foreach (var r in rows) panel.Children.Add(r);

        var border = new Border
        {
            CornerRadius = new CornerRadius(8),
            Padding = Ui.T(0, 8, 0, 8),
            Margin = Ui.T(0, 0, 0, 14),
            Child = panel
        };
        border.SetResourceReference(Border.BackgroundProperty, "Brasa.Surface");
        border.SetResourceReference(Border.BorderBrushProperty, "Brasa.Sep");
        border.BorderThickness = Ui.T(1, 1, 1, 1);
        return border;
    }
}
