using System;
using System.Collections.Generic;
using LibZConfig.Common;
using LibGenesisCommon.Common;

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

    public abstract class Processor<T>
    {
        public string Name { get; set; }
        public abstract ProcessResponse<T> Execute(T data);
    }

    /// <summary>
    /// Interface to be implmenented by processors.
    /// </summary>
    /// <typeparam name="T">Entity type this processor handles.</typeparam>
    public abstract class BasicProcessor<T> : Processor<T>
    {
        public Func<T, bool> Condition { get; set; }

        public virtual bool MatchCondition(T data)
        {
            if (Condition != null)
            {
                return Condition.Invoke(data);
            }
            return true;
        }

        public override ProcessResponse<T> Execute(T data)
        {
            if (data != null)
            {
                if (!MatchCondition(data))
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
        public bool FilterResult { get; set; }
        public Func<T, bool> Condition { get; set; }

        public virtual bool MatchCondition(T data)
        {
            if (Condition != null)
            {
                return Condition.Invoke(data);
            }
            return true;
        }

        public override ProcessResponse<List<T>> Execute(List<T> data)
        {
            if (data != null && data.Count > 0)
            {
                List<T> included = new List<T>();
                List<T> excluded = new List<T>();
                foreach (T value in data)
                {
                    if (MatchCondition(value))
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
                        if (FilterResult)
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
                        if (FilterResult)
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
                    if (FilterResult)
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
