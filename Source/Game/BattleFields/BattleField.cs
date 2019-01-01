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
using Framework.GameMath;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.BattleFields
{
    public enum BattleFieldTypes
    {
        WinterGrasp,
        TolBarad
    }

    public class BattleField : ZoneScript
    {
        public BattleField()
        {
            m_IsEnabled = true;
            m_DefenderTeam = TeamId.Neutral;

            m_TimeForAcceptInvite = 20;
            m_uiKickDontAcceptTimer = 1000;
            m_uiKickAfkPlayersTimer = 1000;

            m_LastResurectTimer = 30 * Time.InMilliseconds;

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
                    m_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = Time.UnixTime + 10;
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
                    player.GetSession().SendBfLeaveMessage(GetQueueId(), GetState(), player.GetZoneId() == GetZoneId());
                    Group group = player.GetGroup();
                    if (group) // Remove the player from the raid group
                        group.RemoveMember(player.GetGUID());

                    OnPlayerLeaveWar(player);
                }
            }

            foreach (var capturePoint in m_capturePoints.Values)
                capturePoint.HandlePlayerLeave(player);

            m_InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());
            m_PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
            m_players[player.GetTeamId()].Remove(player.GetGUID());
            SendRemoveWorldStates(player);
            RemovePlayerFromResurrectQueue(player.GetGUID());
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

            bool objective_changed = false;
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
                    long now = Time.UnixTime;
                    for (int team = 0; team < SharedConst.BGTeamsCount; team++)
                    {
                        foreach (var pair in m_InvitedPlayers[team])
                            if (pair.Value <= now)
                                KickPlayerFromBattlefield(pair.Key);
                    }

                    InvitePlayersInZoneToWar();
                    for (int team = 0; team < SharedConst.BGTeamsCount; team++)
                    {
                        foreach (var pair in m_PlayersWillBeKick[team])
                            if (pair.Value <= now)
                                KickPlayerFromBattlefield(pair.Key);
                    }

                    m_uiKickDontAcceptTimer = 1000;
                }
                else
                    m_uiKickDontAcceptTimer -= diff;

                foreach (var pair in m_capturePoints)
                    if (pair.Value.Update(diff))
                        objective_changed = true;
            }


            if (m_LastResurectTimer <= diff)
            {
                for (byte i = 0; i < m_GraveyardList.Count; i++)
                    if (GetGraveyardById(i) != null)
                        m_GraveyardList[i].Resurrect();
                m_LastResurectTimer = BattlegroundConst.ResurrectionInterval;
            }
            else
                m_LastResurectTimer -= diff;

            return objective_changed;
        }

        void InvitePlayersInZoneToQueue()
        {
            for (byte team = 0; team < SharedConst.BGTeamsCount; ++team)
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        InvitePlayerToQueue(player);
                }
            }
        }

        void InvitePlayerToQueue(Player player)
        {
            if (m_PlayersInQueue[player.GetTeamId()].Contains(player.GetGUID()))
                return;

            if (m_PlayersInQueue[player.GetTeamId()].Count <= m_MinPlayer || m_PlayersInQueue[GetOtherTeam(player.GetTeamId())].Count >= m_MinPlayer)
                player.GetSession().SendBfInvitePlayerToQueue(GetQueueId(), GetState());
        }

        void InvitePlayersInQueueToWar()
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_PlayersInQueue[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
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
                    if (player)
                    {
                        if (m_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) || m_InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
                            continue;
                        if (m_PlayersInWar[player.GetTeamId()].Count + m_InvitedPlayers[player.GetTeamId()].Count < m_MaxPlayer)
                            InvitePlayerToWar(player);
                        else // Battlefield is full of players
                            m_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = Time.UnixTime + 10;
                    }
                }
            }
        }

        void InvitePlayerToWar(Player player)
        {
            if (!player)
                return;

            // todo needed ?
            if (player.IsInFlight())
                return;

            if (player.InArena() || player.GetBattleground())
            {
                m_PlayersInQueue[player.GetTeamId()].Remove(player.GetGUID());
                return;
            }

            // If the player does not match minimal level requirements for the battlefield, kick him
            if (player.getLevel() < m_MinLevel)
            {
                if (!m_PlayersWillBeKick[player.GetTeamId()].ContainsKey(player.GetGUID()))
                    m_PlayersWillBeKick[player.GetTeamId()][player.GetGUID()] = Time.UnixTime + 10;
                return;
            }

            // Check if player is not already in war
            if (m_PlayersInWar[player.GetTeamId()].Contains(player.GetGUID()) || m_InvitedPlayers[player.GetTeamId()].ContainsKey(player.GetGUID()))
                return;

            m_PlayersWillBeKick[player.GetTeamId()].Remove(player.GetGUID());
            m_InvitedPlayers[player.GetTeamId()][player.GetGUID()] = Time.UnixTime + m_TimeForAcceptInvite;
            player.GetSession().SendBfInvitePlayerToWar(GetQueueId(), m_ZoneId, m_TimeForAcceptInvite);
        }

        public void InitStalker(uint entry, Position pos)
        {
            Creature creature = SpawnCreature(entry, pos);
            if (creature)
                StalkerGuid = creature.GetGUID();
            else
                Log.outError(LogFilter.Battlefield, "Battlefield.InitStalker: could not spawn Stalker (Creature entry {0}), zone messeges will be un-available", entry);
        }

        void KickAfkPlayers()
        {
            for (byte team = 0; team < 2; ++team)
            {
                foreach (var guid in m_PlayersInWar[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        if (player.isAFK())
                            KickPlayerFromBattlefield(guid);
                }
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

            OnBattleEnd(endByTimer);

            // Reset battlefield timer
            m_Timer = m_NoWarBattleTime;
            SendInitWorldStatesToAll();
        }

        void DoPlaySoundToAll(uint soundID)
        {
            BroadcastPacketToWar(new PlaySound(ObjectGuid.Empty, soundID));
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
            // Send notification
            player.GetSession().SendBfQueueInviteResponse(GetQueueId(), m_ZoneId, GetState());
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
                player.GetSession().SendBfEntered(GetQueueId(), player.GetZoneId() != GetZoneId(), player.GetTeamId() == GetAttackerTeam());
                m_PlayersInWar[player.GetTeamId()].Add(player.GetGUID());
                m_InvitedPlayers[player.GetTeamId()].Remove(player.GetGUID());

                if (player.isAFK())
                    player.ToggleAFK();

                OnPlayerJoinWar(player);                               //for scripting
            }
        }

        public void TeamCastSpell(uint teamIndex, int spellId)
        {
            foreach (var guid in m_PlayersInWar[teamIndex])
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
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
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
                    if (player)
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
                    if (player)
                        player.SendPacket(data);
                }
            }
        }

        public void SendWarning(uint id, WorldObject target = null)
        {
            Creature stalker = GetCreature(StalkerGuid);
            if (stalker)
                Global.CreatureTextMgr.SendChat(stalker, (byte)id, target);
        }

        public void SendUpdateWorldState(uint variable, uint value, bool hidden = false)
        {
            UpdateWorldState worldstate = new UpdateWorldState();
            worldstate.VariableID = variable;
            worldstate.Value = (int)value;
            worldstate.Hidden = hidden;
            BroadcastPacketToZone(worldstate);
        }

        public void AddCapturePoint(BfCapturePoint cp)
        {
            if (m_capturePoints.ContainsKey(cp.GetCapturePointEntry()))
                Log.outError(LogFilter.Battlefield, "Battlefield.AddCapturePoint: CapturePoint {0} already exists!", cp.GetCapturePointEntry());

            m_capturePoints[cp.GetCapturePointEntry()] = cp;
        }

        BfCapturePoint GetCapturePoint(uint entry)
        {
            return m_capturePoints.LookupByKey(entry);
        }

        public void RegisterZone(uint zoneId)
        {
            Global.BattleFieldMgr.AddZone(zoneId, this);
        }

        public void HideNpc(Creature creature)
        {
            creature.CombatStop();
            creature.SetReactState(ReactStates.Passive);
            creature.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            creature.DisappearAndDie();
            creature.SetVisible(false);
        }

        public void ShowNpc(Creature creature, bool aggressive)
        {
            creature.SetVisible(true);
            creature.RemoveFlag(UnitFields.Flags, UnitFlags.NonAttackable | UnitFlags.NotSelectable);
            if (!creature.IsAlive())
                creature.Respawn(true);
            if (aggressive)
                creature.SetReactState(ReactStates.Aggressive);
            else
            {
                creature.SetFlag(UnitFields.Flags, UnitFlags.NonAttackable);
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
                if (group)
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
                if (group)
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
            if (oldgroup)
                oldgroup.RemoveMember(player.GetGUID());

            Group group = GetFreeBfRaid(player.GetTeamId());
            if (!group)
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

        public WorldSafeLocsRecord GetClosestGraveYard(Player player)
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
                return CliDB.WorldSafeLocsStorage.LookupByKey(closestGY.GetGraveyardId());

            return null;
        }

        public virtual void AddPlayerToResurrectQueue(ObjectGuid npcGuid, ObjectGuid playerGuid)
        {
            for (byte i = 0; i < m_GraveyardList.Count; i++)
            {
                if (m_GraveyardList[i] == null)
                    continue;

                if (m_GraveyardList[i].HasNpc(npcGuid))
                {
                    m_GraveyardList[i].AddPlayer(playerGuid);
                    break;
                }
            }
        }

        public void RemovePlayerFromResurrectQueue(ObjectGuid playerGuid)
        {
            for (byte i = 0; i < m_GraveyardList.Count; i++)
            {
                if (m_GraveyardList[i] == null)
                    continue;

                if (m_GraveyardList[i].HasPlayer(playerGuid))
                {
                    m_GraveyardList[i].RemovePlayer(playerGuid);
                    break;
                }
            }
        }

        public void SendAreaSpiritHealerQuery(Player player, ObjectGuid guid)
        {
            AreaSpiritHealerTime areaSpiritHealerTime = new AreaSpiritHealerTime();
            areaSpiritHealerTime.HealerGuid = guid;
            areaSpiritHealerTime.TimeLeft = m_LastResurectTimer;// resurrect every 30 seconds

            player.SendPacket(areaSpiritHealerTime);
        }

        public Creature SpawnCreature(uint entry, Position pos)
        {
            //Get map object
            Map map = Global.MapMgr.CreateBaseMap(m_MapId);
            if (!map)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: Can't create creature entry: {0} map not found", entry);
                return null;
            }

            if (Global.ObjectMgr.GetCreatureTemplate(entry) == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: entry {0} does not exist.", entry);
                return null;
            }

            Creature creature = Creature.CreateCreature(entry, map, pos);
            if (!creature)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnCreature: Can't create creature entry: {0}", entry);
                return null;
            }

            creature.SetHomePosition(pos);

            // Set creature in world
            map.AddToMap(creature);
            creature.setActive(true);

            return creature;
        }

        // Method for spawning gameobject on map
        public GameObject SpawnGameObject(uint entry, Position pos, Quaternion rotation)
        {
            // Get map object
            Map map = Global.MapMgr.CreateBaseMap(m_MapId);
            if (!map)
                return null;

            if (Global.ObjectMgr.GetGameObjectTemplate(entry) == null)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield.SpawnGameObject: GameObject template {0} not found in database! Battlefield not created!", entry);
                return null;
            }

            // Create gameobject
            GameObject go = GameObject.CreateGameObject(entry, map, pos, rotation, 255, GameObjectState.Ready);
            if (!go)
            {
                Log.outError(LogFilter.Battlefield, "Battlefield:SpawnGameObject: Cannot create gameobject template {1}! Battlefield not created!", entry);
                return null;
            }

            // Add to world
            map.AddToMap(go);
            go.setActive(true);

            return go;
        }

        public Creature GetCreature(ObjectGuid guid)
        {
            if (!m_Map)
                return null;
            return m_Map.GetCreature(guid);
        }

        public GameObject GetGameObject(ObjectGuid guid)
        {
            if (!m_Map)
                return null;
            return m_Map.GetGameObject(guid);
        }

        // Call this to init the Battlefield
        public virtual bool SetupBattlefield() { return true; }

        // Called when a Unit is kill in battlefield zone
        public virtual void HandleKill(Player killer, Unit killed) { }

        public uint GetTypeId() { return m_TypeId; }
        public uint GetZoneId() { return m_ZoneId; }
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
        public int GetOtherTeam(int teamIndex) { return (teamIndex == TeamId.Horde ? TeamId.Alliance : TeamId.Horde); }
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

        // Send all worldstate data to all player in zone.
        public virtual void SendInitWorldStatesToAll() { }

        public virtual void FillInitialWorldStates(InitWorldStates data) { }

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
        Dictionary<uint, BfCapturePoint> m_capturePoints = new Dictionary<uint, BfCapturePoint>();

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
        protected List<BfGraveyard> m_GraveyardList = new List<BfGraveyard>();                          // Vector witch contain the different GY of the battle
        uint m_LastResurectTimer;                             // Timer for resurect player every 30 sec

        protected uint m_StartGroupingTimer;                            // Timer for invite players in area 15 minute before start battle
        protected bool m_StartGrouping;                                   // bool for know if all players in area has been invited

        List<ObjectGuid>[] m_Groups = new List<ObjectGuid>[2];                       // Contain different raid group

        Dictionary<int, ulong> m_Data64 = new Dictionary<int, ulong>();
        protected Dictionary<int, uint> m_Data32 = new Dictionary<int, uint>();
    }

    public class BfGraveyard
    {
        public BfGraveyard(BattleField battlefield)
        {
            m_Bf = battlefield;
            m_GraveyardId = 0;
            m_ControlTeam = TeamId.Neutral;
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
            if (!spirit)
            {
                Log.outError(LogFilter.Battlefield, "BfGraveyard:SetSpirit: Invalid Spirit.");
                return;
            }

            m_SpiritGuide[teamIndex] = spirit.GetGUID();
            spirit.SetReactState(ReactStates.Passive);
        }

        public float GetDistance(Player player)
        {
            WorldSafeLocsRecord safeLoc = CliDB.WorldSafeLocsStorage.LookupByKey(m_GraveyardId);
            return player.GetDistance2d(safeLoc.Loc.X, safeLoc.Loc.Y);
        }

        public void AddPlayer(ObjectGuid playerGuid)
        {
            if (!m_ResurrectQueue.Contains(playerGuid))
            {
                m_ResurrectQueue.Add(playerGuid);
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                if (player)
                    player.CastSpell(player, BattlegroundConst.SpellWaitingForResurrect, true);
            }
        }

        public void RemovePlayer(ObjectGuid playerGuid)
        {
            m_ResurrectQueue.Remove(playerGuid);

            Player player = Global.ObjAccessor.FindPlayer(playerGuid);
            if (player)
                player.RemoveAurasDueToSpell(BattlegroundConst.SpellWaitingForResurrect);
        }

        public void Resurrect()
        {
            if (m_ResurrectQueue.Empty())
                return;

            foreach (var guid in m_ResurrectQueue)
            {
                // Get player object from his guid
                Player player = Global.ObjAccessor.FindPlayer(guid);
                if (!player)
                    continue;

                // Check  if the player is in world and on the good graveyard
                if (player.IsInWorld)
                {
                    Creature spirit = m_Bf.GetCreature(m_SpiritGuide[m_ControlTeam]);
                    if (spirit)
                        spirit.CastSpell(spirit, BattlegroundConst.SpellSpiritHeal, true);
                }

                // Resurect player
                player.CastSpell(player, BattlegroundConst.SpellResurrectionVisual, true);
                player.ResurrectPlayer(1.0f);
                player.CastSpell(player, 6962, true);
                player.CastSpell(player, BattlegroundConst.SpellSpiritHealMana, true);

                player.SpawnCorpseBones(false);
            }

            m_ResurrectQueue.Clear();
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

            m_ControlTeam = team;
            // Teleport to other graveyard, player witch were on this graveyard
            RelocateDeadPlayers();
        }

        void RelocateDeadPlayers()
        {
            WorldSafeLocsRecord closestGrave = null;
            foreach (var guid in m_ResurrectQueue)
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);
                if (!player)
                    continue;

                if (closestGrave != null)
                    player.TeleportTo(player.GetMapId(), closestGrave.Loc.X, closestGrave.Loc.Y, closestGrave.Loc.Z, player.GetOrientation());
                else
                {
                    closestGrave = m_Bf.GetClosestGraveYard(player);
                    if (closestGrave != null)
                        player.TeleportTo(player.GetMapId(), closestGrave.Loc.X, closestGrave.Loc.Y, closestGrave.Loc.Z, player.GetOrientation());
                }
            }
        }

        public bool HasNpc(ObjectGuid guid)
        {
            if (m_SpiritGuide[TeamId.Alliance].IsEmpty() || m_SpiritGuide[TeamId.Horde].IsEmpty())
                return false;

            if (!m_Bf.GetCreature(m_SpiritGuide[TeamId.Alliance]) ||
                !m_Bf.GetCreature(m_SpiritGuide[TeamId.Horde]))
                return false;

            return (m_SpiritGuide[TeamId.Alliance] == guid || m_SpiritGuide[TeamId.Horde] == guid);
        }

        // Check if a player is in this graveyard's ressurect queue
        public bool HasPlayer(ObjectGuid guid) { return m_ResurrectQueue.Contains(guid); }

        // Get the graveyard's ID.
        public uint GetGraveyardId() { return m_GraveyardId; }

        public uint GetControlTeamId() { return m_ControlTeam; }

        uint m_ControlTeam;
        uint m_GraveyardId;
        ObjectGuid[] m_SpiritGuide = new ObjectGuid[SharedConst.BGTeamsCount];
        List<ObjectGuid> m_ResurrectQueue = new List<ObjectGuid>();
        protected BattleField m_Bf;
    }

    public class BfCapturePoint
    {
        public BfCapturePoint(BattleField battlefield)
        {
            m_Bf = battlefield;
            m_capturePointGUID = ObjectGuid.Empty;
            m_team = TeamId.Neutral;
            m_value = 0;
            m_minValue = 0.0f;
            m_maxValue = 0.0f;
            m_State = BattleFieldObjectiveStates.Neutral;
            m_OldState = BattleFieldObjectiveStates.Neutral;
            m_capturePointEntry = 0;
            m_neutralValuePct = 0;
            m_maxSpeed = 0;

            m_activePlayers[0] = new HashSet<ObjectGuid>();
            m_activePlayers[1] = new HashSet<ObjectGuid>();
        }

        public virtual bool HandlePlayerEnter(Player player)
        {
            if (!m_capturePointGUID.IsEmpty())
            {
                GameObject capturePoint = m_Bf.GetGameObject(m_capturePointGUID);
                if (capturePoint)
                {
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 1);
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)(Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f)));
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate3, m_neutralValuePct);
                }
            }

            return m_activePlayers[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeave(Player player)
        {
            if (!m_capturePointGUID.IsEmpty())
            {
                GameObject capturePoint = m_Bf.GetGameObject(m_capturePointGUID);
                if (capturePoint)
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 0);
            }

            m_activePlayers[player.GetTeamId()].Remove(player.GetGUID());
        }

        public virtual void SendChangePhase()
        {
            if (m_capturePointGUID.IsEmpty())
                return;

            GameObject capturePoint = m_Bf.GetGameObject(m_capturePointGUID);
            if (capturePoint)
            {
                // send this too, sometimes the slider disappears, dunno why :(
                SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 1);
                // send these updates to only the ones in this objective
                SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f));
                // send this too, sometimes it resets :S
                SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate3, m_neutralValuePct);
            }
        }

        public bool SetCapturePointData(GameObject capturePoint)
        {
            Cypher.Assert(capturePoint);

            Log.outError(LogFilter.Battlefield, "Creating capture point {0}", capturePoint.GetEntry());

            m_capturePointGUID = capturePoint.GetGUID();
            m_capturePointEntry = capturePoint.GetEntry();

            // check info existence
            GameObjectTemplate goinfo = capturePoint.GetGoInfo();
            if (goinfo.type != GameObjectTypes.ControlZone)
            {
                Log.outError(LogFilter.Server, "OutdoorPvP: GO {0} is not capture point!", capturePoint.GetEntry());
                return false;
            }

            // get the needed values from goinfo
            m_maxValue = goinfo.ControlZone.maxTime;
            m_maxSpeed = m_maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
            m_neutralValuePct = goinfo.ControlZone.neutralPercent;
            m_minValue = m_maxValue * goinfo.ControlZone.neutralPercent / 100;

            if (m_team == TeamId.Alliance)
            {
                m_value = m_maxValue;
                m_State = BattleFieldObjectiveStates.Alliance;
            }
            else
            {
                m_value = -m_maxValue;
                m_State = BattleFieldObjectiveStates.Horde;
            }

            return true;
        }

        GameObject GetCapturePointGo()
        {
            return m_Bf.GetGameObject(m_capturePointGUID);
        }

        bool DelCapturePoint()
        {
            if (!m_capturePointGUID.IsEmpty())
            {
                GameObject capturePoint = m_Bf.GetGameObject(m_capturePointGUID);
                if (capturePoint)
                {
                    capturePoint.SetRespawnTime(0);                  // not save respawn time
                    capturePoint.Delete();
                    capturePoint = null;
                }
                m_capturePointGUID.Clear();
            }

            return true;
        }

        public virtual bool Update(uint diff)
        {
            if (m_capturePointGUID.IsEmpty())
                return false;

            GameObject capturePoint = m_Bf.GetGameObject(m_capturePointGUID);
            if (capturePoint)
            {
                float radius = capturePoint.GetGoInfo().ControlZone.radius;

                for (byte team = 0; team < SharedConst.BGTeamsCount; ++team)
                {
                    foreach (var guid in m_activePlayers[team])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player)
                        {
                            if (!capturePoint.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                                HandlePlayerLeave(player);
                        }
                    }
                }

                List<Player> players = new List<Player>();
                var checker = new AnyPlayerInObjectRangeCheck(capturePoint, radius);
                var searcher = new PlayerListSearcher(capturePoint, players, checker);
                Cell.VisitWorldObjects(capturePoint, searcher, radius);

                foreach (var player in players)
                {
                    if (player.IsOutdoorPvPActive())
                    {
                        if (m_activePlayers[player.GetTeamId()].Add(player.GetGUID()))
                            HandlePlayerEnter(player);
                    }
                }
            }

            // get the difference of numbers
            float fact_diff = ((float)m_activePlayers[TeamId.Alliance].Count - m_activePlayers[TeamId.Horde].Count) * diff / 1000;
            if (MathFunctions.fuzzyEq(fact_diff, 0.0f))
                return false;

            Team Challenger = 0;
            float maxDiff = m_maxSpeed * diff;

            if (fact_diff < 0)
            {
                // horde is in majority, but it's already horde-controlled . no change
                if (m_State == BattleFieldObjectiveStates.Horde && m_value <= -m_maxValue)
                    return false;

                if (fact_diff < -maxDiff)
                    fact_diff = -maxDiff;

                Challenger = Team.Horde;
            }
            else
            {
                // ally is in majority, but it's already ally-controlled . no change
                if (m_State == BattleFieldObjectiveStates.Alliance && m_value >= m_maxValue)
                    return false;

                if (fact_diff > maxDiff)
                    fact_diff = maxDiff;

                Challenger = Team.Alliance;
            }

            float oldValue = m_value;
            uint oldTeam = m_team;

            m_OldState = m_State;

            m_value += fact_diff;

            if (m_value < -m_minValue)                              // red
            {
                if (m_value < -m_maxValue)
                    m_value = -m_maxValue;
                m_State = BattleFieldObjectiveStates.Horde;
                m_team = TeamId.Horde;
            }
            else if (m_value > m_minValue)                          // blue
            {
                if (m_value > m_maxValue)
                    m_value = m_maxValue;
                m_State = BattleFieldObjectiveStates.Alliance;
                m_team = TeamId.Alliance;
            }
            else if (oldValue * m_value <= 0)                       // grey, go through mid point
            {
                // if challenger is ally, then n.a challenge
                if (Challenger == Team.Alliance)
                    m_State = BattleFieldObjectiveStates.NeutralAllianceChallenge;
                // if challenger is horde, then n.h challenge
                else if (Challenger == Team.Horde)
                    m_State = BattleFieldObjectiveStates.NeutralHordeChallenge;
                m_team = TeamId.Neutral;
            }
            else                                                    // grey, did not go through mid point
            {
                // old phase and current are on the same side, so one team challenges the other
                if (Challenger == Team.Alliance && (m_OldState == BattleFieldObjectiveStates.Horde || m_OldState == BattleFieldObjectiveStates.NeutralHordeChallenge))
                    m_State = BattleFieldObjectiveStates.HordeAllianceChallenge;
                else if (Challenger == Team.Horde && (m_OldState == BattleFieldObjectiveStates.Alliance || m_OldState == BattleFieldObjectiveStates.NeutralAllianceChallenge))
                    m_State = BattleFieldObjectiveStates.AllianceHordeChallenge;
                m_team = TeamId.Neutral;
            }

            if (MathFunctions.fuzzyNe(m_value, oldValue))
                SendChangePhase();

            if (m_OldState != m_State)
            {
                if (oldTeam != m_team)
                    ChangeTeam(oldTeam);
                return true;
            }

            return false;
        }

        void SendUpdateWorldState(uint field, uint value)
        {
            for (byte team = 0; team < SharedConst.BGTeamsCount; ++team)
            {
                foreach (var guid in m_activePlayers[team])  // send to all players present in the area
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        player.SendUpdateWorldState(field, value);
                }
            }
        }

        void SendObjectiveComplete(uint id, ObjectGuid guid)
        {
            uint team;
            switch (m_State)
            {
                case BattleFieldObjectiveStates.Alliance:
                    team = TeamId.Alliance;
                    break;
                case BattleFieldObjectiveStates.Horde:
                    team = TeamId.Horde;
                    break;
                default:
                    return;
            }

            // send to all players present in the area
            foreach (var _guid in m_activePlayers[team])
            {
                Player player = Global.ObjAccessor.FindPlayer(_guid);
                if (player)
                    player.KilledMonsterCredit(id, guid);
            }
        }

        bool IsInsideObjective(Player player)
        {
            return m_activePlayers[player.GetTeamId()].Contains(player.GetGUID());
        }

        public virtual void ChangeTeam(uint oldTeam) { }

        public uint GetCapturePointEntry() { return m_capturePointEntry; }
        uint GetTeamId() { return m_team; }

        // active Players in the area of the objective, 0 - alliance, 1 - horde
        HashSet<ObjectGuid>[] m_activePlayers = new HashSet<ObjectGuid>[SharedConst.BGTeamsCount];

        // Total shift needed to capture the objective
        float m_maxValue;
        float m_minValue;

        // Maximum speed of capture
        float m_maxSpeed;

        // The status of the objective
        float m_value;
        protected uint m_team;

        // Objective states
        BattleFieldObjectiveStates m_OldState;
        BattleFieldObjectiveStates m_State;

        // Neutral value on capture bar
        uint m_neutralValuePct;

        // Battlefield this objective belongs to
        protected BattleField m_Bf;

        // Capture point entry
        uint m_capturePointEntry;

        // Gameobject related to that capture point
        ObjectGuid m_capturePointGUID;
    }
}
