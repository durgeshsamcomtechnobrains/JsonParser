namespace Test4
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string json = "[{\"Id\":1,\"Name\":\"Item 1\",\"IsActive\":true},{\"Id\":2,\"Name\":\"Item 2\",\"IsActive\":false}]";

            JsonParser parser = new JsonParser();
            List<ABC> abcList = parser.Parse<ABC>(json);

            foreach (var item in abcList)
            {
                Console.WriteLine($"Id: {item.Id}, Name: {item.Name}, IsActive: {item.IsActive}");
            }
        }
    }
}
