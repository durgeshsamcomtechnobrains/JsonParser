using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test3
{
    [AttributeUsage(AttributeTargets.Property)]
    public class JsonPropertyAttribute : Attribute
    {
        public string Key { get; }
        public JsonPropertyAttribute(string key)
        {
            Key = key;
        }
    }
}
