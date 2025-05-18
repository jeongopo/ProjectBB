using System.Data;
using System.Xml;

namespace ExcelToXml
{
    public static class XmlHelper
    {
        public static void SaveDataTableToXml(DataTable table, string filePath)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("Rows");
            doc.AppendChild(root);

            foreach (DataRow row in table.Rows)
            {
                XmlElement item = doc.CreateElement("Row");
                foreach (DataColumn col in table.Columns)
                {
                    XmlElement field = doc.CreateElement(col.ColumnName);
                    field.InnerText = row[col]?.ToString() ?? string.Empty;
                    item.AppendChild(field);
                }
                root.AppendChild(item);
            }

            doc.Save(filePath);
        }
    }
}