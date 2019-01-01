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

using Bgs.Protocol;
using Bgs.Protocol.Challenge.V1;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Framework.Networking;
using Framework.Rest;
using Framework.Serialization;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace BNetServer.Networking
{
    public class Session : SSLSocket
    {
        public Session(Socket socket) : base(socket)
        {
            _accountInfo = new AccountInfo();

            ClientRequestHandlers.Add("Command_RealmListTicketRequest_v1_b9", GetRealmListTicket);
            ClientRequestHandlers.Add("Command_LastCharPlayedRequest_v1_b9", GetLastCharPlayed);
            ClientRequestHandlers.Add("Command_RealmListRequest_v1_b9", GetRealmList);
            ClientRequestHandlers.Add("Command_RealmJoinRequest_v1_b9", JoinRealm);
        }

        public override void Start()
        {
            string ip_address = GetRemoteIpAddress().ToString();
            Log.outInfo(LogFilter.Network, "{0} Accepted connection", GetClientInfo());

            // Verify that this IP is not in the ip_banned table
            DB.Login.Execute(DB.Login.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS));

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_IP_INFO);
            stmt.AddValue(0, ip_address);
            stmt.AddValue(1, BitConverter.ToUInt32(GetRemoteIpAddress().GetAddressBytes(), 0));

            _queryProcessor.AddQuery(DB.Login.AsyncQuery(stmt).WithCallback(CheckIpCallback));
        }

        void CheckIpCallback(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                bool banned = false;
                do
                {
                    if (result.Read<ulong>(0) != 0)
                        banned = true;

                    if (!string.IsNullOrEmpty(result.Read<string>(1)))
                        _ipCountry = result.Read<string>(1);

                } while (result.NextRow());

                if (banned)
                {
                    Log.outDebug(LogFilter.Session, "{0} tries to log in using banned IP!", GetClientInfo());
                    CloseSocket();
                    return;
                }
            }

            AsyncHandshake(Global.SessionMgr.GetCertificate());
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            _queryProcessor.ProcessReadyQueries();

            return true;
        }

        public override void ReadHandler(int transferredBytes)
        {
            if (!IsOpen())
                return;

            var stream = new CodedInputStream(GetReceiveBuffer(), 0, transferredBytes);
            while (!stream.IsAtEnd)
            {
                var header = new Header();
                stream.ReadMessage(header);

                if (header.ServiceId != 0xFE)
                {
                    Global.ServiceDispatcher.Dispatch(this, header.ServiceHash, header.Token, header.MethodId, stream);
                }
                else
                {
                    var handler = _responseCallbacks.LookupByKey(header.Token);
                    if (handler != null)
                    {
                        handler(stream);
                        _responseCallbacks.Remove(header.Token);
                    }
                }
            }

            AsyncRead();
        }

        void AsyncWrite(ByteBuffer packet)
        {
            if (!IsOpen())
                return;

            AsyncWrite(packet.GetData());
        }

        public void SendResponse(uint token, IMessage response)
        {
            Header header = new Header();
            header.Token = token;
            header.ServiceId = 0xFE;
            header.Size = (uint)response.CalculateSize();

            var headerSizeBytes = BitConverter.GetBytes((ushort)header.CalculateSize());
            Array.Reverse(headerSizeBytes);

            ByteBuffer packet = new ByteBuffer();
            packet.WriteBytes(headerSizeBytes, 2);
            packet.WriteBytes(header.ToByteArray());
            packet.WriteBytes(response.ToByteArray());

            AsyncWrite(packet.GetData());
        }

        public void SendResponse(uint token, BattlenetRpcErrorCode status)
        {
            Header header = new Header();
            header.Token = token;
            header.Status = (uint)status;
            header.ServiceId = 0xFE;

            var headerSizeBytes = BitConverter.GetBytes((ushort)header.CalculateSize());
            Array.Reverse(headerSizeBytes);

            ByteBuffer packet = new ByteBuffer();
            packet.WriteBytes(headerSizeBytes, 2);
            packet.WriteBytes(header.ToByteArray());

            AsyncWrite(packet);
        }

        public void SendRequest(uint serviceHash, uint methodId, IMessage request, Action<CodedInputStream> callback)
        {
            _responseCallbacks[_requestToken] = callback;
            SendRequest(serviceHash, methodId, request);
        }

        public void SendRequest(uint serviceHash, uint methodId, IMessage request)
        {
            Header header = new Header();
            header.ServiceId = 0;
            header.ServiceHash = serviceHash;
            header.MethodId = methodId;
            header.Size = (uint)request.CalculateSize();
            header.Token = _requestToken++;

            var headerSizeBytes = BitConverter.GetBytes((ushort)header.CalculateSize());
            Array.Reverse(headerSizeBytes);

            ByteBuffer packet = new ByteBuffer();
            packet.WriteBytes(headerSizeBytes, 2);
            packet.WriteBytes(header.ToByteArray());
            packet.WriteBytes(request.ToByteArray());

            AsyncWrite(packet);
        }

        public BattlenetRpcErrorCode HandleLogon(Bgs.Protocol.Authentication.V1.LogonRequest logonRequest)
        {
            if (logonRequest.Program != "WoW")
            {
                Log.outDebug(LogFilter.Session, "Battlenet.LogonRequest: {0} attempted to log in with game other than WoW (using {1})!", GetClientInfo(), logonRequest.Program);
                return BattlenetRpcErrorCode.BadProgram;
            }

            if (logonRequest.Platform != "Win" && logonRequest.Platform != "Wn64" && logonRequest.Platform != "Mc64")
            {
                Log.outDebug(LogFilter.Session, "Battlenet.LogonRequest: {0} attempted to log in from an unsupported platform (using {1})!", GetClientInfo(), logonRequest.Platform);
                return BattlenetRpcErrorCode.BadPlatform;
            }

            if (logonRequest.Locale.ToEnum<LocaleConstant>() == LocaleConstant.enUS && logonRequest.Locale != "enUS")
            {
                Log.outDebug(LogFilter.Session, "Battlenet.LogonRequest: {0} attempted to log in with unsupported locale (using {1})!", GetClientInfo(), logonRequest.Locale);
                return BattlenetRpcErrorCode.BadLocale;
            }

            _locale = logonRequest.Locale;
            _os = logonRequest.Platform;
            _build = (uint)logonRequest.ApplicationVersion;

            var endpoint = Global.SessionMgr.GetAddressForClient(GetRemoteIpAddress());

            ChallengeExternalRequest externalChallenge = new ChallengeExternalRequest();
            externalChallenge.PayloadType = "web_auth_url";

            externalChallenge.Payload = ByteString.CopyFromUtf8(string.Format("https://{0}:{1}/bnetserver/login/", endpoint.Address, endpoint.Port));
            SendRequest((uint)OriginalHash.ChallengeListener, 3, externalChallenge);
            return BattlenetRpcErrorCode.Ok;
        }

        public BattlenetRpcErrorCode HandleVerifyWebCredentials(Bgs.Protocol.Authentication.V1.VerifyWebCredentialsRequest verifyWebCredentialsRequest)
        {
            if (verifyWebCredentialsRequest.WebCredentials.IsEmpty)
                return BattlenetRpcErrorCode.Denied;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ACCOUNT_INFO);
            stmt.AddValue(0, verifyWebCredentialsRequest.WebCredentials.ToStringUtf8());

            SQLResult result = DB.Login.Query(stmt);
            if (result.IsEmpty())
                return BattlenetRpcErrorCode.Denied;

            _accountInfo = new AccountInfo();
            _accountInfo.LoadResult(result);

            if (_accountInfo.LoginTicketExpiry < Time.UnixTime)
                return BattlenetRpcErrorCode.TimedOut;

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_BNET_ID);
            stmt.AddValue(0, _accountInfo.Id);

            SQLResult characterCountsResult = DB.Login.Query(stmt);
            if (!characterCountsResult.IsEmpty())
            {
                do
                {
                    RealmHandle realmId = new RealmHandle(characterCountsResult.Read<byte>(3), characterCountsResult.Read<byte>(4), characterCountsResult.Read<uint>(2));
                    _accountInfo.GameAccounts[characterCountsResult.Read<uint>(0)].CharacterCounts[realmId.GetAddress()] = characterCountsResult.Read<byte>(1);

                } while (characterCountsResult.NextRow());
            }

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_LAST_PLAYER_CHARACTERS);
            stmt.AddValue(0, _accountInfo.Id);

            SQLResult lastPlayerCharactersResult = DB.Login.Query(stmt);
            if (!lastPlayerCharactersResult.IsEmpty())
            {
                do
                {
                    RealmHandle realmId = new RealmHandle(lastPlayerCharactersResult.Read<byte>(1), lastPlayerCharactersResult.Read<byte>(2), lastPlayerCharactersResult.Read<uint>(3));

                    LastPlayedCharacterInfo lastPlayedCharacter = new LastPlayedCharacterInfo();
                    lastPlayedCharacter.RealmId = realmId;
                    lastPlayedCharacter.CharacterName = lastPlayerCharactersResult.Read<string>(4);
                    lastPlayedCharacter.CharacterGUID = lastPlayerCharactersResult.Read<ulong>(5);
                    lastPlayedCharacter.LastPlayedTime = lastPlayerCharactersResult.Read<uint>(6);

                    _accountInfo.GameAccounts[lastPlayerCharactersResult.Read<uint>(0)].LastPlayedCharacters[realmId.GetSubRegionAddress()] = lastPlayedCharacter;

                } while (lastPlayerCharactersResult.NextRow());
            }

            string ip_address = GetRemoteIpAddress().ToString();

            // If the IP is 'locked', check that the player comes indeed from the correct IP address
            if (_accountInfo.IsLockedToIP)
            {
                Log.outDebug(LogFilter.Session, "Session.HandleVerifyWebCredentials: Account '{0}' is locked to IP - '{1}' is logging in from '{2}'",
                    _accountInfo.Login, _accountInfo.LastIP, ip_address);

                if (_accountInfo.LastIP != ip_address)
                    return BattlenetRpcErrorCode.RiskAccountLocked;
            }
            else
            {
                Log.outDebug(LogFilter.Session, "Session.HandleVerifyWebCredentials: Account '{0}' is not locked to ip", _accountInfo.Login);
                if (_accountInfo.LockCountry.IsEmpty() || _accountInfo.LockCountry == "00")
                    Log.outDebug(LogFilter.Session, "Session.HandleVerifyWebCredentials: Account '{0}' is not locked to country", _accountInfo.Login);
                else if (!_accountInfo.LockCountry.IsEmpty() && !_ipCountry.IsEmpty())
                {
                    Log.outDebug(LogFilter.Session, "Session.HandleVerifyWebCredentials: Account '{0}' is locked to country: '{1}' Player country is '{2}'",
                        _accountInfo.Login, _accountInfo.LockCountry, _ipCountry);

                    if (_ipCountry != _accountInfo.LockCountry)
                        return BattlenetRpcErrorCode.RiskAccountLocked;
                }
            }

            // If the account is banned, reject the logon attempt
            if (_accountInfo.IsBanned)
            {
                if (_accountInfo.IsPermanenetlyBanned)
                {
                    Log.outDebug(LogFilter.Session, "{0} Session.HandleVerifyWebCredentials: Banned account {1} tried to login!", GetClientInfo(), _accountInfo.Login);
                    return BattlenetRpcErrorCode.GameAccountBanned;
                }
                else
                {
                    Log.outDebug(LogFilter.Session, "{0} Session.HandleVerifyWebCredentials: Temporarily banned account {1} tried to login!", GetClientInfo(), _accountInfo.Login);
                    return BattlenetRpcErrorCode.GameAccountSuspended;
                }
            }

            Bgs.Protocol.Authentication.V1.LogonResult logonResult = new Bgs.Protocol.Authentication.V1.LogonResult();
            logonResult.ErrorCode = 0;
            logonResult.AccountId = new EntityId();
            logonResult.AccountId.Low = _accountInfo.Id;
            logonResult.AccountId.High = 0x100000000000000;
            foreach (var pair in _accountInfo.GameAccounts)
            {
                EntityId gameAccountId = new EntityId();
                gameAccountId.Low = pair.Value.Id;
                gameAccountId.High = 0x200000200576F57;
                logonResult.GameAccountId.Add(gameAccountId);
            }

            if (!_ipCountry.IsEmpty())
                logonResult.GeoipCountry = _ipCountry;

            logonResult.SessionKey = ByteString.CopyFrom(new byte[64].GenerateRandomKey(64));

            _authed = true;

            SendRequest((uint)OriginalHash.AuthenticationListener, 5, logonResult);
            return BattlenetRpcErrorCode.Ok;
        }

        public BattlenetRpcErrorCode HandleGetAccountState(Bgs.Protocol.Account.V1.GetAccountStateRequest request, Bgs.Protocol.Account.V1.GetAccountStateResponse response)
        {
            if (!_authed)
                return BattlenetRpcErrorCode.Denied;

            if (request.Options.FieldPrivacyInfo)
            {
                response.State = new Bgs.Protocol.Account.V1.AccountState();
                response.State.PrivacyInfo = new Bgs.Protocol.Account.V1.PrivacyInfo();
                response.State.PrivacyInfo.IsUsingRid = false;
                response.State.PrivacyInfo.IsRealIdVisibleForViewFriends = false;
                response.State.PrivacyInfo.IsHiddenFromFriendFinder = true;

                response.Tags = new Bgs.Protocol.Account.V1.AccountFieldTags();
                response.Tags.PrivacyInfoTag = 0xD7CA834D;
            }

            return BattlenetRpcErrorCode.Ok;
        }

        public BattlenetRpcErrorCode HandleGetGameAccountState(Bgs.Protocol.Account.V1.GetGameAccountStateRequest request, Bgs.Protocol.Account.V1.GetGameAccountStateResponse response)
        {
            if (!_authed)
                return BattlenetRpcErrorCode.Denied;

            if (request.Options.FieldGameLevelInfo)
            {
                var gameAccountInfo = _accountInfo.GameAccounts.LookupByKey(request.GameAccountId.Low);
                if (gameAccountInfo != null)
                {
                    response.State = new Bgs.Protocol.Account.V1.GameAccountState();
                    response.State.GameLevelInfo = new Bgs.Protocol.Account.V1.GameLevelInfo();
                    response.State.GameLevelInfo.Name = gameAccountInfo.DisplayName;
                    response.State.GameLevelInfo.Program = 5730135; // WoW
                }

                response.Tags = new Bgs.Protocol.Account.V1.GameAccountFieldTags();
                response.Tags.GameLevelInfoTag = 0x5C46D483;
            }

            if (request.Options.FieldGameStatus)
            {
                if (response.State == null)
                    response.State = new Bgs.Protocol.Account.V1.GameAccountState();

                response.State.GameStatus = new Bgs.Protocol.Account.V1.GameStatus();

                var gameAccountInfo = _accountInfo.GameAccounts.LookupByKey(request.GameAccountId.Low);
                if (gameAccountInfo != null)
                {
                    response.State.GameStatus.IsSuspended = gameAccountInfo.IsBanned;
                    response.State.GameStatus.IsBanned = gameAccountInfo.IsPermanenetlyBanned;
                    response.State.GameStatus.SuspensionExpires = (gameAccountInfo.UnbanDate * 1000000);
                }

                response.State.GameStatus.Program = 5730135; // WoW
                response.Tags.GameStatusTag = 0x98B75F99;
            }

            return BattlenetRpcErrorCode.Ok;
        }

        public BattlenetRpcErrorCode HandleProcessClientRequest(Bgs.Protocol.GameUtilities.V1.ClientRequest request, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
        {
            if (!_authed)
                return BattlenetRpcErrorCode.Denied;

            Bgs.Protocol.Attribute command = null;
            Dictionary<string, Variant> Params = new Dictionary<string, Variant>();

            for (int i = 0; i < request.Attribute.Count; ++i)
            {
                Bgs.Protocol.Attribute attr = request.Attribute[i];
                Params[attr.Name] = attr.Value;
                if (attr.Name.Contains("Command_"))
                    command = attr;
            }

            if (command == null)
            {
                Log.outError(LogFilter.SessionRpc, "{0} sent ClientRequest with no command.", GetClientInfo());
                return BattlenetRpcErrorCode.RpcMalformedRequest;
            }

            var handler = ClientRequestHandlers.LookupByKey(command.Name);
            if (handler == null)
            {
                Log.outError(LogFilter.SessionRpc, "{0} sent ClientRequest with unknown command {1}.", GetClientInfo(), command.Name);
                return BattlenetRpcErrorCode.RpcNotImplemented;
            }

            return handler(Params, response);
        }

        Variant GetParam(Dictionary<string, Variant> Params, string paramName)
        {
            return Params.LookupByKey(paramName);
        }

        delegate BattlenetRpcErrorCode ClientRequestHandler(Dictionary<string, Variant> Params, Bgs.Protocol.GameUtilities.V1.ClientResponse response);
        BattlenetRpcErrorCode GetRealmListTicket(Dictionary<string, Variant> Params, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
        {
            Variant identity = GetParam(Params, "Param_Identity");
            if (identity != null)
            {
                var realmListTicketIdentity = Json.CreateObject<RealmListTicketIdentity>(identity.BlobValue.ToStringUtf8(), true);
                var gameAccountInfo = _accountInfo.GameAccounts.LookupByKey(realmListTicketIdentity.GameAccountId);
                if (gameAccountInfo != null)
                    _gameAccountInfo = gameAccountInfo;
            }

            if (_gameAccountInfo == null)
                return BattlenetRpcErrorCode.UtilServerInvalidIdentityArgs;

            if (_gameAccountInfo.IsPermanenetlyBanned)
                return BattlenetRpcErrorCode.GameAccountBanned;
            else if (_gameAccountInfo.IsBanned)
                return BattlenetRpcErrorCode.GameAccountSuspended;

            bool clientInfoOk = false;
            Variant clientInfo = GetParam(Params, "Param_ClientInfo");
            if (clientInfo != null)
            {
                var realmListTicketClientInformation = Json.CreateObject<RealmListTicketClientInformation>(clientInfo.BlobValue.ToStringUtf8(), true);
                clientInfoOk = true;
                int i = 0;
                foreach (var b in realmListTicketClientInformation.Info.Secret.Select(Convert.ToByte))
                    _clientSecret[i++] = b;
            }

            if (!clientInfoOk)
                return BattlenetRpcErrorCode.WowServicesDeniedRealmListTicket;

            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_BNET_LAST_LOGIN_INFO);
            stmt.AddValue(0, GetRemoteIpAddress().ToString());
            stmt.AddValue(1, Enum.Parse(typeof(LocaleConstant), _locale));
            stmt.AddValue(2, _os);
            stmt.AddValue(3, _accountInfo.Id);

            DB.Login.Execute(stmt);

            var attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_RealmListTicket";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom("AuthRealmListTicket", System.Text.Encoding.UTF8);
            response.Attribute.Add(attribute);

            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode GetLastCharPlayed(Dictionary<string, Variant> Params, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
        {
            Variant subRegion = GetParam(Params, "Command_LastCharPlayedRequest_v1_b9");
            if (subRegion != null)
            {
                var lastPlayerChar = _gameAccountInfo.LastPlayedCharacters.LookupByKey(subRegion.StringValue);
                if (lastPlayerChar != null)
                {
                    var compressed = Global.RealmMgr.GetRealmEntryJSON(lastPlayerChar.RealmId, _build);
                    if (compressed.Length == 0)
                        return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

                    var attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_RealmEntry";
                    attribute.Value = new Variant();
                    attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
                    response.Attribute.Add(attribute);

                    attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_CharacterName";
                    attribute.Value = new Variant();
                    attribute.Value.StringValue = lastPlayerChar.CharacterName;
                    response.Attribute.Add(attribute);

                    attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_CharacterGUID";
                    attribute.Value = new Variant();
                    attribute.Value.BlobValue = ByteString.CopyFrom(BitConverter.GetBytes(lastPlayerChar.CharacterGUID));
                    response.Attribute.Add(attribute);

                    attribute = new Bgs.Protocol.Attribute();
                    attribute.Name = "Param_LastPlayedTime";
                    attribute.Value = new Variant();
                    attribute.Value.IntValue = (int)lastPlayerChar.LastPlayedTime;
                    response.Attribute.Add(attribute);
                }

                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.UtilServerUnknownRealm;
        }

        BattlenetRpcErrorCode GetRealmList(Dictionary<string, Variant> Params, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
        {
            if (_gameAccountInfo == null)
                return BattlenetRpcErrorCode.UserServerBadWowAccount;

            string subRegionId = "";
            Variant subRegion = GetParam(Params, "Command_RealmListRequest_v1_b9");
            if (subRegion != null)
                subRegionId = subRegion.StringValue;

            var compressed = Global.RealmMgr.GetRealmList(_build, subRegionId);
            if (compressed.Length == 0)
                return BattlenetRpcErrorCode.UtilServerFailedToSerializeResponse;

            var attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_RealmList";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);

            var realmCharacterCounts = new RealmCharacterCountList();
            foreach (var characterCount in _gameAccountInfo.CharacterCounts)
            {
                var countEntry = new RealmCharacterCountEntry();
                countEntry.WowRealmAddress = (int)characterCount.Key;
                countEntry.Count = characterCount.Value;
                realmCharacterCounts.Counts.Add(countEntry);
            }

            compressed = Json.Deflate("JSONRealmCharacterCountList", realmCharacterCounts);

            attribute = new Bgs.Protocol.Attribute();
            attribute.Name = "Param_CharacterCountList";
            attribute.Value = new Variant();
            attribute.Value.BlobValue = ByteString.CopyFrom(compressed);
            response.Attribute.Add(attribute);
            return BattlenetRpcErrorCode.Ok;
        }

        BattlenetRpcErrorCode JoinRealm(Dictionary<string, Variant> Params, Bgs.Protocol.GameUtilities.V1.ClientResponse response)
        {
            Variant realmAddress = GetParam(Params, "Param_RealmAddress");
            if (realmAddress != null)
                return Global.RealmMgr.JoinRealm((uint)realmAddress.UintValue, _build, GetRemoteIpAddress(), _clientSecret, (LocaleConstant)Enum.Parse(typeof(LocaleConstant), _locale), _os, _gameAccountInfo.Name, response);

            return BattlenetRpcErrorCode.WowServicesInvalidJoinTicket;
        }

        public BattlenetRpcErrorCode HandleGetAllValuesForAttribute(Bgs.Protocol.GameUtilities.V1.GetAllValuesForAttributeRequest request, Bgs.Protocol.GameUtilities.V1.GetAllValuesForAttributeResponse response)
        {
            if (!_authed)
                return BattlenetRpcErrorCode.Denied;

            if (request.AttributeKey == "Command_RealmListRequest_v1_b9")
            {
                Global.RealmMgr.WriteSubRegions(response);
                return BattlenetRpcErrorCode.Ok;
            }

            return BattlenetRpcErrorCode.RpcNotImplemented;
        }

        public string GetClientInfo()
        {
            string stream = '[' + GetRemoteIpAddress().ToString() + ':' + GetRemotePort();
            if (_accountInfo != null && !string.IsNullOrEmpty(_accountInfo.Login))
                stream += ", Account: " + _accountInfo.Login;

            if (_gameAccountInfo != null)
                stream += ", Game account: " + _gameAccountInfo.Name;

            stream += ']';

            return stream;
        }

        public uint GetAccountId() { return _accountInfo.Id; }
        public uint GetGameAccountId() { return _gameAccountInfo.Id; }

        Dictionary<string, ClientRequestHandler> ClientRequestHandlers = new Dictionary<string, ClientRequestHandler>();

        AccountInfo _accountInfo;
        GameAccountInfo _gameAccountInfo;          // Points at selected game account (inside _gameAccounts)

        string _locale;
        string _os;
        uint _build;

        string _ipCountry;

        Array<byte> _clientSecret = new Array<byte>(32);

        bool _authed;

        QueryCallbackProcessor _queryProcessor = new QueryCallbackProcessor();

        Dictionary<uint, Action<CodedInputStream>> _responseCallbacks = new Dictionary<uint, Action<CodedInputStream>>();
        uint _requestToken;
    }

    public class AccountInfo
    {
        public void LoadResult(SQLResult result)
        {
            Id = result.Read<uint>(0);
            Login = result.Read<string>(1);
            IsLockedToIP = result.Read<bool>(2);
            LockCountry = result.Read<string>(3);
            LastIP = result.Read<string>(4);
            LoginTicketExpiry = result.Read<uint>(5);
            IsBanned = result.Read<ulong>(6) != 0;
            IsPermanenetlyBanned = result.Read<ulong>(7) != 0;
            PasswordVerifier = result.Read<string>(9);
            Salt = result.Read<string>(10);

            const int GameAccountFieldsOffset = 8;
            do
            {
                var account = new GameAccountInfo();
                account.LoadResult(result.GetFields(), GameAccountFieldsOffset);
                GameAccounts[result.Read<uint>(GameAccountFieldsOffset)] = account;

            } while (result.NextRow());

        }

        public uint Id;
        public string Login;
        public bool IsLockedToIP;
        public string LockCountry;
        public string LastIP;
        public uint LoginTicketExpiry;
        public bool IsBanned;
        public bool IsPermanenetlyBanned;
        public string PasswordVerifier;
        public string Salt;

        public Dictionary<uint, GameAccountInfo> GameAccounts = new Dictionary<uint, GameAccountInfo>();
    }

    public class GameAccountInfo
    {
        public void LoadResult(SQLFields fields, int startColumn)
        {
            Id = fields.Read<uint>(startColumn + 0);
            Name = fields.Read<string>(startColumn + 1);
            UnbanDate = fields.Read<uint>(startColumn + 2);
            IsPermanenetlyBanned = fields.Read<uint>(startColumn + 3) != 0;
            IsBanned = IsPermanenetlyBanned || UnbanDate > Time.UnixTime;
            SecurityLevel = (AccountTypes)fields.Read<byte>(startColumn + 4);

            int hashPos = Name.IndexOf('#');
            if (hashPos != -1)
                DisplayName = "WoW" + Name.Substring(hashPos + 1);
            else
                DisplayName = Name;
        }

        public uint Id;
        public string Name;
        public string DisplayName;
        public uint UnbanDate;
        public bool IsBanned;
        public bool IsPermanenetlyBanned;
        public AccountTypes SecurityLevel;

        public Dictionary<uint /*realmAddress*/, byte> CharacterCounts = new Dictionary<uint, byte>();
        public Dictionary<string /*subRegion*/, LastPlayedCharacterInfo> LastPlayedCharacters = new Dictionary<string, LastPlayedCharacterInfo>();
    }

    public class LastPlayedCharacterInfo
    {
        public RealmHandle RealmId;
        public string CharacterName;
        public ulong CharacterGUID;
        public uint LastPlayedTime;
    }
}
