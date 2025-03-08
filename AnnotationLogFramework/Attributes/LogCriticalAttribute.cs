namespace AnnotationLogger
{
    /// <summary>
    /// Critical logging attribute - for severe errors where the application cannot continue
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogCriticalAttribute : LogAttributeBase
    {
        public LogCriticalAttribute() : base(LogLevel.Critical) { }
    }
}