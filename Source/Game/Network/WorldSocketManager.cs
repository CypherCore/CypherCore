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

using Framework.Configuration;
using Framework.Constants;
using Framework.Networking;
using System.Net.Sockets;

namespace Game.Network
{
    public class WorldSocketManager : SocketManager<WorldSocket>
    {
        public override bool StartNetwork(string bindIp, int port, int threadCount)
        {
            _tcpNoDelay = ConfigMgr.GetDefaultValue("Network.TcpNodelay", true);

            Log.outDebug(LogFilter.Misc, "Max allowed socket connections {0}", ushort.MaxValue);

            // -1 means use default
            _socketSendBufferSize = ConfigMgr.GetDefaultValue("Network.OutKBuff", -1);

            m_SockOutUBuff = ConfigMgr.GetDefaultValue("Network.OutUBuff", 65536);

            if (m_SockOutUBuff <= 0)
            {
                Log.outError(LogFilter.Network, "Network.OutUBuff is wrong in your config file");
                return false;
            }

            if (!base.StartNetwork(bindIp, port, threadCount))
                return false;

            _instanceAcceptor = new AsyncAcceptor(bindIp, WorldConfig.GetIntValue(WorldCfg.PortInstance));
            _instanceAcceptor.AsyncAcceptSocket(OnSocketOpen);

            return true;
        }

        public override void StopNetwork()
        {
            _instanceAcceptor.Close();

            base.StopNetwork();

            _instanceAcceptor = null;
        }

        public override void OnSocketOpen(Socket sock)
        {
            // set some options here
            try
            {
                if (_socketSendBufferSize >= 0)
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendBuffer, _socketSendBufferSize);

                // Set TCP_NODELAY.
                if (_tcpNoDelay)
                    sock.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.NoDelay, true);
            }
            catch (SocketException ex)
            {
                Log.outException(ex);
                return;
            }

            base.OnSocketOpen(sock);
        }

        AsyncAcceptor _instanceAcceptor;
        int _socketSendBufferSize;
        int m_SockOutUBuff;
        bool _tcpNoDelay;
    }
}
