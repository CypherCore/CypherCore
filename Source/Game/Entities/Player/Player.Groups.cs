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
using Game.Groups;
using Game.Maps;
using System.Collections.Generic;

namespace Game.Entities
{
    public partial class Player
    {
        Player GetNextRandomRaidMember(float radius)
        {
            Group group = GetGroup();
            if (!group)
                return null;

            List<Player> nearMembers = new List<Player>();

            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
            {
                Player Target = refe.GetSource();

                // IsHostileTo check duel and controlled by enemy
                if (Target && Target != this && IsWithinDistInMap(Target, radius) &&
                    !Target.HasInvisibilityAura() && !IsHostileTo(Target))
                    nearMembers.Add(Target);
            }

            if (nearMembers.Empty())
                return null;

            int randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);
            return nearMembers[randTarget];
        }

        public PartyResult CanUninviteFromGroup(ObjectGuid guidMember = default(ObjectGuid))
        {
            Group grp = GetGroup();
            if (!grp)
                return PartyResult.NotInGroup;

            if (grp.isLFGGroup())
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

                if (grp.isRollLootActive())
                    return PartyResult.PartyLfgBootLootRolls;

                // @todo Should also be sent when anyone has recently left combat, with an aprox ~5 seconds timer.
                for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.next())
                    if (refe.GetSource() && refe.GetSource().IsInCombat())
                        return PartyResult.PartyLfgBootInCombat;

                /* Missing support for these types
                    return ERR_PARTY_LFG_BOOT_COOLDOWN_S;
                    return ERR_PARTY_LFG_BOOT_NOT_ELIGIBLE_S;
                */
            }
            else
            {
                if (!grp.IsLeader(GetGUID()) && !grp.IsAssistant(GetGUID()))
                    return PartyResult.NotLeader;

                if (InBattleground())
                    return PartyResult.InviteRestricted;

                if (grp.IsLeader(guidMember))
                    return PartyResult.NotLeader;
            }

            return PartyResult.Ok;
        }

        public bool isUsingLfg()
        {
            return Global.LFGMgr.GetState(GetGUID()) != LfgState.None;
        }

        bool inRandomLfgDungeon()
        {
            if (Global.LFGMgr.selectedRandomLfgDungeon(GetGUID()))
            {
                Map map = GetMap();
                return Global.LFGMgr.inLfgDungeonMap(GetGUID(), map.GetId(), map.GetDifficultyID());
            }

            return false;
        }

        public void SetBattlegroundOrBattlefieldRaid(Group group, byte subgroup)
        {
            //we must move references from m_group to m_originalGroup
            SetOriginalGroup(GetGroup(), GetSubGroup());

            m_group.unlink();
            m_group.link(group, this);
            m_group.setSubGroup(subgroup);
        }

        public void RemoveFromBattlegroundOrBattlefieldRaid()
        {
            //remove existing reference
            m_group.unlink();
            Group group = GetOriginalGroup();
            if (group)
            {
                m_group.link(group, this);
                m_group.setSubGroup(GetOriginalSubGroup());
            }
            SetOriginalGroup(null);
        }

        public void SetOriginalGroup(Group group, byte subgroup = 0)
        {
            if (!group)
                m_originalGroup.unlink();
            else
            {
                m_originalGroup.link(group, this);
                m_originalGroup.setSubGroup(subgroup);
            }
        }

        public void SetGroup(Group group, byte subgroup = 0)
        {
            if (!group)
                m_group.unlink();
            else
            {
                m_group.link(group, this);
                m_group.setSubGroup(subgroup);
            }

            UpdateObjectVisibility(false);
        }

        public void SetPartyType(GroupCategory category, GroupType type)
        {
            Cypher.Assert(category < GroupCategory.Max);
            byte value = GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetPartyType);
            value &= (byte)~((byte)0xFF << ((byte)category * 4));
            value |= (byte)((byte)type << ((byte)category * 4));
            SetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetPartyType, value);
        }

        public void ResetGroupUpdateSequenceIfNeeded(Group group)
        {
            GroupCategory category = group.GetGroupCategory();
            // Rejoining the last group should not reset the sequence
            if (m_groupUpdateSequences[(int)category].GroupGuid != group.GetGUID())
            {
                GroupUpdateCounter groupUpdate;
                groupUpdate.GroupGuid = group.GetGUID();
                groupUpdate.UpdateSequenceNumber = 1;
                m_groupUpdateSequences[(int) category] = groupUpdate;
            }
        }

        public int NextGroupUpdateSequenceNumber(GroupCategory category)
        {
            var groupUpdate = m_groupUpdateSequences[(int)category];
            return groupUpdate.UpdateSequenceNumber++;
        }

        public bool IsAtGroupRewardDistance(WorldObject pRewardSource)
        {
            if (!pRewardSource)
                return false;
            WorldObject player = GetCorpse();
            if (!player || IsAlive())
                player = this;

            if (player.GetMapId() != pRewardSource.GetMapId() || player.GetInstanceId() != pRewardSource.GetInstanceId())
                return false;

            if (player.GetMap().IsDungeon())
                return true;

            return pRewardSource.GetDistance(player) <= WorldConfig.GetFloatValue(WorldCfg.GroupXpDistance);
        }

        public Group GetGroupInvite() { return m_groupInvite; }
        public void SetGroupInvite(Group group) { m_groupInvite = group; }
        public Group GetGroup() { return m_group.getTarget(); }
        public GroupReference GetGroupRef() { return m_group; }
        public byte GetSubGroup() { return m_group.getSubGroup(); }
        public GroupUpdateFlags GetGroupUpdateFlag() { return m_groupUpdateMask; }
        public void SetGroupUpdateFlag(GroupUpdateFlags flag) { m_groupUpdateMask |= flag; }
        public void RemoveGroupUpdateFlag(GroupUpdateFlags flag) { m_groupUpdateMask &= ~flag; }

        public Group GetOriginalGroup() { return m_originalGroup.getTarget(); }
        public GroupReference GetOriginalGroupRef() { return m_originalGroup; }
        public byte GetOriginalSubGroup() { return m_originalGroup.getSubGroup(); }

        public void SetPassOnGroupLoot(bool bPassOnGroupLoot) { m_bPassOnGroupLoot = bPassOnGroupLoot; }
        public bool GetPassOnGroupLoot() { return m_bPassOnGroupLoot; }

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
            return p == this || (GetGroup() &&
                GetGroup() == p.GetGroup() && GetGroup().SameSubGroup(this, p));
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

            if (group.GetMembersCount() <= 1)                       // group has just 1 member => disband
            {
                if (group.IsCreated())
                    group.Disband(true);
                else
                    group.RemoveAllInvites();
            }
        }

        public void RemoveFromGroup(RemoveMethod method = RemoveMethod.Default) { RemoveFromGroup(GetGroup(), GetGUID(), method); }
        public static void RemoveFromGroup(Group group, ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default(ObjectGuid), string reason = null)
        {
            if (!group)
                return;

            group.RemoveMember(guid, method, kicker, reason);
        }

        void SendUpdateToOutOfRangeGroupMembers()
        {
            if (m_groupUpdateMask == GroupUpdateFlags.None)
                return;
            Group group = GetGroup();
            if (group)
                group.UpdatePlayerOutOfRange(this);

            m_groupUpdateMask = GroupUpdateFlags.None;

            Pet pet = GetPet();
            if (pet)
                pet.ResetGroupUpdateFlag();
        }
    }
}
