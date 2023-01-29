// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.Achievements;
using Game.DataStorage;
using Game.Entities;
using Game.Groups;
using Game.Networking;
using Game.Networking.Packets;
using Game.Scripting.Interfaces.IGuild;

namespace Game.Guilds
{
    public class Guild
    {
        public Guild()
        {
            _achievementSys = new GuildAchievementMgr(this);

            for (var i = 0; i < _bankEventLog.Length; ++i)
                _bankEventLog[i] = new LogHolder<BankEventLogEntry>();
        }

        public bool Create(Player pLeader, string name)
        {
            // Check if guild with such Name already exists
            if (Global.GuildMgr.GetGuildByName(name) != null)
                return false;

            WorldSession pLeaderSession = pLeader.GetSession();

            if (pLeaderSession == null)
                return false;

            _id = Global.GuildMgr.GenerateGuildId();
            _leaderGuid = pLeader.GetGUID();
            _name = name;
            _info = "";
            _motd = "No message set.";
            _bankMoney = 0;
            _createdDate = GameTime.GetGameTime();

            Log.outDebug(LogFilter.Guild,
                         "GUILD: creating guild [{0}] for leader {1} ({2})",
                         name,
                         pLeader.GetName(),
                         _leaderGuid);

            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBERS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            byte index = 0;
            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD);
            stmt.AddValue(index, _id);
            stmt.AddValue(++index, name);
            stmt.AddValue(++index, _leaderGuid.GetCounter());
            stmt.AddValue(++index, _info);
            stmt.AddValue(++index, _motd);
            stmt.AddValue(++index, _createdDate);
            stmt.AddValue(++index, _emblemInfo.GetStyle());
            stmt.AddValue(++index, _emblemInfo.GetColor());
            stmt.AddValue(++index, _emblemInfo.GetBorderStyle());
            stmt.AddValue(++index, _emblemInfo.GetBorderColor());
            stmt.AddValue(++index, _emblemInfo.GetBackgroundColor());
            stmt.AddValue(++index, _bankMoney);
            trans.Append(stmt);

            _CreateDefaultGuildRanks(trans, pLeaderSession.GetSessionDbLocaleIndex()); // Create default ranks
            bool ret = AddMember(trans, _leaderGuid, GuildRankId.GuildMaster);         // Add guildmaster

            DB.Characters.CommitTransaction(trans);

            if (ret)
            {
                Member leader = GetMember(_leaderGuid);

                if (leader != null)
                    SendEventNewLeader(leader, null);

                Global.ScriptMgr.ForEach<IGuildOnCreate>(p => p.OnCreate(this, pLeader, name));
            }

            return ret;
        }

        public void Disband()
        {
            Global.ScriptMgr.ForEach<IGuildOnDisband>(p => p.OnDisband(this));

            BroadcastPacket(new GuildEventDisbanded());

            SQLTransaction trans = new();

            while (!_members.Empty())
            {
                var member = _members.First();
                DeleteMember(trans, member.Value.GetGUID(), true);
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_TABS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            // Free bank tab used memory and delete items stored in them
            _DeleteBankItems(trans, true);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEMS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOGS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOGS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            Global.GuildMgr.RemoveGuild(_id);
        }

        public void SaveToDB()
        {
            SQLTransaction trans = new();

            GetAchievementMgr().SaveToDB(trans);

            DB.Characters.CommitTransaction(trans);
        }

        public void UpdateMemberData(Player player, GuildMemberData dataid, uint value)
        {
            Member member = GetMember(player.GetGUID());

            if (member != null)
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

        private void OnPlayerStatusChange(Player player, GuildMemberFlags flag, bool state)
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
            if (_name == name ||
                string.IsNullOrEmpty(name) ||
                name.Length > 24 ||
                Global.ObjectMgr.IsReservedName(name) ||
                !ObjectManager.IsValidCharterName(name))
                return false;

            _name = name;
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_NAME);
            stmt.AddValue(0, _name);
            stmt.AddValue(1, GetId());
            DB.Characters.Execute(stmt);

            GuildNameChanged guildNameChanged = new();
            guildNameChanged.GuildGUID = GetGUID();
            guildNameChanged.GuildName = _name;
            BroadcastPacket(guildNameChanged);

            return true;
        }

        public void HandleRoster(WorldSession session = null)
        {
            GuildRoster roster = new();
            roster.NumAccounts = (int)_accountsNumber;
            roster.CreateDate = (uint)_createdDate;
            roster.GuildFlags = 0;

            bool sendOfficerNote = _HasRankRight(session.GetPlayer(), GuildRankRights.ViewOffNote);

            foreach (var member in _members.Values)
            {
                GuildRosterMemberData memberData = new();

                memberData.Guid = member.GetGUID();
                memberData.RankID = (int)member.GetRankId();
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
                memberData.RaceID = (byte)member.GetRace();

                memberData.Authenticated = false;
                memberData.SorEligible = false;

                memberData.Name = member.GetName();
                memberData.Note = member.GetPublicNote();

                if (sendOfficerNote)
                    memberData.OfficerNote = member.GetOfficerNote();

                roster.MemberData.Add(memberData);
            }

            roster.WelcomeText = _motd;
            roster.InfoText = _info;

            session?.SendPacket(roster);
        }

        public void SendQueryResponse(WorldSession session)
        {
            QueryGuildInfoResponse response = new();
            response.GuildGUID = GetGUID();
            response.HasGuildInfo = true;

            response.Info.GuildGuid = GetGUID();
            response.Info.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();

            response.Info.EmblemStyle = _emblemInfo.GetStyle();
            response.Info.EmblemColor = _emblemInfo.GetColor();
            response.Info.BorderStyle = _emblemInfo.GetBorderStyle();
            response.Info.BorderColor = _emblemInfo.GetBorderColor();
            response.Info.BackgroundColor = _emblemInfo.GetBackgroundColor();

            foreach (RankInfo rankInfo in _ranks)
                response.Info.Ranks.Add(new QueryGuildInfoResponse.GuildInfo.RankInfo((byte)rankInfo.GetId(), (byte)rankInfo.GetOrder(), rankInfo.GetName()));

            response.Info.GuildName = _name;

            session.SendPacket(response);
        }

        public void SendGuildRankInfo(WorldSession session)
        {
            GuildRanks ranks = new();

            foreach (RankInfo rankInfo in _ranks)
            {
                GuildRankData rankData = new();

                rankData.RankID = (byte)rankInfo.GetId();
                rankData.RankOrder = (byte)rankInfo.GetOrder();
                rankData.Flags = (uint)rankInfo.GetRights();
                rankData.WithdrawGoldLimit = (rankInfo.GetId() == GuildRankId.GuildMaster ? uint.MaxValue : (rankInfo.GetBankMoneyPerDay() / MoneyConstants.Gold));
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
                List<uint> criteriaIds = new();

                foreach (var achievementId in achievementIds)
                {
                    var achievement = CliDB.AchievementStorage.LookupByKey(achievementId);

                    if (achievement != null)
                    {
                        CriteriaTree tree = Global.CriteriaMgr.GetCriteriaTree(achievement.CriteriaTree);

                        if (tree != null)
                            CriteriaManager.WalkCriteriaTree(tree,
                                                             node =>
                                                             {
                                                                 if (node.Criteria != null)
                                                                     criteriaIds.Add(node.Criteria.Id);
                                                             });
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
            if (_motd == motd)
                return;

            // Player must have rights to set MOTD
            if (!_HasRankRight(session.GetPlayer(), GuildRankRights.SetMotd))
            {
                SendCommandResult(session, GuildCommandType.EditMOTD, GuildCommandError.Permissions);
            }
            else
            {
                _motd = motd;

                Global.ScriptMgr.ForEach<IGuildOnMOTDChanged>(p => p.OnMOTDChanged(this, motd));

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MOTD);
                stmt.AddValue(0, motd);
                stmt.AddValue(1, _id);
                DB.Characters.Execute(stmt);

                SendEventMOTD(session, true);
            }
        }

        public void HandleSetInfo(WorldSession session, string info)
        {
            if (_info == info)
                return;

            // Player must have rights to set guild's info
            if (_HasRankRight(session.GetPlayer(), GuildRankRights.ModifyGuildInfo))
            {
                _info = info;

                Global.ScriptMgr.ForEach<IGuildOnInfoChanged>(p => p.OnInfoChanged(this, info));

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_INFO);
                stmt.AddValue(0, info);
                stmt.AddValue(1, _id);
                DB.Characters.Execute(stmt);
            }
        }

        public void HandleSetEmblem(WorldSession session, EmblemInfo emblemInfo)
        {
            Player player = session.GetPlayer();

            if (!_IsLeader(player))
            {
                SendSaveEmblemResult(session, GuildEmblemError.NotGuildMaster); // "Only guild leaders can create emblems."
            }
            else if (!player.HasEnoughMoney(10 * MoneyConstants.Gold))
            {
                SendSaveEmblemResult(session, GuildEmblemError.NotEnoughMoney); // "You can't afford to do that."
            }
            else
            {
                player.ModifyMoney(-(long)10 * MoneyConstants.Gold);

                _emblemInfo = emblemInfo;
                _emblemInfo.SaveToDB(_id);

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

                RankInfo oldRank = GetRankInfo(newGuildMaster.GetRankId());

                // only second highest rank can take over guild
                if (oldRank.GetOrder() != (GuildRankOrder)1 ||
                    oldGuildMaster.GetInactiveDays() < GuildConst.MasterDethroneInactiveDays)
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

            SQLTransaction trans = new();

            _SetLeader(trans, newGuildMaster);
            oldGuildMaster.ChangeRank(trans, _GetLowestRankId());

            SendEventNewLeader(newGuildMaster, oldGuildMaster, isSelfPromote);

            DB.Characters.CommitTransaction(trans);
        }

        public void HandleSetBankTabInfo(WorldSession session, byte tabId, string name, string icon)
        {
            BankTab tab = GetBankTab(tabId);

            if (tab == null)
            {
                Log.outError(LogFilter.Guild,
                             "Guild.HandleSetBankTabInfo: Player {0} trying to change bank tab info from unexisting tab {1}.",
                             session.GetPlayer().GetName(),
                             tabId);

                return;
            }

            tab.SetInfo(name, icon);

            GuildEventTabModified packet = new();
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

                GuildMemberUpdateNote updateNote = new();
                updateNote.Member = guid;
                updateNote.IsPublic = isPublic;
                updateNote.Note = note;
                BroadcastPacket(updateNote);
            }
        }

        public void HandleSetRankInfo(WorldSession session, GuildRankId rankId, string name, GuildRankRights rights, uint moneyPerDay, GuildBankRightsAndSlots[] rightsAndSlots)
        {
            // Only leader can modify ranks
            if (!_IsLeader(session.GetPlayer()))
                SendCommandResult(session, GuildCommandType.ChangeRank, GuildCommandError.Permissions);

            RankInfo rankInfo = GetRankInfo(rankId);

            if (rankInfo != null)
            {
                rankInfo.SetName(name);
                rankInfo.SetRights(rights);
                _SetRankBankMoneyPerDay(rankId, moneyPerDay * MoneyConstants.Gold);

                foreach (var rightsAndSlot in rightsAndSlots)
                    _SetRankBankTabRightsAndSlots(rankId, rightsAndSlot);

                GuildEventRankChanged packet = new();
                packet.RankID = (byte)rankId;
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

                if (!player.HasEnoughMoney(tabCost)) // Should not happen, this is checked by client
                    return;

                player.ModifyMoney(-tabCost);
            }

            _CreateNewBankTab();

            BroadcastPacket(new GuildEventTabAdded());

            SendPermissions(session); //Hack to Force client to update permissions
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
            if (pInvitee.GetSocial().HasIgnore(player.GetGUID(), player.GetSession().GetAccountGUID()))
                return;

            if (!WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGuild) &&
                pInvitee.GetTeam() != player.GetTeam())
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

            pInvitee.SetGuildIdInvited(_id);
            _LogEvent(GuildEventLogTypes.InvitePlayer, player.GetGUID().GetCounter(), pInvitee.GetGUID().GetCounter());

            GuildInvite invite = new();

            invite.InviterVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            invite.GuildVirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            invite.GuildGUID = GetGUID();

            invite.EmblemStyle = _emblemInfo.GetStyle();
            invite.EmblemColor = _emblemInfo.GetColor();
            invite.BorderStyle = _emblemInfo.GetBorderStyle();
            invite.BorderColor = _emblemInfo.GetBorderColor();
            invite.Background = _emblemInfo.GetBackgroundColor();
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
                player.GetTeam() != Global.CharacterCacheStorage.GetCharacterTeamByGuid(GetLeaderGUID()))
                return;

            AddMember(null, player.GetGUID());
        }

        public void HandleLeaveMember(WorldSession session)
        {
            Player player = session.GetPlayer();

            // If leader is leaving
            if (_IsLeader(player))
            {
                if (_members.Count > 1)
                    // Leader cannot leave if he is not the last member
                    SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.LeaderLeave);
                else
                    // Guild is disbanded if leader leaves.
                    Disband();
            }
            else
            {
                DeleteMember(null, player.GetGUID(), false, false);

                _LogEvent(GuildEventLogTypes.LeaveGuild, player.GetGUID().GetCounter());
                SendEventPlayerLeft(player);

                SendCommandResult(session, GuildCommandType.LeaveGuild, GuildCommandError.Success, _name);
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
                if (member.IsRank(GuildRankId.GuildMaster))
                {
                    SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.LeaderLeave);
                }
                // Do not allow to remove player with the same rank or higher
                else
                {
                    Member memberMe = GetMember(player.GetGUID());
                    RankInfo myRank = GetRankInfo(memberMe.GetRankId());
                    RankInfo targetRank = GetRankInfo(member.GetRankId());

                    if (memberMe == null ||
                        targetRank.GetOrder() <= myRank.GetOrder())
                    {
                        SendCommandResult(session, GuildCommandType.RemovePlayer, GuildCommandError.RankTooHigh_S, name);
                    }
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
            {
                SendCommandResult(session, type, GuildCommandError.LeaderLeave);
            }
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
                RankInfo myRank = GetRankInfo(memberMe.GetRankId());
                RankInfo oldRank = GetRankInfo(member.GetRankId());
                GuildRankId newRankId;

                if (demote)
                {
                    // Player can demote only lower rank members
                    if (oldRank.GetOrder() <= myRank.GetOrder())
                    {
                        SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);

                        return;
                    }

                    // Lowest rank cannot be demoted
                    RankInfo newRank = GetRankInfo(oldRank.GetOrder() + 1);

                    if (newRank == null)
                    {
                        SendCommandResult(session, type, GuildCommandError.RankTooLow_S, name);

                        return;
                    }

                    newRankId = newRank.GetId();
                }
                else
                {
                    // Allow to promote only to lower rank than member's rank
                    // memberMe.GetRankId() + 1 is the highest rank that current player can promote to
                    if ((oldRank.GetOrder() - 1) <= myRank.GetOrder())
                    {
                        SendCommandResult(session, type, GuildCommandError.RankTooHigh_S, name);

                        return;
                    }

                    newRankId = GetRankInfo((oldRank.GetOrder() - 1)).GetId();
                }

                member.ChangeRank(null, newRankId);
                _LogEvent(demote ? GuildEventLogTypes.DemotePlayer : GuildEventLogTypes.PromotePlayer, player.GetGUID().GetCounter(), member.GetGUID().GetCounter(), (byte)newRankId);
                //_BroadcastEvent(demote ? GuildEvents.Demotion : GuildEvents.Promotion, ObjectGuid.Empty, player.GetName(), Name, _GetRankName((byte)newRankId));
            }
        }

        public void HandleSetMemberRank(WorldSession session, ObjectGuid targetGuid, ObjectGuid setterGuid, GuildRankOrder rank)
        {
            Player player = session.GetPlayer();
            Member member = GetMember(targetGuid);
            GuildRankRights rights = GuildRankRights.Promote;
            GuildCommandType type = GuildCommandType.PromotePlayer;

            RankInfo oldRank = GetRankInfo(member.GetRankId());
            RankInfo newRank = GetRankInfo(rank);

            if (oldRank == null ||
                newRank == null)
                return;

            if (rank > oldRank.GetOrder())
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

            SendGuildRanksUpdate(setterGuid, targetGuid, newRank.GetId());
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

        public void HandleRemoveRank(WorldSession session, GuildRankOrder rankOrder)
        {
            // Cannot remove rank if total Count is minimum allowed by the client or is not leader
            if (_GetRanksSize() <= GuildConst.MinRanks ||
                !_IsLeader(session.GetPlayer()))
                return;

            var rankInfo = _ranks.Find(rank => rank.GetOrder() == rankOrder);

            if (rankInfo == null)
                return;

            SQLTransaction trans = new();

            // Delete bank rights for rank
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS_FOR_RANK);
            stmt.AddValue(0, _id);
            stmt.AddValue(1, (byte)rankInfo.GetId());
            trans.Append(stmt);

            // Delete rank
            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_RANK);
            stmt.AddValue(0, _id);
            stmt.AddValue(1, (byte)rankInfo.GetId());
            trans.Append(stmt);

            _ranks.Remove(rankInfo);

            // correct order of other ranks
            foreach (RankInfo otherRank in _ranks)
            {
                if (otherRank.GetOrder() < rankOrder)
                    continue;

                otherRank.SetOrder(otherRank.GetOrder() - 1);

                stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
                stmt.AddValue(0, (byte)otherRank.GetOrder());
                stmt.AddValue(1, (byte)otherRank.GetId());
                stmt.AddValue(2, _id);
                trans.Append(stmt);
            }

            DB.Characters.CommitTransaction(trans);

            BroadcastPacket(new GuildEventRanksUpdated());
        }

        public void HandleShiftRank(WorldSession session, GuildRankOrder rankOrder, bool shiftUp)
        {
            // Only leader can modify ranks
            if (!_IsLeader(session.GetPlayer()))
                return;

            GuildRankOrder otherRankOrder = (GuildRankOrder)(rankOrder + (shiftUp ? -1 : 1));

            RankInfo rankInfo = GetRankInfo(rankOrder);
            RankInfo otherRankInfo = GetRankInfo(otherRankOrder);

            if (rankInfo == null ||
                otherRankInfo == null)
                return;

            // can't shift guild master rank (rank Id = 0) - there's already a client-side limitation for it so that's just a safe-guard
            if (rankInfo.GetId() == GuildRankId.GuildMaster ||
                otherRankInfo.GetId() == GuildRankId.GuildMaster)
                return;

            rankInfo.SetOrder(otherRankOrder);
            otherRankInfo.SetOrder(rankOrder);

            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
            stmt.AddValue(0, (byte)rankInfo.GetOrder());
            stmt.AddValue(1, (byte)rankInfo.GetId());
            stmt.AddValue(2, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_ORDER);
            stmt.AddValue(0, (byte)otherRankInfo.GetOrder());
            stmt.AddValue(1, (byte)otherRankInfo.GetId());
            stmt.AddValue(2, _id);
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);

            // Force client to re-request SMSG_GUILD_RANKS
            BroadcastPacket(new GuildEventRanksUpdated());
        }

        public void HandleMemberDepositMoney(WorldSession session, ulong amount, bool cashFlow = false)
        {
            // guild bank cannot have more than MAX_MONEY_AMOUNT
            amount = Math.Min(amount, PlayerConst.MaxMoneyAmount - _bankMoney);

            if (amount == 0)
                return;

            Player player = session.GetPlayer();

            // Call script after validation and before money transfer.
            Global.ScriptMgr.ForEach<IGuildOnMemberDepositMoney>(p => p.OnMemberDepositMoney(this, player, amount));

            if (_bankMoney > GuildConst.MoneyLimit - amount)
            {
                if (!cashFlow)
                    SendCommandResult(session, GuildCommandType.MoveItem, GuildCommandError.TooMuchMoney);

                return;
            }

            SQLTransaction trans = new();
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
                Log.outCommand(player.GetSession().GetAccountId(),
                               "GM {0} (Account: {1}) deposit money (Amount: {2}) to guild bank (Guild ID {3})",
                               player.GetName(),
                               player.GetSession().GetAccountId(),
                               amount,
                               _id);
        }

        public bool HandleMemberWithdrawMoney(WorldSession session, ulong amount, bool repair = false)
        {
            // clamp amount to MAX_MONEY_AMOUNT, Players can't hold more than that anyway
            amount = Math.Min(amount, PlayerConst.MaxMoneyAmount);

            if (_bankMoney < amount) // Not enough money in bank
                return false;

            Player player = session.GetPlayer();

            Member member = GetMember(player.GetGUID());

            if (member == null)
                return false;

            if (!_HasRankRight(player, repair ? GuildRankRights.WithdrawRepair : GuildRankRights.WithdrawGold))
                return false;

            if (_GetMemberRemainingMoney(member) < (long)amount) // Check if we have enough Slot/money today
                return false;

            // Call script after validation and before money transfer.
            Global.ScriptMgr.ForEach<IGuildOnMemberWithDrawMoney>(p => p.OnMemberWitdrawMoney(this, player, amount, repair));

            SQLTransaction trans = new();

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
            if (!IsMember(player.GetGUID()) ||
                !group)
                return;

            GuildPartyState partyStateResponse = new();
            partyStateResponse.InGuildParty = (player.GetMap().GetOwnerGuildId(player.GetTeam()) == GetId());
            partyStateResponse.NumMembers = 0;
            partyStateResponse.NumRequired = 0;
            partyStateResponse.GuildXPEarnedMult = 0.0f;
            session.SendPacket(partyStateResponse);
        }

        public void HandleGuildRequestChallengeUpdate(WorldSession session)
        {
            GuildChallengeUpdate updatePacket = new();

            for (int i = 0; i < GuildConst.ChallengesTypes; ++i)
                updatePacket.CurrentCount[i] = 0; // @todo current Count

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
            var eventLog = _eventLog.GetGuildLog();

            GuildEventLogQueryResults packet = new();

            foreach (var entry in eventLog)
                entry.WritePacket(packet);

            session.SendPacket(packet);
        }

        public void SendNewsUpdate(WorldSession session)
        {
            var newsLog = _newsLog.GetGuildLog();

            GuildNewsPkt packet = new();

            foreach (var newsLogEntry in newsLog)
                newsLogEntry.WritePacket(packet);

            session.SendPacket(packet);
        }

        public void SendBankLog(WorldSession session, byte tabId)
        {
            // GuildConst.MaxBankTabs send by client for money log
            if (tabId < _GetPurchasedTabsSize() ||
                tabId == GuildConst.MaxBankTabs)
            {
                var bankEventLog = _bankEventLog[tabId].GetGuildLog();

                GuildBankLogQueryResults packet = new();
                packet.Tab = tabId;

                //if (tabId == GUILD_BANK_MAX_TABS && hasCashFlow)
                //    packet.WeeklyBonusMoney.Set(uint64(weeklyBonusMoney));

                foreach (var entry in bankEventLog)
                    entry.WritePacket(packet);

                session.SendPacket(packet);
            }
        }

        public void SendBankTabText(WorldSession session, byte tabId)
        {
            BankTab tab = GetBankTab(tabId);

            tab?.SendText(this, session);
        }

        public void SendPermissions(WorldSession session)
        {
            Member member = GetMember(session.GetPlayer().GetGUID());

            if (member == null)
                return;

            GuildRankId rankId = member.GetRankId();

            GuildPermissionsQueryResults queryResult = new();
            queryResult.RankID = (byte)rankId;
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

            GuildBankRemainingWithdrawMoney packet = new();
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
            SendEventPresenceChanged(session, true, true); // Broadcast

            // Send to self separately, player is not in world yet and is not found by _BroadcastEvent
            SendEventPresenceChanged(session, true);

            if (member.GetGUID() == GetLeaderGUID())
            {
                GuildFlaggedForRename renameFlag = new();
                renameFlag.FlagSet = false;
                player.SendPacket(renameFlag);
            }

            foreach (var entry in CliDB.GuildPerkSpellsStorage.Values)
                player.LearnSpell(entry.SpellID, true);

            GetAchievementMgr().SendAllData(player);

            // tells the client to request bank withdrawal limit
            player.SendPacket(new GuildMemberDailyReset());

            member.SetStats(player);
            member.AddFlag(GuildMemberFlags.Online);
        }

        public void SendEventAwayChanged(ObjectGuid memberGuid, bool afk, bool dnd)
        {
            Member member = GetMember(memberGuid);

            if (member == null)
                return;

            if (afk)
                member.AddFlag(GuildMemberFlags.AFK);
            else
                member.RemoveFlag(GuildMemberFlags.AFK);

            if (dnd)
                member.AddFlag(GuildMemberFlags.DND);
            else
                member.RemoveFlag(GuildMemberFlags.DND);

            GuildEventStatusChange statusChange = new();
            statusChange.Guid = memberGuid;
            statusChange.AFK = afk;
            statusChange.DND = dnd;
            BroadcastPacket(statusChange);
        }

        private void SendEventBankMoneyChanged()
        {
            GuildEventBankMoneyChanged eventPacket = new();
            eventPacket.Money = GetBankMoney();
            BroadcastPacket(eventPacket);
        }

        private void SendEventMOTD(WorldSession session, bool broadcast = false)
        {
            GuildEventMotd eventPacket = new();
            eventPacket.MotdText = GetMOTD();

            if (broadcast)
            {
                BroadcastPacket(eventPacket);
            }
            else
            {
                session.SendPacket(eventPacket);
                Log.outDebug(LogFilter.Guild, "SMSG_GUILD_EVENT_MOTD [{0}] ", session.GetPlayerInfo());
            }
        }

        private void SendEventNewLeader(Member newLeader, Member oldLeader, bool isSelfPromoted = false)
        {
            GuildEventNewLeader eventPacket = new();
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

        private void SendEventPlayerLeft(Player leaver, Player remover = null, bool isRemoved = false)
        {
            GuildEventPlayerLeft eventPacket = new();
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

        private void SendEventPresenceChanged(WorldSession session, bool loggedOn, bool broadcast = false)
        {
            Player player = session.GetPlayer();

            GuildEventPresenceChange eventPacket = new();
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
            _id = fields.Read<uint>(0);
            _name = fields.Read<string>(1);
            _leaderGuid = ObjectGuid.Create(HighGuid.Player, fields.Read<ulong>(2));

            if (!_emblemInfo.LoadFromDB(fields))
            {
                Log.outError(LogFilter.Guild,
                             "Guild {0} has invalid emblem colors (Background: {1}, Border: {2}, Emblem: {3}), skipped.",
                             _id,
                             _emblemInfo.GetBackgroundColor(),
                             _emblemInfo.GetBorderColor(),
                             _emblemInfo.GetColor());

                return false;
            }

            _info = fields.Read<string>(8);
            _motd = fields.Read<string>(9);
            _createdDate = fields.Read<uint>(10);
            _bankMoney = fields.Read<ulong>(11);

            byte purchasedTabs = (byte)fields.Read<uint>(12);

            if (purchasedTabs > GuildConst.MaxBankTabs)
                purchasedTabs = GuildConst.MaxBankTabs;

            _bankTabs.Clear();

            for (byte i = 0; i < purchasedTabs; ++i)
                _bankTabs.Add(new BankTab(_id, i));

            return true;
        }

        public void LoadRankFromDB(SQLFields field)
        {
            RankInfo rankInfo = new(_id);

            rankInfo.LoadFromDB(field);

            _ranks.Add(rankInfo);
        }

        public bool LoadMemberFromDB(SQLFields field)
        {
            ulong lowguid = field.Read<ulong>(1);
            ObjectGuid playerGuid = ObjectGuid.Create(HighGuid.Player, lowguid);

            Member member = new(_id, playerGuid, (GuildRankId)field.Read<byte>(2));
            bool isNew = _members.TryAdd(playerGuid, member);

            if (!isNew)
            {
                Log.outError(LogFilter.Guild, $"Tried to add {playerGuid} to guild '{_name}'. Member already exists.");

                return false;
            }

            if (!member.LoadFromDB(field))
            {
                _DeleteMemberFromDB(null, lowguid);

                return false;
            }

            Global.CharacterCacheStorage.UpdateCharacterGuildId(playerGuid, GetId());
            _members[member.GetGUID()] = member;

            return true;
        }

        public void LoadBankRightFromDB(SQLFields field)
        {
            // tabId              rights                slots
            GuildBankRightsAndSlots rightsAndSlots = new(field.Read<byte>(1), field.Read<sbyte>(3), field.Read<int>(4));
            // rankId
            _SetRankBankTabRightsAndSlots((GuildRankId)field.Read<byte>(2), rightsAndSlots, false);
        }

        public bool LoadEventLogFromDB(SQLFields field)
        {
            if (_eventLog.CanInsert())
            {
                _eventLog.LoadEvent(new EventLogEntry(_id,                                     // guild Id
                                                      field.Read<uint>(1),                     // Guid
                                                      field.Read<long>(6),                     // timestamp
                                                      (GuildEventLogTypes)field.Read<byte>(2), // event Type
                                                      field.Read<ulong>(3),                    // player Guid 1
                                                      field.Read<ulong>(4),                    // player Guid 2
                                                      field.Read<byte>(5)));                   // rank

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
                var pLog = _bankEventLog[tabId];

                if (pLog.CanInsert())
                {
                    uint guid = field.Read<uint>(2);
                    GuildBankEventLogTypes eventType = (GuildBankEventLogTypes)field.Read<byte>(3);

                    if (BankEventLogEntry.IsMoneyEvent(eventType))
                    {
                        if (!isMoneyTab)
                        {
                            Log.outError(LogFilter.Guild, "GuildBankEventLog ERROR: MoneyEvent(LogGuid: {0}, Guild: {1}) does not belong to money tab ({2}), ignoring...", guid, _id, dbTabId);

                            return false;
                        }
                    }
                    else if (isMoneyTab)
                    {
                        Log.outError(LogFilter.Guild, "GuildBankEventLog ERROR: non-money event (LogGuid: {0}, Guild: {1}) belongs to money tab, ignoring...", guid, _id);

                        return false;
                    }

                    pLog.LoadEvent(new BankEventLogEntry(_id,                   // guild Id
                                                         guid,                  // Guid
                                                         field.Read<long>(8),   // timestamp
                                                         dbTabId,               // tab Id
                                                         eventType,             // event Type
                                                         field.Read<ulong>(4),  // player Guid
                                                         field.Read<ulong>(5),  // Item or money
                                                         field.Read<ushort>(6), // itam stack Count
                                                         field.Read<byte>(7))); // dest tab Id
                }
            }

            return true;
        }

        public void LoadGuildNewsLogFromDB(SQLFields field)
        {
            if (!_newsLog.CanInsert())
                return;

            var news = new NewsLogEntry(_id,                                                      // guild Id
                                        field.Read<uint>(1),                                      // Guid
                                        field.Read<long>(6),                                      // timestamp //64 bits?
                                        (GuildNews)field.Read<byte>(2),                           // Type
                                        ObjectGuid.Create(HighGuid.Player, field.Read<ulong>(3)), // player Guid
                                        field.Read<uint>(4),                                      // Flags
                                        field.Read<uint>(5));                                     // value)

            _newsLog.LoadEvent(news);
        }

        public void LoadBankTabFromDB(SQLFields field)
        {
            byte tabId = field.Read<byte>(1);

            if (tabId >= _GetPurchasedTabsSize())
                Log.outError(LogFilter.Guild, "Invalid tab (tabId: {0}) in guild bank, skipped.", tabId);
            else
                _bankTabs[tabId].LoadFromDB(field);
        }

        public bool LoadBankItemFromDB(SQLFields field)
        {
            byte tabId = field.Read<byte>(52);

            if (tabId >= _GetPurchasedTabsSize())
            {
                Log.outError(LogFilter.Guild,
                             "Invalid tab for Item (GUID: {0}, Id: {1}) in guild bank, skipped.",
                             field.Read<uint>(0),
                             field.Read<uint>(1));

                return false;
            }

            return _bankTabs[tabId].LoadItemFromDB(field);
        }

        public bool Validate()
        {
            // Validate ranks _data
            // GUILD RANKS represent a sequence starting from 0 = GUILD_MASTER (ALL PRIVILEGES) to max 9 (lowest privileges).
            // The lower rank Id is considered higher rank - so promotion does rank-- and demotion does rank++
            // Between ranks in sequence cannot be gaps - so 0, 1, 2, 4 is impossible
            // Min ranks Count is 2 and max is 10.
            bool broken_ranks = false;
            byte ranks = _GetRanksSize();

            SQLTransaction trans = new();

            if (ranks < GuildConst.MinRanks ||
                ranks > GuildConst.MaxRanks)
            {
                Log.outError(LogFilter.Guild, "Guild {0} has invalid number of ranks, creating new...", _id);
                broken_ranks = true;
            }
            else
            {
                for (byte rankId = 0; rankId < ranks; ++rankId)
                {
                    RankInfo rankInfo = GetRankInfo((GuildRankId)rankId);

                    if (rankInfo.GetId() != (GuildRankId)rankId)
                    {
                        Log.outError(LogFilter.Guild, "Guild {0} has broken rank Id {1}, creating default set of ranks...", _id, rankId);
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
                _ranks.Clear();
                _CreateDefaultGuildRanks(trans, SharedConst.DefaultLocale);
            }

            // Validate members' _data
            foreach (var member in _members.Values)
                if (GetRankInfo(member.GetRankId()) == null)
                    member.ChangeRank(trans, _GetLowestRankId());

            // Repair the structure of the guild.
            // If the guildmaster doesn't exist or isn't member of the guild
            // attempt to promote another member.
            Member leader = GetMember(_leaderGuid);

            if (leader == null)
            {
                DeleteMember(trans, _leaderGuid);

                // If no more members left, disband guild
                if (_members.Empty())
                {
                    Disband();

                    return false;
                }
            }
            else if (!leader.IsRank(GuildRankId.GuildMaster))
            {
                _SetLeader(trans, leader);
            }

            if (trans.commands.Count > 0)
                DB.Characters.CommitTransaction(trans);

            _UpdateAccountsNumber();

            return true;
        }

        public void BroadcastToGuild(WorldSession session, bool officerOnly, string msg, Language language)
        {
            if (session != null &&
                session.GetPlayer() != null &&
                _HasRankRight(session.GetPlayer(), officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
            {
                ChatPkt data = new();
                data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, language, session.GetPlayer(), null, msg);

                foreach (var member in _members.Values)
                {
                    Player player = member.FindPlayer();

                    if (player != null)
                        if (player.GetSession() != null &&
                            _HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
                            !player.GetSocial().HasIgnore(session.GetPlayer().GetGUID(), session.GetAccountGUID()))
                            player.SendPacket(data);
                }
            }
        }

        public void BroadcastAddonToGuild(WorldSession session, bool officerOnly, string msg, string prefix, bool isLogged)
        {
            if (session != null &&
                session.GetPlayer() != null &&
                _HasRankRight(session.GetPlayer(), officerOnly ? GuildRankRights.OffChatSpeak : GuildRankRights.GChatSpeak))
            {
                ChatPkt data = new();
                data.Initialize(officerOnly ? ChatMsg.Officer : ChatMsg.Guild, isLogged ? Language.AddonLogged : Language.Addon, session.GetPlayer(), null, msg, 0, "", Locale.enUS, prefix);

                foreach (var member in _members.Values)
                {
                    Player player = member.FindPlayer();

                    if (player)
                        if (player.GetSession() != null &&
                            _HasRankRight(player, officerOnly ? GuildRankRights.OffChatListen : GuildRankRights.GChatListen) &&
                            !player.GetSocial().HasIgnore(session.GetPlayer().GetGUID(), session.GetAccountGUID()) &&
                            player.GetSession().IsAddonRegistered(prefix))
                            player.SendPacket(data);
                }
            }
        }

        public void BroadcastPacketToRank(ServerPacket packet, GuildRankId rankId)
        {
            foreach (var member in _members.Values)
                if (member.IsRank(rankId))
                {
                    Player player = member.FindPlayer();

                    player?.SendPacket(packet);
                }
        }

        public void BroadcastPacket(ServerPacket packet)
        {
            foreach (var member in _members.Values)
            {
                Player player = member.FindPlayer();

                player?.SendPacket(packet);
            }
        }

        public void BroadcastPacketIfTrackingAchievement(ServerPacket packet, uint criteriaId)
        {
            foreach (var member in _members.Values)
                if (member.IsTrackingCriteriaId(criteriaId))
                {
                    Player player = member.FindPlayer();

                    if (player)
                        player.SendPacket(packet);
                }
        }

        public void MassInviteToEvent(WorldSession session, uint minLevel, uint maxLevel, GuildRankOrder minRank)
        {
            CalendarCommunityInvite packet = new();

            foreach (var (guid, member) in _members)
            {
                // not sure if needed, maybe client checks it as well
                if (packet.Invites.Count >= SharedConst.CalendarMaxInvites)
                {
                    Player player = session.GetPlayer();

                    if (player != null)
                        Global.CalendarMgr.SendCalendarCommandResult(player.GetGUID(), CalendarError.InvitesExceeded);

                    return;
                }

                if (guid == session.GetPlayer().GetGUID())
                    continue;

                uint level = Global.CharacterCacheStorage.GetCharacterLevelByGuid(guid);

                if (level < minLevel ||
                    level > maxLevel)
                    continue;

                RankInfo rank = GetRankInfo(member.GetRankId());

                if (rank.GetOrder() > minRank)
                    continue;

                packet.Invites.Add(new CalendarEventInitialInviteInfo(guid, (byte)level));
            }

            session.SendPacket(packet);
        }

        public bool AddMember(SQLTransaction trans, ObjectGuid guid, GuildRankId? rankId = null)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            // Player cannot be in guild
            if (player != null)
            {
                if (player.GetGuildId() != 0)
                    return false;
            }
            else if (Global.CharacterCacheStorage.GetCharacterGuildIdByGuid(guid) != 0)
            {
                return false;
            }

            // Remove all player signs from another petitions
            // This will be prevent attempt to join many guilds and corrupt guild _data integrity
            Player.RemovePetitionsAndSigns(guid);

            ulong lowguid = guid.GetCounter();

            // If rank was not passed, assign lowest possible rank
            if (!rankId.HasValue)
                rankId = _GetLowestRankId();

            Member member = new(_id, guid, rankId.Value);
            bool isNew = _members.TryAdd(guid, member);

            if (!isNew)
            {
                Log.outError(LogFilter.Guild, $"Tried to add {guid} to guild '{_name}'. Member already exists.");

                return false;
            }

            string name = "";

            if (player != null)
            {
                _members[guid] = member;
                player.SetInGuild(_id);
                player.SetGuildIdInvited(0);
                player.SetGuildRank((byte)rankId);
                player.SetGuildLevel(GetLevel());
                member.SetStats(player);
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

                    member.SetStats(name,
                                    result.Read<byte>(1),
                                    (Race)result.Read<byte>(2),
                                    (Class)result.Read<byte>(3),
                                    (Gender)result.Read<byte>(4),
                                    result.Read<ushort>(5),
                                    result.Read<uint>(6),
                                    0);

                    ok = member.CheckStats();
                }

                if (!ok)
                    return false;

                _members[guid] = member;
                Global.CharacterCacheStorage.UpdateCharacterGuildId(guid, GetId());
            }

            member.SaveToDB(trans);

            _UpdateAccountsNumber();
            _LogEvent(GuildEventLogTypes.JoinGuild, lowguid);

            GuildEventPlayerJoined joinNotificationPacket = new();
            joinNotificationPacket.Guid = guid;
            joinNotificationPacket.Name = name;
            joinNotificationPacket.VirtualRealmAddress = Global.WorldMgr.GetVirtualRealmAddress();
            BroadcastPacket(joinNotificationPacket);

            // Call scripts if member was succesfully added (and stored to database)
            Global.ScriptMgr.ForEach<IGuildOnAddMember>(p => p.OnAddMember(this, player, (byte)rankId));

            return true;
        }

        public void DeleteMember(SQLTransaction trans, ObjectGuid guid, bool isDisbanding = false, bool isKicked = false, bool canDeleteGuild = false)
        {
            Player player = Global.ObjAccessor.FindPlayer(guid);

            // Guild master can be deleted when loading guild and Guid doesn't exist in characters table
            // or when he is removed from guild by gm command
            if (_leaderGuid == guid &&
                !isDisbanding)
            {
                Member oldLeader = null;
                Member newLeader = null;

                foreach (var (memberGuid, member) in _members)
                    if (memberGuid == guid)
                        oldLeader = member;
                    else if (newLeader == null ||
                             newLeader.GetRankId() > member.GetRankId())
                        newLeader = member;

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
            Global.ScriptMgr.ForEach<IGuildOnRemoveMember>(p => p.OnRemoveMember(this, player, isDisbanding, isKicked));

            _members.Remove(guid);

            // If player not online _data in _data field will be loaded from guild tabs no need to update it !!
            if (player != null)
            {
                player.SetInGuild(0);
                player.SetGuildRank(0);
                player.SetGuildLevel(0);

                foreach (var entry in CliDB.GuildPerkSpellsStorage.Values)
                    player.RemoveSpell(entry.SpellID, false, false);
            }
            else
            {
                Global.CharacterCacheStorage.UpdateCharacterGuildId(guid, 0);
            }

            _DeleteMemberFromDB(trans, guid.GetCounter());

            if (!isDisbanding)
                _UpdateAccountsNumber();
        }

        public bool ChangeMemberRank(SQLTransaction trans, ObjectGuid guid, GuildRankId newRank)
        {
            if (GetRankInfo(newRank) != null) // Validate rank (allow only existing ranks)
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
            return _members.ContainsKey(guid);
        }

        public ulong GetMemberAvailableMoneyForRepairItems(ObjectGuid guid)
        {
            Member member = GetMember(guid);

            if (member == null)
                return 0;

            return Math.Min(_bankMoney, (ulong)_GetMemberRemainingMoney(member));
        }

        public void SwapItems(Player player, byte tabId, byte slotId, byte destTabId, byte destSlotId, uint splitedAmount)
        {
            if (tabId >= _GetPurchasedTabsSize() ||
                slotId >= GuildConst.MaxBankSlots ||
                destTabId >= _GetPurchasedTabsSize() ||
                destSlotId >= GuildConst.MaxBankSlots)
                return;

            if (tabId == destTabId &&
                slotId == destSlotId)
                return;

            BankMoveItemData from = new(this, player, tabId, slotId);
            BankMoveItemData to = new(this, player, destTabId, destSlotId);
            _MoveItems(from, to, splitedAmount);
        }

        public void SwapItemsWithInventory(Player player, bool toChar, byte tabId, byte slotId, byte playerBag, byte playerSlotId, uint splitedAmount)
        {
            if ((slotId >= GuildConst.MaxBankSlots && slotId != ItemConst.NullSlot) ||
                tabId >= _GetPurchasedTabsSize())
                return;

            BankMoveItemData bankData = new(this, player, tabId, slotId);
            PlayerMoveItemData charData = new(this, player, playerBag, playerSlotId);

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

                GuildEventTabTextChanged eventPacket = new();
                eventPacket.Tab = tabId;
                BroadcastPacket(eventPacket);
            }
        }

        private RankInfo GetRankInfo(GuildRankId rankId)
        {
            return _ranks.Find(rank => rank.GetId() == rankId);
        }

        private RankInfo GetRankInfo(GuildRankOrder rankOrder)
        {
            return _ranks.Find(rank => rank.GetOrder() == rankOrder);
        }

        // Private methods
        private void _CreateNewBankTab()
        {
            byte tabId = _GetPurchasedTabsSize(); // Next free Id
            _bankTabs.Add(new BankTab(_id, tabId));

            SQLTransaction trans = new();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_TAB);
            stmt.AddValue(0, _id);
            stmt.AddValue(1, tabId);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_TAB);
            stmt.AddValue(0, _id);
            stmt.AddValue(1, tabId);
            trans.Append(stmt);

            ++tabId;

            foreach (var rank in _ranks)
                rank.CreateMissingTabsIfNeeded(tabId, trans, false);

            DB.Characters.CommitTransaction(trans);
        }

        private void _CreateDefaultGuildRanks(SQLTransaction trans, Locale loc = Locale.enUS)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_RANKS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_RIGHTS);
            stmt.AddValue(0, _id);
            trans.Append(stmt);

            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildMaster, loc), GuildRankRights.All);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildOfficer, loc), GuildRankRights.All);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildVeteran, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildMember, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
            _CreateRank(trans, Global.ObjectMgr.GetCypherString(CypherStrings.GuildInitiate, loc), GuildRankRights.GChatListen | GuildRankRights.GChatSpeak);
        }

        private bool _CreateRank(SQLTransaction trans, string name, GuildRankRights rights)
        {
            if (_ranks.Count >= GuildConst.MaxRanks)
                return false;

            byte newRankId = 0;

            while (GetRankInfo((GuildRankId)newRankId) != null)
                ++newRankId;

            // Ranks represent sequence 0, 1, 2, ... where 0 means guildmaster
            RankInfo info = new(_id, (GuildRankId)newRankId, (GuildRankOrder)_ranks.Count, name, rights, 0);
            _ranks.Add(info);

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

        private void _UpdateAccountsNumber()
        {
            // We use a set to be sure each element will be unique
            List<uint> accountsIdSet = new();

            foreach (var member in _members.Values)
                accountsIdSet.Add(member.GetAccountId());

            _accountsNumber = (uint)accountsIdSet.Count;
        }

        private bool _IsLeader(Player player)
        {
            if (player.GetGUID() == _leaderGuid)
                return true;

            Member member = GetMember(player.GetGUID());

            if (member != null)
                return member.IsRank(GuildRankId.GuildMaster);

            return false;
        }

        private void _DeleteBankItems(SQLTransaction trans, bool removeItemsFromDB)
        {
            for (byte tabId = 0; tabId < _GetPurchasedTabsSize(); ++tabId)
            {
                _bankTabs[tabId].Delete(trans, removeItemsFromDB);
                _bankTabs[tabId] = null;
            }

            _bankTabs.Clear();
        }

        private bool _ModifyBankMoney(SQLTransaction trans, ulong amount, bool add)
        {
            if (add)
            {
                _bankMoney += amount;
            }
            else
            {
                // Check if there is enough money in bank.
                if (_bankMoney < amount)
                    return false;

                _bankMoney -= amount;
            }

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_MONEY);
            stmt.AddValue(0, _bankMoney);
            stmt.AddValue(1, _id);
            trans.Append(stmt);

            return true;
        }

        private void _SetLeader(SQLTransaction trans, Member leader)
        {
            bool isInTransaction = trans != null;

            if (!isInTransaction)
                trans = new SQLTransaction();

            _leaderGuid = leader.GetGUID();
            leader.ChangeRank(trans, GuildRankId.GuildMaster);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_LEADER);
            stmt.AddValue(0, _leaderGuid.GetCounter());
            stmt.AddValue(1, _id);
            trans.Append(stmt);

            if (!isInTransaction)
                DB.Characters.CommitTransaction(trans);
        }

        private void _SetRankBankMoneyPerDay(GuildRankId rankId, uint moneyPerDay)
        {
            RankInfo rankInfo = GetRankInfo(rankId);

            rankInfo?.SetBankMoneyPerDay(moneyPerDay);
        }

        private void _SetRankBankTabRightsAndSlots(GuildRankId rankId, GuildBankRightsAndSlots rightsAndSlots, bool saveToDB = true)
        {
            if (rightsAndSlots.GetTabId() >= _GetPurchasedTabsSize())
                return;

            RankInfo rankInfo = GetRankInfo(rankId);

            rankInfo?.SetBankTabSlotsAndRights(rightsAndSlots, saveToDB);
        }

        private string _GetRankName(GuildRankId rankId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);

            if (rankInfo != null)
                return rankInfo.GetName();

            return "<unknown>";
        }

        private GuildRankRights _GetRankRights(GuildRankId rankId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);

            if (rankInfo != null)
                return rankInfo.GetRights();

            return 0;
        }

        private uint _GetRankBankMoneyPerDay(GuildRankId rankId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);

            if (rankInfo != null)
                return rankInfo.GetBankMoneyPerDay();

            return 0;
        }

        private int _GetRankBankTabSlotsPerDay(GuildRankId rankId, byte tabId)
        {
            if (tabId < _GetPurchasedTabsSize())
            {
                RankInfo rankInfo = GetRankInfo(rankId);

                if (rankInfo != null)
                    return rankInfo.GetBankTabSlotsPerDay(tabId);
            }

            return 0;
        }

        private GuildBankRights _GetRankBankTabRights(GuildRankId rankId, byte tabId)
        {
            RankInfo rankInfo = GetRankInfo(rankId);

            if (rankInfo != null)
                return rankInfo.GetBankTabRights(tabId);

            return 0;
        }

        private int _GetMemberRemainingSlots(Member member, byte tabId)
        {
            GuildRankId rankId = member.GetRankId();

            if (rankId == GuildRankId.GuildMaster)
                return GuildConst.WithdrawSlotUnlimited;

            if ((_GetRankBankTabRights(rankId, tabId) & GuildBankRights.ViewTab) != 0)
            {
                int remaining = _GetRankBankTabSlotsPerDay(rankId, tabId) - (int)member.GetBankTabWithdrawValue(tabId);

                if (remaining > 0)
                    return remaining;
            }

            return 0;
        }

        private long _GetMemberRemainingMoney(Member member)
        {
            GuildRankId rankId = member.GetRankId();

            if (rankId == GuildRankId.GuildMaster)
                return long.MaxValue;

            if ((_GetRankRights(rankId) & (GuildRankRights.WithdrawRepair | GuildRankRights.WithdrawGold)) != 0)
            {
                long remaining = (long)((_GetRankBankMoneyPerDay(rankId) * MoneyConstants.Gold) - member.GetBankMoneyWithdrawValue());

                if (remaining > 0)
                    return remaining;
            }

            return 0;
        }

        private void _UpdateMemberWithdrawSlots(SQLTransaction trans, ObjectGuid guid, byte tabId)
        {
            Member member = GetMember(guid);

            member?.UpdateBankTabWithdrawValue(trans, tabId, 1);
        }

        private bool _MemberHasTabRights(ObjectGuid guid, byte tabId, GuildBankRights rights)
        {
            Member member = GetMember(guid);

            if (member != null)
            {
                // Leader always has full rights
                if (member.IsRank(GuildRankId.GuildMaster) ||
                    _leaderGuid == guid)
                    return true;

                return (_GetRankBankTabRights(member.GetRankId(), tabId) & rights) == rights;
            }

            return false;
        }

        private void _LogEvent(GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2 = 0, byte newRank = 0)
        {
            SQLTransaction trans = new();
            _eventLog.AddEvent(trans, new EventLogEntry(_id, _eventLog.GetNextGUID(), eventType, playerGuid1, playerGuid2, newRank));
            DB.Characters.CommitTransaction(trans);

            Global.ScriptMgr.ForEach<IGuildOnEvent>(p => p.OnEvent(this, (byte)eventType, playerGuid1, playerGuid2, newRank));
        }

        private void _LogBankEvent(SQLTransaction trans, GuildBankEventLogTypes eventType, byte tabId, ulong lowguid, uint itemOrMoney, ushort itemStackCount = 0, byte destTabId = 0)
        {
            if (tabId > GuildConst.MaxBankTabs)
                return;

            // not logging moves within the same tab
            if (eventType == GuildBankEventLogTypes.MoveItem &&
                tabId == destTabId)
                return;

            byte dbTabId = tabId;

            if (BankEventLogEntry.IsMoneyEvent(eventType))
            {
                tabId = GuildConst.MaxBankTabs;
                dbTabId = GuildConst.BankMoneyLogsTab;
            }

            var pLog = _bankEventLog[tabId];
            pLog.AddEvent(trans, new BankEventLogEntry(_id, pLog.GetNextGUID(), eventType, dbTabId, lowguid, itemOrMoney, itemStackCount, destTabId));

            Global.ScriptMgr.ForEach<IGuildOnBankEvent>(p => p.OnBankEvent(this, (byte)eventType, tabId, lowguid, itemOrMoney, itemStackCount, destTabId));
        }

        private Item _GetItem(byte tabId, byte slotId)
        {
            BankTab tab = GetBankTab(tabId);

            if (tab != null)
                return tab.GetItem(slotId);

            return null;
        }

        private void _RemoveItem(SQLTransaction trans, byte tabId, byte slotId)
        {
            BankTab pTab = GetBankTab(tabId);

            pTab?.SetItem(trans, slotId, null);
        }

        private void _MoveItems(MoveItemData pSrc, MoveItemData pDest, uint splitedAmount)
        {
            // 1. Initialize source Item
            if (!pSrc.InitItem())
                return; // No source Item

            // 2. Check source Item
            if (!pSrc.CheckItem(ref splitedAmount))
                return; // Source Item or splited amount is invalid

            // 3. Check destination rights
            if (!pDest.HasStoreRights(pSrc))
                return; // Player has no rights to store Item in destination

            // 4. Check source withdraw rights
            if (!pSrc.HasWithdrawRights(pDest))
                return; // Player has no rights to withdraw items from source

            // 5. Check split
            if (splitedAmount != 0)
            {
                // 5.1. Clone source Item
                if (!pSrc.CloneItem(splitedAmount))
                    return; // Item could not be cloned

                // 5.2. Move splited Item to destination
                _DoItemsMove(pSrc, pDest, true, splitedAmount);
            }
            else // 6. No split
            {
                // 6.1. Try to merge items in destination (pDest.GetItem() == NULL)
                InventoryResult mergeAttemptResult = _DoItemsMove(pSrc, pDest, false);

                if (mergeAttemptResult != InventoryResult.Ok) // Item could not be merged
                {
                    // 6.2. Try to swap items
                    // 6.2.1. Initialize destination Item
                    if (!pDest.InitItem())
                    {
                        pSrc.SendEquipError(mergeAttemptResult, pSrc.GetItem(false));

                        return;
                    }

                    // 6.2.2. Check rights to store Item in source (opposite direction)
                    if (!pSrc.HasStoreRights(pDest))
                        return; // Player has no rights to store Item in source (opposite direction)

                    if (!pDest.HasWithdrawRights(pSrc))
                        return; // Player has no rights to withdraw Item from destination (opposite direction)

                    // 6.2.3. Swap items (pDest.GetItem() != NULL)
                    _DoItemsMove(pSrc, pDest, true);
                }
            }

            // 7. Send changes
            _SendBankContentUpdate(pSrc, pDest);
        }

        private InventoryResult _DoItemsMove(MoveItemData pSrc, MoveItemData pDest, bool sendError, uint splitedAmount = 0)
        {
            Item pDestItem = pDest.GetItem();
            bool swap = (pDestItem != null);

            Item pSrcItem = pSrc.GetItem(splitedAmount != 0);
            // 1. Can store source Item in destination
            InventoryResult destResult = pDest.CanStore(pSrcItem, swap, sendError);

            if (destResult != InventoryResult.Ok)
                return destResult;

            // 2. Can store destination Item in source
            if (swap)
            {
                InventoryResult srcResult = pSrc.CanStore(pDestItem, true, true);

                if (srcResult != InventoryResult.Ok)
                    return srcResult;
            }

            // GM LOG (@todo move to scripts)
            pDest.LogAction(pSrc);

            if (swap)
                pSrc.LogAction(pDest);

            SQLTransaction trans = new();
            // 3. Log bank events
            pDest.LogBankEvent(trans, pSrc, pSrcItem.GetCount());

            if (swap)
                pSrc.LogBankEvent(trans, pDest, pDestItem.GetCount());

            // 4. Remove Item from source
            pSrc.RemoveItem(trans, pDest, splitedAmount);

            // 5. Remove Item from destination
            if (swap)
                pDest.RemoveItem(trans, pSrc);

            // 6. Store Item in destination
            pDest.StoreItem(trans, pSrcItem);

            // 7. Store Item in source
            if (swap)
                pSrc.StoreItem(trans, pDestItem);

            DB.Characters.CommitTransaction(trans);

            return InventoryResult.Ok;
        }

        private void _SendBankContentUpdate(MoveItemData pSrc, MoveItemData pDest)
        {
            Cypher.Assert(pSrc.IsBank() || pDest.IsBank());

            byte tabId = 0;
            List<byte> slots = new();

            if (pSrc.IsBank()) // B .
            {
                tabId = pSrc.GetContainer();
                slots.Insert(0, pSrc.GetSlotId());

                if (pDest.IsBank()) // B . B
                {
                    // Same tab - add destination slots to collection
                    if (pDest.GetContainer() == pSrc.GetContainer())
                    {
                        pDest.CopySlots(slots);
                    }
                    else // Different tabs - send second message
                    {
                        List<byte> destSlots = new();
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

        private void _SendBankContentUpdate(byte tabId, List<byte> slots)
        {
            BankTab tab = GetBankTab(tabId);

            if (tab != null)
            {
                GuildBankQueryResults packet = new();
                packet.FullUpdate = true; // @todo
                packet.Tab = tabId;
                packet.Money = _bankMoney;

                foreach (var slot in slots)
                {
                    Item tabItem = tab.GetItem(slot);

                    GuildBankItemInfo itemInfo = new();

                    itemInfo.Slot = slot;
                    itemInfo.Item.ItemID = tabItem ? tabItem.GetEntry() : 0;
                    itemInfo.Count = (int)(tabItem ? tabItem.GetCount() : 0);
                    itemInfo.EnchantmentID = (int)(tabItem ? tabItem.GetEnchantmentId(EnchantmentSlot.Perm) : 0);
                    itemInfo.Charges = tabItem ? Math.Abs(tabItem.GetSpellCharges()) : 0;
                    itemInfo.OnUseEnchantmentID = (int)(tabItem ? tabItem.GetEnchantmentId(EnchantmentSlot.Use) : 0);
                    itemInfo.Flags = 0;
                    itemInfo.Locked = false;

                    if (tabItem != null)
                    {
                        byte i = 0;

                        foreach (SocketedGem gemData in tabItem._itemData.Gems)
                        {
                            if (gemData.ItemId != 0)
                            {
                                ItemGemData gem = new();
                                gem.Slot = i;
                                gem.Item = new ItemInstance(gemData);
                                itemInfo.SocketEnchant.Add(gem);
                            }

                            ++i;
                        }
                    }

                    packet.ItemInfo.Add(itemInfo);
                }

                foreach (var (guid, member) in _members)
                {
                    if (!_MemberHasTabRights(guid, tabId, GuildBankRights.ViewTab))
                        continue;

                    Player player = member.FindPlayer();

                    if (player == null)
                        continue;

                    packet.WithdrawalsRemaining = _GetMemberRemainingSlots(member, tabId);
                    player.SendPacket(packet);
                }
            }
        }

        public void SendBankList(WorldSession session, byte tabId, bool fullUpdate)
        {
            Member member = GetMember(session.GetPlayer().GetGUID());

            if (member == null) // Shouldn't happen, just in case
                return;

            GuildBankQueryResults packet = new();

            packet.Money = _bankMoney;
            packet.WithdrawalsRemaining = _GetMemberRemainingSlots(member, tabId);
            packet.Tab = tabId;
            packet.FullUpdate = fullUpdate;

            // TabInfo
            if (fullUpdate)
                for (byte i = 0; i < _GetPurchasedTabsSize(); ++i)
                {
                    GuildBankTabInfo tabInfo;
                    tabInfo.TabIndex = i;
                    tabInfo.Name = _bankTabs[i].GetName();
                    tabInfo.Icon = _bankTabs[i].GetIcon();
                    packet.TabInfo.Add(tabInfo);
                }

            if (fullUpdate && _MemberHasTabRights(session.GetPlayer().GetGUID(), tabId, GuildBankRights.ViewTab))
            {
                BankTab tab = GetBankTab(tabId);

                if (tab != null)
                    for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
                    {
                        Item tabItem = tab.GetItem(slotId);

                        if (tabItem)
                        {
                            GuildBankItemInfo itemInfo = new();

                            itemInfo.Slot = slotId;
                            itemInfo.Item.ItemID = tabItem.GetEntry();
                            itemInfo.Count = (int)tabItem.GetCount();
                            itemInfo.Charges = Math.Abs(tabItem.GetSpellCharges());
                            itemInfo.EnchantmentID = (int)tabItem.GetEnchantmentId(EnchantmentSlot.Perm);
                            itemInfo.OnUseEnchantmentID = (int)tabItem.GetEnchantmentId(EnchantmentSlot.Use);
                            itemInfo.Flags = tabItem._itemData.DynamicFlags;

                            byte i = 0;

                            foreach (SocketedGem gemData in tabItem._itemData.Gems)
                            {
                                if (gemData.ItemId != 0)
                                {
                                    ItemGemData gem = new();
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

            session.SendPacket(packet);
        }

        private void SendGuildRanksUpdate(ObjectGuid setterGuid, ObjectGuid targetGuid, GuildRankId rank)
        {
            Member member = GetMember(targetGuid);
            Cypher.Assert(member != null);

            GuildSendRankChange rankChange = new();
            rankChange.Officer = setterGuid;
            rankChange.Other = targetGuid;
            rankChange.RankID = (byte)rank;
            rankChange.Promote = (rank < member.GetRankId());
            BroadcastPacket(rankChange);

            member.ChangeRank(null, rank);

            Log.outDebug(LogFilter.Network, "SMSG_GUILD_RANKS_UPDATE [Broadcast] Target: {0}, Issuer: {1}, RankId: {2}", targetGuid.ToString(), setterGuid.ToString(), rank);
        }

        public void ResetTimes(bool weekly)
        {
            foreach (var member in _members.Values)
            {
                member.ResetValues(weekly);
                Player player = member.FindPlayer();

                // tells the client to request bank withdrawal limit
                player?.SendPacket(new GuildMemberDailyReset());
            }
        }

        public void AddGuildNews(GuildNews type, ObjectGuid guid, uint flags, uint value)
        {
            SQLTransaction trans = new();
            NewsLogEntry news = _newsLog.AddEvent(trans, new NewsLogEntry(_id, _newsLog.GetNextGUID(), type, guid, flags, value));
            DB.Characters.CommitTransaction(trans);

            GuildNewsPkt newsPacket = new();
            news.WritePacket(newsPacket);
            BroadcastPacket(newsPacket);
        }

        private bool HasAchieved(uint achievementId)
        {
            return GetAchievementMgr().HasAchieved(achievementId);
        }

        public void UpdateCriteria(CriteriaType type, ulong miscValue1, ulong miscValue2, ulong miscValue3, WorldObject refe, Player player)
        {
            GetAchievementMgr().UpdateCriteria(type, miscValue1, miscValue2, miscValue3, refe, player);
        }

        public void HandleNewsSetSticky(WorldSession session, uint newsId, bool sticky)
        {
            var newsLog = _newsLog.GetGuildLog().Find(p => p.GetGUID() == newsId);

            if (newsLog == null)
            {
                Log.outDebug(LogFilter.Guild, "HandleNewsSetSticky: [{0}] requested unknown newsId {1} - Sticky: {2}", session.GetPlayerInfo(), newsId, sticky);

                return;
            }

            newsLog.SetSticky(sticky);

            GuildNewsPkt newsPacket = new();
            newsLog.WritePacket(newsPacket);
            session.SendPacket(newsPacket);
        }

        public ulong GetId()
        {
            return _id;
        }

        public ObjectGuid GetGUID()
        {
            return ObjectGuid.Create(HighGuid.Guild, _id);
        }

        public ObjectGuid GetLeaderGUID()
        {
            return _leaderGuid;
        }

        public string GetName()
        {
            return _name;
        }

        public string GetMOTD()
        {
            return _motd;
        }

        public string GetInfo()
        {
            return _info;
        }

        public long GetCreatedDate()
        {
            return _createdDate;
        }

        public ulong GetBankMoney()
        {
            return _bankMoney;
        }

        public void BroadcastWorker(IDoWork<Player> _do, Player except = null)
        {
            foreach (var member in _members.Values)
            {
                Player player = member.FindPlayer();

                if (player != null)
                    if (player != except)
                        _do.Invoke(player);
            }
        }

        public int GetMembersCount()
        {
            return _members.Count;
        }

        public GuildAchievementMgr GetAchievementMgr()
        {
            return _achievementSys;
        }

        // Pre-6.x guild leveling
        public byte GetLevel()
        {
            return GuildConst.OldMaxLevel;
        }

        public EmblemInfo GetEmblemInfo()
        {
            return _emblemInfo;
        }

        private byte _GetRanksSize()
        {
            return (byte)_ranks.Count;
        }

        private RankInfo GetRankInfo(uint rankId)
        {
            return rankId < _GetRanksSize() ? _ranks[(int)rankId] : null;
        }

        private bool _HasRankRight(Player player, GuildRankRights right)
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

        private GuildRankId _GetLowestRankId()
        {
            return _ranks.Last().GetId();
        }

        private byte _GetPurchasedTabsSize()
        {
            return (byte)_bankTabs.Count;
        }

        private BankTab GetBankTab(byte tabId)
        {
            return tabId < _bankTabs.Count ? _bankTabs[tabId] : null;
        }

        public Member GetMember(ObjectGuid guid)
        {
            return _members.LookupByKey(guid);
        }

        public Member GetMember(string name)
        {
            foreach (var member in _members.Values)
                if (member.GetName() == name)
                    return member;

            return null;
        }

        private void _DeleteMemberFromDB(SQLTransaction trans, ulong lowguid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_MEMBER);
            stmt.AddValue(0, lowguid);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        private ulong GetGuildBankTabPrice(byte tabId)
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
            GuildCommandResult resultPacket = new();
            resultPacket.Command = type;
            resultPacket.Result = errCode;
            resultPacket.Name = param;
            session.SendPacket(resultPacket);
        }

        public static void SendSaveEmblemResult(WorldSession session, GuildEmblemError errCode)
        {
            PlayerSaveGuildEmblem saveResponse = new();
            saveResponse.Error = errCode;
            session.SendPacket(saveResponse);
        }

        public static implicit operator bool(Guild guild)
        {
            return guild != null;
        }

        #region Fields

        private ulong _id;
        private string _name;
        private ObjectGuid _leaderGuid;
        private string _motd;
        private string _info;
        private long _createdDate;

        private EmblemInfo _emblemInfo = new();
        private uint _accountsNumber;
        private ulong _bankMoney;

        private readonly List<RankInfo> _ranks = new();
        private readonly Dictionary<ObjectGuid, Member> _members = new();
        private readonly List<BankTab> _bankTabs = new();

        // These are actually ordered lists. The first element is the oldest entry.
        private readonly LogHolder<EventLogEntry> _eventLog = new();
        private readonly LogHolder<BankEventLogEntry>[] _bankEventLog = new LogHolder<BankEventLogEntry>[GuildConst.MaxBankTabs + 1];
        private readonly LogHolder<NewsLogEntry> _newsLog = new();
        private readonly GuildAchievementMgr _achievementSys;

        #endregion

        #region Classes

        public class Member
        {
            public Member(ulong guildId, ObjectGuid guid, GuildRankId rankId)
            {
                _guildId = guildId;
                _guid = guid;
                _zoneId = 0;
                _level = 0;
                _class = 0;
                _flags = GuildMemberFlags.None;
                _logoutTime = (ulong)GameTime.GetGameTime();
                _accountId = 0;
                _rankId = rankId;
                _achievementPoints = 0;
                _totalActivity = 0;
                _weekActivity = 0;
                _totalReputation = 0;
                _weekReputation = 0;
            }

            public void SetStats(Player player)
            {
                _name = player.GetName();
                _level = (byte)player.GetLevel();
                _race = player.GetRace();
                _class = player.GetClass();
                _gender = player.GetNativeGender();
                _zoneId = player.GetZoneId();
                _accountId = player.GetSession().GetAccountId();
                _achievementPoints = player.GetAchievementPoints();
            }

            public void SetStats(string name, byte level, Race race, Class _class, Gender gender, uint zoneId, uint accountId, uint reputation)
            {
                _name = name;
                _level = level;
                _race = race;
                this._class = _class;
                _gender = gender;
                _zoneId = zoneId;
                _accountId = accountId;
                _totalReputation = reputation;
            }

            public void SetPublicNote(string publicNote)
            {
                if (_publicNote == publicNote)
                    return;

                _publicNote = publicNote;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_PNOTE);
                stmt.AddValue(0, publicNote);
                stmt.AddValue(1, _guid.GetCounter());
                DB.Characters.Execute(stmt);
            }

            public void SetOfficerNote(string officerNote)
            {
                if (_officerNote == officerNote)
                    return;

                _officerNote = officerNote;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_OFFNOTE);
                stmt.AddValue(0, officerNote);
                stmt.AddValue(1, _guid.GetCounter());
                DB.Characters.Execute(stmt);
            }

            public void ChangeRank(SQLTransaction trans, GuildRankId newRank)
            {
                _rankId = newRank;

                // Update rank information in player's field, if he is online.
                Player player = FindConnectedPlayer();

                player?.SetGuildRank((byte)newRank);

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_MEMBER_RANK);
                stmt.AddValue(0, (byte)newRank);
                stmt.AddValue(1, _guid.GetCounter());
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void SaveToDB(SQLTransaction trans)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER);
                stmt.AddValue(0, _guildId);
                stmt.AddValue(1, _guid.GetCounter());
                stmt.AddValue(2, (byte)_rankId);
                stmt.AddValue(3, _publicNote);
                stmt.AddValue(4, _officerNote);
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public bool LoadFromDB(SQLFields field)
            {
                _publicNote = field.Read<string>(3);
                _officerNote = field.Read<string>(4);

                for (byte i = 0; i < GuildConst.MaxBankTabs; ++i)
                    _bankWithdraw[i] = field.Read<uint>(5 + i);

                _bankWithdrawMoney = field.Read<ulong>(13);

                SetStats(field.Read<string>(14),
                         field.Read<byte>(15),         // characters.level
                         (Race)field.Read<byte>(16),   // characters.race
                         (Class)field.Read<byte>(17),  // characters.class
                         (Gender)field.Read<byte>(18), // characters.Gender
                         field.Read<ushort>(19),       // characters.zone
                         field.Read<uint>(20),         // characters.account
                         0);

                _logoutTime = field.Read<ulong>(21); // characters.logout_time
                _totalActivity = 0;
                _weekActivity = 0;
                _weekReputation = 0;

                if (!CheckStats())
                    return false;

                if (_zoneId == 0)
                {
                    Log.outError(LogFilter.Guild, "Player ({0}) has broken zone-_data", _guid.ToString());
                    _zoneId = Player.GetZoneIdFromDB(_guid);
                }

                ResetFlags();

                return true;
            }

            public bool CheckStats()
            {
                if (_level < 1)
                {
                    Log.outError(LogFilter.Guild, $"{_guid} has a broken _data in field `characters`.`level`, deleting him from guild!");

                    return false;
                }

                if (!CliDB.ChrRacesStorage.ContainsKey((uint)_race))
                {
                    Log.outError(LogFilter.Guild, $"{_guid} has a broken _data in field `characters`.`race`, deleting him from guild!");

                    return false;
                }

                if (!CliDB.ChrClassesStorage.ContainsKey((uint)_class))
                {
                    Log.outError(LogFilter.Guild, $"{_guid} has a broken _data in field `characters`.`class`, deleting him from guild!");

                    return false;
                }

                return true;
            }

            public float GetInactiveDays()
            {
                if (IsOnline())
                    return 0.0f;

                return (float)((GameTime.GetGameTime() - (long)GetLogoutTime()) / (float)Time.Day);
            }

            // Decreases amount of slots left for today.
            public void UpdateBankTabWithdrawValue(SQLTransaction trans, byte tabId, uint amount)
            {
                _bankWithdraw[tabId] += amount;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_TABS);
                stmt.AddValue(0, _guid.GetCounter());

                for (byte i = 0; i < GuildConst.MaxBankTabs;)
                {
                    uint withdraw = _bankWithdraw[i++];
                    stmt.AddValue(i, withdraw);
                }

                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            // Decreases amount of money left for today.
            public void UpdateBankMoneyWithdrawValue(SQLTransaction trans, ulong amount)
            {
                _bankWithdrawMoney += amount;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_MEMBER_WITHDRAW_MONEY);
                stmt.AddValue(0, _guid.GetCounter());
                stmt.AddValue(1, _bankWithdrawMoney);
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void ResetValues(bool weekly = false)
            {
                for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
                    _bankWithdraw[tabId] = 0;

                _bankWithdrawMoney = 0;

                if (weekly)
                {
                    _weekActivity = 0;
                    _weekReputation = 0;
                }
            }

            public void SetZoneId(uint id)
            {
                _zoneId = id;
            }

            public void SetAchievementPoints(uint val)
            {
                _achievementPoints = val;
            }

            public void SetLevel(uint var)
            {
                _level = (byte)var;
            }

            public void AddFlag(GuildMemberFlags var)
            {
                _flags |= var;
            }

            public void RemoveFlag(GuildMemberFlags var)
            {
                _flags &= ~var;
            }

            public void ResetFlags()
            {
                _flags = GuildMemberFlags.None;
            }

            public ObjectGuid GetGUID()
            {
                return _guid;
            }

            public string GetName()
            {
                return _name;
            }

            public uint GetAccountId()
            {
                return _accountId;
            }

            public GuildRankId GetRankId()
            {
                return _rankId;
            }

            public ulong GetLogoutTime()
            {
                return _logoutTime;
            }

            public string GetPublicNote()
            {
                return _publicNote;
            }

            public string GetOfficerNote()
            {
                return _officerNote;
            }

            public Race GetRace()
            {
                return _race;
            }

            public Class GetClass()
            {
                return _class;
            }

            public Gender GetGender()
            {
                return _gender;
            }

            public byte GetLevel()
            {
                return _level;
            }

            public GuildMemberFlags GetFlags()
            {
                return _flags;
            }

            public uint GetZoneId()
            {
                return _zoneId;
            }

            public uint GetAchievementPoints()
            {
                return _achievementPoints;
            }

            public ulong GetTotalActivity()
            {
                return _totalActivity;
            }

            public ulong GetWeekActivity()
            {
                return _weekActivity;
            }

            public uint GetTotalReputation()
            {
                return _totalReputation;
            }

            public uint GetWeekReputation()
            {
                return _weekReputation;
            }

            public List<uint> GetTrackedCriteriaIds()
            {
                return _trackedCriteriaIds;
            }

            public void SetTrackedCriteriaIds(List<uint> criteriaIds)
            {
                _trackedCriteriaIds = criteriaIds;
            }

            public bool IsTrackingCriteriaId(uint criteriaId)
            {
                return _trackedCriteriaIds.Contains(criteriaId);
            }

            public bool IsOnline()
            {
                return _flags.HasFlag(GuildMemberFlags.Online);
            }

            public void UpdateLogoutTime()
            {
                _logoutTime = (ulong)GameTime.GetGameTime();
            }

            public bool IsRank(GuildRankId rankId)
            {
                return _rankId == rankId;
            }

            public bool IsSamePlayer(ObjectGuid guid)
            {
                return _guid == guid;
            }

            public uint GetBankTabWithdrawValue(byte tabId)
            {
                return _bankWithdraw[tabId];
            }

            public ulong GetBankMoneyWithdrawValue()
            {
                return _bankWithdrawMoney;
            }

            public Player FindPlayer()
            {
                return Global.ObjAccessor.FindPlayer(_guid);
            }

            private Player FindConnectedPlayer()
            {
                return Global.ObjAccessor.FindConnectedPlayer(_guid);
            }

            #region Fields

            private readonly ulong _guildId;
            private ObjectGuid _guid;
            private string _name;
            private uint _zoneId;
            private byte _level;
            private Race _race;
            private Class _class;
            private Gender _gender;
            private GuildMemberFlags _flags;
            private ulong _logoutTime;
            private uint _accountId;
            private GuildRankId _rankId;
            private string _publicNote = "";
            private string _officerNote = "";

            private List<uint> _trackedCriteriaIds = new();

            private readonly uint[] _bankWithdraw = new uint[GuildConst.MaxBankTabs];
            private ulong _bankWithdrawMoney;
            private uint _achievementPoints;
            private ulong _totalActivity;
            private ulong _weekActivity;
            private uint _totalReputation;
            private uint _weekReputation;

            #endregion
        }

        public class LogEntry
        {
            public uint _guid;

            public ulong _guildId;
            public long _timestamp;

            public LogEntry(ulong guildId, uint guid)
            {
                _guildId = guildId;
                _guid = guid;
                _timestamp = GameTime.GetGameTime();
            }

            public LogEntry(ulong guildId, uint guid, long timestamp)
            {
                _guildId = guildId;
                _guid = guid;
                _timestamp = timestamp;
            }

            public uint GetGUID()
            {
                return _guid;
            }

            public long GetTimestamp()
            {
                return _timestamp;
            }

            public virtual void SaveToDB(SQLTransaction trans)
            {
            }
        }

        public class EventLogEntry : LogEntry
        {
            private readonly GuildEventLogTypes _eventType;
            private readonly byte _newRank;
            private readonly ulong _playerGuid1;
            private readonly ulong _playerGuid2;

            public EventLogEntry(ulong guildId, uint guid, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank)
                : base(guildId, guid)
            {
                _eventType = eventType;
                _playerGuid1 = playerGuid1;
                _playerGuid2 = playerGuid2;
                _newRank = newRank;
            }

            public EventLogEntry(ulong guildId, uint guid, long timestamp, GuildEventLogTypes eventType, ulong playerGuid1, ulong playerGuid2, byte newRank)
                : base(guildId, guid, timestamp)
            {
                _eventType = eventType;
                _playerGuid1 = playerGuid1;
                _playerGuid2 = playerGuid2;
                _newRank = newRank;
            }

            public override void SaveToDB(SQLTransaction trans)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_EVENTLOG);
                stmt.AddValue(0, _guildId);
                stmt.AddValue(1, _guid);
                trans.Append(stmt);

                byte index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_EVENTLOG);
                stmt.AddValue(index, _guildId);
                stmt.AddValue(++index, _guid);
                stmt.AddValue(++index, (byte)_eventType);
                stmt.AddValue(++index, _playerGuid1);
                stmt.AddValue(++index, _playerGuid2);
                stmt.AddValue(++index, _newRank);
                stmt.AddValue(++index, _timestamp);
                trans.Append(stmt);
            }

            public void WritePacket(GuildEventLogQueryResults packet)
            {
                ObjectGuid playerGUID = ObjectGuid.Create(HighGuid.Player, _playerGuid1);
                ObjectGuid otherGUID = ObjectGuid.Create(HighGuid.Player, _playerGuid2);

                GuildEventEntry eventEntry = new();
                eventEntry.PlayerGUID = playerGUID;
                eventEntry.OtherGUID = otherGUID;
                eventEntry.TransactionType = (byte)_eventType;
                eventEntry.TransactionDate = (uint)(GameTime.GetGameTime() - _timestamp);
                eventEntry.RankID = _newRank;
                packet.Entry.Add(eventEntry);
            }
        }

        public class BankEventLogEntry : LogEntry
        {
            private readonly byte _bankTabId;
            private readonly byte _destTabId;

            private readonly GuildBankEventLogTypes _eventType;
            private readonly ulong _itemOrMoney;
            private readonly ushort _itemStackCount;
            private readonly ulong _playerGuid;

            public BankEventLogEntry(ulong guildId, uint guid, GuildBankEventLogTypes eventType, byte tabId, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId)
                : base(guildId, guid)
            {
                _eventType = eventType;
                _bankTabId = tabId;
                _playerGuid = playerGuid;
                _itemOrMoney = itemOrMoney;
                _itemStackCount = itemStackCount;
                _destTabId = destTabId;
            }

            public BankEventLogEntry(ulong guildId, uint guid, long timestamp, byte tabId, GuildBankEventLogTypes eventType, ulong playerGuid, ulong itemOrMoney, ushort itemStackCount, byte destTabId)
                : base(guildId, guid, timestamp)
            {
                _eventType = eventType;
                _bankTabId = tabId;
                _playerGuid = playerGuid;
                _itemOrMoney = itemOrMoney;
                _itemStackCount = itemStackCount;
                _destTabId = destTabId;
            }

            public static bool IsMoneyEvent(GuildBankEventLogTypes eventType)
            {
                return
                    eventType == GuildBankEventLogTypes.DepositMoney ||
                    eventType == GuildBankEventLogTypes.WithdrawMoney ||
                    eventType == GuildBankEventLogTypes.RepairMoney ||
                    eventType == GuildBankEventLogTypes.CashFlowDeposit;
            }

            private bool IsMoneyEvent()
            {
                return IsMoneyEvent(_eventType);
            }

            public override void SaveToDB(SQLTransaction trans)
            {
                byte index = 0;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_EVENTLOG);
                stmt.AddValue(index, _guildId);
                stmt.AddValue(++index, _guid);
                stmt.AddValue(++index, _bankTabId);
                trans.Append(stmt);

                index = 0;
                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_EVENTLOG);
                stmt.AddValue(index, _guildId);
                stmt.AddValue(++index, _guid);
                stmt.AddValue(++index, _bankTabId);
                stmt.AddValue(++index, (byte)_eventType);
                stmt.AddValue(++index, _playerGuid);
                stmt.AddValue(++index, _itemOrMoney);
                stmt.AddValue(++index, _itemStackCount);
                stmt.AddValue(++index, _destTabId);
                stmt.AddValue(++index, _timestamp);
                trans.Append(stmt);
            }

            public void WritePacket(GuildBankLogQueryResults packet)
            {
                ObjectGuid logGuid = ObjectGuid.Create(HighGuid.Player, _playerGuid);

                bool hasItem = _eventType == GuildBankEventLogTypes.DepositItem ||
                               _eventType == GuildBankEventLogTypes.WithdrawItem ||
                               _eventType == GuildBankEventLogTypes.MoveItem ||
                               _eventType == GuildBankEventLogTypes.MoveItem2;

                bool itemMoved = (_eventType == GuildBankEventLogTypes.MoveItem || _eventType == GuildBankEventLogTypes.MoveItem2);

                bool hasStack = (hasItem && _itemStackCount > 1) || itemMoved;

                GuildBankLogEntry bankLogEntry = new();
                bankLogEntry.PlayerGUID = logGuid;
                bankLogEntry.TimeOffset = (uint)(GameTime.GetGameTime() - _timestamp);
                bankLogEntry.EntryType = (sbyte)_eventType;

                if (hasStack)
                    bankLogEntry.Count = _itemStackCount;

                if (IsMoneyEvent())
                    bankLogEntry.Money = _itemOrMoney;

                if (hasItem)
                    bankLogEntry.ItemID = (int)_itemOrMoney;

                if (itemMoved)
                    bankLogEntry.OtherTab = (sbyte)_destTabId;

                packet.Entry.Add(bankLogEntry);
            }
        }

        public class NewsLogEntry : LogEntry
        {
            private int _flags;
            private ObjectGuid _playerGuid;

            private readonly GuildNews _type;
            private readonly uint _value;

            public NewsLogEntry(ulong guildId, uint guid, GuildNews type, ObjectGuid playerGuid, uint flags, uint value)
                : base(guildId, guid)
            {
                _type = type;
                _playerGuid = playerGuid;
                _flags = (int)flags;
                _value = value;
            }

            public NewsLogEntry(ulong guildId, uint guid, long timestamp, GuildNews type, ObjectGuid playerGuid, uint flags, uint value)
                : base(guildId, guid, timestamp)
            {
                _type = type;
                _playerGuid = playerGuid;
                _flags = (int)flags;
                _value = value;
            }

            public GuildNews GetNewsType()
            {
                return _type;
            }

            public ObjectGuid GetPlayerGuid()
            {
                return _playerGuid;
            }

            public uint GetValue()
            {
                return _value;
            }

            public int GetFlags()
            {
                return _flags;
            }

            public void SetSticky(bool sticky)
            {
                if (sticky)
                    _flags |= 1;
                else
                    _flags &= ~1;
            }

            public override void SaveToDB(SQLTransaction trans)
            {
                byte index = 0;
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_NEWS);
                stmt.AddValue(index, _guildId);
                stmt.AddValue(++index, GetGUID());
                stmt.AddValue(++index, (byte)GetNewsType());
                stmt.AddValue(++index, GetPlayerGuid().GetCounter());
                stmt.AddValue(++index, GetFlags());
                stmt.AddValue(++index, GetValue());
                stmt.AddValue(++index, GetTimestamp());
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void WritePacket(GuildNewsPkt newsPacket)
            {
                GuildNewsEvent newsEvent = new();
                newsEvent.Id = (int)GetGUID();
                newsEvent.MemberGuid = GetPlayerGuid();
                newsEvent.CompletedDate = (uint)GetTimestamp();
                newsEvent.Flags = GetFlags();
                newsEvent.Type = (int)GetNewsType();

                //for (public byte i = 0; i < 2; i++)
                //    newsEvent.Data[i] =

                //newsEvent.MemberList.push_back(MemberGuid);

                if (GetNewsType() == GuildNews.ItemLooted ||
                    GetNewsType() == GuildNews.ItemCrafted ||
                    GetNewsType() == GuildNews.ItemPurchased)
                {
                    ItemInstance itemInstance = new();
                    itemInstance.ItemID = GetValue();
                    newsEvent.Item = itemInstance;
                }

                newsPacket.NewsEvents.Add(newsEvent);
            }
        }

        public class LogHolder<T> where T : LogEntry
        {
            private readonly List<T> _log = new();
            private readonly uint _maxRecords;
            private uint _nextGUID;

            public LogHolder()
            {
                _maxRecords = WorldConfig.GetUIntValue(typeof(T) == typeof(BankEventLogEntry) ? WorldCfg.GuildBankEventLogCount : WorldCfg.GuildEventLogCount);
                _nextGUID = GuildConst.EventLogGuidUndefined;
            }

            // Checks if new log entry can be added to holder
            public bool CanInsert()
            {
                return _log.Count < _maxRecords;
            }

            public byte GetSize()
            {
                return (byte)_log.Count;
            }

            public void LoadEvent(T entry)
            {
                if (_nextGUID == GuildConst.EventLogGuidUndefined)
                    _nextGUID = entry.GetGUID();

                _log.Insert(0, entry);
            }

            public T AddEvent(SQLTransaction trans, T entry)
            {
                // Check max records limit
                if (!CanInsert())
                    _log.RemoveAt(0);

                // Add event to list
                _log.Add(entry);

                // Save to DB
                entry.SaveToDB(trans);

                return entry;
            }

            public uint GetNextGUID()
            {
                if (_nextGUID == GuildConst.EventLogGuidUndefined)
                    _nextGUID = 0;
                else
                    _nextGUID = (_nextGUID + 1) % _maxRecords;

                return _nextGUID;
            }

            public List<T> GetGuildLog()
            {
                return _log;
            }
        }

        public class RankInfo
        {
            private uint _bankMoneyPerDay;
            private readonly GuildBankRightsAndSlots[] _bankTabRightsAndSlots = new GuildBankRightsAndSlots[GuildConst.MaxBankTabs];

            private readonly ulong _guildId;
            private string _name;
            private GuildRankId _rankId;
            private GuildRankOrder _rankOrder;
            private GuildRankRights _rights;

            public RankInfo(ulong guildId = 0)
            {
                _guildId = guildId;
                _rankId = (GuildRankId)0xFF;
                _rankOrder = 0;
                _rights = GuildRankRights.None;
                _bankMoneyPerDay = 0;

                for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
                    _bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
            }

            public RankInfo(ulong guildId, GuildRankId rankId, GuildRankOrder rankOrder, string name, GuildRankRights rights, uint money)
            {
                _guildId = guildId;
                _rankId = rankId;
                _rankOrder = rankOrder;
                _name = name;
                _rights = rights;
                _bankMoneyPerDay = money;

                for (var i = 0; i < GuildConst.MaxBankTabs; ++i)
                    _bankTabRightsAndSlots[i] = new GuildBankRightsAndSlots();
            }

            public void LoadFromDB(SQLFields field)
            {
                _rankId = (GuildRankId)field.Read<byte>(1);
                _rankOrder = (GuildRankOrder)field.Read<byte>(2);
                _name = field.Read<string>(3);
                _rights = (GuildRankRights)field.Read<uint>(4);
                _bankMoneyPerDay = field.Read<uint>(5);

                if (_rankId == GuildRankId.GuildMaster) // Prevent loss of leader rights
                    _rights |= GuildRankRights.All;
            }

            public void SaveToDB(SQLTransaction trans)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_RANK);
                stmt.AddValue(0, _guildId);
                stmt.AddValue(1, (byte)_rankId);
                stmt.AddValue(2, (byte)_rankOrder);
                stmt.AddValue(3, _name);
                stmt.AddValue(4, (uint)_rights);
                stmt.AddValue(5, _bankMoneyPerDay);
                DB.Characters.ExecuteOrAppend(trans, stmt);
            }

            public void CreateMissingTabsIfNeeded(byte tabs, SQLTransaction trans, bool logOnCreate = false)
            {
                for (byte i = 0; i < tabs; ++i)
                {
                    GuildBankRightsAndSlots rightsAndSlots = _bankTabRightsAndSlots[i];

                    if (rightsAndSlots.GetTabId() == i)
                        continue;

                    rightsAndSlots.SetTabId(i);

                    if (_rankId == GuildRankId.GuildMaster)
                        rightsAndSlots.SetGuildMasterValues();

                    if (logOnCreate)
                        Log.outError(LogFilter.Guild, $"Guild {_guildId} has broken Tab {i} for rank {_rankId}. Created default tab.");

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
                    stmt.AddValue(0, _guildId);
                    stmt.AddValue(1, i);
                    stmt.AddValue(2, (byte)_rankId);
                    stmt.AddValue(3, (sbyte)rightsAndSlots.GetRights());
                    stmt.AddValue(4, rightsAndSlots.GetSlots());
                    trans.Append(stmt);
                }
            }

            public void SetName(string name)
            {
                if (_name == name)
                    return;

                _name = name;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_NAME);
                stmt.AddValue(0, _name);
                stmt.AddValue(1, (byte)_rankId);
                stmt.AddValue(2, _guildId);
                DB.Characters.Execute(stmt);
            }

            public void SetRights(GuildRankRights rights)
            {
                if (_rankId == GuildRankId.GuildMaster) // Prevent loss of leader rights
                    rights = GuildRankRights.All;

                if (_rights == rights)
                    return;

                _rights = rights;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_RIGHTS);
                stmt.AddValue(0, (uint)_rights);
                stmt.AddValue(1, (byte)_rankId);
                stmt.AddValue(2, _guildId);
                DB.Characters.Execute(stmt);
            }

            public void SetBankMoneyPerDay(uint money)
            {
                if (_bankMoneyPerDay == money)
                    return;

                _bankMoneyPerDay = money;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_RANK_BANK_MONEY);
                stmt.AddValue(0, money);
                stmt.AddValue(1, (byte)_rankId);
                stmt.AddValue(2, _guildId);
                DB.Characters.Execute(stmt);
            }

            public void SetBankTabSlotsAndRights(GuildBankRightsAndSlots rightsAndSlots, bool saveToDB)
            {
                if (_rankId == GuildRankId.GuildMaster) // Prevent loss of leader rights
                    rightsAndSlots.SetGuildMasterValues();

                _bankTabRightsAndSlots[rightsAndSlots.GetTabId()] = rightsAndSlots;

                if (saveToDB)
                {
                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_RIGHT);
                    stmt.AddValue(0, _guildId);
                    stmt.AddValue(1, rightsAndSlots.GetTabId());
                    stmt.AddValue(2, (byte)_rankId);
                    stmt.AddValue(3, (sbyte)rightsAndSlots.GetRights());
                    stmt.AddValue(4, rightsAndSlots.GetSlots());
                    DB.Characters.Execute(stmt);
                }
            }

            public GuildRankId GetId()
            {
                return _rankId;
            }

            public GuildRankOrder GetOrder()
            {
                return _rankOrder;
            }

            public void SetOrder(GuildRankOrder rankOrder)
            {
                _rankOrder = rankOrder;
            }

            public string GetName()
            {
                return _name;
            }

            public GuildRankRights GetRights()
            {
                return _rights;
            }

            public uint GetBankMoneyPerDay()
            {
                return _rankId != GuildRankId.GuildMaster ? _bankMoneyPerDay : GuildConst.WithdrawMoneyUnlimited;
            }

            public GuildBankRights GetBankTabRights(byte tabId)
            {
                return tabId < GuildConst.MaxBankTabs ? _bankTabRightsAndSlots[tabId].GetRights() : 0;
            }

            public int GetBankTabSlotsPerDay(byte tabId)
            {
                return tabId < GuildConst.MaxBankTabs ? _bankTabRightsAndSlots[tabId].GetSlots() : 0;
            }
        }

        public class BankTab
        {
            private readonly ulong _guildId;
            private string _icon;
            private readonly Item[] _items = new Item[GuildConst.MaxBankSlots];
            private string _name;
            private readonly byte _tabId;
            private string _text;

            public BankTab(ulong guildId, byte tabId)
            {
                _guildId = guildId;
                _tabId = tabId;
            }

            public void LoadFromDB(SQLFields field)
            {
                _name = field.Read<string>(2);
                _icon = field.Read<string>(3);
                _text = field.Read<string>(4);
            }

            public bool LoadItemFromDB(SQLFields field)
            {
                byte slotId = field.Read<byte>(53);
                uint itemGuid = field.Read<uint>(0);
                uint itemEntry = field.Read<uint>(1);

                if (slotId >= GuildConst.MaxBankSlots)
                {
                    Log.outError(LogFilter.Guild, "Invalid Slot for Item (GUID: {0}, Id: {1}) in guild bank, skipped.", itemGuid, itemEntry);

                    return false;
                }

                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemEntry);

                if (proto == null)
                {
                    Log.outError(LogFilter.Guild, "Unknown Item (GUID: {0}, Id: {1}) in guild bank, skipped.", itemGuid, itemEntry);

                    return false;
                }

                Item pItem = Item.NewItemOrBag(proto);

                if (!pItem.LoadFromDB(itemGuid, ObjectGuid.Empty, field, itemEntry))
                {
                    Log.outError(LogFilter.Guild, "Item (GUID {0}, Id: {1}) not found in item_instance, deleting from guild bank!", itemGuid, itemEntry);

                    PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_NONEXISTENT_GUILD_BANK_ITEM);
                    stmt.AddValue(0, _guildId);
                    stmt.AddValue(1, _tabId);
                    stmt.AddValue(2, slotId);
                    DB.Characters.Execute(stmt);

                    return false;
                }

                pItem.AddToWorld();
                _items[slotId] = pItem;

                return true;
            }

            public void Delete(SQLTransaction trans, bool removeItemsFromDB = false)
            {
                for (byte slotId = 0; slotId < GuildConst.MaxBankSlots; ++slotId)
                {
                    Item pItem = _items[slotId];

                    if (pItem != null)
                    {
                        pItem.RemoveFromWorld();

                        if (removeItemsFromDB)
                            pItem.DeleteFromDB(trans);
                    }
                }
            }

            public void SetInfo(string name, string icon)
            {
                if (_name == name &&
                    _icon == icon)
                    return;

                _name = name;
                _icon = icon;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_INFO);
                stmt.AddValue(0, _name);
                stmt.AddValue(1, _icon);
                stmt.AddValue(2, _guildId);
                stmt.AddValue(3, _tabId);
                DB.Characters.Execute(stmt);
            }

            public void SetText(string text)
            {
                if (_text == text)
                    return;

                _text = text;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_BANK_TAB_TEXT);
                stmt.AddValue(0, _text);
                stmt.AddValue(1, _guildId);
                stmt.AddValue(2, _tabId);
                DB.Characters.Execute(stmt);
            }

            public void SendText(Guild guild, WorldSession session = null)
            {
                GuildBankTextQueryResult textQuery = new();
                textQuery.Tab = _tabId;
                textQuery.Text = _text;

                if (session != null)
                {
                    Log.outDebug(LogFilter.Guild, "SMSG_GUILD_BANK_QUERY_TEXT_RESULT [{0}]: Tabid: {1}, Text: {2}", session.GetPlayerInfo(), _tabId, _text);
                    session.SendPacket(textQuery);
                }
                else
                {
                    Log.outDebug(LogFilter.Guild, "SMSG_GUILD_BANK_QUERY_TEXT_RESULT [Broadcast]: Tabid: {0}, Text: {1}", _tabId, _text);
                    guild.BroadcastPacket(textQuery);
                }
            }

            public string GetName()
            {
                return _name;
            }

            public string GetIcon()
            {
                return _icon;
            }

            public string GetText()
            {
                return _text;
            }

            public Item GetItem(byte slotId)
            {
                return slotId < GuildConst.MaxBankSlots ? _items[slotId] : null;
            }

            public bool SetItem(SQLTransaction trans, byte slotId, Item item)
            {
                if (slotId >= GuildConst.MaxBankSlots)
                    return false;

                _items[slotId] = item;

                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GUILD_BANK_ITEM);
                stmt.AddValue(0, _guildId);
                stmt.AddValue(1, _tabId);
                stmt.AddValue(2, slotId);
                trans.Append(stmt);

                if (item != null)
                {
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_GUILD_BANK_ITEM);
                    stmt.AddValue(0, _guildId);
                    stmt.AddValue(1, _tabId);
                    stmt.AddValue(2, slotId);
                    stmt.AddValue(3, item.GetGUID().GetCounter());
                    trans.Append(stmt);

                    item.SetContainedIn(ObjectGuid.Empty);
                    item.SetOwnerGUID(ObjectGuid.Empty);
                    item.FSetState(ItemUpdateState.New);
                    item.SaveToDB(trans); // Not in inventory and can be saved standalone
                }

                return true;
            }
        }

        public class GuildBankRightsAndSlots
        {
            private GuildBankRights rights;
            private int slots;

            private byte tabId;

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

            public void SetTabId(byte _tabId)
            {
                tabId = _tabId;
            }

            public void SetSlots(int _slots)
            {
                slots = _slots;
            }

            public void SetRights(GuildBankRights _rights)
            {
                rights = _rights;
            }

            public byte GetTabId()
            {
                return tabId;
            }

            public int GetSlots()
            {
                return slots;
            }

            public GuildBankRights GetRights()
            {
                return rights;
            }
        }

        public class EmblemInfo
        {
            private uint _backgroundColor;
            private uint _borderColor;
            private uint _borderStyle;
            private uint _color;

            private uint _style;

            public EmblemInfo()
            {
                _style = 0;
                _color = 0;
                _borderStyle = 0;
                _borderColor = 0;
                _backgroundColor = 0;
            }

            public void ReadPacket(SaveGuildEmblem packet)
            {
                _style = packet.EStyle;
                _color = packet.EColor;
                _borderStyle = packet.BStyle;
                _borderColor = packet.BColor;
                _backgroundColor = packet.Bg;
            }

            public bool ValidateEmblemColors()
            {
                return CliDB.GuildColorBackgroundStorage.ContainsKey(_backgroundColor) &&
                       CliDB.GuildColorBorderStorage.ContainsKey(_borderColor) &&
                       CliDB.GuildColorEmblemStorage.ContainsKey(_color);
            }

            public bool LoadFromDB(SQLFields field)
            {
                _style = field.Read<byte>(3);
                _color = field.Read<byte>(4);
                _borderStyle = field.Read<byte>(5);
                _borderColor = field.Read<byte>(6);
                _backgroundColor = field.Read<byte>(7);

                return ValidateEmblemColors();
            }

            public void SaveToDB(ulong guildId)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GUILD_EMBLEM_INFO);
                stmt.AddValue(0, _style);
                stmt.AddValue(1, _color);
                stmt.AddValue(2, _borderStyle);
                stmt.AddValue(3, _borderColor);
                stmt.AddValue(4, _backgroundColor);
                stmt.AddValue(5, guildId);
                DB.Characters.Execute(stmt);
            }

            public uint GetStyle()
            {
                return _style;
            }

            public uint GetColor()
            {
                return _color;
            }

            public uint GetBorderStyle()
            {
                return _borderStyle;
            }

            public uint GetBorderColor()
            {
                return _borderColor;
            }

            public uint GetBackgroundColor()
            {
                return _backgroundColor;
            }
        }

        public abstract class MoveItemData
        {
            public byte _container;
            public Item _pClonedItem;

            public Guild _pGuild;
            public Item _pItem;
            public Player _pPlayer;
            public byte _slotId;
            public List<ItemPosCount> _vec = new();

            protected MoveItemData(Guild guild, Player player, byte container, byte slotId)
            {
                _pGuild = guild;
                _pPlayer = player;
                _container = container;
                _slotId = slotId;
                _pItem = null;
                _pClonedItem = null;
            }

            public virtual bool CheckItem(ref uint splitedAmount)
            {
                Cypher.Assert(_pItem != null);

                if (splitedAmount > _pItem.GetCount())
                    return false;

                if (splitedAmount == _pItem.GetCount())
                    splitedAmount = 0;

                return true;
            }

            public InventoryResult CanStore(Item pItem, bool swap, bool sendError)
            {
                _vec.Clear();
                InventoryResult msg = CanStore(pItem, swap);

                if (sendError && msg != InventoryResult.Ok)
                    SendEquipError(msg, pItem);

                return msg;
            }

            public bool CloneItem(uint count)
            {
                Cypher.Assert(_pItem != null);
                _pClonedItem = _pItem.CloneItem(count);

                if (_pClonedItem == null)
                {
                    SendEquipError(InventoryResult.ItemNotFound, _pItem);

                    return false;
                }

                return true;
            }

            public virtual void LogAction(MoveItemData pFrom)
            {
                Cypher.Assert(pFrom.GetItem() != null);

                Global.ScriptMgr.ForEach<IGuildOnItemMove>(p => p.OnItemMove(_pGuild,
                                                                             _pPlayer,
                                                                             pFrom.GetItem(),
                                                                             pFrom.IsBank(),
                                                                             pFrom.GetContainer(),
                                                                             pFrom.GetSlotId(),
                                                                             IsBank(),
                                                                             GetContainer(),
                                                                             GetSlotId()));
            }

            public void CopySlots(List<byte> ids)
            {
                foreach (var item in _vec)
                    ids.Add((byte)item.Pos);
            }

            public void SendEquipError(InventoryResult result, Item item)
            {
                _pPlayer.SendEquipError(result, item);
            }

            public abstract bool IsBank();

            // Initializes Item. Returns true, if Item exists, false otherwise.
            public abstract bool InitItem();

            // Checks splited amount against Item. Splited amount cannot be more that number of items in stack.
            // Defines if player has rights to save Item in container
            public virtual bool HasStoreRights(MoveItemData pOther)
            {
                return true;
            }

            // Defines if player has rights to withdraw Item from container
            public virtual bool HasWithdrawRights(MoveItemData pOther)
            {
                return true;
            }

            // Remove Item from container (if splited update items fields)
            public abstract void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0);

            // Saves Item to container
            public abstract Item StoreItem(SQLTransaction trans, Item pItem);

            // Log bank event
            public abstract void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count);

            public abstract InventoryResult CanStore(Item pItem, bool swap);

            public Item GetItem(bool isCloned = false)
            {
                return isCloned ? _pClonedItem : _pItem;
            }

            public byte GetContainer()
            {
                return _container;
            }

            public byte GetSlotId()
            {
                return _slotId;
            }
        }

        public class PlayerMoveItemData : MoveItemData
        {
            public PlayerMoveItemData(Guild guild, Player player, byte container, byte slotId)
                : base(guild, player, container, slotId)
            {
            }

            public override bool IsBank()
            {
                return false;
            }

            public override bool InitItem()
            {
                _pItem = _pPlayer.GetItemByPos(_container, _slotId);

                if (_pItem != null)
                {
                    // Anti-WPE protection. Do not move non-empty bags to bank.
                    if (_pItem.IsNotEmptyBag())
                    {
                        SendEquipError(InventoryResult.DestroyNonemptyBag, _pItem);
                        _pItem = null;
                    }
                    // Bound items cannot be put into bank.
                    else if (!_pItem.CanBeTraded())
                    {
                        SendEquipError(InventoryResult.CantSwap, _pItem);
                        _pItem = null;
                    }
                }

                return (_pItem != null);
            }

            public override void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0)
            {
                if (splitedAmount != 0)
                {
                    _pItem.SetCount(_pItem.GetCount() - splitedAmount);
                    _pItem.SetState(ItemUpdateState.Changed, _pPlayer);
                    _pPlayer.SaveInventoryAndGoldToDB(trans);
                }
                else
                {
                    _pPlayer.MoveItemFromInventory(_container, _slotId, true);
                    _pItem.DeleteFromInventoryDB(trans);
                    _pItem = null;
                }
            }

            public override Item StoreItem(SQLTransaction trans, Item pItem)
            {
                Cypher.Assert(pItem != null);
                _pPlayer.MoveItemToInventory(_vec, pItem, true);
                _pPlayer.SaveInventoryAndGoldToDB(trans);

                return pItem;
            }

            public override void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count)
            {
                Cypher.Assert(pFrom != null);

                // Bank . Char
                _pGuild._LogBankEvent(trans,
                                      GuildBankEventLogTypes.WithdrawItem,
                                      pFrom.GetContainer(),
                                      _pPlayer.GetGUID().GetCounter(),
                                      pFrom.GetItem().GetEntry(),
                                      (ushort)count);
            }

            public override InventoryResult CanStore(Item pItem, bool swap)
            {
                return _pPlayer.CanStoreItem(_container, _slotId, _vec, pItem, swap);
            }
        }

        public class BankMoveItemData : MoveItemData
        {
            public BankMoveItemData(Guild guild, Player player, byte container, byte slotId)
                : base(guild, player, container, slotId)
            {
            }

            public override bool IsBank()
            {
                return true;
            }

            public override bool InitItem()
            {
                _pItem = _pGuild._GetItem(_container, _slotId);

                return (_pItem != null);
            }

            public override bool HasStoreRights(MoveItemData pOther)
            {
                Cypher.Assert(pOther != null);

                // Do not check rights if Item is being swapped within the same bank tab
                if (pOther.IsBank() &&
                    pOther.GetContainer() == _container)
                    return true;

                return _pGuild._MemberHasTabRights(_pPlayer.GetGUID(), _container, GuildBankRights.DepositItem);
            }

            public override bool HasWithdrawRights(MoveItemData pOther)
            {
                Cypher.Assert(pOther != null);

                // Do not check rights if Item is being swapped within the same bank tab
                if (pOther.IsBank() &&
                    pOther.GetContainer() == _container)
                    return true;

                int slots = 0;
                Member member = _pGuild.GetMember(_pPlayer.GetGUID());

                if (member != null)
                    slots = _pGuild._GetMemberRemainingSlots(member, _container);

                return slots != 0;
            }

            public override void RemoveItem(SQLTransaction trans, MoveItemData pOther, uint splitedAmount = 0)
            {
                Cypher.Assert(_pItem != null);

                if (splitedAmount != 0)
                {
                    _pItem.SetCount(_pItem.GetCount() - splitedAmount);
                    _pItem.FSetState(ItemUpdateState.Changed);
                    _pItem.SaveToDB(trans);
                }
                else
                {
                    _pGuild._RemoveItem(trans, _container, _slotId);
                    _pItem = null;
                }

                // Decrease amount of player's remaining items (if Item is moved to different tab or to player)
                if (!pOther.IsBank() ||
                    pOther.GetContainer() != _container)
                    _pGuild._UpdateMemberWithdrawSlots(trans, _pPlayer.GetGUID(), _container);
            }

            public override Item StoreItem(SQLTransaction trans, Item pItem)
            {
                if (pItem == null)
                    return null;

                BankTab pTab = _pGuild.GetBankTab(_container);

                if (pTab == null)
                    return null;

                Item pLastItem = pItem;

                foreach (var pos in _vec)
                {
                    Log.outDebug(LogFilter.Guild,
                                 "GUILD STORAGE: StoreItem tab = {0}, Slot = {1}, Item = {2}, Count = {3}",
                                 _container,
                                 _slotId,
                                 pItem.GetEntry(),
                                 pItem.GetCount());

                    pLastItem = _StoreItem(trans, pTab, pItem, pos, pos.Equals(_vec.Last()));
                }

                return pLastItem;
            }

            public override void LogBankEvent(SQLTransaction trans, MoveItemData pFrom, uint count)
            {
                Cypher.Assert(pFrom.GetItem() != null);

                if (pFrom.IsBank())
                    // Bank . Bank
                    _pGuild._LogBankEvent(trans,
                                          GuildBankEventLogTypes.MoveItem,
                                          pFrom.GetContainer(),
                                          _pPlayer.GetGUID().GetCounter(),
                                          pFrom.GetItem().GetEntry(),
                                          (ushort)count,
                                          _container);
                else
                    // Char . Bank
                    _pGuild._LogBankEvent(trans,
                                          GuildBankEventLogTypes.DepositItem,
                                          _container,
                                          _pPlayer.GetGUID().GetCounter(),
                                          pFrom.GetItem().GetEntry(),
                                          (ushort)count);
            }

            public override void LogAction(MoveItemData pFrom)
            {
                base.LogAction(pFrom);

                if (!pFrom.IsBank() &&
                    _pPlayer.GetSession().HasPermission(RBACPermissions.LogGmTrade)) // @todo Move this to scripts
                    Log.outCommand(_pPlayer.GetSession().GetAccountId(),
                                   "GM {0} ({1}) (Account: {2}) deposit Item: {3} (Entry: {4} Count: {5}) to guild bank named: {6} (Guild ID: {7})",
                                   _pPlayer.GetName(),
                                   _pPlayer.GetGUID().ToString(),
                                   _pPlayer.GetSession().GetAccountId(),
                                   pFrom.GetItem().GetTemplate().GetName(),
                                   pFrom.GetItem().GetEntry(),
                                   pFrom.GetItem().GetCount(),
                                   _pGuild.GetName(),
                                   _pGuild.GetId());
            }

            private Item _StoreItem(SQLTransaction trans, BankTab pTab, Item pItem, ItemPosCount pos, bool clone)
            {
                byte slotId = (byte)pos.Pos;
                uint count = pos.Count;
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

                if (pItem != null &&
                    pTab.SetItem(trans, slotId, pItem))
                    return pItem;

                return null;
            }

            private bool _ReserveSpace(byte slotId, Item pItem, Item pItemDest, ref uint count)
            {
                uint requiredSpace = pItem.GetMaxStackCount();

                if (pItemDest != null)
                {
                    // Make sure source and destination items match and destination Item has space for more stacks.
                    if (pItemDest.GetEntry() != pItem.GetEntry() ||
                        pItemDest.GetCount() >= pItem.GetMaxStackCount())
                        return false;

                    requiredSpace -= pItemDest.GetCount();
                }

                // Let's not be greedy, reserve only required space
                requiredSpace = Math.Min(requiredSpace, count);

                // Reserve space
                ItemPosCount pos = new(slotId, requiredSpace);

                if (!pos.IsContainedIn(_vec))
                {
                    _vec.Add(pos);
                    count -= requiredSpace;
                }

                return true;
            }

            private void CanStoreItemInTab(Item pItem, byte skipSlotId, bool merge, ref uint count)
            {
                for (byte slotId = 0; (slotId < GuildConst.MaxBankSlots) && (count > 0); ++slotId)
                {
                    // Skip Slot already processed in CanStore (when destination Slot was specified)
                    if (slotId == skipSlotId)
                        continue;

                    Item pItemDest = _pGuild._GetItem(_container, slotId);

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
                Log.outDebug(LogFilter.Guild,
                             "GUILD STORAGE: CanStore() tab = {0}, Slot = {1}, Item = {2}, Count = {3}",
                             _container,
                             _slotId,
                             pItem.GetEntry(),
                             pItem.GetCount());

                uint count = pItem.GetCount();

                // Soulbound items cannot be moved
                if (pItem.IsSoulBound())
                    return InventoryResult.DropBoundItem;

                // Make sure destination bank tab exists
                if (_container >= _pGuild._GetPurchasedTabsSize())
                    return InventoryResult.WrongBagType;

                // Slot explicitely specified. Check it.
                if (_slotId != ItemConst.NullSlot)
                {
                    Item pItemDest = _pGuild._GetItem(_container, _slotId);

                    // Ignore swapped Item (this Slot will be empty after move)
                    if ((pItemDest == pItem) || swap)
                        pItemDest = null;

                    if (!_ReserveSpace(_slotId, pItem, pItemDest, ref count))
                        return InventoryResult.CantStack;

                    if (count == 0)
                        return InventoryResult.Ok;
                }

                // Slot was not specified or it has not enough space for all the items in stack
                // Search for stacks to merge with
                if (pItem.GetMaxStackCount() > 1)
                {
                    CanStoreItemInTab(pItem, _slotId, true, ref count);

                    if (count == 0)
                        return InventoryResult.Ok;
                }

                // Search free Slot for Item
                CanStoreItemInTab(pItem, _slotId, false, ref count);

                if (count == 0)
                    return InventoryResult.Ok;

                return InventoryResult.BankFull;
            }
        }

        #endregion
    }
}