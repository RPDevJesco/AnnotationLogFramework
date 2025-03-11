namespace AnnotationLogger
{
    /// <summary>
    /// Configuration specific to data-oriented logging features
    /// </summary>
    public class DataLoggerConfiguration
    {
        /// <summary>
        /// Whether to automatically track entity changes in methods with TrackDataChanges attribute
        /// </summary>
        public bool EnableDataChangeTracking { get; set; } = true;
        
        /// <summary>
        /// Maximum depth for object comparison when tracking changes
        /// </summary>
        public int MaxComparisonDepth { get; set; } = 3;
        
        /// <summary>
        /// Whether to include sensitive properties when tracking changes
        /// </summary>
        public bool IncludeSensitivePropertiesInChanges { get; set; } = false;
        
        /// <summary>
        /// Whether to log full object state before changes
        /// </summary>
        public bool LogBeforeState { get; set; } = false;
        
        /// <summary>
        /// Whether to log full object state after changes 
        /// </summary>
        public bool LogAfterState { get; set; } = false;
        
        /// <summary>
        /// Default log level for data changes
        /// </summary>
        public LogLevel DataChangeLogLevel { get; set; } = LogLevel.Info;
    }
}