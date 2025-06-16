// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Misc;
using Game.Networking;
using Game.Networking.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.TabardVendorActivate, Processing = PacketProcessing.Inplace)]
        void HandleTabardVendorActivate(TabardVendorActivate tabardVendorActivate)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(tabardVendorActivate.Vendor, NPCFlags.TabardDesigner, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleTabardVendorActivateOpcode - {tabardVendorActivate.Vendor} not found or you can not interact with him.");
                return;
            }

            TabardVendorType type = (TabardVendorType)tabardVendorActivate.Type;
            if (type != TabardVendorType.Guild && type != TabardVendorType.Personal)
                return;

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendTabardVendorActivate(tabardVendorActivate.Vendor, type);
        }

        public void SendTabardVendorActivate(ObjectGuid guid, TabardVendorType type)
        {
            NPCInteractionOpenResult npcInteraction = new();
            npcInteraction.Npc = guid;

            switch (type)
            {
                case TabardVendorType.Guild:
                    npcInteraction.InteractionType = PlayerInteractionType.GuildTabardVendor;
                    break;
                case TabardVendorType.Personal:
                    npcInteraction.InteractionType = PlayerInteractionType.PersonalTabardVendor;
                    break;
                default:
                    Log.outFatal(LogFilter.Server, $"Unsupported tabard vendor type {type}");
                    break;
            }

            npcInteraction.Success = true;
            SendPacket(npcInteraction);
        }

        [WorldPacketHandler(ClientOpcodes.TrainerList, Processing = PacketProcessing.Inplace)]
        void HandleTrainerList(Hello packet)
        {
            Creature npc = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Trainer, NPCFlags2.None);
            if (npc == null)
            {
                Log.outDebug(LogFilter.Network, $"WorldSession.SendTrainerList - {packet.Unit} not found or you can not interact with him.");
                return;
            }

            uint trainerId = npc.GetTrainerId();
            if (trainerId != 0)
                SendTrainerList(npc, trainerId);
            else
                Log.outDebug(LogFilter.Network, $"WorldSession.SendTrainerList - Creature id {npc.GetEntry()} has no trainer data.");
        }

        public void SendTrainerList(Creature npc, uint trainerId)
        {
            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            Trainer trainer = Global.ObjectMgr.GetTrainer(trainerId);
            if (trainer == null)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: SendTrainerList - trainer spells not found for trainer {npc.GetGUID()} id {trainerId}");
                return;
            }

            _player.PlayerTalkClass.GetInteractionData().Reset();
            _player.PlayerTalkClass.GetInteractionData().SourceGuid = npc.GetGUID();
            _player.PlayerTalkClass.GetInteractionData().TrainerId = trainerId;
            trainer.SendSpells(npc, _player, GetSessionDbLocaleIndex());
        }

        [WorldPacketHandler(ClientOpcodes.TrainerBuySpell, Processing = PacketProcessing.Inplace)]
        void HandleTrainerBuySpell(TrainerBuySpell packet)
        {
            Creature npc = _player.GetNPCIfCanInteractWith(packet.TrainerGUID, NPCFlags.Trainer, NPCFlags2.None);
            if (npc == null)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleTrainerBuySpell - {packet.TrainerGUID} not found or you can not interact with him.");
                return;
            }

            // remove fake death
            if (_player.HasUnitState(UnitState.Died))
                _player.RemoveAurasByType(AuraType.FeignDeath);

            if (_player.PlayerTalkClass.GetInteractionData().SourceGuid != packet.TrainerGUID)
                return;

            if (_player.PlayerTalkClass.GetInteractionData().TrainerId != packet.TrainerID)
                return;

            // check present spell in trainer spell list
            Trainer trainer = Global.ObjectMgr.GetTrainer(packet.TrainerID);
            if (trainer == null)
                return;

            trainer.TeachSpell(npc, _player, packet.SpellID);
        }

        void SendTrainerBuyFailed(ObjectGuid trainerGUID, uint spellID, TrainerFailReason trainerFailedReason)
        {
            TrainerBuyFailed trainerBuyFailed = new();
            trainerBuyFailed.TrainerGUID = trainerGUID;
            trainerBuyFailed.SpellID = spellID;                             // should be same as in packet from client
            trainerBuyFailed.TrainerFailedReason = trainerFailedReason;     // 1 == "Not enough money for trainer service." 0 == "Trainer service %d unavailable."
            SendPacket(trainerBuyFailed);
        }

        [WorldPacketHandler(ClientOpcodes.TalkToGossip, Processing = PacketProcessing.Inplace)]
        void HandleGossipHello(Hello packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Gossip, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleGossipHello - {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            // set faction visible if needed
            var factionTemplateEntry = CliDB.FactionTemplateStorage.LookupByKey(unit.GetFaction());
            if (factionTemplateEntry != null)
                GetPlayer().GetReputationMgr().SetVisible(factionTemplateEntry);

            GetPlayer().RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Interacting);

            // Stop the npc if moving
            uint pause = unit.GetMovementTemplate().GetInteractionPauseTimer();
            if (pause != 0)
                unit.PauseMovement(pause);
            unit.SetHomePosition(unit.GetPosition());

            if (unit.IsAreaSpiritHealer())
            {
                _player.SetAreaSpiritHealer(unit);
                _player.SendAreaSpiritHealerTime(unit);
            }

            _player.PlayerTalkClass.ClearMenus();
            if (!unit.GetAI().OnGossipHello(_player))
            {
                GetPlayer().PrepareGossipMenu(unit, _player.GetGossipMenuForSource(unit), true);
                GetPlayer().SendPreparedGossip(unit);
            }
        }

        [WorldPacketHandler(ClientOpcodes.GossipSelectOption)]
        void HandleGossipSelectOption(GossipSelectOption packet)
        {
            GossipMenuItem gossipMenuItem = _player.PlayerTalkClass.GetGossipMenu().GetItem(packet.GossipOptionID);
            if (gossipMenuItem == null)
                return;

            // Prevent cheating on C# scripted menus
            if (GetPlayer().PlayerTalkClass.GetInteractionData().SourceGuid != packet.GossipUnit)
                return;

            Creature unit = null;
            GameObject go = null;
            if (packet.GossipUnit.IsCreatureOrVehicle())
            {
                unit = GetPlayer().GetNPCIfCanInteractWith(packet.GossipUnit, NPCFlags.Gossip, NPCFlags2.None);
                if (unit == null)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - {0} not found or you can't interact with him.", packet.GossipUnit.ToString());
                    return;
                }
            }
            else if (packet.GossipUnit.IsGameObject())
            {
                go = GetPlayer().GetGameObjectIfCanInteractWith(packet.GossipUnit);
                if (go == null)
                {
                    Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - {0} not found or you can't interact with it.", packet.GossipUnit.ToString());
                    return;
                }
            }
            else
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - unsupported {0}.", packet.GossipUnit.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            if ((unit != null && unit.GetScriptId() != unit.LastUsedScriptID) || (go != null && go.GetScriptId() != go.LastUsedScriptID))
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleGossipSelectOption - Script reloaded while in use, ignoring and set new scipt id");
                if (unit != null)
                    unit.LastUsedScriptID = unit.GetScriptId();

                if (go != null)
                    go.LastUsedScriptID = go.GetScriptId();
                GetPlayer().PlayerTalkClass.SendCloseGossip();
                return;
            }
            if (!string.IsNullOrEmpty(packet.PromotionCode))
            {
                if (unit != null)
                {
                    if (!unit.GetAI().OnGossipSelectCode(_player, packet.GossipID, gossipMenuItem.OrderIndex, packet.PromotionCode))
                        GetPlayer().OnGossipSelect(unit, packet.GossipOptionID, packet.GossipID);
                }
                else
                {
                    if (!go.GetAI().OnGossipSelectCode(_player, packet.GossipID, gossipMenuItem.OrderIndex, packet.PromotionCode))
                        _player.OnGossipSelect(go, packet.GossipOptionID, packet.GossipID);
                }
            }
            else
            {
                if (unit != null)
                {
                    if (!unit.GetAI().OnGossipSelect(_player, packet.GossipID, gossipMenuItem.OrderIndex))
                        GetPlayer().OnGossipSelect(unit, packet.GossipOptionID, packet.GossipID);
                }
                else
                {
                    if (!go.GetAI().OnGossipSelect(_player, packet.GossipID, gossipMenuItem.OrderIndex))
                        GetPlayer().OnGossipSelect(go, packet.GossipOptionID, packet.GossipID);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.SpiritHealerActivate)]
        void HandleSpiritHealerActivate(SpiritHealerActivate packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Healer, NPCFlags.SpiritHealer, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleSpiritHealerActivateOpcode - {0} not found or you can not interact with him.", packet.Healer.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendSpiritResurrect();
        }

        void SendSpiritResurrect()
        {
            GetPlayer().ResurrectPlayer(0.5f, true);

            GetPlayer().DurabilityLossAll(0.25f, true);

            // get corpse nearest graveyard
            WorldSafeLocsEntry corpseGrave = null;
            WorldLocation corpseLocation = GetPlayer().GetCorpseLocation();
            if (GetPlayer().HasCorpse())
            {
                corpseGrave = Global.ObjectMgr.GetClosestGraveyard(corpseLocation, GetPlayer().GetTeam(), GetPlayer());
            }

            // now can spawn bones
            GetPlayer().SpawnCorpseBones();

            // teleport to nearest from corpse graveyard, if different from nearest to player ghost
            if (corpseGrave != null)
            {
                WorldSafeLocsEntry ghostGrave = Global.ObjectMgr.GetClosestGraveyard(GetPlayer(), GetPlayer().GetTeam(), GetPlayer());

                if (corpseGrave != ghostGrave)
                    GetPlayer().TeleportTo(corpseGrave.Loc);
            }
        }

        [WorldPacketHandler(ClientOpcodes.BinderActivate, Processing = PacketProcessing.Inplace)]
        void HandleBinderActivate(Hello packet)
        {
            if (!GetPlayer().IsInWorld || !GetPlayer().IsAlive())
                return;

            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Innkeeper, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleBinderActivate - {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendBindPoint(unit);
        }

        void SendBindPoint(Creature npc)
        {
            // prevent set homebind to instances in any case
            if (GetPlayer().GetMap().Instanceable())
                return;

            uint bindspell = 3286;

            // send spell for homebinding (3286)
            npc.CastSpell(GetPlayer(), bindspell, true);

            GetPlayer().PlayerTalkClass.SendCloseGossip();
        }

        [WorldPacketHandler(ClientOpcodes.RequestStabledPets, Processing = PacketProcessing.Inplace)]
        void HandleRequestStabledPets(RequestStabledPets packet)
        {
            if (!CheckStableMaster(packet.StableMaster))
                return;

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            // remove mounts this fix bug where getting pet from stable while mounted deletes pet.
            if (GetPlayer().IsMounted())
                GetPlayer().RemoveAurasByType(AuraType.Mounted);

            _player.SetStableMaster(packet.StableMaster);
        }

        public void SendPetStableResult(StableResult result)
        {
            PetStableResult petStableResult = new();
            petStableResult.Result = result;
            SendPacket(petStableResult);
        }

        [WorldPacketHandler(ClientOpcodes.SetPetSlot)]
        void HandleSetPetSlot(SetPetSlot setPetSlot)
        {
            if (!CheckStableMaster(setPetSlot.StableMaster))
            {
                SendPetStableResult(StableResult.NotStableMaster);
                return;
            }

            if (setPetSlot.DestSlot >= (byte)PetSaveMode.LastStableSlot)
            {
                SendPetStableResult(StableResult.InvalidSlot);
                return;
            }

            _player.SetPetSlot(setPetSlot.PetNumber, (PetSaveMode)setPetSlot.DestSlot);
        }

        [WorldPacketHandler(ClientOpcodes.RepairItem, Processing = PacketProcessing.Inplace)]
        void HandleRepairItem(RepairItem packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.NpcGUID, NPCFlags.Repair, NPCFlags2.None);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleRepairItemOpcode - {0} not found or you can not interact with him.", packet.NpcGUID.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            // reputation discount
            float discountMod = GetPlayer().GetReputationPriceDiscount(unit);

            if (!packet.ItemGUID.IsEmpty())
            {
                Log.outDebug(LogFilter.Network, "ITEM: Repair {0}, at {1}", packet.ItemGUID.ToString(), packet.NpcGUID.ToString());

                Item item = GetPlayer().GetItemByGuid(packet.ItemGUID);
                if (item != null)
                    GetPlayer().DurabilityRepair(item.GetPos(), true, discountMod);
            }
            else
            {
                Log.outDebug(LogFilter.Network, "ITEM: Repair all items at {0}", packet.NpcGUID.ToString());
                GetPlayer().DurabilityRepairAll(true, discountMod, packet.UseGuildBank);
            }
        }

        [WorldPacketHandler(ClientOpcodes.ListInventory, Processing = PacketProcessing.Inplace)]
        void HandleListInventory(Hello packet)
        {
            if (!GetPlayer().IsAlive())
                return;

            SendListInventory(packet.Unit);
        }

        public void SendListInventory(ObjectGuid vendorGuid)
        {
            Creature vendor = GetPlayer().GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor, NPCFlags2.None);
            if (vendor == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: SendListInventory - {0} not found or you can not interact with him.", vendorGuid.ToString());
                GetPlayer().SendSellError(SellResult.CantFindVendor, null, ObjectGuid.Empty);
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            // Stop the npc if moving
            uint pause = vendor.GetMovementTemplate().GetInteractionPauseTimer();
            if (pause != 0)
                vendor.PauseMovement(pause);
            vendor.SetHomePosition(vendor.GetPosition());

            VendorItemData vendorItems = vendor.GetVendorItems();
            int rawItemCount = vendorItems != null ? vendorItems.GetItemCount() : 0;

            VendorInventory packet = new();
            packet.Vendor = vendor.GetGUID();

            float discountMod = GetPlayer().GetReputationPriceDiscount(vendor);
            byte count = 0;
            for (uint slot = 0; slot < rawItemCount; ++slot)
            {
                VendorItem vendorItem = vendorItems.GetItem(slot);
                if (vendorItem == null)
                    continue;

                VendorItemPkt item = new();

                if (!ConditionManager.IsPlayerMeetingCondition(_player, vendorItem.PlayerConditionId))
                    item.PlayerConditionFailed = (int)vendorItem.PlayerConditionId;

                if (vendorItem.Type == ItemVendorType.Item)
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(vendorItem.item);
                    if (itemTemplate == null)
                        continue;

                    int leftInStock = vendorItem.maxcount == 0 ? -1 : (int)vendor.GetVendorItemCurrentCount(vendorItem);
                    if (!GetPlayer().IsGameMaster())
                    {
                        if (!Convert.ToBoolean(itemTemplate.GetAllowableClass() & GetPlayer().GetClassMask()) && itemTemplate.GetBonding() == ItemBondingType.OnAcquire)
                            continue;

                        if ((itemTemplate.HasFlag(ItemFlags2.FactionHorde) && GetPlayer().GetTeam() == Team.Alliance) ||
                            (itemTemplate.HasFlag(ItemFlags2.FactionAlliance) && GetPlayer().GetTeam() == Team.Horde))
                            continue;

                        if (leftInStock == 0)
                            continue;
                    }

                    if (!Global.ConditionMgr.IsObjectMeetingVendorItemConditions(vendor.GetEntry(), vendorItem.item, _player, vendor))
                    {
                        Log.outDebug(LogFilter.Condition, "SendListInventory: conditions not met for creature entry {0} item {1}", vendor.GetEntry(), vendorItem.item);
                        continue;
                    }

                    ulong price = (ulong)Math.Floor(itemTemplate.GetBuyPrice() * discountMod);
                    price = itemTemplate.GetBuyPrice() > 0 ? Math.Max(1ul, price) : price;

                    int priceMod = GetPlayer().GetTotalAuraModifier(AuraType.ModVendorItemsPrices);
                    if (priceMod != 0)
                        price -= MathFunctions.CalculatePct(price, priceMod);

                    item.MuID = (int)slot + 1;
                    item.ExtendedCostID = (int)vendorItem.ExtendedCost;
                    item.Type = (int)vendorItem.Type;
                    item.Quantity = leftInStock;
                    item.StackCount = (int)itemTemplate.GetBuyCount();
                    item.Price = (ulong)price;
                    item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;
                    item.Refundable = itemTemplate.HasFlag(ItemFlags.ItemPurchaseRecord) && vendorItem.ExtendedCost != 0 && itemTemplate.GetMaxStackSize() == 1;

                    item.Item.ItemID = vendorItem.item;
                    if (!vendorItem.BonusListIDs.Empty())
                    {
                        item.Item.ItemBonus = new();
                        item.Item.ItemBonus.BonusListIDs = vendorItem.BonusListIDs;
                    }

                    packet.Items.Add(item);
                }
                else if (vendorItem.Type == ItemVendorType.Currency)
                {
                    CurrencyTypesRecord currencyTemplate = CliDB.CurrencyTypesStorage.LookupByKey(vendorItem.item);
                    if (currencyTemplate == null)
                        continue;

                    if (vendorItem.ExtendedCost == 0)
                        continue; // there's no price defined for currencies, only extendedcost is used

                    item.MuID = (int)slot + 1; // client expects counting to start at 1
                    item.ExtendedCostID = (int)vendorItem.ExtendedCost;
                    item.Item.ItemID = vendorItem.item;
                    item.Type = (int)vendorItem.Type;
                    item.StackCount = (int)vendorItem.maxcount;
                    item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;

                    packet.Items.Add(item);
                }
                else
                    continue;

                if (++count >= SharedConst.MaxVendorItems)
                    break;
            }

            packet.Reason = (byte)(count != 0 ? VendorInventoryReason.None : VendorInventoryReason.Empty);

            SendPacket(packet);
        }
    }
}
