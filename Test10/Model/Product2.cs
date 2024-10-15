using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test10.Model
{
    public class Product2
    {
        public int Pid { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; }
        public List<string> Colors { get; set; }
        public List<Specification> Specification { get; set; }
        public string Gst { get; set; }
        public City City { get; set; }
    }
}
