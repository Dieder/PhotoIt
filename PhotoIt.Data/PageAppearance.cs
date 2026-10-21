namespace PhotoIt.Data;

public sealed class PageAppearance
{
    public static IReadOnlyList<string> AvailableFonts { get; } =
    [
        "Roboto", "Open Sans", "Lato", "Montserrat", "Oswald", "Source Sans 3", "Raleway", "Nunito",
        "Merriweather", "Playfair Display", "Poppins", "Ubuntu", "PT Sans", "Noto Sans", "Roboto Slab"
    ];

    public string Title { get; set; } = string.Empty;
    public string FontFamily { get; set; } = "Roboto";
    public string TextColor { get; set; } = "#000000";
    public int FontSize { get; set; } = 32;
    public PageTitleAlignment TitleAlignment { get; set; } = PageTitleAlignment.Center;
    public PageBackgroundStyle BackgroundStyle { get; set; } = PageBackgroundStyle.Solid;
    public string BackgroundColor { get; set; } = "#FFFFFF";
    public string GradientEndColor { get; set; } = "#FFFFFF";
}

public enum PageTitleAlignment
{
    Left,
    Center,
    Right
}

public enum PageBackgroundStyle
{
    Solid,
    Gradient
}
