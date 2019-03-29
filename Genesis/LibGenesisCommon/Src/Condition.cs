#region copyright
//
// Licensed to the Apache Software Foundation (ASF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The ASF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.
//
// Copyright (c) 2019
// Date: 2019-3-28
// Project: LibGenesisCommon
// Subho Ghosh (subho dot ghosh at outlook.com)
//
//
#endregion
using DynamicExpresso;
using System;
using System.Diagnostics.Contracts;

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

    /// <summary>
    /// Utility class to parse the condition string as Ling compatible delegate.
    /// </summary>
    public static class ConditionParser
    {
        /// <summary>
        /// Parse the specified condition string as a delegate
        /// </summary>
        /// <typeparam name="T">Type</typeparam>
        /// <param name="condition">Condition string</param>
        /// <param name="prefix">Object prefix.</param>
        /// <returns>Delegate function.</returns>
        public static Func<T, bool> Parse<T>(string condition, string prefix)
        {
            Contract.Requires(!String.IsNullOrWhiteSpace(condition));
            Contract.Requires(!String.IsNullOrWhiteSpace(prefix));

            var interpreter = new Interpreter();
            return interpreter.ParseAsDelegate<Func<T, bool>>(condition, prefix);
        }
    }
}
