using System;
using System.Collections.Concurrent;
using System.Linq;

namespace AnnotationLogger.Performance
{
    /// <summary>
    /// Tracks and analyzes method execution performance.
    /// </summary>
    public class PerformanceTracker
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _executionTimes = 
            new ConcurrentDictionary<string, ConcurrentBag<long>>();
        
        /// <summary>
        /// Records a method execution time.
        /// </summary>
        public void TrackExecutionTime(string methodName, long milliseconds)
        {
            _executionTimes.GetOrAdd(methodName, _ => new ConcurrentBag<long>())
                .Add(milliseconds);
        }
        
        /// <summary>
        /// Gets performance stats for the tracked methods.
        /// </summary>
        public Dictionary<string, MethodPerformanceStats> GetStats()
        {
            var result = new Dictionary<string, MethodPerformanceStats>();
            
            foreach (var entry in _executionTimes)
            {
                var times = entry.Value.ToArray();
                if (times.Length == 0) continue;
                
                result[entry.Key] = new MethodPerformanceStats
                {
                    MethodName = entry.Key,
                    CallCount = times.Length,
                    AverageTime = times.Average(),
                    MinTime = times.Min(),
                    MaxTime = times.Max(),
                    TotalTime = times.Sum(),
                    MedianTime = CalculateMedian(times)
                };
            }
            
            return result;
        }
        
        /// <summary>
        /// Calculates the median value from an array of execution times.
        /// </summary>
        /// <param name="values">Array of execution time values</param>
        /// <returns>The median value</returns>
        private double CalculateMedian(long[] values)
        {
            if (values == null || values.Length == 0)
                return 0;
                
            // Sort the array
            var sortedValues = values.OrderBy(v => v).ToArray();
            
            int count = sortedValues.Length;
            int midpoint = count / 2;
            
            if (count % 2 == 0)
            {
                // Even number of items - average the middle two
                return (sortedValues[midpoint - 1] + sortedValues[midpoint]) / 2.0;
            }
            else
            {
                // Odd number of items - return the middle one
                return sortedValues[midpoint];
            }
        }
        
        /// <summary>
        /// Clears all tracked performance data.
        /// </summary>
        public void Reset()
        {
            _executionTimes.Clear();
        }

        /// <summary>
        /// Gets performance stats for a specific method.
        /// </summary>
        /// <param name="methodName">The name of the method</param>
        /// <returns>Performance statistics or null if the method hasn't been tracked</returns>
        public MethodPerformanceStats? GetStatsForMethod(string methodName)
        {
            if (!_executionTimes.TryGetValue(methodName, out var timeBag))
                return null;
                
            var times = timeBag.ToArray();
            if (times.Length == 0)
                return null;
                
            return new MethodPerformanceStats
            {
                MethodName = methodName,
                CallCount = times.Length,
                AverageTime = times.Average(),
                MinTime = times.Min(),
                MaxTime = times.Max(),
                TotalTime = times.Sum(),
                MedianTime = CalculateMedian(times)
            };
        }
    }
}