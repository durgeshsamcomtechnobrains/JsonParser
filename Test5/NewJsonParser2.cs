using System;
using System.Collections.Generic;
using System.Reflection;

namespace Test5
{
    public class NewJsonParser2
    {
        public List<T> Parse<T>(string json) where T : new()
        {
            // The JSON could be either an object or an array.
            // For simplicity, we'll start parsing it as an object or array without string manipulation.
            object jsonObject = DeserializeJson(json);

            // If the parsed JSON is an array, we convert it to a list of T.
            if (jsonObject is List<Dictionary<string, object>> jsonArray)
            {
                List<T> items = new List<T>();
                foreach (var dict in jsonArray)
                {
                    items.Add(CreateObjectFromDictionary<T>(dict));
                }
                return items;
            }
            // If the parsed JSON is a single object, create a list with that object.
            else if (jsonObject is Dictionary<string, object> jsonObjectDict)
            {
                return new List<T> { CreateObjectFromDictionary<T>(jsonObjectDict) };
            }

            return new List<T>(); // Return an empty list if the input JSON is not valid.
        }

        private T CreateObjectFromDictionary<T>(Dictionary<string, object> dict) where T : new()
        {
            T obj = new T();
            foreach (var kvp in dict)
            {
                // Use reflection to set property values
                var property = typeof(T).GetProperty(ToPascalCase(kvp.Key));
                if (property != null)
                {
                    try
                    {
                        // Convert the value from object to the property type
                        object convertedValue = Convert.ChangeType(kvp.Value, property.PropertyType);
                        property.SetValue(obj, convertedValue);
                    }
                    catch
                    {
                        // Handle conversion errors if needed
                    }
                }
            }
            return obj;
        }

        private object DeserializeJson(string json)
        {
            // This method would parse the JSON and convert it into a usable C# structure
            // As we are not using string manipulation or libraries, we will assume a simple structure for demonstration purposes.

            // Here is a basic implementation for parsing:
            // In a real-world scenario, you would replace this with a proper JSON parsing logic.

            if (json.StartsWith("[") && json.EndsWith("]")) // JSON array
            {
                // Assume that we get a list of dictionaries (for each JSON object in the array)
                return ParseJsonArray(json);
            }
            else if (json.StartsWith("{") && json.EndsWith("}")) // JSON object
            {
                // Assume that we get a dictionary representing the JSON object
                return ParseJsonObject(json);
            }

            return null; // Invalid JSON
        }

        private List<Dictionary<string, object>> ParseJsonArray(string json)
        {
            // Implement logic to parse JSON array here
            // For demonstration purposes, you can return a dummy list of dictionaries
            return new List<Dictionary<string, object>>
            {
                new Dictionary<string, object> { { "id", 1 }, { "title", "Sample Product" }, { "price", 29.99 }, { "description", "Description here"}, { "category", "Sample Category" }, { "image", "image-url.jpg" } }
            };
        }

        private Dictionary<string, object> ParseJsonObject(string json)
        {
            // Implement logic to parse JSON object here
            // For demonstration purposes, you can return a dummy dictionary
            return new Dictionary<string, object> { { "id", 1 }, { "title", "Sample Product" }, { "price", 29.99 }, { "description", "Description here" }, { "category", "Sample Category" }, { "image", "image-url.jpg" } };
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
