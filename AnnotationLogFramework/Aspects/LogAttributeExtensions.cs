namespace AnnotationLogger
{
    // Extension method for LogAttributeBase
    public static class LogAttributeExtensions
    {
        /// <summary>
        /// Enables progress logging for methods with this attribute.
        /// </summary>
        /// <param name="attribute">The log attribute</param>
        /// <param name="intervalMs">Interval between progress logs</param>
        public static T WithProgressLogging<T>(this T attribute, int intervalMs = 5000) 
            where T : LogAttributeBase
        {
            if (attribute is IProgressLoggingOptions options)
            {
                options.EnableProgressLogging = true;
                options.ProgressLoggingIntervalMs = intervalMs;
            }
            
            return attribute;
        }
    }
}