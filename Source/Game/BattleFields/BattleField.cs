﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.BattleFields
{
    public enum BattleFieldTypes
    {
        WinterGrasp = 1,
        TolBarad = 2,
        Max
    }

    public class BattleField : ZoneScript
    {
        public BattleField(Map map)
        {
            m_IsEnabled = true;
            m_DefenderTeam = BatttleGroundTeamId.Neutral;

            m_TimeForAcceptInvite = 20;
            m_uiKickDontAcceptTimer = 1000;
            m_uiKickAfkPlayersTimer = 1000;

            m_Map = map;
            m_MapId = map.GetId();

            for (byte i = 0; i < 2; ++i)
            {
                m_players[i] = new List<ObjectGuid>();
                m_PlayersInQueue[i] = new List<ObjectGuid>();
                m_PlayersInWar[i] = new List<ObjectGuid>();
                m_InvitedPlayers[i] = new Dictionary<ObjectGuid, long>();
                m_PlayersWillBeKick[i] = new Dictionary<ObjectGuid, long>();
                m_Groups[i] = new List<ObjectGuid>();
            }
        }

        public void HandlePlayerEnterZone(Player player, uint zone)
        {
            // If battle is started,
            // If not full of players > invite player to join the war
            // If full of players > announce to player that BF is full and kick him after a few second if he desn't leave
            if (IsWarTime())
            {
                if (m_PlayersInWar[player.GetTeamId()].Count + m_InvitedPlayers[player.GetTeamId()].Count < m_MaxPlayer) // Vacant spaces
                    InvitePlayerToWar(player);
                else // No more vacant places
                {
                    // todo Send a packet to announce it to player
                    m_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
                    InvitePlayerToQueue(player);
                }
            }
            else
            {
                // If time left is < 15 minutes invite player to join queue
                if (m_Timer <= m_StartGroupingTimer)
                    InvitePlayerToQueue(player);
            }

            // Add player in the list of player in zone
            m_players[player.GetTeamId()].Add(player.GetGUID());
            OnPlayerEnterZone(player);
        }

        // Called when a player leave the zone
        public void HandlePlayerLeaveZone(Player player, uint zone)
        {
            if (IsWarTime())
            {
                // If the player is participating to the battle
                if (m_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()))
                {
                    m_PlayersInWar[player.GetTeamId()].Remove(player.GetGUID());
                    Group group = player.GetGroup();
                    if (group != null) // Remove the player from the raid group
                        group.RemoveMember(player.GetGUID());

                    OnPlayerLeaveWar(player);
                }
            }

            m_InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());
            m_PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
            m_players[player.GetTeamId()].Remove(player.GetGUID());
            SendRemoveWorldStates(player);
            OnPlayerLeaveZone(player);
        }

        public virtual bool Update(uint diff)
        {
            if (m_Timer <= diff)
            {
                // Battlefield ends on time
                if (IsWarTime())
                    EndBattle(true);
                else // Time to start a new battle!
                    StartBattle();
            }
            else
                m_Timer -= diff;

            // Invite players a few minutes before the battle's beginning
            if (!IsWarTime() && !m_StartGrouping && m_Timer <= m_StartGroupingTimer)
            {
                m_StartGrouping = true;
                InvitePlayersInZoneToQueue();
                OnStartGrouping();
            }

            if (IsWarTime())
            {
                if (m_uiKickAfkPlayersTimer <= diff)
                {
                    m_uiKickAfkPlayersTimer = 1000;
                    KickAfkPlayers();
                }
                else
                    m_uiKickAfkPlayersTimer -= diff;

                // Kick players who chose not to accept invitation to the battle
                if (m_uiKickDontAcceptTimer <= diff)
                {
                    long now = GameTime.GetGameTime();
                    for (int team = 0; team < SharedConst.PvpTeamsCount; team++)
                    {
                        foreach (var pair in m_InvitedPlayers[team])
                            if (pair.Value <= now)
                                KickPlayerFromBattlefield(pair.Key);
                    }

                    InvitePlayersInZoneToWar();
                    for (int team = 0; team < SharedConst.PvpTeamsCount; team++)
                    {
                        foreach (var pair in m_PlayersWillBeKick[team])
                            if (pair.Value <= now)
                                KickPlayerFromBattlefield(pair.Key);
                    }

                    m_uiKickDontAcceptTimer = 1000;
                }
                else
                    m_uiKickDontAcceptTimer -= diff;
            }

            return false;
        }

        void InvitePlayersInZoneToQueue()
        {
            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        InvitePlayerToQueue(player);
                }
            }
        }

        void InvitePlayerToQueue(Player player)
        {
            if (m_PlayersInQueue[player.GetTeamId()].Contains(player.GetGUID()))
                return;

            if (m_PlayersInQueue[player.GetTeamId()].Count <= m_MinPlayer || m_PlayersInQueue[GetOtherTeam(player.GetTeamId())].Count >= m_MinPlayer)
                PlayerAcceptInviteToQueue(player);
        }

        void InvitePlayersInQueueToWar()
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_PlayersInQueue[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                    {
                        if (m_PlayersInWar[player.GetTeamId()].Count + m_InvitedPlayers[player.GetTeamId()].Count < m_MaxPlayer)
                            InvitePlayerToWar(player);
                        else
                        {
                            //Full
                        }
                    }
                }
                m_PlayersInQueue[team].Clear();
            }
        }

        void InvitePlayersInZoneToWar()
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                    {
                        if (m_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) || m_InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
                            continue;
                        if (m_PlayersInWar[player.GetTeamId()].Count + m_InvitedPlayers[player.GetTeamId()].Count < m_MaxPlayer)
                            InvitePlayerToWar(player);
                        else // Battlefield is full of players
                            m_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
                    }
                }
            }
        }

        void InvitePlayerToWar(Player player)
        {
            if (player == null)
                return;

            // todo needed ?
            if (player.IsInFlight())
                return;

            if (player.InArena() || player.GetBattleground() != null)
            {
                m_PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());
                return;
            }

            // If the player does not match minimal level requirements for the battlefield, kick him
            if (player.GetLevel() < m_MinLevel)
            {
                if (!m_PlayersWillBeKick[player.GetTeamId()].ContainsKey(player.GetGUID()))
                    m_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + 10;
                return;
            }

            // Check if player is not already in war
            if (m_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) || m_InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
                return;

            m_PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
            m_InvitedPlayers[player.GetTeamId()][player.GetGUID()] = GameTime.GetGameTime() + m_TimeForAcceptInvite;
            PlayerAcceptInviteToWar(player);
        }

        public void InitStalker(uint entry, Position pos)
        {
            Creature creature = SpawnCreature(entry, pos);
            if (creature != null)
                StalkerGuid = creature.GetGUID();
            else
                Log.outError(LogFilter.Battlefield, "Battlefield.InitStalker: could not spawn Stalker (Creature entry {0}), zone messeges will be un-available", entry);
        }

        public override void ProcessEvent(WorldObject target, uint eventId, WorldObject invoker)
        {
            base.ProcessEvent(target, eventId, invoker);

            if (invoker != null)
            {
                GameObject gameobject = invoker.ToGameObject();
                if (gameobject != null)
                {
                    if (gameobject.GetGoType() == GameObjectTypes.ControlZone)
                    {
                        if (!ControlZoneHandlers.TryGetValue(gameobject.GetEntry(), out BattleFieldControlZoneHandler handler))
                            return;

                        var controlzone = gameobject.GetGoInfo().ControlZone;
                        if (eventId == controlzone.CaptureEventAlliance)
                            handler.HandleCaptureEventAlliance(gameobject);
                        else if (eventId == controlzone.CaptureEventHorde)
                            handler.HandleCaptureEventHorde(gameobject);
                        else if (eventId == controlzone.ContestedEventAlliance)
                            handler.HandleContestedEventAlliance(gameobject);
                        else if (eventId == controlzone.ContestedEventHorde)
                            handler.HandleContestedEventHorde(gameobject);
                        else if (eventId == controlzone.NeutralEventAlliance)
                            handler.HandleNeutralEventAlliance(gameobject);
                        else if (eventId == controlzone.NeutralEventHorde)
                            handler.HandleNeutralEventHorde(gameobject);
                        else if (eventId == controlzone.ProgressEventAlliance)
                            handler.HandleProgressEventAlliance(gameobject);
                        else if (eventId == controlzone.ProgressEventHorde)
                            handler.HandleProgressEventHorde(gameobject);
                    }
                }
            }
        }

        void KickAfkPlayers()
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        if (player.IsAFK())
                            KickPlayerFromBattlefield(guid);
                }
            }
        }

        public void KickPlayerFromBattlefield(ObjectGuid guid)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player != null)
                if (player.GetZoneId() == GetZoneId())
                    player.TeleportTo(KickPosition);
        }

        public void StartBattle()
        {
            if (m_isActive)
                return;

            for (int team = 0; team < 2; team++)
            {
                m_PlayersInWar[team].Clear();
                m_Groups[team].Clear();
            }

            m_Timer = m_BattleTime;
            m_isActive = true;

            InvitePlayersInZoneToWar();
            InvitePlayersInQueueToWar();

            OnBattleStart();
        }

        public void EndBattle(bool endByTimer)
        {
            if (!m_isActive)
                return;

            m_isActive = false;

            m_StartGrouping = false;

            if (!endByTimer)
                SetDefenderTeam(GetAttackerTeam());

            // Reset battlefield timer
            m_Timer = m_NoWarBattleTime;

            OnBattleEnd(endByTimer);
        }

        void DoPlaySoundToAll(uint soundID)
        {
            BroadcastPacketToWar(new PlaySound(ObjectGuid.Empty, soundID, 0));
        }

        public bool HasPlayer(Player player)
        {
            return m_players[player.GetTeamId()].Contains(player.GetGUID());
        }

        // Called in WorldSession:HandleBfQueueInviteResponse
        public void PlayerAcceptInviteToQueue(Player player)
        {
            // Add player in queue
            m_PlayersInQueue[player.GetTeamId()].Add(player.GetGUID());
        }

        // Called in WorldSession:HandleBfExitRequest
        public void AskToLeaveQueue(Player player)
        {
            // Remove player from queue
            m_PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());
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
                m_PlayersInWar[player.GetTeamId()].Add(player.GetGUID());
                m_InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());

                if (player.IsAFK())
                    player.ToggleAFK();

                OnPlayerJoinWar(player);                               //for scripting
            }
        }

        public void TeamCastSpell(uint teamIndex, int spellId)
        {
            foreach (var guid in m_PlayersInWar[teamIndex])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);
                if (player != null)
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
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        player.SendPacket(data);
                }
            }
        }

        public void BroadcastPacketToQueue(ServerPacket data)
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_PlayersInQueue[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        player.SendPacket(data);
                }
            }
        }

        public void BroadcastPacketToWar(ServerPacket data)
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        player.SendPacket(data);
                }
            }
        }

        public void SendWarning(uint id, WorldObject target = null)
        {
            Creature stalker = GetCreature(StalkerGuid);
            if (stalker != null)
                Global.CreatureTextMgr.SendChat(stalker, (byte)id, target);
        }

        public void RegisterZone(uint zoneId)
        {
            Global.BattleFieldMgr.AddZone(zoneId, this);
        }

        public void HideNpc(Creature creature)
        {
            creature.CombatStop();
            creature.SetReactState(ReactStates.Passive);
            creature.SetUnitFlag(UnitFlags.NonAttackable);
            creature.SetUninteractible(true);
            creature.DisappearAndDie();
            creature.SetVisible(false);
        }

        public void ShowNpc(Creature creature, bool aggressive)
        {
            creature.SetVisible(true);
            creature.RemoveUnitFlag(UnitFlags.NonAttackable);
            creature.SetUninteractible(false);
            if (creature.IsAlive())
                creature.Respawn(true);
            if (aggressive)
                creature.SetReactState(ReactStates.Aggressive);
            else
            {
                creature.SetUnitFlag(UnitFlags.NonAttackable);
                creature.SetReactState(ReactStates.Passive);
            }
        }

        // ****************************************************
        // ******************* Group System *******************
        // ****************************************************
        Group GetFreeBfRaid(int teamIndex)
        {
            foreach (var guid in m_Groups[teamIndex])
            {
                Group group = Global.GroupMgr.GetGroupByGUID(guid);
                if (group != null)
                    if (!group.IsFull())
                        return group;
            }

            return null;
        }

        Group GetGroupPlayer(ObjectGuid plguid, int teamIndex)
        {
            foreach (var guid in m_Groups[teamIndex])
            {
                Group group = Global.GroupMgr.GetGroupByGUID(guid);
                if (group != null)
                    if (group.IsMember(plguid))
                        return group;
            }

            return null;
        }

        bool AddOrSetPlayerToCorrectBfGroup(Player player)
        {
            if (!player.IsInWorld)
                return false;

            Group oldgroup = player.GetGroup();
            if (oldgroup != null)
                oldgroup.RemoveMember(player.GetGUID());

            Group group = GetFreeBfRaid(player.GetTeamId());
            if (group == null)
            {
                group = new Group();
                group.SetBattlefieldGroup(this);
                group.Create(player);
                Global.GroupMgr.AddGroup(group);
                m_Groups[player.GetTeamId()].Add(group.GetGUID());
            }
            else if (group.IsMember(player.GetGUID()))
            {
                byte subgroup = group.GetMemberGroup(player.GetGUID());
                player.SetBattlegroundOrBattlefieldRaid(group, subgroup);
            }
            else
                group.AddMember(player);

            return true;
        }

        //***************End of Group System*******************

        public BfGraveyard GetGraveyardById(int id)
        {
            if (id < m_GraveyardList.Count)
            {
                BfGraveyard graveyard = m_GraveyardList[id];
                if (graveyard != null)
                    return graveyard;
                else
                    Log.outError(LogFilter.Battlefield, "Battlefield:GetGraveyardById Id: {0} not existed", id);
            }
            else
                Log.outError(LogFilter.Battlefield, "Battlefield:GetGraveyardById Id: {0} cant be found", id);

            return null;
        }

        public WorldSafeLocsEntry GetClosestGraveyard(Player player)
        {
            BfGraveyard closestGY = null;
            float maxdist = -1;
            for (byte i = 0; i < m_GraveyardList.Count; i++)
            {
                if (m_GraveyardList[i] != null)
                {
                    if (m_GraveyardList[i].GetControlTeamId() != player.GetTeamId())
                        continue;

                    float dist = m_GraveyardList[i].GetDistance(player);
                    if (dist < maxdist || maxdist < 0)
                    {
                        closestGY = m_GraveyardList[i];
                        maxdist = dist;
                    }
                }
            }

            if (closestGY != null)
                return Global.ObjectMgr.GetWorldSafeLoc(closestGY.GetGraveyardId());

            return null;
        }

        public Creature SpawnCreature(uint entry, Position pos)
        {
            if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: entry {0} does not exist.", entry);
                return null;
            }

            Creature creature = Creature.CreateCreature(entry, m_Map, pos);
            if (creature == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: Can't create creature entry: {0}", entry);
                return null;
            }

            creature.SetHomePosition(pos);

            // Set creature in world
            m_Map.AddToMap(creature);
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
            GameObject go = GameObject.CreateGameObject(entry, m_Map, pos, rotation, 255, GameObjectState.Ready);
            if (go == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnGameObject: Cannot create gameobject template {1}! Battlefield not created!", entry);
                return null;
            }

            // Add to world
            m_Map.AddToMap(go);
            go.SetActive(true);
            go.SetFarVisible(true);

            return go;
        }

        public Creature GetCreature(ObjectGuid guid)
        {
            if (m_Map == null)
                return null;
            return m_Map.GetCreature(guid);
        }

        public GameObject GetGameObject(ObjectGuid guid)
        {
            if (m_Map == null)
                return null;
            return m_Map.GetGameObject(guid);
        }

        // Call this to init the Battlefield
        public virtual bool SetupBattlefield() { return true; }

        // Called when a Unit is kill in battlefield zone
        public virtual void HandleKill(Player killer, Unit killed) { }

        public uint GetTypeId() { return m_TypeId; }
        public uint GetZoneId() { return m_ZoneId; }
        public uint GetMapId() { return m_MapId; }
        public Map GetMap() { return m_Map; }
        public ulong GetQueueId() { return MathFunctions.MakePair64(m_BattleId | 0x20000, 0x1F100000); }

        // Return true if battle is start, false if battle is not started
        public bool IsWarTime() { return m_isActive; }

        BattlefieldState GetState() { return m_isActive ? BattlefieldState.InProgress : (m_Timer <= m_StartGroupingTimer ? BattlefieldState.Warnup : BattlefieldState.Inactive); }

        // Enable or Disable battlefield
        public void ToggleBattlefield(bool enable) { m_IsEnabled = enable; }

        // Return if battlefield is enable
        public bool IsEnabled() { return m_IsEnabled; }

        // All-purpose data storage 64 bit
        public virtual ulong GetData64(int dataId) { return m_Data64[dataId]; }
        public virtual void SetData64(int dataId, ulong value) { m_Data64[dataId] = value; }

        // All-purpose data storage 32 bit
        public virtual uint GetData(int dataId) { return m_Data32[dataId]; }
        public virtual void SetData(int dataId, uint value) { m_Data32[dataId] = value; }
        public virtual void UpdateData(int index, int pad)
        {
            if (pad < 0)
                m_Data32[index] -= (uint)-pad;
            else
                m_Data32[index] += (uint)pad;
        }

        // Battlefield - generic methods
        public uint GetDefenderTeam() { return m_DefenderTeam; }
        public uint GetAttackerTeam() { return 1 - m_DefenderTeam; }
        public int GetOtherTeam(int teamIndex) { return (teamIndex == BatttleGroundTeamId.Horde ? BatttleGroundTeamId.Alliance : BatttleGroundTeamId.Horde); }
        void SetDefenderTeam(uint team) { m_DefenderTeam = team; }

        // Called on start
        public virtual void OnBattleStart() { }
        // Called at the end of battle
        public virtual void OnBattleEnd(bool endByTimer) { }
        // Called x minutes before battle start when player in zone are invite to join queue
        public virtual void OnStartGrouping() { }
        // Called when a player accept to join the battle
        public virtual void OnPlayerJoinWar(Player player) { }
        // Called when a player leave the battle
        public virtual void OnPlayerLeaveWar(Player player) { }
        // Called when a player leave battlefield zone
        public virtual void OnPlayerLeaveZone(Player player) { }
        // Called when a player enter in battlefield zone
        public virtual void OnPlayerEnterZone(Player player) { }

        public uint GetBattleId() { return m_BattleId; }

        public virtual void DoCompleteOrIncrementAchievement(uint achievement, Player player, byte incrementNumber = 1) { }

        // Return if we can use mount in battlefield
        public bool CanFlyIn() { return !m_isActive; }

        List<BfGraveyard> GetGraveyardVector() { return m_GraveyardList; }

        public uint GetTimer() { return m_Timer; }
        public void SetTimer(uint timer) { m_Timer = timer; }

        // use for switch off all worldstate for client
        public virtual void SendRemoveWorldStates(Player player) { }

        public ObjectGuid StalkerGuid;
        protected uint m_Timer;                                         // Global timer for event
        protected bool m_IsEnabled;
        protected bool m_isActive;
        protected uint m_DefenderTeam;

        // Map of the objectives belonging to this OutdoorPvP
        Dictionary<uint /*control zone entry*/, BattleFieldControlZoneHandler> ControlZoneHandlers = new();

        // Players info maps
        protected List<ObjectGuid>[] m_players = new List<ObjectGuid>[2];                      // Players in zone
        protected List<ObjectGuid>[] m_PlayersInQueue = new List<ObjectGuid>[2];               // Players in the queue
        protected List<ObjectGuid>[] m_PlayersInWar = new List<ObjectGuid>[2];                 // Players in WG combat
        protected Dictionary<ObjectGuid, long>[] m_InvitedPlayers = new Dictionary<ObjectGuid, long>[2];
        protected Dictionary<ObjectGuid, long>[] m_PlayersWillBeKick = new Dictionary<ObjectGuid, long>[2];

        // Variables that must exist for each battlefield
        protected uint m_TypeId;                                        // See enum BattlefieldTypes
        protected uint m_BattleId;                                      // BattleID (for packet)
        protected uint m_ZoneId;                                        // ZoneID of Wintergrasp = 4197
        protected uint m_MapId;                                         // MapId where is Battlefield
        protected Map m_Map;
        protected uint m_MaxPlayer;                                     // Maximum number of player that participated to Battlefield
        protected uint m_MinPlayer;                                     // Minimum number of player for Battlefield start
        protected uint m_MinLevel;                                      // Required level to participate at Battlefield
        protected uint m_BattleTime;                                    // Length of a battle
        protected uint m_NoWarBattleTime;                               // Time between two battles
        protected uint m_RestartAfterCrash;                             // Delay to restart Wintergrasp if the server crashed during a running battle.
        protected uint m_TimeForAcceptInvite;
        protected uint m_uiKickDontAcceptTimer;
        protected WorldLocation KickPosition;                             // Position where players are teleported if they switch to afk during the battle or if they don't accept invitation

        uint m_uiKickAfkPlayersTimer;                         // Timer for check Afk in war

        // Graveyard variables
        protected List<BfGraveyard> m_GraveyardList = new();                          // Vector witch contain the different GY of the battle

        protected uint m_StartGroupingTimer;                            // Timer for invite players in area 15 minute before start battle
        protected bool m_StartGrouping;                                   // bool for know if all players in area has been invited

        List<ObjectGuid>[] m_Groups = new List<ObjectGuid>[2];                       // Contain different raid group

        Dictionary<int, ulong> m_Data64 = new();
        protected Dictionary<int, uint> m_Data32 = new();
    }

    public class BfGraveyard
    {
        public BfGraveyard(BattleField battlefield)
        {
            m_Bf = battlefield;
            m_GraveyardId = 0;
            m_ControlTeam = BatttleGroundTeamId.Neutral;
            m_SpiritGuide[0] = ObjectGuid.Empty;
            m_SpiritGuide[1] = ObjectGuid.Empty;
        }

        public void Initialize(uint startControl, uint graveyardId)
        {
            m_ControlTeam = startControl;
            m_GraveyardId = graveyardId;
        }

        public void SetSpirit(Creature spirit, int teamIndex)
        {
            if (spirit == null)
            {
                Log.outError(LogFilter.Battlefield, "BfGraveyard:SetSpirit: Invalid Spirit.");
                return;
            }

            m_SpiritGuide[teamIndex] = spirit.GetGUID();
            spirit.SetReactState(ReactStates.Passive);
        }

        public float GetDistance(Player player)
        {
            WorldSafeLocsEntry safeLoc = Global.ObjectMgr.GetWorldSafeLoc(m_GraveyardId);
            return player.GetDistance2d(safeLoc.Loc.GetPositionX(), safeLoc.Loc.GetPositionY());
        }

        // For changing graveyard control
        public void GiveControlTo(uint team)
        {
            // Guide switching
            // Note: Visiblity changes are made by phasing
            /*if (m_SpiritGuide[1 - team])
                m_SpiritGuide[1 - team].SetVisible(false);
            if (m_SpiritGuide[team])
                m_SpiritGuide[team].SetVisible(true);*/

            Creature spiritHealer = m_Bf.GetCreature(m_SpiritGuide[team]);
            if (spiritHealer != null)
                spiritHealer.SummonGraveyardTeleporter();

            m_ControlTeam = team;
        }

        public bool HasNpc(ObjectGuid guid)
        {
            if (m_SpiritGuide[BatttleGroundTeamId.Alliance].IsEmpty() || m_SpiritGuide[BatttleGroundTeamId.Horde].IsEmpty())
                return false;

            if (m_Bf.GetCreature(m_SpiritGuide[BatttleGroundTeamId.Alliance]) == null ||
                m_Bf.GetCreature(m_SpiritGuide[BatttleGroundTeamId.Horde]) == null)
                return false;

            return (m_SpiritGuide[BatttleGroundTeamId.Alliance] == guid || m_SpiritGuide[BatttleGroundTeamId.Horde] == guid);
        }

        // Get the graveyard's ID.
        public uint GetGraveyardId() { return m_GraveyardId; }

        public uint GetControlTeamId() { return m_ControlTeam; }

        uint m_ControlTeam;
        uint m_GraveyardId;
        ObjectGuid[] m_SpiritGuide = new ObjectGuid[SharedConst.PvpTeamsCount];
        protected BattleField m_Bf;
    }

    class BattleFieldControlZoneHandler : ControlZoneHandler
    {
        BattleField _battlefield;

        public BattleFieldControlZoneHandler(BattleField battlefield)
        {
            _battlefield = battlefield;
        }

        public BattleField GetBattlefield()
        {
            return _battlefield;
        }
    }
}
