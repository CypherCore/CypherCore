// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Configuration;
using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.Networking.Http;
using Framework.Web;
using Framework.Web.Rest.Login;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BNetServer.REST
{
    public class LoginRESTService : HttpService<LoginHttpSession>
    {
        public static LoginRESTService Instance { get; } = new LoginRESTService();

        FormInputs _formInputs = new();
        int _port;
        string[] _hostnames = new string[2];
        IPAddress[] _addresses = new IPAddress[2];
        uint _loginTicketDuration;

        LoginRESTService() : base(LogFilter.Http) { }

        public override bool StartNetwork(string bindIp, int port, int threadCount = 1)
        {
            if (!base.StartNetwork(bindIp, port, threadCount))
                return false;

            RegisterHandler(HttpMethod.Get, "/bnetserver/login/", HandleGetForm);
            RegisterHandler(HttpMethod.Get, "/bnetserver/gameAccounts/", HandleGetGameAccounts);
            RegisterHandler(HttpMethod.Get, "/bnetserver/portal/", HandleGetPortal);
            RegisterHandler(HttpMethod.Post, "/bnetserver/login/", HandlePostLogin, RequestHandlerFlag.DoNotLogRequestContent);
            RegisterHandler(HttpMethod.Post, "/bnetserver/login/srp/", HandlePostLoginSrpChallenge);
            RegisterHandler(HttpMethod.Post, "/bnetserver/refreshLoginTicket/", HandlePostRefreshLoginTicket);

            _port = port;

            _hostnames[0] = ConfigMgr.GetDefaultValue("LoginREST.ExternalAddress", "127.0.0.1");
            var externalAddress = Dns.GetHostAddresses(_hostnames[0], System.Net.Sockets.AddressFamily.InterNetwork);
            if (externalAddress == null || externalAddress.Empty())
            {
                Log.outError(LogFilter.Network, $"Could not resolve LoginREST.ExternalAddress {_hostnames[0]}");
                return false;
            }

            _addresses[0] = externalAddress[0];

            _hostnames[1] = ConfigMgr.GetDefaultValue("LoginREST.LocalAddress", "127.0.0.1");
            var localAddress = Dns.GetHostAddresses(_hostnames[1], AddressFamily.InterNetwork);
            if (localAddress == null || localAddress.Empty())
            {
                Log.outError(LogFilter.Http, $"Could not resolve LoginREST.LocalAddress {_hostnames[1]}");
                return false;
            }

            _addresses[1] = localAddress[0];

            // set up form inputs
            _formInputs.Type = "LOGIN_FORM";

            var input = new FormInput();
            input.Id = "account_name";
            input.Type = "text";
            input.Label = "E-mail";
            input.MaxLength = 320;
            _formInputs.Inputs.Add(input);

            input = new FormInput();
            input.Id = "password";
            input.Type = "password";
            input.Label = "Password";
            input.MaxLength = 128;
            _formInputs.Inputs.Add(input);

            input = new FormInput();
            input.Id = "log_in_submit";
            input.Type = "submit";
            input.Label = "Log In";
            _formInputs.Inputs.Add(input);

            _loginTicketDuration = ConfigMgr.GetDefaultValue("LoginREST.TicketDuration", 3600u);

            MigrateLegacyPasswordHashes();

            Acceptor.AsyncAcceptSocket(OnSocketAccept);

            return true;
        }

        public string GetHostnameForClient(IPAddress address)
        {
            if (IPAddress.IsLoopback(address))
                return _hostnames[1];

            return _hostnames[0];
        }

        string ExtractAuthorization(HttpHeader request)
        {
            if (request.Authorization.IsEmpty())
                return null;

            string BASIC_PREFIX = "Basic ";

            string authorization = request.Authorization;
            if (authorization.StartsWith(BASIC_PREFIX))
                authorization = authorization.Remove(0, BASIC_PREFIX.Length);

            byte[] decoded = Convert.FromBase64String(authorization);
            if (decoded.Empty())
                return null;

            string decodedHeader = Encoding.UTF8.GetString(decoded);
            int ticketEnd = decodedHeader.IndexOf(':');
            if (ticketEnd != -1)
                decodedHeader = decodedHeader.Remove(decodedHeader.Length - ticketEnd);

            return decodedHeader;
        }

        RequestHandlerResult HandleGetForm(LoginHttpSession session, RequestContext context)
        {
            FormInputs form = _formInputs;
            form.SrpUrl = $"https://{GetHostnameForClient(session.GetRemoteIpAddress())}:{_port}/bnetserver/login/srp/";

            context.response.ContentType = "application/json;charset=utf-8";
            context.response.Content = JsonSerializer.Serialize(form);
            return RequestHandlerResult.Handled;
        }

        RequestHandlerResult HandleGetGameAccounts(LoginHttpSession session, RequestContext context)
        {
            string ticket = ExtractAuthorization(context.request);
            if (ticket.IsEmpty())
                return dispatcherService.HandleUnauthorized(session, context);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_GAME_ACCOUNT_LIST);
            stmt.AddValue(0, ticket);
            session.QueueQuery(DB.Login.AsyncQuery(stmt).WithCallback(result =>
            {
                GameAccountList gameAccounts = new();
                if (!result.IsEmpty())
                {
                    string formatDisplayName(string name)
                    {
                        var hashPos = name.IndexOf('#');
                        if (hashPos != -1)
                            return "WoW" + name.Substring(++hashPos);
                        else
                            return name;
                    };

                    long now = Time.UnixTime;
                    do
                    {
                        GameAccountInfo gameAccount = new();
                        gameAccount.DisplayName = formatDisplayName(result.Read<string>(0));
                        gameAccount.Expansion = result.Read<byte>(1);
                        if (!result.IsNull(2))
                        {
                            uint banDate = result.Read<uint>(2);
                            uint unbanDate = result.Read<uint>(3);
                            gameAccount.IsSuspended = unbanDate > now;
                            gameAccount.IsBanned = banDate == unbanDate;
                            gameAccount.SuspensionReason = result.Read<string>(4);
                            gameAccount.SuspensionExpires = unbanDate;
                        }

                        gameAccounts.GameAccounts.Add(gameAccount);
                    } while (result.NextRow());
                }

                context.response.ContentType = "application/json;charset=utf-8";
                context.response.Content = JsonSerializer.Serialize(gameAccounts);
                session.SendResponse(context);
            }));

            return RequestHandlerResult.Async;
        }

        RequestHandlerResult HandleGetPortal(LoginHttpSession session, RequestContext context)
        {
            context.response.ContentType = "text/plain";
            context.response.Content = $"{session.GetRemoteIpAddress()}:{ConfigMgr.GetDefaultValue("BattlenetPort", 1119)}";
            return RequestHandlerResult.Handled;
        }

        RequestHandlerResult HandlePostLogin(LoginHttpSession session, RequestContext context)
        {
            LoginForm loginForm = JsonSerializer.Deserialize<LoginForm>(context.request.Content);
            if (loginForm == null)
            {
                LoginResult loginResult = new();
                loginResult.AuthenticationState = AuthenticationState.LOGIN.ToString();
                loginResult.ErrorCode = "UNABLE_TO_DECODE";
                loginResult.ErrorMessage = "There was an internal error while connecting to Battle.net. Please try again later.";

                context.response.Status = HttpStatusCode.BadRequest;
                context.response.ContentType = "application/json;charset=utf-8";
                context.response.Content = JsonSerializer.Serialize(loginResult);
                session.SendResponse(context);

                return RequestHandlerResult.Handled;
            }

            string getInputValue(LoginForm loginForm, string inputId)
            {
                for (int i = 0; i < loginForm.Inputs.Count; ++i)
                    if (loginForm.Inputs[i].Id == inputId)
                        return loginForm.Inputs[i].Value;
                return "";
            };

            string login = getInputValue(loginForm, "account_name").ToUpper();

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_AUTHENTICATION);
            stmt.AddValue(0, login);

            session.QueueQuery(DB.Login.AsyncQuery(stmt).WithChainingCallback((callback, result) =>
            {
                if (result.IsEmpty())
                {
                    LoginResult loginResult = new();
                    loginResult.AuthenticationState = AuthenticationState.DONE.ToString();
                    context.response.ContentType = "application/json;charset=utf-8";
                    context.response.Content = JsonSerializer.Serialize(loginResult);
                    session.SendResponse(context);
                    return;
                }

                string login = getInputValue(loginForm, "account_name").ToUpper();
                bool passwordCorrect = false;
                string serverM2 = null;

                uint accountId = result.Read<uint>(0);
                if (session.GetSessionState().Srp == null)
                {
                    SrpVersion version = (SrpVersion)result.Read<sbyte>(1);
                    string srpUsername = SHA256.HashData(Encoding.UTF8.GetBytes(login)).ToHexString();
                    byte[] s = result.Read<byte[]>(2);
                    byte[] v = result.Read<byte[]>(3);
                    session.GetSessionState().Srp = CreateSrpImplementation(version, SrpHashFunction.Sha256, srpUsername, s, v);

                    string password = getInputValue(loginForm, "password");
                    if (version == SrpVersion.v1)
                        password = password.ToUpper();

                    passwordCorrect = session.GetSessionState().Srp.CheckCredentials(srpUsername, password);
                }
                else
                {
                    BigInteger A = new(getInputValue(loginForm, "public_A").ToByteArray(), true, true);
                    BigInteger M1 = new(getInputValue(loginForm, "client_evidence_M1").ToByteArray(), true, true);

                    var sessionKey = session.GetSessionState().Srp.VerifyClientEvidence(A, M1);
                    if (sessionKey.HasValue)
                    {
                        passwordCorrect = true;
                        serverM2 = session.GetSessionState().Srp.CalculateServerEvidence(A, M1, sessionKey.Value).ToByteArray(true, true).ToHexString();
                    }
                }

                uint failedLogins = result.Read<uint>(4);
                string loginTicket = result.Read<string>(5);
                uint loginTicketExpiry = result.Read<uint>(6);
                bool isBanned = result.Read<ulong>(7) != 0;

                if (!passwordCorrect)
                {
                    if (!isBanned)
                    {
                        string ip_address = session.GetRemoteIpAddress().ToString();
                        uint maxWrongPassword = ConfigMgr.GetDefaultValue("WrongPass.MaxCount", 0u);

                        if (ConfigMgr.GetDefaultValue("WrongPass.Logging", false))
                            Log.outDebug(LogFilter.Http, $"[{ip_address}, Account {login}, Id {accountId}] Attempted to connect with wrong password!");

                        if (maxWrongPassword != 0)
                        {
                            SQLTransaction trans = new();
                            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_FAILED_LOGINS);
                            stmt.AddValue(0, accountId);
                            trans.Append(stmt);

                            ++failedLogins;

                            Log.outDebug(LogFilter.Http, $"MaxWrongPass : {maxWrongPassword}, failed_login : {accountId}");

                            if (failedLogins >= maxWrongPassword)
                            {
                                BanMode banType = ConfigMgr.GetDefaultValue("WrongPass.BanType", BanMode.IP);
                                int banTime = ConfigMgr.GetDefaultValue("WrongPass.BanTime", 600);

                                if (banType == BanMode.Account)
                                {
                                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_BNET_ACCOUNT_AUTO_BANNED);
                                    stmt.AddValue(0, accountId);
                                }
                                else
                                {
                                    stmt = LoginDatabase.GetPreparedStatement(LoginStatements.INS_IP_AUTO_BANNED);
                                    stmt.AddValue(0, ip_address);
                                }

                                stmt.AddValue(1, banTime);
                                trans.Append(stmt);

                                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_RESET_FAILED_LOGINS);
                                stmt.AddValue(0, accountId);
                                trans.Append(stmt);
                            }

                            DB.Login.CommitTransaction(trans);
                        }
                    }

                    LoginResult loginResult = new();
                    loginResult.AuthenticationState = AuthenticationState.DONE.ToString();

                    context.response.ContentType = "application/json;charset=utf-8";
                    context.response.Content = JsonSerializer.Serialize(loginResult);
                    session.SendResponse(context);
                    return;
                }

                if (loginTicket.IsEmpty() || loginTicketExpiry < Time.UnixTime)
                    loginTicket = "TC-" + RandomHelper.GetRandomBytes(20).ToHexString();

                stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_AUTHENTICATION);
                stmt.AddValue(0, loginTicket);
                stmt.AddValue(1, Time.UnixTime + _loginTicketDuration);
                stmt.AddValue(2, accountId);
                callback.WithCallback(_ =>
                {
                    LoginResult loginResult = new();
                    loginResult.AuthenticationState = AuthenticationState.DONE.ToString();
                    loginResult.LoginTicket = loginTicket;
                    if (!serverM2.IsEmpty())
                        loginResult.ServerEvidenceM2 = serverM2;

                    context.response.ContentType = "application/json;charset=utf-8";
                    JsonSerializerOptions options = new JsonSerializerOptions();
                    options.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                    context.response.Content = JsonSerializer.Serialize(loginResult);
                    session.SendResponse(context);
                }).SetNextQuery(DB.Login.AsyncQuery(stmt));
            }));

            return RequestHandlerResult.Async;
        }

        RequestHandlerResult HandlePostLoginSrpChallenge(LoginHttpSession session, RequestContext context)
        {
            LoginForm loginForm = JsonSerializer.Deserialize<LoginForm>(context.request.Content);
            if (loginForm == null)
            {
                LoginResult loginResult = new();
                loginResult.AuthenticationState = AuthenticationState.LOGIN.ToString();
                loginResult.ErrorCode = "UNABLE_TO_DECODE";
                loginResult.ErrorMessage = "There was an internal error while connecting to Battle.net. Please try again later.";

                context.response.Status = HttpStatusCode.BadRequest;
                context.response.ContentType = "application/json;charset=utf-8";
                context.response.Content = JsonSerializer.Serialize(loginResult);
                session.SendResponse(context);

                return RequestHandlerResult.Handled;
            }

            string login = "";

            for (int i = 0; i < loginForm.Inputs.Count; ++i)
                if (loginForm.Inputs[i].Id == "account_name")
                    login = loginForm.Inputs[i].Value;

            login = login.ToUpper();

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_CHECK_PASSWORD_BY_EMAIL);
            stmt.AddValue(0, login);

            session.QueueQuery(DB.Login.AsyncQuery(stmt).WithCallback(result =>
            {
                if (result.IsEmpty())
                {
                    LoginResult loginResult = new();
                    loginResult.AuthenticationState = AuthenticationState.DONE.ToString();
                    context.response.ContentType = "application/json;charset=utf-8";
                    context.response.Content = JsonSerializer.Serialize(loginResult);
                    session.SendResponse(context);
                    return;
                }

                SrpVersion version = (SrpVersion)result.Read<byte>(0);
                SrpHashFunction hashFunction = SrpHashFunction.Sha256;
                string srpUsername = SHA256.HashData(Encoding.UTF8.GetBytes(login)).ToHexString();
                byte[] s = result.Read<byte[]>(1);
                byte[] v = result.Read<byte[]>(2);

                session.GetSessionState().Srp = CreateSrpImplementation(version, hashFunction, srpUsername, s, v);
                if (session.GetSessionState().Srp == null)
                {
                    context.response.Status = HttpStatusCode.InternalServerError;
                    session.SendResponse(context);
                    return;
                }

                SrpLoginChallenge challenge = new();
                challenge.Version = session.GetSessionState().Srp.GetVersion();
                challenge.Iterations = (int)session.GetSessionState().Srp.GetXIterations();
                challenge.Modulus = session.GetSessionState().Srp.GetN().ToByteArray(true, true).ToHexString();
                challenge.Generator = session.GetSessionState().Srp.Getg().ToByteArray(true, true).ToHexString();

                string hash = "";
                switch (hashFunction)
                {
                    case SrpHashFunction.Sha256:
                        hash = "SHA-256";
                        break;
                    case SrpHashFunction.Sha512:
                        hash = "SHA-512";
                        break;
                }

                challenge.HashFunction = hash;
                challenge.Username = srpUsername;
                challenge.Salt = session.GetSessionState().Srp.s.ToHexString();
                challenge.PublicB = session.GetSessionState().Srp.B.ToByteArray(true, true).ToHexString();

                context.response.ContentType = "application/json;charset=utf-8";
                context.response.Content = JsonSerializer.Serialize(challenge);
                session.SendResponse(context);
            }));

            return RequestHandlerResult.Async;
        }

        RequestHandlerResult HandlePostRefreshLoginTicket(LoginHttpSession session, RequestContext context)
        {
            string ticket = ExtractAuthorization(context.request);
            if (ticket.IsEmpty())
                return dispatcherService.HandleUnauthorized(session, context);

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_EXISTING_AUTHENTICATION);
            stmt.AddValue(0, ticket);
            session.QueueQuery(DB.Login.AsyncQuery(stmt).WithCallback(result =>
            {
                LoginRefreshResult loginRefreshResult = new();
                if (!result.IsEmpty())
                {
                    uint loginTicketExpiry = result.Read<uint>(0);
                    long now = Time.UnixTime;
                    if (loginTicketExpiry > now)
                    {
                        loginRefreshResult.LoginTicketExpiry = (now + _loginTicketDuration);

                        PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_EXISTING_AUTHENTICATION);
                        stmt.AddValue(0, (uint)(now + _loginTicketDuration));
                        stmt.AddValue(1, ticket);
                        DB.Login.Execute(stmt);
                    }
                    else
                        loginRefreshResult.IsExpired = true;
                }
                else
                    loginRefreshResult.IsExpired = true;

                context.response.ContentType = "application/json;charset=utf-8";
                context.response.Content = JsonSerializer.Serialize(loginRefreshResult);
                session.SendResponse(context);
            }));

            return RequestHandlerResult.Async;
        }

        public BnetSRP6Base CreateSrpImplementation(SrpVersion version, SrpHashFunction hashFunction, string username, byte[] salt, byte[] verifier)
        {
            if (version == SrpVersion.v2)
            {
                if (hashFunction == SrpHashFunction.Sha256)
                    return new BnetSRP6v2Hash256(username, salt, verifier);
                if (hashFunction == SrpHashFunction.Sha512)
                    return new BnetSRP6v2Hash512(username, salt, verifier);
            }

            if (version == SrpVersion.v1)
            {
                if (hashFunction == SrpHashFunction.Sha256)
                    return new BnetSRP6v1Hash256(username, salt, verifier);
                if (hashFunction == SrpHashFunction.Sha512)
                    return new BnetSRP6v1Hash512(username, salt, verifier);
            }

            return null;
        }

        public override SessionState CreateNewSessionState(IPAddress address)
        {
            var state = new LoginSessionState();
            sessionService.InitAndStoreSessionState(state, address);
            return state;
        }

        public static void OnSocketAccept(Socket sock)
        {
            Global.LoginService.OnSocketOpen(sock);
        }

        void MigrateLegacyPasswordHashes()
        {
            if (DB.Login.Query("SELECT 1 FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = SCHEMA() AND TABLE_NAME = 'battlenet_accounts' AND COLUMN_NAME = 'sha_pass_hash'").IsEmpty())
                return;

            Log.outInfo(_logger, "Updating password hashes...");
            uint start = Time.GetMSTime();
            // the auth update query nulls salt/verifier if they cannot be converted
            // if they are non-null but s/v have been cleared, that means a legacy tool touched our auth DB (otherwise, the core might've done it itself, it used to use those hacks too)
            SQLResult result = DB.Login.Query("SELECT id, sha_pass_hash, IF((salt IS null) OR (verifier IS null), 0, 1) AS shouldWarn FROM battlenet_accounts WHERE sha_pass_hash != DEFAULT(sha_pass_hash) OR salt IS NULL OR verifier IS NULL");
            if (result.IsEmpty())
            {
                Log.outInfo(_logger, $"No password hashes to update - this took us {Time.GetMSTimeDiffToNow(start)} ms to realize");
                return;
            }

            bool hadWarning = false;
            uint count = 0;
            SQLTransaction tx = new SQLTransaction();
            do
            {
                uint id = result.Read<uint>(0);

                byte[] salt = RandomHelper.GetRandomBytes(SRP6.SaltLength);
                BigInteger x = new BigInteger(SHA256.HashData(salt.Combine(result.Read<string>(1).ToByteArray(true))), true);
                byte[] verifier = BigInteger.ModPow(BnetSRP6v1Base.g, x, BnetSRP6v1Base.N).ToByteArray(true, true);

                if (result.Read<long>(2) != 0)
                {
                    if (!hadWarning)
                    {
                        hadWarning = true;
                        Log.outWarn(_logger,
                            "       ========\n" +
                            "(!) You appear to be using an outdated external account management tool.\n" +
                            "(!) Update your external tool.\n" +
                            "(!!) If no update is available, refer your tool's developer to https://github.com/TrinityCore/TrinityCore/issues/25157.\n" +
                            "       ========");
                    }
                }

                PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.UPD_BNET_LOGON);
                stmt.AddValue(0, (sbyte)SrpVersion.v1);
                stmt.AddValue(1, salt);
                stmt.AddValue(2, verifier);
                stmt.AddValue(3, id);
                tx.Append(stmt);

                tx.Append($"UPDATE battlenet_accounts SET sha_pass_hash = DEFAULT(sha_pass_hash) WHERE id = {id}");

                if (tx.GetSize() >= 10000)
                {
                    DB.Login.CommitTransaction(tx);
                    tx = new SQLTransaction();
                }

                ++count;
            } while (result.NextRow());
            DB.Login.CommitTransaction(tx);

            Log.outInfo(_logger, $"{count} password hashes updated in {Time.GetMSTimeDiffToNow(start)} ms");
        }

        public int GetPort() { return _port; }
    }
}