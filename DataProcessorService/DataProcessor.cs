using System.Text;
using LoggingLibrary;
using ModuleLibrary;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Microsoft.Data.Sqlite;
using SQLitePCL;

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






      var factory = new ConnectionFactory() {
        HostName = _connectionString,
        //    Port = 15672,
        //  UserName = "guest",
        //  Password = "guest"
      };




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

        channel.BasicAck(deliveryTag: ea.DeliveryTag, multiple: false);
      };
      channel.BasicConsume(queue: _queueName,
                           autoAck: true,
                           consumer: consumer);

      Console.ReadLine();
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

        string createTableQuery = @"CREATE TABLE IF NOT EXISTS Modules (
                                ModuleCategoryID TEXT PRIMARY KEY,
                                ModuleState TEXT
                            )";

        using (SqliteCommand command = new(createTableQuery, connection)) {
          command.ExecuteNonQuery();
        }

        string Query = "INSERT OR REPLACE INTO TableName (ModuleCategoryID, ModuleState) VALUES (@CategoryID, @State);";
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
