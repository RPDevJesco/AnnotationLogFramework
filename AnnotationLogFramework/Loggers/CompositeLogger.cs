namespace AnnotationLogger
{
    /// <summary>
    /// Multi-logger that can send logs to multiple destinations
    /// </summary>
    public class CompositeLogger : ILogger
    {
        private readonly List<ILogger> _loggers = new List<ILogger>();
        private readonly LogLevel _minimumLevel;
        private readonly bool _parallelLogging;

        public CompositeLogger(LogLevel minimumLevel = LogLevel.Info, bool parallelLogging = false)
        {
            _minimumLevel = minimumLevel;
            _parallelLogging = parallelLogging;
        }
        
        public CompositeLogger(IEnumerable<ILogger> loggers, LogLevel minimumLevel = LogLevel.Info, bool parallelLogging = false)
        {
            _loggers.AddRange(loggers);
            _minimumLevel = minimumLevel;
            _parallelLogging = parallelLogging;
        }
        
        public void AddLogger(ILogger logger)
        {
            _loggers.Add(logger);
        }
        
        public bool RemoveLogger(ILogger logger)
        {
            return _loggers.Remove(logger);
        }

        public void Log(LogEntry entry)
        {
            if (!IsEnabled(entry.Level)) return;
            
            if (_parallelLogging)
            {
                // Log in parallel for better performance with multiple loggers
                Parallel.ForEach(_loggers, logger =>
                {
                    if (logger.IsEnabled(entry.Level))
                    {
                        try
                        {
                            logger.Log(entry);
                        }
                        catch (Exception ex)
                        {
                            // Log failure but don't break the chain
                            Console.Error.WriteLine($"Error in logger {logger.GetType().Name}: {ex.Message}");
                        }
                    }
                });
            }
            else
            {
                // Traditional sequential logging
                foreach (var logger in _loggers)
                {
                    if (logger.IsEnabled(entry.Level))
                    {
                        try
                        {
                            logger.Log(entry);
                        }
                        catch (Exception ex)
                        {
                            // Log failure but don't break the chain
                            Console.Error.WriteLine($"Error in logger {logger.GetType().Name}: {ex.Message}");
                        }
                    }
                }
            }
        }

        public bool IsEnabled(LogLevel level)
        {
            return level >= _minimumLevel && _loggers.Any(l => l.IsEnabled(level));
        }
    }
}