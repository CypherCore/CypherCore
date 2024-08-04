// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Framework.Networking
{
    public interface ISocket
    {
        void Start();
        bool Update();
        bool IsOpen();
        void CloseSocket();
    }

    public delegate Task SocketReadCallback(byte[] data, int receivedLength);

    public abstract class SocketBase : ISocket, IDisposable
    {
        Socket _socket;
        Stream _stream;
        IPEndPoint _remoteEndPoint;
        byte[] _receiveBuffer;

        protected SocketBase(Socket socket, bool useSSL = false)
        {
            _socket = socket;
            _remoteEndPoint = (IPEndPoint)_socket.RemoteEndPoint;
            _receiveBuffer = new byte[ushort.MaxValue];

            if (useSSL)
                _stream = new SslStream(new NetworkStream(socket), false);
            else
                _stream = new NetworkStream(socket);
        }

        public virtual void Dispose()
        {
            _receiveBuffer = null;
            _stream.Dispose();
        }

        public abstract void Start();

        public virtual bool Update()
        {
            return IsOpen();
        }

        public IPAddress GetRemoteIpAddress()
        {
            return _remoteEndPoint.Address;
        }

        public IPEndPoint GetRemoteIpEndPoint()
        {
            return _remoteEndPoint;
        }

        public async void AsyncReadWithCallback(SocketReadCallback callback)
        {
            if (!IsOpen())
                return;

            try
            {
                var result = await _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length);
                if (result == 0)
                {
                    CloseSocket();
                    return;
                }

                await callback(_receiveBuffer, result);
            }
            catch (Exception ex)
            {
                Log.outDebug(LogFilter.Network, ex.Message);
            }
        }

        public async Task AsyncRead()
        {
            if (!IsOpen())
                return;

            try
            {
                var result = await _stream.ReadAsync(_receiveBuffer, 0, _receiveBuffer.Length);
                if (result == 0)
                {
                    CloseSocket();
                    return;
                }

                ReadHandler(_receiveBuffer, result);
            }
            catch (Exception ex)
            {
                Log.outDebug(LogFilter.Network, ex.Message);
            }
        }

        public async Task AsyncHandshake(X509Certificate2 certificate)
        {
            try
            {
                await (_stream as SslStream).AuthenticateAsServerAsync(certificate, false, SslProtocols.Tls12, false);
            }
            catch (Exception ex)
            {
                await HandshakeHandler(ex);
                return;
            }

            await HandshakeHandler();
        }

        public virtual Task HandshakeHandler(Exception exception = null) { return null; }

        public abstract void ReadHandler(byte[] data, int receivedLength);

        public async Task AsyncWrite(byte[] data)
        {
            if (!IsOpen())
                return;

            try
            {
                await _stream.WriteAsync(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
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
