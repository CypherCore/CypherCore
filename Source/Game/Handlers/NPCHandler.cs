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
using Game.BattleGrounds;
using Game.DataStorage;
using Game.Entities;
using Game.Network;
using Game.Network.Packets;
using System;
using System.Collections.Generic;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.TabardVendorActivate)]
        void HandleTabardVendorActivate(Hello packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.TabardDesigner);
            if (!unit)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleTabardVendorActivateOpcode - {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            // remove fake death
            if (GetPlayer().HasUnitState(UnitState.Died))
                GetPlayer().RemoveAurasByType(AuraType.FeignDeath);

            SendTabardVendorActivate(packet.Unit);
        }

        public void SendTabardVendorActivate(ObjectGuid guid)
        {
            PlayerTabardVendorActivate packet = new PlayerTabardVendorActivate();
            packet.Vendor = guid;
            SendPacket(packet);
        }

        [WorldPacketHandler(ClientOpcodes.TrainerList)]
        void HandleTrainerList(Hello packet)
        {
            Creature npc = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Trainer);
            if (!npc)
            {
                Log.outDebug(LogFilter.Network, $"WorldSession.SendTrainerList - {packet.Unit.ToString()} not found or you can not interact with him.");
                return;
            }

            uint trainerId = Global.ObjectMgr.GetCreatureDefaultTrainer(npc.GetEntry());
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
                Log.outDebug(LogFilter.Network, $"WORLD: SendTrainerList - trainer spells not found for trainer {npc.GetGUID().ToString()} id {trainerId}");
                return;
            }

            _player.PlayerTalkClass.GetInteractionData().Reset();
            _player.PlayerTalkClass.GetInteractionData().SourceGuid = npc.GetGUID();
            _player.PlayerTalkClass.GetInteractionData().TrainerId = trainerId;
            trainer.SendSpells(npc, _player, GetSessionDbLocaleIndex());
        }

        [WorldPacketHandler(ClientOpcodes.TrainerBuySpell)]
        void HandleTrainerBuySpell(TrainerBuySpell packet)
        {
            Creature npc = _player.GetNPCIfCanInteractWith(packet.TrainerGUID, NPCFlags.Trainer);
            if (npc == null)
            {
                Log.outDebug(LogFilter.Network, $"WORLD: HandleTrainerBuySpell - {packet.TrainerGUID.ToString()} not found or you can not interact with him.");
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
            TrainerBuyFailed trainerBuyFailed = new TrainerBuyFailed();
            trainerBuyFailed.TrainerGUID = trainerGUID;
            trainerBuyFailed.SpellID = spellID;                             // should be same as in packet from client
            trainerBuyFailed.TrainerFailedReason = trainerFailedReason;     // 1 == "Not enough money for trainer service." 0 == "Trainer service %d unavailable."
            SendPacket(trainerBuyFailed);
        }

        [WorldPacketHandler(ClientOpcodes.TalkToGossip)]
        void HandleGossipHello(Hello packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Gossip);
            if (unit == null)
            {
                Log.outDebug(LogFilter.Network, "WORLD: HandleGossipHello - {0} not found or you can not interact with him.", packet.Unit.ToString());
                return;
            }

            // set faction visible if needed
            var factionTemplateEntry = CliDB.FactionTemplateStorage.LookupByKey(unit.getFaction());
            if (factionTemplateEntry != null)
                GetPlayer().GetReputationMgr().SetVisible(factionTemplateEntry);

            GetPlayer().RemoveAurasWithInterruptFlags(SpellAuraInterruptFlags.Talk);

            if (unit.IsArmorer() || unit.IsCivilian() || unit.IsQuestGiver() || unit.IsServiceProvider() || unit.IsGuard())
                unit.StopMoving();

            // If spiritguide, no need for gossip menu, just put player into resurrect queue
            if (unit.IsSpiritGuide())
            {
                Battleground bg = GetPlayer().GetBattleground();
                if (bg)
                {
                    bg.AddPlayerToResurrectQueue(unit.GetGUID(), GetPlayer().GetGUID());
                    Global.BattlegroundMgr.SendAreaSpiritHealerQuery(GetPlayer(), bg, unit.GetGUID());
                    return;
                }
            }

            if (!Global.ScriptMgr.OnGossipHello(GetPlayer(), unit))
            {
                GetPlayer().PrepareGossipMenu(unit, unit.GetCreatureTemplate().GossipMenuId, true);
                GetPlayer().SendPreparedGossip(unit);
            }
            unit.GetAI().sGossipHello(GetPlayer());
        }

        [WorldPacketHandler(ClientOpcodes.GossipSelectOption)]
        void HandleGossipSelectOption(GossipSelectOption packet)
        {
            if (GetPlayer().PlayerTalkClass.GetGossipMenu().GetItem(packet.GossipIndex) == null)
                return;

            // Prevent cheating on C# scripted menus
            if (GetPlayer().PlayerTalkClass.GetInteractionData().SourceGuid != packet.GossipUnit)
                return;

            Creature unit = null;
            GameObject go = null;
            if (packet.GossipUnit.IsCreatureOrVehicle())
            {
                unit = GetPlayer().GetNPCIfCanInteractWith(packet.GossipUnit, NPCFlags.Gossip);
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

            if ((unit && unit.GetScriptId() != unit.LastUsedScriptID) || (go != null && go.GetScriptId() != go.LastUsedScriptID))
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
                if (unit)
                {
                    unit.GetAI().sGossipSelectCode(GetPlayer(), packet.GossipID, packet.GossipIndex, packet.PromotionCode);
                    if (!Global.ScriptMgr.OnGossipSelectCode(GetPlayer(), unit, GetPlayer().PlayerTalkClass.GetGossipOptionSender(packet.GossipIndex), GetPlayer().PlayerTalkClass.GetGossipOptionAction(packet.GossipIndex), packet.PromotionCode))
                        GetPlayer().OnGossipSelect(unit, packet.GossipIndex, packet.GossipID);
                }
                else
                {
                    go.GetAI().GossipSelectCode(GetPlayer(), packet.GossipID, packet.GossipIndex, packet.PromotionCode);
                    Global.ScriptMgr.OnGossipSelectCode(GetPlayer(), go, GetPlayer().PlayerTalkClass.GetGossipOptionSender(packet.GossipIndex), GetPlayer().PlayerTalkClass.GetGossipOptionAction(packet.GossipIndex), packet.PromotionCode);
                }
            }
            else
            {
                if (unit != null)
                {
                    unit.GetAI().sGossipSelect(GetPlayer(), packet.GossipID, packet.GossipIndex);
                    if (!Global.ScriptMgr.OnGossipSelect(GetPlayer(), unit, GetPlayer().PlayerTalkClass.GetGossipOptionSender(packet.GossipIndex), GetPlayer().PlayerTalkClass.GetGossipOptionAction(packet.GossipIndex)))
                        GetPlayer().OnGossipSelect(unit, packet.GossipIndex, packet.GossipID);
                }
                else
                {
                    go.GetAI().GossipSelect(GetPlayer(), packet.GossipID, packet.GossipIndex);
                    if (!Global.ScriptMgr.OnGossipSelect(GetPlayer(), go, GetPlayer().PlayerTalkClass.GetGossipOptionSender(packet.GossipIndex), GetPlayer().PlayerTalkClass.GetGossipOptionAction(packet.GossipIndex)))
                        GetPlayer().OnGossipSelect(go, packet.GossipIndex, packet.GossipID);
                }
            }
        }

        [WorldPacketHandler(ClientOpcodes.SpiritHealerActivate)]
        void HandleSpiritHealerActivate(SpiritHealerActivate packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Healer, NPCFlags.SpiritHealer);
            if (!unit)
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
            WorldSafeLocsRecord corpseGrave = null;
            WorldLocation corpseLocation = GetPlayer().GetCorpseLocation();
            if (GetPlayer().HasCorpse())
            {
                corpseGrave = Global.ObjectMgr.GetClosestGraveYard(corpseLocation, GetPlayer().GetTeam(), GetPlayer());
            }

            // now can spawn bones
            GetPlayer().SpawnCorpseBones();

            // teleport to nearest from corpse graveyard, if different from nearest to player ghost
            if (corpseGrave != null)
            {
                WorldSafeLocsRecord ghostGrave = Global.ObjectMgr.GetClosestGraveYard(GetPlayer(), GetPlayer().GetTeam(), GetPlayer());

                if (corpseGrave != ghostGrave)
                    GetPlayer().TeleportTo(corpseGrave.MapID, corpseGrave.Loc.X, corpseGrave.Loc.Y, corpseGrave.Loc.Z, GetPlayer().GetOrientation());
                // or update at original position
                else
                    GetPlayer().UpdateObjectVisibility();
            }
            // or update at original position
            else
                GetPlayer().UpdateObjectVisibility();
        }

        [WorldPacketHandler(ClientOpcodes.BinderActivate)]
        void HandleBinderActivate(Hello packet)
        {
            if (!GetPlayer().IsInWorld || !GetPlayer().IsAlive())
                return;

            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.Unit, NPCFlags.Innkeeper);
            if (!unit)
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

        [WorldPacketHandler(ClientOpcodes.RequestStabledPets)]
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

            SendStablePet(packet.StableMaster);
        }

        public void SendStablePet(ObjectGuid guid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_PET_SLOTS_DETAIL);
            stmt.AddValue(0, guid.GetCounter());
            stmt.AddValue(1, PetSaveMode.FirstStableSlot);
            stmt.AddValue(2, PetSaveMode.LastStableSlot);

            _queryProcessor.AddQuery(DB.Characters.AsyncQuery(stmt).WithCallback(SendStablePetCallback, guid));
        }

        void SendStablePetCallback(ObjectGuid guid, SQLResult result)
        {
            if (!GetPlayer())
                return;

            PetStableList packet = new PetStableList();
            packet.StableMaster = guid;

            Pet pet = GetPlayer().GetPet();

            uint petSlot = 0;
            // not let move dead pet in slot
            if (pet && pet.IsAlive() && pet.getPetType() == PetType.Hunter)
            {
                PetStableInfo stableEntry;// = new PetStableInfo();
                stableEntry.PetSlot = petSlot;
                stableEntry.PetNumber = pet.GetCharmInfo().GetPetNumber();
                stableEntry.CreatureID = pet.GetEntry();
                stableEntry.DisplayID = pet.GetDisplayId();
                stableEntry.ExperienceLevel = pet.getLevel();
                stableEntry.PetFlags = PetStableinfo.Active;
                stableEntry.PetName = pet.GetName();
                ++petSlot;

                packet.Pets.Add(stableEntry);
            }

            if (!result.IsEmpty())
            {
                do
                {
                    PetStableInfo stableEntry;// = new PetStableInfo();

                    stableEntry.PetSlot = petSlot;
                    stableEntry.PetNumber = result.Read<uint>(1);          // petnumber
                    stableEntry.CreatureID = result.Read<uint>(2);         // creature entry
                    stableEntry.DisplayID = result.Read<uint>(5);          // creature displayid
                    stableEntry.ExperienceLevel = result.Read<ushort>(3);    // level
                    stableEntry.PetFlags = PetStableinfo.Inactive;
                    stableEntry.PetName = result.Read<string>(4);            // Name

                    ++petSlot;
                    packet.Pets.Add(stableEntry);
                }
                while (result.NextRow());
            }

            SendPacket(packet);
        }

        void SendPetStableResult(byte res)
        {
            SendPacket(new PetStableResult(res));
        }

        [WorldPacketHandler(ClientOpcodes.RepairItem)]
        void HandleRepairItem(RepairItem packet)
        {
            Creature unit = GetPlayer().GetNPCIfCanInteractWith(packet.NpcGUID, NPCFlags.Repair);
            if (!unit)
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
                if (item)
                    GetPlayer().DurabilityRepair(item.GetPos(), true, discountMod, packet.UseGuildBank);
            }
            else
            {
                Log.outDebug(LogFilter.Network, "ITEM: Repair all items at {0}", packet.NpcGUID.ToString());
                GetPlayer().DurabilityRepairAll(true, discountMod, packet.UseGuildBank);
            }
        }

        [WorldPacketHandler(ClientOpcodes.ListInventory)]
        void HandleListInventory(Hello packet)
        {
            if (!GetPlayer().IsAlive())
                return;

            SendListInventory(packet.Unit);
        }

        public void SendListInventory(ObjectGuid vendorGuid)
        {
            Creature vendor = GetPlayer().GetNPCIfCanInteractWith(vendorGuid, NPCFlags.Vendor);
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
            if (vendor.HasUnitState(UnitState.Moving))
                vendor.StopMoving();

            VendorItemData vendorItems = vendor.GetVendorItems();
            int rawItemCount = vendorItems != null ? vendorItems.GetItemCount() : 0;

            VendorInventory packet = new VendorInventory();
            packet.Vendor = vendor.GetGUID();

            float discountMod = GetPlayer().GetReputationPriceDiscount(vendor);
            byte count = 0;
            for (uint slot = 0; slot < rawItemCount; ++slot)
            {
                VendorItem vendorItem = vendorItems.GetItem(slot);
                if (vendorItem == null)
                    continue;

                VendorItemPkt item = new VendorItemPkt();

                PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(vendorItem.PlayerConditionId);
                if (playerCondition != null)
                    if (!ConditionManager.IsPlayerMeetingCondition(_player, playerCondition))
                        item.PlayerConditionFailed = (int)playerCondition.Id;

                if (vendorItem.Type == ItemVendorType.Item)
                {
                    ItemTemplate itemTemplate = Global.ObjectMgr.GetItemTemplate(vendorItem.item);
                    if (itemTemplate == null)
                        continue;

                    int leftInStock = vendorItem.maxcount == 0 ? -1 : (int)vendor.GetVendorItemCurrentCount(vendorItem);
                    if (!GetPlayer().IsGameMaster())
                    {
                        if (!Convert.ToBoolean(itemTemplate.GetAllowableClass() & GetPlayer().getClassMask()) && itemTemplate.GetBonding() == ItemBondingType.OnAcquire)
                            continue;

                        if ((itemTemplate.GetFlags2().HasAnyFlag(ItemFlags2.FactionHorde) && GetPlayer().GetTeam() == Team.Alliance) ||
                            (itemTemplate.GetFlags2().HasAnyFlag(ItemFlags2.FactionAlliance) && GetPlayer().GetTeam() == Team.Horde))
                            continue;

                        if (leftInStock == 0)
                            continue;
                    }

                    if (!Global.ConditionMgr.IsObjectMeetingVendorItemConditions(vendor.GetEntry(), vendorItem.item, _player, vendor))
                    {
                        Log.outDebug(LogFilter.Condition, "SendListInventory: conditions not met for creature entry {0} item {1}", vendor.GetEntry(), vendorItem.item);
                        continue;
                    }

                    int price = (int)(vendorItem.IsGoldRequired(itemTemplate) ? Math.Floor(itemTemplate.GetBuyPrice() * discountMod) : 0);

                    int priceMod = GetPlayer().GetTotalAuraModifier(AuraType.ModVendorItemsPrices);
                    if (priceMod != 0)
                        price -= MathFunctions.CalculatePct(price, priceMod);

                    item.MuID = (int)slot + 1;
                    item.Durability = (int)itemTemplate.MaxDurability;
                    item.ExtendedCostID = (int)vendorItem.ExtendedCost;
                    item.Type = (int)vendorItem.Type;
                    item.Quantity = leftInStock;
                    item.StackCount = (int)itemTemplate.GetBuyCount();
                    item.Price = (ulong)price;
                    item.DoNotFilterOnVendor = vendorItem.IgnoreFiltering;

                    item.Item.ItemID = vendorItem.item;
                    if (!vendorItem.BonusListIDs.Empty())
                    {
                        item.Item.ItemBonus.HasValue = true;
                        item.Item.ItemBonus.Value.BonusListIDs = vendorItem.BonusListIDs;
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

            SendPacket(packet);
        }
    }
}
