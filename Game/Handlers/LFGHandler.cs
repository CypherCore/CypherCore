/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Game.DungeonFinding;
using Game.Entities;
using Game.Groups;
using Game.Network;
using Game.Network.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        void BuildQuestReward(LfgPlayerQuestReward questReward, Quest quest, Player player)
        {
            byte rewCount = (byte)(quest.GetRewItemsCount() + quest.GetRewCurrencyCount());

            questReward.RewardMoney = player.GetQuestMoneyReward(quest);
            questReward.RewardXP = player.GetQuestXPReward(quest);
            if (rewCount != 0)
            {
                for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
                {
                    var rewardCurrency = new LfgPlayerQuestReward.LfgPlayerQuestRewardCurrency();
                    uint currencyId = quest.RewardCurrencyId[i];
                    if (currencyId != 0)
                    {
                        rewardCurrency.CurrencyID = currencyId;
                        rewardCurrency.Quantity = quest.RewardCurrencyCount[i];
                        questReward.Currency.Add(rewardCurrency); 
                    }
                }

                for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
                {
                    var rewardItem = new LfgPlayerQuestReward.LfgPlayerQuestRewardItem();
                    uint itemId = quest.RewardItemId[i];
                    if (itemId != 0)
                    {
                        ItemTemplate item = Global.ObjectMgr.GetItemTemplate(itemId);
                        rewardItem.ItemID = itemId;
                        rewardItem.Quantity = quest.RewardItemCount[i];
                        questReward.Items.Add(rewardItem);
                    }
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.DfJoin)]
        void HandleDfJoin(DFJoin dfJoin)
        {
            if (!Global.LFGMgr.isOptionEnabled(LfgOptions.EnableDungeonFinder | LfgOptions.EnableRaidBrowser) ||
                (GetPlayer().GetGroup() && GetPlayer().GetGroup().GetLeaderGUID() != GetPlayer().GetGUID() &&
                (GetPlayer().GetGroup().GetMembersCount() == MapConst.MaxGroupSize || !GetPlayer().GetGroup().isLFGGroup())))
                return;

            if (dfJoin.Slots.Empty())
            {
                Log.outDebug(LogFilter.Lfg, "ClientOpcodes.DfJoin {0} no dungeons selected", GetPlayerInfo());
                return;
            }

            List<uint> newDungeons = new List<uint>();
            for (int i = 0; i < dfJoin.Slots.Count; ++i)
            {
                uint dungeon = dfJoin.Slots[i];
                if (dungeon != 0)
                    newDungeons.Add(dungeon);

            }

            Global.LFGMgr.JoinLfg(GetPlayer(), dfJoin.Roles, newDungeons);
        }

        [WorldPacketHandler(ClientOpcodes.DfLeave)]
        void HandleDfLeave(DFLeave dfLeave)
        {
            ObjectGuid leaveGuid = dfLeave.Ticket.RequesterGuid;
            Group group = GetPlayer().GetGroup();
            ObjectGuid guid = GetPlayer().GetGUID();
            ObjectGuid gguid = group ? group.GetGUID() : guid;

            // Check cheating - only leader can leave the queue
            if (!group || group.GetLeaderGUID() == leaveGuid)
                Global.LFGMgr.LeaveLfg(gguid);
        }

        [WorldPacketHandler(ClientOpcodes.DfProposalResponse)]
        void HandleDfProposalResponse(DFProposalResponse dfProposalResponse)
        {
            Global.LFGMgr.UpdateProposal(dfProposalResponse.ProposalID, dfProposalResponse.Ticket.RequesterGuid, dfProposalResponse.Accepted);
        }

        [WorldPacketHandler(ClientOpcodes.DfSetRoles)]
        void HandleDfSetRoles(DFSetRoles setRoles)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            Group group = GetPlayer().GetGroup();
            if (!group)
            {
                Log.outDebug(LogFilter.Lfg, "CMSG_LFG_SET_ROLES {0} Not in group",
                    GetPlayerInfo());
                return;
            }
            ObjectGuid gguid = group.GetGUID();
            Log.outDebug(LogFilter.Lfg, "CMSG_LFG_SET_ROLES: Group {0}, Player {1}, Roles: {2}", gguid.ToString(), GetPlayerInfo(), setRoles.RolesDesired);
            Global.LFGMgr.UpdateRoleCheck(gguid, guid, setRoles.RolesDesired);
        }

        [WorldPacketHandler(ClientOpcodes.DfBootPlayerVote)]
        void HandleDfBootPlayerVote(DFBootPlayerVote bootPlayerVote)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            Log.outDebug(LogFilter.Lfg, "ClientOpcodes.DfBootPlayerVote: {0} agree: {1}", GetPlayerInfo(), bootPlayerVote.Vote);
            Global.LFGMgr.UpdateBoot(guid, bootPlayerVote.Vote);
        }

        [WorldPacketHandler(ClientOpcodes.DfTeleport)]
        void HandleDfTeleport(DFTeleport teleport)
        {
            Log.outDebug(LogFilter.Lfg, "CMSG_LFG_TELEPORT {0} out: {1}", GetPlayerInfo(), teleport.TeleportOut);
            Global.LFGMgr.TeleportPlayer(GetPlayer(), teleport.TeleportOut, true);
        }

        //[WorldPacketHandler(ClientOpcodes.DfGetSystemInfo, Processing = PacketProcessing.ThreadSafe)]
        void HandleDfGetSystemInfo(DFGetSystemInfo dfGetSystemInfo)
        {
            Log.outDebug(LogFilter.Lfg, "CMSG_LFG_Lock_INFO_REQUEST {0} for {1}", GetPlayerInfo(), (dfGetSystemInfo.Player ? "player" : "party"));

            if (dfGetSystemInfo.Player)
                SendLfgPlayerLockInfo();
            else
                SendLfgPartyLockInfo();
        }

        [WorldPacketHandler(ClientOpcodes.DfGetJoinStatus, Processing = PacketProcessing.ThreadSafe)]
        void HandleDfGetJoinStatus(DFGetJoinStatus packet)
        {
            if (!GetPlayer().isUsingLfg())
                return;

            ObjectGuid guid = GetPlayer().GetGUID();
            LfgUpdateData updateData = Global.LFGMgr.GetLfgStatus(guid);

            if (GetPlayer().GetGroup())
            {
                SendLfgUpdateStatus(updateData, true);
                updateData.dungeons.Clear();
                SendLfgUpdateStatus(updateData, false);
            }
            else
            {
                SendLfgUpdateStatus(updateData, false);
                updateData.dungeons.Clear();
                SendLfgUpdateStatus(updateData, true);
            }
        }

        public void SendLfgPlayerLockInfo()
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            // Get Random dungeons that can be done at a certain level and expansion
            uint level = GetPlayer().getLevel();
            List<uint> randomDungeons = Global.LFGMgr.GetRandomAndSeasonalDungeons(level, (uint)GetExpansion());

            // Get player Locked Dungeons
            Dictionary<uint, LfgLockInfoData> Lock = Global.LFGMgr.GetLockedDungeons(guid);
            uint rsize = (uint)randomDungeons.Count;
            uint lsize = (uint)Lock.Count;

            LfgPlayerInfo lfgPlayerInfo = new LfgPlayerInfo();
            lfgPlayerInfo.BlackList = new LFGBlackList(Lock);

            foreach (var slot in randomDungeons)
            {
                var dungeonInfo = new LfgPlayerDungeonInfo();
                dungeonInfo.Slot = slot;
                dungeonInfo.FirstReward = true;

                LfgReward reward = Global.LFGMgr.GetRandomDungeonReward(slot, level);
                Quest quest = null;
                bool done = false;
                if (reward != null)
                {
                    quest = Global.ObjectMgr.GetQuestTemplate(reward.firstQuest);
                    if (quest != null)
                    {
                        done = !GetPlayer().CanRewardQuest(quest, false);
                        if (done)
                        {
                            quest = Global.ObjectMgr.GetQuestTemplate(reward.otherQuest);
                            dungeonInfo.FirstReward = false;
                        }
                    }
                }

                if (quest != null)
                {
                    dungeonInfo.ShortageEligible = true;

                    dungeonInfo.CompletionQuantity = 1;
                    dungeonInfo.CompletionLimit = 1;
                    dungeonInfo.SpecificLimit = 1;
                    dungeonInfo.OverallLimit = 1;
                    dungeonInfo.Quantity = 1;

                    BuildQuestReward(dungeonInfo.Rewards, quest, GetPlayer());
                }
                lfgPlayerInfo.Dungeons.Add(dungeonInfo);
            }

            SendPacket(lfgPlayerInfo);
        }

        public void SendLfgPartyLockInfo()
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            Group group = GetPlayer().GetGroup();
            if (!group)
                return;

            // Get the Locked dungeons of the other party members
            Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> LockMap = new Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>>();
            for (GroupReference refe = group.GetFirstMember(); refe != null; refe = refe.next())
            {
                Player plrg = refe.GetSource();
                if (!plrg)
                    continue;

                ObjectGuid pguid = plrg.GetGUID();
                if (pguid == guid)
                    continue;

                LockMap[pguid] = Global.LFGMgr.GetLockedDungeons(pguid);
            }

            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_PARTY_INFO {0}", GetPlayerInfo());

            LfgPartyInfo partyInfo = new LfgPartyInfo();
            foreach (var it in LockMap)
            {
                var blackList = new LFGBlackList(it.Value);
                blackList.PlayerGuid.Set(it.Key);

                partyInfo.Players.Add(blackList);
            }

            SendPacket(partyInfo);
        }

        public void SendLfgUpdateStatus(LfgUpdateData updateData, bool party)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            LFGUpdateStatus updateStatus = new LFGUpdateStatus();
            updateStatus.Ticket.RequesterGuid = guid;
            updateStatus.Ticket.Id = Global.LFGMgr.GetQueueId(guid);
            updateStatus.Ticket.Type = (byte)updateData.state;
            updateStatus.Ticket.Time = (uint)Global.LFGMgr.GetQueueJoinTime(guid);

            updateStatus.SubType = (byte)(updateData.dungeons.Count > 0 ? Global.LFGMgr.GetDungeonType(updateData.dungeons[0]) : 0);
            updateStatus.Reason = (byte)updateData.updateType;

            updateStatus.Slots = updateData.dungeons;
            updateStatus.RequestedRoles = (uint)Global.LFGMgr.GetRoles(guid);
            updateStatus.IsParty = party;

            switch (updateData.updateType)
            {
                case LfgUpdateType.JoinQueueInitial:            // Joined queue outside the dungeon
                    updateStatus.Joined = updateStatus.LfgJoined = true;
                    break;
                case LfgUpdateType.JoinQueue:
                case LfgUpdateType.AddedToQueue:                // Rolecheck Success
                    updateStatus.Joined = updateStatus.LfgJoined = true;
                    updateStatus.Queued = true;
                    break;
                case LfgUpdateType.ProposalBegin:
                    updateStatus.Joined = updateStatus.LfgJoined = true;
                    break;
                case LfgUpdateType.UpdateStatus:
                    updateStatus.Joined = updateStatus.LfgJoined = updateData.state != LfgState.Rolecheck && updateData.state != LfgState.None;
                    updateStatus.Queued = updateData.state == LfgState.Queued;
                    break;
                default:
                    break;
            }

            SendPacket(updateStatus);
        }

        public void SendLfgRoleChosen(ObjectGuid guid, LfgRoles roles)
        {
            RoleChosen roleChosen = new RoleChosen();
            roleChosen.Player = guid;
            roleChosen.RoleMask = roles;
            roleChosen.Accepted = roles > 0;
            SendPacket(roleChosen);
        }

        public void SendLfgRoleCheckUpdate(LfgRoleCheck roleCheck)
        {
            List<uint> dungeons = new List<uint>();
            if (roleCheck.rDungeonId != 0)
                dungeons.Add(roleCheck.rDungeonId);
            else
                dungeons = roleCheck.dungeons;

            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_ROLE_CHECK_UPDATE {0}", GetPlayerInfo());

            LFGRoleCheckUpdate roleCheckUpdate = new LFGRoleCheckUpdate();
            roleCheckUpdate.PartyIndex = (byte)roleCheck.rDungeonId; //Wrong
            roleCheckUpdate.RoleCheckStatus = (byte)roleCheck.state;
            roleCheckUpdate.IsBeginning = roleCheck.state == LfgRoleCheckState.Initialiting;

            if (!dungeons.Empty())
                foreach (var it in dungeons)
                    roleCheckUpdate.JoinSlots.Add(Global.LFGMgr.GetLFGDungeonEntry(it)); // Dungeon

            if (!roleCheck.roles.Empty())
            {
                // Leader info MUST be sent 1st :S
                ObjectGuid guid = roleCheck.leader;
                LfgRoles roles = roleCheck.roles.LookupByKey(guid);
                Player player = Global.ObjAccessor.FindPlayer(guid);

                var roleCheckUpdateMember = new LFGRoleCheckUpdate.LFGRoleCheckUpdateMember();

                roleCheckUpdateMember.Guid = guid;
                roleCheckUpdateMember.RolesDesired = (uint)roles;

                roleCheckUpdateMember.Level = (byte)(player ? player.getLevel() : 0);
                roleCheckUpdateMember.RoleCheckComplete = roles > 0;

                roleCheckUpdate.Members.Add(roleCheckUpdateMember);

                foreach (var it in roleCheck.roles)
                {
                    if (it.Key == roleCheck.leader)
                        continue;

                    roleCheckUpdateMember = new LFGRoleCheckUpdate.LFGRoleCheckUpdateMember();

                    guid = it.Key;
                    roles = it.Value;
                    player = Global.ObjAccessor.FindPlayer(guid);
                    roleCheckUpdateMember.Guid = guid;
                    roleCheckUpdateMember.RolesDesired = (uint)roles;
                    roleCheckUpdateMember.Level = (byte)(player ? player.getLevel() : 0);
                    roleCheckUpdateMember.RoleCheckComplete = roles > 0;

                    roleCheckUpdate.Members.Add(roleCheckUpdateMember);
                }
            }

            SendPacket(roleCheckUpdate);
        }

        public void SendLfgJoinResult(LfgJoinResultData joinData)
        {
            ObjectGuid guid = GetPlayer().GetGUID();

            LFGJoinResult joinResult = new LFGJoinResult();
            joinResult.Ticket = new RideTicket(guid, Global.LFGMgr.GetQueueId(guid), (int)Global.LFGMgr.GetState(guid), (uint)Time.UnixTime);

            joinResult.Result = (byte)joinData.result;
            joinResult.ResultDetail= (byte)joinData.state;
            foreach (var it in joinData.lockmap)
            {
                var blackList = new LFGBlackList(it.Value);
                blackList.PlayerGuid.Set(it.Key);
                joinResult.BlackList.Add(blackList);
            }

            SendPacket(joinResult);
        }

        public void SendLfgQueueStatus(LfgQueueStatusData queueData)
        {
            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_QUEUE_STATUS {0} state: {1} dungeon: {2}, waitTime: {3}, " +
                "avgWaitTime: {4}, waitTimeTanks: {5}, waitTimeHealer: {6}, waitTimeDps: {7}, queuedTime: {8}, tanks: {9}, healers: {10}, dps: {11}",
                GetPlayerInfo(), Global.LFGMgr.GetState(GetPlayer().GetGUID()), queueData.dungeonId, queueData.waitTime, queueData.waitTimeAvg,
                queueData.waitTimeTank, queueData.waitTimeHealer, queueData.waitTimeDps, queueData.queuedTime, queueData.tanks, queueData.healers, queueData.dps);

            LFGQueueStatus queueStatus = new LFGQueueStatus();
            queueStatus.Ticket = new RideTicket(GetPlayer().GetGUID(), queueData.dungeonId, 3, (uint)queueData.joinTime);
            queueStatus.Slot = queueData.queueId;
            queueStatus.AvgWaitTime = (uint)queueData.waitTimeAvg;
            queueStatus.QueuedTime = queueData.queuedTime;


            queueStatus.LastNeeded[0] = queueData.tanks;
            queueStatus.AvgWaitTimeByRole[0] = (uint)queueData.waitTimeTank;
            queueStatus.LastNeeded[1] = queueData.healers;
            queueStatus.AvgWaitTimeByRole[1] = (uint)queueData.waitTimeHealer;
            queueStatus.LastNeeded[2] = queueData.dps;
            queueStatus.AvgWaitTimeByRole[2] = (uint)queueData.waitTimeDps;
            queueStatus.AvgWaitTimeMe = (uint)queueData.waitTime;

            SendPacket(queueStatus);
        }

        public void SendLfgPlayerReward(LfgPlayerRewardData rewardData)
        {
            if (rewardData.rdungeonEntry == 0 || rewardData.sdungeonEntry == 0 || rewardData.quest == null)
                return;

            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_PLAYER_REWARD {0} rdungeonEntry: {1}, sdungeonEntry: {2}, done: {3}",
                GetPlayerInfo(), rewardData.rdungeonEntry, rewardData.sdungeonEntry, rewardData.done);

            byte itemNum = (byte)(rewardData.quest.GetRewItemsCount() + rewardData.quest.GetRewCurrencyCount());

            LFGPlayerReward playerReward = new LFGPlayerReward();
            playerReward.ActualSlot = rewardData.rdungeonEntry;                               // Random Dungeon Finished
            playerReward.QueuedSlot = rewardData.sdungeonEntry;                               // Dungeon Finished

            playerReward.RewardMoney = GetPlayer().GetQuestMoneyReward(rewardData.quest);
            playerReward.AddedXP = GetPlayer().GetQuestXPReward(rewardData.quest);

            for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            {
                var playerRewards = new LFGPlayerReward.LFGPlayerRewards();
                uint currencyId = rewardData.quest.RewardCurrencyId[i];
                if (currencyId != 0)
                {
                    playerRewards.RewardItem = currencyId;
                    playerRewards.RewardItemQuantity = rewardData.quest.RewardCurrencyCount[i];
                    playerRewards.IsCurrency = true;
                    playerReward.Rewards.Add(playerRewards);
                }
            }

            for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                var playerRewards = new LFGPlayerReward.LFGPlayerRewards();
                uint itemId = rewardData.quest.RewardItemId[i];
                if (itemId != 0)
                {
                    ItemTemplate item = Global.ObjectMgr.GetItemTemplate(itemId);
                    playerRewards.RewardItem = itemId;
                    playerRewards.RewardItemQuantity = rewardData.quest.RewardItemCount[i];
                    playerRewards.IsCurrency = false;
                    playerReward.Rewards.Add(playerRewards);
                }
            }

            SendPacket(playerReward);
        }

        public void SendLfgBootProposalUpdate(LfgPlayerBoot boot)
        {
            ObjectGuid guid = GetPlayer().GetGUID();
            LfgAnswer playerVote = boot.votes.LookupByKey(guid);
            byte votesNum = 0;
            byte agreeNum = 0;
            uint secsleft = (uint)((boot.cancelTime - Time.UnixTime) / 1000);
            foreach (var it in boot.votes)
            {
                if (it.Value != LfgAnswer.Pending)
                {
                    ++votesNum;
                    if (it.Value == LfgAnswer.Agree)
                        ++agreeNum;
                }
            }
            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_BOOT_PROPOSAL_UPDATE {0} inProgress: {1} - didVote: {2} - agree: {3} - victim: {4} votes: {5} - agrees: {6} - left: {7} - needed: {8} - reason {9}",
                GetPlayerInfo(), boot.inProgress, playerVote != LfgAnswer.Pending, playerVote == LfgAnswer.Agree, boot.victim.ToString(), votesNum, agreeNum, secsleft, SharedConst.LFGKickVotesNeeded, boot.reason);

            LfgBootPlayer lfgBootPlayer = new LfgBootPlayer();
            lfgBootPlayer.Info.VoteInProgress = boot.inProgress;                                 // Vote in progress
            lfgBootPlayer.Info.VotePassed = agreeNum >= SharedConst.LFGKickVotesNeeded;    // Did succeed
            lfgBootPlayer.Info.MyVoteCompleted = playerVote != LfgAnswer.Pending;           // Did Vote
            lfgBootPlayer.Info.MyVote = playerVote == LfgAnswer.Agree;             // Agree
            lfgBootPlayer.Info.Target = boot.victim;                                    // Victim GUID
            lfgBootPlayer.Info.TotalVotes = votesNum;                                       // Total Votes
            lfgBootPlayer.Info.BootVotes = agreeNum;                                       // Agree Count
            lfgBootPlayer.Info.TimeLeft = secsleft;                                       // Time Left
            lfgBootPlayer.Info.VotesNeeded = SharedConst.LFGKickVotesNeeded;               // Needed Votes
            lfgBootPlayer.Info.Reason = boot.reason;                                    // Kick reason
            SendPacket(lfgBootPlayer);
        }

        public void SendLfgProposalUpdate(LfgProposal proposal)
        {
            ObjectGuid playerGuid = GetPlayer().GetGUID();
            ObjectGuid guildGuid = proposal.players.LookupByKey(playerGuid).group;
            bool silent = !proposal.isNew && guildGuid == proposal.group;
            uint dungeonEntry = proposal.dungeonId;

            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_PROPOSAL_UPDATE {0} state: {1}", GetPlayerInfo(), proposal.state);

            // show random dungeon if player selected random dungeon and it's not lfg group
            if (!silent)
            {
                List<uint> playerDungeons = Global.LFGMgr.GetSelectedDungeons(playerGuid);
                if (!playerDungeons.Contains(proposal.dungeonId))
                    dungeonEntry = playerDungeons.First();
            }

            dungeonEntry = Global.LFGMgr.GetLFGDungeonEntry(dungeonEntry);

            LFGProposalUpdate proposalUpdate = new LFGProposalUpdate();
            proposalUpdate.Ticket = new RideTicket(playerGuid, Global.LFGMgr.GetQueueId(playerGuid), 3, (uint)Global.LFGMgr.GetQueueJoinTime(playerGuid));

            proposalUpdate.InstanceID = dungeonEntry;
            proposalUpdate.ProposalID = proposal.id;
            proposalUpdate.Slot = proposal.dungeonId;
            proposalUpdate.State = (byte)proposal.state;
            proposalUpdate.CompletedMask = proposal.encounters;

            proposalUpdate.ValidCompletedMask = false;
            proposalUpdate.ProposalSilent = silent;

            foreach (var pair in proposal.players)
            {
                var updatePlayer = new LFGProposalUpdate.LFGProposalUpdatePlayer();
                updatePlayer.Roles = (uint)pair.Value.role;
                updatePlayer.Me = (pair.Key == playerGuid);
                updatePlayer.SameParty = (pair.Value.group == guildGuid);
                updatePlayer.MyParty = (pair.Value.group == proposal.group);
                updatePlayer.Responded = (pair.Value.accept != LfgAnswer.Pending);
                updatePlayer.Accepted = (pair.Value.accept == LfgAnswer.Agree);

                proposalUpdate.Players.Add(updatePlayer);
            }

            SendPacket(proposalUpdate);
        }

        public void SendLfgLfrList(bool update)
        {
            /* WorldPacket data = new WorldPacket(ServerOpcodes.LfgUpdateSearch);
            data.WriteUInt8(update);                                 // In Lfg Queue?
            SendPacket(data);
            */
        }

        public void SendLfgDisabled()
        {
            SendPacket(new LfgDisabled());
        }

        public void SendLfgOfferContinue(uint dungeonEntry)
        {
            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_OFFER_CONTINUE {0} dungeon entry: {1}", GetPlayerInfo(), dungeonEntry);
            LfgOfferContinue lfgOfferContinue = new LfgOfferContinue();
            lfgOfferContinue.Slot = dungeonEntry;
            SendPacket(lfgOfferContinue);
        }

        public void SendLfgTeleportError(LfgTeleportResult err)
        {
            Log.outDebug(LogFilter.Lfg, "SMSG_LFG_TELEPORT_DENIED {0} reason: {1}", GetPlayerInfo(), err);
            LfgTeleportDenied lfgTeleportDenied = new LfgTeleportDenied();
            lfgTeleportDenied.Reason = err;
            SendPacket(lfgTeleportDenied);
        }
    }
}