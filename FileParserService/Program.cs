using LoggingLibrary;

namespace FileParserService {
  class Program {
    static void Main(string[] args) {
      string directoryPath = @"C:\Users\Furer\Downloads\XMLData";
      string queueName = "YourQueueName";
      string connectionString = "localhost";
      RabbitMQClient rabbitMQClient = new(queueName, connectionString);
      ConsoleLogger logger = new();

      FileParser fileParser = new(directoryPath, rabbitMQClient, logger);
      fileParser.Start();

      Console.WriteLine("FileParserService is running. Press any key to complete.");
      Console.ReadKey();
    }
  }
}