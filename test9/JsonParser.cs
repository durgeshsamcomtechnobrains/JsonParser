using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

public class JsonParser
{    
    public List<T> Deserialize<T>(string json)
    {
        // Remove surrounding brackets for array
        json = json.Trim().TrimStart('[').TrimEnd(']');

        var objects = new List<T>();
        var entries = json.Split(new[] { "}," }, StringSplitOptions.None);

        foreach (var entry in entries)
        {
            var obj = Activator.CreateInstance<T>();
            var properties = entry.Trim().TrimEnd('}').TrimStart('{').Split(',');

            foreach (var property in properties)
            {
                var keyValue = property.Split(new[] { ':' }, 2).Select(x => x.Trim().Trim('"')).ToArray();
                if (keyValue.Length == 2)
                {
                    var propertyInfo = typeof(T).GetProperty(keyValue[0], BindingFlags.Public | BindingFlags.Instance);
                    if (propertyInfo != null)
                    {
                        // Handle array properties
                        if (propertyInfo.PropertyType.IsArray)
                        {
                            // Extract the array values
                            var arrayValues = keyValue[1].Trim().TrimStart('[').TrimEnd(']').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                                .Select(x => x.Trim().Trim('"')).ToArray();

                            // Create an array of the correct type and assign it
                            var array = Array.CreateInstance(propertyInfo.PropertyType.GetElementType(), arrayValues.Length);
                            for (int i = 0; i < arrayValues.Length; i++)
                            {
                                array.SetValue(Convert.ChangeType(arrayValues[i], propertyInfo.PropertyType.GetElementType()), i);
                            }
                            propertyInfo.SetValue(obj, array);
                        }
                        else
                        {
                            // Handle single values
                            var value = Convert.ChangeType(keyValue[1], propertyInfo.PropertyType);
                            propertyInfo.SetValue(obj, value);
                        }
                    }
                }
            }

            objects.Add(obj);
        }

        return objects;
    }
}