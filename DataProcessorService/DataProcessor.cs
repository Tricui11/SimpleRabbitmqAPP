using System.Text;
using LoggingLibrary;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataProcessorService {
  public class DataProcessor {
    private readonly string _queueName;
    private readonly ILogger _logger;
    private readonly string _connectionString;

    public DataProcessor(string queueName, ILogger logger, string connectionString) {
      _queueName = queueName;
      _logger = logger;
      _connectionString = connectionString;
    }

    public void Start() {
      _logger.LogInfo("DataProcessor started");

      var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
      using var connection = factory.CreateConnection();
      using var channel = connection.CreateModel();

      channel.QueueDeclare(queue: _queueName,
                           durable: false,
                           exclusive: false,
                           autoDelete: false,
                           arguments: null);

      var consumer = new EventingBasicConsumer(channel);
      consumer.Received += (model, ea) => {
        var body = ea.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);

        ProcessMessage(message);

        _logger.LogInfo("DataProcessor received and processed a message: {message}");
      };
      channel.BasicConsume(queue: _queueName,
                           autoAck: true,
                           consumer: consumer);

      Console.ReadLine();
    }

    private void ProcessMessage(string message) {
      // Implement your message processing logic here
      _logger.LogInfo($"Processing message: {message}");

      // For demonstration purposes, let's just log the message
      _logger.LogInfo($"Message processed: {message}");
    }
  }
}
