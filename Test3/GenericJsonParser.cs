using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Test3
{
    public class GenericJsonParser
    {
        // Generic method to parse JSON into any model class
        public static List<T> ParseJson<T>(string json) where T : new()
        {
            List<T> resultList = new List<T>();

            // Remove the brackets and split by "},{"
            json = json.Trim('[', ']'); // Removing the array brackets
            string[] entries = json.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                // Create a new instance of T (generic model)
                T obj = new T();

                // Clean up the entry
                string cleanedEntry = entry.Replace("{", "").Replace("}", "").Trim();

                // Split the entry by commas to get the key-value pairs
                string[] keyValuePairs = cleanedEntry.Split(',');

                foreach (var pair in keyValuePairs)
                {
                    // Split each pair by colon (":") to get the key and value
                    string[] keyValue = pair.Split(':');
                    string key = keyValue[0].Trim(' ', '"'); // Remove spaces and quotes from key
                    string value = keyValue[1].Trim(' ', '"'); // Remove spaces and quotes from value

                    // Use reflection to map the JSON key to the class property
                    SetPropertyValue(obj, key, value);
                }

                resultList.Add(obj);
            }

            return resultList;
        }

        // Method to set the property value using reflection
        private static void SetPropertyValue<T>(T obj, string key, string value)
        {
            // Get all properties of the class
            var properties = typeof(T).GetProperties();

            foreach (var prop in properties)
            {
                // Check if the property has the JsonPropertyAttribute
                var attribute = prop.GetCustomAttribute<JsonPropertyAttribute>();
                if (attribute != null && attribute.Key == key)
                {
                    // Set the value of the property based on its type
                    if (prop.PropertyType == typeof(int))
                    {
                        prop.SetValue(obj, int.Parse(value));
                    }
                    else if (prop.PropertyType == typeof(double))
                    {
                        prop.SetValue(obj, double.Parse(value));
                    }
                    else
                    {
                        prop.SetValue(obj, value);
                    }
                    break;
                }
            }
        }
    }
}
