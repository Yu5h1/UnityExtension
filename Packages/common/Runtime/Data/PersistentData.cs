using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public abstract class PersistentData
{
    protected static string GetPath(string fileName) =>
        Path.Combine(Application.persistentDataPath, fileName + ".json");

    public void Save(string fileName)
    {
        try
        {
            File.WriteAllText(GetPath(fileName), JsonUtility.ToJson(this));
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Save failed: {e.Message}");
        }
    }

    public static bool TryLoad<T>(string fileName,out T data) where T : PersistentData, new()
    {
        data = default(T);
        try
        {
            if (File.Exists(GetPath(fileName)))
            {
                data = JsonUtility.FromJson<T>(File.ReadAllText(GetPath(fileName)));
                return true;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Load failed: {e.Message}");
        }
        return false;
    }

    public static bool Exists(string fileName) => File.Exists(GetPath(fileName));
}
