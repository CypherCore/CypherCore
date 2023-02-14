// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Framework.Database
{
    public class QueryCallback : ISqlCallback
    {

        public QueryCallback(PreparedStatementTask task, Action<ISqlOperation, Action<bool>> queueAction)
        {
            _awaitingTask = task;
            _queueAction = queueAction;
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
            _callbacks.Enqueue(callback);
            return this;
        }

        public void SetNextQuery(QueryCallback next)
        {
            _next = next;
        }

        public void QueryProcessed(bool success)
        {
            if (success)
            {
                // queue to invoke on main thread
                while (_callbacks.Count > 0)
                {
                    if (_callbacks.TryDequeue(out var cb) && cb != null)
                        if (_awaitingTask.Result != null)
                            cb(this, _awaitingTask.Result);
                }

                if (_callbacks.Count == 0 && _next != null) // if we processed everything call next.
                {
                    _queueAction(_next._awaitingTask, _next.QueryProcessed);
                    _next = null;
                }
            }
            else
            {   // if we fail, clear the queue, dont invoke.
                _next = null;
                _callbacks.Clear();
            }
        }

        public bool InvokeIfReady()
        {
            return _callbacks.Count > 0 && _next == null;
        }

        QueryCallback _next;
        PreparedStatementTask _awaitingTask;
        ConcurrentQueue<Action<QueryCallback, SQLResult>> _callbacks = new();

        Action<ISqlOperation, Action<bool>> _queueAction;
    }

}
