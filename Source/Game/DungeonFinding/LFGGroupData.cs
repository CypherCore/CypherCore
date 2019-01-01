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

namespace Game.DungeonFinding
{
    public class LFGGroupData
    {
        public LFGGroupData()
        {
            m_State = LfgState.None;
            m_OldState = LfgState.None;
            m_KicksLeft = SharedConst.LFGMaxKicks;
        }

        public bool IsLfgGroup()
        {
            return m_OldState != LfgState.None;
        }

        public void SetState(LfgState state)
        {
            switch (state)
            {
                case LfgState.None:
                    m_Dungeon = 0;
                    m_KicksLeft = SharedConst.LFGMaxKicks;
                    m_OldState = state;
                    break;
                case LfgState.FinishedDungeon:
                case LfgState.Dungeon:
                    m_OldState = state;
                    break;
            }
            m_State = state;
        }

        public void RestoreState()
        {
            m_State = m_OldState;
        }

        public void AddPlayer(ObjectGuid guid)
        {
            m_Players.Add(guid);
        }

        public byte RemovePlayer(ObjectGuid guid)
        {
            m_Players.Remove(guid);
            return (byte)m_Players.Count;
        }

        public void RemoveAllPlayers()
        {
            m_Players.Clear();
        }

        public void SetLeader(ObjectGuid guid)
        {
            m_Leader = guid;
        }

        public void SetDungeon(uint dungeon)
        {
            m_Dungeon = dungeon;
        }

        public void DecreaseKicksLeft()
        {
            if (m_KicksLeft != 0)
                --m_KicksLeft;
        }

        public LfgState GetState()
        {
            return m_State;
        }

        public LfgState GetOldState()
        {
            return m_OldState;
        }

        public List<ObjectGuid> GetPlayers()
        {
            return m_Players;
        }

        public byte GetPlayerCount()
        {
            return (byte)m_Players.Count;
        }

        public ObjectGuid GetLeader()
        {
            return m_Leader;
        }

        public uint GetDungeon(bool asId = true)
        {
            if (asId)
                return (m_Dungeon & 0x00FFFFFF);
            else
                return m_Dungeon;
        }

        public byte GetKicksLeft()
        {
            return m_KicksLeft;
        }

        public void SetVoteKick(bool active)
        {
            m_VoteKickActive = active;
        }

        public bool IsVoteKickActive()
        {
            return m_VoteKickActive;
        }

        // General
        LfgState m_State;
        LfgState m_OldState;
        ObjectGuid m_Leader;
        List<ObjectGuid> m_Players = new List<ObjectGuid>();
        // Dungeon
        uint m_Dungeon;
        // Vote Kick
        byte m_KicksLeft;
        bool m_VoteKickActive;
    }
}
