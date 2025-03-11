using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace AnnotationLogger
{
    /// <summary>
    /// Attribute to mark a method as tracking data changes
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public class TrackDataChangesAttribute : Attribute
    {
        /// <summary>
        /// When true, detailed comparison of objects will be performed
        /// </summary>
        public bool DetailedComparison { get; set; } = true;
        
        /// <summary>
        /// Maximum depth for object comparison
        /// </summary>
        public int MaxComparisonDepth { get; set; } = 3;
        
        /// <summary>
        /// When true, logs will include the original state
        /// </summary>
        public bool IncludeOriginalState { get; set; } = false;
        
        /// <summary>
        /// When true, logs will include the updated state
        /// </summary>
        public bool IncludeUpdatedState { get; set; } = false;
        
        /// <summary>
        /// Override the default operation type with a custom name
        /// </summary>
        public string OperationType { get; set; }
    }
    
    /// <summary>
    /// Attribute to mark a parameter as containing the before-change state
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class BeforeChangeAttribute : Attribute
    {
    }
    
    /// <summary>
    /// Attribute to mark a parameter as containing the after-change state
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
    public class AfterChangeAttribute : Attribute
    {
    }
    
    /// <summary>
    /// Captures and represents changes between two objects
    /// </summary>
    public class ChangeRecord
    {
        /// <summary>
        /// Name of the property that changed
        /// </summary>
        public string PropertyPath { get; set; }
        
        /// <summary>
        /// Value before the change
        /// </summary>
        public object OldValue { get; set; }
        
        /// <summary>
        /// Value after the change
        /// </summary>
        public object NewValue { get; set; }
        
        /// <summary>
        /// Type of the property that changed
        /// </summary>
        public Type PropertyType { get; set; }
        
        public override string ToString()
        {
            return $"{PropertyPath}: {OldValue} -> {NewValue}";
        }
    }
    
    /// <summary>
    /// Helper class for detecting differences between objects
    /// </summary>
    public static class ObjectComparer
    {
        /// <summary>
        /// Compares two objects and returns a list of changes
        /// </summary>
        public static List<ChangeRecord> CompareObjects(object before, object after, int maxDepth = 3, string path = "")
        {
            var changes = new List<ChangeRecord>();
            
            // If either object is null, handle that case
            if (before == null && after == null)
                return changes;
            
            if (before == null || after == null)
            {
                changes.Add(new ChangeRecord
                {
                    PropertyPath = path,
                    OldValue = before,
                    NewValue = after,
                    PropertyType = (before ?? after)?.GetType()
                });
                return changes;
            }
            
            // If they're not the same type, treat as completely different
            if (before.GetType() != after.GetType())
            {
                changes.Add(new ChangeRecord
                {
                    PropertyPath = path,
                    OldValue = before,
                    NewValue = after,
                    PropertyType = before.GetType()
                });
                return changes;
            }
            
            Type type = before.GetType();
            
            // Handle primitive types, strings, and other value types directly
            if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime) || type.IsEnum)
            {
                if (!object.Equals(before, after))
                {
                    changes.Add(new ChangeRecord
                    {
                        PropertyPath = path,
                        OldValue = before,
                        NewValue = after,
                        PropertyType = type
                    });
                }
                return changes;
            }
            
            // Don't go deeper than maxDepth
            if (maxDepth <= 0)
            {
                if (!object.Equals(before, after))
                {
                    changes.Add(new ChangeRecord
                    {
                        PropertyPath = path,
                        OldValue = $"{before.GetType().Name} (max depth reached)",
                        NewValue = $"{after.GetType().Name} (max depth reached)",
                        PropertyType = type
                    });
                }
                return changes;
            }
            
            // Handle dictionaries
            if (typeof(System.Collections.IDictionary).IsAssignableFrom(type))
            {
                var beforeDict = (System.Collections.IDictionary)before;
                var afterDict = (System.Collections.IDictionary)after;
                
                // Check for removed keys
                foreach (var key in beforeDict.Keys)
                {
                    if (!afterDict.Contains(key))
                    {
                        changes.Add(new ChangeRecord
                        {
                            PropertyPath = $"{path}[{key}]",
                            OldValue = beforeDict[key],
                            NewValue = null,
                            PropertyType = beforeDict[key]?.GetType()
                        });
                    }
                }
                
                // Check for added or changed keys
                foreach (var key in afterDict.Keys)
                {
                    string itemPath = string.IsNullOrEmpty(path) ? $"[{key}]" : $"{path}[{key}]";
                    
                    if (!beforeDict.Contains(key))
                    {
                        changes.Add(new ChangeRecord
                        {
                            PropertyPath = itemPath,
                            OldValue = null,
                            NewValue = afterDict[key],
                            PropertyType = afterDict[key]?.GetType()
                        });
                    }
                    else
                    {
                        // Recursively compare values
                        changes.AddRange(CompareObjects(beforeDict[key], afterDict[key], maxDepth - 1, itemPath));
                    }
                }
                
                return changes;
            }
            
            // Handle collections
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string))
            {
                // This is a simple approach - for collections, we just note if they're different
                // A more sophisticated approach would use a diff algorithm to find specific changes
                var beforeList = ((System.Collections.IEnumerable)before).Cast<object>().ToList();
                var afterList = ((System.Collections.IEnumerable)after).Cast<object>().ToList();
                
                if (beforeList.Count != afterList.Count)
                {
                    changes.Add(new ChangeRecord
                    {
                        PropertyPath = path,
                        OldValue = $"Count: {beforeList.Count}",
                        NewValue = $"Count: {afterList.Count}",
                        PropertyType = type
                    });
                }
                else
                {
                    // Simple element-by-element comparison for same-size collections
                    for (int i = 0; i < beforeList.Count; i++)
                    {
                        string itemPath = string.IsNullOrEmpty(path) ? $"[{i}]" : $"{path}[{i}]";
                        changes.AddRange(CompareObjects(beforeList[i], afterList[i], maxDepth - 1, itemPath));
                    }
                }
                
                return changes;
            }
            
            // For regular objects, compare their properties
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
            
            foreach (var prop in properties)
            {
                // Skip properties with the ExcludeFromComparison attribute
                if (prop.GetCustomAttribute<ExcludeFromComparisonAttribute>() != null)
                    continue;
                
                string propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
                
                try
                {
                    object beforeValue = prop.GetValue(before);
                    object afterValue = prop.GetValue(after);
                    
                    changes.AddRange(CompareObjects(beforeValue, afterValue, maxDepth - 1, propPath));
                }
                catch (Exception ex)
                {
                    // Log that we couldn't compare this property
                    changes.Add(new ChangeRecord
                    {
                        PropertyPath = propPath,
                        OldValue = "<error reading value>",
                        NewValue = "<error reading value>",
                        PropertyType = prop.PropertyType
                    });
                }
            }
            
            return changes;
        }
    }
    
    /// <summary>
    /// Attribute to exclude a property from comparison when tracking changes
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class ExcludeFromComparisonAttribute : Attribute
    {
        /// <summary>
        /// Reason for excluding this property from comparison
        /// </summary>
        public string Reason { get; }
        
        public ExcludeFromComparisonAttribute(string reason = null)
        {
            Reason = reason;
        }
    }
}