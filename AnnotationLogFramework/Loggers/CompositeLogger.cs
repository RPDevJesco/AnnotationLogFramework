namespace AnnotationLogger
{
    /// <summary>
    /// Multi-logger that can send logs to multiple destinations
    /// </summary>
    public class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers = new List<ILogger>();
        private readonly LogLevel _minimumLevel;

        public CompositeLogger(LogLevel minimumLevel = LogLevel.Info)
        {
            _minimumLevel = minimumLevel;
        }
        
        public CompositeLogger(IEnumerable<ILogger> loggers, LogLevel minimumLevel = LogLevel.Info)
        {
            _loggers.AddRange(loggers);
            _minimumLevel = minimumLevel;
        }
        
        public void AddLogger(ILogger logger)
        {
            _loggers.Add(logger);
        }

        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Level)) return;
            
            foreach (var logger in _loggers)
            {
                logger.Log(entry);
            }
        }

        public bool IsEnabled(LogLevel level)
        {
            return level >= _minimumLevel && _loggers.Any(l => l.IsEnabled(level));
        }
    }
}