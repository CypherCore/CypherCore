/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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

using System;
using System.Collections.Generic;
using System.Threading;

namespace Game.Maps
{
    public class MapUpdater : IDisposable
    {
        public MapUpdater(int numThreads)
        {
            _queue = new Queue<MapUpdateRequest>();

            autoResetEvent = new AutoResetEvent[numThreads];
            for (var i = 0; i < numThreads; ++i)
            {
                autoResetEvent[i] = new AutoResetEvent(false);
                ThreadPool.QueueUserWorkItem(new WaitCallback(OnEnqueue), autoResetEvent[i]);
            }
        }

        public void Enqueue(Map map, uint diff)
        {
            lock (_syncLock)
            {
                _queue.Enqueue(new MapUpdateRequest(map, diff));
                Monitor.PulseAll(_syncLock);
            }
        }

        protected void OnEnqueue(object state)
        {
            while (true)
            {
                lock (_syncLock)
                {
                    if (_queue.Count == 0)
                    {
                        ((AutoResetEvent)state).Set();
                        Monitor.Wait(_syncLock);
                    }

                    if (_queue.Count > 0)
                    {
                        _queue.Dequeue().Call();
                    }
                }
            }
        }

        public void Wait()
        {
            WaitHandle.WaitAll(autoResetEvent);
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                Monitor.PulseAll(_syncLock);
            }
        }

        private Queue<MapUpdateRequest> _queue;
        private object _syncLock = new object();
        private AutoResetEvent[] autoResetEvent;
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

        public uint GetId()
        {
            return m_map.GetId();
        }
    }
}