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

    /// <summary>label column + control column, used for every leaf editor.</summary>
    public static Grid Row(FrameworkElement label, FrameworkElement editor, double labelWidth = 250)
    {
        var g = new Grid { Margin = T(0, 3, 0, 3) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(labelWidth) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        Grid.SetColumn(label, 0);
        Grid.SetColumn(editor, 1);
        g.Children.Add(label);
        g.Children.Add(editor);
        return g;
    }

    public static Button Btn(string content, RoutedEventHandler onClick, bool primary = false, string tip = "")
    {
        var b = new Button
        {
            Content = content,
            Padding = T(11, 5, 11, 5),
            Cursor = System.Windows.Input.Cursors.Hand
        };
        if (primary) b.Style = (Style)Application.Current.FindResource("Brasa.Button.Primary");
        if (tip.Length > 0) b.ToolTip = tip;
        b.Click += onClick;
        return b;
    }

    public static Border Card(UIElement child, Thickness? padding = null, string margin = "")
    {
        var b = new Border
        {
            Background = Res("Brasa.Surface"),
            BorderBrush = Res("Brasa.Border"),
            BorderThickness = T(1, 1, 1, 1),
            CornerRadius = new CornerRadius(5),
            Padding = padding ?? T(14, 11, 14, 12),
            Child = child
        };
        b.SetResourceReference(Border.BorderBrushProperty, "Brasa.Border");
        b.SetResourceReference(Border.BackgroundProperty, "Brasa.Surface");
        return b;
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
            Background = Res(bg),
            BorderBrush = Res("Brasa.Border"),
            BorderThickness = T(1, 1, 1, 1),
            CornerRadius = new CornerRadius(3),
            Padding = T(7, 2, 7, 2),
            Child = Text(text, size, fg)
        };
        return b;
    }

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
