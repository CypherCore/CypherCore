// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Numerics;

namespace Game.PvP
{
    // base class for specific outdoor pvp handlers
    public class OutdoorPvP : ZoneScript
    {
        public OutdoorPvP(Map map)
        {
            m_TypeId = 0;
            m_map = map;
            m_players[0] = new List<ObjectGuid>();
            m_players[1] = new List<ObjectGuid>();
        }

        public virtual void HandlePlayerEnterZone(Player player, uint zone)
        {
            m_players[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeaveZone(Player player, uint zone)
        {
            // remove the world state information from the player (we can't keep everyone up to date, so leave out those who are not in the concerning zones)
            if (!player.GetSession().PlayerLogout())
                SendRemoveWorldStates(player);
            m_players[player.GetTeamId()].Remove(player.GetGUID());
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} left an outdoorpvp zone", player.GetName());
        }

        public virtual void HandlePlayerResurrects(Player player, uint zone) { }

        public virtual void Update(uint diff)
        {
            foreach (var (_, capturePoint) in m_capturePoints)
                capturePoint.Update(diff);
        }

        public int GetWorldState(int worldStateId)
        {
            return Global.WorldStateMgr.GetValue(worldStateId, m_map);
        }

        public void SetWorldState(int worldStateId, int value)
        {
            Global.WorldStateMgr.SetValue(worldStateId, value, false, m_map);
        }

        public virtual void HandleKill(Player killer, Unit killed)
        {
            Group group = killer.GetGroup();
            if (group != null)
            {
                foreach (GroupReference groupRef in group.GetMembers())
                {
                    Player groupGuy = groupRef.GetSource();

                    // skip if too far away
                    if (!groupGuy.IsAtGroupRewardDistance(killed))
                        continue;

                    // creature kills must be notified, even if not inside objective / not outdoor pvp active
                    // player kills only count if active and inside objective
                    if (groupGuy.IsOutdoorPvPActive() || killed.IsTypeId(TypeId.Unit))
                        HandleKillImpl(groupGuy, killed);
                }
            }
            else
            {
                // creature kills must be notified, even if not inside objective / not outdoor pvp active
                if (killer.IsOutdoorPvPActive() || killed.IsTypeId(TypeId.Unit))
                    HandleKillImpl(killer, killed);
            }
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

        public void BroadcastPacket(ServerPacket packet)
        {
            // This is faster than sWorld.SendZoneMessage
            for (int team = 0; team < 2; ++team)
            {
                foreach (var guid in m_players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
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
                if (player != null)
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
            TeamCastSpell((uint)(teamIndex == BattleGroundTeamId.Alliance ? BattleGroundTeamId.Horde : BattleGroundTeamId.Alliance), spellId2 != 0 ? -(int)spellId2 : -(int)spellId);
        }

        public void SendDefenseMessage(uint zoneId, uint id)
        {
            DefenseMessageBuilder builder = new(zoneId, id);
            var localizer = new LocalizedDo(builder);
            BroadcastWorker(localizer, zoneId);
        }

        public override void ProcessEvent(WorldObject target, uint eventId, WorldObject invoker)
        {
            if (invoker != null)
            {
                GameObject gameobject = invoker.ToGameObject();
                if (gameobject != null)
                {
                    if (gameobject.GetGoType() == GameObjectTypes.ControlZone)
                    {
                        if (!ControlZoneHandlers.TryGetValue(gameobject.GetEntry(), out OutdoorPvPControlZoneHandler handler))
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

        void BroadcastWorker(IDoWork<Player> _worker, uint zoneId)
        {
            for (uint i = 0; i < SharedConst.PvpTeamsCount; ++i)
            {
                foreach (var guid in m_players[i])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);
                    if (player != null)
                        if (player.GetZoneId() == zoneId)
                            _worker.Invoke(player);
                }
            }
        }

        // setup stuff
        public virtual bool SetupOutdoorPvP() { return true; }

        public virtual void HandleKillImpl(Player killer, Unit killed) { }

        // awards rewards for player kill
        public virtual void AwardKillBonus(Player player) { }

        public OutdoorPvPTypes GetTypeId() { return m_TypeId; }

        public virtual void SendRemoveWorldStates(Player player) { }

        public Map GetMap() { return m_map; }

        // the map of the objectives belonging to this outdoorpvp
        public Dictionary<ulong, OPvPCapturePoint> m_capturePoints = new();
        protected Dictionary<uint /*control zone entry*/, OutdoorPvPControlZoneHandler> ControlZoneHandlers = new();
        List<ObjectGuid>[] m_players = new List<ObjectGuid>[2];
        public OutdoorPvPTypes m_TypeId;

        Map m_map;
    }

    public class OPvPCapturePoint
    {
        uint m_team;

        // objective states
        public ObjectiveStates OldState { get; set; }
        public ObjectiveStates State { get; set; }

        // pointer to the OutdoorPvP this objective belongs to
        public OutdoorPvP PvP { get; set; }

        public OPvPCapturePoint(OutdoorPvP pvp)
        {
            m_team = BattleGroundTeamId.Neutral;
            OldState = ObjectiveStates.Neutral;
            State = ObjectiveStates.Neutral;
            PvP = pvp;
        }

        public virtual void Update(uint diff) { }

        public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            if (!player.IsOutdoorPvPActive())
                return false;
            return false;
        }

        public virtual bool HandleDropFlag(Player player, uint id)
        {
            return false;
        }

        public virtual int HandleOpenGo(Player player, GameObject go)
        {
            return -1;
        }

        public virtual void ChangeState() { }

        public virtual void ChangeTeam(uint oldTeam) { }
    }

    public class OutdoorPvPControlZoneHandler : ControlZoneHandler
    {
        OutdoorPvP _pvp;

        public OutdoorPvPControlZoneHandler(OutdoorPvP pvp)
        {
            _pvp = pvp;
        }

        public T GetOutdoorPvP<T>() where T : OutdoorPvP { return _pvp as T; }
    }

    class DefenseMessageBuilder : MessageBuilder
    {
        public DefenseMessageBuilder(uint zoneId, uint id)
        {
            _zoneId = zoneId;
            _id = id;
        }

        public override PacketSenderOwning<DefenseMessage> Invoke(Locale locale = Locale.enUS)
        {
            string text = Global.OutdoorPvPMgr.GetDefenseMessage(_zoneId, _id, locale);

            PacketSenderOwning<DefenseMessage> defenseMessage = new();
            defenseMessage.Data.ZoneID = _zoneId;
            defenseMessage.Data.MessageText = text;
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
            pos = new Position(_x, _y, _z, _o);
            rot = new Quaternion(_rot0, _rot1, _rot2, _rot3);
        }

        public uint entry;
        public uint map;
        public Position pos;
        public Quaternion rot;
    }

    class creature_type
    {
        public creature_type(uint _entry, uint _map, float _x, float _y, float _z, float _o)
        {
            entry = _entry;
            map = _map;
            pos = new Position(_x, _y, _z, _o);
        }

        public uint entry;
        public uint map;
        Position pos;
    }
}
