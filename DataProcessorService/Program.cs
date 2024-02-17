using LoggingLibrary;

namespace DataProcessorService {
  class Program {
    static void Main(string[] args) {

      string queueName = "YourQueueName";
      string connectionString = "YourRabbitMQConnectionString";

      ILogger logger = new ConsoleLogger();

      DataProcessor dataProcessor = new DataProcessor(queueName, logger, connectionString);
      dataProcessor.Start();
    }
  }
}
