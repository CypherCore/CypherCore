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
using Game.Entities;
using System.Collections.Generic;
using Game.Network.Packets;

namespace Game.DungeonFinding
{
    public class LFGPlayerData
    {
        public LFGPlayerData()
        {
            m_State = LfgState.None;
            m_OldState = LfgState.None;
        }

        public void SetTicket(RideTicket ticket)
        {
            m_Ticket = ticket;
        }

        public void SetState(LfgState state)
        {
            switch (state)
            {
                case LfgState.None:
                case LfgState.FinishedDungeon:
                    m_Roles = 0;
                    m_SelectedDungeons.Clear();
                    goto case LfgState.Dungeon;
                case LfgState.Dungeon:
                    m_OldState = state;
                    break;
            }
            m_State = state;
        }

        public void RestoreState()
        {
            if (m_OldState == LfgState.None)
            {
                m_SelectedDungeons.Clear();
                m_Roles = 0;
            }
            m_State = m_OldState;
        }

        public void SetTeam(Team team)
        {
            m_Team = team;
        }

        public void SetGroup(ObjectGuid group)
        {
            m_Group = group;
        }

        public void SetRoles(LfgRoles roles)
        {
            m_Roles = roles;
        }

        public void SetSelectedDungeons(List<uint> dungeons)
        {
            m_SelectedDungeons = dungeons;
        }

        public RideTicket GetTicket()
        {
            return m_Ticket;
        }

        public LfgState GetState()
        {
            return m_State;
        }

        public LfgState GetOldState()
        {
            return m_OldState;
        }

        public Team GetTeam()
        {
            return m_Team;
        }

        public ObjectGuid GetGroup()
        {
            return m_Group;
        }

        public LfgRoles GetRoles()
        {
            return m_Roles;
        }

        public List<uint> GetSelectedDungeons()
        {
            return m_SelectedDungeons;
        }

        // General
        RideTicket m_Ticket;
        LfgState m_State;
        LfgState m_OldState;
        // Player
        Team m_Team;
        ObjectGuid m_Group;

        // Queue
        LfgRoles m_Roles;
        List<uint> m_SelectedDungeons = new List<uint>();
    }
}
