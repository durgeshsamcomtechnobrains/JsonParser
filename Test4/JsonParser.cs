using System;
using System.Collections.Generic;
using System.Reflection;

namespace Test4
{
    public class JsonParser
    {
        public List<T> Parse<T>(string json) where T : new()
        {
            json = json.Trim();

            // Check for JSON array or single object
            if (json.StartsWith("[") && json.EndsWith("]"))
            {
                return ParseArray<T>(json);
            }
            else if (json.StartsWith("{") && json.EndsWith("}"))
            {
                T obj = ParseJsonObject<T>(json);                
                return new List<T> { obj }; // Return a list containing a single object
            }
            else
            {
                throw new ArgumentException("Invalid JSON format. Expected a JSON array or object.");
            }
        }

        private T ParseJsonObject<T>(string json) where T : new()        
        {
            T obj = new T();
            var properties = typeof(T).GetProperties();

            var dict = ParseJsonToDictionary(json);

            foreach (var property in properties)
            {
                string jsonKey = property.Name; // Use property name directly (case-sensitive)
                if (dict.ContainsKey(jsonKey))
                {
                    var value = dict[jsonKey];
                    SetPropertyValue(obj, property, value);
                }
            }

            return obj;
        }

        private List<T> ParseArray<T>(string json) where T : new()
        {
            json = json.TrimStart('[').TrimEnd(']');
            var elements = new List<T>();
            string[] arrayItems = json.Split(new[] { "},{" }, StringSplitOptions.None);

            foreach (var item in arrayItems)
            {
                var formattedItem = "{" + item.TrimStart('{').TrimEnd('}') + "}";
                elements.Add(ParseJsonObject<T>(formattedItem));
            }

            return elements;
        }

        private Dictionary<string, object> ParseJsonToDictionary(string json)
        {
            var dict = new Dictionary<string, object>();

            // Assume a simple flat key-value JSON object
            string[] keyValuePairs = json.TrimStart('{').TrimEnd('}').Split(',');

            foreach (var kvp in keyValuePairs)
            {
                var pair = kvp.Split(':');
                if (pair.Length == 2)
                {
                    string key = pair[0].Trim().Trim('\"');
                    string value = pair[1].Trim().Trim('\"');
                    dict[key] = value;
                }
            }

            return dict;
        }

        private void SetPropertyValue(object obj, PropertyInfo property, object value)
        {
            if (value == null) return;

            try
            {
                // Check property type and assign value accordingly
                if (property.PropertyType == typeof(int))
                    property.SetValue(obj, int.Parse(value.ToString()));
                else if (property.PropertyType == typeof(decimal))
                    property.SetValue(obj, decimal.Parse(value.ToString()));
                else if (property.PropertyType == typeof(bool))
                    property.SetValue(obj, bool.Parse(value.ToString()));
                else if (property.PropertyType == typeof(double))
                    property.SetValue(obj, double.Parse(value.ToString()));
                else
                    property.SetValue(obj, value.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error setting property '{property.Name}': {ex.Message}");
            }
        }
    }
}
