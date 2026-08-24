// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Game.Entities
{
    public class ObjectFieldData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Object, 4), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<uint> EntryId = new(0, 1);
        public UpdateField<uint> DynamicFlags = new(0, 2);
        public UpdateField<float> Scale = new(0, 3);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WriteUInt32(GetViewerDependentEntryId(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentDynamicFlags(this, receiver, owner));
            data.WriteFloat(Scale);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, WorldObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(GetViewerDependentEntryId(this, receiver, owner));
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(GetViewerDependentDynamicFlags(this, receiver, owner));
                }
                if (changesMask[3])
                {
                    data.WriteFloat(Scale);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(EntryId);
            ClearChangesMask(DynamicFlags);
            ClearChangesMask(Scale);
            _changesMask.ResetAll();
        }

        uint GetViewerDependentEntryId(ObjectFieldData objectData, Player receiver, WorldObject obj)
        {
            uint entryId = objectData.EntryId;
            Unit unit = obj.ToUnit();
            if (unit != null)
            {
                TempSummon summon = unit.ToTempSummon();
                if (summon != null)
                    if (summon.GetSummonerGUID() == receiver.GetGUID() && summon.GetCreatureIdVisibleToSummoner().HasValue)
                        entryId = summon.GetCreatureIdVisibleToSummoner().Value;
            }

            return entryId;
        }

        uint GetViewerDependentDynamicFlags(ObjectFieldData objectData, Player receiver, WorldObject obj)
        {
            uint dynamicFlags = objectData.DynamicFlags;

            Unit unit = obj.ToUnit();
            if (unit != null)
            {
                Creature creature = obj.ToCreature();
                if (creature != null)
                {
                    if ((dynamicFlags & (uint)UnitDynFlags.Tapped) != 0 && !creature.IsTappedBy(receiver))
                        dynamicFlags &= ~(uint)UnitDynFlags.Tapped;

                    if ((dynamicFlags & (uint)UnitDynFlags.Lootable) != 0 && !receiver.IsAllowedToLoot(creature))
                        dynamicFlags &= ~(uint)UnitDynFlags.Lootable;

                    if ((dynamicFlags & (uint)UnitDynFlags.CanSkin) != 0 && creature.IsSkinnedBy(receiver))
                        dynamicFlags &= ~(uint)UnitDynFlags.CanSkin;
                }

                // unit UNIT_DYNFLAG_TRACK_UNIT should only be sent to caster of SPELL_AURA_MOD_STALKED auras
                if (dynamicFlags.HasAnyFlag((uint)UnitDynFlags.TrackUnit))
                    if (!unit.HasAuraTypeWithCaster(AuraType.ModStalked, receiver.GetGUID()))
                        dynamicFlags &= ~(uint)UnitDynFlags.TrackUnit;
            }
            else
            {
                GameObject gameObject = obj.ToGameObject();
                if (gameObject != null)
                {
                    uint dynFlags = (uint)GameObjectDynamicLowFlags.StateTransitionAnimDone;
                    switch (gameObject.GetGoType())
                    {
                        case GameObjectTypes.Button:
                        case GameObjectTypes.Goober:
                            if (gameObject.HasConditionalInteraction() && gameObject.CanActivateForPlayer(receiver))
                                if (gameObject.GetGoStateFor(receiver.GetGUID()) != GameObjectState.Active)
                                    dynFlags |= (uint)(GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Highlight);
                            break;
                        case GameObjectTypes.QuestGiver:
                            if (gameObject.CanActivateForPlayer(receiver))
                                dynFlags |= (uint)GameObjectDynamicLowFlags.Activate;
                            break;
                        case GameObjectTypes.Chest:
                            if (gameObject.HasConditionalInteraction() && gameObject.CanActivateForPlayer(receiver))
                                dynFlags |= (uint)(GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight);
                            else if (receiver.IsGameMaster())
                                dynFlags |= (uint)(GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle);
                            break;
                        case GameObjectTypes.Generic:
                        case GameObjectTypes.SpellFocus:
                            if (gameObject.HasConditionalInteraction() && gameObject.CanActivateForPlayer(receiver))
                                dynFlags |= (uint)GameObjectDynamicLowFlags.Sparkle;
                            break;
                        case GameObjectTypes.Transport:
                        case GameObjectTypes.MapObjTransport:
                            dynFlags |= dynamicFlags;   // preserve all dynamicflgs
                            break;
                        case GameObjectTypes.CapturePoint:
                            if (!gameObject.CanInteractWithCapturePoint(receiver))
                                dynFlags |= (uint)GameObjectDynamicLowFlags.NoInterract;
                            else
                                dynFlags &= ~(uint)GameObjectDynamicLowFlags.NoInterract;
                            break;
                        case GameObjectTypes.GatheringNode:
                            if (gameObject.HasConditionalInteraction() && gameObject.CanActivateForPlayer(receiver))
                                dynFlags |= (uint)(GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight);
                            if (gameObject.GetGoStateFor(receiver.GetGUID()) == GameObjectState.Active)
                                dynFlags |= (uint)GameObjectDynamicLowFlags.Depleted;
                            break;
                        default:
                            break;
                    }

                    if (!receiver.IsGameMaster())
                    {
                        // GO_DYNFLAG_LO_INTERACT_COND should be applied to GOs with conditional interaction (without GO_FLAG_INTERACT_COND) to disable interaction
                        // (Ignore GAMEOBJECT_TYPE_GATHERING_NODE as some profession-related GOs may include quest loot and can always be interacted with)
                        // (Ignore GAMEOBJECT_TYPE_FLAGSTAND as interaction is handled by GO_DYNFLAG_LO_NO_INTERACT)
                        // (Ignore GAMEOBJECT_TYPE_SPELLCASTER as interaction is handled by GO_DYNFLAG_LO_NO_INTERACT)
                        if (gameObject.GetGoType() != GameObjectTypes.GatheringNode && gameObject.GetGoType() != GameObjectTypes.FlagStand && gameObject.GetGoType() != GameObjectTypes.SpellCaster)
                            if (gameObject.HasConditionalInteraction() && !gameObject.HasFlag(GameObjectFlags.InteractCond))
                                dynFlags |= (uint)GameObjectDynamicLowFlags.InteractCond;

                        if (!gameObject.MeetsInteractCondition(receiver))
                            dynFlags |= (uint)GameObjectDynamicLowFlags.NoInterract;

                        var data = Global.ObjectMgr.GetSpawnMetadata(SpawnObjectType.GameObject, gameObject.GetSpawnId());
                        if (data != null && data.spawnTrackingData != null && !data.spawnTrackingQuestObjectives.Empty())
                            if (receiver.GetSpawnTrackingStateByObjectives(data.spawnTrackingData.SpawnTrackingId, data.spawnTrackingQuestObjectives) != SpawnTrackingState.Active)
                                dynFlags &= ~(uint)GameObjectDynamicLowFlags.Activate;
                    }

                    dynamicFlags = dynFlags;
                }
            }

            return dynamicFlags;
        }
    }

    public class ItemEnchantment() : HasChangesMask(5), IsUpdateFieldStructure<Item>
    {
        public UpdateField<uint> ID = new(0, 1);
        public UpdateField<uint> Duration = new(0, 2);
        public UpdateField<short> Charges = new(0, 3);
        public UpdateField<ushort> Inactive = new(0, 4);

        public void WriteCreate(WorldPacket data, Player receiver, Item owner)
        {
            data.WriteUInt32(ID);
            data.WriteUInt32(Duration);
            data.WriteInt16(Charges);
            data.WriteUInt16(Inactive);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Item owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 5);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(ID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(Duration);
                }
                if (changesMask[3])
                {
                    data.WriteInt16(Charges);
                }
                if (changesMask[4])
                {
                    data.WriteUInt16(Inactive);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ID);
            ClearChangesMask(Duration);
            ClearChangesMask(Charges);
            ClearChangesMask(Inactive);
            _changesMask.ResetAll();
        }
    }

    public class ItemMod : IEquatable<ItemMod>, IsUpdateFieldStructure<Item>
    {
        public byte Type;
        public uint Value;

        public void WriteCreate(WorldPacket data, Player receiver, Item owner)
        {
            data.WriteUInt8(Type);
            data.WriteUInt32(Value);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Item owner) => WriteCreate(data, receiver, owner);

        public bool Equals(ItemMod right)
        {
            return Type == right.Type
                && Value == right.Value;
        }
    }

    public class ItemModList() : HasChangesMask(1), IsUpdateFieldStructure<Item>
    {
        public DynamicUpdateField<ItemMod> Values = new(-1, 0);

        public void WriteCreate(WorldPacket data, Player receiver, Item owner)
        {
            data.WriteBits(Values.Size(), 7);
            data.FlushBits();
            for (int i = 0; i < Values.Size(); ++i)
            {
                Values[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Item owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 1);

            if (changesMask[0])
            {
                if (!ignoreChangesMask)
                    Values.WriteUpdateMask(data, 7);
                else
                    WriteCompleteDynamicFieldUpdateMask(Values.Size(), data, 7);
            }
            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < Values.Size(); ++i)
                {
                    if (Values.HasChanged(i) || ignoreChangesMask)
                    {
                        Values[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Values);
            _changesMask.ResetAll();
        }
    }

    public class ArtifactPower : IEquatable<ArtifactPower>, IsUpdateFieldStructure<Item>
    {
        public ushort ArtifactPowerId;
        public byte PurchasedRank;
        public byte CurrentRankWithBonus;

        public void WriteCreate(WorldPacket data, Player receiver, Item owner)
        {
            data.WriteUInt16(ArtifactPowerId);
            data.WriteUInt8(PurchasedRank);
            data.WriteUInt8(CurrentRankWithBonus);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Item owner) => WriteCreate(data, receiver, owner);

        public bool Equals(ArtifactPower right)
        {
            return ArtifactPowerId == right.ArtifactPowerId
                && PurchasedRank == right.PurchasedRank
                && CurrentRankWithBonus == right.CurrentRankWithBonus;
        }
    }

    public class SocketedGem() : HasChangesMask(20), IsUpdateFieldStructure<Item>
    {
        public UpdateField<uint> ItemId = new(0, 1);
        public UpdateField<byte> Context = new(0, 2);
        public UpdateFieldArray<ushort> BonusListIDs = new(16, 3, 4);

        public void WriteCreate(WorldPacket data, Player receiver, Item owner)
        {
            data.WriteUInt32(ItemId);
            for (int i = 0; i < 16; ++i)
                data.WriteUInt16(BonusListIDs[i]);

            data.WriteUInt8(Context);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Item owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            if (changesMask.GetBlock(0) != 0)
                data.WriteBits(changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(ItemId);
                }
                if (changesMask[2])
                {
                    data.WriteUInt8(Context);
                }
            }
            if (changesMask[3])
            {
                for (int i = 0; i < 16; ++i)
                {
                    if (changesMask[4 + i])
                    {
                        data.WriteUInt16(BonusListIDs[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ItemId);
            ClearChangesMask(Context);
            ClearChangesMask(BonusListIDs);
            _changesMask.ResetAll();
        }
    }

    public class ItemData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Item, 41), IsUpdateFieldStructure<Item>
    {
        public DynamicUpdateField<ArtifactPower> ArtifactPowers = new(0, 1);
        public DynamicUpdateField<SocketedGem> Gems = new(0, 2);
        public UpdateField<ObjectGuid> Owner = new(0, 3);
        public UpdateField<ObjectGuid> ContainedIn = new(0, 4);
        public UpdateField<ObjectGuid> Creator = new(0, 5);
        public UpdateField<ObjectGuid> GiftCreator = new(0, 6);
        public UpdateField<uint> StackCount = new(0, 7);
        public UpdateField<uint> Expiration = new(0, 8);
        public UpdateField<uint> DynamicFlags = new(0, 9);
        public UpdateField<uint> Durability = new(0, 10);
        public UpdateField<uint> MaxDurability = new(0, 11);
        public UpdateField<uint> CreatePlayedTime = new(0, 12);
        public UpdateField<byte> Context = new(0, 13);
        public UpdateField<long> CreateTime = new(0, 14);
        public UpdateField<ulong> ArtifactXP = new(0, 15);
        public UpdateField<byte> ItemAppearanceModID = new(0, 16);
        public UpdateField<ItemModList> Modifiers = new(0, 17);
        public UpdateField<uint> ZoneFlags = new(0, 18);
        public UpdateField<ItemBonusKey> ItemBonusKey = new(0, 19);
        public UpdateField<ushort> DEBUGItemLevel = new(0, 20);
        public UpdateFieldArray<int> SpellCharges = new(5, 21, 22);
        public UpdateFieldArray<ItemEnchantment> Enchantment = new(13, 27, 28);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Item owner)
        {
            data.WritePackedGuid(Owner);
            data.WritePackedGuid(ContainedIn);
            data.WritePackedGuid(Creator);
            data.WritePackedGuid(GiftCreator);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt32(StackCount);
                data.WriteUInt32(Expiration);
                for (int i = 0; i < 5; ++i)
                {
                    data.WriteInt32(SpellCharges[i]);
                }
            }
            data.WriteUInt32(DynamicFlags);
            for (int i = 0; i < 13; ++i)
            {
                Enchantment[i].WriteCreate(data, receiver, owner);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt32(Durability);
                data.WriteUInt32(MaxDurability);
            }
            data.WriteUInt32(CreatePlayedTime);
            data.WriteUInt8(Context);
            data.WriteInt64(CreateTime);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt64(ArtifactXP);
                data.WriteUInt8(ItemAppearanceModID);
            }
            Modifiers.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(ArtifactPowers.Size());
            data.WriteInt32(Gems.Size());
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt32(ZoneFlags);
            }
            ItemBonusKey.GetValue().Write(data);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt16(DEBUGItemLevel);
            }
            for (int i = 0; i < ArtifactPowers.Size(); ++i)
            {
                ArtifactPowers[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < Gems.Size(); ++i)
            {
                Gems[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Item owner)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xF80A727Fu, 0x000001FFu]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(_changesMask & allowedMaskForTarget, data, receiver, owner, false);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x07F58D80u, 0x00000000u]));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xF80A727Fu, 0x000001FFu]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Item owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 2);
            for (uint i = 0; i < 2; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        ArtifactPowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ArtifactPowers.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        Gems.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Gems.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < ArtifactPowers.Size(); ++i)
                    {
                        if (ArtifactPowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ArtifactPowers[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < Gems.Size(); ++i)
                    {
                        if (Gems.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Gems[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[3])
                {
                    data.WritePackedGuid(Owner);
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(ContainedIn);
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(Creator);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(GiftCreator);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(StackCount);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(Expiration);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(DynamicFlags);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(Durability);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(MaxDurability);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(CreatePlayedTime);
                }
                if (changesMask[13])
                {
                    data.WriteUInt8(Context);
                }
                if (changesMask[14])
                {
                    data.WriteInt64(CreateTime);
                }
                if (changesMask[15])
                {
                    data.WriteUInt64(ArtifactXP);
                }
                if (changesMask[16])
                {
                    data.WriteUInt8(ItemAppearanceModID);
                }
                if (changesMask[17])
                {
                    Modifiers.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(ZoneFlags);
                }
                if (changesMask[19])
                {
                    ItemBonusKey.GetValue().Write(data);
                }
                if (changesMask[20])
                {
                    data.WriteUInt16(DEBUGItemLevel);
                }
            }
            if (changesMask[21])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[22 + i])
                    {
                        data.WriteInt32(SpellCharges[i]);
                    }
                }
            }
            if (changesMask[27])
            {
                for (int i = 0; i < 13; ++i)
                {
                    if (changesMask[28 + i])
                    {
                        Enchantment[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ArtifactPowers);
            ClearChangesMask(Gems);
            ClearChangesMask(Owner);
            ClearChangesMask(ContainedIn);
            ClearChangesMask(Creator);
            ClearChangesMask(GiftCreator);
            ClearChangesMask(StackCount);
            ClearChangesMask(Expiration);
            ClearChangesMask(DynamicFlags);
            ClearChangesMask(Durability);
            ClearChangesMask(MaxDurability);
            ClearChangesMask(CreatePlayedTime);
            ClearChangesMask(Context);
            ClearChangesMask(CreateTime);
            ClearChangesMask(ArtifactXP);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(Modifiers);
            ClearChangesMask(ZoneFlags);
            ClearChangesMask(ItemBonusKey);
            ClearChangesMask(DEBUGItemLevel);
            ClearChangesMask(SpellCharges);
            ClearChangesMask(Enchantment);
            _changesMask.ResetAll();
        }
    }

    public class ContainerData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Container, 101), IsUpdateFieldStructure<Bag>
    {
        public UpdateField<uint> NumSlots = new(0, 1);
        public UpdateFieldArray<ObjectGuid> Slots = new(98, 2, 3);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Bag owner)
        {
            for (int i = 0; i < 98; ++i)
            {
                data.WritePackedGuid(Slots[i]);
            }
            data.WriteUInt32(NumSlots);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Bag owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Item owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(_changesMask.GetBlocksMask(0), 4);
            for (uint i = 0; i < 4; ++i)
                if (_changesMask.GetBlock(i) != 0)
                    data.WriteBits(_changesMask.GetBlock(i), 32);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(NumSlots);
                }
            }
            if (_changesMask[2])
            {
                for (int i = 0; i < 98; ++i)
                {
                    if (_changesMask[3 + i])
                    {
                        data.WritePackedGuid(Slots[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(NumSlots);
            ClearChangesMask(Slots);
            _changesMask.ResetAll();
        }
    }

    public class AzeriteEmpoweredItemData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.AzeriteEmpoweredItem, 6), IsUpdateFieldStructure<Item>
    {
        public UpdateFieldArray<int> Selections = new(5, 0, 1);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, AzeriteEmpoweredItem owner)
        {
            for (int i = 0; i < 5; ++i)
            {
                data.WriteInt32(Selections[i]);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, AzeriteEmpoweredItem owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, AzeriteEmpoweredItem owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(_changesMask.GetBlocksMask(0), 1);
            if (_changesMask.GetBlock(0) != 0)
                data.WriteBits(_changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (_changesMask[0])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (_changesMask[1 + i])
                    {
                        data.WriteInt32(Selections[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Selections);
            _changesMask.ResetAll();
        }
    }

    public class UnlockedAzeriteEssence : IEquatable<UnlockedAzeriteEssence>, IsUpdateFieldStructure<AzeriteItem>
    {
        public uint AzeriteEssenceID;
        public uint Rank;

        public void WriteCreate(WorldPacket data, Player receiver, AzeriteItem owner)
        {
            data.WriteUInt32(AzeriteEssenceID);
            data.WriteUInt32(Rank);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AzeriteItem owner) => WriteCreate(data, receiver, owner);

        public bool Equals(UnlockedAzeriteEssence right)
        {
            return AzeriteEssenceID == right.AzeriteEssenceID
                && Rank == right.Rank;
        }
    }

    public class SelectedAzeriteEssences() : HasChangesMask(8), IsUpdateFieldStructure<AzeriteItem>
    {
        public UpdateField<bool> Enabled = new(0, 1);
        public UpdateField<uint> SpecializationID = new(0, 2);
        public UpdateFieldArray<uint> AzeriteEssenceID = new(4, 3, 4);

        public void WriteCreate(WorldPacket data, Player receiver, AzeriteItem owner)
        {
            for (int i = 0; i < 4; ++i)
            {
                data.WriteUInt32(AzeriteEssenceID[i]);
            }
            data.WriteUInt32(SpecializationID);
            data.WriteBit(Enabled);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AzeriteItem owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            if (changesMask.GetBlock(0) != 0)
                data.WriteBits(changesMask.GetBlock(0), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(Enabled);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    data.WriteUInt32(SpecializationID);
                }
            }
            if (changesMask[3])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[4 + i])
                    {
                        data.WriteUInt32(AzeriteEssenceID[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Enabled);
            ClearChangesMask(SpecializationID);
            ClearChangesMask(AzeriteEssenceID);
            _changesMask.ResetAll();
        }
    }

    public class AzeriteItemData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.AzeriteItem, 10), IsUpdateFieldStructure<AzeriteItem>
    {
        public UpdateField<bool> Enabled = new(0, 1);
        public DynamicUpdateField<UnlockedAzeriteEssence> UnlockedEssences = new(0, 2);
        public DynamicUpdateField<uint> UnlockedEssenceMilestones = new(0, 4);
        public DynamicUpdateField<SelectedAzeriteEssences> SelectedEssences = new(0, 3);
        public UpdateField<ulong> Xp = new(0, 5);
        public UpdateField<uint> Level = new(0, 6);
        public UpdateField<uint> AuraLevel = new(0, 7);
        public UpdateField<uint> KnowledgeLevel = new(0, 8);
        public UpdateField<int> DEBUGknowledgeWeek = new(0, 9);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, AzeriteItem owner)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt64(Xp);
                data.WriteUInt32(Level);
                data.WriteUInt32(AuraLevel);
                data.WriteUInt32(KnowledgeLevel);
                data.WriteInt32(DEBUGknowledgeWeek);
            }
            data.WriteInt32(UnlockedEssences.Size());
            data.WriteInt32(SelectedEssences.Size());
            data.WriteInt32(UnlockedEssenceMilestones.Size());
            for (int i = 0; i < UnlockedEssences.Size(); ++i)
            {
                UnlockedEssences[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < SelectedEssences.Size(); ++i)
            {
                SelectedEssences[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < UnlockedEssenceMilestones.Size(); ++i)
            {
                data.WriteUInt32(UnlockedEssenceMilestones[i]);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteBit(Enabled);
            }
            data.FlushBits();
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, AzeriteItem owner)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0x0000001Du]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(_changesMask & allowedMaskForTarget, data, receiver, owner, false);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x000003E2u]));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0x0000001Du]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, AzeriteItem owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 10);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(Enabled);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        UnlockedEssences.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(UnlockedEssences.Size(), data);
                }
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        SelectedEssences.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SelectedEssences.Size(), data);
                }
                if (changesMask[4])
                {
                    if (!ignoreNestedChangesMask)
                        UnlockedEssenceMilestones.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(UnlockedEssenceMilestones.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    for (int i = 0; i < UnlockedEssences.Size(); ++i)
                    {
                        if (UnlockedEssences.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            UnlockedEssences[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < SelectedEssences.Size(); ++i)
                    {
                        if (SelectedEssences.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SelectedEssences[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < UnlockedEssenceMilestones.Size(); ++i)
                    {
                        if (UnlockedEssenceMilestones.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(UnlockedEssenceMilestones[i]);
                        }
                    }
                }
                if (changesMask[5])
                {
                    data.WriteUInt64(Xp);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(AuraLevel);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(KnowledgeLevel);
                }
                if (changesMask[9])
                {
                    data.WriteInt32(DEBUGknowledgeWeek);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Enabled);
            ClearChangesMask(UnlockedEssences);
            ClearChangesMask(SelectedEssences);
            ClearChangesMask(UnlockedEssenceMilestones);
            ClearChangesMask(Xp);
            ClearChangesMask(Level);
            ClearChangesMask(AuraLevel);
            ClearChangesMask(KnowledgeLevel);
            ClearChangesMask(DEBUGknowledgeWeek);
            _changesMask.ResetAll();
        }
    }

    public class SpellCastVisualField : IEquatable<SpellCastVisual>, IsUpdateFieldStructure<WorldObject>
    {
        public uint SpellXSpellVisualID;
        public uint ScriptVisualID;

        public void WriteCreate(WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteUInt32(ScriptVisualID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, WorldObject owner) => WriteCreate(data, receiver, owner);

        public bool Equals(SpellCastVisual right)
        {
            return SpellXSpellVisualID == right.SpellXSpellVisualID
                && ScriptVisualID == right.ScriptVisualID;
        }
    }

    public class UnitChannel : IEquatable<UnitChannel>, IsUpdateFieldStructure<Unit>
    {
        public uint SpellID;
        public SpellCastVisualField SpellVisual = new();
        public uint StartTimeMs;
        public uint Duration;

        public void WriteCreate(WorldPacket data, Player receiver, Unit owner)
        {
            data.WriteUInt32(SpellID);
            SpellVisual.WriteCreate(data, receiver, owner);
            data.WriteUInt32(StartTimeMs);
            data.WriteUInt32(Duration);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Unit owner) => WriteCreate(data, receiver, owner);

        public bool Equals(UnitChannel right)
        {
            return SpellID == right.SpellID
                && SpellVisual == right.SpellVisual
                && StartTimeMs == right.StartTimeMs
                && Duration == right.Duration;
        }
    }

    public class VisibleItem() : HasChangesMask(11), IsUpdateFieldStructure<Unit>
    {
        public UpdateField<bool> HasTransmog = new(0, 1);
        public UpdateField<bool> HasIllusion = new(0, 2);
        public UpdateField<uint> ItemID = new(0, 3);
        public UpdateField<uint> SecondaryItemModifiedAppearanceID = new(0, 4);
        public UpdateField<int> ConditionalItemAppearanceID = new(0, 5);
        public UpdateField<ushort> ItemAppearanceModID = new(0, 6);
        public UpdateField<ushort> ItemVisual = new(0, 7);
        public UpdateField<uint> ItemModifiedAppearanceID = new(0, 8);
        public UpdateField<byte> TransmogSlotOption = new(0, 9);
        public UpdateField<byte> SheatheCategory = new(0, 10);

        public void WriteCreate(WorldPacket data, Player receiver, Unit owner)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(SecondaryItemModifiedAppearanceID);
            data.WriteInt32(ConditionalItemAppearanceID);
            data.WriteUInt16(ItemAppearanceModID);
            data.WriteUInt16(ItemVisual);
            data.WriteUInt32(ItemModifiedAppearanceID);
            data.WriteUInt8(TransmogSlotOption);
            data.WriteUInt8(SheatheCategory);
            data.WriteBit(HasTransmog);
            data.WriteBit(HasIllusion);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Unit owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 11);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(HasTransmog);
                }
                if (changesMask[2])
                {
                    data.WriteBit(HasIllusion);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    data.WriteUInt32(ItemID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(SecondaryItemModifiedAppearanceID);
                }
                if (changesMask[5])
                {
                    data.WriteInt32(ConditionalItemAppearanceID);
                }
                if (changesMask[6])
                {
                    data.WriteUInt16(ItemAppearanceModID);
                }
                if (changesMask[7])
                {
                    data.WriteUInt16(ItemVisual);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(ItemModifiedAppearanceID);
                }
                if (changesMask[9])
                {
                    data.WriteUInt8(TransmogSlotOption);
                }
                if (changesMask[10])
                {
                    data.WriteUInt8(SheatheCategory);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(HasTransmog);
            ClearChangesMask(HasIllusion);
            ClearChangesMask(ItemID);
            ClearChangesMask(SecondaryItemModifiedAppearanceID);
            ClearChangesMask(ConditionalItemAppearanceID);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(ItemVisual);
            ClearChangesMask(ItemModifiedAppearanceID);
            ClearChangesMask(TransmogSlotOption);
            ClearChangesMask(SheatheCategory);
            _changesMask.ResetAll();
        }
    }

    public class PassiveSpellHistory : IEquatable<PassiveSpellHistory>, IsUpdateFieldStructure<Unit>
    {
        public int SpellID;
        public int AuraSpellID;

        public void WriteCreate(WorldPacket data, Player receiver, Unit owner)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(AuraSpellID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Unit owner) => WriteCreate(data, receiver, owner);

        public bool Equals(PassiveSpellHistory right)
        {
            return SpellID == right.SpellID
                && AuraSpellID == right.AuraSpellID;
        }
    }

    public class UnitAssistActionData() : HasChangesMask(4), IsUpdateFieldStructure<Unit>
    {
        public UpdateField<byte> Type = new(0, 1);
        public UpdateFieldString PlayerName = new(0, 2);
        public UpdateField<uint> VirtualRealmAddress = new(0, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Unit owner)
        {
            data.WriteUInt8(Type);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteBits(PlayerName.Size(), 6);
            data.FlushBits();
            data.WriteString(PlayerName);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Unit owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt8(Type);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(VirtualRealmAddress);
                }
                if (changesMask[2])
                {
                    data.WriteBits(PlayerName.Size(), 6);
                    data.FlushBits();
                    data.WriteString(PlayerName);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Type);
            ClearChangesMask(PlayerName);
            ClearChangesMask(VirtualRealmAddress);
            _changesMask.ResetAll();
        }
    }

    public class UnitData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Unit, 230), IsUpdateFieldStructure<Unit>
    {
        public UpdateField<bool> Field_314 = new(0, 1);
        public UpdateField<List<uint>> StateWorldEffectIDs = new(0, 2);
        public DynamicUpdateField<PassiveSpellHistory> PassiveSpells = new(0, 3);
        public DynamicUpdateField<int> WorldEffects = new(0, 4);
        public DynamicUpdateField<ObjectGuid> ChannelObjects = new(0, 5);
        public UpdateField<uint> DisplayID = new(0, 6);
        public UpdateField<uint> NpcFlags = new(0, 7);
        public UpdateField<uint> NpcFlags2 = new(0, 8);
        public UpdateField<uint> StateSpellVisualID = new(0, 9);
        public UpdateField<uint> StateAnimID = new(0, 10);
        public UpdateField<uint> StateAnimKitID = new(0, 11);
        public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new(0, 12);
        public UpdateField<int> SpellOverrideNameID = new(0, 13);
        public UpdateField<ObjectGuid> Charm = new(0, 14);
        public UpdateField<ObjectGuid> Summon = new(0, 15);
        public UpdateField<ObjectGuid> Critter = new(0, 16);
        public UpdateField<ObjectGuid> CharmedBy = new(0, 17);
        public UpdateField<ObjectGuid> SummonedBy = new(0, 18);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 19);
        public UpdateField<ObjectGuid> DemonCreator = new(0, 20);
        public UpdateField<ObjectGuid> LookAtControllerTarget = new(0, 21);
        public UpdateField<ObjectGuid> Target = new(0, 22);
        public UpdateField<ObjectGuid> BattlePetCompanionGUID = new(0, 23);
        public UpdateField<ulong> BattlePetDBID = new(0, 24);
        public UpdateField<ObjectGuid> BattlePetAttachedToDecorGUID = new(0, 25);
        public UpdateField<ObjectGuid> BattlePetDecorHouseGUID = new(0, 26);
        public UpdateField<UnitChannel> ChannelData = new(0, 27);
        public UpdateField<sbyte> SpellEmpowerStage = new(0, 28);
        public UpdateField<uint> SummonedByHomeRealm = new(0, 29);
        public UpdateField<byte> Race = new(0, 30);
        public UpdateField<byte> ClassId = new(0, 31);
        public UpdateField<byte> PlayerClassId = new(32, 33);
        public UpdateField<byte> Sex = new(32, 34);
        public UpdateField<byte> CreatureType = new(32, 35);
        public UpdateField<byte> DisplayPower = new(32, 36);
        public UpdateField<uint> OverrideDisplayPowerID = new(32, 37);
        public UpdateField<ulong> Health = new(32, 38);
        public UpdateField<ulong> MaxHealth = new(32, 39);
        public UpdateField<uint> Level = new(32, 40);
        public UpdateField<uint> EffectiveLevel = new(32, 41);
        public UpdateField<uint> ContentTuningID = new(32, 42);
        public UpdateField<int> ScalingLevelMin = new(32, 43);
        public UpdateField<int> ScalingLevelMax = new(32, 44);
        public UpdateField<int> ScalingLevelDelta = new(32, 45);
        public UpdateField<byte> ScalingFactionGroup = new(32, 46);
        public UpdateField<uint> FactionTemplate = new(32, 47);
        public UpdateField<uint> Flags = new(32, 48);
        public UpdateField<uint> Flags2 = new(32, 49);
        public UpdateField<uint> Flags3 = new(32, 50);
        public UpdateField<uint> Flags4 = new(32, 51);
        public UpdateField<uint> AuraState = new(32, 52);
        public UpdateField<uint> RangedAttackRoundBaseTime = new(32, 53);
        public UpdateField<float> BoundingRadius = new(32, 54);
        public UpdateField<float> CombatReach = new(32, 55);
        public UpdateField<float> DisplayScale = new(32, 56);
        public UpdateField<int> CreatureFamily = new(32, 57);
        public UpdateField<byte> OverrideCreatureType = new(32, 58);
        public UpdateField<uint> NativeDisplayID = new(32, 59);
        public UpdateField<float> NativeXDisplayScale = new(32, 60);
        public UpdateField<uint> MountDisplayID = new(32, 61);
        public UpdateField<uint> CosmeticMountDisplayID = new(32, 62);
        public UpdateField<float> MinDamage = new(32, 63);
        public UpdateField<float> MaxDamage = new(64, 65);
        public UpdateField<float> MinOffHandDamage = new(64, 66);
        public UpdateField<float> MaxOffHandDamage = new(64, 67);
        public UpdateField<byte> StandState = new(64, 68);
        public UpdateField<byte> PetTalentPoints = new(64, 69);
        public UpdateField<byte> VisFlags = new(64, 70);
        public UpdateField<byte> AnimTier = new(64, 71);
        public UpdateField<uint> PetNumber = new(64, 72);
        public UpdateField<uint> PetNameTimestamp = new(64, 73);
        public UpdateField<uint> PetExperience = new(64, 74);
        public UpdateField<uint> PetNextLevelExperience = new(64, 75);
        public UpdateField<float> ModCastingSpeed = new(64, 76);
        public UpdateField<float> ModCastingSpeedNeg = new(64, 77);
        public UpdateField<float> ModSpellHaste = new(64, 78);
        public UpdateField<float> ModHaste = new(64, 79);
        public UpdateField<float> ModRangedHaste = new(64, 80);
        public UpdateField<float> ModHasteRegen = new(64, 81);
        public UpdateField<float> ModTimeRate = new(64, 82);
        public UpdateField<uint> CreatedBySpell = new(64, 83);
        public UpdateField<int> EmoteState = new(64, 84);
        public UpdateField<uint> BaseMana = new(64, 85);
        public UpdateField<uint> BaseHealth = new(64, 86);
        public UpdateField<byte> SheatheState = new(64, 87);
        public UpdateField<byte> PvpFlags = new(64, 88);
        public UpdateField<byte> PetFlags = new(64, 89);
        public UpdateField<byte> ShapeshiftForm = new(64, 90);
        public UpdateField<int> AttackPower = new(64, 91);
        public UpdateField<int> AttackPowerModPos = new(64, 92);
        public UpdateField<int> AttackPowerModNeg = new(64, 93);
        public UpdateField<float> AttackPowerMultiplier = new(64, 94);
        public UpdateField<int> AttackPowerModSupport = new(64, 95);
        public UpdateField<int> RangedAttackPower = new(96, 97);
        public UpdateField<int> RangedAttackPowerModPos = new(96, 98);
        public UpdateField<int> RangedAttackPowerModNeg = new(96, 99);
        public UpdateField<float> RangedAttackPowerMultiplier = new(96, 100);
        public UpdateField<int> RangedAttackPowerModSupport = new(96, 101);
        public UpdateField<int> MainHandWeaponAttackPower = new(96, 102);
        public UpdateField<int> OffHandWeaponAttackPower = new(96, 103);
        public UpdateField<int> RangedWeaponAttackPower = new(96, 104);
        public UpdateField<int> SetAttackSpeedAura = new(96, 105);
        public UpdateField<float> Lifesteal = new(96, 106);
        public UpdateField<float> MinRangedDamage = new(96, 107);
        public UpdateField<float> MaxRangedDamage = new(96, 108);
        public UpdateField<float> ManaCostMultiplier = new(96, 109);
        public UpdateField<float> MaxHealthModifier = new(96, 110);
        public UpdateField<float> HoverHeight = new(96, 111);
        public UpdateField<uint> MinItemLevelCutoff = new(96, 112);
        public UpdateField<uint> MinItemLevel = new(96, 113);
        public UpdateField<uint> MaxItemLevel = new(96, 114);
        public UpdateField<uint> AzeriteItemLevel = new(96, 115);
        public UpdateField<uint> WildBattlePetLevel = new(96, 116);
        public UpdateField<uint> BattlePetCompanionExperience = new(96, 117);
        public UpdateField<uint> BattlePetCompanionNameTimestamp = new(96, 118);
        public UpdateField<int> InteractSpellID = new(96, 119);
        public UpdateField<int> ScaleDuration = new(96, 120);
        public UpdateField<int> LooksLikeMountID = new(96, 121);
        public UpdateField<int> LooksLikeCreatureID = new(96, 122);
        public UpdateField<int> LookAtControllerID = new(96, 123);
        public UpdateField<int> PerksVendorItemID = new(96, 124);
        public UpdateField<int> TaxiNodesID = new(96, 125);
        public UpdateField<ObjectGuid> GuildGUID = new(96, 126);
        public UpdateField<int> FlightCapabilityID = new(96, 127);
        public UpdateField<float> GlideEventSpeedDivisor = new(128, 129);                        // Movement speed gets divided by this value when evaluating what GlideEvents to use
        public UpdateField<int> DriveCapabilityID = new(128, 130);
        public UpdateField<int> MaxHealthModifierFlatNeg = new(128, 131);
        public UpdateField<int> MaxHealthModifierFlatPos = new(128, 132);
        public UpdateField<uint> SilencedSchoolMask = new(128, 133);
        public UpdateField<uint> CurrentAreaID = new(128, 134);
        public UpdateField<float> NameplateDistanceMod = new(128, 135);
        public UpdateField<float> AutoAttackRangeMod = new(128, 136);                            // When UnitFlags3 & 0x40000000 is set, increases some autoattack range by this amount
        public UpdateField<ObjectGuid> NameplateAttachToGUID = new(128, 137);                    // When set, nameplate of this unit will instead appear on that object
        public OptionalUpdateField<UnitAssistActionData> AssistActionData = new(128, 138);
        public UpdateFieldArray<int> Power = new(10, 139, 140);
        public UpdateFieldArray<uint> MaxPower = new(10, 139, 150);
        public UpdateFieldArray<float> PowerRegenFlatModifier = new(10, 139, 160);
        public UpdateFieldArray<float> PowerRegenInterruptedFlatModifier = new(10, 139, 170);
        public UpdateFieldArray<VisibleItem> VirtualItems = new(3, 180, 181);
        public UpdateFieldArray<uint> AttackRoundBaseTime = new(2, 184, 185);
        public UpdateFieldArray<int> Stats = new(5, 187, 188);
        public UpdateFieldArray<int> StatPosBuff = new(5, 187, 193);
        public UpdateFieldArray<int> StatNegBuff = new(5, 187, 198);
        public UpdateFieldArray<int> StatSupportBuff = new(5, 187, 203);
        public UpdateFieldArray<int> Resistances = new(7, 208, 209);
        public UpdateFieldArray<int> BonusResistanceMods = new(7, 208, 216);
        public UpdateFieldArray<int> ManaCostModifier = new(7, 208, 223);


        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Unit owner)
        {
            data.WriteUInt32(GetViewerDependentDisplayId(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentNpcFlags(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentNpcFlags2(this, receiver, owner));

            data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentStateAnimID(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentStateAnimKitID(this, receiver, owner));

            var stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, receiver, owner);
            data.WriteInt32(stateWorldEffectIDs.Count);
            data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, receiver, owner));
            data.WriteInt32(SpellOverrideNameID);
            for (int i = 0; i < stateWorldEffectIDs.Count; ++i)
                data.WriteUInt32(stateWorldEffectIDs[i]);

            data.WritePackedGuid(Charm);
            data.WritePackedGuid(Summon);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WritePackedGuid(Critter);

            data.WritePackedGuid(CharmedBy);
            data.WritePackedGuid(SummonedBy);
            data.WritePackedGuid(CreatedBy);
            data.WritePackedGuid(DemonCreator);
            data.WritePackedGuid(LookAtControllerTarget);
            data.WritePackedGuid(Target);
            data.WritePackedGuid(BattlePetCompanionGUID);
            data.WriteUInt64(BattlePetDBID);
            data.WritePackedGuid(BattlePetAttachedToDecorGUID);
            data.WritePackedGuid(BattlePetDecorHouseGUID);
            ChannelData.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt8(SpellEmpowerStage);
            data.WriteUInt32(SummonedByHomeRealm);
            data.WriteUInt8(Race);
            data.WriteUInt8(ClassId);
            data.WriteUInt8(PlayerClassId);
            data.WriteUInt8(Sex);
            data.WriteUInt8(CreatureType);
            data.WriteUInt8(DisplayPower);
            data.WriteUInt32(OverrideDisplayPowerID);
            data.WriteUInt64(Health);
            for (int i = 0; i < 10; ++i)
            {
                data.WriteInt32(Power[i]);
                data.WriteUInt32(MaxPower[i]);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
            {
                for (int i = 0; i < 10; ++i)
                {
                    data.WriteFloat(PowerRegenFlatModifier[i]);
                    data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
                }
            }
            data.WriteUInt64(MaxHealth);
            data.WriteUInt32(Level);
            data.WriteUInt32(EffectiveLevel);
            data.WriteUInt32(ContentTuningID);
            data.WriteInt32(ScalingLevelMin);
            data.WriteInt32(ScalingLevelMax);
            data.WriteInt32(ScalingLevelDelta);
            data.WriteUInt8(ScalingFactionGroup);
            data.WriteUInt32(GetViewerDependentFactionTemplate(this, receiver, owner));
            for (int i = 0; i < 3; ++i)
            {
                VirtualItems[i].WriteCreate(data, receiver, owner);
            }
            data.WriteUInt32(GetViewerDependentFlags(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentFlags2(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentFlags3(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentFlags4(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentAuraState(this, receiver, owner));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(AttackRoundBaseTime[i]);

            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WriteUInt32(RangedAttackRoundBaseTime);

            data.WriteFloat(BoundingRadius);
            data.WriteFloat(CombatReach);
            data.WriteFloat(DisplayScale);
            data.WriteInt32(CreatureFamily);
            data.WriteUInt8(OverrideCreatureType);
            data.WriteUInt32(NativeDisplayID);
            data.WriteFloat(NativeXDisplayScale);
            data.WriteUInt32(MountDisplayID);
            data.WriteUInt32(CosmeticMountDisplayID);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
            {
                data.WriteFloat(MinDamage);
                data.WriteFloat(MaxDamage);
                data.WriteFloat(MinOffHandDamage);
                data.WriteFloat(MaxOffHandDamage);
            }
            data.WriteUInt8(StandState);
            data.WriteUInt8(PetTalentPoints);
            data.WriteUInt8(VisFlags);
            data.WriteUInt8(AnimTier);
            data.WriteUInt32(PetNumber);
            data.WriteUInt32(PetNameTimestamp);
            data.WriteUInt32(PetExperience);
            data.WriteUInt32(PetNextLevelExperience);
            data.WriteFloat(ModCastingSpeed);
            data.WriteFloat(ModCastingSpeedNeg);
            data.WriteFloat(ModSpellHaste);
            data.WriteFloat(ModHaste);
            data.WriteFloat(ModRangedHaste);
            data.WriteFloat(ModHasteRegen);
            data.WriteFloat(ModTimeRate);
            data.WriteUInt32(CreatedBySpell);
            data.WriteInt32(EmoteState);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                for (int i = 0; i < 5; ++i)
                {
                    data.WriteInt32(Stats[i]);
                    data.WriteInt32(StatPosBuff[i]);
                    data.WriteInt32(StatNegBuff[i]);
                    data.WriteInt32(StatSupportBuff[i]);
                }
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
            {
                for (int i = 0; i < 7; ++i)
                {
                    data.WriteInt32(Resistances[i]);
                }
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                for (int i = 0; i < 7; ++i)
                {
                    data.WriteInt32(BonusResistanceMods[i]);
                    data.WriteInt32(ManaCostModifier[i]);
                }
            }
            data.WriteUInt32(BaseMana);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WriteUInt32(BaseHealth);

            data.WriteUInt8(SheatheState);
            data.WriteUInt8(GetViewerDependentPvpFlags(this, receiver, owner));
            data.WriteUInt8(PetFlags);
            data.WriteUInt8(ShapeshiftForm);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteInt32(AttackPower);
                data.WriteInt32(AttackPowerModPos);
                data.WriteInt32(AttackPowerModNeg);
                data.WriteFloat(AttackPowerMultiplier);
                data.WriteInt32(AttackPowerModSupport);
                data.WriteInt32(RangedAttackPower);
                data.WriteInt32(RangedAttackPowerModPos);
                data.WriteInt32(RangedAttackPowerModNeg);
                data.WriteFloat(RangedAttackPowerMultiplier);
                data.WriteInt32(RangedAttackPowerModSupport);
                data.WriteInt32(MainHandWeaponAttackPower);
                data.WriteInt32(OffHandWeaponAttackPower);
                data.WriteInt32(RangedWeaponAttackPower);
                data.WriteInt32(SetAttackSpeedAura);
                data.WriteFloat(Lifesteal);
                data.WriteFloat(MinRangedDamage);
                data.WriteFloat(MaxRangedDamage);
                data.WriteFloat(ManaCostMultiplier);
            }
            data.WriteFloat(MaxHealthModifier);
            data.WriteFloat(HoverHeight);
            data.WriteUInt32(MinItemLevelCutoff);
            data.WriteUInt32(MinItemLevel);
            data.WriteUInt32(MaxItemLevel);
            data.WriteUInt32(AzeriteItemLevel);
            data.WriteUInt32(WildBattlePetLevel);
            data.WriteUInt32(BattlePetCompanionExperience);
            data.WriteUInt32(BattlePetCompanionNameTimestamp);
            data.WriteInt32(GetViewerDependentInteractSpellId(this, receiver, owner));
            data.WriteInt32(ScaleDuration);
            data.WriteInt32(LooksLikeMountID);
            data.WriteInt32(LooksLikeCreatureID);
            data.WriteInt32(LookAtControllerID);
            data.WriteInt32(PerksVendorItemID);
            data.WriteInt32(TaxiNodesID);
            data.WritePackedGuid(GuildGUID);
            data.WriteInt32(PassiveSpells.Size());
            data.WriteInt32(WorldEffects.Size());
            data.WriteInt32(ChannelObjects.Size());
            data.WriteInt32(FlightCapabilityID);
            data.WriteFloat(GlideEventSpeedDivisor);
            data.WriteInt32(DriveCapabilityID);
            data.WriteInt32(MaxHealthModifierFlatNeg);
            data.WriteInt32(MaxHealthModifierFlatPos);
            data.WriteUInt32(SilencedSchoolMask);
            data.WriteUInt32(CurrentAreaID);
            data.WriteFloat(NameplateDistanceMod);
            data.WriteFloat(AutoAttackRangeMod);
            data.WritePackedGuid(NameplateAttachToGUID);

            for (int i = 0; i < PassiveSpells.Size(); ++i)
                PassiveSpells[i].WriteCreate(data, receiver, owner);

            for (int i = 0; i < WorldEffects.Size(); ++i)
                data.WriteInt32(WorldEffects[i]);

            for (int i = 0; i < ChannelObjects.Size(); ++i)
                data.WritePackedGuid(ChannelObjects[i]);

            data.WriteBit(Field_314);
            data.WriteBit(AssistActionData.HasValue());
            data.FlushBits();
            if (AssistActionData.HasValue())
            {
                AssistActionData.GetValue().WriteCreate(data, receiver, owner);
            }
        }

        public void UnitDataAppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00010000u, 0x80200000u, 0xF840000Eu, 0x00003FFEu, 0x00000800u, 0xF80FFFFFu, 0xFFFFFFFFu, 0x0000003Fu]));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000800u, 0x000FFFFFu, 0x00000000u, 0x00000000u]));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00000000u, 0x80000000u, 0x0000000Eu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00FF0000u, 0x00000000u]));
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            UnitDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xFFFEFFFFu, 0x7FDFFFFFu, 0x07BFFFF1u, 0xFFFFC001u, 0xFFFFFFFFu, 0x07F00000u, 0x00000000u, 0x00000000u]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Unit owner)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xFFFEFFFFu, 0x7FDFFFFFu, 0x07BFFFF1u, 0xFFFFC001u, 0xFFFFFFFFu, 0x07F00000u, 0x00000000u, 0x00000000u]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(_changesMask & allowedMaskForTarget, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Unit owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 8);
            for (uint i = 0; i < 8; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            List<uint> stateWorldEffectIDs = [];

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(Field_314);
                }
                if (changesMask[2])
                {
                    stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, receiver, owner);
                    data.WriteBits(stateWorldEffectIDs.Count, 32);
                    for (int i = 0; i < stateWorldEffectIDs.Count; ++i)
                    {
                        data.WriteUInt32(stateWorldEffectIDs[i]);
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        PassiveSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(PassiveSpells.Size(), data);
                }
                if (changesMask[4])
                {
                    if (!ignoreNestedChangesMask)
                        WorldEffects.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(WorldEffects.Size(), data);
                }
                if (changesMask[5])
                {
                    if (!ignoreNestedChangesMask)
                        ChannelObjects.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ChannelObjects.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    for (int i = 0; i < PassiveSpells.Size(); ++i)
                    {
                        if (PassiveSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PassiveSpells[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < WorldEffects.Size(); ++i)
                    {
                        if (WorldEffects.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(WorldEffects[i]);
                        }
                    }
                }
                if (changesMask[5])
                {
                    for (int i = 0; i < ChannelObjects.Size(); ++i)
                    {
                        if (ChannelObjects.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(ChannelObjects[i]);
                        }
                    }
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(GetViewerDependentDisplayId(this, receiver, owner));
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(GetViewerDependentNpcFlags(this, receiver, owner));
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(GetViewerDependentNpcFlags2(this, receiver, owner));
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, receiver, owner));
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(GetViewerDependentStateAnimID(this, receiver, owner));
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(GetViewerDependentStateAnimKitID(this, receiver, owner));
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, receiver, owner));
                }
                if (changesMask[13])
                {
                    data.WriteInt32(SpellOverrideNameID);
                }
                if (changesMask[14])
                {
                    data.WritePackedGuid(Charm);
                }
                if (changesMask[15])
                {
                    data.WritePackedGuid(Summon);
                }
                if (changesMask[16])
                {
                    data.WritePackedGuid(Critter);
                }
                if (changesMask[17])
                {
                    data.WritePackedGuid(CharmedBy);
                }
                if (changesMask[18])
                {
                    data.WritePackedGuid(SummonedBy);
                }
                if (changesMask[19])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (changesMask[20])
                {
                    data.WritePackedGuid(DemonCreator);
                }
                if (changesMask[21])
                {
                    data.WritePackedGuid(LookAtControllerTarget);
                }
                if (changesMask[22])
                {
                    data.WritePackedGuid(Target);
                }
                if (changesMask[23])
                {
                    data.WritePackedGuid(BattlePetCompanionGUID);
                }
                if (changesMask[24])
                {
                    data.WriteUInt64(BattlePetDBID);
                }
                if (changesMask[25])
                {
                    data.WritePackedGuid(BattlePetAttachedToDecorGUID);
                }
                if (changesMask[26])
                {
                    data.WritePackedGuid(BattlePetDecorHouseGUID);
                }
                if (changesMask[27])
                {
                    ChannelData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[28])
                {
                    data.WriteInt8(SpellEmpowerStage);
                }
                if (changesMask[29])
                {
                    data.WriteUInt32(SummonedByHomeRealm);
                }
                if (changesMask[30])
                {
                    data.WriteUInt8(Race);
                }
                if (changesMask[31])
                {
                    data.WriteUInt8(ClassId);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt8(PlayerClassId);
                }
                if (changesMask[34])
                {
                    data.WriteUInt8(Sex);
                }
                if (changesMask[35])
                {
                    data.WriteUInt8(CreatureType);
                }
                if (changesMask[36])
                {
                    data.WriteUInt8(DisplayPower);
                }
                if (changesMask[37])
                {
                    data.WriteUInt32(OverrideDisplayPowerID);
                }
                if (changesMask[38])
                {
                    data.WriteUInt64(Health);
                }
                if (changesMask[39])
                {
                    data.WriteUInt64(MaxHealth);
                }
                if (changesMask[40])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[41])
                {
                    data.WriteUInt32(EffectiveLevel);
                }
                if (changesMask[42])
                {
                    data.WriteUInt32(ContentTuningID);
                }
                if (changesMask[43])
                {
                    data.WriteInt32(ScalingLevelMin);
                }
                if (changesMask[44])
                {
                    data.WriteInt32(ScalingLevelMax);
                }
                if (changesMask[45])
                {
                    data.WriteInt32(ScalingLevelDelta);
                }
                if (changesMask[46])
                {
                    data.WriteUInt8(ScalingFactionGroup);
                }
                if (changesMask[47])
                {
                    data.WriteUInt32(GetViewerDependentFactionTemplate(this, receiver, owner));
                }
                if (changesMask[48])
                {
                    data.WriteUInt32(GetViewerDependentFlags(this, receiver, owner));
                }
                if (changesMask[49])
                {
                    data.WriteUInt32(GetViewerDependentFlags2(this, receiver, owner));
                }
                if (changesMask[50])
                {
                    data.WriteUInt32(GetViewerDependentFlags3(this, receiver, owner));
                }
                if (changesMask[51])
                {
                    data.WriteUInt32(GetViewerDependentFlags4(this, receiver, owner));
                }
                if (changesMask[52])
                {
                    data.WriteUInt32(GetViewerDependentAuraState(this, receiver, owner));
                }
                if (changesMask[53])
                {
                    data.WriteUInt32(RangedAttackRoundBaseTime);
                }
                if (changesMask[54])
                {
                    data.WriteFloat(BoundingRadius);
                }
                if (changesMask[55])
                {
                    data.WriteFloat(CombatReach);
                }
                if (changesMask[56])
                {
                    data.WriteFloat(DisplayScale);
                }
                if (changesMask[57])
                {
                    data.WriteInt32(CreatureFamily);
                }
                if (changesMask[58])
                {
                    data.WriteUInt8(OverrideCreatureType);
                }
                if (changesMask[59])
                {
                    data.WriteUInt32(NativeDisplayID);
                }
                if (changesMask[60])
                {
                    data.WriteFloat(NativeXDisplayScale);
                }
                if (changesMask[61])
                {
                    data.WriteUInt32(MountDisplayID);
                }
                if (changesMask[62])
                {
                    data.WriteUInt32(CosmeticMountDisplayID);
                }
                if (changesMask[63])
                {
                    data.WriteFloat(MinDamage);
                }
            }
            if (changesMask[64])
            {
                if (changesMask[65])
                {
                    data.WriteFloat(MaxDamage);
                }
                if (changesMask[66])
                {
                    data.WriteFloat(MinOffHandDamage);
                }
                if (changesMask[67])
                {
                    data.WriteFloat(MaxOffHandDamage);
                }
                if (changesMask[68])
                {
                    data.WriteUInt8(StandState);
                }
                if (changesMask[69])
                {
                    data.WriteUInt8(PetTalentPoints);
                }
                if (changesMask[70])
                {
                    data.WriteUInt8(VisFlags);
                }
                if (changesMask[71])
                {
                    data.WriteUInt8(AnimTier);
                }
                if (changesMask[72])
                {
                    data.WriteUInt32(PetNumber);
                }
                if (changesMask[73])
                {
                    data.WriteUInt32(PetNameTimestamp);
                }
                if (changesMask[74])
                {
                    data.WriteUInt32(PetExperience);
                }
                if (changesMask[75])
                {
                    data.WriteUInt32(PetNextLevelExperience);
                }
                if (changesMask[76])
                {
                    data.WriteFloat(ModCastingSpeed);
                }
                if (changesMask[77])
                {
                    data.WriteFloat(ModCastingSpeedNeg);
                }
                if (changesMask[78])
                {
                    data.WriteFloat(ModSpellHaste);
                }
                if (changesMask[79])
                {
                    data.WriteFloat(ModHaste);
                }
                if (changesMask[80])
                {
                    data.WriteFloat(ModRangedHaste);
                }
                if (changesMask[81])
                {
                    data.WriteFloat(ModHasteRegen);
                }
                if (changesMask[82])
                {
                    data.WriteFloat(ModTimeRate);
                }
                if (changesMask[83])
                {
                    data.WriteUInt32(CreatedBySpell);
                }
                if (changesMask[84])
                {
                    data.WriteInt32(EmoteState);
                }
                if (changesMask[85])
                {
                    data.WriteUInt32(BaseMana);
                }
                if (changesMask[86])
                {
                    data.WriteUInt32(BaseHealth);
                }
                if (changesMask[87])
                {
                    data.WriteUInt8(SheatheState);
                }
                if (changesMask[88])
                {
                    data.WriteUInt8(GetViewerDependentPvpFlags(this, receiver, owner));
                }
                if (changesMask[89])
                {
                    data.WriteUInt8(PetFlags);
                }
                if (changesMask[90])
                {
                    data.WriteUInt8(ShapeshiftForm);
                }
                if (changesMask[91])
                {
                    data.WriteInt32(AttackPower);
                }
                if (changesMask[92])
                {
                    data.WriteInt32(AttackPowerModPos);
                }
                if (changesMask[93])
                {
                    data.WriteInt32(AttackPowerModNeg);
                }
                if (changesMask[94])
                {
                    data.WriteFloat(AttackPowerMultiplier);
                }
                if (changesMask[95])
                {
                    data.WriteInt32(AttackPowerModSupport);
                }
            }
            if (changesMask[96])
            {
                if (changesMask[97])
                {
                    data.WriteInt32(RangedAttackPower);
                }
                if (changesMask[98])
                {
                    data.WriteInt32(RangedAttackPowerModPos);
                }
                if (changesMask[99])
                {
                    data.WriteInt32(RangedAttackPowerModNeg);
                }
                if (changesMask[100])
                {
                    data.WriteFloat(RangedAttackPowerMultiplier);
                }
                if (changesMask[101])
                {
                    data.WriteInt32(RangedAttackPowerModSupport);
                }
                if (changesMask[102])
                {
                    data.WriteInt32(MainHandWeaponAttackPower);
                }
                if (changesMask[103])
                {
                    data.WriteInt32(OffHandWeaponAttackPower);
                }
                if (changesMask[104])
                {
                    data.WriteInt32(RangedWeaponAttackPower);
                }
                if (changesMask[105])
                {
                    data.WriteInt32(SetAttackSpeedAura);
                }
                if (changesMask[106])
                {
                    data.WriteFloat(Lifesteal);
                }
                if (changesMask[107])
                {
                    data.WriteFloat(MinRangedDamage);
                }
                if (changesMask[108])
                {
                    data.WriteFloat(MaxRangedDamage);
                }
                if (changesMask[109])
                {
                    data.WriteFloat(ManaCostMultiplier);
                }
                if (changesMask[110])
                {
                    data.WriteFloat(MaxHealthModifier);
                }
                if (changesMask[111])
                {
                    data.WriteFloat(HoverHeight);
                }
                if (changesMask[112])
                {
                    data.WriteUInt32(MinItemLevelCutoff);
                }
                if (changesMask[113])
                {
                    data.WriteUInt32(MinItemLevel);
                }
                if (changesMask[114])
                {
                    data.WriteUInt32(MaxItemLevel);
                }
                if (changesMask[115])
                {
                    data.WriteUInt32(AzeriteItemLevel);
                }
                if (changesMask[116])
                {
                    data.WriteUInt32(WildBattlePetLevel);
                }
                if (changesMask[117])
                {
                    data.WriteUInt32(BattlePetCompanionExperience);
                }
                if (changesMask[118])
                {
                    data.WriteUInt32(BattlePetCompanionNameTimestamp);
                }
                if (changesMask[119])
                {
                    data.WriteInt32(GetViewerDependentInteractSpellId(this, receiver, owner));
                }
                if (changesMask[120])
                {
                    data.WriteInt32(ScaleDuration);
                }
                if (changesMask[121])
                {
                    data.WriteInt32(LooksLikeMountID);
                }
                if (changesMask[122])
                {
                    data.WriteInt32(LooksLikeCreatureID);
                }
                if (changesMask[123])
                {
                    data.WriteInt32(LookAtControllerID);
                }
                if (changesMask[124])
                {
                    data.WriteInt32(PerksVendorItemID);
                }
                if (changesMask[125])
                {
                    data.WriteInt32(TaxiNodesID);
                }
                if (changesMask[126])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[127])
                {
                    data.WriteInt32(FlightCapabilityID);
                }
            }
            if (changesMask[128])
            {
                if (changesMask[129])
                {
                    data.WriteFloat(GlideEventSpeedDivisor);
                }
                if (changesMask[130])
                {
                    data.WriteInt32(DriveCapabilityID);
                }
                if (changesMask[131])
                {
                    data.WriteInt32(MaxHealthModifierFlatNeg);
                }
                if (changesMask[132])
                {
                    data.WriteInt32(MaxHealthModifierFlatPos);
                }
                if (changesMask[133])
                {
                    data.WriteUInt32(SilencedSchoolMask);
                }
                if (changesMask[134])
                {
                    data.WriteUInt32(CurrentAreaID);
                }
                if (changesMask[135])
                {
                    data.WriteFloat(NameplateDistanceMod);
                }
                if (changesMask[136])
                {
                    data.WriteFloat(AutoAttackRangeMod);
                }
                if (changesMask[137])
                {
                    data.WritePackedGuid(NameplateAttachToGUID);
                }
                data.WriteBit(AssistActionData.HasValue());
            }
            data.FlushBits();
            if (changesMask[128])
            {
                if (changesMask[138])
                {
                    if (AssistActionData.HasValue())
                    {
                        AssistActionData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[139])
            {
                for (int i = 0; i < 10; ++i)
                {
                    if (changesMask[140 + i])
                    {
                        data.WriteInt32(Power[i]);
                    }
                    if (changesMask[150 + i])
                    {
                        data.WriteUInt32(MaxPower[i]);
                    }
                    if (changesMask[160 + i])
                    {
                        data.WriteFloat(PowerRegenFlatModifier[i]);
                    }
                    if (changesMask[170 + i])
                    {
                        data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
                    }
                }
            }
            if (changesMask[180])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[181 + i])
                    {
                        VirtualItems[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[184])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[185 + i])
                    {
                        data.WriteUInt32(AttackRoundBaseTime[i]);
                    }
                }
            }
            if (changesMask[187])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[188 + i])
                    {
                        data.WriteInt32(Stats[i]);
                    }
                    if (changesMask[193 + i])
                    {
                        data.WriteInt32(StatPosBuff[i]);
                    }
                    if (changesMask[198 + i])
                    {
                        data.WriteInt32(StatNegBuff[i]);
                    }
                    if (changesMask[203 + i])
                    {
                        data.WriteInt32(StatSupportBuff[i]);
                    }
                }
            }
            if (changesMask[208])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[209 + i])
                    {
                        data.WriteInt32(Resistances[i]);
                    }
                    if (changesMask[216 + i])
                    {
                        data.WriteInt32(BonusResistanceMods[i]);
                    }
                    if (changesMask[223 + i])
                    {
                        data.WriteInt32(ManaCostModifier[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Field_314);
            ClearChangesMask(StateWorldEffectIDs);
            ClearChangesMask(PassiveSpells);
            ClearChangesMask(WorldEffects);
            ClearChangesMask(ChannelObjects);
            ClearChangesMask(DisplayID);
            ClearChangesMask(NpcFlags);
            ClearChangesMask(NpcFlags2);
            ClearChangesMask(StateSpellVisualID);
            ClearChangesMask(StateAnimID);
            ClearChangesMask(StateAnimKitID);
            ClearChangesMask(StateWorldEffectsQuestObjectiveID);
            ClearChangesMask(SpellOverrideNameID);
            ClearChangesMask(Charm);
            ClearChangesMask(Summon);
            ClearChangesMask(Critter);
            ClearChangesMask(CharmedBy);
            ClearChangesMask(SummonedBy);
            ClearChangesMask(CreatedBy);
            ClearChangesMask(DemonCreator);
            ClearChangesMask(LookAtControllerTarget);
            ClearChangesMask(Target);
            ClearChangesMask(BattlePetCompanionGUID);
            ClearChangesMask(BattlePetDBID);
            ClearChangesMask(BattlePetAttachedToDecorGUID);
            ClearChangesMask(BattlePetDecorHouseGUID);
            ClearChangesMask(ChannelData);
            ClearChangesMask(SpellEmpowerStage);
            ClearChangesMask(SummonedByHomeRealm);
            ClearChangesMask(Race);
            ClearChangesMask(ClassId);
            ClearChangesMask(PlayerClassId);
            ClearChangesMask(Sex);
            ClearChangesMask(CreatureType);
            ClearChangesMask(DisplayPower);
            ClearChangesMask(OverrideDisplayPowerID);
            ClearChangesMask(Health);
            ClearChangesMask(MaxHealth);
            ClearChangesMask(Level);
            ClearChangesMask(EffectiveLevel);
            ClearChangesMask(ContentTuningID);
            ClearChangesMask(ScalingLevelMin);
            ClearChangesMask(ScalingLevelMax);
            ClearChangesMask(ScalingLevelDelta);
            ClearChangesMask(ScalingFactionGroup);
            ClearChangesMask(FactionTemplate);
            ClearChangesMask(Flags);
            ClearChangesMask(Flags2);
            ClearChangesMask(Flags3);
            ClearChangesMask(Flags4);
            ClearChangesMask(AuraState);
            ClearChangesMask(RangedAttackRoundBaseTime);
            ClearChangesMask(BoundingRadius);
            ClearChangesMask(CombatReach);
            ClearChangesMask(DisplayScale);
            ClearChangesMask(CreatureFamily);
            ClearChangesMask(OverrideCreatureType);
            ClearChangesMask(NativeDisplayID);
            ClearChangesMask(NativeXDisplayScale);
            ClearChangesMask(MountDisplayID);
            ClearChangesMask(CosmeticMountDisplayID);
            ClearChangesMask(MinDamage);
            ClearChangesMask(MaxDamage);
            ClearChangesMask(MinOffHandDamage);
            ClearChangesMask(MaxOffHandDamage);
            ClearChangesMask(StandState);
            ClearChangesMask(PetTalentPoints);
            ClearChangesMask(VisFlags);
            ClearChangesMask(AnimTier);
            ClearChangesMask(PetNumber);
            ClearChangesMask(PetNameTimestamp);
            ClearChangesMask(PetExperience);
            ClearChangesMask(PetNextLevelExperience);
            ClearChangesMask(ModCastingSpeed);
            ClearChangesMask(ModCastingSpeedNeg);
            ClearChangesMask(ModSpellHaste);
            ClearChangesMask(ModHaste);
            ClearChangesMask(ModRangedHaste);
            ClearChangesMask(ModHasteRegen);
            ClearChangesMask(ModTimeRate);
            ClearChangesMask(CreatedBySpell);
            ClearChangesMask(EmoteState);
            ClearChangesMask(BaseMana);
            ClearChangesMask(BaseHealth);
            ClearChangesMask(SheatheState);
            ClearChangesMask(PvpFlags);
            ClearChangesMask(PetFlags);
            ClearChangesMask(ShapeshiftForm);
            ClearChangesMask(AttackPower);
            ClearChangesMask(AttackPowerModPos);
            ClearChangesMask(AttackPowerModNeg);
            ClearChangesMask(AttackPowerMultiplier);
            ClearChangesMask(AttackPowerModSupport);
            ClearChangesMask(RangedAttackPower);
            ClearChangesMask(RangedAttackPowerModPos);
            ClearChangesMask(RangedAttackPowerModNeg);
            ClearChangesMask(RangedAttackPowerMultiplier);
            ClearChangesMask(RangedAttackPowerModSupport);
            ClearChangesMask(MainHandWeaponAttackPower);
            ClearChangesMask(OffHandWeaponAttackPower);
            ClearChangesMask(RangedWeaponAttackPower);
            ClearChangesMask(SetAttackSpeedAura);
            ClearChangesMask(Lifesteal);
            ClearChangesMask(MinRangedDamage);
            ClearChangesMask(MaxRangedDamage);
            ClearChangesMask(ManaCostMultiplier);
            ClearChangesMask(MaxHealthModifier);
            ClearChangesMask(HoverHeight);
            ClearChangesMask(MinItemLevelCutoff);
            ClearChangesMask(MinItemLevel);
            ClearChangesMask(MaxItemLevel);
            ClearChangesMask(AzeriteItemLevel);
            ClearChangesMask(WildBattlePetLevel);
            ClearChangesMask(BattlePetCompanionExperience);
            ClearChangesMask(BattlePetCompanionNameTimestamp);
            ClearChangesMask(InteractSpellID);
            ClearChangesMask(ScaleDuration);
            ClearChangesMask(LooksLikeMountID);
            ClearChangesMask(LooksLikeCreatureID);
            ClearChangesMask(LookAtControllerID);
            ClearChangesMask(PerksVendorItemID);
            ClearChangesMask(TaxiNodesID);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(FlightCapabilityID);
            ClearChangesMask(GlideEventSpeedDivisor);
            ClearChangesMask(DriveCapabilityID);
            ClearChangesMask(MaxHealthModifierFlatNeg);
            ClearChangesMask(MaxHealthModifierFlatPos);
            ClearChangesMask(SilencedSchoolMask);
            ClearChangesMask(CurrentAreaID);
            ClearChangesMask(NameplateDistanceMod);
            ClearChangesMask(AutoAttackRangeMod);
            ClearChangesMask(NameplateAttachToGUID);
            ClearChangesMask(AssistActionData);
            ClearChangesMask(Power);
            ClearChangesMask(MaxPower);
            ClearChangesMask(PowerRegenFlatModifier);
            ClearChangesMask(PowerRegenInterruptedFlatModifier);
            ClearChangesMask(VirtualItems);
            ClearChangesMask(AttackRoundBaseTime);
            ClearChangesMask(Stats);
            ClearChangesMask(StatPosBuff);
            ClearChangesMask(StatNegBuff);
            ClearChangesMask(StatSupportBuff);
            ClearChangesMask(Resistances);
            ClearChangesMask(BonusResistanceMods);
            ClearChangesMask(ManaCostModifier);
            _changesMask.ResetAll();
        }

        uint GetViewerDependentDisplayId(UnitData unitData, Player receiver, Unit unit)
        {
            uint displayId = unitData.DisplayID;
            if (unit.IsCreature())
            {
                CreatureTemplate cinfo = unit.ToCreature().GetCreatureTemplate();
                TempSummon summon = unit.ToTempSummon();
                if (summon != null)
                {
                    if (summon.GetSummonerGUID() == receiver.GetGUID())
                    {
                        if (summon.GetCreatureIdVisibleToSummoner().HasValue)
                            cinfo = Global.ObjectMgr.GetCreatureTemplate(summon.GetCreatureIdVisibleToSummoner().Value);

                        if (summon.GetDisplayIdVisibleToSummoner().HasValue)
                            displayId = summon.GetDisplayIdVisibleToSummoner().Value;
                    }
                }

                // this also applies for transform auras
                SpellInfo transform = Global.SpellMgr.GetSpellInfo(unit.GetTransformSpell(), unit.GetMap().GetDifficultyID());
                if (transform != null)
                {
                    foreach (var spellEffectInfo in transform.GetEffects())
                    {
                        if (spellEffectInfo.IsAura(AuraType.Transform))
                        {
                            CreatureTemplate transformInfo = Global.ObjectMgr.GetCreatureTemplate((uint)spellEffectInfo.MiscValue);
                            if (transformInfo != null)
                            {
                                cinfo = transformInfo;
                                break;
                            }
                        }
                    }
                }

                if (cinfo.FlagsExtra.HasAnyFlag(CreatureFlagsExtra.Trigger))
                    if (receiver.IsGameMaster())
                        displayId = cinfo.GetFirstVisibleModel().CreatureDisplayID;
            }

            return displayId;
        }

        uint GetViewerDependentNpcFlags(UnitData unitData, Player receiver, Unit unit)
        {
            uint npcFlag = unitData.NpcFlags;
            if (npcFlag != 0)
            {
                if ((!unit.IsInteractionAllowedInCombat() && unit.IsInCombat())
                    || (!unit.IsInteractionAllowedWhileHostile() && unit.IsHostileTo(receiver)))
                    npcFlag = 0;
                else
                {
                    Creature creature = unit.ToCreature();
                    if (creature != null)
                    {
                        if (!receiver.CanSeeGossipOn(creature))
                            npcFlag &= ~(uint)(NPCFlags.Gossip | NPCFlags.QuestGiver);

                        if (!receiver.CanSeeSpellClickOn(creature))
                            npcFlag &= ~(uint)NPCFlags.SpellClick;
                    }
                }
            }

            return npcFlag;
        }

        uint GetViewerDependentNpcFlags2(UnitData unitData, Player receiver, Unit unit)
        {
            uint npcFlag = unitData.NpcFlags2;
            if (npcFlag != 0)
            {
                if ((!unit.IsInteractionAllowedInCombat() && unit.IsInCombat())
                   || (!unit.IsInteractionAllowedWhileHostile() && unit.IsHostileTo(receiver)))
                    npcFlag = 0;
            }

            return npcFlag;
        }

        uint GetViewerDependentFactionTemplate(UnitData unitData, Player receiver, Unit unit)
        {
            uint factionTemplate = unitData.FactionTemplate;
            if (unit.IsControlledByPlayer() && receiver != unit && WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && unit.IsInRaidWith(receiver))
            {
                FactionTemplateRecord ft1 = unit.GetFactionTemplateEntry();
                FactionTemplateRecord ft2 = receiver.GetFactionTemplateEntry();
                if (ft1 != null && ft2 != null && !ft1.IsFriendlyTo(ft2))
                    // pretend that all other HOSTILE players have own faction, to allow follow, heal, rezz (trade wont work)
                    factionTemplate = receiver.GetFaction();
            }

            return factionTemplate;
        }

        uint GetViewerDependentFlags(UnitData unitData, Player receiver, Unit unit)
        {
            uint flags = unitData.Flags;
            // Update fields of triggers, transformed units or uninteractible units (values dependent on GM state)
            if (receiver.IsGameMaster())
                flags &= ~(uint)UnitFlags.Uninteractible;

            return flags;
        }

        uint GetViewerDependentFlags2(UnitData unitData, Player receiver, Unit unit)
        {
            uint flags = unitData.Flags2;
            // Gamemasters should be always able to interact with units - remove uninteractible flag
            if (receiver.IsGameMaster())
                flags &= ~(uint)UnitFlags2.UntargetableByClient;

            return flags;
        }

        uint GetViewerDependentFlags3(UnitData unitData, Player receiver, Unit unit)
        {
            uint flags = unitData.Flags3;
            if ((flags & (uint)UnitFlags3.AlreadySkinned) != 0 && unit.IsCreature() && !unit.ToCreature().IsSkinnedBy(receiver))
                flags &= ~(uint)UnitFlags3.AlreadySkinned;

            return flags;
        }

        uint GetViewerDependentFlags4(UnitData unitData, Player receiver, Unit unit)
        {
            return unitData.Flags4;
        }

        uint GetViewerDependentAuraState(UnitData unitData, Player receiver, Unit unit)
        {
            // Check per caster aura states to not enable using a spell in client if specified aura is not by target
            return unit.BuildAuraStateUpdateForTarget(receiver);
        }

        byte GetViewerDependentPvpFlags(UnitData unitData, Player receiver, Unit unit)
        {
            byte pvpFlags = unitData.PvpFlags;
            if (unit.IsControlledByPlayer() && receiver != unit && WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && unit.IsInRaidWith(receiver))
            {
                FactionTemplateRecord ft1 = unit.GetFactionTemplateEntry();
                FactionTemplateRecord ft2 = receiver.GetFactionTemplateEntry();
                if (ft1 != null && ft2 != null && !ft1.IsFriendlyTo(ft2))
                    // Allow targeting opposite faction in party when enabled in config
                    pvpFlags &= (byte)UnitPVPStateFlags.Sanctuary;
            }

            return pvpFlags;
        }

        int GetViewerDependentInteractSpellId(UnitData unitData, Player receiver, Unit unit)
        {
            int interactSpellId = unitData.InteractSpellID;
            if ((unitData.NpcFlags & (uint)NPCFlags.SpellClick) != 0 && interactSpellId == 0)
            {
                // this field is not set if there are multiple available spellclick spells
                var clickBounds = Global.ObjectMgr.GetSpellClickInfoMapBounds(unit.GetEntry());
                foreach (var spellClickInfo in clickBounds)
                {
                    if (!spellClickInfo.IsFitToRequirements(receiver, unit))
                        continue;

                    if (!Global.ConditionMgr.IsObjectMeetingSpellClickConditions(unit.GetEntry(), spellClickInfo.spellId, receiver, unit))
                        continue;

                    interactSpellId = (int)spellClickInfo.spellId;
                    break;
                }

            }
            return interactSpellId;
        }

        List<uint> GetViewerDependentStateWorldEffectIDs(UnitData unitData, Player receiver, Unit unit)
        {
            List<uint> stateWorldEffects = unitData.StateWorldEffectIDs;

            if (unit.IsCreature())
            {
                var spawnTrackingStateData = unit.GetSpawnTrackingStateDataForPlayer(receiver);
                if (spawnTrackingStateData != null)
                    stateWorldEffects = spawnTrackingStateData.StateWorldEffects;
            }

            return stateWorldEffects;
        }

        uint GetViewerDependentStateSpellVisualID(UnitData unitData, Player receiver, Unit unit)
        {
            uint stateSpellVisual = unitData.StateSpellVisualID;

            if (unit.IsCreature())
            {
                var spawnTrackingStateData = unit.GetSpawnTrackingStateDataForPlayer(receiver);
                if (spawnTrackingStateData != null)
                    stateSpellVisual = spawnTrackingStateData.StateSpellVisualId.GetValueOrDefault(0);
            }

            return stateSpellVisual;
        }

        uint GetViewerDependentStateAnimID(UnitData unitData, Player receiver, Unit unit)
        {
            uint stateAnimId = Global.DB2Mgr.GetEmptyAnimStateID();

            if (unit.IsCreature())
            {
                var spawnTrackingStateData = unit.GetSpawnTrackingStateDataForPlayer(receiver);
                if (spawnTrackingStateData != null)
                    stateAnimId = spawnTrackingStateData.StateAnimId.GetValueOrDefault((ushort)stateAnimId);
            }

            return stateAnimId;
        }

        uint GetViewerDependentStateAnimKitID(UnitData unitData, Player receiver, Unit unit)
        {
            uint stateAnimKitId = unitData.StateAnimKitID;

            if (unit.IsCreature())
            {
                var spawnTrackingStateData = unit.GetSpawnTrackingStateDataForPlayer(receiver);
                if (spawnTrackingStateData != null)
                    stateAnimKitId = spawnTrackingStateData.StateAnimKitId.GetValueOrDefault(0);
            }

            return stateAnimKitId;
        }

        uint GetViewerDependentStateWorldEffectsQuestObjectiveID(UnitData unitData, Player receiver, Unit unit)
        {
            uint stateWorldEffectsQuestObjectiveId = unitData.StateWorldEffectsQuestObjectiveID;

            if (stateWorldEffectsQuestObjectiveId == 0 && unit.IsCreature())
            {
                CreatureData data = unit.ToCreature().GetCreatureData();
                if (data != null)
                {
                    if (data.spawnTrackingQuestObjectives.Any())
                    {
                        // If there is no valid objective for player, fill UF with first objective (if any)
                        stateWorldEffectsQuestObjectiveId = data.spawnTrackingQuestObjectives.First();
                        int i = 0;
                        while (++i <= data.spawnTrackingQuestObjectives.Count)
                        {
                            var id = data.spawnTrackingQuestObjectives[i];
                            if (receiver.GetSpawnTrackingStateByObjective(data.spawnTrackingData.SpawnTrackingId, id) != SpawnTrackingState.Active)
                                continue;

                            stateWorldEffectsQuestObjectiveId = id;
                            break;
                        }
                    }
                }
            }

            return stateWorldEffectsQuestObjectiveId;
        }
    }

    public class ChrCustomizationChoice : IEquatable<ChrCustomizationChoice>, IsUpdateFieldStructure<WorldObject>
    {
        public uint ChrCustomizationOptionID;
        public uint ChrCustomizationChoiceID;

        public void WriteCreate(WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WriteUInt32(ChrCustomizationOptionID);
            data.WriteUInt32(ChrCustomizationChoiceID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, WorldObject owner) => WriteCreate(data, receiver, owner);

        public bool Equals(ChrCustomizationChoice right)
        {
            return ChrCustomizationOptionID == right.ChrCustomizationOptionID
                && ChrCustomizationChoiceID == right.ChrCustomizationChoiceID;
        }
    }

    public class QuestLog() : HasChangesMask(30), IsUpdateFieldStructure<Player>
    {
        public UpdateField<uint> QuestID = new(0, 1);
        public UpdateField<ushort> StateFlags = new(0, 2);
        public UpdateField<long> EndTime = new(0, 3);
        public UpdateField<uint> ObjectiveFlags = new(0, 4);
        public UpdateField<uint> EnabledObjectivesMask = new(0, 5);
        public UpdateFieldArray<ushort> ObjectiveProgress = new(24, 6, 7);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(QuestID);
            data.WriteUInt16(StateFlags);
            for (int i = 0; i < 24; ++i)
            {
                data.WriteUInt16(ObjectiveProgress[i]);
            }
            data.WriteInt64(EndTime);
            data.WriteUInt32(ObjectiveFlags);
            data.WriteUInt32(EnabledObjectivesMask);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            if (changesMask.GetBlock(0) != 0)
                data.WriteBits(changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(QuestID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt16(StateFlags);
                }
                if (changesMask[3])
                {
                    data.WriteInt64(EndTime);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(ObjectiveFlags);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(EnabledObjectivesMask);
                }
            }
            if (changesMask[6])
            {
                for (int i = 0; i < 24; ++i)
                {
                    if (changesMask[7 + i])
                    {
                        data.WriteUInt16(ObjectiveProgress[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(QuestID);
            ClearChangesMask(StateFlags);
            ClearChangesMask(EndTime);
            ClearChangesMask(ObjectiveFlags);
            ClearChangesMask(EnabledObjectivesMask);
            ClearChangesMask(ObjectiveProgress);
            _changesMask.ResetAll();
        }
    }

    public class ArenaCooldown() : HasChangesMask(8), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> SpellID = new(0, 1);
        public UpdateField<int> Charges = new(0, 2);
        public UpdateField<uint> Flags = new(0, 3);
        public UpdateField<uint> StartTime = new(0, 4);
        public UpdateField<uint> EndTime = new(0, 5);
        public UpdateField<uint> NextChargeTime = new(0, 6);
        public UpdateField<byte> MaxCharges = new(0, 7);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(Charges);
            data.WriteUInt32(Flags);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(EndTime);
            data.WriteUInt32(NextChargeTime);
            data.WriteUInt8(MaxCharges);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 8);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(SpellID);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(Charges);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(Flags);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(StartTime);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(EndTime);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(NextChargeTime);
                }
                if (changesMask[7])
                {
                    data.WriteUInt8(MaxCharges);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(SpellID);
            ClearChangesMask(Charges);
            ClearChangesMask(Flags);
            ClearChangesMask(StartTime);
            ClearChangesMask(EndTime);
            ClearChangesMask(NextChargeTime);
            ClearChangesMask(MaxCharges);
            _changesMask.ResetAll();
        }
    }

    public class ZonePlayerForcedReaction() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> FactionID = new(0, 1);
        public UpdateField<int> Reaction = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(FactionID);
            data.WriteInt32(Reaction);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(FactionID);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(Reaction);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(FactionID);
            ClearChangesMask(Reaction);
            _changesMask.ResetAll();
        }
    }

    public class PetCreatureName() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public UpdateField<uint> CreatureID = new(0, 1);
        public UpdateFieldString Name = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(CreatureID);
            data.WriteBits(Name.Size(), 8);
            data.FlushBits();
            data.WriteString(Name);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(CreatureID);
                }
                if (changesMask[2])
                {
                    data.WriteBits(Name.Size(), 8);
                    data.FlushBits();
                    data.WriteString(Name);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(CreatureID);
            ClearChangesMask(Name);
            _changesMask.ResetAll();
        }
    }

    public class CTROptions : IEquatable<CTROptions>, IsUpdateFieldStructure<Player>
    {
        public List<uint> ConditionalFlags = [];
        public byte FactionGroup;
        public uint ChromieTimeExpansionMask;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(ConditionalFlags.Count);
            data.WriteUInt8(FactionGroup);
            data.WriteUInt32(ChromieTimeExpansionMask);
            for (int i = 0; i < ConditionalFlags.Count; ++i)
            {
                data.WriteUInt32(ConditionalFlags[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(CTROptions right)
        {
            return ConditionalFlags == right.ConditionalFlags
                && FactionGroup == right.FactionGroup
                && ChromieTimeExpansionMask == right.ChromieTimeExpansionMask;
        }
    }

    public struct LeaverInfo : IEquatable<LeaverInfo>, IsUpdateFieldStructure<Player>
    {
        public bool IsLeaver;
        public ObjectGuid BnetAccountGUID;
        public float LeaveScore;
        public uint SeasonID;
        public uint TotalLeaves;
        public uint TotalSuccesses;
        public int ConsecutiveSuccesses;
        public long LastPenaltyTime;
        public long LeaverExpirationTime;
        public int Flags;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(BnetAccountGUID);
            data.WriteFloat(LeaveScore);
            data.WriteUInt32(SeasonID);
            data.WriteUInt32(TotalLeaves);
            data.WriteUInt32(TotalSuccesses);
            data.WriteInt32(ConsecutiveSuccesses);
            data.WriteInt64(LastPenaltyTime);
            data.WriteInt64(LeaverExpirationTime);
            data.WriteInt32(Flags);
            data.WriteBit(IsLeaver);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(LeaverInfo right)
        {
            return IsLeaver == right.IsLeaver
                && BnetAccountGUID == right.BnetAccountGUID
                && LeaveScore == right.LeaveScore
                && SeasonID == right.SeasonID
                && TotalLeaves == right.TotalLeaves
                && TotalSuccesses == right.TotalSuccesses
                && ConsecutiveSuccesses == right.ConsecutiveSuccesses
                && LastPenaltyTime == right.LastPenaltyTime
                && LeaverExpirationTime == right.LeaverExpirationTime
                && Flags == right.Flags;
        }
    }

    public class DeclinedNames() : HasChangesMask(6), IsUpdateFieldStructure<Player>
    {
        public UpdateFieldArrayString Name = new(5, 0, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            for (int i = 0; i < 5; ++i)
            {
                data.WriteBits(Name[i].GetByteCount(), 10);
            }
            data.FlushBits();
            for (int i = 0; i < 5; ++i)
            {
                data.WriteString(Name[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            if (changesMask.GetBlock(0) != 0)
                data.WriteBits(changesMask.GetBlock(0), 32);

            if (changesMask[0])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[1 + i])
                    {
                        data.WriteBits(Name[i].GetByteCount(), 10);
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[1 + i])
                    {
                        data.WriteString(Name[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Name);
            _changesMask.ResetAll();
        }
    }

    public class CustomTabardInfo() : HasChangesMask(6), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> EmblemStyle = new(0, 1);
        public UpdateField<int> EmblemColor = new(0, 2);
        public UpdateField<int> BorderStyle = new(0, 3);
        public UpdateField<int> BorderColor = new(0, 4);
        public UpdateField<int> BackgroundColor = new(0, 5);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(EmblemStyle);
            data.WriteInt32(EmblemColor);
            data.WriteInt32(BorderStyle);
            data.WriteInt32(BorderColor);
            data.WriteInt32(BackgroundColor);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 6);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(EmblemStyle);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(EmblemColor);
                }
                if (changesMask[3])
                {
                    data.WriteInt32(BorderStyle);
                }
                if (changesMask[4])
                {
                    data.WriteInt32(BorderColor);
                }
                if (changesMask[5])
                {
                    data.WriteInt32(BackgroundColor);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(EmblemStyle);
            ClearChangesMask(EmblemColor);
            ClearChangesMask(BorderStyle);
            ClearChangesMask(BorderColor);
            ClearChangesMask(BackgroundColor);
            _changesMask.ResetAll();
        }
    }

    public class NPCAsPlayerInfo : IEquatable<NPCAsPlayerInfo>, IsUpdateFieldStructure<Player>
    {
        public int Field_0;
        public int CharacterLoadoutID;
        public int CreatureID;
        public Position LocWorldSpace = new();
        public float FacingWorldSpace;
        public ObjectGuid TransportGUID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(CharacterLoadoutID);
            data.WriteInt32(CreatureID);
            data.WriteVector3(LocWorldSpace);
            data.WriteFloat(FacingWorldSpace);
            data.WritePackedGuid(TransportGUID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(NPCAsPlayerInfo right)
        {
            return Field_0 == right.Field_0
            && CharacterLoadoutID == right.CharacterLoadoutID
            && CreatureID == right.CreatureID
            && LocWorldSpace == right.LocWorldSpace
            && FacingWorldSpace == right.FacingWorldSpace
            && TransportGUID == right.TransportGUID;
        }
    }

    public class PlayerData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Player, 326), IsUpdateFieldStructure<Player>
    {
        public UpdateField<bool> HasQuestSession = new(0, 1);
        public UpdateField<bool> HasLevelLink = new(0, 2);
        public DynamicUpdateField<ChrCustomizationChoice> Customizations = new(0, 3);
        public DynamicUpdateField<ChrCustomizationChoice> RandomCustomizations = new(0, 4);
        public DynamicUpdateField<QuestLog> QuestSessionQuestLog = new(0, 5);
        public DynamicUpdateField<ArenaCooldown> ArenaCooldowns = new(0, 6);
        public DynamicUpdateField<PetCreatureName> PetNames = new(0, 7);
        public DynamicUpdateField<int> VisualItemReplacements = new(0, 8);
        public UpdateField<ObjectGuid> DuelArbiter = new(0, 9);
        public UpdateField<ObjectGuid> WowAccount = new(0, 10);
        public UpdateField<ObjectGuid> BnetAccount = new(0, 11);
        public UpdateField<ulong> GuildClubMemberID = new(0, 12);
        public UpdateField<ObjectGuid> LootTargetGUID = new(0, 13);
        public UpdateField<uint> PlayerFlags = new(0, 14);
        public UpdateField<uint> PlayerFlagsEx = new(0, 15);
        public UpdateField<uint> GuildRankID = new(0, 16);
        public UpdateField<uint> GuildDeleteDate = new(0, 17);
        public UpdateField<uint> GuildLevel = new(0, 18);
        public UpdateField<byte> NativeSex = new(0, 19);
        public UpdateField<byte> Inebriation = new(0, 20);
        public UpdateField<byte> PvpTitle = new(0, 21);
        public UpdateField<byte> ArenaFaction = new(0, 22);
        public UpdateField<uint> DuelTeam = new(0, 23);
        public UpdateField<int> GuildTimeStamp = new(0, 24);
        public MapUpdateField<int, int> QuestLogQuestIdToIndex = new(0, 25);
        public UpdateField<uint> PlayerTitle = new(0, 26);
        public UpdateField<int> FakeInebriation = new(0, 27);
        public UpdateField<uint> VirtualPlayerRealm = new(0, 28);
        public UpdateField<uint> CurrentSpecID = new(0, 29);
        public UpdateField<int> CurrentCombatTraitConfigSubTreeID = new(0, 30);
        public UpdateField<int> TaxiMountAnimKitID = new(0, 31);
        public UpdateField<byte> CurrentBattlePetBreedQuality = new(32, 33);
        public UpdateField<uint> HonorLevel = new(32, 34);
        public UpdateField<long> LogoutTime = new(32, 35);
        public UpdateFieldString Name = new(32, 36);
        public UpdateField<int> OfferedAdvJournalQuestID = new(32, 37);
        public UpdateField<int> OfferedScriptQuestID = new(32, 38);
        public UpdateField<int> CurrentBattlePetSpeciesID = new(32, 39);
        public UpdateField<CTROptions> CtrOptions = new(32, 40);
        public UpdateField<int> CovenantID = new(32, 41);
        public UpdateField<int> SoulbindID = new(32, 42);
        public UpdateField<DungeonScoreSummary> DungeonScore = new(32, 43);
        public UpdateField<LeaverInfo> LeaverInfo = new(32, 44);
        public UpdateField<ObjectGuid> SpectateTarget = new(32, 45);
        public UpdateField<int> WorldLootObjectInventorySwapSlot = new(32, 46);
        public OptionalUpdateField<DeclinedNames> DeclinedNames = new(32, 47);
        public UpdateField<CustomTabardInfo> PersonalTabard = new(32, 48);
        public UpdateField<NPCAsPlayerInfo> NpcAsPlayerInfo = new(32, 49);
        public UpdateFieldArray<byte> PartyType = new(2, 50, 51);
        public UpdateFieldArray<QuestLog> QuestLog = new(175, 53, 54);
        public UpdateFieldArray<VisibleItem> VisibleItems = new(19, 229, 230);
        public UpdateFieldArray<float> AvgItemLevel = new(6, 249, 250);
        public UpdateFieldArray<ZonePlayerForcedReaction> ForcedReactions = new(32, 256, 257);
        public UpdateFieldArray<ItemInstance> VisibleEquipableSpells = new(16, 289, 290);
        public UpdateFieldArray<uint> PlunderstormItemDisplayID = new(19, 306, 307);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(DuelArbiter);
            data.WritePackedGuid(WowAccount);
            data.WritePackedGuid(BnetAccount);
            data.WriteUInt64(GuildClubMemberID);
            data.WritePackedGuid(LootTargetGUID);
            data.WriteUInt32(PlayerFlags);
            data.WriteUInt32(PlayerFlagsEx);
            data.WriteUInt32(GuildRankID);
            data.WriteUInt32(GuildDeleteDate);
            data.WriteUInt32(GuildLevel);
            data.WriteInt32(Customizations.Size());
            data.WriteInt32(RandomCustomizations.Size());
            for (int i = 0; i < 2; ++i)
            {
                data.WriteUInt8(PartyType[i]);
            }
            data.WriteUInt8(NativeSex);
            data.WriteUInt8(Inebriation);
            data.WriteUInt8(PvpTitle);
            data.WriteUInt8(ArenaFaction);
            data.WriteUInt32(DuelTeam);
            data.WriteInt32(GuildTimeStamp);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                for (int i = 0; i < 175; ++i)
                    QuestLog[i].WriteCreate(data, receiver, owner);

                WriteMapFieldCreate(QuestLogQuestIdToIndex, data, receiver, owner);
                data.WriteInt32(QuestSessionQuestLog.Size());
            }
            for (int i = 0; i < 19; ++i)
            {
                VisibleItems[i].WriteCreate(data, receiver, owner);
            }
            data.WriteUInt32(PlayerTitle);
            data.WriteInt32(FakeInebriation);
            data.WriteUInt32(VirtualPlayerRealm);
            data.WriteUInt32(CurrentSpecID);
            data.WriteInt32(CurrentCombatTraitConfigSubTreeID);
            data.WriteInt32(TaxiMountAnimKitID);
            for (int i = 0; i < 6; ++i)
            {
                data.WriteFloat(AvgItemLevel[i]);
            }
            data.WriteUInt8(CurrentBattlePetBreedQuality);
            data.WriteUInt32(HonorLevel);
            data.WriteInt64(LogoutTime);
            data.WriteInt32(ArenaCooldowns.Size());
            for (int i = 0; i < 32; ++i)
            {
                ForcedReactions[i].WriteCreate(data, receiver, owner);
            }
            data.WriteInt32(OfferedAdvJournalQuestID);
            data.WriteInt32(OfferedScriptQuestID);
            data.WriteInt32(CurrentBattlePetSpeciesID);
            data.WriteInt32(PetNames.Size());
            ((CTROptions)CtrOptions).WriteCreate(data, receiver, owner);
            data.WriteInt32(CovenantID);
            data.WriteInt32(SoulbindID);
            DungeonScore.GetValue().Write(data);
            LeaverInfo.GetValue().WriteCreate(data, receiver, owner);
            data.WritePackedGuid(SpectateTarget);
            data.WriteInt32(WorldLootObjectInventorySwapSlot);
            for (int i = 0; i < 16; ++i)
            {
                VisibleEquipableSpells[i].Write(data);
            }
            data.WriteInt32(VisualItemReplacements.Size());
            for (int i = 0; i < 19; ++i)
            {
                data.WriteUInt32(PlunderstormItemDisplayID[i]);
            }
            PersonalTabard.GetValue().WriteCreate(data, receiver, owner);
            NpcAsPlayerInfo.GetValue().WriteCreate(data, receiver, owner);
            for (int i = 0; i < Customizations.Size(); ++i)
            {
                Customizations[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < RandomCustomizations.Size(); ++i)
            {
                RandomCustomizations[i].WriteCreate(data, receiver, owner);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                for (int i = 0; i < QuestSessionQuestLog.Size(); ++i)
                {
                    QuestSessionQuestLog[i].WriteCreate(data, receiver, owner);
                }
            }
            for (int i = 0; i < ArenaCooldowns.Size(); ++i)
            {
                ArenaCooldowns[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < PetNames.Size(); ++i)
            {
                PetNames[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < VisualItemReplacements.Size(); ++i)
            {
                data.WriteInt32(VisualItemReplacements[i]);
            }
            data.WriteBits(Name.Size(), 6);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                data.WriteBit(HasQuestSession);
            }
            data.WriteBit(HasLevelLink);
            data.WriteBit(DeclinedNames.HasValue());
            data.FlushBits();
            data.WriteString(Name);
            if (DeclinedNames.HasValue())
            {
                DeclinedNames.GetValue().WriteCreate(data, receiver, owner);
            }
        }

        public void PlayerDataAppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x02000022u, 0xFFE00000u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x0000001Fu, 0x00000000u, 0x00000000u, 0x00000000u]));
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            PlayerDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xFDFFFFDDu, 0x001FFFFFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0xFFFFFFE0u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x0000003Fu]);
            PlayerDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xFDFFFFDDu, 0x001FFFFFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0xFFFFFFE0u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x0000003Fu]);
            PlayerDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(_changesMask & allowedMaskForTarget, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Player owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 11);
            for (uint i = 0; i < 11; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            bool noQuestLogChangesMask = data.WriteBit(IsQuestLogChangesMaskSkipped());
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(HasQuestSession);
                }
                if (changesMask[2])
                {
                    data.WriteBit(HasLevelLink);
                }
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        Customizations.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Customizations.Size(), data);
                }
                if (changesMask[4])
                {
                    if (!ignoreNestedChangesMask)
                        RandomCustomizations.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(RandomCustomizations.Size(), data);
                }
                if (changesMask[5])
                {
                    if (!ignoreNestedChangesMask)
                        QuestSessionQuestLog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(QuestSessionQuestLog.Size(), data);
                }
                if (changesMask[6])
                {
                    if (!ignoreNestedChangesMask)
                        ArenaCooldowns.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ArenaCooldowns.Size(), data);
                }
                if (changesMask[7])
                {
                    if (!ignoreNestedChangesMask)
                        PetNames.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(PetNames.Size(), data);
                }
                if (changesMask[8])
                {
                    if (!ignoreNestedChangesMask)
                        VisualItemReplacements.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(VisualItemReplacements.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    for (int i = 0; i < Customizations.Size(); ++i)
                    {
                        if (Customizations.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Customizations[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < RandomCustomizations.Size(); ++i)
                    {
                        if (RandomCustomizations.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            RandomCustomizations[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[5])
                {
                    for (int i = 0; i < QuestSessionQuestLog.Size(); ++i)
                    {
                        if (QuestSessionQuestLog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            if (noQuestLogChangesMask)
                                QuestSessionQuestLog[i].WriteCreate(data, receiver, owner);
                            else
                                QuestSessionQuestLog[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[6])
                {
                    for (int i = 0; i < ArenaCooldowns.Size(); ++i)
                    {
                        if (ArenaCooldowns.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ArenaCooldowns[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[7])
                {
                    for (int i = 0; i < PetNames.Size(); ++i)
                    {
                        if (PetNames.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PetNames[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[8])
                {
                    for (int i = 0; i < VisualItemReplacements.Size(); ++i)
                    {
                        if (VisualItemReplacements.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(VisualItemReplacements[i]);
                        }
                    }
                }
                if (changesMask[9])
                {
                    data.WritePackedGuid(DuelArbiter);
                }
                if (changesMask[10])
                {
                    data.WritePackedGuid(WowAccount);
                }
                if (changesMask[11])
                {
                    data.WritePackedGuid(BnetAccount);
                }
                if (changesMask[12])
                {
                    data.WriteUInt64(GuildClubMemberID);
                }
                if (changesMask[13])
                {
                    data.WritePackedGuid(LootTargetGUID);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(PlayerFlags);
                }
                if (changesMask[15])
                {
                    data.WriteUInt32(PlayerFlagsEx);
                }
                if (changesMask[16])
                {
                    data.WriteUInt32(GuildRankID);
                }
                if (changesMask[17])
                {
                    data.WriteUInt32(GuildDeleteDate);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(GuildLevel);
                }
                if (changesMask[19])
                {
                    data.WriteUInt8(NativeSex);
                }
                if (changesMask[20])
                {
                    data.WriteUInt8(Inebriation);
                }
                if (changesMask[21])
                {
                    data.WriteUInt8(PvpTitle);
                }
                if (changesMask[22])
                {
                    data.WriteUInt8(ArenaFaction);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(DuelTeam);
                }
                if (changesMask[24])
                {
                    data.WriteInt32(GuildTimeStamp);
                }
                if (changesMask[25])
                {
                    WriteMapFieldUpdate(QuestLogQuestIdToIndex, ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[26])
                {
                    data.WriteUInt32(PlayerTitle);
                }
                if (changesMask[27])
                {
                    data.WriteInt32(FakeInebriation);
                }
                if (changesMask[28])
                {
                    data.WriteUInt32(VirtualPlayerRealm);
                }
                if (changesMask[29])
                {
                    data.WriteUInt32(CurrentSpecID);
                }
                if (changesMask[30])
                {
                    data.WriteInt32(CurrentCombatTraitConfigSubTreeID);
                }
                if (changesMask[31])
                {
                    data.WriteInt32(TaxiMountAnimKitID);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt8(CurrentBattlePetBreedQuality);
                }
                if (changesMask[34])
                {
                    data.WriteUInt32(HonorLevel);
                }
                if (changesMask[35])
                {
                    data.WriteInt64(LogoutTime);
                }
                if (changesMask[37])
                {
                    data.WriteInt32(OfferedAdvJournalQuestID);
                }
                if (changesMask[38])
                {
                    data.WriteInt32(OfferedScriptQuestID);
                }
                if (changesMask[39])
                {
                    data.WriteInt32(CurrentBattlePetSpeciesID);
                }
                if (changesMask[40])
                {
                    CtrOptions.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[41])
                {
                    data.WriteInt32(CovenantID);
                }
                if (changesMask[42])
                {
                    data.WriteInt32(SoulbindID);
                }
                if (changesMask[43])
                {
                    DungeonScore.GetValue().Write(data);
                }
                if (changesMask[44])
                {
                    LeaverInfo.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[45])
                {
                    data.WritePackedGuid(SpectateTarget);
                }
                if (changesMask[46])
                {
                    data.WriteInt32(WorldLootObjectInventorySwapSlot);
                }
                if (changesMask[48])
                {
                    PersonalTabard.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[49])
                {
                    NpcAsPlayerInfo.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[36])
                {
                    data.WriteBits(Name.Size(), 6);
                }
                data.WriteBit(DeclinedNames.HasValue());
            }
            data.FlushBits();
            if (changesMask[32])
            {
                if (changesMask[36])
                {
                    data.WriteString(Name);
                }
                if (changesMask[47])
                {
                    if (DeclinedNames.HasValue())
                    {
                        DeclinedNames.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[50])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[51 + i])
                    {
                        data.WriteUInt8(PartyType[i]);
                    }
                }
            }
            if (changesMask[53])
            {
                for (int i = 0; i < 175; ++i)
                {
                    if (changesMask[54 + i])
                    {
                        if (noQuestLogChangesMask)
                            QuestLog[i].WriteCreate(data, receiver, owner);
                        else
                            QuestLog[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[229])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[230 + i])
                    {
                        VisibleItems[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[249])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[250 + i])
                    {
                        data.WriteFloat(AvgItemLevel[i]);
                    }
                }
            }
            if (changesMask[256])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (changesMask[257 + i])
                    {
                        ForcedReactions[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[289])
            {
                for (int i = 0; i < 16; ++i)
                {
                    if (changesMask[290 + i])
                    {
                        VisibleEquipableSpells[i].Write(data);
                    }
                }
            }
            if (changesMask[306])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[307 + i])
                    {
                        data.WriteUInt32(PlunderstormItemDisplayID[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(HasQuestSession);
            ClearChangesMask(HasLevelLink);
            ClearChangesMask(Customizations);
            ClearChangesMask(RandomCustomizations);
            ClearChangesMask(QuestSessionQuestLog);
            ClearChangesMask(ArenaCooldowns);
            ClearChangesMask(PetNames);
            ClearChangesMask(VisualItemReplacements);
            ClearChangesMask(DuelArbiter);
            ClearChangesMask(WowAccount);
            ClearChangesMask(BnetAccount);
            ClearChangesMask(GuildClubMemberID);
            ClearChangesMask(LootTargetGUID);
            ClearChangesMask(PlayerFlags);
            ClearChangesMask(PlayerFlagsEx);
            ClearChangesMask(GuildRankID);
            ClearChangesMask(GuildDeleteDate);
            ClearChangesMask(GuildLevel);
            ClearChangesMask(NativeSex);
            ClearChangesMask(Inebriation);
            ClearChangesMask(PvpTitle);
            ClearChangesMask(ArenaFaction);
            ClearChangesMask(DuelTeam);
            ClearChangesMask(GuildTimeStamp);
            ClearChangesMask(QuestLogQuestIdToIndex);
            ClearChangesMask(PlayerTitle);
            ClearChangesMask(FakeInebriation);
            ClearChangesMask(VirtualPlayerRealm);
            ClearChangesMask(CurrentSpecID);
            ClearChangesMask(CurrentCombatTraitConfigSubTreeID);
            ClearChangesMask(TaxiMountAnimKitID);
            ClearChangesMask(CurrentBattlePetBreedQuality);
            ClearChangesMask(HonorLevel);
            ClearChangesMask(LogoutTime);
            ClearChangesMask(Name);
            ClearChangesMask(OfferedAdvJournalQuestID);
            ClearChangesMask(OfferedScriptQuestID);
            ClearChangesMask(CurrentBattlePetSpeciesID);
            ClearChangesMask(CtrOptions);
            ClearChangesMask(CovenantID);
            ClearChangesMask(SoulbindID);
            ClearChangesMask(DungeonScore);
            ClearChangesMask(LeaverInfo);
            ClearChangesMask(SpectateTarget);
            ClearChangesMask(WorldLootObjectInventorySwapSlot);
            ClearChangesMask(DeclinedNames);
            ClearChangesMask(PersonalTabard);
            ClearChangesMask(NpcAsPlayerInfo);
            ClearChangesMask(PartyType);
            ClearChangesMask(QuestLog);
            ClearChangesMask(VisibleItems);
            ClearChangesMask(AvgItemLevel);
            ClearChangesMask(ForcedReactions);
            ClearChangesMask(VisibleEquipableSpells);
            ClearChangesMask(PlunderstormItemDisplayID);
            _changesMask.ResetAll();
        }

        bool IsQuestLogChangesMaskSkipped() { return false; } // bandwidth savings aren't worth the cpu time
    }

    public class SkillInfo() : HasChangesMask(2101), IsUpdateFieldStructure<Player>
    {
        public UpdateFieldArray<ushort> SkillLineID = new(300, 0, 1);
        public UpdateFieldArray<ushort> SkillStep = new(300, 0, 301);
        public UpdateFieldArray<ushort> SkillRank = new(300, 0, 601);
        public UpdateFieldArray<ushort> SkillStartingRank = new(300, 0, 901);
        public UpdateFieldArray<ushort> SkillMaxRank = new(300, 0, 1201);
        public UpdateFieldArray<ushort> SkillTempBonus = new(300, 0, 1501);
        public UpdateFieldArray<ushort> SkillPermBonus = new(300, 0, 1801);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            for (int i = 0; i < 300; ++i)
            {
                data.WriteUInt16(SkillLineID[i]);
                data.WriteUInt16(SkillStep[i]);
                data.WriteUInt16(SkillRank[i]);
                data.WriteUInt16(SkillStartingRank[i]);
                data.WriteUInt16(SkillMaxRank[i]);
                data.WriteUInt16(SkillTempBonus[i]);
                data.WriteUInt16(SkillPermBonus[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            for (uint i = 0; i < 2; ++i)
                data.WriteUInt32(changesMask.GetBlocksMask(i));
            data.WriteBits(changesMask.GetBlocksMask(2), 2);
            for (uint i = 0; i < 66; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < 300; ++i)
                {
                    if (changesMask[1 + i])
                    {
                        data.WriteUInt16(SkillLineID[i]);
                    }
                    if (changesMask[301 + i])
                    {
                        data.WriteUInt16(SkillStep[i]);
                    }
                    if (changesMask[601 + i])
                    {
                        data.WriteUInt16(SkillRank[i]);
                    }
                    if (changesMask[901 + i])
                    {
                        data.WriteUInt16(SkillStartingRank[i]);
                    }
                    if (changesMask[1201 + i])
                    {
                        data.WriteUInt16(SkillMaxRank[i]);
                    }
                    if (changesMask[1501 + i])
                    {
                        data.WriteUInt16(SkillTempBonus[i]);
                    }
                    if (changesMask[1801 + i])
                    {
                        data.WriteUInt16(SkillPermBonus[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(SkillLineID);
            ClearChangesMask(SkillStep);
            ClearChangesMask(SkillRank);
            ClearChangesMask(SkillStartingRank);
            ClearChangesMask(SkillMaxRank);
            ClearChangesMask(SkillTempBonus);
            ClearChangesMask(SkillPermBonus);
            _changesMask.ResetAll();
        }
    }

    public class BitVector() : HasChangesMask(2), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<ulong> Values = new(0, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(Values.Size());
            for (int i = 0; i < Values.Size(); ++i)
                data.WriteUInt64(Values[i]);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 2);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreChangesMask)
                        Values.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Values.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Values.Size(); ++i)
                    {
                        if (Values.HasChanged(i) || ignoreChangesMask)
                        {
                            data.WriteUInt64(Values[i]);
                        }
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Values);
            _changesMask.ResetAll();
        }
    }

    public class BitVectors() : HasChangesMask(15), IsUpdateFieldStructure<Player>
    {
        public UpdateFieldArray<BitVector> Values = new(14, 0, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            for (int i = 0; i < 14; ++i)
                Values[i].WriteCreate(data, receiver, owner);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            if (changesMask.GetBlock(0) != 0)
                data.WriteBits(changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < 14; ++i)
                {
                    if (changesMask[1 + i])
                    {
                        Values[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Values);
            _changesMask.ResetAll();
        }
    }

    public class PlayerDataElement : IEquatable<PlayerDataElement>, IsUpdateFieldStructure<Player>
    {
        public uint Type;
        public float FloatValue;
        public long Int64Value;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(Type);
            if (Type == 1)
            {
                data.WriteFloat(FloatValue);
            }
            if (Type == 0)
            {
                data.WriteInt64(Int64Value);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(PlayerDataElement right)
        {
            return Type == right.Type
                && FloatValue == right.FloatValue
                && Int64Value == right.Int64Value;
        }
    }

    public class RestInfo() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public UpdateField<uint> Threshold = new(0, 1);
        public UpdateField<byte> StateID = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(Threshold);
            data.WriteUInt8(StateID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(Threshold);
                }
                if (changesMask[2])
                {
                    data.WriteUInt8(StateID);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Threshold);
            ClearChangesMask(StateID);
            _changesMask.ResetAll();
        }
    }

    public class PVPInfo() : HasChangesMask(19), IsUpdateFieldStructure<Player>
    {
        public UpdateField<bool> Disqualified = new(0, 1);
        public UpdateField<sbyte> Bracket = new(0, 2);
        public UpdateField<uint> PvpRatingID = new(0, 3);
        public UpdateField<uint> WeeklyPlayed = new(0, 4);
        public UpdateField<uint> WeeklyWon = new(0, 5);
        public UpdateField<uint> SeasonPlayed = new(0, 6);
        public UpdateField<uint> SeasonWon = new(0, 7);
        public UpdateField<uint> Rating = new(0, 8);
        public UpdateField<uint> WeeklyBestRating = new(0, 9);
        public UpdateField<uint> LastWeeksBestRating = new(0, 10);
        public UpdateField<uint> PvpTierID = new(0, 11);
        public UpdateField<uint> WeeklyBestWinPvpTierID = new(0, 12);
        public UpdateField<uint> SeasonBestRating = new(0, 13);
        public UpdateField<uint> SeasonBestTier = new(0, 14);
        public UpdateField<uint> WeeklyRoundsPlayed = new(0, 15);
        public UpdateField<uint> WeeklyRoundsWon = new(0, 16);
        public UpdateField<uint> SeasonRoundsPlayed = new(0, 17);
        public UpdateField<uint> SeasonRoundsWon = new(0, 18);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt8(Bracket);
            data.WriteUInt32(PvpRatingID);
            data.WriteUInt32(WeeklyPlayed);
            data.WriteUInt32(WeeklyWon);
            data.WriteUInt32(SeasonPlayed);
            data.WriteUInt32(SeasonWon);
            data.WriteUInt32(Rating);
            data.WriteUInt32(WeeklyBestRating);
            data.WriteUInt32(LastWeeksBestRating);
            data.WriteUInt32(PvpTierID);
            data.WriteUInt32(WeeklyBestWinPvpTierID);
            data.WriteUInt32(SeasonBestRating);
            data.WriteUInt32(SeasonBestTier);
            data.WriteUInt32(WeeklyRoundsPlayed);
            data.WriteUInt32(WeeklyRoundsWon);
            data.WriteUInt32(SeasonRoundsPlayed);
            data.WriteUInt32(SeasonRoundsWon);
            data.WriteBit(Disqualified);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 19);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(Disqualified);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    data.WriteInt8(Bracket);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(PvpRatingID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(WeeklyPlayed);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(WeeklyWon);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(SeasonPlayed);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(SeasonWon);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(Rating);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(WeeklyBestRating);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(LastWeeksBestRating);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(PvpTierID);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(WeeklyBestWinPvpTierID);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(SeasonBestRating);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(SeasonBestTier);
                }
                if (changesMask[15])
                {
                    data.WriteUInt32(WeeklyRoundsPlayed);
                }
                if (changesMask[16])
                {
                    data.WriteUInt32(WeeklyRoundsWon);
                }
                if (changesMask[17])
                {
                    data.WriteUInt32(SeasonRoundsPlayed);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(SeasonRoundsWon);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Disqualified);
            ClearChangesMask(Bracket);
            ClearChangesMask(PvpRatingID);
            ClearChangesMask(WeeklyPlayed);
            ClearChangesMask(WeeklyWon);
            ClearChangesMask(SeasonPlayed);
            ClearChangesMask(SeasonWon);
            ClearChangesMask(Rating);
            ClearChangesMask(WeeklyBestRating);
            ClearChangesMask(LastWeeksBestRating);
            ClearChangesMask(PvpTierID);
            ClearChangesMask(WeeklyBestWinPvpTierID);
            ClearChangesMask(SeasonBestRating);
            ClearChangesMask(SeasonBestTier);
            ClearChangesMask(WeeklyRoundsPlayed);
            ClearChangesMask(WeeklyRoundsWon);
            ClearChangesMask(SeasonRoundsPlayed);
            ClearChangesMask(SeasonRoundsWon);
            _changesMask.ResetAll();
        }
    }

    public class Research : IEquatable<Research>, IsUpdateFieldStructure<Player>
    {
        public short ResearchProjectID;

        public void WriteCreate(ByteBuffer data, Player receiver, Player owner)
        {
            data.WriteInt16(ResearchProjectID);
        }

        public void WriteUpdate(bool ignoreChangesMask, ByteBuffer data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(Research right)
        {
            return ResearchProjectID == right.ResearchProjectID;
        }
    }

    public class CharacterRestriction : IEquatable<CharacterRestriction>, IsUpdateFieldStructure<Player>
    {
        public int Field_0;
        public int Field_4;
        public int Field_8;
        public uint Type;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(Field_4);
            data.WriteInt32(Field_8);
            data.WriteBits(Type, 5);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(CharacterRestriction right)
        {
            return Field_0 == right.Field_0
                && Field_4 == right.Field_4
                && Field_8 == right.Field_8
                && Type == right.Type;
        }
    }

    public class SpellPctModByLabel : IEquatable<SpellPctModByLabel>, IsUpdateFieldStructure<Player>
    {
        public int ModIndex;
        public float ModifierValue;
        public int LabelID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(ModIndex);
            data.WriteFloat(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(SpellPctModByLabel right)
        {
            return ModIndex == right.ModIndex
                && ModifierValue == right.ModifierValue
                && LabelID == right.LabelID;
        }
    }

    public class SpellFlatModByLabel : IEquatable<SpellFlatModByLabel>, IsUpdateFieldStructure<Player>
    {
        public int ModIndex;
        public int ModifierValue;
        public int LabelID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(ModIndex);
            data.WriteInt32(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(SpellFlatModByLabel right)
        {
            return ModIndex == right.ModIndex
                && ModifierValue == right.ModifierValue
                && LabelID == right.LabelID;
        }
    }

    public struct SpellPctPVPModByLabel : IEquatable<SpellPctPVPModByLabel>, IsUpdateFieldStructure<Player>
    {
        public int PvpModIndex;
        public float ModifierValue;
        public int LabelID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(PvpModIndex);
            data.WriteFloat(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(SpellPctPVPModByLabel right)
        {
            return PvpModIndex == right.PvpModIndex
            && ModifierValue == right.ModifierValue
            && LabelID == right.LabelID;
        }
    }

    public struct SpellFlatPVPModByLabel : IEquatable<SpellFlatPVPModByLabel>, IsUpdateFieldStructure<Player>
    {
        public int PvpModIndex;
        public int ModifierValue;
        public int LabelID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(PvpModIndex);
            data.WriteInt32(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(SpellFlatPVPModByLabel right)
        {
            return PvpModIndex == right.PvpModIndex
            && ModifierValue == right.ModifierValue
            && LabelID == right.LabelID;
        }
    }

    public class CompletedProject() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public UpdateField<long> FirstCompleted = new(0, 1);
        public UpdateField<uint> ProjectID = new(0, 2);
        public UpdateField<uint> CompletionCount = new(0, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt64(FirstCompleted);
            data.WriteUInt32(ProjectID);
            data.WriteUInt32(CompletionCount);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt64(FirstCompleted);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(ProjectID);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(CompletionCount);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(FirstCompleted);
            ClearChangesMask(ProjectID);
            ClearChangesMask(CompletionCount);
            _changesMask.ResetAll();
        }
    }

    public class ResearchHistory() : HasChangesMask(2), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<CompletedProject> CompletedProjects = new(0, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(CompletedProjects.Size());
            for (int i = 0; i < CompletedProjects.Size(); ++i)
            {
                CompletedProjects[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 2);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreChangesMask)
                        CompletedProjects.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CompletedProjects.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < CompletedProjects.Size(); ++i)
                    {
                        if (CompletedProjects.HasChanged(i) || ignoreChangesMask)
                        {
                            CompletedProjects[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                        }
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(CompletedProjects);
            _changesMask.ResetAll();
        }
    }

    public class MawPower : IEquatable<MawPower>, IsUpdateFieldStructure<Player>
    {
        public int SpellID;
        public int MawPowerID;
        public int Stacks;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(MawPowerID);
            data.WriteInt32(Stacks);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(MawPower right)
        {
            return SpellID == right.SpellID
                && MawPowerID == right.MawPowerID
                && Stacks == right.Stacks;
        }
    }

    public class MultiFloorExplore : IEquatable<MultiFloorExplore>, IsUpdateFieldStructure<Player>
    {
        public List<int> WorldMapOverlayIDs = new();

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(WorldMapOverlayIDs.Count);
            for (int i = 0; i < WorldMapOverlayIDs.Count; ++i)
            {
                data.WriteInt32(WorldMapOverlayIDs[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(MultiFloorExplore right)
        {
            return WorldMapOverlayIDs == right.WorldMapOverlayIDs;
        }
    }

    public class RecipeProgressionInfo : IEquatable<RecipeProgressionInfo>, IsUpdateFieldStructure<Player>
    {
        public ushort RecipeProgressionGroupID;
        public ushort Experience;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt16(RecipeProgressionGroupID);
            data.WriteUInt16(Experience);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(RecipeProgressionInfo right)
        {
            return RecipeProgressionGroupID == right.RecipeProgressionGroupID
                && Experience == right.Experience;
        }
    }

    public class DiscordPlayerInfo : IEquatable<DiscordPlayerInfo>, IsUpdateFieldStructure<Player>
    {
        public ulong DiscordUserID;
        public byte AccountType;
        public ulong GuildLobbyID;
        public byte GuildSettings;
        public byte DisplayNameType;
        public string AccessToken = "";

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteBits(AccessToken.GetByteCount() + 1, 24);
            data.WriteUInt64(DiscordUserID);
            data.WriteUInt8(AccountType);
            data.WriteUInt64(GuildLobbyID);
            data.WriteUInt8(GuildSettings);
            data.WriteUInt8(DisplayNameType);
            data.WriteCString(AccessToken);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(DiscordPlayerInfo right)
        {
            return DiscordUserID == right.DiscordUserID
                && AccountType == right.AccountType
                && GuildLobbyID == right.GuildLobbyID
                && GuildSettings == right.GuildSettings
                && DisplayNameType == right.DisplayNameType
                && AccessToken == right.AccessToken;
        }
    }

    public class QuestSession() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public UpdateField<ObjectGuid> Owner = new(0, 1);
        public UpdateField<BitVector> QuestCompleted = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(Owner);
            QuestCompleted.GetValue().WriteCreate(data, receiver, owner);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(Owner);
                }

                if (changesMask[2])
                {
                    QuestCompleted.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Owner);
            ClearChangesMask(QuestCompleted);
            _changesMask.ResetAll();
        }
    }

    public class ReplayedQuest() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> QuestID = new(0, 1);
        public UpdateField<uint> ReplayTime = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(QuestID);
            data.WriteUInt32(ReplayTime);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(QuestID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(ReplayTime);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(QuestID);
            ClearChangesMask(ReplayTime);
            _changesMask.ResetAll();
        }
    }

    public class LevelLinkInfo() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public UpdateField<ObjectGuid> TargetGUID = new(0, 1);
        public UpdateField<int> Level = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(TargetGUID);
            data.WriteInt32(Level);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(TargetGUID);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(Level);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(TargetGUID);
            ClearChangesMask(Level);
            _changesMask.ResetAll();
        }
    }

    public class TraitEntry : IEquatable<TraitEntry>, IsUpdateFieldStructure<Player>
    {
        public int TraitNodeID;
        public int TraitNodeEntryID;
        public int Rank;
        public int GrantedRanks;
        public int BonusRanks;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(TraitNodeID);
            data.WriteInt32(TraitNodeEntryID);
            data.WriteInt32(Rank);
            data.WriteInt32(GrantedRanks);
            data.WriteInt32(BonusRanks);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(TraitEntry right)
        {
            return TraitNodeID == right.TraitNodeID
                && TraitNodeEntryID == right.TraitNodeEntryID
                && Rank == right.Rank
                && GrantedRanks == right.GrantedRanks
                && BonusRanks == right.BonusRanks;
        }
    }

    public class TraitSubTreeCache : IEquatable<TraitSubTreeCache>, IsUpdateFieldStructure<Player>
    {
        public List<TraitEntry> Entries = new();
        public int TraitSubTreeID;
        public uint Active;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(TraitSubTreeID);
            data.WriteInt32(Entries.Count);
            for (int i = 0; i < Entries.Count; ++i)
            {
                Entries[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBits(Active, 1);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(TraitSubTreeCache right)
        {
            return Entries == right.Entries
                && TraitSubTreeID == right.TraitSubTreeID
                && Active == right.Active;
        }
    }

    public class TraitConfig() : HasChangesMask(15), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<TraitEntry> Entries = new(0, 1);
        public DynamicUpdateField<TraitSubTreeCache> SubTrees = new(0, 2);
        public UpdateField<int> ID = new(0, 3);
        public UpdateFieldString Name = new(4, 5);
        public UpdateField<int> Type = new(4, 6);
        public UpdateField<int> SkillLineID = new(4, 7);
        public UpdateField<int> ChrSpecializationID = new(8, 9);
        public UpdateField<int> CombatConfigFlags = new(8, 10);
        public UpdateField<int> LocalIdentifier = new(8, 11);
        public UpdateField<uint> TraitSystemID = new(12, 13);
        public UpdateField<int> VariationID = new(12, 14);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(ID);
            data.WriteInt32(Type);
            data.WriteInt32(Entries.Size());
            data.WriteInt32(SubTrees.Size());
            if (Type == 2)
            {
                data.WriteInt32(SkillLineID);
            }
            if (Type == 1)
            {
                data.WriteInt32(ChrSpecializationID);
                data.WriteInt32(CombatConfigFlags);
                data.WriteInt32(LocalIdentifier);
            }
            if (Type == 3)
            {
                data.WriteUInt32(TraitSystemID);
                data.WriteInt32(VariationID);
            }
            for (int i = 0; i < Entries.Size(); ++i)
            {
                Entries[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < SubTrees.Size(); ++i)
            {
                SubTrees[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBits(Name.Size(), 9);
            data.FlushBits();
            data.WriteString(Name);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 15);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreChangesMask)
                        Entries.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Entries.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreChangesMask)
                        SubTrees.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SubTrees.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Entries.Size(); ++i)
                    {
                        if (Entries.HasChanged(i) || ignoreChangesMask)
                        {
                            Entries[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < SubTrees.Size(); ++i)
                    {
                        if (SubTrees.HasChanged(i) || ignoreChangesMask)
                        {
                            SubTrees[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[3])
                {
                    data.WriteInt32(ID);
                }
            }
            if (changesMask[4])
            {
                if (changesMask[6])
                {
                    data.WriteInt32(Type);
                }
                if (changesMask[7])
                {
                    if (Type == 2)
                    {
                        data.WriteInt32(SkillLineID);
                    }
                }
            }
            if (changesMask[8])
            {
                if (changesMask[9])
                {
                    if (Type == 1)
                    {
                        data.WriteInt32(ChrSpecializationID);
                    }
                }
                if (changesMask[10])
                {
                    if (Type == 1)
                    {
                        data.WriteInt32(CombatConfigFlags);
                    }
                }
                if (changesMask[11])
                {
                    if (Type == 1)
                    {
                        data.WriteInt32(LocalIdentifier);
                    }
                }
            }
            if (changesMask[12])
            {
                if (changesMask[13])
                {
                    if (Type == 3)
                    {
                        data.WriteUInt32(TraitSystemID);
                    }
                }
                if (changesMask[14])
                {
                    if (Type == 3)
                    {
                        data.WriteInt32(VariationID);
                    }
                }
            }
            if (changesMask[4])
            {
                if (changesMask[5])
                {
                    data.WriteBits(Name.Size(), 9);
                }
            }
            data.FlushBits();
            if (changesMask[4])
            {
                if (changesMask[5])
                {
                    data.WriteString(Name);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Entries);
            ClearChangesMask(SubTrees);
            ClearChangesMask(ID);
            ClearChangesMask(Name);
            ClearChangesMask(Type);
            ClearChangesMask(SkillLineID);
            ClearChangesMask(ChrSpecializationID);
            ClearChangesMask(CombatConfigFlags);
            ClearChangesMask(LocalIdentifier);
            ClearChangesMask(TraitSystemID);
            ClearChangesMask(VariationID);
            _changesMask.ResetAll();
        }
    }

    public struct CraftingReagentBase : IEquatable<CraftingReagentBase>, IsUpdateFieldStructure<Player>
    {
        public int? ItemID;
        public int? CurrencyID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteBit(ItemID.HasValue);
            data.WriteBit(CurrencyID.HasValue);
            data.FlushBits();
            if (ItemID.HasValue)
            {
                data.WriteInt32(ItemID.Value);
            }
            if (CurrencyID.HasValue)
            {
                data.WriteInt32(CurrencyID.Value);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(CraftingReagentBase right)
        {
            return ItemID == right.ItemID
                && CurrencyID == right.CurrencyID;
        }
    }

    public class CraftingOrderItem() : HasChangesMask(7), IsUpdateFieldStructure<Player>
    {
        public UpdateField<ulong> OrderItemID = new(0, 1);
        public UpdateField<int> OrderItemType = new(0, 2);
        public UpdateField<ObjectGuid> ItemGUID = new(0, 3);
        public UpdateField<ObjectGuid> OwnerGUID = new(4, 5);
        public UpdateField<CraftingReagentBase> Reagent = new(4, 6);
        public UpdateField<uint> Quantity = new(4, 7);
        public UpdateField<int> ReagentQuality = new(8, 9);
        public OptionalUpdateField<byte> DataSlotIndex = new(8, 10);
        public UpdateField<uint> Flags = new(8, 11);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt64(OrderItemID);
            data.WriteInt32(OrderItemType);
            data.WritePackedGuid(ItemGUID);
            data.WritePackedGuid(OwnerGUID);
            Reagent.GetValue().WriteCreate(data, receiver, owner);
            data.WriteUInt32(Quantity);
            data.WriteInt32(ReagentQuality);
            data.WriteUInt32(Flags);
            data.WriteBit(DataSlotIndex.HasValue());
            data.FlushBits();
            if (DataSlotIndex.HasValue())
            {
                data.WriteUInt8(DataSlotIndex);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 12);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt64(OrderItemID);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(OrderItemType);
                }
                if (changesMask[3])
                {
                    data.WritePackedGuid(ItemGUID);
                }
            }
            if (changesMask[4])
            {
                if (changesMask[5])
                {
                    data.WritePackedGuid(OwnerGUID);
                }
                if (changesMask[6])
                {
                    Reagent.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(Quantity);
                }
            }
            if (changesMask[8])
            {
                if (changesMask[9])
                {
                    data.WriteInt32(ReagentQuality);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(Flags);
                }
                data.WriteBit(DataSlotIndex.HasValue());
            }
            data.FlushBits();
            if (changesMask[8])
            {
                if (changesMask[10])
                {
                    if (DataSlotIndex.HasValue())
                    {
                        data.WriteUInt8(DataSlotIndex);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(OrderItemID);
            ClearChangesMask(OrderItemType);
            ClearChangesMask(ItemGUID);
            ClearChangesMask(OwnerGUID);
            ClearChangesMask(Reagent);
            ClearChangesMask(Quantity);
            ClearChangesMask(ReagentQuality);
            ClearChangesMask(DataSlotIndex);
            ClearChangesMask(Flags);
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrderCustomer() : HasChangesMask(2), IsUpdateFieldStructure<Player>
    {
        public UpdateField<ObjectGuid> CustomerGUID = new(-1, 0);
        public UpdateField<ObjectGuid> CustomerAccountGUID = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(CustomerGUID);
            data.WritePackedGuid(CustomerAccountGUID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 2);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WritePackedGuid(CustomerGUID);
            }
            if (changesMask[1])
            {
                data.WritePackedGuid(CustomerAccountGUID);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(CustomerGUID);
            ClearChangesMask(CustomerAccountGUID);
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrderNpcCustomer() : HasChangesMask(2), IsUpdateFieldStructure<Player>
    {
        public UpdateField<long> NpcCraftingOrderCustomerID = new(-1, 0);
        public UpdateField<int> RealmAddress = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt64(NpcCraftingOrderCustomerID);
            data.WriteInt32(RealmAddress);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 2);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteInt64(NpcCraftingOrderCustomerID);
            }
            if (changesMask[1])
            {
                data.WriteInt32(RealmAddress);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(NpcCraftingOrderCustomerID);
            ClearChangesMask(RealmAddress);
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrderData() : HasChangesMask(26), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<CraftingOrderItem> Reagents = new(0, 1);
        public UpdateField<int> Field_0 = new(0, 2);
        public UpdateField<ulong> OrderID = new(0, 3);
        public UpdateField<int> SkillLineAbilityID = new(0, 4);
        public UpdateField<int> OrderState = new(0, 5);
        public UpdateField<byte> OrderType = new(6, 7);
        public UpdateField<byte> MinQuality = new(6, 8);
        public UpdateField<long> ExpirationTime = new(6, 9);
        public UpdateField<long> ClaimEndTime = new(6, 10);
        public UpdateField<long> TipAmount = new(6, 11);
        public UpdateField<long> ConsortiumCut = new(12, 13);
        public UpdateField<uint> Flags = new(12, 14);
        public UpdateField<ObjectGuid> CrafterGUID = new(12, 15);
        public UpdateField<ObjectGuid> PersonalCrafterGUID = new(12, 16);
        public UpdateField<int> NpcCraftingOrderSetID = new(12, 17);
        public UpdateField<int> NpcTreasureID = new(18, 19);
        public UpdateFieldString CustomerNotes = new(18, 20);
        public OptionalUpdateField<CraftingOrderCustomer> Customer = new(18, 21);
        public OptionalUpdateField<CraftingOrderNpcCustomer> NpcCustomer = new(18, 22);
        public OptionalUpdateField<CraftingOrderItem> OutputItem = new(18, 23);
        public OptionalUpdateField<ItemInstance> OutputItemData = new(24, 25);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(Field_0);
            data.WriteUInt64(OrderID);
            data.WriteInt32(SkillLineAbilityID);
            data.WriteInt32(OrderState);
            data.WriteUInt8(OrderType);
            data.WriteUInt8(MinQuality);
            data.WriteInt64(ExpirationTime);
            data.WriteInt64(ClaimEndTime);
            data.WriteInt64(TipAmount);
            data.WriteInt64(ConsortiumCut);
            data.WriteUInt32(Flags);
            data.WritePackedGuid(CrafterGUID);
            data.WritePackedGuid(PersonalCrafterGUID);
            data.WriteInt32(NpcCraftingOrderSetID);
            data.WriteInt32(NpcTreasureID);
            data.WriteInt32(Reagents.Size());
            for (int i = 0; i < Reagents.Size(); ++i)
            {
                Reagents[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBits(CustomerNotes.Size(), 10);
            data.WriteBit(Customer.HasValue());
            data.WriteBit(NpcCustomer.HasValue());
            data.WriteBit(OutputItem.HasValue());
            data.WriteBit(OutputItemData.HasValue());
            data.FlushBits();
            data.WriteString(CustomerNotes);
            if (Customer.HasValue())
            {
                Customer.GetValue().WriteCreate(data, receiver, owner);
            }
            if (NpcCustomer.HasValue())
            {
                NpcCustomer.GetValue().WriteCreate(data, receiver, owner);
            }
            if (OutputItem.HasValue())
            {
                OutputItem.GetValue().WriteCreate(data, receiver, owner);
            }
            if (OutputItemData.HasValue())
            {
                OutputItemData.GetValue().Write(data);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 26);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreChangesMask)
                        Reagents.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Reagents.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Reagents.Size(); ++i)
                    {
                        if (Reagents.HasChanged(i) || ignoreChangesMask)
                        {
                            Reagents[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    data.WriteInt32(Field_0);
                }
                if (changesMask[3])
                {
                    data.WriteUInt64(OrderID);
                }
                if (changesMask[4])
                {
                    data.WriteInt32(SkillLineAbilityID);
                }
                if (changesMask[5])
                {
                    data.WriteInt32(OrderState);
                }
            }
            if (changesMask[6])
            {
                if (changesMask[7])
                {
                    data.WriteUInt8(OrderType);
                }
                if (changesMask[8])
                {
                    data.WriteUInt8(MinQuality);
                }
                if (changesMask[9])
                {
                    data.WriteInt64(ExpirationTime);
                }
                if (changesMask[10])
                {
                    data.WriteInt64(ClaimEndTime);
                }
                if (changesMask[11])
                {
                    data.WriteInt64(TipAmount);
                }
            }
            if (changesMask[12])
            {
                if (changesMask[13])
                {
                    data.WriteInt64(ConsortiumCut);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(Flags);
                }
                if (changesMask[15])
                {
                    data.WritePackedGuid(CrafterGUID);
                }
                if (changesMask[16])
                {
                    data.WritePackedGuid(PersonalCrafterGUID);
                }
                if (changesMask[17])
                {
                    data.WriteInt32(NpcCraftingOrderSetID);
                }
            }
            if (changesMask[18])
            {
                if (changesMask[19])
                {
                    data.WriteInt32(NpcTreasureID);
                }
                if (changesMask[20])
                {
                    data.WriteBits(CustomerNotes.Size(), 10);
                }
                data.WriteBit(Customer.HasValue());
                data.WriteBit(NpcCustomer.HasValue());
                data.WriteBit(OutputItem.HasValue());
            }
            if (changesMask[24])
            {
                data.WriteBit(OutputItemData.HasValue());
            }
            data.FlushBits();
            if (changesMask[18])
            {
                if (changesMask[20])
                {
                    data.WriteString(CustomerNotes);
                }
                if (changesMask[21])
                {
                    if (Customer.HasValue())
                    {
                        Customer.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[22])
                {
                    if (NpcCustomer.HasValue())
                    {
                        NpcCustomer.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[23])
                {
                    if (OutputItem.HasValue())
                    {
                        OutputItem.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[24])
            {
                if (changesMask[25])
                {
                    if (OutputItemData.HasValue())
                    {
                        OutputItemData.GetValue().Write(data);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Reagents);
            ClearChangesMask(Field_0);
            ClearChangesMask(OrderID);
            ClearChangesMask(SkillLineAbilityID);
            ClearChangesMask(OrderState);
            ClearChangesMask(OrderType);
            ClearChangesMask(MinQuality);
            ClearChangesMask(ExpirationTime);
            ClearChangesMask(ClaimEndTime);
            ClearChangesMask(TipAmount);
            ClearChangesMask(ConsortiumCut);
            ClearChangesMask(Flags);
            ClearChangesMask(CrafterGUID);
            ClearChangesMask(PersonalCrafterGUID);
            ClearChangesMask(NpcCraftingOrderSetID);
            ClearChangesMask(NpcTreasureID);
            ClearChangesMask(CustomerNotes);
            ClearChangesMask(Customer);
            ClearChangesMask(NpcCustomer);
            ClearChangesMask(OutputItem);
            ClearChangesMask(OutputItemData);
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrder() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<ItemEnchantData> Enchantments = new(-1, 0);
        public DynamicUpdateField<ItemGemData> Gems = new(-1, 1);
        public UpdateField<CraftingOrderData> Data = new(-1, 2);
        public OptionalUpdateField<ItemInstance> RecraftItemInfo = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            Data.GetValue().WriteCreate(data, receiver, owner);
            data.WriteBit(RecraftItemInfo.HasValue());
            data.WriteBits(Enchantments.Size(), 4);
            data.WriteBits(Gems.Size(), 2);
            data.FlushBits();
            if (RecraftItemInfo.HasValue())
            {
                RecraftItemInfo.GetValue().Write(data);
            }
            for (int i = 0; i < Enchantments.Size(); ++i)
            {
                Enchantments[i].Write(data);
            }
            for (int i = 0; i < Gems.Size(); ++i)
            {
                Gems[i].Write(data);
            }
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            if (changesMask[0])
            {
                if (!ignoreChangesMask)
                    Enchantments.WriteUpdateMask(data, 4);
                else
                    WriteCompleteDynamicFieldUpdateMask(Enchantments.Size(), data, 4);
            }
            if (changesMask[1])
            {
                if (!ignoreChangesMask)
                    Gems.WriteUpdateMask(data, 2);
                else
                    WriteCompleteDynamicFieldUpdateMask(Gems.Size(), data, 2);
            }
            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < Enchantments.Size(); ++i)
                {
                    if (Enchantments.HasChanged(i) || ignoreChangesMask)
                    {
                        Enchantments[i].Write(data);
                    }
                }
            }
            if (changesMask[1])
            {
                for (int i = 0; i < Gems.Size(); ++i)
                {
                    if (Gems.HasChanged(i) || ignoreChangesMask)
                    {
                        Gems[i].Write(data);
                    }
                }
            }

            if (changesMask[2])
            {
                Data.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
            }
            data.WriteBit(RecraftItemInfo.HasValue());
            data.FlushBits();
            if (changesMask[3])
            {
                if (RecraftItemInfo.HasValue())
                {
                    RecraftItemInfo.GetValue().Write(data);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Enchantments);
            ClearChangesMask(Gems);
            ClearChangesMask(Data);
            ClearChangesMask(RecraftItemInfo);
            _changesMask.ResetAll();
        }
    }

    public class PersonalCraftingOrderCount() : HasChangesMask(2), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> ProfessionID = new(-1, 0);
        public UpdateField<uint> Count = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(ProfessionID);
            data.WriteUInt32(Count);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 2);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteInt32(ProfessionID);
            }
            if (changesMask[1])
            {
                data.WriteUInt32(Count);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ProfessionID);
            ClearChangesMask(Count);
            _changesMask.ResetAll();
        }
    }

    public class NPCCraftingOrderInfo() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public UpdateField<ulong> OrderID = new(-1, 0);
        public UpdateField<int> NpcCraftingOrderSetID = new(-1, 1);
        public UpdateField<int> NpcTreasureID = new(-1, 2);
        public UpdateField<int> NpcCraftingOrderCustomerID = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt64(OrderID);
            data.WriteInt32(NpcCraftingOrderSetID);
            data.WriteInt32(NpcTreasureID);
            data.WriteInt32(NpcCraftingOrderCustomerID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteUInt64(OrderID);
            }
            if (changesMask[1])
            {
                data.WriteInt32(NpcCraftingOrderSetID);
            }
            if (changesMask[2])
            {
                data.WriteInt32(NpcTreasureID);
            }
            if (changesMask[3])
            {
                data.WriteInt32(NpcCraftingOrderCustomerID);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(OrderID);
            ClearChangesMask(NpcCraftingOrderSetID);
            ClearChangesMask(NpcTreasureID);
            ClearChangesMask(NpcCraftingOrderCustomerID);
            _changesMask.ResetAll();
        }
    }

    public struct CategoryCooldownMod : IEquatable<CategoryCooldownMod>, IsUpdateFieldStructure<Player>
    {
        public int SpellCategoryID;
        public int ModCooldown;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteInt32(ModCooldown);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(CategoryCooldownMod right)
        {
            return SpellCategoryID == right.SpellCategoryID
                && ModCooldown == right.ModCooldown;
        }
    }

    public struct WeeklySpellUse : IEquatable<WeeklySpellUse>, IsUpdateFieldStructure<Player>
    {
        public int SpellCategoryID;
        public byte Uses;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteUInt8(Uses);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(WeeklySpellUse right)
        {
            return SpellCategoryID == right.SpellCategoryID
                && Uses == right.Uses;
        }
    }

    public class StablePetInfo() : HasChangesMask(9), IsUpdateFieldStructure<Player>
    {
        public UpdateField<uint> PetSlot = new(0, 1);
        public UpdateField<uint> PetNumber = new(0, 2);
        public UpdateField<uint> CreatureID = new(0, 3);
        public UpdateField<uint> DisplayID = new(0, 4);
        public UpdateField<uint> ExperienceLevel = new(0, 5);
        public UpdateFieldString Name = new(0, 6);
        public UpdateField<byte> PetFlags = new(0, 7);
        public UpdateField<uint> Specialization = new(0, 8);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(PetSlot);
            data.WriteUInt32(PetNumber);
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(DisplayID);
            data.WriteUInt32(ExperienceLevel);
            data.WriteUInt8(PetFlags);
            data.WriteUInt32(Specialization);
            data.WriteBits(Name.Size(), 8);
            data.FlushBits();
            data.WriteString(Name);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 9);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(PetSlot);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(PetNumber);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(CreatureID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(DisplayID);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(ExperienceLevel);
                }
                if (changesMask[7])
                {
                    data.WriteUInt8(PetFlags);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(Specialization);
                }
                if (changesMask[6])
                {
                    data.WriteBits(Name.Size(), 8);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[6])
                {
                    data.WriteString(Name);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(PetSlot);
            ClearChangesMask(PetNumber);
            ClearChangesMask(CreatureID);
            ClearChangesMask(DisplayID);
            ClearChangesMask(ExperienceLevel);
            ClearChangesMask(Name);
            ClearChangesMask(PetFlags);
            ClearChangesMask(Specialization);
            _changesMask.ResetAll();
        }
    }

    public class StableInfo() : HasChangesMask(3), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<StablePetInfo> Pets = new(0, 1);
        public UpdateField<ObjectGuid> StableMaster = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(Pets.Size());
            data.WritePackedGuid(StableMaster);
            for (int i = 0; i < Pets.Size(); ++i)
            {
                Pets[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreChangesMask)
                        Pets.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Pets.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Pets.Size(); ++i)
                    {
                        if (Pets.HasChanged(i) || ignoreChangesMask)
                        {
                            Pets[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    data.WritePackedGuid(StableMaster);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Pets);
            ClearChangesMask(StableMaster);
            _changesMask.ResetAll();
        }
    }

    public class CollectableSourceTrackedData() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> TargetType = new(0, 1);
        public UpdateField<int> TargetID = new(0, 2);
        public UpdateField<int> CollectableSourceInfoID = new(0, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(TargetType);
            data.WriteInt32(TargetID);
            data.WriteInt32(CollectableSourceInfoID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(TargetType);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(TargetID);
                }
                if (changesMask[3])
                {
                    data.WriteInt32(CollectableSourceInfoID);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(TargetType);
            ClearChangesMask(TargetID);
            ClearChangesMask(CollectableSourceInfoID);
            _changesMask.ResetAll();
        }
    }

    public class BankTabSettings() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public UpdateFieldString Name = new(-1, 0);
        public UpdateFieldString Icon = new(-1, 1);
        public UpdateFieldString Description = new(-1, 2);
        public UpdateField<int> DepositFlags = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteBits(Name.Size(), 7);
            data.WriteBits(Icon.Size(), 9);
            data.WriteBits(Description.Size(), 14);
            data.WriteInt32(DepositFlags);
            data.WriteString(Name);
            data.WriteString(Icon);
            data.WriteString(Description);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            if (changesMask[0])
            {
                data.WriteBits(Name.Size(), 7);
            }
            if (changesMask[1])
            {
                data.WriteBits(Icon.Size(), 9);
            }
            if (changesMask[2])
            {
                data.WriteBits(Description.Size(), 14);
            }
            data.FlushBits();
            if (changesMask[3])
            {
                data.WriteInt32(DepositFlags);
            }
            if (changesMask[0])
            {
                data.WriteString(Name);
            }
            if (changesMask[1])
            {
                data.WriteString(Icon);
            }
            if (changesMask[2])
            {
                data.WriteString(Description);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Name);
            ClearChangesMask(Icon);
            ClearChangesMask(Description);
            ClearChangesMask(DepositFlags);
            _changesMask.ResetAll();
        }
    }

    public struct WalkInData : IEquatable<WalkInData>, IsUpdateFieldStructure<Player>
    {
        public int MapID;
        public long InstanceID;
        public uint WalkInInstanceType;
        public ObjectGuid WalkInPartyGUID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(MapID);
            data.WriteInt64(InstanceID);
            data.WritePackedGuid(WalkInPartyGUID);
            data.WriteBits(WalkInInstanceType, 1);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(WalkInData right)
        {
            return MapID == right.MapID
                && InstanceID == right.InstanceID
                && WalkInInstanceType == right.WalkInInstanceType
                && WalkInPartyGUID == right.WalkInPartyGUID;
        }
    }

    public class DelveData : IEquatable<DelveData>, IsUpdateFieldStructure<Player>
    {
        public List<ObjectGuid> PlayersEligibleForRewards = [];
        public List<int> ActiveOptionalAffixIDs = [];
        public int MapID;
        public int Tier;
        public ulong InstanceID;
        public int EntranceType;
        public uint RestrictingRewardPlayers;                                        // Restricts rewards to players in m_owners if set to true. Intended to prevent rewarwding players that join in-progress delve?
        public uint IsLfg;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(MapID);
            data.WriteInt32(Tier);
            data.WriteUInt64(InstanceID);
            data.WriteInt32(PlayersEligibleForRewards.Count);
            data.WriteInt32(ActiveOptionalAffixIDs.Count);
            data.WriteInt32(EntranceType);
            for (int i = 0; i < PlayersEligibleForRewards.Count; ++i)
            {
                data.WritePackedGuid(PlayersEligibleForRewards[i]);
            }
            for (int i = 0; i < ActiveOptionalAffixIDs.Count; ++i)
            {
                data.WriteInt32(ActiveOptionalAffixIDs[i]);
            }
            data.WriteBits(RestrictingRewardPlayers, 1);
            data.WriteBits(IsLfg, 1);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(DelveData right)
        {
            return PlayersEligibleForRewards == right.PlayersEligibleForRewards
                && ActiveOptionalAffixIDs == right.ActiveOptionalAffixIDs
                && MapID == right.MapID
                && Tier == right.Tier
                && InstanceID == right.InstanceID
                && EntranceType == right.EntranceType
                && RestrictingRewardPlayers == right.RestrictingRewardPlayers
                && IsLfg == right.IsLfg;
        }
    }

    public class TransmogOutfitDataInfo() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public UpdateField<bool> SituationsEnabled = new(-1, 0);
        public UpdateField<byte> SetType = new(-1, 1);
        public UpdateFieldString Name = new(-1, 2);
        public UpdateField<uint> Icon = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt8(SetType);
            data.WriteUInt32(Icon);
            data.WriteBits(Name.Size(), 8);
            data.WriteBit(SituationsEnabled);
            data.FlushBits();
            data.WriteString(Name);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            if (changesMask[0])
            {
                data.WriteBit(SituationsEnabled);
            }
            data.FlushBits();
            if (changesMask[1])
            {
                data.WriteUInt8(SetType);
            }
            if (changesMask[3])
            {
                data.WriteUInt32(Icon);
            }
            if (changesMask[2])
            {
                data.WriteBits(Name.Size(), 8);
                data.WriteString(Name);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(SituationsEnabled);
            ClearChangesMask(SetType);
            ClearChangesMask(Name);
            ClearChangesMask(Icon);
            _changesMask.ResetAll();
        }
    }

    public class TransmogOutfitSituationInfo() : HasChangesMask(4), IsUpdateFieldStructure<Player>
    {
        public UpdateField<uint> SituationID = new(-1, 0);
        public UpdateField<uint> SpecID = new(-1, 1);
        public UpdateField<uint> LoadoutID = new(-1, 2);
        public UpdateField<uint> EquipmentSetID = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(SituationID);
            data.WriteUInt32(SpecID);
            data.WriteUInt32(LoadoutID);
            data.WriteUInt32(EquipmentSetID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteUInt32(SituationID);
            }
            if (changesMask[1])
            {
                data.WriteUInt32(SpecID);
            }
            if (changesMask[2])
            {
                data.WriteUInt32(LoadoutID);
            }
            if (changesMask[3])
            {
                data.WriteUInt32(EquipmentSetID);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(SituationID);
            ClearChangesMask(SpecID);
            ClearChangesMask(LoadoutID);
            ClearChangesMask(EquipmentSetID);
            _changesMask.ResetAll();
        }
    }

    public class TransmogOutfitSlotData() : HasChangesMask(11), IsUpdateFieldStructure<Player>
    {
        public UpdateField<sbyte> Slot = new(0, 1);
        public UpdateField<byte> SlotOption = new(0, 2);
        public UpdateField<byte> SheatheCategory = new(0, 3);
        public UpdateField<uint> ItemModifiedAppearanceID = new(4, 5);
        public UpdateField<byte> AppearanceDisplayType = new(4, 6);
        public UpdateField<uint> SpellItemEnchantmentID = new(4, 7);
        public UpdateField<byte> IllusionDisplayType = new(8, 9);
        public UpdateField<uint> Flags = new(8, 10);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt8(Slot);
            data.WriteUInt8(SlotOption);
            data.WriteUInt8(SheatheCategory);
            data.WriteUInt32(ItemModifiedAppearanceID);
            data.WriteUInt8(AppearanceDisplayType);
            data.WriteUInt32(SpellItemEnchantmentID);
            data.WriteUInt8(IllusionDisplayType);
            data.WriteUInt32(Flags);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 11);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt8(Slot);
                }
                if (changesMask[2])
                {
                    data.WriteUInt8(SlotOption);
                }
                if (changesMask[3])
                {
                    data.WriteUInt8(SheatheCategory);
                }
            }
            if (changesMask[4])
            {
                if (changesMask[5])
                {
                    data.WriteUInt32(ItemModifiedAppearanceID);
                }
                if (changesMask[6])
                {
                    data.WriteUInt8(AppearanceDisplayType);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(SpellItemEnchantmentID);
                }
            }
            if (changesMask[8])
            {
                if (changesMask[9])
                {
                    data.WriteUInt8(IllusionDisplayType);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(Flags);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Slot);
            ClearChangesMask(SlotOption);
            ClearChangesMask(SheatheCategory);
            ClearChangesMask(ItemModifiedAppearanceID);
            ClearChangesMask(AppearanceDisplayType);
            ClearChangesMask(SpellItemEnchantmentID);
            ClearChangesMask(IllusionDisplayType);
            ClearChangesMask(Flags);
            _changesMask.ResetAll();
        }
    }

    public class TransmogOutfitData() : HasChangesMask(5), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<TransmogOutfitSituationInfo> Situations = new(-1, 0);
        public DynamicUpdateField<TransmogOutfitSlotData> Slots = new(-1, 1);
        public UpdateField<uint> Id = new(-1, 2);
        public UpdateField<TransmogOutfitDataInfo> OutfitInfo = new(-1, 3);
        public UpdateField<uint> Flags = new(-1, 4);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(Id);
            OutfitInfo.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(Situations.Size());
            data.WriteInt32(Slots.Size());
            data.WriteUInt32(Flags);
            for (int i = 0; i < Situations.Size(); ++i)
            {
                Situations[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < Slots.Size(); ++i)
            {
                Slots[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 5);

            if (changesMask[0])
            {
                if (!ignoreChangesMask)
                    Situations.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(Situations.Size(), data);
            }
            if (changesMask[1])
            {
                if (!ignoreChangesMask)
                    Slots.WriteUpdateMask(data);
                else
                    WriteCompleteDynamicFieldUpdateMask(Slots.Size(), data);
            }
            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < Situations.Size(); ++i)
                {
                    if (Situations.HasChanged(i) || ignoreChangesMask)
                    {
                        Situations[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[1])
            {
                for (int i = 0; i < Slots.Size(); ++i)
                {
                    if (Slots.HasChanged(i) || ignoreChangesMask)
                    {
                        Slots[i].WriteUpdate(ignoreChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[2])
            {
                data.WriteUInt32(Id);
            }
            if (changesMask[3])
            {
                OutfitInfo.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
            }
            if (changesMask[4])
            {
                data.WriteUInt32(Flags);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Situations);
            ClearChangesMask(Slots);
            ClearChangesMask(Id);
            ClearChangesMask(OutfitInfo);
            ClearChangesMask(Flags);
            _changesMask.ResetAll();
        }
    }

    public struct ChallengeModeData : IEquatable<ChallengeModeData>, IsUpdateFieldStructure<Player>
    {
        public int MapID;
        public int InitialPlayerCount;
        public ulong InstanceID;
        public long StartTime;
        public ObjectGuid KeystoneOwnerGUID;
        public ObjectGuid LeaverGUID;
        public long InstanceAbandonVoteCooldown;
        public uint IsActive;
        public uint HasRestrictions;
        public uint CanVoteAbandon;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(MapID);
            data.WriteInt32(InitialPlayerCount);
            data.WriteUInt64(InstanceID);
            data.WriteInt64(StartTime);
            data.WritePackedGuid(KeystoneOwnerGUID);
            data.WritePackedGuid(LeaverGUID);
            data.WriteInt64(InstanceAbandonVoteCooldown);
            data.WriteBits(IsActive, 1);
            data.WriteBits(HasRestrictions, 1);
            data.WriteBits(CanVoteAbandon, 1);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(ChallengeModeData right)
        {
            return MapID == right.MapID
                && InitialPlayerCount == right.InitialPlayerCount
                && InstanceID == right.InstanceID
                && StartTime == right.StartTime
                && KeystoneOwnerGUID == right.KeystoneOwnerGUID
                && LeaverGUID == right.LeaverGUID
                && InstanceAbandonVoteCooldown == right.InstanceAbandonVoteCooldown
                && IsActive == right.IsActive
                && HasRestrictions == right.HasRestrictions
                && CanVoteAbandon == right.CanVoteAbandon;
        }
    }

    public class TransmogOutfitMetadata : IEquatable<TransmogOutfitMetadata>, IsUpdateFieldStructure<Player>
    {
        public bool Locked;
        public byte SituationTrigger;
        public uint TransmogOutfitID;
        public byte StampedOptionMainHand;
        public byte StampedOptionOffHand;
        public float CostMod;                                                       // Used only with SPELL_AURA_MOD_TRANSMOG_OUTFIT_UPDATE_COST

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt8(SituationTrigger);
            data.WriteUInt32(TransmogOutfitID);
            data.WriteUInt8(StampedOptionMainHand);
            data.WriteUInt8(StampedOptionOffHand);
            data.WriteFloat(CostMod);
            data.WriteBit(Locked);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(TransmogOutfitMetadata right)
        {
            return Locked == right.Locked
            && SituationTrigger == right.SituationTrigger
            && TransmogOutfitID == right.TransmogOutfitID
            && StampedOptionMainHand == right.StampedOptionMainHand
            && StampedOptionOffHand == right.StampedOptionOffHand
            && CostMod == right.CostMod;
        }
    }

    public class ActivePlayerData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.ActivePlayer, 398), IsUpdateFieldStructure<Player>
    {
        public static int QuestCompletedBitsPerBlock = sizeof(ulong) * 8;

        public UpdateField<bool> BackpackAutoSortDisabled = new(0, 1);
        public UpdateField<bool> BackpackSellJunkDisabled = new(0, 2);
        public UpdateField<bool> BankAutoSortDisabled = new(0, 3);
        public UpdateField<bool> SortBagsRightToLeft = new(0, 4);
        public UpdateField<bool> InsertItemsLeftToRight = new(0, 5);
        public UpdateField<bool> HasPerksProgramPendingReward = new(0, 6);
        public DynamicUpdateField<ulong> KnownTitles = new(0, 7);
        public DynamicUpdateField<PlayerDataElement> CharacterDataElements = new(0, 8);
        public DynamicUpdateField<PlayerDataElement> AccountDataElements = new(0, 9);
        public DynamicUpdateField<PVPInfo> PvpInfo = new(0, 10);
        public DynamicUpdateField<uint> DailyQuestsCompleted = new(0, 11);
        public DynamicUpdateField<int> Field_1328 = new(0, 12);
        public DynamicUpdateField<int> AvailableQuestLineXQuestIDs = new(0, 13);
        public DynamicUpdateField<uint> Heirlooms = new(0, 14);
        public DynamicUpdateField<uint> HeirloomFlags = new(0, 15);
        public DynamicUpdateField<uint> Toys = new(0, 16);
        public DynamicUpdateField<uint> ToyFlags = new(0, 17);
        public DynamicUpdateField<uint> Transmog = new(0, 18);
        public DynamicUpdateField<uint> ConditionalTransmog = new(0, 19);
        public DynamicUpdateField<uint> SelfResSpells = new(0, 20);
        public DynamicUpdateField<uint> RuneforgePowers = new(0, 21);
        public DynamicUpdateField<uint> TransmogIllusions = new(0, 22);
        public DynamicUpdateField<uint> WarbandScenes = new(0, 23);
        public DynamicUpdateField<uint> HouseRooms = new(0, 24);
        public DynamicUpdateField<uint> HouseExteriorComponents = new(0, 25);
        public DynamicUpdateField<uint> HouseThemes = new(0, 26);
        public DynamicUpdateField<uint> HouseRoomComponentTextures = new(0, 27);
        public DynamicUpdateField<uint> HouseTypes = new(0, 28);
        public DynamicUpdateField<int> UnlockedTransmogOutfits = new(0, 29);
        public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new(0, 30);
        public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new(0, 31);
        public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new(32, 33);
        public DynamicUpdateField<SpellPctPVPModByLabel> SpellPctModPVPByLabel = new(32, 34);
        public DynamicUpdateField<SpellFlatPVPModByLabel> SpellFlatModPVPByLabel = new(32, 35);
        public DynamicUpdateField<MawPower> MawPowers = new(32, 36);
        public DynamicUpdateField<MultiFloorExplore> MultiFloorExploration = new(32, 37);
        public DynamicUpdateField<RecipeProgressionInfo> RecipeProgression = new(32, 38);
        public DynamicUpdateField<ReplayedQuest> ReplayedQuests = new(32, 39);
        public DynamicUpdateField<QuestLog> TaskQuests = new(32, 40);
        public DynamicUpdateField<int> DisabledSpells = new(32, 41);
        public DynamicUpdateField<CraftingOrder> CraftingOrders = new(32, 42);
        public DynamicUpdateField<PersonalCraftingOrderCount> PersonalCraftingOrderCounts = new(32, 43);
        public DynamicUpdateField<NPCCraftingOrderInfo> NpcCraftingOrders = new(32, 44);
        public DynamicUpdateField<CategoryCooldownMod> CategoryCooldownMods = new(32, 45);
        public DynamicUpdateField<WeeklySpellUse> WeeklySpellUses = new(32, 46);
        public DynamicUpdateField<CollectableSourceTrackedData> TrackedCollectableSources = new(32, 47);
        public DynamicUpdateField<BankTabSettings> CharacterBankTabSettings = new(32, 48);
        public DynamicUpdateField<BankTabSettings> AccountBankTabSettings = new(32, 49);
        public UpdateFieldArray<DynamicUpdateField<ushort>> ResearchSites = new(1, 50, 51);
        public UpdateFieldArray<DynamicUpdateField<uint>> ResearchSiteProgress = new(1, 52, 53);
        public UpdateFieldArray<DynamicUpdateField<Research>> Research = new(1, 54, 55);
        public UpdateField<ObjectGuid> FarsightObject = new(32, 56);
        public UpdateField<ObjectGuid> SummonedBattlePetGUID = new(32, 57);
        public UpdateField<ulong> Coinage = new(32, 58);
        public UpdateField<ulong> AccountBankCoinage = new(32, 59);
        public UpdateField<uint> XP = new(32, 60);
        public UpdateField<uint> NextLevelXP = new(32, 61);
        public UpdateField<int> TrialXP = new(32, 62);
        public UpdateField<SkillInfo> Skill = new(32, 63);
        public UpdateField<uint> CharacterPoints = new(32, 64);
        public UpdateField<uint> MaxTalentTiers = new(32, 65);
        public UpdateField<uint> TrackCreatureMask = new(32, 66);
        public UpdateField<float> MainhandExpertise = new(32, 67);
        public UpdateField<float> OffhandExpertise = new(32, 68);
        public UpdateField<float> RangedExpertise = new(32, 69);
        public UpdateField<float> CombatRatingExpertise = new(70, 71);
        public UpdateField<float> BlockPercentage = new(70, 72);
        public UpdateField<float> DodgePercentage = new(70, 73);
        public UpdateField<float> DodgePercentageFromAttribute = new(70, 74);
        public UpdateField<float> ParryPercentage = new(70, 75);
        public UpdateField<float> ParryPercentageFromAttribute = new(70, 76);
        public UpdateField<float> CritPercentage = new(70, 77);
        public UpdateField<float> RangedCritPercentage = new(70, 78);
        public UpdateField<float> OffhandCritPercentage = new(70, 79);
        public UpdateField<float> SpellCritPercentage = new(70, 80);
        public UpdateField<uint> ShieldBlock = new(70, 81);
        public UpdateField<float> ShieldBlockCritPercentage = new(70, 82);
        public UpdateField<float> Mastery = new(70, 83);
        public UpdateField<float> Speed = new(70, 84);
        public UpdateField<float> Avoidance = new(70, 85);
        public UpdateField<float> Sturdiness = new(70, 86);
        public UpdateField<int> Versatility = new(70, 87);
        public UpdateField<float> VersatilityBonus = new(70, 88);
        public UpdateField<float> PvpPowerDamage = new(70, 89);
        public UpdateField<float> PvpPowerHealing = new(70, 90);
        public UpdateField<BitVectors> BitVectors = new(70, 91);
        public UpdateField<int> ModHealingDonePos = new(70, 92);
        public UpdateField<float> ModHealingPercent = new(70, 93);
        public UpdateField<float> ModPeriodicHealingDonePercent = new(70, 94);
        public UpdateField<float> ModSpellPowerPercent = new(70, 95);
        public UpdateField<float> ModResiliencePercent = new(70, 96);
        public UpdateField<float> OverrideSpellPowerByAPPercent = new(70, 97);
        public UpdateField<float> OverrideAPBySpellPowerPercent = new(70, 98);
        public UpdateField<int> ModTargetResistance = new(70, 99);
        public UpdateField<int> ModTargetPhysicalResistance = new(70, 100);
        public UpdateField<uint> LocalFlags = new(70, 101);
        public UpdateField<byte> GrantableLevels = new(102, 103);
        public UpdateField<byte> MultiActionBars = new(102, 104);
        public UpdateField<byte> LifetimeMaxRank = new(102, 105);
        public UpdateField<byte> NumRespecs = new(102, 106);
        public UpdateField<uint> PvpMedals = new(102, 107);
        public UpdateField<ushort> TodayHonorableKills = new(102, 108);
        public UpdateField<ushort> YesterdayHonorableKills = new(102, 109);
        public UpdateField<uint> LifetimeHonorableKills = new(102, 110);
        public UpdateField<uint> WatchedFactionIndex = new(102, 111);
        public UpdateField<uint> MaxLevel = new(102, 112);
        public UpdateField<int> ScalingPlayerLevelDelta = new(102, 113);
        public UpdateField<int> MaxCreatureScalingLevel = new(102, 114);
        public UpdateField<byte> TransmogCostMinScalingLevel = new(102, 115);
        public UpdateField<uint> PetSpellPower = new(102, 116);
        public UpdateField<float> UiHitModifier = new(102, 117);
        public UpdateField<float> UiSpellHitModifier = new(102, 118);
        public UpdateField<int> HomeRealmTimeOffset = new(102, 119);
        public UpdateField<float> ModPetHaste = new(102, 120);
        public UpdateField<sbyte> JailersTowerLevelMax = new(102, 121);
        public UpdateField<sbyte> JailersTowerLevel = new(102, 122);
        public UpdateField<byte> LocalRegenFlags = new(102, 123);
        public UpdateField<byte> AuraVision = new(102, 124);
        public UpdateField<byte> NumBackpackSlots = new(102, 125);
        public UpdateField<uint> OverrideSpellsID = new(102, 126);
        public UpdateField<ushort> LootSpecID = new(102, 127);
        public UpdateField<uint> OverrideZonePVPType = new(102, 128);
        public UpdateField<uint> Honor = new(102, 129);
        public UpdateField<uint> HonorNextLevel = new(102, 130);
        public UpdateField<int> PerksProgramCurrency = new(102, 131);
        public UpdateField<byte> NumBankSlots = new(102, 132);
        public UpdateField<byte> NumCharacterBankTabs = new(102, 133);
        public UpdateField<byte> NumAccountBankTabs = new(134, 135);
        public UpdateField<ResearchHistory> ResearchHistory = new(134, 136);
        public UpdateField<PerksVendorItem> FrozenPerksVendorItem = new(134, 137);
        public UpdateField<DiscordPlayerInfo> DiscordInfo = new(134, 138);
        public OptionalUpdateField<QuestSession> QuestSession = new(134, 139);
        public UpdateField<LevelLinkInfo> LevelLinkInfo = new(134, 140);
        public UpdateField<int> UiChromieTimeExpansionID = new(134, 141);
        public UpdateField<int> TimerunningSeasonID = new(134, 142);
        public UpdateField<int> TransportServerTime = new(134, 143);
        public UpdateField<uint> WeeklyRewardsPeriodSinceOrigin = new(134, 144);               // week count since Cfg_RegionsEntry::ChallengeOrigin
        public UpdateField<short> DEBUGSoulbindConduitRank = new(134, 145);
        public UpdateField<DungeonScoreData> DungeonScore = new(134, 146);
        public MapUpdateField<int, TraitConfig> TraitConfigs = new(134, 147);
        public UpdateField<uint> ActiveCombatTraitConfigID = new(134, 148);
        public UpdateField<int> ItemUpgradeHighOnehandWeaponItemID = new(134, 149);
        public UpdateField<int> ItemUpgradeHighFingerItemID = new(134, 150);
        public UpdateField<float> ItemUpgradeHighFingerWatermark = new(134, 151);
        public UpdateField<int> ItemUpgradeHighTrinketItemID = new(134, 152);
        public UpdateField<float> ItemUpgradeHighTrinketWatermark = new(134, 153);
        public UpdateField<ulong> LootHistoryInstanceID = new(134, 154);
        public OptionalUpdateField<StableInfo> PetStable = new(134, 155);
        public UpdateField<byte> RequiredMountCapabilityFlags = new(134, 156);
        public OptionalUpdateField<WalkInData> WalkInData = new(134, 157);
        public MapUpdateField<int, DelveData> DelveData = new(134, 158);
        public MapUpdateField<uint, TransmogOutfitData> TransmogOutfits = new(134, 159);
        public OptionalUpdateField<ChallengeModeData> ChallengeModeData = new(134, 160);
        public UpdateField<TransmogOutfitData> ViewedOutfit = new(134, 161);
        public UpdateField<TransmogOutfitMetadata> TransmogMetadata = new(134, 162);
        public UpdateFieldArray<ObjectGuid> InvSlots = new(105, 163, 164);
        public UpdateFieldArray<RestInfo> RestInfo = new(2, 269, 270);
        public UpdateFieldArray<int> ModDamageDonePos = new(7, 272, 273);
        public UpdateFieldArray<int> ModDamageDoneNeg = new(7, 272, 280);
        public UpdateFieldArray<float> ModDamageDonePercent = new(7, 272, 287);
        public UpdateFieldArray<float> ModHealingDonePercent = new(7, 272, 294);
        public UpdateFieldArray<float> WeaponDmgMultipliers = new(3, 301, 302);
        public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new(3, 301, 305);
        public UpdateFieldArray<uint> BuybackPrice = new(12, 308, 309);
        public UpdateFieldArray<long> BuybackTimestamp = new(12, 308, 321);
        public UpdateFieldArray<uint> CombatRatings = new(32, 333, 334);
        public UpdateFieldArray<uint> NoReagentCostMask = new(4, 366, 367);
        public UpdateFieldArray<uint> ProfessionSkillLine = new(2, 371, 372);
        public UpdateFieldArray<uint> BagSlotFlags = new(5, 374, 375);
        public UpdateFieldArray<float> ItemUpgradeHighWatermark = new(17, 380, 381);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            for (int i = 0; i < 105; ++i)
            {
                data.WritePackedGuid(InvSlots[i]);
            }
            data.WritePackedGuid(FarsightObject);
            data.WritePackedGuid(SummonedBattlePetGUID);
            data.WriteInt32(KnownTitles.Size());
            data.WriteUInt64(Coinage);
            data.WriteUInt64(AccountBankCoinage);
            data.WriteUInt32(XP);
            data.WriteUInt32(NextLevelXP);
            data.WriteInt32(TrialXP);
            Skill.GetValue().WriteCreate(data, receiver, owner);
            data.WriteUInt32(CharacterPoints);
            data.WriteUInt32(MaxTalentTiers);
            data.WriteUInt32(TrackCreatureMask);
            data.WriteFloat(MainhandExpertise);
            data.WriteFloat(OffhandExpertise);
            data.WriteFloat(RangedExpertise);
            data.WriteFloat(CombatRatingExpertise);
            data.WriteFloat(BlockPercentage);
            data.WriteFloat(DodgePercentage);
            data.WriteFloat(DodgePercentageFromAttribute);
            data.WriteFloat(ParryPercentage);
            data.WriteFloat(ParryPercentageFromAttribute);
            data.WriteFloat(CritPercentage);
            data.WriteFloat(RangedCritPercentage);
            data.WriteFloat(OffhandCritPercentage);
            data.WriteFloat(SpellCritPercentage);
            data.WriteUInt32(ShieldBlock);
            data.WriteFloat(ShieldBlockCritPercentage);
            data.WriteFloat(Mastery);
            data.WriteFloat(Speed);
            data.WriteFloat(Avoidance);
            data.WriteFloat(Sturdiness);
            data.WriteInt32(Versatility);
            data.WriteFloat(VersatilityBonus);
            data.WriteFloat(PvpPowerDamage);
            data.WriteFloat(PvpPowerHealing);
            BitVectors.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(CharacterDataElements.Size());
            data.WriteInt32(AccountDataElements.Size());
            for (int i = 0; i < 2; ++i)
            {
                RestInfo[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < 7; ++i)
            {
                data.WriteInt32(ModDamageDonePos[i]);
                data.WriteInt32(ModDamageDoneNeg[i]);
                data.WriteFloat(ModDamageDonePercent[i]);
                data.WriteFloat(ModHealingDonePercent[i]);
            }
            data.WriteInt32(ModHealingDonePos);
            data.WriteFloat(ModHealingPercent);
            data.WriteFloat(ModPeriodicHealingDonePercent);
            for (int i = 0; i < 3; ++i)
            {
                data.WriteFloat(WeaponDmgMultipliers[i]);
                data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
            }
            data.WriteFloat(ModSpellPowerPercent);
            data.WriteFloat(ModResiliencePercent);
            data.WriteFloat(OverrideSpellPowerByAPPercent);
            data.WriteFloat(OverrideAPBySpellPowerPercent);
            data.WriteInt32(ModTargetResistance);
            data.WriteInt32(ModTargetPhysicalResistance);
            data.WriteUInt32(LocalFlags);
            data.WriteUInt8(GrantableLevels);
            data.WriteUInt8(MultiActionBars);
            data.WriteUInt8(LifetimeMaxRank);
            data.WriteUInt8(NumRespecs);
            data.WriteUInt32(PvpMedals);
            for (int i = 0; i < 12; ++i)
            {
                data.WriteUInt32(BuybackPrice[i]);
                data.WriteInt64(BuybackTimestamp[i]);
            }
            data.WriteUInt16(TodayHonorableKills);
            data.WriteUInt16(YesterdayHonorableKills);
            data.WriteUInt32(LifetimeHonorableKills);
            data.WriteUInt32(WatchedFactionIndex);
            for (int i = 0; i < 32; ++i)
            {
                data.WriteUInt32(CombatRatings[i]);
            }
            data.WriteInt32(PvpInfo.Size());
            data.WriteUInt32(MaxLevel);
            data.WriteInt32(ScalingPlayerLevelDelta);
            data.WriteInt32(MaxCreatureScalingLevel);
            data.WriteUInt8(TransmogCostMinScalingLevel);
            for (int i = 0; i < 4; ++i)
            {
                data.WriteUInt32(NoReagentCostMask[i]);
            }
            data.WriteUInt32(PetSpellPower);
            for (int i = 0; i < 2; ++i)
            {
                data.WriteUInt32(ProfessionSkillLine[i]);
            }
            data.WriteFloat(UiHitModifier);
            data.WriteFloat(UiSpellHitModifier);
            data.WriteInt32(HomeRealmTimeOffset);
            data.WriteFloat(ModPetHaste);
            data.WriteInt8(JailersTowerLevelMax);
            data.WriteInt8(JailersTowerLevel);
            data.WriteUInt8(LocalRegenFlags);
            data.WriteUInt8(AuraVision);
            data.WriteUInt8(NumBackpackSlots);
            data.WriteUInt32(OverrideSpellsID);
            data.WriteUInt16(LootSpecID);
            data.WriteUInt32(OverrideZonePVPType);
            for (int i = 0; i < 5; ++i)
            {
                data.WriteUInt32(BagSlotFlags[i]);
            }
            data.WriteUInt32(Honor);
            data.WriteUInt32(HonorNextLevel);
            data.WriteInt32(PerksProgramCurrency);
            data.WriteUInt8(NumBankSlots);
            data.WriteUInt8(NumCharacterBankTabs);
            data.WriteUInt8(NumAccountBankTabs);
            for (int i = 0; i < 1; ++i)
            {
                data.WriteInt32(ResearchSites[i].Size());
                data.WriteInt32(ResearchSiteProgress[i].Size());
                data.WriteInt32(Research[i].Size());
                for (int j = 0; j < ResearchSites[i].Size(); ++j)
                {
                    data.WriteUInt16(ResearchSites[i][j]);
                }
                for (int j = 0; j < ResearchSiteProgress[i].Size(); ++j)
                {
                    data.WriteUInt32(ResearchSiteProgress[i][j]);
                }
                for (int j = 0; j < Research[i].Size(); ++j)
                {
                    Research[i][j].WriteCreate(data, receiver, owner);
                }
            }
            data.WriteInt32(DailyQuestsCompleted.Size());
            data.WriteInt32(Field_1328.Size());
            data.WriteInt32(AvailableQuestLineXQuestIDs.Size());
            data.WriteInt32(Heirlooms.Size());
            data.WriteInt32(HeirloomFlags.Size());
            data.WriteInt32(Toys.Size());
            data.WriteInt32(ToyFlags.Size());
            data.WriteInt32(Transmog.Size());
            data.WriteInt32(ConditionalTransmog.Size());
            data.WriteInt32(SelfResSpells.Size());
            data.WriteInt32(RuneforgePowers.Size());
            data.WriteInt32(TransmogIllusions.Size());
            data.WriteInt32(WarbandScenes.Size());
            data.WriteInt32(HouseRooms.Size());
            data.WriteInt32(HouseExteriorComponents.Size());
            data.WriteInt32(HouseThemes.Size());
            data.WriteInt32(HouseRoomComponentTextures.Size());
            data.WriteInt32(HouseTypes.Size());
            data.WriteInt32(UnlockedTransmogOutfits.Size());
            data.WriteInt32(CharacterRestrictions.Size());
            data.WriteInt32(SpellPctModByLabel.Size());
            data.WriteInt32(SpellFlatModByLabel.Size());
            data.WriteInt32(SpellPctModPVPByLabel.Size());
            data.WriteInt32(SpellFlatModPVPByLabel.Size());
            ResearchHistory.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(MawPowers.Size());
            data.WriteInt32(MultiFloorExploration.Size());
            data.WriteInt32(RecipeProgression.Size());
            FrozenPerksVendorItem.GetValue().Write(data);
            DiscordInfo.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(ReplayedQuests.Size());
            data.WriteInt32(TaskQuests.Size());
            LevelLinkInfo.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(DisabledSpells.Size());
            data.WriteInt32(UiChromieTimeExpansionID);
            data.WriteInt32(TimerunningSeasonID);
            data.WriteInt32(TransportServerTime);
            data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
            data.WriteInt16(DEBUGSoulbindConduitRank);
            DungeonScore.GetValue().Write(data);
            WriteMapFieldCreate(TraitConfigs, data, receiver, owner);
            data.WriteUInt32(ActiveCombatTraitConfigID);
            data.WriteInt32(CraftingOrders.Size());
            data.WriteInt32(PersonalCraftingOrderCounts.Size());
            data.WriteInt32(NpcCraftingOrders.Size());
            data.WriteInt32(CategoryCooldownMods.Size());
            data.WriteInt32(WeeklySpellUses.Size());
            for (int i = 0; i < 17; ++i)
            {
                data.WriteFloat(ItemUpgradeHighWatermark[i]);
            }
            data.WriteInt32(ItemUpgradeHighOnehandWeaponItemID);
            data.WriteInt32(ItemUpgradeHighFingerItemID);
            data.WriteFloat(ItemUpgradeHighFingerWatermark);
            data.WriteInt32(ItemUpgradeHighTrinketItemID);
            data.WriteFloat(ItemUpgradeHighTrinketWatermark);
            data.WriteUInt64(LootHistoryInstanceID);
            data.WriteInt32(TrackedCollectableSources.Size());
            data.WriteUInt8(RequiredMountCapabilityFlags);
            WriteMapFieldCreate(DelveData, data, receiver, owner);
            WriteMapFieldCreate(TransmogOutfits, data, receiver, owner);
            ViewedOutfit.GetValue().WriteCreate(data, receiver, owner);
            TransmogMetadata.GetValue().WriteCreate(data, receiver, owner);
            data.FlushBits();
            for (int i = 0; i < KnownTitles.Size(); ++i)
            {
                data.WriteUInt64(KnownTitles[i]);
            }
            for (int i = 0; i < CharacterDataElements.Size(); ++i)
            {
                CharacterDataElements[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < AccountDataElements.Size(); ++i)
            {
                AccountDataElements[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < PvpInfo.Size(); ++i)
            {
                PvpInfo[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
            {
                data.WriteUInt32(DailyQuestsCompleted[i]);
            }
            for (int i = 0; i < Field_1328.Size(); ++i)
            {
                data.WriteInt32(Field_1328[i]);
            }
            for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
            {
                data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
            }
            for (int i = 0; i < Heirlooms.Size(); ++i)
            {
                data.WriteUInt32(Heirlooms[i]);
            }
            for (int i = 0; i < HeirloomFlags.Size(); ++i)
            {
                data.WriteUInt32(HeirloomFlags[i]);
            }
            for (int i = 0; i < Toys.Size(); ++i)
            {
                data.WriteUInt32(Toys[i]);
            }
            for (int i = 0; i < ToyFlags.Size(); ++i)
            {
                data.WriteUInt32(ToyFlags[i]);
            }
            for (int i = 0; i < Transmog.Size(); ++i)
            {
                data.WriteUInt32(Transmog[i]);
            }
            for (int i = 0; i < ConditionalTransmog.Size(); ++i)
            {
                data.WriteUInt32(ConditionalTransmog[i]);
            }
            for (int i = 0; i < SelfResSpells.Size(); ++i)
            {
                data.WriteUInt32(SelfResSpells[i]);
            }
            for (int i = 0; i < RuneforgePowers.Size(); ++i)
            {
                data.WriteUInt32(RuneforgePowers[i]);
            }
            for (int i = 0; i < TransmogIllusions.Size(); ++i)
            {
                data.WriteUInt32(TransmogIllusions[i]);
            }
            for (int i = 0; i < WarbandScenes.Size(); ++i)
            {
                data.WriteUInt32(WarbandScenes[i]);
            }
            for (int i = 0; i < HouseRooms.Size(); ++i)
            {
                data.WriteUInt32(HouseRooms[i]);
            }
            for (int i = 0; i < HouseExteriorComponents.Size(); ++i)
            {
                data.WriteUInt32(HouseExteriorComponents[i]);
            }
            for (int i = 0; i < HouseThemes.Size(); ++i)
            {
                data.WriteUInt32(HouseThemes[i]);
            }
            for (int i = 0; i < HouseRoomComponentTextures.Size(); ++i)
            {
                data.WriteUInt32(HouseRoomComponentTextures[i]);
            }
            for (int i = 0; i < HouseTypes.Size(); ++i)
            {
                data.WriteUInt32(HouseTypes[i]);
            }
            for (int i = 0; i < UnlockedTransmogOutfits.Size(); ++i)
            {
                data.WriteInt32(UnlockedTransmogOutfits[i]);
            }
            for (int i = 0; i < CharacterRestrictions.Size(); ++i)
            {
                CharacterRestrictions[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
            {
                SpellPctModByLabel[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
            {
                SpellFlatModByLabel[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < SpellPctModPVPByLabel.Size(); ++i)
            {
                SpellPctModPVPByLabel[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < SpellFlatModPVPByLabel.Size(); ++i)
            {
                SpellFlatModPVPByLabel[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < MawPowers.Size(); ++i)
            {
                MawPowers[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < MultiFloorExploration.Size(); ++i)
            {
                MultiFloorExploration[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < RecipeProgression.Size(); ++i)
            {
                RecipeProgression[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < ReplayedQuests.Size(); ++i)
            {
                ReplayedQuests[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < TaskQuests.Size(); ++i)
            {
                TaskQuests[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < DisabledSpells.Size(); ++i)
            {
                data.WriteInt32(DisabledSpells[i]);
            }
            for (int i = 0; i < CraftingOrders.Size(); ++i)
            {
                CraftingOrders[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
            {
                PersonalCraftingOrderCounts[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < NpcCraftingOrders.Size(); ++i)
            {
                NpcCraftingOrders[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < CategoryCooldownMods.Size(); ++i)
            {
                CategoryCooldownMods[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < WeeklySpellUses.Size(); ++i)
            {
                WeeklySpellUses[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < TrackedCollectableSources.Size(); ++i)
            {
                TrackedCollectableSources[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBit(BackpackAutoSortDisabled);
            data.WriteBit(BackpackSellJunkDisabled);
            data.WriteBit(BankAutoSortDisabled);
            data.WriteBit(SortBagsRightToLeft);
            data.WriteBit(InsertItemsLeftToRight);
            data.WriteBit(HasPerksProgramPendingReward);
            data.WriteBit(QuestSession.HasValue());
            data.WriteBit(PetStable.HasValue());
            data.WriteBits(CharacterBankTabSettings.Size(), 3);
            data.WriteBits(AccountBankTabSettings.Size(), 3);
            data.WriteBit(WalkInData.HasValue());
            data.WriteBit(ChallengeModeData.HasValue());
            data.FlushBits();
            if (QuestSession.HasValue())
            {
                QuestSession.GetValue().WriteCreate(data, receiver, owner);
            }
            if (PetStable.HasValue())
            {
                PetStable.GetValue().WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < CharacterBankTabSettings.Size(); ++i)
            {
                CharacterBankTabSettings[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < AccountBankTabSettings.Size(); ++i)
            {
                AccountBankTabSettings[i].WriteCreate(data, receiver, owner);
            }
            if (WalkInData.HasValue())
            {
                WalkInData.GetValue().WriteCreate(data, receiver, owner);
            }
            if (ChallengeModeData.HasValue())
            {
                ChallengeModeData.GetValue().WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Player owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 13);
            for (uint i = 0; i < 13; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(BackpackAutoSortDisabled);
                }
                if (changesMask[2])
                {
                    data.WriteBit(BackpackSellJunkDisabled);
                }
                if (changesMask[3])
                {
                    data.WriteBit(BankAutoSortDisabled);
                }
                if (changesMask[4])
                {
                    data.WriteBit(SortBagsRightToLeft);
                }
                if (changesMask[5])
                {
                    data.WriteBit(InsertItemsLeftToRight);
                }
                if (changesMask[6])
                {
                    data.WriteBit(HasPerksProgramPendingReward);
                }
                if (changesMask[7])
                {
                    if (!ignoreNestedChangesMask)
                        KnownTitles.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(KnownTitles.Size(), data);
                }
                if (changesMask[8])
                {
                    if (!ignoreNestedChangesMask)
                        CharacterDataElements.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterDataElements.Size(), data);
                }
                if (changesMask[9])
                {
                    if (!ignoreNestedChangesMask)
                        AccountDataElements.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(AccountDataElements.Size(), data);
                }
                if (changesMask[10])
                {
                    if (!ignoreNestedChangesMask)
                        PvpInfo.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(PvpInfo.Size(), data);
                }
            }
            if (changesMask[50])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[51])
                    {
                        if (!ignoreNestedChangesMask)
                            ResearchSites[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(ResearchSites[i].Size(), data);
                    }
                }
            }
            if (changesMask[52])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[53])
                    {
                        if (!ignoreNestedChangesMask)
                            ResearchSiteProgress[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(ResearchSiteProgress[i].Size(), data);
                    }
                }
            }
            if (changesMask[54])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[55])
                    {
                        if (!ignoreNestedChangesMask)
                            Research[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(Research[i].Size(), data);
                    }
                }
            }
            if (changesMask[50])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[51])
                    {
                        for (int j = 0; j < ResearchSites[i].Size(); ++j)
                        {
                            if (ResearchSites[i].HasChanged(j) || ignoreNestedChangesMask)
                            {
                                data.WriteUInt16(ResearchSites[i][j]);
                            }
                        }
                    }
                }
            }
            if (changesMask[52])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[53])
                    {
                        for (int j = 0; j < ResearchSiteProgress[i].Size(); ++j)
                        {
                            if (ResearchSiteProgress[i].HasChanged(j) || ignoreNestedChangesMask)
                            {
                                data.WriteUInt32(ResearchSiteProgress[i][j]);
                            }
                        }
                    }
                }
            }
            if (changesMask[54])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[55])
                    {
                        for (int j = 0; j < Research[i].Size(); ++j)
                        {
                            if (Research[i].HasChanged(j) || ignoreNestedChangesMask)
                            {
                                Research[i][j].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                            }
                        }
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[11])
                {
                    if (!ignoreNestedChangesMask)
                        DailyQuestsCompleted.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(DailyQuestsCompleted.Size(), data);
                }
                if (changesMask[12])
                {
                    if (!ignoreNestedChangesMask)
                        Field_1328.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Field_1328.Size(), data);
                }
                if (changesMask[13])
                {
                    if (!ignoreNestedChangesMask)
                        AvailableQuestLineXQuestIDs.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(AvailableQuestLineXQuestIDs.Size(), data);
                }
                if (changesMask[14])
                {
                    if (!ignoreNestedChangesMask)
                        Heirlooms.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Heirlooms.Size(), data);
                }
                if (changesMask[15])
                {
                    if (!ignoreNestedChangesMask)
                        HeirloomFlags.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HeirloomFlags.Size(), data);
                }
                if (changesMask[16])
                {
                    if (!ignoreNestedChangesMask)
                        Toys.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Toys.Size(), data);
                }
                if (changesMask[17])
                {
                    if (!ignoreNestedChangesMask)
                        ToyFlags.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ToyFlags.Size(), data);
                }
                if (changesMask[18])
                {
                    if (!ignoreNestedChangesMask)
                        Transmog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Transmog.Size(), data);
                }
                if (changesMask[19])
                {
                    if (!ignoreNestedChangesMask)
                        ConditionalTransmog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ConditionalTransmog.Size(), data);
                }
                if (changesMask[20])
                {
                    if (!ignoreNestedChangesMask)
                        SelfResSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SelfResSpells.Size(), data);
                }
                if (changesMask[21])
                {
                    if (!ignoreNestedChangesMask)
                        RuneforgePowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(RuneforgePowers.Size(), data);
                }
                if (changesMask[22])
                {
                    if (!ignoreNestedChangesMask)
                        TransmogIllusions.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(TransmogIllusions.Size(), data);
                }
                if (changesMask[23])
                {
                    if (!ignoreNestedChangesMask)
                        WarbandScenes.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(WarbandScenes.Size(), data);
                }
                if (changesMask[24])
                {
                    if (!ignoreNestedChangesMask)
                        HouseRooms.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HouseRooms.Size(), data);
                }
                if (changesMask[25])
                {
                    if (!ignoreNestedChangesMask)
                        HouseExteriorComponents.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HouseExteriorComponents.Size(), data);
                }
                if (changesMask[26])
                {
                    if (!ignoreNestedChangesMask)
                        HouseThemes.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HouseThemes.Size(), data);
                }
                if (changesMask[27])
                {
                    if (!ignoreNestedChangesMask)
                        HouseRoomComponentTextures.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HouseRoomComponentTextures.Size(), data);
                }
                if (changesMask[28])
                {
                    if (!ignoreNestedChangesMask)
                        HouseTypes.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HouseTypes.Size(), data);
                }
                if (changesMask[29])
                {
                    if (!ignoreNestedChangesMask)
                        UnlockedTransmogOutfits.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(UnlockedTransmogOutfits.Size(), data);
                }
                if (changesMask[30])
                {
                    if (!ignoreNestedChangesMask)
                        CharacterRestrictions.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterRestrictions.Size(), data);
                }
                if (changesMask[31])
                {
                    if (!ignoreNestedChangesMask)
                        SpellPctModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellPctModByLabel.Size(), data);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    if (!ignoreNestedChangesMask)
                        SpellFlatModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellFlatModByLabel.Size(), data);
                }
                if (changesMask[34])
                {
                    if (!ignoreNestedChangesMask)
                        SpellPctModPVPByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellPctModPVPByLabel.Size(), data);
                }
                if (changesMask[35])
                {
                    if (!ignoreNestedChangesMask)
                        SpellFlatModPVPByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellFlatModPVPByLabel.Size(), data);
                }
                if (changesMask[36])
                {
                    if (!ignoreNestedChangesMask)
                        MawPowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MawPowers.Size(), data);
                }
                if (changesMask[37])
                {
                    if (!ignoreNestedChangesMask)
                        MultiFloorExploration.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MultiFloorExploration.Size(), data);
                }
                if (changesMask[38])
                {
                    if (!ignoreNestedChangesMask)
                        RecipeProgression.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(RecipeProgression.Size(), data);
                }
                if (changesMask[39])
                {
                    if (!ignoreNestedChangesMask)
                        ReplayedQuests.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ReplayedQuests.Size(), data);
                }
                if (changesMask[40])
                {
                    if (!ignoreNestedChangesMask)
                        TaskQuests.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(TaskQuests.Size(), data);
                }
                if (changesMask[41])
                {
                    if (!ignoreNestedChangesMask)
                        DisabledSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(DisabledSpells.Size(), data);
                }
                if (changesMask[42])
                {
                    if (!ignoreNestedChangesMask)
                        CraftingOrders.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CraftingOrders.Size(), data);
                }
                if (changesMask[43])
                {
                    if (!ignoreNestedChangesMask)
                        PersonalCraftingOrderCounts.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(PersonalCraftingOrderCounts.Size(), data);
                }
                if (changesMask[44])
                {
                    if (!ignoreNestedChangesMask)
                        NpcCraftingOrders.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(NpcCraftingOrders.Size(), data);
                }
                if (changesMask[45])
                {
                    if (!ignoreNestedChangesMask)
                        CategoryCooldownMods.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CategoryCooldownMods.Size(), data);
                }
                if (changesMask[46])
                {
                    if (!ignoreNestedChangesMask)
                        WeeklySpellUses.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(WeeklySpellUses.Size(), data);
                }
                if (changesMask[47])
                {
                    if (!ignoreNestedChangesMask)
                        TrackedCollectableSources.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(TrackedCollectableSources.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[7])
                {
                    for (int i = 0; i < KnownTitles.Size(); ++i)
                    {
                        if (KnownTitles.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt64(KnownTitles[i]);
                        }
                    }
                }
                if (changesMask[8])
                {
                    for (int i = 0; i < CharacterDataElements.Size(); ++i)
                    {
                        if (CharacterDataElements.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterDataElements[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[9])
                {
                    for (int i = 0; i < AccountDataElements.Size(); ++i)
                    {
                        if (AccountDataElements.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            AccountDataElements[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[10])
                {
                    for (int i = 0; i < PvpInfo.Size(); ++i)
                    {
                        if (PvpInfo.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PvpInfo[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[11])
                {
                    for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
                    {
                        if (DailyQuestsCompleted.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(DailyQuestsCompleted[i]);
                        }
                    }
                }
                if (changesMask[12])
                {
                    for (int i = 0; i < Field_1328.Size(); ++i)
                    {
                        if (Field_1328.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(Field_1328[i]);
                        }
                    }
                }
                if (changesMask[13])
                {
                    for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
                    {
                        if (AvailableQuestLineXQuestIDs.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
                        }
                    }
                }
                if (changesMask[14])
                {
                    for (int i = 0; i < Heirlooms.Size(); ++i)
                    {
                        if (Heirlooms.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Heirlooms[i]);
                        }
                    }
                }
                if (changesMask[15])
                {
                    for (int i = 0; i < HeirloomFlags.Size(); ++i)
                    {
                        if (HeirloomFlags.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HeirloomFlags[i]);
                        }
                    }
                }
                if (changesMask[16])
                {
                    for (int i = 0; i < Toys.Size(); ++i)
                    {
                        if (Toys.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Toys[i]);
                        }
                    }
                }
                if (changesMask[17])
                {
                    for (int i = 0; i < ToyFlags.Size(); ++i)
                    {
                        if (ToyFlags.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ToyFlags[i]);
                        }
                    }
                }
                if (changesMask[18])
                {
                    for (int i = 0; i < Transmog.Size(); ++i)
                    {
                        if (Transmog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Transmog[i]);
                        }
                    }
                }
                if (changesMask[19])
                {
                    for (int i = 0; i < ConditionalTransmog.Size(); ++i)
                    {
                        if (ConditionalTransmog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ConditionalTransmog[i]);
                        }
                    }
                }
                if (changesMask[20])
                {
                    for (int i = 0; i < SelfResSpells.Size(); ++i)
                    {
                        if (SelfResSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(SelfResSpells[i]);
                        }
                    }
                }
                if (changesMask[21])
                {
                    for (int i = 0; i < RuneforgePowers.Size(); ++i)
                    {
                        if (RuneforgePowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(RuneforgePowers[i]);
                        }
                    }
                }
                if (changesMask[22])
                {
                    for (int i = 0; i < TransmogIllusions.Size(); ++i)
                    {
                        if (TransmogIllusions.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(TransmogIllusions[i]);
                        }
                    }
                }
                if (changesMask[23])
                {
                    for (int i = 0; i < WarbandScenes.Size(); ++i)
                    {
                        if (WarbandScenes.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(WarbandScenes[i]);
                        }
                    }
                }
                if (changesMask[24])
                {
                    for (int i = 0; i < HouseRooms.Size(); ++i)
                    {
                        if (HouseRooms.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HouseRooms[i]);
                        }
                    }
                }
                if (changesMask[25])
                {
                    for (int i = 0; i < HouseExteriorComponents.Size(); ++i)
                    {
                        if (HouseExteriorComponents.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HouseExteriorComponents[i]);
                        }
                    }
                }
                if (changesMask[26])
                {
                    for (int i = 0; i < HouseThemes.Size(); ++i)
                    {
                        if (HouseThemes.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HouseThemes[i]);
                        }
                    }
                }
                if (changesMask[27])
                {
                    for (int i = 0; i < HouseRoomComponentTextures.Size(); ++i)
                    {
                        if (HouseRoomComponentTextures.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HouseRoomComponentTextures[i]);
                        }
                    }
                }
                if (changesMask[28])
                {
                    for (int i = 0; i < HouseTypes.Size(); ++i)
                    {
                        if (HouseTypes.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HouseTypes[i]);
                        }
                    }
                }
                if (changesMask[29])
                {
                    for (int i = 0; i < UnlockedTransmogOutfits.Size(); ++i)
                    {
                        if (UnlockedTransmogOutfits.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(UnlockedTransmogOutfits[i]);
                        }
                    }
                }
                if (changesMask[30])
                {
                    for (int i = 0; i < CharacterRestrictions.Size(); ++i)
                    {
                        if (CharacterRestrictions.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterRestrictions[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[31])
                {
                    for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
                    {
                        if (SpellPctModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellPctModByLabel[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
                    {
                        if (SpellFlatModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellFlatModByLabel[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[34])
                {
                    for (int i = 0; i < SpellPctModPVPByLabel.Size(); ++i)
                    {
                        if (SpellPctModPVPByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellPctModPVPByLabel[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[35])
                {
                    for (int i = 0; i < SpellFlatModPVPByLabel.Size(); ++i)
                    {
                        if (SpellFlatModPVPByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellFlatModPVPByLabel[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[36])
                {
                    for (int i = 0; i < MawPowers.Size(); ++i)
                    {
                        if (MawPowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            MawPowers[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[37])
                {
                    for (int i = 0; i < MultiFloorExploration.Size(); ++i)
                    {
                        if (MultiFloorExploration.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            MultiFloorExploration[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[38])
                {
                    for (int i = 0; i < RecipeProgression.Size(); ++i)
                    {
                        if (RecipeProgression.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            RecipeProgression[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[39])
                {
                    for (int i = 0; i < ReplayedQuests.Size(); ++i)
                    {
                        if (ReplayedQuests.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ReplayedQuests[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[40])
                {
                    for (int i = 0; i < TaskQuests.Size(); ++i)
                    {
                        if (TaskQuests.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            TaskQuests[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[41])
                {
                    for (int i = 0; i < DisabledSpells.Size(); ++i)
                    {
                        if (DisabledSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(DisabledSpells[i]);
                        }
                    }
                }
                if (changesMask[42])
                {
                    for (int i = 0; i < CraftingOrders.Size(); ++i)
                    {
                        if (CraftingOrders.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CraftingOrders[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[43])
                {
                    for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
                    {
                        if (PersonalCraftingOrderCounts.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PersonalCraftingOrderCounts[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[44])
                {
                    for (int i = 0; i < NpcCraftingOrders.Size(); ++i)
                    {
                        if (NpcCraftingOrders.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            NpcCraftingOrders[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[45])
                {
                    for (int i = 0; i < CategoryCooldownMods.Size(); ++i)
                    {
                        if (CategoryCooldownMods.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CategoryCooldownMods[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[46])
                {
                    for (int i = 0; i < WeeklySpellUses.Size(); ++i)
                    {
                        if (WeeklySpellUses.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            WeeklySpellUses[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[47])
                {
                    for (int i = 0; i < TrackedCollectableSources.Size(); ++i)
                    {
                        if (TrackedCollectableSources.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            TrackedCollectableSources[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
            }
            data.FlushBits();
            if (changesMask[32])
            {
                if (changesMask[48])
                {
                    if (!ignoreNestedChangesMask)
                        CharacterBankTabSettings.WriteUpdateMask(data, 3);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterBankTabSettings.Size(), data, 3);
                }
                if (changesMask[49])
                {
                    if (!ignoreNestedChangesMask)
                        AccountBankTabSettings.WriteUpdateMask(data, 3);
                    else
                        WriteCompleteDynamicFieldUpdateMask(AccountBankTabSettings.Size(), data, 3);
                }
                if (changesMask[48])
                {
                    for (int i = 0; i < CharacterBankTabSettings.Size(); ++i)
                    {
                        if (CharacterBankTabSettings.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterBankTabSettings[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[49])
                {
                    for (int i = 0; i < AccountBankTabSettings.Size(); ++i)
                    {
                        if (AccountBankTabSettings.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            AccountBankTabSettings[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[56])
                {
                    data.WritePackedGuid(FarsightObject);
                }
                if (changesMask[57])
                {
                    data.WritePackedGuid(SummonedBattlePetGUID);
                }
                if (changesMask[58])
                {
                    data.WriteUInt64(Coinage);
                }
                if (changesMask[59])
                {
                    data.WriteUInt64(AccountBankCoinage);
                }
                if (changesMask[60])
                {
                    data.WriteUInt32(XP);
                }
                if (changesMask[61])
                {
                    data.WriteUInt32(NextLevelXP);
                }
                if (changesMask[62])
                {
                    data.WriteInt32(TrialXP);
                }
                if (changesMask[63])
                {
                    Skill.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[64])
                {
                    data.WriteUInt32(CharacterPoints);
                }
                if (changesMask[65])
                {
                    data.WriteUInt32(MaxTalentTiers);
                }
                if (changesMask[66])
                {
                    data.WriteUInt32(TrackCreatureMask);
                }
                if (changesMask[67])
                {
                    data.WriteFloat(MainhandExpertise);
                }
                if (changesMask[68])
                {
                    data.WriteFloat(OffhandExpertise);
                }
                if (changesMask[69])
                {
                    data.WriteFloat(RangedExpertise);
                }
            }
            if (changesMask[70])
            {
                if (changesMask[71])
                {
                    data.WriteFloat(CombatRatingExpertise);
                }
                if (changesMask[72])
                {
                    data.WriteFloat(BlockPercentage);
                }
                if (changesMask[73])
                {
                    data.WriteFloat(DodgePercentage);
                }
                if (changesMask[74])
                {
                    data.WriteFloat(DodgePercentageFromAttribute);
                }
                if (changesMask[75])
                {
                    data.WriteFloat(ParryPercentage);
                }
                if (changesMask[76])
                {
                    data.WriteFloat(ParryPercentageFromAttribute);
                }
                if (changesMask[77])
                {
                    data.WriteFloat(CritPercentage);
                }
                if (changesMask[78])
                {
                    data.WriteFloat(RangedCritPercentage);
                }
                if (changesMask[79])
                {
                    data.WriteFloat(OffhandCritPercentage);
                }
                if (changesMask[80])
                {
                    data.WriteFloat(SpellCritPercentage);
                }
                if (changesMask[81])
                {
                    data.WriteUInt32(ShieldBlock);
                }
                if (changesMask[82])
                {
                    data.WriteFloat(ShieldBlockCritPercentage);
                }
                if (changesMask[83])
                {
                    data.WriteFloat(Mastery);
                }
                if (changesMask[84])
                {
                    data.WriteFloat(Speed);
                }
                if (changesMask[85])
                {
                    data.WriteFloat(Avoidance);
                }
                if (changesMask[86])
                {
                    data.WriteFloat(Sturdiness);
                }
                if (changesMask[87])
                {
                    data.WriteInt32(Versatility);
                }
                if (changesMask[88])
                {
                    data.WriteFloat(VersatilityBonus);
                }
                if (changesMask[89])
                {
                    data.WriteFloat(PvpPowerDamage);
                }
                if (changesMask[90])
                {
                    data.WriteFloat(PvpPowerHealing);
                }
                if (changesMask[91])
                {
                    BitVectors.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[92])
                {
                    data.WriteInt32(ModHealingDonePos);
                }
                if (changesMask[93])
                {
                    data.WriteFloat(ModHealingPercent);
                }
                if (changesMask[94])
                {
                    data.WriteFloat(ModPeriodicHealingDonePercent);
                }
                if (changesMask[95])
                {
                    data.WriteFloat(ModSpellPowerPercent);
                }
                if (changesMask[96])
                {
                    data.WriteFloat(ModResiliencePercent);
                }
                if (changesMask[97])
                {
                    data.WriteFloat(OverrideSpellPowerByAPPercent);
                }
                if (changesMask[98])
                {
                    data.WriteFloat(OverrideAPBySpellPowerPercent);
                }
                if (changesMask[99])
                {
                    data.WriteInt32(ModTargetResistance);
                }
                if (changesMask[100])
                {
                    data.WriteInt32(ModTargetPhysicalResistance);
                }
                if (changesMask[101])
                {
                    data.WriteUInt32(LocalFlags);
                }
            }
            if (changesMask[102])
            {
                if (changesMask[103])
                {
                    data.WriteUInt8(GrantableLevels);
                }
                if (changesMask[104])
                {
                    data.WriteUInt8(MultiActionBars);
                }
                if (changesMask[105])
                {
                    data.WriteUInt8(LifetimeMaxRank);
                }
                if (changesMask[106])
                {
                    data.WriteUInt8(NumRespecs);
                }
                if (changesMask[107])
                {
                    data.WriteUInt32(PvpMedals);
                }
                if (changesMask[108])
                {
                    data.WriteUInt16(TodayHonorableKills);
                }
                if (changesMask[109])
                {
                    data.WriteUInt16(YesterdayHonorableKills);
                }
                if (changesMask[110])
                {
                    data.WriteUInt32(LifetimeHonorableKills);
                }
                if (changesMask[111])
                {
                    data.WriteUInt32(WatchedFactionIndex);
                }
                if (changesMask[112])
                {
                    data.WriteUInt32(MaxLevel);
                }
                if (changesMask[113])
                {
                    data.WriteInt32(ScalingPlayerLevelDelta);
                }
                if (changesMask[114])
                {
                    data.WriteInt32(MaxCreatureScalingLevel);
                }
                if (changesMask[115])
                {
                    data.WriteUInt8(TransmogCostMinScalingLevel);
                }
                if (changesMask[116])
                {
                    data.WriteUInt32(PetSpellPower);
                }
                if (changesMask[117])
                {
                    data.WriteFloat(UiHitModifier);
                }
                if (changesMask[118])
                {
                    data.WriteFloat(UiSpellHitModifier);
                }
                if (changesMask[119])
                {
                    data.WriteInt32(HomeRealmTimeOffset);
                }
                if (changesMask[120])
                {
                    data.WriteFloat(ModPetHaste);
                }
                if (changesMask[121])
                {
                    data.WriteInt8(JailersTowerLevelMax);
                }
                if (changesMask[122])
                {
                    data.WriteInt8(JailersTowerLevel);
                }
                if (changesMask[123])
                {
                    data.WriteUInt8(LocalRegenFlags);
                }
                if (changesMask[124])
                {
                    data.WriteUInt8(AuraVision);
                }
                if (changesMask[125])
                {
                    data.WriteUInt8(NumBackpackSlots);
                }
                if (changesMask[126])
                {
                    data.WriteUInt32(OverrideSpellsID);
                }
                if (changesMask[127])
                {
                    data.WriteUInt16(LootSpecID);
                }
                if (changesMask[128])
                {
                    data.WriteUInt32(OverrideZonePVPType);
                }
                if (changesMask[129])
                {
                    data.WriteUInt32(Honor);
                }
                if (changesMask[130])
                {
                    data.WriteUInt32(HonorNextLevel);
                }
                if (changesMask[131])
                {
                    data.WriteInt32(PerksProgramCurrency);
                }
                if (changesMask[132])
                {
                    data.WriteUInt8(NumBankSlots);
                }
                if (changesMask[133])
                {
                    data.WriteUInt8(NumCharacterBankTabs);
                }
            }
            if (changesMask[134])
            {
                if (changesMask[135])
                {
                    data.WriteUInt8(NumAccountBankTabs);
                }
                if (changesMask[136])
                {
                    ResearchHistory.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[137])
                {
                    FrozenPerksVendorItem.GetValue().Write(data);
                }
                if (changesMask[138])
                {
                    DiscordInfo.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[140])
                {
                    LevelLinkInfo.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[141])
                {
                    data.WriteInt32(UiChromieTimeExpansionID);
                }
                if (changesMask[142])
                {
                    data.WriteInt32(TimerunningSeasonID);
                }
                if (changesMask[143])
                {
                    data.WriteInt32(TransportServerTime);
                }
                if (changesMask[144])
                {
                    data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
                }
                if (changesMask[145])
                {
                    data.WriteInt16(DEBUGSoulbindConduitRank);
                }
                if (changesMask[146])
                {
                    DungeonScore.GetValue().Write(data);
                }
                if (changesMask[147])
                {
                    WriteMapFieldUpdate(TraitConfigs, ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[148])
                {
                    data.WriteUInt32(ActiveCombatTraitConfigID);
                }
                if (changesMask[149])
                {
                    data.WriteInt32(ItemUpgradeHighOnehandWeaponItemID);
                }
                if (changesMask[150])
                {
                    data.WriteInt32(ItemUpgradeHighFingerItemID);
                }
                if (changesMask[151])
                {
                    data.WriteFloat(ItemUpgradeHighFingerWatermark);
                }
                if (changesMask[152])
                {
                    data.WriteInt32(ItemUpgradeHighTrinketItemID);
                }
                if (changesMask[153])
                {
                    data.WriteFloat(ItemUpgradeHighTrinketWatermark);
                }
                if (changesMask[154])
                {
                    data.WriteUInt64(LootHistoryInstanceID);
                }
                if (changesMask[156])
                {
                    data.WriteUInt8(RequiredMountCapabilityFlags);
                }
                if (changesMask[158])
                {
                    WriteMapFieldUpdate(DelveData, ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[159])
                {
                    WriteMapFieldUpdate(TransmogOutfits, ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[161])
                {
                    ViewedOutfit.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[162])
                {
                    TransmogMetadata.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
            }
            if (changesMask[134])
            {
                data.WriteBit(QuestSession.HasValue());
                data.WriteBit(PetStable.HasValue());
                data.WriteBit(WalkInData.HasValue());
                data.WriteBit(ChallengeModeData.HasValue());
                data.FlushBits();
                if (changesMask[139])
                {
                    if (QuestSession.HasValue())
                    {
                        QuestSession.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[155])
                {
                    if (PetStable.HasValue())
                    {
                        PetStable.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[157])
                {
                    if (WalkInData.HasValue())
                    {
                        WalkInData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[160])
                {
                    if (ChallengeModeData.HasValue())
                    {
                        ChallengeModeData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[163])
            {
                for (int i = 0; i < 105; ++i)
                {
                    if (changesMask[164 + i])
                    {
                        data.WritePackedGuid(InvSlots[i]);
                    }
                }
            }
            if (changesMask[269])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[270 + i])
                    {
                        RestInfo[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[272])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[273 + i])
                    {
                        data.WriteInt32(ModDamageDonePos[i]);
                    }
                    if (changesMask[280 + i])
                    {
                        data.WriteInt32(ModDamageDoneNeg[i]);
                    }
                    if (changesMask[287 + i])
                    {
                        data.WriteFloat(ModDamageDonePercent[i]);
                    }
                    if (changesMask[294 + i])
                    {
                        data.WriteFloat(ModHealingDonePercent[i]);
                    }
                }
            }
            if (changesMask[301])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[302 + i])
                    {
                        data.WriteFloat(WeaponDmgMultipliers[i]);
                    }
                    if (changesMask[305 + i])
                    {
                        data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
                    }
                }
            }
            if (changesMask[308])
            {
                for (int i = 0; i < 12; ++i)
                {
                    if (changesMask[309 + i])
                    {
                        data.WriteUInt32(BuybackPrice[i]);
                    }
                    if (changesMask[321 + i])
                    {
                        data.WriteInt64(BuybackTimestamp[i]);
                    }
                }
            }
            if (changesMask[333])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (changesMask[334 + i])
                    {
                        data.WriteUInt32(CombatRatings[i]);
                    }
                }
            }
            if (changesMask[366])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[367 + i])
                    {
                        data.WriteUInt32(NoReagentCostMask[i]);
                    }
                }
            }
            if (changesMask[371])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[372 + i])
                    {
                        data.WriteUInt32(ProfessionSkillLine[i]);
                    }
                }
            }
            if (changesMask[374])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[375 + i])
                    {
                        data.WriteUInt32(BagSlotFlags[i]);
                    }
                }
            }
            if (changesMask[380])
            {
                for (int i = 0; i < 17; ++i)
                {
                    if (changesMask[381 + i])
                    {
                        data.WriteFloat(ItemUpgradeHighWatermark[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BackpackAutoSortDisabled);
            ClearChangesMask(BackpackSellJunkDisabled);
            ClearChangesMask(BankAutoSortDisabled);
            ClearChangesMask(SortBagsRightToLeft);
            ClearChangesMask(InsertItemsLeftToRight);
            ClearChangesMask(HasPerksProgramPendingReward);
            ClearChangesMask(KnownTitles);
            ClearChangesMask(CharacterDataElements);
            ClearChangesMask(AccountDataElements);
            ClearChangesMask(PvpInfo);
            ClearChangesMask(DailyQuestsCompleted);
            ClearChangesMask(Field_1328);
            ClearChangesMask(AvailableQuestLineXQuestIDs);
            ClearChangesMask(Heirlooms);
            ClearChangesMask(HeirloomFlags);
            ClearChangesMask(Toys);
            ClearChangesMask(ToyFlags);
            ClearChangesMask(Transmog);
            ClearChangesMask(ConditionalTransmog);
            ClearChangesMask(SelfResSpells);
            ClearChangesMask(RuneforgePowers);
            ClearChangesMask(TransmogIllusions);
            ClearChangesMask(WarbandScenes);
            ClearChangesMask(HouseRooms);
            ClearChangesMask(HouseExteriorComponents);
            ClearChangesMask(HouseThemes);
            ClearChangesMask(HouseRoomComponentTextures);
            ClearChangesMask(HouseTypes);
            ClearChangesMask(UnlockedTransmogOutfits);
            ClearChangesMask(CharacterRestrictions);
            ClearChangesMask(SpellPctModByLabel);
            ClearChangesMask(SpellFlatModByLabel);
            ClearChangesMask(SpellPctModPVPByLabel);
            ClearChangesMask(SpellFlatModPVPByLabel);
            ClearChangesMask(MawPowers);
            ClearChangesMask(MultiFloorExploration);
            ClearChangesMask(RecipeProgression);
            ClearChangesMask(ReplayedQuests);
            ClearChangesMask(TaskQuests);
            ClearChangesMask(DisabledSpells);
            ClearChangesMask(CraftingOrders);
            ClearChangesMask(PersonalCraftingOrderCounts);
            ClearChangesMask(NpcCraftingOrders);
            ClearChangesMask(CategoryCooldownMods);
            ClearChangesMask(WeeklySpellUses);
            ClearChangesMask(TrackedCollectableSources);
            ClearChangesMask(CharacterBankTabSettings);
            ClearChangesMask(AccountBankTabSettings);
            ClearChangesMask(ResearchSites);
            ClearChangesMask(ResearchSiteProgress);
            ClearChangesMask(Research);
            ClearChangesMask(FarsightObject);
            ClearChangesMask(SummonedBattlePetGUID);
            ClearChangesMask(Coinage);
            ClearChangesMask(AccountBankCoinage);
            ClearChangesMask(XP);
            ClearChangesMask(NextLevelXP);
            ClearChangesMask(TrialXP);
            ClearChangesMask(Skill);
            ClearChangesMask(CharacterPoints);
            ClearChangesMask(MaxTalentTiers);
            ClearChangesMask(TrackCreatureMask);
            ClearChangesMask(MainhandExpertise);
            ClearChangesMask(OffhandExpertise);
            ClearChangesMask(RangedExpertise);
            ClearChangesMask(CombatRatingExpertise);
            ClearChangesMask(BlockPercentage);
            ClearChangesMask(DodgePercentage);
            ClearChangesMask(DodgePercentageFromAttribute);
            ClearChangesMask(ParryPercentage);
            ClearChangesMask(ParryPercentageFromAttribute);
            ClearChangesMask(CritPercentage);
            ClearChangesMask(RangedCritPercentage);
            ClearChangesMask(OffhandCritPercentage);
            ClearChangesMask(SpellCritPercentage);
            ClearChangesMask(ShieldBlock);
            ClearChangesMask(ShieldBlockCritPercentage);
            ClearChangesMask(Mastery);
            ClearChangesMask(Speed);
            ClearChangesMask(Avoidance);
            ClearChangesMask(Sturdiness);
            ClearChangesMask(Versatility);
            ClearChangesMask(VersatilityBonus);
            ClearChangesMask(PvpPowerDamage);
            ClearChangesMask(PvpPowerHealing);
            ClearChangesMask(BitVectors);
            ClearChangesMask(ModHealingDonePos);
            ClearChangesMask(ModHealingPercent);
            ClearChangesMask(ModPeriodicHealingDonePercent);
            ClearChangesMask(ModSpellPowerPercent);
            ClearChangesMask(ModResiliencePercent);
            ClearChangesMask(OverrideSpellPowerByAPPercent);
            ClearChangesMask(OverrideAPBySpellPowerPercent);
            ClearChangesMask(ModTargetResistance);
            ClearChangesMask(ModTargetPhysicalResistance);
            ClearChangesMask(LocalFlags);
            ClearChangesMask(GrantableLevels);
            ClearChangesMask(MultiActionBars);
            ClearChangesMask(LifetimeMaxRank);
            ClearChangesMask(NumRespecs);
            ClearChangesMask(PvpMedals);
            ClearChangesMask(TodayHonorableKills);
            ClearChangesMask(YesterdayHonorableKills);
            ClearChangesMask(LifetimeHonorableKills);
            ClearChangesMask(WatchedFactionIndex);
            ClearChangesMask(MaxLevel);
            ClearChangesMask(ScalingPlayerLevelDelta);
            ClearChangesMask(MaxCreatureScalingLevel);
            ClearChangesMask(TransmogCostMinScalingLevel);
            ClearChangesMask(PetSpellPower);
            ClearChangesMask(UiHitModifier);
            ClearChangesMask(UiSpellHitModifier);
            ClearChangesMask(HomeRealmTimeOffset);
            ClearChangesMask(ModPetHaste);
            ClearChangesMask(JailersTowerLevelMax);
            ClearChangesMask(JailersTowerLevel);
            ClearChangesMask(LocalRegenFlags);
            ClearChangesMask(AuraVision);
            ClearChangesMask(NumBackpackSlots);
            ClearChangesMask(OverrideSpellsID);
            ClearChangesMask(LootSpecID);
            ClearChangesMask(OverrideZonePVPType);
            ClearChangesMask(Honor);
            ClearChangesMask(HonorNextLevel);
            ClearChangesMask(PerksProgramCurrency);
            ClearChangesMask(NumBankSlots);
            ClearChangesMask(NumCharacterBankTabs);
            ClearChangesMask(NumAccountBankTabs);
            ClearChangesMask(ResearchHistory);
            ClearChangesMask(FrozenPerksVendorItem);
            ClearChangesMask(DiscordInfo);
            ClearChangesMask(QuestSession);
            ClearChangesMask(LevelLinkInfo);
            ClearChangesMask(UiChromieTimeExpansionID);
            ClearChangesMask(TimerunningSeasonID);
            ClearChangesMask(TransportServerTime);
            ClearChangesMask(WeeklyRewardsPeriodSinceOrigin);
            ClearChangesMask(DEBUGSoulbindConduitRank);
            ClearChangesMask(DungeonScore);
            ClearChangesMask(TraitConfigs);
            ClearChangesMask(ActiveCombatTraitConfigID);
            ClearChangesMask(ItemUpgradeHighOnehandWeaponItemID);
            ClearChangesMask(ItemUpgradeHighFingerItemID);
            ClearChangesMask(ItemUpgradeHighFingerWatermark);
            ClearChangesMask(ItemUpgradeHighTrinketItemID);
            ClearChangesMask(ItemUpgradeHighTrinketWatermark);
            ClearChangesMask(LootHistoryInstanceID);
            ClearChangesMask(PetStable);
            ClearChangesMask(RequiredMountCapabilityFlags);
            ClearChangesMask(WalkInData);
            ClearChangesMask(DelveData);
            ClearChangesMask(TransmogOutfits);
            ClearChangesMask(ChallengeModeData);
            ClearChangesMask(ViewedOutfit);
            ClearChangesMask(TransmogMetadata);
            ClearChangesMask(InvSlots);
            ClearChangesMask(RestInfo);
            ClearChangesMask(ModDamageDonePos);
            ClearChangesMask(ModDamageDoneNeg);
            ClearChangesMask(ModDamageDonePercent);
            ClearChangesMask(ModHealingDonePercent);
            ClearChangesMask(WeaponDmgMultipliers);
            ClearChangesMask(WeaponAtkSpeedMultipliers);
            ClearChangesMask(BuybackPrice);
            ClearChangesMask(BuybackTimestamp);
            ClearChangesMask(CombatRatings);
            ClearChangesMask(NoReagentCostMask);
            ClearChangesMask(ProfessionSkillLine);
            ClearChangesMask(BagSlotFlags);
            ClearChangesMask(ItemUpgradeHighWatermark);
            _changesMask.ResetAll();
        }
    }

    public struct GameObjectAssistActionData : IEquatable<GameObjectAssistActionData>, IsUpdateFieldStructure<GameObject>
    {
        public string PlayerName;
        public string MonsterName;
        public uint VirtualRealmAddress;
        public byte Sex;
        public long Time;
        public int DelveTier;

        public void WriteCreate(WorldPacket data, Player receiver, GameObject owner)
        {
            data.WriteBits(PlayerName.GetByteCount(), 6);
            data.WriteBits(MonsterName.GetByteCount() + 1, 11);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteUInt8(Sex);
            data.WriteInt64(Time);
            data.WriteInt32(DelveTier);
            data.WriteString(PlayerName);
            data.WriteCString(MonsterName);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, GameObject owner) => WriteCreate(data, receiver, owner);

        public bool Equals(GameObjectAssistActionData right)
        {
            return PlayerName == right.PlayerName
                && MonsterName == right.MonsterName
                && VirtualRealmAddress == right.VirtualRealmAddress
                && Sex == right.Sex
                && Time == right.Time
                && DelveTier == right.DelveTier;
        }
    }

    public class GameObjectFieldData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.GameObject, 27), IsUpdateFieldStructure<GameObject>
    {
        public UpdateField<List<uint>> StateWorldEffectIDs = new(0, 1);
        public DynamicUpdateField<int> EnableDoodadSets = new(0, 2);
        public DynamicUpdateField<int> WorldEffects = new(0, 3);
        public UpdateField<uint> DisplayID = new(0, 4);
        public UpdateField<uint> SpellVisualID = new(0, 5);
        public UpdateField<uint> StateSpellVisualID = new(0, 6);
        public UpdateField<uint> SpawnTrackingStateAnimID = new(0, 7);
        public UpdateField<uint> SpawnTrackingStateAnimKitID = new(0, 8);
        public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new(0, 9);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 10);
        public UpdateField<ObjectGuid> GuildGUID = new(0, 11);
        public UpdateField<uint> Flags = new(0, 12);
        public UpdateField<uint> FlagsB = new(0, 13);
        public UpdateField<Quaternion> ParentRotation = new(0, 14);
        public UpdateField<uint> FactionTemplate = new(0, 15);
        public UpdateField<sbyte> State = new(0, 16);
        public UpdateField<sbyte> TypeID = new(0, 17);
        public UpdateField<byte> PercentHealth = new(0, 18);
        public UpdateField<uint> ArtKit = new(0, 19);
        public UpdateField<uint> CustomParam = new(0, 20);
        public UpdateField<uint> Level = new(0, 21);
        public UpdateField<uint> AnimGroupInstance = new(0, 22);
        public UpdateField<uint> UiWidgetItemID = new(0, 23);
        public UpdateField<uint> UiWidgetItemQuality = new(0, 24);
        public UpdateField<uint> UiWidgetItemCount = new(0, 25);
        public OptionalUpdateField<GameObjectAssistActionData> AssistActionData = new(0, 26);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, GameObject owner)
        {
            data.WriteUInt32(DisplayID);
            data.WriteUInt32(SpellVisualID);
            data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimID(this, receiver, owner));
            data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimKitID(this, receiver, owner));
            var stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, receiver, owner);
            data.WriteInt32(stateWorldEffectIDs.Count);
            data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, receiver, owner));
            for (int i = 0; i < stateWorldEffectIDs.Count; ++i)
            {
                data.WriteUInt32(stateWorldEffectIDs[i]);
            }
            data.WritePackedGuid(CreatedBy);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GetViewerDependentGameObjectFlags(this, receiver, owner));
            data.WriteUInt32(FlagsB);
            Quaternion rotation = ParentRotation;
            data.WriteFloat(rotation.X);
            data.WriteFloat(rotation.Y);
            data.WriteFloat(rotation.Z);
            data.WriteFloat(rotation.W);
            data.WriteUInt32(FactionTemplate);
            data.WriteInt8(GetViewerDependentGameObjectState(this, receiver, owner));
            data.WriteInt8(TypeID);
            data.WriteUInt8(PercentHealth);
            data.WriteUInt32(ArtKit);
            data.WriteInt32(EnableDoodadSets.Size());
            data.WriteUInt32(CustomParam);
            data.WriteUInt32(Level);
            data.WriteUInt32(AnimGroupInstance);
            data.WriteUInt32(UiWidgetItemID);
            data.WriteUInt32(UiWidgetItemQuality);
            data.WriteUInt32(UiWidgetItemCount);
            data.WriteInt32(WorldEffects.Size());
            for (int i = 0; i < EnableDoodadSets.Size(); ++i)
            {
                data.WriteInt32(EnableDoodadSets[i]);
            }
            for (int i = 0; i < WorldEffects.Size(); ++i)
            {
                data.WriteInt32(WorldEffects[i]);
            }
            data.WriteBit(AssistActionData.HasValue());
            data.FlushBits();
            if (AssistActionData.HasValue())
            {
                AssistActionData.GetValue().WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, GameObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, GameObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 27);

            List<uint> stateWorldEffectIDs;

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, receiver, owner);
                    data.WriteBits(stateWorldEffectIDs.Count, 32);
                    for (int i = 0; i < stateWorldEffectIDs.Count; ++i)
                    {
                        data.WriteUInt32(stateWorldEffectIDs[i]);
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        EnableDoodadSets.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(EnableDoodadSets.Size(), data);
                }
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        WorldEffects.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(WorldEffects.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    for (int i = 0; i < EnableDoodadSets.Size(); ++i)
                    {
                        if (EnableDoodadSets.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(EnableDoodadSets[i]);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < WorldEffects.Size(); ++i)
                    {
                        if (WorldEffects.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(WorldEffects[i]);
                        }
                    }
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(DisplayID);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(SpellVisualID);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, receiver, owner));
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimID(this, receiver, owner));
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimKitID(this, receiver, owner));
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, receiver, owner));
                }
                if (changesMask[10])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (changesMask[11])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(GetViewerDependentGameObjectFlags(this, receiver, owner));
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(FlagsB);
                }
                if (changesMask[14])
                {
                    data.WriteFloat(((Quaternion)ParentRotation).X);
                    data.WriteFloat(((Quaternion)ParentRotation).Y);
                    data.WriteFloat(((Quaternion)ParentRotation).Z);
                    data.WriteFloat(((Quaternion)ParentRotation).W);
                }
                if (changesMask[15])
                {
                    data.WriteUInt32(FactionTemplate);
                }
                if (changesMask[16])
                {
                    data.WriteInt8(GetViewerDependentGameObjectState(this, receiver, owner));
                }
                if (changesMask[17])
                {
                    data.WriteInt8(TypeID);
                }
                if (changesMask[18])
                {
                    data.WriteUInt8(PercentHealth);
                }
                if (changesMask[19])
                {
                    data.WriteUInt32(ArtKit);
                }
                if (changesMask[20])
                {
                    data.WriteUInt32(CustomParam);
                }
                if (changesMask[21])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[22])
                {
                    data.WriteUInt32(AnimGroupInstance);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(UiWidgetItemID);
                }
                if (changesMask[24])
                {
                    data.WriteUInt32(UiWidgetItemQuality);
                }
                if (changesMask[25])
                {
                    data.WriteUInt32(UiWidgetItemCount);
                }
                data.WriteBit(AssistActionData.HasValue());
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[26])
                {
                    if (AssistActionData.HasValue())
                    {
                        AssistActionData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(StateWorldEffectIDs);
            ClearChangesMask(EnableDoodadSets);
            ClearChangesMask(WorldEffects);
            ClearChangesMask(DisplayID);
            ClearChangesMask(SpellVisualID);
            ClearChangesMask(StateSpellVisualID);
            ClearChangesMask(SpawnTrackingStateAnimID);
            ClearChangesMask(SpawnTrackingStateAnimKitID);
            ClearChangesMask(StateWorldEffectsQuestObjectiveID);
            ClearChangesMask(CreatedBy);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(Flags);
            ClearChangesMask(FlagsB);
            ClearChangesMask(ParentRotation);
            ClearChangesMask(FactionTemplate);
            ClearChangesMask(State);
            ClearChangesMask(TypeID);
            ClearChangesMask(PercentHealth);
            ClearChangesMask(ArtKit);
            ClearChangesMask(CustomParam);
            ClearChangesMask(Level);
            ClearChangesMask(AnimGroupInstance);
            ClearChangesMask(UiWidgetItemID);
            ClearChangesMask(UiWidgetItemQuality);
            ClearChangesMask(UiWidgetItemCount);
            ClearChangesMask(AssistActionData);
            _changesMask.ResetAll();
        }

        uint GetViewerDependentGameObjectFlags(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            uint flags = gameObjectData.Flags;
            if (gameObject.GetGoType() == GameObjectTypes.Chest)
                if (gameObject.GetGoInfo().IsUsingGroupLootRules() && !gameObject.IsLootAllowedFor(receiver))
                    flags |= (uint)(GameObjectFlags.Locked | GameObjectFlags.NotSelectable);

            return flags;
        }

        sbyte GetViewerDependentGameObjectState(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            return (sbyte)gameObject.GetGoStateFor(receiver.GetGUID());
        }

        List<uint> GetViewerDependentStateWorldEffectIDs(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            List<uint> stateWorldEffects = gameObjectData.StateWorldEffectIDs;

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateWorldEffects = spawnTrackingStateData.StateWorldEffects;

            return stateWorldEffects;
        }

        uint GetViewerDependentStateSpellVisualID(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            uint stateSpellVisual = gameObjectData.StateSpellVisualID;

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateSpellVisual = spawnTrackingStateData.StateSpellVisualId.GetValueOrDefault(0);

            return stateSpellVisual;
        }

        uint GetViewerDependentSpawnTrackingStateAnimID(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            uint stateAnimId = Global.DB2Mgr.GetEmptyAnimStateID();

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateAnimId = spawnTrackingStateData.StateAnimId.GetValueOrDefault((ushort)stateAnimId);

            return stateAnimId;
        }

        uint GetViewerDependentSpawnTrackingStateAnimKitID(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            uint stateAnimKitId = gameObjectData.SpawnTrackingStateAnimKitID;

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateAnimKitId = spawnTrackingStateData.StateAnimKitId.GetValueOrDefault(0);

            return stateAnimKitId;
        }

        uint GetViewerDependentStateWorldEffectsQuestObjectiveID(GameObjectFieldData gameObjectData, Player receiver, GameObject gameObject)
        {
            uint stateWorldEffectsQuestObjectiveId = gameObjectData.StateWorldEffectsQuestObjectiveID;

            if (stateWorldEffectsQuestObjectiveId == 0)
            {
                GameObjectData data = gameObject.GetGameObjectData();
                if (data != null)
                {
                    if (data.spawnTrackingQuestObjectives.Any())
                    {
                        // If there is no valid objective for player, fill UF with first objective (if any)
                        stateWorldEffectsQuestObjectiveId = data.spawnTrackingQuestObjectives.First();
                        int i = 0;
                        while (++i <= data.spawnTrackingQuestObjectives.Count)
                        {
                            var id = data.spawnTrackingQuestObjectives[i];
                            if (receiver.GetSpawnTrackingStateByObjective(data.spawnTrackingData.SpawnTrackingId, id) != SpawnTrackingState.Active)
                                continue;

                            stateWorldEffectsQuestObjectiveId = id;
                            break;
                        }
                    }

                }
            }

            return stateWorldEffectsQuestObjectiveId;
        }
    }

    public class DynamicObjectData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.DynamicObject, 7), IsUpdateFieldStructure<DynamicObject>
    {
        public UpdateField<ObjectGuid> Caster = new(0, 1);
        public UpdateField<byte> Type = new(0, 2);
        public UpdateField<SpellCastVisualField> SpellVisual = new(0, 3);
        public UpdateField<uint> SpellID = new(0, 4);
        public UpdateField<float> Radius = new(0, 5);
        public UpdateField<uint> CastTime = new(0, 6);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, DynamicObject owner)
        {
            data.WritePackedGuid(Caster);
            data.WriteUInt8(Type);
            ((SpellCastVisualField)SpellVisual).WriteCreate(data, receiver, owner);
            data.WriteUInt32(SpellID);
            data.WriteFloat(Radius);
            data.WriteUInt32(CastTime);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, DynamicObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, DynamicObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(_changesMask.GetBlock(0), 7);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WritePackedGuid(Caster);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt8(Type);
                }
                if (_changesMask[3])
                {
                    ((SpellCastVisualField)SpellVisual).WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(SpellID);
                }
                if (_changesMask[5])
                {
                    data.WriteFloat(Radius);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt32(CastTime);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Caster);
            ClearChangesMask(Type);
            ClearChangesMask(SpellVisual);
            ClearChangesMask(SpellID);
            ClearChangesMask(Radius);
            ClearChangesMask(CastTime);
            _changesMask.ResetAll();
        }
    }

    public class CorpseData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Corpse, 33), IsUpdateFieldStructure<Corpse>
    {
        public DynamicUpdateField<ChrCustomizationChoice> Customizations = new(0, 1);
        public UpdateField<uint> DynamicFlags = new(0, 2);
        public UpdateField<ObjectGuid> Owner = new(0, 3);
        public UpdateField<ObjectGuid> PartyGUID = new(0, 4);
        public UpdateField<ObjectGuid> GuildGUID = new(0, 5);
        public UpdateField<uint> DisplayID = new(0, 6);
        public UpdateField<byte> RaceID = new(0, 7);
        public UpdateField<byte> Sex = new(0, 8);
        public UpdateField<byte> Class = new(0, 9);
        public UpdateField<uint> Flags = new(0, 10);
        public UpdateField<int> FactionTemplate = new(0, 11);
        public UpdateField<uint> StateSpellVisualKitID = new(0, 12);
        public UpdateFieldArray<uint> Items = new(19, 13, 14);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Corpse owner)
        {
            data.WriteUInt32(DynamicFlags);
            data.WritePackedGuid(Owner);
            data.WritePackedGuid(PartyGUID);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(DisplayID);
            for (int i = 0; i < 19; ++i)
            {
                data.WriteUInt32(Items[i]);
            }
            data.WriteUInt8(RaceID);
            data.WriteUInt8(Sex);
            data.WriteUInt8(Class);
            data.WriteInt32(Customizations.Size());
            data.WriteUInt32(Flags);
            data.WriteInt32(FactionTemplate);
            data.WriteUInt32(StateSpellVisualKitID);
            for (int i = 0; i < Customizations.Size(); ++i)
            {
                Customizations[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Corpse owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Corpse owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 2);
            for (uint i = 0; i < 2; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        Customizations.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Customizations.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Customizations.Size(); ++i)
                    {
                        if (Customizations.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Customizations[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(DynamicFlags);
                }
                if (changesMask[3])
                {
                    data.WritePackedGuid(Owner);
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(PartyGUID);
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(DisplayID);
                }
                if (changesMask[7])
                {
                    data.WriteUInt8(RaceID);
                }
                if (changesMask[8])
                {
                    data.WriteUInt8(Sex);
                }
                if (changesMask[9])
                {
                    data.WriteUInt8(Class);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(Flags);
                }
                if (changesMask[11])
                {
                    data.WriteInt32(FactionTemplate);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(StateSpellVisualKitID);
                }
            }
            if (changesMask[13])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[14 + i])
                    {
                        data.WriteUInt32(Items[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Customizations);
            ClearChangesMask(DynamicFlags);
            ClearChangesMask(Owner);
            ClearChangesMask(PartyGUID);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(DisplayID);
            ClearChangesMask(RaceID);
            ClearChangesMask(Sex);
            ClearChangesMask(Class);
            ClearChangesMask(Flags);
            ClearChangesMask(FactionTemplate);
            ClearChangesMask(StateSpellVisualKitID);
            ClearChangesMask(Items);
            _changesMask.ResetAll();
        }
    }

    public class ScaleCurve() : HasChangesMask(7), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<bool> OverrideActive = new(0, 1);
        public UpdateField<uint> StartTimeOffset = new(0, 2);
        public UpdateField<uint> ParameterCurve = new(0, 3);
        public UpdateFieldArray<Vector2> Points = new(2, 4, 5);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteUInt32(StartTimeOffset);
            for (int i = 0; i < 2; ++i)
            {
                data.WriteVector2(Points[i]);
            }
            data.WriteUInt32(ParameterCurve);
            data.WriteBit((bool)OverrideActive);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 7);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(OverrideActive);
                }
            }

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    data.WriteUInt32(StartTimeOffset);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(ParameterCurve);
                }
            }
            if (changesMask[4])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[5 + i])
                    {
                        data.WriteVector2(Points[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(OverrideActive);
            ClearChangesMask(StartTimeOffset);
            ClearChangesMask(ParameterCurve);
            ClearChangesMask(Points);
            _changesMask.ResetAll();
        }
    }

    public class VisualAnim() : HasChangesMask(0, TypeId.AreaTrigger, 5), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<bool> IsDecay = new(0, 1);
        public OptionalUpdateField<ushort> AnimationDataID = new(0, 2);
        public UpdateField<uint> AnimKitID = new(0, 3);
        public UpdateField<uint> AnimProgress = new(0, 4);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteBit(AnimationDataID.HasValue());
            data.WriteBit(IsDecay);
            data.FlushBits();
            data.WriteUInt32(AnimKitID);
            data.WriteUInt32(AnimProgress);
            if (AnimationDataID.HasValue())
            {
                data.WriteUInt16(AnimationDataID);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 5);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(IsDecay);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteBit(AnimationDataID.HasValue());
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    data.WriteUInt32(AnimKitID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(AnimProgress);
                }
                if (changesMask[2])
                {
                    if (AnimationDataID.HasValue())
                    {
                        data.WriteInt32(AnimationDataID);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(IsDecay);
            ClearChangesMask(AnimationDataID);
            ClearChangesMask(AnimKitID);
            ClearChangesMask(AnimProgress);
            _changesMask.ResetAll();
        }
    }

    public struct ForceSetAreaTriggerPositionAndRotation : IEquatable<ForceSetAreaTriggerPositionAndRotation>, IsUpdateFieldStructure<AreaTrigger>
    {
        public ObjectGuid TriggerGUID;
        public Position Pos;
        public Quaternion Rotation;

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WritePackedGuid(TriggerGUID);
            data.WriteVector3(Pos);
            data.WriteFloat(Rotation.X);
            data.WriteFloat(Rotation.Y);
            data.WriteFloat(Rotation.Z);
            data.WriteFloat(Rotation.W);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner) => WriteCreate(data, receiver, owner);

        public bool Equals(ForceSetAreaTriggerPositionAndRotation right)
        {
            return TriggerGUID == right.TriggerGUID
                && Pos == right.Pos
                && Rotation == right.Rotation;
        }
    }

    public class AreaTriggerActionSetPeriodModifier() : HasChangesMask(3), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<int> Field_0 = new(0, 1);
        public UpdateField<float> Field_4 = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteInt32(Field_0);
            data.WriteFloat(Field_4);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(Field_0);
                }
                if (changesMask[2])
                {
                    data.WriteFloat(Field_4);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Field_0);
            ClearChangesMask(Field_4);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerSplineCalculator() : HasChangesMask(3), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<bool> Catmullrom = new(0, 1);
        public DynamicUpdateField<Vector3> Points = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteBits(Points.Size(), 16);
            data.WriteBit(Catmullrom);
            data.FlushBits();
            for (int i = 0; i < Points.Size(); ++i)
            {
                data.WriteVector3(Points[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(Catmullrom);
                }
                if (changesMask[2])
                {
                    if (!ignoreChangesMask)
                        Points.WriteUpdateMask(data, 16);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Points.Size(), data, 16);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    for (int i = 0; i < Points.Size(); ++i)
                    {
                        if (Points.HasChanged(i) || ignoreChangesMask)
                        {
                            data.WriteVector3(Points[i]);
                        }
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Catmullrom);
            ClearChangesMask(Points);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerOrbit() : HasChangesMask(7), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<bool> CounterClockwise = new(0, 1);
        public UpdateField<Vector3> Center = new(0, 2);
        public UpdateField<float> Radius = new(0, 3);
        public UpdateField<float> InitialAngle = new(0, 4);
        public UpdateField<float> BlendFromRadius = new(0, 5);
        public UpdateField<int> ExtraTimeForBlending = new(0, 6);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteVector3(Center);
            data.WriteFloat(Radius);
            data.WriteFloat(InitialAngle);
            data.WriteFloat(BlendFromRadius);
            data.WriteInt32(ExtraTimeForBlending);
            data.WriteBit(CounterClockwise);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 7);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(CounterClockwise);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    data.WriteVector3(Center);
                }
                if (changesMask[3])
                {
                    data.WriteFloat(Radius);
                }
                if (changesMask[4])
                {
                    data.WriteFloat(InitialAngle);
                }
                if (changesMask[5])
                {
                    data.WriteFloat(BlendFromRadius);
                }
                if (changesMask[6])
                {
                    data.WriteInt32(ExtraTimeForBlending);
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(CounterClockwise);
            ClearChangesMask(Center);
            ClearChangesMask(Radius);
            ClearChangesMask(InitialAngle);
            ClearChangesMask(BlendFromRadius);
            ClearChangesMask(ExtraTimeForBlending);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerMovementScript() : HasChangesMask(4), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<int> SpellScriptID = new(0, 1);
        public UpdateField<Vector3> Center = new(0, 2);
        public UpdateField<uint> CreationTime = new(0, 3);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteInt32(SpellScriptID);
            data.WriteVector3(Center);
            data.WriteUInt32(CreationTime);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(SpellScriptID);
                }
                if (changesMask[2])
                {
                    data.WriteVector3(Center);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(CreationTime);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(SpellScriptID);
            ClearChangesMask(Center);
            ClearChangesMask(CreationTime);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerSphere() : HasChangesMask(3), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<float> Radius = new(0, 1);
        public UpdateField<float> RadiusTarget = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteFloat(Radius);
            data.WriteFloat(RadiusTarget);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteFloat(Radius);
                }
                if (changesMask[2])
                {
                    data.WriteFloat(RadiusTarget);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Radius);
            ClearChangesMask(RadiusTarget);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerBox() : HasChangesMask(3), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<Vector3> Extents = new(0, 1);
        public UpdateField<Vector3> ExtentsTarget = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteVector3(Extents);
            data.WriteVector3(ExtentsTarget);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteVector3(Extents);
                }
                if (changesMask[2])
                {
                    data.WriteVector3(ExtentsTarget);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Extents);
            ClearChangesMask(ExtentsTarget);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerPolygon() : HasChangesMask(5), IsUpdateFieldStructure<AreaTrigger>
    {
        public DynamicUpdateField<Vector2> Vertices = new(0, 1);
        public DynamicUpdateField<Vector2> VerticesTarget = new(0, 2);
        public UpdateField<float> Height = new(0, 3);
        public UpdateField<float> HeightTarget = new(0, 4);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteInt32(Vertices.Size());
            data.WriteInt32(VerticesTarget.Size());
            data.WriteFloat(Height);
            data.WriteFloat(HeightTarget);
            for (int i = 0; i < Vertices.Size(); ++i)
            {
                data.WriteVector2(Vertices[i]);
            }
            for (int i = 0; i < VerticesTarget.Size(); ++i)
            {
                data.WriteVector2(VerticesTarget[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 5);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreChangesMask)
                        Vertices.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Vertices.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreChangesMask)
                        VerticesTarget.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(VerticesTarget.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Vertices.Size(); ++i)
                    {
                        if (Vertices.HasChanged(i) || ignoreChangesMask)
                        {
                            data.WriteVector2(Vertices[i]);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < VerticesTarget.Size(); ++i)
                    {
                        if (VerticesTarget.HasChanged(i) || ignoreChangesMask)
                        {
                            data.WriteVector2(VerticesTarget[i]);
                        }
                    }
                }
                if (changesMask[3])
                {
                    data.WriteFloat(Height);
                }
                if (changesMask[4])
                {
                    data.WriteFloat(HeightTarget);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Vertices);
            ClearChangesMask(VerticesTarget);
            ClearChangesMask(Height);
            ClearChangesMask(HeightTarget);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerCylinder() : HasChangesMask(7), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<float> Radius = new(0, 1);
        public UpdateField<float> RadiusTarget = new(0, 2);
        public UpdateField<float> Height = new(0, 3);
        public UpdateField<float> HeightTarget = new(0, 4);
        public UpdateField<float> LocationZOffset = new(0, 5);
        public UpdateField<float> LocationZOffsetTarget = new(0, 6);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteFloat(Radius);
            data.WriteFloat(RadiusTarget);
            data.WriteFloat(Height);
            data.WriteFloat(HeightTarget);
            data.WriteFloat(LocationZOffset);
            data.WriteFloat(LocationZOffsetTarget);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 7);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteFloat(Radius);
                }
                if (changesMask[2])
                {
                    data.WriteFloat(RadiusTarget);
                }
                if (changesMask[3])
                {
                    data.WriteFloat(Height);
                }
                if (changesMask[4])
                {
                    data.WriteFloat(HeightTarget);
                }
                if (changesMask[5])
                {
                    data.WriteFloat(LocationZOffset);
                }
                if (changesMask[6])
                {
                    data.WriteFloat(LocationZOffsetTarget);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Radius);
            ClearChangesMask(RadiusTarget);
            ClearChangesMask(Height);
            ClearChangesMask(HeightTarget);
            ClearChangesMask(LocationZOffset);
            ClearChangesMask(LocationZOffsetTarget);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerDisk() : HasChangesMask(9), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<float> InnerRadius = new(0, 1);
        public UpdateField<float> InnerRadiusTarget = new(0, 2);
        public UpdateField<float> OuterRadius = new(0, 3);
        public UpdateField<float> OuterRadiusTarget = new(0, 4);
        public UpdateField<float> Height = new(0, 5);
        public UpdateField<float> HeightTarget = new(0, 6);
        public UpdateField<float> LocationZOffset = new(0, 7);
        public UpdateField<float> LocationZOffsetTarget = new(0, 8);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteFloat(InnerRadius);
            data.WriteFloat(InnerRadiusTarget);
            data.WriteFloat(OuterRadius);
            data.WriteFloat(OuterRadiusTarget);
            data.WriteFloat(Height);
            data.WriteFloat(HeightTarget);
            data.WriteFloat(LocationZOffset);
            data.WriteFloat(LocationZOffsetTarget);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 9);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteFloat(InnerRadius);
                }
                if (changesMask[2])
                {
                    data.WriteFloat(InnerRadiusTarget);
                }
                if (changesMask[3])
                {
                    data.WriteFloat(OuterRadius);
                }
                if (changesMask[4])
                {
                    data.WriteFloat(OuterRadiusTarget);
                }
                if (changesMask[5])
                {
                    data.WriteFloat(Height);
                }
                if (changesMask[6])
                {
                    data.WriteFloat(HeightTarget);
                }
                if (changesMask[7])
                {
                    data.WriteFloat(LocationZOffset);
                }
                if (changesMask[8])
                {
                    data.WriteFloat(LocationZOffsetTarget);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(InnerRadius);
            ClearChangesMask(InnerRadiusTarget);
            ClearChangesMask(OuterRadius);
            ClearChangesMask(OuterRadiusTarget);
            ClearChangesMask(Height);
            ClearChangesMask(HeightTarget);
            ClearChangesMask(LocationZOffset);
            ClearChangesMask(LocationZOffsetTarget);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerBoundedPlane() : HasChangesMask(3), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<Vector2> Extents = new(0, 1);
        public UpdateField<Vector2> ExtentsTarget = new(0, 2);

        public void WriteCreate(WorldPacket data, Player receiver, AreaTrigger owner)
        {
            data.WriteVector2(Extents);
            data.WriteVector2(ExtentsTarget);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteVector2(Extents);
                }
                if (changesMask[2])
                {
                    data.WriteVector2(ExtentsTarget);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Extents);
            ClearChangesMask(ExtentsTarget);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerFieldData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.AreaTrigger, 39), IsUpdateFieldStructure<AreaTrigger>
    {
        public UpdateField<ScaleCurve> OverrideScaleCurve = new(0, 1);
        public UpdateField<ScaleCurve> ExtraScaleCurve = new(0, 2);
        public UpdateField<ScaleCurve> OverrideMoveCurveX = new(0, 3);
        public UpdateField<ScaleCurve> OverrideMoveCurveY = new(0, 4);
        public UpdateField<ScaleCurve> OverrideMoveCurveZ = new(0, 5);
        public UpdateField<ScaleCurve> Unk1205Curve = new(0, 6);
        public UpdateField<ObjectGuid> Caster = new(0, 7);
        public UpdateField<uint> Duration = new(0, 8);
        public UpdateField<uint> TimeToTarget = new(0, 9);
        public UpdateField<uint> TimeToTargetScale = new(0, 10);
        public UpdateField<uint> TimeToTargetExtraScale = new(0, 11);
        public UpdateField<uint> TimeToTargetPos = new(0, 12);                                 // Linked to m_overrideMoveCurve
        public UpdateField<uint> TimeToTargetUnk1205Curve = new(0, 13);                        // Linked to m_unk1205Curve
        public UpdateField<uint> SpellID = new(0, 14);
        public UpdateField<uint> SpellForVisuals = new(0, 15);
        public UpdateField<SpellCastVisualField> SpellVisual = new(0, 16);
        public UpdateField<float> BoundsRadius2D = new(0, 17);
        public UpdateField<uint> DecalPropertiesID = new(0, 18);
        public UpdateField<ObjectGuid> CreatingEffectGUID = new(0, 19);
        public UpdateField<ObjectGuid> OrbitPathTarget = new(0, 20);
        public UpdateField<Position> RollPitchYaw = new(0, 21);
        public UpdateField<int> PositionalSoundKitID = new(0, 22);
        public UpdateField<uint> MovementStartTime = new(0, 23);
        public UpdateField<uint> CreationTime = new(0, 24);
        public UpdateField<float> ZOffset = new(0, 25);
        public OptionalUpdateField<Position> TargetRollPitchYaw = new(0, 26);
        public UpdateField<uint> Flags = new(0, 27);
        public UpdateField<VisualAnim> VisualAnim = new(0, 28);
        public UpdateField<uint> ScaleCurveId = new(0, 29);
        public UpdateField<uint> FacingCurveId = new(0, 30);
        public UpdateField<uint> MorphCurveId = new(0, 31);
        public UpdateField<uint> MoveCurveId = new(32, 33);
        public UpdateField<float> Facing = new(32, 34);
        public OptionalUpdateField<ForceSetAreaTriggerPositionAndRotation> ForcedPositionAndRotation = new(32, 35);
        public UpdateField<int> PathType = new(32, 36);
        public VariantUpdateField PathData = new(32, 36);//, AreaTriggerSplineCalculator, AreaTriggerOrbit, AreaTriggerMovementScript);
        public UpdateField<byte> ShapeType = new(32, 37);
        public VariantUpdateField ShapeData = new(32, 37);//, AreaTriggerSphere, AreaTriggerBox, AreaTriggerPolygon, AreaTriggerCylinder, AreaTriggerDisk, AreaTriggerBoundedPlane);
        public UpdateField<AreaTriggerActionSetPeriodModifier> PeriodModifier = new(32, 38);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            OverrideScaleCurve.GetValue().WriteCreate(data, receiver, owner);
            ExtraScaleCurve.GetValue().WriteCreate(data, receiver, owner);
            OverrideMoveCurveX.GetValue().WriteCreate(data, receiver, owner);
            OverrideMoveCurveY.GetValue().WriteCreate(data, receiver, owner);
            OverrideMoveCurveZ.GetValue().WriteCreate(data, receiver, owner);
            Unk1205Curve.GetValue().WriteCreate(data, receiver, owner);
            data.WritePackedGuid(Caster);
            data.WriteUInt32(Duration);
            data.WriteUInt32(TimeToTarget);
            data.WriteUInt32(TimeToTargetScale);
            data.WriteUInt32(TimeToTargetExtraScale);
            data.WriteUInt32(TimeToTargetPos);
            data.WriteUInt32(TimeToTargetUnk1205Curve);
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellForVisuals);
            SpellVisual.GetValue().WriteCreate(data, receiver, owner);
            data.WriteFloat(BoundsRadius2D);
            data.WriteUInt32(DecalPropertiesID);
            data.WritePackedGuid(CreatingEffectGUID);
            data.WritePackedGuid(OrbitPathTarget);
            data.WriteVector3(RollPitchYaw.GetValue());
            data.WriteInt32(PositionalSoundKitID);
            data.WriteUInt32(MovementStartTime);
            data.WriteUInt32(CreationTime);
            data.WriteFloat(ZOffset);
            data.WriteUInt32(Flags);
            VisualAnim.GetValue().WriteCreate(data, receiver, owner);
            data.WriteUInt32(ScaleCurveId);
            data.WriteUInt32(FacingCurveId);
            data.WriteUInt32(MorphCurveId);
            data.WriteUInt32(MoveCurveId);
            data.WriteFloat(Facing);
            data.WriteInt32(PathType);
            data.WriteUInt8(ShapeType);
            PeriodModifier.GetValue().WriteCreate(data, receiver, owner);
            if (PathType == 0)
            {
                PathData.Get<AreaTriggerSplineCalculator>().WriteCreate(data, receiver, owner);
            }
            if (PathType == 1)
            {
                PathData.Get<AreaTriggerOrbit>().WriteCreate(data, receiver, owner);
            }
            if (PathType == 3)
            {
                PathData.Get<AreaTriggerMovementScript>().WriteCreate(data, receiver, owner);
            }
            if (ShapeType == 0)
            {
                ShapeData.Get<AreaTriggerSphere>().WriteCreate(data, receiver, owner);
            }
            if (ShapeType == 1)
            {
                ShapeData.Get<AreaTriggerBox>().WriteCreate(data, receiver, owner);
            }
            if (ShapeType == 2 || ShapeType == 3 || ShapeType == 5 || ShapeType == 6)
            {
                ShapeData.Get<AreaTriggerPolygon>().WriteCreate(data, receiver, owner);
            }
            if (ShapeType == 4)
            {
                ShapeData.Get<AreaTriggerCylinder>().WriteCreate(data, receiver, owner);
            }
            if (ShapeType == 7)
            {
                ShapeData.Get<AreaTriggerDisk>().WriteCreate(data, receiver, owner);
            }
            if (ShapeType == 8)
            {
                ShapeData.Get<AreaTriggerBoundedPlane>().WriteCreate(data, receiver, owner);
            }
            data.WriteBit(TargetRollPitchYaw.HasValue());
            data.WriteBit(ForcedPositionAndRotation.HasValue());
            data.FlushBits();
            if (TargetRollPitchYaw.HasValue())
            {
                data.WriteVector3(TargetRollPitchYaw.GetValue());
            }
            if (ForcedPositionAndRotation.HasValue())
            {
                ForcedPositionAndRotation.GetValue().WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, AreaTrigger owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, AreaTrigger owner, bool ignoreNestedChangesMask)
        {
            data.WriteUInt32(changesMask.GetBlock(0));
            data.WriteBits(changesMask.GetBlock(1), 7);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    OverrideScaleCurve.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[2])
                {
                    ExtraScaleCurve.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[3])
                {
                    OverrideMoveCurveX.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[4])
                {
                    OverrideMoveCurveY.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[5])
                {
                    OverrideMoveCurveZ.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[6])
                {
                    Unk1205Curve.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[7])
                {
                    data.WritePackedGuid(Caster);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(Duration);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(TimeToTarget);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(TimeToTargetScale);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(TimeToTargetExtraScale);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(TimeToTargetPos);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(TimeToTargetUnk1205Curve);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(SpellID);
                }
                if (changesMask[15])
                {
                    data.WriteUInt32(SpellForVisuals);
                }
                if (changesMask[16])
                {
                    SpellVisual.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[17])
                {
                    data.WriteFloat(BoundsRadius2D);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(DecalPropertiesID);
                }
                if (changesMask[19])
                {
                    data.WritePackedGuid(CreatingEffectGUID);
                }
                if (changesMask[20])
                {
                    data.WritePackedGuid(OrbitPathTarget);
                }
                if (changesMask[21])
                {
                    data.WriteVector3(RollPitchYaw.GetValue());
                }
                if (changesMask[22])
                {
                    data.WriteInt32(PositionalSoundKitID);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(MovementStartTime);
                }
                if (changesMask[24])
                {
                    data.WriteUInt32(CreationTime);
                }
                if (changesMask[25])
                {
                    data.WriteFloat(ZOffset);
                }
                if (changesMask[27])
                {
                    data.WriteUInt32(Flags);
                }
                if (changesMask[28])
                {
                    VisualAnim.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[29])
                {
                    data.WriteUInt32(ScaleCurveId);
                }
                if (changesMask[30])
                {
                    data.WriteUInt32(FacingCurveId);
                }
                if (changesMask[31])
                {
                    data.WriteUInt32(MorphCurveId);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt32(MoveCurveId);
                }
                if (changesMask[34])
                {
                    data.WriteFloat(Facing);
                }
                if (changesMask[36])
                {
                    data.WriteInt32(PathType);
                }
                if (changesMask[37])
                {
                    data.WriteUInt8(ShapeType);
                }
                if (changesMask[38])
                {
                    PeriodModifier.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[36])
                {
                    if (PathType == 0)
                    {
                        PathData.Get<AreaTriggerSplineCalculator>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (PathType == 1)
                    {
                        PathData.Get<AreaTriggerOrbit>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (PathType == 3)
                    {
                        PathData.Get<AreaTriggerMovementScript>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[37])
                {
                    if (ShapeType == 0)
                    {
                        ShapeData.Get<AreaTriggerSphere>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (ShapeType == 1)
                    {
                        ShapeData.Get<AreaTriggerBox>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (ShapeType == 2 || ShapeType == 3 || ShapeType == 5 || ShapeType == 6)
                    {
                        ShapeData.Get<AreaTriggerPolygon>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (ShapeType == 4)
                    {
                        ShapeData.Get<AreaTriggerCylinder>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (ShapeType == 7)
                    {
                        ShapeData.Get<AreaTriggerDisk>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                    if (ShapeType == 8)
                    {
                        ShapeData.Get<AreaTriggerBoundedPlane>().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            if (changesMask[0])
            {
                data.WriteBit(TargetRollPitchYaw.HasValue());
            }
            if (changesMask[32])
            {
                data.WriteBit(ForcedPositionAndRotation.HasValue());
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[26])
                {
                    if (TargetRollPitchYaw.HasValue())
                    {
                        data.WriteVector3(TargetRollPitchYaw.GetValue());
                    }
                }
            }
            if (changesMask[32])
            {
                if (changesMask[35])
                {
                    if (ForcedPositionAndRotation.HasValue())
                    {
                        ForcedPositionAndRotation.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(OverrideScaleCurve);
            ClearChangesMask(ExtraScaleCurve);
            ClearChangesMask(OverrideMoveCurveX);
            ClearChangesMask(OverrideMoveCurveY);
            ClearChangesMask(OverrideMoveCurveZ);
            ClearChangesMask(Unk1205Curve);
            ClearChangesMask(Caster);
            ClearChangesMask(Duration);
            ClearChangesMask(TimeToTarget);
            ClearChangesMask(TimeToTargetScale);
            ClearChangesMask(TimeToTargetExtraScale);
            ClearChangesMask(TimeToTargetPos);
            ClearChangesMask(TimeToTargetUnk1205Curve);
            ClearChangesMask(SpellID);
            ClearChangesMask(SpellForVisuals);
            ClearChangesMask(SpellVisual);
            ClearChangesMask(BoundsRadius2D);
            ClearChangesMask(DecalPropertiesID);
            ClearChangesMask(CreatingEffectGUID);
            ClearChangesMask(OrbitPathTarget);
            ClearChangesMask(RollPitchYaw);
            ClearChangesMask(PositionalSoundKitID);
            ClearChangesMask(MovementStartTime);
            ClearChangesMask(CreationTime);
            ClearChangesMask(ZOffset);
            ClearChangesMask(TargetRollPitchYaw);
            ClearChangesMask(Flags);
            ClearChangesMask(VisualAnim);
            ClearChangesMask(ScaleCurveId);
            ClearChangesMask(FacingCurveId);
            ClearChangesMask(MorphCurveId);
            ClearChangesMask(MoveCurveId);
            ClearChangesMask(Facing);
            ClearChangesMask(ForcedPositionAndRotation);
            ClearChangesMask(PathType);
            ClearChangesMask(PathData);
            ClearChangesMask(ShapeType);
            ClearChangesMask(ShapeData);
            ClearChangesMask(PeriodModifier);
            _changesMask.ResetAll();
        }
    }

    public class SceneObjectData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.SceneObject, 5), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<int> ScriptPackageID = new(0, 1);
        public UpdateField<uint> RndSeedVal = new(0, 2);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 3);
        public UpdateField<uint> SceneType = new(0, 4);

        //(nt|UpdateFieldFlag fieldVisibilityFlags),
        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WriteInt32(ScriptPackageID);
            data.WriteUInt32(RndSeedVal);
            data.WritePackedGuid(CreatedBy);
            data.WriteUInt32(SceneType);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, WorldObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(_changesMask.GetBlock(0), 5);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteInt32(ScriptPackageID);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt32(RndSeedVal);
                }
                if (_changesMask[3])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(SceneType);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ScriptPackageID);
            ClearChangesMask(RndSeedVal);
            ClearChangesMask(CreatedBy);
            ClearChangesMask(SceneType);
            _changesMask.ResetAll();
        }
    }

    public class ConversationLine : IEquatable<ConversationLine>, IsUpdateFieldStructure<Conversation>
    {
        public uint ConversationLineID;
        public uint BroadcastTextID;
        public uint StartTime;
        public uint UiCameraID;
        public byte ActorIndex;
        public byte Flags;
        public byte ChatType;

        public void WriteCreate(WorldPacket data, Player receiver, Conversation owner)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(BroadcastTextID);
            data.WriteUInt32(GetViewerStartTime(this, receiver, owner));
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
            data.WriteUInt8(ChatType);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Conversation owner) => WriteCreate(data, receiver, owner);

        public uint GetViewerStartTime(ConversationLine conversationLine, Player receiver, Conversation conversation)
        {
            uint startTime = conversationLine.StartTime;
            Locale locale = receiver.GetSession().GetSessionDbLocaleIndex();

            TimeSpan localizedStartTime = conversation.GetLineStartTime(locale, (int)conversationLine.ConversationLineID);
            if (localizedStartTime != TimeSpan.Zero)
                startTime = (uint)localizedStartTime.TotalMilliseconds;

            return startTime;
        }

        public bool Equals(ConversationLine right)
        {
            return ConversationLineID == right.ConversationLineID
                && BroadcastTextID == right.BroadcastTextID
                && StartTime == right.StartTime
                && UiCameraID == right.UiCameraID
                && ActorIndex == right.ActorIndex
                && Flags == right.Flags
                && ChatType == right.ChatType;
        }
    }

    public class ConversationActorField : IEquatable<ConversationActorField>, IsUpdateFieldStructure<Conversation>
    {
        public uint CreatureID;
        public uint CreatureDisplayInfoID;
        public ObjectGuid ActorGUID;
        public int Id;
        public ConversationActorType Type;
        public uint NoActorObject;

        public void WriteCreate(WorldPacket data, Player receiver, Conversation owner)
        {
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.WritePackedGuid(ActorGUID);
            data.WriteInt32(Id);
            data.WriteBits(Type, 1);
            data.WriteBits(NoActorObject, 1);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Conversation owner) => WriteCreate(data, receiver, owner);

        public bool Equals(ConversationActorField right)
        {
            return CreatureID == right.CreatureID
                && CreatureDisplayInfoID == right.CreatureDisplayInfoID
                && ActorGUID == right.ActorGUID
                && Id == right.Id
                && Type == right.Type
                && NoActorObject == right.NoActorObject;
        }
    }

    public class ConversationData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Conversation, 8), IsUpdateFieldStructure<Conversation>
    {
        public UpdateField<bool> DontPlayBroadcastTextSounds = new(0, 1);
        public UpdateField<bool> Field_33 = new(0, 2);                                           // UNK: Prevents line lookup from succeeding
        public UpdateField<List<ConversationLine>> Lines = new(0, 3);
        public DynamicUpdateField<ConversationActorField> Actors = new(0, 4);
        public UpdateField<uint> LastLineEndTime = new(0, 5);
        public UpdateField<uint> Progress = new(0, 6);
        public UpdateField<uint> Flags = new(0, 7);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Conversation owner)
        {
            data.WriteInt32(Lines.GetValue().Count);
            data.WriteUInt32(GetViewerLastLineEndTime(this, receiver, owner));
            data.WriteUInt32(Progress);
            for (int i = 0; i < Lines.GetValue().Count; ++i)
            {
                Lines.GetValue()[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBit(DontPlayBroadcastTextSounds);
            data.WriteBit(Field_33);
            data.WriteInt32(Actors.Size());
            data.WriteUInt32(Flags);
            for (int i = 0; i < Actors.Size(); ++i)
            {
                Actors[i].WriteCreate(data, receiver, owner);
            }
            data.FlushBits();
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Conversation owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Conversation owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(_changesMask.GetBlock(0), 8);

            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteBit(DontPlayBroadcastTextSounds);
                }
                if (changesMask[2])
                {
                    data.WriteBit(Field_33);
                }
                if (changesMask[3])
                {
                    List<ConversationLine> list = Lines;
                    data.WriteBits(list.Count, 32);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        list[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[4])
                {
                    if (!ignoreNestedChangesMask)
                        Actors.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Actors.Size(), data);
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[4])
                {
                    for (int i = 0; i < Actors.Size(); ++i)
                    {
                        if (Actors.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Actors[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(GetViewerLastLineEndTime(this, receiver, owner));
                }
                if (_changesMask[6])
                {
                    data.WriteUInt32(Progress);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(Flags);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(DontPlayBroadcastTextSounds);
            ClearChangesMask(Field_33);
            ClearChangesMask(Lines);
            ClearChangesMask(Actors);
            ClearChangesMask(LastLineEndTime);
            ClearChangesMask(Progress);
            _changesMask.ResetAll();
        }

        public uint GetViewerLastLineEndTime(ConversationData conversationLineData, Player receiver, Conversation conversation)
        {
            Locale locale = receiver.GetSession().GetSessionDbLocaleIndex();
            return (uint)conversation.GetLastLineEndTime(locale).TotalMilliseconds;
        }
    }

    class MeshObjectData() : HasChangesMask(5), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<bool> IsWMO = new(0, 1);
        public UpdateField<bool> IsRoom = new(0, 2);
        public UpdateField<int> FileDataID = new(0, 3);
        public OptionalUpdateField<AaBox> Geobox = new(0, 4);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WriteBit(IsWMO);
            data.WriteBit(IsRoom);
            data.WriteBit(Geobox.HasValue());
            data.FlushBits();
            data.WriteInt32(FileDataID);
            if (Geobox.HasValue())
            {
                Geobox.GetValue().WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, WorldObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 5);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(IsWMO);
                }
                if (changesMask[2])
                {
                    data.WriteBit(IsRoom);
                }
                data.WriteBit(Geobox.HasValue());
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    data.WriteInt32(FileDataID);
                }
                if (changesMask[4])
                {
                    if (Geobox.HasValue())
                    {
                        Geobox.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(IsWMO);
            ClearChangesMask(IsRoom);
            ClearChangesMask(FileDataID);
            ClearChangesMask(Geobox);
            _changesMask.ResetAll();
        }
    }

    public class VendorData() : HasChangesMask(2), IsUpdateFieldStructure<Creature>
    {
        public UpdateField<int> Flags = new(0, 1);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Creature owner)
        {
            data.WriteInt32(Flags);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Creature owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Creature owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 2);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(Flags);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Flags);
            _changesMask.ResetAll();
        }
    }

    class DecorDyeSlots : IEquatable<DecorDyeSlots>, IsUpdateFieldStructure<BaseEntity>
    {
        public int[] DyeColorID = new int[3];

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            for (uint i = 0; i < 3; ++i)
            {
                data.WriteInt32(DyeColorID[i]);
            }
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner) => WriteCreate(data, receiver, owner);

        public bool Equals(DecorDyeSlots right)
        {
            return DyeColorID.SequenceEqual(right.DyeColorID);
        }
    }

    class DecorStoragePersistedData() : HasChangesMask(5), IsUpdateFieldStructure<BaseEntity>
    {
        public UpdateField<ObjectGuid> HouseGUID = new(-1, 0);
        public UpdateField<byte> PlacementStatus = new(-1, 1);
        public OptionalUpdateField<DecorDyeSlots> DyeSlots = new(-1, 2);
        public UpdateField<byte> SourceType = new(-1, 3);
        public UpdateFieldString SourceValue = new(-1, 4);

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(HouseGUID);
            data.WriteUInt8(PlacementStatus);
            data.WriteUInt8(SourceType);
            data.WriteBit(DyeSlots.HasValue());
            data.WriteBits(SourceValue.Size() + 1, 24);
            data.FlushBits();
            if (DyeSlots.HasValue())
            {
                DyeSlots.GetValue().WriteCreate(data, receiver, owner);
            }
            data.WriteCString(SourceValue);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 5);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WritePackedGuid(HouseGUID);
            }
            if (changesMask[1])
            {
                data.WriteUInt8(PlacementStatus);
            }
            if (changesMask[3])
            {
                data.WriteUInt8(SourceType);
            }
            data.WriteBit(DyeSlots.HasValue());
            if (changesMask[4])
            {
                data.WriteBits(SourceValue.Size() + 1, 24);
            }
            data.FlushBits();
            if (changesMask[2])
            {
                if (DyeSlots.HasValue())
                {
                    DyeSlots.GetValue().WriteUpdate(ignoreChangesMask, data, receiver, owner);
                }
            }
            if (changesMask[4])
            {
                data.WriteCString(SourceValue);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(HouseGUID);
            ClearChangesMask(PlacementStatus);
            ClearChangesMask(DyeSlots);
            ClearChangesMask(SourceType);
            ClearChangesMask(SourceValue);
            _changesMask.ResetAll();
        }
    }

    class DecorPetInfo() : HasChangesMask(6), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<ObjectGuid> BattlePetGUID = new(-1, 0);
        public UpdateField<ObjectGuid> SpawnGroup = new(-1, 1);
        public UpdateField<ObjectGuid> SpawnedPet = new(-1, 2);
        public UpdateField<uint> CreatureID = new(-1, 3);
        public UpdateFieldString PetName = new(-1, 4);
        public UpdateField<byte> PetBehavior = new(-1, 5);

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(BattlePetGUID);
            data.WritePackedGuid(SpawnGroup);
            data.WritePackedGuid(SpawnedPet);
            data.WriteUInt32(CreatureID);
            data.WriteUInt8(PetBehavior);
            data.WriteBits(PetName.Size(), 6);
            data.FlushBits();
            data.WriteString(PetName);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 6);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WritePackedGuid(BattlePetGUID);
            }
            if (changesMask[1])
            {
                data.WritePackedGuid(SpawnGroup);
            }
            if (changesMask[2])
            {
                data.WritePackedGuid(SpawnedPet);
            }
            if (changesMask[3])
            {
                data.WriteUInt32(CreatureID);
            }
            if (changesMask[5])
            {
                data.WriteUInt8(PetBehavior);
            }
            if (changesMask[4])
            {
                data.WriteBits(PetName.Size(), 6);
            }
            data.FlushBits();
            if (changesMask[4])
            {
                data.WriteString(PetName);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BattlePetGUID);
            ClearChangesMask(SpawnGroup);
            ClearChangesMask(SpawnedPet);
            ClearChangesMask(CreatureID);
            ClearChangesMask(PetName);
            ClearChangesMask(PetBehavior);
            _changesMask.ResetAll();
        }
    }

    class HousingDecorData() : HasChangesMask(7), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<ObjectGuid> DecorGUID = new(0, 1);
        public UpdateField<ObjectGuid> AttachParentGUID = new(0, 2);
        public UpdateField<byte> Flags = new(0, 3);
        public OptionalUpdateField<DecorStoragePersistedData> PersistedData = new(0, 4);
        public OptionalUpdateField<DecorPetInfo> PetInfo = new(0, 5);
        public UpdateField<ObjectGuid> TargetGameObjectGUID = new(0, 6);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WritePackedGuid(DecorGUID);
            data.WritePackedGuid(AttachParentGUID);
            data.WriteUInt8(Flags);
            data.WritePackedGuid(TargetGameObjectGUID);
            data.WriteBit(PersistedData.HasValue());
            data.WriteBit(PetInfo.HasValue());
            data.FlushBits();
            if (PersistedData.HasValue())
            {
                PersistedData.GetValue().WriteCreate(data, receiver, owner);
            }
            if (PetInfo.HasValue())
            {
                PetInfo.GetValue().WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, WorldObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 7);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(DecorGUID);
                }
                if (changesMask[2])
                {
                    data.WritePackedGuid(AttachParentGUID);
                }
                if (changesMask[3])
                {
                    data.WriteUInt8(Flags);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(TargetGameObjectGUID);
                }
            }
            if (changesMask[0])
            {
                data.WriteBit(PersistedData.HasValue());
                data.WriteBit(PetInfo.HasValue());
                data.FlushBits();
                if (changesMask[4])
                {
                    if (PersistedData.HasValue())
                    {
                        PersistedData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
                if (changesMask[5])
                {
                    if (PetInfo.HasValue())
                    {
                        PetInfo.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(DecorGUID);
            ClearChangesMask(AttachParentGUID);
            ClearChangesMask(Flags);
            ClearChangesMask(PersistedData);
            ClearChangesMask(PetInfo);
            ClearChangesMask(TargetGameObjectGUID);
            _changesMask.ResetAll();
        }
    }

    class HousingDoorData() : HasChangesMask(5), IsUpdateFieldStructure<BaseEntity>
    {
        public UpdateField<int> RoomComponentID = new(0, 1);
        public UpdateField<Position> RoomComponentOffset = new(0, 2);
        public UpdateField<byte> RoomComponentType = new(0, 3);
        public UpdateField<ObjectGuid> AttachedRoomGUID = new(0, 4);

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WriteInt32(RoomComponentID);
            data.WriteVector3(RoomComponentOffset.GetValue());
            data.WriteUInt8(RoomComponentType);
            data.WritePackedGuid(AttachedRoomGUID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 5);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(RoomComponentID);
                }
                if (changesMask[2])
                {
                    data.WriteVector3(RoomComponentOffset.GetValue());
                }
                if (changesMask[3])
                {
                    data.WriteUInt8(RoomComponentType);
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(AttachedRoomGUID);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(RoomComponentID);
            ClearChangesMask(RoomComponentOffset);
            ClearChangesMask(RoomComponentType);
            ClearChangesMask(AttachedRoomGUID);
            _changesMask.ResetAll();
        }
    }

    class HousingRoomData() : HasChangesMask(6), IsUpdateFieldStructure<BaseEntity>
    {
        public DynamicUpdateField<ObjectGuid> MeshObjects = new(0, 1);
        public DynamicUpdateField<HousingDoorData> Doors = new(0, 2);
        public UpdateField<ObjectGuid> HouseGUID = new(0, 3);
        public UpdateField<int> HouseRoomID = new(0, 4);
        public UpdateField<int> Flags = new(0, 5);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(HouseGUID);
            data.WriteInt32(HouseRoomID);
            data.WriteInt32(Flags);
            data.WriteInt32(MeshObjects.Size());
            data.WriteInt32(Doors.Size());
            for (int i = 0; i < MeshObjects.Size(); ++i)
            {
                data.WritePackedGuid(MeshObjects[i]);
            }
            for (int i = 0; i < Doors.Size(); ++i)
            {
                Doors[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, BaseEntity owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 6);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        MeshObjects.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MeshObjects.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        Doors.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Doors.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < MeshObjects.Size(); ++i)
                    {
                        if (MeshObjects.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(MeshObjects[i]);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < Doors.Size(); ++i)
                    {
                        if (Doors.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Doors[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[3])
                {
                    data.WritePackedGuid(HouseGUID);
                }
                if (changesMask[4])
                {
                    data.WriteInt32(HouseRoomID);
                }
                if (changesMask[5])
                {
                    data.WriteInt32(Flags);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(MeshObjects);
            ClearChangesMask(Doors);
            ClearChangesMask(HouseGUID);
            ClearChangesMask(HouseRoomID);
            ClearChangesMask(Flags);
            _changesMask.ResetAll();
        }
    }

    class HousingRoomComponentMeshData() : HasChangesMask(10), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<ObjectGuid> RoomGUID = new(0, 1);
        public UpdateField<int> RoomComponentOptionID = new(0, 2);
        public UpdateField<int> RoomComponentID = new(0, 3);
        public UpdateField<byte> Field_20 = new(0, 4);
        public UpdateField<byte> RoomComponentType = new(0, 5);
        public UpdateField<int> Field_24 = new(0, 6);
        public UpdateField<int> HouseThemeID = new(0, 7);
        public UpdateField<int> RoomComponentTextureID = new(0, 8);
        public UpdateField<int> RoomComponentTypeParam = new(0, 9);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WritePackedGuid(RoomGUID);
            data.WriteInt32(RoomComponentOptionID);
            data.WriteInt32(RoomComponentID);
            data.WriteUInt8(Field_20);
            data.WriteUInt8(RoomComponentType);
            data.WriteInt32(Field_24);
            data.WriteInt32(HouseThemeID);
            data.WriteInt32(RoomComponentTextureID);
            data.WriteInt32(RoomComponentTypeParam);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, WorldObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 10);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(RoomGUID);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(RoomComponentOptionID);
                }
                if (changesMask[3])
                {
                    data.WriteInt32(RoomComponentID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt8(Field_20);
                }
                if (changesMask[5])
                {
                    data.WriteUInt8(RoomComponentType);
                }
                if (changesMask[6])
                {
                    data.WriteInt32(Field_24);
                }
                if (changesMask[7])
                {
                    data.WriteInt32(HouseThemeID);
                }
                if (changesMask[8])
                {
                    data.WriteInt32(RoomComponentTextureID);
                }
                if (changesMask[9])
                {
                    data.WriteInt32(RoomComponentTypeParam);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(RoomGUID);
            ClearChangesMask(RoomComponentOptionID);
            ClearChangesMask(RoomComponentID);
            ClearChangesMask(Field_20);
            ClearChangesMask(RoomComponentType);
            ClearChangesMask(Field_24);
            ClearChangesMask(HouseThemeID);
            ClearChangesMask(RoomComponentTextureID);
            ClearChangesMask(RoomComponentTypeParam);
            _changesMask.ResetAll();
        }
    }

    class HousingPlayerHouseData() : HasChangesMask(11), IsUpdateFieldStructure<BaseEntity>
    {
        public UpdateField<ObjectGuid> BnetAccount = new(0, 1);
        public UpdateField<ObjectGuid> CosmeticOwner = new(0, 2);
        public UpdateField<int> PlotIndex = new(0, 3);
        public UpdateField<uint> Level = new(0, 4);
        public UpdateField<ulong> Favor = new(0, 5);
        public UpdateField<uint> InteriorDecorPlacementBudget = new(0, 6);
        public UpdateField<uint> ExteriorDecorPlacementBudget = new(0, 7);
        public UpdateField<uint> ExteriorFixtureBudget = new(0, 8);
        public UpdateField<uint> RoomPlacementBudget = new(0, 9);
        public UpdateField<ObjectGuid> EntityGUID = new(0, 10);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(BnetAccount);
            data.WritePackedGuid(CosmeticOwner);
            data.WriteInt32(PlotIndex);
            data.WriteUInt32(Level);
            data.WriteUInt64(Favor);
            data.WriteUInt32(InteriorDecorPlacementBudget);
            data.WriteUInt32(ExteriorDecorPlacementBudget);
            data.WriteUInt32(ExteriorFixtureBudget);
            data.WriteUInt32(RoomPlacementBudget);
            data.WritePackedGuid(EntityGUID);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, BaseEntity owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 11);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(BnetAccount);
                }
                if (changesMask[2])
                {
                    data.WritePackedGuid(CosmeticOwner);
                }
                if (changesMask[3])
                {
                    data.WriteInt32(PlotIndex);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[5])
                {
                    data.WriteUInt64(Favor);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(InteriorDecorPlacementBudget);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(ExteriorDecorPlacementBudget);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(ExteriorFixtureBudget);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(RoomPlacementBudget);
                }
                if (changesMask[10])
                {
                    data.WritePackedGuid(EntityGUID);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BnetAccount);
            ClearChangesMask(CosmeticOwner);
            ClearChangesMask(PlotIndex);
            ClearChangesMask(Level);
            ClearChangesMask(Favor);
            ClearChangesMask(InteriorDecorPlacementBudget);
            ClearChangesMask(ExteriorDecorPlacementBudget);
            ClearChangesMask(ExteriorFixtureBudget);
            ClearChangesMask(RoomPlacementBudget);
            ClearChangesMask(EntityGUID);
            _changesMask.ResetAll();
        }
    }

    class HousingCornerstoneData() : HasChangesMask(3), IsUpdateFieldStructure<GameObject>
    {
        public UpdateField<ulong> Cost = new(0, 1);
        public UpdateField<int> PlotIndex = new(0, 2);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, GameObject owner)
        {
            data.WriteUInt64(Cost);
            data.WriteInt32(PlotIndex);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, GameObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, GameObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt64(Cost);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(PlotIndex);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Cost);
            ClearChangesMask(PlotIndex);
            _changesMask.ResetAll();
        }
    }

    class PlayerHouseInfo : IEquatable<PlayerHouseInfo>, IsUpdateFieldStructure<BaseEntity>
    {
        public ObjectGuid HouseGUID;
        public ObjectGuid OwnerGUID;

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(HouseGUID);
            data.WritePackedGuid(OwnerGUID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner) => WriteCreate(data, receiver, owner);

        public bool Equals(PlayerHouseInfo right)
        {
            return HouseGUID == right.HouseGUID
                && OwnerGUID == right.OwnerGUID;
        }
    }

    class HousingOwner : IEquatable<HousingOwner>, IsUpdateFieldStructure<BaseEntity>
    {
        public ObjectGuid BnetAccountGUID;
        public ObjectGuid PlayerGUID;

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(BnetAccountGUID);
            data.WritePackedGuid(PlayerGUID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner) => WriteCreate(data, receiver, owner);

        public bool Equals(HousingOwner right)
        {
            return BnetAccountGUID == right.BnetAccountGUID
                && PlayerGUID == right.PlayerGUID;
        }
    }

    class NeighborhoodMirrorData() : HasChangesMask(5), IsUpdateFieldStructure<BaseEntity>
    {
        public DynamicUpdateField<PlayerHouseInfo> Houses = new(0, 1);
        public DynamicUpdateField<HousingOwner> Managers = new(0, 2);
        public UpdateFieldString Name = new(0, 3);
        public UpdateField<ObjectGuid> OwnerGUID = new(0, 4);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WriteBits(Name.Size() + 1, 8);
            data.WritePackedGuid(OwnerGUID);
            data.WriteInt32(Houses.Size());
            data.WriteInt32(Managers.Size());
            data.WriteCString(Name);
            for (int i = 0; i < Houses.Size(); ++i)
            {
                Houses[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < Managers.Size(); ++i)
            {
                Managers[i].WriteCreate(data, receiver, owner);
            }
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, BaseEntity owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 5);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        Houses.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Houses.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        Managers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Managers.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Houses.Size(); ++i)
                    {
                        if (Houses.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Houses[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < Managers.Size(); ++i)
                    {
                        if (Managers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Managers[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    data.WriteBits(Name.Size() + 1, 8);
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(OwnerGUID);
                }
                if (changesMask[3])
                {
                    data.WriteCString(Name);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Houses);
            ClearChangesMask(Managers);
            ClearChangesMask(Name);
            ClearChangesMask(OwnerGUID);
            _changesMask.ResetAll();
        }
    }

    class MirroredMeshObjectData() : HasChangesMask(6), IsUpdateFieldStructure<BaseEntity>
    {
        public UpdateField<ObjectGuid> AttachParentGUID = new(0, 1);
        public UpdateField<Position> PositionLocalSpace = new(0, 2);
        public UpdateField<Quaternion> RotationLocalSpace = new(0, 3);
        public UpdateField<float> ScaleLocalSpace = new(0, 4);
        public UpdateField<byte> AttachmentFlags = new(0, 5);

        public void WriteCreate(WorldPacket data, Player receiver, BaseEntity owner)
        {
            data.WritePackedGuid(AttachParentGUID);
            data.WriteVector3(PositionLocalSpace.GetValue());
            data.WriteFloat(RotationLocalSpace.GetValue().X);
            data.WriteFloat(RotationLocalSpace.GetValue().Y);
            data.WriteFloat(RotationLocalSpace.GetValue().Z);
            data.WriteFloat(RotationLocalSpace.GetValue().W);
            data.WriteFloat(ScaleLocalSpace);
            data.WriteUInt8(AttachmentFlags);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, BaseEntity owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 6);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(AttachParentGUID);
                }
                if (changesMask[2])
                {
                    data.WriteVector3(PositionLocalSpace.GetValue());
                }
                if (changesMask[3])
                {
                    data.WriteFloat(RotationLocalSpace.GetValue().X);
                    data.WriteFloat(RotationLocalSpace.GetValue().Y);
                    data.WriteFloat(RotationLocalSpace.GetValue().Z);
                    data.WriteFloat(RotationLocalSpace.GetValue().W);
                }
                if (changesMask[4])
                {
                    data.WriteFloat(ScaleLocalSpace);
                }
                if (changesMask[5])
                {
                    data.WriteUInt8(AttachmentFlags);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(AttachParentGUID);
            ClearChangesMask(PositionLocalSpace);
            ClearChangesMask(RotationLocalSpace);
            ClearChangesMask(ScaleLocalSpace);
            ClearChangesMask(AttachmentFlags);
            _changesMask.ResetAll();
        }
    }

    class MirroredPositionData() : HasChangesMask(2), IsUpdateFieldStructure<BaseEntity>
    {
        public UpdateField<MirroredMeshObjectData> PositionData = new(0, 1);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            PositionData.GetValue().WriteCreate(data, receiver, owner);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, BaseEntity owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 2);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    PositionData.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(PositionData);
            _changesMask.ResetAll();
        }
    }

    class PlayerMirrorHouse : IEquatable<PlayerMirrorHouse>, IsUpdateFieldStructure<Player>
    {
        public ObjectGuid HouseGUID;
        public ObjectGuid NeighborhoodGUID;
        public uint Level;
        public uint Favor;
        public uint InitiativeFavor;
        public int MapID;
        public int PlotID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(HouseGUID);
            data.WritePackedGuid(NeighborhoodGUID);
            data.WriteUInt32(Level);
            data.WriteUInt32(Favor);
            data.WriteUInt32(InitiativeFavor);
            data.WriteInt32(MapID);
            data.WriteInt32(PlotID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(PlayerMirrorHouse right)
        {
            return HouseGUID == right.HouseGUID
                && NeighborhoodGUID == right.NeighborhoodGUID
                && Level == right.Level
                && Favor == right.Favor
                && InitiativeFavor == right.InitiativeFavor
                && MapID == right.MapID
                && PlotID == right.PlotID;
        }
    }

    class NeighborhoodCharterSignature : IEquatable<NeighborhoodCharterSignature>, IsUpdateFieldStructure<Player>
    {
        public ObjectGuid Guid;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(Guid);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(NeighborhoodCharterSignature right)
        {
            return Guid == right.Guid;
        }
    }

    class NeighborhoodCharter : IEquatable<NeighborhoodCharter>, IsUpdateFieldStructure<Player>
    {
        public List<NeighborhoodCharterSignature> Signatures = [];
        public int Field_0;
        public int Field_4;
        public string Name;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(Field_4);
            data.WriteInt32(Signatures.Count);
            for (int i = 0; i < Signatures.Count; ++i)
            {
                Signatures[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBits(Name.GetByteCount() + 1, 8);
            data.FlushBits();
            data.WriteCString(Name);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(NeighborhoodCharter right)
        {
            return Signatures == right.Signatures
                && Field_0 == right.Field_0
                && Field_4 == right.Field_4
                && Name == right.Name;
        }
    }

    struct NeighborhoodOwnershipTransfer : IEquatable<NeighborhoodOwnershipTransfer>, IsUpdateFieldStructure<Player>
    {
        public ObjectGuid NeighborhoodGUID;
        public ObjectGuid CurrentOwnerGUID;
        public string NeighborhoodName;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(NeighborhoodGUID);
            data.WritePackedGuid(CurrentOwnerGUID);
            data.WriteBits(NeighborhoodName.GetByteCount() + 1, 8);
            data.FlushBits();
            data.WriteCString(NeighborhoodName);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(NeighborhoodOwnershipTransfer right)
        {
            return NeighborhoodGUID == right.NeighborhoodGUID
            && CurrentOwnerGUID == right.CurrentOwnerGUID
            && NeighborhoodName == right.NeighborhoodName;
        }
    }

    class PlayerHouseInfoComponentData() : HasChangesMask(11), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<ObjectGuid> Field_8 = new(0, 1);
        public DynamicUpdateField<PlayerMirrorHouse> Houses = new(0, 2);
        public DynamicUpdateField<ObjectGuid> Field_88 = new(0, 3);
        public DynamicUpdateField<ObjectGuid> Field_C0 = new(0, 4);
        public DynamicUpdateField<ObjectGuid> Field_F8 = new(0, 5);
        public DynamicUpdateField<ObjectGuid> Field_130 = new(0, 6);
        public UpdateField<NeighborhoodCharter> Charter = new(0, 7);
        public UpdateField<byte> EditorMode = new(0, 8);
        public UpdateField<NeighborhoodOwnershipTransfer> NeighborhoodOwnershipTransfer = new(0, 9);
        public UpdateField<ObjectGuid> CurrentHouse = new(0, 10);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteInt32(Field_8.Size());
            }
            data.WriteInt32(Houses.Size());
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteInt32(Field_88.Size());
                data.WriteInt32(Field_C0.Size());
                data.WriteInt32(Field_F8.Size());
                data.WriteInt32(Field_130.Size());
                Charter.GetValue().WriteCreate(data, receiver, owner);
                data.WriteUInt8(EditorMode);
                NeighborhoodOwnershipTransfer.GetValue().WriteCreate(data, receiver, owner);
                data.WritePackedGuid(CurrentHouse);
                for (int i = 0; i < Field_8.Size(); ++i)
                {
                    data.WritePackedGuid(Field_8[i]);
                }
            }
            for (int i = 0; i < Houses.Size(); ++i)
            {
                Houses[i].WriteCreate(data, receiver, owner);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                for (int i = 0; i < Field_88.Size(); ++i)
                {
                    data.WritePackedGuid(Field_88[i]);
                }
                for (int i = 0; i < Field_C0.Size(); ++i)
                {
                    data.WritePackedGuid(Field_C0[i]);
                }
                for (int i = 0; i < Field_F8.Size(); ++i)
                {
                    data.WritePackedGuid(Field_F8[i]);
                }
                for (int i = 0; i < Field_130.Size(); ++i)
                {
                    data.WritePackedGuid(Field_130[i]);
                }
            }
        }

        static void PlayerHouseInfoComponentDataAppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget |= new UpdateMask(1, [0x000007FAu]);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            PlayerHouseInfoComponentDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0x00000005u]);
            PlayerHouseInfoComponentDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask &= allowedMaskForTarget;
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0x00000005u]);
            PlayerHouseInfoComponentDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(_changesMask & allowedMaskForTarget, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Player owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 11);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        Field_8.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Field_8.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        Houses.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Houses.Size(), data);
                }
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        Field_88.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Field_88.Size(), data);
                }
                if (changesMask[4])
                {
                    if (!ignoreNestedChangesMask)
                        Field_C0.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Field_C0.Size(), data);
                }
                if (changesMask[5])
                {
                    if (!ignoreNestedChangesMask)
                        Field_F8.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Field_F8.Size(), data);
                }
                if (changesMask[6])
                {
                    if (!ignoreNestedChangesMask)
                        Field_130.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Field_130.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < Field_8.Size(); ++i)
                    {
                        if (Field_8.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(Field_8[i]);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < Houses.Size(); ++i)
                    {
                        if (Houses.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Houses[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < Field_88.Size(); ++i)
                    {
                        if (Field_88.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(Field_88[i]);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < Field_C0.Size(); ++i)
                    {
                        if (Field_C0.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(Field_C0[i]);
                        }
                    }
                }
                if (changesMask[5])
                {
                    for (int i = 0; i < Field_F8.Size(); ++i)
                    {
                        if (Field_F8.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(Field_F8[i]);
                        }
                    }
                }
                if (changesMask[6])
                {
                    for (int i = 0; i < Field_130.Size(); ++i)
                    {
                        if (Field_130.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(Field_130[i]);
                        }
                    }
                }
                if (changesMask[7])
                {
                    Charter.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[8])
                {
                    data.WriteUInt8(EditorMode);
                }
                if (changesMask[9])
                {
                    NeighborhoodOwnershipTransfer.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[10])
                {
                    data.WritePackedGuid(CurrentHouse);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Field_8);
            ClearChangesMask(Houses);
            ClearChangesMask(Field_88);
            ClearChangesMask(Field_C0);
            ClearChangesMask(Field_F8);
            ClearChangesMask(Field_130);
            ClearChangesMask(Charter);
            ClearChangesMask(EditorMode);
            ClearChangesMask(NeighborhoodOwnershipTransfer);
            ClearChangesMask(CurrentHouse);
            _changesMask.ResetAll();
        }
    }

    class HousingStorageData() : HasChangesMask((int)EntityFragment.FHousingStorage_C, TypeId.Object, 3), IsUpdateFieldStructure<BaseEntity>
    {
        public MapUpdateField<ObjectGuid, DecorStoragePersistedData> Decor = new(0, 1);
        public UpdateField<uint> DecorMaxOwnedCount = new(0, 2);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            WriteMapFieldCreate(Decor, data, receiver, owner);
            data.WriteUInt32(DecorMaxOwnedCount);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, BaseEntity owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, BaseEntity owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    WriteMapFieldUpdate(Decor, ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(DecorMaxOwnedCount);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Decor);
            ClearChangesMask(DecorMaxOwnedCount);
            _changesMask.ResetAll();
        }
    }

    class HousingFixtureData() : HasChangesMask(11), IsUpdateFieldStructure<WorldObject>
    {
        public UpdateField<int> ExteriorComponentID = new(0, 1);
        public UpdateField<int> HouseExteriorWmoDataID = new(0, 2);
        public UpdateField<int> ExteriorComponentHookID = new(0, 3);
        public UpdateField<ObjectGuid> HouseGUID = new(0, 4);
        public UpdateField<ObjectGuid> AttachParentGUID = new(0, 5);
        public UpdateField<ObjectGuid> Guid = new(0, 6);
        public UpdateField<ObjectGuid> GameObjectGUID = new(0, 7);
        public UpdateField<byte> ExteriorComponentType = new(0, 8);
        public UpdateField<byte> Field_59 = new(0, 9);
        public UpdateField<byte> Size = new(0, 10);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            data.WriteInt32(ExteriorComponentID);
            data.WriteInt32(HouseExteriorWmoDataID);
            data.WriteInt32(ExteriorComponentHookID);
            data.WritePackedGuid(HouseGUID);
            data.WritePackedGuid(AttachParentGUID);
            data.WritePackedGuid(Guid);
            data.WritePackedGuid(GameObjectGUID);
            data.WriteUInt8(ExteriorComponentType);
            data.WriteUInt8(Field_59);
            data.WriteUInt8(Size);
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, WorldObject owner)
        {
            WriteUpdate(_changesMask, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, WorldObject owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 11);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteInt32(ExteriorComponentID);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(HouseExteriorWmoDataID);
                }
                if (changesMask[3])
                {
                    data.WriteInt32(ExteriorComponentHookID);
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(HouseGUID);
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(AttachParentGUID);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(Guid);
                }
                if (changesMask[7])
                {
                    data.WritePackedGuid(GameObjectGUID);
                }
                if (changesMask[8])
                {
                    data.WriteUInt8(ExteriorComponentType);
                }
                if (changesMask[9])
                {
                    data.WriteUInt8(Field_59);
                }
                if (changesMask[10])
                {
                    data.WriteUInt8(Size);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ExteriorComponentID);
            ClearChangesMask(HouseExteriorWmoDataID);
            ClearChangesMask(ExteriorComponentHookID);
            ClearChangesMask(HouseGUID);
            ClearChangesMask(AttachParentGUID);
            ClearChangesMask(Guid);
            ClearChangesMask(GameObjectGUID);
            ClearChangesMask(ExteriorComponentType);
            ClearChangesMask(Field_59);
            ClearChangesMask(Size);
            _changesMask.ResetAll();
        }
    }

    class PlayerInitiativeInfo() : HasChangesMask(7), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> RemainingDuration = new(-1, 0);
        public UpdateField<int> CurrentInitiativeID = new(-1, 1);
        public UpdateField<int> CurrentMilestoneID = new(-1, 2);
        public UpdateField<int> CurrentCycleID = new(-1, 3);
        public UpdateField<float> ProgressRequired = new(-1, 4);
        public UpdateField<float> CurrentProgress = new(-1, 5);
        public UpdateField<float> PlayerTotalContribution = new(-1, 6);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt64(RemainingDuration);
            data.WriteInt32(CurrentInitiativeID);
            data.WriteInt32(CurrentMilestoneID);
            data.WriteInt32(CurrentCycleID);
            data.WriteFloat(ProgressRequired);
            data.WriteFloat(CurrentProgress);
            data.WriteFloat(PlayerTotalContribution);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 7);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteInt64(RemainingDuration);
            }
            if (changesMask[1])
            {
                data.WriteInt32(CurrentInitiativeID);
            }
            if (changesMask[2])
            {
                data.WriteInt32(CurrentMilestoneID);
            }
            if (changesMask[3])
            {
                data.WriteInt32(CurrentCycleID);
            }
            if (changesMask[4])
            {
                data.WriteFloat(ProgressRequired);
            }
            if (changesMask[5])
            {
                data.WriteFloat(CurrentProgress);
            }
            if (changesMask[6])
            {
                data.WriteFloat(PlayerTotalContribution);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(RemainingDuration);
            ClearChangesMask(CurrentInitiativeID);
            ClearChangesMask(CurrentMilestoneID);
            ClearChangesMask(CurrentCycleID);
            ClearChangesMask(ProgressRequired);
            ClearChangesMask(CurrentProgress);
            ClearChangesMask(PlayerTotalContribution);
            _changesMask.ResetAll();
        }
    }

    class PlayerInitiativeTaskInfo() : HasChangesMask(2), IsUpdateFieldStructure<Player>
    {
        public UpdateField<int> TaskID = new(-1, 0);
        public UpdateField<int> TimesCompleted = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt32(TaskID);
            data.WriteInt32(TimesCompleted);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 2);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteInt32(TaskID);
            }
            if (changesMask[1])
            {
                data.WriteInt32(TimesCompleted);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(TaskID);
            ClearChangesMask(TimesCompleted);
            _changesMask.ResetAll();
        }
    }

    struct NICompletedMilestoneEntry : IEquatable<NICompletedMilestoneEntry>, IsUpdateFieldStructure<Player>
    {
        public long AwardDate;
        public uint MilestoneID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteInt64(AwardDate);
            data.WriteUInt32(MilestoneID);
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(NICompletedMilestoneEntry right)
        {
            return AwardDate == right.AwardDate
                && MilestoneID == right.MilestoneID;
        }
    }

    class NICompletedInitiativesEntry : IEquatable<NICompletedInitiativesEntry>, IsUpdateFieldStructure<Player>
    {
        public bool Completed;
        public List<NICompletedMilestoneEntry> CompletedMilestones = [];
        public uint InitiativeID;

        public void WriteCreate(WorldPacket data, Player receiver, Player owner)
        {
            data.WriteUInt32(InitiativeID);
            data.WriteInt32(CompletedMilestones.Count);
            for (int i = 0; i < CompletedMilestones.Count; ++i)
            {
                CompletedMilestones[i].WriteCreate(data, receiver, owner);
            }
            data.WriteBit(Completed);
            data.FlushBits();
        }

        public void WriteUpdate(bool ignoreChangesMask, WorldPacket data, Player receiver, Player owner) => WriteCreate(data, receiver, owner);

        public bool Equals(NICompletedInitiativesEntry right)
        {
            return Completed == right.Completed
            && CompletedMilestones == right.CompletedMilestones
            && InitiativeID == right.InitiativeID;
        }
    }

    class PlayerInitiativeComponentData() : HasChangesMask(6), IsUpdateFieldStructure<Player>
    {
        public DynamicUpdateField<PlayerInitiativeTaskInfo> CompletedTasks = new(0, 1);
        public DynamicUpdateField<NICompletedInitiativesEntry> CompletedInitiatives = new(0, 2);
        public UpdateField<ObjectGuid> NeighborhoodGUID = new(0, 3);
        public UpdateField<PlayerInitiativeInfo> InitiativeInfo = new(0, 4);
        public SetUpdateField<ObjectGuid> Houses = new(0, 5);

        public void WriteCreate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            data.WritePackedGuid(NeighborhoodGUID);
            InitiativeInfo.GetValue().WriteCreate(data, receiver, owner);
            data.WriteInt32(CompletedTasks.Size());
            data.WriteInt32(CompletedInitiatives.Size());
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                WriteSetFieldCreate(Houses, data, receiver, owner);
            }
            for (int i = 0; i < CompletedTasks.Size(); ++i)
            {
                CompletedTasks[i].WriteCreate(data, receiver, owner);
            }
            for (int i = 0; i < CompletedInitiatives.Size(); ++i)
            {
                CompletedInitiatives[i].WriteCreate(data, receiver, owner);
            }
        }

        static void PlayerInitiativeComponentDataAppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget |= new UpdateMask(1, [0x00000020u]);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            PlayerInitiativeComponentDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(1, [0x0000001Fu]);
            PlayerInitiativeComponentDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask &= allowedMaskForTarget;
        }

        public void WriteUpdate(UpdateFieldFlag fieldVisibilityFlags, WorldPacket data, Player receiver, Player owner)
        {
            UpdateMask allowedMaskForTarget = new(1, [0x0000001Fu]);
            PlayerInitiativeComponentDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(_changesMask & allowedMaskForTarget, data, receiver, owner, false);
        }

        public void WriteUpdate(UpdateMask changesMask, WorldPacket data, Player receiver, Player owner, bool ignoreNestedChangesMask)
        {
            data.WriteBits(changesMask.GetBlock(0), 6);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        CompletedTasks.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CompletedTasks.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        CompletedInitiatives.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CompletedInitiatives.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < CompletedTasks.Size(); ++i)
                    {
                        if (CompletedTasks.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CompletedTasks[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < CompletedInitiatives.Size(); ++i)
                    {
                        if (CompletedInitiatives.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CompletedInitiatives[i].WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                        }
                    }
                }
                if (changesMask[3])
                {
                    data.WritePackedGuid(NeighborhoodGUID);
                }
                if (changesMask[4])
                {
                    InitiativeInfo.GetValue().WriteUpdate(ignoreNestedChangesMask, data, receiver, owner);
                }
                if (changesMask[5])
                {
                    WriteSetFieldUpdate(Houses, ignoreNestedChangesMask, data, receiver, owner);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(CompletedTasks);
            ClearChangesMask(CompletedInitiatives);
            ClearChangesMask(NeighborhoodGUID);
            ClearChangesMask(InitiativeInfo);
            ClearChangesMask(Houses);
            _changesMask.ResetAll();
        }
    }
}