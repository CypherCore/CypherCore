// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking.Packets;
using Game.Scenarios;
using Game.Scripting.Interfaces.IMap;

namespace Game.Maps
{
    public class InstanceMap : Map
    {
        private readonly InstanceLock _instanceLock;
        private readonly GroupInstanceReference _owningGroupRef = new();
        private InstanceScript _data;
        private DateTime? _instanceExpireEvent;
        private InstanceScenario _scenario;
        private uint _script_id;

        public InstanceMap(uint id, long expiry, uint InstanceId, Difficulty spawnMode, int instanceTeam, InstanceLock instanceLock) : base(id, expiry, InstanceId, spawnMode)
        {
            _instanceLock = instanceLock;

            //lets initialize visibility distance for dungeons
            InitVisibilityDistance();

            // the timer is started by default, and stopped when the first player joins
            // this make sure it gets unloaded if for some reason no player joins
            _unloadTimer = (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, instanceTeam == TeamId.Alliance ? 1 : 0, false, this);
            Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, instanceTeam == TeamId.Horde ? 1 : 0, false, this);

            if (_instanceLock != null)
            {
                _instanceLock.SetInUse(true);
                _instanceExpireEvent = _instanceLock.GetExpiryTime(); // ignore extension State for reset event (will ask players to accept extended save on expiration)
            }
        }

        public override void InitVisibilityDistance()
        {
            //init visibility distance for instances
            _VisibleDistance = Global.WorldMgr.GetMaxVisibleDistanceInInstances();
            _VisibilityNotifyPeriod = Global.WorldMgr.GetVisibilityNotifyPeriodInInstances();
        }

        public override TransferAbortParams CannotEnter(Player player)
        {
            if (player.GetMap() == this)
            {
                Log.outError(LogFilter.Maps, "InstanceMap:CannotEnter - player {0} ({1}) already in map {2}, {3}, {4}!", player.GetName(), player.GetGUID().ToString(), GetId(), GetInstanceId(), GetDifficultyID());
                Cypher.Assert(false);

                return new TransferAbortParams(TransferAbortReason.Error);
            }

            // allow GM's to enter
            if (player.IsGameMaster())
                return base.CannotEnter(player);

            // cannot enter if the instance is full (player cap), GMs don't Count
            uint maxPlayers = GetMaxPlayers();

            if (GetPlayersCountExceptGMs() >= maxPlayers)
            {
                Log.outInfo(LogFilter.Maps, "MAP: Instance '{0}' of map '{1}' cannot have more than '{2}' players. Player '{3}' rejected", GetInstanceId(), GetMapName(), maxPlayers, player.GetName());

                return new TransferAbortParams(TransferAbortReason.MaxPlayers);
            }

            // cannot enter while an encounter is in progress (unless this is a relog, in which case it is permitted)
            if (!player.IsLoading() &&
                IsRaid() &&
                GetInstanceScript() != null &&
                GetInstanceScript().IsEncounterInProgress())
                return new TransferAbortParams(TransferAbortReason.ZoneInCombat);

            if (_instanceLock != null)
            {
                // cannot enter if player is permanent saved to a different instance Id
                TransferAbortReason lockError = Global.InstanceLockMgr.CanJoinInstanceLock(player.GetGUID(), new MapDb2Entries(GetEntry(), GetMapDifficulty()), _instanceLock);

                if (lockError != TransferAbortReason.None)
                    return new TransferAbortParams(lockError);
            }

            return base.CannotEnter(player);
        }

        public override bool AddPlayerToMap(Player player, bool initPlayer = true)
        {
            // increase current instances (hourly limit)
            player.AddInstanceEnterTime(GetInstanceId(), GameTime.GetGameTime());

            MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());

            if (entries.MapDifficulty.HasResetSchedule() &&
                _instanceLock != null &&
                _instanceLock.GetData().CompletedEncountersMask != 0)
                if (!entries.MapDifficulty.IsUsingEncounterLocks())
                {
                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);

                    if (playerLock == null ||
                        (playerLock.IsExpired() && playerLock.IsExtended()) ||
                        playerLock.GetData().CompletedEncountersMask != _instanceLock.GetData().CompletedEncountersMask)
                    {
                        PendingRaidLock pendingRaidLock = new();
                        pendingRaidLock.TimeUntilLock = 60000;
                        pendingRaidLock.CompletedMask = _instanceLock.GetData().CompletedEncountersMask;
                        pendingRaidLock.Extending = playerLock != null && playerLock.IsExtended();
                        pendingRaidLock.WarningOnly = entries.Map.IsFlexLocking(); // events it triggers:  1 : INSTANCE_LOCK_WARNING   0 : INSTANCE_LOCK_STOP / INSTANCE_LOCK_START
                        player.GetSession().SendPacket(pendingRaidLock);

                        if (!entries.Map.IsFlexLocking())
                            player.SetPendingBind(GetInstanceId(), 60000);
                    }
                }

            Log.outInfo(LogFilter.Maps,
                        "MAP: Player '{0}' entered instance '{1}' of map '{2}'",
                        player.GetName(),
                        GetInstanceId(),
                        GetMapName());

            // initialize unload State
            _unloadTimer = 0;

            // this will acquire the same mutex so it cannot be in the previous block
            base.AddPlayerToMap(player, initPlayer);

            _data?.OnPlayerEnter(player);

            _scenario?.OnPlayerEnter(player);

            return true;
        }

        public override void Update(uint diff)
        {
            base.Update(diff);

            if (_data != null)
            {
                _data.Update(diff);
                _data.UpdateCombatResurrection(diff);
            }

            _scenario?.Update(diff);

            if (_instanceExpireEvent.HasValue &&
                _instanceExpireEvent.Value < GameTime.GetSystemTime())
            {
                Reset(InstanceResetMethod.Expire);
                _instanceExpireEvent = Global.InstanceLockMgr.GetNextResetTime(new MapDb2Entries(GetEntry(), GetMapDifficulty()));
            }
        }

        public override void RemovePlayerFromMap(Player player, bool remove)
        {
            Log.outInfo(LogFilter.Maps, "MAP: Removing player '{0}' from instance '{1}' of map '{2}' before relocating to another map", player.GetName(), GetInstanceId(), GetMapName());

            _data?.OnPlayerLeave(player);

            // if last player set unload timer
            if (_unloadTimer == 0 &&
                GetPlayers().Count == 1)
                _unloadTimer = (_instanceLock != null && _instanceLock.IsExpired()) ? 1 : (uint)Math.Max(WorldConfig.GetIntValue(WorldCfg.InstanceUnloadDelay), 1);

            _scenario?.OnPlayerExit(player);

            base.RemovePlayerFromMap(player, remove);
        }

        public void CreateInstanceData()
        {
            if (_data != null)
                return;

            InstanceTemplate mInstance = Global.ObjectMgr.GetInstanceTemplate(GetId());

            if (mInstance != null)
            {
                _script_id = mInstance.ScriptId;
                _data = Global.ScriptMgr.RunScriptRet<IInstanceMapGetInstanceScript, InstanceScript>(p => p.GetInstanceScript(this), GetScriptId(), null);
            }

            if (_data == null)
                return;

            if (_instanceLock == null ||
                _instanceLock.GetInstanceId() == 0)
            {
                _data.Create();

                return;
            }

            MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());

            if (!entries.IsInstanceIdBound() ||
                !IsRaid() ||
                !entries.MapDifficulty.IsRestoringDungeonState() ||
                _owningGroupRef.IsValid())
            {
                _data.Create();

                return;
            }

            InstanceLockData lockData = _instanceLock.GetInstanceInitializationData();
            _data.SetCompletedEncountersMask(lockData.CompletedEncountersMask);
            _data.SetEntranceLocation(lockData.EntranceWorldSafeLocId);

            if (!lockData.Data.IsEmpty())
            {
                Log.outDebug(LogFilter.Maps, $"Loading instance _data for `{Global.ObjectMgr.GetScriptName(_script_id)}` with Id {i_InstanceId}");
                _data.Load(lockData.Data);
            }
            else
            {
                _data.Create();
            }
        }

        public Group GetOwningGroup()
        {
            return _owningGroupRef.GetTarget();
        }

        public void TrySetOwningGroup(Group group)
        {
            if (!_owningGroupRef.IsValid())
                _owningGroupRef.Link(group, this);
        }

        public InstanceResetResult Reset(InstanceResetMethod method)
        {
            // raids can be reset if no boss was killed
            if (method != InstanceResetMethod.Expire &&
                _instanceLock != null &&
                _instanceLock.GetData().CompletedEncountersMask != 0)
                return InstanceResetResult.CannotReset;

            if (HavePlayers())
            {
                switch (method)
                {
                    case InstanceResetMethod.Manual:
                        // notify the players to leave the instance so it can be reset
                        foreach (var player in GetPlayers())
                            player.SendResetFailedNotify(GetId());

                        break;
                    case InstanceResetMethod.OnChangeDifficulty:
                        // no client notification
                        break;
                    case InstanceResetMethod.Expire:
                        {
                            RaidInstanceMessage raidInstanceMessage = new();
                            raidInstanceMessage.Type = InstanceResetWarningType.Expired;
                            raidInstanceMessage.MapID = GetId();
                            raidInstanceMessage.DifficultyID = GetDifficultyID();
                            raidInstanceMessage.Write();

                            PendingRaidLock pendingRaidLock = new();
                            pendingRaidLock.TimeUntilLock = 60000;
                            pendingRaidLock.CompletedMask = _instanceLock.GetData().CompletedEncountersMask;
                            pendingRaidLock.Extending = true;
                            pendingRaidLock.WarningOnly = GetEntry().IsFlexLocking();
                            pendingRaidLock.Write();

                            foreach (Player player in GetPlayers())
                            {
                                player.SendPacket(raidInstanceMessage);
                                player.SendPacket(pendingRaidLock);

                                if (!pendingRaidLock.WarningOnly)
                                    player.SetPendingBind(GetInstanceId(), 60000);
                            }

                            break;
                        }
                    default:
                        break;
                }

                return InstanceResetResult.NotEmpty;
            }
            else
            {
                // unloaded at next update
                _unloadTimer = 1;
            }

            return InstanceResetResult.Success;
        }

        public string GetScriptName()
        {
            return Global.ObjectMgr.GetScriptName(_script_id);
        }

        public void UpdateInstanceLock(UpdateBossStateSaveDataEvent updateSaveDataEvent)
        {
            if (_instanceLock != null)
            {
                uint instanceCompletedEncounters = _instanceLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);

                MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());

                SQLTransaction trans = new();

                if (entries.IsInstanceIdBound())
                    Global.InstanceLockMgr.UpdateSharedInstanceLock(trans,
                                                                    new InstanceLockUpdateEvent(GetInstanceId(),
                                                                                                _data.GetSaveData(),
                                                                                                instanceCompletedEncounters,
                                                                                                updateSaveDataEvent.DungeonEncounter,
                                                                                                _data.GetEntranceLocationForCompletedEncounters(instanceCompletedEncounters)));

                foreach (var player in GetPlayers())
                {
                    // never instance bind GMs with GM mode enabled
                    if (player.IsGameMaster())
                        continue;

                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);
                    string oldData = "";
                    uint playerCompletedEncounters = 0;

                    if (playerLock != null)
                    {
                        oldData = playerLock.GetData().Data;
                        playerCompletedEncounters = playerLock.GetData().CompletedEncountersMask | (1u << updateSaveDataEvent.DungeonEncounter.Bit);
                    }

                    bool isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

                    InstanceLock newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans,
                                                                                              player.GetGUID(),
                                                                                              entries,
                                                                                              new InstanceLockUpdateEvent(GetInstanceId(),
                                                                                                                          _data.UpdateBossStateSaveData(oldData, updateSaveDataEvent),
                                                                                                                          instanceCompletedEncounters,
                                                                                                                          updateSaveDataEvent.DungeonEncounter,
                                                                                                                          _data.GetEntranceLocationForCompletedEncounters(playerCompletedEncounters)));

                    if (isNewLock)
                    {
                        InstanceSaveCreated data = new();
                        data.Gm = player.IsGameMaster();
                        player.SendPacket(data);

                        player.GetSession().SendCalendarRaidLockoutAdded(newLock);
                    }
                }

                DB.Characters.CommitTransaction(trans);
            }
        }

        public void UpdateInstanceLock(UpdateAdditionalSaveDataEvent updateSaveDataEvent)
        {
            if (_instanceLock != null)
            {
                uint instanceCompletedEncounters = _instanceLock.GetData().CompletedEncountersMask;

                MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());

                SQLTransaction trans = new();

                if (entries.IsInstanceIdBound())
                    Global.InstanceLockMgr.UpdateSharedInstanceLock(trans, new InstanceLockUpdateEvent(GetInstanceId(), _data.GetSaveData(), instanceCompletedEncounters, null, null));

                foreach (var player in GetPlayers())
                {
                    // never instance bind GMs with GM mode enabled
                    if (player.IsGameMaster())
                        continue;

                    InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);
                    string oldData = "";

                    if (playerLock != null)
                        oldData = playerLock.GetData().Data;

                    bool isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

                    InstanceLock newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans,
                                                                                              player.GetGUID(),
                                                                                              entries,
                                                                                              new InstanceLockUpdateEvent(GetInstanceId(),
                                                                                                                          _data.UpdateAdditionalSaveData(oldData, updateSaveDataEvent),
                                                                                                                          instanceCompletedEncounters,
                                                                                                                          null,
                                                                                                                          null));

                    if (isNewLock)
                    {
                        InstanceSaveCreated data = new();
                        data.Gm = player.IsGameMaster();
                        player.SendPacket(data);

                        player.GetSession().SendCalendarRaidLockoutAdded(newLock);
                    }
                }

                DB.Characters.CommitTransaction(trans);
            }
        }

        public void CreateInstanceLockForPlayer(Player player)
        {
            MapDb2Entries entries = new(GetEntry(), GetMapDifficulty());
            InstanceLock playerLock = Global.InstanceLockMgr.FindActiveInstanceLock(player.GetGUID(), entries);

            bool isNewLock = playerLock == null || playerLock.GetData().CompletedEncountersMask == 0 || playerLock.IsExpired();

            SQLTransaction trans = new();

            InstanceLock newLock = Global.InstanceLockMgr.UpdateInstanceLockForPlayer(trans, player.GetGUID(), entries, new InstanceLockUpdateEvent(GetInstanceId(), _data.GetSaveData(), _instanceLock.GetData().CompletedEncountersMask, null, null));

            DB.Characters.CommitTransaction(trans);

            if (isNewLock)
            {
                InstanceSaveCreated data = new();
                data.Gm = player.IsGameMaster();
                player.SendPacket(data);

                player.GetSession().SendCalendarRaidLockoutAdded(newLock);
            }
        }

        public uint GetMaxPlayers()
        {
            MapDifficultyRecord mapDiff = GetMapDifficulty();

            if (mapDiff != null &&
                mapDiff.MaxPlayers != 0)
                return mapDiff.MaxPlayers;

            return GetEntry().MaxPlayers;
        }

        public int GetTeamIdInInstance()
        {
            if (Global.WorldStateMgr.GetValue(WorldStates.TeamInInstanceAlliance, this) != 0)
                return TeamId.Alliance;

            if (Global.WorldStateMgr.GetValue(WorldStates.TeamInInstanceHorde, this) != 0)
                return TeamId.Horde;

            return TeamId.Neutral;
        }

        public Team GetTeamInInstance()
        {
            return GetTeamIdInInstance() == TeamId.Alliance ? Team.Alliance : Team.Horde;
        }

        public uint GetScriptId()
        {
            return _script_id;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nScriptId: {GetScriptId()} ScriptName: {GetScriptName()}";
        }

        public InstanceScript GetInstanceScript()
        {
            return _data;
        }

        public InstanceScenario GetInstanceScenario()
        {
            return _scenario;
        }

        public void SetInstanceScenario(InstanceScenario scenario)
        {
            _scenario = scenario;
        }

        public InstanceLock GetInstanceLock()
        {
            return _instanceLock;
        }

        ~InstanceMap()
        {
            _instanceLock?.SetInUse(false);
        }
    }
}