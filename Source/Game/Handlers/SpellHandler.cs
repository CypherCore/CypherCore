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
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;
using Game.Network;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.UseItem)]
        void HandleUseItem(UseItem packet)
        {
            Player user = GetPlayer();

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

            ItemTemplate proto = item.GetTemplate();
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
                for (int i = 0; i < proto.Effects.Count; ++i)
                {
                    SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo((uint)proto.Effects[i].SpellID);
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

            SpellCastTargets targets = new SpellCastTargets(user, packet.Cast);

            // Note: If script stop casting it must send appropriate data to client to prevent stuck item in gray state.
            if (!Global.ScriptMgr.OnItemUse(user, item, targets, packet.Cast.CastID))
            {
                // no script or script not process request by self
                user.CastItemUseSpell(item, targets, packet.Cast.CastID, packet.Cast.Misc);
            }
        }

        [WorldPacketHandler(ClientOpcodes.OpenItem)]
        void HandleOpenItem(OpenItem packet)
        {
            Player player = GetPlayer();

            // ignore for remote control state
            if (player.m_unitMovedByMe != player)
                return;

            Item item = player.GetItemByPos(packet.Slot, packet.PackSlot);
            if (!item)
            {
                player.SendEquipError(InventoryResult.ItemNotFound);
                return;
            }

            ItemTemplate proto = item.GetTemplate();
            if (proto == null)
            {
                player.SendEquipError(InventoryResult.ItemNotFound, item);
                return;
            }

            // Verify that the bag is an actual bag or wrapped item that can be used "normally"
            if (!proto.GetFlags().HasAnyFlag(ItemFlags.HasLoot) && !item.HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))
            {
                player.SendEquipError(InventoryResult.ClientLockedOut, item);
                Log.outError(LogFilter.Network, "Possible hacking attempt: Player {0} [guid: {1}] tried to open item [guid: {2}, entry: {3}] which is not openable!",
                        player.GetName(), player.GetGUID().ToString(), item.GetGUID().ToString(), proto.GetId());
                return;
            }

            // locked item
            uint lockId = proto.GetLockID();
            if (lockId != 0)
            {
                LockRecord  lockInfo = CliDB.LockStorage.LookupByKey(lockId);
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

            if (item.HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))// wrapped?
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_CHARACTER_GIFT_BY_ITEM);
                stmt.AddValue(0, item.GetGUID().GetCounter());
                SQLResult result = DB.Characters.Query(stmt);

                if (!result.IsEmpty())
                {
                    uint entry = result.Read<uint>(0);
                    uint flags = result.Read<uint>(1);

                    item.SetUInt64Value(ItemFields.GiftCreator, 0);
                    item.SetEntry(entry);
                    item.SetUInt32Value(ItemFields.Flags, flags);
                    item.SetState(ItemUpdateState.Changed, player);
                }
                else
                {
                    Log.outError(LogFilter.Network, "Wrapped item {0} don't have record in character_gifts table and will deleted", item.GetGUID().ToString());
                    player.DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
                    return;
                }

                stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
                stmt.AddValue(0, item.GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
            else
                player.SendLoot(item.GetGUID(), LootType.Corpse);
        }

        [WorldPacketHandler(ClientOpcodes.GameObjUse)]
        void HandleGameObjectUse(GameObjUse packet)
        {
            GameObject obj = GetPlayer().GetGameObjectIfCanInteractWith(packet.Guid);
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
        void HandleGameobjectReportUse(GameObjReportUse packet)
        {
            // ignore for remote control state
            if (GetPlayer().m_unitMovedByMe != GetPlayer())
                return;

            GameObject go = GetPlayer().GetGameObjectIfCanInteractWith(packet.Guid);
            if (go)
            {
                if (go.GetAI().GossipHello(GetPlayer(), false))
                    return;

                GetPlayer().UpdateCriteria(CriteriaTypes.UseGameobject, go.GetEntry());
            }
        }

        [WorldPacketHandler(ClientOpcodes.CastSpell, Processing = PacketProcessing.ThreadSafe)]
        void HandleCastSpell(CastSpell cast)
        {
            // ignore for remote control state (for player case)
            Unit mover = GetPlayer().m_unitMovedByMe;
            if (mover != GetPlayer() && mover.IsTypeId(TypeId.Player))
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(cast.Cast.SpellID);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "WORLD: unknown spell id {0}", cast.Cast.SpellID);
                return;
            }

            if (spellInfo.IsPassive())
                return;

            Unit caster = mover;
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
            if (GetPlayer().isPossessing())
                return;

            // client provided targets
            SpellCastTargets targets = new SpellCastTargets(caster, cast.Cast);

            // auto-selection buff level base at target level (in spellInfo)
            if (targets.GetUnitTarget() != null)
            {
                SpellInfo actualSpellInfo = spellInfo.GetAuraRankForLevel(targets.GetUnitTarget().GetLevelForTarget(caster));

                // if rank not found then function return NULL but in explicit cast case original spell can be casted and later failed with appropriate error message
                if (actualSpellInfo != null)
                    spellInfo = actualSpellInfo;
            }

            if (cast.Cast.MoveUpdate.HasValue)
                HandleMovementOpcode(ClientOpcodes.MoveStop, cast.Cast.MoveUpdate.Value);

            Spell spell = new Spell(caster, spellInfo, TriggerCastFlags.None, ObjectGuid.Empty, false);

            SpellPrepare spellPrepare = new SpellPrepare();
            spellPrepare.ClientCastID = cast.Cast.CastID;
            spellPrepare.ServerCastID = spell.m_castId;
            SendPacket(spellPrepare);

            spell.m_fromClient = true;
            spell.m_misc.Data0 = cast.Cast.Misc[0];
            spell.m_misc.Data1 = cast.Cast.Misc[1];
            spell.prepare(targets);
        }

        [WorldPacketHandler(ClientOpcodes.CancelCast, Processing = PacketProcessing.ThreadSafe)]
        void HandleCancelCast(CancelCast packet)
        {
            if (GetPlayer().IsNonMeleeSpellCast(false))
                GetPlayer().InterruptNonMeleeSpells(false, packet.SpellID, false);
        }

        [WorldPacketHandler(ClientOpcodes.CancelAura)]
        void HandleCancelAura(CancelAura cancelAura)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(cancelAura.SpellID);
            if (spellInfo == null)
                return;

            // not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
            if (spellInfo.HasAttribute(SpellAttr0.CantCancel))
                return;

            // channeled spell case (it currently casted then)
            if (spellInfo.IsChanneled())
            {
                Spell curSpell = GetPlayer().GetCurrentSpell(CurrentSpellTypes.Channeled);
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
        void HandleCancelGrowthAura(CancelGrowthAura cancelGrowthAura)
        {
            GetPlayer().RemoveAurasByType(AuraType.ModScale, aurApp =>
            {
                SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.CantCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.CancelMountAura)]
        void HandleCancelMountAura(CancelMountAura packet)
        {
            GetPlayer().RemoveAurasByType(AuraType.Mounted, aurApp =>
            {
                SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.CantCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.PetCancelAura)]
        void HandlePetCancelAura(PetCancelAura packet)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(packet.SpellID);
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, "WORLD: unknown PET spell id {0}", packet.SpellID);
                return;
            }

            Creature pet= ObjectAccessor.GetCreatureOrPetOrVehicle(_player, packet.PetGUID);
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
                pet.SendPetActionFeedback(packet.SpellID, ActionFeedback.PetDead);
                return;
            }

            pet.RemoveOwnedAura(packet.SpellID, ObjectGuid.Empty, 0, AuraRemoveMode.Cancel);
        }

        [WorldPacketHandler(ClientOpcodes.CancelAutoRepeatSpell)]
        void HandleCancelAutoRepeatSpell(CancelAutoRepeatSpell packet)
        {
            //may be better send SMSG_CANCEL_AUTO_REPEAT?
            //cancel and prepare for deleting
            _player.InterruptSpell(CurrentSpellTypes.AutoRepeat);
        }

        [WorldPacketHandler(ClientOpcodes.CancelChannelling)]
        void HandleCancelChanneling(CancelChannelling cancelChanneling)
        {
            // ignore for remote control state (for player case)
            Unit mover = _player.m_unitMovedByMe;
            if (mover != _player && mover.IsTypeId(TypeId.Player))
                return;

            mover.InterruptSpell(CurrentSpellTypes.Channeled);
        }

        [WorldPacketHandler(ClientOpcodes.TotemDestroyed)]
        void HandleTotemDestroyed(TotemDestroyed totemDestroyed)
        {
            // ignore for remote control state
            if (GetPlayer().m_unitMovedByMe != GetPlayer())
                return;

            byte slotId = totemDestroyed.Slot;
            slotId += (int)SummonSlot.Totem;

            if (slotId >= SharedConst.MaxTotemSlot)
                return;

            if (GetPlayer().m_SummonSlot[slotId].IsEmpty())
                return;

            Creature totem = ObjectAccessor.GetCreature(GetPlayer(), _player.m_SummonSlot[slotId]);
            if (totem != null && totem.IsTotem())// && totem.GetGUID() == packet.TotemGUID)  Unknown why blizz doesnt send the guid when you right click it.
                totem.ToTotem().UnSummon();
        }

        [WorldPacketHandler(ClientOpcodes.SelfRes)]
        void HandleSelfRes(SelfRes selfRes)
        {
            if (_player.HasAuraType(AuraType.PreventResurrection))
                return; // silent return, client should display error by itself and not send this opcode

            var selfResSpells = _player.GetDynamicValues(ActivePlayerDynamicFields.SelfResSpells);
            if (!selfResSpells.Contains(selfRes.SpellId))
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(selfRes.SpellId);
            if (spellInfo != null)
                _player.CastSpell(_player, spellInfo, false, null);

            _player.RemoveDynamicValue(ActivePlayerDynamicFields.SelfResSpells, selfRes.SpellId);
        }

        [WorldPacketHandler(ClientOpcodes.SpellClick)]
        void HandleSpellClick(SpellClick packet)
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
        void HandleMirrorImageDataRequest(GetMirrorImageData packet)
        {
            ObjectGuid guid = packet.UnitGUID;

            // Get unit for which data is needed by client
            Unit unit = Global.ObjAccessor.GetUnit(GetPlayer(), guid);
            if (!unit)
                return;

            if (!unit.HasAuraType(AuraType.CloneCaster))
                return;

            // Get creator of the unit (SPELL_AURA_CLONE_CASTER does not stack)
            Unit creator = unit.GetAuraEffectsByType(AuraType.CloneCaster).FirstOrDefault().GetCaster();
            if (!creator)
                return;

            Player player = creator.ToPlayer();
            if (player)
            {
                MirrorImageComponentedData data = new MirrorImageComponentedData();
                data.UnitGUID = guid;
                data.DisplayID = (int)creator.GetDisplayId();
                data.RaceID = (byte)creator.GetRace();
                data.Gender = (byte)creator.GetGender();
                data.ClassID = (byte)creator.GetClass();

                Guild guild = player.GetGuild();

                data.SkinColor = player.GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetSkinId);
                data.FaceVariation = player.GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetFaceId);
                data.HairVariation = player.GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairStyleId);
                data.HairColor = player.GetByteValue(PlayerFields.Bytes, PlayerFieldOffsets.BytesOffsetHairColorId);
                data.BeardVariation = player.GetByteValue(PlayerFields.Bytes2, PlayerFieldOffsets.Bytes2OffsetFacialStyle);
                for (int i = 0; i < PlayerConst.CustomDisplaySize; ++i)
                    data.CustomDisplay[i] = player.GetByteValue(PlayerFields.Bytes2, (byte)(PlayerFieldOffsets.Bytes2OffsetCustomDisplayOption + i));
                data.GuildGUID = (guild ? guild.GetGUID() : ObjectGuid.Empty);

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
                    if ((slot == EquipmentSlot.Head && player.HasFlag(PlayerFields.Flags, PlayerFlags.HideHelm)) ||
                        (slot == EquipmentSlot.Cloak && player.HasFlag(PlayerFields.Flags, PlayerFlags.HideCloak)))
                        itemDisplayId = 0;
                    else if (item)
                        itemDisplayId = item.GetDisplayId(player);
                    else
                        itemDisplayId = 0;

                    data.ItemDisplayID.Add((int)itemDisplayId);
                }

                SendPacket(data);
            }
            else
            {
                MirrorImageCreatureData data = new MirrorImageCreatureData();
                data.UnitGUID = guid;
                data.DisplayID = (int)creator.GetDisplayId();
                SendPacket(data);
            }
        }

        [WorldPacketHandler(ClientOpcodes.MissileTrajectoryCollision)]
        void HandleMissileTrajectoryCollision(MissileTrajectoryCollision packet)
        {
            Unit caster = Global.ObjAccessor.GetUnit(_player, packet.Target);
            if (caster == null)
                return;

            Spell spell = caster.FindCurrentSpellBySpellId(packet.SpellID);
            if (spell == null || !spell.m_targets.HasDst())
                return;

            Position pos = spell.m_targets.GetDstPos();
            pos.Relocate(packet.CollisionPos);
            spell.m_targets.ModDst(pos);

            NotifyMissileTrajectoryCollision data = new NotifyMissileTrajectoryCollision();
            data.Caster = packet.Target;
            data.CastID = packet.CastID;
            data.CollisionPos = packet.CollisionPos;
            caster.SendMessageToSet(data, true);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateMissileTrajectory)]
        void HandleUpdateMissileTrajectory(UpdateMissileTrajectory packet)
        {
            Unit caster = Global.ObjAccessor.GetUnit(GetPlayer(), packet.Guid);
            Spell spell = caster ? caster.GetCurrentSpell(CurrentSpellTypes.Generic) : null;
            if (!spell || spell.m_spellInfo.Id != packet.SpellID || !spell.m_targets.HasDst() || !spell.m_targets.HasSrc())
                return;

            Position pos = spell.m_targets.GetSrcPos();
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
        void HandleRequestCategoryCooldowns(RequestCategoryCooldowns requestCategoryCooldowns)
        {
            GetPlayer().SendSpellCategoryCooldowns();
        }
    }
}
