using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleJSON;

namespace TEDinc.Utils.Json
{
    public static class JsonHelper
    {
        public static T FromJson<T>(string json, Type type = null, JSONNode jsonNode = null)
        {
            if (type == null)
                type = typeof(T);
            if (type == typeof(string))
                return (T)(object)json;
            if (type == typeof(bool))
                return (T)(object)Convert.ToBoolean(json);

            if (jsonNode == null)
                jsonNode = JSON.Parse(json);
            if (jsonNode == null)
                jsonNode = JSON.Parse("{" + json + "}");
            if (jsonNode == null)
                Debug.LogError("[JH] parsing of json failed");

            if (string.IsNullOrEmpty(json) && (string.IsNullOrEmpty(jsonNode.ToString()) || jsonNode.ToString() == "{}"))
                return (T)Activator.CreateInstance(type);   

            Type[] genericArgs = type.GetGenericArguments();
            FieldInfo[] typeFields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


            if (typeof(ICollection).IsAssignableFrom(type))
                return CollectionFromJson();
            else if ((jsonNode.Count > 1 || typeFields.Length > 1) && !type.FullName.Contains(nameof(UnityEngine)))
                return ObjectFromJson();
            else if (genericArgs.Length == 0 && jsonNode.Count <= 1 && typeFields.Length <= 1)
            {
                object simpleParse = Convert.ChangeType(jsonNode[0].Value, type);
                if (type == simpleParse.GetType())
                    return (T)simpleParse;
            }
             
            return JsonUtility.FromJson<T>(json);


            T CollectionFromJson()
            {
                JSONArray jsonArray = jsonNode.AsArray;

                if (type.IsArray)
                {
                    T arr = (T)Activator.CreateInstance(type, new object[] { jsonArray.Count });
                    MethodInfo setValue = type.GetMethod(
                        nameof(Array.SetValue),
                        new Type[] { type.GetElementType(), typeof(int) });

                    for (int i = 0; i < jsonArray.Count; i++)
                    {
                        string item = jsonArray[i];
                        setValue.Invoke(arr, new object[] { FromJson<object>(item, type.GetElementType()), i });
                    }

                    return arr;
                }
                else
                {
                    T collection = (T)Activator.CreateInstance(type);
                    MethodInfo add = type.GetMethod("Add");
                    ParameterInfo[] addParams = add.GetParameters();

                    for (int itemIndex = 0; itemIndex < jsonArray.Count; itemIndex++)
                    {
                        object[] args = new object[genericArgs.Length];
                        JSONNode jsonItem = jsonArray[itemIndex];

                        if (args.Length == 1)
                        {
                            string jsonOfArg = jsonItem;
                            if (string.IsNullOrEmpty(jsonOfArg))
                                args[0] = FromJson<object>(jsonOfArg, genericArgs[0], jsonItem);
                            else
                                args[0] = FromJson<object>(jsonOfArg, genericArgs[0]);
                        }
                        else
                            for (int argIndex = 0; argIndex < args.Length; argIndex++)
                            {
                                string jsonOfArg = jsonItem[argIndex];
                                if (string.IsNullOrEmpty(jsonOfArg))
                                    args[argIndex] = FromJson<object>(jsonOfArg, genericArgs[argIndex], jsonItem[argIndex]);
                                else
                                    args[argIndex] = FromJson<object>(jsonOfArg, genericArgs[argIndex]);
                            }

                        add.Invoke(collection, args);
                    }
                    return collection;
                }
            }

            T ObjectFromJson()
            {
                T obj = (T)Activator.CreateInstance(type);

                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                {
                    string value = jsonNode[field.Name];
                    if (!string.IsNullOrEmpty(value))
                        field.SetValue(obj, FromJson<object>(value, field.FieldType));
                    else
                    {
                        JSONNode fieldJsonNode = jsonNode[field.Name];
                        if (fieldJsonNode != null)
                            field.SetValue(obj, FromJson<object>(value, field.FieldType, fieldJsonNode));
                    }
                }

                return obj;
            }
        }

        public static string ToJson<T>(T item, Type type = null)
        {
            if (type == null)
                type = typeof(T);
            if (item == null)
                item = (T)Activator.CreateInstance(type);


            if (type == typeof(string))
            {
                string strItem = item as string;
                if (strItem == null)
                    return null;
                if (strItem.StartsWith("\"") && strItem.EndsWith("\""))
                    return item as string;
                else
                    return "\"" + strItem + "\"";
            }
            else if (typeof(ICollection).IsAssignableFrom(type))
                return CollectionToJson(item as ICollection);
            else if (type.GetGenericArguments().Length > 0)
                return ObjWithGenericArgsToJson(item);

            string unityJson = JsonUtility.ToJson(item);
            if (string.IsNullOrEmpty(unityJson) || unityJson == "{}")
                return item.ToString();
            else
                return unityJson;



            string CollectionToJson<TCollection>(TCollection collection) where TCollection : ICollection
            {
                string json = "[\n\t";
                int i = 0;

                foreach (object elem in collection)
                    json += ToJsonOfGeneric(elem)
                        + (++i == collection.Count ? "\n" : ",\n\t");
                return json + "]";



                string ToJsonOfGeneric(object obj)
                {
                    Type objType = obj.GetType();
                    if (objType.GetGenericArguments().Length <= 1)
                        return ToJson(obj, objType);
                    else
                        return ObjWithGenericArgsToJson(obj);
                }
            }

            string ObjWithGenericArgsToJson(object obj)
            {
                Type[] argsTypes = type.GetGenericArguments();
                List<PropertyInfo> addedProperties = new List<PropertyInfo>();
                string json = "{";

                for (int i = 0; i < argsTypes.Length; i++)
                {
                    string argsName;
                    string argsValue = FindProperty(argsTypes[i], out argsName);

                    json += "\"" + argsName + "\":" + argsValue
                        + (i == argsTypes.Length - 1 ? "" : ",");
                }
                return json + "}";



                string FindProperty(Type propertyType, out string propertyName)
                {
                    foreach (PropertyInfo propertyInfo in obj.GetType().GetProperties())
                        if (propertyInfo.PropertyType == propertyType
                            && !addedProperties.Contains(propertyInfo))
                        {
                            addedProperties.Add(propertyInfo);
                            propertyName = propertyInfo.Name;
                            return ToJson(propertyInfo.GetValue(obj), propertyType);
                        }

                    propertyName = null;
                    return "";
                }
            }
        }
    }
}