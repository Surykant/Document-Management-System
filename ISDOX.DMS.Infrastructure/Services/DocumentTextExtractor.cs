using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using ISDOX.DMS.Application.Interfaces;
using System.Text;
using Tesseract;
using UglyToad.PdfPig;
using Text = DocumentFormat.OpenXml.Spreadsheet.Text;

namespace ISDOX.DMS.Infrastructure.Services
{
    public class DocumentTextExtractor : IDocumentTextExtractor
    {
        public string ExtractText(string filePath, string extension)
        {
            try
            {
                return extension.ToLowerInvariant() switch
                {
                    ".pdf" => ExtractFromPdf(filePath),
                    ".docx" or ".doc" => ExtractFromWord(filePath), 
                    ".xlsx" or ".xls" => ExtractFromExcel(filePath),
                    ".jpg" or ".jpeg" or ".png" or ".tiff" => ExtractFromImageOCR(filePath),
                    _ => string.Empty
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TextExtractor] Failed to extract {filePath}: {ex.Message}");
                return string.Empty;
            }
        }

        private string ExtractFromPdf(string filePath)
        {
            var textBuilder = new StringBuilder();
            using var document = PdfDocument.Open(filePath);
            foreach (var page in document.GetPages())
            {
                textBuilder.Append(page.Text).Append(" ");
            }
            return textBuilder.ToString().Trim();
        }

        private string ExtractFromWord(string filePath)
        {
            var textBuilder = new StringBuilder();
            using var wordDocument = WordprocessingDocument.Open(filePath, false);
            var body = wordDocument.MainDocumentPart?.Document?.Body;
            if (body != null)
            {
                foreach (var text in body.Descendants<Text>())
                {
                    textBuilder.Append(text.Text).Append(" ");
                }
            }
            return textBuilder.ToString().Trim();
        }

        private string ExtractFromExcel(string filePath)
        {
            var textBuilder = new StringBuilder();
            using var spreadsheetDocument = SpreadsheetDocument.Open(filePath, false);
            var workbookPart = spreadsheetDocument.WorkbookPart;
            if (workbookPart == null) return string.Empty;

            var sharedStringTable = workbookPart.SharedStringTablePart?.SharedStringTable;

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart?.Worksheet?.Elements<SheetData>().FirstOrDefault();
                if (sheetData == null) continue;

                foreach (var row in sheetData.Elements<Row>())
                {
                    foreach (var cell in row.Elements<Cell>())
                    {
                        string cellValue = cell.CellValue?.Text ?? string.Empty;
                        if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString && sharedStringTable != null)
                        {
                            cellValue = sharedStringTable.ElementAt(int.Parse(cellValue)).InnerText;
                        }
                        textBuilder.Append(cellValue).Append(" ");
                    }
                }
            }
            return textBuilder.ToString().Trim();
        }

        private string ExtractFromImageOCR(string filePath)
        {
            using var engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
            using var img = Pix.LoadFromFile(filePath);
            using var page = engine.Process(img);
            return page.GetText().Trim();
        }
    }
}
