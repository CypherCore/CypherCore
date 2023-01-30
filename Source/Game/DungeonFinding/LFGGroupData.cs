// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.DungeonFinding
{
    public class LFGGroupData
    {
        private readonly List<ObjectGuid> _players = new();

        // Dungeon
        private uint _dungeon;

        // Vote Kick
        private byte _kicksLeft;
        private ObjectGuid _leader;
        private LfgState _oldState;

        // General
        private LfgState _state;
        private bool _voteKickActive;

        public LFGGroupData()
        {
            _state = LfgState.None;
            _oldState = LfgState.None;
            _kicksLeft = SharedConst.LFGMaxKicks;
        }

        public bool IsLfgGroup()
        {
            return _oldState != LfgState.None;
        }

        public void SetState(LfgState state)
        {
            switch (state)
            {
                case LfgState.None:
                    _dungeon = 0;
                    _kicksLeft = SharedConst.LFGMaxKicks;
                    _oldState = state;

                    break;
                case LfgState.FinishedDungeon:
                case LfgState.Dungeon:
                    _oldState = state;

                    break;
            }

            _state = state;
        }

        public void RestoreState()
        {
            _state = _oldState;
        }

        public void AddPlayer(ObjectGuid guid)
        {
            _players.Add(guid);
        }

        public byte RemovePlayer(ObjectGuid guid)
        {
            _players.Remove(guid);

            return (byte)_players.Count;
        }

        public void RemoveAllPlayers()
        {
            _players.Clear();
        }

        public void SetLeader(ObjectGuid guid)
        {
            _leader = guid;
        }

        public void SetDungeon(uint dungeon)
        {
            _dungeon = dungeon;
        }

        public void DecreaseKicksLeft()
        {
            if (_kicksLeft != 0)
                --_kicksLeft;
        }

        public LfgState GetState()
        {
            return _state;
        }

        public LfgState GetOldState()
        {
            return _oldState;
        }

        public List<ObjectGuid> GetPlayers()
        {
            return _players;
        }

        public byte GetPlayerCount()
        {
            return (byte)_players.Count;
        }

        public ObjectGuid GetLeader()
        {
            return _leader;
        }

        public uint GetDungeon(bool asId = true)
        {
            if (asId)
                return (_dungeon & 0x00FFFFFF);
            else
                return _dungeon;
        }

        public byte GetKicksLeft()
        {
            return _kicksLeft;
        }

        public void SetVoteKick(bool active)
        {
            _voteKickActive = active;
        }

        public bool IsVoteKickActive()
        {
            return _voteKickActive;
        }
    }
}