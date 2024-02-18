using CommonLibrary.Models;
using Microsoft.Data.Sqlite;

namespace DataProcessorService {
  public class DatabaseManager {
    private readonly string _connectionString;

    public DatabaseManager(string connectionString) {
      _connectionString = connectionString;
      InitializeDatabase();
    }

    private void InitializeDatabase() {
      using (SqliteConnection connection = new(_connectionString)) {
        connection.Open();

        string createTableQuery = @"CREATE TABLE IF NOT EXISTS Modules (ModuleCategoryID TEXT PRIMARY KEY, ModuleState TEXT)";

        using SqliteCommand command = new(createTableQuery, connection);
        command.ExecuteNonQuery();
      }
    }

    public void SaveModulesToDatabase(List<Module> modules) {
      using (SqliteConnection connection = new(_connectionString)) {
        connection.Open();

        using (var transaction = connection.BeginTransaction()) {
          try {
            string insertQuery = "INSERT OR REPLACE INTO Modules (ModuleCategoryID, ModuleState) VALUES (@CategoryID, @State);";
            foreach (var module in modules) {
              Dictionary<string, object> parameters = new()
              {
                { "@CategoryID", module.ModuleCategoryID },
                { "@State", module.ModuleState.ToString() }
              };
              ExecuteNonQuery(insertQuery, parameters, connection, transaction);
            }
            
            transaction.Commit();
          }
          catch (Exception ex) {
            transaction.Rollback();
            throw new Exception($"Failed to save modules to the database. Stack trace: {ex.StackTrace}", ex);
          }
        }
      }
    }

    private static int ExecuteNonQuery(string query, Dictionary<string, object> parameters, SqliteConnection connection, SqliteTransaction transaction = null) {
      using (SqliteCommand command = new(query, connection, transaction)) {
        foreach (var parameter in parameters) {
          command.Parameters.AddWithValue(parameter.Key, parameter.Value);
        }

        return command.ExecuteNonQuery();
      }
    }
  }
}
