using UnityEngine;
using System.Collections;
using System;
using System.IO;

public class JsonHelper
{

    public static T[] FromJson<T>(string json)
    {
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(json);
        return wrapper.Items;
    }

    public static string ToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.Items = array;
        return JsonUtility.ToJson(wrapper);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] Items;
    }

    public static void SaveClass<T>(T data, string name)
    {
        string path = Application.dataPath + "/JsonFiles/" + name + ".json";
        string terrain_noise_data = JsonUtility.ToJson(data);

        File.WriteAllText(path, terrain_noise_data);

        Debug.Log(name + " saved at: " + path);
    }

    public static T LoadClass<T>(string name)
    {
        string path = Application.dataPath + "/JsonFiles/" + name + ".json";
        string json = File.ReadAllText(path);

        T data = JsonUtility.FromJson<T>(json);

        Debug.Log(name + " loaded from: " + path);

        return data;
    }

    public static void SaveClassArray<T>(T[] data, string name)
    {
        string path = Application.dataPath + "/JsonFiles/" + name + ".json";
        string terrain_noise_data = ToJson(data);

        File.WriteAllText(path, terrain_noise_data);

        Debug.Log(name + " saved to: " + path);
    }

    public static T[] LoadClassArray<T>(string name)
    {
        string path = Application.dataPath + "/JsonFiles/" + name + ".json";
        string json = File.ReadAllText(path);

        T[] data = FromJson<T>(json);

        Debug.Log(name + " loaded from: " + path);

        return data;
    }
}
