using ClosedXML.Excel;
using QualityControl.Application.Common.Interfaces;
using QualityControl.Application.Common.Models;

namespace QualityControl.Infrastructure.Services.Export;

/// <summary>
/// Renders a report table to XLSX with ClosedXML.
/// </summary>
public class ExcelExportService : IExcelExportService
{
    public byte[] Export<T>(ExportTable<T> table)
    {
        using var workbook = new XLWorkbook();

        // Excel rejects sheet names over 31 chars or containing : \ / ? * [ ]
        var sheet = workbook.Worksheets.Add(SanitiseSheetName(table.Title));

        var row = 1;

        sheet.Cell(row, 1).Value = table.Title;
        sheet.Cell(row, 1).Style.Font.Bold = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 14;
        sheet.Range(row, 1, row, Math.Max(table.Columns.Count, 1)).Merge();
        row++;

        if (!string.IsNullOrWhiteSpace(table.Subtitle))
        {
            sheet.Cell(row, 1).Value = table.Subtitle;
            sheet.Cell(row, 1).Style.Font.Italic = true;
            sheet.Range(row, 1, row, Math.Max(table.Columns.Count, 1)).Merge();
            row++;
        }

        row++;

        var headerRow = row;

        for (var i = 0; i < table.Columns.Count; i++)
        {
            var cell = sheet.Cell(headerRow, i + 1);
            cell.Value = table.Columns[i].Header;
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#E8EDF3");
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
            cell.Style.Alignment.WrapText = true;
        }

        row++;

        foreach (var item in table.Rows)
        {
            for (var i = 0; i < table.Columns.Count; i++)
            {
                WriteCell(sheet.Cell(row, i + 1), table.Columns[i].Value(item), table.Columns[i].Format);
            }

            row++;
        }

        if (table.Rows.Count > 0)
        {
            // A real Excel table: users get filter dropdowns and banded rows for free.
            var range = sheet.Range(headerRow, 1, row - 1, table.Columns.Count);
            range.CreateTable().Theme = XLTableTheme.TableStyleLight9;
        }

        // Freeze the header so it stays visible while scrolling a long report.
        sheet.SheetView.FreezeRows(headerRow);

        sheet.Columns().AdjustToContents(minWidth: 8, maxWidth: 60);

        row++;
        sheet.Cell(row, 1).Value = BuildFooter(table);
        sheet.Cell(row, 1).Style.Font.Italic = true;
        sheet.Cell(row, 1).Style.Font.FontSize = 9;

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    /// <summary>
    /// Writes a value with its native Excel type. Passing everything as text would
    /// look identical but make sorting and formulas useless in the exported file.
    /// </summary>
    private static void WriteCell(IXLCell cell, object? value, ExportColumnFormat format)
    {
        if (value is null)
        {
            cell.Value = string.Empty;
            return;
        }

        switch (format)
        {
            case ExportColumnFormat.Integer:
                cell.Value = Convert.ToDouble(value);
                cell.Style.NumberFormat.Format = "#,##0";
                break;

            case ExportColumnFormat.Decimal:
                cell.Value = Convert.ToDouble(value);
                cell.Style.NumberFormat.Format = "#,##0.00";
                break;

            case ExportColumnFormat.Percent:
                // Excel's percent format multiplies by 100, and the values here are
                // already percentages, so divide to keep "12.5" reading as 12.5%.
                cell.Value = Convert.ToDouble(value) / 100d;
                cell.Style.NumberFormat.Format = "0.00%";
                break;

            case ExportColumnFormat.Date:
                cell.Value = Convert.ToDateTime(value);
                cell.Style.DateFormat.Format = "dd.MM.yyyy";
                break;

            case ExportColumnFormat.DateTime:
                cell.Value = Convert.ToDateTime(value);
                cell.Style.DateFormat.Format = "dd.MM.yyyy HH:mm";
                break;

            default:
                cell.Value = value.ToString();
                cell.Style.Alignment.WrapText = true;
                break;
        }
    }

    private static string BuildFooter<T>(ExportTable<T> table)
    {
        var footer = $"Сформировано: {table.GeneratedAt:dd.MM.yyyy HH:mm} UTC";

        if (!string.IsNullOrWhiteSpace(table.GeneratedBy))
        {
            footer += $", пользователь: {table.GeneratedBy}";
        }

        return $"{footer}. Записей: {table.Rows.Count}.";
    }

    private static string SanitiseSheetName(string title)
    {
        var invalid = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var cleaned = new string(title.Where(c => !invalid.Contains(c)).ToArray()).Trim();

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Отчёт";
        }

        return cleaned.Length > 31 ? cleaned[..31] : cleaned;
    }
}
