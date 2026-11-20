namespace QualityControl.Infrastructure.Services.Export;

/// <summary>
/// PDF rendering settings, bound from the "PdfExport" configuration section.
/// </summary>
public class PdfExportOptions
{
    public const string SectionName = "PdfExport";

    /// <summary>
    /// Font family used in generated PDFs. It MUST contain Cyrillic glyphs —
    /// QuestPDF's built-in Lato does not, and reports would render as blank boxes.
    ///
    /// "Segoe UI" ships with Windows. On a Linux container install a Cyrillic font
    /// (e.g. fonts-dejavu) and set this to "DejaVu Sans", or the same problem
    /// reappears at runtime with no build-time warning.
    /// </summary>
    public string FontFamily { get; set; } = "Segoe UI";

    public string OrganisationName { get; set; } = "ОАО «Амкодор-Пинск»";

    public string SystemName { get; set; } = "Автоматизированная система контроля качества продукции";
}
