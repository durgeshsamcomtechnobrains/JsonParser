using Test4.Model;

namespace Test4
{
    internal class Program
    {
        static void Main(string[] args)
        {
        //    string json = @"
        //{
        //    ""walks"": [
        //        {
        //            ""id"": 1,
        //            ""name"": ""River Walk"",
        //            ""distance"": 5.5,
        //            ""duration"": ""2 hours"",
        //            ""difficulty"": ""Easy"",
        //            ""description"": ""A scenic walk along the river with beautiful views.""
        //        },
        //        {
        //            ""id"": 2,
        //            ""name"": ""Mountain Trail"",
        //            ""distance"": 10.2,
        //            ""duration"": ""4 hours"",
        //            ""difficulty"": ""Moderate"",
        //            ""description"": ""A challenging trail through the mountains.""
        //        },
        //        {
        //            ""id"": 3,
        //            ""name"": ""Forest Loop"",
        //            ""distance"": 3.8,
        //            ""duration"": ""1 hour"",
        //            ""difficulty"": ""Easy"",
        //            ""description"": ""A peaceful walk through the forest.""
        //        }
        //    ]
        //}";
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test3\test.json";
            string json = File.ReadAllText(path);

            var walksJson = json.Substring(json.IndexOf("\"walks\":") + "\"walks\":".Length).Trim();
            JsonParser parser = new JsonParser();
            List<Walk> walksList = parser.Parse<Walk>(walksJson);            

            foreach (var walk in walksList)
            {
                Console.WriteLine($"Id: {walk.Id}");
                Console.WriteLine($"Name: {walk.Name}");
                Console.WriteLine($"Distance: {walk.Distance}");
                Console.WriteLine($"Duration: {walk.Duration}");
                Console.WriteLine($"Difficulty: {walk.Difficulty}");
                Console.WriteLine($"Description: {walk.Description}");
                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}
