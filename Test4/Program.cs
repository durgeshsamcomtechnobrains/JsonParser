using Test4.Model;

namespace Test4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test4\test2.json";
            string json = File.ReadAllText(path);
            JsonParser parser = new JsonParser();
            var products = parser.Parse<Product>(json);

            var walksJson = json.Substring(json.IndexOf("\"walks\":") + "\"walks\":".Length).Trim();
            //JsonParser parser = new JsonParser();
            //List<Walk> walksList = parser.Parse<Walk>(walksJson);            

            foreach (var product in products)
            {
                Console.WriteLine($"Id: {product.id}");
                Console.WriteLine($"Title: {product.title}");
                Console.WriteLine($"Price: {product.price}");
                Console.WriteLine($"Description: {product.description}");
                Console.WriteLine($"Category: {product.category}");
                Console.WriteLine($"Description: {product.image}");
                Console.WriteLine();
            }
            Console.ReadLine();
        }
    }
}
