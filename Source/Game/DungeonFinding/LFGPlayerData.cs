// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Networking.Packets;

namespace Game.DungeonFinding
{
    public class LFGPlayerData
    {
        private ObjectGuid _Group;

        // Achievement-related
        private byte _NumberOfPartyMembersAtJoin;
        private LfgState _OldState;

        // Queue
        private LfgRoles _Roles;
        private List<uint> _SelectedDungeons = new();

        private LfgState _State;

        // Player
        private Team _Team;

        // General
        private RideTicket _Ticket;

        public LFGPlayerData()
        {
            _State = LfgState.None;
            _OldState = LfgState.None;
        }

        public void SetTicket(RideTicket ticket)
        {
            _Ticket = ticket;
        }

        public void SetState(LfgState state)
        {
            switch (state)
            {
                case LfgState.None:
                case LfgState.FinishedDungeon:
                    _Roles = 0;
                    _SelectedDungeons.Clear();
                    goto case LfgState.Dungeon;
                case LfgState.Dungeon:
                    _OldState = state;

                    break;
            }

            _State = state;
        }

        public void RestoreState()
        {
            if (_OldState == LfgState.None)
            {
                _SelectedDungeons.Clear();
                _Roles = 0;
            }

            _State = _OldState;
        }

        public void SetTeam(Team team)
        {
            _Team = team;
        }

        public void SetGroup(ObjectGuid group)
        {
            _Group = group;
        }

        public void SetRoles(LfgRoles roles)
        {
            _Roles = roles;
        }

        public void SetSelectedDungeons(List<uint> dungeons)
        {
            _SelectedDungeons = dungeons;
        }

        public RideTicket GetTicket()
        {
            return _Ticket;
        }

        public LfgState GetState()
        {
            return _State;
        }

        public LfgState GetOldState()
        {
            return _OldState;
        }

        public Team GetTeam()
        {
            return _Team;
        }

        public ObjectGuid GetGroup()
        {
            return _Group;
        }

        public LfgRoles GetRoles()
        {
            return _Roles;
        }

        public List<uint> GetSelectedDungeons()
        {
            return _SelectedDungeons;
        }

        public void SetNumberOfPartyMembersAtJoin(byte count)
        {
            _NumberOfPartyMembersAtJoin = count;
        }

        public byte GetNumberOfPartyMembersAtJoin()
        {
            return _NumberOfPartyMembersAtJoin;
        }
    }
}