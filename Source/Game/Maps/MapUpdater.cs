// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Threading;
using Framework.Threading;

namespace Game.Maps
{
    public class MapUpdater
    {
        private readonly object _lock = new();
        private readonly ProducerConsumerQueue<MapUpdateRequest> _queue = new();

        private readonly Thread[] _workerThreads;
        private volatile bool _cancelationToken;
        private int _pendingRequests;

        public MapUpdater(int numThreads)
        {
            _workerThreads = new Thread[numThreads];

            for (var i = 0; i < numThreads; ++i)
            {
                _workerThreads[i] = new Thread(WorkerThread);
                _workerThreads[i].Start();
            }
        }

        public void Deactivate()
        {
            _cancelationToken = true;

            Wait();

            _queue.Cancel();

            foreach (var thread in _workerThreads)
                thread.Join();
        }

        public void Wait()
        {
            lock (_lock)
            {
                while (_pendingRequests > 0)
                    Monitor.Wait(_lock);
            }
        }

        public void ScheduleUpdate(Map map, uint diff)
        {
            lock (_lock)
            {
                ++_pendingRequests;

                _queue.Push(new MapUpdateRequest(map, this, diff));
            }
        }

        public void UpdateFinished()
        {
            lock (_lock)
            {
                --_pendingRequests;

                Monitor.PulseAll(_lock);
            }
        }

        private void WorkerThread()
        {
            while (true)
            {
                MapUpdateRequest request;

                _queue.WaitAndPop(out request);

                if (_cancelationToken)
                    return;

                request.Call();
            }
        }
    }

    public class MapUpdateRequest
    {
        private readonly uint _diff;
        private readonly Map _map;
        private readonly MapUpdater _updater;

        public MapUpdateRequest(Map m, uint d)
        {
            _map = m;
            _diff = d;
        }

        public MapUpdateRequest(Map m, MapUpdater u, uint d)
        {
            _map = m;
            _updater = u;
            _diff = d;
        }

        public void Call()
        {
            _map.Update(_diff);
            _updater.UpdateFinished();
        }
    }
}