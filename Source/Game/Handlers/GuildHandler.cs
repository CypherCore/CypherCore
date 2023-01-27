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
		private void HandleGuildQuery(QueryGuildInfo query)
		{
			Guild guild = Global.GuildMgr.GetGuildByGuid(query.GuildGuid);

			if (guild)
			{
				guild.SendQueryResponse(this);

				return;
			}

			QueryGuildInfoResponse response = new();
			response.GuildGUID = query.GuildGuid;
			SendPacket(response);
		}

		[WorldPacketHandler(ClientOpcodes.GuildInviteByName)]
		private void HandleGuildInviteByName(GuildInviteByName packet)
		{
			if (!ObjectManager.NormalizePlayerName(ref packet.Name))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleInviteMember(this, packet.Name);
		}

		[WorldPacketHandler(ClientOpcodes.GuildOfficerRemoveMember)]
		private void HandleGuildOfficerRemoveMember(GuildOfficerRemoveMember packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleRemoveMember(this, packet.Removee);
		}

		[WorldPacketHandler(ClientOpcodes.AcceptGuildInvite)]
		private void HandleGuildAcceptInvite(AcceptGuildInvite packet)
		{
			if (GetPlayer().GetGuildId() == 0)
			{
				Guild guild = Global.GuildMgr.GetGuildById(GetPlayer().GetGuildIdInvited());

				if (guild)
					guild.HandleAcceptMember(this);
			}
		}

		[WorldPacketHandler(ClientOpcodes.GuildDeclineInvitation)]
		private void HandleGuildDeclineInvitation(GuildDeclineInvitation packet)
		{
			if (GetPlayer().GetGuildId() != 0)
				return;

			GetPlayer().SetGuildIdInvited(0);
			GetPlayer().SetInGuild(0);
		}

		[WorldPacketHandler(ClientOpcodes.GuildGetRoster)]
		private void HandleGuildGetRoster(GuildGetRoster packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleRoster(this);
			else
				Guild.SendCommandResult(this, GuildCommandType.GetRoster, GuildCommandError.PlayerNotInGuild);
		}

		[WorldPacketHandler(ClientOpcodes.GuildPromoteMember)]
		private void HandleGuildPromoteMember(GuildPromoteMember packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleUpdateMemberRank(this, packet.Promotee, false);
		}

		[WorldPacketHandler(ClientOpcodes.GuildDemoteMember)]
		private void HandleGuildDemoteMember(GuildDemoteMember packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleUpdateMemberRank(this, packet.Demotee, true);
		}

		[WorldPacketHandler(ClientOpcodes.GuildAssignMemberRank)]
		private void HandleGuildAssignRank(GuildAssignMemberRank packet)
		{
			ObjectGuid setterGuid = GetPlayer().GetGUID();

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetMemberRank(this, packet.Member, setterGuid, (GuildRankOrder)packet.RankOrder);
		}

		[WorldPacketHandler(ClientOpcodes.GuildLeave)]
		private void HandleGuildLeave(GuildLeave packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleLeaveMember(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildDelete)]
		private void HandleGuildDisband(GuildDelete packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleDelete(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildUpdateMotdText)]
		private void HandleGuildUpdateMotdText(GuildUpdateMotdText packet)
		{
			if (!DisallowHyperlinksAndMaybeKick(packet.MotdText))
				return;

			if (packet.MotdText.Length > 255)
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetMOTD(this, packet.MotdText);
		}

		[WorldPacketHandler(ClientOpcodes.GuildSetMemberNote)]
		private void HandleGuildSetMemberNote(GuildSetMemberNote packet)
		{
			if (!DisallowHyperlinksAndMaybeKick(packet.Note))
				return;

			if (packet.Note.Length > 31)
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetMemberNote(this, packet.Note, packet.NoteeGUID, packet.IsPublic);
		}

		[WorldPacketHandler(ClientOpcodes.GuildGetRanks)]
		private void HandleGuildGetRanks(GuildGetRanks packet)
		{
			Guild guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);

			if (guild)
				if (guild.IsMember(GetPlayer().GetGUID()))
					guild.SendGuildRankInfo(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildAddRank)]
		private void HandleGuildAddRank(GuildAddRank packet)
		{
			if (!DisallowHyperlinksAndMaybeKick(packet.Name))
				return;

			if (packet.Name.Length > 15)
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleAddNewRank(this, packet.Name);
		}

		[WorldPacketHandler(ClientOpcodes.GuildDeleteRank)]
		private void HandleGuildDeleteRank(GuildDeleteRank packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleRemoveRank(this, (GuildRankOrder)packet.RankOrder);
		}

		[WorldPacketHandler(ClientOpcodes.GuildShiftRank)]
		private void HandleGuildShiftRank(GuildShiftRank shiftRank)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleShiftRank(this, (GuildRankOrder)shiftRank.RankOrder, shiftRank.ShiftUp);
		}

		[WorldPacketHandler(ClientOpcodes.GuildUpdateInfoText)]
		private void HandleGuildUpdateInfoText(GuildUpdateInfoText packet)
		{
			if (!DisallowHyperlinksAndMaybeKick(packet.InfoText))
				return;

			if (packet.InfoText.Length > 500)
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetInfo(this, packet.InfoText);
		}

		[WorldPacketHandler(ClientOpcodes.SaveGuildEmblem)]
		private void HandleSaveGuildEmblem(SaveGuildEmblem packet)
		{
			Guild.EmblemInfo emblemInfo = new();
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

				Guild guild = GetPlayer().GetGuild();

				if (guild)
					guild.HandleSetEmblem(this, emblemInfo);
				else
					Guild.SendSaveEmblemResult(this, GuildEmblemError.NoGuild); // "You are not part of a guild!";
			}
			else
			{
				Guild.SendSaveEmblemResult(this, GuildEmblemError.InvalidVendor); // "That's not an emblem vendor!"
			}
		}

		[WorldPacketHandler(ClientOpcodes.GuildEventLogQuery)]
		private void HandleGuildEventLogQuery(GuildEventLogQuery packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.SendEventLog(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankRemainingWithdrawMoneyQuery)]
		private void HandleGuildBankMoneyWithdrawn(GuildBankRemainingWithdrawMoneyQuery packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.SendMoneyInfo(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildPermissionsQuery)]
		private void HandleGuildPermissionsQuery(GuildPermissionsQuery packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.SendPermissions(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankActivate)]
		private void HandleGuildBankActivate(GuildBankActivate packet)
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
		private void HandleGuildBankQueryTab(GuildBankQueryTab packet)
		{
			if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
			{
				Guild guild = GetPlayer().GetGuild();

				if (guild)
					guild.SendBankList(this, packet.Tab, true /*packet.FullUpdate*/);
				// HACK: client doesn't query entire tab content if it had received SMSG_GUILD_BANK_LIST in this session
				// but we broadcast bank updates to entire guild when *ANYONE* changes anything, incorrectly initializing clients
				// tab content with only data for that change
			}
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankDepositMoney)]
		private void HandleGuildBankDepositMoney(GuildBankDepositMoney packet)
		{
			if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
				if (packet.Money != 0 &&
				    GetPlayer().HasEnoughMoney(packet.Money))
				{
					Guild guild = GetPlayer().GetGuild();

					if (guild)
						guild.HandleMemberDepositMoney(this, packet.Money);
				}
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankWithdrawMoney)]
		private void HandleGuildBankWithdrawMoney(GuildBankWithdrawMoney packet)
		{
			if (packet.Money != 0 &&
			    GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
			{
				Guild guild = GetPlayer().GetGuild();

				if (guild)
					guild.HandleMemberWithdrawMoney(this, packet.Money);
			}
		}

		[WorldPacketHandler(ClientOpcodes.AutoGuildBankItem)]
		private void HandleAutoGuildBankItem(AutoGuildBankItem depositGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(depositGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(depositGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), depositGuildBankItem.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             false,
				                             depositGuildBankItem.BankTab,
				                             depositGuildBankItem.BankSlot,
				                             depositGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             depositGuildBankItem.ContainerItemSlot,
				                             0);
		}

		[WorldPacketHandler(ClientOpcodes.StoreGuildBankItem)]
		private void HandleStoreGuildBankItem(StoreGuildBankItem storeGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(storeGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(storeGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), storeGuildBankItem.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             true,
				                             storeGuildBankItem.BankTab,
				                             storeGuildBankItem.BankSlot,
				                             storeGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             storeGuildBankItem.ContainerItemSlot,
				                             0);
		}

		[WorldPacketHandler(ClientOpcodes.SwapItemWithGuildBankItem)]
		private void HandleSwapItemWithGuildBankItem(SwapItemWithGuildBankItem swapItemWithGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(swapItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(swapItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), swapItemWithGuildBankItem.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             false,
				                             swapItemWithGuildBankItem.BankTab,
				                             swapItemWithGuildBankItem.BankSlot,
				                             swapItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             swapItemWithGuildBankItem.ContainerItemSlot,
				                             0);
		}

		[WorldPacketHandler(ClientOpcodes.SwapGuildBankItemWithGuildBankItem)]
		private void HandleSwapGuildBankItemWithGuildBankItem(SwapGuildBankItemWithGuildBankItem swapGuildBankItemWithGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(swapGuildBankItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			guild.SwapItems(GetPlayer(),
			                swapGuildBankItemWithGuildBankItem.BankTab[0],
			                swapGuildBankItemWithGuildBankItem.BankSlot[0],
			                swapGuildBankItemWithGuildBankItem.BankTab[1],
			                swapGuildBankItemWithGuildBankItem.BankSlot[1],
			                0);
		}

		[WorldPacketHandler(ClientOpcodes.MoveGuildBankItem)]
		private void HandleMoveGuildBankItem(MoveGuildBankItem moveGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(moveGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			guild.SwapItems(GetPlayer(), moveGuildBankItem.BankTab, moveGuildBankItem.BankSlot, moveGuildBankItem.BankTab1, moveGuildBankItem.BankSlot1, 0);
		}

		[WorldPacketHandler(ClientOpcodes.MergeItemWithGuildBankItem)]
		private void HandleMergeItemWithGuildBankItem(MergeItemWithGuildBankItem mergeItemWithGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(mergeItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(mergeItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeItemWithGuildBankItem.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             false,
				                             mergeItemWithGuildBankItem.BankTab,
				                             mergeItemWithGuildBankItem.BankSlot,
				                             mergeItemWithGuildBankItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             mergeItemWithGuildBankItem.ContainerItemSlot,
				                             mergeItemWithGuildBankItem.StackCount);
		}

		[WorldPacketHandler(ClientOpcodes.SplitItemToGuildBank)]
		private void HandleSplitItemToGuildBank(SplitItemToGuildBank splitItemToGuildBank)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(splitItemToGuildBank.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(splitItemToGuildBank.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitItemToGuildBank.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             false,
				                             splitItemToGuildBank.BankTab,
				                             splitItemToGuildBank.BankSlot,
				                             splitItemToGuildBank.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             splitItemToGuildBank.ContainerItemSlot,
				                             splitItemToGuildBank.StackCount);
		}

		[WorldPacketHandler(ClientOpcodes.MergeGuildBankItemWithItem)]
		private void HandleMergeGuildBankItemWithItem(MergeGuildBankItemWithItem mergeGuildBankItemWithItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(mergeGuildBankItemWithItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(mergeGuildBankItemWithItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), mergeGuildBankItemWithItem.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             true,
				                             mergeGuildBankItemWithItem.BankTab,
				                             mergeGuildBankItemWithItem.BankSlot,
				                             mergeGuildBankItemWithItem.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             mergeGuildBankItemWithItem.ContainerItemSlot,
				                             mergeGuildBankItemWithItem.StackCount);
		}

		[WorldPacketHandler(ClientOpcodes.SplitGuildBankItemToInventory)]
		private void HandleSplitGuildBankItemToInventory(SplitGuildBankItemToInventory splitGuildBankItemToInventory)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(splitGuildBankItemToInventory.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			if (!Player.IsInventoryPos(splitGuildBankItemToInventory.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0), splitGuildBankItemToInventory.ContainerItemSlot))
				GetPlayer().SendEquipError(InventoryResult.InternalBagError, null);
			else
				guild.SwapItemsWithInventory(GetPlayer(),
				                             true,
				                             splitGuildBankItemToInventory.BankTab,
				                             splitGuildBankItemToInventory.BankSlot,
				                             splitGuildBankItemToInventory.ContainerSlot.GetValueOrDefault(InventorySlots.Bag0),
				                             splitGuildBankItemToInventory.ContainerItemSlot,
				                             splitGuildBankItemToInventory.StackCount);
		}

		[WorldPacketHandler(ClientOpcodes.AutoStoreGuildBankItem)]
		private void HandleAutoStoreGuildBankItem(AutoStoreGuildBankItem autoStoreGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(autoStoreGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			guild.SwapItemsWithInventory(GetPlayer(), true, autoStoreGuildBankItem.BankTab, autoStoreGuildBankItem.BankSlot, InventorySlots.Bag0, ItemConst.NullSlot, 0);
		}

		[WorldPacketHandler(ClientOpcodes.MergeGuildBankItemWithGuildBankItem)]
		private void HandleMergeGuildBankItemWithGuildBankItem(MergeGuildBankItemWithGuildBankItem mergeGuildBankItemWithGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(mergeGuildBankItemWithGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			guild.SwapItems(GetPlayer(),
			                mergeGuildBankItemWithGuildBankItem.BankTab,
			                mergeGuildBankItemWithGuildBankItem.BankSlot,
			                mergeGuildBankItemWithGuildBankItem.BankTab1,
			                mergeGuildBankItemWithGuildBankItem.BankSlot1,
			                mergeGuildBankItemWithGuildBankItem.StackCount);
		}

		[WorldPacketHandler(ClientOpcodes.SplitGuildBankItem)]
		private void HandleSplitGuildBankItem(SplitGuildBankItem splitGuildBankItem)
		{
			if (!GetPlayer().GetGameObjectIfCanInteractWith(splitGuildBankItem.Banker, GameObjectTypes.GuildBank))
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild == null)
				return;

			guild.SwapItems(GetPlayer(),
			                splitGuildBankItem.BankTab,
			                splitGuildBankItem.BankSlot,
			                splitGuildBankItem.BankTab1,
			                splitGuildBankItem.BankSlot1,
			                splitGuildBankItem.StackCount);
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankBuyTab)]
		private void HandleGuildBankBuyTab(GuildBankBuyTab packet)
		{
			if (packet.Banker.IsEmpty() ||
			    GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
			{
				Guild guild = GetPlayer().GetGuild();

				if (guild)
					guild.HandleBuyBankTab(this, packet.BankTab);
			}
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankUpdateTab)]
		private void HandleGuildBankUpdateTab(GuildBankUpdateTab packet)
		{
			if (!DisallowHyperlinksAndMaybeKick(packet.Name))
				return;

			if ((packet.Name.Length > 15) ||
			    (packet.Icon.Length > 127))
				return;

			if (!string.IsNullOrEmpty(packet.Name) &&
			    !string.IsNullOrEmpty(packet.Icon))
				if (GetPlayer().GetGameObjectIfCanInteractWith(packet.Banker, GameObjectTypes.GuildBank))
				{
					Guild guild = GetPlayer().GetGuild();

					if (guild)
						guild.HandleSetBankTabInfo(this, packet.BankTab, packet.Name, packet.Icon);
				}
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankLogQuery)]
		private void HandleGuildBankLogQuery(GuildBankLogQuery packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.SendBankLog(this, (byte)packet.Tab);
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankTextQuery)]
		private void HandleGuildBankTextQuery(GuildBankTextQuery packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.SendBankTabText(this, (byte)packet.Tab);
		}

		[WorldPacketHandler(ClientOpcodes.GuildBankSetTabText)]
		private void HandleGuildBankSetTabText(GuildBankSetTabText packet)
		{
			if (!DisallowHyperlinksAndMaybeKick(packet.TabText))
				return;

			if (packet.TabText.Length > 500)
				return;

			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.SetBankTabText((byte)packet.Tab, packet.TabText);
		}

		[WorldPacketHandler(ClientOpcodes.GuildSetRankPermissions)]
		private void HandleGuildSetRankPermissions(GuildSetRankPermissions packet)
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
		private void HandleGuildRequestPartyState(RequestGuildPartyState packet)
		{
			Guild guild = Global.GuildMgr.GetGuildByGuid(packet.GuildGUID);

			if (guild)
				guild.HandleGuildPartyRequest(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildChangeNameRequest, Processing = PacketProcessing.Inplace)]
		private void HandleGuildChallengeUpdateRequest(GuildChallengeUpdateRequest packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleGuildRequestChallengeUpdate(this);
		}

		[WorldPacketHandler(ClientOpcodes.DeclineGuildInvites)]
		private void HandleDeclineGuildInvites(DeclineGuildInvites packet)
		{
			if (packet.Allow)
				GetPlayer().SetPlayerFlag(PlayerFlags.AutoDeclineGuild);
			else
				GetPlayer().RemovePlayerFlag(PlayerFlags.AutoDeclineGuild);
		}

		[WorldPacketHandler(ClientOpcodes.RequestGuildRewardsList)]
		private void HandleRequestGuildRewardsList(RequestGuildRewardsList packet)
		{
			if (Global.GuildMgr.GetGuildById(GetPlayer().GetGuildId()))
			{
				var rewards = Global.GuildMgr.GetGuildRewards();

				GuildRewardList rewardList = new();
				rewardList.Version = GameTime.GetGameTime();

				for (int i = 0; i < rewards.Count; i++)
				{
					GuildRewardItem rewardItem = new();
					rewardItem.ItemID               = rewards[i].ItemID;
					rewardItem.RaceMask             = (uint)rewards[i].RaceMask;
					rewardItem.MinGuildLevel        = 0;
					rewardItem.MinGuildRep          = rewards[i].MinGuildRep;
					rewardItem.AchievementsRequired = rewards[i].AchievementsRequired;
					rewardItem.Cost                 = rewards[i].Cost;
					rewardList.RewardItems.Add(rewardItem);
				}

				SendPacket(rewardList);
			}
		}

		[WorldPacketHandler(ClientOpcodes.GuildQueryNews)]
		private void HandleGuildQueryNews(GuildQueryNews packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				if (guild.GetGUID() == packet.GuildGUID)
					guild.SendNewsUpdate(this);
		}

		[WorldPacketHandler(ClientOpcodes.GuildNewsUpdateSticky)]
		private void HandleGuildNewsUpdateSticky(GuildNewsUpdateSticky packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleNewsSetSticky(this, (uint)packet.NewsID, packet.Sticky);
		}

		[WorldPacketHandler(ClientOpcodes.GuildReplaceGuildMaster)]
		private void HandleGuildReplaceGuildMaster(GuildReplaceGuildMaster replaceGuildMaster)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetNewGuildMaster(this, "", true);
		}

		[WorldPacketHandler(ClientOpcodes.GuildSetGuildMaster)]
		private void HandleGuildSetGuildMaster(GuildSetGuildMaster packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetNewGuildMaster(this, packet.NewMasterName, false);
		}

		[WorldPacketHandler(ClientOpcodes.GuildSetAchievementTracking)]
		private void HandleGuildSetAchievementTracking(GuildSetAchievementTracking packet)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleSetAchievementTracking(this, packet.AchievementIDs);
		}

		[WorldPacketHandler(ClientOpcodes.GuildGetAchievementMembers)]
		private void HandleGuildGetAchievementMembers(GuildGetAchievementMembers getAchievementMembers)
		{
			Guild guild = GetPlayer().GetGuild();

			if (guild)
				guild.HandleGetAchievementMembers(this, getAchievementMembers.AchievementID);
		}
	}
}