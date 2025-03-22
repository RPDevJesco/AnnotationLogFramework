# AnnotationLogger

A high-performance, attributes-based .NET logging framework that brings clean code and aspect-oriented programming principles to your application logging.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

## Table of Contents

- [Overview](#overview)
- [Key Features](#key-features)
- [Why AnnotationLogger?](#why-annotationlogger)
  - [Comparison with Popular Logging Libraries](#comparison-with-popular-logging-libraries)
  - [Benefits Over Traditional Loggers](#benefits-over-traditional-loggers)
- [Getting Started](#getting-started)
  - [Installation](#installation)
  - [Basic Setup](#basic-setup)
  - [First Log](#first-log)
- [Core Components](#core-components)
  - [LogManager](#logmanager)
  - [LoggedMethodCaller](#loggedmethodcaller)
  - [CorrelationManager](#correlationmanager)
- [Logging Attributes](#logging-attributes)
  - [Basic Usage](#basic-attributes-usage)
  - [Customizing Attributes](#customizing-attributes)
- [Advanced Features](#advanced-features)
  - [Data Change Tracking](#data-change-tracking)
  - [Performance Monitoring](#performance-monitoring)
  - [Progress Logging](#progress-logging)
  - [Log Throttling](#log-throttling)
  - [Sensitive Data Protection](#sensitive-data-protection)
  - [Log Routing](#log-routing)
  - [Database Logging](#database-logging)
- [Configuration Options](#configuration-options)
- [Implementation Details](#implementation-details)
- [Contributing](#contributing)
- [License](#license)

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
- **Extensible Output**: Console, file, database, and composite loggers with structured output options
- **Environment-Aware**: Configure different logging behaviors for development vs. production
- **Distributed Tracing**: Built-in correlation ID management for trace context propagation
- **Thread-Safe**: Implements proper synchronization for multi-threaded environments
- **Data Change Tracking**: Track changes to objects across method calls
- **Progress Monitoring**: Log progress for long-running operations
- **Log Throttling**: Control log volume in high-frequency methods
- **Advanced Routing**: Direct logs to different destinations based on content

## Why AnnotationLogger?

### Comparison with Popular Logging Libraries

| Feature | AnnotationLogger | Serilog | NLog | log4net |
|---------|-----------------|---------|------|---------|
| Aspect-Oriented Logging | ✅ | ❌ | ❌ | ❌ |
| Clean Business Logic | ✅ | ❌ | ❌ | ❌ |
| Automatic Parameter Capture | ✅ | ❌ | ❌ | ❌ |
| Fluent Configuration | ✅ | ✅ | ✅ | ❌ |
| Structured Logging | ✅ | ✅ | ✅ | ❌ |
| Performance Tracking | ✅ | ❌ | ❌ | ❌ |
| Data Change Tracking | ✅ | ❌ | ❌ | ❌ |
| Progress Logging | ✅ | ❌ | ❌ | ❌ |
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

## Getting Started

### Installation

Add AnnotationLogger to your project using your preferred method:

```bash
dotnet add package AnnotationLogger
```

### Basic Setup

Set up AnnotationLogger in your application startup code:

```csharp
using AnnotationLogger;

// Configure logger in Program.cs or Startup.cs
LogManager.Configure(config => 
{
    config.Logger = new ConsoleLogger();
    config.Environment = EnvironmentType.Development;
});

// Optional: Configure data change tracking
LogManager.ConfigureDataLogging(config => 
{
    config.EnableDataChangeTracking = true;
    config.MaxComparisonDepth = 3;
});
```

### First Log

Create a class with logged methods and then call them using `LoggedMethodCaller`:

```csharp
// Define a class with logging attributes
public class UserService
{
    [LogInfo]
    public User GetUser(int userId)
    {
        // Business logic here
        return new User { Id = userId, Name = "John" };
    }
}

// Use LoggedMethodCaller to invoke the method with logging
var userService = new UserService();
User user = LoggedMethodCaller.Call(() => userService.GetUser(123));
```

## Core Components

### LogManager

The central static class that manages all logging configurations and operations.

```csharp
// Configure general logging
LogManager.Configure(config => 
{
    config.Logger = new ConsoleLogger();
    config.Environment = EnvironmentType.Development;
});

// Direct logging methods
LogManager.Info("Application started");
LogManager.Warning("Unusual condition detected");
LogManager.Error("Operation failed", new Dictionary<string, object> { 
    { "ErrorCode", 500 }, 
    { "Details", "Connection timeout" } 
});

// Add context information
LogManager.AddContext("TraceId", Guid.NewGuid().ToString());
LogManager.AddContext("SessionId", sessionId);
```

### LoggedMethodCaller

The main API for automatic method interception and logging.

```csharp
// For methods that return a value
int result = LoggedMethodCaller.Call(() => calculator.Add(5, 3));

// For methods that don't return a value
LoggedMethodCaller.Call(() => dataService.ProcessRecord(record));

// For async methods
User user = await LoggedMethodCaller.CallAsync(() => userService.GetUserAsync(123));
await LoggedMethodCaller.CallAsync(() => notificationService.SendNotificationsAsync());
```

### CorrelationManager

Manages correlation IDs for tracking related log entries across multiple components.

```csharp
// Start a new logical operation
CorrelationManager.StartNewCorrelation();

// Get the current correlation ID
string correlationId = CorrelationManager.CurrentCorrelationId;

// Pass to other systems
httpClient.DefaultRequestHeaders.Add("X-Correlation-ID", correlationId);
```

## Logging Attributes

### Basic Attributes Usage

```csharp
// Different logging levels
[LogTrace]
public void HighDetailDiagnostics() { }

[LogDebug]
public List<string> GetDebugInfo() { }

[LogInfo]
public User CreateUser(UserRequest request) { }

[LogWarning]
public bool ValidateInput(string input) { }

[LogError]
public void ProcessPayment(Payment payment) { }

[LogCritical]
public void UpdateSystemConfig(SystemConfig config) { }

// Logs in all environments, including production
[LogProd]
public void BusinessCriticalOperation() { }
```

### Customizing Attributes

```csharp
// Control what gets logged
[LogInfo(
    IncludeParameters = true,       // Log method parameters
    IncludeReturnValue = true,      // Log return value
    IncludeExecutionTime = true     // Log execution time
)]
public Report GenerateReport(ReportRequest request) { }

// With progress logging for long operations
[LogInfo]
[WithProgressLogging(intervalMs: 1000)]  // Log progress every second
public async Task ImportLargeDatasetAsync() { }
```

## Advanced Features

### Data Change Tracking

Track changes to objects across method calls:

```csharp
[TrackDataChanges(
    IncludeOriginalState = true,     // Include original state in logs
    IncludeUpdatedState = true,      // Include updated state in logs
    OperationType = "UserUpdate"     // Custom operation name
)]
public User UpdateUser([BeforeChange] User user, UserUpdateRequest request)
{
    // Update user properties
    user.Name = request.Name;
    user.Email = request.Email;
    
    return user;
}
```

### Performance Monitoring

```csharp
// Performance tracking happens automatically for logged methods
[LogInfo]
public void PerformanceIntensiveOperation() { }

// Get performance statistics after running your application
var stats = LogManager.GetPerformanceTracker().GetStats();
foreach (var stat in stats)
{
    Console.WriteLine($"Method: {stat.Value.MethodName}");
    Console.WriteLine($"  Calls: {stat.Value.CallCount}");
    Console.WriteLine($"  Avg Time: {stat.Value.AverageTime:F2}ms");
}
```

### Progress Logging

```csharp
// Automatic progress logging for long operations
[LogInfo]
[WithProgressLogging(intervalMs: 1000)]
public async Task ProcessBatchesAsync(IEnumerable<DataBatch> batches)
{
    foreach (var batch in batches)
    {
        await Task.Delay(500); // Simulate work
        // Process batch...
    }
}

// Manual progress logging
using (var progressLogger = new ProgressLogger("Import Operation", 2000))
{
    // Long-running operation...
}
```

### Log Throttling

```csharp
// Prevent log flooding in high-frequency operations
[LogDebug]
[ThrottleLogging(MaxLogsPerSecond = 5)]
public double CalculateValue(double input)
{
    // High-frequency operation
    return Math.Pow(input, 2);
}
```

### Sensitive Data Protection

```csharp
public class User
{
    public string Username { get; set; }
    
    [MaskInLogs(ShowFirstChars = true, FirstCharsCount = 1)]
    public string Password { get; set; }  // "p***"
    
    [MaskInLogs(ShowFirstChars = true, FirstCharsCount = 3, 
                ShowLastChars = true, LastCharsCount = 4)]
    public string Email { get; set; }  // "joh***ple.com"
    
    [RedactContents(ReplacementText = "[CREDIT CARD REDACTED]")]
    public string CreditCardNumber { get; set; }
    
    [ExcludeFromLogs]
    public string ApiKey { get; set; }  // Excluded completely
}
```

### Log Routing

```csharp
// Create a router with custom routing rules
var router = new LogRouter(new ConsoleLogger())
    // Send all errors to an error log file
    .AddRoute(entry => entry.Level >= LogLevel.Error, 
              new FileLogger(LogLevel.Error, "errors.log"))
    
    // Send debug logs to a debug file
    .AddRoute(entry => entry.Level == LogLevel.Debug,
              new FileLogger(LogLevel.Debug, "debug.log"))
    
    // Send security-related logs to a secure log
    .AddRoute(entry => entry.Message.Contains("security"),
              new FileLogger(LogLevel.Info, "security.log"))
    
    // Send data changes to a database
    .AddRoute(entry => entry.HasDataChanges,
              new DatabaseLogger(LogLevel.Info, "Data Source=audit.db"));

LogManager.Configure(config => 
{
    config.Logger = router;
});
```

### Database Logging

```csharp
// Configure logging to a SQLite database
string dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs/logs.db");
var dbLogger = new DatabaseLogger(LogLevel.Info, $"Data Source={dbPath}");

LogManager.Configure(config => 
{
    config.Logger = new CompositeLogger(new[] {
        new ConsoleLogger(),
        dbLogger
    });
});
```

## Configuration Options

Configure AnnotationLogger with various options:

```csharp
LogManager.Configure(config => 
{
    // Logger and environment
    config.Logger = new CompositeLogger(new[] {
        new ConsoleLogger(),
        new FileLogger("app.log")
    });
    config.Environment = EnvironmentType.Development;
    
    // What to include in logs
    config.EnableMethodEntryExit = true;
    config.EnableParameterLogging = true;
    config.EnableReturnValueLogging = true;
    config.EnableExecutionTimeLogging = true;
    
    // Output format
    config.UseStructuredOutput = false;  // Set to true for JSON
    
    // Advanced settings
    config.MaxStringLength = 10000;      // Truncate long strings
    config.MaxCollectionItems = 100;     // Limit collection items
    config.IncludeStackTraces = true;
    config.MaxObjectDepth = 3;           // Nested object depth
    config.EnablePerformanceTracking = true;
});

// Configure data logging features
LogManager.ConfigureDataLogging(config => 
{
    config.EnableDataChangeTracking = true;
    config.MaxComparisonDepth = 3;
    config.IncludeSensitivePropertiesInChanges = false;
    config.LogBeforeState = false;
    config.LogAfterState = false;
    config.DataChangeLogLevel = LogLevel.Info;
});
```

## Implementation Details

AnnotationLogger uses several advanced C# features:

- **Expression Trees**: For method interception and parameter extraction
- **Reflection**: For attribute discovery and parameter information
- **AsyncLocal<T>**: For correlation ID propagation across async contexts
- **Stopwatch**: For precise execution time measurement
- **ReaderWriterLockSlim**: For thread-safe file logging
- **ConcurrentDictionary/ConcurrentBag**: For thread-safe performance tracking

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.