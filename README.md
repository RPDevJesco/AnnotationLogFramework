# AnnotationLogger

A high-performance, attributes-based .NET logging framework that brings clean code, aspect-oriented programming, and data-oriented design principles to your application logging.

## Overview

AnnotationLogger is an advanced .NET logging framework that leverages C# attributes to provide declarative, aspect-oriented logging with minimal code clutter. By simply annotating your methods with log attributes, you can automatically capture entry/exit tracking, parameter values, return values, execution times, and data changes without polluting your business logic with logging code.

## Table of Contents

- [Key Features](#key-features)
- [Why AnnotationLogger?](#why-annotationlogger)
- [Getting Started](#getting-started)
- [Basic Usage](#basic-usage)
- [Advanced Usage](#advanced-usage)
  - [Logging Configuration](#logging-configuration)
  - [Log Levels](#log-levels)
  - [Correlation IDs](#correlation-ids)
  - [Performance Considerations](#performance-considerations)
- [Data-Oriented Features](#data-oriented-features)
  - [Data Change Tracking](#data-change-tracking)
  - [Sensitive Data Protection](#sensitive-data-protection)
  - [Contextual Logging](#contextual-logging)
- [Usage Scenarios](#usage-scenarios)
  - [API Services](#api-services)
  - [Microservices](#microservices)
  - [Enterprise Applications](#enterprise-applications)
  - [Diagnostic Logging](#diagnostic-logging)
  - [Data Audit Trail](#data-audit-trail)
- [Implementation Details](#implementation-details)
- [Contributing](#contributing)
- [License](#license)

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
- **Data Change Tracking**: Automatically detects and logs changes to domain objects
- **Sensitive Data Protection**: Masks, excludes, or redacts sensitive information in logs
- **Contextual Logging**: Add rich context that flows through related operations

## Why AnnotationLogger?

### Comparison with Popular Logging Libraries

| Feature | AnnotationLogger | Serilog | NLog | log4net |
|---------|-----------------|---------|------|---------|
| Aspect-Oriented Logging | ✅ | ❌ | ❌ | ❌ |
| Clean Business Logic | ✅ | ❌ | ❌ | ❌ |
| Automatic Parameter Capture | ✅ | ❌ | ❌ | ❌ |
| Data Change Tracking | ✅ | ❌ | ❌ | ❌ |
| Sensitive Data Protection | ✅ | ⚠️ | ⚠️ | ❌ |
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

**6. Data-Oriented Insights**

The data-oriented features enable tracking what changed in your domain objects, protecting sensitive information, and maintaining context across operations.

## Getting Started

### Quick Start

1. Configure the logger:

```csharp
using AnnotationLogger;

// In your application startup
LogManager.Configure(config =>
{
    config.Logger = new ConsoleLogger();
    config.Environment = EnvironmentType.Development;
    config.EnableMethodEntryExit = true;
});

// Configure data-oriented features
LogManager.ConfigureDataLogging(config => 
{
    config.EnableDataChangeTracking = true;
    config.MaxComparisonDepth = 3;
    config.DataChangeLogLevel = LogLevel.Info;
});
```

2. Annotate your methods:

```csharp
using AnnotationLogger;

public class CustomerService
{
    [LogInfo]
    public Customer GetCustomer(int id)
    {
        // Method logic here
        return new Customer { Id = id, Name = "Test Customer" };
    }
    
    [LogDebug]
    public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
    {
        // Async method logic
        return await _orderRepository.GetOrdersForCustomerAsync(customerId);
    }
    
    [LogInfo]
    [TrackDataChanges]
    public Customer UpdateCustomer(
        [BeforeChange] Customer existingCustomer, 
        string newName, 
        string newEmail)
    {
        // Data changes will be tracked automatically
        var updatedCustomer = new Customer
        {
            Id = existingCustomer.Id,
            Name = newName,
            Email = newEmail
            // Other properties
        };
        
        return updatedCustomer;
    }
}
```

3. Call methods using the LoggedMethodCaller:

```csharp
using AnnotationLogger;

var customer = LoggedMethodCaller.Call(() => customerService.GetCustomer(123));
var orders = await LoggedMethodCaller.CallAsync(() => customerService.GetCustomerOrdersAsync(123));

// With data change tracking
var updatedCustomer = LoggedMethodCaller.Call(() => customerService.UpdateCustomer(
    customer, 
    "John Updated", 
    "john.updated@example.com"
));
```

## Basic Usage

### Annotating Methods

Use the pre-defined attribute classes to decorate your methods:

```csharp
[LogTrace]      // Highly detailed diagnostic information
[LogDebug]      // Debug-build information
[LogInfo]       // Standard operational information
[LogWarning]    // Potential issues that aren't errors
[LogError]      // Errors and exceptions
[LogCritical]   // Severe failures
```

### Customizing Log Attributes

You can customize what gets logged:

```csharp
[LogInfo(IncludeParameters = true, IncludeReturnValue = true, IncludeExecutionTime = true)]
public Customer GetCustomer(int id)
{
    // Method implementation
}
```

### Using LoggedMethodCaller

The primary API for invoking methods with logging:

```csharp
// Method with return value
Customer customer = LoggedMethodCaller.Call(() => _service.GetCustomer(123));

// Void method
LoggedMethodCaller.Call(() => _service.UpdateCustomer(customer));

// Async method with return value
List<Order> orders = await LoggedMethodCaller.CallAsync(() => _service.GetOrdersAsync(123));

// Async void method
await LoggedMethodCaller.CallAsync(() => _service.ProcessOrderAsync(order));
```

## Advanced Usage

### Logging Configuration

Configure logging behavior in your application startup:

```csharp
LogManager.Configure(config =>
{
    // Use composite logger to send logs to multiple destinations
    var compositeLogger = new CompositeLogger(LogLevel.Debug);
    compositeLogger.AddLogger(new ConsoleLogger(LogLevel.Debug));
    compositeLogger.AddLogger(new FileLogger(LogLevel.Info, "app.log", useStructuredOutput: true));
    
    config.Logger = compositeLogger;
    config.Environment = EnvironmentType.Production;
    config.EnableMethodEntryExit = true;
    config.EnableParameterLogging = true;
    config.EnableReturnValueLogging = true;
    config.EnableExecutionTimeLogging = true;
});

// Configure data-oriented features
LogManager.ConfigureDataLogging(config => 
{
    config.EnableDataChangeTracking = true;
    config.MaxComparisonDepth = 3;
    config.LogBeforeState = false;
    config.LogAfterState = false;
    config.DataChangeLogLevel = LogLevel.Info;
});
```

### Log Levels

AnnotationLogger supports standard log levels:

- **Trace**: Most detailed information for deep diagnostics
- **Debug**: Information useful for debugging
- **Info**: General operational information
- **Warning**: Potential issues that aren't errors
- **Error**: Exceptions and failures
- **Critical**: Severe errors that may require immediate attention

### Correlation IDs

For distributed systems, you can track operations across multiple services:

```csharp
// Set correlation ID at the entry point
CorrelationManager.StartNewCorrelation();

// Or use an existing correlation ID (e.g., from incoming request)
string requestCorrelationId = httpContext.Request.Headers["X-Correlation-ID"];
CorrelationManager.CurrentCorrelationId = requestCorrelationId;

// Correlation ID is automatically included in all log entries
```

### Performance Considerations

AnnotationLogger uses expression trees and reflection, which has some performance overhead. For extremely performance-critical methods that are called thousands of times per second, consider:

- Using the `[LogTrace]` attribute, which can be disabled in production
- Disabling parameter logging for methods with large object parameters
- Using the LoggingAspect direct approach for more control
- Setting an appropriate MaxComparisonDepth for data change tracking
- Using ExcludeFromComparison for frequently changing properties that aren't important for auditing

## Data-Oriented Features

### Data Change Tracking

Track changes to your domain objects automatically:

```csharp
// Using attributes for automatic tracking
[LogInfo]
[TrackDataChanges(MaxComparisonDepth = 3)]
public Customer UpdateCustomer(
    [BeforeChange] Customer existingCustomer, 
    string newName, 
    string newEmail)
{
    // Changes will be automatically tracked and logged
    var updatedCustomer = new Customer
    {
        Id = existingCustomer.Id,
        Name = newName,
        Email = newEmail
        // Other properties
    };
    
    return updatedCustomer;
}

// Exclude properties from comparison
public class Customer
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    
    [ExcludeFromComparison]
    public DateTime LastModified { get; set; } = DateTime.Now;
}

// Manual tracking for complex scenarios
public Order UpdateOrderTotal(Order originalOrder, decimal newTotal)
{
    // Create a tracker for this change
    var tracker = DataChangeTracker.Track(originalOrder, originalOrder.Id.ToString(), "UpdateTotal")
        .WithContext("CustomerID", originalOrder.CustomerId)
        .WithContext("OriginalDate", originalOrder.OrderDate);
    
    // Create updated order
    var updatedOrder = new Order
    {
        Id = originalOrder.Id,
        CustomerId = originalOrder.CustomerId,
        Total = newTotal,
        // Other properties
    };
    
    // Log changes
    tracker.LogChanges(updatedOrder);
    
    return updatedOrder;
}
```

### Sensitive Data Protection

Protect sensitive information in your logs:

```csharp
public class PaymentInfo
{
    public int CustomerId { get; set; }
    
    // Show only last 4 digits
    [MaskInLogs(ShowFirstChars = false, ShowLastChars = true, LastCharsCount = 4)]
    public string CreditCardNumber { get; set; }
    
    // Use default masking (complete replacement with ***)
    [MaskInLogs]
    public string ExpirationDate { get; set; }
    
    // Completely exclude from logs
    [ExcludeFromLogs]
    public string CVV { get; set; }
    
    // Replace with custom message
    [RedactContents(ReplacementText = "[SENSITIVE CUSTOMER DATA]")]
    public string CustomerNotes { get; set; }
}
```

### Contextual Logging

Add context information that flows through operations:

```csharp
public void ProcessOrder(int orderId, int customerId)
{
    // Add global context for this operation
    LogManager.AddContext("OrderID", orderId);
    LogManager.AddContext("CustomerID", customerId);
    LogManager.AddContext("OperationStart", DateTime.UtcNow);
    
    try
    {
        // All these method calls will include the context
        VerifyOrder(orderId);
        ProcessPayment(orderId);
        SendNotifications(customerId);
    }
    finally
    {
        // Always clean up context when done
        LogManager.ClearContext();
    }
}
```

## Usage Scenarios

### API Services

For RESTful APIs or microservices, annotate your controller methods:

```csharp
[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    
    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }
    
    [HttpGet("{id}")]
    [LogInfo]
    public ActionResult<Customer> GetCustomer(int id)
    {
        var customer = LoggedMethodCaller.Call(() => _customerService.GetCustomer(id));
        if (customer == null)
            return NotFound();
            
        return Ok(customer);
    }
    
    [HttpPut("{id}")]
    [LogInfo]
    public ActionResult<Customer> UpdateCustomer(int id, CustomerUpdateModel model)
    {
        var existingCustomer = LoggedMethodCaller.Call(() => _customerService.GetCustomer(id));
        if (existingCustomer == null)
            return NotFound();
            
        var updatedCustomer = LoggedMethodCaller.Call(() => _customerService.UpdateCustomer(
            existingCustomer, 
            model.Name, 
            model.Email
        ));
            
        return Ok(updatedCustomer);
    }
}
```

### Microservices

For distributed systems with multiple services:

```csharp
public class OrderProcessor
{
    private readonly IMessagePublisher _publisher;
    
    [LogInfo]
    public async Task ProcessOrderAsync(Order order)
    {
        // Process order
        
        // Publish event with correlation ID
        await _publisher.PublishAsync(new OrderProcessedEvent(order.Id)
        {
            CorrelationId = CorrelationManager.CurrentCorrelationId
        });
    }
}
```

### Enterprise Applications

For large-scale applications with many components:

```csharp
// Application entry point
public class Application
{
    public void Initialize()
    {
        LogManager.Configure(config =>
        {
            // Complex logging setup for enterprise needs
            var compositeLogger = new CompositeLogger();
            
            // Development console logging
            if (Environment.IsDevelopment())
            {
                compositeLogger.AddLogger(new ConsoleLogger(LogLevel.Debug));
            }
            
            // Standard file logging
            var logPath = Path.Combine(AppContext.BaseDirectory, "logs", "app.log");
            compositeLogger.AddLogger(new FileLogger(LogLevel.Info, logPath));
            
            // Add other log destinations as needed
            // compositeLogger.AddLogger(new ElasticSearchLogger());
            // compositeLogger.AddLogger(new AzureAppInsightsLogger());
            
            config.Logger = compositeLogger;
        });
        
        // Configure data-oriented features
        LogManager.ConfigureDataLogging(config => 
        {
            config.EnableDataChangeTracking = true;
            config.MaxComparisonDepth = 3;
            config.DataChangeLogLevel = LogLevel.Info;
        });
    }
}
```

### Diagnostic Logging

For detailed troubleshooting in complex systems:

```csharp
public class DiagnosticService
{
    [LogTrace(IncludeParameters = true, IncludeReturnValue = true, IncludeExecutionTime = true)]
    public async Task<DiagnosticResult> RunDiagnosticsAsync(DiagnosticOptions options)
    {
        // The method input, output, and execution time will be logged automatically
        var result = await PerformDiagnosticChecksAsync(options);
        return result;
    }
}
```

### Data Audit Trail

For tracking changes to important entities:

```csharp
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly IAuditService _auditService;
    
    [LogInfo]
    [TrackDataChanges(OperationType = "UserUpdate")]
    public User UpdateUserProfile(
        [BeforeChange] User existingUser,
        UserProfileUpdateModel updateModel)
    {
        // Create updated user
        var updatedUser = new User
        {
            Id = existingUser.Id,
            Username = existingUser.Username,
            Email = updateModel.Email ?? existingUser.Email,
            FirstName = updateModel.FirstName ?? existingUser.FirstName,
            LastName = updateModel.LastName ?? existingUser.LastName,
            PhoneNumber = updateModel.PhoneNumber ?? existingUser.PhoneNumber,
            LastModified = DateTime.UtcNow
        };
        
        // Save to repository
        _repository.Update(updatedUser);
        
        // Changes will be automatically logged with data change tracking
        return updatedUser;
    }
}
```

## Implementation Details

AnnotationLogger uses several advanced C# features:

- **Expression Trees**: For method interception and parameter extraction
- **Reflection**: For attribute discovery and parameter information
- **AsyncLocal<T>**: For correlation ID propagation across async contexts
- **Stopwatch**: For precise execution time measurement
- **ReaderWriterLockSlim**: For thread-safe file logging
- **Object Comparison**: For detecting changes in complex object graphs
- **Attribute-Based Metadata**: For controlling logging behavior and sensitivity

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the LICENSE file for details.
