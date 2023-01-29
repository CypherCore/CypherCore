// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps;

namespace Game.BattleFields
{
    public class BfCapturePoint
    {
        // active Players in the area of the objective, 0 - alliance, 1 - horde
        private readonly HashSet<ObjectGuid>[] _activePlayers = new HashSet<ObjectGuid>[SharedConst.PvpTeamsCount];

        // Capture point entry
        private uint _capturePointEntry;

        // Gameobject related to that capture point
        private ObjectGuid _capturePointGUID;

        // Maximum speed of capture
        private float _maxSpeed;

        // Total shift needed to capture the objective
        private float _maxValue;
        private float _minValue;

        // Neutral value on capture bar
        private uint _neutralValuePct;

        // Objective states
        private BattleFieldObjectiveStates _oldState;
        private BattleFieldObjectiveStates _state;

        // The status of the objective
        private float _value;

        public BfCapturePoint(BattleField battlefield)
        {
            Bf = battlefield;
            _capturePointGUID = ObjectGuid.Empty;
            Team = TeamId.Neutral;
            _value = 0;
            _minValue = 0.0f;
            _maxValue = 0.0f;
            _state = BattleFieldObjectiveStates.Neutral;
            _oldState = BattleFieldObjectiveStates.Neutral;
            _capturePointEntry = 0;
            _neutralValuePct = 0;
            _maxSpeed = 0;

            _activePlayers[0] = new HashSet<ObjectGuid>();
            _activePlayers[1] = new HashSet<ObjectGuid>();
        }

        // Battlefield this objective belongs to
        protected BattleField Bf { get; set; }
        protected uint Team { get; set; }

        public virtual bool HandlePlayerEnter(Player player)
        {
            if (!_capturePointGUID.IsEmpty())
            {
                GameObject capturePoint = Bf.GetGameObject(_capturePointGUID);

                if (capturePoint)
                {
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 1);
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)(Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f)));
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate3, _neutralValuePct);
                }
            }

            return _activePlayers[player.GetTeamId()].Add(player.GetGUID());
        }

        public virtual void HandlePlayerLeave(Player player)
        {
            if (!_capturePointGUID.IsEmpty())
            {
                GameObject capturePoint = Bf.GetGameObject(_capturePointGUID);

                if (capturePoint)
                    player.SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 0);
            }

            _activePlayers[player.GetTeamId()].Remove(player.GetGUID());
        }

        public virtual void SendChangePhase()
        {
            if (_capturePointGUID.IsEmpty())
                return;

            GameObject capturePoint = Bf.GetGameObject(_capturePointGUID);

            if (capturePoint)
            {
                // send this too, sometimes the slider disappears, dunno why :(
                SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldState1, 1);
                // send these updates to only the ones in this objective
                SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate2, (uint)Math.Ceiling((_value + _maxValue) / (2 * _maxValue) * 100.0f));
                // send this too, sometimes it resets :S
                SendUpdateWorldState(capturePoint.GetGoInfo().ControlZone.worldstate3, _neutralValuePct);
            }
        }

        public bool SetCapturePointData(GameObject capturePoint)
        {
            Cypher.Assert(capturePoint);

            Log.outError(LogFilter.Battlefield, "Creating capture point {0}", capturePoint.GetEntry());

            _capturePointGUID = capturePoint.GetGUID();
            _capturePointEntry = capturePoint.GetEntry();

            // check info existence
            GameObjectTemplate goinfo = capturePoint.GetGoInfo();

            if (goinfo.type != GameObjectTypes.ControlZone)
            {
                Log.outError(LogFilter.Server, "OutdoorPvP: GO {0} is not capture point!", capturePoint.GetEntry());

                return false;
            }

            // get the needed values from goinfo
            _maxValue = goinfo.ControlZone.maxTime;
            _maxSpeed = _maxValue / (goinfo.ControlZone.minTime != 0 ? goinfo.ControlZone.minTime : 60);
            _neutralValuePct = goinfo.ControlZone.neutralPercent;
            _minValue = _maxValue * goinfo.ControlZone.neutralPercent / 100;

            if (Team == TeamId.Alliance)
            {
                _value = _maxValue;
                _state = BattleFieldObjectiveStates.Alliance;
            }
            else
            {
                _value = -_maxValue;
                _state = BattleFieldObjectiveStates.Horde;
            }

            return true;
        }

        public virtual bool Update(uint diff)
        {
            if (_capturePointGUID.IsEmpty())
                return false;

            GameObject capturePoint = Bf.GetGameObject(_capturePointGUID);

            if (capturePoint)
            {
                float radius = capturePoint.GetGoInfo().ControlZone.radius;

                for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                    foreach (var guid in _activePlayers[team])
                    {
                        Player player = Global.ObjAccessor.FindPlayer(guid);

                        if (player)
                            if (!capturePoint.IsWithinDistInMap(player, radius) ||
                                !player.IsOutdoorPvPActive())
                                HandlePlayerLeave(player);
                    }

                List<Unit> players = new();
                var checker = new AnyPlayerInObjectRangeCheck(capturePoint, radius);
                var searcher = new PlayerListSearcher(capturePoint, players, checker);
                Cell.VisitWorldObjects(capturePoint, searcher, radius);

                foreach (Player player in players)
                    if (player.IsOutdoorPvPActive())
                        if (_activePlayers[player.GetTeamId()].Add(player.GetGUID()))
                            HandlePlayerEnter(player);
            }

            // get the difference of numbers
            float fact_diff = ((float)_activePlayers[TeamId.Alliance].Count - _activePlayers[TeamId.Horde].Count) * diff / 1000;

            if (MathFunctions.fuzzyEq(fact_diff, 0.0f))
                return false;

            Team Challenger;
            float maxDiff = _maxSpeed * diff;

            if (fact_diff < 0)
            {
                // horde is in majority, but it's already horde-controlled . no change
                if (_state == BattleFieldObjectiveStates.Horde &&
                    _value <= -_maxValue)
                    return false;

                if (fact_diff < -maxDiff)
                    fact_diff = -maxDiff;

                Challenger = Framework.Constants.Team.Horde;
            }
            else
            {
                // ally is in majority, but it's already ally-controlled . no change
                if (_state == BattleFieldObjectiveStates.Alliance &&
                    _value >= _maxValue)
                    return false;

                if (fact_diff > maxDiff)
                    fact_diff = maxDiff;

                Challenger = Framework.Constants.Team.Alliance;
            }

            float oldValue = _value;
            uint oldTeam = Team;

            _oldState = _state;

            _value += fact_diff;

            if (_value < -_minValue) // red
            {
                if (_value < -_maxValue)
                    _value = -_maxValue;

                _state = BattleFieldObjectiveStates.Horde;
                Team = TeamId.Horde;
            }
            else if (_value > _minValue) // blue
            {
                if (_value > _maxValue)
                    _value = _maxValue;

                _state = BattleFieldObjectiveStates.Alliance;
                Team = TeamId.Alliance;
            }
            else if (oldValue * _value <= 0) // grey, go through mid point
            {
                // if challenger is ally, then n.a challenge
                if (Challenger == Framework.Constants.Team.Alliance)
                    _state = BattleFieldObjectiveStates.NeutralAllianceChallenge;
                // if challenger is horde, then n.h challenge
                else if (Challenger == Framework.Constants.Team.Horde)
                    _state = BattleFieldObjectiveStates.NeutralHordeChallenge;

                Team = TeamId.Neutral;
            }
            else // grey, did not go through mid point
            {
                // old phase and current are on the same side, so one team challenges the other
                if (Challenger == Framework.Constants.Team.Alliance &&
                    (_oldState == BattleFieldObjectiveStates.Horde || _oldState == BattleFieldObjectiveStates.NeutralHordeChallenge))
                    _state = BattleFieldObjectiveStates.HordeAllianceChallenge;
                else if (Challenger == Framework.Constants.Team.Horde &&
                         (_oldState == BattleFieldObjectiveStates.Alliance || _oldState == BattleFieldObjectiveStates.NeutralAllianceChallenge))
                    _state = BattleFieldObjectiveStates.AllianceHordeChallenge;

                Team = TeamId.Neutral;
            }

            if (MathFunctions.fuzzyNe(_value, oldValue))
                SendChangePhase();

            if (_oldState != _state)
            {
                if (oldTeam != Team)
                    ChangeTeam(oldTeam);

                return true;
            }

            return false;
        }

        public virtual void ChangeTeam(uint oldTeam)
        {
        }

        public uint GetCapturePointEntry()
        {
            return _capturePointEntry;
        }

        private GameObject GetCapturePointGo()
        {
            return Bf.GetGameObject(_capturePointGUID);
        }

        private bool DelCapturePoint()
        {
            if (!_capturePointGUID.IsEmpty())
            {
                GameObject capturePoint = Bf.GetGameObject(_capturePointGUID);

                if (capturePoint)
                {
                    capturePoint.SetRespawnTime(0); // not save respawn Time
                    capturePoint.Delete();
                    capturePoint.Dispose();
                }

                _capturePointGUID.Clear();
            }

            return true;
        }

        private void SendUpdateWorldState(uint field, uint value)
        {
            for (byte team = 0; team < SharedConst.PvpTeamsCount; ++team)
                foreach (var guid in _activePlayers[team]) // send to all players present in the area
                {
                    Player player = Global.ObjAccessor.FindPlayer(guid);

                    if (player)
                        player.SendUpdateWorldState(field, value);
                }
        }

        private void SendObjectiveComplete(uint id, ObjectGuid guid)
        {
            uint team;

            switch (_state)
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
            foreach (var _guid in _activePlayers[team])
            {
                Player player = Global.ObjAccessor.FindPlayer(_guid);

                if (player)
                    player.KilledMonsterCredit(id, guid);
            }
        }

        private bool IsInsideObjective(Player player)
        {
            return _activePlayers[player.GetTeamId()].Contains(player.GetGUID());
        }

        private uint GetTeamId()
        {
            return Team;
        }
    }
}