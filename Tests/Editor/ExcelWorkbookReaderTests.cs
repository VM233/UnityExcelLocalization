using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NUnit.Framework;
using UnityEditor;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace VM233.ExcelLocalization.Tests
{
    public sealed class ExcelWorkbookReaderTests
    {
        private string path;

        [SetUp]
        public void Setup()
        {
            Directory.CreateDirectory("Library/ExcelLocalizationTests");
            path = Path.Combine("Library/ExcelLocalizationTests", "workbook-" + Guid.NewGuid().ToString("N") + ".xlsx");
        }

        [TearDown]
        public void Cleanup()
        {
            File.Delete(path);
        }

        private void Write(Action<XSSFWorkbook> configure)
        {
            using (var workbook = new XSSFWorkbook())
            {
                workbook.CreateSheet("Items");
                configure(workbook);
                using (var file = File.Create(path))
                {
                    workbook.Write(file);
                }
            }
        }

        [Test]
        public void ReadsSharedInlineAndNumericValuesWithoutLosingEmptyRowsOrColumns()
        {
            Write(workbook =>
            {
                var sheet = workbook.GetSheetAt(0);
                var row = sheet.CreateRow(0);
                row.CreateCell(0).SetCellValue("itemId");
                row.CreateCell(2).SetCellValue("木头");
                var number = row.CreateCell(3);
                number.SetCellValue(5);
                var style = workbook.CreateCellStyle();
                style.DataFormat = workbook.CreateDataFormat().GetFormat("0000");
                number.CellStyle = style;
                row.CreateCell(4).SetCellValue(1.5);
                sheet.CreateRow(2).CreateCell(2).SetCellValue("生铁");
                workbook.CreateSheet("配方").CreateRow(0).CreateCell(0).SetCellValue("铁弓");
            });
            using (var archive = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                var entry = archive.GetEntry("xl/worksheets/sheet1.xml");
                XDocument document;
                using (var stream = entry.Open())
                {
                    document = XDocument.Load(stream);
                }

                XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
                var cell = document.Descendants(ns + "c").Single(x => (string)x.Attribute("r") == "A1");
                cell.SetAttributeValue("t", "inlineStr");
                cell.ReplaceNodes(new XElement(ns + "is", new XElement(ns + "r", new XElement(ns + "t", "item")),
                    new XElement(ns + "r", new XElement(ns + "t", "Id"))));
                entry.Delete();
                using (var stream = archive.CreateEntry("xl/worksheets/sheet1.xml").Open())
                {
                    document.Save(stream);
                }
            }

            var previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
                var sheets = ExcelWorkbookReader.Read(path);
                CollectionAssert.AreEqual(new[] { "itemId", null, "木头", "5", "1.5" }, sheets["Items"][0]);
                Assert.IsEmpty(sheets["Items"][1]);
                CollectionAssert.AreEqual(new[] { null, null, "生铁" }, sheets["Items"][2]);
                Assert.AreEqual("铁弓", sheets["配方"][0][0]);
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }

            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }

        [TestCase("formula")]
        [TestCase("boolean")]
        [TestCase("error")]
        [TestCase("date")]
        public void RejectsUnsupportedValuesWithTheCellAddress(string type)
        {
            Write(workbook =>
            {
                var cell = workbook.GetSheetAt(0).CreateRow(1).CreateCell(2);
                switch (type)
                {
                    case "formula":
                        cell.SetCellFormula("2+3");
                        cell.SetCellValue(5);
                        break;
                    case "boolean":
                        cell.SetCellValue(true);
                        break;
                    case "error":
                        cell.SetCellErrorValue(FormulaError.DIV0.Code);
                        break;
                    case "date":
                        cell.SetCellValue(new DateTime(2026, 9, 24));
                        var style = workbook.CreateCellStyle();
                        style.DataFormat = workbook.CreateDataFormat().GetFormat("yyyy-mm-dd");
                        cell.CellStyle = style;
                        break;
                }
            });

            var error = Assert.Throws<FormatException>(() => ExcelWorkbookReader.Read(path));
            StringAssert.Contains("Items!C2", error.Message);
            using (File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { }
        }

        [TestCase(10000, 0)]
        [TestCase(0, 64)]
        public void RejectsRowsAndColumnsBeyondConfigurationLimits(int row, int column)
        {
            Write(workbook => workbook.GetSheetAt(0).CreateRow(row).CreateCell(column).SetCellValue("wood"));
            Assert.Throws<FormatException>(() => ExcelWorkbookReader.Read(path));
        }

        [Test]
        public void NpoiAssembliesAreEditorOnlyDependencies()
        {
            var package = PackageInfo.FindForAssembly(typeof(ExcelWorkbookReader).Assembly);
            var files = Directory.GetFiles(Path.Combine(package.resolvedPath, "Editor/Plugins/NPOI"), "*.dll");
            Assert.IsNotEmpty(files);
            foreach (var file in files)
            {
                var assetPath = "Packages/" + package.name + "/Editor/Plugins/NPOI/" + Path.GetFileName(file);
                var importer = AssetImporter.GetAtPath(assetPath) as PluginImporter;
                Assert.IsNotNull(importer, file);
                Assert.IsTrue(importer.GetCompatibleWithEditor(), file);
                Assert.IsFalse(importer.GetCompatibleWithAnyPlatform(), file);
                Assert.IsFalse(importer.GetCompatibleWithPlatform(BuildTarget.StandaloneWindows64), file);
            }
        }

        [Test]
        public void IncludedExampleHasValidKeysTranslationsAndArguments()
        {
            var package = PackageInfo.FindForAssembly(typeof(ExcelWorkbookReader).Assembly);
            var data = ExcelTableData.Read(Path.Combine(package.resolvedPath, "Samples~/Basic/UI.xlsx"), "Strings");
            CollectionAssert.AreEqual(new[] { "zh-CN", "en" }, data.Locales);
            Assert.AreEqual(3, data.Entries.Count);
            Assert.AreEqual("Inventory ({0} / {1})", data.Entries["inventory.title"][1]);
        }

        [Test]
        public void BindingReadsOnlyItsSelectedWorksheet()
        {
            Write(workbook =>
            {
                workbook.GetSheetAt(0).CreateRow(0).CreateCell(0).SetCellFormula("2+3");
                var sheet = workbook.CreateSheet("Strings");
                var header = sheet.CreateRow(0);
                header.CreateCell(0).SetCellValue("Key");
                header.CreateCell(1).SetCellValue("en");
                var row = sheet.CreateRow(1);
                row.CreateCell(0).SetCellValue("close");
                row.CreateCell(1).SetCellValue("Close");
            });
            var data = ExcelTableData.Read(path, "Strings");
            Assert.AreEqual("Close", data.Entries["close"][0]);
        }
    }
}
