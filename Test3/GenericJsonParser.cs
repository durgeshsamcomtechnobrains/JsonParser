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

            // Clean up the JSON to make parsing easier (remove unnecessary whitespace and new lines)
            json = json.Replace("\r", "").Replace("\n", "").Trim('[', ']');
            
            string[] entries = json.Split(new string[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                // Add curly braces back to the entry if they were removed during splitting
                string cleanedEntry = "{" + entry.Trim('{', '}') + "}";

                // Parse the cleaned entry into an object of type T
                T obj = ParseSingleObject<T>(cleanedEntry);

                resultList.Add(obj);
            }

            return resultList;
        }

        // Method to parse a single JSON object into an instance of T
        private static T ParseSingleObject<T>(string json) where T : new()
        {
            T obj = new T();

            // Remove curly braces and split the JSON into key-value pairs
            string[] keyValuePairs = json.Trim('{', '}').Split(new string[] { "\",\"", "\":\"", "\", \"" }, StringSplitOptions.None);

            foreach (var pair in keyValuePairs)
            {
                // Each key-value pair is split by ":"
                string[] keyValue = pair.Split(new char[] { ':' }, 2);
                if (keyValue.Length == 2)
                {
                    string key = keyValue[0].Trim(' ', '"');  // Remove spaces and quotes from key
                    string value = keyValue[1].Trim(' ', '"');  // Remove spaces and quotes from value

                    // Use reflection to map the JSON key to the class property
                    SetPropertyValue(obj, key, value);
                }
            }

            return obj;
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
                        if (int.TryParse(value, out int intValue))
                        {
                            prop.SetValue(obj, intValue);
                        }
                    }
                    else if (prop.PropertyType == typeof(double))
                    {
                        if (double.TryParse(value, out double doubleValue))
                        {
                            prop.SetValue(obj, doubleValue);
                        }
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
