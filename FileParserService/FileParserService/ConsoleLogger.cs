namespace FileParserService {
  public class ConsoleLogger : ILogger {
    public void LogInfo(string message) {
      Console.ForegroundColor = ConsoleColor.White;
      Console.WriteLine($"[INFO] {DateTime.Now:s}: {message}");
      Console.ResetColor();
    }

    public void LogError(string message) {
      Console.ForegroundColor = ConsoleColor.Red;
      Console.WriteLine($"[ERROR] {DateTime.Now:s}: {message}");
      Console.ResetColor();
    }
  }
}