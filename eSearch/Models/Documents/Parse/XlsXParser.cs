using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using eSearch;
using System.IO;

namespace eSearch.Models.Documents.Parse
{
    internal class XlsXParser : IParser
    {
        public string[] Extensions
        {
            get { return new string[] { "xlsx" }; }
        }

        public void Parse(string filePath, out ParseResult parseResult)
        {
            parseResult = new();
            parseResult.ParserName = "xlsxParser (OpenXML)";
            StringBuilder docTextBuilder = new StringBuilder();
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(filePath, false))
            {
                #region Extract Data from WorkSheets
                if (doc.WorkbookPart?.WorksheetParts != null)
                {
                    foreach(var wsp in  doc.WorkbookPart.WorksheetParts)
                    {
                        var sheet = GetSheetFromWorkSheet(doc.WorkbookPart, wsp);
                        if (sheet != null)
                        {
                            string name = "Untitled";
                            if (sheet.Name != null && sheet.Name.Value != null)
                            {
                                name = sheet.Name.Value;
                            }
                            docTextBuilder.Append("Sheet: ").AppendLine(name);
                            foreach (var sheetData in wsp.Worksheet.Elements<SheetData>())
                            {
                                foreach (var row in sheetData.Elements<Row>())
                                {
                                    docTextBuilder.AppendLine();
                                    foreach (var cell in row.Elements<Cell>())
                                    {
                                        docTextBuilder.Append(GetHumanCellValue(cell, doc.WorkbookPart)).Append(", ");
                                        if (cell != null && cell.CellValue != null)
                                        {
                                            docTextBuilder.Append(cell.CellValue.Text).Append(", ");
                                        }
                                    }
                                    docTextBuilder.TrimEnd();
                                }
                            }
                        }
                    }
                }
                parseResult.TextContent = docTextBuilder.ToString();
                if (!string.IsNullOrEmpty(doc.PackageProperties.Creator))
                {
                    parseResult.Authors = new string[] {doc.PackageProperties.Creator};
                }
                if (!string.IsNullOrEmpty(doc.PackageProperties.Title)) { 
                    parseResult.Title = doc.PackageProperties.Title;
                } else
                {
                    parseResult.Title = Path.GetFileNameWithoutExtension(filePath);
                }
                #endregion
            }
        }

        public string GetHumanCellValue(Cell cell, WorkbookPart workbookPart)
        {
            string cellValue = string.Empty;
            if (cell.DataType != null)
            {
                if (cell.DataType == CellValues.SharedString)
                {
                    int id = -1;

                    if (Int32.TryParse(cell.InnerText, out id))
                    {
                        SharedStringItem item = GetSharedStringItemById(workbookPart, id);

                        if (item.Text != null)
                        {
                            cellValue = item.Text.Text;
                        }
                        else if (item.InnerText != null)
                        {
                            cellValue = item.InnerText;
                        }
                        else if (item.InnerXml != null)
                        {
                            cellValue = item.InnerXml;
                        }
                    }
                }
                else { cellValue = cell.InnerText; }
            }
            if (cellValue == string.Empty)
            {
                if (cell.CellValue != null && cell.CellValue.Text != null) {
                    cellValue = cell.CellValue.Text;
                }
            }
            return cellValue;
        }

        public static SharedStringItem GetSharedStringItemById(WorkbookPart workbookPart, int id)
        {
            return workbookPart.SharedStringTablePart.SharedStringTable.Elements<SharedStringItem>().ElementAt(id);
        }

        public static Sheet GetSheetFromWorkSheet (WorkbookPart workbookPart, WorksheetPart worksheetPart)
        {
            string relationshipId = workbookPart.GetIdOfPart(worksheetPart);
            IEnumerable<Sheet> sheets = workbookPart.Workbook.Sheets.Elements<Sheet>();
            return sheets.FirstOrDefault(s => s.Id.HasValue && s.Id.Value == relationshipId);
        }
    }
}
