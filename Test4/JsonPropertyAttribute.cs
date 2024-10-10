using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test4
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class JsonPropertyAttribute : Attribute
    {
        public string Name { get; }

        public JsonPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}
