using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveSystem : ScriptableObject
{
	public string saveFilename = "BBSaveData";
	public string backupSaveFilename = "BBSaveData.bak";
	public SaveData saveData = new SaveData();

	public List<string> _playerInventory;

	void OnEnable()
	{
		//_loadLocation.OnLoadingRequested += CacheLoadLocations;
	}

	void OnDisable()
	{
		//_loadLocation.OnLoadingRequested -= CacheLoadLocations;
	}

	public bool LoadSaveDataFromDisk()
	{
		if (FileManager.LoadFromFile(saveFilename, out var json))
		{
			saveData.LoadFromJson(json);
			return true;
		}

		return false;
	}

	public void SaveDataToDisk()
	{
		if (FileManager.MoveFile(saveFilename, backupSaveFilename))
		{
			if (FileManager.WriteToFile(saveFilename, saveData.ToJson()))
			{
			}
		}
	}

	public void WriteEmptySaveFile()
	{
		FileManager.WriteToFile(saveFilename, "");

	}
	public void SetNewGameData()
	{
		FileManager.WriteToFile(saveFilename, "");
		_playerInventory.Clear();

		SaveDataToDisk();

	}
	void SaveSettings()
	{
		//saveData.SaveSettings(_currentSettings);
	}
}
