namespace Test10
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\test9\Jsonfiles\samplejson.json";
            string jsonread = File.ReadAllText(path);
            Console.WriteLine(jsonread);

            Console.ReadLine();
        }
    }
}
