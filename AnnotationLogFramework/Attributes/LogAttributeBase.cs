namespace AnnotationLogger
{
    /// <summary>
    /// Base attribute for method logging
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public abstract class LogAttributeBase : Attribute, IProgressLoggingOptions
    {
        public LogLevel Level { get; }
        public bool IncludeParameters { get; set; } = true;
        public bool IncludeReturnValue { get; set; } = true;
        public bool IncludeExecutionTime { get; set; } = true;
        public bool EnableProgressLogging { get; set; }
        public int ProgressLoggingIntervalMs { get; set; } = 5000;
        protected LogAttributeBase(LogLevel level)
        {
            Level = level;
        }
    }
}