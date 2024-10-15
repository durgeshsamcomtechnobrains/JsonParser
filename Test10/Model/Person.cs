using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test10.Model
{
    public class Person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public bool IsEmployed { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Age: {Age}, IsEmployed: {IsEmployed}";
        }
    }
}
