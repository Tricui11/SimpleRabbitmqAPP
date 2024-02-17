namespace FileParserService {
  class Program {
    static void Main(string[] args) {
      string directoryPath = @"C:\Users\Furer\Downloads\XMLData";
      RabbitMQClient rabbitMQClient = new();
      ConsoleLogger logger = new();

      FileParser fileParser = new(directoryPath, rabbitMQClient, logger);
      fileParser.Start();

      Console.WriteLine("FileParserService is running. Press any key to complete.");
      Console.ReadKey();
    }
  }
}