using System;
using System.IO;
using UnityEngine;

public static class FileManager
{
    public static bool TryReadSave(string filename, out string json)
    {
        json = null;
        string path = Path.Combine(Application.persistentDataPath, filename);
        if (!File.Exists(path))
        {
            path = Path.Combine(Application.dataPath, filename);
        }

        try
        {
            if (File.Exists(path))
            {
                json = File.ReadAllText(path);
            }

            return !string.IsNullOrWhiteSpace(json);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.LogWarning($"[Save] {path}: {exception.Message}");
            return false;
        }
    }

    public static bool WriteSave(string filename, string json)
    {
        string path = Path.Combine(Application.persistentDataPath, filename);
        string temporary = path + ".tmp";
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(temporary, json);
            if (File.Exists(path))
            {
                File.Replace(temporary, path, path + ".bak");
            }
            else
            {
                File.Move(temporary, path);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Debug.LogError($"[Save] {path}: {exception.Message}");
            return false;
        }
    }
}
