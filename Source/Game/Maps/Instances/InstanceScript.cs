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
using Framework.IO;
using Game.Entities;
using Game.Groups;
using Game.Network.Packets;
using Game.Scenarios;
using System.Collections.Generic;
using System.Text;

namespace Game.Maps
{
    public class InstanceScript : ZoneScript
    {
        public InstanceScript(InstanceMap map)
        {
            instance = map;
        }

        public void SaveToDB()
        {
            InstanceScenario scenario = instance.GetInstanceScenario();
            if (scenario != null)
                scenario.SaveToDB();

            string data = GetSaveData();
            if (string.IsNullOrEmpty(data))
                return;

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_INSTANCE_DATA);
            stmt.AddValue(0, GetCompletedEncounterMask());
            stmt.AddValue(1, data);
            stmt.AddValue(2, _entranceId);
            stmt.AddValue(3, instance.GetInstanceId());
            DB.Characters.Execute(stmt);
        }

        public virtual bool IsEncounterInProgress()
        {
            foreach (var boss in bosses.Values)
            {
                if (boss.state == EncounterState.InProgress)
                    return true;
            }

            return false;
        }

        public override void OnCreatureCreate(Creature creature)
        {
            AddObject(creature, true);
            AddMinion(creature, true);
        }

        public override void OnCreatureRemove(Creature creature)
        {
            AddObject(creature, false);
            AddMinion(creature, false);
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            AddObject(go, true);
            AddDoor(go, true);
        }

        public override void OnGameObjectRemove(GameObject go)
        {
            AddObject(go, false);
            AddDoor(go, false);
        }

        public ObjectGuid GetObjectGuid(uint type)
        {
            return _objectGuids.LookupByKey(type);
        }

        public override ObjectGuid GetGuidData(uint type)
        {
            return GetObjectGuid(type);
        }

        public void SetHeaders(string dataHeaders)
        {
            foreach (char header in dataHeaders)
                if (char.IsLetter(header))
                    headers.Add(header);
        }

        public void LoadBossBoundaries(BossBoundaryEntry[] data)
        {
            foreach (BossBoundaryEntry entry in data)
            {
                if (entry.BossId < bosses.Count)
                    bosses[entry.BossId].boundary.Add(entry.Boundary);
            }
        }

        public void LoadMinionData(params MinionData[] data)
        {
            foreach (var minion in data)
            {
                if (minion.entry == 0)
                    continue;

                if (minion.bossId < bosses.Count)
                    minions.Add(minion.entry, new MinionInfo(bosses[minion.bossId]));
            }

            Log.outDebug(LogFilter.Scripts, "InstanceScript.LoadMinionData: {0} minions loaded.", minions.Count);
        }

        public void LoadDoorData(params DoorData[] data)
        {
            foreach (var door in data)
            {
                if (door.entry == 0)
                    continue;

                if (door.bossId < bosses.Count)
                    doors.Add(door.entry, new DoorInfo(bosses[door.bossId], door.type));
            }

            Log.outDebug(LogFilter.Scripts, "InstanceScript.LoadDoorData: {0} doors loaded.", doors.Count);
        }

        public void LoadObjectData(ObjectData[] creatureData, ObjectData[] gameObjectData)
        {
            if (creatureData != null)
                LoadObjectData(creatureData, _creatureInfo);

            if (gameObjectData != null)
                LoadObjectData(gameObjectData, _gameObjectInfo);

            Log.outDebug(LogFilter.Scripts, "InstanceScript.LoadObjectData: {0} objects loaded.", _creatureInfo.Count + _gameObjectInfo.Count);
        }

        void LoadObjectData(ObjectData[] objectData, Dictionary<uint, uint> objectInfo)
        {
            foreach (var data in objectData)
            {
                Cypher.Assert(!objectInfo.ContainsKey(data.entry));
                objectInfo[data.entry] = data.type;
            }
        }

        void UpdateMinionState(Creature minion, EncounterState state)
        {
            switch (state)
            {
                case EncounterState.NotStarted:
                    if (!minion.IsAlive())
                        minion.Respawn();
                    else if (minion.IsInCombat())
                        minion.GetAI().EnterEvadeMode();
                    break;
                case EncounterState.InProgress:
                    if (!minion.IsAlive())
                        minion.Respawn();
                    else if (minion.GetVictim() == null)
                        minion.GetAI().DoZoneInCombat();
                    break;
                default:
                    break;
            }
        }

        public virtual void UpdateDoorState(GameObject door)
        {
            var range = doors.LookupByKey(door.GetEntry());
            if (range.Empty())
                return;

            bool open = true;
            foreach (var info in range)
            {
                if (!open)
                    break;

                switch (info.type)
                {
                    case DoorType.Room:
                        open = (info.bossInfo.state != EncounterState.InProgress);
                        break;
                    case DoorType.Passage:
                        open = (info.bossInfo.state == EncounterState.Done);
                        break;
                    case DoorType.SpawnHole:
                        open = (info.bossInfo.state == EncounterState.InProgress);
                        break;
                    default:
                        break;
                }
            }

            door.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
        }

        public BossInfo GetBossInfo(uint id)
        {
            Cypher.Assert(id < bosses.Count);
            return bosses[id];
        }

        void AddObject(Creature obj, bool add)
        {
            if (_creatureInfo.ContainsKey(obj.GetEntry()))
                AddObject(obj, _creatureInfo[obj.GetEntry()], add);
        }

        void AddObject(GameObject obj, bool add)
        {
            if (_gameObjectInfo.ContainsKey(obj.GetEntry()))
                AddObject(obj, _gameObjectInfo[obj.GetEntry()], add);
        }

        void AddObject(WorldObject obj, uint type, bool add)
        {
            if (add)
                _objectGuids[type] = obj.GetGUID();
            else
            {
                var guid = _objectGuids.LookupByKey(type);
                if (!guid.IsEmpty() && guid == obj.GetGUID())
                    _objectGuids.Remove(type);
            }
        }

        public virtual void AddDoor(GameObject door, bool add)
        {
            var range = doors.LookupByKey(door.GetEntry());
            if (range.Empty())
                return;

            foreach (var data in range)
            {
                if (add)
                    data.bossInfo.door[(int)data.type].Add(door.GetGUID());
                else
                    data.bossInfo.door[(int)data.type].Remove(door.GetGUID());
            }

            if (add)
                UpdateDoorState(door);
        }

        public void AddMinion(Creature minion, bool add)
        {
            var minionInfo = minions.LookupByKey(minion.GetEntry());
            if (minionInfo == null)
                return;

            if (add)
                minionInfo.bossInfo.minion.Add(minion.GetGUID());
            else
                minionInfo.bossInfo.minion.Remove(minion.GetGUID());
        }

        public Creature GetCreature(uint type)
        {
            return instance.GetCreature(GetObjectGuid(type));
        }

        public GameObject GetGameObject(uint type)
        {
            return instance.GetGameObject(GetObjectGuid(type));
        }

        public virtual bool SetBossState(uint id, EncounterState state)
        {
            if (id < bosses.Count)
            {
                BossInfo bossInfo = bosses[id];
                if (bossInfo.state == EncounterState.ToBeDecided) // loading
                {
                    bossInfo.state = state;
                    //Log.outError(LogFilter.General "Inialize boss {0} state as {1}.", id, (uint32)state);
                    return false;
                }
                else
                {
                    if (bossInfo.state == state)
                        return false;

                    if (state == EncounterState.Done)
                    {
                        foreach (var guid in bossInfo.minion)
                        {
                            Creature minion = instance.GetCreature(guid);
                            if (minion)
                                if (minion.isWorldBoss() && minion.IsAlive())
                                    return false;
                        }
                    }

                    switch (state)
                    {
                        case EncounterState.InProgress:
                            {
                                uint resInterval = GetCombatResurrectionChargeInterval();
                                InitializeCombatResurrections(1, resInterval);
                                SendEncounterStart(1, 9, resInterval, resInterval);

                                var playerList = instance.GetPlayers();
                                foreach (var player in playerList)
                                        if (player.IsAlive())
                                            player.ProcSkillsAndAuras(null, ProcFlags.EncounterStart, ProcFlags.None, ProcFlagsSpellType.MaskAll, ProcFlagsSpellPhase.None, ProcFlagsHit.None, null, null, null);
                                break;
                            }
                        case EncounterState.Fail:
                        case EncounterState.Done:
                            ResetCombatResurrections();
                            SendEncounterEnd();
                            break;
                        default:
                            break;
                    }

                    bossInfo.state = state;
                    SaveToDB();
                }

                for (uint type = 0; type < (int)DoorType.Max; ++type)
                {
                    foreach (var guid in bossInfo.door[type])
                    {
                        GameObject door = instance.GetGameObject(guid);
                        if (door)
                            UpdateDoorState(door);
                    }
                }

                foreach (var guid in bossInfo.minion.ToArray())
                {
                    Creature minion = instance.GetCreature(guid);
                    if (minion)
                        UpdateMinionState(minion, state);
                }

                return true;
            }
            return false;
        }

        public bool _SkipCheckRequiredBosses(Player player = null)
        {
            return player && player.GetSession().HasPermission(RBACPermissions.SkipCheckInstanceRequiredBosses);
        }

        public virtual void Load(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                OUT_LOAD_INST_DATA_FAIL();
                return;
            }

            OUT_LOAD_INST_DATA(data);

            var loadStream = new StringArguments(data);

            if (ReadSaveDataHeaders(loadStream))
            {
                ReadSaveDataBossStates(loadStream);
                ReadSaveDataMore(loadStream);
            }
            else
                OUT_LOAD_INST_DATA_FAIL();

            OUT_LOAD_INST_DATA_COMPLETE();
        }

        bool ReadSaveDataHeaders(StringArguments data)
        {
            foreach (char header in headers)
            {
                char buff = data.NextChar();

                if (header != buff)
                    return false;
            }

            return true;
        }

        void ReadSaveDataBossStates(StringArguments data)
        {
            foreach (var pair in bosses)
            {
                EncounterState buff = (EncounterState)data.NextUInt32();
                if (buff == EncounterState.InProgress || buff == EncounterState.Fail || buff == EncounterState.Special)
                    buff = EncounterState.NotStarted;

                if (buff < EncounterState.ToBeDecided)
                    SetBossState(pair.Key, buff);
            }
        }

        public virtual string GetSaveData()
        {
            OUT_SAVE_INST_DATA();

            StringBuilder saveStream = new StringBuilder();

            WriteSaveDataHeaders(saveStream);
            WriteSaveDataBossStates(saveStream);
            WriteSaveDataMore(saveStream);

            OUT_SAVE_INST_DATA_COMPLETE();

            return saveStream.ToString();
        }

        void WriteSaveDataHeaders(StringBuilder data)
        {
            foreach (char header in headers)
                data.AppendFormat("{0} ", header);
        }

        void WriteSaveDataBossStates(StringBuilder data)
        {
            foreach (BossInfo bossInfo in bosses.Values)
                data.AppendFormat("{0} ", (uint)bossInfo.state);
        }

        public void HandleGameObject(ObjectGuid guid, bool open, GameObject go = null)
        {
            if (!go)
                go = instance.GetGameObject(guid);
            if (go)
                go.SetGoState(open ? GameObjectState.Active : GameObjectState.Ready);
            else
                Log.outDebug(LogFilter.Scripts, "InstanceScript: HandleGameObject failed");
        }

        public void DoUseDoorOrButton(ObjectGuid uiGuid, uint withRestoreTime = 0, bool useAlternativeState = false)
        {
            if (uiGuid.IsEmpty())
                return;

            GameObject go = instance.GetGameObject(uiGuid);
            if (go)
            {
                if (go.GetGoType() == GameObjectTypes.Door || go.GetGoType() == GameObjectTypes.Button)
                {
                    if (go.getLootState() == LootState.Ready)
                        go.UseDoorOrButton(withRestoreTime, useAlternativeState);
                    else if (go.getLootState() == LootState.Activated)
                        go.ResetDoorOrButton();
                }
                else
                    Log.outError(LogFilter.Scripts, "InstanceScript: DoUseDoorOrButton can't use gameobject entry {0}, because type is {1}.", go.GetEntry(), go.GetGoType());
            }
            else
                Log.outDebug(LogFilter.Scripts, "InstanceScript: DoUseDoorOrButton failed");
        }

        void DoCloseDoorOrButton(ObjectGuid guid)
        {
            if (guid.IsEmpty())
                return;

            GameObject go = instance.GetGameObject(guid);
            if (go)
            {
                if (go.GetGoType() == GameObjectTypes.Door || go.GetGoType() == GameObjectTypes.Button)
                {
                    if (go.getLootState() == LootState.Activated)
                        go.ResetDoorOrButton();
                }
                else
                    Log.outError(LogFilter.Scripts, "InstanceScript: DoCloseDoorOrButton can't use gameobject entry {0}, because type is {1}.", go.GetEntry(), go.GetGoType());
            }
            else
                Log.outDebug(LogFilter.Scripts, "InstanceScript: DoCloseDoorOrButton failed");
        }

        public void DoRespawnGameObject(ObjectGuid guid, uint timeToDespawn)
        {
            GameObject go = instance.GetGameObject(guid);
            if (go)
            {
                switch (go.GetGoType())
                {
                    case GameObjectTypes.Door:
                    case GameObjectTypes.Button:
                    case GameObjectTypes.Trap:
                    case GameObjectTypes.FishingNode:
                        // not expect any of these should ever be handled
                        Log.outError(LogFilter.Scripts, "InstanceScript: DoRespawnGameObject can't respawn gameobject entry {0}, because type is {1}.", go.GetEntry(), go.GetGoType());
                        return;
                    default:
                        break;
                }

                if (go.isSpawned())
                    return;

                go.SetRespawnTime((int)timeToDespawn);
            }
            else
                Log.outDebug(LogFilter.Scripts, "InstanceScript: DoRespawnGameObject failed");
        }

        public void DoUpdateWorldState(uint uiStateId, uint uiStateData)
        {
            var lPlayers = instance.GetPlayers();

            if (!lPlayers.Empty())
            {
                foreach (var player in lPlayers)
                    player.SendUpdateWorldState(uiStateId, uiStateData);
            }
            else
                Log.outDebug(LogFilter.Scripts, "DoUpdateWorldState attempt send data but no players in map.");
        }

        // Send Notify to all players in instance
        void DoSendNotifyToInstance(string format, params object[] args)
        {
            var players = instance.GetPlayers();

            if (!players.Empty())
            {
                foreach (var player in players)
                {
                    WorldSession session = player.GetSession();
                    if (session != null)
                        session.SendNotification(format, args);
                }
            }
        }

        // Update Achievement Criteria for all players in instance
        public void DoUpdateCriteria(CriteriaTypes type, uint miscValue1 = 0, uint miscValue2 = 0, Unit unit = null)
        {
            var PlayerList = instance.GetPlayers();

            if (!PlayerList.Empty())
                foreach (var player in PlayerList)
                    player.UpdateCriteria(type, miscValue1, miscValue2, 0, unit);
        }

        // Start timed achievement for all players in instance
        public void DoStartCriteriaTimer(CriteriaTimedTypes type, uint entry)
        {
            var PlayerList = instance.GetPlayers();

            if (!PlayerList.Empty())
                foreach (var player in PlayerList)
                    player.StartCriteriaTimer(type, entry);
        }

        // Stop timed achievement for all players in instance
        public void DoStopCriteriaTimer(CriteriaTimedTypes type, uint entry)
        {
            var PlayerList = instance.GetPlayers();

            if (!PlayerList.Empty())
                foreach (var player in PlayerList)
                    player.RemoveCriteriaTimer(type, entry);
        }

        // Remove Auras due to Spell on all players in instance
        public void DoRemoveAurasDueToSpellOnPlayers(uint spell)
        {
            var PlayerList = instance.GetPlayers();
            if (!PlayerList.Empty())
            {
                foreach (var player in PlayerList)
                {
                    player.RemoveAurasDueToSpell(spell);
                    Pet pet = player.GetPet();
                    if (pet != null)
                        pet.RemoveAurasDueToSpell(spell);
                }
            }
        }

        // Cast spell on all players in instance
        public void DoCastSpellOnPlayers(uint spell)
        {
            var PlayerList = instance.GetPlayers();

            if (!PlayerList.Empty())
                foreach (var player in PlayerList)
                    player.CastSpell(player, spell, true);
        }

        public virtual bool CheckAchievementCriteriaMeet(uint criteria_id, Player source, Unit target = null, uint miscvalue1 = 0)
        {
            Log.outError(LogFilter.Server, "Achievement system call CheckAchievementCriteriaMeet but instance script for map {0} not have implementation for achievement criteria {1}",
                instance.GetId(), criteria_id);
            return false;
        }

        public void SetEntranceLocation(uint worldSafeLocationId)
        {
            _entranceId = worldSafeLocationId;
            if (_temporaryEntranceId != 0)
                _temporaryEntranceId = 0;
        }

        public void SendEncounterUnit(EncounterFrameType type, Unit unit = null, byte priority = 0)
        {
            switch (type)
            {
                case EncounterFrameType.Engage:
                    if (unit == null)
                        return;

                    InstanceEncounterEngageUnit encounterEngageMessage = new InstanceEncounterEngageUnit();
                    encounterEngageMessage.Unit = unit.GetGUID();
                    encounterEngageMessage.TargetFramePriority = priority;
                    instance.SendToPlayers(encounterEngageMessage);
                    break;
                case EncounterFrameType.Disengage:
                    if (!unit)
                        return;

                    InstanceEncounterDisengageUnit encounterDisengageMessage = new InstanceEncounterDisengageUnit();
                    encounterDisengageMessage.Unit = unit.GetGUID();
                    instance.SendToPlayers(encounterDisengageMessage);
                    break;
                case EncounterFrameType.UpdatePriority:
                    if (!unit)
                        return;

                    InstanceEncounterChangePriority encounterChangePriorityMessage = new InstanceEncounterChangePriority();
                    encounterChangePriorityMessage.Unit = unit.GetGUID();
                    encounterChangePriorityMessage.TargetFramePriority = priority;
                    instance.SendToPlayers(encounterChangePriorityMessage);
                    break;
                default:
                    break;
            }
        }

        void SendEncounterStart(uint inCombatResCount = 0, uint maxInCombatResCount = 0, uint inCombatResChargeRecovery = 0, uint nextCombatResChargeTime = 0)
        {
            InstanceEncounterStart encounterStartMessage = new InstanceEncounterStart();
            encounterStartMessage.InCombatResCount = inCombatResCount;
            encounterStartMessage.MaxInCombatResCount = maxInCombatResCount;
            encounterStartMessage.CombatResChargeRecovery = inCombatResChargeRecovery;
            encounterStartMessage.NextCombatResChargeTime = nextCombatResChargeTime;

            instance.SendToPlayers(encounterStartMessage);
        }

        void SendEncounterEnd()
        {
            instance.SendToPlayers(new InstanceEncounterEnd());
        }

        void SendBossKillCredit(uint encounterId)
        {
            BossKillCredit bossKillCreditMessage = new BossKillCredit();
            bossKillCreditMessage.DungeonEncounterID = encounterId;

            instance.SendToPlayers(bossKillCreditMessage);
        }

        public void UpdateEncounterStateForKilledCreature(uint creatureId, Unit source)
        {
            UpdateEncounterState(EncounterCreditType.KillCreature, creatureId, source);
        }

        public void UpdateEncounterStateForSpellCast(uint spellId, Unit source)
        {
            UpdateEncounterState(EncounterCreditType.CastSpell, spellId, source);
        }

        void UpdateEncounterState(EncounterCreditType type, uint creditEntry, Unit source)
        {
            var encounters = Global.ObjectMgr.GetDungeonEncounterList(instance.GetId(), instance.GetDifficultyID());
            if (encounters.Empty())
                return;

            uint dungeonId = 0;

            foreach (var encounter in encounters)
            {
                if (encounter.creditType == type && encounter.creditEntry == creditEntry)
                {
                    completedEncounters |= (1u << encounter.dbcEntry.Bit);
                    if (encounter.lastEncounterDungeon != 0)
                    {
                        dungeonId = encounter.lastEncounterDungeon;
                        Log.outDebug(LogFilter.Lfg, "UpdateEncounterState: Instance {0} (instanceId {1}) completed encounter {2}. Credit Dungeon: {3}", 
                            instance.GetMapName(), instance.GetInstanceId(), encounter.dbcEntry.Name[Global.WorldMgr.GetDefaultDbcLocale()], dungeonId);
                        break;
                    }
                }
            }

            if (dungeonId != 0)
            {
                var players = instance.GetPlayers();
                foreach (var player in players)
                {
                    Group grp = player.GetGroup();
                    if (grp != null)
                        if (grp.isLFGGroup())
                        {
                            Global.LFGMgr.FinishDungeon(grp.GetGUID(), dungeonId);
                            return;
                        }
                }
            }
        }

        void UpdatePhasing()
        {
            var players = instance.GetPlayers();
            foreach (var player in players)
                PhasingHandler.SendToPlayer(player);
        }

        public void UpdateCombatResurrection(uint diff)
        {
            if (!_combatResurrectionTimerStarted)
                return;

            if (_combatResurrectionTimer <= diff)
                AddCombatResurrectionCharge();
            else
                _combatResurrectionTimer -= diff;
        }

        void InitializeCombatResurrections(byte charges = 1, uint interval = 0)
        {
            _combatResurrectionCharges = charges;
            if (interval == 0)
                return;

            _combatResurrectionTimer = interval;
            _combatResurrectionTimerStarted = true;
        }

        public void AddCombatResurrectionCharge()
        {
            ++_combatResurrectionCharges;
            _combatResurrectionTimer = GetCombatResurrectionChargeInterval();
            _combatResurrectionTimerStarted = true;

            var gainCombatResurrectionCharge = new InstanceEncounterGainCombatResurrectionCharge();
            gainCombatResurrectionCharge.InCombatResCount = _combatResurrectionCharges;
            gainCombatResurrectionCharge.CombatResChargeRecovery = _combatResurrectionTimer;
            instance.SendToPlayers(gainCombatResurrectionCharge);
        }

        public void UseCombatResurrection()
        {
            --_combatResurrectionCharges;

            instance.SendToPlayers(new InstanceEncounterInCombatResurrection());
        }

        public void ResetCombatResurrections()
        {
            _combatResurrectionCharges = 0;
            _combatResurrectionTimer = 0;
            _combatResurrectionTimerStarted = false;
        }

        public uint GetCombatResurrectionChargeInterval()
        {
            uint interval = 0;
            int playerCount = instance.GetPlayers().Count;
            if (playerCount != 0)
                interval = (uint)(90 * Time.Minute * Time.InMilliseconds / playerCount);

            return interval;
        }

        bool InstanceHasScript(WorldObject obj, string scriptName)
        {
            InstanceMap instance = obj.GetMap().ToInstanceMap();
            if (instance != null)
                return instance.GetScriptName() == scriptName;

            return false;
        }

        public virtual void Initialize() { }
        public virtual void Update(uint diff) { }
        public virtual void OnPlayerEnter(Player player) { }

        // Return wether server allow two side groups or not
        public bool ServerAllowsTwoSideGroups() { return WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup); }

        public EncounterState GetBossState(uint id) { return id < bosses.Count ? bosses[id].state : EncounterState.ToBeDecided; }
        public List<AreaBoundary> GetBossBoundary(uint id) { return id < bosses.Count ? bosses[id].boundary : null; }

        public virtual bool CheckRequiredBosses(uint bossId, Player player = null) { return true; }

        public void SetCompletedEncountersMask(uint newMask) { completedEncounters = newMask; }

        public uint GetCompletedEncounterMask() { return completedEncounters; }

        // Sets a temporary entrance that does not get saved to db
        void SetTemporaryEntranceLocation(uint worldSafeLocationId) { _temporaryEntranceId = worldSafeLocationId; }

        // Get's the current entrance id
        public uint GetEntranceLocation() { return _temporaryEntranceId != 0 ? _temporaryEntranceId : _entranceId; }

        public virtual void FillInitialWorldStates(InitWorldStates data) { }

        public int GetEncounterCount() { return bosses.Count; }

        public byte GetCombatResurrectionCharges() { return _combatResurrectionCharges; }

        public void SetBossNumber(uint number)
        {
            for (uint i = 0; i < number; ++i)
                bosses.Add(i, new BossInfo());
        }

        public void OUT_SAVE_INST_DATA() { Log.outDebug(LogFilter.Scripts, "Saving Instance Data for Instance {0} (Map {1}, Instance Id {2})", instance.GetMapName(), instance.GetId(), instance.GetInstanceId()); }
        public void OUT_SAVE_INST_DATA_COMPLETE() { Log.outDebug(LogFilter.Scripts, "Saving Instance Data for Instance {0} (Map {1}, Instance Id {2}) completed.", instance.GetMapName(), instance.GetId(), instance.GetInstanceId()); }
        public void OUT_LOAD_INST_DATA(string input) { Log.outDebug(LogFilter.Scripts, "Loading Instance Data for Instance {0} (Map {1}, Instance Id {2}). Input is '{3}'", instance.GetMapName(), instance.GetId(), instance.GetInstanceId(), input); }
        public void OUT_LOAD_INST_DATA_COMPLETE() { Log.outDebug(LogFilter.Scripts, "Instance Data Load for Instance {0} (Map {1}, Instance Id: {2}) is complete.", instance.GetMapName(), instance.GetId(), instance.GetInstanceId()); }
        public void OUT_LOAD_INST_DATA_FAIL() { Log.outDebug(LogFilter.Scripts, "Unable to load Instance Data for Instance {0} (Map {1}, Instance Id: {2}).", instance.GetMapName(), instance.GetId(), instance.GetInstanceId()); }

        public virtual void ReadSaveDataMore(StringArguments data) { }

        public virtual void WriteSaveDataMore(StringBuilder data) { }

        public InstanceMap instance;
        List<char> headers = new List<char>();
        Dictionary<uint, BossInfo> bosses = new Dictionary<uint, BossInfo>();
        MultiMap<uint, DoorInfo> doors = new MultiMap<uint, DoorInfo>();
        Dictionary<uint, MinionInfo> minions = new Dictionary<uint, MinionInfo>();
        Dictionary<uint, uint> _creatureInfo = new Dictionary<uint, uint>();
        Dictionary<uint, uint> _gameObjectInfo = new Dictionary<uint, uint>();
        Dictionary<uint, ObjectGuid> _objectGuids = new Dictionary<uint, ObjectGuid>();
        uint completedEncounters;
        uint _entranceId;
        uint _temporaryEntranceId;
        uint _combatResurrectionTimer;
        byte _combatResurrectionCharges; // the counter for available battle resurrections
        bool _combatResurrectionTimerStarted;
    }


    public class DoorData
    {
        public DoorData(uint _entry, uint _bossid, DoorType _type)
        {
            entry = _entry;
            bossId = _bossid;
            type = _type;
        }

        public uint entry;
        public uint bossId;
        public DoorType type;
    }

    public class BossBoundaryEntry
    {
        public BossBoundaryEntry(uint bossId, AreaBoundary boundary)
        {
            BossId = bossId;
            Boundary = boundary;
        }

        public uint BossId;
        public AreaBoundary Boundary;
    }

    public class MinionData
    {
        public MinionData(uint _entry, uint _bossid)
        {
            entry = _entry;
            bossId = _bossid;
        }

        public uint entry;
        public uint bossId;
    }

    public struct ObjectData
    {
        public ObjectData(uint _entry, uint _type)
        {
            entry = _entry;
            type = _type;
        }

        public uint entry;
        public uint type;
    }

    public class BossInfo
    {
        public BossInfo()
        {
            state = EncounterState.ToBeDecided;
            for (var i = 0; i < (int)DoorType.Max; ++i)
                door[i] = new List<ObjectGuid>();
        }

        public EncounterState state;
        public List<ObjectGuid>[] door = new List<ObjectGuid>[(int)DoorType.Max];
        public List<ObjectGuid> minion = new List<ObjectGuid>();
        public List<AreaBoundary> boundary = new List<AreaBoundary>();
    }
    class DoorInfo
    {
        public DoorInfo(BossInfo _bossInfo, DoorType _type)
        {
            bossInfo = _bossInfo;
            type = _type;
        }

        public BossInfo bossInfo;
        public DoorType type;
    }
    class MinionInfo
    {
        public MinionInfo(BossInfo _bossInfo)
        {
            bossInfo = _bossInfo;
        }

        public BossInfo bossInfo;
    }
}
