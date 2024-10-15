using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test5.Model
{
    public class Product2
    {
        public int Pid { get; set; } // Maps to "pid"
        public string Name { get; set; } // Maps to "name"
        public string Description { get; set; } // Maps to "description"
        public decimal Price { get; set; } // Maps to "price" (use decimal for currency)
        public string Currency { get; set; } // Maps to "currency"
        public List<string> Colors { get; set; } // Maps to "colors"
        public List<Specification> Specification { get; set; } // Maps to "specification"
        public string Gst { get; set; } // Maps to "gst"
        public City City { get; set; } // Maps to "city"
    }
}
