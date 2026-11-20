using Microsoft.Extensions.Options;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;

namespace QualityControl.Infrastructure.Services.Export;

/// <summary>
/// Renders a report table to PDF with QuestPDF.
/// </summary>
public class PdfExportService : IPdfExportService
{
    private readonly PdfExportOptions _options;

    public PdfExportService(IOptions<PdfExportOptions> options)
    {
        _options = options.Value;
    }

    public byte[] Export<T>(ExportTable<T> table)
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                // Landscape: quality reports are wide, and portrait would squeeze
                // the columns to unreadable.
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontFamily(_options.FontFamily).FontSize(9));

                page.Header().Element(c => ComposeHeader(c, table));
                page.Content().Element(c => ComposeTable(c, table));
                page.Footer().Element(c => ComposeFooter(c, table));
            });
        });

        return document.GeneratePdf();
    }

    private void ComposeHeader<T>(IContainer container, ExportTable<T> table)
    {
        container.PaddingBottom(8).Column(column =>
        {
            column.Item().Text(_options.OrganisationName).FontSize(11).SemiBold();
            column.Item().Text(_options.SystemName).FontSize(8).FontColor(Colors.Grey.Darken1);

            column.Item().PaddingTop(6).Text(table.Title).FontSize(14).Bold();

            if (!string.IsNullOrWhiteSpace(table.Subtitle))
            {
                column.Item().Text(table.Subtitle).FontSize(9).FontColor(Colors.Grey.Darken2);
            }

            column.Item().PaddingTop(6).LineHorizontal(1).LineColor(Colors.Grey.Medium);
        });
    }

    private static void ComposeTable<T>(IContainer container, ExportTable<T> table)
    {
        if (table.Rows.Count == 0)
        {
            container.PaddingTop(20).AlignCenter()
                .Text("Нет данных за выбранный период.")
                .FontSize(11).FontColor(Colors.Grey.Darken1);

            return;
        }

        container.Table(t =>
        {
            t.ColumnsDefinition(columns =>
            {
                foreach (var column in table.Columns)
                {
                    columns.RelativeColumn(column.RelativeWidth);
                }
            });

            // Repeats on every page — a multi-page report is unreadable otherwise.
            t.Header(header =>
            {
                foreach (var column in table.Columns)
                {
                    header.Cell()
                        .Background(Colors.Grey.Lighten3)
                        .Padding(4)
                        .Text(column.Header)
                        .SemiBold()
                        .FontSize(9);
                }

                header.Cell().ColumnSpan((uint)table.Columns.Count)
                    .PaddingTop(1)
                    .LineHorizontal(1)
                    .LineColor(Colors.Grey.Medium);
            });

            var index = 0;

            foreach (var item in table.Rows)
            {
                // Banded rows: at 9pt across a landscape page, the eye loses the row
                // without them.
                var background = index++ % 2 == 0 ? Colors.White : Colors.Grey.Lighten5;

                foreach (var column in table.Columns)
                {
                    t.Cell()
                        .Background(background)
                        .BorderBottom(0.5f)
                        .BorderColor(Colors.Grey.Lighten2)
                        .Padding(4)
                        .Text(Format(column.Value(item), column.Format))
                        .FontSize(8);
                }
            }
        });
    }

    private static void ComposeFooter<T>(IContainer container, ExportTable<T> table)
    {
        container.PaddingTop(6).Column(column =>
        {
            column.Item().LineHorizontal(0.5f).LineColor(Colors.Grey.Medium);

            column.Item().PaddingTop(4).Row(row =>
            {
                var generated = $"Сформировано: {table.GeneratedAt:dd.MM.yyyy HH:mm} UTC";

                if (!string.IsNullOrWhiteSpace(table.GeneratedBy))
                {
                    generated += $" · {table.GeneratedBy}";
                }

                row.RelativeItem().Text(generated).FontSize(7).FontColor(Colors.Grey.Darken1);

                row.RelativeItem().AlignRight().Text(text =>
                {
                    text.DefaultTextStyle(x => x.FontSize(7).FontColor(Colors.Grey.Darken1));
                    text.Span("Стр. ");
                    text.CurrentPageNumber();
                    text.Span(" из ");
                    text.TotalPages();
                });
            });
        });
    }

    private static string Format(object? value, ExportColumnFormat format) => value switch
    {
        null => string.Empty,
        _ => format switch
        {
            ExportColumnFormat.Integer => Convert.ToDecimal(value).ToString("#,##0"),
            ExportColumnFormat.Decimal => Convert.ToDecimal(value).ToString("#,##0.00"),
            ExportColumnFormat.Percent => $"{Convert.ToDecimal(value):#,##0.00} %",
            ExportColumnFormat.Date => Convert.ToDateTime(value).ToString("dd.MM.yyyy"),
            ExportColumnFormat.DateTime => Convert.ToDateTime(value).ToString("dd.MM.yyyy HH:mm"),
            _ => value.ToString() ?? string.Empty
        }
    };
}
