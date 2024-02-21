// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Networking;
using System.Net.Sockets;

namespace BNetServer.Networking
{
    public class SessionManager : SocketManager<Session>
    {
        public static SessionManager Instance { get; } = new SessionManager();

        public override bool StartNetwork(string bindIp, int port, int threadCount = 1)
        {
            if (!base.StartNetwork(bindIp, port, threadCount))
                return false;

            Acceptor.AsyncAcceptSocket(OnSocketAccept);
            return true;
        }

        static void OnSocketAccept(Socket sock)
        {
            Global.SessionMgr.OnSocketOpen(sock);
        }
    }
}