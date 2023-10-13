// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.BattleFields;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Loots;
using Game.Maps;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Groups
{
    public class Group
    {
        public Group()
        {
            m_leaderName = "";
            m_groupFlags = GroupFlags.None;
            m_dungeonDifficulty = Difficulty.Normal;
            m_raidDifficulty = Difficulty.NormalRaid;
            m_legacyRaidDifficulty = Difficulty.Raid10N;
            m_lootMethod = LootMethod.PersonalLoot;
            m_lootThreshold = ItemQuality.Uncommon;
        }

        public void Update(uint diff)
        {
            if (_isLeaderOffline)
            {
                _leaderOfflineTimer.Update(diff);
                if (_leaderOfflineTimer.Passed())
                {
                    SelectNewPartyOrRaidLeader();
                    _isLeaderOffline = false;
                }
            }

            UpdateReadyCheck(diff);
        }

        void SelectNewPartyOrRaidLeader()
        {
            Player newLeader = null;

            // Attempt to give leadership to main assistant first
            if (IsRaidGroup())
            {
                foreach (var memberSlot in m_memberSlots)
                {
                    if (memberSlot.flags.HasFlag(GroupMemberFlags.Assistant))
                    {
                        Player player = Global.ObjAccessor.FindPlayer(memberSlot.guid);
                        if (player != null)
                        {
                            newLeader = player;
                            break;
                        }
                    }
                }
            }

            // If there aren't assistants in raid, or if the group is not a raid, pick the first available member
            if (newLeader == null)
            {
                foreach (var memberSlot in m_memberSlots)
                {
                    Player player = Global.ObjAccessor.FindPlayer(memberSlot.guid);
                    if (player != null)
                    {
                        newLeader = player;
                        break;
                    }
                }
            }

            if (newLeader != null)
            {
                ChangeLeader(newLeader.GetGUID());
                SendUpdate();
            }
        }

        public bool Create(Player leader)
        {
            ObjectGuid leaderGuid = leader.GetGUID();

            m_guid = ObjectGuid.Create(HighGuid.Party, Global.GroupMgr.GenerateGroupId());
            m_leaderGuid = leaderGuid;
            m_leaderFactionGroup = Player.GetFactionGroupForRace(leader.GetRace());
            m_leaderName = leader.GetName();
            leader.SetPlayerFlag(PlayerFlags.GroupLeader);

            if (IsBGGroup() || IsBFGroup())
            {
                m_groupFlags = GroupFlags.MaskBgRaid;
                m_groupCategory = GroupCategory.Instance;
            }

            if (m_groupFlags.HasAnyFlag(GroupFlags.Raid))
                _initRaidSubGroupsCounter();

            m_lootThreshold = ItemQuality.Uncommon;
            m_looterGuid = leaderGuid;

            m_dungeonDifficulty = Difficulty.Normal;
            m_raidDifficulty = Difficulty.NormalRaid;
            m_legacyRaidDifficulty = Difficulty.Raid10N;

            if (!IsBGGroup() && !IsBFGroup())
            {
                m_dungeonDifficulty = leader.GetDungeonDifficultyID();
                m_raidDifficulty = leader.GetRaidDifficultyID();
                m_legacyRaidDifficulty = leader.GetLegacyRaidDifficultyID();

                m_dbStoreId = Global.GroupMgr.GenerateNewGroupDbStoreId();

                Global.GroupMgr.RegisterGroupDbStoreId(m_dbStoreId, this);

                // Store group in database
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_GROUP);

                byte index = 0;

                stmt.AddValue(index++, m_dbStoreId);
                stmt.AddValue(index++, m_leaderGuid.GetCounter());
                stmt.AddValue(index++, (byte)m_lootMethod);
                stmt.AddValue(index++, m_looterGuid.GetCounter());
                stmt.AddValue(index++, (byte)m_lootThreshold);
                stmt.AddValue(index++, m_targetIcons[0].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[1].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[2].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[3].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[4].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[5].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[6].GetRawValue());
                stmt.AddValue(index++, m_targetIcons[7].GetRawValue());
                stmt.AddValue(index++, (ushort)m_groupFlags);
                stmt.AddValue(index++, (byte)m_dungeonDifficulty);
                stmt.AddValue(index++, (byte)m_raidDifficulty);
                stmt.AddValue(index++, (byte)m_legacyRaidDifficulty);
                stmt.AddValue(index++, m_masterLooterGuid.GetCounter());

                DB.Characters.Execute(stmt);

                InstanceMap leaderInstance = leader.GetMap().ToInstanceMap();
                if (leaderInstance != null)
                    leaderInstance.TrySetOwningGroup(this);

                Cypher.Assert(AddMember(leader)); // If the leader can't be added to a new group because it appears full, something is clearly wrong.
            }
            else if (!AddMember(leader))
                return false;

            return true;
        }

        public void LoadGroupFromDB(SQLFields field)
        {
            m_dbStoreId = field.Read<uint>(17);
            m_guid = ObjectGuid.Create(HighGuid.Party, Global.GroupMgr.GenerateGroupId());
            m_leaderGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(0));

            // group leader not exist
            var leader = Global.CharacterCacheStorage.GetCharacterCacheByGuid(m_leaderGuid);
            if (leader == null)
                return;

            m_leaderFactionGroup = Player.GetFactionGroupForRace(leader.RaceId);
            m_leaderName = leader.Name;
            m_lootMethod = (LootMethod)field.Read<byte>(1);
            m_looterGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(2));
            m_lootThreshold = (ItemQuality)field.Read<byte>(3);

            for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                m_targetIcons[i].SetRawValue(field.Read<byte[]>(4 + i));

            m_groupFlags = (GroupFlags)field.Read<ushort>(12);
            if (m_groupFlags.HasAnyFlag(GroupFlags.Raid))
                _initRaidSubGroupsCounter();

            m_dungeonDifficulty = Player.CheckLoadedDungeonDifficultyID((Difficulty)field.Read<byte>(13));
            m_raidDifficulty = Player.CheckLoadedRaidDifficultyID((Difficulty)field.Read<byte>(14));
            m_legacyRaidDifficulty = Player.CheckLoadedLegacyRaidDifficultyID((Difficulty)field.Read<byte>(15));

            m_masterLooterGuid = ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(16));

            if (m_groupFlags.HasAnyFlag(GroupFlags.Lfg))
                Global.LFGMgr._LoadFromDB(field, GetGUID());
        }

        public void LoadMemberFromDB(ulong guidLow, byte memberFlags, byte subgroup, LfgRoles roles)
        {
            MemberSlot member = new();
            member.guid = ObjectGuid.Create(HighGuid.Player, guidLow);

            // skip non-existed member
            var character = Global.CharacterCacheStorage.GetCharacterCacheByGuid(member.guid);
            if (character == null)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER);
                stmt.AddValue(0, guidLow);
                DB.Characters.Execute(stmt);
                return;
            }

            if (m_groupFlags.HasFlag(GroupFlags.EveryoneAssistant))
                memberFlags |= (byte)GroupMemberFlags.Assistant;

            member.name = character.Name;
            member.race = character.RaceId;
            member._class = (byte)character.ClassId;
            member.group = subgroup;
            member.flags = (GroupMemberFlags)memberFlags;
            member.roles = roles;
            member.readyChecked = false;

            m_memberSlots.Add(member);

            SubGroupCounterIncrease(subgroup);

            Global.LFGMgr.SetupGroupMember(member.guid, GetGUID());
        }

        public void ConvertToLFG()
        {
            m_groupFlags = (m_groupFlags | GroupFlags.Lfg | GroupFlags.LfgRestricted);
            m_groupCategory = GroupCategory.Instance;
            m_lootMethod = LootMethod.PersonalLoot;
            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (ushort)m_groupFlags);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();
        }

        public void ConvertToRaid()
        {
            m_groupFlags |= GroupFlags.Raid;

            _initRaidSubGroupsCounter();

            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (ushort)m_groupFlags);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();

            // update quest related GO states (quest activity dependent from raid membership)
            foreach (var member in m_memberSlots)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.guid);
                if (player != null)
                    player.UpdateVisibleGameobjectsOrSpellClicks();
            }
        }

        public void ConvertToGroup()
        {
            if (m_memberSlots.Count > 5)
                return; // What message error should we send?

            m_groupFlags &= ~GroupFlags.Raid;

            m_subGroupsCounts = null;

            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (ushort)m_groupFlags);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();

            // update quest related GO states (quest activity dependent from raid membership)
            foreach (var member in m_memberSlots)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.guid);
                if (player != null)
                    player.UpdateVisibleGameobjectsOrSpellClicks();
            }
        }

        public bool AddInvite(Player player)
        {
            if (player == null || player.GetGroupInvite() != null)
                return false;
            Group group = player.GetGroup();
            if (group != null && (group.IsBGGroup() || group.IsBFGroup()))
                group = player.GetOriginalGroup();
            if (group != null)
                return false;

            RemoveInvite(player);

            m_invitees.Add(player);

            player.SetGroupInvite(this);

            Global.ScriptMgr.OnGroupInviteMember(this, player.GetGUID());

            return true;
        }

        public bool AddLeaderInvite(Player player)
        {
            if (!AddInvite(player))
                return false;

            m_leaderGuid = player.GetGUID();
            m_leaderFactionGroup = Player.GetFactionGroupForRace(player.GetRace());
            m_leaderName = player.GetName();
            return true;
        }

        public void RemoveInvite(Player player)
        {
            if (player != null)
            {
                m_invitees.Remove(player);
                player.SetGroupInvite(null);
            }
        }

        public void RemoveAllInvites()
        {
            foreach (var pl in m_invitees)
                if (pl != null)
                    pl.SetGroupInvite(null);

            m_invitees.Clear();
        }

        public Player GetInvited(ObjectGuid guid)
        {
            foreach (var pl in m_invitees)
            {
                if (pl != null && pl.GetGUID() == guid)
                    return pl;
            }
            return null;
        }

        public Player GetInvited(string name)
        {
            foreach (var pl in m_invitees)
            {
                if (pl != null && pl.GetName() == name)
                    return pl;
            }
            return null;
        }

        public bool AddMember(Player player)
        {
            // Get first not-full group
            byte subGroup = 0;
            if (m_subGroupsCounts != null)
            {
                bool groupFound = false;
                for (; subGroup < MapConst.MaxRaidSubGroups; ++subGroup)
                {
                    if (m_subGroupsCounts[subGroup] < MapConst.MaxGroupSize)
                    {
                        groupFound = true;
                        break;
                    }
                }
                // We are raid group and no one slot is free
                if (!groupFound)
                    return false;
            }

            MemberSlot member = new();
            member.guid = player.GetGUID();
            member.name = player.GetName();
            member.race = player.GetRace();
            member._class = (byte)player.GetClass();
            member.group = subGroup;
            member.flags = 0;
            member.roles = 0;
            member.readyChecked = false;
            m_memberSlots.Add(member);

            SubGroupCounterIncrease(subGroup);

            player.SetGroupInvite(null);
            if (player.GetGroup() != null)
            {
                if (IsBGGroup() || IsBFGroup()) // if player is in group and he is being added to BG raid group, then call SetBattlegroundRaid()
                    player.SetBattlegroundOrBattlefieldRaid(this, subGroup);
                else //if player is in bg raid and we are adding him to normal group, then call SetOriginalGroup()
                    player.SetOriginalGroup(this, subGroup);
            }
            else //if player is not in group, then call set group
                player.SetGroup(this, subGroup);

            player.SetPartyType(m_groupCategory, GroupType.Normal);
            player.ResetGroupUpdateSequenceIfNeeded(this);

            // if the same group invites the player back, cancel the homebind timer
            player.m_InstanceValid = player.CheckInstanceValidity(false);

            if (!IsRaidGroup())                                      // reset targetIcons for non-raid-groups
            {
                for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                    m_targetIcons[i].Clear();
            }

            // insert into the table if we're not a Battlegroundgroup
            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_GROUP_MEMBER);

                stmt.AddValue(0, m_dbStoreId);
                stmt.AddValue(1, member.guid.GetCounter());
                stmt.AddValue(2, (byte)member.flags);
                stmt.AddValue(3, member.group);
                stmt.AddValue(4, (byte)member.roles);

                DB.Characters.Execute(stmt);

            }

            SendUpdate();
            Global.ScriptMgr.OnGroupAddMember(this, player.GetGUID());

            if (!IsLeader(player.GetGUID()) && !IsBGGroup() && !IsBFGroup())
            {
                if (player.GetDungeonDifficultyID() != GetDungeonDifficultyID())
                {
                    player.SetDungeonDifficultyID(GetDungeonDifficultyID());
                    player.SendDungeonDifficulty();
                }
                if (player.GetRaidDifficultyID() != GetRaidDifficultyID())
                {
                    player.SetRaidDifficultyID(GetRaidDifficultyID());
                    player.SendRaidDifficulty(false);
                }
                if (player.GetLegacyRaidDifficultyID() != GetLegacyRaidDifficultyID())
                {
                    player.SetLegacyRaidDifficultyID(GetLegacyRaidDifficultyID());
                    player.SendRaidDifficulty(true);
                }
            }
            player.SetGroupUpdateFlag(GroupUpdateFlags.Full);
            Pet pet = player.GetPet();
            if (pet != null)
                pet.SetGroupUpdateFlag(GroupUpdatePetFlags.Full);

            UpdatePlayerOutOfRange(player);

            // quest related GO state dependent from raid membership
            if (IsRaidGroup())
                player.UpdateVisibleGameobjectsOrSpellClicks();

            {
                // Broadcast new player group member fields to rest of the group
                UpdateData groupData = new(player.GetMapId());
                UpdateObject groupDataPacket;

                // Broadcast group members' fields to player
                for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
                {
                    if (refe.GetSource() == player)
                        continue;

                    Player existingMember = refe.GetSource();
                    if (existingMember != null)
                    {
                        if (player.HaveAtClient(existingMember))
                            existingMember.BuildValuesUpdateBlockForPlayerWithFlag(groupData, UpdateFieldFlag.PartyMember, player);

                        if (existingMember.HaveAtClient(player))
                        {
                            UpdateData newData = new(player.GetMapId());
                            UpdateObject newDataPacket;
                            player.BuildValuesUpdateBlockForPlayerWithFlag(newData, UpdateFieldFlag.PartyMember, existingMember);
                            if (newData.HasData())
                            {
                                newData.BuildPacket(out newDataPacket);
                                existingMember.SendPacket(newDataPacket);
                            }
                        }
                    }
                }

                if (groupData.HasData())
                {
                    groupData.BuildPacket(out groupDataPacket);
                    player.SendPacket(groupDataPacket);
                }
            }

            return true;
        }

        public bool RemoveMember(ObjectGuid guid, RemoveMethod method = RemoveMethod.Default, ObjectGuid kicker = default, string reason = null)
        {
            BroadcastGroupUpdate();

            Global.ScriptMgr.OnGroupRemoveMember(this, guid, method, kicker, reason);

            Player player = Global.ObjAccessor.FindConnectedPlayer(guid);
            if (player != null)
            {
                for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
                {
                    Player groupMember = refe.GetSource();
                    if (groupMember != null)
                    {
                        if (groupMember.GetGUID() == guid)
                            continue;

                        groupMember.RemoveAllGroupBuffsFromCaster(guid);
                        player.RemoveAllGroupBuffsFromCaster(groupMember.GetGUID());
                    }
                }
            }

            // LFG group vote kick handled in scripts
            if (IsLFGGroup() && method == RemoveMethod.Kick)
                return m_memberSlots.Count != 0;

            // remove member and change leader (if need) only if strong more 2 members _before_ member remove (BG/BF allow 1 member group)
            if (GetMembersCount() > ((IsBGGroup() || IsLFGGroup() || IsBFGroup()) ? 1 : 2))
            {
                if (player != null)
                {
                    // Battlegroundgroup handling
                    if (IsBGGroup() || IsBFGroup())
                        player.RemoveFromBattlegroundOrBattlefieldRaid();
                    else
                    // Regular group
                    {
                        if (player.GetOriginalGroup() == this)
                            player.SetOriginalGroup(null);
                        else
                            player.SetGroup(null);

                        // quest related GO state dependent from raid membership
                        player.UpdateVisibleGameobjectsOrSpellClicks();


                    }
                    player.SetPartyType(m_groupCategory, GroupType.None);

                    if (method == RemoveMethod.Kick || method == RemoveMethod.KickLFG)
                        player.SendPacket(new GroupUninvite());

                    _homebindIfInstance(player);
                }

                // Remove player from group in DB
                if (!IsBGGroup() && !IsBFGroup())
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER);
                    stmt.AddValue(0, guid.GetCounter());
                    DB.Characters.Execute(stmt);
                    DelinkMember(guid);
                }

                // Update subgroups
                var slot = _getMemberSlot(guid);
                if (slot != null)
                {
                    SubGroupCounterDecrease(slot.group);
                    m_memberSlots.Remove(slot);
                }

                // Pick new leader if necessary
                if (m_leaderGuid == guid)
                {
                    foreach (var member in m_memberSlots)
                    {
                        if (Global.ObjAccessor.FindPlayer(member.guid) != null)
                        {
                            ChangeLeader(member.guid);
                            break;
                        }
                    }
                }

                SendUpdate();

                if (IsLFGGroup() && GetMembersCount() == 1)
                {
                    Player leader = Global.ObjAccessor.FindPlayer(GetLeaderGUID());
                    uint mapId = Global.LFGMgr.GetDungeonMapId(GetGUID());
                    if (mapId == 0 || leader == null || (leader.IsAlive() && leader.GetMapId() != mapId))
                    {
                        Disband();
                        return false;
                    }
                }

                if (m_memberMgr.GetSize() < ((IsLFGGroup() || IsBGGroup()) ? 1 : 2))
                    Disband();
                else if (player != null)
                {
                    // send update to removed player too so party frames are destroyed clientside
                    SendUpdateDestroyGroupToPlayer(player);
                }

                return true;
            }
            // If group size before player removal <= 2 then disband it
            else
            {
                Disband();
                return false;
            }
        }

        public void ChangeLeader(ObjectGuid newLeaderGuid)
        {
            var slot = _getMemberSlot(newLeaderGuid);
            if (slot == null)
                return;

            Player newLeader = Global.ObjAccessor.FindPlayer(slot.guid);

            // Don't allow switching leader to offline players
            if (newLeader == null)
                return;

            Global.ScriptMgr.OnGroupChangeLeader(this, newLeaderGuid, m_leaderGuid);

            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt;
                SQLTransaction trans = new();

                // Update the group leader
                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_LEADER);

                stmt.AddValue(0, newLeader.GetGUID().GetCounter());
                stmt.AddValue(1, m_dbStoreId);

                trans.Append(stmt);

                DB.Characters.CommitTransaction(trans);
            }

            Player oldLeader = Global.ObjAccessor.FindConnectedPlayer(m_leaderGuid);
            if (oldLeader != null)
                oldLeader.RemovePlayerFlag(PlayerFlags.GroupLeader);

            newLeader.SetPlayerFlag(PlayerFlags.GroupLeader);
            m_leaderGuid = newLeader.GetGUID();
            m_leaderFactionGroup = Player.GetFactionGroupForRace(newLeader.GetRace());
            m_leaderName = newLeader.GetName();
            ToggleGroupMemberFlag(slot, GroupMemberFlags.Assistant, false);

            GroupNewLeader groupNewLeader = new();
            groupNewLeader.Name = m_leaderName;
            groupNewLeader.PartyIndex = (sbyte)GetGroupCategory();
            BroadcastPacket(groupNewLeader, true);
        }

        public void Disband(bool hideDestroy = false)
        {
            Global.ScriptMgr.OnGroupDisband(this);

            Player player;
            foreach (var member in m_memberSlots)
            {
                player = Global.ObjAccessor.FindPlayer(member.guid);
                if (player == null)
                    continue;

                //we cannot call _removeMember because it would invalidate member iterator
                //if we are removing player from Battlegroundraid
                if (IsBGGroup() || IsBFGroup())
                    player.RemoveFromBattlegroundOrBattlefieldRaid();
                else
                {
                    //we can remove player who is in Battlegroundfrom his original group
                    if (player.GetOriginalGroup() == this)
                        player.SetOriginalGroup(null);
                    else
                        player.SetGroup(null);
                }

                player.SetPartyType(m_groupCategory, GroupType.None);

                // quest related GO state dependent from raid membership
                if (IsRaidGroup())
                    player.UpdateVisibleGameobjectsOrSpellClicks();

                if (!hideDestroy)
                    player.SendPacket(new GroupDestroyed());

                SendUpdateDestroyGroupToPlayer(player);

                _homebindIfInstance(player);
            }

            m_memberSlots.Clear();

            RemoveAllInvites();

            if (!IsBGGroup() && !IsBFGroup())
            {
                SQLTransaction trans = new();

                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP);
                stmt.AddValue(0, m_dbStoreId);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GROUP_MEMBER_ALL);
                stmt.AddValue(0, m_dbStoreId);
                trans.Append(stmt);

                stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_LFG_DATA);
                stmt.AddValue(0, m_dbStoreId);
                trans.Append(stmt);

                DB.Characters.CommitTransaction(trans);

                Global.GroupMgr.FreeGroupDbStoreId(this);
            }

            Global.GroupMgr.RemoveGroup(this);
        }

        public void SetTargetIcon(byte symbol, ObjectGuid target, ObjectGuid changedBy)
        {
            if (symbol >= MapConst.TargetIconsCount)
                return;

            // clean other icons
            if (!target.IsEmpty())
                for (byte i = 0; i < MapConst.TargetIconsCount; ++i)
                    if (m_targetIcons[i] == target)
                        SetTargetIcon(i, ObjectGuid.Empty, changedBy);

            m_targetIcons[symbol] = target;

            SendRaidTargetUpdateSingle updateSingle = new();
            updateSingle.PartyIndex = (sbyte)GetGroupCategory();
            updateSingle.Target = target;
            updateSingle.ChangedBy = changedBy;
            updateSingle.Symbol = (sbyte)symbol;
            BroadcastPacket(updateSingle, true);
        }

        public void SendTargetIconList(WorldSession session)
        {
            if (session == null)
                return;

            SendRaidTargetUpdateAll updateAll = new();
            updateAll.PartyIndex = (sbyte)GetGroupCategory();
            for (byte i = 0; i < MapConst.TargetIconsCount; i++)
                updateAll.TargetIcons.Add(i, m_targetIcons[i]);

            session.SendPacket(updateAll);
        }

        public void SendUpdate()
        {
            foreach (var member in m_memberSlots)
                SendUpdateToPlayer(member.guid, member);
        }

        public void SendUpdateToPlayer(ObjectGuid playerGUID, MemberSlot memberSlot = null)
        {
            Player player = Global.ObjAccessor.FindPlayer(playerGUID);

            if (player == null || player.GetSession() == null || player.GetGroup() != this)
                return;

            // if MemberSlot wasn't provided
            if (memberSlot == null)
            {
                var slot = _getMemberSlot(playerGUID);
                if (slot == null) // if there is no MemberSlot for such a player
                    return;

                memberSlot = slot;
            }
            PartyUpdate partyUpdate = new();

            partyUpdate.PartyFlags = m_groupFlags;
            partyUpdate.PartyIndex = (byte)m_groupCategory;
            partyUpdate.PartyType = IsCreated() ? GroupType.Normal : GroupType.None;

            partyUpdate.PartyGUID = m_guid;
            partyUpdate.LeaderGUID = m_leaderGuid;
            partyUpdate.LeaderFactionGroup = m_leaderFactionGroup;

            partyUpdate.SequenceNum = player.NextGroupUpdateSequenceNumber(m_groupCategory);

            partyUpdate.MyIndex = -1;
            byte index = 0;
            for (var i = 0; i < m_memberSlots.Count; ++i, ++index)
            {
                var member = m_memberSlots[i];
                if (memberSlot.guid == member.guid)
                    partyUpdate.MyIndex = index;

                Player memberPlayer = Global.ObjAccessor.FindConnectedPlayer(member.guid);

                PartyPlayerInfo playerInfos = new();

                playerInfos.GUID = member.guid;
                playerInfos.Name = member.name;
                playerInfos.Class = member._class;

                playerInfos.FactionGroup = Player.GetFactionGroupForRace(member.race);

                playerInfos.Connected = memberPlayer?.GetSession() != null && !memberPlayer.GetSession().PlayerLogout();

                playerInfos.Subgroup = member.group;         // groupid
                playerInfos.Flags = (byte)member.flags;            // See enum GroupMemberFlags
                playerInfos.RolesAssigned = (byte)member.roles;    // Lfg Roles

                partyUpdate.PlayerList.Add(playerInfos);
            }

            if (GetMembersCount() > 1)
            {
                // LootSettings
                PartyLootSettings lootSettings = new();

                lootSettings.Method = (byte)m_lootMethod;
                lootSettings.Threshold = (byte)m_lootThreshold;
                lootSettings.LootMaster = m_lootMethod == LootMethod.MasterLoot ? m_masterLooterGuid : ObjectGuid.Empty;

                partyUpdate.LootSettings = lootSettings;

                // Difficulty Settings
                PartyDifficultySettings difficultySettings = new();

                difficultySettings.DungeonDifficultyID = (uint)m_dungeonDifficulty;
                difficultySettings.RaidDifficultyID = (uint)m_raidDifficulty;
                difficultySettings.LegacyRaidDifficultyID = (uint)m_legacyRaidDifficulty;

                partyUpdate.DifficultySettings = difficultySettings;
            }

            // LfgInfos
            if (IsLFGGroup())
            {
                PartyLFGInfo lfgInfos = new();

                lfgInfos.Slot = Global.LFGMgr.GetLFGDungeonEntry(Global.LFGMgr.GetDungeon(m_guid));
                lfgInfos.BootCount = 0;
                lfgInfos.Aborted = false;

                lfgInfos.MyFlags = (byte)(Global.LFGMgr.GetState(m_guid) == LfgState.FinishedDungeon ? 2 : 0);
                lfgInfos.MyRandomSlot = Global.LFGMgr.GetSelectedRandomDungeon(player.GetGUID());

                lfgInfos.MyPartialClear = 0;
                lfgInfos.MyGearDiff = 0.0f;
                lfgInfos.MyFirstReward = false;

                DungeonFinding.LfgReward reward = Global.LFGMgr.GetRandomDungeonReward(partyUpdate.LfgInfos.Value.MyRandomSlot, player.GetLevel());
                if (reward != null)
                {
                    Quest quest = Global.ObjectMgr.GetQuestTemplate(reward.firstQuest);
                    if (quest != null)
                        lfgInfos.MyFirstReward = player.CanRewardQuest(quest, false);
                }

                lfgInfos.MyStrangerCount = 0;
                lfgInfos.MyKickVoteCount = 0;

                partyUpdate.LfgInfos = lfgInfos;
            }

            player.SendPacket(partyUpdate);
        }

        void SendUpdateDestroyGroupToPlayer(Player player)
        {
            PartyUpdate partyUpdate = new();
            partyUpdate.PartyFlags = GroupFlags.Destroyed;
            partyUpdate.PartyIndex = (byte)m_groupCategory;
            partyUpdate.PartyType = GroupType.None;
            partyUpdate.PartyGUID = m_guid;
            partyUpdate.MyIndex = -1;
            partyUpdate.SequenceNum = player.NextGroupUpdateSequenceNumber(m_groupCategory);
            player.SendPacket(partyUpdate);
        }

        public void UpdatePlayerOutOfRange(Player player)
        {
            if (player == null || !player.IsInWorld)
                return;

            PartyMemberFullState packet = new();
            packet.Initialize(player);

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player member = refe.GetSource();
                if (member != null && member != player && (!member.IsInMap(player) || !member.IsWithinDist(player, member.GetSightRange(), false)))
                    member.SendPacket(packet);
            }
        }

        public void BroadcastAddonMessagePacket(ServerPacket packet, string prefix, bool ignorePlayersInBGRaid, int group = -1, ObjectGuid ignore = default)
        {
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();
                if (player == null || (!ignore.IsEmpty() && player.GetGUID() == ignore) || (ignorePlayersInBGRaid && player.GetGroup() != this))
                    continue;

                if ((group == -1 || refe.GetSubGroup() == group))
                    if (player.GetSession().IsAddonRegistered(prefix))
                        player.SendPacket(packet);
            }
        }

        public void BroadcastPacket(ServerPacket packet, bool ignorePlayersInBGRaid, int group = -1, ObjectGuid ignore = default)
        {
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();
                if (player == null || (!ignore.IsEmpty() && player.GetGUID() == ignore) || (ignorePlayersInBGRaid && player.GetGroup() != this))
                    continue;

                if (player.GetSession() != null && (group == -1 || refe.GetSubGroup() == group))
                    player.SendPacket(packet);
            }
        }

        bool _setMembersGroup(ObjectGuid guid, byte group)
        {
            var slot = _getMemberSlot(guid);
            if (slot == null)
                return false;

            slot.group = group;

            SubGroupCounterIncrease(group);

            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);

                stmt.AddValue(0, group);
                stmt.AddValue(1, guid.GetCounter());

                DB.Characters.Execute(stmt);
            }

            return true;
        }

        public bool SameSubGroup(Player member1, Player member2)
        {
            if (member1 == null || member2 == null)
                return false;

            if (member1.GetGroup() != this || member2.GetGroup() != this)
                return false;
            else
                return member1.GetSubGroup() == member2.GetSubGroup();
        }

        public void ChangeMembersGroup(ObjectGuid guid, byte group)
        {
            // Only raid groups have sub groups
            if (!IsRaidGroup())
                return;

            // Check if player is really in the raid
            var slot = _getMemberSlot(guid);
            if (slot == null)
                return;

            byte prevSubGroup = slot.group;
            // Abort if the player is already in the target sub group
            if (prevSubGroup == group)
                return;

            // Update the player slot with the new sub group setting
            slot.group = group;

            // Increase the counter of the new sub group..
            SubGroupCounterIncrease(group);

            // ..and decrease the counter of the previous one
            SubGroupCounterDecrease(prevSubGroup);

            // Preserve new sub group in database for non-raid groups
            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);

                stmt.AddValue(0, group);
                stmt.AddValue(1, guid.GetCounter());

                DB.Characters.Execute(stmt);
            }

            // In case the moved player is online, update the player object with the new sub group references
            Player player = Global.ObjAccessor.FindPlayer(guid);
            if (player != null)
            {
                if (player.GetGroup() == this)
                    player.GetGroupRef().SetSubGroup(group);
                else
                {
                    // If player is in BG raid, it is possible that he is also in normal raid - and that normal raid is stored in m_originalGroup reference
                    player.GetOriginalGroupRef().SetSubGroup(group);
                }
            }

            // Broadcast the changes to the group
            SendUpdate();
        }

        public void SwapMembersGroups(ObjectGuid firstGuid, ObjectGuid secondGuid)
        {
            if (!IsRaidGroup())
                return;

            MemberSlot[] slots = new MemberSlot[2];
            slots[0] = _getMemberSlot(firstGuid);
            slots[1] = _getMemberSlot(secondGuid);
            if (slots[0] == null || slots[1] == null)
                return;

            if (slots[0].group == slots[1].group)
                return;

            byte tmp = slots[0].group;
            slots[0].group = slots[1].group;
            slots[1].group = tmp;

            SQLTransaction trans = new();
            for (byte i = 0; i < 2; i++)
            {
                // Preserve new sub group in database for non-raid groups
                if (!IsBGGroup() && !IsBFGroup())
                {
                    PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_SUBGROUP);
                    stmt.AddValue(0, slots[i].group);
                    stmt.AddValue(1, slots[i].guid.GetCounter());

                    trans.Append(stmt);
                }

                Player player = Global.ObjAccessor.FindConnectedPlayer(slots[i].guid);
                if (player != null)
                {
                    if (player.GetGroup() == this)
                        player.GetGroupRef().SetSubGroup(slots[i].group);
                    else
                        player.GetOriginalGroupRef().SetSubGroup(slots[i].group);
                }
            }
            DB.Characters.CommitTransaction(trans);

            SendUpdate();
        }

        public void UpdateLooterGuid(WorldObject pLootedObject, bool ifneed = false)
        {
            switch (GetLootMethod())
            {
                case LootMethod.MasterLoot:
                case LootMethod.FreeForAll:
                    return;
                default:
                    // round robin style looting applies for all low
                    // quality items in each loot method except free for all and master loot
                    break;
            }

            ObjectGuid oldLooterGUID = GetLooterGuid();
            var memberSlot = _getMemberSlot(oldLooterGUID);
            if (memberSlot != null)
            {
                if (ifneed)
                {
                    // not update if only update if need and ok
                    Player looter = Global.ObjAccessor.FindPlayer(memberSlot.guid);
                    if (looter != null && looter.IsAtGroupRewardDistance(pLootedObject))
                        return;
                }
            }

            // search next after current
            Player pNewLooter = null;
            foreach (var member in m_memberSlots)
            {
                if (member == memberSlot)
                    continue;

                Player player = Global.ObjAccessor.FindPlayer(member.guid);
                if (player != null)
                    if (player.IsAtGroupRewardDistance(pLootedObject))
                    {
                        pNewLooter = player;
                        break;
                    }
            }

            if (pNewLooter == null)
            {
                // search from start
                foreach (var member in m_memberSlots)
                {
                    Player player = Global.ObjAccessor.FindPlayer(member.guid);
                    if (player != null)
                        if (player.IsAtGroupRewardDistance(pLootedObject))
                        {
                            pNewLooter = player;
                            break;
                        }
                }
            }

            if (pNewLooter != null)
            {
                if (oldLooterGUID != pNewLooter.GetGUID())
                {
                    SetLooterGuid(pNewLooter.GetGUID());
                    SendUpdate();
                }
            }
            else
            {
                SetLooterGuid(ObjectGuid.Empty);
                SendUpdate();
            }
        }

        public GroupJoinBattlegroundResult CanJoinBattlegroundQueue(BattlegroundTemplate bgOrTemplate, BattlegroundQueueTypeId bgQueueTypeId, uint MinPlayerCount, uint MaxPlayerCount, bool isRated, uint arenaSlot, out ObjectGuid errorGuid)
        {
            errorGuid = new ObjectGuid();
            // check if this group is LFG group
            if (IsLFGGroup())
                return GroupJoinBattlegroundResult.LfgCantUseBattleground;

            BattlemasterListRecord bgEntry = CliDB.BattlemasterListStorage.LookupByKey(bgOrTemplate.Id);
            if (bgEntry == null)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;            // shouldn't happen

            // check for min / max count
            uint memberscount = GetMembersCount();

            if (memberscount > bgEntry.MaxGroupSize)                // no MinPlayerCount for Battlegrounds
                return GroupJoinBattlegroundResult.None;                        // ERR_GROUP_JOIN_Battleground_TOO_MANY handled on client side

            // get a player as reference, to compare other players' stats to (arena team id, queue id based on level, etc.)
            Player reference = GetFirstMember().GetSource();
            // no reference found, can't join this way
            if (reference == null)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            PvpDifficultyRecord bracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel((uint)bgOrTemplate.BattlemasterEntry.MapId[0], reference.GetLevel());
            if (bracketEntry == null)
                return GroupJoinBattlegroundResult.BattlegroundJoinFailed;

            uint arenaTeamId = reference.GetArenaTeamId((byte)arenaSlot);
            Team team = reference.GetTeam();
            bool isMercenary = reference.HasAura(BattlegroundConst.SpellMercenaryContractHorde) || reference.HasAura(BattlegroundConst.SpellMercenaryContractAlliance);

            // check every member of the group to be able to join
            memberscount = 0;
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next(), ++memberscount)
            {
                Player member = refe.GetSource();
                // offline member? don't let join
                if (member == null)
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;
                // rbac permissions
                if (!member.CanJoinToBattleground(bgOrTemplate))
                    return GroupJoinBattlegroundResult.JoinTimedOut;
                // don't allow cross-faction join as group
                if (member.GetTeam() != team)
                {
                    errorGuid = member.GetGUID();
                    return GroupJoinBattlegroundResult.JoinTimedOut;
                }
                // not in the same Battleground level braket, don't let join
                PvpDifficultyRecord memberBracketEntry = Global.DB2Mgr.GetBattlegroundBracketByLevel(bracketEntry.MapID, member.GetLevel());
                if (memberBracketEntry != bracketEntry)
                    return GroupJoinBattlegroundResult.JoinRangeIndex;
                // don't let join rated matches if the arena team id doesn't match
                if (isRated && member.GetArenaTeamId((byte)arenaSlot) != arenaTeamId)
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;
                // don't let join if someone from the group is already in that bg queue
                if (member.InBattlegroundQueueForBattlegroundQueueType(bgQueueTypeId))
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;            // not blizz-like
                // don't let join if someone from the group is in bg queue random
                bool isInRandomBgQueue = member.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RB, BattlegroundQueueIdType.Battleground, false, 0))
                    || member.InBattlegroundQueueForBattlegroundQueueType(Global.BattlegroundMgr.BGQueueTypeId((ushort)BattlegroundTypeId.RandomEpic, BattlegroundQueueIdType.Battleground, false, 0));
                if (bgOrTemplate.Id != BattlegroundTypeId.AA && isInRandomBgQueue)
                    return GroupJoinBattlegroundResult.InRandomBg;
                // don't let join to bg queue random if someone from the group is already in bg queue
                if (Global.BattlegroundMgr.IsRandomBattleground(bgOrTemplate.Id) && member.InBattlegroundQueue(true) && !isInRandomBgQueue)
                    return GroupJoinBattlegroundResult.InNonRandomBg;
                // check for deserter debuff in case not arena queue
                if (bgOrTemplate.Id != BattlegroundTypeId.AA && member.IsDeserter())
                    return GroupJoinBattlegroundResult.Deserters;
                // check if member can join any more Battleground queues
                if (!member.HasFreeBattlegroundQueueId())
                    return GroupJoinBattlegroundResult.TooManyQueues;        // not blizz-like
                // check if someone in party is using dungeon system
                if (member.IsUsingLfg())
                    return GroupJoinBattlegroundResult.LfgCantUseBattleground;
                // check Freeze debuff
                if (member.HasAura(9454))
                    return GroupJoinBattlegroundResult.BattlegroundJoinFailed;
                if (isMercenary != (member.HasAura(BattlegroundConst.SpellMercenaryContractHorde) || member.HasAura(BattlegroundConst.SpellMercenaryContractAlliance)))
                    return GroupJoinBattlegroundResult.BattlegroundJoinMercenary;
            }

            // only check for MinPlayerCount since MinPlayerCount == MaxPlayerCount for arenas...
            if (bgOrTemplate.IsArena() && memberscount != MinPlayerCount)
                return GroupJoinBattlegroundResult.ArenaTeamPartySize;

            return GroupJoinBattlegroundResult.None;
        }

        public void SetDungeonDifficultyID(Difficulty difficulty)
        {
            m_dungeonDifficulty = difficulty;
            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_DIFFICULTY);

                stmt.AddValue(0, (byte)m_dungeonDifficulty);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();
                if (player.GetSession() == null)
                    continue;

                player.SetDungeonDifficultyID(difficulty);
                player.SendDungeonDifficulty();
            }
        }

        public void SetRaidDifficultyID(Difficulty difficulty)
        {
            m_raidDifficulty = difficulty;
            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_RAID_DIFFICULTY);

                stmt.AddValue(0, (byte)m_raidDifficulty);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();
                if (player.GetSession() == null)
                    continue;

                player.SetRaidDifficultyID(difficulty);
                player.SendRaidDifficulty(false);
            }
        }

        public void SetLegacyRaidDifficultyID(Difficulty difficulty)
        {
            m_legacyRaidDifficulty = difficulty;
            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_LEGACY_RAID_DIFFICULTY);

                stmt.AddValue(0, (byte)m_legacyRaidDifficulty);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
            {
                Player player = refe.GetSource();
                if (player.GetSession() == null)
                    continue;

                player.SetLegacyRaidDifficultyID(difficulty);
                player.SendRaidDifficulty(true);
            }
        }

        public Difficulty GetDifficultyID(MapRecord mapEntry)
        {
            if (!mapEntry.IsRaid())
                return m_dungeonDifficulty;

            MapDifficultyRecord defaultDifficulty = Global.DB2Mgr.GetDefaultMapDifficulty(mapEntry.Id);
            if (defaultDifficulty == null)
                return m_legacyRaidDifficulty;

            DifficultyRecord difficulty = CliDB.DifficultyStorage.LookupByKey(defaultDifficulty.DifficultyID);
            if (difficulty == null || difficulty.Flags.HasAnyFlag(DifficultyFlags.Legacy))
                return m_legacyRaidDifficulty;

            return m_raidDifficulty;
        }

        public Difficulty GetDungeonDifficultyID() { return m_dungeonDifficulty; }
        public Difficulty GetRaidDifficultyID() { return m_raidDifficulty; }
        public Difficulty GetLegacyRaidDifficultyID() { return m_legacyRaidDifficulty; }

        public void ResetInstances(InstanceResetMethod method, Player notifyPlayer)
        {
            for (GroupInstanceReference refe = m_ownedInstancesMgr.GetFirst(); refe != null; refe = refe.Next())
            {
                InstanceMap map = refe.GetSource();
                switch (map.Reset(method))
                {
                    case InstanceResetResult.Success:
                        notifyPlayer.SendResetInstanceSuccess(map.GetId());
                        m_recentInstances.Remove(map.GetId());
                        break;
                    case InstanceResetResult.NotEmpty:
                        if (method == InstanceResetMethod.Manual)
                            notifyPlayer.SendResetInstanceFailed(ResetFailedReason.Failed, map.GetId());
                        else if (method == InstanceResetMethod.OnChangeDifficulty)
                            m_recentInstances.Remove(map.GetId()); // map might not have been reset on difficulty change but we still don't want to zone in there again
                        break;
                    case InstanceResetResult.CannotReset:
                        m_recentInstances.Remove(map.GetId()); // forget the instance, allows retrying different lockout with a new leader
                        break;
                    default:
                        break;
                }
            }
        }

        public void LinkOwnedInstance(GroupInstanceReference refe)
        {
            m_ownedInstancesMgr.InsertLast(refe);
        }

        void _homebindIfInstance(Player player)
        {
            if (player != null && !player.IsGameMaster() && CliDB.MapStorage.LookupByKey(player.GetMapId()).IsDungeon())
                player.m_InstanceValid = false;
        }

        public void BroadcastGroupUpdate()
        {
            // FG: HACK: force flags update on group leave - for values update hack
            // -- not very efficient but safe
            foreach (var member in m_memberSlots)
            {
                Player player = Global.ObjAccessor.FindPlayer(member.guid);
                if (player != null && player.IsInWorld)
                {
                    player.m_values.ModifyValue(player.m_unitData).ModifyValue(player.m_unitData.PvpFlags);
                    player.m_values.ModifyValue(player.m_unitData).ModifyValue(player.m_unitData.FactionTemplate);
                    player.ForceUpdateFieldChange();
                    Log.outDebug(LogFilter.Server, "-- Forced group value update for '{0}'", player.GetName());
                }
            }
        }

        public void SetLootMethod(LootMethod method)
        {
            m_lootMethod = method;
        }

        public void SetLooterGuid(ObjectGuid guid)
        {
            m_looterGuid = guid;
        }

        public void SetMasterLooterGuid(ObjectGuid guid)
        {
            m_masterLooterGuid = guid;
        }

        public void SetLootThreshold(ItemQuality threshold)
        {
            m_lootThreshold = threshold;
        }

        public void SetLfgRoles(ObjectGuid guid, LfgRoles roles)
        {
            var slot = _getMemberSlot(guid);
            if (slot == null)
                return;

            slot.roles = roles;
            SendUpdate();
        }

        public LfgRoles GetLfgRoles(ObjectGuid guid)
        {
            MemberSlot slot = _getMemberSlot(guid);
            if (slot == null)
                return 0;

            return slot.roles;
        }

        void UpdateReadyCheck(uint diff)
        {
            if (!m_readyCheckStarted)
                return;

            m_readyCheckTimer -= TimeSpan.FromMilliseconds(diff);
            if (m_readyCheckTimer <= TimeSpan.Zero)
                EndReadyCheck();
        }

        public void StartReadyCheck(ObjectGuid starterGuid, TimeSpan duration)
        {
            if (m_readyCheckStarted)
                return;

            MemberSlot slot = _getMemberSlot(starterGuid);
            if (slot == null)
                return;

            m_readyCheckStarted = true;
            m_readyCheckTimer = duration;

            SetOfflineMembersReadyChecked();

            SetMemberReadyChecked(slot);

            ReadyCheckStarted readyCheckStarted = new();
            readyCheckStarted.PartyGUID = m_guid;
            readyCheckStarted.PartyIndex = (sbyte)GetGroupCategory();
            readyCheckStarted.InitiatorGUID = starterGuid;
            readyCheckStarted.Duration = (uint)duration.TotalMilliseconds;
            BroadcastPacket(readyCheckStarted, false);
        }

        void EndReadyCheck()
        {
            if (!m_readyCheckStarted)
                return;

            m_readyCheckStarted = false;
            m_readyCheckTimer = TimeSpan.Zero;

            ResetMemberReadyChecked();

            ReadyCheckCompleted readyCheckCompleted = new();
            readyCheckCompleted.PartyIndex = 0;
            readyCheckCompleted.PartyGUID = m_guid;
            BroadcastPacket(readyCheckCompleted, false);
        }

        bool IsReadyCheckCompleted()
        {
            foreach (var member in m_memberSlots)
                if (!member.readyChecked)
                    return false;
            return true;
        }

        public void SetMemberReadyCheck(ObjectGuid guid, bool ready)
        {
            if (!m_readyCheckStarted)
                return;

            MemberSlot slot = _getMemberSlot(guid);
            if (slot != null)
                SetMemberReadyCheck(slot, ready);
        }

        void SetMemberReadyCheck(MemberSlot slot, bool ready)
        {
            ReadyCheckResponse response = new();
            response.PartyGUID = m_guid;
            response.Player = slot.guid;
            response.IsReady = ready;
            BroadcastPacket(response, false);

            SetMemberReadyChecked(slot);
        }

        void SetOfflineMembersReadyChecked()
        {
            foreach (MemberSlot member in m_memberSlots)
            {
                Player player = Global.ObjAccessor.FindConnectedPlayer(member.guid);
                if (player == null || player.GetSession() == null)
                    SetMemberReadyCheck(member, false);
            }
        }

        void SetMemberReadyChecked(MemberSlot slot)
        {
            slot.readyChecked = true;
            if (IsReadyCheckCompleted())
                EndReadyCheck();
        }

        void ResetMemberReadyChecked()
        {
            foreach (MemberSlot member in m_memberSlots)
                member.readyChecked = false;
        }

        public void AddRaidMarker(byte markerId, uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
        {
            if (markerId >= MapConst.RaidMarkersCount || m_markers[markerId] != null)
                return;

            m_activeMarkers |= (1u << markerId);
            m_markers[markerId] = new RaidMarker(mapId, positionX, positionY, positionZ, transportGuid);
            SendRaidMarkersChanged();
        }

        public void DeleteRaidMarker(byte markerId)
        {
            if (markerId > MapConst.RaidMarkersCount)
                return;

            for (byte i = 0; i < MapConst.RaidMarkersCount; i++)
            {
                if (m_markers[i] != null && (markerId == i || markerId == MapConst.RaidMarkersCount))
                {
                    m_markers[i] = null;
                    m_activeMarkers &= ~(1u << i);
                }
            }

            SendRaidMarkersChanged();
        }

        public void SendRaidMarkersChanged(WorldSession session = null)
        {
            RaidMarkersChanged packet = new();

            packet.PartyIndex = (sbyte)GetGroupCategory();
            packet.ActiveMarkers = m_activeMarkers;

            for (byte i = 0; i < MapConst.RaidMarkersCount; i++)
            {
                if (m_markers[i] != null)
                    packet.RaidMarkers.Add(m_markers[i]);
            }

            if (session != null)
                session.SendPacket(packet);
            else
                BroadcastPacket(packet, false);
        }

        public bool IsFull()
        {
            return IsRaidGroup() ? (m_memberSlots.Count >= MapConst.MaxRaidSize) : (m_memberSlots.Count >= MapConst.MaxGroupSize);
        }

        public bool IsLFGGroup()
        {
            return m_groupFlags.HasAnyFlag(GroupFlags.Lfg);
        }
        public bool IsRaidGroup()
        {
            return m_groupFlags.HasAnyFlag(GroupFlags.Raid);
        }

        public bool IsBGGroup()
        {
            return m_bgGroup != null;
        }

        public bool IsBFGroup()
        {
            return m_bfGroup != null;
        }

        public bool IsCreated()
        {
            return GetMembersCount() > 0;
        }

        public ObjectGuid GetLeaderGUID()
        {
            return m_leaderGuid;
        }

        public ObjectGuid GetGUID()
        {
            return m_guid;
        }

        public ulong GetLowGUID()
        {
            return m_guid.GetCounter();
        }

        public string GetLeaderName()
        {
            return m_leaderName;
        }

        public LootMethod GetLootMethod()
        {
            return m_lootMethod;
        }

        public ObjectGuid GetLooterGuid()
        {
            if (GetLootMethod() == LootMethod.FreeForAll)
                return ObjectGuid.Empty;

            return m_looterGuid;
        }

        public ObjectGuid GetMasterLooterGuid()
        {
            return m_masterLooterGuid;
        }

        public ItemQuality GetLootThreshold()
        {
            return m_lootThreshold;
        }

        public bool IsMember(ObjectGuid guid)
        {
            return _getMemberSlot(guid) != null;
        }

        public bool IsLeader(ObjectGuid guid)
        {
            return GetLeaderGUID() == guid;
        }

        public bool IsAssistant(ObjectGuid guid)
        {
            return GetMemberFlags(guid).HasAnyFlag(GroupMemberFlags.Assistant);
        }

        public ObjectGuid GetMemberGUID(string name)
        {
            foreach (var member in m_memberSlots)
                if (member.name == name)
                    return member.guid;
            return ObjectGuid.Empty;
        }

        public GroupMemberFlags GetMemberFlags(ObjectGuid guid)
        {
            var mslot = _getMemberSlot(guid);
            if (mslot == null)
                return 0;

            return mslot.flags;
        }

        public bool SameSubGroup(ObjectGuid guid1, ObjectGuid guid2)
        {
            var mslot2 = _getMemberSlot(guid2);
            if (mslot2 == null)
                return false;
            return SameSubGroup(guid1, mslot2);
        }

        public bool SameSubGroup(ObjectGuid guid1, MemberSlot slot2)
        {
            var mslot1 = _getMemberSlot(guid1);
            if (mslot1 == null || slot2 == null)
                return false;
            return (mslot1.group == slot2.group);
        }

        public bool HasFreeSlotSubGroup(byte subgroup)
        {
            return (m_subGroupsCounts != null && m_subGroupsCounts[subgroup] < MapConst.MaxGroupSize);
        }

        public byte GetMemberGroup(ObjectGuid guid)
        {
            var mslot = _getMemberSlot(guid);
            if (mslot == null)
                return (byte)(MapConst.MaxRaidSubGroups + 1);
            return mslot.group;
        }

        public void SetBattlegroundGroup(Battleground bg)
        {
            m_bgGroup = bg;
        }

        public void SetBattlefieldGroup(BattleField bg)
        {
            m_bfGroup = bg;
        }

        public void SetGroupMemberFlag(ObjectGuid guid, bool apply, GroupMemberFlags flag)
        {
            // Assistants, main assistants and main tanks are only available in raid groups
            if (!IsRaidGroup())
                return;

            // Check if player is really in the raid
            var slot = _getMemberSlot(guid);
            if (slot == null)
                return;

            // Do flag specific actions, e.g ensure uniqueness
            switch (flag)
            {
                case GroupMemberFlags.MainAssist:
                    RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainAssist);         // Remove main assist flag from current if any.
                    break;
                case GroupMemberFlags.MainTank:
                    RemoveUniqueGroupMemberFlag(GroupMemberFlags.MainTank);           // Remove main tank flag from current if any.
                    break;
                case GroupMemberFlags.Assistant:
                    break;
                default:
                    return;                                                      // This should never happen
            }

            // Switch the actual flag
            ToggleGroupMemberFlag(slot, flag, apply);

            // Preserve the new setting in the db
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_MEMBER_FLAG);

            stmt.AddValue(0, (byte)slot.flags);
            stmt.AddValue(1, guid.GetCounter());

            DB.Characters.Execute(stmt);

            // Broadcast the changes to the group
            SendUpdate();
        }

        public void LinkMember(GroupReference pRef)
        {
            m_memberMgr.InsertFirst(pRef);
        }

        void DelinkMember(ObjectGuid guid)
        {
            GroupReference refe = m_memberMgr.GetFirst();
            while (refe != null)
            {
                GroupReference nextRef = refe.Next();
                if (refe.GetSource().GetGUID() == guid)
                {
                    refe.Unlink();
                    break;
                }
                refe = nextRef;
            }
        }

        void _initRaidSubGroupsCounter()
        {
            // Sub group counters initialization
            if (m_subGroupsCounts == null)
                m_subGroupsCounts = new byte[MapConst.MaxRaidSubGroups];

            foreach (var memberSlot in m_memberSlots)
                ++m_subGroupsCounts[memberSlot.group];
        }

        MemberSlot _getMemberSlot(ObjectGuid guid)
        {
            foreach (var member in m_memberSlots)
                if (member.guid == guid)
                    return member;
            return null;
        }

        void SubGroupCounterIncrease(byte subgroup)
        {
            if (m_subGroupsCounts != null)
                ++m_subGroupsCounts[subgroup];
        }

        void SubGroupCounterDecrease(byte subgroup)
        {
            if (m_subGroupsCounts != null)
                --m_subGroupsCounts[subgroup];
        }

        public void RemoveUniqueGroupMemberFlag(GroupMemberFlags flag)
        {
            foreach (var member in m_memberSlots)
                if (member.flags.HasAnyFlag(flag))
                    member.flags &= ~flag;
        }

        void ToggleGroupMemberFlag(MemberSlot slot, GroupMemberFlags flag, bool apply)
        {
            if (apply)
                slot.flags |= flag;
            else
                slot.flags &= ~flag;
        }

        public void StartLeaderOfflineTimer()
        {
            _isLeaderOffline = true;
            _leaderOfflineTimer.Reset(2 * Time.Minute * Time.InMilliseconds);
        }

        public void StopLeaderOfflineTimer()
        {
            _isLeaderOffline = false;
        }

        public void SetEveryoneIsAssistant(bool apply)
        {
            if (apply)
                m_groupFlags |= GroupFlags.EveryoneAssistant;
            else
                m_groupFlags &= ~GroupFlags.EveryoneAssistant;

            foreach (MemberSlot member in m_memberSlots)
                ToggleGroupMemberFlag(member, GroupMemberFlags.Assistant, apply);

            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (ushort)m_groupFlags);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();
        }

        public bool IsRestrictPingsToAssistants()
        {
            return m_groupFlags.HasFlag(GroupFlags.RestrictPings);
        }

        public void SetRestrictPingsToAssistants(bool restrictPingsToAssistants)
        {
            if (restrictPingsToAssistants)
                m_groupFlags |= GroupFlags.RestrictPings;
            else
                m_groupFlags &= ~GroupFlags.RestrictPings;

            if (!IsBGGroup() && !IsBFGroup())
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GROUP_TYPE);

                stmt.AddValue(0, (ushort)m_groupFlags);
                stmt.AddValue(1, m_dbStoreId);

                DB.Characters.Execute(stmt);
            }

            SendUpdate();
        }

        public GroupCategory GetGroupCategory() { return m_groupCategory; }

        public uint GetDbStoreId() { return m_dbStoreId; }
        public List<MemberSlot> GetMemberSlots() { return m_memberSlots; }
        public GroupReference GetFirstMember() { return (GroupReference)m_memberMgr.GetFirst(); }
        public uint GetMembersCount() { return (uint)m_memberSlots.Count; }
        public uint GetInviteeCount() { return (uint)m_invitees.Count; }
        public GroupFlags GetGroupFlags() { return m_groupFlags; }

        bool IsReadyCheckStarted() { return m_readyCheckStarted; }

        public void BroadcastWorker(Action<Player> worker)
        {
            for (GroupReference refe = GetFirstMember(); refe != null; refe = refe.Next())
                worker(refe.GetSource());
        }

        public ObjectGuid GetRecentInstanceOwner(uint mapId)
        {

            if (m_recentInstances.TryGetValue(mapId, out Tuple<ObjectGuid, uint> value))
                return value.Item1;

            return m_leaderGuid;
        }

        public uint GetRecentInstanceId(uint mapId)
        {
            if (m_recentInstances.TryGetValue(mapId, out Tuple<ObjectGuid, uint> value))
                return value.Item2;

            return 0;
        }

        public void SetRecentInstance(uint mapId, ObjectGuid instanceOwner, uint instanceId)
        {
            m_recentInstances[mapId] = Tuple.Create(instanceOwner, instanceId);
        }
        
        List<MemberSlot> m_memberSlots = new();
        GroupRefManager m_memberMgr = new();
        List<Player> m_invitees = new();
        ObjectGuid m_leaderGuid;
        byte m_leaderFactionGroup;
        string m_leaderName;
        GroupFlags m_groupFlags;
        GroupCategory m_groupCategory;
        Difficulty m_dungeonDifficulty;
        Difficulty m_raidDifficulty;
        Difficulty m_legacyRaidDifficulty;
        Battleground m_bgGroup;
        BattleField m_bfGroup;
        ObjectGuid[] m_targetIcons = new ObjectGuid[MapConst.TargetIconsCount];
        LootMethod m_lootMethod;
        ItemQuality m_lootThreshold;
        ObjectGuid m_looterGuid;
        ObjectGuid m_masterLooterGuid;
        Dictionary<uint, Tuple<ObjectGuid, uint>> m_recentInstances = new();
        GroupInstanceRefManager m_ownedInstancesMgr = new();
        byte[] m_subGroupsCounts;
        ObjectGuid m_guid;
        uint m_dbStoreId;
        bool _isLeaderOffline;
        TimeTracker _leaderOfflineTimer = new();

        // Ready Check
        bool m_readyCheckStarted;
        TimeSpan m_readyCheckTimer;

        // Raid markers
        RaidMarker[] m_markers = new RaidMarker[MapConst.RaidMarkersCount];
        uint m_activeMarkers;
    }

    public class MemberSlot
    {
        public ObjectGuid guid;
        public string name;
        public Race race;
        public byte _class;
        public byte group;
        public GroupMemberFlags flags;
        public LfgRoles roles;
        public bool readyChecked;
    }

    public class RaidMarker
    {
        public RaidMarker(uint mapId, float positionX, float positionY, float positionZ, ObjectGuid transportGuid = default)
        {
            Location = new WorldLocation(mapId, positionX, positionY, positionZ);
            TransportGUID = transportGuid;
        }

        public WorldLocation Location;
        public ObjectGuid TransportGUID;
    }
}
