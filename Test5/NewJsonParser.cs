using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test5
{
    public class NewJsonParser
    {
        public List<T> Parse<T>(string json) where T : new()
        {
            List<T> items = new List<T>();
            bool insideString = false;
            bool insideObject = false;
            string currentObject = string.Empty;

            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == '"')
                {
                    insideString = !insideString;
                }

                if (json[i] == '{' && !insideString)
                {
                    insideObject = true;
                    currentObject += json[i];
                }
                else if (json[i] == '}' && !insideString)
                {
                    currentObject += json[i];
                    insideObject = false;
                    items.Add(CreateObjectFromJson<T>(currentObject));
                    currentObject = string.Empty; 
                }
                else if (insideObject)
                {
                    currentObject += json[i];
                }
            }

            return items;
        }

        private T CreateObjectFromJson<T>(string jsonObject) where T : new()
        {
            T obj = new T();
            jsonObject = jsonObject.Trim('{', '}');
            string[] keyValuePairs = jsonObject.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in keyValuePairs)
            {
                // Find the colon that separates the key from the value
                int colonIndex = pair.IndexOf(':');
                if (colonIndex >= 0)
                {
                    string key = pair.Substring(0, colonIndex).Trim().Trim('"');
                    string value = pair.Substring(colonIndex + 1).Trim();

                    // Use reflection to set the property values
                    var property = typeof(T).GetProperty(ToPascalCase(key));
                    if (property != null)
                    {
                        try
                        {
                            // Handle conversion of different types
                            object convertedValue = ConvertValue(value, property.PropertyType);
                            property.SetValue(obj, convertedValue);
                        }
                        catch
                        {
                            // Handle conversion errors if needed
                        }
                    }
                }
            }

            return obj;
        }

        private object ConvertValue(string value, Type type)
        {
            // Check for boolean values
            if (type == typeof(bool))
            {
                return value.ToLower() == "true";
            }

            // Check for integer values
            if (type == typeof(int))
            {
                return int.Parse(value);
            }

            // Check for decimal values
            if (type == typeof(decimal))
            {
                return decimal.Parse(value);
            }

            // If the type is a string, return the value as is (removing surrounding quotes)
            if (type == typeof(string))
            {
                return value.Trim('"');
            }

            return null; // Return null for unsupported types
        }

        private string ToPascalCase(string key)
        {
            // Converts the key to PascalCase
            if (string.IsNullOrEmpty(key)) return key;

            var parts = key.Split(new[] { '_' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = char.ToUpper(parts[i][0]) + parts[i].Substring(1).ToLower();
            }

            return string.Join(string.Empty, parts);
        }
    }
}
