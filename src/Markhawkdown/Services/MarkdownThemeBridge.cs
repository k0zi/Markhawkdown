using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using SukiUI;

namespace Markhawkdown.Services;

/// <summary>
/// Markdown.Avalonia colours its output from the Simple theme keys (<c>ThemeForegroundColor</c>, …), which SukiUI
/// does not define. This keeps those keys in sync with the active SukiUI base and colour theme.
/// </summary>
public static class MarkdownThemeBridge
{
    public static void Attach(Application app)
    {
        var suki = SukiTheme.GetInstance(app);

        // SukiUI updates its own resources in the same callbacks, so read them after it is done.
        void Schedule() => Dispatcher.UIThread.Post(() => Update(app), DispatcherPriority.Background);

        app.ActualThemeVariantChanged += (_, _) => Schedule();
        var previousColor = suki.OnColorThemeChanged;
        suki.OnColorThemeChanged = theme =>
        {
            previousColor?.Invoke(theme);
            Schedule();
        };

        Update(app);
    }

    private static void Update(Application app)
    {
        var variant = app.ActualThemeVariant;
        var isDark = variant == ThemeVariant.Dark;
        var accent = SukiTheme.GetInstance(app).ActiveColorTheme?.Primary ?? Colors.DodgerBlue;

        var foreground = FindColor(app, "SukiText", variant) ?? (isDark ? Color.Parse("#E6EDF3") : Color.Parse("#1F2328"));
        var border = FindColor(app, "SukiBorderBrush", variant) ?? (isDark ? Color.Parse("#3D444D") : Color.Parse("#D0D7DE"));

        // Links and headings accents need to stay readable on the dark, tinted SukiUI background.
        if (isDark)
            accent = Lighten(accent, 0.35);

        app.Resources["ThemeForegroundColor"] = foreground;
        app.Resources["ThemeForegroundBrush"] = new SolidColorBrush(foreground);
        app.Resources["ThemeAccentColor"] = accent;
        app.Resources["ThemeAccentBrush"] = new SolidColorBrush(accent);
        app.Resources["ThemeBorderHighColor"] = border;
    }

    private static Color? FindColor(Application app, string key, ThemeVariant variant) =>
        app.TryGetResource(key, variant, out var value)
            ? value switch
            {
                Color color => color,
                ISolidColorBrush brush => brush.Color,
                _ => null,
            }
            : null;

    private static Color Lighten(Color color, double amount) =>
        Color.FromArgb(
            color.A,
            (byte)(color.R + (255 - color.R) * amount),
            (byte)(color.G + (255 - color.G) * amount),
            (byte)(color.B + (255 - color.B) * amount));
}
