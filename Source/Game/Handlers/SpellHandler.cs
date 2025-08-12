// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Entities;
using Game.Guilds;
using Game.Loots;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;

namespace Game
{
    public partial class WorldSession
    {
        [WorldPacketHandler(ClientOpcodes.UseItem, Processing = PacketProcessing.Inplace)]
        void HandleUseItem(UseItem packet)
        {
            // ignore for remote control state
            if (_player.GetUnitBeingMoved() != _player)
                return;

            // Skip casting invalid spells right away
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(packet.Cast.SpellID, _player.GetMap().GetDifficultyID());
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, $"WorldSession::HandleUseItemOpcode: attempted to cast a non-existing spell (Id: {packet.Cast.SpellID})");
                return;
            }

            if (_player.CanRequestSpellCast(spellInfo, _player))
                _player.RequestSpellCast(new SpellCastRequest(packet.Cast, _player.GetGUID(), new SpellCastRequestItemData(packet.PackSlot, packet.Slot, packet.CastItem)));
            else
                Spell.SendCastResult(_player, spellInfo, default, packet.Cast.CastID, SpellCastResult.SpellInProgress);
        }

        [WorldPacketHandler(ClientOpcodes.OpenItem, Processing = PacketProcessing.Inplace)]
        void HandleOpenItem(OpenItem packet)
        {
            Player player = GetPlayer();

            // ignore for remote control state
            if (player.GetUnitBeingMoved() != player)
                return;

            // additional check, client outputs message on its own
            if (!player.IsAlive())
            {
                player.SendEquipError(InventoryResult.PlayerDead);
                return;
            }

            Item item = player.GetItemByPos(packet.Slot, packet.PackSlot);
            if (item == null)
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
            if (!proto.HasFlag(ItemFlags.HasLoot) && !item.IsWrapped())
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
                LockRecord lockInfo = CliDB.LockStorage.LookupByKey(lockId);
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

            if (item.IsWrapped())// wrapped?
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.SEL_CHARACTER_GIFT_BY_ITEM);
                stmt.AddValue(0, item.GetGUID().GetCounter());

                var pos = item.GetPos();
                var itemGuid = item.GetGUID();
                _queryProcessor.AddCallback(DB.Characters.AsyncQuery(stmt)
                    .WithCallback(result => HandleOpenWrappedItemCallback(pos, itemGuid, result)));
            }
            else
            {
                // If item doesn't already have loot, attempt to load it. If that
                // fails then this is first time opening, generate loot
                if (!item.m_lootGenerated && !Global.LootItemStorage.LoadStoredLoot(item, player))
                {
                    Loot loot = new(player.GetMap(), item.GetGUID(), LootType.Item, null);
                    item.loot = loot;
                    item.m_lootGenerated = true;
                    loot.GenerateMoneyLoot(item.GetTemplate().MinMoneyLoot, item.GetTemplate().MaxMoneyLoot);
                    loot.FillLoot(item.GetEntry(), LootStorage.Items, player, true, loot.gold != 0);

                    // Force save the loot and money items that were just rolled
                    //  Also saves the container item ID in Loot struct (not to DB)
                    if (loot.gold > 0 || loot.unlootedCount > 0)
                        Global.LootItemStorage.AddNewStoredLoot(item.GetGUID().GetCounter(), loot, player);
                }
                if (item.loot != null)
                    player.SendLoot(item.loot);
                else
                    player.SendLootError(ObjectGuid.Empty, item.GetGUID(), LootError.NoLoot);
            }
        }

        void HandleOpenWrappedItemCallback(ushort pos, ObjectGuid itemGuid, SQLResult result)
        {
            if (GetPlayer() == null)
                return;

            Item item = GetPlayer().GetItemByPos(pos);
            if (item == null)
                return;

            if (item.GetGUID() != itemGuid || !item.IsWrapped()) // during getting result, gift was swapped with another item
                return;

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.Network, $"Wrapped item {item.GetGUID()} don't have record in character_gifts table and will deleted");
                GetPlayer().DestroyItem(item.GetBagSlot(), item.GetSlot(), true);
                return;
            }

            SQLTransaction trans = new();

            uint entry = result.Read<uint>(0);
            uint flags = result.Read<uint>(1);

            item.SetGiftCreator(ObjectGuid.Empty);
            item.SetEntry(entry);
            item.ReplaceAllItemFlags((ItemFieldFlags)flags);
            item.SetMaxDurability(item.GetTemplate().MaxDurability);
            item.SetState(ItemUpdateState.Changed, GetPlayer());

            GetPlayer().SaveInventoryAndGoldToDB(trans);

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
            stmt.AddValue(0, itemGuid.GetCounter());
            trans.Append(stmt);

            DB.Characters.CommitTransaction(trans);
        }

        [WorldPacketHandler(ClientOpcodes.GameObjUse, Processing = PacketProcessing.Inplace)]
        void HandleGameObjectUse(GameObjUse packet)
        {
            GameObject obj = GetPlayer().GetGameObjectIfCanInteractWith(packet.Guid);
            if (obj != null)
            {
                // ignore for remote control state
                if (GetPlayer().GetUnitBeingMoved() != GetPlayer())
                    if (!(GetPlayer().IsOnVehicle(GetPlayer().GetUnitBeingMoved()) || GetPlayer().IsMounted()) && !obj.GetGoInfo().IsUsableMounted())
                        return;

                obj.Use(GetPlayer());
            }
        }

        [WorldPacketHandler(ClientOpcodes.GameObjReportUse, Processing = PacketProcessing.Inplace)]
        void HandleGameobjectReportUse(GameObjReportUse packet)
        {
            // ignore for remote control state
            if (GetPlayer().GetUnitBeingMoved() != GetPlayer())
                return;

            GameObject go = GetPlayer().GetGameObjectIfCanInteractWith(packet.Guid);
            if (go != null)
            {
                if (go.GetAI().OnGossipHello(GetPlayer()))
                    return;

                GetPlayer().UpdateCriteria(CriteriaType.UseGameobject, go.GetEntry());
            }
        }

        [WorldPacketHandler(ClientOpcodes.CastSpell, Processing = PacketProcessing.ThreadSafe)]
        void HandleCastSpell(CastSpell cast)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(cast.Cast.SpellID, _player.GetMap().GetDifficultyID());
            if (spellInfo == null)
            {
                Log.outError(LogFilter.Network, $"WorldSession::HandleCastSpellOpcode: attempted to cast a non-existing spell (Id: {cast.Cast.SpellID})");
                return;
            }

            // ignore for remote control state (for player case)
            Unit mover = _player.GetUnitBeingMoved();
            if (mover != _player && mover.IsPlayer())
                return;

            Unit castingUnit = mover;
            if (castingUnit.IsCreature() && !castingUnit.ToCreature().HasSpell(spellInfo.Id))
            {
                // If the vehicle creature does not have the spell but it allows the passenger to cast own spells
                // change caster to player and let him cast
                if (!GetPlayer().IsOnVehicle(castingUnit) || spellInfo.CheckVehicle(GetPlayer()) != SpellCastResult.SpellCastOk)
                    return;

                castingUnit = GetPlayer();
            }

            if (cast.Cast.MoveUpdate != null)
                HandleMovementOpcode(ClientOpcodes.MoveStop, cast.Cast.MoveUpdate);

            if (_player.CanRequestSpellCast(spellInfo, castingUnit))
                _player.RequestSpellCast(new SpellCastRequest(cast.Cast, castingUnit.GetGUID()));
            else
                Spell.SendCastResult(_player, spellInfo, default, cast.Cast.CastID, SpellCastResult.SpellInProgress);
        }

        [WorldPacketHandler(ClientOpcodes.CancelCast, Processing = PacketProcessing.ThreadSafe)]
        void HandleCancelCast(CancelCast packet)
        {
            if (_player.IsNonMeleeSpellCast(false))
            {
                _player.InterruptNonMeleeSpells(false, packet.SpellID, false);
                _player.CancelPendingCastRequest(); // canceling casts also cancels pending spell cast requests
            }
        }

        [WorldPacketHandler(ClientOpcodes.CancelAura, Processing = PacketProcessing.Inplace)]
        void HandleCancelAura(CancelAura cancelAura)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(cancelAura.SpellID, _player.GetMap().GetDifficultyID());
            if (spellInfo == null)
                return;

            // not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
            if (spellInfo.HasAttribute(SpellAttr0.NoAuraCancel))
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

        [WorldPacketHandler(ClientOpcodes.CancelGrowthAura, Processing = PacketProcessing.Inplace)]
        void HandleCancelGrowthAura(CancelGrowthAura cancelGrowthAura)
        {
            GetPlayer().RemoveAurasByType(AuraType.ModScale, aurApp =>
            {
                SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.CancelMountAura, Processing = PacketProcessing.Inplace)]
        void HandleCancelMountAura(CancelMountAura packet)
        {
            GetPlayer().RemoveAurasByType(AuraType.Mounted, aurApp =>
            {
                SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.PetCancelAura, Processing = PacketProcessing.Inplace)]
        void HandlePetCancelAura(PetCancelAura packet)
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

            if (pet != GetPlayer().GetGuardianPet() && pet != GetPlayer().GetCharmed())
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

        [WorldPacketHandler(ClientOpcodes.CancelModSpeedNoControlAuras, Processing = PacketProcessing.Inplace)]
        void HandleCancelModSpeedNoControlAuras(CancelModSpeedNoControlAuras cancelModSpeedNoControlAuras)
        {
            Unit mover = _player.GetUnitBeingMoved();
            if (mover == null || mover.GetGUID() != cancelModSpeedNoControlAuras.TargetGUID)
                return;

            _player.RemoveAurasByType(AuraType.ModSpeedNoControl, aurApp =>
            {
                SpellInfo spellInfo = aurApp.GetBase().GetSpellInfo();
                return !spellInfo.HasAttribute(SpellAttr0.NoAuraCancel) && spellInfo.IsPositive() && !spellInfo.IsPassive();
            });
        }

        [WorldPacketHandler(ClientOpcodes.CancelAutoRepeatSpell, Processing = PacketProcessing.Inplace)]
        void HandleCancelAutoRepeatSpell(CancelAutoRepeatSpell packet)
        {
            //may be better send SMSG_CANCEL_AUTO_REPEAT?
            //cancel and prepare for deleting
            _player.InterruptSpell(CurrentSpellTypes.AutoRepeat);
        }

        [WorldPacketHandler(ClientOpcodes.CancelChannelling, Processing = PacketProcessing.Inplace)]
        void HandleCancelQueuedSpellOpcode(CancelQueuedSpell cancelQueuedSpell)
        {
            _player.CancelPendingCastRequest();
        }

        [WorldPacketHandler(ClientOpcodes.CancelChannelling, Processing = PacketProcessing.Inplace)]
        void HandleCancelChanneling(CancelChannelling cancelChanneling)
        {
            // ignore for remote control state (for player case)
            Unit mover = _player.GetUnitBeingMoved();
            if (mover != _player && mover.IsTypeId(TypeId.Player))
                return;

            var spellInfo = Global.SpellMgr.GetSpellInfo((uint)cancelChanneling.ChannelSpell, mover.GetMap().GetDifficultyID());
            if (spellInfo == null)
                return;

            // not allow remove spells with attr SPELL_ATTR0_CANT_CANCEL
            if (spellInfo.HasAttribute(SpellAttr0.NoAuraCancel))
                return;

            var spell = mover.GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (spell == null || spell.GetSpellInfo().Id != spellInfo.Id)
                return;

            mover.InterruptSpell(CurrentSpellTypes.Channeled);
        }

        [WorldPacketHandler(ClientOpcodes.SetEmpowerMinHoldStagePercent, Processing = PacketProcessing.Inplace)]
        void HandleSetEmpowerMinHoldStagePercent(SetEmpowerMinHoldStagePercent setEmpowerMinHoldStagePercent)
        {
            _player.SetEmpowerMinHoldStagePercent(setEmpowerMinHoldStagePercent.MinHoldStagePercent);
        }

        [WorldPacketHandler(ClientOpcodes.SpellEmpowerRelease, Processing = PacketProcessing.Inplace)]
        void HandleSpellEmpowerRelease(SpellEmpowerRelease spellEmpowerRelease)
        {
            // ignore for remote control state (for player case)
            Unit mover = _player.GetUnitBeingMoved();
            if (mover != _player && mover.IsPlayer())
                return;

            Spell spell = mover.GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (spell == null || spell.GetSpellInfo().Id != spellEmpowerRelease.SpellID || !spell.IsEmpowerSpell())
                return;

            spell.SetEmpowerReleasedByClient(true);
        }

        [WorldPacketHandler(ClientOpcodes.SpellEmpowerRestart, Processing = PacketProcessing.Inplace)]
        void HandleSpellEmpowerRestart(SpellEmpowerRestart spellEmpowerRestart)
        {
            // ignore for remote control state (for player case)
            Unit mover = _player.GetUnitBeingMoved();
            if (mover != _player && mover.IsPlayer())
                return;

            Spell spell = mover.GetCurrentSpell(CurrentSpellTypes.Channeled);
            if (spell == null || spell.GetSpellInfo().Id != spellEmpowerRestart.SpellID || !spell.IsEmpowerSpell())
                return;

            spell.SetEmpowerReleasedByClient(false);
        }

        [WorldPacketHandler(ClientOpcodes.TotemDestroyed, Processing = PacketProcessing.Inplace)]
        void HandleTotemDestroyed(TotemDestroyed totemDestroyed)
        {
            // ignore for remote control state
            if (GetPlayer().GetUnitBeingMoved() != GetPlayer())
                return;

            byte slotId = totemDestroyed.Slot;
            slotId += (int)SummonSlot.Totem;

            if (slotId >= SharedConst.MaxTotemSlot)
                return;

            if (GetPlayer().m_SummonSlot[slotId].IsEmpty())
                return;

            Creature totem = ObjectAccessor.GetCreature(GetPlayer(), _player.m_SummonSlot[slotId]);
            if (totem != null && totem.IsTotem() && (totemDestroyed.TotemGUID.IsEmpty() || totem.GetGUID() == totemDestroyed.TotemGUID))
                totem.DespawnOrUnsummon();
        }

        [WorldPacketHandler(ClientOpcodes.SelfRes)]
        void HandleSelfRes(SelfRes selfRes)
        {
            List<uint> selfResSpells = _player.m_activePlayerData.SelfResSpells;
            if (!selfResSpells.Contains(selfRes.SpellId))
                return;

            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(selfRes.SpellId, _player.GetMap().GetDifficultyID());
            if (spellInfo == null)
                return;

            if (_player.HasAuraType(AuraType.PreventResurrection) && !spellInfo.HasAttribute(SpellAttr7.BypassNoResurrectAura))
                return; // silent return, client should display error by itself and not send this opcode

            _player.CastSpell(_player, selfRes.SpellId, new CastSpellExtraArgs(_player.GetMap().GetDifficultyID()));
            _player.RemoveSelfResSpell(selfRes.SpellId);
        }

        [WorldPacketHandler(ClientOpcodes.SpellClick, Processing = PacketProcessing.Inplace)]
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
            if (unit == null)
                return;

            if (!unit.HasAuraType(AuraType.CloneCaster))
                return;

            // Get creator of the unit (SPELL_AURA_CLONE_CASTER does not stack)
            Unit creator = unit.GetAuraEffectsByType(AuraType.CloneCaster).FirstOrDefault().GetCaster();
            if (creator == null)
                return;

            Player player = creator.ToPlayer();
            if (player != null)
            {
                MirrorImageComponentedData mirrorImageComponentedData = new();
                mirrorImageComponentedData.UnitGUID = guid;
                var chrModel = Global.DB2Mgr.GetChrModel(creator.GetRace(), creator.GetGender());
                if (chrModel != null)
                    mirrorImageComponentedData.ChrModelID = (int)chrModel.Id;
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

                Guild guild = player.GetGuild();
                mirrorImageComponentedData.GuildGUID = (guild != null ? guild.GetGUID() : ObjectGuid.Empty);

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
                MirrorImageCreatureData data = new();
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

            // we changed dest, recalculate flight time
            spell.RecalculateDelayMomentForDst();

            NotifyMissileTrajectoryCollision data = new();
            data.Caster = packet.Target;
            data.CastID = packet.CastID;
            data.CollisionPos = packet.CollisionPos;
            caster.SendMessageToSet(data, true);
        }

        [WorldPacketHandler(ClientOpcodes.UpdateMissileTrajectory)]
        void HandleUpdateMissileTrajectory(UpdateMissileTrajectory packet)
        {
            Unit caster = Global.ObjAccessor.GetUnit(GetPlayer(), packet.Guid);
            Spell spell = caster != null ? caster.GetCurrentSpell(CurrentSpellTypes.Generic) : null;
            if (spell == null || spell.m_spellInfo.Id != packet.SpellID || spell.m_castId != packet.CastID || !spell.m_targets.HasDst() || !spell.m_targets.HasSrc())
                return;

            Position pos = spell.m_targets.GetSrcPos();
            pos.Relocate(packet.FirePos);
            spell.m_targets.ModSrc(pos);

            pos = spell.m_targets.GetDstPos();
            pos.Relocate(packet.ImpactPos);
            spell.m_targets.ModDst(pos);

            spell.m_targets.SetPitch(packet.Pitch);
            spell.m_targets.SetSpeed(packet.Speed);

            if (packet.Status != null)
            {
                GetPlayer().ValidateMovementInfo(packet.Status);
                /*public uint opcode;
                recvPacket >> opcode;
                recvPacket.SetOpcode(CMSG_MOVE_STOP); // always set to CMSG_MOVE_STOP in client SetOpcode
                //HandleMovementOpcodes(recvPacket);*/
            }
        }

        [WorldPacketHandler(ClientOpcodes.KeyboundOverride, Processing = PacketProcessing.ThreadSafe)]
        void HandleKeyboundOverride(KeyboundOverride keyboundOverride)
        {
            Player player = GetPlayer();
            if (!player.HasAuraTypeWithMiscvalue(AuraType.KeyboundOverride, keyboundOverride.OverrideID))
                return;

            SpellKeyboundOverrideRecord spellKeyboundOverride = CliDB.SpellKeyboundOverrideStorage.LookupByKey(keyboundOverride.OverrideID);
            if (spellKeyboundOverride == null)
                return;

            player.CastSpell(player, spellKeyboundOverride.Data);
        }
    }
}
