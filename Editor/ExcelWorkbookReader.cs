using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace VM233.ExcelLocalization
{
    public static class ExcelWorkbookReader
    {
        private const int MAX_ROWS = 10000;
        private const int MAX_COLUMNS = 64;

        public static Dictionary<string, List<string[]>> Read(string path, string worksheet = null)
        {
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                using (var workbook = new XSSFWorkbook(file))
                {
                    var result = new Dictionary<string, List<string[]>>(StringComparer.Ordinal);
                    for (var sheetIndex = 0; sheetIndex < workbook.NumberOfSheets; sheetIndex++)
                    {
                        var sheet = workbook.GetSheetAt(sheetIndex);
                        if (worksheet != null && !string.Equals(sheet.SheetName, worksheet, StringComparison.Ordinal))
                        {
                            continue;
                        }

                        if (sheet.LastRowNum >= MAX_ROWS)
                        {
                            throw new FormatException($"{sheet.SheetName}: at most {MAX_ROWS} rows are supported.");
                        }

                        var rows = new List<string[]>();
                        for (var rowIndex = 0; rowIndex <= sheet.LastRowNum; rowIndex++)
                        {
                            var row = sheet.GetRow(rowIndex);
                            if (row == null)
                            {
                                rows.Add(Array.Empty<string>());
                                continue;
                            }

                            if (row.LastCellNum > MAX_COLUMNS)
                            {
                                throw new FormatException($"{sheet.SheetName}, row {rowIndex + 1}: " +
                                    $"at most {MAX_COLUMNS} columns are supported.");
                            }

                            var values = new string[Math.Max(0, (int)row.LastCellNum)];
                            foreach (var cell in row.Cells)
                            {
                                values[cell.ColumnIndex] = ReadCell(cell);
                            }

                            rows.Add(values);
                        }

                        result.Add(sheet.SheetName, rows);
                    }

                    return result;
                }
            }
        }

        private static string ReadCell(ICell cell)
        {
            switch (cell.CellType)
            {
                case CellType.Blank:
                    return null;
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    if (!DateUtil.IsCellDateFormatted(cell))
                    {
                        return cell.NumericCellValue.ToString("R", CultureInfo.InvariantCulture);
                    }
                    break;
            }

            throw new FormatException($"{cell.Sheet.SheetName}!{cell.Address}: use text or numbers; " +
                "formulas, dates, booleans and errors are not supported.");
        }
    }
}
