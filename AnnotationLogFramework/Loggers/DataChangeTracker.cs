using System;
using System.Collections.Generic;

namespace AnnotationLogger
{
    /// <summary>
    /// Utility class for tracking data state changes across method calls
    /// </summary>
    public class DataChangeTracker<T> where T : class
    {
        private T _originalState;
        private string _entityId;
        private string _operationType;
        private Dictionary<string, object> _additionalContext;
        
        /// <summary>
        /// Creates a new DataChangeTracker for the specified entity
        /// </summary>
        /// <param name="entity">The entity to track</param>
        /// <param name="entityId">Optional identifier for the entity</param>
        /// <param name="operationType">The type of operation being performed (Create, Update, Delete, etc.)</param>
        public DataChangeTracker(T entity, string entityId = null, string operationType = "Update")
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
                
            // Create a deep copy of the entity if possible, otherwise store reference
            _originalState = entity;
            _entityId = entityId;
            _operationType = operationType;
            _additionalContext = new Dictionary<string, object>();
        }
        
        /// <summary>
        /// Adds additional context information to be included with the change log
        /// </summary>
        /// <param name="key">Context key</param>
        /// <param name="value">Context value</param>
        /// <returns>This tracker instance for method chaining</returns>
        public DataChangeTracker<T> WithContext(string key, object value)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));
                
            _additionalContext[key] = value;
            return this;
        }
        
        /// <summary>
        /// Logs changes between the original state and the current state
        /// </summary>
        /// <param name="currentState">Current state of the entity</param>
        /// <param name="level">Log level to use</param>
        /// <returns>List of detected changes</returns>
        public List<ChangeRecord> LogChanges(T currentState, LogLevel level = LogLevel.Info)
        {
            if (currentState == null)
                throw new ArgumentNullException(nameof(currentState));
                
            // Log changes between original and current state
            LogManager.LogDataChanges(
                _originalState, 
                currentState, 
                _operationType, 
                _entityId, 
                level, 
                _additionalContext);
                
            // Return the changes for possible further processing
            return ObjectComparer.CompareObjects(_originalState, currentState);
        }
        
        /// <summary>
        /// Updates the tracked original state with a new state
        /// </summary>
        /// <param name="newOriginalState">New state to track as the original</param>
        public void UpdateOriginalState(T newOriginalState)
        {
            if (newOriginalState == null)
                throw new ArgumentNullException(nameof(newOriginalState));
                
            _originalState = newOriginalState;
        }
    }
    
    /// <summary>
    /// Static factory for DataChangeTracker instances
    /// </summary>
    public static class DataChangeTracker
    {
        /// <summary>
        /// Creates a new DataChangeTracker for the specified entity
        /// </summary>
        public static DataChangeTracker<T> Track<T>(T entity, string entityId = null, string operationType = "Update") 
            where T : class
        {
            return new DataChangeTracker<T>(entity, entityId, operationType);
        }
    }
}