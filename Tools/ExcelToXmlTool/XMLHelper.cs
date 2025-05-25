using System.Data;
using System.Xml;
using System.Collections.Generic;

namespace ExcelToXml
{
    public static class XmlHelper
    {
        public static int SaveDataTableToXml(DataTable table, string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Rows");
            doc.AppendChild(root);

            if (table == null || table.Rows.Count == 0)
            {
                doc.Save(filePath);
                return 0;
            }

            XmlElement InfoElement = doc.CreateElement("Info");
            for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
            {
                string columnName = table.Columns[colIndex].ColumnName;
                string type = "string"; // 기본 타입 설정

                // 첫 번째 행의 첫 번째 컬럼을 타입으로 사용
                type = table.Rows[0][colIndex]?.ToString()?.Trim() ?? "string";
                if (type == "int" || type == "integer")
                {
                    type = "int";
                }
                else if (type == "float" || type == "double")
                {
                    type = "float";
                }
                else if (type == "bool" || type == "boolean")
                {
                    type = "bool";
                }
                else if (type == "string" || type == "text")
                {
                    type = "string";
                }
                else if (type == "date" || type == "datetime")
                {
                    type = "date";
                }
                else
                {
                    return -1; // 타입이 유효하지 않으면 저장하지 않음
                }

                XmlElement field = doc.CreateElement(columnName);
                field.InnerText = type;
                InfoElement.AppendChild(field);
            }
            root.AppendChild(InfoElement);

            // 나머지 Row: 실제 데이터
            for (int rowIndex = 1; rowIndex < table.Rows.Count; rowIndex++)
            {
                var dataRow = table.Rows[rowIndex];
                XmlElement rowElement = doc.CreateElement("Row");

                for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
                {
                    string columnName = table.Columns[colIndex].ColumnName;
                    string value = dataRow[colIndex]?.ToString() ?? "";

                    XmlElement field = doc.CreateElement(columnName);
                    field.InnerText = value;

                    rowElement.AppendChild(field);
                }

                root.AppendChild(rowElement);
            }

            doc.Save(filePath);
            return 1;
        }
    }
}