namespace AnnotationLogger
{
    /// <summary>
    /// Production logging attribute - logs in all environments
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class LogProdAttribute : LogAttributeBase
    {
        public LogProdAttribute() : base(LogLevel.Info) { }
    }
}