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
using Game.Chat;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Misc;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.PvP
{
    // base class for specific outdoor pvp handlers
    public class OutdoorPvP : ZoneScript
    {
        public OutdoorPvP()
        {
            m_TypeId = 0;
            m_sendUpdate = true;
            m_players[0] = new List<ObjectGuid>();
            m_players[1] = new List<ObjectGuid>();
        }

        public virtual void HandlePlayerEnterZone(Player player, uint zone)
        {
            m_players[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeaveZone(Player player, uint zone)
        {
            // inform the objectives of the leaving
            foreach (var pair in m_capturePoints)
                pair.Value.HandlePlayerLeave(player);
            // remove the world state information from the player (we can't keep everyone up to date, so leave out those who are not in the concerning zones)
            if (!player.GetSession().PlayerLogout())
                SendRemoveWorldStates(player);
            m_players[player.GetTeamId()].Remove(player.GetGUID());
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} left an outdoorpvp zone", player.GetName());
        }

        public virtual void HandlePlayerResurrects(Player player, uint zone) { }

        public virtual bool Update(uint diff)
        {
            bool objective_changed = false;
            foreach (var pair in m_capturePoints)
            {
                if (pair.Value.Update(diff))
                    objective_changed = true;
            }
            return objective_changed;
        }

        public void SendUpdateWorldState(uint field, uint value)
        {
            if (m_sendUpdate)
            {
                for (int i = 0; i < 2; ++i)
                {
                    foreach (var guid in m_players[i])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);
                        if (player)
                            player.SendUpdateWorldState(field, value);
                    }
                }
            }
        }

        public virtual void HandleKill(Player killer, Unit killed)
        {
            Group group = killer.GetGroup();
            if (group)
            {
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
                {
                    Player groupGuy = refe.GetSource();

                    if (!groupGuy)
                        continue;

                    // skip if too far away
                    if (!groupGuy.IsAtGroupRewardDistance(killed))
                        continue;

                    // creature kills must be notified, even if not inside objective / not outdoor pvp active
                    // player kills only count if active and inside objective
                    if ((groupGuy.IsOutdoorPvPActive() && IsInsideObjective(groupGuy)) || killed.IsTypeId(TypeId.Unit))
                        HandleKillImpl(groupGuy, killed);
                }
            }
            else
            {
                // creature kills must be notified, even if not inside objective / not outdoor pvp active
                if ((killer.IsOutdoorPvPActive() && IsInsideObjective(killer)) || killed.IsTypeId(TypeId.Unit))
                    HandleKillImpl(killer, killed);
            }
        }

        bool IsInsideObjective(Player player)
        {
            foreach (var pair in m_capturePoints)
                if (pair.Value.IsInsideObjective(player))
                    return true;

            return false;
        }

        public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            foreach (var pair in m_capturePoints)
                if (pair.Value.HandleCustomSpell(player, spellId, go))
                    return true;

            return false;
        }

        public virtual bool HandleOpenGo(Player player, GameObject go)
        {
            foreach (var pair in m_capturePoints)
                if (pair.Value.HandleOpenGo(player, go) >= 0)
                    return true;

            return false;
        }

        public virtual bool HandleGossipOption(Player player, Creature creature, uint id)
        {
            foreach (var pair in m_capturePoints)
                if (pair.Value.HandleGossipOption(player, creature, id))
                    return true;

            return false;
        }

        public virtual bool CanTalkTo(Player player, Creature c, GossipMenuItems gso)
        {
            foreach (var pair in m_capturePoints)
                if (pair.Value.CanTalkTo(player, c, gso))
                    return true;

            return false;
        }

        public virtual bool HandleDropFlag(Player player, uint id)
        {
            foreach (var pair in m_capturePoints)
                if (pair.Value.HandleDropFlag(player, id))
                    return true;

            return false;
        }

        public virtual bool HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            return false;
        }

        void BroadcastPacket(ServerPacket packet)
        {
            // This is faster than sWorld.SendZoneMessage
            for (int team = 0; team < 2; ++team)
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        player.SendPacket(packet);
                }
            }
        }

        public void RegisterZone(uint zoneId)
        {
            Global.OutdoorPvPMgr.AddZone(zoneId, this);
        }

        public bool HasPlayer(Player player)
        {
            return m_players[player.GetTeamId()].Contains(player.GetGUID());
        }

        public void TeamCastSpell(uint teamIndex, int spellId)
        {
            foreach (var guid in m_players[teamIndex])
            {
                Player player = Global.ObjAccessor.FindPlayer(guid);
                if (player)
                {
                    if (spellId > 0)
                        player.CastSpell(player, (uint)spellId, true);
                    else
                        player.RemoveAura((uint)-spellId); // by stack?
                }
            }
        }

        public void TeamApplyBuff(uint teamIndex, uint spellId, uint spellId2)
        {
            TeamCastSpell(teamIndex, (int)spellId);
            TeamCastSpell((uint)(teamIndex == TeamId.Alliance ? TeamId.Horde : TeamId.Alliance), spellId2 != 0 ? -(int)spellId2 : -(int)spellId);
        }

        public override void OnGameObjectCreate(GameObject go)
        {
            if (go.GetGoType() != GameObjectTypes.ControlZone)
                return;

            OPvPCapturePoint cp = GetCapturePoint(go.GetSpawnId());
            if (cp != null)
                cp.m_capturePoint = go;
        }

        public override void OnGameObjectRemove(GameObject go)
        {
            if (go.GetGoType() != GameObjectTypes.ControlZone)
                return;

            OPvPCapturePoint cp = GetCapturePoint(go.GetSpawnId());
            if (cp != null)
                cp.m_capturePoint = null;
        }

        public void SendDefenseMessage(uint zoneId, uint id)
        {
            DefenseMessageBuilder builder = new DefenseMessageBuilder(zoneId, id);
            var localizer = new LocalizedPacketDo(builder);
            BroadcastWorker(localizer, zoneId);
        }

        void BroadcastWorker(IDoWork<Player> _worker, uint zoneId)
        {
            for (uint i = 0; i < SharedConst.BGTeamsCount; ++i)
            {
                foreach (var guid in m_players[i])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        if (player.GetZoneId() == zoneId)
                            _worker.Invoke(player);
                }
            }
        }

        public virtual void FillInitialWorldStates(InitWorldStates data) { }

        // setup stuff
        public virtual bool SetupOutdoorPvP() { return true; }

        public virtual void HandleKillImpl(Player killer, Unit killed) { }

        // awards rewards for player kill
        public virtual void AwardKillBonus(Player player) { }

        public OutdoorPvPTypes GetTypeId() { return m_TypeId; }

        public virtual void SendRemoveWorldStates(Player player) { }

        public void AddCapturePoint(OPvPCapturePoint cp)
        {
            if (m_capturePoints.ContainsKey(cp.m_capturePointSpawnId))
                Log.outError(LogFilter.Outdoorpvp, "OutdoorPvP.AddCapturePoint: CapturePoint {0} already exists!", cp.m_capturePointSpawnId);

            m_capturePoints[cp.m_capturePointSpawnId] = cp;
        }

        OPvPCapturePoint GetCapturePoint(ulong lowguid)
        {
            return m_capturePoints.LookupByKey(lowguid);
        }

        public Map GetMap() { return m_map; }

        // Hack to store map because this code is just shit
        public void SetMapFromZone(uint zone)
        {
            AreaTableRecord areaTable = CliDB.AreaTableStorage.LookupByKey(zone);
            Cypher.Assert(areaTable != null);
            Map map = Global.MapMgr.CreateBaseMap(areaTable.ContinentID);
            Cypher.Assert(!map.Instanceable());
            m_map = map;
        }

        // the map of the objectives belonging to this outdoorpvp
        public Dictionary<ulong, OPvPCapturePoint> m_capturePoints = new Dictionary<ulong, OPvPCapturePoint>();
        List<ObjectGuid>[] m_players = new List<ObjectGuid>[2];
        public OutdoorPvPTypes m_TypeId;
        bool m_sendUpdate;

        Map m_map;
    }

    public class OPvPCapturePoint
    {
        public OPvPCapturePoint(OutdoorPvP pvp)
        {
            m_team = TeamId.Neutral;
            m_OldState = ObjectiveStates.Neutral;
            m_State = ObjectiveStates.Neutral;
            m_PvP = pvp;

            m_activePlayers[0] = new HashSet<ObjectGuid>();
            m_activePlayers[1] = new HashSet<ObjectGuid>();
        }

        public virtual bool HandlePlayerEnter(Player player)
        {
            if (m_capturePoint)
            {
                player.SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldState1, 1);
                player.SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f));
                player.SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldstate3, m_neutralValuePct);
            }
            return m_activePlayers[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeave(Player player)
        {
            if (m_capturePoint)
                player.SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldState1, 0);
            m_activePlayers[player.GetTeamId()].Remove(player.GetGUID());
        }

        public virtual void SendChangePhase()
        {
            if (!m_capturePoint)
                return;

            // send this too, sometimes the slider disappears, dunno why :(
            SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldState1, 1);
            // send these updates to only the ones in this objective
            SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((m_value + m_maxValue) / (2 * m_maxValue) * 100.0f));
            // send this too, sometimes it resets :S
            SendUpdateWorldState(m_capturePoint.GetGoInfo().ControlZone.worldstate3, m_neutralValuePct);
        }

        void AddGO(uint type, ulong guid)
        {
            GameObjectData data = Global.ObjectMgr.GetGOData(guid);
            if (data == null)
                return;

            m_Objects[type] = guid;
            m_ObjectTypes[guid] = type;
        }

        void AddCre(uint type, ulong guid)
        {
            CreatureData data = Global.ObjectMgr.GetCreatureData(guid);
            if (data == null)
                return;

            m_Creatures[type] = guid;
            m_CreatureTypes[guid] = type;
        }

        public bool AddObject(uint type, uint entry, uint map, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3)
        {
            ulong guid = Global.ObjectMgr.AddGOData(entry, map, x, y, z, o, 0, rotation0, rotation1, rotation2, rotation3);
            if (guid != 0)
            {
                AddGO(type, guid);
                return true;
            }

            return false;
        }

        public bool AddCreature(uint type, uint entry, uint team, uint map, float x, float y, float z, float o, uint spawntimedelay)
        {
            ulong guid = Global.ObjectMgr.AddCreatureData(entry, team, map, x, y, z, o, spawntimedelay);
            if (guid != 0)
            {
                AddCre(type, guid);
                return true;
            }

            return false;
        }

        public bool SetCapturePointData(uint entry, uint map, float x, float y, float z, float o, float rotation0, float rotation1, float rotation2, float rotation3)
        {
            Log.outDebug(LogFilter.Outdoorpvp, "Creating capture point {0}", entry);

            // check info existence
            GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);
            if (goinfo == null || goinfo.type != GameObjectTypes.ControlZone)
            {
                Log.outError(LogFilter.Outdoorpvp, "OutdoorPvP: GO {0} is not capture point!", entry);
                return false;
            }

            m_capturePointSpawnId = Global.ObjectMgr.AddGOData(entry, map, x, y, z, o, 0, rotation0, rotation1, rotation2, rotation3);
            if (m_capturePointSpawnId == 0)
                return false;

            // get the needed values from goinfo
            m_maxValue = goinfo.ControlZone.maxTime;
            m_maxSpeed = m_maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
            m_neutralValuePct = goinfo.ControlZone.neutralPercent;
            m_minValue = MathFunctions.CalculatePct(m_maxValue, m_neutralValuePct);

            return true;
        }

        public bool DelCreature(uint type)
        {
            if (!m_Creatures.ContainsKey(type))
            {
                Log.outDebug(LogFilter.Outdoorpvp, "opvp creature type {0} was already deleted", type);
                return false;
            }
            ulong spawnId = m_Creatures[type];

            var bounds = m_PvP.GetMap().GetCreatureBySpawnIdStore().LookupByKey(spawnId);
            foreach (var creature in bounds)
            {
                // Don't save respawn time
                creature.SetRespawnTime(0);
                creature.RemoveCorpse();
                creature.AddObjectToRemoveList();
            }

            Log.outDebug(LogFilter.Outdoorpvp, "deleting opvp creature type {0}", type);

            // delete respawn time for this creature
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CREATURE_RESPAWN);
            stmt.AddValue(0, spawnId);
            stmt.AddValue(1, m_PvP.GetMap().GetId());
            stmt.AddValue(2, 0);  // instance id, always 0 for world maps
            DB.Characters.Execute(stmt);

            Global.ObjectMgr.DeleteCreatureData(spawnId);
            m_CreatureTypes.Remove(spawnId);
            m_Creatures.Remove(type);
            return true;
        }

        public bool DelObject(uint type)
        {
            if (!m_Objects.ContainsKey(type))
                return false;

            ulong spawnId = m_Objects[type];
            var bounds = m_PvP.GetMap().GetGameObjectBySpawnIdStore().LookupByKey(spawnId);
            foreach (var gameobject in bounds)
            {
                // Don't save respawn time
                gameobject.SetRespawnTime(0);
                gameobject.Delete();
            }

            Global.ObjectMgr.DeleteGOData(spawnId);
            m_ObjectTypes.Remove(spawnId);
            m_Objects.Remove(type);
            return true;
        }

        bool DelCapturePoint()
        {
            Global.ObjectMgr.DeleteGOData(m_capturePointSpawnId);
            m_capturePointSpawnId = 0;

            if (m_capturePoint)
            {
                m_capturePoint.SetRespawnTime(0);                                 // not save respawn time
                m_capturePoint.Delete();
            }

            return true;
        }

        public virtual void DeleteSpawns()
        {
            foreach (var type in m_Objects.Keys)
                DelObject(type);

            foreach (var type in m_Creatures.Keys)
                DelCreature(type);

            DelCapturePoint();
        }

        public virtual bool Update(uint diff)
        {
            if (!m_capturePoint)
                return false;

            float radius = m_capturePoint.GetGoInfo().ControlZone.radius;

            for (int team = 0; team < 2; ++team)
            {
                foreach (var playerGuid in m_activePlayers[team].ToList())
                {
                    Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                    if (player)
                        if (!m_capturePoint.IsWithinDistInMap(player, radius) || !player.IsOutdoorPvPActive())
                            HandlePlayerLeave(player);
                }
            }

            List<Player> players = new List<Player>();
            var checker = new AnyPlayerInObjectRangeCheck(m_capturePoint, radius);
            var searcher = new PlayerListSearcher(m_capturePoint, players, checker);
            Cell.VisitWorldObjects(m_capturePoint, searcher, radius);

            foreach (var player in players)
            {
                if (player.IsOutdoorPvPActive())
                {
                    if (m_activePlayers[player.GetTeamId()].Add(player.GetGUID()))
                        HandlePlayerEnter(player);
                }
            }

            // get the difference of numbers
            float fact_diff = (float)(m_activePlayers[0].Count - m_activePlayers[1].Count) * diff / 1000;
            if (fact_diff == 0.0f)
                return false;

            Team Challenger = 0;
            float maxDiff = m_maxSpeed * diff;

            if (fact_diff < 0)
            {
                // horde is in majority, but it's already horde-controlled . no change
                if (m_State == ObjectiveStates.Horde && m_value <= -m_maxValue)
                    return false;

                if (fact_diff < -maxDiff)
                    fact_diff = -maxDiff;

                Challenger = Team.Horde;
            }
            else
            {
                // ally is in majority, but it's already ally-controlled . no change
                if (m_State == ObjectiveStates.Alliance && m_value >= m_maxValue)
                    return false;

                if (fact_diff > maxDiff)
                    fact_diff = maxDiff;

                Challenger = Team.Alliance;
            }

            float oldValue = m_value;
            uint oldTeam = m_team;

            m_OldState = m_State;

            m_value += fact_diff;

            if (m_value < -m_minValue) // red
            {
                if (m_value < -m_maxValue)
                    m_value = -m_maxValue;
                m_State = ObjectiveStates.Horde;
                m_team = TeamId.Horde;
            }
            else if (m_value > m_minValue) // blue
            {
                if (m_value > m_maxValue)
                    m_value = m_maxValue;
                m_State = ObjectiveStates.Alliance;
                m_team = TeamId.Alliance;
            }
            else if (oldValue * m_value <= 0) // grey, go through mid point
            {
                // if challenger is ally, then n.a challenge
                if (Challenger == Team.Alliance)
                    m_State = ObjectiveStates.NeutralAllianceChallenge;
                // if challenger is horde, then n.h challenge
                else if (Challenger == Team.Horde)
                    m_State = ObjectiveStates.NeutralHordeChallenge;
                m_team = TeamId.Neutral;
            }
            else // grey, did not go through mid point
            {
                // old phase and current are on the same side, so one team challenges the other
                if (Challenger == Team.Alliance && (m_OldState == ObjectiveStates.Horde || m_OldState == ObjectiveStates.NeutralHordeChallenge))
                    m_State = ObjectiveStates.HordeAllianceChallenge;
                else if (Challenger == Team.Horde && (m_OldState == ObjectiveStates.Alliance || m_OldState == ObjectiveStates.NeutralAllianceChallenge))
                    m_State = ObjectiveStates.AllianceHordeChallenge;
                m_team = TeamId.Neutral;
            }

            if (m_value != oldValue)
                SendChangePhase();

            if (m_OldState != m_State)
            {
                if (oldTeam != m_team)
                    ChangeTeam(oldTeam);
                ChangeState();
                return true;
            }

            return false;
        }

        public void SendUpdateWorldState(uint field, uint value)
        {
            for (int team = 0; team < 2; ++team)
            {
                // send to all players present in the area
                foreach (var guid in m_activePlayers[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player)
                        player.SendUpdateWorldState(field, value);
                }
            }
        }

        public void SendObjectiveComplete(uint id, ObjectGuid guid)
        {
            uint team;
            switch (m_State)
            {
                case ObjectiveStates.Alliance:
                    team = 0;
                    break;
                case ObjectiveStates.Horde:
                    team = 1;
                    break;
                default:
                    return;
            }

            // send to all players present in the area
            foreach (var playerGuid in m_activePlayers[team])
            {
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);
                if (player)
                    player.KilledMonsterCredit(id, guid);
            }
        }

        public bool IsInsideObjective(Player player)
        {
            var plSet = m_activePlayers[player.GetTeamId()];
            return plSet.Contains(player.GetGUID());
        }

        public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            if (!player.IsOutdoorPvPActive())
                return false;
            return false;
        }

        public virtual bool HandleGossipOption(Player player, Creature creature, uint id)
        {
            return false;
        }

        public virtual bool CanTalkTo(Player player, Creature c, GossipMenuItems gso)
        {
            return false;
        }

        public virtual bool HandleDropFlag(Player player, uint id)
        {
            return false;
        }

        public virtual int HandleOpenGo(Player player, GameObject go)
        {
            var value = m_ObjectTypes.LookupByKey(go.GetSpawnId());
            if (value != 0)
                return (int)value;

            return -1;
        }

        public virtual void FillInitialWorldStates(InitWorldStates data) { }

        public virtual void ChangeState() { }

        public virtual void ChangeTeam(uint oldTeam) { }

        public ulong m_capturePointSpawnId;
        public GameObject m_capturePoint;
        // active players in the area of the objective, 0 - alliance, 1 - horde
        public HashSet<ObjectGuid>[] m_activePlayers = new HashSet<ObjectGuid>[2];
        // total shift needed to capture the objective
        public float m_maxValue;
        float m_minValue;
        // maximum speed of capture
        float m_maxSpeed;
        // the status of the objective
        public float m_value;
        uint m_team;
        // objective states
        public ObjectiveStates m_OldState { get; set; }
        public ObjectiveStates m_State { get; set; }
        // neutral value on capture bar
        public uint m_neutralValuePct;
        // pointer to the OutdoorPvP this objective belongs to
        public OutdoorPvP m_PvP { get; set; }

        public Dictionary<uint, ulong> m_Objects = new Dictionary<uint, ulong>();
        public Dictionary<uint, ulong> m_Creatures = new Dictionary<uint, ulong>();
        Dictionary<ulong, uint> m_ObjectTypes = new Dictionary<ulong, uint>();
        Dictionary<ulong, uint> m_CreatureTypes = new Dictionary<ulong, uint>();
    }

    class DefenseMessageBuilder : MessageBuilder
    {
        public DefenseMessageBuilder(uint zoneId, uint id)
        {
            _zoneId = zoneId;
            _id = id;
        }

        public override ServerPacket Invoke(LocaleConstant locale = LocaleConstant.enUS)
        {
            string text = Global.OutdoorPvPMgr.GetDefenseMessage(_zoneId, _id, locale);

            DefenseMessage defenseMessage = new DefenseMessage();
            defenseMessage.ZoneID = _zoneId;
            defenseMessage.MessageText = text;
            return defenseMessage;
        }

        uint _zoneId; // ZoneId
        uint _id;     // BroadcastTextId
    }

    public class go_type
    {
        public go_type(uint _entry, uint _map, float _x, float _y, float _z, float _o, float _rot0, float _rot1, float _rot2, float _rot3)
        {
            entry = _entry;
            map = _map;
            x = _x;
            y = _y;
            z = _z;
            o = _o;
            rot0 = _rot0;
            rot1 = _rot1;
            rot2 = _rot2;
            rot3 = _rot3;
        }

        public uint entry;
        public uint map;
        public float x;
        public float y;
        public float z;
        public float o;
        public float rot0;
        public float rot1;
        public float rot2;
        public float rot3;
    }

    class creature_type
    {
        public creature_type(uint _entry, uint _map, float _x, float _y, float _z, float _o)
        {
            entry = _entry;
            map = _map;
            x = _x;
            y = _y;
            z = _z;
            o = _o;
        }

        public uint entry;
        public uint map;
        public float x;
        public float y;
        public float z;
        public float o;
    }
}
