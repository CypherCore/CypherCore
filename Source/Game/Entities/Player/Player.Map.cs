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
using Game.DataStorage;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        public Difficulty GetDifficultyID(MapRecord mapEntry)
        {
            if (!mapEntry.IsRaid())
                return m_dungeonDifficulty;

            MapDifficultyRecord defaultDifficulty = Global.DB2Mgr.GetDefaultMapDifficulty(mapEntry.Id);
            if (defaultDifficulty == null)
                return m_legacyRaidDifficulty;

            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(defaultDifficulty.DifficultyID);
            if (difficulty == null || difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy))
                return m_legacyRaidDifficulty;

            return m_raidDifficulty;
        }
        public Difficulty GetDungeonDifficultyID() { return m_dungeonDifficulty; }
        public Difficulty GetRaidDifficultyID() { return m_raidDifficulty; }
        public Difficulty GetLegacyRaidDifficultyID() { return m_legacyRaidDifficulty; }
        public void SetDungeonDifficultyID(Difficulty dungeon_difficulty) { m_dungeonDifficulty = dungeon_difficulty; }
        public void SetRaidDifficultyID(Difficulty raid_difficulty) { m_raidDifficulty = raid_difficulty; }
        public void SetLegacyRaidDifficultyID(Difficulty raid_difficulty) { m_legacyRaidDifficulty = raid_difficulty; }

        public static Difficulty CheckLoadedDungeonDifficultyID(Difficulty difficulty)
        {
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (difficultyEntry == null)
                return Difficulty.Normal;

            if (difficultyEntry.InstanceType != MapTypes.Instance)
                return Difficulty.Normal;

            if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect))
                return Difficulty.Normal;

            return difficulty;
        }
        public static Difficulty CheckLoadedRaidDifficultyID(Difficulty difficulty)
        {
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (difficultyEntry == null)
                return Difficulty.NormalRaid;

            if (difficultyEntry.InstanceType != MapTypes.Raid)
                return Difficulty.NormalRaid;

            if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) || difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Legacy))
                return Difficulty.NormalRaid;

            return difficulty;
        }
        public static Difficulty CheckLoadedLegacyRaidDifficultyID(Difficulty difficulty)
        {
            DifficultyRecord difficultyEntry = CliDB.DifficultyStorage.LookupByKey(difficulty);
            if (difficultyEntry == null)
                return Difficulty.Raid10N;

            if (difficultyEntry.InstanceType != MapTypes.Raid)
                return Difficulty.Raid10N;

            if (!difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.CanSelect) || !difficultyEntry.Flags.HasAnyFlag(DifficultyFlags.Legacy))
                return Difficulty.Raid10N;

            return difficulty;
        }

        public void SendRaidGroupOnlyMessage(RaidGroupReason reason, int delay)
        {
            RaidGroupOnly raidGroupOnly = new RaidGroupOnly();
            raidGroupOnly.Delay = delay;
            raidGroupOnly.Reason = reason;

            SendPacket(raidGroupOnly);
        }

        void UpdateArea(uint newArea)
        {
            // FFA_PVP flags are area and not zone id dependent
            // so apply them accordingly
            m_areaUpdateId = newArea;

            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(newArea);
            pvpInfo.IsInFFAPvPArea = area != null && area.Flags[0].HasAnyFlag(AreaFlags.Arena);
            UpdatePvPState(true);

            PhasingHandler.OnAreaChange(this);
            UpdateAreaDependentAuras(newArea);

            if (IsAreaThatActivatesPvpTalents(newArea))
                EnablePvpRules();
            else
                DisablePvpRules();

            // previously this was in UpdateZone (but after UpdateArea) so nothing will break
            pvpInfo.IsInNoPvPArea = false;
            if (area != null && area.IsSanctuary())    // in sanctuary
            {
                SetByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.Sanctuary);
                pvpInfo.IsInNoPvPArea = true;
                if (duel == null)
                    CombatStopWithPets();
            }
            else
                RemoveByteFlag(UnitFields.Bytes2, UnitBytes2Offsets.PvpFlag, UnitBytes2Flags.Sanctuary);

            AreaFlags areaRestFlag = (GetTeam() == Team.Alliance) ? AreaFlags.RestZoneAlliance : AreaFlags.RestZoneHorde;
            if (area != null && area.Flags[0].HasAnyFlag(areaRestFlag))
                _restMgr.SetRestFlag(RestFlag.FactionArea);
            else
                _restMgr.RemoveRestFlag(RestFlag.FactionArea);
        }

        public void UpdateZone(uint newZone, uint newArea)
        {
            if (m_zoneUpdateId != newZone)
            {
                Global.OutdoorPvPMgr.HandlePlayerLeaveZone(this, m_zoneUpdateId);
                Global.OutdoorPvPMgr.HandlePlayerEnterZone(this, newZone);
                Global.BattleFieldMgr.HandlePlayerLeaveZone(this, m_zoneUpdateId);
                Global.BattleFieldMgr.HandlePlayerEnterZone(this, newZone);
                SendInitWorldStates(newZone, newArea);              // only if really enters to new zone, not just area change, works strange...
                Guild guild = GetGuild();
                if (guild)
                    guild.UpdateMemberData(this, GuildMemberData.ZoneId, newZone);
            }

            // group update
            if (GetGroup())
            {
                SetGroupUpdateFlag(GroupUpdateFlags.Full);

                Pet pet = GetPet();
                if (pet)
                    pet.SetGroupUpdateFlag(GroupUpdatePetFlags.Full);
            }

            m_zoneUpdateId = newZone;
            m_zoneUpdateTimer = 1 * Time.InMilliseconds;

            // zone changed, so area changed as well, update it
            UpdateArea(newArea);

            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(newZone);
            if (zone == null)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.Weather))
                GetMap().GetOrGenerateZoneDefaultWeather(newZone);

            GetMap().SendZoneDynamicInfo(newZone, this);

            Global.ScriptMgr.OnPlayerUpdateZone(this, newZone, newArea);

            // in PvP, any not controlled zone (except zone.team == 6, default case)
            // in PvE, only opposition team capital
            switch ((AreaTeams)zone.FactionGroupMask)
            {
                case AreaTeams.Ally:
                    pvpInfo.IsInHostileArea = GetTeam() != Team.Alliance && (Global.WorldMgr.IsPvPRealm() || zone.Flags[0].HasAnyFlag(AreaFlags.Capital));
                    break;
                case AreaTeams.Horde:
                    pvpInfo.IsInHostileArea = GetTeam() != Team.Horde && (Global.WorldMgr.IsPvPRealm() || zone.Flags[0].HasAnyFlag(AreaFlags.Capital));
                    break;
                case AreaTeams.None:
                    // overwrite for Battlegrounds, maybe batter some zone flags but current known not 100% fit to this
                    pvpInfo.IsInHostileArea = Global.WorldMgr.IsPvPRealm() || InBattleground() || zone.Flags[0].HasAnyFlag(AreaFlags.Wintergrasp);
                    break;
                default:                                            // 6 in fact
                    pvpInfo.IsInHostileArea = false;
                    break;
            }

            // Treat players having a quest flagging for PvP as always in hostile area
            pvpInfo.IsHostile = pvpInfo.IsInHostileArea || HasPvPForcingQuest();

            if (zone.Flags[0].HasAnyFlag(AreaFlags.Capital))                     // Is in a capital city
            {
                if (!pvpInfo.IsInHostileArea || zone.IsSanctuary())
                    _restMgr.SetRestFlag(RestFlag.City);
                pvpInfo.IsInNoPvPArea = true;
            }
            else
                _restMgr.RemoveRestFlag(RestFlag.City);

            UpdatePvPState();

            // remove items with area/map limitations (delete only for alive player to allow back in ghost mode)
            // if player resurrected at teleport this will be applied in resurrect code
            if (IsAlive())
                DestroyZoneLimitedItem(true, newZone);

            // check some item equip limitations (in result lost CanTitanGrip at talent reset, for example)
            AutoUnequipOffhandIfNeed();

            // recent client version not send leave/join channel packets for built-in local channels
            UpdateLocalChannels(newZone);

            UpdateZoneDependentAuras(newZone);
        }

        public InstanceBind GetBoundInstance(uint mapid, Difficulty difficulty, bool withExpired = false)
        {
            // some instances only have one difficulty
            MapDifficultyRecord mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(mapid, ref difficulty);
            if (mapDiff == null)
                return null;

            var difficultyDic = m_boundInstances.LookupByKey(difficulty);
            if (difficultyDic == null)
                return null;

            var instanceBind = difficultyDic.LookupByKey(mapid);
            if (instanceBind != null)
                if (instanceBind.extendState != 0 || withExpired)
                    return instanceBind;

            return null;
        }
        public Dictionary<uint, InstanceBind> GetBoundInstances(Difficulty difficulty) { return m_boundInstances.LookupByKey(difficulty); }

        public InstanceSave GetInstanceSave(uint mapid)
        {
            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(mapid);
            InstanceBind pBind = GetBoundInstance(mapid, GetDifficultyID(mapEntry));
            InstanceSave pSave = pBind?.save;
            if (pBind == null || !pBind.perm)
            {
                Group group = GetGroup();
                if (group)
                {
                    InstanceBind groupBind = group.GetBoundInstance(GetDifficultyID(mapEntry), mapid);
                    if (groupBind != null)
                        pSave = groupBind.save;
                }
            }

            return pSave;
        }

        public void UnbindInstance(uint mapid, Difficulty difficulty, bool unload = false)
        {
            var difficultyDic = m_boundInstances.LookupByKey(difficulty);
            if (difficultyDic != null)
            {
                var pair = difficultyDic.Find(mapid);
                if (pair.Value != null)
                    UnbindInstance(pair, difficultyDic, unload);
            }
        }

        public void UnbindInstance(KeyValuePair<uint, InstanceBind> pair, Dictionary<uint, InstanceBind> difficultyDic, bool unload)
        {
            if (pair.Value != null)
            {
                if (!unload)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INSTANCE_BY_INSTANCE_GUID);

                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, pair.Value.save.GetInstanceId());

                    DB.Characters.Execute(stmt);
                }

                if (pair.Value.perm)
                    GetSession().SendCalendarRaidLockout(pair.Value.save, false);

                pair.Value.save.RemovePlayer(this);               // save can become invalid
                difficultyDic.Remove(pair.Key);
            }
        }

        public InstanceBind BindToInstance(InstanceSave save, bool permanent, BindExtensionState extendState = BindExtensionState.Normal, bool load = false)
        {
            if (save != null)
            {
                InstanceBind bind = new InstanceBind();
                if (m_boundInstances.ContainsKey(save.GetDifficultyID()) && m_boundInstances[save.GetDifficultyID()].ContainsKey(save.GetMapId()))
                    bind = m_boundInstances[save.GetDifficultyID()][save.GetMapId()];

                if (extendState == BindExtensionState.Keep) // special flag, keep the player's current extend state when updating for new boss down
                {
                    if (save == bind.save)
                        extendState = bind.extendState;
                    else
                        extendState = BindExtensionState.Normal;
                }

                if (!load)
                {
                    PreparedStatement stmt;
                    if (bind.save != null)
                    {
                        // update the save when the group kills a boss
                        if (permanent != bind.perm || save != bind.save || extendState != bind.extendState)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_CHAR_INSTANCE);

                            stmt.AddValue(0, save.GetInstanceId());
                            stmt.AddValue(1, permanent);
                            stmt.AddValue(2, extendState);
                            stmt.AddValue(3, GetGUID().GetCounter());
                            stmt.AddValue(4, bind.save.GetInstanceId());

                            DB.Characters.Execute(stmt);
                        }
                    }
                    else
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_CHAR_INSTANCE);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, save.GetInstanceId());
                        stmt.AddValue(2, permanent);
                        stmt.AddValue(3, extendState);
                        DB.Characters.Execute(stmt);
                    }
                }

                if (bind.save != save)
                {
                    if (bind.save != null)
                        bind.save.RemovePlayer(this);
                    save.AddPlayer(this);
                }

                if (permanent)
                    save.SetCanReset(false);

                bind.save = save;
                bind.perm = permanent;
                bind.extendState = extendState;
                if (!load)
                    Log.outDebug(LogFilter.Maps, "Player.BindToInstance: Player '{0}' ({1}) is now bound to map (ID: {2}, Instance {3}, Difficulty {4})", GetName(), GetGUID().ToString(), save.GetMapId(), save.GetInstanceId(), save.GetDifficultyID());

                Global.ScriptMgr.OnPlayerBindToInstance(this, save.GetDifficultyID(), save.GetMapId(), permanent, extendState);

                if (!m_boundInstances.ContainsKey(save.GetDifficultyID()))
                    m_boundInstances[save.GetDifficultyID()] = new Dictionary<uint, InstanceBind>();

                m_boundInstances[save.GetDifficultyID()][save.GetMapId()] = bind;
                return bind;
            }

            return null;
        }

        public void BindToInstance()
        {
            InstanceSave mapSave = Global.InstanceSaveMgr.GetInstanceSave(_pendingBindId);
            if (mapSave == null) //it seems sometimes mapSave is NULL, but I did not check why
                return;

            InstanceSaveCreated data = new InstanceSaveCreated();
            data.Gm = IsGameMaster();
            SendPacket(data);
            if (!IsGameMaster())
            {
                BindToInstance(mapSave, true, BindExtensionState.Keep);
                GetSession().SendCalendarRaidLockout(mapSave, true);
            }
        }

        public void SetPendingBind(uint instanceId, uint bindTimer)
        {
            _pendingBindId = instanceId;
            _pendingBindTimer = bindTimer;
        }

        public void SendRaidInfo()
        {
            InstanceInfoPkt instanceInfo = new InstanceInfoPkt();

            long now = Time.UnixTime;
            foreach (var difficultyDic in m_boundInstances.Values)
            {
                foreach (var instanceBind in difficultyDic.Values)
                {
                    if (instanceBind.perm)
                    {
                        InstanceSave save = instanceBind.save;

                        InstanceLock lockInfos;
                        lockInfos.InstanceID = save.GetInstanceId();
                        lockInfos.MapID = save.GetMapId();
                        lockInfos.DifficultyID = (uint)save.GetDifficultyID();
                        if (instanceBind.extendState != BindExtensionState.Extended)
                            lockInfos.TimeRemaining = (int)(save.GetResetTime() - now);
                        else
                            lockInfos.TimeRemaining = (int)(Global.InstanceSaveMgr.GetSubsequentResetTime(save.GetMapId(), save.GetDifficultyID(), save.GetResetTime()) - now);

                        lockInfos.CompletedMask = 0;
                        Map map = Global.MapMgr.FindMap(save.GetMapId(), save.GetInstanceId());
                        if (map != null)
                        {
                            InstanceScript instanceScript = ((InstanceMap)map).GetInstanceScript();
                            if (instanceScript != null)
                                lockInfos.CompletedMask = instanceScript.GetCompletedEncounterMask();
                        }

                        lockInfos.Locked = instanceBind.extendState != BindExtensionState.Expired;
                        lockInfos.Extended = instanceBind.extendState == BindExtensionState.Extended;

                        instanceInfo.LockList.Add(lockInfos);
                    }
                }
            }

            SendPacket(instanceInfo);
        }

        public bool Satisfy(AccessRequirement ar, uint target_map, bool report = false)
        {
            if (!IsGameMaster() && ar != null)
            {
                byte LevelMin = 0;
                byte LevelMax = 0;

                MapRecord mapEntry = CliDB.MapStorage.LookupByKey(target_map);
                if (mapEntry == null)
                    return false;

                if (!WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreLevel))
                {
                    if (ar.levelMin != 0 && getLevel() < ar.levelMin)
                        LevelMin = ar.levelMin;
                    if (ar.levelMax != 0 && getLevel() > ar.levelMax)
                        LevelMax = ar.levelMax;
                }

                uint missingItem = 0;
                if (ar.item != 0)
                {
                    if (!HasItemCount(ar.item) &&
                        (ar.item2 == 0 || !HasItemCount(ar.item2)))
                        missingItem = ar.item;
                }
                else if (ar.item2 != 0 && !HasItemCount(ar.item2))
                    missingItem = ar.item2;

                if (Global.DisableMgr.IsDisabledFor(DisableType.Map, target_map, this))
                {
                    GetSession().SendNotification("{0}", Global.ObjectMgr.GetCypherString(CypherStrings.InstanceClosed));
                    return false;
                }

                uint missingQuest = 0;
                if (GetTeam() == Team.Alliance && ar.quest_A != 0 && !GetQuestRewardStatus(ar.quest_A))
                    missingQuest = ar.quest_A;
                else if (GetTeam() == Team.Horde && ar.quest_H != 0 && !GetQuestRewardStatus(ar.quest_H))
                    missingQuest = ar.quest_H;

                uint missingAchievement = 0;
                Player leader = this;
                ObjectGuid leaderGuid = GetGroup() != null ? GetGroup().GetLeaderGUID() : GetGUID();
                if (leaderGuid != GetGUID())
                    leader = Global.ObjAccessor.FindPlayer(leaderGuid);

                if (ar.achievement != 0)
                    if (leader == null || !leader.HasAchieved(ar.achievement))
                        missingAchievement = ar.achievement;

                Difficulty target_difficulty = GetDifficultyID(mapEntry);
                MapDifficultyRecord mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(target_map, ref target_difficulty);
                if (LevelMin != 0 || LevelMax != 0 || missingItem != 0 || missingQuest != 0 || missingAchievement != 0)
                {
                    if (report)
                    {
                        if (missingQuest != 0 && !string.IsNullOrEmpty(ar.questFailedText))
                            SendSysMessage("{0}", ar.questFailedText);
                        else if (mapDiff.Message.HasString(Global.WorldMgr.GetDefaultDbcLocale())) // if (missingAchievement) covered by this case
                            SendTransferAborted(target_map, TransferAbortReason.Difficulty, (byte)target_difficulty);
                        else if (missingItem != 0)
                            GetSession().SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.LevelMinrequiredAndItem), LevelMin, Global.ObjectMgr.GetItemTemplate(missingItem).GetName());
                        else if (LevelMin != 0)
                            GetSession().SendNotification(Global.ObjectMgr.GetCypherString(CypherStrings.LevelMinrequired), LevelMin);
                    }
                    return false;
                }
            }
            return true;
        }

        bool IsInstanceLoginGameMasterException()
        {
            if (!CanBeGameMaster())
                return false;

            SendSysMessage(CypherStrings.InstanceLoginGamemasterException);
            return true;
        }

        public bool CheckInstanceValidity(bool isLogin)
        {
            // game masters' instances are always valid
            if (IsGameMaster())
                return true;

            // non-instances are always valid
            Map map = GetMap();
            if (!map || !map.IsDungeon())
                return true;

            // raid instances require the player to be in a raid group to be valid
            if (map.IsRaid() && !WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreRaid) && (map.GetEntry().Expansion() >= (Expansion)WorldConfig.GetIntValue(WorldCfg.Expansion)))
                if (!GetGroup() || !GetGroup().isRaidGroup())
                    return false;

            Group group = GetGroup();
            if (group)
            {
                // check if player's group is bound to this instance
                InstanceBind bind = group.GetBoundInstance(map.GetDifficultyID(), map.GetId());
                if (bind == null || bind.save == null || bind.save.GetInstanceId() != map.GetInstanceId())
                    return false;

                var players = map.GetPlayers();
                if (!players.Empty())
                    foreach (var otherPlayer in players)
                    {
                        if (otherPlayer.IsGameMaster())
                            continue;
                        if (!otherPlayer.m_InstanceValid) // ignore players that currently have a homebind timer active
                            continue;
                        if (group != otherPlayer.GetGroup())
                            return false;
                    }
            }
            else
            {
                // instance is invalid if we are not grouped and there are other players
                if (map.GetPlayersCountExceptGMs() > 1)
                    return false;

                // check if the player is bound to this instance
                InstanceBind bind = GetBoundInstance(map.GetId(), map.GetDifficultyID());
                if (bind == null || bind.save == null || bind.save.GetInstanceId() != map.GetInstanceId())
                    return false;
            }

            return true;
        }

        public bool CheckInstanceCount(uint instanceId)
        {
            if (_instanceResetTimes.Count < WorldConfig.GetIntValue(WorldCfg.MaxInstancesPerHour))
                return true;
            return _instanceResetTimes.ContainsKey(instanceId);
        }

        public void AddInstanceEnterTime(uint instanceId, long enterTime)
        {
            if (!_instanceResetTimes.ContainsKey(instanceId))
                _instanceResetTimes.Add(instanceId, enterTime + Time.Hour);
        }

        public void SendDungeonDifficulty(int forcedDifficulty = -1)
        {
            DungeonDifficultySet dungeonDifficultySet = new DungeonDifficultySet();
            dungeonDifficultySet.DifficultyID = forcedDifficulty == -1 ? (int)GetDungeonDifficultyID() : forcedDifficulty;
            SendPacket(dungeonDifficultySet);
        }

        public void SendRaidDifficulty(bool legacy, int forcedDifficulty = -1)
        {
            RaidDifficultySet raidDifficultySet = new RaidDifficultySet();
            raidDifficultySet.DifficultyID = forcedDifficulty == -1 ? (int)(legacy ? GetLegacyRaidDifficultyID() : GetRaidDifficultyID()) : forcedDifficulty;
            raidDifficultySet.Legacy = legacy;
            SendPacket(raidDifficultySet);
        }

        public void SendResetFailedNotify(uint mapid)
        {
            SendPacket(new ResetFailedNotify());
        }

        // Reset all solo instances and optionally send a message on success for each
        public void ResetInstances(InstanceResetMethod method, bool isRaid, bool isLegacy)
        {
            // method can be INSTANCE_RESET_ALL, INSTANCE_RESET_CHANGE_DIFFICULTY, INSTANCE_RESET_GROUP_JOIN

            // we assume that when the difficulty changes, all instances that can be reset will be
            Difficulty difficulty = GetDungeonDifficultyID();
            if (isRaid)
            {
                if (!isLegacy)
                    difficulty = GetRaidDifficultyID();
                else
                    difficulty = GetLegacyRaidDifficultyID();
            }

            var difficultyDic = m_boundInstances.LookupByKey(difficulty);
            if (difficultyDic == null)
                return;

            foreach (var pair in difficultyDic)
            {
                InstanceSave p = pair.Value.save;
                MapRecord entry = CliDB.MapStorage.LookupByKey(difficulty);
                if (entry == null || entry.IsRaid() != isRaid || !p.CanReset())
                    continue;

                if (method == InstanceResetMethod.All)
                {
                    // the "reset all instances" method can only reset normal maps
                    if (entry.InstanceType == MapTypes.Raid || difficulty == Difficulty.Heroic)
                        continue;
                }

                // if the map is loaded, reset it
                Map map = Global.MapMgr.FindMap(p.GetMapId(), p.GetInstanceId());
                if (map != null && map.IsDungeon())
                    if (!map.ToInstanceMap().Reset(method))
                        continue;

                // since this is a solo instance there should not be any players inside
                if (method == InstanceResetMethod.All || method == InstanceResetMethod.ChangeDifficulty)
                    SendResetInstanceSuccess(p.GetMapId());

                p.DeleteFromDB();
                difficultyDic.Remove(pair.Key);

                // the following should remove the instance save from the manager and delete it as well
                p.RemovePlayer(this);
            }
        }

        public void SendResetInstanceSuccess(uint MapId)
        {
            InstanceReset data = new InstanceReset();
            data.MapID = MapId;
            SendPacket(data);
        }

        public void SendResetInstanceFailed(ResetFailedReason reason, uint MapId)
        {
            InstanceResetFailed data = new InstanceResetFailed();
            data.MapID = MapId;
            data.ResetFailedReason = reason;
            SendPacket(data);
        }

        public void SendTransferAborted(uint mapid, TransferAbortReason reason, byte arg = 0)
        {
            TransferAborted transferAborted = new TransferAborted();
            transferAborted.MapID = mapid;
            transferAborted.Arg = arg;
            transferAborted.TransfertAbort = reason;
            SendPacket(transferAborted);
        }

        public void SendInstanceResetWarning(uint mapid, Difficulty difficulty, uint time, bool welcome)
        {
            // type of warning, based on the time remaining until reset
            InstanceResetWarningType type;
            if (welcome)
                type = InstanceResetWarningType.Welcome;
            else if (time > 21600)
                type = InstanceResetWarningType.Welcome;
            else if (time > 3600)
                type = InstanceResetWarningType.WarningHours;
            else if (time > 300)
                type = InstanceResetWarningType.WarningMin;
            else
                type = InstanceResetWarningType.WarningMinSoon;

            RaidInstanceMessage raidInstanceMessage = new RaidInstanceMessage();
            raidInstanceMessage.Type = type;
            raidInstanceMessage.MapID = mapid;
            raidInstanceMessage.DifficultyID = difficulty;

            InstanceBind bind = GetBoundInstance(mapid, difficulty);
            if (bind != null)
                raidInstanceMessage.Locked = bind.perm;
            else
                raidInstanceMessage.Locked = false;
            raidInstanceMessage.Extended = false;
            SendPacket(raidInstanceMessage);
        }

        public override void UpdateUnderwaterState(Map m, float x, float y, float z)
        {
            LiquidData liquid_status;
            ZLiquidStatus res = m.getLiquidStatus(GetPhaseShift(), x, y, z, MapConst.MapAllLiquidTypes, out liquid_status);
            if (res == 0)
            {
                m_MirrorTimerFlags &= ~(PlayerUnderwaterState.InWater | PlayerUnderwaterState.InLava | PlayerUnderwaterState.InSlime | PlayerUnderwaterState.InDarkWater);
                if (_lastLiquid != null && _lastLiquid.SpellID != 0)
                    RemoveAurasDueToSpell(_lastLiquid.SpellID);

                _lastLiquid = null;
                return;
            }
            uint liqEntry = liquid_status.entry;
            if (liqEntry != 0)
            {
                LiquidTypeRecord liquid = CliDB.LiquidTypeStorage.LookupByKey(liqEntry);
                if (_lastLiquid != null && _lastLiquid.SpellID != 0 && _lastLiquid != liquid)
                    RemoveAurasDueToSpell(_lastLiquid.SpellID);

                if (liquid != null && liquid.SpellID != 0)
                {
                    if (res.HasAnyFlag(ZLiquidStatus.UnderWater | ZLiquidStatus.InWater))
                    {
                        if (!HasAura(liquid.SpellID))
                            CastSpell(this, liquid.SpellID, true);
                    }
                    else
                        RemoveAurasDueToSpell(liquid.SpellID);
                }

                _lastLiquid = liquid;
            }
            else if (_lastLiquid != null && _lastLiquid.SpellID != 0)
            {
                RemoveAurasDueToSpell(_lastLiquid.SpellID);
                _lastLiquid = null;
            }


            // All liquids type - check under water position
            if (liquid_status.type_flags.HasAnyFlag<uint>(MapConst.MapLiquidTypeWater | MapConst.MapLiquidTypeOcean | MapConst.MapLiquidTypeMagma | MapConst.MapLiquidTypeSlime))
            {
                if (res.HasAnyFlag(ZLiquidStatus.UnderWater))
                    m_MirrorTimerFlags |= PlayerUnderwaterState.InWater;
                else
                    m_MirrorTimerFlags &= ~PlayerUnderwaterState.InWater;
            }

            // Allow travel in dark water on taxi or transport
            if (liquid_status.type_flags.HasAnyFlag<uint>(MapConst.MapLiquidTypeDarkWater) && !IsInFlight() && GetTransport() == null)
                m_MirrorTimerFlags |= PlayerUnderwaterState.InDarkWater;
            else
                m_MirrorTimerFlags &= ~PlayerUnderwaterState.InDarkWater;

            // in lava check, anywhere in lava level
            if (liquid_status.type_flags.HasAnyFlag<uint>(MapConst.MapLiquidTypeMagma))
            {
                if (res.HasAnyFlag(ZLiquidStatus.UnderWater | ZLiquidStatus.InWater | ZLiquidStatus.WaterWalk))
                    m_MirrorTimerFlags |= PlayerUnderwaterState.InLava;
                else
                    m_MirrorTimerFlags &= ~PlayerUnderwaterState.InLava;
            }
            // in slime check, anywhere in slime level
            if (liquid_status.type_flags.HasAnyFlag<uint>(MapConst.MapLiquidTypeSlime))
            {
                if (res.HasAnyFlag(ZLiquidStatus.UnderWater | ZLiquidStatus.InWater | ZLiquidStatus.WaterWalk))
                    m_MirrorTimerFlags |= PlayerUnderwaterState.InSlime;
                else
                    m_MirrorTimerFlags &= ~PlayerUnderwaterState.InSlime;
            }
        }
    }
}
