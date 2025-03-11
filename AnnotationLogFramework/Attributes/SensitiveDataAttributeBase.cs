using System;

namespace AnnotationLogger
{
    /// <summary>
    /// Base attribute for marking sensitive data that requires special handling in logs
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field, 
        AllowMultiple = false, Inherited = true)]
    public abstract class SensitiveDataAttributeBase : Attribute
    {
        /// <summary>
        /// Gets the reason why this data is considered sensitive
        /// </summary>
        public string Reason { get; }

        protected SensitiveDataAttributeBase(string reason = null)
        {
            Reason = reason;
        }
    }

    /// <summary>
    /// Indicates that a property or parameter should be excluded completely from logs
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field, 
        AllowMultiple = false, Inherited = true)]
    public class ExcludeFromLogsAttribute : SensitiveDataAttributeBase
    {
        public ExcludeFromLogsAttribute(string reason = null) : base(reason) { }
    }

    /// <summary>
    /// Indicates that a property or parameter should be masked in logs
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field, 
        AllowMultiple = false, Inherited = true)]
    public class MaskInLogsAttribute : SensitiveDataAttributeBase
    {
        /// <summary>
        /// Gets or sets the masking pattern to use
        /// </summary>
        public string MaskingPattern { get; set; } = "***";
        
        /// <summary>
        /// When true, shows first few characters of the value
        /// </summary>
        public bool ShowFirstChars { get; set; } = false;
        
        /// <summary>
        /// Number of characters to show at beginning (if ShowFirstChars is true)
        /// </summary>
        public int FirstCharsCount { get; set; } = 2;
        
        /// <summary>
        /// When true, shows last few characters of the value
        /// </summary>
        public bool ShowLastChars { get; set; } = false;
        
        /// <summary>
        /// Number of characters to show at end (if ShowLastChars is true)
        /// </summary>
        public int LastCharsCount { get; set; } = 2;

        public MaskInLogsAttribute(string reason = null) : base(reason) { }
    }
    
    /// <summary>
    /// Indicates that a property or parameter should have its contents redacted but maintain structure
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field, 
        AllowMultiple = false, Inherited = true)]
    public class RedactContentsAttribute : SensitiveDataAttributeBase
    {
        /// <summary>
        /// The replacement text to use for redacted values
        /// </summary>
        public string ReplacementText { get; set; } = "[REDACTED]";
        
        public RedactContentsAttribute(string reason = null) : base(reason) { }
    }
}