using Test4.Model;

namespace Test4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test4\Jsonfiles\test5.json";
            string json = File.ReadAllText(path);
            //NewJsonParser parser = new NewJsonParser();
            NestedJsonParser nestedParser = new NestedJsonParser();
            var products = nestedParser.Parse<Product>(json);

            foreach (var product in products)
            {
                Console.WriteLine($"id: {product.id}");
                Console.WriteLine($"title: {product.title}");
                Console.WriteLine($"price: {product.price}");
                Console.WriteLine($"description: {product.description}");
                Console.WriteLine($"category: {product.category}");
                Console.WriteLine($"image: {product.image}");
                if (product.rating != null)
                {
                    Console.WriteLine($"rating: {product.rating.rate}");
                    Console.WriteLine($"rating count: {product.rating.count}");
                }

                Console.WriteLine();
            }

            Console.ReadLine();
        }       
    }
}
