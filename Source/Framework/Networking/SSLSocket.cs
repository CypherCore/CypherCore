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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;

namespace Framework.Networking
{
    public abstract class SSLSocket : ISocket
    {
        protected SSLSocket(Socket socket)
        {
            _socket = socket;
            _remoteAddress = ((IPEndPoint)_socket.RemoteEndPoint).Address;
            _remotePort = (ushort)((IPEndPoint)_socket.RemoteEndPoint).Port;
            _receiveBuffer = new byte[ushort.MaxValue];

            _stream = new SslStream(new NetworkStream(socket), false);
        }

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
                _stream.BeginRead(_receiveBuffer, 0, _receiveBuffer.Length, ReadHandlerInternal, _stream);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
        }

        void ReadHandlerInternal(IAsyncResult result)
        {
            int bytes = 0;
            try
            {
                bytes = _stream.EndRead(result);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }

            ReadHandler(bytes);
        }

        public abstract void ReadHandler(int transferredBytes);

        public void AsyncHandshake(X509Certificate2 certificate)
        {
            try
            {
                _stream.AuthenticateAsServer(certificate, false, System.Security.Authentication.SslProtocols.Tls, false);
            }
            catch(Exception ex)
            {
                Log.outException(ex);
                CloseSocket();
                return;
            }
            AsyncRead();
        }

        public void AsyncWrite(byte[] data)
        {
            if (!IsOpen())
                return;

            try
            {
                _stream.Write(data, 0, data.Length);
            }
            catch (Exception ex)
            {
                Log.outException(ex);
            }
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
        internal SslStream _stream;
        byte[] _receiveBuffer;

        volatile bool _closed;

        IPAddress _remoteAddress;
        ushort _remotePort;
    }
}
