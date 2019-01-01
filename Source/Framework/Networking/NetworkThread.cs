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

using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Framework.Networking
{
    public class NetworkThread<TSocketType> where TSocketType : ISocket
    {
        public void Stop()
        {
            _stopped = true;
        }

        public bool Start()
        {
            if (_thread != null)
                return false;

            _thread = new Thread(Run);
            _thread.Start();
            return true;
        }

        public void Wait()
        {
            _thread.Join();
            _thread = null;
        }

        public int GetConnectionCount()
        {
            return _connections;
        }

        public virtual void AddSocket(TSocketType sock)
        {
            Interlocked.Increment(ref _connections);
            _newSockets.Add(sock);
            SocketAdded(sock);
        }

        protected virtual void SocketAdded(TSocketType sock) { }
        protected virtual void SocketRemoved(TSocketType sock) { }

        void AddNewSockets()
        {
            if (_newSockets.Empty())
                return;

            foreach (var socket in _newSockets.ToArray())
            {
                if (!socket.IsOpen())
                {
                    SocketRemoved(socket);

                    Interlocked.Decrement(ref _connections);
                }
                else
                    _Sockets.Add(socket);
            }

            _newSockets.Clear();
        }

        void Run()
        {
            Log.outDebug(LogFilter.Network, "Network Thread Starting");

            int sleepTime = 10;
            while (!_stopped)
            {
                Thread.Sleep(sleepTime);

                uint tickStart = Time.GetMSTime();

                AddNewSockets();

                for (var i =0; i < _Sockets.Count; ++i)
                {
                    TSocketType socket = _Sockets[i];
                    if (!socket.Update())
                    {
                        if (socket.IsOpen())
                            socket.CloseSocket();

                        SocketRemoved(socket);

                        --_connections;
                        _Sockets.Remove(socket);
                    }
                }

                uint diff = Time.GetMSTimeDiffToNow(tickStart);
                sleepTime = (int)(diff > 10 ? 0 : 10 - diff);
            }

            Log.outDebug(LogFilter.Misc, "Network Thread exits");
            _newSockets.Clear();
            _Sockets.Clear();
        }

        int _connections;
        volatile bool _stopped;

        Thread _thread;

        List<TSocketType> _Sockets = new List<TSocketType>();
        List<TSocketType> _newSockets = new List<TSocketType>();
    }
}
