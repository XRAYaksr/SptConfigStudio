using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Text.Json;
using System.Text.Json.Nodes;
using SptConfigStudio.Core;

namespace SptConfigStudio.Views;

/// <summary>
/// Turns a <see cref="FieldNode"/> into an editor with no per-config markup: the inferred kind picks the control,
/// curated metadata supplies labels and ranges. Structural edits ask the host to rebuild.
/// </summary>
public sealed class FieldRenderer
{
    private readonly Workspace _ws;
    public Action? Rebuild { get; set; }
    public Action? Touched { get; set; }

    private Brush Danger => Ui.Res("Brasa.Bad");
    private const int InlineCap = 240;
    private const int ObjectCap = 80;

    public FieldRenderer(Workspace ws) => _ws = ws;

    public FrameworkElement Render(FieldNode n, int depth)
    {
        if (depth == 0) n.DefaultExpanded = true;
        var el = n.Kind switch
        {
            FieldKind.Toggle => ToggleRow(n, depth),
            FieldKind.Number or FieldKind.Percent or FieldKind.Duration => NumberRow(n, depth),
            FieldKind.Text or FieldKind.Enum => TextRow(n, depth),
            FieldKind.IdRef => IdRefRow(n, depth),
            FieldKind.Range => RangeRow(n, depth),
            FieldKind.Group => Section(n, depth, () => ChildrenStack(n, depth)),
            FieldKind.IdList => Section(n, depth, () => IdListBody(n)),
            FieldKind.ScalarList => Section(n, depth, () => ScalarListBody(n)),
            FieldKind.ObjectList => Section(n, depth, () => ObjectListBody(n, depth + 1)),
            FieldKind.IdMap => Section(n, depth, () => IdMapBody(n)),
            FieldKind.IdMapRange => Section(n, depth, () => IdMapRangeBody(n)),
            _ => RawRow(n, depth)
        };
        el.Tag = n.Path;
        return el;
    }

    // ---------------------------------------------------------------- labels

    private static StackPanel LabelFor(FieldNode n, int depth)
    {
        var panel = new StackPanel { Orientation = Orientation.Vertical, VerticalAlignment = VerticalAlignment.Center };
        var label = Ui.Text(n.Label, depth == 0 ? 13.5 : 13, weight: n.IsLeaf ? null : FontWeights.SemiBold);
        if (n.Hint.Length > 0) label.ToolTip = n.Hint;
        panel.Children.Add(label);

        if (n.Label != n.Key)
        {
            var key = Ui.Mono(n.Key, 10.5);
            key.Margin = Ui.T(0, 1, 0, 0);
            panel.Children.Add(key);
        }
        return panel;
    }

    private TextBlock ValueHint(FieldNode n, string text)
    {
        var tb = Ui.Text(text, 11.5, "Brasa.TextDim");
        tb.Margin = Ui.T(8, 0, 0, 0);
        tb.VerticalAlignment = VerticalAlignment.Center;
        return tb;
    }

    // ---------------------------------------------------------------- toggles

    private FrameworkElement ToggleRow(FieldNode n, int depth)
    {
        var sw = new CheckBox { Style = (Style)Application.Current.FindResource("Brasa.Switch"), IsChecked = n.GetBool() };
        var state = Ui.Text(sw.IsChecked.Value ? "开" : "关", 12, sw.IsChecked.Value ? "Brasa.Good" : "Brasa.TextDim");
        state.Margin = Ui.T(9, 0, 0, 0);
        state.VerticalAlignment = VerticalAlignment.Center;

        sw.Checked += (_, _) => { n.SetBool(true); state.Text = "开"; state.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Good"); Touch(n); };
        sw.Unchecked += (_, _) => { n.SetBool(false); state.Text = "关"; state.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.TextDim"); Touch(n); };

        return Ui.Row(LabelFor(n, depth), Ui.HBox(0, sw, state));
    }

    // ---------------------------------------------------------------- numbers

    private FrameworkElement NumberRow(FieldNode n, int depth)
    {
        var val = n.GetNumber();
        var box = new TextBox
        {
            Text = FieldNode.NumberText(val),
            Width = 140,
            Height = 28,
            HorizontalAlignment = HorizontalAlignment.Left,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Mono")
        };
        Ui.SelectAllOnFocus(box);

        var bounded = n.Min is not null && n.Max is not null && n.Max - n.Min <= 100000;
        var slider = bounded
            ? new Slider
            {
                Minimum = n.Min!.Value,
                Maximum = n.Max!.Value,
                Value = Math.Clamp(val, n.Min.Value, n.Max.Value),
                Width = 190,
                VerticalAlignment = VerticalAlignment.Center,
                IsSnapToTickEnabled = n.Max.Value - n.Min.Value <= 100
            }
            : null;
        if (slider is { IsSnapToTickEnabled: true }) slider.TickFrequency = 1;

        var hint = ValueHint(n, Describe(n, val));

        void Commit(string text, bool fromSlider)
        {
            if (!TryParseNumber(text, out var parsed))
            {
                box.BorderBrush = Danger;
                box.ToolTip = "请输入数字（可用小数、负数；-1 通常表示不覆写）。";
                return;
            }
            box.BorderBrush = Ui.Res("Brasa.Border");
            n.SetNumber(parsed);
            hint.Text = Describe(n, parsed);
            if (!fromSlider && slider is not null) slider.Value = Math.Clamp(parsed, slider.Minimum, slider.Maximum);
            Touch(n);
        }

        box.LostFocus += (_, _) => Commit(box.Text, false);
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) Commit(box.Text, false); };
        if (slider is not null)
            slider.ValueChanged += (_, e) =>
            {
                var v = Math.Round(e.NewValue, 4);
                box.Text = FieldNode.NumberText(v);
                Commit(box.Text, true);
            };

        var editors = slider is null ? (UIElement)box : Ui.HBox(10, box, slider);
        var wrap = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
        wrap.Children.Add(editors);
        wrap.Children.Add(hint);
        return Ui.Row(LabelFor(n, depth), wrap);
    }

    private string Describe(FieldNode n, double v) => n.Kind switch
    {
        FieldKind.Duration => v < 0 ? "不覆写，沿用游戏默认耗时" : "≈ " + FieldNode.DurationText(v),
        FieldKind.Percent => "%",
        _ when n.Unit.Length > 0 => n.Unit,
        _ when v >= 1000 && n.Key.ToLowerInvariant().Contains("rub") => $"≈ {v:N0} ₽",
        _ => ""
    };

    private bool TryParseNumber(string text, out double value) =>
        double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.InvariantCulture, out value)
        || double.TryParse(text.Trim(), NumberStyles.Any, CultureInfo.CurrentCulture, out value);

    // ---------------------------------------------------------------- text / enum

    private FrameworkElement TextRow(FieldNode n, int depth)
    {
        if (n.EnumOptions is { Length: > 0 })
        {
            var options = n.EnumOptions.ToList();
            var current = n.GetString();
            if (!options.Contains(current)) options.Insert(0, current);
            var combo = new ComboBox { ItemsSource = options, SelectedItem = current, Width = 240, HorizontalAlignment = HorizontalAlignment.Left };
            combo.SelectionChanged += (_, _) =>
            {
                if (combo.SelectedItem is string s) { n.SetString(s); Touch(n); }
            };
            return Ui.Row(LabelFor(n, depth), combo);
        }

        var box = new TextBox { Text = n.GetString(), MinWidth = 240, MaxWidth = 520, Height = 28, HorizontalAlignment = HorizontalAlignment.Left };
        Ui.SelectAllOnFocus(box);
        box.LostFocus += (_, _) => { n.SetString(box.Text); Touch(n); };
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) { n.SetString(box.Text); Touch(n); } };
        return Ui.Row(LabelFor(n, depth), box);
    }

    // ---------------------------------------------------------------- item refs

    private FrameworkElement IdRefRow(FieldNode n, int depth)
    {
        var box = new TextBox
        {
            Text = n.GetString(),
            Width = 220,
            Height = 28,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Mono"),
            FontSize = 11.5
        };
        Ui.SelectAllOnFocus(box);
        var chip = Ui.Chip(DescribeId(n.GetString()));

        void Commit()
        {
            n.SetString(box.Text.Trim());
            chip.Child = Ui.Text(DescribeId(box.Text), 11.5);
            Touch(n);
        }
        box.LostFocus += (_, _) => Commit();
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) Commit(); };

        var pick = Ui.Btn("选择…", (_, _) =>
        {
            var id = ItemPicker.Pick(Window.GetWindow(box), _ws, "选择物品");
            if (id is null) return;
            box.Text = id;
            Commit();
        });

        return Ui.Row(LabelFor(n, depth), Ui.HBox(8, box, chip, pick));
    }

    private string DescribeId(string id)
    {
        var info = _ws.Items.Find(id);
        if (info is null) return ItemLookup.LooksLikeId(id) ? "未知编号 " + id : "(空)";
        var fam = _ws.Items.FamilyOf(id);
        var suffix = fam.Length == 0 || fam == info.Display ? "" : " · " + fam;
        return info.Display + suffix + "  " + info.PriceText;
    }

    // ---------------------------------------------------------------- lists

    private FrameworkElement IdListBody(FieldNode n)
    {
        var panel = new StackPanel();
        if (n.AsArray() is not JsonArray arr) return panel;

        var shown = Math.Min(arr.Count, InlineCap);
        for (var i = 0; i < shown; i++)
        {
            var idx = i;
            var value = arr[idx] is JsonValue v ? (v.TryGetValue(out string? s) ? s ?? "" : v.ToJsonString()) : arr[idx]?.ToJsonString() ?? "";

            var name = Ui.Text(DescribeId(value), 12);
            name.TextTrimming = TextTrimming.CharacterEllipsis;

            var idBox = new TextBox
            {
                Text = value,
                Width = 210,
                Height = 24,
                FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Mono"),
                FontSize = 11.5
            };
            Ui.SelectAllOnFocus(idBox);
            idBox.LostFocus += (_, _) =>
            {
                var trimmed = idBox.Text.Trim();
                if (trimmed == value || idx >= arr.Count) return;
                arr[idx] = JsonValue.Create(trimmed);
                Touch(n);
            };

            var del = SmallDelete(() =>
            {
                if (idx < arr.Count) { arr.RemoveAt(idx); Touch(n); Rebuild?.Invoke(); }
            });

            panel.Children.Add(TwoColumn(name, Ui.HBox(8, idBox, del)));
        }

        if (arr.Count > shown)
            panel.Children.Add(Ui.Text($"… 另有 {arr.Count - shown} 项未显示，请用「原始 JSON」页签编辑。", 11.5, "Brasa.Warn", wrap: true));

        panel.Children.Add(AddItemRow(v =>
        {
            arr.Add(JsonValue.Create(v));
            Touch(n);
            Rebuild?.Invoke();
        }, "新增编号", allowFreeText: false));
        return panel;
    }

    private FrameworkElement ScalarListBody(FieldNode n)
    {
        var panel = new StackPanel();
        if (n.AsArray() is not JsonArray arr) return panel;

        var shown = Math.Min(arr.Count, InlineCap);
        for (var i = 0; i < shown; i++)
        {
            var idx = i;
            var box = new TextBox
            {
                Text = arr[idx]?.ToJsonString().Trim('"') ?? "",
                Height = 24,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            Ui.SelectAllOnFocus(box);
            box.LostFocus += (_, _) =>
            {
                if (idx < arr.Count) { arr[idx] = JsonValue.Create(box.Text); Touch(n); }
            };
            panel.Children.Add(Ui.HBox(8, box,
                SmallDelete(() => { if (idx < arr.Count) { arr.RemoveAt(idx); Touch(n); Rebuild?.Invoke(); } })));
        }

        if (arr.Count > shown)
            panel.Children.Add(Ui.Text($"… 另有 {arr.Count - shown} 项未显示。", 11.5, "Brasa.Warn"));

        panel.Children.Add(AddItemRow(v =>
        {
            arr.Add(JsonValue.Create(v));
            Touch(n);
            Rebuild?.Invoke();
        }, "新增一项", allowFreeText: true));
        return panel;
    }

    private FrameworkElement ObjectListBody(FieldNode n, int depth)
    {
        var panel = new StackPanel();
        if (n.AsArray() is not JsonArray arr) return panel;

        var count = Math.Min(arr.Count, ObjectCap);
        for (var i = 0; i < count; i++)
        {
            var idx = i;
            var title = DescribeObject(arr[idx], idx);
            var child = new FieldNode
            {
                File = n.File,
                Key = title,
                Path = $"{n.Path}[{idx}]",
                Container = arr,
                Index = idx,
                Owner = n,
                Lookup = n.Lookup,
                Kind = FieldKind.Group,
                Label = title,
                DefaultExpanded = arr.Count <= 6
            };

            panel.Children.Add(Section(child, depth,
                () => ChildrenStack(child, depth),
                SmallDelete(() => { if (idx < arr.Count) { arr.RemoveAt(idx); Touch(n); Rebuild?.Invoke(); } })));
        }

        if (arr.Count > count)
            panel.Children.Add(Ui.Text($"共 {arr.Count} 条，仅渲染前 {count} 条。", 11.5, "Brasa.Warn"));

        panel.Children.Add(Ui.HBox(8,
            Ui.Btn("追加空白条目", (_, _) =>
            {
                arr.Add(new JsonObject());
                Touch(n);
                Rebuild?.Invoke();
            }, tip: "插入一个空对象，再逐项填写"),
            Ui.Btn("粘贴 JSON 追加", (_, _) =>
            {
                var json = JsonInputDialog.Edit(Window.GetWindow(panel), "新增条目 JSON", "{\n  \"键\": \"值\"\n}", 240);
                if (json is null) return;
                try
                {
                    var node = ConfigDocument.Parse(json) ?? throw new JsonException("内容为空");
                    arr.Add(node);
                    Touch(n);
                    Rebuild?.Invoke();
                }
                catch (JsonException ex)
                {
                    MessageBox.Show(ex.Message, "JSON 无效", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            })));
        return panel;
    }

    private static string DescribeObject(JsonNode? el, int idx)
    {
        var title = $"第 {idx + 1} 项";
        if (el is not JsonObject o) return title;
        foreach (var pref in new[] { "newId", "id", "name", "craftIdToCopy", "areaType", "reward" })
        {
            if (!o.ContainsKey(pref)) continue;
            var v = o[pref];
            var txt = v is JsonArray a && a.Count > 0 ? a[0]?.ToJsonString().Trim('"') : v?.ToJsonString().Trim('"');
            if (string.IsNullOrEmpty(txt)) continue;
            return $"第 {idx + 1} 项 · {Shorten(txt!)}";
        }
        return title;
    }

    // ---------------------------------------------------------------- id maps

    private FrameworkElement IdMapBody(FieldNode n)
    {
        var panel = new StackPanel();
        if (n.AsObject() is not JsonObject obj) return panel;

        var keys = obj.Select(p => p.Key).ToList();
        var shown = Math.Min(keys.Count, InlineCap);
        for (var i = 0; i < shown; i++)
        {
            var key = keys[i];
            var child = ChildNumber(n, key, obj, key);
            var name = Ui.Text(DescribeId(key), 12);
            name.TextTrimming = TextTrimming.CharacterEllipsis;
            panel.Children.Add(TwoColumn(name, Ui.HBox(8, NumberBox(child, n),
                SmallDelete(() => { obj.Remove(key); Touch(n); Rebuild?.Invoke(); }))));
        }
        if (keys.Count > shown) panel.Children.Add(Ui.Text($"… 另有 {keys.Count - shown} 项未显示。", 11.5, "Brasa.Warn"));

        panel.Children.Add(Ui.HBox(8,
            Ui.Btn("新增物品…", (_, _) =>
            {
                var id = ItemPicker.Pick(Window.GetWindow(panel), _ws, "选择要加入的物品");
                if (id is null || obj.ContainsKey(id)) return;
                obj[id] = JsonValue.Create(1);
                Touch(n);
                Rebuild?.Invoke();
            }),
            Ui.Btn("粘贴 JSON 合并", (_, _) =>
            {
                var json = JsonInputDialog.Edit(Window.GetWindow(panel), "要合并的 {编号: 数值} 对象", "{\n  \"5449016a4bdc2d6f028b456f\": 1\n}", 200);
                if (json is null) return;
                try
                {
                    if (ConfigDocument.Parse(json) is not JsonObject add) throw new JsonException("需要是一个 JSON 对象");
                    foreach (var p in add) obj[p.Key] = p.Value?.DeepClone();
                    Touch(n);
                    Rebuild?.Invoke();
                }
                catch (JsonException ex) { MessageBox.Show(ex.Message, "JSON 无效", MessageBoxButton.OK, MessageBoxImage.Warning); }
            })));
        return panel;
    }

    private FrameworkElement IdMapRangeBody(FieldNode n)
    {
        var panel = new StackPanel();
        if (n.AsObject() is not JsonObject obj) return panel;

        var keys = obj.Select(p => p.Key).ToList();
        var shown = Math.Min(keys.Count, InlineCap);
        for (var i = 0; i < shown; i++)
        {
            var key = keys[i];
            if (obj[key] is not JsonObject pair) continue;
            var name = Ui.Text(DescribeId(key), 12);
            name.TextTrimming = TextTrimming.CharacterEllipsis;

            var mins = Ui.HBox(4, Ui.Text("最小 min", 11, "Brasa.TextDim"), NumberBox(ChildNumber(n, "min", pair, key), n));
            var maxs = Ui.HBox(4, Ui.Text("最大 max", 11, "Brasa.TextDim"), NumberBox(ChildNumber(n, "max", pair, key), n));
            panel.Children.Add(TwoColumn(name, Ui.HBox(10, mins, maxs,
                SmallDelete(() => { obj.Remove(key); Touch(n); Rebuild?.Invoke(); }))));
        }
        if (keys.Count > shown) panel.Children.Add(Ui.Text($"… 另有 {keys.Count - shown} 项未显示。", 11.5, "Brasa.Warn"));

        panel.Children.Add(Ui.Btn("新增物品…", (_, _) =>
        {
            var id = ItemPicker.Pick(Window.GetWindow(panel), _ws, "选择要加入的物品");
            if (id is null || obj.ContainsKey(id)) return;
            obj[id] = new JsonObject { ["min"] = 1, ["max"] = 1 };
            Touch(n);
            Rebuild?.Invoke();
        }));
        return panel;
    }

    private FieldNode ChildNumber(FieldNode parent, string prop, JsonObject container, string labelKey)
    {
        var child = new FieldNode
        {
            File = parent.File,
            Key = prop,
            Path = $"{parent.Path}.{labelKey}",
            Container = container,
            Lookup = parent.Lookup,
            Kind = FieldKind.Number,
            Label = prop
        };
        if (prop == labelKey) MetadataCatalog.Apply(child);
        return child;
    }

    private TextBox NumberBox(FieldNode child, FieldNode owner)
    {
        var box = new TextBox
        {
            Text = FieldNode.NumberText(child.GetNumber()),
            Width = 130,
            Height = 26,
            HorizontalContentAlignment = HorizontalAlignment.Right,
            FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Mono")
        };
        Ui.SelectAllOnFocus(box);
        void Commit()
        {
            if (TryParseNumber(box.Text, out var v)) { child.SetNumber(v); Touch(owner); }
            else box.BorderBrush = Danger;
        }
        box.LostFocus += (_, _) => Commit();
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) Commit(); };
        return box;
    }

    // ---------------------------------------------------------------- min/max pair

    private FrameworkElement RangeRow(FieldNode n, int depth)
    {
        if (n.AsObject() is not JsonObject obj)
            return Ui.Row(LabelFor(n, depth), Ui.Text("—", 12, "Brasa.TextDim"));

        var min = ChildNumber(n, "min", obj, "min");
        var max = ChildNumber(n, "max", obj, "max");
        var left = Ui.HBox(4, Ui.Text("最小 min", 11.5, "Brasa.TextDim"), NumberBox(min, n));
        var right = Ui.HBox(4, Ui.Text("最大 max", 11.5, "Brasa.TextDim"), NumberBox(max, n));
        return Ui.Row(LabelFor(n, depth), Ui.HBox(14, left, right));
    }

    // ---------------------------------------------------------------- raw

    private FrameworkElement RawRow(FieldNode n, int depth)
    {
        var box = new TextBox
        {
            Text = n.RawValue?.ToJsonString() ?? "null",
            Style = (Style)Application.Current.FindResource("Brasa.JsonBox"),
            Height = 120,
            Margin = Ui.T(0, 4, 0, 4)
        };
        var status = Ui.Text("", 11.5, "Brasa.TextDim");
        var apply = Ui.Btn("应用此 JSON", (_, _) =>
        {
            if (n.TrySetRawJson(box.Text))
            {
                status.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Good");
                status.Text = "已应用";
                Touch(n);
                Rebuild?.Invoke();
            }
            else
            {
                status.SetResourceReference(TextBlock.ForegroundProperty, "Brasa.Bad");
                status.Text = "JSON 无效，未应用";
            }
        });
        var body = new StackPanel();
        body.Children.Add(LabelFor(n, depth));
        body.Children.Add(box);
        body.Children.Add(Ui.HBox(8, apply, status));
        return body;
    }

    // ---------------------------------------------------------------- section shell

    private FrameworkElement ChildrenStack(FieldNode n, int depth)
    {
        var p = new StackPanel();
        foreach (var c in n.Children) p.Children.Add(Render(c, depth + 1));
        return p;
    }

    private FrameworkElement Section(FieldNode n, int depth, Func<FrameworkElement> build, FrameworkElement? headerExtra = null)
    {
        var body = new StackPanel { Margin = Ui.T(12, 4, 12, 10) };
        var chevron = Ui.Text("▸", 11, "Brasa.Accent");
        chevron.Width = 13;
        chevron.VerticalAlignment = VerticalAlignment.Center;

        var summary = Ui.Text(n.ValueSummary(), 11.5, "Brasa.TextDim");
        summary.VerticalAlignment = VerticalAlignment.Center;
        summary.Margin = Ui.T(8, 0, 0, 0);

        var label = LabelFor(n, depth);
        var header = new Grid { Background = Brushes.Transparent, Cursor = Cursors.Hand, Margin = Ui.T(12, 9, 12, 9) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        if (headerExtra is not null)
        {
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerExtra.Margin = Ui.T(8, 0, 0, 0);
            Grid.SetColumn(headerExtra, 3);
            header.Children.Add(headerExtra);
        }
        Grid.SetColumn(chevron, 0);
        Grid.SetColumn(label, 1);
        Grid.SetColumn(summary, 2);
        header.Children.Add(chevron);
        header.Children.Add(label);
        header.Children.Add(summary);

        var root = new StackPanel();
        root.Children.Add(header);
        root.Children.Add(body);

        var border = new Border
        {
            BorderBrush = Ui.Res("Brasa.Border"),
            BorderThickness = Ui.T(1, 1, 1, 1),
            CornerRadius = new CornerRadius(4),
            Margin = Ui.T(0, 3, 0, 3),
            Background = depth == 0 ? Ui.Res("Brasa.Surface") : Ui.Res("Brasa.SurfaceAlt"),
            Child = root
        };

        var open = n.Expanded;
        body.Visibility = open ? Visibility.Visible : Visibility.Collapsed;
        chevron.Text = open ? "▾" : "▸";

        var built = false;
        void Expand()
        {
            if (built) return;
            body.Children.Add(build());
            built = true;
        }

        void Toggle()
        {
            n.Expanded = !n.Expanded;
            if (n.Expanded)
            {
                Expand();
                body.Visibility = Visibility.Visible;
                chevron.Text = "▾";
            }
            else
            {
                body.Visibility = Visibility.Collapsed;
                chevron.Text = "▸";
            }
        }

        if (open) Expand();
        // Only the header grid handles the click: it spans the whole row, and adding handlers to its
        // children would toggle twice per click because the event bubbles through both.
        header.MouseLeftButtonUp += (_, _) => Toggle();
        return border;
    }

    // ---------------------------------------------------------------- small parts

    private Button SmallDelete(Action action)
    {
        var b = Ui.Btn("删除", (_, _) => action());
        b.Padding = Ui.T(8, 2, 8, 2);
        b.FontSize = 11.5;
        return b;
    }

    private static Grid TwoColumn(FrameworkElement left, FrameworkElement right)
    {
        var g = new Grid { Margin = Ui.T(0, 1, 0, 1) };
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var pad = new Border { Child = left, Padding = Ui.T(0, 0, 10, 0) };
        Grid.SetColumn(pad, 0);
        Grid.SetColumn(right, 1);
        g.Children.Add(pad);
        g.Children.Add(right);
        return g;
    }

    private FrameworkElement AddItemRow(Action<string> add, string button, bool allowFreeText)
    {
        var box = new TextBox
        {
            Width = allowFreeText ? 320 : 220,
            Height = 24,
            HorizontalAlignment = HorizontalAlignment.Left,
            FontFamily = (FontFamily)Application.Current.FindResource("Brasa.Mono"),
            FontSize = 11.5
        };
        var tip = Ui.Text(allowFreeText ? "输入值后回车" : "粘贴 24 位物品编号，或用右侧搜索", 11, "Brasa.TextDim");
        tip.Margin = Ui.T(8, 0, 0, 0);
        tip.VerticalAlignment = VerticalAlignment.Center;

        var btn = Ui.Btn(button, (_, _) =>
        {
            var v = box.Text.Trim();
            if (v.Length == 0) return;
            add(v);
            box.Text = "";
        });
        btn.Margin = Ui.T(0, 6, 0, 0);
        box.KeyDown += (_, e) => { if (e.Key == Key.Enter) btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); };

        var panel = new StackPanel { Margin = Ui.T(0, 6, 0, 0) };
        panel.Children.Add(Ui.HBox(0, box, tip));
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Margin = Ui.T(0, 4, 0, 0) };
        actions.Children.Add(btn);
        if (!allowFreeText)
        {
            var pick = Ui.Btn("搜索物品…", (_, _) =>
            {
                var id = ItemPicker.Pick(Window.GetWindow(box), _ws, "选择物品");
                if (id is not null) add(id);
            });
            pick.Margin = Ui.T(6, 0, 0, 0);
            actions.Children.Add(pick);
        }
        panel.Children.Add(actions);
        return panel;
    }

    private void Touch(FieldNode n)
    {
        n.Invalidate();
        Touched?.Invoke();
        _ws.MarkChanged();
    }

    private static string Shorten(string s) => s.Length <= 28 ? s : s[..25] + "…";
}
