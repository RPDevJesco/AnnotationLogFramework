namespace AnnotationLogger.Performance
{
    /// <summary>
    /// Contains performance statistics for a method.
    /// </summary>
    public class MethodPerformanceStats
    {
        /// <summary>
        /// Name of the method
        /// </summary>
        public string MethodName { get; set; }
        
        /// <summary>
        /// Number of times the method was called
        /// </summary>
        public int CallCount { get; set; }
        
        /// <summary>
        /// Average execution time in milliseconds
        /// </summary>
        public double AverageTime { get; set; }
        
        /// <summary>
        /// Minimum execution time in milliseconds
        /// </summary>
        public long MinTime { get; set; }
        
        /// <summary>
        /// Maximum execution time in milliseconds
        /// </summary>
        public long MaxTime { get; set; }
        
        /// <summary>
        /// Total execution time in milliseconds
        /// </summary>
        public long TotalTime { get; set; }
        
        /// <summary>
        /// Median execution time in milliseconds
        /// </summary>
        public double MedianTime { get; set; }
    }
}