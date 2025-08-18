// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.DataStorage;
using Game.Groups;
using Game.Guilds;
using Game.Maps;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

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
            if (difficulty == null || difficulty.HasFlag(DifficultyFlags.Legacy))
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

            if (!difficultyEntry.HasFlag(DifficultyFlags.CanSelect))
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

            if (!difficultyEntry.HasFlag(DifficultyFlags.CanSelect) || difficultyEntry.HasFlag(DifficultyFlags.Legacy))
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

            if (!difficultyEntry.HasFlag(DifficultyFlags.CanSelect) || !difficultyEntry.HasFlag(DifficultyFlags.Legacy))
                return Difficulty.Raid10N;

            return difficulty;
        }

        public void SendRaidGroupOnlyMessage(RaidGroupReason reason, int delay)
        {
            RaidGroupOnly raidGroupOnly = new();
            raidGroupOnly.Delay = delay;
            raidGroupOnly.Reason = reason;

            SendPacket(raidGroupOnly);
        }

        void UpdateArea(uint newArea)
        {
            // FFA_PVP flags are area and not zone id dependent
            // so apply them accordingly
            uint oldArea = m_areaUpdateId;
            m_areaUpdateId = newArea;

            AreaTableRecord oldAreaEntry = CliDB.AreaTableStorage.LookupByKey(oldArea);
            AreaTableRecord area = CliDB.AreaTableStorage.LookupByKey(newArea);
            bool oldFFAPvPArea = pvpInfo.IsInFFAPvPArea;
            pvpInfo.IsInFFAPvPArea = area != null && area.HasFlag(AreaFlags.FreeForAllPvP);
            UpdatePvPState(true);

            // check if we were in ffa arena and we left
            if (oldFFAPvPArea && !pvpInfo.IsInFFAPvPArea)
                ValidateAttackersAndOwnTarget();

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
                SetPvpFlag(UnitPVPStateFlags.Sanctuary);
                pvpInfo.IsInNoPvPArea = true;
                if (duel == null && GetCombatManager().HasPvPCombat())
                    CombatStopWithPets();
            }
            else
                RemovePvpFlag(UnitPVPStateFlags.Sanctuary);

            AreaFlags areaRestFlag = (GetTeam() == Team.Alliance) ? AreaFlags.AllianceResting : AreaFlags.HordeResting;
            if (area != null && area.HasFlag(areaRestFlag))
                _restMgr.SetRestFlag(RestFlag.FactionArea);
            else
                _restMgr.RemoveRestFlag(RestFlag.FactionArea);

            PushQuests();

            UpdateMountCapability();

            if ((oldAreaEntry != null && oldAreaEntry.HasFlag(AreaFlags2.UseSubzoneForChatChannel))
                || (area != null && area.HasFlag(AreaFlags2.UseSubzoneForChatChannel)))
                UpdateLocalChannels(newArea);

            if (oldArea != newArea)
            {
                UpdateCriteria(CriteriaType.EnterArea, newArea);
                UpdateCriteria(CriteriaType.LeaveArea, oldArea);
            }
        }

        public void UpdateZone(uint newZone, uint newArea)
        {
            if (!IsInWorld)
                return;

            uint oldZone = m_zoneUpdateId;
            m_zoneUpdateId = newZone;

            GetMap().UpdatePlayerZoneStats(oldZone, newZone);

            // call leave script hooks immedately (before updating flags)
            if (oldZone != newZone)
            {
                Global.OutdoorPvPMgr.HandlePlayerLeaveZone(this, oldZone);
                Global.BattleFieldMgr.HandlePlayerLeaveZone(this, oldZone);
            }

            // group update
            if (GetGroup() != null)
            {
                SetGroupUpdateFlag(GroupUpdateFlags.Full);

                Pet pet = GetPet();
                if (pet != null)
                    pet.SetGroupUpdateFlag(GroupUpdatePetFlags.Full);
            }

            // zone changed, so area changed as well, update it
            UpdateArea(newArea);

            AreaTableRecord zone = CliDB.AreaTableStorage.LookupByKey(newZone);
            if (zone == null)
                return;

            if (WorldConfig.GetBoolValue(WorldCfg.Weather))
                GetMap().GetOrGenerateZoneDefaultWeather(newZone);

            GetMap().SendZoneDynamicInfo(newZone, this);

            UpdateWarModeAuras();

            UpdateHostileAreaState(zone);

            if (zone.HasFlag(AreaFlags.LinkedChat))                     // Is in a capital city
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
            var newAreaEntry = CliDB.AreaTableStorage.LookupByKey(newArea);
            if (newAreaEntry == null || !newAreaEntry.HasFlag(AreaFlags2.UseSubzoneForChatChannel))
                UpdateLocalChannels(newZone);

            UpdateZoneDependentAuras(newZone);

            // call enter script hooks after everyting else has processed
            Global.ScriptMgr.OnPlayerUpdateZone(this, newZone, newArea);
            if (oldZone != newZone)
            {
                Global.OutdoorPvPMgr.HandlePlayerEnterZone(this, newZone);
                Global.BattleFieldMgr.HandlePlayerEnterZone(this, newZone);
                SendInitWorldStates(newZone, newArea);              // only if really enters to new zone, not just area change, works strange...
                Guild guild = GetGuild();
                if (guild != null)
                    guild.UpdateMemberData(this, GuildMemberData.ZoneId, newZone);

                UpdateCriteria(CriteriaType.EnterTopLevelArea, newZone);
                UpdateCriteria(CriteriaType.LeaveTopLevelArea, oldZone);

                VignetteUpdate vignetteUpdate = new();

                foreach (var vignette in GetMap().GetInfiniteAOIVignettes())
                {
                    if (!vignette.Data.HasFlag(VignetteFlags.ZoneInfiniteAOI))
                        continue;

                    if (vignette.ZoneID == newZone && Vignettes.CanSee(this, vignette))
                        vignette.FillPacket(vignetteUpdate.Added);
                    else if (vignette.ZoneID == oldZone)
                        vignetteUpdate.Removed.Add(vignette.Guid);
                }

                if (!vignetteUpdate.Added.IDs.Empty() || !vignetteUpdate.Removed.Empty())
                    SendPacket(vignetteUpdate);
            }
        }

        public void UpdateHostileAreaState(AreaTableRecord area)
        {
            ZonePVPTypeOverride overrideZonePvpType = GetOverrideZonePVPType();

            pvpInfo.IsInHostileArea = false;

            if (area.IsSanctuary()) // sanctuary and arena cannot be overriden
                pvpInfo.IsInHostileArea = false;
            else if (area.HasFlag(AreaFlags.FreeForAllPvP))
                pvpInfo.IsInHostileArea = true;
            else if (overrideZonePvpType == ZonePVPTypeOverride.None)
            {
                if (area != null)
                {
                    if (InBattleground() || area.HasFlag(AreaFlags.CombatZone) || (area.PvpCombatWorldStateID != -1 && Global.WorldStateMgr.GetValue(area.PvpCombatWorldStateID, GetMap()) != 0))
                        pvpInfo.IsInHostileArea = true;
                    else if (IsWarModeLocalActive() || area.HasFlag(AreaFlags.EnemiesPvPFlagged))
                    {
                        if (area.HasFlag(AreaFlags.Contested))
                            pvpInfo.IsInHostileArea = IsWarModeLocalActive();
                        else
                        {
                            FactionTemplateRecord factionTemplate = GetFactionTemplateEntry();
                            if (factionTemplate == null || factionTemplate.FriendGroup.HasAnyFlag(area.FactionGroupMask))
                                pvpInfo.IsInHostileArea = false; // friend area are considered hostile if war mode is active
                            else if (factionTemplate.EnemyGroup.HasAnyFlag(area.FactionGroupMask))
                                pvpInfo.IsInHostileArea = true;
                            else
                                pvpInfo.IsInHostileArea = Global.WorldMgr.IsPvPRealm();
                        }
                    }
                }
            }
            else
            {
                switch (overrideZonePvpType)
                {
                    case ZonePVPTypeOverride.Friendly:
                        pvpInfo.IsInHostileArea = false;
                        break;
                    case ZonePVPTypeOverride.Hostile:
                    case ZonePVPTypeOverride.Contested:
                    case ZonePVPTypeOverride.Combat:
                        pvpInfo.IsInHostileArea = true;
                        break;
                    default:
                        break;
                }
            }

            // Treat players having a quest flagging for PvP as always in hostile area
            pvpInfo.IsHostile = pvpInfo.IsInHostileArea || HasPvPForcingQuest() || IsWarModeLocalActive();
        }

        public ZonePVPTypeOverride GetOverrideZonePVPType() { return (ZonePVPTypeOverride)(uint)m_activePlayerData.OverrideZonePVPType; }
        public void SetOverrideZonePVPType(ZonePVPTypeOverride type) { SetUpdateFieldValue(m_values.ModifyValue(m_activePlayerData).ModifyValue(m_activePlayerData.OverrideZonePVPType), (uint)type); }

        public void ConfirmPendingBind()
        {
            InstanceMap map = GetMap().ToInstanceMap();
            if (map == null || map.GetInstanceId() != _pendingBindId)
                return;

            if (!IsGameMaster())
                map.CreateInstanceLockForPlayer(this);
        }

        public void SetPendingBind(uint instanceId, uint bindTimer)
        {
            _pendingBindId = instanceId;
            _pendingBindTimer = bindTimer;
        }

        public void SendRaidInfo()
        {
            DateTime now = GameTime.GetSystemTime();

            var instanceLocks = Global.InstanceLockMgr.GetInstanceLocksForPlayer(GetGUID());

            InstanceInfoPkt instanceInfo = new();

            foreach (InstanceLock instanceLock in instanceLocks)
            {
                InstanceLockPkt lockInfos = new();
                lockInfos.InstanceID = instanceLock.GetInstanceId();
                lockInfos.MapID = instanceLock.GetMapId();
                lockInfos.DifficultyID = (uint)instanceLock.GetDifficultyId();
                lockInfos.TimeRemaining = (int)Math.Max((instanceLock.GetEffectiveExpiryTime() - now).TotalSeconds, 0);
                lockInfos.CompletedMask = instanceLock.GetData().CompletedEncountersMask;

                lockInfos.Locked = !instanceLock.IsExpired();
                lockInfos.Extended = instanceLock.IsExtended();

                instanceInfo.LockList.Add(lockInfos);
            }

            SendPacket(instanceInfo);
        }

        public bool Satisfy(AccessRequirement ar, uint target_map, TransferAbortParams abortParams = null, bool report = false)
        {
            if (!IsGameMaster())
            {
                byte LevelMin = 0;
                byte LevelMax = 0;
                uint failedMapDifficultyXCondition = 0;
                uint missingItem = 0;
                uint missingQuest = 0;
                uint missingAchievement = 0;

                MapRecord mapEntry = CliDB.MapStorage.LookupByKey(target_map);
                if (mapEntry == null)
                    return false;

                Difficulty target_difficulty = GetDifficultyID(mapEntry);
                MapDifficultyRecord mapDiff = Global.DB2Mgr.GetDownscaledMapDifficultyData(target_map, ref target_difficulty);
                if (!WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreLevel))
                {
                    var mapDifficultyConditions = Global.DB2Mgr.GetMapDifficultyConditions(mapDiff.Id);
                    foreach (var pair in mapDifficultyConditions)
                    {
                        if (!ConditionManager.IsPlayerMeetingCondition(this, pair.Item2.Id))
                        {
                            failedMapDifficultyXCondition = pair.Item1;
                            break;
                        }
                    }
                }

                if (ar != null)
                {
                    if (!WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreLevel))
                    {
                        if (ar.levelMin != 0 && GetLevel() < ar.levelMin)
                            LevelMin = ar.levelMin;
                        if (ar.levelMax != 0 && GetLevel() > ar.levelMax)
                            LevelMax = ar.levelMax;
                    }

                    if (ar.item != 0)
                    {
                        if (!HasItemCount(ar.item) &&
                        (ar.item2 == 0 || !HasItemCount(ar.item2)))
                            missingItem = ar.item;
                    }
                    else if (ar.item2 != 0 && !HasItemCount(ar.item2))
                        missingItem = ar.item2;

                    if (GetTeam() == Team.Alliance && ar.quest_A != 0 && !GetQuestRewardStatus(ar.quest_A))
                        missingQuest = ar.quest_A;
                    else if (GetTeam() == Team.Horde && ar.quest_H != 0 && !GetQuestRewardStatus(ar.quest_H))
                        missingQuest = ar.quest_H;

                    Player leader = this;
                    ObjectGuid leaderGuid = GetGroup() != null ? GetGroup().GetLeaderGUID() : GetGUID();
                    if (leaderGuid != GetGUID())
                        leader = Global.ObjAccessor.FindPlayer(leaderGuid);

                    if (ar.achievement != 0)
                        if (leader == null || !leader.HasAchieved(ar.achievement))
                            missingAchievement = ar.achievement;
                }

                if (LevelMin != 0 || LevelMax != 0 || failedMapDifficultyXCondition != 0 || missingItem != 0 || missingQuest != 0 || missingAchievement != 0)
                {
                    if (abortParams != null)
                        abortParams.Reason = TransferAbortReason.Error;

                    if (report)
                    {
                        if (missingQuest != 0 && !string.IsNullOrEmpty(ar.questFailedText))
                            SendSysMessage("{0}", ar.questFailedText);
                        else if (!mapDiff.Message[Global.WorldMgr.GetDefaultDbcLocale()].IsEmpty() && mapDiff.Message[Global.WorldMgr.GetDefaultDbcLocale()][0] != '\0' || failedMapDifficultyXCondition != 0) // if (missingAchievement) covered by this case
                        {
                            if (abortParams != null)
                            {
                                abortParams.Reason = TransferAbortReason.Difficulty;
                                abortParams.Arg = (byte)target_difficulty;
                                abortParams.MapDifficultyXConditionId = failedMapDifficultyXCondition;
                            }
                        }
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
            InstanceMap instance = map?.ToInstanceMap();
            if (instance == null)
                return true;

            Group group = GetGroup();
            // raid instances require the player to be in a raid group to be valid
            if (map.IsRaid() && !WorldConfig.GetBoolValue(WorldCfg.InstanceIgnoreRaid) && (map.GetEntry().Expansion() >= (Expansion)WorldConfig.GetIntValue(WorldCfg.Expansion)))
                if (group == null || group.IsRaidGroup())
                    return false;

            if (group != null)
            {
                // check if player's group is bound to this instance
                if (group != instance.GetOwningGroup())
                    return false;
            }
            else
            {
                // instance is invalid if we are not grouped and there are other players
                if (map.GetPlayersCountExceptGMs() > 1)
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

        public WorldSafeLocsEntry GetInstanceEntrance(uint targetMapId)
        {
            WorldSafeLocsEntry entranceLocation = null;
            MapRecord mapEntry = CliDB.MapStorage.LookupByKey(targetMapId);

            if (mapEntry.Instanceable())
            {
                // Check if we can contact the instancescript of the instance for an updated entrance location
                uint targetInstanceId = Global.MapMgr.FindInstanceIdForPlayer(targetMapId, this);
                if (targetInstanceId != 0)
                {
                    Map map = Global.MapMgr.FindMap(targetMapId, targetInstanceId);
                    if (map != null)
                    {
                        InstanceMap instanceMap = map.ToInstanceMap();
                        if (instanceMap != null)
                        {
                            InstanceScript instanceScript = instanceMap.GetInstanceScript();
                            if (instanceScript != null)
                                entranceLocation = Global.ObjectMgr.GetWorldSafeLoc(instanceScript.GetEntranceLocation());
                        }
                    }
                }

                // Finally check with the instancesave for an entrance location if we did not get a valid one from the instancescript
                if (entranceLocation == null)
                {
                    Group group = GetGroup();
                    Difficulty difficulty = group != null ? group.GetDifficultyID(mapEntry) : GetDifficultyID(mapEntry);
                    ObjectGuid instanceOwnerGuid = group != null ? group.GetRecentInstanceOwner(targetMapId) : GetGUID();

                    InstanceLock instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(instanceOwnerGuid, new MapDb2Entries(mapEntry, Global.DB2Mgr.GetDownscaledMapDifficultyData(targetMapId, ref difficulty)));
                    if (instanceLock != null)
                        entranceLocation = Global.ObjectMgr.GetWorldSafeLoc(instanceLock.GetData().EntranceWorldSafeLocId);
                }
            }
            return entranceLocation;
        }

        public void SendDungeonDifficulty(int forcedDifficulty = -1)
        {
            DungeonDifficultySet dungeonDifficultySet = new();
            dungeonDifficultySet.DifficultyID = forcedDifficulty == -1 ? (int)GetDungeonDifficultyID() : forcedDifficulty;
            SendPacket(dungeonDifficultySet);
        }

        public void SendRaidDifficulty(bool legacy, int forcedDifficulty = -1)
        {
            RaidDifficultySet raidDifficultySet = new();
            raidDifficultySet.DifficultyID = forcedDifficulty == -1 ? (int)(legacy ? GetLegacyRaidDifficultyID() : GetRaidDifficultyID()) : forcedDifficulty;
            raidDifficultySet.Legacy = legacy ? 1 : 0;
            SendPacket(raidDifficultySet);
        }

        public void SendResetFailedNotify(uint mapid)
        {
            SendPacket(new ResetFailedNotify());
        }

        // Reset all solo instances and optionally send a message on success for each
        public void ResetInstances(InstanceResetMethod method)
        {
            foreach (var (mapId, instanceId) in m_recentInstances.ToList())
            {
                Map map = Global.MapMgr.FindMap(mapId, instanceId);
                bool forgetInstance = false;
                if (map != null)
                {
                    InstanceMap instance = map.ToInstanceMap();
                    if (instance != null)
                    {
                        switch (instance.Reset(method))
                        {
                            case InstanceResetResult.Success:
                                SendResetInstanceSuccess(map.GetId());
                                forgetInstance = true;
                                break;
                            case InstanceResetResult.NotEmpty:
                                if (method == InstanceResetMethod.Manual)
                                    SendResetInstanceFailed(ResetFailedReason.Failed, map.GetId());
                                else if (method == InstanceResetMethod.OnChangeDifficulty)
                                    forgetInstance = true;
                                break;
                            case InstanceResetResult.CannotReset:
                                break;
                            default:
                                break;
                        }
                    }
                }

                if (forgetInstance)
                    m_recentInstances.Remove(mapId);
            }
        }

        public void SendResetInstanceSuccess(uint MapId)
        {
            InstanceReset data = new();
            data.MapID = MapId;
            SendPacket(data);
        }

        public void SendResetInstanceFailed(ResetFailedReason reason, uint MapId)
        {
            InstanceResetFailed data = new();
            data.MapID = MapId;
            data.ResetFailedReason = reason;
            SendPacket(data);
        }

        public void SendTransferAborted(uint mapid, TransferAbortReason reason, byte arg = 0, uint mapDifficultyXConditionID = 0)
        {
            TransferAborted transferAborted = new();
            transferAborted.MapID = mapid;
            transferAborted.Arg = arg;
            transferAborted.TransfertAbort = reason;
            transferAborted.MapDifficultyXConditionID = mapDifficultyXConditionID;
            SendPacket(transferAborted);
        }

        public bool IsLockedToDungeonEncounter(uint dungeonEncounterId)
        {
            DungeonEncounterRecord dungeonEncounter = CliDB.DungeonEncounterStorage.LookupByKey(dungeonEncounterId);
            if (dungeonEncounter == null)
                return false;

            InstanceLock instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(GetGUID(), new MapDb2Entries(GetMap().GetEntry(), GetMap().GetMapDifficulty()));
            if (instanceLock == null)
                return false;

            return (instanceLock.GetData().CompletedEncountersMask & (1u << dungeonEncounter.Bit)) != 0;
        }

        public bool IsLockedToDungeonEncounter(uint dungeonEncounterId, Difficulty difficulty)
        {
            var dungeonEncounter = CliDB.DungeonEncounterStorage.LookupByKey(dungeonEncounterId);
            if (dungeonEncounter == null)
                return false;

            InstanceLock instanceLock = Global.InstanceLockMgr.FindActiveInstanceLock(GetGUID(), new MapDb2Entries((uint)dungeonEncounter.MapID, difficulty));
            if (instanceLock == null)
                return false;

            return (instanceLock.GetData().CompletedEncountersMask & (1u << dungeonEncounter.Bit)) != 0;
        }

        public override void ProcessTerrainStatusUpdate(ZLiquidStatus oldLiquidStatus, LiquidData newLiquidData)
        {
            // process liquid auras using generic unit code
            base.ProcessTerrainStatusUpdate(oldLiquidStatus, newLiquidData);

            m_MirrorTimerFlags &= ~(PlayerUnderwaterState.InWater | PlayerUnderwaterState.InLava | PlayerUnderwaterState.InSlime | PlayerUnderwaterState.InDarkWater);

            // player specific logic for mirror timers
            if (GetLiquidStatus() != 0 && newLiquidData != null)
            {
                // Breath bar state (under water in any liquid type)
                if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.AllLiquids))
                    if (GetLiquidStatus().HasAnyFlag(ZLiquidStatus.UnderWater))
                        m_MirrorTimerFlags |= PlayerUnderwaterState.InWater;

                // Fatigue bar state (if not on flight path or transport)
                if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.DarkWater) && !IsInFlight() && GetTransport() == null)
                    m_MirrorTimerFlags |= PlayerUnderwaterState.InDarkWater;

                // Lava state (any contact)
                if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.Magma))
                    if (GetLiquidStatus().HasAnyFlag(ZLiquidStatus.InContact))
                        m_MirrorTimerFlags |= PlayerUnderwaterState.InLava;

                // Slime state (any contact)
                if (newLiquidData.type_flags.HasAnyFlag(LiquidHeaderTypeFlags.Slime))
                    if (GetLiquidStatus().HasAnyFlag(ZLiquidStatus.InContact))
                        m_MirrorTimerFlags |= PlayerUnderwaterState.InSlime;
            }

            if (HasAuraType(AuraType.ForceBeathBar))
                m_MirrorTimerFlags |= PlayerUnderwaterState.InWater;
        }

        public uint GetRecentInstanceId(uint mapId)
        {
            return m_recentInstances.LookupByKey(mapId);
        }

        public void SetRecentInstance(uint mapId, uint instanceId)
        {
            m_recentInstances[mapId] = instanceId;
        }
    }
}
