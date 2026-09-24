using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace VM233.ExcelLocalization
{
    public sealed class ExcelTableData
    {
        public string[] Locales { get; }

        public IReadOnlyDictionary<string, string[]> Entries { get; }

        private ExcelTableData(string[] locales, Dictionary<string, string[]> entries)
        {
            Locales = locales;
            Entries = entries;
        }

        public static ExcelTableData Read(string path, string worksheet)
        {
            var sheets = ExcelWorkbookReader.Read(path, worksheet);
            if (!sheets.TryGetValue(worksheet, out var rows) || rows.Count == 0 ||
                rows[0].Length < 2 || rows[0][0] != "Key")
            {
                throw new InvalidDataException($"{path}, {worksheet}: expected Key followed by locale columns.");
            }

            var locales = rows[0].Skip(1).ToArray();
            var seenLocales = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < locales.Length; i++)
            {
                try
                {
                    var code = locales[i];
                    if (string.IsNullOrWhiteSpace(code) || code != code.Trim() ||
                        CultureInfo.GetCultureInfo(code).Name != code || !seenLocales.Add(code))
                    {
                        throw new ArgumentException();
                    }
                }
                catch (ArgumentException)
                {
                    throw new InvalidDataException($"{path}, {worksheet}, column {i + 2}: " +
                        $"'{locales[i]}' is not a unique, canonical locale code (for example zh-CN or en).");
                }
            }

            var entries = new Dictionary<string, string[]>(StringComparer.Ordinal);
            for (var i = 1; i < rows.Count; i++)
            {
                var row = rows[i];
                if (row.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                var location = $"{path}, {worksheet}, row {i + 1}";
                if (row.Length != locales.Length + 1 || row.Any(string.IsNullOrWhiteSpace))
                {
                    throw new InvalidDataException(location + ": supply a Key and a translation for every locale.");
                }

                var key = row[0];
                if (key != key.Trim() || key.Contains("\n") || key.Contains("\r") || entries.ContainsKey(key))
                {
                    throw new InvalidDataException(location + ": Key must be unique, nonempty and have no outer whitespace.");
                }

                var translations = row.Skip(1).ToArray();
                HashSet<int> expectedArguments = null;
                for (var column = 0; column < translations.Length; column++)
                {
                    var arguments = FormatArguments(translations[column], location + ", " + locales[column]);
                    if (expectedArguments != null && !expectedArguments.SetEquals(arguments))
                    {
                        throw new InvalidDataException(location + ": all translations must use the same argument indices.");
                    }

                    expectedArguments = arguments;
                }

                entries.Add(key, translations);
            }

            return new ExcelTableData(locales, entries);
        }

        private static HashSet<int> FormatArguments(string text, string location)
        {
            try
            {
                string.Format(CultureInfo.InvariantCulture, text, new object[32]);
            }
            catch (FormatException exception)
            {
                throw new InvalidDataException(location +
                    ": invalid format; use {0} through {31}, or escape literal braces.", exception);
            }

            var result = new HashSet<int>();
            for (var i = 0; i < text.Length; i++)
            {
                if (text[i] != '{')
                {
                    continue;
                }

                if (i + 1 < text.Length && text[i + 1] == '{')
                {
                    i++;
                    continue;
                }

                var start = ++i;
                while (i < text.Length && char.IsDigit(text[i]))
                {
                    i++;
                }

                result.Add(int.Parse(text.Substring(start, i - start), CultureInfo.InvariantCulture));
                while (i < text.Length && text[i] != '}')
                {
                    i++;
                }
            }

            return result;
        }
    }
}
