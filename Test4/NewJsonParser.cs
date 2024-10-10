using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test4
{
    public class NewJsonParser
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
            string[] keyValuePairs = jsonObject.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var pair in keyValuePairs)
            {
                int colonIndex = pair.IndexOf(':');
                if (colonIndex >= 0)
                {
                    string key = pair.Substring(0, colonIndex).Trim().Trim('"');
                    string value = pair.Substring(colonIndex + 1).Trim().Trim('"');

                    // Use reflection to set the property values
                    var property = typeof(T).GetProperty(key);
                    if (property != null)
                    {
                        try
                        {
                            // Convert the value to the appropriate type
                            object convertedValue = Convert.ChangeType(value, property.PropertyType);
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
    }
}
