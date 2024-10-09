// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.Realm;
using Game.Accounts;
using Game.BattleGrounds;
using Game.BattlePets;
using Game.Chat;
using Game.Entities;
using Game.Guilds;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;

namespace Game
{
    public partial class WorldSession : IDisposable
    {
        public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, long mute_time, string os, TimeSpan timezoneOffset, uint build, Framework.ClientBuild.ClientBuildVariantId clientBuildVariant, Locale locale, uint recruiter, bool isARecruiter)
        {
            m_muteTime = mute_time;
            AntiDOS = new DosProtection(this);
            m_Socket[(int)ConnectionType.Realm] = sock;
            _security = sec;
            _accountId = id;
            _accountName = name;
            _battlenetAccountId = battlenetAccountId;
            m_accountExpansion = expansion;
            m_expansion = (Expansion)Math.Min((byte)expansion, WorldConfig.GetIntValue(WorldCfg.Expansion));
            _os = os;
            _clientBuild = build;
            _clientBuildVariant = clientBuildVariant;
            m_sessionDbcLocale = Global.WorldMgr.GetAvailableDbcLocale(locale);
            m_sessionDbLocaleIndex = locale;
            _timezoneOffset = timezoneOffset;
            recruiterId = recruiter;
            isRecruiter = isARecruiter;
            expireTime = 60000; // 1 min after socket loss, session is deleted
            _battlePetMgr = new BattlePetMgr(this);
            _collectionMgr = new CollectionMgr(this);

            m_Address = sock.GetRemoteIpAddress().ToString();
            ResetTimeOutTime(false);
            DB.Login.Execute("UPDATE account SET online = 1 WHERE id = {0};", GetAccountId());     // One-time query
        }

        public void Dispose()
        {
            // unload player if not unloaded
            if (_player != null)
                LogoutPlayer(true);

            // - If have unclosed socket, close it
            for (byte i = 0; i < 2; ++i)
            {
                if (m_Socket[i] != null)
                {
                    m_Socket[i].CloseSocket();
                    m_Socket[i] = null;
                }
            }

            // empty incoming packet queue
            _recvQueue.Clear();

            DB.Login.Execute("UPDATE account SET online = 0 WHERE id = {0};", GetAccountId());     // One-time query
        }

        public void LogoutPlayer(bool save)
        {
            if (m_playerLogout)
                return;

            // finish pending transfers before starting the logout
            while (_player != null && _player.IsBeingTeleportedFar())
                HandleMoveWorldportAck();

            m_playerLogout = true;
            m_playerSave = save;

            if (_player != null)
            {
                if (!_player.GetLootGUID().IsEmpty())
                    DoLootReleaseAll();

                // If the player just died before logging out, make him appear as a ghost
                //FIXME: logout must be delayed in case lost connection with client in time of combat
                if (GetPlayer().GetDeathTimer() != 0)
                {
                    _player.CombatStop();
                    _player.BuildPlayerRepop();
                    _player.RepopAtGraveyard();
                }
                else if (GetPlayer().HasAuraType(AuraType.SpiritOfRedemption))
                {
                    // this will kill character by SPELL_AURA_SPIRIT_OF_REDEMPTION
                    _player.RemoveAurasByType(AuraType.ModShapeshift);
                    _player.KillPlayer();
                    _player.BuildPlayerRepop();
                    _player.RepopAtGraveyard();
                }
                else if (GetPlayer().HasPendingBind())
                {
                    _player.RepopAtGraveyard();
                    _player.SetPendingBind(0, 0);
                }

                //drop a flag if player is carrying it
                Battleground bg = GetPlayer().GetBattleground();
                if (bg != null)
                    bg.EventPlayerLoggedOut(GetPlayer());

                // Teleport to home if the player is in an invalid instance
                if (!_player.m_InstanceValid && !_player.IsGameMaster())
                    _player.TeleportTo(_player.GetHomebind());

                Global.OutdoorPvPMgr.HandlePlayerLeaveZone(_player, _player.GetZoneId());

                for (uint i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                {
                    BattlegroundQueueTypeId bgQueueTypeId = _player.GetBattlegroundQueueTypeId(i);
                    if (bgQueueTypeId != default)
                    {
                        _player.RemoveBattlegroundQueueId(bgQueueTypeId);
                        BattlegroundQueue queue = Global.BattlegroundMgr.GetBattlegroundQueue(bgQueueTypeId);
                        queue.RemovePlayer(_player.GetGUID(), true);
                    }
                }

                // Repop at GraveYard or other player far teleport will prevent saving player because of not present map
                // Teleport player immediately for correct player save
                while (_player.IsBeingTeleportedFar())
                    HandleMoveWorldportAck();

                // If the player is in a guild, update the guild roster and broadcast a logout message to other guild members
                Guild guild = Global.GuildMgr.GetGuildById(_player.GetGuildId());
                if (guild != null)
                    guild.HandleMemberLogout(this);

                // Remove pet
                _player.RemovePet(null, PetSaveMode.AsCurrent, true);

                ///- Release battle pet journal lock
                if (_battlePetMgr.HasJournalLock())
                    _battlePetMgr.ToggleJournalLock(false);

                // Clear whisper whitelist
                _player.ClearWhisperWhiteList();

                _player.FailQuestsWithFlag(QuestFlags.FailOnLogout);

                // empty buyback items and save the player in the database
                // some save parts only correctly work in case player present in map/player_lists (pets, etc)
                if (save)
                {
                    for (uint j = InventorySlots.BuyBackStart; j < InventorySlots.BuyBackEnd; ++j)
                    {
                        uint eslot = j - InventorySlots.BuyBackStart;
                        _player.SetInvSlot(j, ObjectGuid.Empty);
                        _player.SetBuybackPrice(eslot, 0);
                        _player.SetBuybackTimestamp(eslot, 0);
                    }
                    _player.SaveToDB();
                }

                // Leave all channels before player delete...
                _player.CleanupChannels();

                // If the player is in a group (or invited), remove him. If the group if then only 1 person, disband the group.
                _player.UninviteFromGroup();

                //! Send update to group and reset stored max enchanting level
                var group = _player.GetGroup();
                if (group != null)
                {
                    group.SendUpdate();
                    if (group.GetLeaderGUID() == _player.GetGUID())
                        group.StartLeaderOfflineTimer();
                }

                //! Broadcast a logout message to the player's friends
                Global.SocialMgr.SendFriendStatus(_player, FriendsResult.Offline, _player.GetGUID(), true);
                _player.RemoveSocial();

                //! Call script hook before deletion
                Global.ScriptMgr.OnPlayerLogout(GetPlayer());

                //! Remove the player from the world
                // the player may not be in the world when logging out
                // e.g if he got disconnected during a transfer to another map
                // calls to GetMap in this case may cause crashes
                _player.SetDestroyedObject(true);
                _player.CleanupsBeforeDelete();
                Log.outInfo(LogFilter.Player, $"Account: {GetAccountId()} (IP: {GetRemoteAddress()}) Logout Character:[{_player.GetName()}] ({_player.GetGUID()}) Level: {_player.GetLevel()}, XP: {_player.GetXP()}/{_player.GetXPForNextLevel()} ({_player.GetXPForNextLevel() - _player.GetXP()} left)");

                Map map = GetPlayer().GetMap();
                if (map != null)
                    map.RemovePlayerFromMap(GetPlayer(), true);

                SetPlayer(null);

                //! Send the 'logout complete' packet to the client
                //! Client will respond by sending 3x CMSG_CANCEL_TRADE, which we currently dont handle
                LogoutComplete logoutComplete = new();
                SendPacket(logoutComplete);

                //! Since each account can only have one online character at any given time, ensure all characters for active account are marked as offline
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ACCOUNT_ONLINE);
                stmt.AddValue(0, GetAccountId());
                DB.Characters.Execute(stmt);
            }

            if (m_Socket[(int)ConnectionType.Instance] != null)
            {
                m_Socket[(int)ConnectionType.Instance].CloseSocket();
                m_Socket[(int)ConnectionType.Instance] = null;
            }

            m_playerLogout = false;
            m_playerSave = false;
            m_playerRecentlyLogout = true;
            SetLogoutStartTime(0);
        }

        public bool Update(uint diff, PacketFilter updater)
        {
            // Before we process anything:
            /// If necessary, kick the player because the client didn't send anything for too long
            /// (or they've been idling in character select)
            if (IsConnectionIdle() && !HasPermission(RBACPermissions.IgnoreIdleConnection))
                m_Socket[(int)ConnectionType.Realm].CloseSocket();

            WorldPacket firstDelayedPacket = null;
            uint processedPackets = 0;
            long currentTime = GameTime.GetGameTime();

            WorldPacket packet;
            //Check for any packets they was not recived yet.
            while (m_Socket[(int)ConnectionType.Realm] != null && !_recvQueue.IsEmpty && (_recvQueue.TryPeek(out packet, updater) && packet != firstDelayedPacket) && _recvQueue.TryDequeue(out packet))
            {
                try
                {
                    var handler = PacketManager.GetHandler((ClientOpcodes)packet.GetOpcode());
                    switch (handler.sessionStatus)
                    {
                        case SessionStatus.Loggedin:
                            if (_player == null)
                            {
                                if (!m_playerRecentlyLogout)
                                {
                                    if (firstDelayedPacket == null)
                                        firstDelayedPacket = packet;

                                    QueuePacket(packet);
                                    Log.outDebug(LogFilter.Network, "Re-enqueueing packet with opcode {0} with with status OpcodeStatus.Loggedin. Player is currently not in world yet.", (ClientOpcodes)packet.GetOpcode());
                                }
                                break;
                            }
                            else if (_player.IsInWorld && AntiDOS.EvaluateOpcode(packet, currentTime))
                                handler.Invoke(this, packet);
                            break;
                        case SessionStatus.LoggedinOrRecentlyLogout:
                            if (_player == null && !m_playerRecentlyLogout && !m_playerLogout)
                                LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet and not recently logout");
                            else if (AntiDOS.EvaluateOpcode(packet, currentTime))
                                handler.Invoke(this, packet);
                            break;
                        case SessionStatus.Transfer:
                            if (_player == null)
                                LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet");
                            else if (_player.IsInWorld)
                                LogUnexpectedOpcode(packet, handler.sessionStatus, "the player is still in world");
                            else if (AntiDOS.EvaluateOpcode(packet, currentTime))
                                handler.Invoke(this, packet);
                            break;
                        case SessionStatus.Authed:
                            // prevent cheating with skip queue wait
                            if (m_inQueue)
                            {
                                LogUnexpectedOpcode(packet, handler.sessionStatus, "the player not pass queue yet");
                                break;
                            }

                            if ((ClientOpcodes)packet.GetOpcode() == ClientOpcodes.EnumCharacters)
                                m_playerRecentlyLogout = false;

                            if (AntiDOS.EvaluateOpcode(packet, currentTime))
                                handler.Invoke(this, packet);
                            break;
                        default:
                            Log.outError(LogFilter.Network, "Received not handled opcode {0} from {1}", (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());
                            break;
                    }
                }
                catch (InternalBufferOverflowException ex)
                {
                    Log.outError(LogFilter.Network, "InternalBufferOverflowException: {0} while parsing {1} from {2}.", ex.Message, (ClientOpcodes)packet.GetOpcode(), GetPlayerInfo());
                }
                catch (EndOfStreamException)
                {
                    Log.outError(LogFilter.Network, "WorldSession:Update EndOfStreamException occured while parsing a packet (opcode: {0}) from client {1}, accountid={2}. Skipped packet.",
                        (ClientOpcodes)packet.GetOpcode(), GetRemoteAddress(), GetAccountId());
                }

                processedPackets++;

                if (processedPackets > 100)
                    break;
            }

            if (!updater.ProcessUnsafe()) // <=> updater is of type MapSessionFilter
            {
                // Send time sync packet every 10s.
                if (_timeSyncTimer > 0)
                {
                    if (diff >= _timeSyncTimer)
                        SendTimeSync();
                    else
                        _timeSyncTimer -= diff;
                }
            }

            ProcessQueryCallbacks();

            if (updater.ProcessUnsafe())
            {
                if (m_Socket[(int)ConnectionType.Realm] != null && m_Socket[(int)ConnectionType.Realm].IsOpen() && _warden != null)
                    _warden.Update(diff);

                // If necessary, log the player out
                if (ShouldLogOut(currentTime) && m_playerLoading.IsEmpty())
                    LogoutPlayer(true);

                //- Cleanup socket if need
                if ((m_Socket[(int)ConnectionType.Realm] != null && !m_Socket[(int)ConnectionType.Realm].IsOpen()) ||
                    (m_Socket[(int)ConnectionType.Instance] != null && !m_Socket[(int)ConnectionType.Instance].IsOpen()))
                {
                    if (GetPlayer() != null && _warden != null)
                        _warden.Update(diff);

                    expireTime -= expireTime > diff ? diff : expireTime;
                    if (expireTime < diff || forceExit || GetPlayer() == null)
                    {
                        if (m_Socket[(int)ConnectionType.Realm] != null)
                        {
                            m_Socket[(int)ConnectionType.Realm].CloseSocket();
                            m_Socket[(int)ConnectionType.Realm] = null;
                        }
                        if (m_Socket[(int)ConnectionType.Instance] != null)
                        {
                            m_Socket[(int)ConnectionType.Instance].CloseSocket();
                            m_Socket[(int)ConnectionType.Instance] = null;
                        }
                    }
                }

                if (m_Socket[(int)ConnectionType.Realm] == null)
                    return false;                                       //Will remove this session from the world session map
            }

            return true;
        }

        public void QueuePacket(WorldPacket packet)
        {
            _recvQueue.Enqueue(packet);
        }

        void LogUnexpectedOpcode(WorldPacket packet, SessionStatus status, string reason)
        {
            Log.outError(LogFilter.Network, "Received unexpected opcode {0} Status: {1} Reason: {2} from {3}", (ClientOpcodes)packet.GetOpcode(), status, reason, GetPlayerInfo());
        }

        public void SendPacket(ServerPacket packet)
        {
            if (packet == null)
                return;

            if (packet.GetOpcode() == ServerOpcodes.Unknown || packet.GetOpcode() == ServerOpcodes.Max)
            {
                Log.outError(LogFilter.Network, "Prevented sending of UnknownOpcode to {0}", GetPlayerInfo());
                return;
            }

            ConnectionType conIdx = packet.GetConnection();
            if (conIdx != ConnectionType.Instance && PacketManager.IsInstanceOnlyOpcode(packet.GetOpcode()))
            {
                Log.outError(LogFilter.Network, "Prevented sending of instance only opcode {0} with connection type {1} to {2}", packet.GetOpcode(), packet.GetConnection(), GetPlayerInfo());
                return;
            }

            if (m_Socket[(int)conIdx] == null)
            {
                Log.outError(LogFilter.Network, "Prevented sending of {0} to non existent socket {1} to {2}", packet.GetOpcode(), conIdx, GetPlayerInfo());
                return;
            }

            m_Socket[(int)conIdx].SendPacket(packet);
        }

        public void AddInstanceConnection(WorldSocket sock) { m_Socket[(int)ConnectionType.Instance] = sock; }

        public void KickPlayer(string reason)
        {
            Log.outInfo(LogFilter.Network, $"Account: {GetAccountId()} Character: '{(_player != null ? _player.GetName() : "<none>")}' {(_player != null ? _player.GetGUID() : "")} kicked with reason: {reason}");

            for (byte i = 0; i < 2; ++i)
            {
                if (m_Socket[i] != null)
                {
                    m_Socket[i].CloseSocket();
                    forceExit = true;
                }
            }
        }

        public bool IsAddonRegistered(string prefix)
        {
            if (!_filterAddonMessages) // if we have hit the softcap (64) nothing should be filtered
                return true;

            if (_registeredAddonPrefixes.Empty())
                return false;

            return _registeredAddonPrefixes.Contains(prefix);
        }

        public void SendAccountDataTimes(ObjectGuid playerGuid, AccountDataTypes mask)
        {
            AccountDataTimes accountDataTimes = new();
            accountDataTimes.PlayerGuid = playerGuid;
            accountDataTimes.ServerTime = GameTime.GetGameTime();
            for (int i = 0; i < (int)AccountDataTypes.Max; ++i)
                if (((int)mask & (1 << i)) != 0)
                    accountDataTimes.AccountTimes[i] = GetAccountData((AccountDataTypes)i).Time;

            SendPacket(accountDataTimes);
        }

        public void LoadTutorialsData(SQLResult result)
        {
            if (!result.IsEmpty())
            {
                for (var i = 0; i < SharedConst.MaxAccountTutorialValues; i++)
                    tutorials[i] = result.Read<uint>(i);
                tutorialsChanged |= TutorialsFlag.LoadedFromDB;
            }

            tutorialsChanged &= ~TutorialsFlag.Changed;
        }

        public void SaveTutorialsData(SQLTransaction trans)
        {
            if (!tutorialsChanged.HasAnyFlag(TutorialsFlag.Changed))
                return;

            bool hasTutorialsInDB = tutorialsChanged.HasAnyFlag(TutorialsFlag.LoadedFromDB);
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(hasTutorialsInDB ? CharStatements.UPD_TUTORIALS : CharStatements.INS_TUTORIALS);
            for (var i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
                stmt.AddValue(i, tutorials[i]);
            stmt.AddValue(SharedConst.MaxAccountTutorialValues, GetAccountId());
            trans.Append(stmt);

            // now has, set flag so next save uses update query
            if (!hasTutorialsInDB)
                tutorialsChanged |= TutorialsFlag.LoadedFromDB;

            tutorialsChanged &= ~TutorialsFlag.Changed;
        }

        public void SendConnectToInstance(ConnectToSerial serial)
        {
            System.Net.IPAddress instanceAddress = null;
            var currentRealm = Global.RealmMgr.GetCurrentRealm();
            if (currentRealm != null)
                instanceAddress = currentRealm.GetAddressForClient(System.Net.IPAddress.Parse(GetRemoteAddress()));

            _instanceConnectKey.AccountId = GetAccountId();
            _instanceConnectKey.connectionType = ConnectionType.Instance;
            _instanceConnectKey.Key = RandomHelper.URand(0, 0x7FFFFFFF);

            ConnectTo connectTo = new();
            connectTo.Key = _instanceConnectKey.Raw;
            connectTo.Serial = serial;
            connectTo.Payload.Port = (ushort)WorldConfig.GetIntValue(WorldCfg.PortInstance);
            connectTo.Con = (byte)ConnectionType.Instance;

            if (instanceAddress != null)
            {
                if (instanceAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    connectTo.Payload.Where.IPv4 = instanceAddress.GetAddressBytes();
                    connectTo.Payload.Where.Type = ConnectTo.AddressType.IPv4;
                }
                else
                {
                    connectTo.Payload.Where.IPv6 = instanceAddress.GetAddressBytes();
                    connectTo.Payload.Where.Type = ConnectTo.AddressType.IPv6;
                }
            }

            SendPacket(connectTo);
        }

        void LoadAccountData(SQLResult result, AccountDataTypes mask)
        {
            for (int i = 0; i < (int)AccountDataTypes.Max; ++i)
                if (Convert.ToBoolean((int)mask & (1 << i)))
                    _accountData[i] = new AccountData();

            if (result.IsEmpty())
                return;

            do
            {
                int type = result.Read<byte>(0);
                if (type >= (int)AccountDataTypes.Max)
                {
                    Log.outError(LogFilter.Server, "Table `{0}` have invalid account data type ({1}), ignore.",
                        mask == AccountDataTypes.GlobalCacheMask ? "account_data" : "character_account_data", type);
                    continue;
                }

                if (((int)mask & (1 << type)) == 0)
                {
                    Log.outError(LogFilter.Server, "Table `{0}` have non appropriate for table  account data type ({1}), ignore.",
                        mask == AccountDataTypes.GlobalCacheMask ? "account_data" : "character_account_data", type);
                    continue;
                }

                _accountData[type].Time = result.Read<long>(1);
                var bytes = result.Read<byte[]>(2);
                var line = Encoding.Default.GetString(bytes);
                _accountData[type].Data = line;
            }
            while (result.NextRow());
        }

        void SetAccountData(AccountDataTypes type, long time, string data)
        {
            if (Convert.ToBoolean((1 << (int)type) & (int)AccountDataTypes.GlobalCacheMask))
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_ACCOUNT_DATA);
                stmt.AddValue(0, GetAccountId());
                stmt.AddValue(1, (byte)type);
                stmt.AddValue(2, time);
                stmt.AddValue(3, data);
                DB.Characters.Execute(stmt);
            }
            else
            {
                // _player can be NULL and packet received after logout but m_GUID still store correct guid
                if (m_GUIDLow == 0)
                    return;

                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.REP_PLAYER_ACCOUNT_DATA);
                stmt.AddValue(0, m_GUIDLow);
                stmt.AddValue(1, (byte)type);
                stmt.AddValue(2, time);
                stmt.AddValue(3, data);
                DB.Characters.Execute(stmt);
            }

            _accountData[(int)type].Time = time;
            _accountData[(int)type].Data = data;
        }

        public void SendTutorialsData()
        {
            TutorialFlags packet = new();
            Array.Copy(tutorials, packet.TutorialData, SharedConst.MaxAccountTutorialValues);
            SendPacket(packet);
        }

        public bool CanSpeak()
        {
            return m_muteTime <= GameTime.GetGameTime();
        }

        bool ValidateHyperlinksAndMaybeKick(string str)
        {
            if (Hyperlink.CheckAllLinks(str))
                return true;

            Log.outError(LogFilter.Network, $"Player {GetPlayer().GetName()} {GetPlayer().GetGUID()} sent a message with an invalid link:\n{str}");

            if (WorldConfig.GetIntValue(WorldCfg.ChatStrictLinkCheckingKick) != 0)
                KickPlayer("WorldSession::ValidateHyperlinksAndMaybeKick Invalid chat link");

            return false;
        }

        public bool DisallowHyperlinksAndMaybeKick(string str)
        {
            if (!str.Contains('|'))
                return true;

            Log.outError(LogFilter.Network, $"Player {GetPlayer().GetName()} ({GetPlayer().GetGUID()}) sent a message which illegally contained a hyperlink:\n{str}");

            if (WorldConfig.GetIntValue(WorldCfg.ChatStrictLinkCheckingKick) != 0)
                KickPlayer("WorldSession::DisallowHyperlinksAndMaybeKick Illegal chat link");

            return false;
        }

        public void SendNotification(CypherStrings str, params object[] args)
        {
            SendNotification(Global.ObjectMgr.GetCypherString(str), args);
        }

        public void SendNotification(string str, params object[] args)
        {
            string message = string.Format(str, args);
            if (!string.IsNullOrEmpty(message))
            {
                SendPacket(new PrintNotification(message));
            }
        }

        public void SetPlayer(Player pl)
        {
            _player = pl;

            if (_player != null)
                m_GUIDLow = _player.GetGUID().GetCounter();
        }

        public string GetPlayerName()
        {
            return _player != null ? _player.GetName() : "Unknown";
        }

        public string GetPlayerInfo()
        {
            StringBuilder ss = new();
            ss.Append("[Player: ");
            if (!m_playerLoading.IsEmpty())
                ss.AppendFormat("Logging in: {0}, ", m_playerLoading.ToString());
            else if (_player != null)
                ss.AppendFormat("{0} {1}, ", _player.GetName(), _player.GetGUID().ToString());

            ss.AppendFormat("Account: {0}]", GetAccountId());
            return ss.ToString();
        }

        void HandleWardenData(WardenData packet)
        {
            if (_warden == null || packet.Data.GetSize() == 0)
                return;

            _warden.HandleData(packet.Data);
        }

        public bool PlayerLoading() { return !m_playerLoading.IsEmpty(); }
        public bool PlayerLogout() { return m_playerLogout; }
        public bool PlayerLogoutWithSave() { return m_playerLogout && m_playerSave; }
        public bool PlayerRecentlyLoggedOut() { return m_playerRecentlyLogout; }
        public bool PlayerDisconnected()
        {
            return !(m_Socket[(int)ConnectionType.Realm] != null && m_Socket[(int)ConnectionType.Realm].IsOpen() &&
                m_Socket[(int)ConnectionType.Instance] != null && m_Socket[(int)ConnectionType.Instance].IsOpen());
        }

        public AccountTypes GetSecurity() { return _security; }
        public uint GetAccountId() { return _accountId; }
        public ObjectGuid GetAccountGUID() { return ObjectGuid.Create(HighGuid.WowAccount, GetAccountId()); }
        public string GetAccountName() { return _accountName; }
        public uint GetBattlenetAccountId() { return _battlenetAccountId; }
        public ObjectGuid GetBattlenetAccountGUID() { return ObjectGuid.Create(HighGuid.BNetAccount, GetBattlenetAccountId()); }

        public Player GetPlayer() { return _player; }

        void SetSecurity(AccountTypes security) { _security = security; }

        public string GetRemoteAddress() { return m_Address; }

        public Expansion GetAccountExpansion() { return m_accountExpansion; }
        public Expansion GetExpansion() { return m_expansion; }
        public string GetOS() { return _os; }
        public uint GetClientBuild() { return _clientBuild; }
        public Framework.ClientBuild.ClientBuildVariantId GetClientBuildVariant() { return _clientBuildVariant; }
        public void SetInQueue(bool state) { m_inQueue = state; }

        public bool IsLogingOut() { return _logoutTime != 0 || m_playerLogout; }

        public ulong GetConnectToInstanceKey() { return _instanceConnectKey.Raw; }

        public AsyncCallbackProcessor<QueryCallback> GetQueryProcessor() { return _queryProcessor; }

        void SetLogoutStartTime(long requestTime)
        {
            _logoutTime = requestTime;
        }

        bool ShouldLogOut(long currTime)
        {
            return (_logoutTime > 0 && currTime >= _logoutTime + 20);
        }

        void ProcessQueryCallbacks()
        {
            _queryProcessor.ProcessReadyCallbacks();
            _transactionCallbacks.ProcessReadyCallbacks();
            _queryHolderProcessor.ProcessReadyCallbacks();
        }

        public TransactionCallback AddTransactionCallback(TransactionCallback callback)
        {
            return _transactionCallbacks.AddCallback(callback);
        }

        public SQLQueryHolderCallback<R> AddQueryHolderCallback<R>(SQLQueryHolderCallback<R> callback)
        {
            return (SQLQueryHolderCallback<R>)_queryHolderProcessor.AddCallback(callback);
        }

        public bool CanAccessAlliedRaces()
        {
            return GetAccountExpansion() >= Expansion.BattleForAzeroth;
        }

        void InitWarden(BigInteger k)
        {
            if (_os == "Win")
            {
                _warden = new WardenWin();
                _warden.Init(this, k);
            }
            else if (_os == "Wn64")
            {
                // Not implemented
            }
            else if (_os == "Mc64")
            {
                // Not implemented
            }
        }

        public void LoadPermissions()
        {
            uint id = GetAccountId();
            AccountTypes secLevel = GetSecurity();

            Log.outDebug(LogFilter.Rbac, "WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                id, _accountName, Global.RealmMgr.GetCurrentRealmId().Index, secLevel);

            _RBACData = new RBACData(id, _accountName, (int)Global.RealmMgr.GetCurrentRealmId().Index, (byte)secLevel);
            _RBACData.LoadFromDB();
        }

        public QueryCallback LoadPermissionsAsync()
        {
            uint id = GetAccountId();
            AccountTypes secLevel = GetSecurity();

            Log.outDebug(LogFilter.Rbac, "WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                id, _accountName, Global.RealmMgr.GetCurrentRealmId().Index, secLevel);

            _RBACData = new RBACData(id, _accountName, (int)Global.RealmMgr.GetCurrentRealmId().Index, (byte)secLevel);
            return _RBACData.LoadFromDBAsync();
        }

        public void InitializeSession()
        {
            AccountInfoQueryHolderPerRealm realmHolder = new();
            realmHolder.Initialize(GetAccountId(), GetBattlenetAccountId());

            AccountInfoQueryHolder holder = new();
            holder.Initialize(GetAccountId(), GetBattlenetAccountId());

            AccountInfoQueryHolderPerRealm characterHolder = null;
            AccountInfoQueryHolder loginHolder = null;

            AddQueryHolderCallback(DB.Characters.DelayQueryHolder(realmHolder)).AfterComplete(result =>
            {
                characterHolder = (AccountInfoQueryHolderPerRealm)result;
                if (loginHolder != null && characterHolder != null)
                    InitializeSessionCallback(loginHolder, characterHolder);
            });

            AddQueryHolderCallback(DB.Login.DelayQueryHolder(holder)).AfterComplete(result =>
            {
                loginHolder = (AccountInfoQueryHolder)result;
                if (loginHolder != null && characterHolder != null)
                    InitializeSessionCallback(loginHolder, characterHolder);
            });
        }

        void InitializeSessionCallback(SQLQueryHolder<AccountInfoQueryLoad> holder, SQLQueryHolder<AccountInfoQueryLoad> realmHolder)
        {
            LoadAccountData(realmHolder.GetResult(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm), AccountDataTypes.GlobalCacheMask);
            LoadTutorialsData(realmHolder.GetResult(AccountInfoQueryLoad.TutorialsIndexPerRealm));
            _collectionMgr.LoadAccountToys(holder.GetResult(AccountInfoQueryLoad.GlobalAccountToys));
            _collectionMgr.LoadAccountHeirlooms(holder.GetResult(AccountInfoQueryLoad.GlobalAccountHeirlooms));
            _collectionMgr.LoadAccountMounts(holder.GetResult(AccountInfoQueryLoad.Mounts));
            _collectionMgr.LoadAccountItemAppearances(holder.GetResult(AccountInfoQueryLoad.ItemAppearances), holder.GetResult(AccountInfoQueryLoad.ItemFavoriteAppearances));
            _collectionMgr.LoadAccountTransmogIllusions(holder.GetResult(AccountInfoQueryLoad.TransmogIllusions));

            if (!m_inQueue)
                SendAuthResponse(BattlenetRpcErrorCode.Ok, false);
            else
                SendAuthWaitQueue(0);

            SetInQueue(false);
            ResetTimeOutTime(false);

            SendSetTimeZoneInformation();
            SendFeatureSystemStatusGlueScreen();
            SendClientCacheVersion(WorldConfig.GetUIntValue(WorldCfg.ClientCacheVersion));
            SendAvailableHotfixes();
            SendAccountDataTimes(ObjectGuid.Empty, AccountDataTypes.GlobalCacheMask);
            SendTutorialsData();

            SQLResult result = holder.GetResult(AccountInfoQueryLoad.GlobalRealmCharacterCounts);
            if (!result.IsEmpty())
            {
                do
                {
                    _realmCharacterCounts[new RealmId(result.Read<byte>(3), result.Read<byte>(4), result.Read<uint>(2)).GetAddress()] = result.Read<byte>(1);

                } while (result.NextRow());
            }

            ConnectionStatus bnetConnected = new();
            bnetConnected.State = 1;
            SendPacket(bnetConnected);

            _battlePetMgr.LoadFromDB(holder.GetResult(AccountInfoQueryLoad.BattlePets), holder.GetResult(AccountInfoQueryLoad.BattlePetSlot));
        }

        public RBACData GetRBACData()
        {
            return _RBACData;
        }

        public bool HasPermission(RBACPermissions permission)
        {
            if (_RBACData == null)
                LoadPermissions();

            bool hasPermission = _RBACData.HasPermission(permission);
            Log.outDebug(LogFilter.Rbac, "WorldSession:HasPermission [AccountId: {0}, Name: {1}, realmId: {2}]",
                           _RBACData.GetId(), _RBACData.GetName(), Global.RealmMgr.GetCurrentRealmId().Index);

            return hasPermission;
        }

        public void InvalidateRBACData()
        {
            Log.outDebug(LogFilter.Rbac, "WorldSession:Invalidaterbac:RBACData [AccountId: {0}, Name: {1}, realmId: {2}]",
                           _RBACData.GetId(), _RBACData.GetName(), Global.RealmMgr.GetCurrentRealmId().Index);
            _RBACData = null;
        }

        AccountData GetAccountData(AccountDataTypes type) { return _accountData[(int)type]; }

        uint GetTutorialInt(byte index) { return tutorials[index]; }

        void SetTutorialInt(byte index, uint value)
        {
            if (tutorials[index] != value)
            {
                tutorials[index] = value;
                tutorialsChanged |= TutorialsFlag.Changed;
            }
        }

        public void ResetTimeSync()
        {
            _timeSyncNextCounter = 0;
            _pendingTimeSyncRequests.Clear();
        }

        public void SendTimeSync()
        {
            TimeSyncRequest timeSyncRequest = new();
            timeSyncRequest.SequenceIndex = _timeSyncNextCounter;
            SendPacket(timeSyncRequest);

            _pendingTimeSyncRequests[_timeSyncNextCounter] = Time.GetMSTime();

            // Schedule next sync in 10 sec (except for the 2 first packets, which are spaced by only 5s)
            _timeSyncTimer = _timeSyncNextCounter == 0 ? 5000 : 10000u;
            _timeSyncNextCounter++;
        }

        uint AdjustClientMovementTime(uint time)
        {
            long movementTime = (long)time + _timeSyncClockDelta;
            if (_timeSyncClockDelta == 0 || movementTime < 0 || movementTime > 0xFFFFFFFF)
            {
                Log.outWarn(LogFilter.Misc, "The computed movement time using clockDelta is erronous. Using fallback instead");
                return GameTime.GetGameTimeMS();
            }
            else
                return (uint)movementTime;
        }

        public Locale GetSessionDbcLocale() { return m_sessionDbcLocale; }
        public Locale GetSessionDbLocaleIndex() { return m_sessionDbLocaleIndex; }

        public TimeSpan GetTimezoneOffset() { return _timezoneOffset; }

        public uint GetLatency() { return m_latency; }
        public void SetLatency(uint latency) { m_latency = latency; }
        public void ResetTimeOutTime(bool onlyActive)
        {
            if (GetPlayer() != null)
                m_timeOutTime = GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.SocketTimeoutTimeActive);
            else if (!onlyActive)
                m_timeOutTime = GameTime.GetGameTime() + WorldConfig.GetIntValue(WorldCfg.SocketTimeoutTime);
        }
        bool IsConnectionIdle()
        {
            return m_timeOutTime < GameTime.GetGameTime() && !m_inQueue;
        }

        public uint GetRecruiterId() { return recruiterId; }
        public bool IsARecruiter() { return isRecruiter; }

        // Packets cooldown
        public long GetCalendarEventCreationCooldown() { return _calendarEventCreationCooldown; }
        public void SetCalendarEventCreationCooldown(long cooldown) { _calendarEventCreationCooldown = cooldown; }

        // Battle Pets
        public BattlePetMgr GetBattlePetMgr() { return _battlePetMgr; }
        public CollectionMgr GetCollectionMgr() { return _collectionMgr; }

        // Battlenet
        public Array<byte> GetRealmListSecret() { return _realmListSecret; }
        void SetRealmListSecret(Array<byte> secret) { _realmListSecret = secret; }
        public Dictionary<uint, byte> GetRealmCharacterCounts() { return _realmCharacterCounts; }

        #region Fields
        List<ObjectGuid> _legitCharacters = new();
        ulong m_GUIDLow;
        Player _player;
        WorldSocket[] m_Socket = new WorldSocket[(int)ConnectionType.Max];
        string m_Address;

        AccountTypes _security;
        uint _accountId;
        string _accountName;
        uint _battlenetAccountId;
        Expansion m_accountExpansion;
        Expansion m_expansion;
        string _os;
        uint _clientBuild;
        Framework.ClientBuild.ClientBuildVariantId _clientBuildVariant;

        uint expireTime;
        bool forceExit;

        DosProtection AntiDOS;
        Warden _warden;                                    // Remains NULL if Warden system is not enabled by config

        long _logoutTime;
        bool m_inQueue;
        ObjectGuid m_playerLoading;                               // code processed in LoginPlayer
        bool m_playerLogout;                                // code processed in LogoutPlayer
        bool m_playerRecentlyLogout;
        bool m_playerSave;
        Locale m_sessionDbcLocale;
        Locale m_sessionDbLocaleIndex;
        TimeSpan _timezoneOffset;
        uint m_latency;
        AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
        uint[] tutorials = new uint[SharedConst.MaxAccountTutorialValues];
        TutorialsFlag tutorialsChanged;

        Array<byte> _realmListSecret = new(32);
        Dictionary<uint /*realmAddress*/, byte> _realmCharacterCounts = new();
        Dictionary<uint, Action<Google.Protobuf.CodedInputStream>> _battlenetResponseCallbacks = new();
        uint _battlenetRequestToken;

        List<string> _registeredAddonPrefixes = new();
        bool _filterAddonMessages;
        uint recruiterId;
        bool isRecruiter;

        public long m_muteTime;
        long m_timeOutTime;

        ConcurrentQueue<WorldPacket> _recvQueue = new();
        RBACData _RBACData;

        CircularBuffer<Tuple<long, uint>> _timeSyncClockDeltaQueue = new(6); // first member: clockDelta. Second member: latency of the packet exchange that was used to compute that clockDelta.
        long _timeSyncClockDelta;

        Dictionary<uint, uint> _pendingTimeSyncRequests = new(); // key: counter. value: server time when packet with that counter was sent.
        uint _timeSyncNextCounter;
        uint _timeSyncTimer;

        CollectionMgr _collectionMgr;

        ConnectToKey _instanceConnectKey;

        // Packets cooldown
        long _calendarEventCreationCooldown;

        BattlePetMgr _battlePetMgr;

        AsyncCallbackProcessor<QueryCallback> _queryProcessor = new();
        AsyncCallbackProcessor<TransactionCallback> _transactionCallbacks = new();
        AsyncCallbackProcessor<ISqlCallback> _queryHolderProcessor = new();
        #endregion
    }

    public struct ConnectToKey
    {
        public ulong Raw
        {
            get { return ((ulong)AccountId | ((ulong)connectionType << 32) | (Key << 33)); }
            set
            {
                AccountId = (uint)(value & 0xFFFFFFFF);
                connectionType = (ConnectionType)((value >> 32) & 1);
                Key = (value >> 33);
            }
        }

        public uint AccountId;
        public ConnectionType connectionType;
        public ulong Key;
    }

    public class DosProtection
    {
        public DosProtection(WorldSession s)
        {
            Session = s;
            _policy = (Policy)WorldConfig.GetIntValue(WorldCfg.PacketSpoofPolicy);
        }
        //todo fix me
        public bool EvaluateOpcode(WorldPacket packet, long time)
        {
            uint maxPacketCounterAllowed = 0;// GetMaxPacketCounterAllowed(p.GetOpcode());

            // Return true if there no limit for the opcode
            if (maxPacketCounterAllowed == 0)
                return true;

            if (!_PacketThrottlingMap.ContainsKey(packet.GetOpcode()))
                _PacketThrottlingMap[packet.GetOpcode()] = new PacketCounter();

            PacketCounter packetCounter = _PacketThrottlingMap[packet.GetOpcode()];
            if (packetCounter.lastReceiveTime != time)
            {
                packetCounter.lastReceiveTime = time;
                packetCounter.amountCounter = 0;
            }

            // Check if player is flooding some packets
            if (++packetCounter.amountCounter <= maxPacketCounterAllowed)
                return true;

            Log.outWarn(LogFilter.Network, "AntiDOS: Account {0}, IP: {1}, Ping: {2}, Character: {3}, flooding packet (opc: {4} (0x{4}), count: {5})",
                Session.GetAccountId(), Session.GetRemoteAddress(), Session.GetLatency(), Session.GetPlayerName(), packet.GetOpcode(), packetCounter.amountCounter);

            switch (_policy)
            {
                case Policy.Log:
                    return true;
                case Policy.Kick:
                    Log.outInfo(LogFilter.Network, "AntiDOS: Player kicked!");
                    return false;
                case Policy.Ban:
                    BanMode bm = (BanMode)WorldConfig.GetIntValue(WorldCfg.PacketSpoofBanmode);
                    uint duration = WorldConfig.GetUIntValue(WorldCfg.PacketSpoofBanduration); // in seconds
                    string nameOrIp = "";
                    switch (bm)
                    {
                        case BanMode.Character: // not supported, ban account
                        case BanMode.Account:
                            Global.AccountMgr.GetName(Session.GetAccountId(), out nameOrIp);
                            break;
                        case BanMode.IP:
                            nameOrIp = Session.GetRemoteAddress();
                            break;
                    }
                    Global.WorldMgr.BanAccount(bm, nameOrIp, duration, "DOS (Packet Flooding/Spoofing", "Server: AutoDOS");
                    Log.outInfo(LogFilter.Network, "AntiDOS: Player automatically banned for {0} seconds.", duration);
                    return false;
            }
            return true;
        }

        Policy _policy;
        WorldSession Session;
        Dictionary<uint, PacketCounter> _PacketThrottlingMap = new();

        enum Policy
        {
            Log,
            Kick,
            Ban,
        }
    }

    struct PacketCounter
    {
        public long lastReceiveTime;
        public uint amountCounter;
    }

    public struct AccountData
    {
        public long Time;
        public string Data;
    }

    class AccountInfoQueryHolderPerRealm : SQLQueryHolder<AccountInfoQueryLoad>
    {
        public void Initialize(uint accountId, uint battlenetAccountId)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_ACCOUNT_DATA);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_TUTORIALS);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.TutorialsIndexPerRealm, stmt);
        }
    }

    class AccountInfoQueryHolder : SQLQueryHolder<AccountInfoQueryLoad>
    {
        public void Initialize(uint accountId, uint battlenetAccountId)
        {
            PreparedStatement stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOYS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountToys, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BATTLE_PETS);
            stmt.AddValue(0, battlenetAccountId);
            stmt.AddValue(1, Global.RealmMgr.GetCurrentRealmId().Index);
            SetQuery(AccountInfoQueryLoad.BattlePets, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BATTLE_PET_SLOTS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.BattlePetSlot, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_HEIRLOOMS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountHeirlooms, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_MOUNTS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.Mounts, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_ACCOUNT_ID);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.GlobalRealmCharacterCounts, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_APPEARANCES);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.ItemAppearances, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_FAVORITE_APPEARANCES);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.ItemFavoriteAppearances, stmt);

            stmt = LoginDatabase.GetPreparedStatement(LoginStatements.SEL_BNET_TRANSMOG_ILLUSIONS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.TransmogIllusions, stmt);
        }
    }

    enum AccountInfoQueryLoad
    {
        GlobalAccountToys,
        BattlePets,
        BattlePetSlot,
        GlobalAccountHeirlooms,
        GlobalRealmCharacterCounts,
        Mounts,
        ItemAppearances,
        ItemFavoriteAppearances,
        GlobalAccountDataIndexPerRealm,
        TutorialsIndexPerRealm,
        TransmogIllusions,
    }
}
