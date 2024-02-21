// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace Framework.Networking
{
    public interface ISocket
    {
        void Start();
        bool Update();
        bool IsOpen();
        void CloseSocket();
    }

    public abstract class SocketBase : ISocket, IDisposable
    {
        Socket _socket;
        IPEndPoint _remoteIPEndPoint;

        SocketAsyncEventArgs receiveSocketAsyncEventArgsWithCallback;
        SocketAsyncEventArgs receiveSocketAsyncEventArgs;

        public delegate void SocketReadCallback(SocketAsyncEventArgs args);

        protected SocketBase(Socket socket)
        {
            _socket = socket;
            _remoteIPEndPoint = (IPEndPoint)_socket.RemoteEndPoint;

            receiveSocketAsyncEventArgsWithCallback = new SocketAsyncEventArgs();
            receiveSocketAsyncEventArgsWithCallback.SetBuffer(new byte[0x4000], 0, 0x4000);

            receiveSocketAsyncEventArgs = new SocketAsyncEventArgs();
            receiveSocketAsyncEventArgs.SetBuffer(new byte[0x4000], 0, 0x4000);
            receiveSocketAsyncEventArgs.Completed += (sender, args) => ProcessReadAsync(args);
        }

        public virtual void Dispose()
        {
            _socket.Dispose();
        }

        public virtual void Start() { }

        public virtual bool Update()
        {
            return IsOpen();
        }

        public IPEndPoint GetRemoteIpAddress()
        {
            return _remoteIPEndPoint;
        }

        public void AsyncReadWithCallback(SocketReadCallback callback)
        {
            if (!IsOpen())
                return;

            receiveSocketAsyncEventArgsWithCallback.Completed += (sender, args) => callback(args);
            receiveSocketAsyncEventArgsWithCallback.SetBuffer(0, 0x4000);
            if (!_socket.ReceiveAsync(receiveSocketAsyncEventArgsWithCallback))
                callback(receiveSocketAsyncEventArgsWithCallback);
        }

        public void AsyncRead()
        {
            if (!IsOpen())
                return;

            receiveSocketAsyncEventArgs.SetBuffer(0, 0x4000);
            if (!_socket.ReceiveAsync(receiveSocketAsyncEventArgs))
                ProcessReadAsync(receiveSocketAsyncEventArgs);
        }

        void ProcessReadAsync(SocketAsyncEventArgs args)
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

            ReadHandler(args);
        }

        public abstract void ReadHandler(SocketAsyncEventArgs args);

        public void AsyncWrite(byte[] data)
        {
            if (!IsOpen())
                return;

            _socket.Send(data);
        }

        public void CloseSocket()
        {
            if (_socket == null || !_socket.Connected)
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
    }
}
