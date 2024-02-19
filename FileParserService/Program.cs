using CommonLibrary.Logging;
using CommonLibrary.Settings;
using Microsoft.Extensions.Configuration;

namespace FileParserService {
  class Program {
    static async Task Main() {
      var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
      var config = configuration.Build();
      var section = config.GetSection("RabbitMQSettings");
      var rabbitMQSettings = section.Get<RabbitMQSettings>();
      string dataDirectoryPath = config["DataDirectoryPath"];
      ConsoleLogger logger = new();

      Console.WriteLine("FileParserService is running.");

      FileParser fileParser = new(rabbitMQSettings, logger, dataDirectoryPath);

      await fileParser.MonitorDirectory();
    }
  }
}