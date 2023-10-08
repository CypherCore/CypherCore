// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
            if (group == null)
                return null;

            List<Player> nearMembers = new();

            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player Target = refe.GetSource();

                // IsHostileTo check duel and controlled by enemy
                if (Target != null && Target != this && IsWithinDistInMap(Target, radius) &&
                    !Target.HasInvisibilityAura() && !IsHostileTo(Target))
                    nearMembers.Add(Target);
            }

            if (nearMembers.Empty())
                return null;

            int randTarget = RandomHelper.IRand(0, nearMembers.Count - 1);
            return nearMembers[randTarget];
        }

        public PartyResult CanUninviteFromGroup(ObjectGuid guidMember, byte? partyIndex)
        {
            Group grp = GetGroup(partyIndex);
            if (grp == null)
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
                if (player != null && !player.m_lootRolls.Empty())
                    return PartyResult.PartyLfgBootLootRolls;

                // @todo Should also be sent when anyone has recently left combat, with an aprox ~5 seconds timer.
                for (GroupReference refe = grp.GetFirstMember(); refe != null; refe = refe.Next())
                    if (refe.GetSource() != null && refe.GetSource().IsInMap(this) && refe.GetSource().IsInCombat())
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

        public bool IsUsingLfg()
        {
            return Global.LFGMgr.GetState(GetGUID()) != LfgState.None;
        }

        bool InRandomLfgDungeon()
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
            //we must move references from m_group to m_originalGroup
            SetOriginalGroup(GetGroup(), GetSubGroup());

            m_group.Unlink();
            m_group.Link(group, this);
            m_group.SetSubGroup(subgroup);
        }

        public void RemoveFromBattlegroundOrBattlefieldRaid()
        {
            //remove existing reference
            m_group.Unlink();
            Group group = GetOriginalGroup();
            if (group != null)
            {
                m_group.Link(group, this);
                m_group.SetSubGroup(GetOriginalSubGroup());
            }
            SetOriginalGroup(null);
        }

        public void SetOriginalGroup(Group group, byte subgroup = 0)
        {
            if (group == null)
                m_originalGroup.Unlink();
            else
            {
                m_originalGroup.Link(group, this);
                m_originalGroup.SetSubGroup(subgroup);
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

        public Group GetGroup(byte? partyIndex)
        {
            Group group = GetGroup();
            if (!partyIndex.HasValue)
                return group;

            GroupCategory category = (GroupCategory)partyIndex;
            if (group != null && group.GetGroupCategory() == category)
                return group;

            Group originalGroup = GetOriginalGroup();
            if (originalGroup != null && originalGroup.GetGroupCategory() == category)
                return originalGroup;

            return null;
        }
        
        public void SetGroup(Group group, byte subgroup = 0)
        {
            if (group == null)
                m_group.Unlink();
            else
            {
                m_group.Link(group, this);
                m_group.SetSubGroup(subgroup);
            }

            UpdateObjectVisibility(false);
        }

        public void SetPartyType(GroupCategory category, GroupType type)
        {
            Cypher.Assert(category < GroupCategory.Max);
            SetUpdateFieldValue(ref m_values.ModifyValue(m_playerData).ModifyValue(m_playerData.PartyType, (int)category), (byte)type);
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
            return m_groupUpdateSequences[(int)category].UpdateSequenceNumber++;
        }

        public bool IsAtGroupRewardDistance(WorldObject pRewardSource)
        {
            if (pRewardSource == null || !IsInMap(pRewardSource))
                return false;

            WorldObject player = GetCorpse();
            if (player == null || IsAlive())
                player = this;

            if (player.GetMap().IsDungeon())
                return true;

            return pRewardSource.GetDistance(player) <= WorldConfig.GetFloatValue(WorldCfg.GroupXpDistance);
        }

        public Group GetGroupInvite() { return m_groupInvite; }

        public void SetGroupInvite(Group group) { m_groupInvite = group; }

        public Group GetGroup() { return m_group.GetTarget(); }

        public GroupReference GetGroupRef() { return m_group; }

        public byte GetSubGroup() { return m_group.GetSubGroup(); }

        public GroupUpdateFlags GetGroupUpdateFlag() { return m_groupUpdateMask; }

        public void SetGroupUpdateFlag(GroupUpdateFlags flag) { m_groupUpdateMask |= flag; }

        public void RemoveGroupUpdateFlag(GroupUpdateFlags flag) { m_groupUpdateMask &= ~flag; }

        public Group GetOriginalGroup() { return m_originalGroup.GetTarget(); }
        public GroupReference GetOriginalGroupRef() { return m_originalGroup; }
        public byte GetOriginalSubGroup() { return m_originalGroup.GetSubGroup(); }

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
            return p == this || (GetGroup() != null &&
                GetGroup() == p.GetGroup() && GetGroup().SameSubGroup(this, p));
        }

        public bool IsInSameRaidWith(Player p)
        {
            return p == this || (GetGroup() != null && GetGroup() == p.GetGroup());
        }

        public void UninviteFromGroup()
        {
            Group group = GetGroupInvite();
            if (group == null)
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

        public void RemoveFromGroup(RemoveMethod method = RemoveMethod.Default) { RemoveFromGroup(GetGroup(), GetGUID(), method); }
        public static void RemoveFromGroup(Group group, ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
        {
            if (group == null)
                return;

            group.RemoveMember(guid, method, kicker, reason);
        }

        void SendUpdateToOutOfRangeGroupMembers()
        {
            if (m_groupUpdateMask == GroupUpdateFlags.None)
                return;
            Group group = GetGroup();
            if (group != null)
                group.UpdatePlayerOutOfRange(this);

            m_groupUpdateMask = GroupUpdateFlags.None;

            Pet pet = GetPet();
            if (pet != null)
                pet.ResetGroupUpdateFlag();
        }
    }
}
