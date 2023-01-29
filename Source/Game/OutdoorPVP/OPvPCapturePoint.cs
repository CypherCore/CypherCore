// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Maps;
using Game.Maps.Checks;
using Game.Maps.Notifiers;

namespace Game.PvP
{
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
}