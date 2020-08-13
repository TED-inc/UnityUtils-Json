using System;
using UnityEngine;
using SimpleJSON;

namespace TEDinc.Utils.Json
{
    public static class JsonArrayHelper
    {
        public static T[] FromJson<T>(string json)
        {
            JSONArray jsonArr = JSON.Parse(json).AsArray;
            T[] array = new T[jsonArr.Count];

            for (int i = 0; i < jsonArr.Count; i++)
                if (typeof(T) == typeof(string))
                    array[i] = (T)Convert.ChangeType(jsonArr[i].ToString(), typeof(T));
                else
                    array[i] = JsonUtility.FromJson<T>(jsonArr[i].ToString());
            
            return array;
        }

        public static string ToJson<T>(T[] array)
        {
            string json = "[";
            foreach (T item in array)
                if (typeof(T) == typeof(string))
                    json += item + ",";
                else
                    json += JsonUtility.ToJson(item) + ",";

            return json + "]";
        }
    }
}