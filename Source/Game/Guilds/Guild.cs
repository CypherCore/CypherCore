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
using Framework.Database;
using Game.Achievements;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Guilds
{
    public class Guild
    {
        public Guild()
        {
            m_achievementSys = new GuildAchievementMgr(this);
        }

        public bool Create(Player pLeader, string name)
        {
            // Check if guild with such name already exists
            if (Global.GuildMgr.GetGuildByName(name) != null)
                return false;

            WorldSession pLeaderSession = pLeader.GetSession();
            if (pLeaderSession == null)
                return false;

            m_id = Global.GuildMgr.GenerateGuildId();
            m_leaderGuid = pLeader.GetGUID();
            m_name = name;
            m_info = "";
            m_motd = "No message set.";
            m_bankMoney = 0;
            m_createdDate = Time.UnixTime;
            _CreateLogHolders();

            Log.outDebug(LogFilter.Guild, "GUILD: creating guild [{0}] for leader {1} ({2})",
                name, pLeader.GetName(), m_leaderGuid);

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBERS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            byte index = 0;
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD);
            stmt.AddValue(index, m_id);
            stmt.AddValue(++index, name);
            stmt.AddValue(++index, m_leaderGuid.GetCounter());
            stmt.AddValue(++index, m_info);
            stmt.AddValue(++index, m_motd);
            stmt.AddValue(++index, m_createdDate);
            stmt.AddValue(++index, m_emblemInfo.GetStyle());
            stmt.AddValue(++index, m_emblemInfo.GetColor());
            stmt.AddValue(++index, m_emblemInfo.GetBorderStyle());
            stmt.AddValue(++index, m_emblemInfo.GetBorderColor());
            stmt.AddValue(++index, m_emblemInfo.GetBackgroundColor());
            stmt.AddValue(++index, m_bankMoney);
            trans.Append(stmt);

            _CreateDefaultGuildRanks(trans, pLeaderSession.GetSessionDbLocaleIndex()); // Create default ranks
            bool ret = AddMember(trans, m_leaderGuid, GuildDefaultRanks.Master);                  // Add guildmaster

            DB.Characters.CommitTransaction(trans);
            if (ret)
            {
                Member leader = GetMember(m_leaderGuid);
                if (leader != null)
                    SendEventNewLeader(leader, null);

                Global.ScriptMgr.OnGuildCreate(this, pLeader, name);
            }

            return ret;
        }

        public void Disband()
        {
            Global.ScriptMgr.OnGuildDisband(this);

            BroadcastPacket(new GuildEventDisbanded());

            SQLTransaction trans = new SQLTransaction();
            while (!m_members.Empty())
            {
                var member = m_members.First();
                DeleteMember(trans, member.Value.GetGUID(), true);
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_TABS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            // Free bank tab used memory and delete items stored in them
            _DeleteBankItems(trans, true);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEMS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOGS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOGS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            Global.GuildFinderMgr.DeleteGuild(GetGUID());

            Global.GuildMgr.RemoveGuild(m_id);
        }

        public void SaveToDB()
        {
            SQLTransaction trans = new SQLTransaction();

            m_achievementSys.SaveToDB(trans);

            DB.Characters.CommitTransaction(trans);
        }

        public void UpdateMemberData(Player player, GuildMemberData dataid, uint value)
        {
            Member member = GetMember(player.GetGUID());
            if (member != null)
            {
                switch (dataid)
                {
                    case GuildMemberData.ZoneId:
                        member.SetZoneId(value);
                        break;
                    case GuildMemberData.AchievementPoints:
                        member.SetAchievementPoints(value);
                        break;
                    case GuildMemberData.Level:
                        member.SetLevel(value);
                        break;
                    default:
                        Log.outError(LogFilter.Guild, "Guild.UpdateMemberData: Called with incorrect DATAID {0} (value {1})", dataid, value);
                        return;
                }
            }
        }

        void OnPlayerStatusChange(Player player, GuildMemberFlags flag, bool state)
        {
            Member member = GetMember(player.GetGUID());
            if (member != null)
            {
                if (state)
                    member.AddFlag(flag);
                else
                    member.RemoveFlag(flag);
            }
        }

        public bool SetName(string name)
        {
            if (m_name == name || string.IsNullOrEmpty(name) || name.Length > 24 || Global.ObjectMgr.IsReservedName(name) || !ObjectManager.IsValidCharterName(name))
                return false;

            m_name = name;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_NAME);
            stmt.AddValue(0, m_name);
            stmt.AddValue(1, GetId());
            DB.Characters.Execute(stmt);

            GuildNameChanged guildNameChanged = new GuildNameChanged();
            guildNameChanged.GuildGUID = GetGUID();
            guildNameChanged.GuildName = m_name;
            BroadcastPacket(guildNameChanged);

            return true;
        }

        public void HandleRoster(WorldSession session = null)
        {
            GuildRoster roster = new GuildRoster();
            roster.NumAccounts = (int)m_accountsNumber;
            roster.CreateDate = (uint)m_createdDate;
            roster.GuildFlags = 0;


            foreach (var member in m_members.Values)
            {
                GuildRosterMemberData memberData = new GuildRosterMemberData();

                memberData.Guid = member.GetGUID();
                memberData.RankID = member.GetRankId();
                memberData.AreaID = (int)member.GetZoneId();
                memberData.PersonalAchievementPoints = (int)member.GetAchievementPoints();
                memberData.GuildReputation = (int)member.GetTotalReputation();
                memberData.LastSave = member.GetInactiveDays();

                //GuildRosterProfessionData

                memberData.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
                memberData.Status = (byte)member.GetFlags();
                memberData.Level = member.GetLevel();
                memberData.ClassID = (byte)member.GetClass();
                memberData.Gender = (byte)member.GetGender();

                memberData.Authenticated = false;
                memberData.SorEligible = false;

                memberData.Name = member.GetName();
                memberData.Note = member.GetPublicNote();
                memberData.OfficerNote = member.GetOfficerNote();

                roster.MemberData.Add(memberData);
            }

            roster.WelcomeText = m_motd;
            roster.InfoText = m_info;

            if (session != null)
                session.SendPacket(roster);
        }

        public void SendQueryResponse(WorldSession session)
        {
            QueryGuildInfoResponse response = new QueryGuildInfoResponse();
            response.GuildGUID = GetGUID();
            response.HasGuildInfo = true;

            response.Info.GuildGuid = GetGUID();
            response.Info.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();

            response.Info.EmblemStyle = m_emblemInfo.GetStyle();
            response.Info.EmblemColor = m_emblemInfo.GetColor();
            response.Info.BorderStyle = m_emblemInfo.GetBorderStyle();
            response.Info.BorderColor = m_emblemInfo.GetBorderColor();
            response.Info.BackgroundColor = m_emblemInfo.GetBackgroundColor();

            for (byte i = 0; i < _GetRanksSize(); ++i)
                response.Info.Ranks.Add(new QueryGuildInfoResponse.GuildInfo.RankInfo(m_ranks[i].GetId(), i, m_ranks[i].GetName()));

            response.Info.GuildName = m_name;

            session.SendPacket(response);
        }

        public void SendGuildRankInfo(WorldSession session)
        {
            GuildRanks ranks = new GuildRanks();

            for (byte i = 0; i < _GetRanksSize(); i++)
            {
                RankInfo rankInfo = GetRankInfo(i);
                if (rankInfo == null)
                    continue;

                GuildRankData rankData = new GuildRankData();

                rankData.RankID = rankInfo.GetId();
                rankData.RankOrder = i;
                rankData.Flags = (uint)rankInfo.GetRights();
                rankData.WithdrawGoldLimit = (rankInfo.GetId() == GuildDefaultRanks.Master ? uint.MaxValue : (rankInfo.GetBankMoneyPerDay() / MoneyConstants.Gold));
                rankData.RankName = rankInfo.GetName();

                for (byte j = 0; j < GuildConst.MaxBankTabs; ++j)
                {
                    rankData.TabFlags[j] = (uint)rankInfo.GetBankTabRights(j);
                    rankData.TabWithdrawItemLimit[j] = (uint)rankInfo.GetBankTabSlotsPerDay(j);
                }

                ranks.Ranks.Add(rankData);
            }

            session.SendPacket(ranks);
        }

        public void HandleSetAchievementTracking(WorldSession session, List<uint> achievementIds)
        {
            Player player = session.GetPlayer();

            Member member = GetMember(player.GetGUID());
            if (member != null)
            {
                List<uint> criteriaIds = new List<uint>();
                foreach (var achievementId in achievementIds)
                {
                    var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);
                    if (achievement != null)
                    {
                        CriteriaTree tree = Global.CriteriaMgr.GetCriteriaTree(achievement.CriteriaTree);
                        if (tree != null)
                        {
                            CriteriaManager.WalkCriteriaTree(tree, node =>
                            {
                                if (node.Criteria != null)
                                    criteriaIds.Add(node.Criteria.ID);
                            });
                        }
                    }
                }

                member.SetTrackedCriteriaIds(criteriaIds);
                GetAchievementMgr().SendAllTrackedCriterias(player, member.GetTrackedCriteriaIds());
            }
        }

        public void HandleGetAchievementMembers(WorldSession session, uint achievementId)
        {
            GetAchievementMgr().SendAchievementMembers(session.GetPlayer(), achievementId);
        }

        public void HandleSetMOTD(WorldSession session, string motd)
        {
            if (m_motd == motd)
                return;

            // Player must have rights to set MOTD
            if (!_HasRankRight(session.GetPlayer(), GuildRankRights.SetMotd))
                SendCommandResult(session, GuildCommandType.EditMOTD, GuildCommandError.Permissions);
            else
            {
                m_motd = motd;

                Global.ScriptMgr.OnGuildMOTDChanged(this, motd);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MOTD);
                stmt.AddValue(0, motd);
                stmt.AddValue(1, m_id);
                DB.Characters.Execute(stmt);

                SendEventMOTD(session, true);
            }
        }

        public void HandleSetInfo(WorldSession session, string info)
        {
            if (m_info == info)
                return;

            // Player must have rights to set guild's info
            if (_HasRankRight(session.GetPlayer(), GuildRankRights.ModifyGuildInfo))
            {
                m_info = info;

                Global.ScriptMgr.OnGuildInfoChanged(this, info);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_INFO);
                stmt.AddValue(0, info);
                stmt.AddValue(1, m_id);
                DB.Characters.Execute(stmt);
            }
        }

        public void HandleSetEmblem(WorldSession session, EmblemInfo emblemInfo)
        {
            Player player = session.GetPlayer();
            if (!_IsLeader(player))
                SendSaveEmblemResult(session, GuildEmblemError.NotGuildMaster); // "Only guild leaders can create emblems."
            else if (!player.HasEnoughMoney(10 * MoneyConstants.Gold))
                SendSaveEmblemResult(session, GuildEmblemError.NotEnoughMoney); // "You can't afford to do that."
            else
            {
                player.ModifyMoney(-(long)10 * MoneyConstants.Gold);

                m_emblemInfo = emblemInfo;
                m_emblemInfo.SaveToDB(m_id);

                SendSaveEmblemResult(session, GuildEmblemError.Success); // "Guild Emblem saved."

                SendQueryResponse(session);
            }
        }

        public void HandleSetNewGuildMaster(WorldSession session, string name, bool isSelfPromote)
        {
            Player player = session.GetPlayer();
            Member oldGuildMaster = GetMember(GetLeaderGUID());

            Member newGuildMaster;
            if (isSelfPromote)
            {
                newGuildMaster = GetMember(player.GetGUID());
                if (newGuildMaster == null)
                    return;

                if (!newGuildMaster.IsRankNotLower(GuildDefaultRanks.Member) || oldGuildMaster.GetInactiveDays() < GuildConst.MasterDethroneInactiveDays)
                {
                    SendCommandResult(session, GuildCommandType.ChangeLeader, GuildCommandError.Permissions);
                    return;
                }
            }
            else
            {
                if (!_IsLeader(player))
                {
                    SendCommandResult(session, GuildCommandType.ChangeLeader, GuildCommandError.Permissions);
                    return;
                }

                newGuildMaster = GetMember(name);
                if (newGuildMaster == null)
                    return;
            }

            SQLTransaction trans = new SQLTransaction();

            _SetLeader(trans, newGuildMaster);
            oldGuildMaster.ChangeRank(trans, GuildDefaultRanks.Initiate);

            SendEventNewLeader(newGuildMaster, oldGuildMaster, isSelfPromote);

            DB.Characters.CommitTransaction(trans);
        }

        public void HandleSetBankTabInfo(WorldSession session, byte tabId, string name, string icon)
        {
            BankTab tab = GetBankTab(tabId);
            if (tab == null)
            {
                Log.outError(LogFilter.Guild, "Guild.HandleSetBankTabInfo: Player {0} trying to change bank tab info from unexisting tab {1}.",
                    session.GetPlayer().GetName(), tabId);
                return;
            }

            tab.SetInfo(name, icon);

            GuildEventTabModified packet = new GuildEventTabModified();
            packet.Tab = tabId;
            packet.Name = name;
            packet.Icon = icon;
            BroadcastPacket(packet);
        }

        public void HandleSetMemberNote(WorldSession session, string note, ObjectGuid guid, bool isPublic)
        {
            // Player must have rights to set public/officer note
            if (!_HasRankRight(session.GetPlayer(), isPublic ? GuildRankRights.EditPublicNote : GuildRankRights.EOffNote))
                SendCommandResult(session, GuildCommandType.EditPublicNote, GuildCommandError.Permissions);
            Member member = GetMember(guid);
            if (member != null)
            {
                if (isPublic)
                    member.SetPublicNote(note);
                else
                    member.SetOfficerNote(note);

                GuildMemberUpdateNote updateNote = new GuildMemberUpdateNote();
                updateNote.Member = guid;
                updateNote.IsPublic = isPublic;
                updateNote.Note = note;
                BroadcastPacket(updateNote);
            }
        }

        public void HandleSetRankInfo(WorldSession session, uint rankId, string name, GuildRankRights rights, uint moneyPerDay, GuildBankRightsAndSlots[] rightsAndSlots)
        {
            // Only leader can modify ranks
            if (!_IsLeader(session.GetPlayer()))
                SendCommandResult(session, GuildCommandType.ChangeRank, GuildCommandError.Permissions);

            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
            {
                Log.outDebug(LogFilter.Guild, "Changed RankName to '{0}', rights to 0x{1}", name, rights);

                rankInfo.SetName(name);
                rankInfo.SetRights(rights);
                _SetRankBankMoneyPerDay(rankId, moneyPerDay * MoneyConstants.Gold);

                foreach (var rightsAndSlot in rightsAndSlots)
                    _SetRankBankTabRightsAndSlots(rankId, rightsAndSlot);

                GuildEventRankChanged packet = new GuildEventRankChanged();
                packet.RankID = rankId;
                BroadcastPacket(packet);
            }
        }

        public void HandleBuyBankTab(WorldSession session, byte tabId)
        {
            Player player = session.GetPlayer();
            if (player == null)
                return;

            Member member = GetMember(player.GetGUID());
            if (member == null)
                return;

            if (_GetPurchasedTabsSize() >= GuildConst.MaxBankTabs)
                return;

            if (tabId != _GetPurchasedTabsSize())
                return;

            if (tabId >= GuildConst.MaxBankTabs)
                return;

            // Do not get money for bank tabs that the GM bought, we had to buy them already.
            // This is just a speedup check, GetGuildBankTabPrice will return 0.
            if (tabId < GuildConst.MaxBankTabs - 2) // 7th tab is actually the 6th
            {
                long tabCost = (long)(GetGuildBankTabPrice(tabId) * MoneyConstants.Gold);
                if (!player.HasEnoughMoney(tabCost))                   // Should not happen, this is checked by client
                    return;

                player.ModifyMoney(-tabCost);
            }

            _CreateNewBankTab();

            BroadcastPacket(new GuildEventTabAdded());

            SendPermissions(session); //Hack to force client to update permissions
        }

        public void HandleInviteMember(WorldSession session, string name)
        {
            Player pInvitee = Global.ObjAccessor.FindPlayerByName(name);
            if (pInvitee == null)
            {
                SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.PlayerNotFound_S, name);
                return;
            }

            Player player = session.GetPlayer();
            // Do not show invitations from ignored players
            if (pInvitee.GetSocial().HasIgnore(player.GetGUID()))
                return;

            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) && pInvitee.GetTeam() != player.GetTeam())
            {
                SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.NotAllied, name);
                return;
            }

            // Invited player cannot be in another guild
            if (pInvitee.GetGuildId() != 0)
            {
                SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInGuild_S, name);
                return;
            }

                // Invited player cannot be invited
            if (pInvitee.GetGuildIdInvited() != 0)
            {
                SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.AlreadyInvitedToGuild_S, name);
                return;
            }
            // Inviting player must have rights to invite
            if (!_HasRankRight(player, GuildRankRights.Invite))
            {
                SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.Permissions);
                return;
            }

            SendCommandResult(session, GuildCommandType.InvitePlayer, GuildCommandError.Success, name);

            Log.outDebug(LogFilter.Guild, "Player {0} invited {1} to join his Guild", player.GetName(), name);

            pInvitee.SetGuildIdInvited(m_id);
            _LogEvent(GuildEventLogTypes.InvitePlayer, player.GetGUID().GetCounter(), pInvitee.GetGUID().GetCounter());

            GuildInvite invite = new GuildInvite();

            invite.InviterVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            invite.GuildVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            invite.GuildGUID = GetGUID();

            invite.EmblemStyle = m_emblemInfo.GetStyle();
            invite.EmblemColor = m_emblemInfo.GetColor();
            invite.BorderStyle = m_emblemInfo.GetBorderStyle();
            invite.BorderColor = m_emblemInfo.GetBorderColor();
            invite.Background = m_emblemInfo.GetBackgroundColor();
            invite.AchievementPoints = (int)GetAchievementMgr().GetAchievementPoints();

            invite.InviterName = player.GetName();
            invite.GuildName = GetName();

            Guild oldGuild = pInvitee.GetGuild();
            if (oldGuild)
            {
                invite.OldGuildGUID = oldGuild.GetGUID();
                invite.OldGuildName = oldGuild.GetName();
                invite.OldGuildVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            }

            pInvitee.SendPacket(invite);
        }

        public void HandleAcceptMember(WorldSession session)
        {
            Player player = session.GetPlayer();
            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) &&
                player.GetTeam() != ObjectManager.GetPlayerTeamByGUID(GetLeaderGUID()))
                return;

            AddMember(null, player.GetGUID());
        }

        public void HandleLeaveMember(WorldSession session)
        {
            Player player = session.GetPlayer();

            // If leader is leaving
            if (_IsLeader(player))
            {
                if (m_members.Count > 1)
                    // Leader cannot leave if he is not the last member
                    SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.LeaderLeave);
                else
                {
                    // Guild is disbanded if leader leaves.
                    Disband();
                }
            }
            else
            {
                DeleteMember(null, player.GetGUID(), false, false);

                _LogEvent(GuildEventLogTypes.LeaveGuild, player.GetGUID().GetCounter());
                SendEventPlayerLeft(player);

                SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.Success, m_name);
            }

            Global.CalendarMgr.RemovePlayerGuildEventsAndSignups(player.GetGUID(), GetId());
        }

        public void HandleRemoveMember(WorldSession session, ObjectGuid guid)
        {
            Player player = session.GetPlayer();

            // Player must have rights to remove members
            if (!_HasRankRight(player, GuildRankRights.Remove))
                SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.Permissions);

            Member member = GetMember(guid);
            if (member != null)
            {
                string name = member.GetName();

                // Guild masters cannot be removed
                if (member.IsRank(GuildDefaultRanks.Master))
                    SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.LeaderLeave);
                // Do not allow to remove player with the same rank or higher
                else
                {
                    Member memberMe = GetMember(player.GetGUID());
                    if (memberMe == null || member.IsRankNotLower(memberMe.GetRankId()))
                        SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.RankTooHigh_S, name);
                    else
                    {
                        DeleteMember(null, guid, false, true);
                        _LogEvent(GuildEventLogTypes.UninvitePlayer, player.GetGUID().GetCounter(), guid.GetCounter());

                        Player pMember = Global.ObjAccessor.FindConnectedPlayer(guid);
                        SendEventPlayerLeft(pMember, player, true);

                        SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.Success, name);
                    }
                }
            }
        }

        public void HandleUpdateMemberRank(WorldSession session, ObjectGuid guid, bool demote)
        {
            Player player = session.GetPlayer();
            GuildCommandType type = demote ? GuildCommandType.DemotePlayer : GuildCommandType.PromotePlayer;
            // Player must have rights to promote
            Member member;
            if (!_HasRankRight(player, demote ? GuildRankRights.Demote : GuildRankRights.Promote))
                SendCommandResult(session, type, GuildCommandError.LeaderLeave);
            // Promoted player must be a member of guild
            else if ((member = GetMember(guid)) != null)
            {
                string name = member.GetName();
                // Player cannot promote himself
                if (member.IsSamePlayer(player.GetGUID()))
                {
                    SendCommandResult(session, type, GuildCommandError.NameInvalid);
                    return;
                }

                Member memberMe = GetMember(player.GetGUID());
                byte rankId = memberMe.GetRankId();
                if (demote)
                {
                    // Player can demote only lower rank members
                    if (member.IsRankNotLower(rankId))
                    {
                        SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);
                        return;
                    }
                    // Lowest rank cannot be demoted
                    if (member.GetRankId() >= _GetLowestRankId())
                    {
                        SendCommandResult(session, type, GuildCommandError.RankTooLow_S, name);
                        return;
                    }
                }
                else
                {
                    // Allow to promote only to lower rank than member's rank
                    // member.GetRankId() + 1 is the highest rank that current player can promote to
                    if (member.IsRankNotLower((uint)(rankId + 1)))
                    {
                        SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);
                        return;
                    }
                }

                uint newRankId = (uint)(member.GetRankId() + (demote ? 1 : -1));
                member.ChangeRank(null, newRankId);
                _LogEvent(demote ? GuildEventLogTypes.DemotePlayer : GuildEventLogTypes.PromotePlayer, player.GetGUID().GetCounter(), member.GetGUID().GetCounter(), (byte)newRankId);
                //_BroadcastEvent(demote ? GuildEvents.Demotion : GuildEvents.Promotion, ObjectGuid.Empty, player.GetName(), name, _GetRankName((byte)newRankId));
            }
        }

        public void HandleSetMemberRank(WorldSession session, ObjectGuid targetGuid, ObjectGuid setterGuid, uint rank)
        {
            Player player = session.GetPlayer();
            Member member = GetMember(targetGuid);
            GuildRankRights rights = GuildRankRights.Promote;
            GuildCommandType type = GuildCommandType.PromotePlayer;

            if (rank > member.GetRankId())
            {
                rights = GuildRankRights.Demote;
                type = GuildCommandType.DemotePlayer;
            }

            // Promoted player must be a member of guild
            if (!_HasRankRight(player, rights))
            {
                SendCommandResult(session, type, GuildCommandError.Permissions);
                return;
            }

            // Player cannot promote himself
            if (member.IsSamePlayer(player.GetGUID()))
            {
                SendCommandResult(session, type, GuildCommandError.NameInvalid);
                return;
            }

            SendGuildRanksUpdate(setterGuid, targetGuid, rank);
        }

        public void HandleAddNewRank(WorldSession session, string name)
        {
            byte size = _GetRanksSize();
            if (size >= GuildConst.MaxRanks)
                return;

            // Only leader can add new rank
            if (_IsLeader(session.GetPlayer()))
                if (_CreateRank(null, name, GuildRankRights.GChatListen | GuildRankRights.GChatSpeak))
                    BroadcastPacket(new GuildEventRanksUpdated());
        }

        public void HandleRemoveRank(WorldSession session, uint rankId)
        {
            // Cannot remove rank if total count is minimum allowed by the client or is not leader
            if (_GetRanksSize() <= GuildConst.MinRanks || rankId >= _GetRanksSize() || !_IsLeader(session.GetPlayer()))
                return;

            // Delete bank rights for rank
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS_FOR_RANK);
            stmt.AddValue(0, m_id);
            stmt.AddValue(1, rankId);
            DB.Characters.Execute(stmt);
            // Delete rank
            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_RANK);
            stmt.AddValue(0, m_id);
            stmt.AddValue(1, rankId);
            DB.Characters.Execute(stmt);

            m_ranks.RemoveAt((int)rankId);

            BroadcastPacket(new GuildEventRanksUpdated());
        }

        public void HandleMemberDepositMoney(WorldSession session, ulong amount, bool cashFlow = false)
        {
            // guild bank cannot have more than MAX_MONEY_AMOUNT
            amount = Math.Min(amount, PlayerConst.MaxMoneyAmount - m_bankMoney);
            if (amount == 0)
                return;

            Player player = session.GetPlayer();

            // Call script after validation and before money transfer.
            Global.ScriptMgr.OnGuildMemberDepositMoney(this, player, amount);

            SQLTransaction trans = new SQLTransaction();
            _ModifyBankMoney(trans, amount, true);
            if (!cashFlow)
            {
                player.ModifyMoney(-(long)amount);
                player.SaveGoldToDB(trans);
            }

            _LogBankEvent(trans, cashFlow ? GuildBankEventLogTypes.CashFlowDeposit : GuildBankEventLogTypes.DepositMoney, 0, player.GetGUID().GetCounter(), (uint)amount);
            DB.Characters.CommitTransaction(trans);

            SendEventBankMoneyChanged();

            if (player.GetSession().HasPermission(RBACPermissions.LogGmTrade))
            {
                Log.outCommand(player.GetSession().GetAccountId(), "GM {0} (Account: {1}) deposit money (Amount: {2}) to guild bank (Guild ID {3})",
                    player.GetName(), player.GetSession().GetAccountId(), amount, m_id);
            }
        }

        public bool HandleMemberWithdrawMoney(WorldSession session, ulong amount, bool repair = false)
        {
            // clamp amount to MAX_MONEY_AMOUNT, Players can't hold more than that anyway
            amount = Math.Min(amount, PlayerConst.MaxMoneyAmount);

            if (m_bankMoney < amount)                               // Not enough money in bank
                return false;

            Player player = session.GetPlayer();

            Member member = GetMember(player.GetGUID());
            if (member == null)
                return false;

            if (!_HasRankRight(player, repair ? GuildRankRights.WithdrawRepair : GuildRankRights.WithdrawGold))
                return false;

            if (_GetMemberRemainingMoney(member) < (long)amount)   // Check if we have enough slot/money today
                return false;

            // Call script after validation and before money transfer.
            Global.ScriptMgr.OnGuildMemberWitdrawMoney(this, player, amount, repair);

            SQLTransaction trans = new SQLTransaction();
            // Add money to player (if required)
            if (!repair)
            {
                if (!player.ModifyMoney((long)amount))
                    return false;

                player.SaveGoldToDB(trans);
            }

            // Update remaining money amount
            member.UpdateBankMoneyWithdrawValue(trans, amount);
            // Remove money from bank
            _ModifyBankMoney(trans, amount, false);

            // Log guild bank event
            _LogBankEvent(trans, repair ? GuildBankEventLogTypes.RepairMoney : GuildBankEventLogTypes.WithdrawMoney, 0, player.GetGUID().GetCounter(), (uint)amount);
            DB.Characters.CommitTransaction(trans);

            SendEventBankMoneyChanged();
            return true;
        }

        public void HandleMemberLogout(WorldSession session)
        {
            Player player = session.GetPlayer();
            Member member = GetMember(player.GetGUID());
            if (member != null)
            {
                member.SetStats(player);
                member.UpdateLogoutTime();
                member.ResetFlags();
            }

            SendEventPresenceChanged(session, false, true);
            SaveToDB();
        }

        public void HandleDelete(WorldSession session)
        {
            // Only leader can disband guild
            if (_IsLeader(session.GetPlayer()))
            {
                Disband();
                Log.outDebug(LogFilter.Guild, "Guild Successfully Disbanded");
            }
        }

        public void HandleGuildPartyRequest(WorldSession session)
        {
            Player player = session.GetPlayer();
            Group group = player.GetGroup();

            // Make sure player is a member of the guild and that he is in a group.
            if (!IsMember(player.GetGUID()) || !group)
                return;

            GuildPartyState partyStateResponse = new GuildPartyState();
            partyStateResponse.InGuildParty = (player.GetMap().GetOwnerGuildId(player.GetTeam()) == GetId());
            partyStateResponse.NumMembers = 0;
            partyStateResponse.NumRequired = 0;
            partyStateResponse.GuildXPEarnedMult = 0.0f;
            session.SendPacket(partyStateResponse);
        }

        public void HandleGuildRequestChallengeUpdate(WorldSession session)
        {
            GuildChallengeUpdate updatePacket = new GuildChallengeUpdate();

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                updatePacket.CurrentCount[i] = 0; // @todo current count

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                updatePacket.MaxCount[i] = GuildConst.ChallengesMaxCount[i];

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                updatePacket.MaxLevelGold[i] = GuildConst.ChallengeMaxLevelGoldReward[i];

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                updatePacket.Gold[i] = GuildConst.ChallengeGoldReward[i];

            session.SendPacket(updatePacket);
        }

        public void SendEventLog(WorldSession session)
        {
            var logs = m_eventLog.GetGuildLog();

            if (logs == null)
                return;

            GuildEventLogQueryResults packet = new GuildEventLogQueryResults();
            foreach (var logEntry in logs)
            {
                EventLogEntry eventLog = (EventLogEntry)logEntry;
                eventLog.WritePacket(packet);
            }

            session.SendPacket(packet);
        }

        public void SendNewsUpdate(WorldSession session)
        {
            var logs = m_newsLog.GetGuildLog();

            if (logs.Empty())
                return;

            GuildNewsPkt packet = new GuildNewsPkt();
            foreach (var logEntry in logs)
            {
                NewsLogEntry eventLog = (NewsLogEntry)logEntry;
                eventLog.WritePacket(packet);
            }

            session.SendPacket(packet);
        }

        public void SendBankLog(WorldSession session, byte tabId)
        {
            // GuildConst.MaxBankTabs send by client for money log
            if (tabId < _GetPurchasedTabsSize() || tabId == GuildConst.MaxBankTabs)
            {
                var logs = m_bankEventLog[tabId].GetGuildLog();

                if (logs == null)
                    return;

                GuildBankLogQueryResults packet = new GuildBankLogQueryResults();
                packet.Tab = tabId;

                //if (tabId == GUILD_BANK_MAX_TABS && hasCashFlow)
                //    packet.WeeklyBonusMoney.Set(uint64(weeklyBonusMoney));

                foreach (var logEntry in logs)
                {
                    BankEventLogEntry bankEventLog = (BankEventLogEntry)logEntry;
                    bankEventLog.WritePacket(packet);
                }

                session.SendPacket(packet);
            }
        }

        public void SendBankTabText(WorldSession session, byte tabId)
        {
            BankTab tab = GetBankTab(tabId);
            if (tab != null)
                tab.SendText(this, session);
        }

        public void SendPermissions(WorldSession session)
        {
            Member member = GetMember(session.GetPlayer().GetGUID());
            if (member == null)
                return;

            byte rankId = member.GetRankId();

            GuildPermissionsQueryResults queryResult = new GuildPermissionsQueryResults();
            queryResult.RankID = rankId;
            queryResult.WithdrawGoldLimit = (int)_GetMemberRemainingMoney(member);
            queryResult.Flags = (int)_GetRankRights(rankId);
            queryResult.NumTabs = _GetPurchasedTabsSize();

            for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
            {
                GuildPermissionsQueryResults.GuildRankTabPermissions tabPerm;
                tabPerm.Flags = (int)_GetRankBankTabRights(rankId, tabId);
                tabPerm.WithdrawItemLimit = _GetMemberRemainingSlots(member, tabId);
                queryResult.Tab.Add(tabPerm);
            }

            session.SendPacket(queryResult);
        }

        public void SendMoneyInfo(WorldSession session)
        {
            Member member = GetMember(session.GetPlayer().GetGUID());
            if (member == null)
                return;

            long amount = _GetMemberRemainingMoney(member);

            GuildBankRemainingWithdrawMoney packet = new GuildBankRemainingWithdrawMoney();
            packet.RemainingWithdrawMoney = amount;
            session.SendPacket(packet);
        }

        public void SendLoginInfo(WorldSession session)
        {
            Player player = session.GetPlayer();
            Member member = GetMember(player.GetGUID());
            if (member == null)
                return;

            SendEventMOTD(session);
            SendGuildRankInfo(session);
            SendEventPresenceChanged(session, true, true);      // Broadcast

            // Send to self separately, player is not in world yet and is not found by _BroadcastEvent
            SendEventPresenceChanged(session, true);

            if (member.GetGUID() == GetLeaderGUID())
            {
                GuildFlaggedForRename renameFlag = new GuildFlaggedForRename();
                renameFlag.FlagSet = false;
                player.SendPacket(renameFlag);
            }

            foreach (var entry in CliDB.GuildPerkSpellsStorage.Values)
                player.LearnSpell(entry.SpellID, true);

            m_achievementSys.SendAllData(player);

            // tells the client to request bank withdrawal limit
            player.SendPacket(new GuildMemberDailyReset());

            member.SetStats(player);
            member.AddFlag(GuildMemberFlags.Online);
        }

        void SendEventBankMoneyChanged()
        {
            GuildEventBankMoneyChanged eventPacket = new GuildEventBankMoneyChanged();
            eventPacket.Money = GetBankMoney();
            BroadcastPacket(eventPacket);
        }

        void SendEventMOTD(WorldSession session, bool broadcast = false)
        {
            GuildEventMotd eventPacket = new GuildEventMotd();
            eventPacket.MotdText = GetMOTD();

            if (broadcast)
                BroadcastPacket(eventPacket);
            else
            {
                session.SendPacket(eventPacket);
                Log.outDebug(LogFilter.Guild, "SMSG_GUILD_EVENT_MOTD [{0}] ", session.GetPlayerInfo());
            }
        }

        void SendEventNewLeader(Member newLeader, Member oldLeader, bool isSelfPromoted = false)
        {
            GuildEventNewLeader eventPacket = new GuildEventNewLeader();
            eventPacket.SelfPromoted = isSelfPromoted;
            if (newLeader != null)
            {
                eventPacket.NewLeaderGUID = newLeader.GetGUID();
                eventPacket.NewLeaderName = newLeader.GetName();
                eventPacket.NewLeaderVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            }

            if (oldLeader != null)
            {
                eventPacket.OldLeaderGUID = oldLeader.GetGUID();
                eventPacket.OldLeaderName = oldLeader.GetName();
                eventPacket.OldLeaderVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            }

            BroadcastPacket(eventPacket);
        }

        void SendEventPlayerLeft(Player leaver, Player remover = null, bool isRemoved = false)
        {
            GuildEventPlayerLeft eventPacket = new GuildEventPlayerLeft();
            eventPacket.Removed = isRemoved;
            eventPacket.LeaverGUID = leaver.GetGUID();
            eventPacket.LeaverName = leaver.GetName();
            eventPacket.LeaverVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();

            if (isRemoved && remover)
            {
                eventPacket.RemoverGUID = remover.GetGUID();
                eventPacket.RemoverName = remover.GetName();
                eventPacket.RemoverVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            }

            BroadcastPacket(eventPacket);
        }

        void SendEventPresenceChanged(WorldSession session, bool loggedOn, bool broadcast = false)
        {
            Player player = session.GetPlayer();

            GuildEventPresenceChange eventPacket = new GuildEventPresenceChange();
            eventPacket.Guid = player.GetGUID();
            eventPacket.Name = player.GetName();
            eventPacket.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            eventPacket.LoggedOn = loggedOn;
            eventPacket.Mobile = false;

            if (broadcast)
                BroadcastPacket(eventPacket);
            else
                session.SendPacket(eventPacket);
        }

        public bool LoadFromDB(SQLFields fields)
        {
            m_id = fields.Read<uint>(0);
            m_name = fields.Read<string>(1);
            m_leaderGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(2));

            if (!m_emblemInfo.LoadFromDB(fields))
            {
                Log.outError(LogFilter.Guild, "Guild {0} has invalid emblem colors (Background: {1}, Border: {2}, Emblem: {3}), skipped.",
                    m_id, m_emblemInfo.GetBackgroundColor(), m_emblemInfo.GetBorderColor(), m_emblemInfo.GetColor());
                return false;
            }

            m_info = fields.Read<string>(8);
            m_motd = fields.Read<string>(9);
            m_createdDate = fields.Read<uint>(10);
            m_bankMoney = fields.Read<ulong>(11);

            byte purchasedTabs = (byte)fields.Read<uint>(12);
            if (purchasedTabs > GuildConst.MaxBankTabs)
                purchasedTabs = GuildConst.MaxBankTabs;

            for (byte i = 0; i < purchasedTabs; ++i)
                m_bankTabs.Add(new BankTab(m_id, i));

            _CreateLogHolders();
            return true;
        }

        public void LoadRankFromDB(SQLFields field)
        {
            RankInfo rankInfo = new RankInfo(m_id);

            rankInfo.LoadFromDB(field);

            m_ranks.Add(rankInfo);
        }

        public bool LoadMemberFromDB(SQLFields field)
        {
            ulong lowguid = field.Read<ulong>(1);
            Member member = new Member(m_id, ObjectGuid.Create(HighGuid.Player, lowguid), field.Read<byte>(2));
            if (!member.LoadFromDB(field))
            {
                _DeleteMemberFromDB(null, lowguid);
                return false;
            }
            m_members[member.GetGUID()] = member;
            return true;
        }

        public void LoadBankRightFromDB(SQLFields field)
        {
            // tabId              rights                slots
            GuildBankRightsAndSlots rightsAndSlots = new GuildBankRightsAndSlots(field.Read<byte>(1), field.Read<sbyte>(3), field.Read<int>(4));
            // rankId
            _SetRankBankTabRightsAndSlots(field.Read<byte>(2), rightsAndSlots, false);
        }

        public bool LoadEventLogFromDB(SQLFields field)
        {
            if (m_eventLog.CanInsert())
            {
                m_eventLog.LoadEvent(new EventLogEntry(
                    m_id,                                       // guild id
                    field.Read<uint>(1),                      // guid
                    field.Read<uint>(6),              // timestamp
                    (GuildEventLogTypes)field.Read<byte>(2),   // event type
                    field.Read<ulong>(3),                      // player guid 1
                    field.Read<ulong>(4),                      // player guid 2
                    field.Read<byte>(5)));                     // rank
                return true;
            }
            return false;
        }

        public bool LoadBankEventLogFromDB(SQLFields field)
        {
            byte dbTabId = field.Read<byte>(1);
            bool isMoneyTab = (dbTabId == GuildConst.BankMoneyLogsTab);
            if (dbTabId < _GetPurchasedTabsSize() || isMoneyTab)
            {
                byte tabId = isMoneyTab ? (byte)GuildConst.MaxBankTabs : dbTabId;
                LogHolder pLog = m_bankEventLog[tabId];
                if (pLog.CanInsert())
                {
                    uint guid = field.Read<uint>(2);
                    GuildBankEventLogTypes eventType = (GuildBankEventLogTypes)field.Read<byte>(3);
                    if (BankEventLogEntry.IsMoneyEvent(eventType))
                    {
                        if (!isMoneyTab)
                        {
                            Log.outError(LogFilter.Guild, "GuildBankEventLog ERROR: MoneyEvent(LogGuid: {0}, Guild: {1}) does not belong to money tab ({2}), ignoring...", guid, m_id, dbTabId);
                            return false;
                        }
                    }
                    else if (isMoneyTab)
                    {
                        Log.outError(LogFilter.Guild, "GuildBankEventLog ERROR: non-money event (LogGuid: {0}, Guild: {1}) belongs to money tab, ignoring...", guid, m_id);
                        return false;
                    }
                    pLog.LoadEvent(new BankEventLogEntry(
                        m_id,                                   // guild id
                        guid,                                   // guid
                        field.Read<uint>(8),          // timestamp
                        dbTabId,                                // tab id
                        eventType,                              // event type
                        field.Read<ulong>(4),                  // player guid
                        field.Read<ulong>(5),                  // item or money
                        field.Read<ushort>(6),                  // itam stack count
                        field.Read<byte>(7)));                 // dest tab id
                }
            }
            return true;
        }

        public void LoadGuildNewsLogFromDB(SQLFields field)
        {
            if (!m_newsLog.CanInsert())
                return;

            var news = new NewsLogEntry(
                m_id,                                       // guild id
                field.Read<uint>(1),                      // guid
                field.Read<uint>(6),                      // timestamp //64 bits?
                (GuildNews)field.Read<byte>(2),            // type
                ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(3)),                      // player guid
                field.Read<uint>(4),                      // Flags
                field.Read<uint>(5));                    // value)

            m_newsLog.LoadEvent(news);

        }

        public void LoadBankTabFromDB(SQLFields field)
        {
            byte tabId = field.Read<byte>(1);
            if (tabId >= _GetPurchasedTabsSize())
                Log.outError(LogFilter.Guild, "Invalid tab (tabId: {0}) in guild bank, skipped.", tabId);
            else
                m_bankTabs[tabId].LoadFromDB(field);
        }

        public bool LoadBankItemFromDB(SQLFields field)
        {
            byte tabId = field.Read<byte>(46);
            if (tabId >= _GetPurchasedTabsSize())
            {
                Log.outError(LogFilter.Guild, "Invalid tab for item (GUID: {0}, id: {1}) in guild bank, skipped.",
                    field.Read<uint>(0), field.Read<uint>(1));
                return false;
            }
            return m_bankTabs[tabId].LoadItemFromDB(field);
        }

        public bool Validate()
        {
            // Validate ranks data
            // GUILD RANKS represent a sequence starting from 0 = GUILD_MASTER (ALL PRIVILEGES) to max 9 (lowest privileges).
            // The lower rank id is considered higher rank - so promotion does rank-- and demotion does rank++
            // Between ranks in sequence cannot be gaps - so 0, 1, 2, 4 is impossible
            // Min ranks count is 2 and max is 10.
            bool broken_ranks = false;
            byte ranks = _GetRanksSize();

            SQLTransaction trans = new SQLTransaction();
            if (ranks < GuildConst.MinRanks || ranks > GuildConst.MaxRanks)
            {
                Log.outError(LogFilter.Guild, "Guild {0} has invalid number of ranks, creating new...", m_id);
                broken_ranks = true;
            }
            else
            {
                for (byte rankId = 0; rankId < ranks; ++rankId)
                {
                    RankInfo rankInfo = GetRankInfo(rankId);
                    if (rankInfo.GetId() != rankId)
                    {
                        Log.outError(LogFilter.Guild, "Guild {0} has broken rank id {1}, creating default set of ranks...", m_id, rankId);
                        broken_ranks = true;
                    }
                    else
                    {
                        rankInfo.CreateMissingTabsIfNeeded(_GetPurchasedTabsSize(), trans, true);
                    }
                }
            }

            if (broken_ranks)
            {
                m_ranks.Clear();
                _CreateDefaultGuildRanks(trans, SharedConst.DefaultLocale);
            }

            // Validate members' data
            foreach (var member in m_members.Values)
                if (member.GetRankId() > _GetRanksSize())
                    member.ChangeRank(trans, _GetLowestRankId());

            // Repair the structure of the guild.
            // If the guildmaster doesn't exist or isn't member of the guild
            // attempt to promote another member.
            Member leader = GetMember(m_leaderGuid);
            if (leader == null)
            {
                DeleteMember(trans, m_leaderGuid);
                // If no more members left, disband guild
                if (m_members.Empty())
                {
                    Disband();
                    return false;
                }
            }
            else if (!leader.IsRank(GuildDefaultRanks.Master))
                _SetLeader(trans, leader);

            if (trans.commands.Count > 0)
                DB.Characters.CommitTransaction(trans);
            _UpdateAccountsNumber();
            return true;
        }

        public void BroadcastToGuild(WorldSession session, bool officerOnly, string msg, Language language)
        {
            if (session != null && session.GetPlayer() != null && _HasRankRight(session.GetPlayer(), officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
            {
                ChatPkt data = new ChatPkt();
                data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, language, session.GetPlayer(), null, msg);
                foreach (var member in m_members.Values)
                {
                    Player player = member.FindPlayer();
                    if (player != null)
                        if (player.GetSession() != null && _HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
                        !player.GetSocial().HasIgnore(session.GetPlayer().GetGUID()))
                            player.SendPacket(data);
                }
            }
        }

        public void BroadcastAddonToGuild(WorldSession session, bool officerOnly, string msg, string prefix, bool isLogged)
        {
            if (session != null && session.GetPlayer() != null && _HasRankRight(session.GetPlayer(), officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
            {
                ChatPkt data = new ChatPkt();
                data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, isLogged ? Language.AddonLogged : Language.Addon, session.GetPlayer(), null, msg, 0, "", LocaleConstant.enUS, prefix);
                foreach (var member in m_members.Values)
                {
                    Player player = member.FindPlayer();
                    if (player)
                    {
                        if (player.GetSession() != null && _HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
                            !player.GetSocial().HasIgnore(session.GetPlayer().GetGUID()) && player.GetSession().IsAddonRegistered(prefix))
                            player.SendPacket(data);
                    }
                }
            }
        }

        public void BroadcastPacketToRank(ServerPacket packet, byte rankId)
        {
            foreach (var member in m_members.Values)
            {
                if (member.IsRank(rankId))
                {
                    Player player = member.FindPlayer();
                    if (player != null)
                        player.SendPacket(packet);
                }
            }
        }

        public void BroadcastPacket(ServerPacket packet)
        {
            foreach (var member in m_members.Values)
            {
                Player player = member.FindPlayer();
                if (player != null)
                    player.SendPacket(packet);
            }
        }

        public void BroadcastPacketIfTrackingAchievement(ServerPacket packet, uint criteriaId)
        {
            foreach (var member in m_members.Values)
            {
                if (member.IsTrackingCriteriaId(criteriaId))
                {
                    Player player = member.FindPlayer();
                    if (player)
                        player.SendPacket(packet);
                }
            }
        }

        public void MassInviteToEvent(WorldSession session, uint minLevel, uint maxLevel, uint minRank)
        {
            CalendarEventInitialInvites packet = new CalendarEventInitialInvites();

            foreach (var member in m_members.Values)
            {
                // not sure if needed, maybe client checks it as well
                if (packet.Invites.Count >= SharedConst.CalendarMaxInvites)
                {
                    Player player = session.GetPlayer();
                    if (player != null)
                        Global.CalendarMgr.SendCalendarCommandResult(player.GetGUID(), CalendarError.InvitesExceeded);
                    return;
                }

                uint level = Player.GetLevelFromDB(member.GetGUID());

                if (member.GetGUID() != session.GetPlayer().GetGUID() && level >= minLevel && level <= maxLevel && member.IsRankNotLower(minRank))
                    packet.Invites.Add(new CalendarEventInitialInviteInfo(member.GetGUID(), (byte)level));
            }

            session.SendPacket(packet);
        }

        public bool AddMember(SQLTransaction trans, ObjectGuid guid, byte rankId = GuildConst.RankNone)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);
            // Player cannot be in guild
            if (player != null)
            {
                if (player.GetGuildId() != 0)
                    return false;
            }
            else if (Player.GetGuildIdFromDB(guid) != 0)
                return false;

            // Remove all player signs from another petitions
            // This will be prevent attempt to join many guilds and corrupt guild data integrity
            Player.RemovePetitionsAndSigns(guid);

            ulong lowguid = guid.GetCounter();

            // If rank was not passed, assign lowest possible rank
            if (rankId == GuildConst.RankNone)
                rankId = _GetLowestRankId();

            Member member = new Member(m_id, guid, rankId);
            string name = "";
            if (player != null)
            {
                m_members[guid] = member;
                player.SetInGuild(m_id);
                player.SetGuildIdInvited(0);
                player.SetGuildRank(rankId);
                player.SetGuildLevel(GetLevel());
                SendLoginInfo(player.GetSession());
                name = player.GetName();
            }
            else
            {
                member.ResetFlags();

                bool ok = false;
                // Player must exist
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHAR_DATA_FOR_GUILD);
                stmt.AddValue(0, lowguid);
                SQLResult result = DB.Characters.Query(stmt);
                if (!result.IsEmpty())
                {
                    name = result.Read<string>(0);
                    member.SetStats(
                        name,
                        result.Read<byte>(1),
                        (Class)result.Read<byte>(2),
                        (Gender)result.Read<byte>(3),
                        result.Read<ushort>(4),
                        result.Read<uint>(5),
                        0);

                    ok = member.CheckStats();
                }

                if (!ok)
                    return false;

                m_members[guid] = member;
            }

            member.SaveToDB(trans);

            _UpdateAccountsNumber();
            _LogEvent(GuildEventLogTypes.JoinGuild, lowguid);

            GuildEventPlayerJoined joinNotificationPacket = new GuildEventPlayerJoined();
            joinNotificationPacket.Guid = guid;
            joinNotificationPacket.Name = name;
            joinNotificationPacket.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            BroadcastPacket(joinNotificationPacket);

            Global.GuildFinderMgr.RemoveAllMembershipRequestsFromPlayer(guid);

            // Call scripts if member was succesfully added (and stored to database)
            Global.ScriptMgr.OnGuildAddMember(this, player, rankId);

            return true;
        }

        public void DeleteMember(SQLTransaction trans, ObjectGuid guid, bool isDisbanding = false, bool isKicked = false, bool canDeleteGuild = false)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            // Guild master can be deleted when loading guild and guid doesn't exist in characters table
            // or when he is removed from guild by gm command
            if (m_leaderGuid == guid && !isDisbanding)
            {
                Member oldLeader = null;
                Member newLeader = null;
                foreach (var member in m_members)
                {
                    if (member.Key == guid)
                        oldLeader = member.Value;
                    else if (newLeader == null || newLeader.GetRankId() > member.Value.GetRankId())
                        newLeader = member.Value;
                }

                if (newLeader == null)
                {
                    Disband();
                    return;
                }

                _SetLeader(trans, newLeader);

                // If leader does not exist (at guild loading with deleted leader) do not send broadcasts
                if (oldLeader != null)
                {
                    SendEventNewLeader(newLeader, oldLeader, true);
                    SendEventPlayerLeft(player);
                }
            }
            // Call script on remove before member is actually removed from guild (and database)
            Global.ScriptMgr.OnGuildRemoveMember(this, player, isDisbanding, isKicked);

            m_members.Remove(guid);

            // If player not online data in data field will be loaded from guild tabs no need to update it !!
            if (player != null)
            {
                player.SetInGuild(0);
                player.SetGuildRank(0);
                player.SetGuildLevel(0);

                foreach (var entry in CliDB.GuildPerkSpellsStorage.Values)
                        player.RemoveSpell(entry.SpellID, false, false);
            }

            _DeleteMemberFromDB(trans, guid.GetCounter());
            if (!isDisbanding)
                _UpdateAccountsNumber();
        }

        public bool ChangeMemberRank(SQLTransaction trans, ObjectGuid guid, byte newRank)
        {
            if (newRank <= _GetLowestRankId())                    // Validate rank (allow only existing ranks)
            {
                Member member = GetMember(guid);
                if (member != null)
                {
                    member.ChangeRank(trans, newRank);
                    return true;
                }
            }
            return false;
        }

        public bool IsMember(ObjectGuid guid)
        {
            return m_members.ContainsKey(guid);
        }

        public void SwapItems(Player player, byte tabId, byte slotId, byte destTabId, byte destSlotId, uint splitedAmount)
        {
            if (tabId >= _GetPurchasedTabsSize() || slotId >= GuildConst.MaxBankSlots ||
                destTabId >= _GetPurchasedTabsSize() || destSlotId >= GuildConst.MaxBankSlots)
                return;

            if (tabId == destTabId && slotId == destSlotId)
                return;

            BankMoveItemData from = new BankMoveItemData(this, player, tabId, slotId);
            BankMoveItemData to = new BankMoveItemData(this, player, destTabId, destSlotId);
            _MoveItems(from, to, splitedAmount);
        }

        public void SwapItemsWithInventory(Player player, bool toChar, byte tabId, byte slotId, byte playerBag, byte playerSlotId, uint splitedAmount)
        {
            if ((slotId >= GuildConst.MaxBankSlots && slotId != ItemConst.NullSlot) || tabId >= _GetPurchasedTabsSize())
                return;

            BankMoveItemData bankData = new BankMoveItemData(this, player, tabId, slotId);
            PlayerMoveItemData charData = new PlayerMoveItemData(this, player, playerBag, playerSlotId);
            if (toChar)
                _MoveItems(bankData, charData, splitedAmount);
            else
                _MoveItems(charData, bankData, splitedAmount);
        }

        public void SetBankTabText(byte tabId, string text)
        {
            BankTab pTab = GetBankTab(tabId);
            if (pTab != null)
            {
                pTab.SetText(text);
                pTab.SendText(this);

                GuildEventTabTextChanged eventPacket = new GuildEventTabTextChanged();
                eventPacket.Tab = tabId;
                BroadcastPacket(eventPacket);
            }
        }

        // Private methods
        void _CreateLogHolders()
        {
            m_eventLog = new LogHolder(100);
            m_newsLog = new LogHolder(250);
            for (byte tabId = 0; tabId <= GuildConst.MaxBankTabs; ++tabId)
                m_bankEventLog[tabId] = new LogHolder(25);
        }

        void _CreateNewBankTab()
        {
            byte tabId = _GetPurchasedTabsSize();                      // Next free id
            m_bankTabs.Add(new BankTab(m_id, tabId));

            SQLTransaction trans = new SQLTransaction();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_TAB);
            stmt.AddValue(0, m_id);
            stmt.AddValue(1, tabId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_TAB);
            stmt.AddValue(0, m_id);
            stmt.AddValue(1, tabId);
            trans.Append(stmt);

            ++tabId;
            foreach (var rank in m_ranks)
                rank.CreateMissingTabsIfNeeded(tabId, trans, false);

            DB.Characters.CommitTransaction(trans);
        }

        void _CreateDefaultGuildRanks(SQLTransaction trans, LocaleConstant loc = LocaleConstant.enUS)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
            stmt.AddValue(0, m_id);
            trans.Append(stmt);

            _CreateRank(trans, Global.ObjectMgr.GetCypherString( CypherStrings.GuildMaster, loc), GuildRankRights.All);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildOfficer, loc), GuildRankRights.All);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildVeteran, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildMember, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildInitiate, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
        }

        bool _CreateRank(SQLTransaction trans, string name, GuildRankRights rights)
        {
            byte newRankId = _GetRanksSize();
            if (newRankId >= GuildConst.MaxRanks)
                return false;

            // Ranks represent sequence 0, 1, 2, ... where 0 means guildmaster
            RankInfo info = new RankInfo(m_id, newRankId, name, rights, 0);
            m_ranks.Add(info);

            bool isInTransaction = trans != null;
            if (!isInTransaction)
                trans = new SQLTransaction();

            info.CreateMissingTabsIfNeeded(_GetPurchasedTabsSize(), trans);
            info.SaveToDB(trans);
            DB.Characters.CommitTransaction(trans);

            if (!isInTransaction)
                DB.Characters.CommitTransaction(trans);

            return true;
        }

        void _UpdateAccountsNumber()
        {
            // We use a set to be sure each element will be unique
            List<uint> accountsIdSet = new List<uint>();
            foreach (var member in m_members.Values)
                accountsIdSet.Add(member.GetAccountId());

            m_accountsNumber = (uint)accountsIdSet.Count;
        }

        bool _IsLeader(Player player)
        {
            if (player.GetGUID() == m_leaderGuid)
                return true;

            Member member = GetMember(player.GetGUID());
            if (member != null)
                return member.IsRank(GuildDefaultRanks.Master);
            return false;
        }

        void _DeleteBankItems(SQLTransaction trans, bool removeItemsFromDB)
        {
            for (byte tabId = 0; tabId < _GetPurchasedTabsSize(); ++tabId)
            {
                m_bankTabs[tabId].Delete(trans, removeItemsFromDB);
                m_bankTabs[tabId] = null;
            }
            m_bankTabs.Clear();
        }

        bool _ModifyBankMoney(SQLTransaction trans, ulong amount, bool add)
        {
            if (add)
                m_bankMoney += amount;
            else
            {
                // Check if there is enough money in bank.
                if (m_bankMoney < amount)
                    return false;
                m_bankMoney -= amount;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_MONEY);
            stmt.AddValue(0, m_bankMoney);
            stmt.AddValue(1, m_id);
            trans.Append(stmt);
            return true;
        }

        void _SetLeader(SQLTransaction trans, Member leader)
        {
            if (leader == null)
                return;

            bool isInTransaction = trans != null;
            if (!isInTransaction)
                trans = new SQLTransaction();

            m_leaderGuid = leader.GetGUID();
            leader.ChangeRank(trans, GuildDefaultRanks.Master);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_LEADER);
            stmt.AddValue(0, m_leaderGuid.GetCounter());
            stmt.AddValue(1, m_id);
            trans.Append(stmt);

            if (!isInTransaction)
                DB.Characters.CommitTransaction(trans);
        }

        void _SetRankBankMoneyPerDay(uint rankId, uint moneyPerDay)
        {
            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
                rankInfo.SetBankMoneyPerDay(moneyPerDay);
        }

        void _SetRankBankTabRightsAndSlots(uint rankId, GuildBankRightsAndSlots rightsAndSlots, bool saveToDB = true)
        {
            if (rightsAndSlots.GetTabId() >= _GetPurchasedTabsSize())
                return;

            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
                rankInfo.SetBankTabSlotsAndRights(rightsAndSlots, saveToDB);
        }

        string _GetRankName(byte rankId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
                return rankInfo.GetName();
            return "<unknown>";
        }

        GuildRankRights _GetRankRights(byte rankId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
                return rankInfo.GetRights();
            return 0;
        }

        uint _GetRankBankMoneyPerDay(byte rankId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
                return rankInfo.GetBankMoneyPerDay();
            return 0;
        }

        int _GetRankBankTabSlotsPerDay(byte rankId, byte tabId)
        {
            if (tabId < _GetPurchasedTabsSize())
            {
                RankInfo rankInfo = GetRankInfo(rankId);
                if (rankInfo != null)
                    return rankInfo.GetBankTabSlotsPerDay(tabId);
            }
            return 0;
        }

        GuildBankRights _GetRankBankTabRights(byte rankId, byte tabId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);
            if (rankInfo != null)
                return rankInfo.GetBankTabRights(tabId);
            return 0;
        }

        int _GetMemberRemainingSlots(Member member, byte tabId)
        {
            if (member != null)
            {
                byte rankId = member.GetRankId();
                if (rankId == GuildDefaultRanks.Master)
                    return Convert.ToInt32(GuildConst.WithdrawSlotUnlimited);
                if ((_GetRankBankTabRights(rankId, tabId) & GuildBankRights.ViewTab) != 0)
                {
                    int remaining = _GetRankBankTabSlotsPerDay(rankId, tabId) - (int)member.GetBankTabWithdrawValue(tabId);
                    if (remaining > 0)
                        return remaining;
                }
            }
            return 0;
        }

        long _GetMemberRemainingMoney(Member member)
        {
            if (member != null)
            {
                byte rankId = member.GetRankId();
                if (rankId == GuildDefaultRanks.Master)
                    return long.MaxValue;

                if ((_GetRankRights(rankId) & (GuildRankRights.WithdrawRepair | GuildRankRights.WithdrawGold)) != 0)
                {
                    long remaining = (long)((_GetRankBankMoneyPerDay(rankId) * MoneyConstants.Gold) - member.GetBankMoneyWithdrawValue());
                    if (remaining > 0)
                        return remaining;
                }
            }
            return 0;
        }

        void _UpdateMemberWithdrawSlots(SQLTransaction trans, ObjectGuid guid, byte tabId)
        {
            Member member = GetMember(guid);
            if (member != null)
                member.UpdateBankTabWithdrawValue(trans, tabId, 1);
        }

        bool _MemberHasTabRights(ObjectGuid guid, byte tabId, GuildBankRights rights)
        {
            Member member = GetMember(guid);
            if (member != null)
            {
                // Leader always has full rights
                if (member.IsRank(GuildDefaultRanks.Master) || m_leaderGuid == guid)
                    return true;
                return (_GetRankBankTabRights(member.GetRankId(), tabId) & rights) == rights;
            }
            return false;
        }

        void _LogEvent(GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2 = 0, byte newRank = 0)
        {
            SQLTransaction trans = new SQLTransaction();
            m_eventLog.AddEvent(trans, new EventLogEntry(m_id, m_eventLog.GetNextGUID(), eventType, playerGuid1, playerGuid2, newRank));
            DB.Characters.CommitTransaction(trans);

            Global.ScriptMgr.OnGuildEvent(this, (byte)eventType, playerGuid1, playerGuid2, newRank);
        }

        void _LogBankEvent(SQLTransaction trans, GuildBankEventLogTypes eventType, byte tabId, ulong lowguid, uint itemOrMoney, ushort itemStackCount = 0, byte destTabId = 0)
        {
            if (tabId > GuildConst.MaxBankTabs)
                return;

            // not logging moves within the same tab
            if (eventType == GuildBankEventLogTypes.MoveItem && tabId == destTabId)
                return;

            byte dbTabId = tabId;
            if (BankEventLogEntry.IsMoneyEvent(eventType))
            {
                tabId = GuildConst.MaxBankTabs;
                dbTabId = GuildConst.BankMoneyLogsTab;
            }
            LogHolder pLog = m_bankEventLog[tabId];
            pLog.AddEvent(trans, new BankEventLogEntry(m_id, pLog.GetNextGUID(), eventType, dbTabId, lowguid, itemOrMoney, itemStackCount, destTabId));

            Global.ScriptMgr.OnGuildBankEvent(this, (byte)eventType, tabId, lowguid, itemOrMoney, itemStackCount, destTabId);
        }

        Item _GetItem(byte tabId, byte slotId)
        {
            BankTab tab = GetBankTab(tabId);
            if (tab != null)
                return tab.GetItem(slotId);
            return null;
        }

        void _RemoveItem(SQLTransaction trans, byte tabId, byte slotId)
        {
            BankTab pTab = GetBankTab(tabId);
            if (pTab != null)
                pTab.SetItem(trans, slotId, null);
        }

        void _MoveItems(MoveItemData pSrc, MoveItemData pDest, uint splitedAmount)
        {
            // 1. Initialize source item
            if (!pSrc.InitItem())
                return; // No source item

            // 2. Check source item
            if (!pSrc.CheckItem(ref splitedAmount))
                return; // Source item or splited amount is invalid

            // 3. Check destination rights
            if (!pDest.HasStoreRights(pSrc))
                return; // Player has no rights to store item in destination

            // 4. Check source withdraw rights
            if (!pSrc.HasWithdrawRights(pDest))
                return; // Player has no rights to withdraw items from source

            // 5. Check split
            if (splitedAmount != 0)
            {
                // 5.1. Clone source item
                if (!pSrc.CloneItem(splitedAmount))
                    return; // Item could not be cloned

                // 5.2. Move splited item to destination
                _DoItemsMove(pSrc, pDest, true, splitedAmount);
            }
            else // 6. No split
            {
                // 6.1. Try to merge items in destination (pDest.GetItem() == NULL)
                if (!_DoItemsMove(pSrc, pDest, false)) // Item could not be merged
                {
                    // 6.2. Try to swap items
                    // 6.2.1. Initialize destination item
                    if (!pDest.InitItem())
                        return;

                    // 6.2.2. Check rights to store item in source (opposite direction)
                    if (!pSrc.HasStoreRights(pDest))
                        return; // Player has no rights to store item in source (opposite direction)

                    if (!pDest.HasWithdrawRights(pSrc))
                        return; // Player has no rights to withdraw item from destination (opposite direction)

                    // 6.2.3. Swap items (pDest.GetItem() != NULL)
                    _DoItemsMove(pSrc, pDest, true);
                }
            }
            // 7. Send changes
            _SendBankContentUpdate(pSrc, pDest);
        }

        bool _DoItemsMove(MoveItemData pSrc, MoveItemData pDest, bool sendError, uint splitedAmount = 0)
        {
            Item pDestItem = pDest.GetItem();
            bool swap = (pDestItem != null);

            Item pSrcItem = pSrc.GetItem(splitedAmount != 0);
            // 1. Can store source item in destination
            if (!pDest.CanStore(pSrcItem, swap, sendError))
                return false;

            // 2. Can store destination item in source
            if (swap)
                if (!pSrc.CanStore(pDestItem, true, true))
                    return false;

            // GM LOG (@todo move to scripts)
            pDest.LogAction(pSrc);
            if (swap)
                pSrc.LogAction(pDest);

            SQLTransaction trans = new SQLTransaction();
            // 3. Log bank events
            pDest.LogBankEvent(trans, pSrc, pSrcItem.GetCount());
            if (swap)
                pSrc.LogBankEvent(trans, pDest, pDestItem.GetCount());

            // 4. Remove item from source
            pSrc.RemoveItem(trans, pDest, splitedAmount);

            // 5. Remove item from destination
            if (swap)
                pDest.RemoveItem(trans, pSrc);

            // 6. Store item in destination
            pDest.StoreItem(trans, pSrcItem);

            // 7. Store item in source
            if (swap)
                pSrc.StoreItem(trans, pDestItem);

            DB.Characters.CommitTransaction(trans);
            return true;
        }

        void _SendBankContentUpdate(MoveItemData pSrc, MoveItemData pDest)
        {
            Cypher.Assert(pSrc.IsBank() || pDest.IsBank());

            byte tabId = 0;
            List<byte> slots = new List<byte>();
            if (pSrc.IsBank()) // B .
            {
                tabId = pSrc.GetContainer();
                slots.Insert(0, pSrc.GetSlotId());
                if (pDest.IsBank()) // B . B
                {
                    // Same tab - add destination slots to collection
                    if (pDest.GetContainer() == pSrc.GetContainer())
                        pDest.CopySlots(slots);
                    else // Different tabs - send second message
                    {
                        List<byte> destSlots = new List<byte>();
                        pDest.CopySlots(destSlots);
                        _SendBankContentUpdate(pDest.GetContainer(), destSlots);
                    }
                }
            }
            else if (pDest.IsBank()) // C . B
            {
                tabId = pDest.GetContainer();
                pDest.CopySlots(slots);
            }

            _SendBankContentUpdate(tabId, slots);
        }

        void _SendBankContentUpdate(byte tabId, List<byte> slots)
        {
            BankTab tab = GetBankTab(tabId);
            if (tab != null)
            {
                GuildBankQueryResults packet = new GuildBankQueryResults();
                packet.FullUpdate = true;          // @todo
                packet.Tab = tabId;
                packet.Money = m_bankMoney;

                foreach (var slot in slots)
                {
                    Item tabItem = tab.GetItem(slot);

                    GuildBankItemInfo itemInfo = new GuildBankItemInfo();

                    itemInfo.Slot = slot;
                    itemInfo.Item.ItemID = tabItem ? tabItem.GetEntry() : 0;
                    itemInfo.Count = (int)(tabItem ? tabItem.GetCount() : 0);
                    itemInfo.Charges = tabItem ? Math.Abs(tabItem.GetSpellCharges()) : 0;
                    itemInfo.OnUseEnchantmentID = 0/*int32(tabItem->GetItemSuffixFactor())*/;
                    itemInfo.Flags = 0;
                    itemInfo.Locked = false;

                    if (tabItem != null)
                    {
                        byte i = 0;
                        foreach (ItemDynamicFieldGems gemData in tabItem.GetGems())
                        {
                            if (gemData.ItemId != 0)
                            {
                                ItemGemData gem = new ItemGemData();
                                gem.Slot = i;
                                gem.Item = new ItemInstance(gemData);
                                itemInfo.SocketEnchant.Add(gem);
                            }
                            ++i;
                        }
                    }

                    packet.ItemInfo.Add(itemInfo);
                }

                foreach (var member in m_members.Values)
                {
                    if (_MemberHasTabRights(member.GetGUID(), tabId, GuildBankRights.ViewTab))
                    {
                        Player player = member.FindPlayer();
                        if (player != null)
                        {
                            packet.WithdrawalsRemaining = _GetMemberRemainingSlots(member, tabId);
                            player.SendPacket(packet);
                        }
                    }
                }
            }
        }

        public void SendBankList(WorldSession session, byte tabId, bool fullUpdate)
        {
            Member member = GetMember(session.GetPlayer().GetGUID());
            if (member == null) // Shouldn't happen, just in case
                return;

            GuildBankQueryResults packet = new GuildBankQueryResults();

            packet.Money = m_bankMoney;
            packet.WithdrawalsRemaining = _GetMemberRemainingSlots(member, tabId);
            packet.Tab = tabId;
            packet.FullUpdate = fullUpdate;

            // TabInfo
            if (fullUpdate)
            {
                for (byte i = 0; i < _GetPurchasedTabsSize(); ++i)
                {
                    GuildBankTabInfo tabInfo;
                    tabInfo.TabIndex = i;
                    tabInfo.Name = m_bankTabs[i].GetName();
                    tabInfo.Icon = m_bankTabs[i].GetIcon();
                    packet.TabInfo.Add(tabInfo);
                }

            }

            if (fullUpdate && _MemberHasTabRights(session.GetPlayer().GetGUID(), tabId, GuildBankRights.ViewTab))
            {
                BankTab tab = GetBankTab(tabId);
                if (tab != null)
                {
                    for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
                    {
                        Item tabItem = tab.GetItem(slotId);
                        if (tabItem)
                        {
                            GuildBankItemInfo itemInfo = new GuildBankItemInfo();

                            itemInfo.Slot = slotId;
                            itemInfo.Item.ItemID = tabItem.GetEntry();
                            itemInfo.Count = (int)tabItem.GetCount();
                            itemInfo.Charges = Math.Abs(tabItem.GetSpellCharges());
                            itemInfo.EnchantmentID = tabItem.GetItemRandomPropertyId(); // verify that...
                            itemInfo.OnUseEnchantmentID = 0/*int32(tabItem->GetItemSuffixFactor())*/;
                            itemInfo.Flags = 0;

                            byte i = 0;
                            foreach (ItemDynamicFieldGems gemData in tabItem.GetGems())
                            {
                                if (gemData.ItemId != 0)
                                {
                                    ItemGemData gem = new ItemGemData();
                                    gem.Slot = i;
                                    gem.Item = new ItemInstance(gemData);
                                    itemInfo.SocketEnchant.Add(gem);
                                }
                                ++i;
                            }

                            itemInfo.Locked = false;

                            packet.ItemInfo.Add(itemInfo);
                        }
                    }
                }
            }

            session.SendPacket(packet);
        }

        void SendGuildRanksUpdate(ObjectGuid setterGuid, ObjectGuid targetGuid, uint rank)
        {
            Member member = GetMember(targetGuid);
            Cypher.Assert(member != null);

            GuildSendRankChange rankChange = new GuildSendRankChange();
            rankChange.Officer = setterGuid;
            rankChange.Other = targetGuid;
            rankChange.RankID = rank;
            rankChange.Promote = (rank < member.GetRankId());
            BroadcastPacket(rankChange);

            member.ChangeRank(null, rank);

            Log.outDebug(LogFilter.Network, "SMSG_GUILD_RANKS_UPDATE [Broadcast] Target: {0}, Issuer: {1}, RankId: {2}", targetGuid.ToString(), setterGuid.ToString(), rank);
        }

        public void ResetTimes(bool weekly)
        {
            foreach (var member in m_members.Values)
            {
                member.ResetValues(weekly);
                Player player = member.FindPlayer();
                if (player != null)
                {
                    // tells the client to request bank withdrawal limit
                    player.SendPacket(new GuildMemberDailyReset());
                }
            }
        }

        public void AddGuildNews(GuildNews type, ObjectGuid guid, uint flags, uint value)
        {
            NewsLogEntry news = new NewsLogEntry(m_id, m_newsLog.GetNextGUID(), type, guid, flags, value);

            SQLTransaction trans = new SQLTransaction();
            m_newsLog.AddEvent(trans, news);
            DB.Characters.CommitTransaction(trans);

            GuildNewsPkt newsPacket = new GuildNewsPkt();
            news.WritePacket(newsPacket);
            BroadcastPacket(newsPacket);
        }

        bool HasAchieved(uint achievementId)
        {
            return m_achievementSys.HasAchieved(achievementId);
        }

        public void UpdateCriteria(CriteriaTypes type, ulong miscValue1, ulong miscValue2, ulong miscValue3, Unit unit, Player player)
        {
            m_achievementSys.UpdateCriteria(type, miscValue1, miscValue2, miscValue3, unit, player);
        }

        public void HandleNewsSetSticky(WorldSession session, uint newsId, bool sticky)
        {
            var news = (NewsLogEntry)m_newsLog.GetGuildLog().Find(p => p.GetGUID() == newsId);

            if (news == null)
            {
                Log.outDebug(LogFilter.Guild, "HandleNewsSetSticky: [{0}] requested unknown newsId {1} - Sticky: {2}",
                    session.GetPlayerInfo(), newsId, sticky);
                return;
            }

            news.SetSticky(sticky);

            Log.outDebug(LogFilter.Guild, "HandleNewsSetSticky: [{0}] chenged newsId {1} sticky to {2}", session.GetPlayerInfo(), newsId, sticky);

            GuildNewsPkt newsPacket = new GuildNewsPkt();
            news.WritePacket(newsPacket);
            session.SendPacket(newsPacket);
        }

        public ulong GetId() { return m_id; }
        public ObjectGuid GetGUID() { return ObjectGuid.Create(HighGuid.Guild, m_id); }
        public ObjectGuid GetLeaderGUID() { return m_leaderGuid; }
        public string GetName() { return m_name; }
        public string GetMOTD() { return m_motd; }
        public string GetInfo() { return m_info; }
        public long GetCreatedDate() { return m_createdDate; }
        public ulong GetBankMoney() { return m_bankMoney; }

        public void BroadcastWorker(IDoWork<Player> _do, Player except = null)
        {
            foreach (var member in m_members.Values)
            {
                Player player = member.FindPlayer();
                if (player != null)
                    if (player != except)
                        _do.Invoke(player);
            }
        }

        public int GetMembersCount() { return m_members.Count; }

        public GuildAchievementMgr GetAchievementMgr() { return m_achievementSys; }

        // Pre-6.x guild leveling
        public byte GetLevel() { return GuildConst.OldMaxLevel; }

        public EmblemInfo GetEmblemInfo() { return m_emblemInfo; }

        byte _GetRanksSize() { return (byte)m_ranks.Count; }

        RankInfo GetRankInfo(uint rankId) { return rankId < _GetRanksSize() ? m_ranks[(int)rankId] : null; }

        bool _HasRankRight(Player player, GuildRankRights right)
        {
            if (player != null)
            {
                Member member = GetMember(player.GetGUID());
                if (member != null)
                    return (_GetRankRights(member.GetRankId()) & right) != GuildRankRights.None;
                return false;
            }
            return false;
        }

        byte _GetLowestRankId() { return (byte)(m_ranks.Count - 1); }

        byte _GetPurchasedTabsSize() { return (byte)m_bankTabs.Count; }

        BankTab GetBankTab(byte tabId) { return tabId < m_bankTabs.Count ? m_bankTabs[tabId] : null; }

        public Member GetMember(ObjectGuid guid)
        {
            return m_members.LookupByKey(guid);
        }

        public Member GetMember(string name)
        {
            foreach (var member in m_members.Values)
                if (member.GetName() == name)
                    return member;

            return null;
        }

        void _DeleteMemberFromDB(SQLTransaction trans, ulong lowguid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBER);
            stmt.AddValue(0, lowguid);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        ulong GetGuildBankTabPrice(byte tabId)
        {
            // these prices are in gold units, not copper
            switch (tabId)
            {
                case 0: return 100;
                case 1: return 250;
                case 2: return 500;
                case 3: return 1000;
                case 4: return 2500;
                case 5: return 5000;
                default: return 0;
            }
        }

        public static void SendCommandResult(WorldSession session, GuildCommandType type, GuildCommandError errCode, string param = "")
        {
            GuildCommandResult resultPacket = new GuildCommandResult();
            resultPacket.Command = type;
            resultPacket.Result = errCode;
            resultPacket.Name = param;
            session.SendPacket(resultPacket);
        }

        public static void SendSaveEmblemResult(WorldSession session, GuildEmblemError errCode)
        {
            PlayerSaveGuildEmblem saveResponse = new PlayerSaveGuildEmblem();
            saveResponse.Error = errCode;
            session.SendPacket(saveResponse);
        }

        #region Fields
        ulong m_id;
        string m_name;
        ObjectGuid m_leaderGuid;
        string m_motd;
        string m_info;
        long m_createdDate;

        EmblemInfo m_emblemInfo = new EmblemInfo();
        uint m_accountsNumber;
        ulong m_bankMoney;

        List<RankInfo> m_ranks = new List<RankInfo>();
        Dictionary<ObjectGuid, Member> m_members = new Dictionary<ObjectGuid, Member>();
        List<BankTab> m_bankTabs = new List<BankTab>();

        // These are actually ordered lists. The first element is the oldest entry.
        LogHolder m_eventLog;
        LogHolder[] m_bankEventLog = new LogHolder[GuildConst.MaxBankTabs + 1];
        LogHolder m_newsLog;
        GuildAchievementMgr m_achievementSys;
        #endregion

        public static implicit operator bool(Guild guild)
        {
            return guild != null;
        }

        #region Classes
        public class Member
        {
            public Member(ulong guildId, ObjectGuid guid, byte rankId)
            {
                m_guildId = guildId;
                m_guid = guid;
                m_zoneId = 0;
                m_level = 0;
                m_class = 0;
                m_flags = GuildMemberFlags.None;
                m_logoutTime = (ulong)Time.UnixTime;
                m_accountId = 0;
                m_rankId = rankId;
                m_achievementPoints = 0;
                m_totalActivity = 0;
                m_weekActivity = 0;
                m_totalReputation = 0;
                m_weekReputation = 0;
            }

            public void SetStats(Player player)
            {
                m_name = player.GetName();
                m_level = (byte)player.getLevel();
                m_class = player.GetClass();
                _gender = (Gender)player.GetByteValue(PlayerFields.Bytes3, PlayerFieldOffsets.Bytes3OffsetGender);
                m_zoneId = player.GetZoneId();
                m_accountId = player.GetSession().GetAccountId();
                m_achievementPoints = player.GetAchievementPoints();
            }

            public void SetStats(string name, byte level, Class _class, Gender gender, uint zoneId, uint accountId, uint reputation)
            {
                m_name = name;
                m_level = level;
                m_class = _class;
                _gender = gender;
                m_zoneId = zoneId;
                m_accountId = accountId;
                m_totalReputation = reputation;
            }

            public void SetPublicNote(string publicNote)
            {
                if (m_publicNote == publicNote)
                    return;

                m_publicNote = publicNote;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_PNOTE);
                stmt.AddValue(0, publicNote);
                stmt.AddValue(1, m_guid.GetCounter());
                DB.Characters.Execute(stmt);
            }

            public void SetOfficerNote(string officerNote)
            {
                if (m_officerNote == officerNote)
                    return;

                m_officerNote = officerNote;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_OFFNOTE);
                stmt.AddValue(0, officerNote);
                stmt.AddValue(1, m_guid.GetCounter());
                DB.Characters.Execute(stmt);
            }

            public void ChangeRank(SQLTransaction trans, uint newRank)
            {
                m_rankId = (byte)newRank;

                // Update rank information in player's field, if he is online.
                Player player = FindConnectedPlayer();
                if (player != null)
                    player.SetGuildRank((byte)newRank);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_RANK);
                stmt.AddValue(0, newRank);
                stmt.AddValue(1, m_guid.GetCounter());
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void SaveToDB(SQLTransaction trans)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER);
                stmt.AddValue(0, m_guildId);
                stmt.AddValue(1, m_guid.GetCounter());
                stmt.AddValue(2, m_rankId);
                stmt.AddValue(3, m_publicNote);
                stmt.AddValue(4, m_officerNote);
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public bool LoadFromDB(SQLFields field)
            {
                m_publicNote = field.Read<string>(3);
                m_officerNote = field.Read<string>(4);

                for (byte i = 0; i < GuildConst.MaxBankTabs; ++i)
                    m_bankWithdraw[i] = field.Read<uint>(5 + i);

                m_bankWithdrawMoney = field.Read<ulong>(13);

                SetStats(field.Read<string>(14),
                         field.Read<byte>(15),                          // characters.level
                         (Class)field.Read<byte>(16),                   // characters.class
                         (Gender)field.Read<byte>(17),                  // characters.gender
                         field.Read<ushort>(18),                        // characters.zone
                         field.Read<uint>(19),                          // characters.account
                         0);
                m_logoutTime = field.Read<uint>(20);                    // characters.logout_time
                m_totalActivity = 0;
                m_weekActivity = 0;
                m_weekReputation = 0;

                if (!CheckStats())
                    return false;

                if (m_zoneId == 0)
                {
                    Log.outError(LogFilter.Guild, "Player ({0}) has broken zone-data", m_guid.ToString());
                    m_zoneId = Player.GetZoneIdFromDB(m_guid);
                }
                ResetFlags();
                return true;
            }

            public bool CheckStats()
            {
                if (m_level < 1)
                {
                    Log.outError(LogFilter.Guild, "Player ({0}) has a broken data in field `characters`.`level`, deleting him from guild!", m_guid.ToString());
                    return false;
                }

                if (m_class < Class.Warrior || m_class >= Class.Max)
                {
                    Log.outError(LogFilter.Guild, "Player ({0}) has a broken data in field `characters`.`class`, deleting him from guild!", m_guid.ToString());
                    return false;
                }
                return true;
            }

            public float GetInactiveDays()
            {
                if (IsOnline())
                    return 0.0f;
                return (float)((Time.UnixTime - (long)GetLogoutTime()) / (float)Time.Day);
            }

            // Decreases amount of slots left for today.
            public void UpdateBankTabWithdrawValue(SQLTransaction trans, byte tabId, uint amount)
            {
                m_bankWithdraw[tabId] += amount;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_TABS);
                stmt.AddValue(0, m_guid.GetCounter());
                for (byte i = 0; i < GuildConst.MaxBankTabs;)
                {
                    uint withdraw = m_bankWithdraw[i++];
                    stmt.AddValue(i, withdraw);
                }

                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            // Decreases amount of money left for today.
            public void UpdateBankMoneyWithdrawValue(SQLTransaction trans, ulong amount)
            {
                m_bankWithdrawMoney += amount;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_MONEY);
                stmt.AddValue(0, m_guid.GetCounter());
                stmt.AddValue(1, m_bankWithdrawMoney);
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void ResetValues(bool weekly = false)
            {
                for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
                    m_bankWithdraw[tabId] = 0;

                m_bankWithdrawMoney = 0;

                if (weekly)
                {
                    m_weekActivity = 0;
                    m_weekReputation = 0;
                }
            }

            public void SetZoneId(uint id) { m_zoneId = id; }

            public void SetAchievementPoints(uint val) { m_achievementPoints = val; }

            public void SetLevel(uint var) { m_level = (byte)var; }

            public void AddFlag(GuildMemberFlags var) { m_flags |= var; }

            public void RemoveFlag(GuildMemberFlags var) { m_flags &= ~var; }

            public void ResetFlags() { m_flags = GuildMemberFlags.None; }

            public ObjectGuid GetGUID() { return m_guid; }
            public string GetName() { return m_name; }
            public uint GetAccountId() { return m_accountId; }
            public byte GetRankId() { return m_rankId; }
            public ulong GetLogoutTime() { return m_logoutTime; }
            public string GetPublicNote() { return m_publicNote; }
            public string GetOfficerNote() { return m_officerNote; }
            public Class GetClass() { return m_class; }
            public Gender GetGender() { return _gender; }
            public byte GetLevel() { return m_level; }
            public GuildMemberFlags GetFlags() { return m_flags; }
            public uint GetZoneId() { return m_zoneId; }
            public uint GetAchievementPoints() { return m_achievementPoints; }
            public ulong GetTotalActivity() { return m_totalActivity; }
            public ulong GetWeekActivity() { return m_weekActivity; }
            public uint GetTotalReputation() { return m_totalReputation; }
            public uint GetWeekReputation() { return m_weekReputation; }

            public List<uint> GetTrackedCriteriaIds() { return m_trackedCriteriaIds; }
            public void SetTrackedCriteriaIds(List<uint> criteriaIds) { m_trackedCriteriaIds = criteriaIds; }
            public bool IsTrackingCriteriaId(uint criteriaId) { return m_trackedCriteriaIds.Contains(criteriaId); }
            public bool IsOnline() { return m_flags.HasAnyFlag(GuildMemberFlags.Online); }

            public void UpdateLogoutTime() { m_logoutTime = (ulong)Time.UnixTime; }
            public bool IsRank(byte rankId) { return m_rankId == rankId; }
            public bool IsRankNotLower(uint rankId) { return m_rankId <= rankId; }
            public bool IsSamePlayer(ObjectGuid guid) { return m_guid == guid; }

            public uint GetBankTabWithdrawValue(byte tabId) { return m_bankWithdraw[tabId]; }
            public ulong GetBankMoneyWithdrawValue() { return m_bankWithdrawMoney; }

            public Player FindPlayer() { return Global.ObjAccessor.FindPlayer(m_guid); }
            Player FindConnectedPlayer() { return Global.ObjAccessor.FindConnectedPlayer(m_guid); }

            #region Fields
            ulong m_guildId;
            ObjectGuid m_guid;
            string m_name;
            uint m_zoneId;
            byte m_level;
            Class m_class;
            Gender _gender;
            GuildMemberFlags m_flags;
            ulong m_logoutTime;
            uint m_accountId;
            byte m_rankId;
            string m_publicNote = "";
            string m_officerNote = "";

            List<uint> m_trackedCriteriaIds = new List<uint>();

            uint[] m_bankWithdraw = new uint[GuildConst.MaxBankTabs];
            ulong m_bankWithdrawMoney;
            uint m_achievementPoints;
            ulong m_totalActivity;
            ulong m_weekActivity;
            uint m_totalReputation;
            uint m_weekReputation;
            #endregion
        }

        public class LogEntry
        {
            public LogEntry(ulong guildId, uint guid)
            {
                m_guildId = guildId;
                m_guid = guid;
                m_timestamp = Time.UnixTime;
            }

            public LogEntry(ulong guildId, uint guid, long timestamp)
            {
                m_guildId = guildId;
                m_guid = guid;
                m_timestamp = timestamp;
            }

            public uint GetGUID() { return m_guid; }
            public long GetTimestamp() { return m_timestamp; }

            public virtual void SaveToDB(SQLTransaction trans) { }

            public ulong m_guildId;
            public uint m_guid;
            public long m_timestamp;
        }

        public class EventLogEntry : LogEntry
        {
            public EventLogEntry(ulong guildId, uint guid, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank)
                : base(guildId, guid)
            {
                m_eventType = eventType;
                m_playerGuid1 = playerGuid1;
                m_playerGuid2 = playerGuid2;
                m_newRank = newRank;
            }

            public EventLogEntry(ulong guildId, uint guid, long timestamp, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank)
                : base(guildId, guid, timestamp)
            {
                m_eventType = eventType;
                m_playerGuid1 = playerGuid1;
                m_playerGuid2 = playerGuid2;
                m_newRank = newRank;
            }

            public override void SaveToDB(SQLTransaction trans)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG);
                stmt.AddValue(0, m_guildId);
                stmt.AddValue(1, m_guid);
                trans.Append(stmt);

                byte index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_EVENTLOG);
                stmt.AddValue(index, m_guildId);
                stmt.AddValue(++index, m_guid);
                stmt.AddValue(++index, m_eventType);
                stmt.AddValue(++index, m_playerGuid1);
                stmt.AddValue(++index, m_playerGuid2);
                stmt.AddValue(++index, m_newRank);
                stmt.AddValue(++index, m_timestamp);
                trans.Append(stmt);
            }

            public void WritePacket(GuildEventLogQueryResults packet)
            {
                ObjectGuid playerGUID = ObjectGuid.Create(HighGuid.Player, m_playerGuid1);
                ObjectGuid otherGUID = ObjectGuid.Create(HighGuid.Player, m_playerGuid2);

                GuildEventEntry eventEntry = new GuildEventEntry();
                eventEntry.PlayerGUID = playerGUID;
                eventEntry.OtherGUID = otherGUID;
                eventEntry.TransactionType = (byte)m_eventType;
                eventEntry.TransactionDate = (uint)(Time.UnixTime - m_timestamp);
                eventEntry.RankID = m_newRank;
                packet.Entry.Add(eventEntry);
            }

            GuildEventLogTypes m_eventType;
            ulong m_playerGuid1;
            ulong m_playerGuid2;
            byte m_newRank;
        }

        public class BankEventLogEntry : LogEntry
        {
            public BankEventLogEntry(ulong guildId, uint guid, GuildBankEventLogTypes eventType, byte tabId, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId)
                : base(guildId, guid)
            {
                m_eventType = eventType;
                m_bankTabId = tabId;
                m_playerGuid = playerGuid;
                m_itemOrMoney = itemOrMoney;
                m_itemStackCount = itemStackCount;
                m_destTabId = destTabId;
            }

            public BankEventLogEntry(ulong guildId, uint guid, long timestamp, byte tabId, GuildBankEventLogTypes eventType, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId)
                : base(guildId, guid, timestamp)
            {
                m_eventType = eventType;
                m_bankTabId = tabId;
                m_playerGuid = playerGuid;
                m_itemOrMoney = itemOrMoney;
                m_itemStackCount = itemStackCount;
                m_destTabId = destTabId;
            }

            public static bool IsMoneyEvent(GuildBankEventLogTypes eventType)
            {
                return
                    eventType == GuildBankEventLogTypes.DepositMoney ||
                    eventType == GuildBankEventLogTypes.WithdrawMoney ||
                    eventType == GuildBankEventLogTypes.RepairMoney ||
                    eventType == GuildBankEventLogTypes.CashFlowDeposit;
            }

            bool IsMoneyEvent()
            {
                return IsMoneyEvent(m_eventType);
            }

            public override void SaveToDB(SQLTransaction trans)
            {
                byte index = 0;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG);
                stmt.AddValue(index, m_guildId);
                stmt.AddValue(++index, m_guid);
                stmt.AddValue(++index, m_bankTabId);
                trans.Append(stmt);

                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_EVENTLOG);
                stmt.AddValue(index, m_guildId);
                stmt.AddValue(++index, m_guid);
                stmt.AddValue(++index, m_bankTabId);
                stmt.AddValue(++index, m_eventType);
                stmt.AddValue(++index, m_playerGuid);
                stmt.AddValue(++index, m_itemOrMoney);
                stmt.AddValue(++index, m_itemStackCount);
                stmt.AddValue(++index, m_destTabId);
                stmt.AddValue(++index, m_timestamp);
                trans.Append(stmt);
            }

            public void WritePacket(GuildBankLogQueryResults packet)
            {
                ObjectGuid logGuid = ObjectGuid.Create(HighGuid.Player, m_playerGuid);

                bool hasItem = m_eventType == GuildBankEventLogTypes.DepositItem || m_eventType == GuildBankEventLogTypes.WithdrawItem ||
                               m_eventType == GuildBankEventLogTypes.MoveItem || m_eventType == GuildBankEventLogTypes.MoveItem2;

                bool itemMoved = (m_eventType == GuildBankEventLogTypes.MoveItem || m_eventType == GuildBankEventLogTypes.MoveItem2);

                bool hasStack = (hasItem && m_itemStackCount > 1) || itemMoved;

                GuildBankLogEntry bankLogEntry = new GuildBankLogEntry();
                bankLogEntry.PlayerGUID = logGuid;
                bankLogEntry.TimeOffset = (uint)(Time.UnixTime - m_timestamp);
                bankLogEntry.EntryType = (sbyte)m_eventType;

                if (hasStack)
                    bankLogEntry.Count.Set(m_itemStackCount);

                if (IsMoneyEvent())
                    bankLogEntry.Money.Set(m_itemOrMoney);

                if (hasItem)
                    bankLogEntry.ItemID.Set((int)m_itemOrMoney);

                if (itemMoved)
                    bankLogEntry.OtherTab.Set((sbyte)m_destTabId);

                packet.Entry.Add(bankLogEntry);
            }

            GuildBankEventLogTypes m_eventType;
            byte m_bankTabId;
            ulong m_playerGuid;
            ulong m_itemOrMoney;
            ushort m_itemStackCount;
            byte m_destTabId;
        }

        public class NewsLogEntry : LogEntry
        {
            public NewsLogEntry(ulong guildId, uint guid, GuildNews type, ObjectGuid playerGuid, uint flags, uint value)
                : base(guildId, guid)
            {
                m_type = type;
                m_playerGuid = playerGuid;
                m_flags = (int)flags;
                m_value = value;
            }

            public NewsLogEntry(ulong guildId, uint guid, long timestamp, GuildNews type, ObjectGuid playerGuid, uint flags, uint value)
                : base(guildId, guid, timestamp)
            {
                m_type = type;
                m_playerGuid = playerGuid;
                m_flags = (int)flags;
                m_value = value;
            }

            public GuildNews GetNewsType() { return m_type; }

            public ObjectGuid GetPlayerGuid() { return m_playerGuid; }

            public uint GetValue() { return m_value; }

            public int GetFlags() { return m_flags; }

            public void SetSticky(bool sticky)
            {
                if (sticky)
                    m_flags |= 1;
                else
                    m_flags &= ~1;
            }

            public override void SaveToDB(SQLTransaction trans)
            {
                byte index = 0;
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_NEWS);
                stmt.AddValue(index, m_guildId);
                stmt.AddValue(++index, GetGUID());
                stmt.AddValue(++index, GetNewsType());
                stmt.AddValue(++index, GetPlayerGuid().GetCounter());
                stmt.AddValue(++index, GetFlags());
                stmt.AddValue(++index, GetValue());
                stmt.AddValue(++index, GetTimestamp());
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void WritePacket(GuildNewsPkt newsPacket)
            {
                GuildNewsEvent newsEvent = new GuildNewsEvent();
                newsEvent.Id = (int)GetGUID();
                newsEvent.MemberGuid = GetPlayerGuid();
                newsEvent.CompletedDate = (uint)GetTimestamp();
                newsEvent.Flags = GetFlags();
                newsEvent.Type = (int)GetNewsType();

                //for (public byte i = 0; i < 2; i++)
                //    newsEvent.Data[i] =

                //newsEvent.MemberList.push_back(MemberGuid);

                if (GetNewsType() == GuildNews.ItemLooted || GetNewsType() == GuildNews.ItemCrafted || GetNewsType() == GuildNews.ItemPurchased)
                {
                    ItemInstance itemInstance = new ItemInstance();
                    itemInstance.ItemID = GetValue();
                    newsEvent.Item.Set(itemInstance);
                }

                newsPacket.NewsEvents.Add(newsEvent);
            }

            GuildNews m_type;
            ObjectGuid m_playerGuid;
            int m_flags;
            uint m_value;
        }

        public class LogHolder
        {
            public LogHolder(uint maxRecords)
            {
                m_maxRecords = maxRecords;
                m_nextGUID = GuildConst.EventLogGuidUndefined;
            }

            public byte GetSize() { return (byte)m_log.Count; }

            public bool CanInsert() { return m_log.Count < m_maxRecords; }

            public void LoadEvent(LogEntry entry)
            {
                if (m_nextGUID == GuildConst.EventLogGuidUndefined)
                    m_nextGUID = entry.GetGUID();
                m_log.Insert(0, entry);
            }

            public void AddEvent(SQLTransaction trans, LogEntry entry)
            {
                // Check max records limit
                if (m_log.Count >= m_maxRecords)
                    m_log.RemoveAt(0);

                // Add event to list
                m_log.Add(entry);
                // Save to DB
                entry.SaveToDB(trans);
            }

            public uint GetNextGUID()
            {
                if (m_nextGUID == GuildConst.EventLogGuidUndefined)
                    m_nextGUID = 0;
                else
                    m_nextGUID = (m_nextGUID + 1) % m_maxRecords;
                return m_nextGUID;
            }

            public List<LogEntry> GetGuildLog() { return m_log; }


            List<LogEntry> m_log = new List<LogEntry>();
            uint m_maxRecords;
            uint m_nextGUID;
        }

        public class RankInfo
        {
            public RankInfo(ulong guildId = 0)
            {
                m_guildId = guildId;
                m_rankId = GuildConst.RankNone;
                m_rights = GuildRankRights.None;
                m_bankMoneyPerDay = 0;

                for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
                    m_bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
            }

            public RankInfo(ulong guildId, byte rankId, string name, GuildRankRights rights, uint money)
            {
                m_guildId = guildId;
                m_rankId = rankId;
                m_name = name;
                m_rights = rights;
                m_bankMoneyPerDay = money;

                for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
                    m_bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
            }

            public void LoadFromDB(SQLFields field)
            {
                m_rankId = field.Read<byte>(1);
                m_name = field.Read<string>(2);
                m_rights = (GuildRankRights)field.Read<uint>(3);
                m_bankMoneyPerDay = field.Read<uint>(4);
                if (m_rankId == GuildDefaultRanks.Master)                     // Prevent loss of leader rights
                    m_rights |= GuildRankRights.All;
            }

            public void SaveToDB(SQLTransaction trans)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_RANK);
                stmt.AddValue(0, m_guildId);
                stmt.AddValue(1, m_rankId);
                stmt.AddValue(2, m_name);
                stmt.AddValue(3, m_rights);
                stmt.AddValue(4, m_bankMoneyPerDay);
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void CreateMissingTabsIfNeeded(byte tabs, SQLTransaction trans, bool logOnCreate = false)
            {
                for (byte i = 0; i < tabs; ++i)
                {
                    GuildBankRightsAndSlots rightsAndSlots = m_bankTabRightsAndSlots[i];
                    if (rightsAndSlots.GetTabId() == i)
                        continue;

                    rightsAndSlots.SetTabId(i);
                    if (m_rankId == GuildDefaultRanks.Master)
                        rightsAndSlots.SetGuildMasterValues();

                    if (logOnCreate)
                        Log.outError(LogFilter.Guild, "Guild {0} has broken Tab {1} for rank {2}. Created default tab.", m_guildId, i, m_rankId);

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
                    stmt.AddValue(0, m_guildId);
                    stmt.AddValue(1, i);
                    stmt.AddValue(2, m_rankId);
                    stmt.AddValue(3, rightsAndSlots.GetRights());
                    stmt.AddValue(4, rightsAndSlots.GetSlots());
                    trans.Append(stmt);
                }
            }

            public void SetName(string name)
            {
                if (m_name == name)
                    return;

                m_name = name;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_NAME);
                stmt.AddValue(0, m_name);
                stmt.AddValue(1, m_rankId);
                stmt.AddValue(2, m_guildId);
                DB.Characters.Execute(stmt);
            }

            public void SetRights(GuildRankRights rights)
            {
                if (m_rankId == GuildDefaultRanks.Master)                     // Prevent loss of leader rights
                    rights = GuildRankRights.All;

                if (m_rights == rights)
                    return;

                m_rights = rights;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_RIGHTS);
                stmt.AddValue(0, m_rights);
                stmt.AddValue(1, m_rankId);
                stmt.AddValue(2, m_guildId);
                DB.Characters.Execute(stmt);
            }

            public void SetBankMoneyPerDay(uint money)
            {
                if (m_bankMoneyPerDay == money)
                    return;

                m_bankMoneyPerDay = money;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_BANK_MONEY);
                stmt.AddValue(0, money);
                stmt.AddValue(1, m_rankId);
                stmt.AddValue(2, m_guildId);
                DB.Characters.Execute(stmt);
            }

            public void SetBankTabSlotsAndRights(GuildBankRightsAndSlots rightsAndSlots, bool saveToDB)
            {
                if (m_rankId == GuildDefaultRanks.Master)                     // Prevent loss of leader rights
                    rightsAndSlots.SetGuildMasterValues();

                m_bankTabRightsAndSlots[rightsAndSlots.GetTabId()] = rightsAndSlots;

                if (saveToDB)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
                    stmt.AddValue(0, m_guildId);
                    stmt.AddValue(1, rightsAndSlots.GetTabId());
                    stmt.AddValue(2, m_rankId);
                    stmt.AddValue(3, rightsAndSlots.GetRights());
                    stmt.AddValue(4, rightsAndSlots.GetSlots());
                    DB.Characters.Execute(stmt);
                }
            }

            public byte GetId() { return m_rankId; }

            public string GetName() { return m_name; }

            public GuildRankRights GetRights() { return m_rights; }

            public uint GetBankMoneyPerDay()
            {
                return m_rankId != GuildDefaultRanks.Master ? m_bankMoneyPerDay : GuildConst.WithdrawMoneyUnlimited;
            }

            public GuildBankRights GetBankTabRights(byte tabId)
            {
                return tabId < GuildConst.MaxBankTabs ? m_bankTabRightsAndSlots[tabId].GetRights() : 0;
            }

            public int GetBankTabSlotsPerDay(byte tabId)
            {
                return tabId < GuildConst.MaxBankTabs ? m_bankTabRightsAndSlots[tabId].GetSlots() : 0;
            }


            ulong m_guildId;
            byte m_rankId;
            string m_name;
            GuildRankRights m_rights;
            uint m_bankMoneyPerDay;
            GuildBankRightsAndSlots[] m_bankTabRightsAndSlots = new GuildBankRightsAndSlots[GuildConst.MaxBankTabs];
        }

        public class BankTab
        {
            public BankTab(ulong guildId, byte tabId)
            {
                m_guildId = guildId;
                m_tabId = tabId;
            }

            public void LoadFromDB(SQLFields field)
            {
                m_name = field.Read<string>(2);
                m_icon = field.Read<string>(3);
                m_text = field.Read<string>(4);
            }

            public bool LoadItemFromDB(SQLFields field)
            {
                byte slotId = field.Read<byte>(47);
                uint itemGuid = field.Read<uint>(0);
                uint itemEntry = field.Read<uint>(1);
                if (slotId >= GuildConst.MaxBankSlots)
                {
                    Log.outError(LogFilter.Guild, "Invalid slot for item (GUID: {0}, id: {1}) in guild bank, skipped.", itemGuid, itemEntry);
                    return false;
                }

                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);
                if (proto == null)
                {
                    Log.outError(LogFilter.Guild, "Unknown item (GUID: {0}, id: {1}) in guild bank, skipped.", itemGuid, itemEntry);
                    return false;
                }

                Item pItem = Bag.NewItemOrBag(proto);
                if (!pItem.LoadFromDB(itemGuid, ObjectGuid.Empty, field, itemEntry))
                {
                    Log.outError(LogFilter.Guild, "Item (GUID {0}, id: {1}) not found in item_instance, deleting from guild bank!", itemGuid, itemEntry);

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_NONEXISTENT_GUILD_BANK_ITEM);
                    stmt.AddValue(0, m_guildId);
                    stmt.AddValue(1, m_tabId);
                    stmt.AddValue(2, slotId);
                    DB.Characters.Execute(stmt);
                    return false;
                }

                pItem.AddToWorld();
                m_items[slotId] = pItem;
                return true;
            }

            public void Delete(SQLTransaction trans, bool removeItemsFromDB = false)
            {
                for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
                {
                    Item pItem = m_items[slotId];
                    if (pItem != null)
                    {
                        pItem.RemoveFromWorld();
                        if (removeItemsFromDB)
                            pItem.DeleteFromDB(trans);
                        pItem = null;
                    }
                }
            }

            public void SetInfo(string name, string icon)
            {
                if (m_name == name && m_icon == icon)
                    return;

                m_name = name;
                m_icon = icon;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_INFO);
                stmt.AddValue(0, m_name);
                stmt.AddValue(1, m_icon);
                stmt.AddValue(2, m_guildId);
                stmt.AddValue(3, m_tabId);
                DB.Characters.Execute(stmt);
            }

            public void SetText(string text)
            {
                if (m_text == text)
                    return;

                m_text = text;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_TEXT);
                stmt.AddValue(0, m_text);
                stmt.AddValue(1, m_guildId);
                stmt.AddValue(2, m_tabId);
                DB.Characters.Execute(stmt);
            }

            public void SendText(Guild guild, WorldSession session = null)
            {
                GuildBankTextQueryResult textQuery = new GuildBankTextQueryResult();
                textQuery.Tab = m_tabId;
                textQuery.Text = m_text;

                if (session != null)
                {
                    Log.outDebug(LogFilter.Guild, "SMSG_GUILD_BANK_QUERY_TEXT_RESULT [{0}]: Tabid: {1}, Text: {2}", session.GetPlayerInfo(), m_tabId, m_text);
                    session.SendPacket(textQuery);
                }
                else
                {
                    Log.outDebug(LogFilter.Guild, "SMSG_GUILD_BANK_QUERY_TEXT_RESULT [Broadcast]: Tabid: {0}, Text: {1}", m_tabId, m_text);
                    guild.BroadcastPacket(textQuery);
                }
            }

            public string GetName() { return m_name; }

            public string GetIcon() { return m_icon; }

            public string GetText() { return m_text; }

            public Item GetItem(byte slotId) { return slotId < GuildConst.MaxBankSlots ? m_items[slotId] : null; }

            public bool SetItem(SQLTransaction trans, byte slotId, Item item)
            {
                if (slotId >= GuildConst.MaxBankSlots)
                    return false;

                m_items[slotId] = item;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEM);
                stmt.AddValue(0, m_guildId);
                stmt.AddValue(1, m_tabId);
                stmt.AddValue(2, slotId);
                trans.Append(stmt);

                if (item != null)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_ITEM);
                    stmt.AddValue(0, m_guildId);
                    stmt.AddValue(1, m_tabId);
                    stmt.AddValue(2, slotId);
                    stmt.AddValue(3, item.GetGUID().GetCounter());
                    trans.Append(stmt);

                    item.SetGuidValue(ItemFields.Contained, ObjectGuid.Empty);
                    item.SetGuidValue(ItemFields.Owner, ObjectGuid.Empty);
                    item.FSetState(ItemUpdateState.New);
                    item.SaveToDB(trans);                                 // Not in inventory and can be saved standalone
                }

                return true;
            }


            ulong m_guildId;
            byte m_tabId;
            Item[] m_items = new Item[GuildConst.MaxBankSlots];
            string m_name;
            string m_icon;
            string m_text;
        }

        public class GuildBankRightsAndSlots
        {
            public GuildBankRightsAndSlots(byte _tabId = 0xFF, sbyte _rights = 0, int _slots = 0)
            {
                tabId = _tabId;
                rights = (GuildBankRights)_rights;
                slots = _slots;
            }

            public void SetGuildMasterValues()
            {
                rights = GuildBankRights.Full;
                slots = Convert.ToInt32(GuildConst.WithdrawSlotUnlimited);
            }

            public void SetTabId(byte _tabId) { tabId = _tabId; }
            public void SetSlots(int _slots) { slots = _slots; }
            public void SetRights(GuildBankRights _rights) { rights = _rights; }

            public byte GetTabId() { return tabId; }
            public int GetSlots() { return slots; }
            public GuildBankRights GetRights() { return rights; }

            byte tabId;
            GuildBankRights rights;
            int slots;
        }

        public class EmblemInfo
        {
            public EmblemInfo()
            {
                m_style = 0;
                m_color = 0;
                m_borderStyle = 0;
                m_borderColor = 0;
                m_backgroundColor = 0;
            }

            public void ReadPacket(SaveGuildEmblem packet)
            {
                m_style = packet.EStyle;
                m_color = packet.EColor;
                m_borderStyle = packet.BStyle;
                m_borderColor = packet.BColor;
                m_backgroundColor = packet.Bg;
            }

            public bool ValidateEmblemColors()
            {
                return CliDB.GuildColorBackgroundStorage.ContainsKey(m_backgroundColor) &&
                       CliDB.GuildColorBorderStorage.ContainsKey(m_borderColor) &&
                       CliDB.GuildColorEmblemStorage.ContainsKey(m_color);
            }

            public bool LoadFromDB(SQLFields field)
            {
                m_style = field.Read<byte>(3);
                m_color = field.Read<byte>(4);
                m_borderStyle = field.Read<byte>(5);
                m_borderColor = field.Read<byte>(6);
                m_backgroundColor = field.Read<byte>(7);

                return ValidateEmblemColors();
            }

            public void SaveToDB(ulong guildId)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_EMBLEM_INFO);
                stmt.AddValue(0, m_style);
                stmt.AddValue(1, m_color);
                stmt.AddValue(2, m_borderStyle);
                stmt.AddValue(3, m_borderColor);
                stmt.AddValue(4, m_backgroundColor);
                stmt.AddValue(5, guildId);
                DB.Characters.Execute(stmt);
            }

            public uint GetStyle() { return m_style; }

            public uint GetColor() { return m_color; }

            public uint GetBorderStyle() { return m_borderStyle; }

            public uint GetBorderColor() { return m_borderColor; }

            public uint GetBackgroundColor() { return m_backgroundColor; }

            uint m_style;
            uint m_color;
            uint m_borderStyle;
            uint m_borderColor;
            uint m_backgroundColor;
        }

        public abstract class MoveItemData
        {
            protected MoveItemData(Guild guild, Player player, byte container, byte slotId)
            {
                m_pGuild = guild;
                m_pPlayer = player;
                m_container = container;
                m_slotId = slotId;
                m_pItem = null;
                m_pClonedItem = null;
            }

            public virtual bool CheckItem(ref uint splitedAmount)
            {
                Cypher.Assert(m_pItem != null);
                if (splitedAmount > m_pItem.GetCount())
                    return false;
                if (splitedAmount == m_pItem.GetCount())
                    splitedAmount = 0;
                return true;
            }

            public bool CanStore(Item pItem, bool swap, bool sendError)
            {
                m_vec.Clear();
                InventoryResult msg = CanStore(pItem, swap);
                if (sendError && msg != InventoryResult.Ok)
                    m_pPlayer.SendEquipError(msg, pItem);
                return (msg == InventoryResult.Ok);
            }

            public bool CloneItem(uint count)
            {
                Cypher.Assert(m_pItem != null);
                m_pClonedItem = m_pItem.CloneItem(count);
                if (m_pClonedItem == null)
                {
                    m_pPlayer.SendEquipError(InventoryResult.ItemNotFound, m_pItem);
                    return false;
                }
                return true;
            }

            public virtual void LogAction(MoveItemData pFrom)
            {
                Cypher.Assert(pFrom.GetItem() != null);

                Global.ScriptMgr.OnGuildItemMove(m_pGuild, m_pPlayer, pFrom.GetItem(),
                     pFrom.IsBank(), pFrom.GetContainer(), pFrom.GetSlotId(),
                     IsBank(), GetContainer(), GetSlotId());
            }

            public void CopySlots(List<byte> ids)
            {
                foreach (var item in m_vec)
                    ids.Add((byte)item.pos);
            }

            public abstract bool IsBank();
            // Initializes item. Returns true, if item exists, false otherwise.
            public abstract bool InitItem();
            // Checks splited amount against item. Splited amount cannot be more that number of items in stack.
            // Defines if player has rights to save item in container
            public virtual bool HasStoreRights(MoveItemData pOther) { return true; }
            // Defines if player has rights to withdraw item from container
            public virtual bool HasWithdrawRights(MoveItemData pOther) { return true; }
            // Remove item from container (if splited update items fields)
            public abstract void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0);
            // Saves item to container
            public abstract Item StoreItem(SQLTransaction trans, Item pItem);
            // Log bank event
            public abstract void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count);

            public abstract InventoryResult CanStore(Item pItem, bool swap);

            public Item GetItem(bool isCloned = false) { return isCloned ? m_pClonedItem : m_pItem; }
            public byte GetContainer() { return m_container; }
            public byte GetSlotId() { return m_slotId; }

            public Guild m_pGuild;
            public Player m_pPlayer;
            public byte m_container;
            public byte m_slotId;
            public Item m_pItem;
            public Item m_pClonedItem;
            public List<ItemPosCount> m_vec = new List<ItemPosCount>();
        }

        public class PlayerMoveItemData : MoveItemData
        {
            public PlayerMoveItemData(Guild guild, Player player, byte container, byte slotId)
                : base(guild, player, container, slotId) { }

            public override bool IsBank() { return false; }

            public override bool InitItem()
            {
                m_pItem = m_pPlayer.GetItemByPos(m_container, m_slotId);
                if (m_pItem != null)
                {
                    // Anti-WPE protection. Do not move non-empty bags to bank.
                    if (m_pItem.IsNotEmptyBag())
                    {
                        m_pPlayer.SendEquipError(InventoryResult.DestroyNonemptyBag, m_pItem);
                        m_pItem = null;
                    }
                    // Bound items cannot be put into bank.
                    else if (!m_pItem.CanBeTraded())
                    {
                        m_pPlayer.SendEquipError(InventoryResult.CantSwap, m_pItem);
                        m_pItem = null;
                    }
                }
                return (m_pItem != null);
            }

            public override void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0)
            {
                if (splitedAmount != 0)
                {
                    m_pItem.SetCount(m_pItem.GetCount() - splitedAmount);
                    m_pItem.SetState(ItemUpdateState.Changed, m_pPlayer);
                    m_pPlayer.SaveInventoryAndGoldToDB(trans);
                }
                else
                {
                    m_pPlayer.MoveItemFromInventory(m_container, m_slotId, true);
                    m_pItem.DeleteFromInventoryDB(trans);
                    m_pItem = null;
                }
            }

            public override Item StoreItem(SQLTransaction trans, Item pItem)
            {
                Cypher.Assert(pItem != null);
                m_pPlayer.MoveItemToInventory(m_vec, pItem, true);
                m_pPlayer.SaveInventoryAndGoldToDB(trans);
                return pItem;
            }

            public override void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count)
            {
                Cypher.Assert(pFrom != null);
                // Bank . Char
                m_pGuild._LogBankEvent(trans, GuildBankEventLogTypes.WithdrawItem, pFrom.GetContainer(), m_pPlayer.GetGUID().GetCounter(),
                    pFrom.GetItem().GetEntry(), (ushort)count);
            }

            public override InventoryResult CanStore(Item pItem, bool swap)
            {
                return m_pPlayer.CanStoreItem(m_container, m_slotId, m_vec, pItem, swap);
            }
        }

        public class BankMoveItemData : MoveItemData
        {
            public BankMoveItemData(Guild guild, Player player, byte container, byte slotId)
                : base(guild, player, container, slotId) { }

            public override bool IsBank() { return true; }

            public override bool InitItem()
            {
                m_pItem = m_pGuild._GetItem(m_container, m_slotId);
                return (m_pItem != null);
            }
            public override bool HasStoreRights(MoveItemData pOther)
            {
                Cypher.Assert(pOther != null);
                // Do not check rights if item is being swapped within the same bank tab
                if (pOther.IsBank() && pOther.GetContainer() == m_container)
                    return true;
                return m_pGuild._MemberHasTabRights(m_pPlayer.GetGUID(), m_container, GuildBankRights.DepositItem);
            }

            public override bool HasWithdrawRights(MoveItemData pOther)
            {
                Cypher.Assert(pOther != null);
                // Do not check rights if item is being swapped within the same bank tab
                if (pOther.IsBank() && pOther.GetContainer() == m_container)
                    return true;

                int slots = 0;
                Member member = m_pGuild.GetMember(m_pPlayer.GetGUID());
                if (member != null)
                    slots = m_pGuild._GetMemberRemainingSlots(member, m_container);

                return slots != 0;
            }

            public override void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0)
            {
                Cypher.Assert(m_pItem != null);
                if (splitedAmount != 0)
                {
                    m_pItem.SetCount(m_pItem.GetCount() - splitedAmount);
                    m_pItem.FSetState(ItemUpdateState.Changed);
                    m_pItem.SaveToDB(trans);
                }
                else
                {
                    m_pGuild._RemoveItem(trans, m_container, m_slotId);
                    m_pItem = null;
                }
                // Decrease amount of player's remaining items (if item is moved to different tab or to player)
                if (!pOther.IsBank() || pOther.GetContainer() != m_container)
                    m_pGuild._UpdateMemberWithdrawSlots(trans, m_pPlayer.GetGUID(), m_container);
            }

            public override Item StoreItem(SQLTransaction trans, Item pItem)
            {
                if (pItem == null)
                    return null;

                BankTab pTab = m_pGuild.GetBankTab(m_container);
                if (pTab == null)
                    return null;

                Item pLastItem = pItem;
                foreach (var pos in m_vec)
                {
                    Log.outDebug(LogFilter.Guild, "GUILD STORAGE: StoreItem tab = {0}, slot = {1}, item = {2}, count = {3}",
                        m_container, m_slotId, pItem.GetEntry(), pItem.GetCount());
                    pLastItem = _StoreItem(trans, pTab, pItem, pos, pos.Equals(m_vec.Last()));
                }
                return pLastItem;
            }

            public override void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count)
            {
               Cypher.Assert(pFrom.GetItem() != null);
                if (pFrom.IsBank())
                    // Bank . Bank
                    m_pGuild._LogBankEvent(trans, GuildBankEventLogTypes.MoveItem, pFrom.GetContainer(), m_pPlayer.GetGUID().GetCounter(),
                        pFrom.GetItem().GetEntry(), (ushort)count, m_container);
                else
                    // Char . Bank
                    m_pGuild._LogBankEvent(trans, GuildBankEventLogTypes.DepositItem, m_container, m_pPlayer.GetGUID().GetCounter(),
                        pFrom.GetItem().GetEntry(), (ushort)count);
            }
            public override void LogAction(MoveItemData pFrom)
            {
                base.LogAction(pFrom);
                if (!pFrom.IsBank() && m_pPlayer.GetSession().HasPermission(RBACPermissions.LogGmTrade)) // @todo Move this to scripts
                {
                    Log.outCommand(m_pPlayer.GetSession().GetAccountId(), "GM {0} ({1}) (Account: {2}) deposit item: {3} (Entry: {4} Count: {5}) to guild bank named: {6} (Guild ID: {7})",
                        m_pPlayer.GetName(), m_pPlayer.GetGUID().ToString(), m_pPlayer.GetSession().GetAccountId(), pFrom.GetItem().GetTemplate().GetName(),
                        pFrom.GetItem().GetEntry(), pFrom.GetItem().GetCount(), m_pGuild.GetName(), m_pGuild.GetId());
                }
            }

            Item _StoreItem(SQLTransaction trans, BankTab pTab, Item pItem, ItemPosCount pos, bool clone)
            {
                byte slotId = (byte)pos.pos;
                uint count = pos.count;
                Item pItemDest = pTab.GetItem(slotId);
                if (pItemDest != null)
                {
                    pItemDest.SetCount(pItemDest.GetCount() + count);
                    pItemDest.FSetState(ItemUpdateState.Changed);
                    pItemDest.SaveToDB(trans);
                    if (!clone)
                    {
                        pItem.RemoveFromWorld();
                        pItem.DeleteFromDB(trans);
                    }
                    return pItemDest;
                }

                if (clone)
                    pItem = pItem.CloneItem(count);
                else
                    pItem.SetCount(count);

                if (pItem != null && pTab.SetItem(trans, slotId, pItem))
                    return pItem;

                return null;
            }
            bool _ReserveSpace(byte slotId, Item pItem, Item pItemDest, ref uint count)
            {
                uint requiredSpace = pItem.GetMaxStackCount();
                if (pItemDest != null)
                {
                    // Make sure source and destination items match and destination item has space for more stacks.
                    if (pItemDest.GetEntry() != pItem.GetEntry() || pItemDest.GetCount() >= pItem.GetMaxStackCount())
                        return false;
                    requiredSpace -= pItemDest.GetCount();
                }
                // Let's not be greedy, reserve only required space
                requiredSpace = Math.Min(requiredSpace, count);

                // Reserve space
                ItemPosCount pos = new ItemPosCount(slotId, requiredSpace);
                if (!pos.isContainedIn(m_vec))
                {
                    m_vec.Add(pos);
                    count -= requiredSpace;
                }
                return true;
            }

            void CanStoreItemInTab(Item pItem, byte skipSlotId, bool merge, ref uint count)
            {
                for (byte slotId = 0; (slotId < GuildConst.MaxBankSlots) && (count > 0); ++slotId)
                {
                    // Skip slot already processed in CanStore (when destination slot was specified)
                    if (slotId == skipSlotId)
                        continue;

                    Item pItemDest = m_pGuild._GetItem(m_container, slotId);
                    if (pItemDest == pItem)
                        pItemDest = null;

                    // If merge skip empty, if not merge skip non-empty
                    if ((pItemDest != null) != merge)
                        continue;

                    _ReserveSpace(slotId, pItem, pItemDest, ref count);
                }
            }

            public override InventoryResult CanStore(Item pItem, bool swap)
            {
                Log.outDebug(LogFilter.Guild, "GUILD STORAGE: CanStore() tab = {0}, slot = {1}, item = {2}, count = {3}",
                    m_container, m_slotId, pItem.GetEntry(), pItem.GetCount());

                uint count = pItem.GetCount();
                // Soulbound items cannot be moved
                if (pItem.IsSoulBound())
                    return InventoryResult.DropBoundItem;

                // Make sure destination bank tab exists
                if (m_container >= m_pGuild._GetPurchasedTabsSize())
                    return InventoryResult.WrongBagType;

                // Slot explicitely specified. Check it.
                if (m_slotId != ItemConst.NullSlot)
                {
                    Item pItemDest = m_pGuild._GetItem(m_container, m_slotId);
                    // Ignore swapped item (this slot will be empty after move)
                    if ((pItemDest == pItem) || swap)
                        pItemDest = null;

                    if (!_ReserveSpace(m_slotId, pItem, pItemDest, ref count))
                        return InventoryResult.CantStack;

                    if (count == 0)
                        return InventoryResult.Ok;
                }

                // Slot was not specified or it has not enough space for all the items in stack
                // Search for stacks to merge with
                if (pItem.GetMaxStackCount() > 1)
                {
                    CanStoreItemInTab(pItem, m_slotId, true, ref count);
                    if (count == 0)
                        return InventoryResult.Ok;
                }

                // Search free slot for item
                CanStoreItemInTab(pItem, m_slotId, false, ref count);
                if (count == 0)
                    return InventoryResult.Ok;

                return InventoryResult.BankFull;
            }
        }
        #endregion
    }
}
