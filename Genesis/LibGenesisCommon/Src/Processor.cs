using System;
using LibZConfig.Common;

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
        StopWithError
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
        /// Get the error state for this type.
        /// </summary>
        /// <returns></returns>
        public override EProcessResponse GetErrorState()
        {
            return EProcessResponse.FatalError;
        }
    }

    /// <summary>
    /// Interface to be implmenented by processors.
    /// </summary>
    /// <typeparam name="T">Entity type this processor handles.</typeparam>
    public interface Processor<T>
    {
        /// <summary>
        /// Execute the processor on the specified entity data.
        /// </summary>
        /// <param name="data">Entity data input</param>
        /// <returns>Response</returns>
        ProcessResponse<T> Execute(T data);
    }
}
