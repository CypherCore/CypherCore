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

    public class SQLQueryHolderTask<R> : ISqlOperation
    {

        public SQLQueryHolderTask(SQLQueryHolder<R> holder)
        {
            QueryResults = holder;
        }

        public bool Execute<T>(MySqlBase<T> mySqlBase)
        {
            if (QueryResults == null)
                return false;

            // execute all queries in the holder and pass the results
            foreach (var pair in QueryResults.m_queries)
                QueryResults.SetResult(pair.Key, mySqlBase.Query(pair.Value));

            return true;
        }

        public SQLQueryHolder<R> QueryResults { get; private set; }
    }

    public class SQLQueryHolderCallback<R> : ISqlCallback
    {
        SQLQueryHolderTask<R> _future;
        Action<SQLQueryHolder<R>> _callback;
        public SQLQueryHolderCallback(SQLQueryHolderTask<R> future)
        {
            _future = future;
        }

        public void AfterComplete(Action<SQLQueryHolder<R>> callback)
        {
            _callback = callback;
        }

        public virtual void QueryExecuted(bool success)
        {
            if (success && _future.QueryResults != null)
                _callback(_future.QueryResults);

            _callback = null;
        }

        public bool InvokeIfReady()
        {
            return _callback == null;
        }
    }
}
