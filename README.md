# AnnotationLogger API Documentation

A high-performance, attributes-based .NET logging framework that brings clean code and aspect-oriented programming principles to your application logging.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Overview

AnnotationLogger is a comprehensive logging framework for .NET applications that provides attribute-based method logging, performance tracking, data change monitoring, and more. The framework is designed to be both simple to use and highly configurable, supporting a variety of logging destinations and formats.

## Key Features

- **Attributes-Based Logging**: Simply annotate methods with `[LogDebug]`, `[LogInfo]`, `[LogWarning]`, etc.
- **Expression-Based Method Interception**: Uses expression trees for powerful method introspection
- **Automatic Parameter Logging**: Captures method parameters automatically
- **Return Value Logging**: Records method outputs for complete invocation tracking
- **Exception Tracking**: Automatically captures and logs exceptions with stack traces
- **Execution Time Measurement**: Built-in performance tracking via Stopwatch integration
- **Async Support**: Full support for async/await methods via Task-based interfaces
- **Extensible Output**: Console, file and composite loggers with structured output options
- **Environment-Aware**: Configure different logging behaviors for development vs. production
- **Distributed Tracing**: Built-in correlation ID management for trace context propagation
- **Thread-Safe**: Implements proper synchronization for multi-threaded environments

## Why AnnotationLogger?

### Comparison with Popular Logging Libraries

| Feature | AnnotationLogger | Serilog | NLog | log4net |
|---------|-----------------|---------|------|---------|
| Aspect-Oriented Logging | ✅ | ❌ | ❌ | ❌ |
| Clean Business Logic | ✅ | ❌ | ❌ | ❌ |
| Automatic Parameter Capture | ✅ | ❌ | ❌ | ❌ |
| Fluent Configuration | ✅ | ✅ | ✅ | ❌ |
| Structured Logging | ✅ | ✅ | ✅ | ❌ |
| Performance | High | High | Medium | Medium |
| Learning Curve | Medium | Low | Medium | High |

### Benefits Over Traditional Loggers

**1. Separation of Concerns**

Unlike traditional logging frameworks that require you to inject logging code throughout your business logic, AnnotationLogger cleanly separates your application code from your logging concerns. This leads to more maintainable, readable code.

**2. Reduced Boilerplate**

Traditional logging approaches may require 5-10 lines of code per method for proper entry/exit logging. With AnnotationLogger, it's just one line - the attribute declaration:

```csharp
[LogInfo]
public Customer GetCustomerById(int customerId)
{
    // Your business logic here, no logging code!
    return _repository.FindById(customerId);
}
```

**3. Consistent Logging Standard**

AnnotationLogger ensures all methods are logged consistently following the same pattern and level of detail. This prevents developer-by-developer inconsistencies that plague many codebases.

**4. Better for AOP**

True aspect-oriented programming for logging, separating cross-cutting concerns from your business logic in a clean, maintainable way.

**5. Automatic Parameter Handling**

Parameter values are automatically captured and formatted intelligently based on their type, with special handling for collections, dictionaries, and complex objects.

## Core Components

### LogManager

The central static class that manages all logging configurations and operations.

```csharp
public static class LogManager
{
    // Configure general logging settings
    public static void Configure(Action<LoggerConfiguration> configAction);
    
    // Configure data-specific logging features
    public static void ConfigureDataLogging(Action<DataLoggerConfiguration> configAction);
    
    // Get current configurations
    public static LoggerConfiguration GetConfiguration();
    public static DataLoggerConfiguration GetDataConfiguration();
    
    // Get performance tracking instance
    public static PerformanceTracker GetPerformanceTracker();
    
    // Context management
    public static Dictionary<string, object> CurrentContext { get; }
    public static void AddContext(string key, object value);
    public static void RemoveContext(string key);
    public static void ClearContext();
    
    // Direct logging methods
    public static void Log(LogLevel level, string message, Dictionary<string, object> context = null);
    public static void Trace(string message, Dictionary<string, object> context = null);
    public static void Debug(string message, Dictionary<string, object> context = null);
    public static void Info(string message, Dictionary<string, object> context = null);
    public static void Warning(string message, Dictionary<string, object> context = null);
    public static void Error(string message, Dictionary<string, object> context = null);
    public static void Critical(string message, Dictionary<string, object> context = null);
    public static void Exception(Exception exception, string message = null, Dictionary<string, object> context = null);
    
    // Data change tracking
    public static void LogDataChanges<T>(T before, T after, string operationType, string entityId = null, 
        LogLevel level = LogLevel.Info, Dictionary<string, object> additionalContext = null);
}
```

### LoggedMethodCaller

The main API for automatic method interception and logging.

```csharp
public static class LoggedMethodCaller
{
    // Log synchronous methods with return values
    public static T Call<T>(Expression<Func<T>> methodCall);
    
    // Log synchronous methods without return values
    public static void Call(Expression<Action> methodCall);
    
    // Log asynchronous methods with return values
    public static Task<T> CallAsync<T>(Expression<Func<Task<T>>> methodCall);
    
    // Log asynchronous methods without return values
    public static Task CallAsync(Expression<Func<Task>> methodCall);
}
```

### CorrelationManager

Manages correlation IDs for tracking related log entries across multiple components.

```csharp
public static class CorrelationManager
{
    // Gets or sets the current correlation ID
    public static string CurrentCorrelationId { get; set; }
    
    // Generate a new correlation ID
    public static void StartNewCorrelation();
}
```

## Loggers

### ILogger

The core interface that all loggers implement.

```csharp
public interface ILogger
{
    void Log(LogEntry entry);
    bool IsEnabled(LogLevel level);
}
```

### ConsoleLogger

Outputs logs to the console with optional color formatting.

```csharp
public class ConsoleLogger : ILogger
{
    public ConsoleLogger(
        LogLevel minimumLevel = LogLevel.Info, 
        bool useStructuredOutput = false,
        bool useColors = true,
        bool includeTimestamps = true);
}
```

### FileLogger

Writes logs to files with optional rotation.

```csharp
public class FileLogger : ILogger
{
    public FileLogger(
        LogLevel minimumLevel = LogLevel.Info, 
        string logFilePath = null, 
        bool useStructuredOutput = false,
        bool appendToFile = true);
}
```

### DatabaseLogger

Stores logs in a SQLite database.

```csharp
public class DatabaseLogger : ILogger
{
    public DatabaseLogger(
        LogLevel minimumLevel = LogLevel.Info, 
        string connectionString = "Data Source=logs.db");
}
```

### CompositeLogger

Combines multiple loggers for multi-destination logging.

```csharp
public class CompositeLogger : ILogger
{
    public CompositeLogger(LogLevel minimumLevel = LogLevel.Info, bool parallelLogging = false);
    public CompositeLogger(IEnumerable<ILogger> loggers, LogLevel minimumLevel = LogLevel.Info, bool parallelLogging = false);
    
    public void AddLogger(ILogger logger);
    public bool RemoveLogger(ILogger logger);
}
```

### LogRouter

Routes log entries to different loggers based on conditions.

```csharp
public class LogRouter : ILogger
{
    public LogRouter(ILogger defaultLogger);
    
    public LogRouter AddRoute(Predicate<LogEntry> condition, ILogger logger);
}
```

## Performance Tracking

### PerformanceTracker

Tracks and analyzes method execution times.

```csharp
public class PerformanceTracker
{
    public void TrackExecutionTime(string methodName, long milliseconds);
    public Dictionary<string, MethodPerformanceStats> GetStats();
    public MethodPerformanceStats GetStatsForMethod(string methodName);
    public void Reset();
}

public class MethodPerformanceStats
{
    public string MethodName { get; set; }
    public int CallCount { get; set; }
    public double AverageTime { get; set; }
    public long MinTime { get; set; }
    public long MaxTime { get; set; }
    public long TotalTime { get; set; }
    public double MedianTime { get; set; }
}
```

### ProgressLogger

Tracks progress for long-running operations.

```csharp
public class ProgressLogger : IDisposable
{
    public ProgressLogger(
        string operationName, 
        int intervalMs = 5000, 
        ILogger logger = null);
        
    public void Dispose();
}
```

## Log Throttling

### LogThrottler

Controls log volume for high-frequency operations.

```csharp
public class LogThrottler
{
    public bool ShouldLog(string key, int maxPerSecond);
    public void Reset();
}
```

## Logging Attributes

### Method Logging Attributes

```csharp
// Base attribute class
public abstract class LogAttributeBase : Attribute, IProgressLoggingOptions
{
    public LogLevel Level { get; }
    public bool IncludeParameters { get; set; } = true;
    public bool IncludeReturnValue { get; set; } = true;
    public bool IncludeExecutionTime { get; set; } = true;
    
    // Progress logging options
    public bool EnableProgressLogging { get; set; }
    public int ProgressLoggingIntervalMs { get; set; } = 5000;
}

// Concrete logging attributes
public class LogAttribute : LogAttributeBase
{
    public LogAttribute(LogLevel level);
}

public class LogTraceAttribute : LogAttributeBase { }
public class LogDebugAttribute : LogAttributeBase { }
public class LogInfoAttribute : LogAttributeBase { }
public class LogWarningAttribute : LogAttributeBase { }
public class LogErrorAttribute : LogAttributeBase { }
public class LogCriticalAttribute : LogAttributeBase { }
public class LogProdAttribute : LogAttributeBase { }

// Extension method for enabling progress logging
public static T WithProgressLogging<T>(this T attribute, int intervalMs = 5000) 
    where T : LogAttributeBase;
```

### Data Change Tracking Attributes

```csharp
// For tracking changes between object states
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class TrackDataChangesAttribute : Attribute
{
    public bool DetailedComparison { get; set; } = true;
    public int MaxComparisonDepth { get; set; } = 3;
    public bool IncludeOriginalState { get; set; } = false;
    public bool IncludeUpdatedState { get; set; } = false;
    public string OperationType { get; set; }
}

// For marking parameters that represent the before state
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class BeforeChangeAttribute : Attribute { }

// For marking parameters that represent the after state
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public class AfterChangeAttribute : Attribute { }

// For excluding properties from comparison
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public class ExcludeFromComparisonAttribute : Attribute
{
    public string Reason { get; }
    public ExcludeFromComparisonAttribute(string reason = null);
}
```

### Throttling Attribute

```csharp
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class ThrottleLoggingAttribute : Attribute
{
    public int MaxLogsPerSecond { get; set; } = 10;
    public ThrottleLoggingAttribute(int maxLogsPerSecond = 10);
}
```

### Sensitive Data Attributes

```csharp
// Base attribute
public abstract class SensitiveDataAttributeBase : Attribute
{
    public string Reason { get; }
    protected SensitiveDataAttributeBase(string reason = null);
}

// Exclude sensitive data from logs
public class ExcludeFromLogsAttribute : SensitiveDataAttributeBase
{
    public ExcludeFromLogsAttribute(string reason = null);
}

// Mask sensitive data in logs
public class MaskInLogsAttribute : SensitiveDataAttributeBase
{
    public string MaskingPattern { get; set; } = "***";
    public bool ShowFirstChars { get; set; } = false;
    public int FirstCharsCount { get; set; } = 2;
    public bool ShowLastChars { get; set; } = false;
    public int LastCharsCount { get; set; } = 2;
    
    public MaskInLogsAttribute(string reason = null);
}

// Redact contents but show structure
public class RedactContentsAttribute : SensitiveDataAttributeBase
{
    public string ReplacementText { get; set; } = "[REDACTED]";
    public RedactContentsAttribute(string reason = null);
}
```

## Configuration

### LoggerConfiguration

Configuration options for the general logging system.

```csharp
public class LoggerConfiguration
{
    // Core logging settings
    public ILogger Logger { get; set; } = new ConsoleLogger();
    public EnvironmentType Environment { get; set; } = EnvironmentType.Development;
    
    // Logging behavior
    public bool EnableMethodEntryExit { get; set; } = true;
    public bool EnableParameterLogging { get; set; } = true;
    public bool EnableReturnValueLogging { get; set; } = true;
    public bool EnableExecutionTimeLogging { get; set; } = true;
    
    // Output format
    public bool UseStructuredOutput { get; set; } = false;
    
    // File logging options
    public string LogFilePath { get; set; } = null;
    public bool AppendToFile { get; set; } = true;
    
    // Advanced options
    public int MaxStringLength { get; set; } = 10000;
    public int MaxCollectionItems { get; set; } = 100;
    public bool IncludeStackTraces { get; set; } = true;
    public int MaxObjectDepth { get; set; } = 3;
    public bool EnablePerformanceTracking { get; set; } = true;
    public LogVerbosity DefaultVerbosity { get; set; } = LogVerbosity.Normal;
}

public enum EnvironmentType
{
    Development,
    Testing,
    Staging,
    Production
}

public enum LogVerbosity
{
    Minimal,   // Only essential information
    Normal,    // Standard logging with parameters and return values
    Verbose    // Detailed logging with all available context
}
```

### DataLoggerConfiguration

Configuration options for data-oriented logging features.

```csharp
public class DataLoggerConfiguration
{
    public bool EnableDataChangeTracking { get; set; } = true;
    public int MaxComparisonDepth { get; set; } = 3;
    public bool IncludeSensitivePropertiesInChanges { get; set; } = false;
    public bool LogBeforeState { get; set; } = false;
    public bool LogAfterState { get; set; } = false;
    public LogLevel DataChangeLogLevel { get; set; } = LogLevel.Info;
}
```

## Models

### LogEntry

The core model that represents a log entry.

```csharp
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
    
    // Data change tracking
    public List<ChangeRecord> DataChanges { get; set; }
    public bool HasDataChanges => DataChanges != null && DataChanges.Count > 0;
    public Dictionary<string, object> Context { get; set; }
    public string EntityType { get; set; }
    public string EntityId { get; set; }
    public string OperationType { get; set; }
    
    public override string ToString();
}
```

### ChangeRecord

Represents a single property change when tracking data changes.

```csharp
public class ChangeRecord
{
    public string PropertyPath { get; set; }
    public object OldValue { get; set; }
    public object NewValue { get; set; }
    public Type PropertyType { get; set; }
    
    public override string ToString();
}
```

## Utilities

### ObjectComparer

Compares two objects and identifies their differences.

```csharp
public static class ObjectComparer
{
    public static List<ChangeRecord> CompareObjects(
        object before, 
        object after, 
        int maxDepth = 3, 
        string path = "");
}
```

### LogFormatter

Formats log values with sensitivity handling.

```csharp
public static class LogFormatter
{
    public static object FormatForLog(
        object value, 
        string parameterName, 
        ParameterInfo parameterInfo, 
        int maxDepth = 3);
        
    public static string FormatChanges(List<ChangeRecord> changes);
}
```

## Usage Examples

### Basic Usage

```csharp
// Configure logging
LogManager.Configure(config => 
{
    config.Logger = new CompositeLogger(new[] {
        new ConsoleLogger(),
        new FileLogger(logFilePath: "application.log")
    });
    config.Environment = EnvironmentType.Development;
});

// Log with attributes
public class Calculator
{
    [LogInfo]
    public int Add(int a, int b)
    {
        return a + b;
    }
}

// Call with logging
var calculator = new Calculator();
var result = LoggedMethodCaller.Call(() => calculator.Add(5, 3));
```

### Data Change Tracking

```csharp
public class UserService
{
    [TrackDataChanges(IncludeOriginalState = true)]
    public User UpdateUser([BeforeChange] User existingUser, string newName, string newEmail)
    {
        existingUser.Name = newName;
        existingUser.Email = newEmail;
        return existingUser;
    }
}

// Call with data change tracking
var user = new User { Id = 1, Name = "John", Email = "john@example.com" };
LoggedMethodCaller.Call(() => userService.UpdateUser(user, "John Doe", "john.doe@example.com"));
```

### Progress Logging for Long Operations

```csharp
[LogInfo]
[WithProgressLogging(intervalMs: 1000)] // Log progress every second
public async Task ProcessLargeDatasetAsync()
{
    // Long-running operation...
    for (int i = 0; i < 100; i++)
    {
        await Task.Delay(500);
        // Process data...
    }
}

// Call with progress logging
await LoggedMethodCaller.CallAsync(() => service.ProcessLargeDatasetAsync());
```

### Throttled Logging

```csharp
[LogDebug]
[ThrottleLogging(MaxLogsPerSecond = 5)]
public double GetValue(int index)
{
    // High-frequency operation that will be limited to 5 logs per second
    return Math.Sqrt(index);
}
```

### Sensitive Data Handling

```csharp
public class User
{
    public string Username { get; set; }
    
    [MaskInLogs(ShowFirstChars = true, FirstCharsCount = 1)]
    public string Password { get; set; }
    
    [MaskInLogs(ShowFirstChars = true, FirstCharsCount = 3, ShowLastChars = true, LastCharsCount = 4)]
    public string Email { get; set; }
    
    [RedactContents(ReplacementText = "[CREDIT CARD REDACTED]")]
    public string CreditCardNumber { get; set; }
    
    [ExcludeFromLogs]
    public string ApiKey { get; set; }
}
```

### Advanced Router Configuration

```csharp
// Create a router that sends different logs to different destinations
var router = new LogRouter(new ConsoleLogger())
    .AddRoute(entry => entry.Level >= LogLevel.Error, new FileLogger("errors.log"))
    .AddRoute(entry => entry.Level == LogLevel.Debug, new FileLogger("debug.log"))
    .AddRoute(entry => entry.Message.Contains("sensitive"), new FileLogger("security.log"))
    .AddRoute(entry => entry.ClassName.Contains("Payment"), new DatabaseLogger());

LogManager.Configure(config => {
    config.Logger = router;
});
```

### Performance Analysis

```csharp
// After running your application
var performanceStats = LogManager.GetPerformanceTracker().GetStats();

foreach (var stat in performanceStats)
{
    Console.WriteLine($"Method: {stat.Value.MethodName}");
    Console.WriteLine($"  Calls: {stat.Value.CallCount}");
    Console.WriteLine($"  Avg Time: {stat.Value.AverageTime:F2}ms");
    Console.WriteLine($"  Min Time: {stat.Value.MinTime}ms");
    Console.WriteLine($"  Max Time: {stat.Value.MaxTime}ms");
    Console.WriteLine($"  Median: {stat.Value.MedianTime:F2}ms");
}
```

## Implementation Details

AnnotationLogger uses several advanced C# features:

- **Expression Trees**: For method interception and parameter extraction
- **Reflection**: For attribute discovery and parameter information
- **AsyncLocal<T>**: For correlation ID propagation across async contexts
- **Stopwatch**: For precise execution time measurement
- **ReaderWriterLockSlim**: For thread-safe file logging

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.