// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Bgs.Protocol;
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

namespace BNetServer.Networking
{
    partial class Session : SSLSocket
    {
        AccountInfo accountInfo;
        GameAccountInfo gameAccountInfo;

        string locale;
        string os;
        uint build;
        string ipCountry;

        byte[] clientSecret;
        bool authed;
        uint requestToken;

        AsyncCallbackProcessor<QueryCallback> queryProcessor;
        Dictionary<uint, Action<CodedInputStream>> responseCallbacks;

        public Session(Socket socket) : base(socket)
        {
            clientSecret = new byte[32];
            queryProcessor = new AsyncCallbackProcessor<QueryCallback>();
            responseCallbacks = new Dictionary<uint, Action<CodedInputStream>>();
        }

        public override void Accept()
        {
            string ipAddress = GetRemoteIpEndPoint().ToString();
            Log.outInfo(LogFilter.Network, $"{GetClientInfo()} Connection Accepted.");

            // Verify that this IP is not in the ip_banned table
            DB.Login.Execute(LoginDatabase.GetPreparedStatement(LoginStatements.DelExpiredIpBans));

            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SelIpInfo);
            stmt.AddValue(0, ipAddress);
            stmt.AddValue(1, BitConverter.ToUInt32(GetRemoteIpEndPoint().Address.GetAddressBytes(), 0));

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
                            ipCountry = result.Read<string>(1);

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

        public override bool Update()
        {
            if (!base.Update())
                return false;

            queryProcessor.ProcessReadyCallbacks();

            return true;
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

            ByteBuffer buffer = new();
            buffer.WriteBytes(GetHeaderSize(header), 2);
            buffer.WriteBytes(header.ToByteArray());

            await AsyncWrite(buffer.GetData());
        }

        public async void SendRequest(uint serviceHash, uint methodId, IMessage request)
        {
            Header header = new();
            header.ServiceId = 0;
            header.ServiceHash = serviceHash;
            header.MethodId = methodId;
            header.Size = (uint)request.CalculateSize();
            header.Token = requestToken++;

            ByteBuffer buffer = new();
            buffer.WriteBytes(GetHeaderSize(header), 2);
            buffer.WriteBytes(header.ToByteArray());
            buffer.WriteBytes(request.ToByteArray());

            await AsyncWrite(buffer.GetData());
        }

        public byte[] GetHeaderSize(Header header)
        {
            var size = (ushort)header.CalculateSize();
            byte[] bytes = new byte[2];
            bytes[0] = (byte)((size >> 8) & 0xff);
            bytes[1] = (byte)(size & 0xff);

            var headerSizeBytes = BitConverter.GetBytes((ushort)header.CalculateSize());
            Array.Reverse(headerSizeBytes);

            return bytes;
        }

        public string GetClientInfo()
        {
            string stream = '[' + GetRemoteIpEndPoint().ToString();
            if (accountInfo != null && !accountInfo.Login.IsEmpty())
                stream += ", Account: " + accountInfo.Login;

            if (gameAccountInfo != null)
                stream += ", Game account: " + gameAccountInfo.Name;

            stream += ']';

            return stream;
        }
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
        public uint UnbanDate;
        public bool IsBanned;
        public bool IsPermanenetlyBanned;
        public AccountTypes SecurityLevel;

        public Dictionary<uint, byte> CharacterCounts;
        public Dictionary<string, LastPlayedCharacterInfo> LastPlayedCharacters;

        public GameAccountInfo(SQLFields fields, int startColumn)
        {
            Id = fields.Read<uint>(startColumn + 0);
            Name = fields.Read<string>(startColumn + 1);
            UnbanDate = fields.Read<uint>(startColumn + 2);
            IsPermanenetlyBanned = fields.Read<uint>(startColumn + 3) != 0;
            IsBanned = IsPermanenetlyBanned || UnbanDate > Time.UnixTime;
            SecurityLevel = (AccountTypes)fields.Read<byte>(startColumn + 4);

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
