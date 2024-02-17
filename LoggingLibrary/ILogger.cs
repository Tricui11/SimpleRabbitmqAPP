namespace LoggingLibrary {
  public interface ILogger {
    void LogInfo(string message, ConsoleColor foregroundColor = ConsoleColor.White);
    void LogError(string message);
  }
}
