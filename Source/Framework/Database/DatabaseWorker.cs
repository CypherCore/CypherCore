// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Framework.Database
{
    public interface ISqlOperation
    {
        bool Execute<T>(MySqlBase<T> mySqlBase);
    }

    class DatabaseWorker<T>
    {
        volatile bool _cancelationToken;
        AutoResetEvent _resetEvent = new AutoResetEvent(false);
        ConcurrentQueue<(ISqlOperation, Action<bool>)> _queue = new();
        MySqlBase<T> _mySqlBase;

        public DatabaseWorker(MySqlBase<T> mySqlBase)
        {
            _mySqlBase = mySqlBase;
            _cancelationToken = false;
            Task.Run(WorkerThread);
        }

        void WorkerThread()
        {
            if (_queue == null)
                return;

            while (true)
            {
                _resetEvent.WaitOne(500);

                while (_queue.Count > 0)
                {
                    if (!_queue.TryDequeue(out (ISqlOperation, Action<bool>) operation) || operation.Item1 == null)
                        continue;

                    if (_cancelationToken)
                        return;

                    var success = operation.Item1.Execute(_mySqlBase);
                    
                    if (operation.Item2 != null)
                       Task.Run(() =>
                       {
                           try
                           {
                               operation.Item2(success);
                           }
                           catch(Exception ex)
                           {
                               Log.outException(ex, "DatabaseWorker.CallbackSuccessStatus");
                           }
                    });
                }
            }
        }

        public void QueueQuery(ISqlOperation operation, Action<bool> callback = null)
        {
            _queue.Enqueue((operation, callback));
            _resetEvent.Set();
        }
    }
}
