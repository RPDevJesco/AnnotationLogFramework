namespace AnnotationLogger
{
    /// <summary>
    /// Main logger interface
    /// </summary>
    public interface ILogger
    {
        void Log(LogEntry entry);
        bool IsEnabled(LogLevel level);
    }
}