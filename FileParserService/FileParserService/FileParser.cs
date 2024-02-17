using System.Xml.Linq;

namespace FileParserService {
  public class FileParser {
    private readonly string _directoryPath;
    private readonly RabbitMQClient _rabbitMQClient;
    private readonly ILogger _logger;

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

          foreach (string xmlFile in xmlFiles) {
            ProcessXmlFile(xmlFile);
          }
        }
        catch (Exception ex) {
          _logger.LogError($"Error while monitoring directory: {ex.Message}");
        }

        Thread.Sleep(1000);
      }
    }

    private void ProcessXmlFile(string filePath) {
      try {
        string xmlContent = File.ReadAllText(filePath);

        List<Module> modules = ParseXml(xmlContent);

        _rabbitMQClient.SendModules(modules);

        _logger.LogInfo($"Processed XML file: {filePath}");
      }
      catch (Exception ex) {
        _logger.LogError($"Error processing XML file {filePath}: {ex.Message}");
      }
    }

    private List<Module> ParseXml(string xmlContent) {
      List<Module> modules = new();

      var doc = XDocument.Parse(xmlContent);
      var deviceStatusElements = doc.Descendants("DeviceStatus");

      foreach (var deviceStatusElement in deviceStatusElements) {
        var moduleCategoryID = deviceStatusElement.Element("ModuleCategoryID")?.Value;
        var moduleStateXml = deviceStatusElement.Element("RapidControlStatus")?.Value;

        if (!string.IsNullOrEmpty(moduleCategoryID) && !string.IsNullOrEmpty(moduleStateXml)) {
          var moduleState = ParseModuleStateXml(moduleStateXml);

          modules.Add(new Module {
            ModuleCategoryID = moduleCategoryID,
            ModuleState = moduleState
          });
        }
      }

      return modules;
    }

    private string ParseModuleStateXml(string moduleStateXml) {
      return moduleStateXml;
    }
  }
}
