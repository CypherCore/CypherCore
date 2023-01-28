// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Groups;
using Game.Maps;

namespace Game.Entities
{
	public partial class Player
	{
		private Player GetNextRandomRaidMember(float radius)
		{
			Group group = GetGroup();

			if (!group)
				return null;

			List<Player> nearMembers = new();

			for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
			{
				Player Target = refe.GetSource();

				// IsHostileTo check Duel and controlled by enemy
				if (Target &&
				    Target != this &&
				    IsWithinDistInMap(Target, radius) &&
				    !Target.HasInvisibilityAura() &&
				    !IsHostileTo(Target))
					nearMembers.Add(Target);
			}

			if (nearMembers.Empty())
				return null;

			int randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);

			return nearMembers[randTarget];
		}

		public PartyResult CanUninviteFromGroup(ObjectGuid guidMember = default)
		{
			Group grp = GetGroup();

			if (!grp)
				return PartyResult.NotInGroup;

			if (grp.IsLFGGroup())
			{
				ObjectGuid gguid = grp.GetGUID();

				if (Global.LFGMgr.GetKicksLeft(gguid) == 0)
					return PartyResult.PartyLfgBootLimit;

				LfgState state = Global.LFGMgr.GetState(gguid);

				if (Global.LFGMgr.IsVoteKickActive(gguid))
					return PartyResult.PartyLfgBootInProgress;

				if (grp.GetMembersCount() <= SharedConst.LFGKickVotesNeeded)
					return PartyResult.PartyLfgBootTooFewPlayers;

				if (state == LfgState.FinishedDungeon)
					return PartyResult.PartyLfgBootDungeonComplete;

				Player player = Global.ObjAccessor.FindConnectedPlayer(guidMember);

				if (!player._lootRolls.Empty())
					return PartyResult.PartyLfgBootLootRolls;

				// @todo Should also be sent when anyone has recently left combat, with an aprox ~5 seconds timer.
				for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
					if (refe.GetSource() &&
					    refe.GetSource().IsInMap(this) &&
					    refe.GetSource().IsInCombat())
						return PartyResult.PartyLfgBootInCombat;

				/* Missing support for these types
				    return ERR_PARTY_LFG_BOOT_COOLDOWN_S;
				    return ERR_PARTY_LFG_BOOT_NOT_ELIGIBLE_S;
				*/
			}
			else
			{
				if (!grp.IsLeader(GetGUID()) &&
				    !grp.IsAssistant(GetGUID()))
					return PartyResult.NotLeader;

				if (InBattleground())
					return PartyResult.InviteRestricted;

				if (grp.IsLeader(guidMember))
					return PartyResult.NotLeader;
			}

			return PartyResult.Ok;
		}

		public bool IsUsingLfg()
		{
			return Global.LFGMgr.GetState(GetGUID()) != LfgState.None;
		}

		private bool InRandomLfgDungeon()
		{
			if (Global.LFGMgr.SelectedRandomLfgDungeon(GetGUID()))
			{
				Map map = GetMap();

				return Global.LFGMgr.InLfgDungeonMap(GetGUID(), map.GetId(), map.GetDifficultyID());
			}

			return false;
		}

		public void SetBattlegroundOrBattlefieldRaid(Group group, byte subgroup)
		{
			//we must move references from _group to _originalGroup
			SetOriginalGroup(GetGroup(), GetSubGroup());

			_group.Unlink();
			_group.Link(group, this);
			_group.SetSubGroup(subgroup);
		}

		public void RemoveFromBattlegroundOrBattlefieldRaid()
		{
			//remove existing reference
			_group.Unlink();
			Group group = GetOriginalGroup();

			if (group)
			{
				_group.Link(group, this);
				_group.SetSubGroup(GetOriginalSubGroup());
			}

			SetOriginalGroup(null);
		}

		public void SetOriginalGroup(Group group, byte subgroup = 0)
		{
			if (!group)
			{
				_originalGroup.Unlink();
			}
			else
			{
				_originalGroup.Link(group, this);
				_originalGroup.SetSubGroup(subgroup);
			}
		}

		public bool IsInGroup(ObjectGuid groupGuid)
		{
			Group group = GetGroup();

			if (group != null)
				if (group.GetGUID() == groupGuid)
					return true;

			Group originalGroup = GetOriginalGroup();

			if (originalGroup != null)
				if (originalGroup.GetGUID() == groupGuid)
					return true;

			return false;
		}

		public void SetGroup(Group group, byte subgroup = 0)
		{
			if (!group)
			{
				_group.Unlink();
			}
			else
			{
				_group.Link(group, this);
				_group.SetSubGroup(subgroup);
			}

			UpdateObjectVisibility(false);
		}

		public void SetPartyType(GroupCategory category, GroupType type)
		{
			Cypher.Assert(category < GroupCategory.Max);
			byte value = PlayerData.PartyType;
			value &= (byte)~((byte)0xFF << ((byte)category * 4));
			value |= (byte)((byte)type << ((byte)category * 4));
			SetUpdateFieldValue(_values.ModifyValue(PlayerData).ModifyValue(PlayerData.PartyType), value);
		}

		public void ResetGroupUpdateSequenceIfNeeded(Group group)
		{
			GroupCategory category = group.GetGroupCategory();

			// Rejoining the last group should not reset the sequence
			if (_groupUpdateSequences[(int)category].GroupGuid != group.GetGUID())
			{
				GroupUpdateCounter groupUpdate;
				groupUpdate.GroupGuid                = group.GetGUID();
				groupUpdate.UpdateSequenceNumber     = 1;
				_groupUpdateSequences[(int)category] = groupUpdate;
			}
		}

		public int NextGroupUpdateSequenceNumber(GroupCategory category)
		{
			return _groupUpdateSequences[(int)category].UpdateSequenceNumber++;
		}

		public bool IsAtGroupRewardDistance(WorldObject pRewardSource)
		{
			if (!pRewardSource ||
			    !IsInMap(pRewardSource))
				return false;

			WorldObject player = GetCorpse();

			if (!player ||
			    IsAlive())
				player = this;

			if (player.GetMap().IsDungeon())
				return true;

			return pRewardSource.GetDistance(player) <= WorldConfig.GetFloatValue(WorldCfg.GroupXpDistance);
		}

		public Group GetGroupInvite()
		{
			return _groupInvite;
		}

		public void SetGroupInvite(Group group)
		{
			_groupInvite = group;
		}

		public Group GetGroup()
		{
			return _group.GetTarget();
		}

		public GroupReference GetGroupRef()
		{
			return _group;
		}

		public byte GetSubGroup()
		{
			return _group.GetSubGroup();
		}

		public GroupUpdateFlags GetGroupUpdateFlag()
		{
			return _groupUpdateMask;
		}

		public void SetGroupUpdateFlag(GroupUpdateFlags flag)
		{
			_groupUpdateMask |= flag;
		}

		public void RemoveGroupUpdateFlag(GroupUpdateFlags flag)
		{
			_groupUpdateMask &= ~flag;
		}

		public Group GetOriginalGroup()
		{
			return _originalGroup.GetTarget();
		}

		public GroupReference GetOriginalGroupRef()
		{
			return _originalGroup;
		}

		public byte GetOriginalSubGroup()
		{
			return _originalGroup.GetSubGroup();
		}

		public void SetPassOnGroupLoot(bool bPassOnGroupLoot)
		{
			_bPassOnGroupLoot = bPassOnGroupLoot;
		}

		public bool GetPassOnGroupLoot()
		{
			return _bPassOnGroupLoot;
		}

		public bool IsGroupVisibleFor(Player p)
		{
			switch (WorldConfig.GetIntValue(WorldCfg.GroupVisibility))
			{
				default:
					return IsInSameGroupWith(p);
				case 1:
					return IsInSameRaidWith(p);
				case 2:
					return GetTeam() == p.GetTeam();
				case 3:
					return false;
			}
		}

		public bool IsInSameGroupWith(Player p)
		{
			return p == this ||
			       (GetGroup() &&
			        GetGroup() == p.GetGroup() &&
			        GetGroup().SameSubGroup(this, p));
		}

		public bool IsInSameRaidWith(Player p)
		{
			return p == this || (GetGroup() != null && GetGroup() == p.GetGroup());
		}

		public void UninviteFromGroup()
		{
			Group group = GetGroupInvite();

			if (!group)
				return;

			group.RemoveInvite(this);

			if (group.IsCreated())
			{
				if (group.GetMembersCount() <= 1) // group has just 1 member => disband
					group.Disband(true);
			}
			else
			{
				if (group.GetInviteeCount() <= 1)
					group.RemoveAllInvites();
			}
		}

		public void RemoveFromGroup(RemoveMethod method = RemoveMethod.Default)
		{
			RemoveFromGroup(GetGroup(), GetGUID(), method);
		}

		public static void RemoveFromGroup(Group group, ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
		{
			if (!group)
				return;

			group.RemoveMember(guid, method, kicker, reason);
		}

		private void SendUpdateToOutOfRangeGroupMembers()
		{
			if (_groupUpdateMask == GroupUpdateFlags.None)
				return;

			Group group = GetGroup();

			if (group)
				group.UpdatePlayerOutOfRange(this);

			_groupUpdateMask = GroupUpdateFlags.None;

			Pet pet = GetPet();

			if (pet)
				pet.ResetGroupUpdateFlag();
		}
	}
}