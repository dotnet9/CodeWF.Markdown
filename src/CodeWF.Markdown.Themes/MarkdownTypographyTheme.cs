namespace CodeWF.Markdown.Themes;

/// <summary>
/// 描述 CodeWF.Markdown.Themes 内置的一套排版资源。
/// </summary>
public sealed record MarkdownTypographyTheme(string Name, string Key);

/// <summary>
/// Markdown 排版主题 Key 集合，运行时切换主题时直接使用这些常量。
/// </summary>
public static class MarkdownTypographyThemes
{
    public const string Basic = "Basic";
    public const string OrangeHeart = "OrangeHeart";
    public const string InkBlack = "InkBlack";
    public const string ColorfulPurple = "ColorfulPurple";
    public const string TenderGreen = "TenderGreen";
    public const string Verdant = "Verdant";
    public const string RedScarlet = "RedScarlet";
    public const string BlueGlow = "BlueGlow";
    public const string TechnologyBlue = "TechnologyBlue";
    public const string LanQing = "LanQing";
    public const string Yamabuki = "Yamabuki";
    public const string FrontendPeak = "FrontendPeak";
    public const string GeekBlack = "GeekBlack";
    public const string Simple = "Simple";
    public const string RosePurple = "RosePurple";
    public const string CuteGreen = "CuteGreen";
    public const string FullStackBlue = "FullStackBlue";

    public static IReadOnlyList<MarkdownTypographyTheme> All { get; } =
    [
        new("简", Simple),
        new("橙心", OrangeHeart),
        new("墨黑", InkBlack),
        new("科技蓝", TechnologyBlue),
        new("全栈蓝", FullStackBlue),
        new("兰青", LanQing),
        new("姹紫", ColorfulPurple),
        new("嫩青", TenderGreen),
        new("山吹", Yamabuki),
        new("极客黑", GeekBlack),
        new("红绯", RedScarlet),
        new("绿意", Verdant),
        new("萌绿", CuteGreen),
        new("蓝莹", BlueGlow),
        new("蔷薇紫", RosePurple),
    ];
}

/// <summary>
/// Markdown 排版尺寸 Key 集合，可与任意排版主题叠加使用。
/// </summary>
public static class MarkdownTypographySizes
{
    public const string Normal = "Normal";
    public const string Small = "Small";
}
