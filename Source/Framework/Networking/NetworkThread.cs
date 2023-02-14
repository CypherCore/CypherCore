// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Threading;

namespace Framework.Networking
{
    public class NetworkThread<TSocketType> where TSocketType : ISocket
    {
        int _connections;
        volatile bool _stopped;

        Thread _thread;

        List<TSocketType> _Sockets = new();
        List<TSocketType> _newSockets = new();

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

            int sleepTime = 1;
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
                sleepTime = (int)(diff > 1 ? 0 : 1 - diff);
            }

            Log.outDebug(LogFilter.Misc, "Network Thread exits");
            _newSockets.Clear();
            _Sockets.Clear();
        }
    }
}
