// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Net;
using System.Net.Sockets;

namespace Framework.Networking
{
    public delegate void SocketAcceptDelegate(Socket newSocket);

    public class AsyncAcceptor
    {
        TcpListener _listener;
        volatile bool _closed;

        public bool Start(string ip, int port)
        {
            IPAddress bindIP;
            if (!IPAddress.TryParse(ip, out bindIP))
            {
                Log.outError(LogFilter.Network, $"Server can't be started: Invalid IP-Address: {ip}");
                return false;
            }

            try
            {
                _listener = new TcpListener(bindIP, port);
                _listener.Start();
            }
            catch (SocketException ex)
            {
                Log.outException(ex);
                return false;
            }

            return true;
        }

        public async void AsyncAcceptSocket(SocketAcceptDelegate mgrHandler)
        {
            try
            {
                var _socket = await _listener.AcceptSocketAsync();
                if (_socket != null)
                {
                    mgrHandler(_socket);

                    if (!_closed)
                        AsyncAcceptSocket(mgrHandler);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Log.outException(ex);
            }
        }

        public async void AsyncAccept<T>() where T : ISocket
        {
            try
            {
                var socket = await _listener.AcceptSocketAsync();
                if (socket != null)
                {
                    T newSocket = (T)Activator.CreateInstance(typeof(T), socket);
                    newSocket.Accept();

                    if (!_closed)
                        AsyncAccept<T>();
                }
            }
            catch (ObjectDisposedException)
            { }
        }

        public void Close()
        {
            if (_closed)
                return;

            _closed = true;
        }
    }
}
