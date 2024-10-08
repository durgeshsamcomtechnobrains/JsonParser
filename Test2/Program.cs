namespace Test2
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string path = @"D:\DotNetTeamBackup\Durgesh\MAUI Learning\JSON_Parser\JsonParser\Test2\test.json";
            string json;
            json = File.ReadAllText(path);
            Console.WriteLine("JSON Content:");
            Console.WriteLine(json);

        }        
    }
}
