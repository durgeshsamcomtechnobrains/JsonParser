using System;
using System.Collections.Generic;
using System.Linq;
using Test9;
using Test9.Model;

class Program
{
    static void Main()
    {
        string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\test9\Jsonfiles\samplejson.json";
        string jsonread = File.ReadAllText(path);
        Console.WriteLine(jsonread);


        //Assuming the JSON is an array of products
        //List<Product> products = NewJsonhead.Deserialize<List<Product>>(jsonread);

        //foreach (var product in products)
        //{
        //    Console.WriteLine($"Id: {product.id}");
        //    Console.WriteLine($"Title: {product.title}");
        //    Console.WriteLine($"Price: {product.price}");
        //    Console.WriteLine($"Description: {product.description}");
        //    Console.WriteLine($"Category: {product.category}");
        //    Console.WriteLine($"Image: {product.image}");

        //    if (product.colors != null && product.colors.Length > 0)
        //    {
        //        Console.WriteLine($"Colors: {string.Join(", ", product.colors)}");
        //    }
        //    else
        //    {
        //        Console.WriteLine("Colors: None");
        //    }
        //    Console.WriteLine();
        //}

        List<Product2> products = NewJsonhead.Deserialize<List<Product2>>(jsonread);
        foreach (var product in products)
        {
            Console.WriteLine($"Id: {product.Pid}"); // Updated property
            Console.WriteLine($"Name: {product.Name}"); // Updated property
            Console.WriteLine($"Price: {product.Price}"); // Updated property
            Console.WriteLine($"Description: {product.Description}"); // Updated property
            Console.WriteLine($"GST: {product.Gst}"); // Added GST property

            // Print the city information
            if (product.City != null)
            {
                Console.WriteLine($"City: {product.City.Name}, Postal Code: {product.City.PostalCode}");
            }

            // Handle colors
            if (product.Colors != null && product.Colors.Count > 0)
            {
                Console.WriteLine($"Colors: {string.Join(", ", product.Colors)}");
            }
            else
            {
                Console.WriteLine("Colors: None");
            }

            // Print specifications
            if (product.Specification != null && product.Specification.Count > 0)
            {
                foreach (var spec in product.Specification)
                {
                    Console.WriteLine($"Specification ID: {spec.SpecId} - Name: {spec.Name} - Description: {spec.desciption}"); // Note: corrected property name
                }
            }
            else
            {
                Console.WriteLine("Specifications: None");
            }

            Console.WriteLine();
        }
        Console.ReadLine();
    }
}
