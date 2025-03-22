namespace AnnotationLogger
{
    // Add an attribute for throttling
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class ThrottleLoggingAttribute : Attribute
    {
        public int MaxLogsPerSecond { get; set; } = 10;
        
        public ThrottleLoggingAttribute(int maxLogsPerSecond = 10)
        {
            MaxLogsPerSecond = maxLogsPerSecond;
        }
    }
}