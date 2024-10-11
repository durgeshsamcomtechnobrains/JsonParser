using Test5.Model;

namespace Test5
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test5\Jsonfiles\test1.json";
            string json = File.ReadAllText(path);
            NewJsonParser parser = new NewJsonParser();
            var products = parser.Parse<Product>(json);

            foreach (var product in products)
            {
                Console.WriteLine($"Id: {product.Id}");
                Console.WriteLine($"Title: {product.Title}");
                Console.WriteLine($"Price: {product.Price}");
                Console.WriteLine($"Description: {product.Description}");
                Console.WriteLine($"Category: {product.Category}");
                Console.WriteLine($"Image: {product.Image}");
                Console.WriteLine();
            }

            Console.ReadLine();
        }
    }
}
