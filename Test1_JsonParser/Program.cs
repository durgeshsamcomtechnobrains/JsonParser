using System;
using System.Collections.Generic;
using System.IO;
using Test1_JsonParser.Model;

namespace Test1_JsonParser
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test1_JsonParser\test.json";
            string json;

            try
            {
                json = File.ReadAllText(path);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading file: {ex.Message}");
                return;
            }

            JsonParser parser = new JsonParser();

            try
            {
                List<Walk> walksList = parser.Parse<Walk>(json);
                foreach (var walk in walksList)
                {
                    Console.WriteLine($"Walk: {walk.Name}, Distance: {walk.Distance}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing JSON: {ex.Message}");
            }

            Console.ReadLine();
        }
    }
}
