// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
using Framework.ClientBuild;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Framework.Networking;
using Framework.Realm;
using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BNetServer.Networking
{
    public partial class Session : SocketBase
    {
        uint _sessionId;
        DateTime _creationTime;
        string _clientInstanceId;

        AccountInfo _accountInfo;
        GameAccountInfo _gameAccountInfo;

        string _locale;
        string _os;
        uint _build;
        ClientBuildVariantId _buildVariant;
        TimeSpan _timezoneOffset;
        string _ipCountry;

        byte[] _clientSecret;
        bool _authed;
        uint _requestToken;

        AsyncCallbackProcessor<QueryCallback> queryProcessor;
        Dictionary<uint, Action<CodedInputStream>> responseCallbacks;

        public Session(Socket socket) : base(socket, true)
        {
            _sessionId = ++Global.SessionMgr.SessionIdGenerator;
            _creationTime = DateTime.Now;

            _clientSecret = new byte[32];
            queryProcessor = new AsyncCallbackProcessor<QueryCallback>();
            responseCallbacks = new Dictionary<uint, Action<CodedInputStream>>();
        }

        public override void Start()
        {
            string ipAddress = GetRemoteIpAddress().ToString();
            Log.outInfo(LogFilter.Network, $"{GetClientInfo()} Connection Accepted.");

            // Verify that this IP is not in the ip_banned table
            DB.Login.Execute(LoginDatabase.GetPreparedStatement(LoginStatements.DEL_EXPIRED_IP_BANS));

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_IP_INFO);
            stmt.AddValue(0, ipAddress);
            stmt.AddValue(1, BitConverter.ToUInt32(GetRemoteIpAddress().GetAddressBytes(), 0));

            queryProcessor.AddCallback(DB.Login.AsyncQuery(stmt).WithCallback(async result =>
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
                        Log.outDebug(LogFilter.Session, $"{GetClientInfo()} trying to login with banned ipaddress!");
                        CloseSocket();
                        return;
                    }
                }

                await AsyncHandshake(Global.LoginServiceMgr.GetCertificate());
            }));
        }

        public async override Task HandshakeHandler(Exception ex = null)
        {
            if (ex != null)
            {
                Log.outError(LogFilter.Session, $"{GetClientInfo()} SSL Handshake failed {ex.Message}");
                CloseSocket();
                return;
            }

            await AsyncRead();
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            queryProcessor.ProcessReadyCallbacks();

            return true;
        }

        public GameAccountInfo GetGameAccountInfo(uint gameAccountId)
        {
            if (_accountInfo == null)
                return null;

            return _accountInfo.GameAccounts.LookupByKey(gameAccountId);
        }

        public async override void ReadHandler(byte[] data, int receivedLength)
        {
            if (!IsOpen())
                return;

            int readPos = 0;
            while (readPos < receivedLength)
            {
                var headerLength = (ushort)IPAddress.HostToNetworkOrder(BitConverter.ToInt16(data, readPos));
                readPos += 2;

                Header header = new();
                header.MergeFrom(data, readPos, headerLength);
                readPos += headerLength;

                var stream = new CodedInputStream(data, readPos, (int)header.Size);
                readPos += (int)header.Size;

                if (header.ServiceId != 0xFE && header.ServiceHash != 0)
                {
                    var handler = Global.LoginServiceMgr.GetHandler(header.ServiceHash, header.MethodId);
                    if (handler != null)
                        handler.Invoke(this, header.Token, stream);
                    else
                    {
                        Log.outError(LogFilter.ServiceProtobuf, $"{GetClientInfo()} tried to call not implemented methodId: {header.MethodId} for servicehash: {header.ServiceHash}");
                        SendResponse(header.Token, BattlenetRpcErrorCode.RpcNotImplemented);
                    }
                }
                else
                {
                    var handler = responseCallbacks.LookupByKey(header.Token);
                    if (handler != null)
                    {
                        handler(stream);
                        responseCallbacks.Remove(header.Token);
                    }
                }
            }

            await AsyncRead();
        }

        public async void SendResponse(uint token, IMessage response)
        {
            Header header = new();
            header.Token = token;
            header.ServiceId = 0xFE;
            header.Size = (uint)response.CalculateSize();
            header.Ciid = _clientInstanceId;

            ByteBuffer buffer = new();
            buffer.WriteBytes(GetHeaderSize(header), 2);
            buffer.WriteBytes(header.ToByteArray());
            buffer.WriteBytes(response.ToByteArray());

            await AsyncWrite(buffer.GetData());
        }

        public async void SendResponse(uint token, BattlenetRpcErrorCode status)
        {
            Header header = new();
            header.Token = token;
            header.Status = (uint)status;
            header.ServiceId = 0xFE;
            header.Ciid = _clientInstanceId;

            ByteBuffer buffer = new();
            buffer.WriteBytes(GetHeaderSize(header), 2);
            buffer.WriteBytes(header.ToByteArray());

            await AsyncWrite(buffer.GetData());
        }

        public async void SendRequest(uint serviceHash, uint methodId, IMessage request, Action<CodedInputStream> callback)
        {
            responseCallbacks[_requestToken] = callback;
            SendRequest(serviceHash, methodId, request);
        }

        public async void SendRequest(uint serviceHash, uint methodId, IMessage request)
        {
            Header header = new();
            header.ServiceId = 0;
            header.ServiceHash = serviceHash;
            header.MethodId = methodId;
            header.Size = (uint)request.CalculateSize();
            header.Token = _requestToken++;
            header.Ciid = _clientInstanceId;

            ByteBuffer buffer = new();
            buffer.WriteBytes(GetHeaderSize(header), 2);
            buffer.WriteBytes(header.ToByteArray());
            buffer.WriteBytes(request.ToByteArray());

            await AsyncWrite(buffer.GetData());
        }

        void SetClientInfo(uint gameAccountId, ClientBuildVariantId buildVariant, byte[] clientSecret)
        {
            _gameAccountInfo = GetGameAccountInfo(gameAccountId);
            _buildVariant = buildVariant;
            _clientSecret = clientSecret;
        }

        LastPlayedCharacterInfo GetLastPlayedCharacter(string subRegion)
        {
            return _gameAccountInfo.LastPlayedCharacters.LookupByKey(subRegion);
        }

        public byte[] GetHeaderSize(Header header)
        {
            var headerSizeBytes = BitConverter.GetBytes((ushort)header.CalculateSize());
            Array.Reverse(headerSizeBytes);

            return headerSizeBytes;
        }

        public string GetClientInfo()
        {
            string stream = '[' + GetRemoteIpAddress().ToString();
            if (_accountInfo != null && !_accountInfo.Login.IsEmpty())
                stream += ", Account: " + _accountInfo.Login;

            if (_gameAccountInfo != null)
                stream += ", Game account: " + _gameAccountInfo.Name;

            stream += ']';

            return stream;
        }

        public bool IsAuthed() { return _authed; }
        uint GetAccountId() { return _accountInfo.Id; }
        AccountInfo GetAccountInfo() { return _accountInfo; }

        uint GetGameAccountId() { return _gameAccountInfo.Id; }
        GameAccountInfo GetGameAccountInfo() { return _gameAccountInfo; }

        string GetLocale() { return _locale; }
        string GetOS() { return _os; }
        uint GetBuild() { return _build; }
        ClientBuildVariantId GetBuildVariant() { return _buildVariant; }
        TimeSpan GetTimezoneOffset() { return _timezoneOffset; }
        byte[] GetClientSecret() { return _clientSecret; }

        public uint GetSessionId() { return _sessionId; }
        public DateTime GetCreationTime() { return _creationTime; }
        public void SetClientInstanceId(string ciid) { _clientInstanceId = ciid; }
    }

    public class AccountInfo
    {
        public uint Id;
        public string Login;
        public bool IsLockedToIP;
        public string LockCountry;
        public string LastIP;
        public uint LoginTicketExpiry;
        public bool IsBanned;
        public bool IsPermanenetlyBanned;

        public Dictionary<uint, GameAccountInfo> GameAccounts;

        public AccountInfo(SQLResult result)
        {
            Id = result.Read<uint>(0);
            Login = result.Read<string>(1);
            IsLockedToIP = result.Read<bool>(2);
            LockCountry = result.Read<string>(3);
            LastIP = result.Read<string>(4);
            LoginTicketExpiry = result.Read<uint>(5);
            IsBanned = result.Read<ulong>(6) != 0;
            IsPermanenetlyBanned = result.Read<ulong>(7) != 0;

            GameAccounts = new Dictionary<uint, GameAccountInfo>();
            const int GameAccountFieldsOffset = 8;
            do
            {
                var account = new GameAccountInfo(result.GetFields(), GameAccountFieldsOffset);
                GameAccounts[result.Read<uint>(GameAccountFieldsOffset)] = account;

            } while (result.NextRow());
        }
    }

    public class GameAccountInfo
    {
        public uint Id;
        public string Name;
        public string DisplayName;
        public long BanDate;
        public long UnbanDate;
        public bool IsBanned;
        public bool IsPermanenetlyBanned;
        public AccountTypes SecurityLevel;

        public Dictionary<uint, byte> CharacterCounts;
        public Dictionary<string, LastPlayedCharacterInfo> LastPlayedCharacters;

        public GameAccountInfo(SQLFields fields, int startColumn)
        {
            Id = fields.Read<uint>(startColumn + 0);
            Name = fields.Read<string>(startColumn + 1);
            BanDate = fields.Read<uint>(startColumn + 2);
            UnbanDate = fields.Read<uint>(startColumn + 3);
            IsPermanenetlyBanned = fields.Read<uint>(startColumn + 4) != 0;
            IsBanned = IsPermanenetlyBanned || UnbanDate > Time.UnixTime;
            SecurityLevel = (AccountTypes)fields.Read<byte>(startColumn + 5);

            int hashPos = Name.IndexOf('#');
            if (hashPos != -1)
                DisplayName = "WoW" + Name[(hashPos + 1)..];
            else
                DisplayName = Name;

            CharacterCounts = new Dictionary<uint, byte>();
            LastPlayedCharacters = new Dictionary<string, LastPlayedCharacterInfo>();
        }
    }

    public class LastPlayedCharacterInfo
    {
        public RealmId RealmId;
        public string CharacterName;
        public ulong CharacterGUID;
        public uint LastPlayedTime;
    }
}