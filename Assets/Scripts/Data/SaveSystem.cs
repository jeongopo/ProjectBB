using System;
using UnityEngine;

public static class SaveSystem
{
    private const string SaveFilename = "BBSaveData";

    public static bool TryLoad(out SaveData data)
    {
        return TryRead(SaveFilename, out data) || TryRead(SaveFilename + ".bak", out data);
    }

    public static bool Save(SaveData data)
    {
        return FileManager.WriteSave(SaveFilename, data.ToJson());
    }

    private static bool TryRead(string filename, out SaveData data)
    {
        data = null;
        if (!FileManager.TryReadSave(filename, out string json))
        {
            return false;
        }

        try
        {
            SaveData candidate = JsonUtility.FromJson<SaveData>(json);
            if (candidate != null && (candidate._currentDay > 0 || candidate.coin > 0 ||
                candidate._playerInventory?.Count > 0))
            {
                data = candidate;
                return true;
            }
        }
        catch (ArgumentException exception)
        {
            Debug.LogWarning($"[Save] {filename}: {exception.Message}");
        }

        return false;
    }
}
