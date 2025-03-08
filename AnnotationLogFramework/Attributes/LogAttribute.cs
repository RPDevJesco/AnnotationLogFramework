namespace AnnotationLogger
{
    /// <summary>
    /// Custom log level attribute - for more precise control
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogAttribute : LogAttributeBase
    {
        public LogAttribute(LogLevel level) : base(level) { }
    }
}