namespace CodeWF.Markdown.Sample;

public class SampleLanguage
{
    public required string CultureName { get; set; }
    public required string Language { get; set; }
    public required string Description { get; set; }
    public string DisplayName => CultureName switch
    {
        "zh-CN" => "简体中文",
        "zh-Hant" => "繁體中文",
        "en-US" => "English",
        "ja-JP" => "日本語",
        _ => string.IsNullOrWhiteSpace(Language) ? CultureName : Language
    };
    public string DisplayTag => CultureName;
    public string DetailText => string.IsNullOrWhiteSpace(Description) ? DisplayName : Description;
}
