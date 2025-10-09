using System.Data;
using ExcelDataReader;
using System.IO;
using System.Text;

namespace ExcelToXml
{
    public static class ExcelHelper
    {
        public static DataTable LoadFirstSheet(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration() { UseHeaderRow = true }
            });

            return result.Tables[0];
        }

        public static DataSet LoadAllSheets(string filePath)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = true
                }
            });

            return result;
        }

        /// <summary>
        /// 엑셀에서 바로 C# 코드로 생성. XML 변환 과정을 거치지 않는다
        /// 모든 Enum 이름과 값은 대문자로 통일한다
        /// </summary>
        public static void GenerateEnumFromExcel(string EnumFilePath, out string EnumCode)
        {
            EnumCode = string.Empty;
            DataSet ExcelSet = new DataSet();
            if (!File.Exists(EnumFilePath))
            {
                System.Windows.MessageBox.Show("지정된 파일이 존재하지 않습니다: " + EnumFilePath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw new FileNotFoundException("The specified file does not exist.");
            }

            ExcelSet = LoadAllSheets(EnumFilePath);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// This file is auto-generated from Excel files.");
            sb.AppendLine("using System;");
            sb.AppendLine("namespace DataEnumDefines");
            sb.AppendLine("{");
            foreach (DataTable table in ExcelSet.Tables)
            {
                if (table.Rows.Count == 0) continue;

                string enumName = table.TableName.ToUpper();
                sb.AppendLine($"\tpublic enum {enumName}");
                sb.AppendLine("\t{");

                foreach (DataRow row in table.Rows)
                {
                    if (row.ItemArray.Length < 2) continue; // Ensure there are at least two columns
                    string name = row[0].ToString().ToUpper();
                    string value = row[1].ToString();

                    sb.AppendLine($"\t\t{name} = {value},");
                }

                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
            EnumCode = sb.ToString();
        }   
    }
}