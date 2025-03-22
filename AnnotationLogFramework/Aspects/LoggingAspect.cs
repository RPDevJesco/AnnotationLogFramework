using System.Reflection;
using System.Runtime.CompilerServices;

namespace AnnotationLogger
{
    /// <summary>
    /// Legacy/simpler API for method interception - requires more manual handling
    /// </summary>
    public static class LoggingAspect
    {
        public static void WrapMethod(Action originalMethod, 
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var method = originalMethod.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null) 
            {
                originalMethod();
                return;
            }
            
            var parameters = new object[0]; // No parameters for Action
            var parameterInfos = method.GetParameters();
            
            LogManager.LogMethod(originalMethod, methodName, method.DeclaringType, parameters, parameterInfos, logAttribute);
        }
        
        public static T WrapMethod<T>(Func<T> originalMethod,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var method = originalMethod.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null) 
            {
                return originalMethod();
            }
            
            var parameters = new object[0]; // No parameters for this Func
            var parameterInfos = method.GetParameters();
            
            return LogManager.LogMethod(originalMethod, methodName, method.DeclaringType, parameters, parameterInfos, logAttribute);
        }
        
        // Add more overloads for methods with parameters
        public static T WrapMethod<T, TParam1>(Func<TParam1, T> originalMethod, TParam1 param1,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var method = originalMethod.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null) 
            {
                return originalMethod(param1);
            }
            
            var parameters = new object[] { param1 };
            var parameterInfos = method.GetParameters();
            
            return LogManager.LogMethod(() => originalMethod(param1), methodName, method.DeclaringType, parameters, parameterInfos, logAttribute);
        }
        
        // For async methods
        public static async Task<T> WrapMethodAsync<T>(Func<Task<T>> originalMethod,
            [CallerMemberName] string methodName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            var method = originalMethod.Method;
            var logAttribute = method.GetCustomAttribute<LogAttributeBase>();
            
            if (logAttribute == null) 
            {
                return await originalMethod();
            }
            
            var parameters = new object[0];
            var parameterInfos = method.GetParameters();
            
            return await LogManager.LogMethodAsync(originalMethod, methodName, method.DeclaringType, parameters, parameterInfos, logAttribute);
        }
    }
}