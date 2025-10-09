using System.Data;
using System.Xml;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Text;
using System;
using System.Linq;

namespace ExcelToXml
{
    public static class XmlHelper
    {
        public static BBErrorCode SaveDataTableToXml(DataTable table, string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Rows");
            doc.AppendChild(root);

            if (table == null || table.Rows.Count == 0)
            {
                doc.Save(filePath);
                return BBErrorCode.NoData;
            }

            XmlElement InfoElement = doc.CreateElement("Info");
            for (int colIndex = 0; colIndex < table.Columns.Count; colIndex++)
            {
                string columnName = table.Columns[colIndex].ColumnName.ToUpper();
                string type = "string"; // 기본 타입 설정

                // 첫 번째 행의 첫 번째 컬럼을 타입으로 사용
                type = table.Rows[0][colIndex]?.ToString()?.Trim().ToLower() ?? "string";
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
                else if (type.Contains("enum"))
                {
                    type = type.ToUpper();           
                }
                else if (type.Contains("[]"))
                {
                    // 배열 타입 처리
                    if (type.Contains("int[]"))
                        type = "int[]";
                    else if (type.Contains("float[]"))
                        type = "float[]";
                    else if (type.Contains("string[]"))
                        type = "string[]";
                    else
                        type = "int[]"; // 기본값
                }
                else
                {
                    return BBErrorCode.InvalidData; // 타입이 유효하지 않으면 저장하지 않음
                }

                XmlElement field = doc.CreateElement(columnName);
                string typeToWrite = type;
                if (!type.Contains("ENUM"))
                {
                    typeToWrite = type.ToLower();
                }
                field.InnerText = typeToWrite;
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
                    string columnName = table.Columns[colIndex].ColumnName.ToUpper();
                    string value = dataRow[colIndex]?.ToString() ?? "";
                    
                    XmlElement field = doc.CreateElement(columnName);
                    string type = InfoElement.ChildNodes[colIndex].InnerText;
                    if (type.Contains("ENUM"))
                    {
                        //대문자 통일 예외처리 
                        //@TODO bool 값도 통일할지
                        value = value.ToUpper();
                    }
                    field.InnerText = value;
                    rowElement.AppendChild(field);
                }

                root.AppendChild(rowElement);
            }

            doc.Save(filePath);
            return BBErrorCode.Success;
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
                    string type = child.InnerText.Trim().ToLower();

                    // 소문자 bool은 C# 예약어로 대소문자 구분이 없으므로 올바르게 처리
                    if (type.Equals("bool", StringComparison.OrdinalIgnoreCase)) type = "bool";
                    else if (type.Equals("int", StringComparison.OrdinalIgnoreCase)) type = "int";
                    else if (type.Equals("float", StringComparison.OrdinalIgnoreCase)) type = "float";
                    else if (type.Contains("enum")) type = type.ToUpper();
                    else if (type.Contains("[]")) type = type; // 배열 타입은 그대로 유지
                    else type = "string"; // 기본값 처리

                    sb.AppendLine($"\t\t\tpublic {type} {fieldName};");
                }
                sb.AppendLine("\t}");

                structNames.Add(structName);
            }

            return sb.ToString();
        }

        /// <summary>
        /// 문자열을 {1;179} 형태의 배열 문자열로 변환
        /// </summary>
        /// <param name="input">입력 문자열 (쉼표, 공백, 세미콜론 등으로 구분된 값들)</param>
        /// <returns>{1;179} 형태의 문자열</returns>
        public static string ConvertToArrayString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "{}";

            // 다양한 구분자로 분리 (쉼표, 공백, 세미콜론, 탭 등)
            char[] separators = { ',', ' ', ';', '\t', '\n', '\r' };
            string[] parts = input.Split(separators, StringSplitOptions.RemoveEmptyEntries);
            
            // 각 부분의 공백 제거
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i].Trim();
            }

            return "{" + string.Join(";", parts) + "}";
        }

        /// <summary>
        /// {1;179} 형태의 문자열을 int 배열로 파싱
        /// </summary>
        /// <param name="input">변환할 문자열 (예: "{1;179}")</param>
        /// <returns>int 배열</returns>
        public static int[] ParseIntArray(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new int[0];
            }

            try
            {
                string cleanInput = input.Trim().TrimStart('{').TrimEnd('}');
                string[] parts = cleanInput.Split(';');
                int[] result = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i].Trim(), out int value))
                    {
                        result[i] = value;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"'{parts[i]}'을 int로 변환할 수 없습니다.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return new int[0];
                    }
                }
                
                return result;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"배열 파싱 중 오류 발생: {e.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return new int[0];
            }
        }

        /// <summary>
        /// {1;179} 형태의 문자열을 float 배열로 파싱
        /// </summary>
        /// <param name="input">변환할 문자열 (예: "{1.5;179.2}")</param>
        /// <returns>float 배열</returns>
        public static float[] ParseFloatArray(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new float[0];
            }

            try
            {
                string cleanInput = input.Trim().TrimStart('{').TrimEnd('}');
                string[] parts = cleanInput.Split(';');
                float[] result = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (float.TryParse(parts[i].Trim(), out float value))
                    {
                        result[i] = value;
                    }
                    else
                    {
                        System.Windows.MessageBox.Show($"'{parts[i]}'을 float로 변환할 수 없습니다.", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        return new float[0];
                    }
                }
                
                return result;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"배열 파싱 중 오류 발생: {e.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return new float[0];
            }
        }

        /// <summary>
        /// {1;179} 형태의 문자열을 string 배열로 파싱
        /// </summary>
        /// <param name="input">변환할 문자열 (예: "{hello;world}")</param>
        /// <returns>string 배열</returns>
        public static string[] ParseStringArray(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return new string[0];
            }

            try
            {
                string cleanInput = input.Trim().TrimStart('{').TrimEnd('}');
                string[] result = cleanInput.Split(';');
                for (int i = 0; i < result.Length; i++)
                {
                    result[i] = result[i].Trim();
                }
                
                return result;
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show($"배열 파싱 중 오류 발생: {e.Message}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return new string[0];
            }
        }
    }
}