using System.Text.RegularExpressions;
using System.Xml.Linq;
using LoggingLibrary;
using ModuleLibrary;

namespace FileParserService {
  public class FileParser {
    private readonly string _directoryPath;
    private readonly RabbitMQClient _rabbitMQClient;
    private readonly ILogger _logger;
    private const string _processedDir = "processed";
    private const string _invalidDir = "invalid";
    private Dictionary<string, List<Module>> _processedFiles = new Dictionary<string, List<Module>>();

    public FileParser(string directoryPath, RabbitMQClient rabbitMQClient, ILogger logger) {
      _directoryPath = directoryPath;
      _rabbitMQClient = rabbitMQClient;
      _logger = logger;
    }

    public void Start() {
      Thread thread = new(MonitorDirectory);
      thread.Start();
    }

    private void MonitorDirectory() {
      while (true) {
        try {
          string[] xmlFiles = Directory.GetFiles(_directoryPath, "*.xml");

          foreach (string xmlFile in xmlFiles.OrderBy(p => p)) {
            _logger.LogInfo($"A file {Path.GetFileName(xmlFile)} is being processed", ConsoleColor.Yellow);

            bool success = ProcessXmlFile(xmlFile);
            if (success) {
              MoveFileTo(_processedDir, xmlFile);
            }

            _logger.LogInfo($"Processing {Path.GetFileName(xmlFile)} is completed");
          }
        }
        catch (Exception ex) {
          _logger.LogError($"Error while monitoring directory: {ex.Message}");
        }

        Thread.Sleep(1000);
      }
    }

    private bool ProcessXmlFile(string filePath) {
      try {
        List<Module> modules;

        if (_processedFiles.ContainsKey(filePath)) {
          modules = _processedFiles[filePath];
        } else {
          string xmlContent = File.ReadAllText(filePath);
          modules = ParseXml(xmlContent, filePath);

          if (modules.Any()) {
            modules.ForEach(p => p.ChangeModuleState());
            _processedFiles.Add(filePath, modules);
          }
        }

        if (modules.Any()) {
          bool success = _rabbitMQClient.SendModules(modules);
          if (!success) {
            _logger.LogError($"Error sending XML file {Path.GetFileName(filePath)} through RabbitMQ");
          }
          return success;
        }

        return false;
      }
      catch (Exception ex) {
        _logger.LogError($"Error processing XML file {Path.GetFileName(filePath)}: {ex.Message}");
        return false;
      }
    }

    private void MoveFileTo(string dir, string filePath) {
      try {
        string processedDirectory = Path.Combine(_directoryPath, dir);
        Directory.CreateDirectory(processedDirectory);

        string fileName = Path.GetFileName(filePath);
        string destinationPath = Path.Combine(processedDirectory, fileName);

        // If a file with the same name already exists, add a random string to the name
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
        _logger.LogError($"Error moving the file: {ex.Message}");
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
              _logger.LogError($"Error parsing ModuleState: {ex.Message}");
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
        _logger.LogError($"Error parsing XML file: {ex.Message}");
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
  }
}
