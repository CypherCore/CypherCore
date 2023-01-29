// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.BattleFields
{
    public class BattleField : ZoneScript
    {
        protected uint BattleId { get; set; }  // BattleID (for packet)
        protected uint BattleTime { get; set; } // Length of a battle

        // Map of the objectives belonging to this OutdoorPvP
        private readonly Dictionary<uint, BfCapturePoint> _capturePoints = new();
        protected Dictionary<int, uint> Data32 { get; set; } = new();

        private readonly Dictionary<int, ulong> _data64 = new();
        protected uint DefenderTeam { get; set; }

        // Graveyard variables
        protected List<BfGraveyard> GraveyardList { get; set; } = new(); // Vector witch contain the different GY of the battle

        private readonly List<ObjectGuid>[] _groups = new List<ObjectGuid>[2]; // Contain different raid group
        protected Dictionary<ObjectGuid, long>[] InvitedPlayers { get; set; } = new Dictionary<ObjectGuid, long>[2];
        protected bool IsActive { get; set; }
        protected bool Enabled { get; set; }
        private uint _lastResurectTimer; // Timer for resurect player every 30 sec
        protected Map Map { get; set; }
        protected uint MapId { get; set; }          // _mapId where is Battlefield
        protected uint MaxPlayer { get; set; }       // Maximum number of player that participated to Battlefield
        protected uint MinLevel { get; set; }        // Required level to participate at Battlefield
        protected uint MinPlayer { get; set; }       // Minimum number of player for Battlefield start
        protected uint NoWarBattleTime { get; set; } // Time between two battles

        // Players info maps
        protected List<ObjectGuid>[] Players { get; set; } = new List<ObjectGuid>[2];        // Players in zone
        protected List<ObjectGuid>[] PlayersInQueue { get; set; } = new List<ObjectGuid>[2]; // Players in the queue
        protected List<ObjectGuid>[] PlayersInWar { get; set; } = new List<ObjectGuid>[2];   // Players in WG combat
        protected Dictionary<ObjectGuid, long>[] PlayersWillBeKick { get; set; } = new Dictionary<ObjectGuid, long>[2];
        protected uint RestartAfterCrash { get; set; } // Delay to restart Wintergrasp if the server crashed during a running battle.
        protected bool StartGrouping { get; set; }     // bool for know if all players in area has been invited

        protected uint StartGroupingTimer { get; set; } // Timer for invite players in area 15 minute before start battle
        protected uint TimeForAcceptInvite { get; set; }
        protected uint Timer { get; set; } // Global timer for event

        // Variables that must exist for each battlefield
        protected uint TypeId { get; set; } // See enum BattlefieldTypes

        private uint _uiKickAfkPlayersTimer; // Timer for check Afk in war
        protected uint UiKickDontAcceptTimer { get; set; }
        protected uint ZoneId { get; set; }               // ZoneID of Wintergrasp = 4197
        protected WorldLocation KickPosition { get; set; } // Position where players are teleported if they switch to afk during the battle or if they don't accept invitation

        public ObjectGuid StalkerGuid;

        public BattleField(Map map)
        {
            Enabled = true;
            DefenderTeam = TeamId.Neutral;

            TimeForAcceptInvite = 20;
            UiKickDontAcceptTimer = 1000;
            _uiKickAfkPlayersTimer = 1000;

            _lastResurectTimer = 30 * Time.InMilliseconds;

            Map = map;
            MapId = map.GetId();

            for (byte i = 0; i < 2; ++i)
            {
                Players[i] = new List<ObjectGuid>();
                PlayersInQueue[i] = new List<ObjectGuid>();
                PlayersInWar[i] = new List<ObjectGuid>();
                InvitedPlayers[i] = new Dictionary<ObjectGuid, long>();
                PlayersWillBeKick[i] = new Dictionary<ObjectGuid, long>();
                _groups[i] = new List<ObjectGuid>();
            }
        }

        public void HandlePlayerEnterZone(Player player, uint zone)
        {
            // If battle is started,
            // If not full of players > invite player to join the war
            // If full of players > announce to player that BF is full and kick him after a few second if he desn't leave
            if (IsWarTime())
            {
                if (PlayersInWar[player.GetTeamId()].Count + InvitedPlayers[player.GetTeamId()].Count < MaxPlayer) // Vacant spaces
                {
                    InvitePlayerToWar(player);
                }
                else // No more vacant places
                {
                    // todo Send a packet to announce it to player
                    PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
                    InvitePlayerToQueue(player);
                }
            }
            else
            {
                // If Time left is < 15 minutes invite player to join queue
                if (Timer <= StartGroupingTimer)
                    InvitePlayerToQueue(player);
            }

            // Add player in the list of player in zone
            Players[player.GetTeamId()].Add(player.GetGUID());
            OnPlayerEnterZone(player);
        }

        // Called when a player leave the zone
        public void HandlePlayerLeaveZone(Player player, uint zone)
        {
            if (IsWarTime())
                // If the player is participating to the battle
                if (PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()))
                {
                    PlayersInWar[player.GetTeamId()].Remove(player.GetGUID());
                    Group group = player.GetGroup();

                    if (group) // Remove the player from the raid group
                        group.RemoveMember(player.GetGUID());

                    OnPlayerLeaveWar(player);
                }

            foreach (var capturePoint in _capturePoints.Values)
                capturePoint.HandlePlayerLeave(player);

            InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());
            PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
            Players[player.GetTeamId()].Remove(player.GetGUID());
            SendRemoveWorldStates(player);
            RemovePlayerFromResurrectQueue(player.GetGUID());
            OnPlayerLeaveZone(player);
        }

        public virtual bool Update(uint diff)
        {
            if (Timer <= diff)
            {
                // Battlefield ends on Time
                if (IsWarTime())
                    EndBattle(true);
                else // Time to start a new battle!
                    StartBattle();
            }
            else
            {
                Timer -= diff;
            }

            // Invite players a few minutes before the battle's beginning
            if (!IsWarTime() &&
                !StartGrouping &&
                Timer <= StartGroupingTimer)
            {
                StartGrouping = true;
                InvitePlayersInZoneToQueue();
                OnStartGrouping();
            }

            bool objective_changed = false;

            if (IsWarTime())
            {
                if (_uiKickAfkPlayersTimer <= diff)
                {
                    _uiKickAfkPlayersTimer = 1000;
                    KickAfkPlayers();
                }
                else
                {
                    _uiKickAfkPlayersTimer -= diff;
                }

                // Kick players who chose not to accept invitation to the battle
                if (UiKickDontAcceptTimer <= diff)
                {
                    long now = GameTime.GetGameTime();

                    for (int team = 0; team < SharedConst.PvpTeamsCount; team++)
                        foreach (var pair in InvitedPlayers[team])
                            if (pair.Value <= now)
                                KickPlayerFromBattlefield(pair.Key);

                    InvitePlayersInZoneToWar();

                    for (int team = 0; team < SharedConst.PvpTeamsCount; team++)
                        foreach (var pair in PlayersWillBeKick[team])
                            if (pair.Value <= now)
                                KickPlayerFromBattlefield(pair.Key);

                    UiKickDontAcceptTimer = 1000;
                }
                else
                {
                    UiKickDontAcceptTimer -= diff;
                }

                foreach (var pair in _capturePoints)
                    if (pair.Value.Update(diff))
                        objective_changed = true;
            }


            if (_lastResurectTimer <= diff)
            {
                for (byte i = 0; i < GraveyardList.Count; i++)
                    if (GetGraveyardById(i) != null)
                        GraveyardList[i].Resurrect();

                _lastResurectTimer = BattlegroundConst.ResurrectionInterval;
            }
            else
            {
                _lastResurectTimer -= diff;
            }

            return objective_changed;
        }

        private void InvitePlayersInZoneToQueue()
        {
            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                foreach (var guid in Players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        InvitePlayerToQueue(player);
                }
        }

        private void InvitePlayerToQueue(Player player)
        {
            if (PlayersInQueue[player.GetTeamId()].Contains(player.GetGUID()))
                return;

            if (PlayersInQueue[player.GetTeamId()].Count <= MinPlayer ||
                PlayersInQueue[GetOtherTeam(player.GetTeamId())].Count >= MinPlayer)
                PlayerAcceptInviteToQueue(player);
        }

        private void InvitePlayersInQueueToWar()
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in PlayersInQueue[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        if (PlayersInWar[player.GetTeamId()].Count + InvitedPlayers[player.GetTeamId()].Count < MaxPlayer)
                        {
                            InvitePlayerToWar(player);
                        }
                        else
                        {
                            //Full
                        }
                    }
                }

                PlayersInQueue[team].Clear();
            }
        }

        private void InvitePlayersInZoneToWar()
        {
            for (byte team = 0; team < 2; ++team)
                foreach (var guid in Players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                    {
                        if (PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) ||
                            InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
                            continue;

                        if (PlayersInWar[player.GetTeamId()].Count + InvitedPlayers[player.GetTeamId()].Count < MaxPlayer)
                            InvitePlayerToWar(player);
                        else // Battlefield is full of players
                            PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
                    }
                }
        }

        private void InvitePlayerToWar(Player player)
        {
            if (!player)
                return;

            // todo needed ?
            if (player.IsInFlight())
                return;

            if (player.InArena() ||
                player.GetBattleground())
            {
                PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());

                return;
            }

            // If the player does not match minimal level requirements for the battlefield, kick him
            if (player.GetLevel() < MinLevel)
            {
                if (!PlayersWillBeKick[player.GetTeamId()].ContainsKey(player.GetGUID()))
                    PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;

                return;
            }

            // Check if player is not already in war
            if (PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) ||
                InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
                return;

            PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
            InvitedPlayers[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + TimeForAcceptInvite;
            PlayerAcceptInviteToWar(player);
        }

        public void InitStalker(uint entry, Position pos)
        {
            Creature creature = SpawnCreature(entry, pos);

            if (creature)
                StalkerGuid = creature.GetGUID();
            else
                Log.outError(LogFilter.Battlefield, "Battlefield.InitStalker: could not spawn Stalker (Creature entry {0}), zone messeges will be un-available", entry);
        }

        private void KickAfkPlayers()
        {
            for (byte team = 0; team < 2; ++team)
                foreach (var guid in PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        if (player.IsAFK())
                            KickPlayerFromBattlefield(guid);
                }
        }

        public void KickPlayerFromBattlefield(ObjectGuid guid)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            if (player)
                if (player.GetZoneId() == GetZoneId())
                    player.TeleportTo(KickPosition);
        }

        public void StartBattle()
        {
            if (IsActive)
                return;

            for (int team = 0; team < 2; team++)
            {
                PlayersInWar[team].Clear();
                _groups[team].Clear();
            }

            Timer = BattleTime;
            IsActive = true;

            InvitePlayersInZoneToWar();
            InvitePlayersInQueueToWar();

            OnBattleStart();
        }

        public void EndBattle(bool endByTimer)
        {
            if (!IsActive)
                return;

            IsActive = false;

            StartGrouping = false;

            if (!endByTimer)
                SetDefenderTeam(GetAttackerTeam());

            // Reset battlefield timer
            Timer = NoWarBattleTime;

            OnBattleEnd(endByTimer);
        }

        private void DoPlaySoundToAll(uint soundID)
        {
            BroadcastPacketToWar(new PlaySound(ObjectGuid.Empty, soundID, 0));
        }

        public bool HasPlayer(Player player)
        {
            return Players[player.GetTeamId()].Contains(player.GetGUID());
        }

        // Called in WorldSession:HandleBfQueueInviteResponse
        public void PlayerAcceptInviteToQueue(Player player)
        {
            // Add player in queue
            PlayersInQueue[player.GetTeamId()].Add(player.GetGUID());
        }

        // Called in WorldSession:HandleBfExitRequest
        public void AskToLeaveQueue(Player player)
        {
            // Remove player from queue
            PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());
        }

        // Called in WorldSession::HandleHearthAndResurrect
        public void PlayerAskToLeave(Player player)
        {
            // Player leaving Wintergrasp, teleport to Dalaran.
            // ToDo: confirm teleport destination.
            player.TeleportTo(571, 5804.1499f, 624.7710f, 647.7670f, 1.6400f);
        }

        // Called in WorldSession:HandleBfEntryInviteResponse
        public void PlayerAcceptInviteToWar(Player player)
        {
            if (!IsWarTime())
                return;

            if (AddOrSetPlayerToCorrectBfGroup(player))
            {
                PlayersInWar[player.GetTeamId()].Add(player.GetGUID());
                InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());

                if (player.IsAFK())
                    player.ToggleAFK();

                OnPlayerJoinWar(player); //for scripting
            }
        }

        public void TeamCastSpell(uint teamIndex, int spellId)
        {
            foreach (var guid in PlayersInWar[teamIndex])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);

                if (player)
                {
                    if (spellId > 0)
                        player.CastSpell(player, (uint)spellId, true);
                    else
                        player.RemoveAuraFromStack((uint)-spellId);
                }
            }
        }

        public void BroadcastPacketToZone(ServerPacket data)
        {
            for (byte team = 0; team < 2; ++team)
                foreach (var guid in Players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SendPacket(data);
                }
        }

        public void BroadcastPacketToQueue(ServerPacket data)
        {
            for (byte team = 0; team < 2; ++team)
                foreach (var guid in PlayersInQueue[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SendPacket(data);
                }
        }

        public void BroadcastPacketToWar(ServerPacket data)
        {
            for (byte team = 0; team < 2; ++team)
                foreach (var guid in PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SendPacket(data);
                }
        }

        public void SendWarning(uint id, WorldObject target = null)
        {
            Creature stalker = GetCreature(StalkerGuid);

            if (stalker)
                Global.CreatureTextMgr.SendChat(stalker, (byte)id, target);
        }

        public void AddCapturePoint(BfCapturePoint cp)
        {
            if (_capturePoints.ContainsKey(cp.GetCapturePointEntry()))
                Log.outError(LogFilter.Battlefield, "Battlefield.AddCapturePoint: CapturePoint {0} already exists!", cp.GetCapturePointEntry());

            _capturePoints[cp.GetCapturePointEntry()] = cp;
        }

        private BfCapturePoint GetCapturePoint(uint entry)
        {
            return _capturePoints.LookupByKey(entry);
        }

        public void RegisterZone(uint zoneId)
        {
            Global.BattleFieldMgr.AddZone(zoneId, this);
        }

        public void HideNpc(Creature creature)
        {
            creature.CombatStop();
            creature.SetReactState(ReactStates.Passive);
            creature.SetUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);
            creature.DisappearAndDie();
            creature.SetVisible(false);
        }

        public void ShowNpc(Creature creature, bool aggressive)
        {
            creature.SetVisible(true);
            creature.RemoveUnitFlag(UnitFlags.NonAttackable | UnitFlags.Uninteractible);

            if (!creature.IsAlive())
                creature.Respawn(true);

            if (aggressive)
            {
                creature.SetReactState(ReactStates.Aggressive);
            }
            else
            {
                creature.SetUnitFlag(UnitFlags.NonAttackable);
                creature.SetReactState(ReactStates.Passive);
            }
        }

        // ****************************************************
        // ******************* Group System *******************
        // ****************************************************
        private Group GetFreeBfRaid(int teamIndex)
        {
            foreach (var guid in _groups[teamIndex])
            {
                Group group = Global.GroupMgr.GetGroupByGUID(guid);

                if (group)
                    if (!group.IsFull())
                        return group;
            }

            return null;
        }

        private Group GetGroupPlayer(ObjectGuid plguid, int teamIndex)
        {
            foreach (var guid in _groups[teamIndex])
            {
                Group group = Global.GroupMgr.GetGroupByGUID(guid);

                if (group)
                    if (group.IsMember(plguid))
                        return group;
            }

            return null;
        }

        private bool AddOrSetPlayerToCorrectBfGroup(Player player)
        {
            if (!player.IsInWorld)
                return false;

            Group oldgroup = player.GetGroup();

            if (oldgroup)
                oldgroup.RemoveMember(player.GetGUID());

            Group group = GetFreeBfRaid(player.GetTeamId());

            if (!group)
            {
                group = new Group();
                group.SetBattlefieldGroup(this);
                group.Create(player);
                Global.GroupMgr.AddGroup(group);
                _groups[player.GetTeamId()].Add(group.GetGUID());
            }
            else if (group.IsMember(player.GetGUID()))
            {
                byte subgroup = group.GetMemberGroup(player.GetGUID());
                player.SetBattlegroundOrBattlefieldRaid(group, subgroup);
            }
            else
            {
                group.AddMember(player);
            }

            return true;
        }

        //***************End of Group System*******************

        public BfGraveyard GetGraveyardById(int id)
        {
            if (id < GraveyardList.Count)
            {
                BfGraveyard graveyard = GraveyardList[id];

                if (graveyard != null)
                    return graveyard;
                else
                    Log.outError(LogFilter.Battlefield, "Battlefield:GetGraveyardById Id: {0} not existed", id);
            }
            else
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:GetGraveyardById Id: {0} cant be found", id);
            }

            return null;
        }

        public WorldSafeLocsEntry GetClosestGraveYard(Player player)
        {
            BfGraveyard closestGY = null;
            float maxdist = -1;

            for (byte i = 0; i < GraveyardList.Count; i++)
                if (GraveyardList[i] != null)
                {
                    if (GraveyardList[i].GetControlTeamId() != player.GetTeamId())
                        continue;

                    float dist = GraveyardList[i].GetDistance(player);

                    if (dist < maxdist ||
                        maxdist < 0)
                    {
                        closestGY = GraveyardList[i];
                        maxdist = dist;
                    }
                }

            if (closestGY != null)
                return Global.ObjectMgr.GetWorldSafeLoc(closestGY.GetGraveyardId());

            return null;
        }

        public virtual void AddPlayerToResurrectQueue(ObjectGuid npcGuid, ObjectGuid playerGuid)
        {
            for (byte i = 0; i < GraveyardList.Count; i++)
            {
                if (GraveyardList[i] == null)
                    continue;

                if (GraveyardList[i].HasNpc(npcGuid))
                {
                    GraveyardList[i].AddPlayer(playerGuid);

                    break;
                }
            }
        }

        public void RemovePlayerFromResurrectQueue(ObjectGuid playerGuid)
        {
            for (byte i = 0; i < GraveyardList.Count; i++)
            {
                if (GraveyardList[i] == null)
                    continue;

                if (GraveyardList[i].HasPlayer(playerGuid))
                {
                    GraveyardList[i].RemovePlayer(playerGuid);

                    break;
                }
            }
        }

        public void SendAreaSpiritHealerQuery(Player player, ObjectGuid guid)
        {
            AreaSpiritHealerTime areaSpiritHealerTime = new();
            areaSpiritHealerTime.HealerGuid = guid;
            areaSpiritHealerTime.TimeLeft = _lastResurectTimer; // resurrect every 30 seconds

            player.SendPacket(areaSpiritHealerTime);
        }

        public Creature SpawnCreature(uint entry, Position pos)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: entry {0} does not exist.", entry);

                return null;
            }

            Creature creature = Creature.CreateCreature(entry, Map, pos);

            if (!creature)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: Can't create creature entry: {0}", entry);

                return null;
            }

            creature.SetHomePosition(pos);

            // Set creature in world
            Map.AddToMap(creature);
            creature.SetActive(true);
            creature.SetFarVisible(true);

            return creature;
        }

        // Method for spawning gameobject on map
        public GameObject SpawnGameObject(uint entry, Position pos, Quaternion rotation)
        {
            if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield.SpawnGameObject: GameObject template {0} not found in database! Battlefield not created!", entry);

                return null;
            }

            // Create gameobject
            GameObject go = GameObject.CreateGameObject(entry, Map, pos, rotation, 255, GameObjectState.Ready);

            if (!go)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnGameObject: Cannot create gameobject template {1}! Battlefield not created!", entry);

                return null;
            }

            // Add to world
            Map.AddToMap(go);
            go.SetActive(true);
            go.SetFarVisible(true);

            return go;
        }

        public Creature GetCreature(ObjectGuid guid)
        {
            if (!Map)
                return null;

            return Map.GetCreature(guid);
        }

        public GameObject GetGameObject(ObjectGuid guid)
        {
            if (!Map)
                return null;

            return Map.GetGameObject(guid);
        }

        // Call this to init the Battlefield
        public virtual bool SetupBattlefield()
        {
            return true;
        }

        // Called when a Unit is kill in battlefield zone
        public virtual void HandleKill(Player killer, Unit killed)
        {
        }

        public uint GetTypeId()
        {
            return TypeId;
        }

        public uint GetZoneId()
        {
            return ZoneId;
        }

        public uint GetMapId()
        {
            return MapId;
        }

        public Map GetMap()
        {
            return Map;
        }

        public ulong GetQueueId()
        {
            return MathFunctions.MakePair64(BattleId | 0x20000, 0x1F100000);
        }

        // Return true if battle is start, false if battle is not started
        public bool IsWarTime()
        {
            return IsActive;
        }

        private BattlefieldState GetState()
        {
            return IsActive ? BattlefieldState.InProgress : (Timer <= StartGroupingTimer ? BattlefieldState.Warnup : BattlefieldState.Inactive);
        }

        // Enable or Disable battlefield
        public void ToggleBattlefield(bool enable)
        {
            Enabled = enable;
        }

        // Return if battlefield is enable
        public bool IsEnabled()
        {
            return Enabled;
        }

        // All-purpose _data storage 64 bit
        public virtual ulong GetData64(int dataId)
        {
            return _data64[dataId];
        }

        public virtual void SetData64(int dataId, ulong value)
        {
            _data64[dataId] = value;
        }

        // All-purpose _data storage 32 bit
        public virtual uint GetData(int dataId)
        {
            return Data32[dataId];
        }

        public virtual void SetData(int dataId, uint value)
        {
            Data32[dataId] = value;
        }

        public virtual void UpdateData(int index, int pad)
        {
            if (pad < 0)
                Data32[index] -= (uint)-pad;
            else
                Data32[index] += (uint)pad;
        }

        // Battlefield - generic methods
        public uint GetDefenderTeam()
        {
            return DefenderTeam;
        }

        public uint GetAttackerTeam()
        {
            return 1 - DefenderTeam;
        }

        public int GetOtherTeam(int teamIndex)
        {
            return (teamIndex == TeamId.Horde ? TeamId.Alliance : TeamId.Horde);
        }

        private void SetDefenderTeam(uint team)
        {
            DefenderTeam = team;
        }

        // Called on start
        public virtual void OnBattleStart()
        {
        }

        // Called at the end of battle
        public virtual void OnBattleEnd(bool endByTimer)
        {
        }

        // Called x minutes before battle start when player in zone are invite to join queue
        public virtual void OnStartGrouping()
        {
        }

        // Called when a player accept to join the battle
        public virtual void OnPlayerJoinWar(Player player)
        {
        }

        // Called when a player leave the battle
        public virtual void OnPlayerLeaveWar(Player player)
        {
        }

        // Called when a player leave battlefield zone
        public virtual void OnPlayerLeaveZone(Player player)
        {
        }

        // Called when a player enter in battlefield zone
        public virtual void OnPlayerEnterZone(Player player)
        {
        }

        public uint GetBattleId()
        {
            return BattleId;
        }

        public virtual void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1)
        {
        }

        // Return if we can use Mount in battlefield
        public bool CanFlyIn()
        {
            return !IsActive;
        }

        private List<BfGraveyard> GetGraveyardVector()
        {
            return GraveyardList;
        }

        public uint GetTimer()
        {
            return Timer;
        }

        public void SetTimer(uint timer)
        {
            Timer = timer;
        }

        // use for switch off all worldstate for client
        public virtual void SendRemoveWorldStates(Player player)
        {
        }
    }
}