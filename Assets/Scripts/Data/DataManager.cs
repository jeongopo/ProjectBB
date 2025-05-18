using System.Xml;
using UnityEngine;

public class DataManager : MonoBehaviour
{
    string xmlFileName = "TestItem";
    string xmlFilePath = "XML/";

    void Start()
    {
        LoadXML(xmlFileName);
    }

    private void LoadXML(string _fileName)
    {
        TextAsset txtAsset = Resources.Load<TextAsset>(xmlFilePath + _fileName);
        XmlDocument xmlDoc = new XmlDocument();
        Debug.Log(txtAsset.text);
        xmlDoc.LoadXml(txtAsset.text);

        XmlNodeList cost_Table = xmlDoc.GetElementsByTagName("cost");
        foreach (XmlNode cost in cost_Table)
        {
            Debug.Log("[one by one] cost : " + cost.Attributes["value"].Value);
        }

        XmlNodeList all_nodes = xmlDoc.SelectNodes("dataroot/TestItem");
        foreach (XmlNode node in all_nodes)
        {
            // 수량이 많으면 반복문 사용.
            Debug.Log("[at once] id :" + node.SelectSingleNode("id").InnerText);
            Debug.Log("[at once] name : " + node.SelectSingleNode("name").InnerText);
            Debug.Log("[at once] cost : " + node.SelectSingleNode("cost").InnerText);
        }
    }
}
