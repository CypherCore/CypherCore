// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class QueryCallback : ISqlCallback
    {
        public QueryCallback(Task<SQLResult> result)
        {
            _result = result;
        }

        public QueryCallback WithCallback(Action<SQLResult> callback)
        {
            return WithChainingCallback((queryCallback, result) => callback(result));
        }

        public QueryCallback WithCallback<T>(Action<T, SQLResult> callback, T obj)
        {
            return WithChainingCallback((queryCallback, result) => callback(obj, result));
        }

        public QueryCallback WithChainingCallback(Action<QueryCallback, SQLResult> callback)
        {
            _callbacks.Enqueue(new QueryCallbackData(callback));
            return this;
        }

        public void SetNextQuery(QueryCallback next)
        {
            _result = next._result;
        }

        public bool InvokeIfReady()
        {
            QueryCallbackData callback = _callbacks.Peek();

            while (true)
            {
                if (_result != null && _result.Wait(0))
                {
                    Task<SQLResult> f = _result;
                    Action<QueryCallback, SQLResult> cb = callback._result;
                    _result = null;

                    cb(this, f.Result);

                    _callbacks.Dequeue();
                    bool hasNext = _result != null;
                    if (_callbacks.Count == 0)
                    {
                        Cypher.Assert(!hasNext);
                        return true;
                    }

                    // abort chain
                    if (!hasNext)
                        return true;

                    callback = _callbacks.Peek();
                }
                else
                    return false;
            }
        }

        Task<SQLResult> _result;
        Queue<QueryCallbackData> _callbacks = new();
    }

    struct QueryCallbackData
    {
        public QueryCallbackData(Action<QueryCallback, SQLResult> callback)
        {
            _result = callback;
        }

        public void Clear()
        {
            _result = null;
        }

        public Action<QueryCallback, SQLResult> _result;
    }
}
