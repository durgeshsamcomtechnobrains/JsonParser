using Test3.Model;

namespace Test3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test3\test.json";
            string json;
            json = File.ReadAllText(path);
            Console.WriteLine("JSON Content:");
            Console.WriteLine(json);



            List<Walks> walksList = GenericJsonParser.ParseJson<Walks>(json);

            // Print the parsed data
            Console.WriteLine("\nParsed Walks data:");
            foreach (var walk in walksList)
            {
                Console.WriteLine($"ID: {walk.Id}, Name: {walk.Name}, Distance: {walk.Distance}, Duration: {walk.Duration}, Difficulty: {walk.Difficulty}, Description: {walk.Description}");
            }

            Console.ReadLine();
        }
    }
}
