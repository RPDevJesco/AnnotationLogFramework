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

        public ConsoleLogger(LogLevel minimumLevel = LogLevel.Info, bool useStructuredOutput = false)
        {
            _minimumLevel = minimumLevel;
            _useStructuredOutput = useStructuredOutput;
        }

        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Level)) return;

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
                // Use traditional formatted output
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = GetColorForLogLevel(entry.Level);
                
                Console.WriteLine(entry);
                
                if (entry.Parameters != null && entry.Parameters.Count > 0)
                {
                    Console.WriteLine("  Parameters:");
                    foreach (var param in entry.Parameters)
                    {
                        Console.WriteLine($"    {param.Key}: {FormatValue(param.Value)}");
                    }
                }
                
                if (entry.ReturnValue != null)
                {
                    Console.WriteLine($"  Return: {FormatValue(entry.ReturnValue)}");
                }
                
                if (entry.ExecutionTime != TimeSpan.Zero)
                {
                    Console.WriteLine($"  Execution Time: {entry.ExecutionTime.TotalMilliseconds}ms");
                }
                
                if (entry.Exception != null)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"  Exception: {entry.Exception.GetType().Name}: {entry.Exception.Message}");
                    Console.WriteLine($"  StackTrace: {entry.Exception.StackTrace}");
                }
                
                Console.ForegroundColor = originalColor;
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