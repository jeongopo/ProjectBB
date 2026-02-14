using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Xml;
using UnityEngine;
using System;
using Mono.Cecil;
using DataEnumDefines;
using System.Linq;
using static ParsingHelper;

public class DataManager : MonoBehaviour
{
    public DataStorage dataStorage = new DataStorage();
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
            Debug.Log($"ID: {item.Value.ID}, Name: {item.Value.NAME}, Grade: {item.Value.GRADE}");
        }
        foreach (var item in dataStorage.TestCustomerData)
        {
            Debug.Log($"ID: {item.Value.ID}, Name: {item.Value.NAME}, Level: {item.Value.LEVEL}, Cooking Step: {item.Value.COOKINGSTEP}, Special: {item.Value.SPECIAL}");
        }
        foreach (var item in dataStorage.TestMinigame_BoilingData)
        {
            Debug.Log($"ID: {item.Value.ID}, Name: {item.Value.NAME}, Ingredient: {item.Value.INGREDIENT}, Boiling Time: {item.Value.BOILING_TIME}, Boiling Difficulty: {item.Value.BOILING_DIFFICULTY}");
            foreach(var spot in item.Value.SWEET_SPOT)
            {
                Debug.Log($"Sweet Spot: {spot}");
            }   
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
                else if (member.FieldType == typeof(int[]))
                {
                    int[] array = ParsingHelper.ParseIntArray(field.InnerText);
                    member.SetValue(item, array);
                }
                else if (member.FieldType == typeof(float[]))
                {
                    float[] array = ParsingHelper.ParseFloatArray(field.InnerText);
                    member.SetValue(item, array);
                }
                else if (member.FieldType == typeof(string[]))
                {
                    string[] array = ParsingHelper.ParseStringArray(field.InnerText);
                    member.SetValue(item, array);
                }
                else
                {
                    object value = Convert.ChangeType(field.InnerText, member.FieldType);
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
