using System.Globalization;
using System.Text.RegularExpressions;

namespace RgaaStudio.Core.Audits;

public sealed record ColorContrastMeasurement(double Ratio, string ForegroundHex, string BackgroundHex);

/// <summary>Evaluates WCAG contrast from computed CSS foreground and background color layers.</summary>
public static partial class ColorContrastAnalyzer
{
    public static ColorContrastMeasurement? Measure(string foreground, IEnumerable<string> backgroundLayers)
    {
        if (!TryParse(foreground, out var foregroundColor))
            return null;

        var background = new RgbColor(255, 255, 255, 1);
        foreach (var layer in backgroundLayers)
            if (TryParse(layer, out var color))
                background = Composite(color, background);

        var visibleForeground = Composite(foregroundColor, background);
        var firstLuminance = Luminance(visibleForeground);
        var secondLuminance = Luminance(background);
        var lighter = Math.Max(firstLuminance, secondLuminance);
        var darker = Math.Min(firstLuminance, secondLuminance);
        return new ColorContrastMeasurement((lighter + 0.05) / (darker + 0.05),
            ToHex(visibleForeground), ToHex(background));
    }

    public static double RequiredRatio(bool largeText, bool aaa) => (largeText, aaa) switch
    {
        (true, true) => 4.5,
        (true, false) => 3.0,
        (false, true) => 7.0,
        _ => 4.5
    };

    private static bool TryParse(string? cssColor, out RgbColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(cssColor)) return false;
        var value = cssColor.Trim();
        if (value.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            color = new RgbColor(0, 0, 0, 0);
            return true;
        }
        if (value.StartsWith('#'))
        {
            var hex = value[1..];
            if (hex.Length is 3 or 4)
            {
                var hexComponents = hex.Select(character => Convert.ToInt32(new string(character, 2), 16)).ToArray();
                color = new RgbColor(hexComponents[0], hexComponents[1], hexComponents[2], hexComponents.Length == 4 ? hexComponents[3] / 255d : 1);
                return true;
            }
            if (hex.Length is 6 or 8 && uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var packed))
            {
                color = hex.Length == 8
                    ? new RgbColor((packed >> 24) & 255, (packed >> 16) & 255, (packed >> 8) & 255, (packed & 255) / 255d)
                    : new RgbColor((packed >> 16) & 255, (packed >> 8) & 255, packed & 255, 1);
                return true;
            }
            return false;
        }

        if (!value.StartsWith("rgb", StringComparison.OrdinalIgnoreCase)) return false;
        var rgbComponents = CssNumberRegex().Matches(value).Select(match => match.Value).ToArray();
        if (rgbComponents.Length is < 3 or > 4) return false;
        if (!TryChannel(rgbComponents[0], out var red) || !TryChannel(rgbComponents[1], out var green) || !TryChannel(rgbComponents[2], out var blue))
            return false;
        var alpha = rgbComponents.Length == 4 && TryAlpha(rgbComponents[3], out var parsedAlpha) ? parsedAlpha : 1;
        color = new RgbColor(red, green, blue, alpha);
        return true;
    }

    private static bool TryChannel(string value, out double channel)
    {
        var percent = value.EndsWith('%');
        var parsed = double.TryParse(percent ? value[..^1] : value, NumberStyles.Float, CultureInfo.InvariantCulture, out channel);
        if (!parsed || (!percent && (channel < 0 || channel > 255))) return false;
        if (percent) channel = Math.Clamp(channel, 0, 100) * 2.55;
        return true;
    }

    private static bool TryAlpha(string value, out double alpha)
    {
        var percent = value.EndsWith('%');
        var parsed = double.TryParse(percent ? value[..^1] : value, NumberStyles.Float, CultureInfo.InvariantCulture, out alpha);
        if (!parsed) return false;
        alpha = Math.Clamp(percent ? alpha / 100 : alpha, 0, 1);
        return true;
    }

    private static RgbColor Composite(RgbColor foreground, RgbColor background)
    {
        var alpha = Math.Clamp(foreground.Alpha, 0, 1);
        return new RgbColor(
            foreground.Red * alpha + background.Red * (1 - alpha),
            foreground.Green * alpha + background.Green * (1 - alpha),
            foreground.Blue * alpha + background.Blue * (1 - alpha),
            1);
    }

    private static double Luminance(RgbColor color)
    {
        static double Linear(double component)
        {
            var normalized = component / 255;
            return normalized <= 0.04045 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
        }
        return 0.2126 * Linear(color.Red) + 0.7152 * Linear(color.Green) + 0.0722 * Linear(color.Blue);
    }

    private static string ToHex(RgbColor color) => $"#{(int)Math.Round(color.Red):X2}{(int)Math.Round(color.Green):X2}{(int)Math.Round(color.Blue):X2}";

    [GeneratedRegex(@"[0-9]*\.?[0-9]+%?", RegexOptions.CultureInvariant)]
    private static partial Regex CssNumberRegex();

    private readonly record struct RgbColor(double Red, double Green, double Blue, double Alpha);
}
