using System;
using System.Collections.Generic;
using System.Text;

namespace AnnotationLogger
{
    /// <summary>
    /// Enhanced log entry model with data change tracking
    /// </summary>
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public string MethodName { get; set; }
        public string ClassName { get; set; }
        public Dictionary<string, object> Parameters { get; set; }
        public object ReturnValue { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public LogLevel Level { get; set; }
        public Exception Exception { get; set; }
        public string CorrelationId { get; set; }
        public string ThreadId { get; set; }
        public string Message { get; set; }
        
        /// <summary>
        /// Record of data changes if this log entry represents a data change operation
        /// </summary>
        public List<ChangeRecord> DataChanges { get; set; }
        
        /// <summary>
        /// Indicates that this log entry represents a data change operation
        /// </summary>
        public bool HasDataChanges => DataChanges != null && DataChanges.Count > 0;
        
        /// <summary>
        /// Optional context information that can be attached to a log entry
        /// </summary>
        public Dictionary<string, object> Context { get; set; }
        
        /// <summary>
        /// Entity type that was changed (if DataChanges is populated)
        /// </summary>
        public string EntityType { get; set; }
        
        /// <summary>
        /// Entity identifier that was changed (if available)
        /// </summary>
        public string EntityId { get; set; }
        
        /// <summary>
        /// Name of the operation performed on the data
        /// </summary>
        public string OperationType { get; set; } // e.g., "Create", "Update", "Delete"
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{ClassName}.{MethodName}] {Message}");
    
            if (!string.IsNullOrEmpty(CorrelationId))
            {
                sb.AppendLine($"  CorrelationId: {CorrelationId}");
            }
            
            if (!string.IsNullOrEmpty(ThreadId))
            {
                sb.AppendLine($"  ThreadId: {ThreadId}");
            }
            
            // Include parameters only once
            if (Parameters != null && Parameters.Count > 0)
            {
                sb.AppendLine("  Parameters:");
                foreach (var param in Parameters)
                {
                    sb.AppendLine($"    {param.Key}: {param.Value}");
                }
            }
    
            // Include data changes if present
            if (HasDataChanges)
            {
                sb.AppendLine($"  Data Changes ({DataChanges.Count}):");
                
                if (!string.IsNullOrEmpty(EntityType))
                    sb.AppendLine($"    Entity Type: {EntityType}");
                    
                if (!string.IsNullOrEmpty(EntityId))
                    sb.AppendLine($"    Entity ID: {EntityId}");
                    
                if (!string.IsNullOrEmpty(OperationType))
                    sb.AppendLine($"    Operation: {OperationType}");
                
                foreach (var change in DataChanges.Take(10)) // Limit to 10 changes for readability
                {
                    sb.AppendLine($"    {change.PropertyPath}: '{change.OldValue}' -> '{change.NewValue}'");
                }
                
                if (DataChanges.Count > 10)
                    sb.AppendLine($"    ... and {DataChanges.Count - 10} more changes");
            }
    
            if (ExecutionTime != TimeSpan.Zero)
            {
                sb.AppendLine($"  Execution Time: {ExecutionTime.TotalMilliseconds}ms");
            }
    
            if (ReturnValue != null)
            {
                sb.AppendLine($"  Return Value: {ReturnValue}");
            }
    
            if (Exception != null)
            {
                sb.AppendLine($"  Exception: {Exception.Message}");
                sb.AppendLine($"  Stack Trace: {Exception.StackTrace}");
            }
            
            // Include additional context if present
            if (Context != null && Context.Count > 0)
            {
                sb.AppendLine("  Context:");
                foreach (var item in Context)
                {
                    sb.AppendLine($"    {item.Key}: {item.Value}");
                }
            }
    
            return sb.ToString();
        }
    }
}