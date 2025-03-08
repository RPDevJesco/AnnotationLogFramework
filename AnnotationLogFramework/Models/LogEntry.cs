using System.Collections;
using System.Text;

namespace AnnotationLogger
{
    /// <summary>
    /// Log entry model
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
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level}] [{ClassName}.{MethodName}] {Message}");
    
            // Include parameters only once
            if (Parameters != null && Parameters.Count > 0)
            {
                sb.AppendLine("  Parameters:");
                foreach (var param in Parameters)
                {
                    sb.AppendLine($"    {param.Key}: {param.Value}");
                }
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
            }
    
            return sb.ToString();
        }
    }
}