namespace RgaaStudio.Core.Navigation;

public readonly record struct TabStripLayout(double TabWidth, bool NavigationRequired);

public static class TabStripLayoutCalculator
{
    public const double PreferredTabWidth = 230;
    public const double MinimumTabWidth = 96;
    public const double TabGap = 8;

    public static TabStripLayout Calculate(int tabCount, double availableWidth)
    {
        if (tabCount <= 0 || availableWidth <= 0)
            return new TabStripLayout(PreferredTabWidth, false);

        if (tabCount * (PreferredTabWidth + TabGap) <= availableWidth)
            return new TabStripLayout(PreferredTabWidth, false);

        var navigationRequired = tabCount * (MinimumTabWidth + TabGap) > availableWidth;
        var tabWidth = navigationRequired
            ? MinimumTabWidth
            : Math.Floor(availableWidth / tabCount - TabGap);

        return new TabStripLayout(tabWidth, navigationRequired);
    }
}
