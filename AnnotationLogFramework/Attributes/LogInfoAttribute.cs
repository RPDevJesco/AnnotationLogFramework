namespace AnnotationLogger
{
    /// <summary>
    /// Info logging attribute - for standard operational messages
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogInfoAttribute : LogAttributeBase
    {
        public LogInfoAttribute() : base(LogLevel.Info) { }
    }
}