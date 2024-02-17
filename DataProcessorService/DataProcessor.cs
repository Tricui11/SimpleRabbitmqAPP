using System.Text;
using CommonLibrary.Logging;
using CommonLibrary.Models;
using CommonLibrary.Settings;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DataProcessorService {
  public class DataProcessor {
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _queueName;
    private readonly ILogger _logger;
    private readonly DatabaseManager _databaseManager;

    public DataProcessor(RabbitMQSettings rabbitMQSettings, ILogger logger, string connectionString) {
      _queueName = rabbitMQSettings.QueueName;
      _logger = logger;
      _connectionFactory = new ConnectionFactory() {
        HostName = rabbitMQSettings.HostName,
        Port = rabbitMQSettings.Port,
        UserName = rabbitMQSettings.UserName,
        Password = rabbitMQSettings.Password
      };
      _databaseManager = new DatabaseManager(connectionString);
    }

    public void Start() {
      _logger.LogInfo("DataProcessor started");

      try {
        using var connection = _connectionFactory.CreateConnection();
        using var channel = connection.CreateModel();

        channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

        var consumer = new EventingBasicConsumer(channel);
        consumer.Received += (model, ea) => {
          var body = ea.Body.ToArray();
          var message = Encoding.UTF8.GetString(body);

          bool success = ProcessMessage(message);
          if (success) {
            _logger.LogInfo($"DataProcessor received and processed the message.");
            channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
          } else {
            _logger.LogError($"Error processing the message. Message will be requeued.");
          }
        };

        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        Console.ReadLine();
      }
      catch (Exception ex) {
        _logger.LogError($"An error occurred while processing: {ex.Message}");
      }
    }

    private bool ProcessMessage(string message) {
      _logger.LogInfo($"Processing message: {message}", ConsoleColor.Yellow);

      try {
        var modules = JsonConvert.DeserializeObject<List<Module>>(message);

        _databaseManager.SaveModulesToDatabase(modules);

        return true;
      }
      catch (Exception ex) {
        _logger.LogError($"Error processing message: {message}. Error: {ex.Message}");
        return false;
      }
    }
  }
}
