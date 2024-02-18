using System.Collections.Concurrent;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CommonLibrary.Logging;
using CommonLibrary.Models;
using CommonLibrary.Settings;
using Newtonsoft.Json;
using RabbitMQ.Client;

namespace FileParserService {
  public class FileParser {
    private readonly string _dataDirectoryPath;
    private readonly ConnectionFactory _connectionFactory;
    private readonly string _queueName;
    private readonly ILogger _logger;
    private const string _processedDir = "processed";
    private const string _invalidDir = "invalid";
    private readonly ConcurrentHashSet<string> _processingFiles = new();
    private readonly ConcurrentDictionary<string, List<Module>> _parsedFiles = new();

    public FileParser(RabbitMQSettings rabbitMQSettings, ILogger logger, string dataDirectoryPath) {
      _queueName = rabbitMQSettings.QueueName;
      _logger = logger;
      _connectionFactory = new ConnectionFactory() {
        HostName = rabbitMQSettings.HostName,
        Port = rabbitMQSettings.Port,
        UserName = rabbitMQSettings.UserName,
        Password = rabbitMQSettings.Password
      };
      _dataDirectoryPath = dataDirectoryPath;
    }

    public async Task MonitorDirectory() {
      while (true) {
        SemaphoreSlim semaphore = new(10);
        try {
          string[] xmlFiles = Directory.GetFiles(_dataDirectoryPath, "*.xml");

          List<Task> processingTasks = new();
          foreach (string xmlFile in xmlFiles.OrderBy(p => p)) {
            if (_processingFiles.Contains(xmlFile)) {
              continue;
            }

            _processingFiles.TryAdd(xmlFile);
            
            try {
              await semaphore.WaitAsync();
              processingTasks.Add(Task.Run(async () => {
                try {
                  await ProcessXmlFileAsync(xmlFile);
                }
                finally {
                  semaphore.Release();
                }
              }));
            }
            catch (Exception ex) {
              _logger.LogError($"Error processing file {xmlFile}: {ex.Message}. Stack trace: {ex.StackTrace}");
            }
            finally {
              _processingFiles.TryRemove(xmlFile);
            }
          }
          await Task.WhenAll(processingTasks);
        }
        catch (Exception ex) {
          _logger.LogError($"Error while monitoring directory: {ex.Message}. Stack trace: {ex.StackTrace}");
        }

        await Task.Delay(1000);
      }
    }

    private async Task ProcessXmlFileAsync(string filePath) {
      try {
        _logger.LogInfo($"A file {Path.GetFileName(filePath)} is being processed", ConsoleColor.Yellow);

        List<Module> modules;

        if (_parsedFiles.ContainsKey(filePath)) {
          modules = _parsedFiles[filePath];
        } else {
          string xmlContent = await File.ReadAllTextAsync(filePath);
          modules = ParseXml(xmlContent, filePath);

          if (modules.Any()) {
            modules.ForEach(p => p.ChangeModuleState());
            _parsedFiles.TryAdd(filePath, modules);
          }
        }

        if (modules.Any()) {
          string modulesJson = JsonConvert.SerializeObject(modules);
          bool success = SendRabbitMQMessage(modulesJson);
          if (success) {
            MoveFileTo(_processedDir, filePath);
          } else {
            _logger.LogError($"Error sending XML file {Path.GetFileName(filePath)} through RabbitMQ");
          }
        }

        _logger.LogInfo($"Processing {Path.GetFileName(filePath)} is completed", ConsoleColor.Green);
      }
      catch (Exception ex) {
        _logger.LogError($"Error processing XML file {Path.GetFileName(filePath)}: {ex.Message}. Stack trace: {ex.StackTrace}");
      }
    }

    private void MoveFileTo(string dir, string filePath) {
      try {
        string processedDirectory = Path.Combine(_dataDirectoryPath, dir);
        Directory.CreateDirectory(processedDirectory);

        string fileName = Path.GetFileName(filePath);
        string destinationPath = Path.Combine(processedDirectory, fileName);

        if (File.Exists(destinationPath)) {
          string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(filePath);
          string fileExtension = Path.GetExtension(filePath);
          string uniqueFileName = $"{fileNameWithoutExtension}_{Guid.NewGuid().ToString()[..8]}{fileExtension}";
          destinationPath = Path.Combine(processedDirectory, uniqueFileName);
        }

        File.Move(filePath, destinationPath);
        _logger.LogInfo($"The file {Path.GetFileName(filePath)} has been moved to the {dir} folder ");
        return;
      }
      catch (Exception ex) {
        _logger.LogError($"Error moving the file: {ex.Message}. Stack trace: {ex.StackTrace}");
      }
    }

    private List<Module> ParseXml(string xmlContent, string filePath) {
      List<Module> modules = new();

      try {
        var doc = XDocument.Parse(xmlContent);
        var deviceStatusElements = doc.Descendants("DeviceStatus");

        foreach (var deviceStatusElement in deviceStatusElements) {
          var moduleCategoryID = deviceStatusElement.Element("ModuleCategoryID")?.Value;
          var moduleStateXml = deviceStatusElement.Element("RapidControlStatus")?.Value;

          if (!string.IsNullOrEmpty(moduleCategoryID) && !string.IsNullOrEmpty(moduleStateXml)) {
            ModuleState moduleState;
            try {
              moduleState = ParseModuleStateXml(moduleStateXml);
            }
            catch (Exception ex) {
              _logger.LogError($"Error parsing ModuleState: {ex.Message}. Stack trace: {ex.StackTrace}");
              MoveFileTo(_invalidDir, filePath);
              return new List<Module>();
            }

            modules.Add(new Module {
              ModuleCategoryID = moduleCategoryID,
              ModuleState = moduleState
            });
          } else {
            _logger.LogError("moduleCategoryID or moduleStateXml is empty");
            MoveFileTo(_invalidDir, filePath);
            return new List<Module>();
          }
        }
      }
      catch (Exception ex) {
        _logger.LogError($"Error parsing XML file: {ex.Message}. Stack trace: {ex.StackTrace}");
        MoveFileTo(_invalidDir, filePath);
        return new List<Module>();
      }

      return modules;
    }

    public static ModuleState ParseModuleStateXml(string moduleStateXml) {
      string pattern = @"<ModuleState>(.*?)<\/ModuleState>";

      Match match = Regex.Match(moduleStateXml, pattern);

      if (match.Success) {
        string stateValue = match.Groups[1].Value;

        if (Enum.TryParse(stateValue, out ModuleState result)) {
          return result;
        } else {
          throw new Exception("ModuleState value incorrect");
        }
      } else {
        throw new Exception("ModuleState not found");
      }
    }

    public bool SendRabbitMQMessage(string json) {
      try {
        using (var connection = _connectionFactory.CreateConnection())
        using (var channel = connection.CreateModel()) {
          channel.QueueDeclare(queue: _queueName, durable: false, exclusive: false, autoDelete: false, arguments: null);

          var body = Encoding.UTF8.GetBytes(json);
          channel.BasicPublish(exchange: "", routingKey: _queueName, basicProperties: null, body: body);

          Console.WriteLine($"Sent message: {json}");

          return true;
        }
      }
      catch (Exception ex) {
        Console.WriteLine($"Error while sending RabbitMQ message: {ex.Message}. Stack trace: {ex.StackTrace}");
        //Thread.Sleep(TimeSpan.FromSeconds(10));
        return false;
      }
    }
  }
}
