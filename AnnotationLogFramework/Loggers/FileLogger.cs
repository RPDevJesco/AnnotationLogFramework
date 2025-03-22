using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace AnnotationLogger
{
    /// <summary>
    /// File logger implementation that writes logs to a file
    /// </summary>
    public class FileLogger : ILogger
    {
        private readonly LogLevel _minimumLevel;
        private readonly string _logFilePath;
        private readonly bool _useStructuredOutput;
        private readonly bool _appendToFile;
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();
        private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        /// <summary>
        /// Creates a new FileLogger
        /// </summary>
        /// <param name="minimumLevel">Minimum log level to record</param>
        /// <param name="logFilePath">Path to the log file. If null, log file is created automatically in the app directory</param>
        /// <param name="useStructuredOutput">Whether to use JSON formatting for log entries</param>
        /// <param name="appendToFile">Whether to append to existing log file or create a new one</param>
        public FileLogger(
            LogLevel minimumLevel = LogLevel.Info, 
            string logFilePath = null, 
            bool useStructuredOutput = false,
            bool appendToFile = true)
        {
            _minimumLevel = minimumLevel;
            _useStructuredOutput = useStructuredOutput;
            _appendToFile = appendToFile;
    
            // If no path provided, create a default log file in the application directory
            if (string.IsNullOrEmpty(logFilePath))
            {
                string appDirectory = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
                string appName = Path.GetFileNameWithoutExtension(Assembly.GetEntryAssembly().Location);
                string timestamp = DateTime.Now.ToString("yyyyMMdd");
                logFilePath = Path.Combine(appDirectory, $"{appName}_{timestamp}.log");
            }
    
            _logFilePath = logFilePath;
    
            // Create directory if it doesn't exist - only if there's an actual directory path
            string directoryPath = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }
    
            // Create or clear the log file if not appending
            if (!_appendToFile && File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, string.Empty);
            }
        }

        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Level)) return;

            string logText;
            
            if (_useStructuredOutput)
            {
                // Use JSON structured output
                logText = JsonSerializer.Serialize(entry, _jsonOptions);
            }
            else
            {
                // Use traditional formatted output
                var lines = new List<string>
                {
                    entry.ToString()
                };
                
                if (entry.Parameters?.Count > 0)
                {
                    lines.Add("  Parameters:");
                    foreach (var param in entry.Parameters)
                    {
                        lines.Add($"    {param.Key}: {FormatValue(param.Value)}");
                    }
                }
                
                if (entry.ReturnValue != null)
                {
                    lines.Add($"  Return: {FormatValue(entry.ReturnValue)}");
                }
                
                if (entry.ExecutionTime != TimeSpan.Zero)
                {
                    lines.Add($"  Execution Time: {entry.ExecutionTime.TotalMilliseconds}ms");
                }
                
                if (entry.Exception != null)
                {
                    lines.Add($"  Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}");
                    lines.Add($"  StackTrace: {entry.Exception.StackTrace}");
                }
                
                logText = string.Join(Environment.NewLine, lines);
            }
            
            // Write to file with thread safety
            try
            {
                _lock.EnterWriteLock();
                File.AppendAllText(_logFilePath, logText + Environment.NewLine + Environment.NewLine);
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
        
        private string FormatValue(object value)
        {
            if (value == null) return "null";
            
            // If value is already a string, return it directly
            // This is important as dictionary parameters may already be formatted as strings
            if (value is string stringValue)
            {
                return stringValue;
            }
            
            // Handle Dictionary types explicitly
            if (value is IDictionary dictionary)
            {
                var entries = new List<string>();
                foreach (DictionaryEntry entry in dictionary)
                {
                    entries.Add($"{FormatValue(entry.Key)}: {FormatValue(entry.Value)}");
                }
                return $"{{ {string.Join(", ", entries)} }}";
            }
            
            // Handle generic dictionaries
            if (value?.GetType().IsGenericType == true && 
                value.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                var entries = new List<string>();
                var enumerableObj = (IEnumerable)value;
                
                foreach (var item in enumerableObj)
                {
                    var itemType = item.GetType();
                    var keyProp = itemType.GetProperty("Key");
                    var valueProp = itemType.GetProperty("Value");
                    
                    if (keyProp != null && valueProp != null)
                    {
                        var key = keyProp.GetValue(item, null);
                        var val = valueProp.GetValue(item, null);
                        entries.Add($"{FormatValue(key)}: {FormatValue(val)}");
                    }
                }
                
                return $"{{ {string.Join(", ", entries)} }}";
            }
            
            // Handle other collections
            if (value is IEnumerable collection && !(value is string))
            {
                var items = new List<string>();
                foreach (var item in collection)
                {
                    items.Add(FormatValue(item));
                }
                return $"[{string.Join(", ", items)}]";
            }
            
            // For other complex objects, use JSON serialization if structured output is enabled
            if (_useStructuredOutput)
            {
                try
                {
                    return JsonSerializer.Serialize(value);
                }
                catch
                {
                    return value.ToString();
                }
            }
            
            return value.ToString();
        }
    }
}