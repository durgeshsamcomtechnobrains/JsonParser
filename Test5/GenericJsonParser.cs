using System;
using System.Collections.Generic;
using System.Linq;

public class GenericJsonParser
{
    public T ParseJson<T>(string json) where T : new()
    {
        var type = typeof(T);
        var obj = new T();

        // Remove the surrounding brackets and trim whitespace
        json = json.TrimStart('[').TrimEnd(']');

        // Split the JSON into key-value pairs
        var properties = json.Split(',')
                             .Select(p => p.Split(new[] { ':' }, 2))
                             .Select(kv => new { Key = kv[0].Trim().Trim('"'), Value = kv.Length > 1 ? kv[1].Trim() : "" });

        foreach (var property in properties)
        {
            // Get the property info
            var propInfo = type.GetProperty(property.Key);

            if (propInfo != null)
            {
                // Handle nested objects or arrays
                if (propInfo.PropertyType.IsGenericType && propInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // Parse array
                    var listType = propInfo.PropertyType.GenericTypeArguments[0];
                    var itemsJson = property.Value.TrimStart('[').TrimEnd(']');

                    if (!string.IsNullOrWhiteSpace(itemsJson))
                    {
                        var items = itemsJson.Split(new[] { "},{" }, StringSplitOptions.None)
                                              .Select(item => ParseJson(item, listType));
                        var list = (IList<object>)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                        foreach (var item in items)
                        {
                            list.Add(item);
                        }
                        propInfo.SetValue(obj, list);
                    }
                    else
                    {
                        propInfo.SetValue(obj, Activator.CreateInstance(propInfo.PropertyType));
                    }
                }
                else if (propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string))
                {
                    // Parse nested object
                    var nestedObj = ParseJson(property.Value.Trim('{', '}'), propInfo.PropertyType);
                    propInfo.SetValue(obj, nestedObj);
                }
                else
                {
                    // Handle primitive types
                    var value = Convert.ChangeType(property.Value.Trim('"'), propInfo.PropertyType);
                    propInfo.SetValue(obj, value);
                }
            }
        }

        return obj;
    }

    private object ParseJson(string json, Type type)
    {
        var obj = Activator.CreateInstance(type);
        var properties = json.Split(',')
                             .Select(p => p.Split(new[] { ':' }, 2))
                             .Select(kv => new { Key = kv[0].Trim().Trim('"'), Value = kv.Length > 1 ? kv[1].Trim() : "" });

        foreach (var property in properties)
        {
            var propInfo = type.GetProperty(property.Key);

            if (propInfo != null)
            {
                // Handle nested objects or arrays
                if (propInfo.PropertyType.IsGenericType && propInfo.PropertyType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    // Parse array
                    var listType = propInfo.PropertyType.GenericTypeArguments[0];
                    var itemsJson = property.Value.TrimStart('[').TrimEnd(']');

                    if (!string.IsNullOrWhiteSpace(itemsJson))
                    {
                        var items = itemsJson.Split(new[] { "},{" }, StringSplitOptions.None)
                                              .Select(item => ParseJson(item, listType));
                        var list = (IList<object>)Activator.CreateInstance(typeof(List<>).MakeGenericType(listType));
                        foreach (var item in items)
                        {
                            list.Add(item);
                        }
                        propInfo.SetValue(obj, list);
                    }
                    else
                    {
                        propInfo.SetValue(obj, Activator.CreateInstance(propInfo.PropertyType));
                    }
                }
                else if (propInfo.PropertyType.IsClass && propInfo.PropertyType != typeof(string))
                {
                    // Parse nested object
                    var nestedObj = ParseJson(property.Value.Trim('{', '}'), propInfo.PropertyType);
                    propInfo.SetValue(obj, nestedObj);
                }
                else
                {
                    // Handle primitive types
                    var value = Convert.ChangeType(property.Value.Trim('"'), propInfo.PropertyType);
                    propInfo.SetValue(obj, value);
                }
            }
        }

        return obj;
    }
}
