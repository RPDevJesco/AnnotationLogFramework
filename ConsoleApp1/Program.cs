using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AnnotationLogger;

namespace AnnotationLoggerDemo
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("==========================================================");
            Console.WriteLine("     AnnotationLogger Framework Demonstration");
            Console.WriteLine("==========================================================\n");
            
            // Configure logging system
            SetupLoggingSystem();
            
            try
            {
                // Create demo services
                var orderService = new OrderService();
                var customerService = new CustomerService(orderService);
                
                // Generate a unique correlation ID for this demo session
                CorrelationManager.StartNewCorrelation();
                Console.WriteLine($"Demo session correlation ID: {CorrelationManager.CurrentCorrelationId}\n");
                
                // Demonstrate different logging scenarios
                await RunStandardDemonstration(customerService);
                
                // Demonstrate new data-oriented features
                await RunDataOrientedDemonstration(customerService);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Demo terminated with error: {ex.Message}");
                Console.ResetColor();
            }
            
            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
        
        static void SetupLoggingSystem()
        {
            Console.WriteLine("Configuring logging system...");
            
            LogManager.Configure(config =>
            {
                // Create and configure our composite logger
                var compositeLogger = new CompositeLogger(LogLevel.Trace);
                
                // Add console logger with colorized output
                compositeLogger.AddLogger(new ConsoleLogger(LogLevel.Trace));
                
                // Add file logger for persistent logs
                string logFilePath = System.Environment.CurrentDirectory + "annotationlogger-demo.log";
                compositeLogger.AddLogger(new FileLogger(
                    LogLevel.Debug, 
                    logFilePath, 
                    useStructuredOutput: true, 
                    appendToFile: false
                ));
                
                // Configure global logging behavior
                config.Logger = compositeLogger;
                config.Environment = EnvironmentType.Development;
                config.EnableMethodEntryExit = true;
                config.EnableParameterLogging = true;
                config.EnableReturnValueLogging = true;
                config.EnableExecutionTimeLogging = true;
                config.UseStructuredOutput = false; // For console visibility
            });
            
            // Configure the data-oriented logging features
            LogManager.ConfigureDataLogging(config => 
            {
                config.EnableDataChangeTracking = true;
                config.MaxComparisonDepth = 4;
                config.LogBeforeState = false;
                config.LogAfterState = false;
                config.DataChangeLogLevel = LogLevel.Info;
            });
            
            Console.WriteLine("Logging system configured successfully!\n");
        }
        
        static async Task RunStandardDemonstration(CustomerService customerService)
        {
            Console.WriteLine("▶️ Starting demonstration of standard logging features...\n");
            
            // Demo 1: Basic method call with logging
            Console.WriteLine("\n📋 DEMO 1: Standard method calls with different log levels");
            var customers = LoggedMethodCaller.Call(() => customerService.GetAllCustomers());
            var customer = LoggedMethodCaller.Call(() => customerService.GetCustomerById(1));
            LoggedMethodCaller.Call(() => customerService.UpdateCustomerDetails(1, "Updated Customer", "updated@example.com"));
            
            // Demo 2: Exception handling and logging
            Console.WriteLine("\n📋 DEMO 2: Exception handling and error logging");
            try 
            {
                LoggedMethodCaller.Call(() => customerService.GetCustomerById(999)); // Non-existent customer
            }
            catch 
            {
                Console.WriteLine("Exception was caught and logged properly");
            }
            
            // Demo 3: Async method calls with logging
            Console.WriteLine("\n📋 DEMO 3: Async methods with Task-based return values");
            var ordersTask = LoggedMethodCaller.CallAsync(() => customerService.GetCustomerOrdersAsync(1));
            var orders = await ordersTask;
            
            await LoggedMethodCaller.CallAsync(() => customerService.ProcessOrderAsync(orders[0]));
            
            // Demo 4: Complex parameter and return value logging
            Console.WriteLine("\n📋 DEMO 4: Complex parameters and return values");
            var searchCriteria = new Dictionary<string, string>
            {
                { "name", "Premium" },
                { "status", "Active" },
                { "orderCount", ">5" }
            };
            
            var searchResults = LoggedMethodCaller.Call(() => customerService.SearchCustomers(searchCriteria));
            
            // Demo 5: Performance measurement
            Console.WriteLine("\n📋 DEMO 5: Performance measurement with execution time logging");
            for (int i = 1; i <= 3; i++)
            {
                LoggedMethodCaller.Call(() => customerService.GetCustomerStats(i));
            }
            
            // Demo 6: Environment-specific logging
            Console.WriteLine("\n📋 DEMO 6: Environment-specific logging behaviors");
            LoggedMethodCaller.Call(() => customerService.DebugOperation("This is debug info"));
            LoggedMethodCaller.Call(() => customerService.ProductionOperation("This is production info"));
            
            Console.WriteLine("\n✅ Standard demonstration completed successfully!");
        }
        
        static async Task RunDataOrientedDemonstration(CustomerService customerService)
        {
            Console.WriteLine("\n\n▶️ Starting demonstration of DATA-ORIENTED logging features...\n");
            
            // Demo 7: Data Change Tracking
            Console.WriteLine("\n📋 DEMO 7: Automatic data change tracking");
            
            // Get a customer to modify
            var originalCustomer = LoggedMethodCaller.Call(() => customerService.GetCustomerById(1));
            
            // Track changes to customer using attributes
            LoggedMethodCaller.Call(() => customerService.UpdateCustomerWithTracking(
                originalCustomer, 
                "John Modified Smith", 
                "john.modified@example.com", 
                CustomerType.Enterprise
            ));
            
            // Demo 8: Manual change tracking for complex scenarios
            Console.WriteLine("\n📋 DEMO 8: Manual data change tracking");
            var order = (await LoggedMethodCaller.CallAsync(() => customerService.GetCustomerOrdersAsync(1)))[0];
            
            // Update order with manual tracking
            LoggedMethodCaller.Call(() => customerService.UpdateOrderWithManualTracking(
                order,
                "Shipped",
                199.95m
            ));
            
            // Demo 9: Contextual logging across operations
            Console.WriteLine("\n📋 DEMO 9: Contextual logging across operations");
            LoggedMethodCaller.Call(() => customerService.ProcessCustomerWithContext(1));
            
            // Demo 10: Sensitive data handling
            Console.WriteLine("\n📋 DEMO 10: Sensitive data handling in logs");
            LoggedMethodCaller.Call(() => customerService.StoreCustomerPaymentInfo(
                1,
                "4111-1111-1111-1111",
                "12/25",
                "123",
                "John Smith"
            ));
            
            // Demo 11: Async operations with data tracking
            Console.WriteLine("\n📋 DEMO 11: Async operations with data tracking");
            await LoggedMethodCaller.CallAsync(() => customerService.UpdateCustomerTypeAsync(originalCustomer, CustomerType.Premium));
            
            // Demo 12: Using DataChangeTracker across service boundaries
            Console.WriteLine("\n📋 DEMO 12: Tracking data changes across service boundaries");
            LoggedMethodCaller.Call(() => customerService.ProcessComplexOrderChange(order));
            
            Console.WriteLine("\n✅ Data-oriented demonstration completed successfully!");
        }
    }
    
    // Domain Models
    public class Customer
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime RegistrationDate { get; set; }
        public CustomerType Type { get; set; }
        public List<Order> Orders { get; set; } = new List<Order>();
        
        // New property with sensitive information
        [MaskInLogs(ShowFirstChars = true, FirstCharsCount = 2, ShowLastChars = true, LastCharsCount = 2)]
        public string PhoneNumber { get; set; }
        
        [ExcludeFromLogs]
        public string InternalNotes { get; set; }
        
        [RedactContents]
        public string CustomerServiceFeedback { get; set; }
        
        [ExcludeFromComparison]
        public DateTime LastModified { get; set; } = DateTime.Now;
        
        public override string ToString() => $"Customer({Id}, {Name})";
    }
    
    public class CustomerPaymentInfo
    {
        public int CustomerId { get; set; }
        
        [MaskInLogs(ShowFirstChars = false, ShowLastChars = true, LastCharsCount = 4)]
        public string CreditCardNumber { get; set; }
        
        [MaskInLogs]
        public string ExpirationDate { get; set; }
        
        [ExcludeFromLogs]
        public string CVV { get; set; }
        
        public string CardholderName { get; set; }
    }
    
    public enum CustomerType
    {
        Standard,
        Premium,
        Enterprise
    }
    
    public class Order
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; }
        public DateTime OrderDate { get; set; }
        public List<OrderItem> Items { get; set; } = new List<OrderItem>();
        
        // Adding shipping information
        public ShippingInfo Shipping { get; set; }
        
        [ExcludeFromComparison]
        public DateTime LastUpdated { get; set; } = DateTime.Now;
        
        public override string ToString() => $"Order({Id}, ${Total})";
    }
    
    public class ShippingInfo
    {
        public string Address { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string ZipCode { get; set; }
        public string Method { get; set; }
        public decimal Cost { get; set; }
        public DateTime EstimatedDelivery { get; set; }
    }
    
    public class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        
        public override string ToString() => $"OrderItem({ProductName}, Qty: {Quantity})";
    }
    
    public class CustomerStatistics
    {
        public int CustomerId { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime FirstOrderDate { get; set; }
        public DateTime LastOrderDate { get; set; }
        public Dictionary<string, int> ProductsPurchased { get; set; }
    }
    
    // Service Classes with Enhanced Attributes
    public class CustomerService
    {
        private readonly OrderService _orderService;
        private readonly List<Customer> _customers;
        
        public CustomerService(OrderService orderService)
        {
            _orderService = orderService;
            _customers = GenerateSampleCustomers();
        }
        
        #region Standard API Methods
        
        [LogInfo]
        public List<Customer> GetAllCustomers()
        {
            // Simulate some work
            Task.Delay(50).Wait();
            return _customers;
        }
        
        [LogDebug(IncludeParameters = true, IncludeReturnValue = true)]
        public Customer GetCustomerById(int id)
        {
            // Simulate some work
            Task.Delay(30).Wait();
            
            var customer = _customers.Find(c => c.Id == id);
            if (customer == null)
                throw new ArgumentException($"Customer with ID {id} not found");
                
            return customer;
        }
        
        [LogWarning]
        public void UpdateCustomerDetails(int id, string name, string email)
        {
            // Validate inputs
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be empty");
                
            if (string.IsNullOrEmpty(email) || !email.Contains("@"))
                throw new ArgumentException("Invalid email format");
                
            // Simulate some work
            Task.Delay(100).Wait();
            
            var customer = GetCustomerById(id);
            customer.Name = name;
            customer.Email = email;
            customer.LastModified = DateTime.Now;
        }
        
        [LogInfo]
        public async Task<List<Order>> GetCustomerOrdersAsync(int customerId)
        {
            // Simulate async operation
            await Task.Delay(150);
            
            var customer = GetCustomerById(customerId);
            
            // Get orders from order service
            var orders = await _orderService.GetOrdersForCustomerAsync(customerId);
            customer.Orders = orders;
            
            return orders;
        }
        
        [LogTrace]
        public async Task ProcessOrderAsync(Order order)
        {
            // Validate order
            if (order == null)
                throw new ArgumentNullException(nameof(order));
                
            if (order.Total <= 0)
                throw new ArgumentException("Order total must be greater than zero");
                
            // Simulate processing time
            await Task.Delay(200);
            
            // Process payment
            await _orderService.ProcessPaymentAsync(order);
            
            // Update order status
            order.Status = "Processed";
            order.LastUpdated = DateTime.Now;
        }
        
        [LogInfo(IncludeParameters = true)]
        public List<Customer> SearchCustomers(Dictionary<string, string> criteria)
        {
            // Simulate search operation
            Task.Delay(120).Wait();
            
            var results = new List<Customer>();
            
            foreach (var customer in _customers)
            {
                bool matches = true;
                
                // Simple dictionary-based criteria matching
                foreach (var criterion in criteria)
                {
                    switch (criterion.Key.ToLower())
                    {
                        case "name":
                            if (!customer.Name.Contains(criterion.Value))
                                matches = false;
                            break;
                        case "status":
                            if (criterion.Value != "Active") // All are active in this demo
                                matches = false;
                            break;
                        case "ordercount":
                            // Simple operator parsing (>5 means more than 5 orders)
                            if (criterion.Value.StartsWith(">"))
                            {
                                int threshold = int.Parse(criterion.Value.Substring(1));
                                if (customer.Orders.Count <= threshold)
                                    matches = false;
                            }
                            break;
                    }
                }
                
                if (matches)
                    results.Add(customer);
            }
            
            return results;
        }
        
        [LogInfo(IncludeExecutionTime = true)]
        public CustomerStatistics GetCustomerStats(int customerId)
        {
            // Simulate heavy computational work
            Task.Delay(300).Wait();
            
            var customer = GetCustomerById(customerId);
            
            var stats = new CustomerStatistics
            {
                CustomerId = customerId,
                TotalOrders = customer.Orders.Count,
                TotalSpent = customer.Orders.Sum(o => o.Total),
                ProductsPurchased = new Dictionary<string, int>()
            };
            
            if (customer.Orders.Any())
            {
                stats.FirstOrderDate = customer.Orders.Min(o => o.OrderDate);
                stats.LastOrderDate = customer.Orders.Max(o => o.OrderDate);
                stats.AverageOrderValue = stats.TotalSpent / stats.TotalOrders;
                
                // Aggregate product purchases
                foreach (var order in customer.Orders)
                {
                    foreach (var item in order.Items)
                    {
                        if (stats.ProductsPurchased.ContainsKey(item.ProductName))
                            stats.ProductsPurchased[item.ProductName] += item.Quantity;
                        else
                            stats.ProductsPurchased[item.ProductName] = item.Quantity;
                    }
                }
            }
            
            return stats;
        }
        
        [LogDebug]
        public void DebugOperation(string debugData)
        {
            // This method demonstrates environment-specific logging
            // In production, Debug logs would be suppressed
            
            Console.WriteLine("Debug operation executed with data: " + debugData);
            Task.Delay(10).Wait();
        }
        
        [LogProd]
        public void ProductionOperation(string productionData)
        {
            // This method demonstrates the production logging attribute
            // This would log in all environments
            
            Console.WriteLine("Production operation executed with data: " + productionData);
            Task.Delay(10).Wait();
        }
        
        [LogError]
        public void SimulateError()
        {
            throw new InvalidOperationException("This is a simulated error");
        }
        
        #endregion
        
        #region Data-Oriented API Methods
        
        [LogInfo]
        [TrackDataChanges(MaxComparisonDepth = 3)]
        public Customer UpdateCustomerWithTracking(
            [BeforeChange] Customer customer, 
            string name, 
            string email, 
            CustomerType type)
        {
            // Make a copy of the customer to simulate updating
            var updatedCustomer = new Customer
            {
                Id = customer.Id,
                Name = name,
                Email = email,
                Type = type,
                RegistrationDate = customer.RegistrationDate,
                Orders = customer.Orders,
                PhoneNumber = customer.PhoneNumber,
                InternalNotes = customer.InternalNotes,
                CustomerServiceFeedback = customer.CustomerServiceFeedback,
                LastModified = DateTime.Now
            };
            
            // Replace the customer in the list
            int index = _customers.FindIndex(c => c.Id == customer.Id);
            if (index >= 0)
                _customers[index] = updatedCustomer;
                
            return updatedCustomer;
        }
        
        [LogInfo]
        public Order UpdateOrderWithManualTracking(Order originalOrder, string newStatus, decimal newTotal)
        {
            // Create a tracker for this change operation
            var tracker = DataChangeTracker.Track(originalOrder, originalOrder.Id.ToString(), "UpdateOrder")
                .WithContext("CustomerID", originalOrder.CustomerId)
                .WithContext("OriginalDate", originalOrder.OrderDate);
            
            // Create an updated copy
            var updatedOrder = new Order
            {
                Id = originalOrder.Id,
                CustomerId = originalOrder.CustomerId,
                Status = newStatus,
                Total = newTotal,
                OrderDate = originalOrder.OrderDate,
                Items = originalOrder.Items,
                Shipping = originalOrder.Shipping,
                LastUpdated = DateTime.Now
            };
            
            // Update the order in the customer's list
            var customer = GetCustomerById(originalOrder.CustomerId);
            int index = customer.Orders.FindIndex(o => o.Id == originalOrder.Id);
            if (index >= 0)
                customer.Orders[index] = updatedOrder;
            
            // Log the changes manually
            tracker.LogChanges(updatedOrder);
            
            return updatedOrder;
        }
        
        [LogInfo]
        public void ProcessCustomerWithContext(int customerId)
        {
            // Add global context for the customer processing
            LogManager.AddContext("CustomerID", customerId);
            LogManager.AddContext("ProcessStartTime", DateTime.Now);
            
            try
            {
                Console.WriteLine($"Starting processing for customer {customerId}");
                
                // Get customer details - context will be included in all these logs
                var customer = GetCustomerById(customerId);
                
                // Verify customer status
                VerifyCustomerStatus(customer);
                
                // Calculate pricing
                CalculateCustomPricing(customer);
                
                // Send notifications
                SendCustomerNotifications(customer);
                
                Console.WriteLine($"Completed processing for customer {customerId}");
            }
            finally
            {
                // Always clean up context when done
                LogManager.ClearContext();
            }
        }
        
        [LogInfo]
        private void VerifyCustomerStatus(Customer customer)
        {
            // This would contain verification logic
            // The logs will include the global context
            Task.Delay(50).Wait();
            Console.WriteLine($"  Verified customer status: {customer.Type}");
        }
        
        [LogDebug]
        private void CalculateCustomPricing(Customer customer)
        {
            // This would calculate custom pricing based on customer type
            // The logs will include the global context
            Task.Delay(100).Wait();
            Console.WriteLine($"  Calculated pricing for customer type: {customer.Type}");
        }
        
        [LogInfo]
        private void SendCustomerNotifications(Customer customer)
        {
            // This would send notifications
            // The logs will include the global context
            Task.Delay(75).Wait();
            Console.WriteLine($"  Sent notifications to: {customer.Email}");
        }
        
        [LogInfo]
        public CustomerPaymentInfo StoreCustomerPaymentInfo(
            int customerId,
            string creditCardNumber,
            string expirationDate,
            string cvv,
            string cardholderName)
        {
            var customer = GetCustomerById(customerId);
            
            // Create payment info object with sensitive data
            var paymentInfo = new CustomerPaymentInfo
            {
                CustomerId = customerId,
                CreditCardNumber = creditCardNumber,
                ExpirationDate = expirationDate,
                CVV = cvv,
                CardholderName = cardholderName
            };
            
            // Simulate storing payment info
            Task.Delay(100).Wait();
            Console.WriteLine($"Payment info stored for customer: {customer.Name}");
            
            return paymentInfo;
        }
        
        [LogInfo]
        [TrackDataChanges]
        public async Task<Customer> UpdateCustomerTypeAsync(
            [BeforeChange] Customer customer,
            CustomerType newType)
        {
            // Simulate async operation
            await Task.Delay(150);
            
            // Create updated customer
            var updatedCustomer = new Customer
            {
                Id = customer.Id,
                Name = customer.Name,
                Email = customer.Email,
                Type = newType, // This is the change we're tracking
                RegistrationDate = customer.RegistrationDate,
                Orders = customer.Orders,
                PhoneNumber = customer.PhoneNumber,
                InternalNotes = customer.InternalNotes,
                CustomerServiceFeedback = customer.CustomerServiceFeedback,
                LastModified = DateTime.Now
            };
            
            // Update in the repository
            int index = _customers.FindIndex(c => c.Id == customer.Id);
            if (index >= 0)
                _customers[index] = updatedCustomer;
            
            return updatedCustomer;
        }
        
        [LogInfo]
        public void ProcessComplexOrderChange(Order order)
        {
            Console.WriteLine($"Processing complex changes for order {order.Id}");
            
            // Track the original order
            var tracker = DataChangeTracker.Track(order, order.Id.ToString(), "ComplexOrderChange")
                .WithContext("ProcessingTime", DateTime.Now)
                .WithContext("ProcessedBy", "CustomerService");
                
            // Make a copy to update
            var updatedOrder = CloneOrder(order);
            
            // Step 1: Update shipping information
            UpdateShippingInformation(updatedOrder);
            
            // Step 2: Apply discounts
            ApplyOrderDiscounts(updatedOrder);
            
            // Step 3: Update the status
            updatedOrder.Status = "Ready for Shipment";
            
            // Log all changes at once
            tracker.LogChanges(updatedOrder);
            
            // Update in the repository
            var customer = GetCustomerById(order.CustomerId);
            int index = customer.Orders.FindIndex(o => o.Id == order.Id);
            if (index >= 0)
                customer.Orders[index] = updatedOrder;
        }
        
        private Order CloneOrder(Order original)
        {
            // Deep clone an order
            var clone = new Order
            {
                Id = original.Id,
                CustomerId = original.CustomerId,
                Total = original.Total,
                Status = original.Status,
                OrderDate = original.OrderDate,
                Items = new List<OrderItem>(original.Items),
                LastUpdated = DateTime.Now
            };
            
            if (original.Shipping != null)
            {
                clone.Shipping = new ShippingInfo
                {
                    Address = original.Shipping.Address,
                    City = original.Shipping.City,
                    State = original.Shipping.State,
                    ZipCode = original.Shipping.ZipCode,
                    Method = original.Shipping.Method,
                    Cost = original.Shipping.Cost,
                    EstimatedDelivery = original.Shipping.EstimatedDelivery
                };
            }
            else
            {
                clone.Shipping = new ShippingInfo
                {
                    Address = "123 Default St",
                    City = "Defaultville",
                    State = "DE",
                    ZipCode = "12345",
                    Method = "Standard",
                    Cost = 9.99m,
                    EstimatedDelivery = DateTime.Now.AddDays(5)
                };
            }
            
            return clone;
        }
        
        private void UpdateShippingInformation(Order order)
        {
            // Update shipping information
            order.Shipping.Method = "Express";
            order.Shipping.Cost = 14.99m;
            order.Shipping.EstimatedDelivery = DateTime.Now.AddDays(2);
        }
        
        private void ApplyOrderDiscounts(Order order)
        {
            // Apply a 10% discount
            decimal discount = order.Total * 0.1m;
            order.Total -= discount;
        }
        
        #endregion
        
        private List<Customer> GenerateSampleCustomers()
        {
            // Create sample data for demonstration
            var customers = new List<Customer>
            {
                new Customer
                {
                    Id = 1,
                    Name = "John Smith",
                    Email = "john@example.com",
                    RegistrationDate = DateTime.Now.AddDays(-60),
                    Type = CustomerType.Premium,
                    PhoneNumber = "555-123-4567",
                    InternalNotes = "Priority customer, handle with care",
                    CustomerServiceFeedback = "Customer complained about delivery time on last order"
                },
                new Customer
                {
                    Id = 2,
                    Name = "Sarah Johnson",
                    Email = "sarah@example.com",
                    RegistrationDate = DateTime.Now.AddDays(-45),
                    Type = CustomerType.Standard,
                    PhoneNumber = "555-987-6543",
                    InternalNotes = "New customer, offer promotions",
                    CustomerServiceFeedback = "Very satisfied with recent support interaction"
                },
                new Customer
                {
                    Id = 3,
                    Name = "Acme Corporation",
                    Email = "contact@acme.com",
                    RegistrationDate = DateTime.Now.AddDays(-120),
                    Type = CustomerType.Enterprise,
                    PhoneNumber = "555-222-3333",
                    InternalNotes = "Corporate account, assigned to business team",
                    CustomerServiceFeedback = "Multiple requests for bulk discount pricing"
                }
            };
            
            return customers;
        }
    }
    
    public class OrderService
    {
        private readonly Dictionary<int, List<Order>> _customerOrders;
        
        public OrderService()
        {
            _customerOrders = GenerateSampleOrders();
        }
        
        [LogInfo]
        public async Task<List<Order>> GetOrdersForCustomerAsync(int customerId)
        {
            // Simulate async database query
            await Task.Delay(100);
            
            if (_customerOrders.TryGetValue(customerId, out var orders))
                return orders;
                
            return new List<Order>();
        }
        
        [LogInfo]
        public async Task<bool> ProcessPaymentAsync(Order order)
        {
            // Simulate payment processing
            await Task.Delay(150);
            
            // Add correlation ID to simulate distributed processing
            var paymentCorrelationId = CorrelationManager.CurrentCorrelationId;
            Console.WriteLine($"Processing payment with correlation ID: {paymentCorrelationId}");
            
            return true; // Success
        }
        
        private Dictionary<int, List<Order>> GenerateSampleOrders()
        {
            var orders = new Dictionary<int, List<Order>>();
            
            // Sample shipping info
            var standardShipping = new ShippingInfo
            {
                Address = "123 Main St",
                City = "Anytown",
                State = "NY",
                ZipCode = "12345",
                Method = "Standard",
                Cost = 5.99m,
                EstimatedDelivery = DateTime.Now.AddDays(5)
            };
            
            // Orders for Customer 1
            orders[1] = new List<Order>
            {
                new Order
                {
                    Id = 101,
                    CustomerId = 1,
                    Total = 149.95m,
                    Status = "Completed",
                    OrderDate = DateTime.Now.AddDays(-30),
                    Shipping = standardShipping,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1001, OrderId = 101, ProductName = "Laptop", Price = 99.95m, Quantity = 1 },
                        new OrderItem { Id = 1002, OrderId = 101, ProductName = "Mouse", Price = 25.00m, Quantity = 2 }
                    }
                },
                new Order
                {
                    Id = 102,
                    CustomerId = 1,
                    Total = 75.50m,
                    Status = "Completed",
                    OrderDate = DateTime.Now.AddDays(-15),
                    Shipping = standardShipping,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1003, OrderId = 102, ProductName = "Headphones", Price = 75.50m, Quantity = 1 }
                    }
                }
            };
            
            // Orders for Customer 2
            orders[2] = new List<Order>
            {
                new Order
                {
                    Id = 103,
                    CustomerId = 2,
                    Total = 49.99m,
                    Status = "Completed",
                    OrderDate = DateTime.Now.AddDays(-10),
                    Shipping = standardShipping,
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1004, OrderId = 103, ProductName = "Book", Price = 24.99m, Quantity = 2 }
                    }
                }
            };
            
            // Orders for Customer 3
            orders[3] = new List<Order>
            {
                new Order
                {
                    Id = 104,
                    CustomerId = 3,
                    Total = 999.95m,
                    Status = "Completed",
                    OrderDate = DateTime.Now.AddDays(-60),
                    Shipping = new ShippingInfo {
                        Address = "789 Corporate Parkway",
                        City = "Business City",
                        State = "CA",
                        ZipCode = "90210",
                        Method = "Priority",
                        Cost = 15.99m,
                        EstimatedDelivery = DateTime.Now.AddDays(-58)
                    },
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1005, OrderId = 104, ProductName = "Server", Price = 999.95m, Quantity = 1 }
                    }
                },
                new Order
                {
                    Id = 105,
                    CustomerId = 3,
                    Total = 1500.00m,
                    Status = "Completed",
                    OrderDate = DateTime.Now.AddDays(-45),
                    Shipping = new ShippingInfo {
                        Address = "789 Corporate Parkway",
                        City = "Business City",
                        State = "CA",
                        ZipCode = "90210",
                        Method = "Priority",
                        Cost = 15.99m,
                        EstimatedDelivery = DateTime.Now.AddDays(-42)
                    },
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1006, OrderId = 105, ProductName = "Software License", Price = 500.00m, Quantity = 3 }
                    }
                },
                new Order
                {
                    Id = 106,
                    CustomerId = 3,
                    Total = 299.95m,
                    Status = "Completed",
                    OrderDate = DateTime.Now.AddDays(-30),
                    Shipping = new ShippingInfo {
                        Address = "789 Corporate Parkway",
                        City = "Business City",
                        State = "CA",
                        ZipCode = "90210",
                        Method = "Standard",
                        Cost = 0.00m, // Free shipping
                        EstimatedDelivery = DateTime.Now.AddDays(-25)
                    },
                    Items = new List<OrderItem>
                    {
                        new OrderItem { Id = 1007, OrderId = 106, ProductName = "Support Contract", Price = 299.95m, Quantity = 1 }
                    }
                }
            };
            
            return orders;
        }
    }
}