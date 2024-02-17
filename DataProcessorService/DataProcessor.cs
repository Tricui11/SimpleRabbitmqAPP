using Newtonsoft.Json;
using RabbitMQ.Client;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using CommonLibrary.Logging;
using CommonLibrary.Settings;
using CommonLibrary.Models;
using RabbitMQ.Client.Events;
using System.Text;

namespace DataProcessorService {
  public class DataProcessor {
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _queueName;
    private readonly ILogger _logger;

    public DataProcessor(RabbitMQSettings rabbitMQSettings, ILogger logger) {
      _queueName = rabbitMQSettings.QueueName;
      _logger = logger;
      _connectionFactory = new ConnectionFactory() {
        HostName = rabbitMQSettings.HostName,
        Port = rabbitMQSettings.Port,
        UserName = rabbitMQSettings.UserName,
        Password = rabbitMQSettings.Password
      };
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

          ProcessMessage(message);

          _logger.LogInfo($"DataProcessor received and processed a message: {message}");

          channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
        };

        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);

        Console.ReadLine();
      }
      catch (Exception ex) {
        _logger.LogError($"An error occurred in DataProcessor: {ex.Message}");
      }
    }

    private void ProcessMessage(string message) {
      _logger.LogInfo($"Processing message: {message}");

      var modules = JsonConvert.DeserializeObject<List<Module>>(message);

      SaveModulesToDatabase(modules);

      _logger.LogInfo($"Message processed: {message}");
    }

    private void SaveModulesToDatabase(List<Module> modules) {

      string databaseFileName = "devicesDB.db";
      string databaseFolderPath = @"C:\Users\Furer\Downloads";
      string databaseFilePath = Path.Combine(databaseFolderPath, databaseFileName);

      string connectionString = $"Data Source={databaseFilePath}";



      using (SqliteConnection connection = new(connectionString)) {
        SQLite3Provider_e_sqlite3 sqlite3Provider = new();
        raw.SetProvider(sqlite3Provider);

        connection.Open();

        string createTableQuery = @"CREATE TABLE IF NOT EXISTS Modules (ModuleCategoryID TEXT PRIMARY KEY, ModuleState TEXT)";

        using (SqliteCommand command = new(createTableQuery, connection)) {
          command.ExecuteNonQuery();
        }

        string Query = "INSERT OR REPLACE INTO Modules (ModuleCategoryID, ModuleState) VALUES (@CategoryID, @State);";
        foreach (var module in modules) {
          using (SqliteCommand command = new(Query, connection)) {
            command.Parameters.AddWithValue("@CategoryID", module.ModuleCategoryID);
            command.Parameters.AddWithValue("@State", module.ModuleState.ToString());
            command.ExecuteNonQuery();
          }
        }
      }
    }
  }
}