using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>Searchable item dialog so id lists can be filled by name instead of by copying hex.</summary>
public static class ItemPicker
{
    public static string? Pick(Window? owner, Workspace ws, string title)
    {
        var win = new Window
        {
            Title = title,
            Width = 620,
            Height = 520,
            Owner = owner,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("Brasa.Background"),
            ShowInTaskbar = false
        };

        var search = new TextBox { Height = 30, Margin = Ui.T(0, 0, 8, 0), VerticalContentAlignment = VerticalAlignment.Center };
        var status = Ui.Text(ws.Items.IsReady ? "" : "物品索引构建中…", 11.5, "Brasa.TextDim");
        var list = new ListBox { Margin = Ui.T(0, 8, 0, 8), BorderThickness = Ui.T(1, 1, 1, 1) };
        list.SetResourceReference(Control.BorderBrushProperty, "Brasa.Border");

        string? picked = null;

        var ok = new Button { Content = "使用该编号", IsDefault = true, Padding = Ui.T(14, 6, 14, 6) };
        var cancel = new Button { Content = "取消", IsCancel = true, Padding = Ui.T(14, 6, 14, 6) };

        void Run()
        {
            var term = search.Text.Trim();
            if (!ws.Items.IsReady)
            {
                status.Text = "物品索引仍在构建中，稍等再试。";
                return;
            }
            status.Text = "";
            var hits = ws.Items.Search(term, 200).ToList();
            list.Items.Clear();
            foreach (var h in hits) list.Items.Add(MakeRow(h));
            if (hits.Count == 0 && term.Length > 0) status.Text = "没有匹配的物品，可尝试英文名或编号前缀。";
        }

        list.MouseDoubleClick += (_, _) =>
        {
            if (list.SelectedItem is Border b && b.Tag is string id) { picked = id; win.Close(); }
        };
        ok.Click += (_, _) =>
        {
            if (list.SelectedItem is Border b && b.Tag is string id) { picked = id; win.Close(); }
            else if (search.Text.Trim() is { Length: 24 } raw && ItemLookup.LooksLikeId(raw)) { picked = raw; win.Close(); }
        };
        search.TextChanged += (_, _) =>
        {
            if (search.Text.Trim().Length >= 2) Run();
        };
        search.KeyDown += (_, e) => { if (e.Key == Key.Enter) Run(); };

        var grid = new Grid { Margin = Ui.T(14) };
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var top = Ui.HBox(0, search, status);
        Grid.SetRow(top, 0);
        Grid.SetRow(new TextBlock(), 1);
        Grid.SetRow(list, 2);
        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        buttons.Children.Add(ok);
        var cancelWrap = new Border { Child = cancel, Margin = Ui.T(8, 0, 0, 0) };
        buttons.Children.Add(cancelWrap);
        Grid.SetRow(buttons, 3);

        grid.Children.Add(top);
        grid.Children.Add(list);
        grid.Children.Add(buttons);
        win.Content = grid;

        win.Loaded += (_, _) =>
        {
            search.Focus();
            _ = ws.Items.EnsureLoadedAsync().ContinueWith(_ =>
            {
                if (!win.IsLoaded) return;
                win.Dispatcher.Invoke(() => { status.Text = ""; if (search.Text.Trim().Length >= 2) Run(); });
            }, TaskScheduler.Default);
        };

        win.ShowDialog();
        return picked;
    }

    private static Border MakeRow(ItemInfo info)
    {
        var grid = new Grid { Margin = Ui.T(2, 3, 2, 3) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        grid.Children.Add(Ui.Text(info.Display, 13));
        var en = Ui.Text(info.NameEn == info.Display ? "" : info.NameEn, 11.5, "Brasa.TextDim");
        en.Margin = Ui.T(10, 0, 0, 0);
        Grid.SetColumn(en, 1);
        grid.Children.Add(en);

        var price = Ui.Text(info.PriceText, 11.5, "Brasa.TextDim");
        price.Margin = Ui.T(10, 0, 0, 0);
        Grid.SetColumn(price, 2);
        grid.Children.Add(price);

        return new Border { Child = grid, Tag = info.Id, Padding = Ui.T(4, 2, 4, 2) };
    }
}

/// <summary>Free-form JSON text entry used when appending whole objects to a config.</summary>
public static class JsonInputDialog
{
    public static string? Edit(Window? owner, string title, string initial, double height)
    {
        var win = new Window
        {
            Title = title,
            Width = 640,
            Height = height + 150,
            Owner = owner,
            WindowStartupLocation = owner is null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner,
            Background = (System.Windows.Media.Brush)Application.Current.FindResource("Brasa.Background"),
            ShowInTaskbar = false
        };

        var box = new TextBox
        {
            Text = initial,
            Style = (Style)Application.Current.FindResource("Brasa.JsonBox"),
            AcceptsReturn = true,
            Margin = Ui.T(0, 0, 0, 10)
        };
        var error = Ui.Text("", 11.5, "Brasa.Bad", wrap: true);
        string? result = null;

        void TryClose()
        {
            try
            {
                if (ConfigDocument.Parse(box.Text) is null) throw new System.Text.Json.JsonException("内容为空");
                result = box.Text;
                win.Close();
            }
            catch (System.Text.Json.JsonException ex)
            {
                error.Text = "JSON 无效：" + ex.Message;
            }
        }

        var ok = new Button { Content = "确定", Padding = Ui.T(16, 6, 16, 6) };
        ok.Click += (_, _) => TryClose();
        var cancel = new Button { Content = "取消", IsCancel = true, Padding = Ui.T(16, 6, 16, 6), Margin = Ui.T(8, 0, 0, 0) };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(ok);
        buttons.Children.Add(cancel);

        var panel = new DockPanel { Margin = Ui.T(14) };
        DockPanel.SetDock(buttons, Dock.Bottom);
        DockPanel.SetDock(error, Dock.Bottom);
        panel.Children.Add(buttons);
        panel.Children.Add(error);
        panel.Children.Add(box);
        win.Content = panel;
        win.ShowDialog();
        return result;
    }
}
