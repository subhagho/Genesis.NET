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
    public class DataException : Exception
    {
        private static readonly string __PREFIX = "Data Processing Error : {0}";

        /// <summary>
        /// Constructor with error message.
        /// </summary>
        /// <param name="mesg">Error message</param>
        public DataException(string mesg) : base(String.Format(__PREFIX, mesg))
        {

        }

        /// <summary>
        /// Constructor with error message and cause.
        /// </summary>
        /// <param name="mesg">Error message</param>
        /// <param name="cause">Cause</param>
        public DataException(string mesg, Exception cause) : base(String.Format(__PREFIX, mesg), cause)
        {

        }

        /// <summary>
        /// Constructor with cause.
        /// </summary>
        /// <param name="exception">Cause</param>
        public DataException(Exception exception) : base(String.Format(__PREFIX, exception.Message), exception)
        {

        }
    }

    public abstract class Entity<K>
    {
        public abstract K GetKey();
    }

    public interface DataSource<E, K> where E : Entity<K>
    {
        E Find(K key);
        List<E> Search(string condition);
    }

    public interface DataSink<E, K> where E : Entity<K>
    {
        E Create(E entity);

        List<E> Create(List<E> entities);

        E Update(E entity);

        List<E> Update(List<E> entities);

        void Delete(E entity);

        void Delete(List<E> entities);
    }

    public abstract class DataLoader<E, K> : BasicPipeline<E> where E : Entity<K>
    {
        protected DataSource<E, K> source;
        protected Context context;

        public E Read(K key)
        {
            Contract.Requires(source != null);
            Contract.Requires(!ReflectionUtils.IsNull(key));

            E data = source.Find(key);
            if (data != null)
            {
                return ProcessData(data);
            }
            return null;
        }

        public List<E> Fetch(string condition)
        {
            Contract.Requires(source != null);
            List<E> data = source.Search(condition);
            if (data != null && data.Count > 0)
            {
                List<E> result = new List<E>();
                foreach (E elem in data)
                {
                    E de = ProcessData(elem);
                    if (de != null)
                    {
                        result.Add(de);
                    }
                }
                if (result.Count > 0)
                {
                    return result;
                }
            }
            return null;
        }

        private E ProcessData(E data)
        {
            if (data != null)
            {
                ProcessResponse<E> response = Execute(data, context, null);
                Conditions.NotNull(response);
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    return response.Data;
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        return response.Data;
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
            return null;
        }
    }

    public abstract class CollectionDataLoader<E, K> : CollectionPipeline<E> where E : Entity<K>
    {
        protected DataSource<E, K> source;
        protected Context context;

        public List<E> Fetch(string condition)
        {
            Contract.Requires(source != null);
            List<E> data = source.Search(condition);
            if (data != null && data.Count > 0)
            {
                ProcessResponse<List<E>> response = Execute(data, context, null);
                Conditions.NotNull(response);
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    return response.Data;
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}][key={1}]", typeof(E).FullName, condition);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        return response.Data;
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}][key={1}]", typeof(E).FullName, condition);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
            return null;
        }
    }

    public abstract class DataWriter<E, K> : BasicPipeline<E> where E : Entity<K>
    {
        protected DataSink<E, K> sink;
        protected Context context;

        public E Create(E data)
        {
            Contract.Requires(sink != null);
            Contract.Requires(!ReflectionUtils.IsNull(data));

            ProcessResponse<E> response = Execute(data, context, null);
            Conditions.NotNull(response);
            if (!ReflectionUtils.IsNull(response.Data))
            {
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    return sink.Create(response.Data);
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}][key={1}]", typeof(E).FullName, data.GetKey());
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        return sink.Create(response.Data);
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}][key={1}]", typeof(E).FullName, data.GetKey());
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
            return null;
        }

        public E Update(E data)
        {
            Contract.Requires(sink != null);
            Contract.Requires(!ReflectionUtils.IsNull(data));

            ProcessResponse<E> response = Execute(data, context, null);
            Conditions.NotNull(response);
            if (!ReflectionUtils.IsNull(response.Data))
            {
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    return sink.Update(response.Data);
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}][key={1}]", typeof(E).FullName, data.GetKey());
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        return sink.Update(response.Data);
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}][key={1}]", typeof(E).FullName, data.GetKey());
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
            return null;
        }

        public void Delete(E data)
        {
            Contract.Requires(sink != null);
            Contract.Requires(!ReflectionUtils.IsNull(data));

            ProcessResponse<E> response = Execute(data, context, null);
            Conditions.NotNull(response);
            if (!ReflectionUtils.IsNull(response.Data))
            {
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    sink.Delete(response.Data);
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}][key={1}]", typeof(E).FullName, data.GetKey());
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        sink.Delete(response.Data);
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}][key={1}]", typeof(E).FullName, data.GetKey());
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
        }
    }

    public abstract class CollectionDataWriter<E, K> : CollectionPipeline<E> where E : Entity<K>
    {
        protected DataSink<E, K> sink;
        protected Context context;

        public List<E> Create(List<E> data)
        {
            Contract.Requires(sink != null);
            Contract.Requires(!ReflectionUtils.IsNull(data));

            ProcessResponse<List<E>> response = Execute(data, context, null);
            Conditions.NotNull(response);
            if (!ReflectionUtils.IsNull(response.Data))
            {
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    return sink.Create(response.Data);
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        return sink.Create(response.Data);
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
            return null;
        }

        public List<E> Update(List<E> data)
        {
            Contract.Requires(sink != null);
            Contract.Requires(!ReflectionUtils.IsNull(data));

            ProcessResponse<List<E>> response = Execute(data, context, null);
            Conditions.NotNull(response);
            if (!ReflectionUtils.IsNull(response.Data))
            {
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    return sink.Update(response.Data);
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        return sink.Update(response.Data);
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
            return null;
        }

        public void Delete(List<E> data)
        {
            Contract.Requires(sink != null);
            Contract.Requires(!ReflectionUtils.IsNull(data));

            ProcessResponse<List<E>> response = Execute(data, context, null);
            Conditions.NotNull(response);
            if (!ReflectionUtils.IsNull(response.Data))
            {
                if (response.State == EProcessResponse.OK || response.State == EProcessResponse.StopWithOk)
                {
                    sink.Delete(response.Data);
                }
                else
                {
                    if (response.State == EProcessResponse.FatalError)
                    {
                        string mesg = String.Format("Error fetching entity : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                    else if (response.State == EProcessResponse.ContinueWithError || response.State == EProcessResponse.StopWithError)
                    {
                        Exception ex = response.GetError();
                        if (ex != null)
                            LogUtils.Error(ex);
                        sink.Delete(response.Data);
                    }
                    else
                    {
                        string mesg = String.Format("Unhandled State : [type={0}]", typeof(E).FullName);
                        Exception ex = response.GetError();
                        if (ex != null)
                            throw new DataException(mesg, ex);
                        else
                            throw new DataException(mesg);
                    }
                }
            }
        }
    }
}
