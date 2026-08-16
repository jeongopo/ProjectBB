using System.Data;
using ExcelDataReader;
using System.Text;

namespace ExcelDataTool
{
    public static class ExcelHelper
    {
        public static DataSet LoadAllSheets(string filePath, bool useHeaderRow = true)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            using var stream = File.Open(filePath, FileMode.Open, FileAccess.Read);
            using var reader = ExcelReaderFactory.CreateReader(stream);

            var result = reader.AsDataSet(new ExcelDataSetConfiguration()
            {
                ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                {
                    UseHeaderRow = useHeaderRow
                }
            });

            return result;
        }

        /// <summary>
        /// 한 엑셀 파일 안의 여러 탭을 하나의 테이블로 합친다 (모든 탭이 같은 컬럼 구성이어야 함).
        /// 예: BBString.xlsx에 언어/카테고리별로 탭을 나눠도 결과적으로 하나의 BBString 테이블/ID 집합으로 취급된다.
        /// 각 탭의 타입 행(Rows[0])은 첫 번째 탭 것만 쓰고, 나머지 탭은 데이터 행만 가져온다.
        /// </summary>
        public static DataTable CombineSheets(DataSet dataSet)
        {
            if (dataSet.Tables.Count == 0)
            {
                throw new InvalidDataException("시트가 없는 엑셀 파일입니다.");
            }

            DataTable first = dataSet.Tables[0];
            DataTable combined = first.Clone();
            foreach (DataRow row in first.Rows)
            {
                combined.ImportRow(row);
            }

            for (int i = 1; i < dataSet.Tables.Count; i++)
            {
                DataTable sheet = dataSet.Tables[i];
                if (sheet.Rows.Count == 0)
                {
                    continue;
                }

                if (sheet.Columns.Count != first.Columns.Count)
                {
                    throw new InvalidDataException(
                        $"탭 '{sheet.TableName}'의 컬럼 수가 '{first.TableName}' 탭과 다릅니다. 같은 파일의 탭들은 컬럼 구성이 동일해야 합니다.");
                }
                for (int c = 0; c < sheet.Columns.Count; c++)
                {
                    if (!string.Equals(sheet.Columns[c].ColumnName, first.Columns[c].ColumnName, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException(
                            $"탭 '{sheet.TableName}'의 컬럼 구성이 '{first.TableName}' 탭과 다릅니다 ('{sheet.Columns[c].ColumnName}' vs '{first.Columns[c].ColumnName}').");
                    }
                }

                // Rows[0]은 그 탭 자신의 타입 선언 행이라 건너뛰고, 데이터 행(Rows[1..])만 합친다.
                for (int r = 1; r < sheet.Rows.Count; r++)
                {
                    combined.ImportRow(sheet.Rows[r]);
                }
            }

            return combined;
        }

        /// <summary>
        /// 엑셀에서 바로 C# 코드로 생성. XML 변환 과정을 거치지 않는다
        /// 모든 Enum 이름과 값은 대문자로 통일한다
        /// 이 파일의 시트는 헤더 행 없이 1행부터 바로 이름/값 데이터라서 UseHeaderRow=false로 읽는다.
        /// </summary>
        public static string GenerateEnumFromExcel(string enumFilePath)
        {
            if (!File.Exists(enumFilePath))
            {
                throw new FileNotFoundException("The specified file does not exist.", enumFilePath);
            }

            DataSet excelSet = LoadAllSheets(enumFilePath, useHeaderRow: false);
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("// This file is auto-generated from Excel files.");
            sb.AppendLine("using System;");
            sb.AppendLine("namespace DataEnumDefines");
            sb.AppendLine("{");
            foreach (DataTable table in excelSet.Tables)
            {
                if (table.Rows.Count == 0) continue;

                string enumName = table.TableName.ToUpper();
                sb.AppendLine($"\tpublic enum {enumName}");
                sb.AppendLine("\t{");

                // 값 칸을 비워두면 이전 값 + 1로 자동 채워진다 (C# enum 기본 규칙과 동일, 첫 행이 비어있으면 0부터).
                int nextValue = 0;
                foreach (DataRow row in table.Rows)
                {
                    if (row.ItemArray.Length < 1) continue;
                    string name = row[0]?.ToString()?.Trim().ToUpper() ?? "";
                    if (string.IsNullOrEmpty(name)) continue;

                    string? rawValue = row.ItemArray.Length > 1 ? row[1]?.ToString()?.Trim() : null;
                    int value;
                    if (string.IsNullOrEmpty(rawValue))
                    {
                        value = nextValue;
                    }
                    else if (!int.TryParse(rawValue, out value))
                    {
                        throw new InvalidDataException($"enum '{enumName}'의 '{name}' 값이 정수가 아닙니다: '{rawValue}'");
                    }

                    sb.AppendLine($"\t\t{name} = {value},");
                    nextValue = value + 1;
                }

                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
