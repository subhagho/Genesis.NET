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
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using LibZConfig.Common.Utils;
using LibGenesisCommon.Common;

namespace LibGenesisCommon.Process
{
    /// <summary>
    /// Exception class to be used to propogate process errors.
    /// </summary>
    public class ProcessException : Exception
    {
        private static readonly string __PREFIX = "Process Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public ProcessException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public ProcessException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public ProcessException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    /// <summary>
    /// Exception class to be used to propogate process errors.
    /// </summary>
    public class ProcessUnhandledException : Exception
    {
        private static readonly string __PREFIX = "Unhandled Exception : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public ProcessUnhandledException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public ProcessUnhandledException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public ProcessUnhandledException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public interface Pipeline<T>
    {
        List<Processor<T>> GetProcessors();

        void Add(Processor<T> processor, string condition, string prefix);
    }

    public class BasicPipeline<T> : BasicProcessor<T>, Pipeline<T>
    {
        protected List<Processor<T>> processors = new List<Processor<T>>();
        protected Dictionary<string, Func<T, bool>> conditions = new Dictionary<string, Func<T, bool>>();

        public void Add(Processor<T> processor, string condition, string prefix)
        {
            Contract.Requires(processor != null);
            processors.Add(processor);
            if (!String.IsNullOrWhiteSpace(condition))
            {
                Func<T, bool> func = ConditionParser.Parse<T>(condition, prefix);
                if (func != null)
                {
                    conditions[processor.Name] = func;
                }
            }
        }

        public List<Processor<T>> GetProcessors()
        {
            return processors;
        }

        protected override ProcessResponse<T> ExecuteProcess(T data, Context context, ProcessResponse<T> response)
        {
            LogUtils.Debug("Running Process Pipeline:", data);
            if (processors.Count > 0)
            {
                try
                {
                    foreach (Processor<T> processor in processors)
                    {
                        Func<T, bool> func = null;
                        if (conditions.ContainsKey(processor.Name))
                        {
                            func = conditions[processor.Name];
                        }
                        response = processor.Execute(response.Data, context, func);
                        Conditions.NotNull(response);
                        if (response.Data == null)
                        {
                            response.State = EProcessResponse.NullData;
                            break;
                        }
                        if (response.State == EProcessResponse.FatalError)
                        {
                            Exception ex = response.GetError();
                            if (ex == null)
                            {
                                ex = new ProcessException("Processor raised error.");
                            }
                            throw new ProcessException(ex);
                        }
                        else if (response.State == EProcessResponse.ContinueWithError)
                        {
                            Exception ex = response.GetError();
                            if (ex != null)
                            {
                                LogUtils.Error(String.Format("Continuing with error: [type={0}]", processor.GetType().FullName));
                                LogUtils.Error(ex);
                            }
                            else
                            {
                                LogUtils.Error(String.Format("Continuing with error: [type={0}]", processor.GetType().FullName));
                            }
                        }
                        else if (response.State == EProcessResponse.StopWithOk)
                        {
                            LogUtils.Warn(String.Format("Terminating further processing: [type={0}][reponse={1}]", processor.GetType().FullName, response.State.ToString()));
                            break;
                        }
                        else if (response.State == EProcessResponse.StopWithError)
                        {
                            Exception ex = response.GetError();
                            if (ex != null)
                            {
                                LogUtils.Error(String.Format("Stopping with error: [type={0}]", processor.GetType().FullName));
                                LogUtils.Error(ex);
                            }
                            else
                            {
                                LogUtils.Error(String.Format("Stopping with error: [type={0}]", processor.GetType().FullName));
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogUtils.Error(e);
                    response.SetError(EProcessResponse.FatalError, e);
                    if (e.GetType() == typeof(ProcessUnhandledException))
                    {
                        response.State = EProcessResponse.UnhandledError;
                    }
                }
            }
            if (ReflectionUtils.IsNull(data))
            {
                if (response.State != EProcessResponse.FatalError)
                {
                    response.State = EProcessResponse.NullData;
                }
            }
            else
            {
                if (response.State != EProcessResponse.FatalError)
                {
                    response.State = EProcessResponse.OK;
                }
            }
            return response;
        }
    }

    public class CollectionPipeline<T> : CollectionProcessor<T>, Pipeline<List<T>>
    {
        protected List<Processor<List<T>>> processors = new List<Processor<List<T>>>();
        protected Dictionary<string, Func<T, bool>> conditions = new Dictionary<string, Func<T, bool>>();

        public void Add(Processor<List<T>> processor, string condition, string prefix)
        {
            Contract.Requires(processor != null);
            processors.Add(processor);
            if (!String.IsNullOrWhiteSpace(condition))
            {
                Func<T, bool> func = ConditionParser.Parse<T>(condition, prefix);
                if (func != null)
                {
                    conditions[processor.Name] = func;
                }
            }
        }

        public List<Processor<List<T>>> GetProcessors()
        {
            return processors;
        }

        protected override ProcessResponse<List<T>> ExecuteProcess(List<T> data, Context context, ProcessResponse<List<T>> response)
        {
            LogUtils.Debug("Running Process Pipeline:", data);
            if (processors.Count > 0)
            {
                try
                {
                    foreach (Processor<List<T>> processor in processors)
                    {
                        Func<T, bool> func = null;
                        if (conditions.ContainsKey(processor.Name))
                        {
                            func = conditions[processor.Name];
                        }
                        response = processor.Execute(response.Data, context, func);
                        Conditions.NotNull(response);
                        if (response.Data == null || response.Data.Count <= 0)
                        {
                            response.State = EProcessResponse.NullData;
                            break;
                        }
                        if (response.State == EProcessResponse.FatalError)
                        {
                            Exception ex = response.GetError();
                            if (ex == null)
                            {
                                ex = new ProcessException("Processor raised error.");
                            }
                            throw new ProcessException(ex);
                        }
                        else if (response.State == EProcessResponse.ContinueWithError)
                        {
                            Exception ex = response.GetError();
                            if (ex != null)
                            {
                                LogUtils.Error(String.Format("Continuing with error: [type={0}]", processor.GetType().FullName));
                                LogUtils.Error(ex);
                            }
                            else
                            {
                                LogUtils.Error(String.Format("Continuing with error: [type={0}]", processor.GetType().FullName));
                            }
                        }
                        else if (response.State == EProcessResponse.StopWithOk)
                        {
                            LogUtils.Warn(String.Format("Terminating further processing: [type={0}][reponse={1}]", processor.GetType().FullName, response.State.ToString()));
                            break;
                        }
                        else if (response.State == EProcessResponse.StopWithError)
                        {
                            Exception ex = response.GetError();
                            if (ex != null)
                            {
                                LogUtils.Error(String.Format("Stopping with error: [type={0}]", processor.GetType().FullName));
                                LogUtils.Error(ex);
                            }
                            else
                            {
                                LogUtils.Error(String.Format("Stopping with error: [type={0}]", processor.GetType().FullName));
                            }
                            break;
                        }
                    }
                }
                catch (Exception e)
                {
                    LogUtils.Error(e);
                    response.SetError(EProcessResponse.FatalError, e);
                    if (e.GetType() == typeof(ProcessUnhandledException))
                    {
                        response.State = EProcessResponse.UnhandledError;
                    }
                }
            }
            if (ReflectionUtils.IsNull(data))
            {
                if (response.State != EProcessResponse.FatalError)
                {
                    response.State = EProcessResponse.NullData;
                }
            }
            else
            {
                if (response.State != EProcessResponse.FatalError)
                {
                    response.State = EProcessResponse.OK;
                }
            }
            return response;
        }
    }
}
