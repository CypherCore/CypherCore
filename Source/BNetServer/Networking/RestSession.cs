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

using Framework.Configuration;
using Framework.Database;
using Framework.Networking;
using Framework.Rest;
using Framework.Serialization;
using Framework.Web;
using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;

namespace BNetServer.Networking
{
    public class RestSession : SSLSocket
    {
        public RestSession(Socket socket) : base(socket) { }

        public override void Start()
        {
            AsyncHandshake(Global.SessionMgr.GetCertificate());
        }

        public override void ReadHandler(int transferredBytes)
        {            
            var httpRequest = HttpHelper.ParseRequest(GetReceiveBuffer(), transferredBytes);
            if (httpRequest == null)
                return;

            switch (httpRequest.Method)
            {
                case "GET":
                default:
                    HandleConnectRequest(httpRequest);
                    break;
                case "POST":
                    HandleLoginRequest(httpRequest);
                    return;
            }

            AsyncRead();
        }

        public void HandleConnectRequest(HttpHeader request)
        {
            // Login form is the same for all clients...
            SendResponse(HttpCode.Ok, Global.SessionMgr.GetFormInput());
        }

        public void HandleLoginRequest(HttpHeader request)
        {
            LogonData loginForm = Json.CreateObject<LogonData>(request.Content);
            LogonResult loginResult = new LogonResult();
            if (loginForm == null)
            {
                loginResult.AuthenticationState = "LOGIN";
                loginResult.ErrorCode = "UNABLE_TO_DECODE";
                loginResult.ErrorMessage = "There was an internal error while connecting to Battle.net. Please try again later.";
                SendResponse(HttpCode.BadRequest, loginResult);
                return;
            }

            string login = "";
            string password = "";

            for (int i = 0; i < loginForm.Inputs.Count; ++i)
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

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_AUTHENTICATION);
            stmt.AddValue(0, login);

            SQLResult result = DB.Login.Query(stmt);
            if (!result.IsEmpty())
            {
                uint accountId = result.Read<uint>(0);
                string pass_hash = result.Read<string>(1);
                uint failedLogins = result.Read<uint>(2);
                string loginTicket = result.Read<string>(3);
                uint loginTicketExpiry = result.Read<uint>(4);
                bool isBanned = result.Read<ulong>(5) != 0;

                if (CalculateShaPassHash(login, password) == pass_hash)
                {
                    if (loginTicket.IsEmpty() || loginTicketExpiry < Time.UnixTime)
                    {
                        byte[] ticket = new byte[0].GenerateRandomKey(20);
                        loginTicket = "TC-" + ticket.ToHexString();
                    }

                    stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_AUTHENTICATION);
                    stmt.AddValue(0, loginTicket);
                    stmt.AddValue(1, Time.UnixTime + 3600);
                    stmt.AddValue(2, accountId);

                    DB.Login.Execute(stmt);
                    loginResult.LoginTicket = loginTicket;
                }
                else if (!isBanned)
                {
                    uint maxWrongPassword = ConfigMgr.GetDefaultValue("WrongPass.MaxCount", 0u);

                    if (ConfigMgr.GetDefaultValue("WrongPass.Logging", false))
                        Log.outDebug(LogFilter.Network, "[{0}, Account {1}, Id {2}] Attempted to connect with wrong password!", request.Host, login, accountId);

                    if (maxWrongPassword != 0)
                    {
                        SQLTransaction trans = new SQLTransaction();
                        stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_FAILED_LOGINS);
                        stmt.AddValue(0, accountId);
                        trans.Append(stmt);

                        ++failedLogins;

                        Log.outDebug(LogFilter.Network, "MaxWrongPass : {0}, failed_login : {1}", maxWrongPassword, accountId);

                        if (failedLogins >= maxWrongPassword)
                        {
                            BanMode banType = ConfigMgr.GetDefaultValue("WrongPass.BanType", BanMode.Ip);
                            int banTime = ConfigMgr.GetDefaultValue("WrongPass.BanTime", 600);

                            if (banType == BanMode.Account)
                            {
                                stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ACCOUNT_AUTO_BANNED);
                                stmt.AddValue(0, accountId);
                            }
                            else
                            {
                                stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_IP_AUTO_BANNED);
                                stmt.AddValue(0, request.Host);
                            }

                            stmt.AddValue(1, banTime);
                            trans.Append(stmt);

                            stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_RESET_FAILED_LOGINS);
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

        void SendResponse<T>(HttpCode code, T response)
        {
           AsyncWrite(HttpHelper.CreateResponse(code, Json.CreateString(response)));
        }

        string CalculateShaPassHash(string name, string password)
        {
            SHA256 sha256 = SHA256.Create();
            var i = sha256.ComputeHash(Encoding.UTF8.GetBytes(name));
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(i.ToHexString() + ":" + password)).ToHexString();
        }
    }
}
