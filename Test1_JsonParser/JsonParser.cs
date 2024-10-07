using System;
using System.Collections.Generic;
using System.Reflection;

namespace Test1_JsonParser
{
    public class JsonParser
    {
        public List<T> Parse<T>(string jsonString) where T : new()
        {
            // Trim the JSON string and remove the enclosing array brackets
            jsonString = jsonString.Trim().TrimStart('[').TrimEnd(']');

            // Split the JSON string into object strings
            string[] objectStrings = jsonString.Split(new[] { "},{" }, StringSplitOptions.RemoveEmptyEntries);
            List<T> result = new List<T>();

            foreach (var objectString in objectStrings)
            {
                // Clean and parse each object string
                string cleanedObjectString = objectString.Trim().Trim('{', '}');
                T obj = ParseObject<T>(cleanedObjectString);
                result.Add(obj); // Add each parsed object to the list
            }

            return result;
        }

        private T ParseObject<T>(string objectString) where T : new()
        {
            T obj = new T();
            string[] keyValuePairs = objectString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var keyValue in keyValuePairs)
            {
                string[] splitPair = keyValue.Split(new[] { ':' }, 2);
                if (splitPair.Length != 2) continue;

                string key = splitPair[0].Trim().Trim('"');
                string value = splitPair[1].Trim().Trim('"');

                // Clean the value from unwanted characters
                value = value.TrimEnd('}', '\n', '\r', ' ');

                SetPropertyValue(obj, key, value);
            }

            return obj;
        }

        private void SetPropertyValue<T>(T obj, string propertyName, string value)
        {
            PropertyInfo prop = typeof(T).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop == null) return;

            try
            {
                if (prop.PropertyType == typeof(int))
                    prop.SetValue(obj, int.Parse(value));
                else if (prop.PropertyType == typeof(double))
                    prop.SetValue(obj, double.Parse(value));
                else if (prop.PropertyType == typeof(string))
                    prop.SetValue(obj, value);
                else if (prop.PropertyType == typeof(bool))
                    prop.SetValue(obj, bool.Parse(value));
            }
            catch (FormatException ex)
            {
                Console.WriteLine($"Error parsing value '{value}' for property '{propertyName}': {ex.Message}");
            }
        }
    }
}
