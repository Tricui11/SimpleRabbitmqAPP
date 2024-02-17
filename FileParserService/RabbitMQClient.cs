using System.Text;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FileParserService {
  public class RabbitMQClient {
    private readonly string _queueName;
    private readonly ConnectionFactory _connectionFactory;

    public RabbitMQClient(string queueName, string rabbitMQHost) {
      _queueName = queueName;
      _connectionFactory = new ConnectionFactory() { HostName = rabbitMQHost };
    }

    public bool SendModules(List<Module> modules) {
      try {
        using (var connection = _connectionFactory.CreateConnection())
        using (var channel = connection.CreateModel()) {
          channel.QueueDeclare(queue: _queueName,
                              durable: false,
                              exclusive: false,
                              autoDelete: false,
                              arguments: null);

          foreach (var module in modules) {
            var modulesJson = JsonConvert.SerializeObject(module);
            var body = Encoding.UTF8.GetBytes(modulesJson);

            channel.BasicPublish(exchange: "",
                                routingKey: _queueName,
                                basicProperties: null,
                                body: body);

            Console.WriteLine($"Sent message: {modulesJson}");
          }

          return true;
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error while sending RabbitMQ message: {ex.Message}");
        return false;
      }
    }

  }
}
