// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Maps.Dos;
using Game.Networking;

namespace Game.PvP
{
    // base class for specific outdoor pvp handlers
    public class OutdoorPvP : ZoneScript
    {
        // the map of the objectives belonging to this outdoorpvp
        public Dictionary<ulong, OPvPCapturePoint> CapturePoints { get; set; } = new();
        public OutdoorPvPTypes TypeId { get; set; }

        private readonly Map _map;
        private readonly List<ObjectGuid>[] _players = new List<ObjectGuid>[2];

        public OutdoorPvP(Map map)
        {
            TypeId = 0;
            _map = map;
            _players[0] = new List<ObjectGuid>();
            _players[1] = new List<ObjectGuid>();
        }

        public virtual void HandlePlayerEnterZone(Player player, uint zone)
        {
            _players[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeaveZone(Player player, uint zone)
        {
            // inform the objectives of the leaving
            foreach (var pair in CapturePoints)
                pair.Value.HandlePlayerLeave(player);

            // remove the world State information from the player (we can't keep everyone up to date, so leave out those who are not in the concerning zones)
            if (!player.Session.PlayerLogout())
                SendRemoveWorldStates(player);

            _players[player.GetTeamId()].Remove(player.GetGUID());
            Log.outDebug(LogFilter.Outdoorpvp, "Player {0} left an outdoorpvp zone", player.GetName());
        }

        public virtual void HandlePlayerResurrects(Player player, uint zone)
        {
        }

        public virtual bool Update(uint diff)
        {
            bool objective_changed = false;

            foreach (var pair in CapturePoints)
                if (pair.Value.Update(diff))
                    objective_changed = true;

            return objective_changed;
        }

        public int GetWorldState(int worldStateId)
        {
            return Global.WorldStateMgr.GetValue(worldStateId, _map);
        }

        public void SetWorldState(int worldStateId, int value)
        {
            Global.WorldStateMgr.SetValue(worldStateId, value, false, _map);
        }

        public virtual void HandleKill(Player killer, Unit killed)
        {
            Group group = killer.GetGroup();

            if (group)
            {
                for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player groupGuy = refe.GetSource();

                    if (!groupGuy)
                        continue;

                    // skip if too far away
                    if (!groupGuy.IsAtGroupRewardDistance(killed))
                        continue;

                    // creature kills must be notified, even if not inside objective / not outdoor pvp active
                    // player kills only Count if active and inside objective
                    if ((groupGuy.IsOutdoorPvPActive() && IsInsideObjective(groupGuy)) ||
                        killed.IsTypeId(Framework.Constants.TypeId.Unit))
                        HandleKillImpl(groupGuy, killed);
                }
            }
            else
            {
                // creature kills must be notified, even if not inside objective / not outdoor pvp active
                if ((killer.IsOutdoorPvPActive() && IsInsideObjective(killer)) ||
                    killed.IsTypeId(Framework.Constants.TypeId.Unit))
                    HandleKillImpl(killer, killed);
            }
        }

        public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            foreach (var pair in CapturePoints)
                if (pair.Value.HandleCustomSpell(player, spellId, go))
                    return true;

            return false;
        }

        public virtual bool HandleOpenGo(Player player, GameObject go)
        {
            foreach (var pair in CapturePoints)
                if (pair.Value.HandleOpenGo(player, go) >= 0)
                    return true;

            return false;
        }

        public virtual bool HandleDropFlag(Player player, uint id)
        {
            foreach (var pair in CapturePoints)
                if (pair.Value.HandleDropFlag(player, id))
                    return true;

            return false;
        }

        public virtual bool HandleAreaTrigger(Player player, uint trigger, bool entered)
        {
            return false;
        }

        public void RegisterZone(uint zoneId)
        {
            Global.OutdoorPvPMgr.AddZone(zoneId, this);
        }

        public bool HasPlayer(Player player)
        {
            return _players[player.GetTeamId()].Contains(player.GetGUID());
        }

        public void TeamCastSpell(uint teamIndex, int spellId)
        {
            foreach (var guid in _players[teamIndex])
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
                cp._capturePoint = go;
        }

        public override void OnGameObjectRemove(GameObject go)
        {
            if (go.GetGoType() != GameObjectTypes.ControlZone)
                return;

            OPvPCapturePoint cp = GetCapturePoint(go.GetSpawnId());

            if (cp != null)
                cp._capturePoint = null;
        }

        public void SendDefenseMessage(uint zoneId, uint id)
        {
            DefenseMessageBuilder builder = new(zoneId, id);
            var localizer = new LocalizedDo(builder);
            BroadcastWorker(localizer, zoneId);
        }

        // setup stuff
        public virtual bool SetupOutdoorPvP()
        {
            return true;
        }

        public virtual void HandleKillImpl(Player killer, Unit killed)
        {
        }

        // awards rewards for player kill
        public virtual void AwardKillBonus(Player player)
        {
        }

        public OutdoorPvPTypes GetTypeId()
        {
            return TypeId;
        }

        public virtual void SendRemoveWorldStates(Player player)
        {
        }

        public void AddCapturePoint(OPvPCapturePoint cp)
        {
            if (CapturePoints.ContainsKey(cp._capturePointSpawnId))
                Log.outError(LogFilter.Outdoorpvp, "OutdoorPvP.AddCapturePoint: CapturePoint {0} already exists!", cp._capturePointSpawnId);

            CapturePoints[cp._capturePointSpawnId] = cp;
        }

        public Map GetMap()
        {
            return _map;
        }

        private bool IsInsideObjective(Player player)
        {
            foreach (var pair in CapturePoints)
                if (pair.Value.IsInsideObjective(player))
                    return true;

            return false;
        }

        private void BroadcastPacket(ServerPacket packet)
        {
            // This is faster than sWorld.SendZoneMessage
            for (int team = 0; team < 2; ++team)
                foreach (var guid in _players[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SendPacket(packet);
                }
        }

        private void BroadcastWorker(IDoWork<Player> _worker, uint zoneId)
        {
            for (uint i = 0; i < SharedConst.PvpTeamsCount; ++i)
                foreach (var guid in _players[i])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        if (player.GetZoneId() == zoneId)
                            _worker.Invoke(player);
                }
        }

        private OPvPCapturePoint GetCapturePoint(ulong lowguid)
        {
            return CapturePoints.LookupByKey(lowguid);
        }
    }
}