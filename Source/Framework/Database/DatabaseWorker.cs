// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Framework.Database
{
    public interface ISqlOperation
    {
        bool Execute<T>(MySqlBase<T> mySqlBase);
    }

    class DatabaseWorker<T>
    {
        Thread _workerThread;
        volatile bool _cancelationToken;
        AutoResetEvent _resetEvent = new AutoResetEvent(false);
        ConcurrentQueue<(ISqlOperation, Action<bool>)> _queue = new();
        MySqlBase<T> _mySqlBase;

        public DatabaseWorker(MySqlBase<T> mySqlBase)
        {
            _mySqlBase = mySqlBase;
            _cancelationToken = false;
            _workerThread = new Thread(WorkerThread);
            _workerThread.Start();
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
                        operation.Item2(success);
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
