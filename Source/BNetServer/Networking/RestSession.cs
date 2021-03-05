// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using Framework.Networking;
using System.Net.Sockets;
using Framework.Web;
using Framework.Constants;
using Framework.Database;
using Framework.Configuration;
using Framework.Serialization;
using System.Security.Cryptography;

namespace BNetServer.Networking
{
    public class RestSession : SSLSocket
    {
        public RestSession(Socket socket) : base(socket) { }

        public override void Accept()
        {
            AsyncHandshake(Global.LoginServiceMgr.GetCertificate());
        }

        public async override void ReadHandler(byte[] data, int receivedLength)
        {
            if (receivedLength == 0)
            {
                CloseSocket();
                return;
            }

            var httpRequest = HttpHelper.ParseRequest(data, receivedLength);
            if (httpRequest == null)
                return;

            switch (httpRequest.Method)
            {
                case "GET":
                default:
                    SendResponse(HttpCode.Ok, Global.LoginServiceMgr.GetFormInput());
                    break;
                case "POST":
                    HandleLoginRequest(httpRequest);
                    return;
            }

            await AsyncRead();
        }

        public void HandleLoginRequest(HttpHeader request)
        {
            var loginForm = Json.CreateObject<LogonData>(request.Content);
            var loginResult = new LogonResult();
            if (loginForm == null)
            {
                loginResult.AuthenticationState = "LOGIN";
                loginResult.ErrorCode = "UNABLE_TO_DECODE";
                loginResult.ErrorMessage = "There was an internal error while connecting to Battle.net. Please try again later.";
                SendResponse(HttpCode.BadRequest, loginResult);
                return;
            }

            var login = "";
            var password = "";

            for (var i = 0; i < loginForm.Inputs.Count; ++i)
            {
                switch (loginForm.Inputs[i].Id)
                {
                    case "account_name":
                        login = loginForm.Inputs[i].Value;
                        break;
                    case "password":
                        password = loginForm.Inputs[i].Value;
                        break;
                }
            }

            var stmt = DB.Login.GetPreparedStatement(LoginStatements.SelBnetAuthentication);
            stmt.AddValue(0, login);

            var result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
            {
                var accountId = result.Read<uint>(0);
                var pass_hash = result.Read<string>(1);
                var failedLogins = result.Read<uint>(2);
                var loginTicket = result.Read<string>(3);
                var loginTicketExpiry = result.Read<uint>(4);
                var isBanned = result.Read<ulong>(5) != 0;

                if (CalculateShaPassHash(login.ToUpper(), password.ToUpper()) == pass_hash)
                {
                    if (loginTicket.IsEmpty() || loginTicketExpiry < Time.UnixTime)
                    {
                        var ticket = new byte[0].GenerateRandomKey(20);
                        loginTicket = "TC-" + ticket.ToHexString();
                    }

                    stmt = DB.Login.GetPreparedStatement(LoginStatements.UpdBnetAuthentication);
                    stmt.AddValue(0, loginTicket);
                    stmt.AddValue(1, Time.UnixTime + 3600);
                    stmt.AddValue(2, accountId);

                    DB.Login.Execute(stmt);
                    loginResult.LoginTicket = loginTicket;
                }
                else if (!isBanned)
                {
                    var maxWrongPassword = ConfigMgr.GetDefaultValue("WrongPass.MaxCount", 0u);

                    if (ConfigMgr.GetDefaultValue("WrongPass.Logging", false))
                        Log.outDebug(LogFilter.Network, $"[{request.Host}, Account {login}, Id {accountId}] Attempted to connect with wrong password!");

                    if (maxWrongPassword != 0)
                    {
                        var trans = new SQLTransaction();
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.UpdBnetFailedLogins);
                        stmt.AddValue(0, accountId);
                        trans.Append(stmt);

                        ++failedLogins;

                        Log.outDebug(LogFilter.Network, $"MaxWrongPass : {maxWrongPassword}, failed_login : {accountId}");

                        if (failedLogins >= maxWrongPassword)
                        {
                            var banType = ConfigMgr.GetDefaultValue("WrongPass.BanType", BanMode.IP);
                            var banTime = ConfigMgr.GetDefaultValue("WrongPass.BanTime", 600);

                            if (banType == BanMode.Account)
                            {
                                stmt = DB.Login.GetPreparedStatement(LoginStatements.InsBnetAccountAutoBanned);
                                stmt.AddValue(0, accountId);
                            }
                            else
                            {
                                stmt = DB.Login.GetPreparedStatement(LoginStatements.InsIpAutoBanned);
                                stmt.AddValue(0, request.Host);
                            }

                            stmt.AddValue(1, banTime);
                            trans.Append(stmt);

                            stmt = DB.Login.GetPreparedStatement(LoginStatements.UpdBnetResetFailedLogins);
                            stmt.AddValue(0, accountId);
                            trans.Append(stmt);
                        }

                        DB.Login.CommitTransaction(trans);
                    }
                }

                loginResult.AuthenticationState = "DONE";
                SendResponse(HttpCode.Ok, loginResult);
            }
            else
            {
                loginResult.AuthenticationState = "LOGIN";
                loginResult.ErrorCode = "UNABLE_TO_DECODE";
                loginResult.ErrorMessage = "There was an internal error while connecting to Battle.net. Please try again later.";
                SendResponse(HttpCode.BadRequest, loginResult);
            }
        }

        private async void SendResponse<T>(HttpCode code, T response)
        {
            await AsyncWrite(HttpHelper.CreateResponse(code, Json.CreateString(response)));
        }

        private string CalculateShaPassHash(string name, string password)
        {
            var sha256 = SHA256.Create();
            var email = sha256.ComputeHash(Encoding.UTF8.GetBytes(name));
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(email.ToHexString() + ":" + password)).ToHexString(true);
        }
    }
}
