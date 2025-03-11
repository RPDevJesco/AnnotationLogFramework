using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AnnotationLogger
{
    /// <summary>
    /// Logging manager that handles the interception, logging, and data change tracking
    /// </summary>
    public static class LogManager
    {
        #region Configuration

        private static LoggerConfiguration _configuration = new LoggerConfiguration();
        private static DataLoggerConfiguration _dataConfiguration = new DataLoggerConfiguration();

        /// <summary>
        /// Configure general logging features
        /// </summary>
        public static void Configure(Action<LoggerConfiguration> configAction)
        {
            var config = new LoggerConfiguration();
            configAction(config);
            _configuration = config;
        }

        /// <summary>
        /// Configure data-oriented logging features
        /// </summary>
        public static void ConfigureDataLogging(Action<DataLoggerConfiguration> configAction)
        {
            if (_dataConfiguration == null)
                _dataConfiguration = new DataLoggerConfiguration();

            configAction(_dataConfiguration);
        }

        /// <summary>
        /// Get the current logger configuration
        /// </summary>
        public static LoggerConfiguration GetConfiguration() => _configuration;

        /// <summary>
        /// Get the current data logging configuration
        /// </summary>
        public static DataLoggerConfiguration GetDataConfiguration() => _dataConfiguration;

        #endregion

        #region Context Management

        /// <summary>
        /// Current context information that will be included in all logs
        /// </summary>
        public static Dictionary<string, object> CurrentContext { get; } = new Dictionary<string, object>();

        /// <summary>
        /// Adds context information that will be included in subsequent log entries
        /// </summary>
        public static void AddContext(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            CurrentContext[key] = value;
        }

        /// <summary>
        /// Removes a specific context item
        /// </summary>
        public static void RemoveContext(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            if (CurrentContext.ContainsKey(key))
                CurrentContext.Remove(key);
        }

        /// <summary>
        /// Clears all context information
        /// </summary>
        public static void ClearContext()
        {
            CurrentContext.Clear();
        }

        /// <summary>
        /// Enriches a log entry with current context
        /// </summary>
        private static LogEntry EnrichWithContext(LogEntry entry)
        {
            if (CurrentContext.Count > 0)
            {
                if (entry.Context == null)
                    entry.Context = new Dictionary<string, object>();

                foreach (var item in CurrentContext)
                {
                    entry.Context[item.Key] = item.Value;
                }
            }

            return entry;
        }

        #endregion

        #region Standard Method Logging

        /// <summary>
        /// Log a method with a return value
        /// </summary>
        public static TResult LogMethod<TResult>(Func<TResult> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            return InternalLogMethod(methodCall, methodName, declaringType, parameters, parameterInfos, logAttribute);
        }

        /// <summary>
        /// Log a method with no return value
        /// </summary>
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

        /// <summary>
        /// Log an async method with a return value
        /// </summary>
        public static async Task<TResult> LogMethodAsync<TResult>(Func<Task<TResult>> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            return await InternalLogMethodAsync(methodCall, methodName, declaringType, parameters, parameterInfos,
                logAttribute);
        }

        /// <summary>
        /// Log an async method with no return value
        /// </summary>
        public static async Task LogMethodAsync(Func<Task> methodCall,
            string methodName, Type declaringType, object[] parameters, ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute)
        {
            // Create a wrapper that returns a Task<bool> to reuse our shared logic
            Func<Task<bool>> wrappedMethod = async () =>
            {
                await methodCall();
                return true;
            };

            await InternalLogMethodAsync(wrappedMethod, methodName, declaringType, parameters, parameterInfos,
                logAttribute);
        }

        /// <summary>
        /// Internal implementation for logging synchronous methods
        /// </summary>
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

        /// <summary>
        /// Internal implementation for logging asynchronous methods
        /// </summary>
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

        #endregion

        #region Data Change Tracking

        /// <summary>
        /// Logs data changes between two objects with optional metadata
        /// </summary>
        public static void LogDataChanges<T>(
            T before,
            T after,
            string operationType,
            string entityId = null,
            LogLevel level = LogLevel.Info,
            Dictionary<string, object> additionalContext = null)
        {
            if (_configuration?.Logger == null || !_configuration.Logger.IsEnabled(level))
                return;

            var changes = ObjectComparer.CompareObjects(before, after, _dataConfiguration.MaxComparisonDepth);
            if (changes.Count == 0)
                return; // No changes to log

            var entityType = typeof(T).Name;
            var methodInfo = new StackTrace().GetFrame(1)?.GetMethod();
            var methodName = methodInfo?.Name ?? "Unknown";
            var className = methodInfo?.DeclaringType?.Name ?? "Unknown";

            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                MethodName = methodName,
                ClassName = className,
                Level = level,
                Message = $"Data changes in {entityType} {(string.IsNullOrEmpty(entityId) ? "" : $"[{entityId}]")}",
                DataChanges = changes,
                EntityType = entityType,
                EntityId = entityId,
                OperationType = operationType,
                CorrelationId = CorrelationManager.CurrentCorrelationId,
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
            };

            // Add additional context if provided
            if (additionalContext != null)
            {
                if (entry.Context == null)
                    entry.Context = new Dictionary<string, object>();

                foreach (var item in additionalContext)
                {
                    entry.Context[item.Key] = item.Value;
                }
            }

            // Add global context
            EnrichWithContext(entry);

            // Log full before state if configured
            if (_dataConfiguration.LogBeforeState && before != null)
            {
                entry.Context = entry.Context ?? new Dictionary<string, object>();
                entry.Context["BeforeState"] = LogFormatter.FormatForLog(before, "BeforeState", null);
            }

            // Log full after state if configured
            if (_dataConfiguration.LogAfterState && after != null)
            {
                entry.Context = entry.Context ?? new Dictionary<string, object>();
                entry.Context["AfterState"] = LogFormatter.FormatForLog(after, "AfterState", null);
            }

            _configuration.Logger.Log(entry);
        }

        /// <summary>
        /// Log a method with data change tracking
        /// </summary>
        public static TResult LogMethodWithDataChanges<TResult>(
            Func<TResult> methodCall,
            string methodName,
            Type declaringType,
            object[] parameters,
            ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute,
            TrackDataChangesAttribute dataChangeAttribute)
        {
            // Find before/after parameters if they exist
            object beforeObject = null;
            object afterObject = null;
            string entityType = null;
            string entityId = null;

            for (int i = 0; i < parameters.Length && i < parameterInfos.Length; i++)
            {
                var param = parameterInfos[i];
                if (param.GetCustomAttribute<BeforeChangeAttribute>() != null)
                {
                    beforeObject = parameters[i];
                    if (beforeObject != null)
                    {
                        entityType = beforeObject.GetType().Name;
                    }
                }
                else if (param.GetCustomAttribute<AfterChangeAttribute>() != null)
                {
                    afterObject = parameters[i];
                }

                // Look for entity ID
                if (string.IsNullOrEmpty(entityId) &&
                    (param.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                     param.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    entityId = parameters[i]?.ToString();
                }
            }

            // Execute the method and track any changes
            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            TResult result = default;

            try
            {
                // Log method entry
                LogMethodEntry(methodName, declaringType, parameters, parameterInfos, logAttribute);

                // Execute the method
                result = methodCall();

                // If afterObject wasn't provided directly, it might be in the result
                if (afterObject == null && result != null &&
                    (beforeObject == null || beforeObject.GetType().IsAssignableFrom(result.GetType())))
                {
                    afterObject = result;
                }

                // Log data changes if we have both before and after objects
                if (beforeObject != null && afterObject != null && _dataConfiguration.EnableDataChangeTracking)
                {
                    var changes = ObjectComparer.CompareObjects(
                        beforeObject,
                        afterObject,
                        dataChangeAttribute.MaxComparisonDepth);

                    if (changes.Count > 0)
                    {
                        // Build context from the global context
                        var context = new Dictionary<string, object>();
                        foreach (var ctxItem in CurrentContext)
                        {
                            context[ctxItem.Key] = ctxItem.Value;
                        }

                        // Enhance the log entry with data change information
                        var entry = new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            MethodName = methodName,
                            ClassName = declaringType.Name,
                            Level = logAttribute?.Level ?? _dataConfiguration.DataChangeLogLevel,
                            Message = $"Data changes in {entityType ?? beforeObject.GetType().Name}",
                            DataChanges = changes,
                            EntityType = entityType ?? beforeObject.GetType().Name,
                            EntityId = entityId,
                            OperationType = methodName, // Use method name as operation type by default
                            CorrelationId = CorrelationManager.CurrentCorrelationId,
                            ThreadId = Thread.CurrentThread.ManagedThreadId.ToString(),
                            Context = context
                        };

                        // Log full before state if configured
                        if (_dataConfiguration.LogBeforeState)
                            entry.Context["BeforeState"] = LogFormatter.FormatForLog(beforeObject, "BeforeState", null);

                        // Log full after state if configured
                        if (_dataConfiguration.LogAfterState)
                            entry.Context["AfterState"] = LogFormatter.FormatForLog(afterObject, "AfterState", null);

                        _configuration.Logger.Log(entry);
                    }
                }

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

                // Log method exit
                LogMethodExit(methodName, declaringType, parameters, parameterInfos,
                    result, stopwatch.Elapsed, exception, logAttribute);
            }
        }

        /// <summary>
        /// Log an async method with data change tracking
        /// </summary>
        public static async Task<TResult> LogMethodWithDataChangesAsync<TResult>(
            Func<Task<TResult>> methodCall,
            string methodName,
            Type declaringType,
            object[] parameters,
            ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute,
            TrackDataChangesAttribute dataChangeAttribute)
        {
            // Find before/after parameters
            object beforeObject = null;
            string entityType = null;
            string entityId = null;

            for (int i = 0; i < parameters.Length && i < parameterInfos.Length; i++)
            {
                var param = parameterInfos[i];
                if (param.GetCustomAttribute<BeforeChangeAttribute>() != null)
                {
                    beforeObject = parameters[i];
                    if (beforeObject != null)
                    {
                        entityType = beforeObject.GetType().Name;
                    }
                }

                // Look for entity ID
                if (string.IsNullOrEmpty(entityId) &&
                    (param.Name.Equals("id", StringComparison.OrdinalIgnoreCase) ||
                     param.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)))
                {
                    entityId = parameters[i]?.ToString();
                }
            }

            // Execute the method and track any changes
            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            TResult result = default;

            try
            {
                // Log method entry
                LogMethodEntry(methodName, declaringType, parameters, parameterInfos, logAttribute);

                // Execute the method
                result = await methodCall();

                // Use result as after object if appropriate
                if (beforeObject != null && result != null &&
                    beforeObject.GetType().IsAssignableFrom(result.GetType()) &&
                    _dataConfiguration.EnableDataChangeTracking)
                {
                    var afterObject = result;

                    var changes = ObjectComparer.CompareObjects(
                        beforeObject,
                        afterObject,
                        dataChangeAttribute.MaxComparisonDepth);

                    if (changes.Count > 0)
                    {
                        // Build context from the global context
                        var context = new Dictionary<string, object>();
                        foreach (var ctxItem in CurrentContext)
                        {
                            context[ctxItem.Key] = ctxItem.Value;
                        }

                        // Enhance the log entry with data change information
                        var entry = new LogEntry
                        {
                            Timestamp = DateTime.UtcNow,
                            MethodName = methodName,
                            ClassName = declaringType.Name,
                            Level = logAttribute?.Level ?? _dataConfiguration.DataChangeLogLevel,
                            Message = $"Data changes in {entityType ?? beforeObject.GetType().Name}",
                            DataChanges = changes,
                            EntityType = entityType ?? beforeObject.GetType().Name,
                            EntityId = entityId,
                            OperationType = methodName, // Use method name as operation type by default
                            CorrelationId = CorrelationManager.CurrentCorrelationId,
                            ThreadId = Thread.CurrentThread.ManagedThreadId.ToString(),
                            Context = context
                        };

                        // Log full before state if configured
                        if (_dataConfiguration.LogBeforeState)
                            entry.Context["BeforeState"] = LogFormatter.FormatForLog(beforeObject, "BeforeState", null);

                        // Log full after state if configured
                        if (_dataConfiguration.LogAfterState)
                            entry.Context["AfterState"] = LogFormatter.FormatForLog(afterObject, "AfterState", null);

                        _configuration.Logger.Log(entry);
                    }
                }

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

                // Log method exit
                LogMethodExit(methodName, declaringType, parameters, parameterInfos,
                    result, stopwatch.Elapsed, exception, logAttribute);
            }
        }

        /// <summary>
        /// Log an async method with no return value with data change tracking
        /// </summary>
        public static async Task LogMethodWithDataChangesAsync(
            Func<Task> methodCall,
            string methodName,
            Type declaringType,
            object[] parameters,
            ParameterInfo[] parameterInfos,
            LogAttributeBase logAttribute,
            TrackDataChangesAttribute dataChangeAttribute)
        {
            // We don't have an elegant way to compare before/after for void async methods
            // So we'll just do regular logging
            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;

            try
            {
                // Log method entry
                LogMethodEntry(methodName, declaringType, parameters, parameterInfos, logAttribute);

                // Execute the method
                await methodCall();
            }
            catch (Exception ex)
            {
                exception = ex;
                throw;
            }
            finally
            {
                stopwatch.Stop();

                // Log method exit
                LogMethodExit(methodName, declaringType, parameters, parameterInfos,
                    null, stopwatch.Elapsed, exception, logAttribute);
            }
        }

        #endregion

        #region Core Logging Methods

        /// <summary>
        /// Log method entry
        /// </summary>
        public static void LogMethodEntry(string methodName, Type declaringType, object[] parameters,
            ParameterInfo[] parameterInfos, LogAttributeBase logAttribute)
        {
            if (!ShouldLog(logAttribute)) return;

            var entry = CreateBaseLogEntry(methodName, declaringType, logAttribute);
            entry.Message = $"Entering {declaringType.Name}.{methodName}";

            if (_configuration.EnableParameterLogging && logAttribute.IncludeParameters && parameters != null)
            {
                entry.Parameters = CreateParameterDictionary(parameters, parameterInfos);
            }

            // Add context information
            EnrichWithContext(entry);

            _configuration.Logger.Log(entry);
        }

        /// <summary>
        /// Log method exit
        /// </summary>
        public static void LogMethodExit(string methodName, Type declaringType, object[] parameters,
            ParameterInfo[] parameterInfos,
            object result, TimeSpan executionTime, Exception exception, LogAttributeBase logAttribute)
        {
            // Prevent duplicate logging
            if (!ShouldLog(logAttribute) && exception == null) return;

            var entry = CreateBaseLogEntry(methodName, declaringType, logAttribute);
            entry.Level = exception != null ? LogLevel.Error : logAttribute?.Level ?? LogLevel.Info;
            entry.Message = exception != null
                ? $"Exception in {declaringType.Name}.{methodName}"
                : $"Exiting {declaringType.Name}.{methodName}";
            entry.Exception = exception;

            if (_configuration.EnableParameterLogging &&
                logAttribute?.IncludeParameters == true && parameters != null)
            {
                entry.Parameters = CreateParameterDictionary(parameters, parameterInfos);
            }

            if (_configuration.EnableExecutionTimeLogging &&
                logAttribute?.IncludeExecutionTime == true)
            {
                entry.ExecutionTime = executionTime;
            }

            if (_configuration.EnableReturnValueLogging &&
                logAttribute?.IncludeReturnValue == true && result != null && exception == null)
            {
                entry.ReturnValue = SummarizeReturnValue(result);
            }

            // Add context information
            EnrichWithContext(entry);

            _configuration.Logger.Log(entry);
        }

        /// <summary>
        /// Create base log entry
        /// </summary>
        private static LogEntry CreateBaseLogEntry(string methodName, Type declaringType, LogAttributeBase logAttribute)
        {
            return new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                MethodName = methodName,
                ClassName = declaringType.Name,
                Level = logAttribute?.Level ?? LogLevel.Info,
                CorrelationId = CorrelationManager.CurrentCorrelationId,
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString()
            };
        }

        /// <summary>
        /// Create parameter dictionary
        /// </summary>
        private static Dictionary<string, object> CreateParameterDictionary(object[] parameters,
            ParameterInfo[] parameterInfos)
        {
            var dict = new Dictionary<string, object>();

            if (parameters == null || parameterInfos == null)
                return dict;

            for (int i = 0; i < parameters.Length && i < parameterInfos.Length; i++)
            {
                var paramInfo = parameterInfos[i];
                var paramValue = parameters[i];

                // Check if parameter should be excluded from logs
                var excludeAttr = paramInfo.GetCustomAttribute<ExcludeFromLogsAttribute>();
                if (excludeAttr != null)
                {
                    dict[paramInfo.Name] = "[EXCLUDED]";
                    continue;
                }

                // Check if parameter should be masked in logs
                var maskAttr = paramInfo.GetCustomAttribute<MaskInLogsAttribute>();
                if (maskAttr != null && paramValue != null)
                {
                    string value = paramValue.ToString();
                    string maskedValue = ApplyMask(value, maskAttr);
                    dict[paramInfo.Name] = maskedValue;
                    continue;
                }

                // Check if parameter contents should be redacted
                var redactAttr = paramInfo.GetCustomAttribute<RedactContentsAttribute>();
                if (redactAttr != null)
                {
                    dict[paramInfo.Name] = redactAttr.ReplacementText;
                    continue;
                }

                // Handle dictionaries specially
                if (paramValue is Dictionary<string, string> stringDict)
                {
                    // Convert directly to a string representation
                    var entries = stringDict.Select(kvp => $"{kvp.Key}: {MaskIfSensitive(kvp.Key, kvp.Value)}");
                    dict[paramInfo.Name] = $"{{ {string.Join(", ", entries)} }}";
                }
                else if (paramValue is IDictionary ||
                         (paramValue?.GetType().IsGenericType == true &&
                          paramValue.GetType().GetGenericTypeDefinition() == typeof(Dictionary<,>)))
                {
                    // Force early conversion to string for dictionaries
                    dict[paramInfo.Name] = FormatDictionaryToString(paramValue);
                }
                else
                {
                    // Use LogFormatter for standard formatting with sensitivity handling
                    dict[paramInfo.Name] = LogFormatter.FormatForLog(paramValue, paramInfo.Name, paramInfo);
                }
            }

            return dict;
        }

        /// <summary>
        /// Apply masking pattern to a string
        /// </summary>
        private static string ApplyMask(string value, MaskInLogsAttribute maskAttr)
        {
            if (string.IsNullOrEmpty(value))
                return value;

            if (!maskAttr.ShowFirstChars && !maskAttr.ShowLastChars)
                return maskAttr.MaskingPattern;

            var sb = new System.Text.StringBuilder();

            if (maskAttr.ShowFirstChars && value.Length > maskAttr.FirstCharsCount)
            {
                sb.Append(value.Substring(0, Math.Min(maskAttr.FirstCharsCount, value.Length)));
            }

            sb.Append(maskAttr.MaskingPattern);

            if (maskAttr.ShowLastChars && value.Length > maskAttr.LastCharsCount)
            {
                int startIndex = Math.Max(0, value.Length - maskAttr.LastCharsCount);
                sb.Append(value.Substring(startIndex));
            }

            return sb.ToString();
        }

        /// <summary>
        /// Check if we should log based on environment and configuration
        /// </summary>
        private static bool ShouldLog(LogAttributeBase logAttribute)
        {
            if (logAttribute == null)
                return _configuration?.Logger?.IsEnabled(LogLevel.Info) ?? false;

            if (logAttribute is LogDebugAttribute && _configuration.Environment == EnvironmentType.Production)
            {
                return false;
            }

            return _configuration?.Logger?.IsEnabled(logAttribute.Level) ?? false;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Helper method to mask sensitive dictionary values
        /// </summary>
        private static string MaskIfSensitive(string key, string value)
        {
            // Check for common sensitive keys
            var sensitiveKeys = new[]
            {
                "password", "secret", "key", "token", "auth", "credentials", "pwd", "social", "ssn", "credit",
                "card", "cvv", "pin", "security"
            };

            foreach (var sensitiveKey in sensitiveKeys)
            {
                if (key.IndexOf(sensitiveKey, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return "***";
                }
            }

            return value;
        }

        /// <summary>
        /// Format a dictionary to string
        /// </summary>
        private static string FormatDictionaryToString(object dictObj)
        {
            // Handle Dictionary<string, string>
            if (dictObj is Dictionary<string, string> stringDict)
            {
                var entries = stringDict.Select(kvp => $"{kvp.Key}: {MaskIfSensitive(kvp.Key, kvp.Value)}");
                return $"{{ {string.Join(", ", entries)} }}";
            }

            // Handle Dictionary<string, object>  
            if (dictObj is Dictionary<string, object> objDict)
            {
                var entries = objDict.Select(kvp =>
                    $"{kvp.Key}: {(MaskIfSensitive(kvp.Key, kvp.Value?.ToString() ?? "null"))}");
                return $"{{ {string.Join(", ", entries)} }}";
            }

            // Handle IDictionary
            if (dictObj is IDictionary dict)
            {
                var entries = new List<string>();
                foreach (DictionaryEntry entry in dict)
                {
                    var key = entry.Key?.ToString() ?? "null";
                    var value = entry.Value?.ToString() ?? "null";
                    entries.Add($"{key}: {MaskIfSensitive(key, value)}");
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
                            var key = keyProp.GetValue(item, null)?.ToString() ?? "null";
                            var value = valueProp.GetValue(item, null)?.ToString() ?? "null";
                            entries.Add($"{key}: {MaskIfSensitive(key, value)}");
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

        /// <summary>
        /// Format a collection for logging
        /// </summary>
        private static string FormatCollection(IEnumerable collection, string typeName, int count, int maxDepth,
            int currentDepth)
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

        /// <summary>
        /// Get sample items from a collection
        /// </summary>
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

        /// <summary>
        /// Get element type name from an array
        /// </summary>
        private static string GetElementTypeName(Array array)
        {
            var elementType = array.GetType().GetElementType();
            return elementType?.Name ?? "object";
        }

        /// <summary>
        /// Format object value for parameters
        /// </summary>
        private static object FormatParameterValue(object value, int maxDepth = 3, int currentDepth = 0)
        {
            if (value == null) return "null";
            if (currentDepth >= maxDepth) return $"{value.GetType().Name} (max depth reached)";

            // Handle strings specially to truncate if needed
            if (value is string str)
            {
                const int maxStringLength = 100;
                return str.Length > maxStringLength
                    ? $"{str.Substring(0, maxStringLength)}... (length: {str.Length})"
                    : str;
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

                    var key = entry.Key?.ToString() ?? "null";
                    formattedEntries.Add($"{key}: {FormatParameterValue(entry.Value, maxDepth, currentDepth + 1)}");
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
                        var key = keyProperty.GetValue(entry)?.ToString() ?? "null";
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
                            // Check if property has sensitivity attributes
                            if (p.GetCustomAttribute<ExcludeFromLogsAttribute>() != null)
                                return $"{p.Name}: [EXCLUDED]";

                            if (p.GetCustomAttribute<MaskInLogsAttribute>() != null)
                            {
                                var propValue = p.GetValue(value)?.ToString();
                                var maskAttr = p.GetCustomAttribute<MaskInLogsAttribute>();
                                return $"{p.Name}: {ApplyMask(propValue, maskAttr)}";
                            }

                            if (p.GetCustomAttribute<RedactContentsAttribute>() != null)
                            {
                                var redactAttr = p.GetCustomAttribute<RedactContentsAttribute>();
                                return $"{p.Name}: {redactAttr.ReplacementText}";
                            }

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

        /// <summary>
        /// Format a simple value 
        /// </summary>
        private static object FormatValue(object value, int maxDepth, int currentDepth)
        {
            // Reuse existing method for consistency
            return FormatParameterValue(value, maxDepth, currentDepth);
        }

        /// <summary>
        /// Summarize a method's return value for logging
        /// </summary>
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
                return str.Length > maxStringLength
                    ? $"{str.Substring(0, maxStringLength)}... (length: {str.Length})"
                    : str;
            }

            // Handle common collection types
            if (result is Array arrayResult)
            {
                var sampleItems = GetSampleItems(arrayResult, 3)
                    .Select(i => SummarizeReturnValue(i, maxDepth, currentDepth + 1))
                    .ToList();

                return
                    $"Array<{GetElementTypeName(arrayResult)}>[{arrayResult.Length}] {{{string.Join(", ", sampleItems)}}}";
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

                            entries.Add(
                                $"{FormatValue(key, maxDepth, currentDepth + 1)}: {FormatValue(value, maxDepth, currentDepth + 1)}");
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
                                // Check if property has sensitivity attributes
                                if (p.GetCustomAttribute<ExcludeFromLogsAttribute>() != null)
                                    return $"{p.Name}: [EXCLUDED]";

                                if (p.GetCustomAttribute<MaskInLogsAttribute>() != null)
                                {
                                    var propValue = p.GetValue(result)?.ToString();
                                    var maskAttr = p.GetCustomAttribute<MaskInLogsAttribute>();
                                    return $"{p.Name}: {ApplyMask(propValue, maskAttr)}";
                                }

                                if (p.GetCustomAttribute<RedactContentsAttribute>() != null)
                                {
                                    var redactAttr = p.GetCustomAttribute<RedactContentsAttribute>();
                                    return $"{p.Name}: {redactAttr.ReplacementText}";
                                }

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

        #endregion
    }
}