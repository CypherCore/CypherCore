// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.DungeonFinding
{
	public class LFGGroupData
	{
		// Dungeon
		private uint _Dungeon;

		// Vote Kick
		private byte _KicksLeft;
		private ObjectGuid _Leader;
		private LfgState _OldState;
		private List<ObjectGuid> _Players = new();

		// General
		private LfgState _State;
		private bool _VoteKickActive;

		public LFGGroupData()
		{
			_State     = LfgState.None;
			_OldState  = LfgState.None;
			_KicksLeft = SharedConst.LFGMaxKicks;
		}

		public bool IsLfgGroup()
		{
			return _OldState != LfgState.None;
		}

		public void SetState(LfgState state)
		{
			switch (state)
			{
				case LfgState.None:
					_Dungeon   = 0;
					_KicksLeft = SharedConst.LFGMaxKicks;
					_OldState  = state;

					break;
				case LfgState.FinishedDungeon:
				case LfgState.Dungeon:
					_OldState = state;

					break;
			}

			_State = state;
		}

		public void RestoreState()
		{
			_State = _OldState;
		}

		public void AddPlayer(ObjectGuid guid)
		{
			_Players.Add(guid);
		}

		public byte RemovePlayer(ObjectGuid guid)
		{
			_Players.Remove(guid);

			return (byte)_Players.Count;
		}

		public void RemoveAllPlayers()
		{
			_Players.Clear();
		}

		public void SetLeader(ObjectGuid guid)
		{
			_Leader = guid;
		}

		public void SetDungeon(uint dungeon)
		{
			_Dungeon = dungeon;
		}

		public void DecreaseKicksLeft()
		{
			if (_KicksLeft != 0)
				--_KicksLeft;
		}

		public LfgState GetState()
		{
			return _State;
		}

		public LfgState GetOldState()
		{
			return _OldState;
		}

		public List<ObjectGuid> GetPlayers()
		{
			return _Players;
		}

		public byte GetPlayerCount()
		{
			return (byte)_Players.Count;
		}

		public ObjectGuid GetLeader()
		{
			return _Leader;
		}

		public uint GetDungeon(bool asId = true)
		{
			if (asId)
				return (_Dungeon & 0x00FFFFFF);
			else
				return _Dungeon;
		}

		public byte GetKicksLeft()
		{
			return _KicksLeft;
		}

		public void SetVoteKick(bool active)
		{
			_VoteKickActive = active;
		}

		public bool IsVoteKickActive()
		{
			return _VoteKickActive;
		}
	}
}