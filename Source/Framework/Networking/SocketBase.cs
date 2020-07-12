/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using System.Threading.Tasks;

namespace Framework.Networking
{
    public interface ISocket
    {
        void Accept();
        bool Update();
        bool IsOpen();
        void CloseSocket();
    }

    public abstract class SocketBase : ISocket, IDisposable
    {
        Socket _socket;
        IPEndPoint _remoteIPEndPoint;
        byte[] _receiveBuffer;

        protected SocketBase(Socket socket)
        {
            _socket = socket;
            _remoteIPEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            _receiveBuffer = new byte[ushort.MaxValue];
        }

        public virtual void Dispose()
        {
            _receiveBuffer = null;
            _socket.Dispose();
        }

        public abstract void Accept();

        public virtual bool Update()
        {
            return IsOpen();
        }

        public IPEndPoint GetRemoteIpAddress()
        {
            return _remoteIPEndPoint;
        }

        public async Task AsyncRead()
        {
            if (!IsOpen())
                return;

            try
            {
                using (var socketEventargs = new SocketAsyncEventArgs())
                {
                    socketEventargs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                    socketEventargs.Completed += async (sender, args) => await ProcessReadAsync(args);
                    socketEventargs.SocketFlags = SocketFlags.None;
                    socketEventargs.RemoteEndPoint = _socket.RemoteEndPoint;

                    if (!_socket.ReceiveAsync(socketEventargs))
                        await ProcessReadAsync(socketEventargs);
                }
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }

        public async Task AsyncReadWithCallback(SocketReadCallback callback)
        {
            if (!IsOpen())
                return;

            try
            {
                using (var socketEventargs = new SocketAsyncEventArgs())
                {
                    socketEventargs.SetBuffer(_receiveBuffer, 0, _receiveBuffer.Length);
                    socketEventargs.Completed += (sender, args) => callback(args);
                    socketEventargs.UserToken = _socket;
                    socketEventargs.SocketFlags = SocketFlags.None;
                    if (!_socket.ReceiveAsync(socketEventargs))
                        await callback(socketEventargs);
                }
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }

        async Task ProcessReadAsync(SocketAsyncEventArgs args)
        {
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

            byte[] data = new byte[args.BytesTransferred];
            Buffer.BlockCopy(_receiveBuffer, 0, data, 0, args.BytesTransferred);
            await ReadHandler(data, args.BytesTransferred);
        }

        public abstract Task ReadHandler(byte[] data, int bytesTransferred);

        public async void AsyncWrite(byte[] data)
        {
            if (!IsOpen())
                return;

            await _socket.SendAsync(data, SocketFlags.None);
        }

        public void CloseSocket()
        {
            if (_socket == null)
                return;

            try
            {
                _socket.Shutdown(SocketShutdown.Both);
                _socket.Close();
            }
            catch (Exception ex)
            {
                Log.outDebug(LogFilter.Network, $"WorldSocket.CloseSocket: {GetRemoteIpAddress()} errored when shutting down socket: {ex.Message}");
            }

            OnClose();
        }

        public virtual void OnClose() { Dispose(); }

        public bool IsOpen() { return _socket.Connected; }

        public void SetNoDelay(bool enable)
        {
            _socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, enable);
        }

        public delegate Task SocketReadCallback(SocketAsyncEventArgs args);
    }
}
