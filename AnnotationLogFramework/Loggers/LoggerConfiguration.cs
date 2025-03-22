namespace AnnotationLogger
{
    /// <summary>
    /// Configuration for the logging system
    /// </summary>
    public class LoggerConfiguration
    {
        /// <summary>
        /// The logger implementation to use
        /// </summary>
        public ILogger Logger { get; set; } = new ConsoleLogger();
        
        /// <summary>
        /// The current environment the application is running in
        /// </summary>
        public EnvironmentType Environment { get; set; } = EnvironmentType.Development;
        
        /// <summary>
        /// Whether to log method entry and exit
        /// </summary>
        public bool EnableMethodEntryExit { get; set; } = true;
        
        /// <summary>
        /// Whether to include method parameters in logs
        /// </summary>
        public bool EnableParameterLogging { get; set; } = true;
        
        /// <summary>
        /// Whether to include method return values in logs
        /// </summary>
        public bool EnableReturnValueLogging { get; set; } = true;
        
        /// <summary>
        /// Whether to include method execution time in logs
        /// </summary>
        public bool EnableExecutionTimeLogging { get; set; } = true;
        
        /// <summary>
        /// Whether to use structured JSON output
        /// </summary>
        public bool UseStructuredOutput { get; set; } = false;
        
        /// <summary>
        /// Path to the log file (only used by FileLogger)
        /// </summary>
        public string LogFilePath { get; set; } = null;
        
        /// <summary>
        /// Whether to append to existing log file or create new (only used by FileLogger)
        /// </summary>
        public bool AppendToFile { get; set; } = true;
        
        // Maximum string length for logging to prevent huge entries
        public int MaxStringLength { get; set; } = 10000;
    
        // Maximum number of items to log from collections
        public int MaxCollectionItems { get; set; } = 100;
    
        // Whether to include stack traces in error logs
        public bool IncludeStackTraces { get; set; } = true;
    
        // Maximum depth for object inspection in logs
        public int MaxObjectDepth { get; set; } = 3;
    
        // Whether to enable performance tracking
        public bool EnablePerformanceTracking { get; set; } = true;
    
        // Controls log verbosity level for different environments
        public LogVerbosity DefaultVerbosity { get; set; } = LogVerbosity.Normal;
    }
}