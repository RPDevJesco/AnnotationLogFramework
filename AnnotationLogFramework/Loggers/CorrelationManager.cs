namespace AnnotationLogger
{
    /// <summary>
    /// Provides correlation management for tracking logical operations across different services, components, and plugins.
    /// Ensures that all logs, spanning core processes, plugins, and background jobs, can share a common correlation ID,
    /// simplifying diagnostics and tracing.
    /// </summary>
    public static class CorrelationManager
    {
        /// <summary>
        /// Holds the correlation ID for the current asynchronous flow.
        /// Uses <see cref="AsyncLocal{T}"/> to ensure correlation ID is scoped correctly across async/await boundaries.
        /// </summary>
        private static AsyncLocal<string> _correlationId = new AsyncLocal<string>();

        /// <summary>
        /// Gets or sets the correlation ID for the current logical operation.
        /// If no correlation ID has been set, a new one is automatically generated.
        /// </summary>
        /// <remarks>
        /// Correlation IDs allow tracing related operations across multiple plugins and services, even if they occur across
        /// different threads or processes.
        /// </remarks>
        public static string CurrentCorrelationId
        {
            get => _correlationId.Value ??= Guid.NewGuid().ToString();
            set => _correlationId.Value = value;
        }

        /// <summary>
        /// Starts a new correlation scope by generating a fresh correlation ID.
        /// This should be called at the start of a new, logically distinct operation.
        /// </summary>
        /// <example>
        /// <code>
        /// CorrelationManager.StartNewCorrelation();
        /// LogManager.GetConfiguration().Logger.Log(new LogEntry
        /// {
        ///     Timestamp = DateTime.UtcNow,
        ///     Level = LogLevel.Info,
        ///     Message = "Started new migration process",
        ///     CorrelationId = CorrelationManager.CurrentCorrelationId
        /// });
        /// </code>
        /// </example>
        public static void StartNewCorrelation()
        {
            CurrentCorrelationId = Guid.NewGuid().ToString();
        }
    } 
}