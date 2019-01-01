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
using Framework.Database;
using Game.Accounts;
using Game.BattleGrounds;
using Game.BattlePets;
using Game.Entities;
using Game.Guilds;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Game
{
    public partial class WorldSession : IDisposable
    {
        public WorldSession(uint id, string name, uint battlenetAccountId, WorldSocket sock, AccountTypes sec, Expansion expansion, long mute_time, string os, LocaleConstant locale, uint recruiter, bool isARecruiter)
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
            m_sessionDbcLocale = Global.WorldMgr.GetAvailableDbcLocale(locale);
            m_sessionDbLocaleIndex = locale;
            recruiterId = recruiter;
            isRecruiter = isARecruiter;
            expireTime = 60000; // 1 min after socket loss, session is deleted
            m_currentBankerGUID = ObjectGuid.Empty;
            _battlePetMgr = new BattlePetMgr(this);
            _collectionMgr = new CollectionMgr(this);

            m_Address = sock.GetRemoteIpAddress().ToString();
            ResetTimeOutTime();
            DB.Login.Execute("UPDATE account SET online = 1 WHERE id = {0};", GetAccountId());     // One-time query
        }

        public void Dispose()
        {
            // unload player if not unloaded
            if (_player)
                LogoutPlayer(true);

            // - If have unclosed socket, close it
            for (byte i = 0; i < 2; ++i)
            {
                if (m_Socket[i] != null)
                    m_Socket[i].CloseSocket();
            }

            // empty incoming packet queue
            WorldPacket packet;
            while (_recvQueue.TryDequeue(out packet)) ;

            DB.Login.Execute("UPDATE account SET online = 0 WHERE id = {0};", GetAccountId());     // One-time query
        }

        public void LogoutPlayer(bool save)
        {
            // finish pending transfers before starting the logout
            while (_player && _player.IsBeingTeleportedFar())
                HandleMoveWorldportAck();

            m_playerLogout = true;
            m_playerSave = save;

            if (_player)
            {
                if (!_player.GetLootGUID().IsEmpty())
                    DoLootReleaseAll();

                // If the player just died before logging out, make him appear as a ghost
                //FIXME: logout must be delayed in case lost connection with client in time of combat
                if (GetPlayer().GetDeathTimer() != 0)
                {
                    _player.getHostileRefManager().deleteReferences();
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
                if (bg)
                    bg.EventPlayerLoggedOut(GetPlayer());

                // Teleport to home if the player is in an invalid instance
                if (!_player.m_InstanceValid && !_player.IsGameMaster())
                    _player.TeleportTo(_player.GetHomebind());

                Global.OutdoorPvPMgr.HandlePlayerLeaveZone(_player, _player.GetZoneId());

                for (uint i = 0; i < SharedConst.MaxPlayerBGQueues; ++i)
                {
                    BattlegroundQueueTypeId bgQueueTypeId = GetPlayer().GetBattlegroundQueueTypeId(i);
                    if (bgQueueTypeId != 0)
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
                if (guild)
                    guild.HandleMemberLogout(this);

                // Remove pet
                _player.RemovePet(null, PetSaveMode.AsCurrent, true);

                // Clear whisper whitelist
                _player.ClearWhisperWhiteList();

                // empty buyback items and save the player in the database
                // some save parts only correctly work in case player present in map/player_lists (pets, etc)
                if (save)
                {
                    for (int j = InventorySlots.BuyBackStart; j < InventorySlots.BuyBackEnd; ++j)
                    {
                        int eslot = j - InventorySlots.BuyBackStart;
                        _player.SetGuidValue(ActivePlayerFields.InvSlotHead + (j * 4), ObjectGuid.Empty);
                        _player.SetUInt32Value(ActivePlayerFields.BuyBackPrice + eslot, 0);
                        _player.SetUInt32Value(ActivePlayerFields.BuyBackTimestamp + eslot, 0);
                    }
                    _player.SaveToDB();
                }

                // Leave all channels before player delete...
                _player.CleanupChannels();

                // If the player is in a group (or invited), remove him. If the group if then only 1 person, disband the group.
                _player.UninviteFromGroup();

                // remove player from the group if he is:
                // a) in group; b) not in raid group; c) logging out normally (not being kicked or disconnected)
                if (_player.GetGroup() && !_player.GetGroup().isRaidGroup() && m_Socket[(int)ConnectionType.Realm] != null)
                    _player.RemoveFromGroup();

                //! Send update to group and reset stored max enchanting level
                if (_player.GetGroup())
                {
                    _player.GetGroup().SendUpdate();
                    _player.GetGroup().ResetMaxEnchantingLevel();
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
                GetPlayer().CleanupsBeforeDelete();
                Log.outInfo(LogFilter.Player, "Account: {0} (IP: {1}) Logout Character:[{2}] (GUID: {3}) Level: {4}",
                    GetAccountId(), GetRemoteAddress(), _player.GetName(), _player.GetGUID().ToString(), _player.getLevel());

                Map map = GetPlayer().GetMap();
                if (map != null)
                    map.RemovePlayerFromMap(GetPlayer(), true);

                SetPlayer(null);

                //! Send the 'logout complete' packet to the client
                //! Client will respond by sending 3x CMSG_CANCEL_TRADE, which we currently dont handle
                LogoutComplete logoutComplete = new LogoutComplete();
                SendPacket(logoutComplete);

                //! Since each account can only have one online character at any given time, ensure all characters for active account are marked as offline
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ACCOUNT_ONLINE);
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
            // Update Timeout timer.
            UpdateTimeOutTime(diff);

            // Before we process anything:
            // If necessary, kick the player from the character select screen
            if (IsConnectionIdle())
                m_Socket[(int)ConnectionType.Realm].CloseSocket();

            WorldPacket firstDelayedPacket = null;
            uint processedPackets = 0;
            long currentTime = Time.UnixTime;

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
                            if (!_player)
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
                            if (!_player && !m_playerRecentlyLogout && !m_playerLogout)
                                LogUnexpectedOpcode(packet, handler.sessionStatus, "the player has not logged in yet and not recently logout");
                            else if (AntiDOS.EvaluateOpcode(packet, currentTime))
                                handler.Invoke(this, packet);
                            break;
                        case SessionStatus.Transfer:
                            if (!_player)
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

            if (m_Socket[(int)ConnectionType.Realm] != null && m_Socket[(int)ConnectionType.Realm].IsOpen() && _warden != null)
                _warden.Update();

            ProcessQueryCallbacks();

            if (updater.ProcessUnsafe())
            {
                long currTime = Time.UnixTime;
                // If necessary, log the player out
                if (ShouldLogOut(currTime) && m_playerLoading.IsEmpty())
                    LogoutPlayer(true);

                if (m_Socket[(int)ConnectionType.Realm] != null && GetPlayer() && _warden != null)
                    _warden.Update();

                //- Cleanup socket if need
                if ((m_Socket[(int)ConnectionType.Realm] != null && !m_Socket[(int)ConnectionType.Realm].IsOpen()) ||
                    (m_Socket[(int)ConnectionType.Instance] != null && !m_Socket[(int)ConnectionType.Instance].IsOpen()))
                {
                    expireTime -= expireTime > diff ? diff : expireTime;
                    if (expireTime < diff || forceExit || !GetPlayer())
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

        public void KickPlayer()
        {
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

        public void LoadTutorialsData(SQLResult result)
        {
            if (!result.IsEmpty())
                for (var i = 0; i < SharedConst.MaxAccountTutorialValues; i++)
                    tutorials[i] = result.Read<uint>(i);

            tutorialsChanged = false;
        }

        public void SaveTutorialsData(SQLTransaction trans)
        {
            if (!tutorialsChanged)
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_HAS_TUTORIALS);
            stmt.AddValue(0, GetAccountId());
            bool hasTutorials = !DB.Characters.Query(stmt).IsEmpty();
            // Modify data in DB
            stmt = DB.Characters.GetPreparedStatement(hasTutorials ? CharStatements.UPD_TUTORIALS : CharStatements.INS_TUTORIALS);
            for (var i = 0; i < SharedConst.MaxAccountTutorialValues; ++i)
                stmt.AddValue(i, tutorials[i]);
            stmt.AddValue(SharedConst.MaxAccountTutorialValues, GetAccountId());
            trans.Append(stmt);

            tutorialsChanged = false;
        }

        public void SendConnectToInstance(ConnectToSerial serial)
        {
            var instanceAddress = Global.WorldMgr.GetRealm().GetAddressForClient(System.Net.IPAddress.Parse(GetRemoteAddress()));
            instanceAddress.Port = WorldConfig.GetIntValue(WorldCfg.PortInstance);

            _instanceConnectKey.AccountId = GetAccountId();
            _instanceConnectKey.connectionType = ConnectionType.Instance;
            _instanceConnectKey.Key = RandomHelper.URand(0, 0x7FFFFFFF);

            ConnectTo connectTo = new ConnectTo();
            connectTo.Key = _instanceConnectKey.Raw;
            connectTo.Serial = serial;
            connectTo.Payload.Where = instanceAddress;
            connectTo.Con = (byte)ConnectionType.Instance;

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

                _accountData[type].Time = result.Read<uint>(1);
                var bytes = result.Read<byte[]>(2);
                var line = Encoding.Default.GetString(bytes);
                _accountData[type].Data = line;
            }
            while (result.NextRow());
        }

        void SetAccountData(AccountDataTypes type, uint time, string data)
        {
            if (Convert.ToBoolean((1 << (int)type) & (int)AccountDataTypes.GlobalCacheMask))
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_ACCOUNT_DATA);
                stmt.AddValue(0, GetAccountId());
                stmt.AddValue(1, type);
                stmt.AddValue(2, time);
                stmt.AddValue(3, data);
                DB.Characters.Execute(stmt);
            }
            else
            {
                // _player can be NULL and packet received after logout but m_GUID still store correct guid
                if (m_GUIDLow == 0)
                    return;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.REP_PLAYER_ACCOUNT_DATA);
                stmt.AddValue(0, m_GUIDLow);
                stmt.AddValue(1, type);
                stmt.AddValue(2, time);
                stmt.AddValue(3, data);
                DB.Characters.Execute(stmt);
            }

            _accountData[(int)type].Time = time;
            _accountData[(int)type].Data = data;
        }

        public void SendTutorialsData()
        {
            TutorialFlags packet = new TutorialFlags();
            Array.Copy(tutorials, packet.TutorialData, (int)AccountDataTypes.Max);
            SendPacket(packet);
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

            if (_player)
                m_GUIDLow = _player.GetGUID().GetCounter();
        }

        public string GetPlayerName()
        {
            return _player != null ? _player.GetName() : "Unknown";
        }

        public string GetPlayerInfo()
        {
            StringBuilder ss = new StringBuilder();
            ss.Append("[Player: ");
            if (!m_playerLoading.IsEmpty())
                ss.AppendFormat("Logging in: {0}, ", m_playerLoading.ToString());
            else if (_player)
                ss.AppendFormat("{0} {1}, ", _player.GetName(), _player.GetGUID().ToString());

            ss.AppendFormat("Account: {0}]", GetAccountId());
            return ss.ToString();
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
        public void SetInQueue(bool state) { m_inQueue = state; }

        public bool isLogingOut() { return _logoutTime != 0 || m_playerLogout; }

        public ulong GetConnectToInstanceKey() { return _instanceConnectKey.Raw; }

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
            _queryProcessor.ProcessReadyQueries();

            if (_realmAccountLoginCallback != null && _realmAccountLoginCallback.IsCompleted && _accountLoginCallback != null && _accountLoginCallback.IsCompleted)
            {
                InitializeSessionCallback(_realmAccountLoginCallback.Result, _accountLoginCallback.Result);
                _realmAccountLoginCallback = null;
                _accountLoginCallback = null;
            }

            // HandlePlayerLoginOpcode
            if (_charLoginCallback != null && _charLoginCallback.IsCompleted)
            {
                HandlePlayerLogin((LoginQueryHolder)_charLoginCallback.Result);
                _charLoginCallback = null;
            }
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
                id, _accountName, Global.WorldMgr.GetRealm().Id.Realm, secLevel);

            _RBACData = new RBACData(id, _accountName, (int)Global.WorldMgr.GetRealm().Id.Realm, (byte)secLevel);
            _RBACData.LoadFromDB();
        }

        public QueryCallback LoadPermissionsAsync()
        {
            uint id = GetAccountId();
            AccountTypes secLevel = GetSecurity();

            Log.outDebug(LogFilter.Rbac, "WorldSession.LoadPermissions [AccountId: {0}, Name: {1}, realmId: {2}, secLevel: {3}]",
                id, _accountName, Global.WorldMgr.GetRealm().Id.Realm, secLevel);

            _RBACData = new RBACData(id, _accountName, (int)Global.WorldMgr.GetRealm().Id.Realm, (byte)secLevel);
            return _RBACData.LoadFromDBAsync();
        }

        public void InitializeSession()
        {
            AccountInfoQueryHolderPerRealm realmHolder = new AccountInfoQueryHolderPerRealm();
            realmHolder.Initialize(GetAccountId(), GetBattlenetAccountId());

            AccountInfoQueryHolder holder = new AccountInfoQueryHolder();
            holder.Initialize(GetAccountId(), GetBattlenetAccountId());

            _realmAccountLoginCallback = DB.Characters.DelayQueryHolder(realmHolder);
            _accountLoginCallback = DB.Login.DelayQueryHolder(holder);
        }

        void InitializeSessionCallback(SQLQueryHolder<AccountInfoQueryLoad> realmHolder, SQLQueryHolder<AccountInfoQueryLoad> holder)
        {
            LoadAccountData(realmHolder.GetResult(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm), AccountDataTypes.GlobalCacheMask);
            LoadTutorialsData(realmHolder.GetResult(AccountInfoQueryLoad.TutorialsIndexPerRealm));
            _collectionMgr.LoadAccountToys(holder.GetResult(AccountInfoQueryLoad.GlobalAccountToys));
            _collectionMgr.LoadAccountHeirlooms(holder.GetResult(AccountInfoQueryLoad.GlobalAccountHeirlooms));
            _collectionMgr.LoadAccountMounts(holder.GetResult(AccountInfoQueryLoad.Mounts));
            _collectionMgr.LoadAccountItemAppearances(holder.GetResult(AccountInfoQueryLoad.ItemAppearances), holder.GetResult(AccountInfoQueryLoad.ItemFavoriteAppearances));

            if (!m_inQueue)
                SendAuthResponse(BattlenetRpcErrorCode.Ok, false);
            else
                SendAuthWaitQue(0);

            SetInQueue(false);
            ResetTimeOutTime();

            SendSetTimeZoneInformation();
            SendFeatureSystemStatusGlueScreen();
            SendClientCacheVersion(WorldConfig.GetUIntValue(WorldCfg.ClientCacheVersion));
            SendAvailableHotfixes(WorldConfig.GetIntValue(WorldCfg.HotfixCacheVersion));
            SendTutorialsData();

            SQLResult result = holder.GetResult(AccountInfoQueryLoad.GlobalRealmCharacterCounts);
            if (!result.IsEmpty())
            {
                do
                {
                    _realmCharacterCounts[new RealmHandle(result.Read<byte>(3), result.Read<byte>(4), result.Read<uint>(2)).GetAddress()] = result.Read<byte>(1);

                } while (result.NextRow());
            }

            SetSessionState bnetConnected = new SetSessionState();
            bnetConnected.State = 1;
            SendPacket(bnetConnected);

            _battlePetMgr.LoadFromDB(holder.GetResult(AccountInfoQueryLoad.BattlePets), holder.GetResult(AccountInfoQueryLoad.BattlePetSlot));

            realmHolder = null;
            holder = null;
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
                           _RBACData.GetId(), _RBACData.GetName(), Global.WorldMgr.GetRealm().Id.Realm);

            return hasPermission;
        }

        public void InvalidateRBACData()
        {
            Log.outDebug(LogFilter.Rbac, "WorldSession:Invalidaterbac:RBACData [AccountId: {0}, Name: {1}, realmId: {2}]",
                           _RBACData.GetId(), _RBACData.GetName(), Global.WorldMgr.GetRealm().Id.Realm);
            _RBACData = null;
        }

        AccountData GetAccountData(AccountDataTypes type) { return _accountData[(int)type]; }

        uint GetTutorialInt(byte index) { return tutorials[index]; }

        void SetTutorialInt(byte index, uint value)
        {
            if (tutorials[index] != value)
            {
                tutorials[index] = value;
                tutorialsChanged = true;
            }
        }

        public LocaleConstant GetSessionDbcLocale() { return m_sessionDbcLocale; }
        public LocaleConstant GetSessionDbLocaleIndex() { return m_sessionDbLocaleIndex; }

        public uint GetLatency() { return m_latency; }
        public void SetLatency(uint latency) { m_latency = latency; }
        public void ResetClientTimeDelay() { m_clientTimeDelay = 0; }
        void UpdateTimeOutTime(uint diff)
        {
            if (diff > m_timeOutTime)
                m_timeOutTime = 0;
            else
                m_timeOutTime -= diff;
        }
        public void ResetTimeOutTime() { m_timeOutTime = WorldConfig.GetIntValue(WorldCfg.SocketTimeouttime); }
        bool IsConnectionIdle() { return (m_timeOutTime <= 0 && !m_inQueue); }

        public uint GetRecruiterId() { return recruiterId; }
        public bool IsARecruiter() { return isRecruiter; }

        // Battle Pets
        public BattlePetMgr GetBattlePetMgr() { return _battlePetMgr; }
        public CollectionMgr GetCollectionMgr() { return _collectionMgr; }

        void ClearRedirectFlag(SessionFlags flag) { m_flags &= ~flag; }
        public bool WasRedirected() { return m_flags.HasAnyFlag(SessionFlags.FromRedirect); }
        bool HasRedirected() { return m_flags.HasAnyFlag(SessionFlags.HasRedirected); }

        // Battlenet
        public Array<byte> GetRealmListSecret() { return _realmListSecret; }
        void SetRealmListSecret(Array<byte> secret) { _realmListSecret = secret; }
        public Dictionary<uint, byte> GetRealmCharacterCounts() { return _realmCharacterCounts; }

        public static implicit operator bool(WorldSession session)
        {
            return session != null;
        }

        #region Fields
        List<ObjectGuid> _legitCharacters = new List<ObjectGuid>();
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

        uint expireTime;
        bool forceExit;

        DosProtection AntiDOS;
        Warden _warden;                                    // Remains NULL if Warden system is not enabled by config

        long _logoutTime;
        bool m_inQueue;
        SessionFlags m_flags;
        ObjectGuid m_playerLoading;                               // code processed in LoginPlayer
        bool m_playerLogout;                                // code processed in LogoutPlayer
        bool m_playerRecentlyLogout;
        bool m_playerSave;
        LocaleConstant m_sessionDbcLocale;
        LocaleConstant m_sessionDbLocaleIndex;
        uint m_latency;
        uint m_clientTimeDelay;
        AccountData[] _accountData = new AccountData[(int)AccountDataTypes.Max];
        uint[] tutorials = new uint[SharedConst.MaxAccountTutorialValues];
        bool tutorialsChanged;

        Array<byte> _realmListSecret = new Array<byte>(32);
        Dictionary<uint /*realmAddress*/, byte> _realmCharacterCounts = new Dictionary<uint, byte>();
        Dictionary<uint, Action<Google.Protobuf.CodedInputStream>> _battlenetResponseCallbacks = new Dictionary<uint, Action<Google.Protobuf.CodedInputStream>>();
        uint _battlenetRequestToken;

        List<string> _registeredAddonPrefixes = new List<string>();
        bool _filterAddonMessages;
        uint recruiterId;
        bool isRecruiter;

        public long m_muteTime;
        long m_timeOutTime;

        ConcurrentQueue<WorldPacket> _recvQueue = new ConcurrentQueue<WorldPacket>();
        RBACData _RBACData;

        ObjectGuid m_currentBankerGUID;

        CollectionMgr _collectionMgr;

        ConnectToKey _instanceConnectKey;

        BattlePetMgr _battlePetMgr;

        Task<SQLQueryHolder<AccountInfoQueryLoad>> _realmAccountLoginCallback;
        Task<SQLQueryHolder<AccountInfoQueryLoad>> _accountLoginCallback;
        Task<SQLQueryHolder<PlayerLoginQueryLoad>> _charLoginCallback;

        QueryCallbackProcessor _queryProcessor = new QueryCallbackProcessor();
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
        Dictionary<uint, PacketCounter> _PacketThrottlingMap = new Dictionary<uint, PacketCounter>();

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
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ACCOUNT_DATA);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountDataIndexPerRealm, stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_TUTORIALS);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.TutorialsIndexPerRealm, stmt);
        }
    }

    class AccountInfoQueryHolder : SQLQueryHolder<AccountInfoQueryLoad>
    {
        public void Initialize(uint accountId, uint battlenetAccountId)
        {
            PreparedStatement stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_TOYS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountToys, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BATTLE_PETS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.BattlePets, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BATTLE_PET_SLOTS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.BattlePetSlot, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_HEIRLOOMS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.GlobalAccountHeirlooms, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_ACCOUNT_MOUNTS);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.Mounts, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_CHARACTER_COUNTS_BY_ACCOUNT_ID);
            stmt.AddValue(0, accountId);
            SetQuery(AccountInfoQueryLoad.GlobalRealmCharacterCounts, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_APPEARANCES);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.ItemAppearances, stmt);

            stmt = DB.Login.GetPreparedStatement(LoginStatements.SEL_BNET_ITEM_FAVORITE_APPEARANCES);
            stmt.AddValue(0, battlenetAccountId);
            SetQuery(AccountInfoQueryLoad.ItemFavoriteAppearances, stmt);
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
        TutorialsIndexPerRealm
    }
}
