namespace AnnotationLogger
{
    // Interface for progress logging options
    public interface IProgressLoggingOptions
    {
        bool EnableProgressLogging { get; set; }
        int ProgressLoggingIntervalMs { get; set; }
    }
}