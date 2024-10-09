using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Test3.Model
{
    public class Walks
    {
        [JsonProperty("Id")]
        public int Id { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Distance")]
        public double Distance { get; set; }

        [JsonProperty("Duration")]
        public string Duration { get; set; }

        [JsonProperty("Difficulty")]
        public string Difficulty { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }
    }
}
