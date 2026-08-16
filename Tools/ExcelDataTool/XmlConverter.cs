using System.Data;
using System.Xml;
using System.Text;

namespace ExcelDataTool
{
    public enum FieldKind
    {
        Int,
        Float,
        Bool,
        String,
        Date,
        Enum,
        IntArray,
        FloatArray,
        StringArray,
        Id,
        IdArray,
        Design,
    }

    public class ColumnType
    {
        public required string ColumnName;
        public FieldKind Kind;
        public required string RawType; // XML/struct 코드에 실제로 기록되는 타입 문자열
        public string? RefTable; // Id / IdArray 전용: 참조 대상 테이블명
    }

    public static class XmlConverter
    {
        /// <summary>
        /// 기획자가 한글 등으로 참조용 별칭을 적어두는 컬럼명. XML/struct에는 내보내지 않고,
        /// 다른 테이블에서 id/id[]로 참조할 때 ID 대신 이 값으로도 찾을 수 있게 해준다.
        /// </summary>
        public const string AliasColumnName = "ID_DEV";

        /// <summary>
        /// 타입 행(Rows[0])의 셀 값을 파싱한다. id/id[] 타입은 "id:TableName" / "id[]:TableName" 문법을 사용하며,
        /// 실제 XML/struct에는 string/string[]으로 기록된다 (런타임 쪽 변경 불필요).
        /// "design" 타입은 기획자 전용 메모 컬럼으로, XML/struct에 아예 포함되지 않는다.
        /// </summary>
        public static ColumnType ParseColumnType(string columnName, string? rawTypeCell)
        {
            string trimmed = (rawTypeCell ?? "").Trim();
            string lower = trimmed.ToLowerInvariant();

            if (lower == "int" || lower == "integer")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Int, RawType = "int" };
            if (lower == "float" || lower == "double")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Float, RawType = "float" };
            if (lower == "bool" || lower == "boolean")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Bool, RawType = "bool" };
            if (lower == "string" || lower == "text")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.String, RawType = "string" };
            if (lower == "date" || lower == "datetime")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Date, RawType = "date" };
            if (lower == "design")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Design, RawType = "" };

            // 테이블명 없이 그냥 "id"만 쓴 경우 = 그 테이블 자신의 ID 컬럼 (참조 아님) -> string과 동일하게 처리.
            if (lower == "id")
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.String, RawType = "string" };

            if (lower.StartsWith("id[]:"))
            {
                string refTable = trimmed.Substring("id[]:".Length).Trim();
                if (string.IsNullOrEmpty(refTable))
                    throw new InvalidDataException($"'{columnName}' 컬럼의 id[] 타입에 참조 테이블명이 없습니다: '{rawTypeCell}'");
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.IdArray, RawType = "string[]", RefTable = refTable };
            }
            if (lower.StartsWith("id:"))
            {
                string refTable = trimmed.Substring("id:".Length).Trim();
                if (string.IsNullOrEmpty(refTable))
                    throw new InvalidDataException($"'{columnName}' 컬럼의 id 타입에 참조 테이블명이 없습니다: '{rawTypeCell}'");
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Id, RawType = "string", RefTable = refTable };
            }

            if (lower.Contains("enum"))
            {
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.Enum, RawType = trimmed.ToUpperInvariant() };
            }

            if (lower.Contains("[]"))
            {
                if (lower.Contains("int[]"))
                    return new ColumnType { ColumnName = columnName, Kind = FieldKind.IntArray, RawType = "int[]" };
                if (lower.Contains("float[]"))
                    return new ColumnType { ColumnName = columnName, Kind = FieldKind.FloatArray, RawType = "float[]" };
                if (lower.Contains("string[]"))
                    return new ColumnType { ColumnName = columnName, Kind = FieldKind.StringArray, RawType = "string[]" };
                return new ColumnType { ColumnName = columnName, Kind = FieldKind.IntArray, RawType = "int[]" }; // 기본값 (기존 동작 유지)
            }

            throw new InvalidDataException($"'{columnName}' 컬럼의 타입을 인식할 수 없습니다: '{rawTypeCell}'");
        }

        public static List<ColumnType> ParseColumnTypes(DataTable table)
        {
            var result = new List<ColumnType>();
            for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
            {
                string columnName = table.Columns[colIndex].ColumnName.ToUpperInvariant();
                string? rawTypeCell = table.Rows.Count > 0 ? table.Rows[0][colIndex]?.ToString() : null;
                result.Add(ParseColumnType(columnName, rawTypeCell));
            }
            return result;
        }

        public static void SaveDataTableToXml(DataTable table, List<ColumnType> columnTypes, string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Rows");
            doc.AppendChild(root);

            if (table.Rows.Count == 0)
            {
                doc.Save(filePath);
                return;
            }

            // design 타입, ID_DEV 컬럼은 기획자 참고용일 뿐 게임 데이터가 아니므로 XML에서 제외한다.
            var exportedColumns = columnTypes
                .Select((columnType, colIndex) => (columnType, colIndex))
                .Where(c => IsExported(c.columnType))
                .ToList();

            XmlElement infoElement = doc.CreateElement("Info");
            foreach (var (columnType, _) in exportedColumns)
            {
                XmlElement field = doc.CreateElement(columnType.ColumnName);
                field.InnerText = columnType.RawType;
                infoElement.AppendChild(field);
            }
            root.AppendChild(infoElement);

            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
            {
                var dataRow = table.Rows[rowIndex];
                XmlElement rowElement = doc.CreateElement("Row");

                foreach (var (columnType, colIndex) in exportedColumns)
                {
                    string value = dataRow[colIndex]?.ToString() ?? "";

                    XmlElement field = doc.CreateElement(columnType.ColumnName);
                    if (columnType.Kind == FieldKind.Enum)
                    {
                        //대문자 통일 예외처리
                        //@TODO bool 값도 통일할지
                        value = value.ToUpperInvariant();
                    }
                    field.InnerText = value;
                    rowElement.AppendChild(field);
                }

                root.AppendChild(rowElement);
            }

            doc.Save(filePath);
        }

        private static bool IsExported(ColumnType columnType)
        {
            if (columnType.Kind == FieldKind.Design) return false;
            if (columnType.ColumnName.Equals(AliasColumnName, StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }

        public static string GenerateStructFromXml(string xmlPath, out List<string> structNames)
        {
            structNames = new List<string>();
            string[] files = Directory.GetFiles(xmlPath, "*.xml");
            if (files.Length == 0)
            {
                throw new FileNotFoundException("지정된 디렉토리에 XML 파일이 없습니다: " + xmlPath);
            }

            StringBuilder sb = new StringBuilder();
            foreach (string file in files.OrderBy(f => f, StringComparer.Ordinal))
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                string structName = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(structName))
                {
                    continue;
                }

                XmlNode? infoNode = doc.SelectSingleNode("//Info");
                if (infoNode == null)
                {
                    continue;
                }

                sb.AppendLine($"\tpublic class {structName}");
                sb.AppendLine("\t{");

                foreach (XmlNode child in infoNode.ChildNodes)
                {
                    string fieldName = child.Name;
                    string type = child.InnerText.Trim().ToLowerInvariant();

                    if (type.Equals("bool", StringComparison.OrdinalIgnoreCase)) type = "bool";
                    else if (type.Equals("int", StringComparison.OrdinalIgnoreCase)) type = "int";
                    else if (type.Equals("float", StringComparison.OrdinalIgnoreCase)) type = "float";
                    else if (type.Contains("enum")) type = type.ToUpperInvariant();
                    else if (type.Contains("[]")) { /* 배열 타입은 그대로 유지 */ }
                    else type = "string"; // 기본값 처리

                    sb.AppendLine($"\t\t\tpublic {type} {fieldName};");
                }
                sb.AppendLine("\t}");

                structNames.Add(structName);
            }

            return sb.ToString();
        }

        /// <summary>
        /// {1;179} 형태 또는 일반 구분자 문자열을 토큰 배열로 분리 (id[] 참조 검증에도 사용)
        /// </summary>
        public static string[] ParseStringArray(string input)
        {
            if (string.IsNullOrEmpty(input)) return Array.Empty<string>();

            string cleanInput = input.Trim();
            if (cleanInput.StartsWith("{") && cleanInput.EndsWith("}"))
            {
                cleanInput = cleanInput.Substring(1, cleanInput.Length - 2);
            }
            char[] separators = { ',', ' ', ';', '\t', '\n', '\r' };
            return cleanInput.Split(separators, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .ToArray();
        }
    }
}
