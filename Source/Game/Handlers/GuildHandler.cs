/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using Game.Guilds;
using Game.Networking;
using Game.Networking.Packets;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.QueryGuildInfo, Status = SessionStatus.Authed)]
        private void HandleGuildQuery(QueryGuildInfo query)
        {
            var guild = Global.GuildMgr.GetGuildByGuid(query.GuildGuid);
            if (guild)
            {
                if (guild.IsMember(query.PlayerGuid))
                {
                    guild.SendQueryResponse(this, query.PlayerGuid);
                    return;
                }
            }

            var response = new QueryGuildInfoResponse();
            response.GuildGUID = query.GuildGuid;
            response.PlayerGuid = query.PlayerGuid;
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.GuildInviteByName)]
        private void HandleGuildInviteByName(GuildInviteByName packet)
        {
            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleInviteMember(this, packet.Name);
        }

        [WorldPacketHandler(ClientOpcodes.GuildOfficerRemoveMember)]
        private void HandleGuildOfficerRemoveMember(GuildOfficerRemoveMember packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleRemoveMember(this, packet.Removee);
        }

        [WorldPacketHandler(ClientOpcodes.AcceptGuildInvite)]
        private void HandleGuildAcceptInvite(AcceptGuildInvite packet)
        {
            if (GetPlayer().GetGuildId() == 0)
            {
                var guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildIdInvited());
                if (guild)
                    guild.HandleAcceptMember(this);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildDeclineInvitation)]
        private void HandleGuildDeclineInvitation(GuildDeclineInvitation packet)
        {
            GetPlayer().SetGuildIdInvited(0);
            GetPlayer().SetInGuild(0);
        }

        [WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
        private void HandleGuildGetRoster(GuildGetRoster packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleRoster(this);
            else
                Guild.SendCommandResult(this, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
        }

        [WorldPacketHandler(ClientOpcodes.GuildPromoteMember)]
        private void HandleGuildPromoteMember(GuildPromoteMember packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleUpdateMemberRank(this, packet.Promotee, false);
        }

        [WorldPacketHandler(ClientOpcodes.GuildDemoteMember)]
        private void HandleGuildDemoteMember(GuildDemoteMember packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleUpdateMemberRank(this, packet.Demotee, true);
        }

        [WorldPacketHandler(ClientOpcodes.GuildAssignMemberRank)]
        private void HandleGuildAssignRank(GuildAssignMemberRank packet)
        {
            var setterGuid = GetPlayer().GetGUID();

            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetMemberRank(this, packet.Member, setterGuid, (byte)packet.RankOrder);
        }

        [WorldPacketHandler(ClientOpcodes.GuildLeave)]
        private void HandleGuildLeave(GuildLeave packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleLeaveMember(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildDelete)]
        private void HandleGuildDisband(GuildDelete packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleDelete(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildUpdateMotdText)]
        private void HandleGuildUpdateMotdText(GuildUpdateMotdText packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetMOTD(this, packet.MotdText);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetMemberNote)]
        private void HandleGuildSetMemberNote(GuildSetMemberNote packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetMemberNote(this, packet.Note, packet.NoteeGUID, packet.IsPublic);
        }

        [WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
        private void HandleGuildGetRanks(GuildGetRanks packet)
        {
            var guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);
            if (guild)
                if (guild.IsMember(GetPlayer().GetGUID()))
                    guild.SendGuildRankInfo(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildAddRank)]
        private void HandleGuildAddRank(GuildAddRank packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleAddNewRank(this, packet.Name);
        }

        [WorldPacketHandler(ClientOpcodes.GuildDeleteRank)]
        private void HandleGuildDeleteRank(GuildDeleteRank packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleRemoveRank(this, (byte)packet.RankOrder);
        }

        [WorldPacketHandler(ClientOpcodes.GuildUpdateInfoText)]
        private void HandleGuildUpdateInfoText(GuildUpdateInfoText packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetInfo(this, packet.InfoText);
        }

        [WorldPacketHandler(ClientOpcodes.SaveGuildEmblem)]
        private void HandleSaveGuildEmblem(SaveGuildEmblem packet)
        {
            var emblemInfo = new Guild.EmblemInfo();
            emblemInfo.ReadPacket(packet);

            if (GetPlayer().GetNPCIfCanInteractWith(packet.Vendor, NPCFlags.TabardDesigner, NPCFlags2.None))
            {
                // Remove fake death
                if (GetPlayer().HasUnitState(UnitState.Died))
                    GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

                if (!emblemInfo.ValidateEmblemColors())
                {
                    Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidTabardColors);
                    return;
                }

                var guild = GetPlayer().GetGuild();
                if (guild)
                    guild.HandleSetEmblem(this, emblemInfo);
                else
                    Guild.SendSaveEmblemResult(this, GuildEmblemError.NoGuild); // "You are not part of a guild!";
            }
            else
                Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidVendor); // "That's not an emblem vendor!"
        }

        [WorldPacketHandler(ClientOpcodes.GuildEventLogQuery)]
        private void HandleGuildEventLogQuery(GuildEventLogQuery packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.SendEventLog(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
        private void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.SendMoneyInfo(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
        private void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.SendPermissions(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankActivate)]
        private void HandleGuildBankActivate(GuildBankActivate packet)
        {
            var go = GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank);
            if (go == null)
                return;

            var guild = GetPlayer().GetGuild();
            if (guild == null)
            {
                Guild.SendCommandResult(this, GuildCommandType.ViewTab, GuildCommandError.PlayerNotInGuild);
                return;
            }

            guild.SendBankList(this, 0, packet.FullUpdate);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankQueryTab)]
        private void HandleGuildBankQueryTab(GuildBankQueryTab packet)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
            {
                var guild = GetPlayer().GetGuild();
                if (guild)
                    guild.SendBankList(this, packet.Tab, packet.FullUpdate);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankDepositMoney)]
        private void HandleGuildBankDepositMoney(GuildBankDepositMoney packet)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
            {
                if (packet.Money != 0 && GetPlayer().HasEnoughMoney(packet.Money))
                {
                    var guild = GetPlayer().GetGuild();
                    if (guild)
                        guild.HandleMemberDepositMoney(this, packet.Money);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankWithdrawMoney)]
        private void HandleGuildBankWithdrawMoney(GuildBankWithdrawMoney packet)
        {
            if (packet.Money != 0 && GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
            {
                var guild = GetPlayer().GetGuild();
                if (guild)
                    guild.HandleMemberWithdrawMoney(this, packet.Money);
            }
        }

        //[WorldPacketHandler(ClientOpcodes.GuildBankSwapItems)]
        private void HandleGuildBankSwapItems(GuildBankSwapItems packet)
        {
            if (!GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
                return;

            var guild = GetPlayer().GetGuild();
            if (!guild)
                return;

            if (packet.BankOnly)
            {
                guild.SwapItems(GetPlayer(), packet.BankTab1, packet.BankSlot1, packet.BankTab, packet.BankSlot, (uint)packet.StackCount);
            }
            else
            {
                // Player <-> Bank
                // Allow to work with inventory only
                if (!Player.IsInventoryPos(packet.ContainerSlot, packet.ContainerItemSlot) && !packet.AutoStore)
                    GetPlayer().SendEquipError(InventoryResult.InternalBagError);
                else
                    guild.SwapItemsWithInventory(GetPlayer(), packet.ToSlot != 0, packet.BankTab, packet.BankSlot, packet.ContainerSlot, packet.ContainerItemSlot, (uint)packet.StackCount);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankBuyTab)]
        private void HandleGuildBankBuyTab(GuildBankBuyTab packet)
        {
            if (packet.Banker.IsEmpty() || GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
            {
                var guild = GetPlayer().GetGuild();
                if (guild)
                    guild.HandleBuyBankTab(this, packet.BankTab);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankUpdateTab)]
        private void HandleGuildBankUpdateTab(GuildBankUpdateTab packet)
        {
            if (!string.IsNullOrEmpty(packet.Name) && !string.IsNullOrEmpty(packet.Icon))
            {
                if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
                {
                    var guild = GetPlayer().GetGuild();
                    if (guild)
                        guild.HandleSetBankTabInfo(this, packet.BankTab, packet.Name, packet.Icon);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankLogQuery)]
        private void HandleGuildBankLogQuery(GuildBankLogQuery packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.SendBankLog(this, (byte)packet.Tab);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankTextQuery)]
        private void HandleGuildBankTextQuery(GuildBankTextQuery packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.SendBankTabText(this, (byte)packet.Tab);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankSetTabText)]
        private void HandleGuildBankSetTabText(GuildBankSetTabText packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.SetBankTabText((byte)packet.Tab, packet.TabText);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetRankPermissions)]
        private void HandleGuildSetRankPermissions(GuildSetRankPermissions packet)
        {
            var guild = GetPlayer().GetGuild();
            if (!guild)
                return;

            var rightsAndSlots = new Guild.GuildBankRightsAndSlots[GuildConst.MaxBankTabs];
            for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
                rightsAndSlots[tabId] = new Guild.GuildBankRightsAndSlots(tabId, (sbyte)packet.TabFlags[tabId], (int)packet.TabWithdrawItemLimit[tabId]);

            guild.HandleSetRankInfo(this, (byte)packet.RankOrder, packet.RankName, (GuildRankRights)packet.Flags, packet.WithdrawGoldLimit, rightsAndSlots);
        }

        [WorldPacketHandler(ClientOpcodes.RequestGuildPartyState)]
        private void HandleGuildRequestPartyState(RequestGuildPartyState packet)
        {
            var guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);
            if (guild)
                guild.HandleGuildPartyRequest(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildChangeNameRequest)]
        private void HandleGuildChallengeUpdateRequest(GuildChallengeUpdateRequest packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleGuildRequestChallengeUpdate(this);
        }

        [WorldPacketHandler(ClientOpcodes.DeclineGuildInvites)]
        private void HandleDeclineGuildInvites(DeclineGuildInvites packet)
        {
            if (packet.Allow)
                GetPlayer().AddPlayerFlag(PlayerFlags.AutoDeclineGuild);
            else
                GetPlayer().RemovePlayerFlag(PlayerFlags.AutoDeclineGuild);
        }

        [WorldPacketHandler(ClientOpcodes.RequestGuildRewardsList, Processing = PacketProcessing.Inplace)]
        private void HandleRequestGuildRewardsList(RequestGuildRewardsList packet)
        {
            if (Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId()))
            {
                var rewards = Global.GuildMgr.GetGuildRewards();

                var rewardList = new GuildRewardList();
                rewardList.Version = (uint)Time.UnixTime;

                for (var i = 0; i < rewards.Count; i++)
                {
                    var rewardItem = new GuildRewardItem();
                    rewardItem.ItemID = rewards[i].ItemID;
                    rewardItem.RaceMask = (uint)rewards[i].RaceMask;
                    rewardItem.MinGuildLevel = 0;
                    rewardItem.MinGuildRep = rewards[i].MinGuildRep;
                    rewardItem.AchievementsRequired = rewards[i].AchievementsRequired;
                    rewardItem.Cost = rewards[i].Cost;
                    rewardList.RewardItems.Add(rewardItem);
                }

                SendPacket(rewardList);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildQueryNews, Processing = PacketProcessing.Inplace)]
        private void HandleGuildQueryNews(GuildQueryNews packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                if (guild.GetGUID() == packet.GuildGUID)
                    guild.SendNewsUpdate(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildNewsUpdateSticky)]
        private void HandleGuildNewsUpdateSticky(GuildNewsUpdateSticky packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleNewsSetSticky(this, (uint)packet.NewsID, packet.Sticky);
        }

        [WorldPacketHandler(ClientOpcodes.GuildReplaceGuildMaster)]
        private void HandleGuildReplaceGuildMaster(GuildReplaceGuildMaster replaceGuildMaster)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetNewGuildMaster(this, "", true);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetGuildMaster)]
        private void HandleGuildSetGuildMaster(GuildSetGuildMaster packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetNewGuildMaster(this, packet.NewMasterName, false);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetAchievementTracking)]
        private void HandleGuildSetAchievementTracking(GuildSetAchievementTracking packet)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleSetAchievementTracking(this, packet.AchievementIDs);
        }

        [WorldPacketHandler(ClientOpcodes.GuildGetAchievementMembers)]
        private void HandleGuildGetAchievementMembers(GuildGetAchievementMembers getAchievementMembers)
        {
            var guild = GetPlayer().GetGuild();
            if (guild)
                guild.HandleGetAchievementMembers(this, getAchievementMembers.AchievementID);
        }
    }
}
