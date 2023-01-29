// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Framework.Constants;
using Game.Chat;
using Game.Entities;
using Game.Groups;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.PvP
{
    // base class for specific outdoor pvp handlers
    public class OutdoorPvP : ZoneScript
    {
        // the map of the objectives belonging to this outdoorpvp
        public Dictionary<ulong, OPvPCapturePoint> _capturePoints = new();
        public OutdoorPvPTypes _TypeId;

        private readonly Map _map;
        private readonly List<ObjectGuid>[] _players = new List<ObjectGuid>[2];

        public OutdoorPvP(Map map)
        {
            _TypeId = 0;
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
            foreach (var pair in _capturePoints)
                pair.Value.HandlePlayerLeave(player);

            // remove the world State information from the player (we can't keep everyone up to date, so leave out those who are not in the concerning zones)
            if (!player.GetSession().PlayerLogout())
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

            foreach (var pair in _capturePoints)
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
                        killed.IsTypeId(TypeId.Unit))
                        HandleKillImpl(groupGuy, killed);
                }
            }
            else
            {
                // creature kills must be notified, even if not inside objective / not outdoor pvp active
                if ((killer.IsOutdoorPvPActive() && IsInsideObjective(killer)) ||
                    killed.IsTypeId(TypeId.Unit))
                    HandleKillImpl(killer, killed);
            }
        }

        public virtual bool HandleCustomSpell(Player player, uint spellId, GameObject go)
        {
            foreach (var pair in _capturePoints)
                if (pair.Value.HandleCustomSpell(player, spellId, go))
                    return true;

            return false;
        }

        public virtual bool HandleOpenGo(Player player, GameObject go)
        {
            foreach (var pair in _capturePoints)
                if (pair.Value.HandleOpenGo(player, go) >= 0)
                    return true;

            return false;
        }

        public virtual bool HandleDropFlag(Player player, uint id)
        {
            foreach (var pair in _capturePoints)
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
            return _TypeId;
        }

        public virtual void SendRemoveWorldStates(Player player)
        {
        }

        public void AddCapturePoint(OPvPCapturePoint cp)
        {
            if (_capturePoints.ContainsKey(cp._capturePointSpawnId))
                Log.outError(LogFilter.Outdoorpvp, "OutdoorPvP.AddCapturePoint: CapturePoint {0} already exists!", cp._capturePointSpawnId);

            _capturePoints[cp._capturePointSpawnId] = cp;
        }

        public Map GetMap()
        {
            return _map;
        }

        private bool IsInsideObjective(Player player)
        {
            foreach (var pair in _capturePoints)
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
            return _capturePoints.LookupByKey(lowguid);
        }
    }

    public class OPvPCapturePoint
    {
        // active players in the area of the objective, 0 - alliance, 1 - horde
        public HashSet<ObjectGuid>[] _activePlayers = new HashSet<ObjectGuid>[2];
        public GameObject _capturePoint;

        public ulong _capturePointSpawnId;

        // total shift needed to capture the objective
        public float _maxValue;

        // neutral value on capture bar
        public uint _neutralValuePct;

        // the status of the objective
        public float _value;

        // maximum speed of capture
        private float _maxSpeed;

        private float _minValue;

        private uint _team;

        public OPvPCapturePoint(OutdoorPvP pvp)
        {
            _team = TeamId.Neutral;
            OldState = ObjectiveStates.Neutral;
            State = ObjectiveStates.Neutral;
            PvP = pvp;

            _activePlayers[0] = new HashSet<ObjectGuid>();
            _activePlayers[1] = new HashSet<ObjectGuid>();
        }

        // objective states
        public ObjectiveStates OldState { get; set; }

        public ObjectiveStates State { get; set; }

        // pointer to the OutdoorPvP this objective belongs to
        public OutdoorPvP PvP { get; set; }

        public virtual bool HandlePlayerEnter(Player player)
        {
            if (_capturePoint)
            {
                player.SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldState1, 1);
                player.SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f));
                player.SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldstate3, _neutralValuePct);
            }

            return _activePlayers[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeave(Player player)
        {
            if (_capturePoint)
                player.SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldState1, 0);

            _activePlayers[player.GetTeamId()].Remove(player.GetGUID());
        }

        public virtual void SendChangePhase()
        {
            if (!_capturePoint)
                return;

            // send this too, sometimes the slider disappears, dunno why :(
            SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldState1, 1);
            // send these updates to only the ones in this objective
            SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f));
            // send this too, sometimes it resets :S
            SendUpdateWorldState(_capturePoint.GetGoInfo().ControlZone.worldstate3, _neutralValuePct);
        }

        public bool SetCapturePointData(uint entry)
        {
            Log.outDebug(LogFilter.Outdoorpvp, "Creating capture point {0}", entry);

            // check info existence
            GameObjectTemplate goinfo = Global.ObjectMgr.GetGameObjectTemplate(entry);

            if (goinfo == null ||
                goinfo.type != GameObjectTypes.ControlZone)
            {
                Log.outError(LogFilter.Outdoorpvp, "OutdoorPvP: GO {0} is not capture point!", entry);

                return false;
            }

            // get the needed values from goinfo
            _maxValue = goinfo.ControlZone.maxTime;
            _maxSpeed = _maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
            _neutralValuePct = goinfo.ControlZone.neutralPercent;
            _minValue = MathFunctions.CalculatePct(_maxValue, _neutralValuePct);

            return true;
        }

        public virtual bool Update(uint diff)
        {
            if (!_capturePoint)
                return false;

            float radius = _capturePoint.GetGoInfo().ControlZone.radius;

            for (int team = 0; team < 2; ++team)
                foreach (var playerGuid in _activePlayers[team].ToList())
                {
                    Player player = Global.ObjAccessor.FindPlayer(playerGuid);

                    if (player)
                        if (!_capturePoint.IsWithinDistInMap(player, radius) ||
                            !player.IsOutdoorPvPActive())
                            HandlePlayerLeave(player);
                }

            List<Unit> players = new();
            var checker = new AnyPlayerInObjectRangeCheck(_capturePoint, radius);
            var searcher = new PlayerListSearcher(_capturePoint, players, checker);
            Cell.VisitWorldObjects(_capturePoint, searcher, radius);

            foreach (Player player in players)
                if (player.IsOutdoorPvPActive())
                    if (_activePlayers[player.GetTeamId()].Add(player.GetGUID()))
                        HandlePlayerEnter(player);

            // get the difference of numbers
            float fact_diff = (float)(_activePlayers[0].Count - _activePlayers[1].Count) * diff / 1000;

            if (fact_diff == 0.0f)
                return false;

            Team Challenger;
            float maxDiff = _maxSpeed * diff;

            if (fact_diff < 0)
            {
                // horde is in majority, but it's already horde-controlled . no change
                if (State == ObjectiveStates.Horde &&
                    _value <= -_maxValue)
                    return false;

                if (fact_diff < -maxDiff)
                    fact_diff = -maxDiff;

                Challenger = Team.Horde;
            }
            else
            {
                // ally is in majority, but it's already ally-controlled . no change
                if (State == ObjectiveStates.Alliance &&
                    _value >= _maxValue)
                    return false;

                if (fact_diff > maxDiff)
                    fact_diff = maxDiff;

                Challenger = Team.Alliance;
            }

            float oldValue = _value;
            uint oldTeam = _team;

            OldState = State;

            _value += fact_diff;

            if (_value < -_minValue) // red
            {
                if (_value < -_maxValue)
                    _value = -_maxValue;

                State = ObjectiveStates.Horde;
                _team = TeamId.Horde;
            }
            else if (_value > _minValue) // blue
            {
                if (_value > _maxValue)
                    _value = _maxValue;

                State = ObjectiveStates.Alliance;
                _team = TeamId.Alliance;
            }
            else if (oldValue * _value <= 0) // grey, go through mid point
            {
                // if challenger is ally, then n.a challenge
                if (Challenger == Team.Alliance)
                    State = ObjectiveStates.NeutralAllianceChallenge;
                // if challenger is horde, then n.h challenge
                else if (Challenger == Team.Horde)
                    State = ObjectiveStates.NeutralHordeChallenge;

                _team = TeamId.Neutral;
            }
            else // grey, did not go through mid point
            {
                // old phase and current are on the same side, so one team challenges the other
                if (Challenger == Team.Alliance &&
                    (OldState == ObjectiveStates.Horde || OldState == ObjectiveStates.NeutralHordeChallenge))
                    State = ObjectiveStates.HordeAllianceChallenge;
                else if (Challenger == Team.Horde &&
                         (OldState == ObjectiveStates.Alliance || OldState == ObjectiveStates.NeutralAllianceChallenge))
                    State = ObjectiveStates.AllianceHordeChallenge;

                _team = TeamId.Neutral;
            }

            if (_value != oldValue)
                SendChangePhase();

            if (OldState != State)
            {
                if (oldTeam != _team)
                    ChangeTeam(oldTeam);

                ChangeState();

                return true;
            }

            return false;
        }

        public void SendUpdateWorldState(uint field, uint value)
        {
            for (int team = 0; team < 2; ++team)
                // send to all players present in the area
                foreach (var guid in _activePlayers[team])
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SendUpdateWorldState(field, value);
                }
        }

        public void SendObjectiveComplete(uint id, ObjectGuid guid)
        {
            uint team;

            switch (State)
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
            foreach (var playerGuid in _activePlayers[team])
            {
                Player player = Global.ObjAccessor.FindPlayer(playerGuid);

                if (player)
                    player.KilledMonsterCredit(id, guid);
            }
        }

        public bool IsInsideObjective(Player player)
        {
            var plSet = _activePlayers[player.GetTeamId()];

            return plSet.Contains(player.GetGUID());
        }

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

        public virtual void ChangeState()
        {
        }

        public virtual void ChangeTeam(uint oldTeam)
        {
        }
    }

    internal class DefenseMessageBuilder : MessageBuilder
    {
        private readonly uint _id; // BroadcastTextId

        private readonly uint _zoneId; // ZoneId

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
    }

    public class go_type
    {
        public uint entry;
        public uint map;
        public Position pos;
        public Quaternion rot;

        public go_type(uint _entry, uint _map, float _x, float _y, float _z, float _o, float _rot0, float _rot1, float _rot2, float _rot3)
        {
            entry = _entry;
            map = _map;
            pos = new Position(_x, _y, _z, _o);
            rot = new Quaternion(_rot0, _rot1, _rot2, _rot3);
        }
    }

    internal class creature_type
    {
        public uint entry;
        public uint map;
        private readonly Position pos;

        public creature_type(uint _entry, uint _map, float _x, float _y, float _z, float _o)
        {
            entry = _entry;
            map = _map;
            pos = new Position(_x, _y, _z, _o);
        }
    }
}