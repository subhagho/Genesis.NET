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
using LibZConfig.Common;
using LibZConfig.Common.Config.Attributes;

namespace LibGenesisCommon.Process
{
    /// <summary>
    /// Enum for process response.
    /// </summary>
    public enum EProcessResponse
    {
        /// <summary>
        /// Not defined.
        /// </summary>
        None,
        /// <summary>
        /// Success
        /// </summary>
        OK,
        /// <summary>
        /// Fatal Error: Terminate
        /// </summary>
        FatalError,
        /// <summary>
        /// Unknown/Unhandled Error: Terminate
        /// </summary>
        UnhandledError,
        /// <summary>
        /// Step raised error but OK to continue
        /// </summary>
        ContinueWithError,
        /// <summary>
        /// Step finished successfully, but stop further processing.
        /// </summary>
        StopWithOk,
        /// <summary>
        /// Step raised error, stop further processing.
        /// </summary>
        StopWithError,
        /// <summary>
        /// Step was not executed (due to condition constraint)
        /// </summary>
        NotExecuted,
        /// <summary>
        /// Processing resulted in NULL data response.
        /// </summary>
        NullData
    }

    /// <summary>
    /// Response object to be returned by each processor.
    /// </summary>
    /// <typeparam name="T">Type of the response entity</typeparam>
    public class ProcessResponse<T> : AbstractState<EProcessResponse>
    {
        /// <summary>
        /// Response Entity
        /// </summary>
        public T Data { get; set; }

        /// <summary>
        /// Default Empty constructor
        /// </summary>
        public ProcessResponse()
        {
            State = EProcessResponse.None;
        }

        /// <summary>
        /// Get the error state(s) for this type.
        /// </summary>
        /// <returns></returns>
        public override EProcessResponse[] GetErrorStates()
        {
            return new EProcessResponse[] { EProcessResponse.FatalError, EProcessResponse.StopWithError, EProcessResponse.UnhandledError, EProcessResponse.ContinueWithError };
        }
    }

    /// <summary>
    /// Base class for defining Data processors
    /// </summary>
    /// <typeparam name="T">Data Type</typeparam>
    public abstract class Processor<T>
    {
        /// <summary>
        /// Name of the processor (must be unique in the defined scope)
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Abstract method to execute the processor on the data element.
        /// </summary>
        /// <param name="data">Data element</param>
        /// <param name="condition">Condition clause to check prior to processing</param>
        /// <returns>Process response</returns>
        public abstract ProcessResponse<T> Execute(T data, object condition);
    }

    /// <summary>
    /// Abstract base class to be implmenented by processors processing on single entities.
    /// </summary>
    /// <typeparam name="T">Entity type this processor handles.</typeparam>
    public abstract class BasicProcessor<T> : Processor<T>
    {
        /// <summary>
        /// Check if this processor should execute based on the passed condition.
        /// </summary>
        /// <param name="data">Data element</param>
        /// <param name="condition">Condition (Func<T, bool>)</param>
        /// <returns>Should execute?</returns>
        public virtual bool MatchCondition(T data, object condition)
        {
            if (condition != null)
            {
                if (typeof(MulticastDelegate).IsAssignableFrom(condition.GetType().BaseType))
                {
                    Func<T, bool> func = (Func<T, bool>)condition;
                    return func.Invoke(data);
                }
                else
                {
                    throw new ProcessException(String.Format("Invalid Condition: [type={0}]", condition.GetType().FullName));
                }
            }

            return true;
        }

        /// <summary>
        /// Method to execute the processor on the data element.
        /// </summary>
        /// <param name="data">Data element</param>
        /// <param name="condition">Condition clause to check prior to processing</param>
        /// <returns>Process response</returns>
        public override ProcessResponse<T> Execute(T data, object condition)
        {
            if (data != null)
            {
                if (!MatchCondition(data, condition))
                {
                    ProcessResponse<T> response = new ProcessResponse<T>();
                    response.State = EProcessResponse.NotExecuted;
                    response.Data = data;
                }
                else
                {
                    return ExecuteProcess(data);
                }
            }
            return null;
        }

        /// <summary>
        /// Execute the processor on the specified entity data.
        /// </summary>
        /// <param name="data">Entity data input</param>
        /// <returns>Response</returns>
        protected abstract ProcessResponse<T> ExecuteProcess(T data);
    }

    public abstract class CollectionProcessor<T> : Processor<List<T>>
    {
        /// <summary>
        /// Property specifying if the processor should exclude all values that don't 
        /// match the passed condition from the result set.
        /// </summary>
        [ConfigAttribute(Name = "filterResults")]
        public bool FilterResults { get; set; }

        /// <summary>
        /// Check if this processor should execute based on the passed condition.
        /// </summary>
        /// <param name="data">Data element</param>
        /// <param name="condition">Condition (Func<T, bool>)</param>
        /// <returns>Should execute?</returns>
        public virtual bool MatchCondition(T data, object condition)
        {
            if (condition != null)
            {
                if (typeof(MulticastDelegate).IsAssignableFrom(condition.GetType().BaseType))
                {
                    Func<T, bool> func = (Func<T, bool>)condition;
                    return func.Invoke(data);
                }
                else
                {
                    throw new ProcessException(String.Format("Invalid Condition: [type={0}]", condition.GetType().FullName));
                }
            }
            
            return true;
        }

        /// <summary>
        /// Method to execute the processor on the data element.
        /// </summary>
        /// <param name="data">Data element</param>
        /// <param name="condition">Condition clause to check prior to processing</param>
        /// <returns>Process response</returns>
        public override ProcessResponse<List<T>> Execute(List<T> data, object condition)
        {
            if (data != null && data.Count > 0)
            {
                List<T> included = new List<T>();
                List<T> excluded = new List<T>();
                foreach (T value in data)
                {
                    if (MatchCondition(value, condition))
                    {
                        included.Add(value);
                    }
                    else
                    {
                        excluded.Add(value);
                    }
                }
                if (included.Count > 0)
                {
                    ProcessResponse<List<T>> response = ExecuteProcess(data);
                    if (response == null)
                    {
                        throw new ProcessException("Null response returned.");
                    }
                    if (response.Data == null || response.Data.Count <= 0)
                    {
                        if (FilterResults)
                        {
                            response.Data = null;
                            response.State = EProcessResponse.NullData;
                            return response;
                        }
                        else
                        {
                            response.Data = excluded;
                            response.State = EProcessResponse.NullData;
                            return response;
                        }
                    }
                    else
                    {
                        if (FilterResults)
                        {
                            return response;
                        }
                        else
                        {
                            if (excluded.Count > 0)
                            {
                                foreach (T value in excluded)
                                {
                                    response.Data.Add(value);
                                }
                            }
                            return response;
                        }
                    }
                }
                else
                {
                    ProcessResponse<List<T>> response = new ProcessResponse<List<T>>();
                    if (FilterResults)
                    {
                        response.Data = null;
                        response.State = EProcessResponse.NullData;
                        return response;
                    }
                    else
                    {
                        response.Data = excluded;
                        response.State = EProcessResponse.NullData;
                        return response;
                    }
                }

            }
            return null;
        }

        /// <summary>
        /// Execute the processor on the specified entity data.
        /// </summary>
        /// <param name="data">Entity data input</param>
        /// <returns>Response</returns>
        protected abstract ProcessResponse<List<T>> ExecuteProcess(List<T> data);
    }
}
