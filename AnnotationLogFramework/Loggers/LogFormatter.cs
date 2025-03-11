using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace AnnotationLogger
{
    /// <summary>
    /// Helper for formatting log values with sensitivity handling
    /// </summary>
    public static class LogFormatter
    {
        /// <summary>
        /// Format an object for logging with sensitivity handling
        /// </summary>
        public static object FormatForLog(object value, string parameterName, ParameterInfo parameterInfo, int maxDepth = 3)
        {
            if (value == null) return "null";
            
            // Check for direct parameter sensitivity attributes
            if (parameterInfo != null)
            {
                var excludeAttr = parameterInfo.GetCustomAttribute<ExcludeFromLogsAttribute>();
                if (excludeAttr != null)
                {
                    return "[EXCLUDED]";
                }
                
                var maskAttr = parameterInfo.GetCustomAttribute<MaskInLogsAttribute>();
                if (maskAttr != null)
                {
                    return ApplyMask(value.ToString(), maskAttr);
                }
                
                var redactAttr = parameterInfo.GetCustomAttribute<RedactContentsAttribute>();
                if (redactAttr != null)
                {
                    return redactAttr.ReplacementText;
                }
            }
            
            // Special handling for strings
            if (value is string str)
            {
                return str;
            }
            
            // Handle dictionaries
            if (value is IDictionary dictionary)
            {
                return FormatDictionary(dictionary, maxDepth);
            }
            
            // Handle collections
            if (value is IEnumerable collection && !(value is string))
            {
                return FormatCollection(collection, maxDepth);
            }
            
            // Handle objects with properties
            if (value.GetType().IsClass && value.GetType() != typeof(string))
            {
                return FormatObject(value, maxDepth);
            }
            
            // Default for simple types
            return value.ToString();
        }
        
        /// <summary>
        /// Format an object with sensitivity handling for complex objects
        /// </summary>
        private static object FormatObject(object value, int maxDepth)
        {
            if (maxDepth <= 0)
                return $"{value.GetType().Name} (max depth reached)";
                
            var type = value.GetType();
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.GetIndexParameters().Length == 0);
                
            var result = new Dictionary<string, object>();
            
            foreach (var prop in properties)
            {
                try
                {
                    // Check for property-level sensitivity
                    var excludeAttr = prop.GetCustomAttribute<ExcludeFromLogsAttribute>();
                    if (excludeAttr != null)
                    {
                        result[prop.Name] = "[EXCLUDED]";
                        continue;
                    }
                    
                    var maskAttr = prop.GetCustomAttribute<MaskInLogsAttribute>();
                    if (maskAttr != null)
                    {
                        var propVal = prop.GetValue(value);
                        result[prop.Name] = propVal != null ? ApplyMask(propVal.ToString(), maskAttr) : "null";
                        continue;
                    }
                    
                    var redactAttr = prop.GetCustomAttribute<RedactContentsAttribute>();
                    if (redactAttr != null)
                    {
                        result[prop.Name] = redactAttr.ReplacementText;
                        continue;
                    }
                    
                    // No sensitivity - recursively format
                    var propValue = prop.GetValue(value);
                    if (propValue == null)
                    {
                        result[prop.Name] = "null";
                    }
                    else if (propValue.GetType().IsPrimitive || propValue is string || propValue is DateTime)
                    {
                        result[prop.Name] = propValue.ToString();
                    }
                    else
                    {
                        // Recursively format complex properties
                        result[prop.Name] = FormatForLog(propValue, prop.Name, null, maxDepth - 1);
                    }
                }
                catch
                {
                    result[prop.Name] = "<error reading value>";
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Format a dictionary for logging with sensitivity handling
        /// </summary>
        private static object FormatDictionary(IDictionary dictionary, int maxDepth)
        {
            if (maxDepth <= 0)
                return $"Dictionary (max depth reached)";
                
            var result = new Dictionary<string, object>();
            
            foreach (DictionaryEntry entry in dictionary)
            {
                string key = entry.Key?.ToString() ?? "null";
                
                if (entry.Value == null)
                {
                    result[key] = "null";
                }
                else if (entry.Value.GetType().IsPrimitive || entry.Value is string || entry.Value is DateTime)
                {
                    result[key] = entry.Value.ToString();
                }
                else
                {
                    // Recursively format complex values
                    result[key] = FormatForLog(entry.Value, key, null, maxDepth - 1);
                }
            }
            
            return result;
        }
        
        /// <summary>
        /// Format a collection for logging with sensitivity handling
        /// </summary>
        private static object FormatCollection(IEnumerable collection, int maxDepth)
        {
            if (maxDepth <= 0)
                return $"Collection (max depth reached)";
                
            var result = new List<object>();
            int count = 0;
            
            foreach (var item in collection)
            {
                if (count >= 10) // Limit items for readability
                {
                    result.Add("...");
                    break;
                }
                
                if (item == null)
                {
                    result.Add("null");
                }
                else if (item.GetType().IsPrimitive || item is string || item is DateTime)
                {
                    result.Add(item.ToString());
                }
                else
                {
                    // Recursively format complex items
                    result.Add(FormatForLog(item, $"[{count}]", null, maxDepth - 1));
                }
                
                count++;
            }
            
            return result;
        }
        
        /// <summary>
        /// Apply masking pattern to a string
        /// </summary>
        private static string ApplyMask(string value, MaskInLogsAttribute maskAttr)
        {
            if (string.IsNullOrEmpty(value))
                return value;
                
            if (!maskAttr.ShowFirstChars && !maskAttr.ShowLastChars)
                return maskAttr.MaskingPattern;
                
            var sb = new StringBuilder();
            
            if (maskAttr.ShowFirstChars && value.Length > maskAttr.FirstCharsCount)
            {
                sb.Append(value.Substring(0, Math.Min(maskAttr.FirstCharsCount, value.Length)));
            }
            
            sb.Append(maskAttr.MaskingPattern);
            
            if (maskAttr.ShowLastChars && value.Length > maskAttr.LastCharsCount)
            {
                int startIndex = Math.Max(0, value.Length - maskAttr.LastCharsCount);
                sb.Append(value.Substring(startIndex));
            }
            
            return sb.ToString();
        }
        
        /// <summary>
        /// Formats a list of changes for logging
        /// </summary>
        public static string FormatChanges(List<ChangeRecord> changes)
        {
            if (changes == null || changes.Count == 0)
                return "No changes detected";
                
            var sb = new StringBuilder();
            sb.AppendLine($"Changes detected ({changes.Count}):");
            
            foreach (var change in changes)
            {
                sb.AppendLine($"  {change.PropertyPath}: '{change.OldValue}' -> '{change.NewValue}'");
            }
            
            return sb.ToString();
        }
    }
}