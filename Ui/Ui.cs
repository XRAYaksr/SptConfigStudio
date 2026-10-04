using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SptConfigStudio.Views;

/// <summary>Small builders shared by the code-defined views, so colours stay resource-driven.</summary>
public static class Ui
{
    public static Brush Res(string key) => (Brush)Application.Current.FindResource(key);
    public static Thickness T(double l, double t, double r, double b) => new(l, t, r, b);
    public static Thickness T(double all) => new(all);

    public static TextBlock Text(string s, double size = 13, string fg = "Brasa.Text", bool wrap = false, FontWeight? weight = null)
    {
        var tb = new TextBlock
        {
            Text = s,
            FontSize = size,
            TextWrapping = wrap ? TextWrapping.Wrap : TextWrapping.NoWrap,
            FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Font")
        };
        tb.SetResourceReference(TextBlock.ForegroundProperty, fg);
        if (weight is not null) tb.FontWeight = weight.Value;
        return tb;
    }

    public static TextBlock Mono(string s, double size = 11.5, string fg = "Brasa.TextDim")
    {
        var tb = Text(s, size, fg);
        tb.FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Mono");
        return tb;
    }

    /// <summary>Fluent settings row. The icon column is dropped when there is no glyph, and the hairline starts
    /// under the label so it never cuts across the icon.</summary>
    public static Grid Line(Wpf.Ui.Controls.IconElement? icon, FrameworkElement label, FrameworkElement editor, bool last = false)
    {
        var g = new Grid { MinHeight = 40 };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        double inset = 12;
        if (icon is not null)
        {
            icon.Width = 20;
            icon.Height = 20;
            icon.Margin = T(12, 0, 10, 0);
            icon.VerticalAlignment = VerticalAlignment.Center;
            Grid.SetColumn(icon, 0);
            g.Children.Add(icon);
            inset = 42;
        }

        label.VerticalAlignment = VerticalAlignment.Center;
        label.Margin = T(icon is null ? 12 : 0, 4, 12, 4);
        Grid.SetColumn(label, 1);
        editor.VerticalAlignment = VerticalAlignment.Center;
        editor.Margin = T(0, 4, 12, 4);
        Grid.SetColumn(editor, 2);
        g.Children.Add(label);
        g.Children.Add(editor);

        var sep = new Border
        {
            Height = 1,
            Tag = "rowsep",
            Margin = T(inset, 0, 12, 0),
            VerticalAlignment = VerticalAlignment.Bottom,
            Visibility = last ? Visibility.Collapsed : Visibility.Visible
        };
        sep.SetResourceReference(Border.BackgroundProperty, "Brasa.Sep");
        Grid.SetColumn(sep, 0);
        Grid.SetColumnSpan(sep, 3);
        g.Children.Add(sep);
        return g;
    }

    /// <summary>Drops the hairline a row drew for itself; used once the row turns out to be the last in its card.</summary>
    public static void TrimDivider(Grid row)
    {
        foreach (var child in row.Children)
            if (child is Border b && b.Tag as string == "rowsep")
            {
                b.Visibility = Visibility.Collapsed;
                return;
            }
    }

    /// <summary>Icon-less row, for the few places that have no meaningful glyph.</summary>
    public static Grid Row(FrameworkElement label, FrameworkElement editor) => Line(null, label, editor);

    public static Button Btn(string content, RoutedEventHandler onClick, bool primary = false, string tip = "")
    {
        Button b = primary
            ? new Wpf.Ui.Controls.Button { Style = (Style)Application.Current.FindResource("Brasa.Button.Primary") }
            : new Button();
        b.Content = content;
        b.Padding = T(11, 5, 11, 5);
        b.Cursor = System.Windows.Input.Cursors.Hand;
        if (tip.Length > 0) b.ToolTip = tip;
        b.Click += onClick;
        return b;
    }

    public static Border Card(UIElement child, Thickness? padding = null, string margin = "")
    {
        var b = new Border
        {
            BorderThickness = T(1, 1, 1, 1),
            CornerRadius = new CornerRadius(8),
            Padding = padding ?? T(14, 11, 14, 12),
            Child = child
        };
        b.SetResourceReference(Border.BorderBrushProperty, "Brasa.Border");
        b.SetResourceReference(Border.BackgroundProperty, "Brasa.Surface");
        return b;
    }

    /// <summary>Windows-settings page header: amber glyph, title, optional mono path, wrapped description,
    /// and right-aligned page actions. <paramref name="status"/> appends a live line under the description.</summary>
    public static Grid PageHeader(string title, Wpf.Ui.Controls.IconElement? icon, string? monoPath, string description,
                                  UIElement? trailing = null, UIElement? status = null)
    {
        var g = new Grid();
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var left = VBox(Text(title, 20, weight: FontWeights.SemiBold));
        if (!string.IsNullOrEmpty(monoPath))
        {
            var path = Mono(monoPath!, 12);
            path.Margin = T(0, 3, 0, 0);
            left.Children.Add(path);
        }
        if (!string.IsNullOrEmpty(description))
        {
            var desc = Text(description, 13, "Brasa.TextDim", wrap: true);
            desc.Margin = T(0, 4, 0, 0);
            left.Children.Add(desc);
        }
        if (status is not null)
        {
            if (status is FrameworkElement se) se.Margin = T(0, 4, 0, 0);
            left.Children.Add(status);
        }

        if (icon is not null)
        {
            icon.Width = 26;
            icon.Height = 26;
            icon.Margin = T(0, 2, 12, 0);
            icon.VerticalAlignment = VerticalAlignment.Top;
            icon.SetResourceReference(Control.ForegroundProperty, "Brasa.Accent");
            Grid.SetColumn(icon, 0);
            g.Children.Add(icon);
        }
        Grid.SetColumn(left, 1);
        g.Children.Add(left);

        if (trailing is not null)
        {
            var tr = trailing as FrameworkElement ?? new Border { Child = trailing };
            tr.HorizontalAlignment = HorizontalAlignment.Right;
            tr.VerticalAlignment = VerticalAlignment.Top;
            tr.Margin = T(16, 2, 0, 0);
            Grid.SetColumn(tr, 2);
            g.Children.Add(tr);
        }
        return g;
    }

    /// <summary>Fluent pill switcher. WPF-UI 4.3 has no Segmented control, and the implicit TabControl draws a
    /// content box around the page, which is the visual noise this replaces.</summary>
    public static Border Segmented(IReadOnlyList<string> items, int selected, Action<int> onSelected)
    {
        var style = (Style)Application.Current.FindResource("Brasa.Segment");
        var panel = new StackPanel { Orientation = Orientation.Horizontal };
        var btns = new List<Button>(items.Count);

        void Paint()
        {
            for (var i = 0; i < btns.Count; i++) btns[i].Tag = i == selected ? "on" : "";
        }

        for (var i = 0; i < items.Count; i++)
        {
            var idx = i;
            var b = new Button { Content = items[i], Style = style, Margin = T(i == 0 ? 0 : 2, 0, 0, 0) };
            b.Click += (_, _) =>
            {
                if (idx == selected) return;
                selected = idx;
                Paint();
                onSelected(idx);
            };
            btns.Add(b);
            panel.Children.Add(b);
        }
        Paint();

        var container = new Border
        {
            CornerRadius = new CornerRadius(6),
            Padding = T(2),
            Child = panel
        };
        container.SetResourceReference(Border.BackgroundProperty, "Brasa.SurfaceAlt");
        return container;
    }

    public static StackPanel VBox(params UIElement[] children)
    {
        var p = new StackPanel { Orientation = Orientation.Vertical };
        foreach (var c in children) p.Children.Add(c);
        return p;
    }

    public static StackPanel HBox(double spacing, params UIElement[] children)
    {
        var p = new StackPanel { Orientation = Orientation.Horizontal };
        foreach (var c in children)
        {
            if (!ReferenceEquals(c, children[0]) && c is FrameworkElement fe) fe.Margin = T(spacing, 0, 0, 0);
            p.Children.Add(c);
        }
        return p;
    }

    /// <summary>Small rounded tag, used for item names and ids.</summary>
    public static Border Chip(string text, string fg = "Brasa.Text", string bg = "Brasa.SurfaceHi", double size = 11.5)
    {
        var b = new Border
        {
            BorderThickness = T(1, 1, 1, 1),
            CornerRadius = new CornerRadius(3),
            Padding = T(7, 2, 7, 2),
            Child = Text(text, size, fg)
        };
        b.SetResourceReference(Border.BackgroundProperty, bg);
        b.SetResourceReference(Border.BorderBrushProperty, "Brasa.Border");
        return b;
    }

    /// <summary>Modal shell: a real title bar and rounded corners, but no backdrop and an opaque surface. Windows'
    /// own dialogs are not translucent, and a see-through change list would double-mix with the window behind it.</summary>
    public static void Chrome(Wpf.Ui.Controls.FluentWindow win, string title, UIElement body, bool maximizable = false)
    {
        win.Title = title;
        win.SetResourceReference(Control.ForegroundProperty, "Brasa.Text");
        win.WindowBackdropType = Wpf.Ui.Controls.WindowBackdropType.None;
        win.WindowCornerPreference = Wpf.Ui.Controls.WindowCornerPreference.Round;
        win.ExtendsContentIntoTitleBar = true;
        SptConfigStudio.Theme.Appearance.AssertWindowBackground(win);

        var bar = new Wpf.Ui.Controls.TitleBar
        {
            Title = title,
            FontWeight = FontWeights.SemiBold,
            ShowHelp = false,
            ShowMaximize = maximizable
        };

        var glyph = new Wpf.Ui.Controls.SymbolIcon
        {
            Symbol = Wpf.Ui.Controls.SymbolRegular.Server24,
            FontSize = 15
        };
        glyph.SetResourceReference(Wpf.Ui.Controls.IconElement.ForegroundProperty, "Brasa.Accent");
        bar.Icon = glyph;

        var grid = new Grid();
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        Grid.SetRow(bar, 0);
        Grid.SetRow(body, 1);
        grid.Children.Add(bar);
        grid.Children.Add(body);
        win.Content = grid;
    }

    /// <summary>The default text colour every window used to get from the implicit TextBlock style. The caption bar's
    /// light/dark flag is not ours to set: WindowBackdrop.ApplyBackdrop reads ApplicationThemeManager.GetAppTheme().</summary>
    public static void BaseChrome(Window w) => w.SetResourceReference(Control.ForegroundProperty, "Brasa.Text");

    public static void SelectAllOnFocus(TextBox tb)
    {
        tb.GotKeyboardFocus += (_, _) => tb.Dispatcher.BeginInvoke(() => tb.SelectAll());
        tb.PreviewMouseLeftButtonDown += (_, e) =>
        {
            if (!tb.IsKeyboardFocused)
            {
                e.Handled = true;
                tb.Focus();
                tb.SelectAll();
            }
        };
    }
}
