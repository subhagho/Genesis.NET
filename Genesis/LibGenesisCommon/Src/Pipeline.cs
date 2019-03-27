using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Linq.Dynamic.Core.CustomTypeProviders;
using System.Diagnostics.Contracts;
using LibZConfig.Common.Utils;

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

    public class Pipeline<T> : Processor<T>
    {
        protected List<Processor<T>> processors = new List<Processor<T>>();
        protected Dictionary<Processor<T>, LambdaExpression> predicates = new Dictionary<Processor<T>, LambdaExpression>();

        public void Add(Processor<T> processor, string condition)
        {
            Contract.Requires(processor != null);
            if (!String.IsNullOrWhiteSpace(condition))
            {
               
            }
        }
        public ProcessResponse<T> Execute(T data)
        {
            LogUtils.Debug("Running Process Pipeline:", data);
            ProcessResponse<T> response = new ProcessResponse<T>();
            response.Data = data;
            if (processors.Count > 0)
            {
                try
                {
                    foreach (Processor<T> processor in processors)
                    {
                        if (predicates.ContainsKey(processor))
                        {
                            LambdaExpression expression = predicates[processor];
                            object value = expression.Compile().DynamicInvoke(data);
                            if (value != null)
                            {
                                if (value.GetType() == typeof(Boolean))
                                {
                                    bool ret = (bool)value;
                                    if (!ret)
                                    {
                                        continue;
                                    }
                                }
                            }
                        }
                        response = processor.Execute(data);
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
                    response.SetError(e);
                    if (e.GetType() == typeof(ProcessUnhandledException))
                    {
                        response.State = EProcessResponse.UnhandledError;
                    }
                }
            }
            return response;
        }
    }
}
