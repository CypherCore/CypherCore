// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Networking;
using System.Net.Sockets;

namespace Game.Networking
{
    public class WorldSocketManager : SocketManager<WorldSocket>
    {
        public override bool StartNetwork(string bindIp, int port, int threadCount)
        {
            _tcpNoDelay = ConfigMgr.GetDefaultValue("Network.TcpNodelay", true);

            Log.outDebug(LogFilter.Misc, "Max allowed socket connections {0}", ushort.MaxValue);

            // -1 means use default
            _socketSendBufferSize = ConfigMgr.GetDefaultValue("Network.OutKBuff", -1);

            if (!base.StartNetwork(bindIp, port, threadCount))
                return false;

            _instanceAcceptor = new AsyncAcceptor();
            if (!_instanceAcceptor.Start(bindIp, WorldConfig.GetIntValue(WorldCfg.PortInstance)))
            {
                Log.outError(LogFilter.Network, "StartNetwork failed to start instance AsyncAcceptor");
                return false;
            }

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
                    sock.SendBufferSize = _socketSendBufferSize;

                // Set TCP_NODELAY.
                sock.NoDelay = _tcpNoDelay;
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
        bool _tcpNoDelay;
    }
}
