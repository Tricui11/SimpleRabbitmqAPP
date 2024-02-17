using CommonLibrary.Logging;
using CommonLibrary.Settings;
using Microsoft.Extensions.Configuration;

namespace FileParserService {
  class Program {
    static void Main() {
      var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
      var config = configuration.Build();
      var section = config.GetSection("RabbitMQSettings");
      var rabbitMQSettings = section.Get<RabbitMQSettings>();
      string dataDirectoryPath = config["DataDirectoryPath"];
      ConsoleLogger logger = new();

      FileParser fileParser = new(rabbitMQSettings, logger, dataDirectoryPath);
      fileParser.Start();

      Console.WriteLine("FileParserService is running. Press any key to complete.");
      Console.ReadKey();
    }
  }
}