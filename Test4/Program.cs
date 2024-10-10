using Test4.Model;

namespace Test4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test4\test2.json";
            string json = File.ReadAllText(path);
            NewJsonParser parser = new NewJsonParser();
            var products = parser.Parse<Product>(json);

            foreach (var product in products)
            {
                Console.WriteLine($"id: {product.id}");
                Console.WriteLine($"title: {product.title}");
                Console.WriteLine($"price: {product.price}");
                Console.WriteLine($"description: {product.description}");
                Console.WriteLine($"category: {product.category}");
                Console.WriteLine($"image: {product.image}");
                Console.WriteLine();
            }

            Console.ReadLine();
        }       
    }
}
