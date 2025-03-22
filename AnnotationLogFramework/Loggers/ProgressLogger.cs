using System.Diagnostics;

namespace AnnotationLogger
{
    /// <summary>
    /// Tracks progress for long-running operations.
    /// </summary>
    public class ProgressLogger : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string _operationName;
        private readonly Timer _timer;
        private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
        private bool _disposed;
        
        /// <summary>
        /// Creates a new progress logger.
        /// </summary>
        /// <param name="operationName">Name of the operation being tracked</param>
        /// <param name="intervalMs">Interval between progress logs in milliseconds</param>
        /// <param name="logger">Optional logger (uses LogManager if not specified)</param>
        public ProgressLogger(string operationName, int intervalMs = 5000, ILogger logger = null)
        {
            _operationName = operationName;
            _logger = logger ?? LogManager.GetConfiguration().Logger;
            
            _timer = new Timer(LogProgress, null, intervalMs, intervalMs);
        }
        
        private void LogProgress(object state)
        {
            if (_disposed) return;
            
            var entry = new LogEntry
            {
                Timestamp = DateTime.UtcNow,
                Level = LogLevel.Info,
                Message = $"Operation in progress: {_operationName} (Running for {_stopwatch.Elapsed.TotalSeconds:0.00} seconds)",
                CorrelationId = CorrelationManager.CurrentCorrelationId,
                ThreadId = Thread.CurrentThread.ManagedThreadId.ToString(),
                Context = new Dictionary<string, object>
                {
                    ["ElapsedMilliseconds"] = _stopwatch.ElapsedMilliseconds,
                    ["OperationName"] = _operationName
                }
            };
            
            _logger.Log(entry);
        }
        
        public void Dispose()
        {
            if (_disposed) return;
            
            _disposed = true;
            _timer.Dispose();
            _stopwatch.Stop();
        }
    }
}