﻿/*
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
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.UseItem)]
        private void HandleUseItem(UseItem packet)
        {
            var user = GetPlayer();

            // ignore for remote control state
            if (user.m_unitMovedByMe != user)
                return;

            Item item = user.GetUseableItemByPos(packet.PackSlot, packet.Slot);
            if (item == null)
            {
                user.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }

            if (item.GetGUID() != packet.CastItem)
            {
                user.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }

            var proto = item.GetTemplate();
            if (proto == null)
            {
                user.SendEquipError(InventoryResult.ItemNotFound, item);
                return;
            }

            // some item classes can be used only in equipped state
            if (proto.GetInventoryType() != InventoryType.NonEquip && !item.IsEquipped())
            {
                user.SendEquipError(InventoryResult.ItemNotFound, item);
                return;
            }

            InventoryResult msg = user.CanUseItem(item);
            if (msg != InventoryResult.Ok)
            {
                user.SendEquipError(msg, item);
                return;
            }

            // only allow conjured consumable, bandage, poisons (all should have the 2^21 item flag set in DB)
            if (proto.GetClass() == ItemClass.Consumable && !proto.GetFlags().HasAnyFlag(ItemFlags.IgnoreDefaultArenaRestrictions) && user.InArena())
            {
                user.SendEquipError(InventoryResult.NotDuringArenaMatch, item);
                return;
            }

            // don't allow items banned in arena
            if (proto.GetFlags().HasAnyFlag(ItemFlags.NotUseableInArena) && user.InArena())
            {
                user.SendEquipError(InventoryResult.NotDuringArenaMatch, item);
                return;
            }

            if (user.IsInCombat())
            {
                foreach (var effect in item.GetEffects())
                {
                    var spellInfo = Global.SpellMgr.GetSpellInfo((uint)effect.SpellID, user.GetMap().GetDifficultyID());
                    if (spellInfo != null)
                    {
                        if (!spellInfo.CanBeUsedInCombat())
                        {
                            user.SendEquipError(InventoryResult.NotInCombat, item);
                            return;
                        }
                    }
                }
            }

            // check also  BIND_WHEN_PICKED_UP and BIND_QUEST_ITEM for .additem or .additemset case by GM (not binded at adding to inventory)
            if (item.GetBonding() == ItemBondingType.OnUse || item.GetBonding() == ItemBondingType.OnAcquire || item.GetBonding() == ItemBondingType.Quest)
            {
                if (!item.IsSoulBound())
                {
                    item.SetState(ItemUpdateState.Changed, user);
                    item.SetBinding(true);
                    GetCollectionMgr().AddItemAppearance(item);
                }
            }

            var targets = new SpellCastTargets(user, packet.Cast);

            // Note: If script stop casting it must send appropriate data to client to prevent stuck item in gray state.
            if (!Global.ScriptMgr.OnItemUse(user, item, targets, packet.Cast.CastID))
            {
                // no script or script not process request by self
                user.CastItemUseSpell(item, targets, packet.Cast.CastID, packet.Cast.Misc);
            }
        }

        [WorldPacketHandler(ClientOpcodes.OpenItem)]
        private void HandleOpenItem(OpenItem packet)
        {
            var player = GetPlayer();

            // ignore for remote control state
            if (player.m_unitMovedByMe != player)
                return;

            // additional check, client outputs message on its own
            if (!player.IsAlive())
            {
                player.SendEquipError(InventoryResult.PlayerDead);
                return;
            }

            Item item = player.GetItemByPos(packet.Slot, packet.PackSlot);
            if (!item)
            {
                player.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }

            var proto = item.GetTemplate();
            if (proto == null)
            {
                player.SendEquipError(InventoryResult.ItemNotFound, item);
                return;
            }

            // Verify that the bag is an actual bag or wrapped item that can be used "normally"
            if (!proto.GetFlags().HasAnyFlag(ItemFlags.HasLoot) && !item.HasItemFlag(ItemFieldFlags.Wrapped))
            {
                player.SendEquipError(InventoryResult.ClientLockedOut, item);
                Log.outError(LogFilter.Network, "Possible hacking attempt: Player {0} [guid: {1}] tried to open item [guid: {2}, entry: {3}] which is not openable!",
                        player.GetName(), player.GetGUID().ToString(), item.GetGUID().ToString(), proto.GetId());
                return;
            }

            // locked item
            var lockId = proto.GetLockID();
            if (lockId != 0)
            {
                var lockInfo = CliDB.LockStorage.LookupByKey(lockId);
                if (lockInfo == null)
                {
                    player.SendEquipError(InventoryResult.ItemLocked, item);
                    Log.outError(LogFilter.Network, "WORLD:OpenItem: item [guid = {0}] has an unknown lockId: {1}!", item.GetGUID().ToString(), lockId);
                    return;
                }

                // was not unlocked yet
                if (item.IsLocked())
                {
                    player.SendEquipError(InventoryResult.ItemLocked, item);
                    return;
                }
            }

            if (item.HasItemFlag(ItemFieldFlags.Wrapped))// wrapped?
            {
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GIFT_BY_ITEM);
                stmt.AddValue(0, item.GetGUID().GetCounter());
                _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt)
                    .WithCallback(result => HandleOpenWrappedItemCallback(item.GetPos(), item.GetGUID(), result)));
            }
            else
                player.SendLoot(item.GetGUID(), LootType.Corpse);
        }

        private void HandleOpenWrappedItemCallback(ushort pos, ObjectGuid itemGuid, SQLResult result)
        {
            if (!GetPlayer())
                return;

            Item item = GetPlayer().GetItemByPos(pos);
            if (!item)
                return;

            if (item.GetGUID() != itemGuid || !item.HasItemFlag(ItemFieldFlags.Wrapped)) // during getting result, gift was swapped with another item
                return;

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Network, $"Wrapped item {item.GetGUID()} don't have record in character_gifts table and will deleted");
                GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
                return;
            }

            var trans = new SQLTransaction();

            var entry = result.Read<uint>(0);
            var flags = result.Read<uint>(1);

            item.SetGiftCreator(ObjectGuid.Empty);
            item.SetEntry(entry);
            item.SetItemFlags((ItemFieldFlags)flags);
            item.SetMaxDurability(item.GetTemplate().MaxDurability);
            item.SetState(ItemUpdateState.Changed, GetPlayer());

            GetPlayer().SaveInventoryAndGoldToDB(trans);

            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
            stmt.AddValue(0, itemGuid.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        [WorldPacketHandler(ClientOpcodes.GameObjUse)]
        private void HandleGameObjectUse(GameObjUse packet)
        {
            var obj = GetPlayer().GetGameObjectIfCanInteractWith(packet.Guid);
            if (obj)
            {
                // ignore for remote control state
                if (GetPlayer().m_unitMovedByMe != GetPlayer())
                    if (!(GetPlayer().IsOnVehicle(GetPlayer().m_unitMovedByMe) || GetPlayer().IsMounted()) && !obj.GetGoInfo().IsUsableMounted())
                        return;

                obj.Use(GetPlayer());
            }
        }

        [WorldPacketHandler(ClientOpcodes.GameObjReportUse)]
        private void HandleGameobjectReportUse(GameObjReportUse packet)
        {
            // ignore for remote control state
            if (GetPlayer().m_unitMovedByMe != GetPlayer())
                return;

            var go = GetPlayer().GetGameObjectIfCanInteractWith(packet.Guid);
            if (go)
            {
                if (go.GetAI().GossipHello(GetPlayer()))
                    return;

                GetPlayer().UpdateCriteria(CriteriaTypes.UseGameobject, go.GetEntry());
            }
        }

        [WorldPacketHandler(ClientOpcodes.CastSpell, Processing = PacketProcessing.ThreadSafe)]
        private void HandleCastSpell(CastSpell cast)
        {
            // ignore for remote control state (for player case)
            var mover = GetPlayer().m_unitMovedByMe;
            if (mover != GetPlayer() && mover.IsTypeId(TypeId.Player))
                return;

            var spellInfo = Global.SpellMgr.GetSpellInfo(cast.Cast.SpellID, mover.GetMap().GetDifficultyID());
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "WORLD: unknown spell id {0}", cast.Cast.SpellID);
                return;
            }

            if (spellInfo.IsPassive())
                return;

            var caster = mover;
            if (caster.IsTypeId(TypeId.Unit) && !caster.ToCreature().HasSpell(spellInfo.Id))
            {
                // If the vehicle creature does not have the spell but it allows the passenger to cast own spells
                // change caster to player and let him cast
                if (!GetPlayer().IsOnVehicle(caster) || spellInfo.CheckVehicle(GetPlayer()) != SpellCastResult.SpellCastOk)
                    return;

                caster = GetPlayer();
            }

            // check known spell or raid marker spell (which not requires player to know it)
            if (caster.IsTypeId(TypeId.Player) && !caster.ToPlayer().HasActiveSpell(spellInfo.Id) && !spellInfo.HasEffect(SpellEffectName.ChangeRaidMarker) && !spellInfo.HasAttribute(SpellAttr8.RaidMarker))
                return;

            // Check possible spell cast overrides
            spellInfo = caster.GetCastSpellInfo(spellInfo);

            // Client is resending autoshot cast opcode when other spell is casted during shoot rotation
            // Skip it to prevent "interrupt" message
            if (spellInfo.IsAutoRepeatRangedSpell() && GetPlayer().GetCurrentSpell(CurrentSpellTypes.AutoRepeat) != null
                && GetPlayer().GetCurrentSpell(CurrentSpellTypes.AutoRepeat).m_spellInfo == spellInfo)
                return;

            // can't use our own spells when we're in possession of another unit,
            if (GetPlayer().IsPossessing())
                return;

            // client provided targets
            var targets = new SpellCastTargets(caster, cast.Cast);

            // auto-selection buff level base at target level (in spellInfo)
            if (targets.GetUnitTarget() != null)
            {
                var actualSpellInfo = spellInfo.GetAuraRankForLevel(targets.GetUnitTarget().GetLevelForTarget(caster));

                // if rank not found then function return NULL but in explicit cast case original spell can be casted and later failed with appropriate error message
                if (actualSpellInfo != null)
                    spellInfo = actualSpellInfo;
            }

            if (cast.Cast.MoveUpdate.HasValue)
                HandleMovementOpcode(ClientOpcodes.MoveStop, cast.Cast.MoveUpdate.Value);

            var spell = new Spell(caster, spellInfo, TriggerCastFlags.None, ObjectGuid.Empty, false);

            var spellPrepare = new SpellPrepare();
            spellPrepare.ClientCastID = cast.Cast.CastID;
            spellPrepare.ServerCastID = spell.m_castId;
            SendPacket(spellPrepare);

            spell.m_fromClient = true;
            spell.m_misc.Data0 = cast.Cast.Misc[0];
            spell.m_misc.Data1 = cast.Cast.Misc[1];
            spell.Prepare(targets);
        }

        [WorldPacketHandler(ClientOpcodes.CancelCast, Processing = PacketProcessing.ThreadSafe)]
        private void HandleCancelCast(CancelCast packet)
        {
            if (GetPlayer().IsNonMeleeSpellCast(false))
                GetPlayer().InterruptNonMeleeSpells(false, packet.SpellID, false);
        }

        [WorldPacketHandler(ClientOpcodes.CancelAura)]
        private void HandleCancelAura(CancelAura cancelAura)
        {
            var spellInfo = Global.SpellMgr.GetSpellInfo(cancelAura.SpellID, _player.GetMap().GetDifficultyID());
            if (spellInfo == null)
                return;

            // not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
            if (spellInfo.HasAttribute(SpellAttr0.CantCancel))
                return;

            // channeled spell case (it currently casted then)
            if (spellInfo.IsChanneled())
            {
                var curSpell = GetPlayer().GetCurrentSpell(CurrentSpellTypes.Channeled);
                if (curSpell != null)
                    if (curSpell.GetSpellInfo().Id == cancelAura.SpellID)
                        GetPlayer().InterruptSpell(CurrentSpellTypes.Channeled);
                return;
            }

            // non channeled case:
            // don't allow remove non positive spells
            // don't allow cancelling passive auras (some of them are visible)
            if (!spellInfo.IsPositive() || spellInfo.IsPassive())
                return;

            GetPlayer().RemoveOwnedAura(cancelAura.SpellID, cancelAura.CasterGUID, 0, AuraRemoveMode.Cancel);
        }

        [WorldPacketHandler(ClientOpcodes.CancelGrowthAura)]
        private void HandleCancelGrowthAura(CancelGrowthAura cancelGrowthAura)
        {
            GetPlayer().RemoveAurasByType(AuraType.ModScale, aurApp =>
            {
                var spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.CantCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.CancelMountAura)]
        private void HandleCancelMountAura(CancelMountAura packet)
        {
            GetPlayer().RemoveAurasByType(AuraType.Mounted, aurApp =>
            {
                var spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.CantCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.PetCancelAura)]
        private void HandlePetCancelAura(PetCancelAura packet)
        {
            if (!Global.SpellMgr.HasSpellInfo(packet.SpellID, Difficulty.None))
            {
                Log.outError(LogFilter.Network, "WORLD: unknown PET spell id {0}", packet.SpellID);
                return;
            }

            Creature pet = ObjectAccessor.GetCreatureOrPetOrVehicle(_player, packet.PetGUID);
            if (pet == null)
            {
                Log.outError(LogFilter.Network, "HandlePetCancelAura: Attempt to cancel an aura for non-existant {0} by player '{1}'", packet.PetGUID.ToString(), GetPlayer().GetName());
                return;
            }

            if (pet != GetPlayer().GetGuardianPet() && pet != GetPlayer().GetCharm())
            {
                Log.outError(LogFilter.Network, "HandlePetCancelAura: {0} is not a pet of player '{1}'", packet.PetGUID.ToString(), GetPlayer().GetName());
                return;
            }

            if (!pet.IsAlive())
            {
                pet.SendPetActionFeedback(PetActionFeedback.Dead, 0);
                return;
            }

            pet.RemoveOwnedAura(packet.SpellID, ObjectGuid.Empty, 0, AuraRemoveMode.Cancel);
        }

        [WorldPacketHandler(ClientOpcodes.CancelAutoRepeatSpell)]
        private void HandleCancelAutoRepeatSpell(CancelAutoRepeatSpell packet)
        {
            //may be better send SMSG_CANCEL_AUTO_REPEAT?
            //cancel and prepare for deleting
            _player.InterruptSpell(CurrentSpellTypes.AutoRepeat);
        }

        [WorldPacketHandler(ClientOpcodes.CancelChannelling)]
        private void HandleCancelChanneling(CancelChannelling cancelChanneling)
        {
            // ignore for remote control state (for player case)
            var mover = _player.m_unitMovedByMe;
            if (mover != _player && mover.IsTypeId(TypeId.Player))
                return;

            mover.InterruptSpell(CurrentSpellTypes.Channeled);
        }

        [WorldPacketHandler(ClientOpcodes.TotemDestroyed)]
        private void HandleTotemDestroyed(TotemDestroyed totemDestroyed)
        {
            // ignore for remote control state
            if (GetPlayer().m_unitMovedByMe != GetPlayer())
                return;

            var slotId = totemDestroyed.Slot;
            slotId += (int)SummonSlot.Totem;

            if (slotId >= SharedConst.MaxTotemSlot)
                return;

            if (GetPlayer().m_SummonSlot[slotId].IsEmpty())
                return;

            var totem = ObjectAccessor.GetCreature(GetPlayer(), _player.m_SummonSlot[slotId]);
            if (totem != null && totem.IsTotem())// && totem.GetGUID() == packet.TotemGUID)  Unknown why blizz doesnt send the guid when you right click it.
                totem.ToTotem().UnSummon();
        }

        [WorldPacketHandler(ClientOpcodes.SelfRes)]
        private void HandleSelfRes(SelfRes selfRes)
        {
            if (_player.HasAuraType(AuraType.PreventResurrection))
                return; // silent return, client should display error by itself and not send this opcode

            List<uint> selfResSpells = _player.m_activePlayerData.SelfResSpells;
            if (!selfResSpells.Contains(selfRes.SpellId))
                return;

            var spellInfo = Global.SpellMgr.GetSpellInfo(selfRes.SpellId, _player.GetMap().GetDifficultyID());
            if (spellInfo != null)
                _player.CastSpell(_player, spellInfo, false, null);

            _player.RemoveSelfResSpell(selfRes.SpellId);
        }

        [WorldPacketHandler(ClientOpcodes.SpellClick)]
        private void HandleSpellClick(SpellClick packet)
        {
            // this will get something not in world. crash
            Creature unit = ObjectAccessor.GetCreatureOrPetOrVehicle(GetPlayer(), packet.SpellClickUnitGuid);
            if (unit == null)
                return;

            // @todo Unit.SetCharmedBy: 28782 is not in world but 0 is trying to charm it! . crash
            if (!unit.IsInWorld)
                return;

            unit.HandleSpellClick(GetPlayer());
        }

        [WorldPacketHandler(ClientOpcodes.GetMirrorImageData)]
        private void HandleMirrorImageDataRequest(GetMirrorImageData packet)
        {
            var guid = packet.UnitGUID;

            // Get unit for which data is needed by client
            var unit = Global.ObjAccessor.GetUnit(GetPlayer(), guid);
            if (!unit)
                return;

            if (!unit.HasAuraType(AuraType.CloneCaster))
                return;

            // Get creator of the unit (SPELL_AURA_CLONE_CASTER does not stack)
            var creator = unit.GetAuraEffectsByType(AuraType.CloneCaster).FirstOrDefault().GetCaster();
            if (!creator)
                return;

            var player = creator.ToPlayer();
            if (player)
            {
                var mirrorImageComponentedData = new MirrorImageComponentedData();
                mirrorImageComponentedData.UnitGUID = guid;
                mirrorImageComponentedData.DisplayID = (int)creator.GetDisplayId();
                mirrorImageComponentedData.RaceID = (byte)creator.GetRace();
                mirrorImageComponentedData.Gender = (byte)creator.GetGender();
                mirrorImageComponentedData.ClassID = (byte)creator.GetClass();

                foreach (var customization in player.m_playerData.Customizations)
                {
                    var chrCustomizationChoice = new ChrCustomizationChoice();
                    chrCustomizationChoice.ChrCustomizationOptionID = customization.ChrCustomizationOptionID;
                    chrCustomizationChoice.ChrCustomizationChoiceID = customization.ChrCustomizationChoiceID;
                    mirrorImageComponentedData.Customizations.Add(chrCustomizationChoice);
                }

                var guild = player.GetGuild();
                mirrorImageComponentedData.GuildGUID = (guild ? guild.GetGUID() : ObjectGuid.Empty);

                byte[] itemSlots =
                {
                    EquipmentSlot.Head,
                    EquipmentSlot.Shoulders,
                    EquipmentSlot.Shirt,
                    EquipmentSlot.Chest,
                    EquipmentSlot.Waist ,
                    EquipmentSlot.Legs ,
                    EquipmentSlot.Feet,
                    EquipmentSlot.Wrist,
                    EquipmentSlot.Hands,
                    EquipmentSlot.Tabard,
                    EquipmentSlot.Cloak
                };

                // Display items in visible slots
                foreach (var slot in itemSlots)
                {
                    uint itemDisplayId;
                    Item item = player.GetItemByPos(InventorySlots.Bag0, slot);
                    if (item != null)
                        itemDisplayId = item.GetDisplayId(player);
                    else
                        itemDisplayId = 0;

                    mirrorImageComponentedData.ItemDisplayID.Add((int)itemDisplayId);
                }

                SendPacket(mirrorImageComponentedData);
            }
            else
            {
                var data = new MirrorImageCreatureData();
                data.UnitGUID = guid;
                data.DisplayID = (int)creator.GetDisplayId();
                SendPacket(data);
            }
        }

        [WorldPacketHandler(ClientOpcodes.MissileTrajectoryCollision)]
        private void HandleMissileTrajectoryCollision(MissileTrajectoryCollision packet)
        {
            var caster = Global.ObjAccessor.GetUnit(_player, packet.Target);
            if (caster == null)
                return;

            var spell = caster.FindCurrentSpellBySpellId(packet.SpellID);
            if (spell == null || !spell.m_targets.HasDst())
                return;

            Position pos = spell.m_targets.GetDstPos();
            pos.Relocate(packet.CollisionPos);
            spell.m_targets.ModDst(pos);

            // we changed dest, recalculate flight time
            spell.RecalculateDelayMomentForDst();

            var data = new NotifyMissileTrajectoryCollision();
            data.Caster = packet.Target;
            data.CastID = packet.CastID;
            data.CollisionPos = packet.CollisionPos;
            caster.SendMessageToSet(data, true);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateMissileTrajectory)]
        private void HandleUpdateMissileTrajectory(UpdateMissileTrajectory packet)
        {
            var caster = Global.ObjAccessor.GetUnit(GetPlayer(), packet.Guid);
            var spell = caster ? caster.GetCurrentSpell(CurrentSpellTypes.Generic) : null;
            if (!spell || spell.m_spellInfo.Id != packet.SpellID || !spell.m_targets.HasDst() || !spell.m_targets.HasSrc())
                return;

            var pos = spell.m_targets.GetSrcPos();
            pos.Relocate(packet.FirePos);
            spell.m_targets.ModSrc(pos);

            pos = spell.m_targets.GetDstPos();
            pos.Relocate(packet.ImpactPos);
            spell.m_targets.ModDst(pos);

            spell.m_targets.SetPitch(packet.Pitch);
            spell.m_targets.SetSpeed(packet.Speed);

            if (packet.Status.HasValue)
            {
                GetPlayer().ValidateMovementInfo(packet.Status.Value);
                /*public uint opcode;
                recvPacket >> opcode;
                recvPacket.SetOpcode(CMSG_MOVE_STOP); // always set to CMSG_MOVE_STOP in client SetOpcode
                //HandleMovementOpcodes(recvPacket);*/
            }
        }

        [WorldPacketHandler(ClientOpcodes.RequestCategoryCooldowns, Processing = PacketProcessing.Inplace)]
        private void HandleRequestCategoryCooldowns(RequestCategoryCooldowns requestCategoryCooldowns)
        {
            GetPlayer().SendSpellCategoryCooldowns();
        }
    }
}
