namespace CodeWF.Markdown.Sample;

public class SampleLanguage
{
    public required string CultureName { get; set; }
    public required string Language { get; set; }
    public required string Description { get; set; }
    public string DisplayName => string.IsNullOrWhiteSpace(Language) ? CultureName : Language;
    public string DisplayTag => CultureName;
    public string DetailText => string.IsNullOrWhiteSpace(Description) ? DisplayName : Description;
}