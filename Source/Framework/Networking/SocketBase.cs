/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using System.Net;
using System.Net.Sockets;

namespace Framework.Networking
{
    public abstract class SocketBase : ISocket, IDisposable
    {
        public SocketBase(Socket socket)
        {
            _socket = socket;
            _remoteAddress = ((IPEndPoint)_socket.RemoteEndPoint).Address;
            _remotePort = (ushort)((IPEndPoint)_socket.RemoteEndPoint).Port;
            _receiveBuffer = new byte[ushort.MaxValue];
        }

        public virtual void Dispose() { }

        public abstract void Start();

        public virtual bool Update()
        {
            return IsOpen();
        }

        public IPAddress GetRemoteIpAddress()
        {
            return _remoteAddress;
        }

        public ushort GetRemotePort()
        {
            return _remotePort;
        }

        public void AsyncRead()
        {
            if (!IsOpen())
                return;

            try
            {
                var socketEventargs = new SocketAsyncEventArgs();

                socketEventargs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);

                socketEventargs.Completed += ReadHandlerInternal;
                socketEventargs.UserToken = _socket;
                socketEventargs.SocketFlags = SocketFlags.None;

                _socket.ReceiveAsync(socketEventargs);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }

        public delegate void SocketReadCallback(object sender, SocketAsyncEventArgs args);
        public void AsyncReadWithCallback(SocketReadCallback callback)
        {
            if (!IsOpen())
                return;

            try
            {
                var socketEventargs = new SocketAsyncEventArgs();

                socketEventargs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);

                socketEventargs.Completed += new EventHandler<SocketAsyncEventArgs>(callback);
                socketEventargs.UserToken = _socket;
                socketEventargs.SocketFlags = SocketFlags.None;

                _socket.ReceiveAsync(socketEventargs);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }

        void ReadHandlerInternal(object sender, SocketAsyncEventArgs args)
        {
            args.Completed -= ReadHandlerInternal;
            if (args.SocketError != SocketError.Success)
            {
                CloseSocket();
                return;
            }

            if (args.BytesTransferred == 0)
            {
                CloseSocket();
                return;
            }

            ReadHandler(args.BytesTransferred);
        }

        public abstract void ReadHandler(int transferredBytes);

        public void AsyncWrite(byte[] data)
        {
            if (!IsOpen())
                return;

            var socketEventargs = new SocketAsyncEventArgs();
            socketEventargs.SetBuffer(data, 0, data.Length);
            socketEventargs.Completed += WriteHandlerInternal;
            socketEventargs.RemoteEndPoint = _socket.RemoteEndPoint;
            socketEventargs.SocketFlags = SocketFlags.None;

            _socket.SendAsync(socketEventargs);
        }

        void WriteHandlerInternal(object sender, SocketAsyncEventArgs args)
        {
            args.Completed -= WriteHandlerInternal;
        }

        public void CloseSocket()
        {
            try
            {
                _closed = true;
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            catch (Exception ex)
            {
                Log.outDebug(LogFilter.Network, "WorldSocket.CloseSocket: {0} errored when shutting down socket: {1}", GetRemoteIpAddress().ToString(), ex.Message);
            }

            OnClose();
        }

        public void SetNoDelay(bool enable)
        {
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
        }

        public virtual void OnClose() { }

        public bool IsOpen() { return !_closed; }

        public byte[] GetReceiveBuffer()
        {
            return _receiveBuffer;
        }

        Socket _socket;
        byte[] _receiveBuffer;

        volatile bool _closed;

        IPAddress _remoteAddress;
        ushort _remotePort;
    }

    public interface ISocket
    {
        void Start();
        bool Update();
        bool IsOpen();
        void CloseSocket();
    }
}
