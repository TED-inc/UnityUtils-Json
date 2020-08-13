using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace TEDinc.Utils.Json
{
    public static class JsonDictionaryHelper
    {
        public static string ToJson<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            string json = "[";
            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
            {
                if (typeof(TKey) == typeof(string))
                    json += pair.Key;
                else
                    json += JsonUtility.ToJson(pair.Key);

                json += ":";

                if (typeof(TValue) == typeof(string))
                    json += pair.Value;
                else
                    json += JsonUtility.ToJson(pair.Value);

                json += ",";
            }

            return json + "]";
        }
    }
}
