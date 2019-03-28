using DynamicExpresso;
using System;
using System.Dynamic;
using System.Linq.Expressions;

namespace LibGenesisCommon.Common
{
    /// <summary>
    /// Exception class to be used to propogate Condition processing errors.
    /// </summary>
    public class ConditionException : Exception
    {
        private static readonly string __PREFIX = "Process Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public ConditionException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public ConditionException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public ConditionException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public static class ConditionParser
    {
        public static Func<T, bool> Parse<T>(string condition, string prefix)
        {
            var interpreter = new Interpreter();
            return interpreter.ParseAsDelegate<Func<T, bool>>(condition, prefix);
        }
    }
}
