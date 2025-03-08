using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

namespace AnnotationLogger
{
    /// <summary>
    /// Helper to call methods with logging attributes, extracting all method information
    /// automatically from expressions. This is the primary API for using the annotation logging.
    /// </summary>
    public static class LoggedMethodCaller
    {
        /// <summary>
        /// Call a method with automatic logging based on method attributes
        /// </summary>
        /// <typeparam name="T">Return type of the method</typeparam>
        /// <param name="methodCall">Expression representing the method call</param>
        /// <returns>The result of the method execution</returns>
        public static T Call<T>(Expression<Func<T>> methodCall)
        {
            // Extract method info from the expression
            var methodCallBody = methodCall.Body as MethodCallExpression;
            if (methodCallBody == null)
            {
                // If not a method call, just execute the original lambda
                return methodCall.Compile()();
            }

            var method = methodCallBody.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null)
            {
                // No logging attribute, just execute the method
                return methodCall.Compile()();
            }
            
            // Extract parameters from the method call expression
            var parameters = methodCallBody.Arguments
                .Select(arg => 
                {
                    // We need to compile and execute each argument expression
                    var lambda = Expression.Lambda(arg).Compile();
                    return lambda.DynamicInvoke();
                })
                .ToArray();
            
            var parameterInfos = method.GetParameters();
            
            // Use our LogManager to handle the method with logging
            return LogManager.LogMethod(
                methodCall.Compile(), 
                method.Name, 
                method.DeclaringType, 
                parameters, 
                parameterInfos, 
                logAttribute);
        }
        
        /// <summary>
        /// Call a method with no return value with automatic logging based on method attributes
        /// </summary>
        /// <param name="methodCall">Expression representing the method call</param>
        public static void Call(Expression<Action> methodCall)
        {
            // Extract method info from the expression
            var methodCallBody = methodCall.Body as MethodCallExpression;
            if (methodCallBody == null)
            {
                // If not a method call, just execute the original lambda
                methodCall.Compile()();
                return;
            }

            var method = methodCallBody.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null)
            {
                // No logging attribute, just execute the method
                methodCall.Compile()();
                return;
            }
            
            // Extract parameters from the method call expression
            var parameters = methodCallBody.Arguments
                .Select(arg => 
                {
                    // We need to compile and execute each argument expression
                    var lambda = Expression.Lambda(arg).Compile();
                    return lambda.DynamicInvoke();
                })
                .ToArray();
            
            var parameterInfos = method.GetParameters();
            
            // Use our LogManager to handle the method with logging
            LogManager.LogMethod(
                methodCall.Compile(), 
                method.Name, 
                method.DeclaringType, 
                parameters, 
                parameterInfos, 
                logAttribute);
        }
        
        /// <summary>
        /// Call an async method with automatic logging based on method attributes
        /// </summary>
        /// <typeparam name="T">Return type of the async method</typeparam>
        /// <param name="methodCall">Expression representing the async method call</param>
        /// <returns>Task representing the async operation with the result</returns>
        public static async Task<T> CallAsync<T>(Expression<Func<Task<T>>> methodCall)
        {
            // Extract method info from the expression
            var methodCallBody = methodCall.Body as MethodCallExpression;
            if (methodCallBody == null)
            {
                // If not a method call, just execute the original lambda
                return await methodCall.Compile()();
            }

            var method = methodCallBody.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null)
            {
                // No logging attribute, just execute the method
                return await methodCall.Compile()();
            }
            
            // Extract parameters from the method call expression
            var parameters = methodCallBody.Arguments
                .Select(arg => 
                {
                    // We need to compile and execute each argument expression
                    var lambda = Expression.Lambda(arg).Compile();
                    return lambda.DynamicInvoke();
                })
                .ToArray();
            
            var parameterInfos = method.GetParameters();
            
            // Use our LogManager to handle the async method with logging
            // Explicitly specify the type parameter to help type inference
            return await LogManager.LogMethodAsync<T>(
                methodCall.Compile(), 
                method.Name, 
                method.DeclaringType, 
                parameters, 
                parameterInfos, 
                logAttribute);
        }
        
        /// <summary>
        /// Call an async method with no return value with automatic logging based on method attributes
        /// </summary>
        /// <param name="methodCall">Expression representing the async method call</param>
        /// <returns>Task representing the async operation</returns>
        public static async Task CallAsync(Expression<Func<Task>> methodCall)
        {
            // Extract method info from the expression
            var methodCallBody = methodCall.Body as MethodCallExpression;
            if (methodCallBody == null)
            {
                // If not a method call, just execute the original lambda
                await methodCall.Compile()();
                return;
            }

            var method = methodCallBody.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null)
            {
                // No logging attribute, just execute the method
                await methodCall.Compile()();
                return;
            }
            
            // Extract parameters from the method call expression
            var parameters = methodCallBody.Arguments
                .Select(arg => 
                {
                    // We need to compile and execute each argument expression
                    var lambda = Expression.Lambda(arg).Compile();
                    return lambda.DynamicInvoke();
                })
                .ToArray();
            
            var parameterInfos = method.GetParameters();
            
            // We need a separate implementation for void async methods
            var stopwatch = Stopwatch.StartNew();
            Exception exception = null;
            
            try
            {
                // Log method entry if appropriate
                if (ShouldLogMethodEntry(logAttribute))
                {
                    LogMethodEntry(method.Name, method.DeclaringType, parameters, parameterInfos, logAttribute);
                }
                
                // Execute the method
                await methodCall.Compile()();
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
                if (ShouldLogMethodExit(logAttribute, exception))
                {
                    LogMethodExit(method.Name, method.DeclaringType, parameters, parameterInfos, 
                        null, stopwatch.Elapsed, exception, logAttribute);
                }
            }
        }
        
        // Helper methods for logging async void methods
        private static bool ShouldLogMethodEntry(LogAttributeBase logAttribute)
        {
            return _configuration?.EnableMethodEntryExit == true && 
                   logAttribute.Level <= LogLevel.Debug;
        }
        
        private static bool ShouldLogMethodExit(LogAttributeBase logAttribute, Exception exception)
        {
            return (_configuration?.EnableMethodEntryExit == true) || 
                   exception != null;
        }
        
        private static void LogMethodEntry(string methodName, Type declaringType, 
            object[] parameters, ParameterInfo[] parameterInfos, LogAttributeBase logAttribute)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                MethodName = methodName,
                ClassName = declaringType.Name,
                Level = logAttribute.Level,
                Message = $"Entering {declaringType.Name}.{methodName}"
            };

            if (_configuration?.EnableParameterLogging == true && 
                logAttribute.IncludeParameters && parameters != null)
            {
                entry.Parameters = CreateParameterDictionary(parameters, parameterInfos);
            }

            _configuration?.Logger?.Log(entry);
        }
        
        private static void LogMethodExit(string methodName, Type declaringType, 
            object[] parameters, ParameterInfo[] parameterInfos, object result, 
            TimeSpan executionTime, Exception exception, LogAttributeBase logAttribute)
        {
            var level = exception != null ? LogLevel.Error : logAttribute.Level;
            
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                MethodName = methodName,
                ClassName = declaringType.Name,
                Level = level,
                Message = exception != null 
                    ? $"Exception in {declaringType.Name}.{methodName}" 
                    : $"Exiting {declaringType.Name}.{methodName}",
                Exception = exception
            };

            if (_configuration?.EnableParameterLogging == true && 
                logAttribute.IncludeParameters && parameters != null)
            {
                entry.Parameters = CreateParameterDictionary(parameters, parameterInfos);
            }

            if (_configuration?.EnableReturnValueLogging == true && 
                logAttribute.IncludeReturnValue && result != null && exception == null)
            {
                entry.ReturnValue = result;
            }

            if (_configuration?.EnableExecutionTimeLogging == true && 
                logAttribute.IncludeExecutionTime)
            {
                entry.ExecutionTime = executionTime;
            }

            _configuration?.Logger?.Log(entry);
        }
        
        private static Dictionary<string, object> CreateParameterDictionary(
            object[] parameters, ParameterInfo[] parameterInfos)
        {
            var dict = new Dictionary<string, object>();
            
            if (parameters == null || parameterInfos == null) return dict;
            
            for (int i = 0; i < parameters.Length && i < parameterInfos.Length; i++)
            {
                dict[parameterInfos[i].Name] = parameters[i];
            }
            
            return dict;
        }
        
        // Reference to configuration for the helper methods
        private static LoggerConfiguration _configuration => 
            LogManager.GetConfiguration();
        
    }
}