// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Framework.Configuration;

namespace Framework.Networking.Http
{
    public class SslSocket<Derived> : BaseSocket<Derived>
    {
        X509Certificate2 _certificate;

        public SslSocket(Socket socket) : base(socket)
        {
            _certificate = new X509Certificate2(ConfigMgr.GetDefaultValue("CertificatesFile", "./BNetServer.pfx"));
        }

        public async override void Start()
        {
            await AsyncHandshake(_certificate);
        }

        public async override Task HandshakeHandler(Exception exception = null)
        {
            if (exception != null)
            {
                Log.outError(LogFilter.Http, $"{GetClientInfo()} SSL Handshake failed {exception.Message}");
                CloseSocket();
                return;
            }

            await AsyncRead();
        }
    }
}
