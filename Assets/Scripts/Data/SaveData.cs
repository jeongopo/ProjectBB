using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This class contains all the variables that will be serialized and saved to a file.<br/>
/// Can be considered as a save file structure or format.
/// </summary>
[Serializable]
public class SaveData
{
	// This is test data, written according to TestScript.cs class
	// This will change according to whatever data that needs to be stored

	public string _locationId;
	public int coin;
	public List<string> _playerInventory;
	public int _currentDay;
	public int _currentTime;

	public string ToJson()
	{
		return JsonUtility.ToJson(this);
	}

	public void LoadFromJson(string json)
	{
		JsonUtility.FromJsonOverwrite(json, this);
	}
}
