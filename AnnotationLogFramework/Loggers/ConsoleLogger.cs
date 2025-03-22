using System.Collections;
using System.Text.Json;

namespace AnnotationLogger
{
    /// <summary>
    /// Console implementation of the logger
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        private readonly LogLevel _minimumLevel;
        private readonly bool _useStructuredOutput;
        private readonly bool _useColors;
        private readonly bool _includeTimestamps;
        private readonly object _consoleLock = new object();

        public ConsoleLogger(
            LogLevel minimumLevel = LogLevel.Info, 
            bool useStructuredOutput = false,
            bool useColors = true,
            bool includeTimestamps = true)
        {
            _minimumLevel = minimumLevel;
            _useStructuredOutput = useStructuredOutput;
            _useColors = useColors;
            _includeTimestamps = includeTimestamps;
        }

        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Level)) return;

            // Thread safety for console output
            lock (_consoleLock)
            {
                if (_useStructuredOutput)
                {
                    // Use JSON structured output
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    string json = JsonSerializer.Serialize(entry, options);
                    Console.WriteLine(json);
                }
                else
                {
                    // Use traditional formatted output with optional colors
                    ConsoleColor originalColor = Console.ForegroundColor;
                    
                    if (_useColors)
                    {
                        Console.ForegroundColor = GetColorForLogLevel(entry.Level);
                    }
                    
                    // Format timestamp if requested
                    string timestamp = _includeTimestamps 
                        ? $"[{entry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] "
                        : "";
                    
                    // Output the entry
                    Console.WriteLine($"{timestamp}[{entry.Level}] {entry.Message}");
                    
                    // Output correlation ID if present
                    if (!string.IsNullOrEmpty(entry.CorrelationId))
                    {
                        Console.WriteLine($"  CorrelationId: {entry.CorrelationId}");
                    }
                    
                    // Output parameters if present
                    if (entry.Parameters != null && entry.Parameters.Count > 0)
                    {
                        Console.WriteLine("  Parameters:");
                        foreach (var param in entry.Parameters)
                        {
                            Console.WriteLine($"    {param.Key}: {FormatValue(param.Value)}");
                        }
                    }
                    
                    // Output return value if present
                    if (entry.ReturnValue != null)
                    {
                        Console.WriteLine($"  Return: {FormatValue(entry.ReturnValue)}");
                    }
                    
                    // Output execution time if present
                    if (entry.ExecutionTime != TimeSpan.Zero)
                    {
                        Console.WriteLine($"  Execution Time: {entry.ExecutionTime.TotalMilliseconds}ms");
                    }
                    
                    // Output exception if present
                    if (entry.Exception != null)
                    {
                        if (_useColors)
                        {
                            Console.ForegroundColor = ConsoleColor.Red;
                        }
                        
                        Console.WriteLine($"  Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}");
                        Console.WriteLine($"  StackTrace: {entry.Exception.StackTrace}");
                    }
                    
                    // Output data changes if present
                    if (entry.HasDataChanges)
                    {
                        if (_useColors)
                        {
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        }
                        
                        Console.WriteLine($"  Data Changes ({entry.DataChanges.Count}):");
                        
                        if (!string.IsNullOrEmpty(entry.EntityType))
                            Console.WriteLine($"    Entity Type: {entry.EntityType}");
                            
                        if (!string.IsNullOrEmpty(entry.EntityId))
                            Console.WriteLine($"    Entity ID: {entry.EntityId}");
                            
                        if (!string.IsNullOrEmpty(entry.OperationType))
                            Console.WriteLine($"    Operation: {entry.OperationType}");
                        
                        foreach (var change in entry.DataChanges.Take(10))
                        {
                            Console.WriteLine($"    {change.PropertyPath}: '{change.OldValue}' -> '{change.NewValue}'");
                        }
                        
                        if (entry.DataChanges.Count > 10)
                            Console.WriteLine($"    ... and {entry.DataChanges.Count - 10} more changes");
                    }
                    
                    // Output additional context if present
                    if (entry.Context != null && entry.Context.Count > 0)
                    {
                        Console.WriteLine("  Context:");
                        foreach (var item in entry.Context)
                        {
                            Console.WriteLine($"    {item.Key}: {FormatValue(item.Value)}");
                        }
                    }
                    
                    // Reset console color
                    if (_useColors)
                    {
                        Console.ForegroundColor = originalColor;
                    }
                }
            }
        }

        public bool IsEnabled(LogLevel level)
        {
            return level >= _minimumLevel;
        }

        private ConsoleColor GetColorForLogLevel(LogLevel level)
        {
            return level switch
            {
                LogLevel.Trace => ConsoleColor.Gray,
                LogLevel.Debug => ConsoleColor.Gray,
                LogLevel.Info => ConsoleColor.White,
                LogLevel.Warning => ConsoleColor.Yellow,
                LogLevel.Error => ConsoleColor.Red,
                LogLevel.Critical => ConsoleColor.DarkRed,
                _ => ConsoleColor.White
            };
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