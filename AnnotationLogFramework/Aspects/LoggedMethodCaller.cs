using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;

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
            var dataChangeAttribute = method.GetCustomAttribute<TrackDataChangesAttribute>();
            
            if (logAttribute == null && dataChangeAttribute == null)
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
            
            // If we have data change tracking, use specialized handling
            if (dataChangeAttribute != null && LogManager.GetDataConfiguration().EnableDataChangeTracking)
            {
                if (logAttribute != null)
                {
                    // Have both log and data change attributes
                    return LogManager.LogMethodWithDataChanges(
                        methodCall.Compile(), 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        logAttribute,
                        dataChangeAttribute);
                }
                else
                {
                    // Only have data change attribute, create a default LogAttribute
                    var defaultLogAttribute = new LogAttribute(LogManager.GetDataConfiguration().DataChangeLogLevel);
                    return LogManager.LogMethodWithDataChanges(
                        methodCall.Compile(), 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        defaultLogAttribute,
                        dataChangeAttribute);
                }
            }
            
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
            var dataChangeAttribute = method.GetCustomAttribute<TrackDataChangesAttribute>();
            
            if (logAttribute == null && dataChangeAttribute == null)
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
            
            // For void methods, we'll wrap in a bool-returning function to use our existing infrastructure
            if (dataChangeAttribute != null && LogManager.GetDataConfiguration().EnableDataChangeTracking)
            {
                Func<bool> wrappedMethod = () => {
                    methodCall.Compile()();
                    return true;
                };
                
                if (logAttribute != null)
                {
                    // Have both log and data change attributes
                    LogManager.LogMethodWithDataChanges(
                        wrappedMethod, 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        logAttribute,
                        dataChangeAttribute);
                }
                else
                {
                    // Only have data change attribute, create a default LogAttribute
                    var defaultLogAttribute = new LogAttribute(LogManager.GetDataConfiguration().DataChangeLogLevel);
                    LogManager.LogMethodWithDataChanges(
                        wrappedMethod, 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        defaultLogAttribute,
                        dataChangeAttribute);
                }
            }
            else
            {
                // Use our LogManager to handle the method with logging
                LogManager.LogMethod(
                    methodCall.Compile(), 
                    method.Name, 
                    method.DeclaringType, 
                    parameters, 
                    parameterInfos, 
                    logAttribute);
            }
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
            var dataChangeAttribute = method.GetCustomAttribute<TrackDataChangesAttribute>();
            
            if (logAttribute == null && dataChangeAttribute == null)
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
            
            // If we have data change tracking, use specialized handling
            if (dataChangeAttribute != null && LogManager.GetDataConfiguration().EnableDataChangeTracking)
            {
                if (logAttribute != null)
                {
                    // Have both log and data change attributes
                    return await LogManager.LogMethodWithDataChangesAsync<T>(
                        methodCall.Compile(), 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        logAttribute,
                        dataChangeAttribute);
                }
                else
                {
                    // Only have data change attribute, create a default LogAttribute
                    var defaultLogAttribute = new LogAttribute(LogManager.GetDataConfiguration().DataChangeLogLevel);
                    return await LogManager.LogMethodWithDataChangesAsync<T>(
                        methodCall.Compile(), 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        defaultLogAttribute,
                        dataChangeAttribute);
                }
            }
            
            // Use our LogManager to handle the async method with logging
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
            var dataChangeAttribute = method.GetCustomAttribute<TrackDataChangesAttribute>();
            
            if (logAttribute == null && dataChangeAttribute == null)
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
            
            // For void-returning async methods
            if (dataChangeAttribute != null && LogManager.GetDataConfiguration().EnableDataChangeTracking)
            {
                if (logAttribute != null)
                {
                    // Have both log and data change attributes
                    await LogManager.LogMethodWithDataChangesAsync(
                        methodCall.Compile(), 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        logAttribute,
                        dataChangeAttribute);
                }
                else
                {
                    // Only have data change attribute, create a default LogAttribute
                    var defaultLogAttribute = new LogAttribute(LogManager.GetDataConfiguration().DataChangeLogLevel);
                    await LogManager.LogMethodWithDataChangesAsync(
                        methodCall.Compile(), 
                        method.Name, 
                        method.DeclaringType, 
                        parameters, 
                        parameterInfos, 
                        defaultLogAttribute,
                        dataChangeAttribute);
                }
            }
            else
            {
                // Use our LogManager to handle the async method with logging
                await LogManager.LogMethodAsync(
                    methodCall.Compile(), 
                    method.Name, 
                    method.DeclaringType, 
                    parameters, 
                    parameterInfos, 
                    logAttribute);
            }
        }
    }
}