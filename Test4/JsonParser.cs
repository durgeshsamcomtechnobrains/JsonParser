using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test4
{
    public class JsonParser
    {
        public List<T> Parse<T>(string json) where T : new()
        {            
            // Check for the array structure and remove unnecessary characters
            if (json.StartsWith("[") && json.EndsWith("]"))
            {
                json = json[1..^1]; // Trim the first and last character
            }
            else if (json.Contains("\"") && !json.StartsWith("{") && !json.EndsWith("}"))
            {
                throw new ArgumentException("Invalid JSON format: Expected an array.");
            }

            List<T> list = new List<T>();
            
            // Split the JSON string by "},{" to get individual objects
            string[] jsonObjects = json.Split(new[] { "},{" }, StringSplitOptions.None);

            foreach (var jsonObject in jsonObjects)
            {
                // Ensure to wrap the object again to make it a valid JSON object
                string jsonObjectWithBraces = "{" + jsonObject.Trim().TrimStart('{').TrimEnd('}') + "}";

                T obj = new T();
                var properties = typeof(T).GetProperties();

                foreach (var property in properties)
                {
                    // Find the value for each property
                    string propertyName = property.Name.ToLower();
                    string valueString = GetValueFromJson(jsonObjectWithBraces, propertyName);

                    if (!string.IsNullOrEmpty(valueString))
                    {
                        // Type casting
                        object value = Convert.ChangeType(valueString, property.PropertyType);
                        property.SetValue(obj, value);
                    }
                }

                list.Add(obj);
            }

            return list;
        }

        private string GetValueFromJson(string jsonObject, string propertyName)
        {
            string searchString = $"\"{propertyName}\"";
            int startIndex = jsonObject.IndexOf(searchString, StringComparison.OrdinalIgnoreCase);

            if (startIndex < 0) return null;

            // Find the start of the value
            startIndex += searchString.Length + 1; // Skip to the start of the value
            int endIndex = jsonObject.IndexOfAny(new[] { ',', '}' }, startIndex);

            // Extract the value string
            return jsonObject[startIndex..endIndex].Trim().Trim('\"');
        }
    }
}