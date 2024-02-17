using CommonLibrary.Logging;
using CommonLibrary.Settings;
using Microsoft.Extensions.Configuration;

namespace DataProcessorService {
  class Program {
    static void Main() {

      var configuration = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
      var config = configuration.Build();
      var section = config.GetSection("RabbitMQSettings");
      var rabbitMQSettings = section.Get<RabbitMQSettings>();
      string connectionString = config["DataBaseConnectionString"];
      ILogger logger = new ConsoleLogger();

      DataProcessor dataProcessor = new(rabbitMQSettings, logger, connectionString);
      dataProcessor.Start();
    }
  }
}
