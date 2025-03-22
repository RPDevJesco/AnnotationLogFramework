using System.Text.Json;
using Microsoft.Data.Sqlite;

namespace AnnotationLogger
{
    /// <summary>
    /// SQLite implementation of the logger
    /// </summary>
    public class DatabaseLogger : ILogger
    {
        private readonly LogLevel _minimumLevel;
        private readonly string _connectionString;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        
        public DatabaseLogger(LogLevel minimumLevel = LogLevel.Info, string connectionString = "Data Source=logs.db")
        {
            _minimumLevel = minimumLevel;
            _connectionString = connectionString;
            
            InitializeDatabase();
        }
        
        private void InitializeDatabase()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            
            using var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Logs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Timestamp TEXT NOT NULL,
                    Level TEXT NOT NULL,
                    ClassName TEXT NOT NULL,
                    MethodName TEXT NOT NULL,
                    Message TEXT NOT NULL,
                    CorrelationId TEXT NULL,
                    ThreadId TEXT NULL,
                    Parameters TEXT NULL,
                    ReturnValue TEXT NULL,
                    ExecutionTime INTEGER NULL,
                    ExceptionDetails TEXT NULL,
                    DataChanges TEXT NULL
                )";
                
            command.ExecuteNonQuery();
            
            // Create indices for faster querying
            command.CommandText = "CREATE INDEX IF NOT EXISTS IX_Logs_Timestamp ON Logs (Timestamp)";
            command.ExecuteNonQuery();
            
            command.CommandText = "CREATE INDEX IF NOT EXISTS IX_Logs_CorrelationId ON Logs (CorrelationId)";
            command.ExecuteNonQuery();
            
            command.CommandText = "CREATE INDEX IF NOT EXISTS IX_Logs_Level ON Logs (Level)";
            command.ExecuteNonQuery();
        }
        
        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Level)) return;
            
            try
            {
                _lock.EnterWriteLock();
                
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();
                
                using var command = connection.CreateCommand();
                command.CommandText = @"
                    INSERT INTO Logs (
                        Timestamp, Level, ClassName, MethodName, Message, 
                        CorrelationId, ThreadId, Parameters, ReturnValue, 
                        ExecutionTime, ExceptionDetails, DataChanges
                    ) VALUES (
                        @Timestamp, @Level, @ClassName, @MethodName, @Message,
                        @CorrelationId, @ThreadId, @Parameters, @ReturnValue,
                        @ExecutionTime, @ExceptionDetails, @DataChanges
                    )";
                
                // Add parameters with safe defaults
                command.Parameters.AddWithValue("@Timestamp", entry.Timestamp.ToString("o"));
                command.Parameters.AddWithValue("@Level", entry.Level.ToString());
                command.Parameters.AddWithValue("@ClassName", entry.ClassName ?? "Unknown");
                command.Parameters.AddWithValue("@MethodName", entry.MethodName ?? "Unknown");
                command.Parameters.AddWithValue("@Message", entry.Message ?? string.Empty);
                
                // Handle nullable parameters
                command.Parameters.AddWithValue("@CorrelationId", 
                    string.IsNullOrEmpty(entry.CorrelationId) ? DBNull.Value : (object)entry.CorrelationId);
                
                command.Parameters.AddWithValue("@ThreadId", 
                    string.IsNullOrEmpty(entry.ThreadId) ? DBNull.Value : (object)entry.ThreadId);
                
                // Handle complex objects with safer serialization
                try
                {
                    command.Parameters.AddWithValue("@Parameters", 
                        entry.Parameters != null && entry.Parameters.Count > 0 
                            ? JsonSerializer.Serialize(entry.Parameters) 
                            : DBNull.Value);
                }
                catch
                {
                    command.Parameters.AddWithValue("@Parameters", DBNull.Value);
                }
                
                try
                {
                    command.Parameters.AddWithValue("@ReturnValue", 
                        entry.ReturnValue != null 
                            ? JsonSerializer.Serialize(entry.ReturnValue) 
                            : DBNull.Value);
                }
                catch
                {
                    command.Parameters.AddWithValue("@ReturnValue", DBNull.Value);
                }
                
                // Execution time
                command.Parameters.AddWithValue("@ExecutionTime", 
                    entry.ExecutionTime != TimeSpan.Zero 
                        ? (object)entry.ExecutionTime.TotalMilliseconds 
                        : DBNull.Value);
                
                // Exception
                try
                {
                    command.Parameters.AddWithValue("@ExceptionDetails", 
                        entry.Exception != null 
                            ? JsonSerializer.Serialize(new {
                                Type = entry.Exception.GetType().FullName,
                                Message = entry.Exception.Message,
                                StackTrace = entry.Exception.StackTrace
                            }) 
                            : DBNull.Value);
                }
                catch
                {
                    command.Parameters.AddWithValue("@ExceptionDetails", DBNull.Value);
                }
                
                // Data changes
                try
                {
                    command.Parameters.AddWithValue("@DataChanges", 
                        entry.HasDataChanges && entry.DataChanges != null && entry.DataChanges.Count > 0 
                            ? JsonSerializer.Serialize(entry.DataChanges) 
                            : DBNull.Value);
                }
                catch
                {
                    command.Parameters.AddWithValue("@DataChanges", DBNull.Value);
                }
                
                command.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                // Write to console for debugging
                Console.Error.WriteLine($"Error in {nameof(DatabaseLogger)}: {ex.Message}");
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }
        
        public bool IsEnabled(LogLevel level)
        {
            return level >= _minimumLevel;
        }
    }
}