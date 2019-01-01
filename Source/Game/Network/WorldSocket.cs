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

using Framework.Constants;
using Framework.Cryptography;
using Framework.Database;
using Framework.IO;
using Framework.Networking;
using Game.Network.Packets;
using System;
using System.IO;
using System.Net.Sockets;

namespace Game.Network
{
    public class WorldSocket : SocketBase
    {
        static string ClientConnectionInitialize = "WORLD OF WARCRAFT CONNECTION - CLIENT TO SERVER";
        static string ServerConnectionInitialize = "WORLD OF WARCRAFT CONNECTION - SERVER TO CLIENT";

        static byte[] AuthCheckSeed = { 0xC5, 0xC6, 0x98, 0x95, 0x76, 0x3F, 0x1D, 0xCD, 0xB6, 0xA1, 0x37, 0x28, 0xB3, 0x12, 0xFF, 0x8A };
        static byte[] SessionKeySeed = { 0x58, 0xCB, 0xCF, 0x40, 0xFE, 0x2E, 0xCE, 0xA6, 0x5A, 0x90, 0xB8, 0x01, 0x68, 0x6C, 0x28, 0x0B };
        static byte[] ContinuedSessionSeed = { 0x16, 0xAD, 0x0C, 0xD4, 0x46, 0xF9, 0x4F, 0xB2, 0xEF, 0x7D, 0xEA, 0x2A, 0x17, 0x66, 0x4D, 0x2F };

        static byte[] ClientTypeSeed_Wn64 = { 0xDD, 0x62, 0x65, 0x17, 0xCC, 0x6D, 0x31, 0x93, 0x2B, 0x47, 0x99, 0x34, 0xCC, 0xDC, 0x0A, 0xBF };
        static byte[] ClientTypeSeed_Mc64 = { 0x34, 0x1C, 0xFE, 0xFE, 0x3D, 0x72, 0xAC, 0xA9, 0xA4, 0x40, 0x7D, 0xC5, 0x35, 0xDE, 0xD6, 0x6A };

        public WorldSocket(Socket socket) : base(socket)
        {
            _connectType = ConnectionType.Realm;
            _serverChallenge = new byte[0].GenerateRandomKey(16);
            worldCrypt = new WorldCrypt();
        }

        public override void Dispose()
        {
            _worldSession = null;
            _queryProcessor = null;
            _serverChallenge = null;
            worldCrypt = null;
            _encryptSeed = null;
            _decryptSeed = null;
            _sessionKey = null;
            _compressionStream = null;

            base.Dispose();
        }

        public override void Start()
        {
            string ip_address = GetRemoteIpAddress().ToString();

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

                    _ipCountry = result.Read<string>(1);

                } while (result.NextRow());

                if (banned)
                {
                    Log.outError(LogFilter.Network, "WorldSocket.Connect: Sent Auth Response (IP {0} banned).", GetRemoteIpAddress().ToString());
                    CloseSocket();
                    return;
                }
            }

            ByteBuffer packet = new ByteBuffer();
            packet.WriteString(ServerConnectionInitialize);
            packet.WriteString("\n");
            AsyncWrite(packet.GetData());

            AsyncReadWithCallback(InitializeHandler);
        }

        public void InitializeHandler(SocketAsyncEventArgs args)
        {
            if (args.SocketError != SocketError.Success)
            {
                CloseSocket();
                return;
            }

            if (args.BytesTransferred > 0)
            {
                ByteBuffer connBuffer = new ByteBuffer(GetReceiveBuffer());

                string initializer = connBuffer.ReadString((uint)ClientConnectionInitialize.Length);
                if (initializer != ClientConnectionInitialize)
                {
                    CloseSocket();
                    return;
                }

                // Initialize the zlib stream
                _compressionStream = new ZLib.z_stream();

                // Initialize the deflate algo...
                var z_res1 = ZLib.deflateInit2(_compressionStream, 1, 8, -15, 8, 0);
                if (z_res1 != 0)
                {
                    CloseSocket();
                    Log.outError(LogFilter.Network, "Can't initialize packet compression (zlib: deflateInit2_) Error code: {0}", z_res1);
                    return;
                }

                HandleSendAuthSession();
                AsyncRead();
                return;
            }

            AsyncReadWithCallback(InitializeHandler);
        }

        public override void ReadHandler(int transferredBytes)
        {
            if (!IsOpen())
                return;

            while (transferredBytes > 5)
            {
                if (worldCrypt.IsInitialized)
                    worldCrypt.Decrypt(GetReceiveBuffer(), 4);

                int size;
                uint opcode;
                if (!ReadHeader(out opcode, out size))
                {
                    CloseSocket();
                    return;
                }

                var data = new byte[size];
                Buffer.BlockCopy(GetReceiveBuffer(), 6, data, 0, size);

                PacketLog.Write(data, opcode, GetRemoteIpAddress(), GetRemotePort(), _connectType, true);
                if (!ProcessPacket(new WorldPacket(data, opcode)))
                {
                    CloseSocket();
                    return;
                }

                transferredBytes -= size + 6;
                Buffer.BlockCopy(GetReceiveBuffer(), size + 6, GetReceiveBuffer(), 0, transferredBytes);
            }

            AsyncRead();
        }

        bool ReadHeader(out uint opcode, out int size)
        {
            size = BitConverter.ToInt32(GetReceiveBuffer(), 0) - 2;
            opcode = BitConverter.ToUInt16(GetReceiveBuffer(), 4);

            if (size >= 0x10000 || (opcode >= (int)ClientOpcodes.Max + 1))
            {
                Log.outError(LogFilter.Network, "WorldSocket.ReadHeader(): client {0} sent malformed packet (size: {1}, cmd: {2})", GetRemoteIpAddress().ToString(), size, opcode);
                return false;
            }

            return true;
        }

        bool ProcessPacket(WorldPacket packet)
        {
            ClientOpcodes opcode = (ClientOpcodes)packet.GetOpcode();

            try
            {
                switch (opcode)
                {
                    case ClientOpcodes.Ping:
                        Ping ping = new Ping(packet);
                        ping.Read();
                        return HandlePing(ping);
                    case ClientOpcodes.AuthSession:
                        if (_worldSession != null)
                        {
                            Log.outError(LogFilter.Network, "WorldSocket.ProcessPacket: received duplicate CMSG_AUTH_SESSION from {0}", _worldSession.GetPlayerInfo());
                            return false;
                        }

                        AuthSession authSession = new AuthSession(packet);
                        authSession.Read();
                        HandleAuthSession(authSession);
                        break;
                    case ClientOpcodes.AuthContinuedSession:
                        if (_worldSession != null)
                        {
                            Log.outError(LogFilter.Network, "WorldSocket.ProcessPacket: received duplicate CMSG_AUTH_CONTINUED_SESSION from {0}", _worldSession.GetPlayerInfo());
                            return false;
                        }

                        AuthContinuedSession authContinuedSession = new AuthContinuedSession(packet);
                        authContinuedSession.Read();
                        HandleAuthContinuedSession(authContinuedSession);
                        break;
                    case ClientOpcodes.LogDisconnect:
                        break;
                    case ClientOpcodes.EnableNagle:
                        Log.outDebug(LogFilter.Network, "Client {0} requested enabling nagle algorithm", GetRemoteIpAddress().ToString());
                        SetNoDelay(false);
                        break;
                    case ClientOpcodes.ConnectToFailed:
                        ConnectToFailed connectToFailed = new ConnectToFailed(packet);
                        connectToFailed.Read();
                        HandleConnectToFailed(connectToFailed);
                        break;
                    case ClientOpcodes.EnableEncryptionAck:
                        HandleEnableEncryptionAck();
                        break;
                    default:
                        if (_worldSession == null)
                        {
                            Log.outError(LogFilter.Network, "ProcessIncoming: Client not authed opcode = {0}", opcode);
                            return false;
                        }

                        if (!PacketManager.ContainsHandler(opcode))
                        {
                            Log.outError(LogFilter.Network, "No defined handler for opcode {0} sent by {1}", opcode, _worldSession.GetPlayerInfo());
                            break;
                        }

                        // Our Idle timer will reset on any non PING opcodes.
                        // Catches people idling on the login screen and any lingering ingame connections.
                        _worldSession.ResetTimeOutTime();
                        _worldSession.QueuePacket(packet);
                        break;
                }
            }
            catch (IOException)
            {
                Log.outError(LogFilter.Network, "WorldSocket.ProcessPacket(): client {0} sent malformed {1}", GetRemoteIpAddress().ToString(), opcode);
                return false;
            }

            return true;
        }

        public void SendPacket(ServerPacket packet)
        {
            if (!IsOpen())
                return;

            packet.LogPacket(_worldSession);
            packet.WritePacketData();

            var data = packet.GetData();
            ServerOpcodes opcode = packet.GetOpcode();
            PacketLog.Write(data, (uint)opcode, GetRemoteIpAddress(), GetRemotePort(), _connectType, false);

            uint packetSize = (uint)data.Length;
            if (packetSize > 0x400 && worldCrypt.IsInitialized)
            {
                ByteBuffer buffer = new ByteBuffer();
                buffer.WriteUInt32(packetSize + 2);
                buffer.WriteUInt32(ZLib.adler32(ZLib.adler32(0x9827D8F1, BitConverter.GetBytes((ushort)opcode), 2), data, packetSize));

                uint compressedSize = CompressPacket(data, opcode);
                buffer.WriteUInt32(ZLib.adler32(0x9827D8F1, data, compressedSize)); 
                buffer.WriteBytes(data, compressedSize);

                packetSize = (ushort)(compressedSize + 12);
                opcode = ServerOpcodes.CompressedPacket;

                data = buffer.GetData();
            }

            ServerPacketHeader header = new ServerPacketHeader(packetSize, opcode);
            if (worldCrypt.IsInitialized)
                worldCrypt.Encrypt(header.data, 4);

            AsyncWrite(header.data.Combine(data));
        }

        public void SetWorldSession(WorldSession session)
        {
            _worldSession = session;
        }

        public uint CompressPacket(byte[] data, ServerOpcodes opcode)
        {
            byte[] uncompressedData = BitConverter.GetBytes((ushort)opcode).Combine(data);

            uint bufferSize = ZLib.deflateBound(_compressionStream, (uint)uncompressedData.Length);
            byte[] outPrt = new byte[bufferSize];

            _compressionStream.next_out = 0;
            _compressionStream.avail_out = bufferSize;
            _compressionStream.out_buf = outPrt;

            _compressionStream.next_in = 0;
            _compressionStream.avail_in = (uint)uncompressedData.Length;
            _compressionStream.in_buf = uncompressedData;

            int z_res = ZLib.deflate(_compressionStream, 2);
            if (z_res != 0)
            {
                Log.outError(LogFilter.Network, "Can't compress packet data (zlib: deflate) Error code: {0} msg: {1}", z_res, _compressionStream.msg);
                return 0;
            }

            uint compressedSize = bufferSize - _compressionStream.avail_out;
            Buffer.BlockCopy(outPrt, 0, data, 0, (int)compressedSize);
            return compressedSize;
        }

        public override bool Update()
        {
            if (!base.Update())
                return false;

            _queryProcessor.ProcessReadyQueries();

            return true;
        }

        void HandleSendAuthSession()
        {
            _encryptSeed = new byte[16].GenerateRandomKey(16);
            _decryptSeed = new byte[16].GenerateRandomKey(16);

            AuthChallenge challenge = new AuthChallenge();
            challenge.Challenge = _serverChallenge;
            challenge.DosChallenge = _encryptSeed.Combine(_decryptSeed);
            challenge.DosZeroBits = 1;

            SendPacket(challenge);
        }

        void HandleAuthSession(AuthSession authSession)
        {
            // Get the account information from the realmd database
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_INFO_BY_NAME);
            stmt.AddValue(0, Global.WorldMgr.GetRealm().Id.Realm);
            stmt.AddValue(1, authSession.RealmJoinTicket);

            _queryProcessor.AddQuery(DB.Login.AsyncQuery(stmt).WithCallback(HandleAuthSessionCallback, authSession));
        }

        void HandleAuthSessionCallback(AuthSession authSession, SQLResult result)
        {
            // Stop if the account is not found
            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Network, "HandleAuthSession: Sent Auth Response (unknown account).");
                CloseSocket();
                return;
            }

            AccountInfo account = new AccountInfo(result.GetFields());

            // For hook purposes, we get Remoteaddress at this point.
            string address = GetRemoteIpAddress().ToString();

            Sha256 digestKeyHash = new Sha256();
            digestKeyHash.Process(account.game.SessionKey, account.game.SessionKey.Length);
            if (account.game.OS == "Wn64")
                digestKeyHash.Finish(ClientTypeSeed_Wn64);
            else if (account.game.OS == "Mc64")
                digestKeyHash.Finish(ClientTypeSeed_Mc64);
            else
            {
                Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Authentication failed for account: {0} ('{1}') address: {2}", account.game.Id, authSession.RealmJoinTicket, address);
                CloseSocket();
                return;
            }

            HmacSha256 hmac = new HmacSha256(digestKeyHash.Digest);
            hmac.Process(authSession.LocalChallenge, authSession.LocalChallenge.Count);
            hmac.Process(_serverChallenge, 16);
            hmac.Finish(AuthCheckSeed, 16);

            // Check that Key and account name are the same on client and server
            if (!hmac.Digest.Compare(authSession.Digest))
            {
                Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Authentication failed for account: {0} ('{1}') address: {2}", account.game.Id, authSession.RealmJoinTicket, address);
                CloseSocket();
                return;
            }


            Sha256 keyData = new Sha256();
            keyData.Finish(account.game.SessionKey);

            HmacSha256 sessionKeyHmac = new HmacSha256(keyData.Digest);
            sessionKeyHmac.Process(_serverChallenge, 16);
            sessionKeyHmac.Process(authSession.LocalChallenge, authSession.LocalChallenge.Count);
            sessionKeyHmac.Finish(SessionKeySeed, 16);

            _sessionKey = new byte[40];
            var sessionKeyGenerator = new SessionKeyGenerator(sessionKeyHmac.Digest, 32);
            sessionKeyGenerator.Generate(_sessionKey, 40);

            // As we don't know if attempted login process by ip works, we update last_attempt_ip right away
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LAST_ATTEMPT_IP);
            stmt.AddValue(0, address);
            stmt.AddValue(1, authSession.RealmJoinTicket);

            DB.Login.Execute(stmt);
            // This also allows to check for possible "hack" attempts on account

            stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_ACCOUNT_INFO_CONTINUED_SESSION);
            stmt.AddValue(0, _sessionKey.ToHexString());
            stmt.AddValue(1, account.game.Id);
            DB.Login.Execute(stmt);

            // First reject the connection if packet contains invalid data or realm state doesn't allow logging in
            if (Global.WorldMgr.IsClosed())
            {
                SendAuthResponseError(BattlenetRpcErrorCode.Denied);
                Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: World closed, denying client ({0}).", GetRemoteIpAddress());
                CloseSocket();
                return;
            }

            if (authSession.RealmID != Global.WorldMgr.GetRealm().Id.Realm)
            {
                SendAuthResponseError(BattlenetRpcErrorCode.Denied);
                Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Client {0} requested connecting with realm id {1} but this realm has id {2} set in config.",
                    GetRemoteIpAddress().ToString(), authSession.RealmID, Global.WorldMgr.GetRealm().Id.Realm);
                CloseSocket();
                return;
            }

            // Must be done before WorldSession is created
            bool wardenActive = WorldConfig.GetBoolValue(WorldCfg.WardenEnabled);
            if (wardenActive && account.game.OS != "Win" && account.game.OS != "Wn64" && account.game.OS != "Mc64")
            {
                SendAuthResponseError(BattlenetRpcErrorCode.Denied);
                Log.outError(LogFilter.Network, "WorldSocket.HandleAuthSession: Client {0} attempted to log in using invalid client OS ({1}).", address, account.game.OS);
                CloseSocket();
                return;
            }

            //Re-check ip locking (same check as in auth).
            if (account.battleNet.IsLockedToIP) // if ip is locked
            {
                if (account.battleNet.LastIP != address)
                {
                    SendAuthResponseError(BattlenetRpcErrorCode.RiskAccountLocked);
                    Log.outDebug(LogFilter.Network, "HandleAuthSession: Sent Auth Response (Account IP differs).");
                    CloseSocket();
                    return;
                }
            }
            else if (!account.battleNet.LockCountry.IsEmpty() && account.battleNet.LockCountry != "00" && !_ipCountry.IsEmpty())
            {
                if (account.battleNet.LockCountry != _ipCountry)
                {
                    SendAuthResponseError(BattlenetRpcErrorCode.RiskAccountLocked);
                    Log.outDebug(LogFilter.Network, "WorldSocket.HandleAuthSession: Sent Auth Response (Account country differs. Original country: {0}, new country: {1}).", account.battleNet.LockCountry, _ipCountry);
                    CloseSocket();
                    return;
                }
            }

            long mutetime = account.game.MuteTime;
            //! Negative mutetime indicates amount of seconds to be muted effective on next login - which is now.
            if (mutetime < 0)
            {
                mutetime = Time.UnixTime + mutetime;

                stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_MUTE_TIME_LOGIN);
                stmt.AddValue(0, mutetime);
                stmt.AddValue(1, account.game.Id);
                DB.Login.Execute(stmt);
            }

            if (account.IsBanned()) // if account banned
            {
                SendAuthResponseError(BattlenetRpcErrorCode.GameAccountBanned);
                Log.outError(LogFilter.Network, "WorldSocket:HandleAuthSession: Sent Auth Response (Account banned).");
                CloseSocket();
                return;
            }

            // Check locked state for server
            AccountTypes allowedAccountType = Global.WorldMgr.GetPlayerSecurityLimit();
            if (allowedAccountType > AccountTypes.Player && account.game.Security < allowedAccountType)
            {
                SendAuthResponseError(BattlenetRpcErrorCode.ServerIsPrivate);
                Log.outInfo(LogFilter.Network, "WorldSocket:HandleAuthSession: User tries to login but his security level is not enough");
                CloseSocket();
                return;
            }

            Log.outDebug(LogFilter.Network, "WorldSocket:HandleAuthSession: Client '{0}' authenticated successfully from {1}.", authSession.RealmJoinTicket, address);

            // Update the last_ip in the database
            stmt = DB.Login.GetPreparedStatement(LoginStatements.UPD_LAST_IP);
            stmt.AddValue(0, address);
            stmt.AddValue(1, authSession.RealmJoinTicket);
            DB.Login.Execute(stmt);

            _worldSession = new WorldSession(account.game.Id, authSession.RealmJoinTicket, account.battleNet.Id, this, account.game.Security, (Expansion)account.game.Expansion,
                mutetime, account.game.OS, account.battleNet.Locale, account.game.Recruiter, account.game.IsRectuiter);

            // Initialize Warden system only if it is enabled by config
            //if (wardenActive)
            //_worldSession.InitWarden(_sessionKey);

            _queryProcessor.AddQuery(_worldSession.LoadPermissionsAsync().WithCallback(LoadSessionPermissionsCallback));
        }

        void LoadSessionPermissionsCallback(SQLResult result)
        {
            // RBAC must be loaded before adding session to check for skip queue permission
            _worldSession.GetRBACData().LoadFromDBCallback(result);

            SendPacket(new EnableEncryption());
        }

        void HandleAuthContinuedSession(AuthContinuedSession authSession)
        {
            ConnectToKey key = new ConnectToKey();
            _key = key.Raw = authSession.Key;

            _connectType = key.connectionType;
            if (_connectType != ConnectionType.Instance)
            {
                SendAuthResponseError(BattlenetRpcErrorCode.Denied);
                CloseSocket();
                return;
            }

            uint accountId = key.AccountId;
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_INFO_CONTINUED_SESSION);
            stmt.AddValue(0, accountId);

            _queryProcessor.AddQuery(DB.Login.AsyncQuery(stmt).WithCallback(HandleAuthContinuedSessionCallback, authSession));
        }

        void HandleAuthContinuedSessionCallback(AuthContinuedSession authSession, SQLResult result)
        {
            if (result.IsEmpty())
            {
                SendAuthResponseError(BattlenetRpcErrorCode.Denied);
                CloseSocket();
                return;
            }

            ConnectToKey key = new ConnectToKey();
            _key = key.Raw = authSession.Key;

            uint accountId = key.AccountId;
            string login = result.Read<string>(0);
            _sessionKey = result.Read<string>(1).ToByteArray();

            HmacSha256 hmac = new HmacSha256(_sessionKey);
            hmac.Process(BitConverter.GetBytes(authSession.Key), 8);
            hmac.Process(authSession.LocalChallenge, authSession.LocalChallenge.Length);
            hmac.Process(_serverChallenge, 16);
            hmac.Finish(ContinuedSessionSeed, 16);

            if (!hmac.Digest.Compare(authSession.Digest))
            {
                Log.outError(LogFilter.Network, "WorldSocket.HandleAuthContinuedSession: Authentication failed for account: {0} ('{1}') address: {2}", accountId, login, GetRemoteIpAddress());
                CloseSocket();
                return;
            }

            SendPacket(new EnableEncryption());
        }

        void HandleConnectToFailed(ConnectToFailed connectToFailed)
        {
            if (_worldSession != null)
            {
                if (_worldSession.PlayerLoading())
                {
                    switch (connectToFailed.Serial)
                    {
                        case ConnectToSerial.WorldAttempt1:
                            _worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt2);
                            break;
                        case ConnectToSerial.WorldAttempt2:
                            _worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt3);
                            break;
                        case ConnectToSerial.WorldAttempt3:
                            _worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt4);
                            break;
                        case ConnectToSerial.WorldAttempt4:
                            _worldSession.SendConnectToInstance(ConnectToSerial.WorldAttempt5);
                            break;
                        case ConnectToSerial.WorldAttempt5:
                            {
                                Log.outError(LogFilter.Network, "{0} failed to connect 5 times to world socket, aborting login", _worldSession.GetPlayerInfo());
                                _worldSession.AbortLogin(LoginFailureReason.NoWorld);
                                break;
                            }
                        default:
                            return;
                    }
                }
                //else
                //{
                //    transfer_aborted when/if we get map node redirection
                //    SendPacket(*WorldPackets.Auth.ResumeComms());
                //}
            }

        }

        void HandleEnableEncryptionAck()
        {
            if (_connectType == ConnectionType.Realm)
            {
                worldCrypt.Initialize(_sessionKey);
                Global.WorldMgr.AddSession(_worldSession);
            }
            else
            {
                worldCrypt.Initialize(_sessionKey, _encryptSeed, _decryptSeed);
                Global.WorldMgr.AddInstanceSocket(this, _key);
            }
        }

        public void SendAuthResponseError(BattlenetRpcErrorCode code)
        {
            AuthResponse response = new AuthResponse();
            response.SuccessInfo.HasValue = false;
            response.WaitInfo.HasValue = false;
            response.Result = code;
            SendPacket(response);
        }

        bool HandlePing(Ping ping)
        {
            if (_LastPingTime == 0)
                _LastPingTime = Time.UnixTime; // for 1st ping
            else
            {
                long now = Time.UnixTime;
                long diff = now - _LastPingTime;
                _LastPingTime = now;

                if (diff < 27)
                {
                    ++_OverSpeedPings;

                    uint maxAllowed = WorldConfig.GetUIntValue(WorldCfg.MaxOverspeedPings);
                    if (maxAllowed != 0 && _OverSpeedPings > maxAllowed)
                    {
                        if (_worldSession != null && !_worldSession.HasPermission(RBACPermissions.SkipCheckOverspeedPing))
                        {
                            Log.outError(LogFilter.Network, "WorldSocket:HandlePing: {0} kicked for over-speed pings (address: {1})", _worldSession.GetPlayerInfo(), GetRemoteIpAddress());
                            //return ReadDataHandlerResult.Error;
                        }
                    }
                }
                else
                    _OverSpeedPings = 0;
            }

            if (_worldSession != null)
            {
                _worldSession.SetLatency(ping.Latency);
                _worldSession.ResetClientTimeDelay();
            }
            else
            {
                Log.outError(LogFilter.Network, "WorldSocket:HandlePing: peer sent CMSG_PING, but is not authenticated or got recently kicked, address = {0}", GetRemoteIpAddress());
                return false;
            }

            SendPacket(new Pong(ping.Serial));
            return true;
        }

        ConnectionType _connectType;
        ulong _key;

        byte[] _serverChallenge;
        WorldCrypt worldCrypt;
        byte[] _encryptSeed;
        byte[] _decryptSeed;
        byte[] _sessionKey;

        long _LastPingTime;
        uint _OverSpeedPings;

        WorldSession _worldSession;

        ZLib.z_stream _compressionStream;

        QueryCallbackProcessor _queryProcessor = new QueryCallbackProcessor();
        string _ipCountry;
    }

    class AccountInfo
    {
        public AccountInfo(SQLFields fields)
        {
            //         0             1           2          3                4            5           6          7            8      9     10          11
            // SELECT a.id, a.sessionkey, ba.last_ip, ba.locked, ba.lock_country, a.expansion, a.mutetime, ba.locale, a.recruiter, a.os, ba.id, aa.gmLevel,
            //                                                              12                                                            13    14
            // bab.unbandate > UNIX_TIMESTAMP() OR bab.unbandate = bab.bandate, ab.unbandate > UNIX_TIMESTAMP() OR ab.unbandate = ab.bandate, r.id
            // FROM account a LEFT JOIN battlenet_accounts ba ON a.battlenet_account = ba.id LEFT JOIN account_access aa ON a.id = aa.id AND aa.RealmID IN (-1, ?)
            // LEFT JOIN battlenet_account_bans bab ON ba.id = bab.id LEFT JOIN account_banned ab ON a.id = ab.id LEFT JOIN account r ON a.id = r.recruiter
            // WHERE a.username = ? ORDER BY aa.RealmID DESC LIMIT 1
            game.Id = fields.Read<uint>(0);
            game.SessionKey = fields.Read<string>(1).ToByteArray();
            battleNet.LastIP = fields.Read<string>(2);
            battleNet.IsLockedToIP = fields.Read<bool>(3);
            battleNet.LockCountry = fields.Read<string>(4);
            game.Expansion = fields.Read<byte>(5);
            game.MuteTime = fields.Read<long>(6);
            battleNet.Locale = (LocaleConstant)fields.Read<byte>(7);
            game.Recruiter = fields.Read<uint>(8);
            game.OS = fields.Read<string>(9);
            battleNet.Id = fields.Read<uint>(10);
            game.Security = (AccountTypes)fields.Read<byte>(11);
            battleNet.IsBanned = fields.Read<uint>(12) != 0;
            game.IsBanned = fields.Read<uint>(13) != 0;
            game.IsRectuiter = fields.Read<uint>(14) != 0;

            if (battleNet.Locale >= LocaleConstant.Total)
                battleNet.Locale = LocaleConstant.enUS;
        }

        public bool IsBanned() { return battleNet.IsBanned || game.IsBanned; }

        public BattleNet battleNet;
        public Game game;

        public struct BattleNet
        {
            public uint Id;
            public bool IsLockedToIP;
            public string LastIP;
            public string LockCountry;
            public LocaleConstant Locale;
            public bool IsBanned;
        }

        public struct Game
        {
            public uint Id;
            public byte[] SessionKey;
            public byte Expansion;
            public long MuteTime;
            public string OS;
            public uint Recruiter;
            public bool IsRectuiter;
            public AccountTypes Security;
            public bool IsBanned;
        }
    }
}
