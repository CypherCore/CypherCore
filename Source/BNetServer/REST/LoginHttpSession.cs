// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.Networking.Http;
using System;
using System.Linq;
using System.Net.Sockets;

namespace BNetServer.REST
{
    public class LoginSessionState : SessionState
    {
        public BnetSRP6Base Srp;
    }

    public class LoginHttpSession : SslSocket<LoginHttpSession>
    {
        public static string SESSION_ID_COOKIE = "JSESSIONID";

        public LoginHttpSession(Socket socket) : base(socket) { }

        public override void Start()
        {
            string ip_address = GetRemoteIpAddress().ToString();
            Log.outTrace(LogFilter.Http, $"{GetClientInfo()} Accepted connection");

            // Verify that this IP is not in the ip_banned table
            DB.Login.Execute(LoginDatabase.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS));

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_IP_INFO);
            stmt.AddValue(0, ip_address);

            _queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(CheckIpCallback));
        }

        async void CheckIpCallback(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                bool banned = false;
                do
                {
                    if (result.Read<ulong>(0) != 0)
                        banned = true;

                } while (result.NextRow());

                if (banned)
                {
                    Log.outDebug(LogFilter.Http, $"{GetClientInfo()} tries to log in using banned IP!");
                    CloseSocket();
                    return;
                }
            }

            await AsyncHandshake(Global.LoginServiceMgr.GetCertificate());
        }

        public override RequestHandlerResult RequestHandler(RequestContext context)
        {
            return Global.LoginService.GetDispatcherService().HandleRequest(this, context);
        }

        public override SessionState ObtainSessionState(RequestContext context)
        {
            SessionState state = null;

            var cookieString = context.request.Cookie;
            if (!cookieString.IsEmpty())
            {
                var cookies = cookieString.Split(';', StringSplitOptions.RemoveEmptyEntries);
                int eq = 0;
                var sessionIdItr = cookies.FirstOrDefault(cookie =>
                {
                    string name = cookie;
                    eq = cookie.IndexOf('=');
                    if (eq != -1)
                        name = cookie.Substring(0, eq);

                    return name == SESSION_ID_COOKIE;
                });
                if (!sessionIdItr.IsEmpty())
                {
                    string value = sessionIdItr.Substring(eq + 1);
                    state = Global.LoginService.GetSessionService().FindAndRefreshSessionState(value, GetRemoteIpAddress());
                }
            }

            if (state == null)
            {
                state = Global.LoginService.CreateNewSessionState(GetRemoteIpAddress());

                string host = context.request.Host;
                int port = host.IndexOf(':');
                if (port != -1)
                    host = host.Remove(host.Length - port);

                context.response.Cookie = $"{SESSION_ID_COOKIE}={state.Id}; Path=/bnetserver; Domain={host}; Secure; HttpOnly; SameSite=None";
            }

            return state;
        }

        public LoginSessionState GetSessionState() { return _state as LoginSessionState; }
    }
}
