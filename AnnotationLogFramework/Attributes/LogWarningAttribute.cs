namespace AnnotationLogger
{
    /// <summary>
    /// Warning logging attribute - for potential issues that aren't errors
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogWarningAttribute : LogAttributeBase
    {
        public LogWarningAttribute() : base(LogLevel.Warning) { }
    }
}