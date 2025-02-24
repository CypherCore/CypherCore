// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
        void HandleGuildQuery(QueryGuildInfo query)
        {
            Guild guild = Global.GuildMgr.GetGuildByGuid(query.GuildGuid);
            if (guild != null)
            {
                guild.SendQueryResponse(this);
                return;
            }

            QueryGuildInfoResponse response = new();
            response.GuildGUID = query.GuildGuid;
            SendPacket(response);
        }

        [WorldPacketHandler(ClientOpcodes.GuildInviteByName)]
        void HandleGuildInviteByName(GuildInviteByName packet)
        {
            if (!ObjectManager.NormalizePlayerName(ref packet.Name))
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleInviteMember(this, packet.Name);
        }

        [WorldPacketHandler(ClientOpcodes.GuildOfficerRemoveMember)]
        void HandleGuildOfficerRemoveMember(GuildOfficerRemoveMember packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleRemoveMember(this, packet.Removee);
        }

        [WorldPacketHandler(ClientOpcodes.AcceptGuildInvite)]
        void HandleGuildAcceptInvite(AcceptGuildInvite packet)
        {
            if (GetPlayer().GetGuildId() == 0)
            {
                Guild guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildIdInvited());
                if (guild != null)
                    guild.HandleAcceptMember(this);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildDeclineInvitation)]
        void HandleGuildDeclineInvitation(GuildDeclineInvitation packet)
        {
            if (GetPlayer().GetGuildId() != 0)
                return;

            GetPlayer().SetGuildIdInvited(0);
            GetPlayer().SetInGuild(0);
        }

        [WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
        void HandleGuildGetRoster(GuildGetRoster packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleRoster(this);
            else
                Guild.SendCommandResult(this, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
        }

        [WorldPacketHandler(ClientOpcodes.GuildPromoteMember)]
        void HandleGuildPromoteMember(GuildPromoteMember packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleUpdateMemberRank(this, packet.Promotee, false);
        }

        [WorldPacketHandler(ClientOpcodes.GuildDemoteMember)]
        void HandleGuildDemoteMember(GuildDemoteMember packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleUpdateMemberRank(this, packet.Demotee, true);
        }

        [WorldPacketHandler(ClientOpcodes.GuildAssignMemberRank)]
        void HandleGuildAssignRank(GuildAssignMemberRank packet)
        {
            ObjectGuid setterGuid = GetPlayer().GetGUID();

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetMemberRank(this, packet.Member, setterGuid, (GuildRankOrder)packet.RankOrder);
        }

        [WorldPacketHandler(ClientOpcodes.GuildLeave)]
        void HandleGuildLeave(GuildLeave packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleLeaveMember(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildDelete)]
        void HandleGuildDisband(GuildDelete packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleDelete(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildUpdateMotdText)]
        void HandleGuildUpdateMotdText(GuildUpdateMotdText packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.MotdText))
                return;

            if (packet.MotdText.Length > 255)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetMOTD(this, packet.MotdText);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetMemberNote)]
        void HandleGuildSetMemberNote(GuildSetMemberNote packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.Note))
                return;

            if (packet.Note.Length > 31)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetMemberNote(this, packet.Note, packet.NoteeGUID, packet.IsPublic);
        }

        [WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
        void HandleGuildGetRanks(GuildGetRanks packet)
        {
            Guild guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);
            if (guild != null)
                if (guild.IsMember(GetPlayer().GetGUID()))
                    guild.SendGuildRankInfo(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildAddRank)]
        void HandleGuildAddRank(GuildAddRank packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.Name))
                return;

            if (packet.Name.Length > 15)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleAddNewRank(this, packet.Name);
        }

        [WorldPacketHandler(ClientOpcodes.GuildDeleteRank)]
        void HandleGuildDeleteRank(GuildDeleteRank packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleRemoveRank(this, (GuildRankOrder)packet.RankOrder);
        }

        [WorldPacketHandler(ClientOpcodes.GuildShiftRank)]
        void HandleGuildShiftRank(GuildShiftRank shiftRank)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleShiftRank(this, (GuildRankOrder)shiftRank.RankOrder, shiftRank.ShiftUp);
        }

        [WorldPacketHandler(ClientOpcodes.GuildUpdateInfoText)]
        void HandleGuildUpdateInfoText(GuildUpdateInfoText packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.InfoText))
                return;

            if (packet.InfoText.Length > 500)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetInfo(this, packet.InfoText);
        }

        [WorldPacketHandler(ClientOpcodes.SaveGuildEmblem)]
        void HandleSaveGuildEmblem(SaveGuildEmblem packet)
        {
            Guild.EmblemInfo emblemInfo = new();
            emblemInfo.ReadPacket(packet);

            if (GetPlayer().GetNPCIfCanInteractWith(packet.Vendor, NPCFlags.TabardDesigner, NPCFlags2.None) != null)
            {
                // Remove fake death
                if (GetPlayer().HasUnitState(UnitState.Died))
                    GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

                if (!emblemInfo.ValidateEmblemColors())
                {
                    Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidTabardColors);
                    return;
                }

                Guild guild = GetPlayer().GetGuild();
                if (guild != null)
                    guild.HandleSetEmblem(this, emblemInfo);
                else
                    Guild.SendSaveEmblemResult(this, GuildEmblemError.NoGuild); // "You are not part of a guild!";
            }
            else
                Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidVendor); // "That's not an emblem vendor!"
        }

        [WorldPacketHandler(ClientOpcodes.GuildEventLogQuery)]
        void HandleGuildEventLogQuery(GuildEventLogQuery packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.SendEventLog(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
        void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.SendMoneyInfo(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
        void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.SendPermissions(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankActivate)]
        void HandleGuildBankActivate(GuildBankActivate packet)
        {
            GameObject go = GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank);
            if (go == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
            {
                Guild.SendCommandResult(this, GuildCommandType.ViewTab, GuildCommandError.PlayerNotInGuild);
                return;
            }

            guild.SendBankList(this, 0, packet.FullUpdate);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankQueryTab)]
        void HandleGuildBankQueryTab(GuildBankQueryTab packet)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank) != null)
            {
                Guild guild = GetPlayer().GetGuild();
                if (guild != null)
                    guild.SendBankList(this, packet.Tab, packet.FullUpdate);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankDepositMoney)]
        void HandleGuildBankDepositMoney(GuildBankDepositMoney packet)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank) != null)
            {
                if (packet.Money != 0 && GetPlayer().HasEnoughMoney(packet.Money))
                {
                    Guild guild = GetPlayer().GetGuild();
                    if (guild != null)
                        guild.HandleMemberDepositMoney(this, packet.Money);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankWithdrawMoney)]
        void HandleGuildBankWithdrawMoney(GuildBankWithdrawMoney packet)
        {
            if (packet.Money != 0 && GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank) != null)
            {
                Guild guild = GetPlayer().GetGuild();
                if (guild != null)
                    guild.HandleMemberWithdrawMoney(this, packet.Money);
            }
        }

        [WorldPacketHandler(ClientOpcodes.AutoGuildBankItem)]
        void HandleAutoGuildBankItem(AutoGuildBankItem depositGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(depositGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(depositGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), depositGuildBankItem.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), false, depositGuildBankItem.BankTab, depositGuildBankItem.BankSlot,
                    depositGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), depositGuildBankItem.ContainerItemSlot, 0);
        }

        [WorldPacketHandler(ClientOpcodes.StoreGuildBankItem)]
        void HandleStoreGuildBankItem(StoreGuildBankItem storeGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(storeGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(storeGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), storeGuildBankItem.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), true, storeGuildBankItem.BankTab, storeGuildBankItem.BankSlot,
                    storeGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), storeGuildBankItem.ContainerItemSlot, 0);
        }

        [WorldPacketHandler(ClientOpcodes.SwapItemWithGuildBankItem)]
        void HandleSwapItemWithGuildBankItem(SwapItemWithGuildBankItem swapItemWithGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(swapItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(swapItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), swapItemWithGuildBankItem.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), false, swapItemWithGuildBankItem.BankTab, swapItemWithGuildBankItem.BankSlot,
                    swapItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), swapItemWithGuildBankItem.ContainerItemSlot, 0);
        }

        [WorldPacketHandler(ClientOpcodes.SwapGuildBankItemWithGuildBankItem)]
        void HandleSwapGuildBankItemWithGuildBankItem(SwapGuildBankItemWithGuildBankItem swapGuildBankItemWithGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(swapGuildBankItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            guild.SwapItems(GetPlayer(), swapGuildBankItemWithGuildBankItem.BankTab[0], swapGuildBankItemWithGuildBankItem.BankSlot[0],
                swapGuildBankItemWithGuildBankItem.BankTab[1], swapGuildBankItemWithGuildBankItem.BankSlot[1], 0);
        }

        [WorldPacketHandler(ClientOpcodes.MoveGuildBankItem)]
        void HandleMoveGuildBankItem(MoveGuildBankItem moveGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(moveGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            guild.SwapItems(GetPlayer(), moveGuildBankItem.BankTab, moveGuildBankItem.BankSlot, moveGuildBankItem.BankTab1, moveGuildBankItem.BankSlot1, 0);
        }

        [WorldPacketHandler(ClientOpcodes.MergeItemWithGuildBankItem)]
        void HandleMergeItemWithGuildBankItem(MergeItemWithGuildBankItem mergeItemWithGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(mergeItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(mergeItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeItemWithGuildBankItem.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), false, mergeItemWithGuildBankItem.BankTab, mergeItemWithGuildBankItem.BankSlot,
                    mergeItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeItemWithGuildBankItem.ContainerItemSlot, mergeItemWithGuildBankItem.StackCount);
        }

        [WorldPacketHandler(ClientOpcodes.SplitItemToGuildBank)]
        void HandleSplitItemToGuildBank(SplitItemToGuildBank splitItemToGuildBank)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(splitItemToGuildBank.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(splitItemToGuildBank.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitItemToGuildBank.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), false, splitItemToGuildBank.BankTab, splitItemToGuildBank.BankSlot,
                    splitItemToGuildBank.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitItemToGuildBank.ContainerItemSlot, splitItemToGuildBank.StackCount);
        }

        [WorldPacketHandler(ClientOpcodes.MergeGuildBankItemWithItem)]
        void HandleMergeGuildBankItemWithItem(MergeGuildBankItemWithItem mergeGuildBankItemWithItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(mergeGuildBankItemWithItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(mergeGuildBankItemWithItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeGuildBankItemWithItem.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), true, mergeGuildBankItemWithItem.BankTab, mergeGuildBankItemWithItem.BankSlot,
                    mergeGuildBankItemWithItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeGuildBankItemWithItem.ContainerItemSlot, mergeGuildBankItemWithItem.StackCount);
        }

        [WorldPacketHandler(ClientOpcodes.SplitGuildBankItemToInventory)]
        void HandleSplitGuildBankItemToInventory(SplitGuildBankItemToInventory splitGuildBankItemToInventory)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(splitGuildBankItemToInventory.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            if (!Player.IsInventoryPos(splitGuildBankItemToInventory.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitGuildBankItemToInventory.ContainerItemSlot))
                GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
            else
                guild.SwapItemsWithInventory(GetPlayer(), true, splitGuildBankItemToInventory.BankTab, splitGuildBankItemToInventory.BankSlot,
                    splitGuildBankItemToInventory.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitGuildBankItemToInventory.ContainerItemSlot, splitGuildBankItemToInventory.StackCount);
        }

        [WorldPacketHandler(ClientOpcodes.AutoStoreGuildBankItem)]
        void HandleAutoStoreGuildBankItem(AutoStoreGuildBankItem autoStoreGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(autoStoreGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            guild.SwapItemsWithInventory(GetPlayer(), true, autoStoreGuildBankItem.BankTab, autoStoreGuildBankItem.BankSlot, InventorySlots.Bag0, ItemConst.NullSlot, 0);
        }

        [WorldPacketHandler(ClientOpcodes.MergeGuildBankItemWithGuildBankItem)]
        void HandleMergeGuildBankItemWithGuildBankItem(MergeGuildBankItemWithGuildBankItem mergeGuildBankItemWithGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(mergeGuildBankItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            guild.SwapItems(GetPlayer(), mergeGuildBankItemWithGuildBankItem.BankTab, mergeGuildBankItemWithGuildBankItem.BankSlot,
                mergeGuildBankItemWithGuildBankItem.BankTab1, mergeGuildBankItemWithGuildBankItem.BankSlot1, mergeGuildBankItemWithGuildBankItem.StackCount);
        }

        [WorldPacketHandler(ClientOpcodes.SplitGuildBankItem)]
        void HandleSplitGuildBankItem(SplitGuildBankItem splitGuildBankItem)
        {
            if (GetPlayer().GetGameObjectIfCanInteractWith(splitGuildBankItem.Banker, GameObjectTypes.GuildBank) == null)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            guild.SwapItems(GetPlayer(), splitGuildBankItem.BankTab, splitGuildBankItem.BankSlot,
                splitGuildBankItem.BankTab1, splitGuildBankItem.BankSlot1, splitGuildBankItem.StackCount);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankBuyTab)]
        void HandleGuildBankBuyTab(GuildBankBuyTab packet)
        {
            if (packet.Banker.IsEmpty() || GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank) != null)
            {
                Guild guild = GetPlayer().GetGuild();
                if (guild != null)
                    guild.HandleBuyBankTab(this, packet.BankTab);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankUpdateTab)]
        void HandleGuildBankUpdateTab(GuildBankUpdateTab packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.Name))
                return;

            if ((packet.Name.Length > 15) || (packet.Icon.Length > 127))
                return;

            if (!string.IsNullOrEmpty(packet.Name) && !string.IsNullOrEmpty(packet.Icon))
            {
                if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank) != null)
                {
                    Guild guild = GetPlayer().GetGuild();
                    if (guild != null)
                        guild.HandleSetBankTabInfo(this, packet.BankTab, packet.Name, packet.Icon);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankLogQuery)]
        void HandleGuildBankLogQuery(GuildBankLogQuery packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.SendBankLog(this, (byte)packet.Tab);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankTextQuery)]
        void HandleGuildBankTextQuery(GuildBankTextQuery packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.SendBankTabText(this, (byte)packet.Tab);
        }

        [WorldPacketHandler(ClientOpcodes.GuildBankSetTabText)]
        void HandleGuildBankSetTabText(GuildBankSetTabText packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.TabText))
                return;

            if (packet.TabText.Length > 500)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.SetBankTabText((byte)packet.Tab, packet.TabText);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetRankPermissions)]
        void HandleGuildSetRankPermissions(GuildSetRankPermissions packet)
        {
            if (!DisallowHyperlinksAndMaybeKick(packet.RankName))
                return;

            if (packet.RankName.Length > 15)
                return;

            Guild guild = GetPlayer().GetGuild();
            if (guild == null)
                return;

            Guild.GuildBankRightsAndSlots[] rightsAndSlots = new Guild.GuildBankRightsAndSlots[GuildConst.MaxBankTabs];
            for (byte tabId = 0; tabId < GuildConst.MaxBankTabs; ++tabId)
                rightsAndSlots[tabId] = new Guild.GuildBankRightsAndSlots(tabId, (sbyte)packet.TabFlags[tabId], (int)packet.TabWithdrawItemLimit[tabId]);

            guild.HandleSetRankInfo(this, (GuildRankId)packet.RankID, packet.RankName, (GuildRankRights)packet.Flags, packet.WithdrawGoldLimit, rightsAndSlots);
        }

        [WorldPacketHandler(ClientOpcodes.RequestGuildPartyState)]
        void HandleGuildRequestPartyState(RequestGuildPartyState packet)
        {
            Guild guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);
            if (guild != null)
                guild.HandleGuildPartyRequest(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildChangeNameRequest, Processing = PacketProcessing.Inplace)]
        void HandleGuildChallengeUpdateRequest(GuildChallengeUpdateRequest packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleGuildRequestChallengeUpdate(this);
        }

        [WorldPacketHandler(ClientOpcodes.DeclineGuildInvites)]
        void HandleDeclineGuildInvites(DeclineGuildInvites packet)
        {
            if (packet.Allow)
                GetPlayer().SetPlayerFlag(PlayerFlags.AutoDeclineGuild);
            else
                GetPlayer().RemovePlayerFlag(PlayerFlags.AutoDeclineGuild);
        }

        [WorldPacketHandler(ClientOpcodes.RequestGuildRewardsList)]
        void HandleRequestGuildRewardsList(RequestGuildRewardsList packet)
        {
            if (Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId()) != null)
            {
                var rewards = Global.GuildMgr.GetGuildRewards();

                GuildRewardList rewardList = new();
                rewardList.Version = GameTime.GetGameTime();

                for (int i = 0; i < rewards.Count; i++)
                {
                    GuildRewardItem rewardItem = new();
                    rewardItem.ItemID = rewards[i].ItemID;
                    rewardItem.RaceMask = rewards[i].RaceMask;
                    rewardItem.MinGuildLevel = 0;
                    rewardItem.MinGuildRep = rewards[i].MinGuildRep;
                    rewardItem.AchievementsRequired = rewards[i].AchievementsRequired;
                    rewardItem.Cost = rewards[i].Cost;
                    rewardList.RewardItems.Add(rewardItem);
                }

                SendPacket(rewardList);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GuildQueryNews)]
        void HandleGuildQueryNews(GuildQueryNews packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                if (guild.GetGUID() == packet.GuildGUID)
                    guild.SendNewsUpdate(this);
        }

        [WorldPacketHandler(ClientOpcodes.GuildNewsUpdateSticky)]
        void HandleGuildNewsUpdateSticky(GuildNewsUpdateSticky packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleNewsSetSticky(this, (uint)packet.NewsID, packet.Sticky);
        }

        [WorldPacketHandler(ClientOpcodes.GuildReplaceGuildMaster)]
        void HandleGuildReplaceGuildMaster(GuildReplaceGuildMaster replaceGuildMaster)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetNewGuildMaster(this, "", true);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetGuildMaster)]
        void HandleGuildSetGuildMaster(GuildSetGuildMaster packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetNewGuildMaster(this, packet.NewMasterName, false);
        }

        [WorldPacketHandler(ClientOpcodes.GuildSetAchievementTracking)]
        void HandleGuildSetAchievementTracking(GuildSetAchievementTracking packet)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleSetAchievementTracking(this, packet.AchievementIDs);
        }

        [WorldPacketHandler(ClientOpcodes.GuildGetAchievementMembers)]
        void HandleGuildGetAchievementMembers(GuildGetAchievementMembers getAchievementMembers)
        {
            Guild guild = GetPlayer().GetGuild();
            if (guild != null)
                guild.HandleGetAchievementMembers(this, getAchievementMembers.AchievementID);
        }
    }
}
