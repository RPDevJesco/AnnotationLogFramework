namespace AnnotationLogger
{
    /// <summary>
    /// Error logging attribute - for failures and exceptions
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogErrorAttribute : LogAttributeBase
    {
        public LogErrorAttribute() : base(LogLevel.Error) { }
    }
}