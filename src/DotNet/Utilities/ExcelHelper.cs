using MonkoraEdge.Core.DotNet.AggregatesModel.ConstantAggregate;
using MonkoraEdge.Core.DotNet.AggregatesModel.FactoryAggregate;
using MonkoraEdge.Core.DotNet.Extensions;
using Microsoft.AspNetCore.Http;
using OfficeOpenXml;
using OfficeOpenXml.DataValidation;
using System.Collections;
using System.Reflection;

namespace MonkoraEdge.Core.DotNet.Utilities
{
    public static class ExcelHelper
    {
        public static void ReadExcelData<T>(IFormFile file, out List<T> excelData) where T : new()
        {
            string methodName = MethodBase.GetCurrentMethod().Name.ToSnakeCase();

            using (var memoryStream = new MemoryStream())
            {
                file.CopyTo(memoryStream);

                using (var package = new ExcelPackage(memoryStream))
                {
                    var worksheet = package.Workbook.Worksheets["data"];

                    if (worksheet is null)
                    {
                        throw ExceptionFactory.BadRequest(typeof(ExcelHelper).Name.ToSnakeCase(), ErrorCodeType.INVALID_INPUT, "Data worksheet is required.");
                    }

                    if (worksheet.Dimension?.End?.Row is null || worksheet.Dimension.End.Row == 0)
                    {
                        throw ExceptionFactory.BadRequest(typeof(ExcelHelper).Name.ToSnakeCase(), ErrorCodeType.INVALID_INPUT, "There are no records.");
                    }

                    excelData = ConvertSheetToObjects<T>(worksheet);
                }
            }
        }

        private static List<T> ConvertSheetToObjects<T>(ExcelWorksheet worksheet, int rowDataStartIndex = 2, int rowHeaderIndex = 1) where T : new()
        {
            string methodName = MethodBase.GetCurrentMethod().Name.ToSnakeCase();

            var objProperties = new T()
                .GetType()
                .GetProperties()
                .ToList();

            var objHeaders = new List<(int colIndex, string colName)>();

            for (var i = 1; i <= worksheet.Dimension.End.Column; i++)
            {
                var cell = worksheet.Cells[rowHeaderIndex, i];

                if (!string.IsNullOrWhiteSpace(cell.Text))
                {
                    objHeaders.Add((i, cell.Text));
                }
            }

            var sheetHeaders = new List<(int colIndex, string colName)>();

            objProperties.ForEach(p =>
            {
                var headers = objHeaders.Where(h => h.colName == p.Name).ToList();

                if (headers.Count == 0)
                {
                    throw ExceptionFactory.BadRequest(typeof(ExcelHelper).Name.ToSnakeCase(), ErrorCodeType.INVALID_INPUT, $"Header({p.Name}) was not found.");
                }

                if (headers.Count > 1)
                {
                    throw ExceptionFactory.BadRequest(typeof(ExcelHelper).Name.ToSnakeCase(), ErrorCodeType.INVALID_INPUT, $"File have duplicate headers({p.Name}).");
                }

                sheetHeaders.Add(headers.Single());
            });

            var rowsValue = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(typeof(T)))!;

            for (var i = rowDataStartIndex; i <= worksheet.Dimension.End.Row; i++)
            {
                var rowValue = new T();

                for (var j = 0; j < sheetHeaders.Count; j++)
                {
                    var cell = worksheet.Cells[i, sheetHeaders[j].colIndex];
                    var prop = objProperties.First(p => p.Name == sheetHeaders[j].colName);
                    prop.SetValue(rowValue, cell.Text.Equals("[NULL]") ? null : cell.Text);
                }

                if (!rowValue.GetType().GetProperties().Select(m => m.GetValue(rowValue)).All(s => string.IsNullOrWhiteSpace(s?.ToString())))
                {
                    rowsValue?.Add(rowValue);
                }
            }

            return rowsValue as List<T>;
        }

        public static byte[] CreateXlsxSheet<T>(List<T> dataToExport,
            List<int> numberFormatColumnIndexes = null,
            List<(List<string> Datasource, List<int> DataIndexes, string SheetName)> dataValidations = null) where T : new()
        {
            var dataSourceType = typeof(T);

            using var stream = new MemoryStream();

            using (var excelPackage = new ExcelPackage(stream))
            {
                var excelWorksheet = excelPackage.Workbook.Worksheets.Add("data");
                var maxRow = ExcelPackage.MaxRows;

                excelWorksheet.Cells["A1"].LoadFromCollection(Collection: dataToExport, PrintHeaders: true);

                #region Set Header and Drop down
                var headers = dataSourceType.GetProperties();

                for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
                {
                    excelWorksheet.Cells[1, columnIndex + 1].Value = headers[columnIndex].Name;
                }
                #endregion

                #region Create Datasource worksheet from csv & Data Validation (Drop Down)
                var dataValidationRowStart = 2;

                for (var i = 0; i < dataValidations?.Count; i++)
                {
                    var dataValidationDataSource = dataValidations[i].Datasource;
                    var columnIndexesTarget = dataValidations[i].DataIndexes;
                    var dataValidationSourceSheetName = dataValidations[i].SheetName;

                    var dataValidationSourceSheet = excelPackage.Workbook.Worksheets.Add(dataValidationSourceSheetName);
                    dataValidationSourceSheet.Hidden = eWorkSheetHidden.VeryHidden;
                    dataValidationSourceSheet.Cells["A1"].LoadFromCollection(dataValidationDataSource);

                    string validationDataExcelFormula = $"'{dataValidationSourceSheetName}'!$a" + "$1:$a$" + dataValidationDataSource.Count;

                    columnIndexesTarget.ForEach(columnIndexTarget =>
                    {
                        var range = ExcelCellBase.GetAddress(dataValidationRowStart, columnIndexTarget, maxRow, columnIndexTarget);
                        var dataValidationList = excelWorksheet.DataValidations.AddListValidation(range);
                        dataValidationList.ErrorTitle = "Entry was invalid.";
                        dataValidationList.Error = "Please choose options from the drop down only.";
                        dataValidationList.ErrorStyle = ExcelDataValidationWarningStyle.stop;
                        dataValidationList.ShowErrorMessage = true;
                        dataValidationList.Formula.ExcelFormula = validationDataExcelFormula;
                    });
                }
                #endregion

                #region Set Excel Format
                numberFormatColumnIndexes?.ForEach(m => excelWorksheet.Column(m).Style.Numberformat.Format = "@");
                #endregion

                excelPackage.SaveAs(stream);

                return stream.ToArray();
            }
        }
    }
}
