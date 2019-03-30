using System;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace LibGenesisCommon.Common
{
    /// <summary>
    /// Context handle to be passed as a parameter.
    /// </summary>
    public class Context
    {
        private Dictionary<string, object> context = new Dictionary<string, object>();

        /// <summary>
        /// Add a context entry.
        /// </summary>
        /// <param name="key">Context Key</param>
        /// <param name="value">Value</param>
        /// <returns>Self</returns>
        public Context AddContext(string key, object value)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(key));
            context[key] = value;
            return this;
        }

        /// <summary>
        /// Get a context entry.
        /// </summary>
        /// <param name="key">Context Key</param>
        /// <returns>Value</returns>
        public object GetContext(string key)
        {
            if (context.ContainsKey(key))
            {
                return context[key];
            }
            return null;
        }
    }
}
