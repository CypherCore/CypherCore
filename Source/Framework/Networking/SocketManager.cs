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

using System;
using System.Net.Sockets;

namespace Framework.Networking
{
    public class SocketManager<TSocketType> where TSocketType : ISocket
    {
        public virtual bool StartNetwork(string bindIp, int port, int threadCount = 1)
        {
            Cypher.Assert(threadCount > 0);

            Acceptor = new AsyncAcceptor();
            if (!Acceptor.Start(bindIp, port))
            {
                Log.outError(LogFilter.Network, "StartNetwork failed to Start AsyncAcceptor");
                return false;
            }

            _threadCount = threadCount;
            _threads = new NetworkThread<TSocketType>[GetNetworkThreadCount()];

            for (int i = 0; i < _threadCount; ++i)
            {
                _threads[i] = new NetworkThread<TSocketType>();
                _threads[i].Start();
            }

            Acceptor.AsyncAcceptSocket(OnSocketOpen);

            return true;
        }

        public virtual void StopNetwork()
        {
            Acceptor.Close();

            if (_threadCount != 0)
                for (int i = 0; i < _threadCount; ++i)
                    _threads[i].Stop();

            Wait();

            Acceptor = null;
            _threads = null;
            _threadCount = 0;
        }

        void Wait()
        {
            if (_threadCount != 0)
                for (int i = 0; i < _threadCount; ++i)
                    _threads[i].Wait();
        }

        public virtual void OnSocketOpen(Socket sock)
        {
            try
            {
                TSocketType newSocket = (TSocketType)Activator.CreateInstance(typeof(TSocketType), sock);
                newSocket.Start();

                _threads[SelectThreadWithMinConnections()].AddSocket(newSocket);
            }
            catch (Exception err)
            {
                Log.outException(err);
            }
        }

        public int GetNetworkThreadCount() { return _threadCount; }

        uint SelectThreadWithMinConnections()
        {
            uint min = 0;

            for (uint i = 1; i < _threadCount; ++i)
                if (_threads[i].GetConnectionCount() < _threads[min].GetConnectionCount())
                    min = i;

            return min;
        }

        public AsyncAcceptor Acceptor;
        NetworkThread<TSocketType>[] _threads;
        int _threadCount;
    }
}
