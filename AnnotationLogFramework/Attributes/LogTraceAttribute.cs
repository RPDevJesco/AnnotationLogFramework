namespace AnnotationLogger
{
    /// <summary>
    /// Trace logging attribute - for highly detailed diagnostic information
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogTraceAttribute : LogAttributeBase
    {
        public LogTraceAttribute() : base(LogLevel.Trace) { }
    }
}