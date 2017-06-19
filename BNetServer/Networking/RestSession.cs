/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_INFO);
            stmt.AddValue(0, login);

            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
            {
                loginResult.AuthenticationState = "LOGIN";
                loginResult.ErrorCode = "UNABLE_TO_DECODE";
                loginResult.ErrorMessage = "There was an internal error while connecting to Battle.net. Please try again later.";
                SendResponse(HttpCode.BadRequest, loginResult);

                return;
            }

            string pass_hash = result.Read<string>(13);

            var accountInfo = new AccountInfo();
            accountInfo.LoadResult(result);

            if (CalculateShaPassHash(login, password) == pass_hash)
            {
                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID);
                stmt.AddValue(0, accountInfo.Id);

                SQLResult characterCountsResult = DB.Login.Query(stmt);
                if (!characterCountsResult.IsEmpty())
                {
                    do
                    {
                        accountInfo.GameAccounts[characterCountsResult.Read<uint>(0)]
                            .CharacterCounts[new RealmHandle(characterCountsResult.Read<byte>(3), characterCountsResult.Read<byte>(4), characterCountsResult.Read<uint>(2)).GetAddress()] = characterCountsResult.Read<byte>(1);

                    } while (characterCountsResult.NextRow());
                }


                stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_LAST_PLAYER_CHARACTERS);
                stmt.AddValue(0, accountInfo.Id);

                SQLResult lastPlayerCharactersResult = DB.Login.Query(stmt);
                if (!lastPlayerCharactersResult.IsEmpty())
                {
                    RealmHandle realmId = new RealmHandle(lastPlayerCharactersResult.Read<byte>(1), lastPlayerCharactersResult.Read<byte>(2), lastPlayerCharactersResult.Read<uint>(3));

                    LastPlayedCharacterInfo lastPlayedCharacter = new LastPlayedCharacterInfo();
                    lastPlayedCharacter.RealmId = realmId;
                    lastPlayedCharacter.CharacterName = lastPlayerCharactersResult.Read<string>(4);
                    lastPlayedCharacter.CharacterGUID = lastPlayerCharactersResult.Read<ulong>(5);
                    lastPlayedCharacter.LastPlayedTime = lastPlayerCharactersResult.Read<uint>(6);

                    accountInfo.GameAccounts[lastPlayerCharactersResult.Read<uint>(0)].LastPlayedCharacters[realmId.GetSubRegionAddress()] = lastPlayedCharacter;
                }

                byte[] ticket = new byte[0].GenerateRandomKey(20);
                loginResult.LoginTicket = "TC-" + ticket.ToHexString();

                Global.SessionMgr.AddLoginTicket(loginResult.LoginTicket, accountInfo);
            }
            else if (!accountInfo.IsBanned)
            {
                uint maxWrongPassword = ConfigMgr.GetDefaultValue("WrongPass.MaxCount", 0u);

                if (ConfigMgr.GetDefaultValue("WrongPass.Logging", false))
                    Log.outDebug(LogFilter.Network, "[{0}, Account {1}, Id {2}] Attempted to connect with wrong password!", request.Host, login, accountInfo.Id);

                if (maxWrongPassword != 0)
                {
                    SQLTransaction trans = new SQLTransaction();
                    stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_FAILED_LOGINS);
                    stmt.AddValue(0, accountInfo.Id);
                    trans.Append(stmt);

                    ++accountInfo.FailedLogins;

                    Log.outDebug(LogFilter.Network, "MaxWrongPass : {0}, failed_login : {1}", maxWrongPassword, accountInfo.Id);

                    if (accountInfo.FailedLogins >= maxWrongPassword)
                    {
                        BanMode banType = ConfigMgr.GetDefaultValue("WrongPass.BanType", BanMode.Ip);
                        int banTime = ConfigMgr.GetDefaultValue("WrongPass.BanTime", 600);

                        if (banType == BanMode.Account)
                        {
                            stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_BNET_ACCOUNT_AUTO_BANNED);
                            stmt.AddValue(0, accountInfo.Id);
                        }
                        else
                        {
                            stmt = DB.Login.GetPreparedStatement(LoginStatements.INS_IP_AUTO_BANNED);
                            stmt.AddValue(0, request.Host);
                        }

                        stmt.AddValue(1, banTime);
                        trans.Append(stmt);

                        stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_RESET_FAILED_LOGINS);
                        stmt.AddValue(0, accountInfo.Id);
                        trans.Append(stmt);
                    }

                    DB.Login.CommitTransaction(trans);
                }
            }

            loginResult.AuthenticationState = "DONE";
            SendResponse(HttpCode.Ok, loginResult);
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
