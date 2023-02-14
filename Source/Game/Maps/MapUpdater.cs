// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Concurrent;
using System.Threading;

namespace Game.Maps
{
    public class MapUpdater
    {
        AutoResetEvent _mapUpdateComplete = new AutoResetEvent(false);
        AutoResetEvent _resetEvent = new AutoResetEvent(false);
        ConcurrentQueue<MapUpdateRequest> _queue = new();

        Thread[] _workerThreads;
        volatile bool _cancelationToken;

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

            _queue.Clear();

            _mapUpdateComplete.Set();
            foreach (var thread in _workerThreads)
                thread.Join(1000); // only time we do this is on shutdown. Give a timeout to gracefully join
        }

        public void Wait()
        {
            while (_queue.Count > 0)
                _mapUpdateComplete.WaitOne();
        }

        public void ScheduleUpdate(Map map, uint diff)
        {
            _queue.Enqueue(new MapUpdateRequest(map, diff));
            _resetEvent.Set();
        }

        void WorkerThread()
        {
            while (true)
            {
                _resetEvent.WaitOne(500);

                while (_queue.Count > 0)
                {
                    if (!_queue.TryDequeue(out MapUpdateRequest request) || request == null)
                        continue;

                    if (_cancelationToken)
                        return;

                    request.Call();
                }

                _mapUpdateComplete.Set();
            }
        }
    }

    public class MapUpdateRequest
    {
        Map m_map;
        uint m_diff;

        public MapUpdateRequest(Map m, uint d)
        {
            m_map = m;
            m_diff = d;
        }

        public void Call()
        {
            m_map.Update(m_diff);
        }
    }
}