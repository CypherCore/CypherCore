// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class SQLQueryHolder<T>
    {
        public Dictionary<T, PreparedStatement> m_queries = new();
        Dictionary<T, SQLResult> _results = new();

        public void SetQuery(T index, string sql, params object[] args)
        {
            SetQuery(index, new PreparedStatement(string.Format(sql, args)));
        }

        public void SetQuery(T index, PreparedStatement stmt)
        {
            m_queries[index] = stmt;
        }

        public void SetResult(T index, SQLResult result)
        {
            _results[index] = result;
        }

        public SQLResult GetResult(T index)
        {
            if (!_results.ContainsKey(index))
                return new SQLResult();

            return _results[index];
        }
    }

    class SQLQueryHolderTask<R> : ISqlOperation
    {
        SQLQueryHolder<R> m_holder;
        TaskCompletionSource<SQLQueryHolder<R>> m_result;

        public SQLQueryHolderTask(SQLQueryHolder<R> holder)
        {
            m_holder = holder;
            m_result = new TaskCompletionSource<SQLQueryHolder<R>>();
        }

        public bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            if (m_holder == null)
                return false;

            // execute all queries in the holder and pass the results
            foreach (var pair in m_holder.m_queries)
                m_holder.SetResult(pair.Key, mySqlBase.Query(pair.Value));

            return m_result.TrySetResult(m_holder);
        }

        public Task<SQLQueryHolder<R>> GetFuture() { return m_result.Task; }
    }

    public class SQLQueryHolderCallback<R> : ISqlCallback
    {
        Task<SQLQueryHolder<R>> m_future;
        Action<SQLQueryHolder<R>> m_callback;

        public SQLQueryHolderCallback(Task<SQLQueryHolder<R>> future)
        {
            m_future = future;
        }

        public void AfterComplete(Action<SQLQueryHolder<R>> callback)
        {
            m_callback = callback;
        }

        public bool InvokeIfReady()
        {
            if (m_future != null && m_future.Wait(0))
            {
                m_callback(m_future.Result);
                return true;
            }

            return false;
        }
    }
}
