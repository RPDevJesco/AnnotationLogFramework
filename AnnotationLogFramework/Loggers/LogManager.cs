using System.Collections;
using System.Diagnostics;
using System.Reflection;

namespace AnnotationLogger
{
    /// <summary>
    /// Logging manager that handles the interception and logging
    /// </summary>
    public static class LogManager
    {
        private static LoggerConfiguration _configuration = new LoggerConfiguration();

        public static void Configure(Action<LoggerConfiguration> configAction)
        {
            var config = new LoggerConfiguration();
            configAction(config);
            _configuration = config;
        }

        public static LoggerConfiguration GetConfiguration() => _configuration;

        public static async Task<TResult> LogMethodAsync<TResult>(Func<Task<TResult>> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            return await InternalLogMethodAsync(methodCall, methodName, declaringType, parameters, parameterInfos,
                logAttribute);
        }

        public static TResult LogMethod<TResult>(Func<TResult> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            return InternalLogMethod(methodCall, methodName, declaringType, parameters, parameterInfos, logAttribute);
        }

        public static void LogMethod(Action methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            InternalLogMethod(() =>
            {
                methodCall();
                return true; // Return something to satisfy shared logic
            }, methodName, declaringType, parameters, parameterInfos, logAttribute);
        }

        private static TResult InternalLogMethod<TResult>(Func<TResult> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            TResult result = default;

            try
            {
                LogMethodEntry(methodName, declaringType, parameters, parameterInfos, logAttribute);
                result = methodCall();
                return result;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                LogMethodExit(methodName, declaringType, parameters, parameterInfos, result, stopwatch.Elapsed,
                    exception, logAttribute);
            }
        }

        private static async Task<TResult> InternalLogMethodAsync<TResult>(Func<Task<TResult>> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            TResult result = default;

            try
            {
                LogMethodEntry(methodName, declaringType, parameters, parameterInfos, logAttribute);
                result = await methodCall();
                return result;
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();
                LogMethodExit(methodName, declaringType, parameters, parameterInfos, result, stopwatch.Elapsed,
                    exception, logAttribute);
            }
        }

        private static void LogMethodEntry(string methodName, Type declaringType, object[] parameters,
            ParameterInfo[] parameterInfos, LogAttributeBase logAttribute)
        {
            if (!ShouldLog(logAttribute)) return;

            var entry = CreateBaseLogEntry(methodName, declaringType, logAttribute);
            entry.Message = $"Entering {declaringType.Name}.{methodName}";

            if (_configuration.EnableParameterLogging && logAttribute.IncludeParameters && parameters != null)
            {
                entry.Parameters = CreateParameterDictionary(parameters, parameterInfos);
            }

            _configuration.Logger.Log(entry);
        }
        
        private static void LogMethodExit(string methodName, Type declaringType, object[] parameters,
            ParameterInfo[] parameterInfos,
            object result, TimeSpan executionTime, Exception exception, LogAttributeBase logAttribute)
        {
            // Prevent duplicate logging
            if (!ShouldLog(logAttribute) && exception == null) return;

            Console.WriteLine($"DEBUG: Creating log entry for {declaringType.Name}.{methodName}");
    
            var entry = CreateBaseLogEntry(methodName, declaringType, logAttribute);
            entry.Level = exception != null ? LogLevel.Error : logAttribute.Level;
            entry.Message = exception != null
                ? $"Exception in {declaringType.Name}.{methodName}"
                : $"Exiting {declaringType.Name}.{methodName}";
            entry.Exception = exception;

            if (_configuration.EnableParameterLogging && logAttribute.IncludeParameters && parameters != null)
            {
                Console.WriteLine("DEBUG: Adding parameters to log entry");
                entry.Parameters = CreateParameterDictionary(parameters, parameterInfos);
            }

            if (_configuration.EnableExecutionTimeLogging && logAttribute.IncludeExecutionTime)
            {
                entry.ExecutionTime = executionTime;
            }

            if (_configuration.EnableReturnValueLogging && logAttribute.IncludeReturnValue && result != null && exception == null)
            {
                entry.ReturnValue = SummarizeReturnValue(result);
            }

            Console.WriteLine("DEBUG: Sending log entry to logger");
            _configuration.Logger.Log(entry);
        }

        private static LogEntry CreateBaseLogEntry(string methodName, Type declaringType, LogAttributeBase logAttribute)
        {
            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                MethodName = methodName,
                ClassName = declaringType.Name,
                Level = logAttribute.Level,
                CorrelationId = CorrelationManager.CurrentCorrelationId, // Inject correlation automatically
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
            };
        }

        private static Dictionary<string, object> CreateParameterDictionary(object[] parameters, ParameterInfo[] parameterInfos)
        {
            var dict = new Dictionary<string, object>();
            if (parameters == null || parameterInfos == null) return dict;

            for (int i = 0; i < parameters.Length && i < parameterInfos.Length; i++)
            {
                var paramName = parameterInfos[i].Name;
                var paramValue = parameters[i];

                // Special case for Dictionary<string, string>
                if (paramValue is Dictionary<string, string> stringDict)
                {
                    // Convert directly to a string representation
                    var entries = stringDict.Select(kvp => $"{kvp.Key}: {kvp.Value}");
                    dict[paramName] = $"{{ {string.Join(", ", entries)} }}";
                }
                // Other dictionary types
                else if (paramValue is IDictionary || 
                         (paramValue?.GetType().IsGenericType == true && 
                          paramValue.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    // Force early conversion to string
                    dict[paramName] = FormatDictionaryToString(paramValue);
                }
                else
                {
                    // Regular parameters
                    dict[paramName] = paramValue;
                }
            }

            return dict;
        }
        
        private static bool ShouldLog(LogAttributeBase logAttribute)
        {
            if (logAttribute is LogDebugAttribute && _configuration.Environment == EnvironmentType.Production)
            {
                return false;
            }

            return _configuration.Logger.IsEnabled(logAttribute.Level);
        }
        
        private static object SummarizeReturnValue(object result, int maxDepth = 2, int currentDepth = 0)
        {
            if (result == null) return null;
            if (currentDepth >= maxDepth) return $"{result.GetType().Name} (max depth reached)";

            Type type = result.GetType();

            // Handle strings specially (they're IEnumerable but we want to treat them as primitives)
            if (result is string str)
            {
                // Truncate long strings
                const int maxStringLength = 100;
                return str.Length > maxStringLength ? $"{str.Substring(0, maxStringLength)}... (length: {str.Length})" : str;
            }

            // Handle common collection types
            if (result is Array arrayResult)
            {
                var sampleItems = GetSampleItems(arrayResult, 3)
                    .Select(i => SummarizeReturnValue(i, maxDepth, currentDepth + 1))
                    .ToList();
                    
                return $"Array<{GetElementTypeName(arrayResult)}>[{arrayResult.Length}] {{{string.Join(", ", sampleItems)}}}";
            }
            
            // Handle generic collections with proper type info
            if (type.IsGenericType)
            {
                var genericTypeDef = type.GetGenericTypeDefinition();
                var genericArgs = type.GetGenericArguments();
                
                // Handle different collection types
                if (typeof(ICollection<>).MakeGenericType(genericArgs[0]).IsAssignableFrom(type))
                {
                    // Get collection count using reflection for strongly typed collections
                    var countProperty = type.GetProperty("Count");
                    int count = (int)countProperty.GetValue(result);
                    
                    // Get a few sample items
                    var sampleItems = GetSampleItems((IEnumerable)result, 3)
                        .Select(i => SummarizeReturnValue(i, maxDepth, currentDepth + 1))
                        .ToList();
                        
                    string typeName = genericArgs[0].Name;
                    return $"Collection<{typeName}>[{count}] {{{string.Join(", ", sampleItems)}}}";
                }
                
                // Handle dictionaries with proper key/value types
                if (typeof(IDictionary<,>).MakeGenericType(genericArgs).IsAssignableFrom(type))
                {
                    var countProperty = type.GetProperty("Count");
                    int count = (int)countProperty.GetValue(result);
                    
                    var dict = result as IEnumerable;
                    var entries = new List<string>();
                    int i = 0;
                    
                    foreach (var entry in dict)
                    {
                        if (i >= 3) break; // Limit to 3 samples
                        
                        var entryType = entry.GetType();
                        var keyProp = entryType.GetProperty("Key");
                        var valueProp = entryType.GetProperty("Value");
                        
                        if (keyProp != null && valueProp != null)
                        {
                            var key = keyProp.GetValue(entry);
                            var value = valueProp.GetValue(entry);
                            
                            entries.Add($"{FormatValue(key, maxDepth, currentDepth + 1)}: {FormatValue(value, maxDepth, currentDepth + 1)}");
                        }
                        i++;
                    }
                    
                    string keyTypeName = genericArgs[0].Name;
                    string valueTypeName = genericArgs[1].Name;
                    
                    if (entries.Count < count)
                        entries.Add("...");
                        
                    return $"Dictionary<{keyTypeName}, {valueTypeName}>[{count}] {{{string.Join(", ", entries)}}}";
                }
            }
            
            // Handle non-generic IEnumerable
            if (result is IEnumerable enumerable && !(result is string))
            {
                // Try to count items without fully enumerating (if possible)
                int count;
                ICollection collection = result as ICollection;
                
                if (collection != null)
                {
                    count = collection.Count;
                }
                else
                {
                    // This might enumerate the full collection, but we need the count
                    count = enumerable.Cast<object>().Count();
                }
                
                var sampleItems = GetSampleItems(enumerable, 3)
                    .Select(i => FormatValue(i, maxDepth, currentDepth + 1))
                    .ToList();
                    
                if (sampleItems.Count < count)
                    sampleItems.Add("...");
                    
                return $"{type.Name}[{count}] {{{string.Join(", ", sampleItems)}}}";
            }

            // Handle classes with properties (use reflection)
            if (type.IsClass && type != typeof(string))
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
                
                if (properties.Length <= 5)
                {
                    // For objects with few properties, show them all
                    var propValues = properties
                        .Where(p => p.GetIndexParameters().Length == 0) // Skip indexed properties
                        .Select(p => 
                        {
                            try 
                            {
                                return $"{p.Name}: {FormatValue(p.GetValue(result), maxDepth, currentDepth + 1)}";
                            }
                            catch 
                            {
                                return $"{p.Name}: <error reading value>";
                            }
                        })
                        .ToList();
                        
                    return $"{type.Name} {{ {string.Join(", ", propValues)} }}";
                }
                else
                {
                    // For objects with many properties, just show count
                    return $"{type.Name} (with {properties.Length} properties)";
                }
            }

            // Return primitives and anything else directly
            return result.ToString();
        }

        private static object FormatParameterValue(object value, int maxDepth = 3, int currentDepth = 0)
        {
            if (value == null) return "null";
            if (currentDepth >= maxDepth) return $"{value.GetType().Name} (max depth reached)";
            
            // Handle strings specially to truncate if needed
            if (value is string str)
            {
                const int maxStringLength = 100;
                return str.Length > maxStringLength ? $"{str.Substring(0, maxStringLength)}... (length: {str.Length})" : str;
            }
            
            // Handle value types (primitives, structs, enums)
            Type type = value.GetType();
            if (type.IsValueType)
            {
                if (type.IsEnum)
                    return $"{type.Name}.{value}";
                    
                return value.ToString();
            }

            // Handle dictionaries recursively with depth limit
            if (value is IDictionary dictionary)
            {
                if (dictionary.Count == 0) return "{}";
                
                var formattedEntries = new List<string>();
                int i = 0;
                
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (i >= 5) // Limit samples
                    {
                        formattedEntries.Add("...");
                        break;
                    }
                    
                    formattedEntries.Add($"{FormatParameterValue(entry.Key, maxDepth, currentDepth + 1)}: " +
                                       $"{FormatParameterValue(entry.Value, maxDepth, currentDepth + 1)}");
                    i++;
                }
                
                return $"{{ {string.Join(", ", formattedEntries)} }}";
            }
            
            // Handle generic dictionaries with proper type info
            if (type.IsGenericType && 
                typeof(IDictionary<,>).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                var countProperty = type.GetProperty("Count");
                int count = (int)countProperty.GetValue(value);
                
                if (count == 0) return "{}";
                
                var formattedEntries = new List<string>();
                var dictionaryItems = (IEnumerable)value;
                int i = 0;
                
                foreach (var entry in dictionaryItems)
                {
                    if (i >= 5) // Limit samples
                    {
                        formattedEntries.Add("...");
                        break;
                    }
                    
                    var entryType = entry.GetType();
                    var keyProperty = entryType.GetProperty("Key");
                    var valueProperty = entryType.GetProperty("Value");
                    
                    if (keyProperty != null && valueProperty != null)
                    {
                        var key = FormatParameterValue(keyProperty.GetValue(entry), maxDepth, currentDepth + 1);
                        var val = FormatParameterValue(valueProperty.GetValue(entry), maxDepth, currentDepth + 1);
                        formattedEntries.Add($"{key}: {val}");
                    }
                    i++;
                }
                
                return $"{{ {string.Join(", ", formattedEntries)} }}";
            }
            
            // Handle arrays with type info
            if (value is Array array)
            {
                if (array.Length == 0) return "[]";
                
                var elementType = array.GetType().GetElementType();
                string typeName = elementType?.Name ?? "object";
                
                return FormatCollection(array, typeName, array.Length, maxDepth, currentDepth);
            }
            
            // Handle lists and other collections
            if (value is ICollection collection)
            {
                if (collection.Count == 0) return "[]";
                
                string typeName = "object";
                if (type.IsGenericType)
                {
                    var genericArgs = type.GetGenericArguments();
                    if (genericArgs.Length > 0)
                        typeName = genericArgs[0].Name;
                }
                
                return FormatCollection(collection, typeName, collection.Count, maxDepth, currentDepth);
            }
            
            // Handle any IEnumerable
            if (value is IEnumerable enumerable && !(value is string))
            {
                return FormatCollection(enumerable, "object", -1, maxDepth, currentDepth);
            }
            
            // Handle objects with properties
            if (type.IsClass)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0) // Skip indexed properties
                    .ToArray();
                    
                if (properties.Length <= 3)
                {
                    // For objects with few properties, show them all
                    var propValues = properties.Select(p => 
                    {
                        try 
                        {
                            return $"{p.Name}: {FormatParameterValue(p.GetValue(value), maxDepth, currentDepth + 1)}";
                        }
                        catch 
                        {
                            return $"{p.Name}: <error reading value>";
                        }
                    });
                    
                    return $"{type.Name} {{ {string.Join(", ", propValues)} }}";
                }
                else
                {
                    // Just show type for complex objects
                    return $"{type.Name} (with {properties.Length} properties)";
                }
            }
            
            // Fallback
            return value.ToString();
        }

        // Helper methods 
        private static string FormatDictionaryToString(object dictObj)
        {
            // Handle Dictionary<string, string>
            if (dictObj is Dictionary<string, string> stringDict)
            {
                var entries = stringDict.Select(kvp => $"{kvp.Key}: {kvp.Value}");
                return $"{{ {string.Join(", ", entries)} }}";
            }
            
            // Handle Dictionary<string, object>  
            if (dictObj is Dictionary<string, object> objDict)
            {
                var entries = objDict.Select(kvp => $"{kvp.Key}: {kvp.Value}");
                return $"{{ {string.Join(", ", entries)} }}";
            }
            
            // Handle IDictionary
            if (dictObj is IDictionary dict)
            {
                var entries = new List<string>();
                foreach (DictionaryEntry entry in dict)
                {
                    entries.Add($"{entry.Key}: {entry.Value}");
                }
                return $"{{ {string.Join(", ", entries)} }}";
            }
            
            // Generic dictionary through reflection
            var type = dictObj.GetType();
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                try
                {
                    // Use reflection to extract and format dictionary entries
                    var entries = new List<string>();
                    var enumerableObj = (IEnumerable)dictObj;
                    
                    foreach (var item in enumerableObj)
                    {
                        var itemType = item.GetType();
                        var keyProp = itemType.GetProperty("Key");
                        var valueProp = itemType.GetProperty("Value");
                        
                        if (keyProp != null && valueProp != null)
                        {
                            var key = keyProp.GetValue(item, null);
                            var value = valueProp.GetValue(item, null);
                            entries.Add($"{key}: {value}");
                        }
                    }
                    
                    return $"{{ {string.Join(", ", entries)} }}";
                }
                catch
                {
                    // Fall back to type name if reflection fails
                    return dictObj.ToString();
                }
            }
            
            return dictObj.ToString();
        }
        
        private static string FormatCollection(IEnumerable collection, string typeName, int count, int maxDepth, int currentDepth)
        {
            var items = new List<string>();
            int i = 0;
            
            foreach (var item in collection)
            {
                if (i >= 5) // Limit samples
                {
                    items.Add("...");
                    break;
                }
                
                items.Add(FormatParameterValue(item, maxDepth, currentDepth + 1).ToString());
                i++;
            }
            
            string countDisplay = count >= 0 ? count.ToString() : items.Count.ToString() + "+";
            return $"{typeName}[{countDisplay}] [{string.Join(", ", items)}]";
        }

        private static IList<object> GetSampleItems(IEnumerable collection, int maxItems)
        {
            var result = new List<object>();
            int count = 0;
            
            foreach (var item in collection)
            {
                if (count >= maxItems) 
                    break;
                    
                result.Add(item);
                count++;
            }
            
            return result;
        }

        private static string GetElementTypeName(Array array)
        {
            var elementType = array.GetType().GetElementType();
            return elementType?.Name ?? "object";
        }

        private static object FormatValue(object value, int maxDepth, int currentDepth)
        {
            // Reuse existing method for consistency
            return FormatParameterValue(value, maxDepth, currentDepth);
        }
    }
}