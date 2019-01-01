/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Threading;
using System.Threading;

namespace Game.Maps
{
    public class MapUpdater
    {
        ProducerConsumerQueue<MapUpdateRequest> _queue = new ProducerConsumerQueue<MapUpdateRequest>();

        Thread[] _workerThreads;
        volatile bool _cancelationToken;

        object _lock = new object();
        int _pendingRequests;

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

        void WorkerThread()
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
        Map m_map;
        MapUpdater m_updater;
        uint m_diff;

        public MapUpdateRequest(Map m, uint d)
        {
            m_map = m;
            m_diff = d;
        }

        public MapUpdateRequest(Map m, MapUpdater u, uint d)
        {
            m_map = m;
            m_updater = u;
            m_diff = d;
        }

        public void Call()
        {
            m_map.Update(m_diff);
            m_updater.UpdateFinished();
        }
    }
}