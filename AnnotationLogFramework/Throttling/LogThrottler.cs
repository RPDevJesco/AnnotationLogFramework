using System.Collections.Concurrent;

namespace AnnotationLogger.Throttling
{
    /// <summary>
    /// Controls log volume for high-frequency operations.
    /// </summary>
    public class LogThrottler
    {
        private readonly ConcurrentDictionary<string, ThrottleInfo> _throttleState = 
            new ConcurrentDictionary<string, ThrottleInfo>();
        
        /// <summary>
        /// Determines if a log should be emitted based on throttling rules.
        /// </summary>
        /// <param name="key">The throttling key (e.g. method name)</param>
        /// <param name="maxPerSecond">Maximum logs per second</param>
        /// <returns>True if the log should be emitted, false if throttled</returns>
        public bool ShouldLog(string key, int maxPerSecond)
        {
            var now = DateTime.UtcNow;
            var second = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);
            
            var info = _throttleState.AddOrUpdate(
                key,
                _ => new ThrottleInfo { Second = second, Count = 1 },
                (_, existing) => {
                    if (existing.Second != second)
                    {
                        return new ThrottleInfo { Second = second, Count = 1 };
                    }
                    
                    existing.Count++;
                    return existing;
                });
            
            return info.Count <= maxPerSecond;
        }
        
        /// <summary>
        /// Clears the throttling state.
        /// </summary>
        public void Reset()
        {
            _throttleState.Clear();
        }
        
        private class ThrottleInfo
        {
            public DateTime Second { get; set; }
            public int Count { get; set; }
        }
    }
}