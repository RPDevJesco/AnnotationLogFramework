namespace AnnotationLogger.Routing
{
    /// <summary>
    /// Routes log entries to different destinations based on configurable conditions.
    /// </summary>
    public class LogRouter : ILogger
    {
        private readonly List<(Predicate<LogEntry> Condition, ILogger Logger)> _routes = 
            new List<(Predicate<LogEntry>, ILogger)>();
        
        private readonly ILogger _defaultLogger;
        
        public LogRouter(ILogger defaultLogger)
        {
            _defaultLogger = defaultLogger ?? throw new ArgumentNullException(nameof(defaultLogger));
        }
        
        /// <summary>
        /// Adds a routing rule.
        /// </summary>
        /// <returns>This instance for method chaining</returns>
        public LogRouter AddRoute(Predicate<LogEntry> condition, ILogger logger)
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));
            if (logger == null) throw new ArgumentNullException(nameof(logger));
            
            _routes.Add((condition, logger));
            return this;
        }
        
        public void Log(LogEntry entry)
        {
            bool routed = false;
            
            foreach (var route in _routes)
            {
                if (route.Condition(entry))
                {
                    route.Logger.Log(entry);
                    routed = true;
                }
            }
            
            // If no routes matched and we have a default logger, use it
            if (!routed)
            {
                _defaultLogger.Log(entry);
            }
        }
        
        public bool IsEnabled(LogLevel level)
        {
            // Enable if any of the loggers would accept this level
            return _routes.Any(r => r.Logger.IsEnabled(level)) || _defaultLogger.IsEnabled(level);
        }
    }
}