namespace AnnotationLogger
{
    /// <summary>
    /// Debug logging attribute - only logs in debug environments
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogDebugAttribute : LogAttributeBase
    {
        public LogDebugAttribute() : base(LogLevel.Debug) { }
    }
}