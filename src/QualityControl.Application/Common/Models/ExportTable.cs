namespace QualityControl.Application.Common.Models;

/// <summary>
/// How a value should be rendered by an exporter.
/// </summary>
public enum ExportColumnFormat
{
    Text = 0,
    Integer = 1,
    Decimal = 2,
    Percent = 3,
    Date = 4,
    DateTime = 5
}

/// <summary>
/// One column of an exported report.
/// </summary>
/// <typeparam name="T">Row type.</typeparam>
/// <param name="Header">Column caption.</param>
/// <param name="Value">Extracts the cell value from a row.</param>
/// <param name="Format">Controls number/date formatting in the output.</param>
/// <param name="RelativeWidth">Column width relative to its siblings.</param>
public sealed record ExportColumn<T>(
    string Header,
    Func<T, object?> Value,
    ExportColumnFormat Format = ExportColumnFormat.Text,
    float RelativeWidth = 1f);

/// <summary>
/// A report rendered as a table. Describing reports this way means Excel and PDF
/// share one definition per report instead of each exporter re-implementing it.
/// </summary>
/// <typeparam name="T">Row type.</typeparam>
public sealed class ExportTable<T>
{
    public required string Title { get; init; }

    public string? Subtitle { get; init; }

    public required IReadOnlyList<ExportColumn<T>> Columns { get; init; }

    public required IReadOnlyList<T> Rows { get; init; }

    /// <summary>Shown in the footer, e.g. "Сформировано: 17.07.2026 14:20, Иванов И.И.".</summary>
    public string? GeneratedBy { get; init; }

    public DateTime GeneratedAt { get; init; } = DateTime.UtcNow;
}
