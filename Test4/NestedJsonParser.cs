using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test4
{
    public class NestedJsonParser
    {
        public List<T> Parse<T>(string json) where T : new()
        {
            List<T> items = new List<T>();
            bool insideString = false;
            int braceCount = 0;
            string currentObject = string.Empty;

            for (int i = 0; i < json.Length; i++)
            {
                if (json[i] == '"')
                {
                    insideString = !insideString;
                }

                if (json[i] == '{' && !insideString)
                {
                    braceCount++;
                    currentObject += json[i]; // Start capturing the new object
                }
                else if (json[i] == '}' && !insideString)
                {
                    currentObject += json[i]; // Add closing brace
                    braceCount--;

                    if (braceCount == 0)
                    {
                        // Parse the current object and add to the list
                        T item = CreateObjectFromJson<T>(currentObject);
                        if (item != null)
                        {
                            items.Add(item);
                        }
                        currentObject = string.Empty; // Reset for the next object
                    }
                }
                else if (braceCount > 0)
                {
                    currentObject += json[i]; // Add character to current object
                }
            }

            return items;
        }

        private T CreateObjectFromJson<T>(string jsonObject) where T : new()
        {
            T obj = new T();
            jsonObject = jsonObject.Trim('{', '}');
            string[] keyValuePairs = SplitKeyValuePairs(jsonObject);

            foreach (var pair in keyValuePairs)
            {
                int colonIndex = pair.IndexOf(':');
                if (colonIndex >= 0)
                {
                    string key = pair.Substring(0, colonIndex).Trim().Trim('"');
                    string value = pair.Substring(colonIndex + 1).Trim();

                    // Handle nested objects
                    if (value.StartsWith("{") && value.EndsWith("}"))
                    {
                        // Recursive call to handle nested objects like Rating
                        var nestedObject = CreateObjectFromJson<object>(value);
                        SetProperty(obj, key, nestedObject);
                    }
                    else
                    {
                        value = value.Trim('"');
                        SetProperty(obj, key, value);
                    }
                }
            }

            return obj;
        }

        private string[] SplitKeyValuePairs(string jsonObject)
        {
            // Handle splitting by commas while respecting nested objects
            List<string> keyValuePairs = new List<string>();
            bool insideString = false;
            int braceCount = 0;
            string currentPair = string.Empty;

            for (int i = 0; i < jsonObject.Length; i++)
            {
                if (jsonObject[i] == '"')
                {
                    insideString = !insideString;
                }

                if (jsonObject[i] == '{')
                {
                    braceCount++;
                }
                else if (jsonObject[i] == '}')
                {
                    braceCount--;
                }

                if (jsonObject[i] == ',' && braceCount == 0 && !insideString)
                {
                    keyValuePairs.Add(currentPair);
                    currentPair = string.Empty;
                }
                else
                {
                    currentPair += jsonObject[i];
                }
            }

            if (!string.IsNullOrEmpty(currentPair))
            {
                keyValuePairs.Add(currentPair);
            }

            return keyValuePairs.ToArray();
        }

        private void SetProperty<T>(T obj, string propertyName, object value)
        {
            var property = typeof(T).GetProperty(propertyName);
            if (property != null)
            {
                try
                {
                    // Convert value to appropriate type and set the property
                    object convertedValue = Convert.ChangeType(value, property.PropertyType);
                    property.SetValue(obj, convertedValue);
                }
                catch
                {
                    // Handle conversion errors if necessary
                }
            }
        }
    }
}
