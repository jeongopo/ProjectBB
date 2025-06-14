using System.Data;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Text;

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
                else if (type.Contains("Enum"))
                {
                    // Enum 타입은 특별히 처리하지 않음           
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

        public static string GenerateStructFromXml(string xmlPath, out List<string> structNames)
        {
            structNames = new List<string>();
            string[] files = Directory.GetFiles(xmlPath, "*.xml");
            if (files.Length == 0)
            {
                System.Windows.MessageBox.Show("지정된 디렉토리에 XML 파일이 없습니다: " + xmlPath, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                throw new FileNotFoundException("No XML files found in the specified directory.");
            }

            StringBuilder sb = new StringBuilder();
            foreach (string file in files)
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);

                string structName = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(structName))
                {
                    System.Windows.MessageBox.Show("파일 이름이 비어 있거나 유효하지 않습니다: " + file, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    continue;
                }

                XmlNode? infoNode = doc.SelectSingleNode("//Info");
                if (infoNode == null)
                {
                    System.Windows.MessageBox.Show("Info 노드를 찾을 수 없습니다: " + file, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    continue;
                }

                sb.AppendLine($"\tpublic class {structName}");
                sb.AppendLine("\t{");

                foreach (XmlNode child in infoNode.ChildNodes)
                {
                    string fieldName = child.Name;
                    string type = child.InnerText.Trim();

                    // 소문자 bool은 C# 예약어로 대소문자 구분이 없으므로 올바르게 처리
                    if (type.Equals("bool", StringComparison.OrdinalIgnoreCase)) type = "bool";
                    else if (type.Equals("int", StringComparison.OrdinalIgnoreCase)) type = "int";
                    else if (type.Equals("float", StringComparison.OrdinalIgnoreCase)) type = "float";
                    else if (type.Equals("string", StringComparison.OrdinalIgnoreCase)) type = "string";
                    else if (type.Contains("Enum")) type = type;
                    else type = "string"; // 기본값 처리

                    sb.AppendLine($"\t\t\tpublic {type} {fieldName};");
                }
                sb.AppendLine("\t}");

                structNames.Add(structName);
                System.Windows.MessageBox.Show($"구조체 {structName} 생성 완료", "Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }

            return sb.ToString();
        }
    }
}