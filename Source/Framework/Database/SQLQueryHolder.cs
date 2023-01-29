// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class SQLQueryHolder<T>
    {
        public Dictionary<T, PreparedStatement> _queries = new();
        private readonly Dictionary<T, SQLResult> _results = new();

        public void SetQuery(T index, string sql, params object[] args)
        {
            SetQuery(index, new PreparedStatement(string.Format(sql, args)));
        }

        public void SetQuery(T index, PreparedStatement stmt)
        {
            _queries[index] = stmt;
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

    internal class SQLQueryHolderTask<R> : ISqlOperation
    {
        private readonly SQLQueryHolder<R> _holder;
        private readonly TaskCompletionSource<SQLQueryHolder<R>> _result;

        public SQLQueryHolderTask(SQLQueryHolder<R> holder)
        {
            _holder = holder;
            _result = new TaskCompletionSource<SQLQueryHolder<R>>();
        }

        public bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            if (_holder == null)
                return false;

            // execute all queries in the holder and pass the results
            foreach (var pair in _holder._queries)
                _holder.SetResult(pair.Key, mySqlBase.Query(pair.Value));

            return _result.TrySetResult(_holder);
        }

        public Task<SQLQueryHolder<R>> GetFuture()
        {
            return _result.Task;
        }
    }

    public class SQLQueryHolderCallback<R> : ISqlCallback
    {
        private Action<SQLQueryHolder<R>> _callback;
        private readonly Task<SQLQueryHolder<R>> _future;

        public SQLQueryHolderCallback(Task<SQLQueryHolder<R>> future)
        {
            _future = future;
        }

        public bool InvokeIfReady()
        {
            if (_future != null &&
                _future.Wait(0))
            {
                _callback(_future.Result);

                return true;
            }

            return false;
        }

        public void AfterComplete(Action<SQLQueryHolder<R>> callback)
        {
            _callback = callback;
        }
    }
}