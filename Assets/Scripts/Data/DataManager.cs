using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using UnityEngine;
using System;
using Mono.Cecil;
using DataEnumDefines;

public class DataManager : MonoBehaviour
{
    DataStorage dataStorage = new DataStorage();
	const string XMLFilePath = "XML/";

    void Start()
    {
        LoadXMLData();
    }

    void LoadXMLData()
    {
        dataStorage.LoadData();

        // Example usage of the loaded data
        foreach (var item in dataStorage.TestIngredientsData)
        {
            string TestStr;
            switch (item.Value.Grade)
            {
                case EnumGrade.Good:
                    TestStr = "Good";
                    break;
                case EnumGrade.Great:
                    TestStr = "Great";
                    break;
                case EnumGrade.Normal:
                    TestStr = "Normal";
                    break;
                default:
                    TestStr = "Unknown";
                    break;
            }
            Debug.Log($"ID: {item.Value.ID}, Name: {item.Value.Name}, Ingredients: {item.Value.Ingredients}, CookingStep: {item.Value.CookingStep}, Main: {item.Value.main}, Grade: {TestStr}");
        }
        foreach (var item in dataStorage.TestCustomerData)
        {
            Debug.Log($"ID: {item.Value.ID}, Name: {item.Value.Name}, Level: {item.Value.Level}, CookingStep: {item.Value.CookingStep}, Special: {item.Value.Special}");
        }
    }
    
    public static Dictionary<string, T> LoadDefineData<T>(string XMLName, string rowTag = "Row") where T : new()
	{
		TextAsset XMLAsset = Resources.Load<TextAsset>(XMLFilePath+XMLName);
		if(XMLAsset == null)
		{
			Debug.LogError($"XML file {XMLName} not found in Resources/XML/");
			return new Dictionary<string, T>();
		}
		var dictionary = new Dictionary<string, T>();
		XmlDocument doc = new XmlDocument();
		doc.LoadXml(XMLAsset.text);

		var rows = doc.SelectNodes($"/Rows/{rowTag}");
		if (rows == null) return dictionary;

		foreach (XmlNode row in rows)
		{
			T item = new T();
			var type = typeof(T);

			var idField = type.GetField("ID");
			if (idField == null)
			{
				Debug.LogError($"Type {type.Name} does not have an ID field.");
				continue;
			}

            string idValue = null;
			foreach (XmlNode field in row.ChildNodes)
			{
				var member = type.GetField(field.Name);
				if (member == null) continue;

                if (field.Name == "ID")
                {
                    idValue = field.InnerText;
                    if (string.IsNullOrEmpty(idValue))
                    {
                        Debug.LogError($"ID field is empty for row: {row.OuterXml}");
                        continue;
                    }
                }
                if (member.FieldType.IsEnum)
                {
                    if (Enum.TryParse(member.FieldType, field.InnerText, out object enumValue))
                    {
                        member.SetValue(item, enumValue);
                    }
                    else
                    {
                        Debug.LogError($"Failed to parse enum {member.FieldType.Name} from value '{field.InnerText}' in row: {row.OuterXml}");
                        continue;
                    }
                }
                else
                {
                    object? value = Convert.ChangeType(field.InnerText, member.FieldType);
                    member.SetValue(item, value);
                }
			}

            if (idValue != null && !dictionary.ContainsKey(idValue))
            {
                dictionary[idValue] = item;
            }
		}

		return dictionary;
	}
}
