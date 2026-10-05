using System.Globalization;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using SptConfigStudio.Core;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace SptConfigStudio.Theme;

public enum Material { Mica, Acrylic, Opaque }

public enum Scheme { Dark, Light, System }

/// <summary>
/// Runtime theming. WPF-UI pins its own accent to the Windows system colour through StaticResource chains we
/// cannot reach, so everything the app colours goes through the brush keys this class rewrites: replacing the
/// brush OBJECT in Application.Resources is what makes the DynamicResource and SetResourceReference sites follow.
/// Mutating the C.Accent colour keys would be inert, because the brushes were built from them with StaticResource.
/// Light mode is the same trick one level up: Theme/Light.xaml is appended after Theme/Dark.xaml, and WPF resolves
/// merged dictionaries in reverse index order, so its palette wins while Dark.xaml's styles keep applying.
/// </summary>
public static class Appearance
{
    public static readonly (string Label, string Hex)[] Presets =
    [
        ("塔科夫琥珀", "E08A2E"),
        ("石墨蓝", "3A76B8"),
        ("苔绿", "6E9B5A"),
        ("砖红", "B8524A"),
        ("藕紫", "8A6BC1"),
        ("铜金", "B8933F")
    ];

    public const string DefaultHex = "E08A2E";

    public static Color? AppliedAccent { get; private set; }

    public static Material ParseMaterial(string? value) =>
        Enum.TryParse<Material>(value, true, out var m) ? m : Material.Mica;

    public static Scheme ParseScheme(string? value) =>
        Enum.TryParse<Scheme>(value, true, out var s) ? s : Scheme.Dark;

    public static Scheme CurrentScheme { get; private set; } = Scheme.Dark;

    /// <summary>Which palette is live right now. Drives the accent clamping in <see cref="ApplyAccent"/>.</summary>
    public static bool IsLight { get; private set; }

    private static ResourceDictionary? _lightDict;

    /// <summary>AppsUseLightTheme rather than SystemThemeWatcher.Watch: the watcher re-applies the system theme on
    /// every WM_SETTINGCHANGE, which would override a manual choice and can land on HighContrast.</summary>
    public static bool SystemPrefersLight()
    {
        try
        {
            const string path = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var key = Registry.CurrentUser.OpenSubKey(path);
            if (key?.GetValue("AppsUseLightTheme") is int v) return v != 0;
        }
        catch { /* an unreadable preference is not worth an exception dialog */ }

        return false;
    }

    // ------------------------------------------------------------------ accent

    /// <summary>Parses a hex triple with or without a leading '#'. Prefs are hand-editable, so bad input falls back.</summary>
    public static Color FromHex(string? hex)
    {
        if (TryHex(hex, out var color)) return color;
        TryHex(DefaultHex, out color);
        return color;
    }

    /// <summary>Strict parse for fields the user is still typing into; false means "leave what is on screen alone".</summary>
    public static bool TryHex(string? hex, out Color color)
    {
        color = Colors.Transparent;
        var s = (hex ?? string.Empty).Trim();
        if (s.StartsWith('#')) s = s[1..];
        if (s.Length != 6) return false;

        if (!byte.TryParse(s.AsSpan(0, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var r) ||
            !byte.TryParse(s.AsSpan(2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var g) ||
            !byte.TryParse(s.AsSpan(4, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var b))
            return false;

        color = Color.FromRgb(r, g, b);
        return true;
    }

    public static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

    /// <summary>Registry first, glass colour last: the DWM values are what Settings shows as the accent swatch.</summary>
    public static Color? ResolveSystemAccent()
    {
        foreach (var (path, name) in new[]
        {
            (@"Software\Microsoft\Windows\CurrentVersion\Explorer\Accent", "AccentColorMenu"),
            (@"Software\Microsoft\Windows\DWM", "AccentColor")
        })
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(path);
                if (key?.GetValue(name) is int raw)
                {
                    // Stored as 0xAABBGGRR, so the channels come out reversed.
                    var v = unchecked((uint)raw);
                    return Color.FromRgb((byte)(v & 0xFF), (byte)((v >> 8) & 0xFF), (byte)((v >> 16) & 0xFF));
                }
            }
            catch { /* an unreadable accent is not worth an exception dialog */ }
        }

        return SystemParameters.WindowGlassColor is { } glass && (glass.R > 0 || glass.G > 0 || glass.B > 0)
            ? Color.FromRgb(glass.R, glass.G, glass.B)
            : null;
    }

    public static Color CurrentAccent() => AppEnv.AccentKind switch
    {
        "system" => ResolveSystemAccent() ?? FromHex(DefaultHex),
        _ => FromHex(AppEnv.AccentHex)
    };

    public static void ApplyAccent(Color accent)
    {
        AppliedAccent = accent;
        // Light mode has to clamp the accent down before it can carry text or fill a pill; the dark board keeps the
        // picked colour byte-for-byte. Contrast is measured against C.Bg, the darker of the two surfaces, so passing
        // it also clears C.Surface where the accent is used as body text.
        var baseAccent = IsLight ? DarkenToContrast(accent, PaletteBackground(), 4.6) : accent;
        var hi = IsLight ? Scale(baseAccent, 0.74) : ShiftLightness(baseAccent, +0.11);
        var lo = IsLight ? Scale(baseAccent, 0.88) : ShiftLightness(baseAccent, -0.11);
        // Perceived luminance decides whether ink on the accent has to go dark or light — measured on the fill that
        // actually ships, otherwise a clamped accent would keep the ink picked for the bright original. The amber
        // lands at 152 and keeps the hand-picked near-black, while a system blue lands at 90 and needs a light ink.
        var on = (byte)(0.299 * baseAccent.R + 0.587 * baseAccent.G + 0.114 * baseAccent.B) >= 140
            ? Color.FromRgb(0x14, 0x14, 0x12)
            : Color.FromRgb(0xF2, 0xEF, 0xE6);

        var res = Application.Current.Resources;
        Put(res, "Brasa.Accent", baseAccent);
        Put(res, "Brasa.AccentHi", hi);
        Put(res, "Brasa.AccentLo", lo);
        Put(res, "Brasa.OnAccent", on);
        res["Brasa.AccentGhost"] = Frozen(new SolidColorBrush(baseAccent) { Opacity = IsLight ? 0.28 : 0.16 });

        // Keys WPF-UI resolves with DynamicResource, retargeted in Theme/Dark.xaml: these are the focused field
        // border, the drop-down pill and the picker selection.
        Put(res, "TextControlFocusedBorderBrush", hi);
        Put(res, "ComboBoxBorderBrushFocused", hi);
        Put(res, "ComboBoxItemPillFillBrush", baseAccent);
        Put(res, "ListBoxItemSelectedBackgroundThemeBrush", baseAccent);
        // The nav selection bar is deliberately invisible: NavPane draws its own so it can slide between rows,
        // and that one is filled from Brasa.Accent above, so accent changes still land. Leaving this on the accent
        // would paint a second bar that pops in and out with no animation at all.
        Put(res, "NavigationViewSelectionIndicatorForeground", Brushes.Transparent);

        // Best effort: WPF-UI derives most of these with StaticResource, so a few library widgets may stay on the
        // system accent. Writing them costs nothing and catches the ones resolved dynamically.
        Put(res, "AccentFillColorDefaultBrush", baseAccent);
        res["AccentFillColorSecondaryBrush"] = Frozen(new SolidColorBrush(baseAccent) { Opacity = 0.9 });
        res["AccentFillColorTertiaryBrush"] = Frozen(new SolidColorBrush(baseAccent) { Opacity = 0.8 });
        Put(res, "AccentFillColorSelectedTextBackgroundBrush", baseAccent);
        Put(res, "AccentTextFillColorPrimaryBrush", hi);
        Put(res, "AccentTextFillColorSecondaryBrush", baseAccent);
        Put(res, "AccentTextFillColorTertiaryBrush", lo);
        Put(res, "TextOnAccentFillColorPrimaryBrush", on);
        Put(res, "SystemFillColorAttentionBrush", baseAccent);
    }

    /// <summary>The live palette's window colour, read through the resource chain so it follows the merged dictionaries.</summary>
    private static Color PaletteBackground() => (Color)Application.Current.FindResource("C.Bg");

    /// <summary>
    /// Multiplicative darkening, not an HSL shift: <see cref="ShiftLightness"/> returns achromatic input unchanged, so
    /// a hand-picked grey or white accent could never be pushed down. Stops at the first ratio that clears the
    /// threshold, and at a floor well above black so a hopeless input degrades instead of vanishing.
    /// </summary>
    private static Color DarkenToContrast(Color c, Color bg, double minRatio)
    {
        var k = 1.0;
        var cur = c;
        for (var i = 0; i < 14 && Contrast(cur, bg) < minRatio; i++)
        {
            k = Math.Max(0.30, k * 0.94);
            cur = Scale(c, k);
        }

        return cur;
    }

    /// <summary>Scales every channel by the same factor: hue-preserving for chromatic colours and still monotonic for grey.</summary>
    private static Color Scale(Color c, double k) =>
        Color.FromRgb(Clamp255(c.R * k), Clamp255(c.G * k), Clamp255(c.B * k));

    private static byte Clamp255(double v) => (byte)Math.Round(Math.Clamp(v, 0d, 255d));

    private static double Contrast(Color a, Color b)
    {
        var la = RelativeLuminance(a);
        var lb = RelativeLuminance(b);
        return (Math.Max(la, lb) + 0.05) / (Math.Min(la, lb) + 0.05);
    }

    private static double RelativeLuminance(Color c) =>
        0.2126 * LinearChannel(c.R) + 0.7152 * LinearChannel(c.G) + 0.0722 * LinearChannel(c.B);

    private static double LinearChannel(int v)
    {
        var s = v / 255.0;
        return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }

    private static void Put(ResourceDictionary res, string key, Color color) => Put(res, key, Frozen(new SolidColorBrush(color)));

    private static void Put(ResourceDictionary res, string key, Brush brush) => res[key] = brush;

    private static Brush Frozen(SolidColorBrush b)
    {
        b.Freeze();
        return b;
    }

    /// <summary>Lightness shift in HSL, which is what keeps the hue of a hand-picked accent intact across ±11 points.</summary>
    private static Color ShiftLightness(Color c, double delta)
    {
        var r = c.R / 255.0;
        var g = c.G / 255.0;
        var b = c.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var l = (max + min) / 2;
        var d = max - min;
        var s = d <= double.Epsilon ? 0 : l > 0.5 ? d / (2 - max - min) : d / (max + min);
        var h = d switch
        {
            <= double.Epsilon => 0,
            _ when max == r => ((g - b) / d + (g < b ? 6 : 0)) / 6,
            _ when max == g => ((b - r) / d + 2) / 6,
            _ => ((r - g) / d + 4) / 6
        };

        l = Math.Clamp(l + delta, 0.05, 0.95);
        if (l <= 0 || l >= 1 || s <= 0) return Color.FromRgb(c.R, c.G, c.B);

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        return Color.FromRgb((byte)Math.Round(Hue(p, q, h + 1.0 / 3) * 255),
                              (byte)Math.Round(Hue(p, q, h) * 255),
                              (byte)Math.Round(Hue(p, q, h - 1.0 / 3) * 255));
    }

    private static double Hue(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6) return p + (q - p) * 6 * t;
        if (t < 1.0 / 2) return q;
        if (t < 2.0 / 3) return p + (q - p) * (2.0 / 3 - t) * 6;
        return p;
    }

    // ------------------------------------------------------------------ scheme

    /// <summary>
    /// The whole light/dark switch. Three steps, in this order, because each one reads what the previous wrote:
    /// the palette dictionary decides the neutral board, the library theme dictionary decides every stock control,
    /// and the accent engine then clamps the accent to whatever board is now live.
    /// </summary>
    public static void ApplyScheme(FluentWindow win, Scheme scheme)
    {
        CurrentScheme = scheme;
        IsLight = scheme == Scheme.Light || (scheme == Scheme.System && SystemPrefersLight());
        SetPalette(IsLight);

        var pane = ParseMaterial(AppEnv.PaneMaterial);
        var content = ParseMaterial(AppEnv.ContentMaterial);
        var effective = IsLight ? ApplicationTheme.Light : ApplicationTheme.Dark;

        // Only when the library really disagrees: Apply also re-runs the backdrop swap that ApplyMaterials owns,
        // so calling it unconditionally would flatten acrylic on every launch and would repaint the window brush.
        // updateAccent has to stay false — ApplicationAccentColorManager writes SystemAccentColor* at the root,
        // which would re-pin the accent to the Windows system colour and undo this app's own accent engine.
        if (ApplicationThemeManager.GetAppTheme() != effective)
            ApplicationThemeManager.Apply(effective, ComposeBackdrop(pane, content), updateAccent: false);

        ApplyAccent(CurrentAccent());
        ApplyMaterials(win, pane, content);
    }

    /// <summary>Appends the light palette after Dark.xaml, or removes it again. Kept as one instance so the swap is idempotent.</summary>
    private static void SetPalette(bool light)
    {
        var merged = Application.Current.Resources.MergedDictionaries;
        if (!light)
        {
            if (_lightDict is not null) _ = merged.Remove(_lightDict);
            return;
        }

        _lightDict ??= new ResourceDictionary { Source = new Uri("pack://application:,,,/Theme/Light.xaml") };
        if (!merged.Contains(_lightDict)) merged.Add(_lightDict);
    }

    public static void SaveScheme(Scheme scheme) => AppEnv.SetThemeKind(scheme.ToString());

    // ------------------------------------------------------------------ material

    /// <summary>
    /// DWM allows exactly one backdrop per window, so the window takes the strongest request (acrylic beats mica,
    /// because a mica region is only a heavier tint and still reads as mica over a blurred desktop).
    /// </summary>
    internal static WindowBackdropType ComposeBackdrop(Material pane, Material content)
    {
        var backdrop = pane == Material.Opaque && content == Material.Opaque
            ? WindowBackdropType.None
            : pane == Material.Acrylic || content == Material.Acrylic
                ? WindowBackdropType.Acrylic
                : WindowBackdropType.Mica;

        return backdrop != WindowBackdropType.None && !WindowBackdrop.IsSupported(backdrop)
            ? WindowBackdropType.None
            : backdrop;
    }

    /// <summary>Sets the per-region tints and the window's own backdrop. The tint strengths are in the palette files.</summary>
    public static void ApplyMaterials(FluentWindow win, Material pane, Material content)
    {
        var res = Application.Current.Resources;
        var paneBrush = (Brush)Application.Current.FindResource($"Brasa.Pane.{pane}");
        var contentBrush = (Brush)Application.Current.FindResource($"Brasa.Content.{content}");
        res["Brasa.PaneBackground"] = paneBrush;
        res["Brasa.ContentBackground"] = contentBrush;
        // The NavigationView's own content column normally sits at zero width beside the pane. WPF-UI restarts its
        // pane storyboard from hard-coded From values when a toggle is interrupted, so mid-flight that column can be
        // 20-odd pixels wide and lands *inside* the sidebar: left on the content brush it shows up as a strip of
        // editor colour, and its 1px stroke adds a hairline. Both go transparent and the gap reads as sidebar.
        // Nothing here dims the main area — that is painted by the editor cell's own Brasa.ContentBackground border.
        res["NavigationViewContentBackground"] = Brushes.Transparent;
        res["NavigationViewContentGridBorderBrush"] = Brushes.Transparent;

        var backdrop = ComposeBackdrop(pane, content);
        win.WindowBackdropType = backdrop;
        // The library removes its backdrop when switching to None but leaves the window brush alone, and ours is
        // Transparent by design so mica can show through — an opaque fallback has to come from us.
        if (backdrop == WindowBackdropType.None) AssertWindowBackground(win);
        else win.Background = Brushes.Transparent;
    }

    /// <summary>
    /// FluentWindow.OnSourceInitialized calls WindowBackdrop.RemoveBackdrop for every window that ends up with no
    /// backdrop, and that writes the library's neutral colour straight onto Background. It is a local assignment, so a
    /// resource reference cannot outlive it — ours has to land after the base class is finished.
    /// </summary>
    internal static void AssertWindowBackground(Window win)
    {
        void Reassert() => win.Background = (Brush)Application.Current.FindResource("Brasa.Background");

        Reassert();
        if (new WindowInteropHelper(win).Handle == IntPtr.Zero)
            win.SourceInitialized += (_, _) => Reassert();
    }

    public static void ApplyFromPrefs(FluentWindow win) => ApplyScheme(win, ParseScheme(AppEnv.ThemeKind));

    public static void SaveAccent(string kind, string hex)
    {
        AppEnv.SetAccentKind(kind);
        AppEnv.SetAccentHex(hex);
    }

    public static void SaveMaterials(Material pane, Material content)
    {
        AppEnv.SetPaneMaterial(pane.ToString());
        AppEnv.SetContentMaterial(content.ToString());
    }
}
