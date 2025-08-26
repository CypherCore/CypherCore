// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
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
    public class ObjectFieldData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Object, 4)
    {
        public UpdateField<uint> EntryId = new(0, 1);
        public UpdateField<uint> DynamicFlags = new(0, 2);
        public UpdateField<float> Scale = new(0, 3);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            data.WriteUInt32(GetViewerDependentEntryId(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentDynamicFlags(this, owner, receiver));
            data.WriteFloat(Scale);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, WorldObject owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(GetViewerDependentEntryId(this, owner, receiver));
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(GetViewerDependentDynamicFlags(this, owner, receiver));
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

        uint GetViewerDependentEntryId(ObjectFieldData objectData, WorldObject obj, Player receiver)
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

        uint GetViewerDependentDynamicFlags(ObjectFieldData objectData, WorldObject obj, Player receiver)
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
                        if (data.spawnTrackingData != null && !data.spawnTrackingQuestObjectives.Empty())
                            if (receiver.GetSpawnTrackingStateByObjectives(data.spawnTrackingData.SpawnTrackingId, data.spawnTrackingQuestObjectives) != SpawnTrackingState.Active)
                                dynFlags &= ~(uint)GameObjectDynamicLowFlags.Activate;
                    }

                    dynamicFlags = dynFlags;
                }
            }

            return dynamicFlags;
        }
    }

    public class ItemEnchantment() : HasChangesMask(5)
    {
        public UpdateField<uint> ID = new(0, 1);
        public UpdateField<uint> Duration = new(0, 2);
        public UpdateField<short> Charges = new(0, 3);
        public UpdateField<ushort> Inactive = new(0, 4);

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(ID);
            data.WriteUInt32(Duration);
            data.WriteInt16(Charges);
            data.WriteUInt16(Inactive);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
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

    public class ItemMod : IEquatable<ItemMod>
    {
        public byte Type;
        public uint Value;

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt8(Type);
            data.WriteUInt32(Value);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
        {
            data.WriteUInt8(Type);
            data.WriteUInt32(Value);
        }

        public bool Equals(ItemMod right)
        {
            return Type == right.Type
                && Value == right.Value;
        }
    }

    public class ItemModList() : HasChangesMask(1)
    {
        public DynamicUpdateField<ItemMod> Values = new(-1, 0);

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteBits(Values.Size(), 6);
            for (int i = 0; i < Values.Size(); ++i)
            {
                Values[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 1);

            if (changesMask[0])
            {
                if (!ignoreChangesMask)
                    Values.WriteUpdateMask(data, 6);
                else
                    WriteCompleteDynamicFieldUpdateMask(Values.Size(), data, 6);
            }
            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < Values.Size(); ++i)
                {
                    if (Values.HasChanged(i) || ignoreChangesMask)
                    {
                        Values[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
                    }
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Values);
            _changesMask.ResetAll();
        }
    }

    public class ArtifactPower : IEquatable<ArtifactPower>
    {
        public ushort ArtifactPowerId;
        public byte PurchasedRank;
        public byte CurrentRankWithBonus;

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt16(ArtifactPowerId);
            data.WriteUInt8(PurchasedRank);
            data.WriteUInt8(CurrentRankWithBonus);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
        {
            data.WriteUInt16(ArtifactPowerId);
            data.WriteUInt8(PurchasedRank);
            data.WriteUInt8(CurrentRankWithBonus);
        }

        public bool Equals(ArtifactPower right)
        {
            return ArtifactPowerId == right.ArtifactPowerId
                && PurchasedRank == right.PurchasedRank
                && CurrentRankWithBonus == right.CurrentRankWithBonus;
        }
    }

    public class SocketedGem() : HasChangesMask(20)
    {
        public UpdateField<uint> ItemId = new(0, 1);
        public UpdateField<byte> Context = new(0, 2);
        public UpdateFieldArray<ushort> BonusListIDs = new(16, 3, 4);

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(ItemId);
            for (int i = 0; i < 16; ++i)
                data.WriteUInt16(BonusListIDs[i]);

            data.WriteUInt8(Context);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
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

    public class ItemData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Item, 41)
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

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
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
                Enchantment[i].WriteCreate(data, owner, receiver);
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
                ArtifactPowers[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < Gems.Size(); ++i)
            {
                Gems[i].WriteCreate(data, owner, receiver);
            }
            Modifiers.GetValue().WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(41, [0xF80A727Fu, 0x000001FFu]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x07F58D80u, 0x00000000u]));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(41, [0xF80A727Fu, 0x000001FFu]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Item owner, Player receiver)
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
                            ArtifactPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < Gems.Size(); ++i)
                    {
                        if (Gems.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Gems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                if (changesMask[17])
                {
                    Modifiers.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                        Enchantment[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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

    public class ContainerData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Container, 101)
    {
        public UpdateField<uint> NumSlots = new(0, 1);
        public UpdateFieldArray<ObjectGuid> Slots = new(98, 2, 3);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
        {
            for (int i = 0; i < 98; ++i)
            {
                data.WritePackedGuid(Slots[i]);
            }
            data.WriteUInt32(NumSlots);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Bag owner, Player receiver)
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

    public class AzeriteEmpoweredItemData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.AzeriteEmpoweredItem, 6)
    {
        public UpdateFieldArray<int> Selections = new(5, 0, 1);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            for (int i = 0; i < 5; ++i)
            {
                data.WriteInt32(Selections[i]);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AzeriteEmpoweredItem owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AzeriteEmpoweredItem owner, Player receiver)
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

    public class UnlockedAzeriteEssence : IEquatable<UnlockedAzeriteEssence>
    {
        public uint AzeriteEssenceID;
        public uint Rank;

        public void WriteCreate(WorldPacket data, AzeriteItem owner, Player receiver)
        {
            data.WriteUInt32(AzeriteEssenceID);
            data.WriteUInt32(Rank);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AzeriteItem owner, Player receiver)
        {
            data.WriteUInt32(AzeriteEssenceID);
            data.WriteUInt32(Rank);
        }

        public bool Equals(UnlockedAzeriteEssence right)
        {
            return AzeriteEssenceID == right.AzeriteEssenceID
                && Rank == right.Rank;
        }
    }

    public class SelectedAzeriteEssences() : HasChangesMask(8)
    {
        public UpdateField<bool> Enabled = new(0, 1);
        public UpdateField<uint> SpecializationID = new(0, 2);
        public UpdateFieldArray<uint> AzeriteEssenceID = new(4, 3, 4);

        public void WriteCreate(WorldPacket data, AzeriteItem owner, Player receiver)
        {
            for (int i = 0; i < 4; ++i)
            {
                data.WriteUInt32(AzeriteEssenceID[i]);
            }
            data.WriteUInt32(SpecializationID);
            data.WriteBit(Enabled);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AzeriteItem owner, Player receiver)
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

            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Enabled);
            ClearChangesMask(SpecializationID);
            ClearChangesMask(AzeriteEssenceID);
            _changesMask.ResetAll();
        }
    }

    public class AzeriteItemData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.AzeriteItem, 10)
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

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AzeriteItem owner, Player receiver)
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
                UnlockedEssences[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < UnlockedEssenceMilestones.Size(); ++i)
            {
                data.WriteUInt32(UnlockedEssenceMilestones[i]);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteBit(Enabled);
            }
            for (int i = 0; i < SelectedEssences.Size(); ++i)
            {
                SelectedEssences[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AzeriteItem owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(10, [0x0000001Du]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x000003E2u]));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(10, [0x0000001Du]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AzeriteItem owner, Player receiver)
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
                            UnlockedEssences[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                if (changesMask[3])
                {
                    for (int i = 0; i < SelectedEssences.Size(); ++i)
                    {
                        if (SelectedEssences.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SelectedEssences[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Enabled);
            ClearChangesMask(UnlockedEssences);
            ClearChangesMask(UnlockedEssenceMilestones);
            ClearChangesMask(SelectedEssences);
            ClearChangesMask(Xp);
            ClearChangesMask(Level);
            ClearChangesMask(AuraLevel);
            ClearChangesMask(KnowledgeLevel);
            ClearChangesMask(DEBUGknowledgeWeek);
            _changesMask.ResetAll();
        }
    }

    public class SpellCastVisualField : IEquatable<SpellCastVisual>
    {
        public uint SpellXSpellVisualID;
        public uint ScriptVisualID;

        public void WriteCreate(WorldPacket data, WorldObject owner, Player receiver)
        {
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteUInt32(ScriptVisualID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, WorldObject owner, Player receiver)
        {
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteUInt32(ScriptVisualID);
        }

        public bool Equals(SpellCastVisual right)
        {
            return SpellXSpellVisualID == right.SpellXSpellVisualID
                && ScriptVisualID == right.ScriptVisualID;
        }
    }

    public class UnitChannel : IEquatable<UnitChannel>
    {
        public uint SpellID;
        public SpellCastVisualField SpellVisual = new();
        public uint StartTimeMs;
        public uint Duration;

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            SpellVisual.WriteCreate(data, owner, receiver);
            data.WriteUInt32(StartTimeMs);
            data.WriteUInt32(Duration);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            SpellVisual.WriteUpdate(data, ignoreChangesMask, owner, receiver);
            data.WriteUInt32(StartTimeMs);
            data.WriteUInt32(Duration);
        }

        public bool Equals(UnitChannel right)
        {
            return SpellID == right.SpellID
                && SpellVisual == right.SpellVisual
                && StartTimeMs == right.StartTimeMs
                && Duration == right.Duration;
        }
    }

    public class VisibleItem() : HasChangesMask(6)
    {
        public UpdateField<uint> ItemID = new(0, 1);
        public UpdateField<uint> SecondaryItemModifiedAppearanceID = new(0, 2);
        public UpdateField<int> ConditionalItemAppearanceID = new(0, 3);
        public UpdateField<ushort> ItemAppearanceModID = new(0, 4);
        public UpdateField<ushort> ItemVisual = new(0, 5);

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(SecondaryItemModifiedAppearanceID);
            data.WriteInt32(ConditionalItemAppearanceID);
            data.WriteUInt16(ItemAppearanceModID);
            data.WriteUInt16(ItemVisual);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
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
                    data.WriteUInt32(ItemID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(SecondaryItemModifiedAppearanceID);
                }
                if (changesMask[3])
                {
                    data.WriteInt32(ConditionalItemAppearanceID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt16(ItemAppearanceModID);
                }
                if (changesMask[5])
                {
                    data.WriteUInt16(ItemVisual);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ItemID);
            ClearChangesMask(SecondaryItemModifiedAppearanceID);
            ClearChangesMask(ConditionalItemAppearanceID);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(ItemVisual);
            _changesMask.ResetAll();
        }
    }

    public class PassiveSpellHistory :  IEquatable<PassiveSpellHistory>
    {
        public int SpellID;
        public int AuraSpellID;

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(AuraSpellID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(AuraSpellID);
        }

        public bool Equals(PassiveSpellHistory right)
        {
            return SpellID == right.SpellID
                && AuraSpellID == right.AuraSpellID;
        }
    }

    public class UnitAssistActionData() : HasChangesMask(4)
    {
        public UpdateField<byte> Type = new(0, 1);
        public UpdateFieldString PlayerName = new(0, 2);
        public UpdateField<uint> VirtualRealmAddress = new(0, 3);

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt8(Type);
            data.WriteUInt32(VirtualRealmAddress);
            data.WriteBits(PlayerName.Size(), 6);
            data.FlushBits();
            data.WriteString(PlayerName);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
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

    public class UnitData() : HasChangesMask(223)
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
        public UpdateField<UnitChannel> ChannelData = new(0, 25);
        public UpdateField<sbyte> SpellEmpowerStage = new(0, 26);
        public UpdateField<uint> SummonedByHomeRealm = new(0, 27);
        public UpdateField<byte> Race = new(0, 28);
        public UpdateField<byte> ClassId = new(0, 29);
        public UpdateField<byte> PlayerClassId = new(0, 30);
        public UpdateField<byte> Sex = new(0, 31);
        public UpdateField<byte> DisplayPower = new(32, 33);
        public UpdateField<uint> OverrideDisplayPowerID = new(32, 34);
        public UpdateField<ulong> Health = new(32, 35);
        public UpdateField<ulong> MaxHealth = new(32, 36);
        public UpdateField<uint> Level = new(32, 37);
        public UpdateField<uint> EffectiveLevel = new(32, 38);
        public UpdateField<uint> ContentTuningID = new(32, 39);
        public UpdateField<int> ScalingLevelMin = new(32, 40);
        public UpdateField<int> ScalingLevelMax = new(32, 41);
        public UpdateField<int> ScalingLevelDelta = new(32, 42);
        public UpdateField<byte> ScalingFactionGroup = new(32, 43);
        public UpdateField<uint> FactionTemplate = new(32, 44);
        public UpdateField<uint> Flags = new(32, 45);
        public UpdateField<uint> Flags2 = new(32, 46);
        public UpdateField<uint> Flags3 = new(32, 47);
        public UpdateField<uint> Flags4 = new(32, 48);
        public UpdateField<uint> AuraState = new(32, 49);
        public UpdateField<uint> RangedAttackRoundBaseTime = new(32, 50);
        public UpdateField<float> BoundingRadius = new(32, 51);
        public UpdateField<float> CombatReach = new(32, 52);
        public UpdateField<float> DisplayScale = new(32, 53);
        public UpdateField<int> CreatureFamily = new(32, 54);
        public UpdateField<byte> CreatureType = new(32, 55);
        public UpdateField<uint> NativeDisplayID = new(32, 56);
        public UpdateField<float> NativeXDisplayScale = new(32, 57);
        public UpdateField<uint> MountDisplayID = new(32, 58);
        public UpdateField<uint> CosmeticMountDisplayID = new(32, 59);
        public UpdateField<float> MinDamage = new(32, 60);
        public UpdateField<float> MaxDamage = new(32, 61);
        public UpdateField<float> MinOffHandDamage = new(32, 62);
        public UpdateField<float> MaxOffHandDamage = new(32, 63);
        public UpdateField<byte> StandState = new(64, 65);
        public UpdateField<byte> PetTalentPoints = new(64, 66);
        public UpdateField<byte> VisFlags = new(64, 67);
        public UpdateField<byte> AnimTier = new(64, 68);
        public UpdateField<uint> PetNumber = new(64, 69);
        public UpdateField<uint> PetNameTimestamp = new(64, 70);
        public UpdateField<uint> PetExperience = new(64, 71);
        public UpdateField<uint> PetNextLevelExperience = new(64, 72);
        public UpdateField<float> ModCastingSpeed = new(64, 73);
        public UpdateField<float> ModCastingSpeedNeg = new(64, 74);
        public UpdateField<float> ModSpellHaste = new(64, 75);
        public UpdateField<float> ModHaste = new(64, 76);
        public UpdateField<float> ModRangedHaste = new(64, 77);
        public UpdateField<float> ModHasteRegen = new(64, 78);
        public UpdateField<float> ModTimeRate = new(64, 79);
        public UpdateField<uint> CreatedBySpell = new(64, 80);
        public UpdateField<int> EmoteState = new(64, 81);
        public UpdateField<uint> BaseMana = new(64, 82);
        public UpdateField<uint> BaseHealth = new(64, 83);
        public UpdateField<byte> SheatheState = new(64, 84);
        public UpdateField<byte> PvpFlags = new(64, 85);
        public UpdateField<byte> PetFlags = new(64, 86);
        public UpdateField<byte> ShapeshiftForm = new(64, 87);
        public UpdateField<int> AttackPower = new(64, 88);
        public UpdateField<int> AttackPowerModPos = new(64, 89);
        public UpdateField<int> AttackPowerModNeg = new(64, 90);
        public UpdateField<float> AttackPowerMultiplier = new(64, 91);
        public UpdateField<int> AttackPowerModSupport = new(64, 92);
        public UpdateField<int> RangedAttackPower = new(64, 93);
        public UpdateField<int> RangedAttackPowerModPos = new(64, 94);
        public UpdateField<int> RangedAttackPowerModNeg = new(64, 95);
        public UpdateField<float> RangedAttackPowerMultiplier = new(96, 97);
        public UpdateField<int> RangedAttackPowerModSupport = new(96, 98);
        public UpdateField<int> MainHandWeaponAttackPower = new(96, 99);
        public UpdateField<int> OffHandWeaponAttackPower = new(96, 100);
        public UpdateField<int> RangedWeaponAttackPower = new(96, 101);
        public UpdateField<int> SetAttackSpeedAura = new(96, 102);
        public UpdateField<float> Lifesteal = new(96, 103);
        public UpdateField<float> MinRangedDamage = new(96, 104);
        public UpdateField<float> MaxRangedDamage = new(96, 105);
        public UpdateField<float> ManaCostMultiplier = new(96, 106);
        public UpdateField<float> MaxHealthModifier = new(96, 107);
        public UpdateField<float> HoverHeight = new(96, 108);
        public UpdateField<uint> MinItemLevelCutoff = new(96, 109);
        public UpdateField<uint> MinItemLevel = new(96, 110);
        public UpdateField<uint> MaxItemLevel = new(96, 111);
        public UpdateField<uint> AzeriteItemLevel = new(96, 112);
        public UpdateField<uint> WildBattlePetLevel = new(96, 113);
        public UpdateField<uint> BattlePetCompanionExperience = new(96, 114);
        public UpdateField<uint> BattlePetCompanionNameTimestamp = new(96, 115);
        public UpdateField<int> InteractSpellID = new(96, 116);
        public UpdateField<int> ScaleDuration = new(96, 117);
        public UpdateField<int> LooksLikeMountID = new(96, 118);
        public UpdateField<int> LooksLikeCreatureID = new(96, 119);
        public UpdateField<int> LookAtControllerID = new(96, 120);
        public UpdateField<int> PerksVendorItemID = new(96, 121);
        public UpdateField<int> TaxiNodesID = new(96, 122);
        public UpdateField<ObjectGuid> GuildGUID = new(96, 123);
        public UpdateField<int> FlightCapabilityID = new(96, 124);
        public UpdateField<float> GlideEventSpeedDivisor = new(96, 125);                         // Movement speed gets divided by this value when evaluating what GlideEvents to use
        public UpdateField<int> DriveCapabilityID = new(96, 126);
        public UpdateField<int> MaxHealthModifierFlatNeg = new(96, 127);
        public UpdateField<int> MaxHealthModifierFlatPos = new(128, 129);
        public UpdateField<uint> SilencedSchoolMask = new(128, 130);
        public UpdateField<uint> CurrentAreaID = new(128, 131);
        public UpdateField<float> Field_31C = new(128, 132);
        public UpdateField<float> Field_320 = new(128, 133);                                     // Soft targeting related? When UnitFlags3 & 0x40000000 is set, increases some range check using CombatReach by this amount
        public UpdateField<ObjectGuid> NameplateAttachToGUID = new(128, 134);                    // When set, nameplate of this unit will instead appear on that object
        public OptionalUpdateField<UnitAssistActionData> AssistActionData = new(128, 135);
        public UpdateFieldArray<int> Power = new(10, 136, 137);
        public UpdateFieldArray<uint> MaxPower = new(10, 136, 147);
        public UpdateFieldArray<float> PowerRegenFlatModifier = new(10, 136, 157);
        public UpdateFieldArray<float> PowerRegenInterruptedFlatModifier = new(10, 136, 167);
        public UpdateFieldArray<VisibleItem> VirtualItems = new(3, 177, 178);
        public UpdateFieldArray<uint> AttackRoundBaseTime = new(2, 181, 182);
        public UpdateFieldArray<int> Stats = new(4, 184, 185);
        public UpdateFieldArray<int> StatPosBuff = new(4, 184, 189);
        public UpdateFieldArray<int> StatNegBuff = new(4, 184, 193);
        public UpdateFieldArray<int> StatSupportBuff = new(4, 184, 197);
        public UpdateFieldArray<int> Resistances = new(7, 201, 202);
        public UpdateFieldArray<int> BonusResistanceMods = new(7, 201, 209);
        public UpdateFieldArray<int> ManaCostModifier = new(7, 201, 216);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            data.WriteUInt32(GetViewerDependentDisplayId(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentNpcFlags(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentNpcFlags2(this, owner, receiver));

            data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentStateAnimID(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentStateAnimKitID(this, owner, receiver));

            var stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, owner, receiver);
            data.WriteInt32(stateWorldEffectIDs.Count);
            data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, owner, receiver));
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
            ChannelData.GetValue().WriteCreate(data, owner, receiver);
            data.WriteInt8(SpellEmpowerStage);
            data.WriteUInt32(SummonedByHomeRealm);
            data.WriteUInt8(Race);
            data.WriteUInt8(ClassId);
            data.WriteUInt8(PlayerClassId);
            data.WriteUInt8(Sex);
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
            data.WriteUInt32(GetViewerDependentFactionTemplate(this, owner, receiver));
            for (int i = 0; i < 3; ++i)
                VirtualItems[i].WriteCreate(data, owner, receiver);

            data.WriteUInt32(GetViewerDependentFlags(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentFlags2(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentFlags3(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentFlags4(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentAuraState(this, owner, receiver));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(AttackRoundBaseTime[i]);

            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WriteUInt32(RangedAttackRoundBaseTime);

            data.WriteFloat(BoundingRadius);
            data.WriteFloat(CombatReach);
            data.WriteFloat(DisplayScale);
            data.WriteInt32(CreatureFamily);
            data.WriteUInt8(CreatureType);
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
                for (int i = 0; i < 4; ++i)
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
            data.WriteUInt8(GetViewerDependentPvpFlags(this, owner, receiver));
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
            data.WriteInt32(GetViewerDependentInteractSpellId(this, owner, receiver));
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
            data.WriteFloat(Field_31C);
            data.WriteFloat(Field_320);
            data.WritePackedGuid(NameplateAttachToGUID);

            for (int i = 0; i < PassiveSpells.Size(); ++i)
                PassiveSpells[i].WriteCreate(data, owner, receiver);

            for (int i = 0; i < WorldEffects.Size(); ++i)
                data.WriteInt32(WorldEffects[i]);

            for (int i = 0; i < ChannelObjects.Size(); ++i)
                data.WritePackedGuid(ChannelObjects[i]);

            data.FlushBits();
            data.WriteBit(Field_314);
            data.WriteBits(AssistActionData.HasValue(), 1);
            if (AssistActionData.HasValue())
            {
                AssistActionData.GetValue().WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void UnitDataAppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00010000u, 0xF0040000u, 0xFF080000u, 0x000007FEu, 0xE0000100u, 0xFF01FFFFu, 0x7FFFFFFFu]));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0xE0000100u, 0x0001FFFFu, 0x00000000u]));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00000000u, 0xF0000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x0001FE00u]));
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            UnitDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xFFFEFFFFu, 0x0FFBFFFFu, 0x00F7FFFFu, 0xFFFFF801u, 0x1FFFFFFFu, 0x00FE0000u, 0x00000000u]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(ChangeMaskLength, [0xFFFEFFFFu, 0x0FFBFFFFu, 0x00F7FFFFu, 0xFFFFF801u, 0x1FFFFFFFu, 0x00FE0000u, 0x00000000u]);
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Unit owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 7);
            for (uint i = 0; i < 7; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            List<uint> stateWorldEffectIDs = new();

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(Field_314);
                }
                if (changesMask[2])
                {
                    stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, owner, receiver);
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
                            PassiveSpells[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                    data.WriteUInt32(GetViewerDependentDisplayId(this, owner, receiver));
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(GetViewerDependentNpcFlags(this, owner, receiver));
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(GetViewerDependentNpcFlags2(this, owner, receiver));
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, owner, receiver));
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(GetViewerDependentStateAnimID(this, owner, receiver));
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(GetViewerDependentStateAnimKitID(this, owner, receiver));
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, owner, receiver));
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
                    ChannelData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[26])
                {
                    data.WriteInt8(SpellEmpowerStage);
                }
                if (changesMask[27])
                {
                    data.WriteUInt32(SummonedByHomeRealm);
                }
                if (changesMask[28])
                {
                    data.WriteUInt8(Race);
                }
                if (changesMask[29])
                {
                    data.WriteUInt8(ClassId);
                }
                if (changesMask[30])
                {
                    data.WriteUInt8(PlayerClassId);
                }
                if (changesMask[31])
                {
                    data.WriteUInt8(Sex);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt8(DisplayPower);
                }
                if (changesMask[34])
                {
                    data.WriteUInt32(OverrideDisplayPowerID);
                }
                if (changesMask[35])
                {
                    data.WriteUInt64(Health);
                }
                if (changesMask[36])
                {
                    data.WriteUInt64(MaxHealth);
                }
                if (changesMask[37])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[38])
                {
                    data.WriteUInt32(EffectiveLevel);
                }
                if (changesMask[39])
                {
                    data.WriteUInt32(ContentTuningID);
                }
                if (changesMask[40])
                {
                    data.WriteInt32(ScalingLevelMin);
                }
                if (changesMask[41])
                {
                    data.WriteInt32(ScalingLevelMax);
                }
                if (changesMask[42])
                {
                    data.WriteInt32(ScalingLevelDelta);
                }
                if (changesMask[43])
                {
                    data.WriteUInt8(ScalingFactionGroup);
                }
                if (changesMask[44])
                {
                    data.WriteUInt32(GetViewerDependentFactionTemplate(this, owner, receiver));
                }
                if (changesMask[45])
                {
                    data.WriteUInt32(GetViewerDependentFlags(this, owner, receiver));
                }
                if (changesMask[46])
                {
                    data.WriteUInt32(GetViewerDependentFlags2(this, owner, receiver));
                }
                if (changesMask[47])
                {
                    data.WriteUInt32(GetViewerDependentFlags3(this, owner, receiver));
                }
                if (changesMask[48])
                {
                    data.WriteUInt32(GetViewerDependentFlags4(this, owner, receiver));
                }
                if (changesMask[49])
                {
                    data.WriteUInt32(GetViewerDependentAuraState(this, owner, receiver));
                }
                if (changesMask[50])
                {
                    data.WriteUInt32(RangedAttackRoundBaseTime);
                }
                if (changesMask[51])
                {
                    data.WriteFloat(BoundingRadius);
                }
                if (changesMask[52])
                {
                    data.WriteFloat(CombatReach);
                }
                if (changesMask[53])
                {
                    data.WriteFloat(DisplayScale);
                }
                if (changesMask[54])
                {
                    data.WriteInt32(CreatureFamily);
                }
                if (changesMask[55])
                {
                    data.WriteUInt8(CreatureType);
                }
                if (changesMask[56])
                {
                    data.WriteUInt32(NativeDisplayID);
                }
                if (changesMask[57])
                {
                    data.WriteFloat(NativeXDisplayScale);
                }
                if (changesMask[58])
                {
                    data.WriteUInt32(MountDisplayID);
                }
                if (changesMask[59])
                {
                    data.WriteUInt32(CosmeticMountDisplayID);
                }
                if (changesMask[60])
                {
                    data.WriteFloat(MinDamage);
                }
                if (changesMask[61])
                {
                    data.WriteFloat(MaxDamage);
                }
                if (changesMask[62])
                {
                    data.WriteFloat(MinOffHandDamage);
                }
                if (changesMask[63])
                {
                    data.WriteFloat(MaxOffHandDamage);
                }
            }
            if (changesMask[64])
            {
                if (changesMask[65])
                {
                    data.WriteUInt8(StandState);
                }
                if (changesMask[66])
                {
                    data.WriteUInt8(PetTalentPoints);
                }
                if (changesMask[67])
                {
                    data.WriteUInt8(VisFlags);
                }
                if (changesMask[68])
                {
                    data.WriteUInt8(AnimTier);
                }
                if (changesMask[69])
                {
                    data.WriteUInt32(PetNumber);
                }
                if (changesMask[70])
                {
                    data.WriteUInt32(PetNameTimestamp);
                }
                if (changesMask[71])
                {
                    data.WriteUInt32(PetExperience);
                }
                if (changesMask[72])
                {
                    data.WriteUInt32(PetNextLevelExperience);
                }
                if (changesMask[73])
                {
                    data.WriteFloat(ModCastingSpeed);
                }
                if (changesMask[74])
                {
                    data.WriteFloat(ModCastingSpeedNeg);
                }
                if (changesMask[75])
                {
                    data.WriteFloat(ModSpellHaste);
                }
                if (changesMask[76])
                {
                    data.WriteFloat(ModHaste);
                }
                if (changesMask[77])
                {
                    data.WriteFloat(ModRangedHaste);
                }
                if (changesMask[78])
                {
                    data.WriteFloat(ModHasteRegen);
                }
                if (changesMask[79])
                {
                    data.WriteFloat(ModTimeRate);
                }
                if (changesMask[80])
                {
                    data.WriteUInt32(CreatedBySpell);
                }
                if (changesMask[81])
                {
                    data.WriteInt32(EmoteState);
                }
                if (changesMask[82])
                {
                    data.WriteUInt32(BaseMana);
                }
                if (changesMask[83])
                {
                    data.WriteUInt32(BaseHealth);
                }
                if (changesMask[84])
                {
                    data.WriteUInt8(SheatheState);
                }
                if (changesMask[85])
                {
                    data.WriteUInt8(GetViewerDependentPvpFlags(this, owner, receiver));
                }
                if (changesMask[86])
                {
                    data.WriteUInt8(PetFlags);
                }
                if (changesMask[87])
                {
                    data.WriteUInt8(ShapeshiftForm);
                }
                if (changesMask[88])
                {
                    data.WriteInt32(AttackPower);
                }
                if (changesMask[89])
                {
                    data.WriteInt32(AttackPowerModPos);
                }
                if (changesMask[90])
                {
                    data.WriteInt32(AttackPowerModNeg);
                }
                if (changesMask[91])
                {
                    data.WriteFloat(AttackPowerMultiplier);
                }
                if (changesMask[92])
                {
                    data.WriteInt32(AttackPowerModSupport);
                }
                if (changesMask[93])
                {
                    data.WriteInt32(RangedAttackPower);
                }
                if (changesMask[94])
                {
                    data.WriteInt32(RangedAttackPowerModPos);
                }
                if (changesMask[95])
                {
                    data.WriteInt32(RangedAttackPowerModNeg);
                }
            }
            if (changesMask[96])
            {
                if (changesMask[97])
                {
                    data.WriteFloat(RangedAttackPowerMultiplier);
                }
                if (changesMask[98])
                {
                    data.WriteInt32(RangedAttackPowerModSupport);
                }
                if (changesMask[99])
                {
                    data.WriteInt32(MainHandWeaponAttackPower);
                }
                if (changesMask[100])
                {
                    data.WriteInt32(OffHandWeaponAttackPower);
                }
                if (changesMask[101])
                {
                    data.WriteInt32(RangedWeaponAttackPower);
                }
                if (changesMask[102])
                {
                    data.WriteInt32(SetAttackSpeedAura);
                }
                if (changesMask[103])
                {
                    data.WriteFloat(Lifesteal);
                }
                if (changesMask[104])
                {
                    data.WriteFloat(MinRangedDamage);
                }
                if (changesMask[105])
                {
                    data.WriteFloat(MaxRangedDamage);
                }
                if (changesMask[106])
                {
                    data.WriteFloat(ManaCostMultiplier);
                }
                if (changesMask[107])
                {
                    data.WriteFloat(MaxHealthModifier);
                }
                if (changesMask[108])
                {
                    data.WriteFloat(HoverHeight);
                }
                if (changesMask[109])
                {
                    data.WriteUInt32(MinItemLevelCutoff);
                }
                if (changesMask[110])
                {
                    data.WriteUInt32(MinItemLevel);
                }
                if (changesMask[111])
                {
                    data.WriteUInt32(MaxItemLevel);
                }
                if (changesMask[112])
                {
                    data.WriteUInt32(AzeriteItemLevel);
                }
                if (changesMask[113])
                {
                    data.WriteUInt32(WildBattlePetLevel);
                }
                if (changesMask[114])
                {
                    data.WriteUInt32(BattlePetCompanionExperience);
                }
                if (changesMask[115])
                {
                    data.WriteUInt32(BattlePetCompanionNameTimestamp);
                }
                if (changesMask[116])
                {
                    data.WriteInt32(GetViewerDependentInteractSpellId(this, owner, receiver));
                }
                if (changesMask[117])
                {
                    data.WriteInt32(ScaleDuration);
                }
                if (changesMask[118])
                {
                    data.WriteInt32(LooksLikeMountID);
                }
                if (changesMask[119])
                {
                    data.WriteInt32(LooksLikeCreatureID);
                }
                if (changesMask[120])
                {
                    data.WriteInt32(LookAtControllerID);
                }
                if (changesMask[121])
                {
                    data.WriteInt32(PerksVendorItemID);
                }
                if (changesMask[122])
                {
                    data.WriteInt32(TaxiNodesID);
                }
                if (changesMask[123])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[124])
                {
                    data.WriteInt32(FlightCapabilityID);
                }
                if (changesMask[125])
                {
                    data.WriteFloat(GlideEventSpeedDivisor);
                }
                if (changesMask[126])
                {
                    data.WriteInt32(DriveCapabilityID);
                }
                if (changesMask[127])
                {
                    data.WriteInt32(MaxHealthModifierFlatNeg);
                }
            }
            if (changesMask[128])
            {
                if (changesMask[129])
                {
                    data.WriteInt32(MaxHealthModifierFlatPos);
                }
                if (changesMask[130])
                {
                    data.WriteUInt32(SilencedSchoolMask);
                }
                if (changesMask[131])
                {
                    data.WriteUInt32(CurrentAreaID);
                }
                if (changesMask[132])
                {
                    data.WriteFloat(Field_31C);
                }
                if (changesMask[133])
                {
                    data.WriteFloat(Field_320);
                }
                if (changesMask[134])
                {
                    data.WritePackedGuid(NameplateAttachToGUID);
                }
                data.WriteBits(AssistActionData.HasValue(), 1);
                data.FlushBits();
                if (changesMask[135])
                {
                    if (AssistActionData.HasValue())
                    {
                        AssistActionData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[136])
            {
                for (int i = 0; i < 10; ++i)
                {
                    if (changesMask[137 + i])
                    {
                        data.WriteInt32(Power[i]);
                    }
                    if (changesMask[147 + i])
                    {
                        data.WriteUInt32(MaxPower[i]);
                    }
                    if (changesMask[157 + i])
                    {
                        data.WriteFloat(PowerRegenFlatModifier[i]);
                    }
                    if (changesMask[167 + i])
                    {
                        data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
                    }
                }
            }
            if (changesMask[177])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[178 + i])
                    {
                        VirtualItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[181])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[182 + i])
                    {
                        data.WriteUInt32(AttackRoundBaseTime[i]);
                    }
                }
            }
            if (changesMask[184])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[185 + i])
                    {
                        data.WriteInt32(Stats[i]);
                    }
                    if (changesMask[189 + i])
                    {
                        data.WriteInt32(StatPosBuff[i]);
                    }
                    if (changesMask[193 + i])
                    {
                        data.WriteInt32(StatNegBuff[i]);
                    }
                    if (changesMask[197 + i])
                    {
                        data.WriteInt32(StatSupportBuff[i]);
                    }
                }
            }
            if (changesMask[201])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[202 + i])
                    {
                        data.WriteInt32(Resistances[i]);
                    }
                    if (changesMask[209 + i])
                    {
                        data.WriteInt32(BonusResistanceMods[i]);
                    }
                    if (changesMask[216 + i])
                    {
                        data.WriteInt32(ManaCostModifier[i]);
                    }
                }
            }
            data.FlushBits();
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
            ClearChangesMask(ChannelData);
            ClearChangesMask(SpellEmpowerStage);
            ClearChangesMask(SummonedByHomeRealm);
            ClearChangesMask(Race);
            ClearChangesMask(ClassId);
            ClearChangesMask(PlayerClassId);
            ClearChangesMask(Sex);
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
            ClearChangesMask(CreatureType);
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
            ClearChangesMask(Field_31C);
            ClearChangesMask(Field_320);
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

        uint GetViewerDependentDisplayId(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentNpcFlags(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentNpcFlags2(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentFactionTemplate(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentFlags(UnitData unitData, Unit unit, Player receiver)
        {
            uint flags = unitData.Flags;
            // Update fields of triggers, transformed units or uninteractible units (values dependent on GM state)
            if (receiver.IsGameMaster())
                flags &= ~(uint)UnitFlags.Uninteractible;

            return flags;
        }

        uint GetViewerDependentFlags2(UnitData unitData, Unit unit, Player receiver)
        {
            uint flags = unitData.Flags2;
            // Gamemasters should be always able to interact with units - remove uninteractible flag
            if (receiver.IsGameMaster())
                flags &= ~(uint)UnitFlags2.UntargetableByClient;

            return flags;
        }

        uint GetViewerDependentFlags3(UnitData unitData, Unit unit, Player receiver)
        {
            uint flags = unitData.Flags3;
            if ((flags & (uint)UnitFlags3.AlreadySkinned) != 0 && unit.IsCreature() && !unit.ToCreature().IsSkinnedBy(receiver))
                flags &= ~(uint)UnitFlags3.AlreadySkinned;

            return flags;
        }

        uint GetViewerDependentFlags4(UnitData unitData, Unit unit, Player receiver)
        {
            return unitData.Flags4;
        }

        uint GetViewerDependentAuraState(UnitData unitData, Unit unit, Player receiver)
        {
            // Check per caster aura states to not enable using a spell in client if specified aura is not by target
            return unit.BuildAuraStateUpdateForTarget(receiver);
        }

        byte GetViewerDependentPvpFlags(UnitData unitData, Unit unit, Player receiver)
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

        int GetViewerDependentInteractSpellId(UnitData unitData, Unit unit, Player receiver)
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

        List<uint> GetViewerDependentStateWorldEffectIDs(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentStateSpellVisualID(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentStateAnimID(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentStateAnimKitID(UnitData unitData, Unit unit, Player receiver)
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

        uint GetViewerDependentStateWorldEffectsQuestObjectiveID(UnitData unitData, Unit unit, Player receiver)
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

    public class ChrCustomizationChoice : IEquatable<ChrCustomizationChoice>
    {
        public uint ChrCustomizationOptionID;
        public uint ChrCustomizationChoiceID;

        public void WriteCreate(WorldPacket data, WorldObject owner, Player receiver)
        {
            data.WriteUInt32(ChrCustomizationOptionID);
            data.WriteUInt32(ChrCustomizationChoiceID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, WorldObject owner, Player receiver)
        {
            data.WriteUInt32(ChrCustomizationOptionID);
            data.WriteUInt32(ChrCustomizationChoiceID);
        }

        public bool Equals(ChrCustomizationChoice right)
        {
            return ChrCustomizationOptionID == right.ChrCustomizationOptionID
                && ChrCustomizationChoiceID == right.ChrCustomizationChoiceID;
        }
    }

    public class QuestLog() : HasChangesMask(30)
    {
        public UpdateField<long> EndTime = new(0, 1);
        public UpdateField<uint> QuestID = new(0, 2);
        public UpdateField<uint> StateFlags = new(0, 3);
        public UpdateField<uint> ObjectiveFlags = new(0, 4);
        public UpdateFieldArray<ushort> ObjectiveProgress = new(24, 5, 6);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt64(EndTime);
            data.WriteUInt32(QuestID);
            data.WriteUInt32(StateFlags);
            data.WriteUInt32(ObjectiveFlags);
            for (int i = 0; i < 24; ++i)
            {
                data.WriteUInt16(ObjectiveProgress[i]);
            }
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    data.WriteInt64(EndTime);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(QuestID);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(StateFlags);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(ObjectiveFlags);
                }
            }
            if (changesMask[5])
            {
                for (int i = 0; i < 24; ++i)
                {
                    if (changesMask[6 + i])
                    {
                        data.WriteUInt16(ObjectiveProgress[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(EndTime);
            ClearChangesMask(QuestID);
            ClearChangesMask(StateFlags);
            ClearChangesMask(ObjectiveFlags);
            ClearChangesMask(ObjectiveProgress);
            _changesMask.ResetAll();
        }
    }

    public class ArenaCooldown() : HasChangesMask(8)
    {
        public UpdateField<int> SpellID = new(0, 1);
        public UpdateField<int> Charges = new(0, 2);
        public UpdateField<uint> Flags = new(0, 3);
        public UpdateField<uint> StartTime = new(0, 4);
        public UpdateField<uint> EndTime = new(0, 5);
        public UpdateField<uint> NextChargeTime = new(0, 6);
        public UpdateField<byte> MaxCharges = new(0, 7);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(Charges);
            data.WriteUInt32(Flags);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(EndTime);
            data.WriteUInt32(NextChargeTime);
            data.WriteUInt8(MaxCharges);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class ZonePlayerForcedReaction() : HasChangesMask(3)
    {
        public UpdateField<int> FactionID = new(0, 1);
        public UpdateField<int> Reaction = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(FactionID);
            data.WriteInt32(Reaction);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class PetCreatureName() : HasChangesMask(3)
    {
        public UpdateField<uint> CreatureID = new(0, 1);
        public UpdateFieldString Name = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(CreatureID);
            data.WriteBits(Name.Size(), 8);
            data.WriteString(Name);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    data.WriteString(Name);
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(CreatureID);
            ClearChangesMask(Name);
            _changesMask.ResetAll();
        }
    }

    public class CTROptions : IEquatable<CTROptions>
    {
        public uint ConditionalFlags;
        public byte FactionGroup;
        public uint ChromieTimeExpansionMask;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(ConditionalFlags);
            data.WriteUInt8(FactionGroup);
            data.WriteUInt32(ChromieTimeExpansionMask);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteUInt32(ConditionalFlags);
            data.WriteUInt8(FactionGroup);
            data.WriteUInt32(ChromieTimeExpansionMask);
        }

        public bool Equals(CTROptions right)
        {
            return ConditionalFlags == right.ConditionalFlags
                && FactionGroup == right.FactionGroup
                && ChromieTimeExpansionMask == right.ChromieTimeExpansionMask;
        }
    }

    public struct LeaverInfo : IEquatable<LeaverInfo>
    {
        public ObjectGuid BnetAccountGUID;
        public float LeaveScore;
        public uint SeasonID;
        public uint TotalLeaves;
        public uint TotalSuccesses;
        public int ConsecutiveSuccesses;
        public long LastPenaltyTime;
        public long LeaverExpirationTime;
        public int Unknown_1120;
        public uint LeaverStatus;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WritePackedGuid(BnetAccountGUID);
            data.WriteFloat(LeaveScore);
            data.WriteUInt32(SeasonID);
            data.WriteUInt32(TotalLeaves);
            data.WriteUInt32(TotalSuccesses);
            data.WriteInt32(ConsecutiveSuccesses);
            data.WriteInt64(LastPenaltyTime);
            data.WriteInt64(LeaverExpirationTime);
            data.WriteInt32(Unknown_1120);
            data.WriteBits(LeaverStatus, 1);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WritePackedGuid(BnetAccountGUID);
            data.WriteFloat(LeaveScore);
            data.WriteUInt32(SeasonID);
            data.WriteUInt32(TotalLeaves);
            data.WriteUInt32(TotalSuccesses);
            data.WriteInt32(ConsecutiveSuccesses);
            data.WriteInt64(LastPenaltyTime);
            data.WriteInt64(LeaverExpirationTime);
            data.WriteInt32(Unknown_1120);
            data.WriteBits(LeaverStatus, 1);
            data.FlushBits();
        }

        public bool Equals(LeaverInfo right)
        {
            return BnetAccountGUID == right.BnetAccountGUID
                && LeaveScore == right.LeaveScore
                && SeasonID == right.SeasonID
                && TotalLeaves == right.TotalLeaves
                && TotalSuccesses == right.TotalSuccesses
                && ConsecutiveSuccesses == right.ConsecutiveSuccesses
                && LastPenaltyTime == right.LastPenaltyTime
                && LeaverExpirationTime == right.LeaverExpirationTime
                && Unknown_1120 == right.Unknown_1120
                && LeaverStatus == right.LeaverStatus;
        }
    }

    public class DeclinedNames() : HasChangesMask(6)
    {
        public UpdateFieldArrayString Name = new(5, 0, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            for (int i = 0; i < 5; ++i)
            {
                data.WriteBits(Name[i].GetByteCount(), 10);
            }
            for (int i = 0; i < 5; ++i)
            {
                data.WriteString(Name[i]);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Name);
            _changesMask.ResetAll();
        }
    }

    public class CustomTabardInfo() : HasChangesMask(6)
    {
        public UpdateField<int> EmblemStyle = new(0, 1);
        public UpdateField<int> EmblemColor = new(0, 2);
        public UpdateField<int> BorderStyle = new(0, 3);
        public UpdateField<int> BorderColor = new(0, 4);
        public UpdateField<int> BackgroundColor = new(0, 5);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(EmblemStyle);
            data.WriteInt32(EmblemColor);
            data.WriteInt32(BorderStyle);
            data.WriteInt32(BorderColor);
            data.WriteInt32(BackgroundColor);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class PlayerData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Player, 324)
    {
        public UpdateField<bool> HasQuestSession = new(0, 1);
        public UpdateField<bool> HasLevelLink = new(0, 2);
        public DynamicUpdateField<ChrCustomizationChoice> Customizations = new(0, 3);
        public DynamicUpdateField<ChrCustomizationChoice> QaCustomizations = new(0, 4);
        public DynamicUpdateField<QuestLog> QuestSessionQuestLog = new(0, 5);
        public DynamicUpdateField<ArenaCooldown> ArenaCooldowns = new(0, 6);
        public DynamicUpdateField<int> VisualItemReplacements = new(0, 8);
        public DynamicUpdateField<PetCreatureName> PetNames = new(0, 7);
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
        public UpdateField<uint> PlayerTitle = new(0, 25);
        public UpdateField<int> FakeInebriation = new(0, 26);
        public UpdateField<uint> VirtualPlayerRealm = new(0, 27);
        public UpdateField<uint> CurrentSpecID = new(0, 28);
        public UpdateField<int> CurrentCombatTraitConfigSubTreeID = new(0, 29);
        public UpdateField<int> TaxiMountAnimKitID = new(0, 30);
        public UpdateField<byte> CurrentBattlePetBreedQuality = new(0, 31);
        public UpdateField<uint> HonorLevel = new(32, 33);
        public UpdateField<long> LogoutTime = new(32, 34);
        public UpdateFieldString Name = new(32, 35);
        public UpdateField<int> Field_1AC = new(32, 36);
        public UpdateField<int> Field_1B0 = new(32, 37);
        public UpdateField<int> CurrentBattlePetSpeciesID = new(32, 38);
        public UpdateField<CTROptions> CtrOptions = new(32, 39);
        public UpdateField<int> CovenantID = new(32, 40);
        public UpdateField<int> SoulbindID = new(32, 41);
        public UpdateField<DungeonScoreSummary> DungeonScore = new(32, 42);
        public UpdateField<LeaverInfo> LeaverInfo = new(32, 43);
        public UpdateField<ObjectGuid> SpectateTarget = new(32, 44);
        public UpdateField<int> Field_200 = new(32, 45);
        public OptionalUpdateField<DeclinedNames> DeclinedNames = new(32, 46);
        public UpdateField<CustomTabardInfo> PersonalTabard = new(32, 47);
        public UpdateFieldArray<byte> PartyType = new(2, 48, 49);
        public UpdateFieldArray<QuestLog> QuestLog = new(175, 51, 52);
        public UpdateFieldArray<VisibleItem> VisibleItems = new(19, 227, 228);
        public UpdateFieldArray<float> AvgItemLevel = new(6, 247, 248);
        public UpdateFieldArray<ZonePlayerForcedReaction> ForcedReactions = new(32, 254, 255);
        public UpdateFieldArray<ItemInstance> VisibleEquipableSpells = new(16, 287, 288);
        public UpdateFieldArray<uint> Field_3120 = new(19, 304, 305);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
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
            data.WriteInt32(QaCustomizations.Size());
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
                    QuestLog[i].WriteCreate(data, owner, receiver);

                data.WriteInt32(QuestSessionQuestLog.Size());
            }
            for (int i = 0; i < 19; ++i)
            {
                VisibleItems[i].WriteCreate(data, owner, receiver);
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
                ForcedReactions[i].WriteCreate(data, owner, receiver);
            }
            data.WriteInt32(Field_1AC);
            data.WriteInt32(Field_1B0);
            data.WriteInt32(CurrentBattlePetSpeciesID);
            data.WriteInt32(PetNames.Size());
            ((CTROptions)CtrOptions).WriteCreate(data, owner, receiver);
            data.WriteInt32(CovenantID);
            data.WriteInt32(SoulbindID);
            data.WritePackedGuid(SpectateTarget);
            data.WriteInt32(Field_200);
            data.WriteInt32(VisualItemReplacements.Size());
            for (int i = 0; i < 19; ++i)
            {
                data.WriteUInt32(Field_3120[i]);
            }
            PersonalTabard.GetValue().WriteCreate(data, owner, receiver);
            for (int i = 0; i < Customizations.Size(); ++i)
            {
                Customizations[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < QaCustomizations.Size(); ++i)
            {
                QaCustomizations[i].WriteCreate(data, owner, receiver);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                for (int i = 0; i < QuestSessionQuestLog.Size(); ++i)
                {
                    QuestSessionQuestLog[i].WriteCreate(data, owner, receiver);
                }
            }
            for (int i = 0; i < ArenaCooldowns.Size(); ++i)
            {
                ArenaCooldowns[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < VisualItemReplacements.Size(); ++i)
            {
                data.WriteInt32(VisualItemReplacements[i]);
            }
            data.FlushBits();
            data.WriteBits(Name.Size(), 6);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                data.WriteBit(HasQuestSession);
            }
            data.WriteBit(HasLevelLink);
            data.WriteBits(DeclinedNames.HasValue(), 1);
            DungeonScore.GetValue().Write(data);
            data.WriteString(Name);
            LeaverInfo.GetValue().WriteCreate(data, owner, receiver);
            for (int i = 0; i < 16; ++i)
            {
                VisibleEquipableSpells[i].Write(data);
            }
            for (int i = 0; i < PetNames.Size(); ++i)
            {
                PetNames[i].WriteCreate(data, owner, receiver);
            }
            if (DeclinedNames.HasValue())
            {
                DeclinedNames.GetValue().WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void PlayerDataAppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
                allowedMaskForTarget.OR(new UpdateMask(ChangeMaskLength, [0x00000022u, 0xFFF80000u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x00000007u, 0x00000000u, 0x00000000u, 0x00000000u]));
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            PlayerDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(324, [0xFFFFFFDDu, 0x0007FFFFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0xFFFFFFF8u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x0000000Fu]);
            PlayerDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(324, [0xFFFFFFDDu, 0x0007FFFFu, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0xFFFFFFF8u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x0000000Fu]);
            PlayerDataAppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
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
                        QaCustomizations.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(QaCustomizations.Size(), data);
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
                            Customizations[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < QaCustomizations.Size(); ++i)
                    {
                        if (QaCustomizations.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            QaCustomizations[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                                QuestSessionQuestLog[i].WriteCreate(data, owner, receiver);
                            else
                                QuestSessionQuestLog[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[6])
                {
                    for (int i = 0; i < ArenaCooldowns.Size(); ++i)
                    {
                        if (ArenaCooldowns.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ArenaCooldowns[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                if (changesMask[7])
                {
                    for (int i = 0; i < PetNames.Size(); ++i)
                    {
                        if (PetNames.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PetNames[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                    data.WriteUInt32(PlayerTitle);
                }
                if (changesMask[26])
                {
                    data.WriteInt32(FakeInebriation);
                }
                if (changesMask[27])
                {
                    data.WriteUInt32(VirtualPlayerRealm);
                }
                if (changesMask[28])
                {
                    data.WriteUInt32(CurrentSpecID);
                }
                if (changesMask[29])
                {
                    data.WriteInt32(CurrentCombatTraitConfigSubTreeID);
                }
                if (changesMask[30])
                {
                    data.WriteInt32(TaxiMountAnimKitID);
                }
                if (changesMask[31])
                {
                    data.WriteUInt8(CurrentBattlePetBreedQuality);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt32(HonorLevel);
                }
                if (changesMask[34])
                {
                    data.WriteInt64(LogoutTime);
                }
                if (changesMask[36])
                {
                    data.WriteInt32(Field_1AC);
                }
                if (changesMask[37])
                {
                    data.WriteInt32(Field_1B0);
                }
                if (changesMask[38])
                {
                    data.WriteInt32(CurrentBattlePetSpeciesID);
                }
                if (changesMask[39])
                {
                    ((CTROptions)CtrOptions).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[40])
                {
                    data.WriteInt32(CovenantID);
                }
                if (changesMask[41])
                {
                    data.WriteInt32(SoulbindID);
                }
                if (changesMask[44])
                {
                    data.WritePackedGuid(SpectateTarget);
                }
                if (changesMask[45])
                {
                    data.WriteInt32(Field_200);
                }
                if (changesMask[47])
                {
                    PersonalTabard.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[35])
                {
                    data.WriteBits(Name.Size(), 6);
                }
                data.WriteBits(DeclinedNames.HasValue(), 1);
                data.FlushBits();
                if (changesMask[42])
                {
                    DungeonScore.GetValue().Write(data);
                }
                if (changesMask[35])
                {
                    data.WriteString(Name);
                }
                if (changesMask[43])
                {
                    LeaverInfo.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[46])
                {
                    if (DeclinedNames.HasValue())
                    {
                        DeclinedNames.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[48])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[49 + i])
                    {
                        data.WriteUInt8(PartyType[i]);
                    }
                }
            }
            if (changesMask[51])
            {
                for (int i = 0; i < 175; ++i)
                {
                    if (changesMask[52 + i])
                    {
                        if (noQuestLogChangesMask)
                            QuestLog[i].WriteCreate(data, owner, receiver);
                        else
                            QuestLog[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[227])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[228 + i])
                    {
                        VisibleItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[247])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[248 + i])
                    {
                        data.WriteFloat(AvgItemLevel[i]);
                    }
                }
            }
            if (changesMask[254])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (changesMask[255 + i])
                    {
                        ForcedReactions[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[304])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[305 + i])
                    {
                        data.WriteUInt32(Field_3120[i]);
                    }
                }
            }
            if (changesMask[287])
            {
                for (int i = 0; i < 16; ++i)
                {
                    if (changesMask[288 + i])
                    {
                        VisibleEquipableSpells[i].Write(data);
                    }
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(HasQuestSession);
            ClearChangesMask(HasLevelLink);
            ClearChangesMask(Customizations);
            ClearChangesMask(QaCustomizations);
            ClearChangesMask(QuestSessionQuestLog);
            ClearChangesMask(ArenaCooldowns);
            ClearChangesMask(VisualItemReplacements);
            ClearChangesMask(PetNames);
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
            ClearChangesMask(Field_1AC);
            ClearChangesMask(Field_1B0);
            ClearChangesMask(CurrentBattlePetSpeciesID);
            ClearChangesMask(CtrOptions);
            ClearChangesMask(CovenantID);
            ClearChangesMask(SoulbindID);
            ClearChangesMask(DungeonScore);
            ClearChangesMask(LeaverInfo);
            ClearChangesMask(SpectateTarget);
            ClearChangesMask(Field_200);
            ClearChangesMask(DeclinedNames);
            ClearChangesMask(PersonalTabard);
            ClearChangesMask(PartyType);
            ClearChangesMask(QuestLog);
            ClearChangesMask(VisibleItems);
            ClearChangesMask(AvgItemLevel);
            ClearChangesMask(ForcedReactions);
            ClearChangesMask(VisibleEquipableSpells);
            ClearChangesMask(Field_3120);
            _changesMask.ResetAll();
        }

        bool IsQuestLogChangesMaskSkipped() { return false; } // bandwidth savings aren't worth the cpu time
    }

    public class SkillInfo() : HasChangesMask(1793)
    {
        public UpdateFieldArray<ushort> SkillLineID = new(256, 0, 1);
        public UpdateFieldArray<ushort> SkillStep = new(256, 0, 257);
        public UpdateFieldArray<ushort> SkillRank = new(256, 0, 513);
        public UpdateFieldArray<ushort> SkillStartingRank = new(256, 0, 769);
        public UpdateFieldArray<ushort> SkillMaxRank = new(256, 0, 1025);
        public UpdateFieldArray<ushort> SkillTempBonus = new(256, 0, 1281);
        public UpdateFieldArray<ushort> SkillPermBonus = new(256, 0, 1537);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            for (int i = 0; i < 256; ++i)
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            for (uint i = 0; i < 1; ++i)
                data.WriteUInt32(changesMask.GetBlocksMask(i));
            data.WriteBits(changesMask.GetBlocksMask(1), 25);
            for (uint i = 0; i < 57; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                for (int i = 0; i < 256; ++i)
                {
                    if (changesMask[1 + i])
                    {
                        data.WriteUInt16(SkillLineID[i]);
                    }
                    if (changesMask[257 + i])
                    {
                        data.WriteUInt16(SkillStep[i]);
                    }
                    if (changesMask[513 + i])
                    {
                        data.WriteUInt16(SkillRank[i]);
                    }
                    if (changesMask[769 + i])
                    {
                        data.WriteUInt16(SkillStartingRank[i]);
                    }
                    if (changesMask[1025 + i])
                    {
                        data.WriteUInt16(SkillMaxRank[i]);
                    }
                    if (changesMask[1281 + i])
                    {
                        data.WriteUInt16(SkillTempBonus[i]);
                    }
                    if (changesMask[1537 + i])
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

    public class BitVector() : HasChangesMask(2)
    {
        public DynamicUpdateField<ulong> Values = new(0, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(Values.Size());
            for (int i = 0; i < Values.Size(); ++i)
                data.WriteUInt64(Values[i]);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class BitVectors() : HasChangesMask(14)
    {
        public UpdateFieldArray<BitVector> Values = new(13, 0, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            for (int i = 0; i < 13; ++i)
                Values[i].WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                for (int i = 0; i < 13; ++i)
                {
                    if (changesMask[1 + i])
                    {
                        Values[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
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

    public class PlayerDataElement : IEquatable<PlayerDataElement>
    {
        public uint Type;
        public float FloatValue;
        public long Int64Value;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

        public bool Equals(PlayerDataElement right)
        {
            return Type == right.Type
                && FloatValue == right.FloatValue
                && Int64Value == right.Int64Value;
        }
    }

    public class RestInfo() : HasChangesMask(3)
    {
        public UpdateField<uint> Threshold = new(0, 1);
        public UpdateField<byte> StateID = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(Threshold);
            data.WriteUInt8(StateID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class PVPInfo() : HasChangesMask(19)
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
        public UpdateField<uint> SeasonBestRating = new(0, 10);
        public UpdateField<uint> PvpTierID = new(0, 11);
        public UpdateField<uint> WeeklyBestWinPvpTierID = new(0, 12);
        public UpdateField<uint> Field_28 = new(0, 13);
        public UpdateField<uint> Field_2C = new(0, 14);
        public UpdateField<uint> WeeklyRoundsPlayed = new(0, 15);
        public UpdateField<uint> WeeklyRoundsWon = new(0, 16);
        public UpdateField<uint> SeasonRoundsPlayed = new(0, 17);
        public UpdateField<uint> SeasonRoundsWon = new(0, 18);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt8(Bracket);
            data.WriteUInt32(PvpRatingID);
            data.WriteUInt32(WeeklyPlayed);
            data.WriteUInt32(WeeklyWon);
            data.WriteUInt32(SeasonPlayed);
            data.WriteUInt32(SeasonWon);
            data.WriteUInt32(Rating);
            data.WriteUInt32(WeeklyBestRating);
            data.WriteUInt32(SeasonBestRating);
            data.WriteUInt32(PvpTierID);
            data.WriteUInt32(WeeklyBestWinPvpTierID);
            data.WriteUInt32(Field_28);
            data.WriteUInt32(Field_2C);
            data.WriteUInt32(WeeklyRoundsPlayed);
            data.WriteUInt32(WeeklyRoundsWon);
            data.WriteUInt32(SeasonRoundsPlayed);
            data.WriteUInt32(SeasonRoundsWon);
            data.WriteBit(Disqualified);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    data.WriteUInt32(SeasonBestRating);
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
                    data.WriteUInt32(Field_28);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(Field_2C);
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
            data.FlushBits();
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
            ClearChangesMask(SeasonBestRating);
            ClearChangesMask(PvpTierID);
            ClearChangesMask(WeeklyBestWinPvpTierID);
            ClearChangesMask(Field_28);
            ClearChangesMask(Field_2C);
            ClearChangesMask(WeeklyRoundsPlayed);
            ClearChangesMask(WeeklyRoundsWon);
            ClearChangesMask(SeasonRoundsPlayed);
            ClearChangesMask(SeasonRoundsWon);
            _changesMask.ResetAll();
        }
    }

    public class CharacterRestriction : IEquatable<CharacterRestriction>
    {
        public int Field_0;
        public int Field_4;
        public int Field_8;
        public uint Type;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(Field_4);
            data.WriteInt32(Field_8);
            data.WriteBits(Type, 5);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(Field_4);
            data.WriteInt32(Field_8);
            data.WriteBits(Type, 5);
            data.FlushBits();
        }

        public bool Equals(CharacterRestriction right)
        {
            return Field_0 == right.Field_0
                && Field_4 == right.Field_4
                && Field_8 == right.Field_8
                && Type == right.Type;
        }
    }

    public class SpellPctModByLabel : IEquatable<SpellPctModByLabel>
    {
        public int ModIndex;
        public float ModifierValue;
        public int LabelID;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(ModIndex);
            data.WriteFloat(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(ModIndex);
            data.WriteFloat(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public bool Equals(SpellPctModByLabel right)
        {
            return ModIndex == right.ModIndex
                && ModifierValue == right.ModifierValue
                && LabelID == right.LabelID;
        }
    }

    public class SpellFlatModByLabel : IEquatable<SpellFlatModByLabel>
    {
        public int ModIndex;
        public int ModifierValue;
        public int LabelID;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(ModIndex);
            data.WriteInt32(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(ModIndex);
            data.WriteInt32(ModifierValue);
            data.WriteInt32(LabelID);
        }

        public bool Equals(SpellFlatModByLabel right)
        {
            return ModIndex == right.ModIndex
                && ModifierValue == right.ModifierValue
                && LabelID == right.LabelID;
        }
    }

    public class CompletedProject() : HasChangesMask(4)
    {
        public UpdateField<uint> ProjectID = new(0, 1);
        public UpdateField<long> FirstCompleted = new(0, 2);
        public UpdateField<uint> CompletionCount = new(0, 3);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(ProjectID);
            data.WriteInt64(FirstCompleted);
            data.WriteUInt32(CompletionCount);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    data.WriteUInt32(ProjectID);
                }
                if (changesMask[2])
                {
                    data.WriteInt64(FirstCompleted);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(CompletionCount);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ProjectID);
            ClearChangesMask(FirstCompleted);
            ClearChangesMask(CompletionCount);
            _changesMask.ResetAll();
        }
    }

    public class ResearchHistory() : HasChangesMask(2)
    {
        public DynamicUpdateField<CompletedProject> CompletedProjects = new(0, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(CompletedProjects.Size());
            for (int i = 0; i < CompletedProjects.Size(); ++i)
            {
                CompletedProjects[i].WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                            CompletedProjects[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
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

    public class MawPower : IEquatable<MawPower>
    {
        public int SpellID;
        public int MawPowerID;
        public int Stacks;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(MawPowerID);
            data.WriteInt32(Stacks);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(MawPowerID);
            data.WriteInt32(Stacks);
        }

        public bool Equals(MawPower right)
        {
            return SpellID == right.SpellID
                && MawPowerID == right.MawPowerID
                && Stacks == right.Stacks;
        }
    }

    public class MultiFloorExplore : IEquatable<MultiFloorExplore>
    {
        public List<int> WorldMapOverlayIDs = new();

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(WorldMapOverlayIDs.Count);
            for (int i = 0; i < WorldMapOverlayIDs.Count; ++i)
            {
                data.WriteInt32(WorldMapOverlayIDs[i]);
            }
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(WorldMapOverlayIDs.Count);
            for (int i = 0; i < WorldMapOverlayIDs.Count; ++i)
            {
                data.WriteInt32(WorldMapOverlayIDs[i]);
            }
            data.FlushBits();
        }

        public bool Equals(MultiFloorExplore right)
        {
            return WorldMapOverlayIDs == right.WorldMapOverlayIDs;
        }
    }

    public class RecipeProgressionInfo : IEquatable<RecipeProgressionInfo>
    {
        public ushort RecipeProgressionGroupID;
        public ushort Experience;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt16(RecipeProgressionGroupID);
            data.WriteUInt16(Experience);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteUInt16(RecipeProgressionGroupID);
            data.WriteUInt16(Experience);
        }

        public bool Equals(RecipeProgressionInfo right)
        {
            return RecipeProgressionGroupID == right.RecipeProgressionGroupID
                && Experience == right.Experience;
        }
    }

    public class ActivePlayerUnk901() : HasChangesMask(3)
    {
        public UpdateField<ObjectGuid> Field_0 = new(0, 1);
        public UpdateField<int> Field_10 = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WritePackedGuid(Field_0);
            data.WriteInt32(Field_10);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    data.WritePackedGuid(Field_0);
                }
                if (changesMask[2])
                {
                    data.WriteInt32(Field_10);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Field_0);
            ClearChangesMask(Field_10);
            _changesMask.ResetAll();
        }
    }

    public class QuestSession() : HasChangesMask(3)
    {
        public UpdateField<ObjectGuid> Owner = new(0, 1);
        public UpdateField<BitVector> QuestCompleted = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WritePackedGuid(Owner);
            QuestCompleted.GetValue().WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    QuestCompleted.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);
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

    public class ReplayedQuest() : HasChangesMask(3)
    {
        public UpdateField<int> QuestID = new(0, 1);
        public UpdateField<uint> ReplayTime = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(QuestID);
            data.WriteUInt32(ReplayTime);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class TraitEntry : IEquatable<TraitEntry>
    {
        public int TraitNodeID;
        public int TraitNodeEntryID;
        public int Rank;
        public int GrantedRanks;
        public int BonusRanks;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(TraitNodeID);
            data.WriteInt32(TraitNodeEntryID);
            data.WriteInt32(Rank);
            data.WriteInt32(GrantedRanks);
            data.WriteInt32(BonusRanks);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(TraitNodeID);
            data.WriteInt32(TraitNodeEntryID);
            data.WriteInt32(Rank);
            data.WriteInt32(GrantedRanks);
            data.WriteInt32(BonusRanks);
        }

        public bool Equals(TraitEntry right)
        {
            return TraitNodeID == right.TraitNodeID
                && TraitNodeEntryID == right.TraitNodeEntryID
                && Rank == right.Rank
                && GrantedRanks == right.GrantedRanks
                && BonusRanks == right.BonusRanks;
        }
    }

    public class TraitSubTreeCache : IEquatable<TraitSubTreeCache>
    {
        public List<TraitEntry> Entries = new();
        public int TraitSubTreeID;
        public uint Active;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(TraitSubTreeID);
            data.WriteInt32(Entries.Count);
            for (int i = 0; i < Entries.Count; ++i)
            {
                Entries[i].WriteCreate(data, owner, receiver);
            }
            data.WriteBits(Active, 1);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(TraitSubTreeID);
            data.WriteInt32(Entries.Count);
            for (int i = 0; i < Entries.Count; ++i)
            {
                Entries[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
            }
            data.FlushBits();
            data.WriteBits(Active, 1);
            data.FlushBits();
        }

        public bool Equals(TraitSubTreeCache right)
        {
            return Entries == right.Entries
                && TraitSubTreeID == right.TraitSubTreeID
                && Active == right.Active;
        }
    }

    public class TraitConfig() : HasChangesMask(14)
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

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
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
            }
            for (int i = 0; i < Entries.Size(); ++i)
            {
                Entries[i].WriteCreate(data, owner, receiver);
            }
            data.WriteBits(Name.Size(), 9);
            for (int i = 0; i < SubTrees.Size(); ++i)
            {
                SubTrees[i].WriteCreate(data, owner, receiver);
            }
            data.WriteString(Name);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 14);

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
                            Entries[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < SubTrees.Size(); ++i)
                    {
                        if (SubTrees.HasChanged(i) || ignoreChangesMask)
                        {
                            SubTrees[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
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
            }
            if (changesMask[4])
            {
                if (changesMask[5])
                {
                    data.WriteBits(Name.Size(), 9);
                    data.WriteString(Name);
                }
            }
            data.FlushBits();
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
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrderItem() : HasChangesMask(7)
    {
        public UpdateField<ulong> Field_0 = new(-1, 0);
        public UpdateField<ObjectGuid> ItemGUID = new(-1, 1);
        public UpdateField<ObjectGuid> OwnerGUID = new(-1, 2);
        public UpdateField<int> ItemID = new(-1, 3);
        public UpdateField<uint> Quantity = new(-1, 4);
        public UpdateField<int> ReagentQuality = new(-1, 5);
        public OptionalUpdateField<byte> DataSlotIndex = new(-1, 6);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt64(Field_0);
            data.WritePackedGuid(ItemGUID);
            data.WritePackedGuid(OwnerGUID);
            data.WriteInt32(ItemID);
            data.WriteUInt32(Quantity);
            data.WriteInt32(ReagentQuality);
            data.WriteBits(DataSlotIndex.HasValue(), 1);
            data.FlushBits();
            if (DataSlotIndex.HasValue())
            {
                data.WriteUInt8(DataSlotIndex);
            }
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 7);

            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteUInt64(Field_0);
            }
            if (changesMask[1])
            {
                data.WritePackedGuid(ItemGUID);
            }
            if (changesMask[2])
            {
                data.WritePackedGuid(OwnerGUID);
            }
            if (changesMask[3])
            {
                data.WriteInt32(ItemID);
            }
            if (changesMask[4])
            {
                data.WriteUInt32(Quantity);
            }
            if (changesMask[5])
            {
                data.WriteInt32(ReagentQuality);
            }
            data.WriteBits(DataSlotIndex.HasValue(), 1);
            data.FlushBits();
            if (changesMask[6])
            {
                if (DataSlotIndex.HasValue())
                {
                    data.WriteUInt8(DataSlotIndex);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Field_0);
            ClearChangesMask(ItemGUID);
            ClearChangesMask(OwnerGUID);
            ClearChangesMask(ItemID);
            ClearChangesMask(Quantity);
            ClearChangesMask(ReagentQuality);
            ClearChangesMask(DataSlotIndex);
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrderCustomer() : HasChangesMask(2)
    {
        public UpdateField<ObjectGuid> CustomerGUID = new(-1, 0);
        public UpdateField<ObjectGuid> CustomerAccountGUID = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WritePackedGuid(CustomerGUID);
            data.WritePackedGuid(CustomerAccountGUID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class CraftingOrderNpcCustomer() : HasChangesMask(2)
    {
        public UpdateField<long> NpcCraftingOrderCustomerID = new(-1, 0);
        public UpdateField<int> Field_8 = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt64(NpcCraftingOrderCustomerID);
            data.WriteInt32(Field_8);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                data.WriteInt32(Field_8);
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(NpcCraftingOrderCustomerID);
            ClearChangesMask(Field_8);
            _changesMask.ResetAll();
        }
    }

    public class CraftingOrderData() : HasChangesMask(26)
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

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
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
            data.WriteBits(CustomerNotes.Size(), 10);
            data.WriteBits(Customer.HasValue(), 1);
            data.WriteBits(NpcCustomer.HasValue(), 1);
            data.WriteBits(OutputItem.HasValue(), 1);
            data.WriteBits(OutputItemData.HasValue(), 1);
            data.FlushBits();
            for (int i = 0; i < Reagents.Size(); ++i)
            {
                Reagents[i].WriteCreate(data, owner, receiver);
            }
            data.WriteString(CustomerNotes);
            if (Customer.HasValue())
            {
                Customer.GetValue().WriteCreate(data, owner, receiver);
            }
            if (NpcCustomer.HasValue())
            {
                NpcCustomer.GetValue().WriteCreate(data, owner, receiver);
            }
            if (OutputItem.HasValue())
            {
                OutputItem.GetValue().WriteCreate(data, owner, receiver);
            }
            if (OutputItemData.HasValue())
            {
                OutputItemData.GetValue().Write(data);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                            Reagents[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
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
                data.WriteBits(Customer.HasValue(), 1);
                data.WriteBits(NpcCustomer.HasValue(), 1);
                data.WriteBits(OutputItem.HasValue(), 1);
            }
            if (changesMask[24])
            {
                data.WriteBits(OutputItemData.HasValue(), 1);
            }
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
                        Customer.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);
                    }
                }
                if (changesMask[22])
                {
                    if (NpcCustomer.HasValue())
                    {
                        NpcCustomer.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);
                    }
                }
                if (changesMask[23])
                {
                    if (OutputItem.HasValue())
                    {
                        OutputItem.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);
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
            data.FlushBits();
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

    public class CraftingOrder() : HasChangesMask(4)
    {
        public DynamicUpdateField<ItemEnchantData> Enchantments = new(-1, 0);
        public DynamicUpdateField<ItemGemData> Gems = new(-1, 1);
        public UpdateField<CraftingOrderData> Data = new(-1, 2);
        public OptionalUpdateField<ItemInstance> RecraftItemInfo = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            Data.GetValue().WriteCreate(data, owner, receiver);
            data.WriteBits(RecraftItemInfo.HasValue(), 1);
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                Data.GetValue().WriteUpdate(data, ignoreChangesMask, owner, receiver);
            }
            data.WriteBits(RecraftItemInfo.HasValue(), 1);
            data.FlushBits();
            if (changesMask[3])
            {
                if (RecraftItemInfo.HasValue())
                {
                    RecraftItemInfo.GetValue().Write(data);
                }
            }
            data.FlushBits();
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

    public class PersonalCraftingOrderCount() : HasChangesMask(2)
    {
        public UpdateField<int> ProfessionID = new(-1, 0);
        public UpdateField<uint> Count = new(-1, 1);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(ProfessionID);
            data.WriteUInt32(Count);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class NPCCraftingOrderInfo() : HasChangesMask(4)
    {
        public UpdateField<ulong> OrderID = new(-1, 0);
        public UpdateField<int> NpcCraftingOrderSetID = new(-1, 1);
        public UpdateField<int> NpcTreasureID = new(-1, 2);
        public UpdateField<int> NpcCraftingOrderCustomerID = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt64(OrderID);
            data.WriteInt32(NpcCraftingOrderSetID);
            data.WriteInt32(NpcTreasureID);
            data.WriteInt32(NpcCraftingOrderCustomerID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public struct CategoryCooldownMod : IEquatable<CategoryCooldownMod>
    {
        public int SpellCategoryID;
        public int ModCooldown;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteInt32(ModCooldown);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteInt32(ModCooldown);
        }

        public bool Equals(CategoryCooldownMod right)
        {
            return SpellCategoryID == right.SpellCategoryID
                && ModCooldown == right.ModCooldown;
        }
    }

    public struct WeeklySpellUse : IEquatable<WeeklySpellUse>
    {
        public int SpellCategoryID;
        public byte Uses;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteUInt8(Uses);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(SpellCategoryID);
            data.WriteUInt8(Uses);
        }

        public bool Equals(WeeklySpellUse right)
        {
            return SpellCategoryID == right.SpellCategoryID
                && Uses == right.Uses;
        }
    }

    public class StablePetInfo() : HasChangesMask(9)
    {
        public UpdateField<uint> PetSlot = new(0, 1);
        public UpdateField<uint> PetNumber = new(0, 2);
        public UpdateField<uint> CreatureID = new(0, 3);
        public UpdateField<uint> DisplayID = new(0, 4);
        public UpdateField<uint> ExperienceLevel = new(0, 5);
        public UpdateFieldString Name = new(0, 6);
        public UpdateField<byte> PetFlags = new(0, 7);
        public UpdateField<uint> Specialization = new(0, 8);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(PetSlot);
            data.WriteUInt32(PetNumber);
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(DisplayID);
            data.WriteUInt32(ExperienceLevel);
            data.WriteUInt8(PetFlags);
            data.WriteUInt32(Specialization);
            data.WriteBits(Name.Size(), 8);
            data.WriteString(Name);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                    data.WriteString(Name);
                }
            }
            data.FlushBits();
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

    public class StableInfo() : HasChangesMask(3)
    {
        public DynamicUpdateField<StablePetInfo> Pets = new(0, 1);
        public UpdateField<ObjectGuid> StableMaster = new(0, 2);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(Pets.Size());
            data.WritePackedGuid(StableMaster);
            for (int i = 0; i < Pets.Size(); ++i)
            {
                Pets[i].WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
                            Pets[i].WriteUpdate(data, ignoreChangesMask, owner, receiver);
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

    public class CollectableSourceTrackedData() : HasChangesMask(4)
    {
        public UpdateField<int> TargetType = new(0, 1);
        public UpdateField<int> TargetID = new(0, 2);
        public UpdateField<int> CollectableSourceInfoID = new(0, 3);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(TargetType);
            data.WriteInt32(TargetID);
            data.WriteInt32(CollectableSourceInfoID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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

    public class BankTabSettings() : HasChangesMask(4)
    {
        public UpdateFieldString Name = new(-1, 0);
        public UpdateFieldString Icon = new(-1, 1);
        public UpdateFieldString Description = new(-1, 2);
        public UpdateField<int> DepositFlags = new(-1, 3);

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(Name.Size(), 7);
            data.WriteBits(Icon.Size(), 9);
            data.WriteBits(Description.Size(), 14);
            data.WriteInt32(DepositFlags);
            data.WriteString(Name);
            data.WriteString(Icon);
            data.WriteString(Description);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
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
            data.FlushBits();
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

    public struct WalkInData : IEquatable<WalkInData>
    {
        public int MapID;
        public long Field_8;
        public uint Type;
        public ObjectGuid Field_18;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(MapID);
            data.WriteInt64(Field_8);
            data.WritePackedGuid(Field_18);
            data.WriteBits(Type, 1);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(MapID);
            data.WriteInt64(Field_8);
            data.WritePackedGuid(Field_18);
            data.WriteBits(Type, 1);
            data.FlushBits();
        }

        public bool Equals(WalkInData right)
        {
            return MapID == right.MapID
                && Field_8 == right.Field_8
                && Type == right.Type
                && Field_18 == right.Field_18;
        }
    }

    public class DelveData :  IEquatable<DelveData>
    {
        public List<ObjectGuid> Owners = new();
        public int Field_0;
        public ulong Field_8;
        public int Field_10;
        public int SpellID;
        public uint Started;                                                             // Restricts rewards to players in m_owners if set to true. Intended to prevent rewarwding players that join in-progress delve?

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(Field_0);
            data.WriteUInt64(Field_8);
            data.WriteInt32(Field_10);
            data.WriteInt32(SpellID);
            data.WriteInt32(Owners.Count);
            for (int i = 0; i < Owners.Count; ++i)
            {
                data.WritePackedGuid(Owners[i]);
            }
            data.WriteBits(Started, 1);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.FlushBits();
            data.WriteInt32(Field_0);
            data.WriteUInt64(Field_8);
            data.WriteInt32(Field_10);
            data.WriteInt32(SpellID);
            data.WriteInt32(Owners.Count);
            for (int i = 0; i < Owners.Count; ++i)
            {
                data.WritePackedGuid(Owners[i]);
            }
            data.WriteBits(Started, 1);
            data.FlushBits();
        }

        public bool Equals(DelveData right)
        {
            return Owners == right.Owners
                && Field_0 == right.Field_0
                && Field_8 == right.Field_8
                && Field_10 == right.Field_10
                && SpellID == right.SpellID
                && Started == right.Started;
        }
    }

    public struct ChallengeModeData : IEquatable<ChallengeModeData>
    {
        public int Unknown_1120_1;
        public int Unknown_1120_2;
        public ulong Unknown_1120_3;
        public long Unknown_1120_4;
        public ObjectGuid KeystoneOwnerGUID;
        public ObjectGuid LeaverGUID;
        public uint IsActive;
        public uint HasRestrictions;
        public uint CanVoteAbandon;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(Unknown_1120_1);
            data.WriteInt32(Unknown_1120_2);
            data.WriteUInt64(Unknown_1120_3);
            data.WriteInt64(Unknown_1120_4);
            data.WritePackedGuid(KeystoneOwnerGUID);
            data.WritePackedGuid(LeaverGUID);
            data.WriteBits(IsActive, 1);
            data.WriteBits(HasRestrictions, 1);
            data.WriteBits(CanVoteAbandon, 1);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(Unknown_1120_1);
            data.WriteInt32(Unknown_1120_2);
            data.WriteUInt64(Unknown_1120_3);
            data.WriteInt64(Unknown_1120_4);
            data.WritePackedGuid(KeystoneOwnerGUID);
            data.WritePackedGuid(LeaverGUID);
            data.WriteBits(IsActive, 1);
            data.WriteBits(HasRestrictions, 1);
            data.WriteBits(CanVoteAbandon, 1);
            data.FlushBits();
        }

        public bool Equals(ChallengeModeData right)
        {
            return Unknown_1120_1 == right.Unknown_1120_1
                && Unknown_1120_2 == right.Unknown_1120_2
                && Unknown_1120_3 == right.Unknown_1120_3
                && Unknown_1120_4 == right.Unknown_1120_4
                && KeystoneOwnerGUID == right.KeystoneOwnerGUID
                && LeaverGUID == right.LeaverGUID
                && IsActive == right.IsActive
                && HasRestrictions == right.HasRestrictions
                && CanVoteAbandon == right.CanVoteAbandon;
        }
    }

    public struct Research : IEquatable<Research>
    {
        public short ResearchProjectID;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt16(ResearchProjectID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt16(ResearchProjectID);
        }

        public bool Equals(Research right)
        {
            return ResearchProjectID == right.ResearchProjectID;
        }
    }

    public class ActivePlayerData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.ActivePlayer, 385)
    {
        public static int QuestCompletedBitsPerBlock = sizeof(ulong) * 8;

        public UpdateField<bool> BackpackAutoSortDisabled = new(0, 1);
        public UpdateField<bool> BackpackSellJunkDisabled = new(0, 2);
        public UpdateField<bool> BankAutoSortDisabled = new(0, 3);
        public UpdateField<bool> SortBagsRightToLeft = new(0, 4);
        public UpdateField<bool> InsertItemsLeftToRight = new(0, 5);
        public UpdateField<bool> HasPerksProgramPendingReward = new(0, 6);
        public UpdateFieldArray<DynamicUpdateField<ushort>> ResearchSites = new(1, 43, 44);
        public UpdateFieldArray<DynamicUpdateField<uint>> ResearchSiteProgress = new(1, 45, 46);
        public UpdateFieldArray<DynamicUpdateField<Research>> Research = new(1, 47, 48);
        public DynamicUpdateField<ulong> KnownTitles = new(0, 7);
        public DynamicUpdateField<PlayerDataElement> CharacterDataElements = new(0, 8);
        public DynamicUpdateField<PlayerDataElement> AccountDataElements = new(0, 9);
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
        public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new(0, 25);
        public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new(0, 26);
        public DynamicUpdateField<MawPower> MawPowers = new(0, 27);
        public DynamicUpdateField<MultiFloorExplore> MultiFloorExploration = new(0, 28);
        public DynamicUpdateField<RecipeProgressionInfo> RecipeProgression = new(0, 29);
        public DynamicUpdateField<ReplayedQuest> ReplayedQuests = new(0, 30);
        public DynamicUpdateField<QuestLog> TaskQuests = new(0, 31);
        public DynamicUpdateField<int> DisabledSpells = new(32, 33);
        public DynamicUpdateField<PersonalCraftingOrderCount> PersonalCraftingOrderCounts = new(32, 36);
        public DynamicUpdateField<NPCCraftingOrderInfo> NpcCraftingOrders = new(32, 37);
        public DynamicUpdateField<CategoryCooldownMod> CategoryCooldownMods = new(32, 38);
        public DynamicUpdateField<WeeklySpellUse> WeeklySpellUses = new(32, 39);
        public DynamicUpdateField<CollectableSourceTrackedData> TrackedCollectableSources = new(32, 40);
        public DynamicUpdateField<PVPInfo> PvpInfo = new(0, 10);
        public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new(0, 24);
        public DynamicUpdateField<TraitConfig> TraitConfigs = new(32, 34);
        public DynamicUpdateField<CraftingOrder> CraftingOrders = new(32, 35);
        public DynamicUpdateField<BankTabSettings> CharacterBankTabSettings = new(32, 41);
        public DynamicUpdateField<BankTabSettings> AccountBankTabSettings = new(32, 42);
        public UpdateField<ObjectGuid> FarsightObject = new(32, 49);
        public UpdateField<ObjectGuid> SummonedBattlePetGUID = new(32, 50);
        public UpdateField<ulong> Coinage = new(32, 51);
        public UpdateField<ulong> AccountBankCoinage = new(32, 52);
        public UpdateField<uint> XP = new(32, 53);
        public UpdateField<uint> NextLevelXP = new(32, 54);
        public UpdateField<int> TrialXP = new(32, 55);
        public UpdateField<SkillInfo> Skill = new(32, 56);
        public UpdateField<uint> CharacterPoints = new(32, 57);
        public UpdateField<uint> MaxTalentTiers = new(32, 58);
        public UpdateField<uint> TrackCreatureMask = new(32, 59);
        public UpdateField<float> MainhandExpertise = new(32, 60);
        public UpdateField<float> OffhandExpertise = new(32, 61);
        public UpdateField<float> RangedExpertise = new(32, 62);
        public UpdateField<float> CombatRatingExpertise = new(32, 63);
        public UpdateField<float> BlockPercentage = new(32, 64);
        public UpdateField<float> DodgePercentage = new(32, 65);
        public UpdateField<float> DodgePercentageFromAttribute = new(32, 66);
        public UpdateField<float> ParryPercentage = new(32, 67);
        public UpdateField<float> ParryPercentageFromAttribute = new(32, 68);
        public UpdateField<float> CritPercentage = new(32, 69);
        public UpdateField<float> RangedCritPercentage = new(70, 71);
        public UpdateField<float> OffhandCritPercentage = new(70, 72);
        public UpdateField<float> SpellCritPercentage = new(70, 73);
        public UpdateField<uint> ShieldBlock = new(70, 74);
        public UpdateField<float> ShieldBlockCritPercentage = new(70, 75);
        public UpdateField<float> Mastery = new(70, 76);
        public UpdateField<float> Speed = new(70, 77);
        public UpdateField<float> Avoidance = new(70, 78);
        public UpdateField<float> Sturdiness = new(70, 79);
        public UpdateField<int> Versatility = new(70, 80);
        public UpdateField<float> VersatilityBonus = new(70, 81);
        public UpdateField<float> PvpPowerDamage = new(70, 82);
        public UpdateField<float> PvpPowerHealing = new(70, 83);
        public UpdateField<BitVectors> BitVectors = new(70, 84);
        public UpdateField<int> ModHealingDonePos = new(70, 85);
        public UpdateField<float> ModHealingPercent = new(70, 86);
        public UpdateField<float> ModPeriodicHealingDonePercent = new(70, 87);
        public UpdateField<float> ModSpellPowerPercent = new(70, 88);
        public UpdateField<float> ModResiliencePercent = new(70, 89);
        public UpdateField<float> OverrideSpellPowerByAPPercent = new(70, 90);
        public UpdateField<float> OverrideAPBySpellPowerPercent = new(70, 91);
        public UpdateField<int> ModTargetResistance = new(70, 92);
        public UpdateField<int> ModTargetPhysicalResistance = new(70, 93);
        public UpdateField<uint> LocalFlags = new(70, 94);
        public UpdateField<byte> GrantableLevels = new(70, 95);
        public UpdateField<byte> MultiActionBars = new(70, 96);
        public UpdateField<byte> LifetimeMaxRank = new(70, 97);
        public UpdateField<byte> NumRespecs = new(70, 98);
        public UpdateField<uint> PvpMedals = new(70, 99);
        public UpdateField<ushort> TodayHonorableKills = new(70, 100);
        public UpdateField<ushort> YesterdayHonorableKills = new(70, 101);
        public UpdateField<uint> LifetimeHonorableKills = new(102, 103);
        public UpdateField<uint> WatchedFactionIndex = new(102, 104);
        public UpdateField<uint> MaxLevel = new(102, 105);
        public UpdateField<int> ScalingPlayerLevelDelta = new(102, 106);
        public UpdateField<int> MaxCreatureScalingLevel = new(102, 107);
        public UpdateField<uint> PetSpellPower = new(102, 108);
        public UpdateField<float> UiHitModifier = new(102, 109);
        public UpdateField<float> UiSpellHitModifier = new(102, 110);
        public UpdateField<int> HomeRealmTimeOffset = new(102, 111);
        public UpdateField<float> ModPetHaste = new(102, 112);
        public UpdateField<sbyte> JailersTowerLevelMax = new(102, 113);
        public UpdateField<sbyte> JailersTowerLevel = new(102, 114);
        public UpdateField<byte> LocalRegenFlags = new(102, 115);
        public UpdateField<byte> AuraVision = new(102, 116);
        public UpdateField<byte> NumBackpackSlots = new(102, 117);
        public UpdateField<uint> OverrideSpellsID = new(102, 118);
        public UpdateField<ushort> LootSpecID = new(102, 119);
        public UpdateField<uint> OverrideZonePVPType = new(102, 120);
        public UpdateField<uint> Honor = new(102, 121);
        public UpdateField<uint> HonorNextLevel = new(102, 122);
        public UpdateField<int> PerksProgramCurrency = new(102, 123);
        public UpdateField<byte> NumBankSlots = new(102, 124);
        public UpdateField<byte> NumCharacterBankTabs = new(102, 125);
        public UpdateField<byte> NumAccountBankTabs = new(102, 126);
        public UpdateField<ResearchHistory> ResearchHistory = new(102, 127);
        public UpdateField<PerksVendorItem> FrozenPerksVendorItem = new(102, 128);
        public UpdateField<ActivePlayerUnk901> Field_1410 = new(102, 130);
        public OptionalUpdateField<QuestSession> QuestSession = new(102, 129);
        public UpdateField<int> UiChromieTimeExpansionID = new(102, 131);
        public UpdateField<int> TimerunningSeasonID = new(102, 132);
        public UpdateField<int> TransportServerTime = new(102, 133);
        public UpdateField<uint> WeeklyRewardsPeriodSinceOrigin = new(134, 135);               // week count since Cfg_RegionsEntry::ChallengeOrigin
        public UpdateField<short> DEBUGSoulbindConduitRank = new(134, 136);
        public UpdateField<DungeonScoreData> DungeonScore = new(134, 137);
        public UpdateField<uint> ActiveCombatTraitConfigID = new(134, 138);
        public UpdateField<int> ItemUpgradeHighOnehandWeaponItemID = new(134, 139);
        public UpdateField<int> ItemUpgradeHighFingerItemID = new(134, 140);
        public UpdateField<float> ItemUpgradeHighFingerWatermark = new(134, 141);
        public UpdateField<int> ItemUpgradeHighTrinketItemID = new(134, 142);
        public UpdateField<float> ItemUpgradeHighTrinketWatermark = new(134, 143);
        public UpdateField<ulong> LootHistoryInstanceID = new(134, 144);
        public OptionalUpdateField<StableInfo> PetStable = new(134, 145);
        public UpdateField<byte> RequiredMountCapabilityFlags = new(134, 146);
        public OptionalUpdateField<WalkInData> WalkInData = new(134, 147);
        public OptionalUpdateField<DelveData> DelveData = new(134, 148);
        public OptionalUpdateField<ChallengeModeData> ChallengeModeData = new(134, 149);
        public UpdateFieldArray<ObjectGuid> InvSlots = new(105, 150, 151);
        public UpdateFieldArray<RestInfo> RestInfo = new(2, 256, 257);
        public UpdateFieldArray<int> ModDamageDonePos = new(7, 259, 260);
        public UpdateFieldArray<int> ModDamageDoneNeg = new(7, 259, 267);
        public UpdateFieldArray<float> ModDamageDonePercent = new(7, 259, 274);
        public UpdateFieldArray<float> ModHealingDonePercent = new(7, 259, 281);
        public UpdateFieldArray<float> WeaponDmgMultipliers = new(3, 288, 289);
        public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new(3, 288, 292);
        public UpdateFieldArray<uint> BuybackPrice = new(12, 295, 296);
        public UpdateFieldArray<long> BuybackTimestamp = new(12, 295, 308);
        public UpdateFieldArray<uint> CombatRatings = new(32, 320, 321);
        public UpdateFieldArray<uint> NoReagentCostMask = new(4, 353, 354);
        public UpdateFieldArray<uint> ProfessionSkillLine = new(2, 358, 359);
        public UpdateFieldArray<uint> BagSlotFlags = new(5, 361, 362);
        public UpdateFieldArray<float> ItemUpgradeHighWatermark = new(17, 367, 368);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
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
            Skill.GetValue().WriteCreate(data, owner, receiver);
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
            BitVectors.GetValue().WriteCreate(data, owner, receiver);
            data.WriteInt32(CharacterDataElements.Size());
            data.WriteInt32(AccountDataElements.Size());
            for (int i = 0; i < 2; ++i)
            {
                RestInfo[i].WriteCreate(data, owner, receiver);
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
                    Research[i][j].WriteCreate(data, owner, receiver);
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
            data.WriteInt32(CharacterRestrictions.Size());
            data.WriteInt32(SpellPctModByLabel.Size());
            data.WriteInt32(SpellFlatModByLabel.Size());
            data.WriteInt32(MawPowers.Size());
            data.WriteInt32(MultiFloorExploration.Size());
            data.WriteInt32(RecipeProgression.Size());
            data.WriteInt32(ReplayedQuests.Size());
            data.WriteInt32(TaskQuests.Size());
            data.WriteInt32(DisabledSpells.Size());
            data.WriteInt32(UiChromieTimeExpansionID);
            data.WriteInt32(TimerunningSeasonID);
            data.WriteInt32(TransportServerTime);
            data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
            data.WriteInt16(DEBUGSoulbindConduitRank);
            data.WriteInt32(TraitConfigs.Size());
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
            for (int i = 0; i < KnownTitles.Size(); ++i)
            {
                data.WriteUInt64(KnownTitles[i]);
            }
            for (int i = 0; i < CharacterDataElements.Size(); ++i)
            {
                CharacterDataElements[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < AccountDataElements.Size(); ++i)
            {
                AccountDataElements[i].WriteCreate(data, owner, receiver);
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
            for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
            {
                SpellPctModByLabel[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
            {
                SpellFlatModByLabel[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < MawPowers.Size(); ++i)
            {
                MawPowers[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < MultiFloorExploration.Size(); ++i)
            {
                MultiFloorExploration[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < RecipeProgression.Size(); ++i)
            {
                RecipeProgression[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < ReplayedQuests.Size(); ++i)
            {
                ReplayedQuests[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < TaskQuests.Size(); ++i)
            {
                TaskQuests[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < DisabledSpells.Size(); ++i)
            {
                data.WriteInt32(DisabledSpells[i]);
            }
            for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
            {
                PersonalCraftingOrderCounts[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < NpcCraftingOrders.Size(); ++i)
            {
                NpcCraftingOrders[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < CategoryCooldownMods.Size(); ++i)
            {
                CategoryCooldownMods[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < WeeklySpellUses.Size(); ++i)
            {
                WeeklySpellUses[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < TrackedCollectableSources.Size(); ++i)
            {
                TrackedCollectableSources[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
            data.WriteBit(BackpackAutoSortDisabled);
            data.WriteBit(BackpackSellJunkDisabled);
            data.WriteBit(BankAutoSortDisabled);
            data.WriteBit(SortBagsRightToLeft);
            data.WriteBit(InsertItemsLeftToRight);
            data.WriteBit(HasPerksProgramPendingReward);
            data.WriteBits(QuestSession.HasValue(), 1);
            data.WriteBits(PetStable.HasValue(), 1);
            data.WriteBits(CharacterBankTabSettings.Size(), 3);
            data.WriteBits(AccountBankTabSettings.Size(), 3);
            data.WriteBits(WalkInData.HasValue(), 1);
            data.WriteBits(DelveData.HasValue(), 1);
            data.WriteBits(ChallengeModeData.HasValue(), 1);
            data.FlushBits();
            ResearchHistory.GetValue().WriteCreate(data, owner, receiver);
            if (QuestSession.HasValue())
            {
                QuestSession.GetValue().WriteCreate(data, owner, receiver);
            }
            FrozenPerksVendorItem.GetValue().Write(data);
            Field_1410.GetValue().WriteCreate(data, owner, receiver);
            DungeonScore.GetValue().Write(data);
            for (int i = 0; i < PvpInfo.Size(); ++i)
            {
                PvpInfo[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < CharacterRestrictions.Size(); ++i)
            {
                CharacterRestrictions[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < TraitConfigs.Size(); ++i)
            {
                TraitConfigs[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < CraftingOrders.Size(); ++i)
            {
                CraftingOrders[i].WriteCreate(data, owner, receiver);
            }
            if (PetStable.HasValue())
            {
                PetStable.GetValue().WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < CharacterBankTabSettings.Size(); ++i)
            {
                CharacterBankTabSettings[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < AccountBankTabSettings.Size(); ++i)
            {
                AccountBankTabSettings[i].WriteCreate(data, owner, receiver);
            }
            if (WalkInData.HasValue())
            {
                WalkInData.GetValue().WriteCreate(data, owner, receiver);
            }
            if (DelveData.HasValue())
            {
                DelveData.GetValue().WriteCreate(data, owner, receiver);
            }
            if (ChallengeModeData.HasValue())
            {
                ChallengeModeData.GetValue().WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
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
            if (changesMask[43])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[44])
                    {
                        if (!ignoreNestedChangesMask)
                            ResearchSites[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(ResearchSites[i].Size(), data);
                    }
                }
            }
            if (changesMask[45])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[46])
                    {
                        if (!ignoreNestedChangesMask)
                            ResearchSiteProgress[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(ResearchSiteProgress[i].Size(), data);
                    }
                }
            }
            if (changesMask[47])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[48])
                    {
                        if (!ignoreNestedChangesMask)
                            Research[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(Research[i].Size(), data);
                    }
                }
            }
            if (changesMask[43])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[44])
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
            if (changesMask[45])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[46])
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
            if (changesMask[47])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[48])
                    {
                        for (int j = 0; j < Research[i].Size(); ++j)
                        {
                            if (Research[i].HasChanged(j) || ignoreNestedChangesMask)
                            {
                                Research[i][j].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                        CharacterRestrictions.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterRestrictions.Size(), data);
                }
                if (changesMask[25])
                {
                    if (!ignoreNestedChangesMask)
                        SpellPctModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellPctModByLabel.Size(), data);
                }
                if (changesMask[26])
                {
                    if (!ignoreNestedChangesMask)
                        SpellFlatModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellFlatModByLabel.Size(), data);
                }
                if (changesMask[27])
                {
                    if (!ignoreNestedChangesMask)
                        MawPowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MawPowers.Size(), data);
                }
                if (changesMask[28])
                {
                    if (!ignoreNestedChangesMask)
                        MultiFloorExploration.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MultiFloorExploration.Size(), data);
                }
                if (changesMask[29])
                {
                    if (!ignoreNestedChangesMask)
                        RecipeProgression.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(RecipeProgression.Size(), data);
                }
                if (changesMask[30])
                {
                    if (!ignoreNestedChangesMask)
                        ReplayedQuests.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ReplayedQuests.Size(), data);
                }
                if (changesMask[31])
                {
                    if (!ignoreNestedChangesMask)
                        TaskQuests.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(TaskQuests.Size(), data);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    if (!ignoreNestedChangesMask)
                        DisabledSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(DisabledSpells.Size(), data);
                }
                if (changesMask[34])
                {
                    if (!ignoreNestedChangesMask)
                        TraitConfigs.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(TraitConfigs.Size(), data);
                }
                if (changesMask[35])
                {
                    if (!ignoreNestedChangesMask)
                        CraftingOrders.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CraftingOrders.Size(), data);
                }
                if (changesMask[36])
                {
                    if (!ignoreNestedChangesMask)
                        PersonalCraftingOrderCounts.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(PersonalCraftingOrderCounts.Size(), data);
                }
                if (changesMask[37])
                {
                    if (!ignoreNestedChangesMask)
                        NpcCraftingOrders.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(NpcCraftingOrders.Size(), data);
                }
                if (changesMask[38])
                {
                    if (!ignoreNestedChangesMask)
                        CategoryCooldownMods.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CategoryCooldownMods.Size(), data);
                }
                if (changesMask[39])
                {
                    if (!ignoreNestedChangesMask)
                        WeeklySpellUses.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(WeeklySpellUses.Size(), data);
                }
                if (changesMask[40])
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
                            CharacterDataElements[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[9])
                {
                    for (int i = 0; i < AccountDataElements.Size(); ++i)
                    {
                        if (AccountDataElements.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            AccountDataElements[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                if (changesMask[25])
                {
                    for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
                    {
                        if (SpellPctModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellPctModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[26])
                {
                    for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
                    {
                        if (SpellFlatModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellFlatModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[27])
                {
                    for (int i = 0; i < MawPowers.Size(); ++i)
                    {
                        if (MawPowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            MawPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[28])
                {
                    for (int i = 0; i < MultiFloorExploration.Size(); ++i)
                    {
                        if (MultiFloorExploration.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            MultiFloorExploration[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[29])
                {
                    for (int i = 0; i < RecipeProgression.Size(); ++i)
                    {
                        if (RecipeProgression.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            RecipeProgression[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[30])
                {
                    for (int i = 0; i < ReplayedQuests.Size(); ++i)
                    {
                        if (ReplayedQuests.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ReplayedQuests[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[31])
                {
                    for (int i = 0; i < TaskQuests.Size(); ++i)
                    {
                        if (TaskQuests.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            TaskQuests[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    for (int i = 0; i < DisabledSpells.Size(); ++i)
                    {
                        if (DisabledSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(DisabledSpells[i]);
                        }
                    }
                }
                if (changesMask[36])
                {
                    for (int i = 0; i < PersonalCraftingOrderCounts.Size(); ++i)
                    {
                        if (PersonalCraftingOrderCounts.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PersonalCraftingOrderCounts[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[37])
                {
                    for (int i = 0; i < NpcCraftingOrders.Size(); ++i)
                    {
                        if (NpcCraftingOrders.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            NpcCraftingOrders[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[38])
                {
                    for (int i = 0; i < CategoryCooldownMods.Size(); ++i)
                    {
                        if (CategoryCooldownMods.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CategoryCooldownMods[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[39])
                {
                    for (int i = 0; i < WeeklySpellUses.Size(); ++i)
                    {
                        if (WeeklySpellUses.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            WeeklySpellUses[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[40])
                {
                    for (int i = 0; i < TrackedCollectableSources.Size(); ++i)
                    {
                        if (TrackedCollectableSources.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            TrackedCollectableSources[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
            }
            data.FlushBits();
            if (changesMask[32])
            {
                if (changesMask[41])
                {
                    if (!ignoreNestedChangesMask)
                        CharacterBankTabSettings.WriteUpdateMask(data, 3);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterBankTabSettings.Size(), data, 3);
                }
                if (changesMask[42])
                {
                    if (!ignoreNestedChangesMask)
                        AccountBankTabSettings.WriteUpdateMask(data, 3);
                    else
                        WriteCompleteDynamicFieldUpdateMask(AccountBankTabSettings.Size(), data, 3);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[10])
                {
                    for (int i = 0; i < PvpInfo.Size(); ++i)
                    {
                        if (PvpInfo.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PvpInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[24])
                {
                    for (int i = 0; i < CharacterRestrictions.Size(); ++i)
                    {
                        if (CharacterRestrictions.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterRestrictions[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
            }
            if (changesMask[32])
            {
                if (changesMask[34])
                {
                    for (int i = 0; i < TraitConfigs.Size(); ++i)
                    {
                        if (TraitConfigs.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            TraitConfigs[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[35])
                {
                    for (int i = 0; i < CraftingOrders.Size(); ++i)
                    {
                        if (CraftingOrders.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CraftingOrders[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[41])
                {
                    for (int i = 0; i < CharacterBankTabSettings.Size(); ++i)
                    {
                        if (CharacterBankTabSettings.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterBankTabSettings[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[42])
                {
                    for (int i = 0; i < AccountBankTabSettings.Size(); ++i)
                    {
                        if (AccountBankTabSettings.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            AccountBankTabSettings[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[49])
                {
                    data.WritePackedGuid(FarsightObject);
                }
                if (changesMask[50])
                {
                    data.WritePackedGuid(SummonedBattlePetGUID);
                }
                if (changesMask[51])
                {
                    data.WriteUInt64(Coinage);
                }
                if (changesMask[52])
                {
                    data.WriteUInt64(AccountBankCoinage);
                }
                if (changesMask[53])
                {
                    data.WriteUInt32(XP);
                }
                if (changesMask[54])
                {
                    data.WriteUInt32(NextLevelXP);
                }
                if (changesMask[55])
                {
                    data.WriteInt32(TrialXP);
                }
                if (changesMask[56])
                {
                    Skill.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[57])
                {
                    data.WriteUInt32(CharacterPoints);
                }
                if (changesMask[58])
                {
                    data.WriteUInt32(MaxTalentTiers);
                }
                if (changesMask[59])
                {
                    data.WriteUInt32(TrackCreatureMask);
                }
                if (changesMask[60])
                {
                    data.WriteFloat(MainhandExpertise);
                }
                if (changesMask[61])
                {
                    data.WriteFloat(OffhandExpertise);
                }
                if (changesMask[62])
                {
                    data.WriteFloat(RangedExpertise);
                }
                if (changesMask[63])
                {
                    data.WriteFloat(CombatRatingExpertise);
                }
                if (changesMask[64])
                {
                    data.WriteFloat(BlockPercentage);
                }
                if (changesMask[65])
                {
                    data.WriteFloat(DodgePercentage);
                }
                if (changesMask[66])
                {
                    data.WriteFloat(DodgePercentageFromAttribute);
                }
                if (changesMask[67])
                {
                    data.WriteFloat(ParryPercentage);
                }
                if (changesMask[68])
                {
                    data.WriteFloat(ParryPercentageFromAttribute);
                }
                if (changesMask[69])
                {
                    data.WriteFloat(CritPercentage);
                }
            }
            if (changesMask[70])
            {
                if (changesMask[71])
                {
                    data.WriteFloat(RangedCritPercentage);
                }
                if (changesMask[72])
                {
                    data.WriteFloat(OffhandCritPercentage);
                }
                if (changesMask[73])
                {
                    data.WriteFloat(SpellCritPercentage);
                }
                if (changesMask[74])
                {
                    data.WriteUInt32(ShieldBlock);
                }
                if (changesMask[75])
                {
                    data.WriteFloat(ShieldBlockCritPercentage);
                }
                if (changesMask[76])
                {
                    data.WriteFloat(Mastery);
                }
                if (changesMask[77])
                {
                    data.WriteFloat(Speed);
                }
                if (changesMask[78])
                {
                    data.WriteFloat(Avoidance);
                }
                if (changesMask[79])
                {
                    data.WriteFloat(Sturdiness);
                }
                if (changesMask[80])
                {
                    data.WriteInt32(Versatility);
                }
                if (changesMask[81])
                {
                    data.WriteFloat(VersatilityBonus);
                }
                if (changesMask[82])
                {
                    data.WriteFloat(PvpPowerDamage);
                }
                if (changesMask[83])
                {
                    data.WriteFloat(PvpPowerHealing);
                }
                if (changesMask[84])
                {
                    BitVectors.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[85])
                {
                    data.WriteInt32(ModHealingDonePos);
                }
                if (changesMask[86])
                {
                    data.WriteFloat(ModHealingPercent);
                }
                if (changesMask[87])
                {
                    data.WriteFloat(ModPeriodicHealingDonePercent);
                }
                if (changesMask[88])
                {
                    data.WriteFloat(ModSpellPowerPercent);
                }
                if (changesMask[89])
                {
                    data.WriteFloat(ModResiliencePercent);
                }
                if (changesMask[90])
                {
                    data.WriteFloat(OverrideSpellPowerByAPPercent);
                }
                if (changesMask[91])
                {
                    data.WriteFloat(OverrideAPBySpellPowerPercent);
                }
                if (changesMask[92])
                {
                    data.WriteInt32(ModTargetResistance);
                }
                if (changesMask[93])
                {
                    data.WriteInt32(ModTargetPhysicalResistance);
                }
                if (changesMask[94])
                {
                    data.WriteUInt32(LocalFlags);
                }
                if (changesMask[95])
                {
                    data.WriteUInt8(GrantableLevels);
                }
                if (changesMask[96])
                {
                    data.WriteUInt8(MultiActionBars);
                }
                if (changesMask[97])
                {
                    data.WriteUInt8(LifetimeMaxRank);
                }
                if (changesMask[98])
                {
                    data.WriteUInt8(NumRespecs);
                }
                if (changesMask[99])
                {
                    data.WriteUInt32(PvpMedals);
                }
                if (changesMask[100])
                {
                    data.WriteUInt16(TodayHonorableKills);
                }
                if (changesMask[101])
                {
                    data.WriteUInt16(YesterdayHonorableKills);
                }
            }
            if (changesMask[102])
            {
                if (changesMask[103])
                {
                    data.WriteUInt32(LifetimeHonorableKills);
                }
                if (changesMask[104])
                {
                    data.WriteUInt32(WatchedFactionIndex);
                }
                if (changesMask[105])
                {
                    data.WriteUInt32(MaxLevel);
                }
                if (changesMask[106])
                {
                    data.WriteInt32(ScalingPlayerLevelDelta);
                }
                if (changesMask[107])
                {
                    data.WriteInt32(MaxCreatureScalingLevel);
                }
                if (changesMask[108])
                {
                    data.WriteUInt32(PetSpellPower);
                }
                if (changesMask[109])
                {
                    data.WriteFloat(UiHitModifier);
                }
                if (changesMask[110])
                {
                    data.WriteFloat(UiSpellHitModifier);
                }
                if (changesMask[111])
                {
                    data.WriteInt32(HomeRealmTimeOffset);
                }
                if (changesMask[112])
                {
                    data.WriteFloat(ModPetHaste);
                }
                if (changesMask[113])
                {
                    data.WriteInt8(JailersTowerLevelMax);
                }
                if (changesMask[114])
                {
                    data.WriteInt8(JailersTowerLevel);
                }
                if (changesMask[115])
                {
                    data.WriteUInt8(LocalRegenFlags);
                }
                if (changesMask[116])
                {
                    data.WriteUInt8(AuraVision);
                }
                if (changesMask[117])
                {
                    data.WriteUInt8(NumBackpackSlots);
                }
                if (changesMask[118])
                {
                    data.WriteUInt32(OverrideSpellsID);
                }
                if (changesMask[119])
                {
                    data.WriteUInt16(LootSpecID);
                }
                if (changesMask[120])
                {
                    data.WriteUInt32(OverrideZonePVPType);
                }
                if (changesMask[121])
                {
                    data.WriteUInt32(Honor);
                }
                if (changesMask[122])
                {
                    data.WriteUInt32(HonorNextLevel);
                }
                if (changesMask[123])
                {
                    data.WriteInt32(PerksProgramCurrency);
                }
                if (changesMask[124])
                {
                    data.WriteUInt8(NumBankSlots);
                }
                if (changesMask[125])
                {
                    data.WriteUInt8(NumCharacterBankTabs);
                }
                if (changesMask[126])
                {
                    data.WriteUInt8(NumAccountBankTabs);
                }
                if (changesMask[131])
                {
                    data.WriteInt32(UiChromieTimeExpansionID);
                }
                if (changesMask[132])
                {
                    data.WriteInt32(TimerunningSeasonID);
                }
                if (changesMask[133])
                {
                    data.WriteInt32(TransportServerTime);
                }
            }
            if (changesMask[134])
            {
                if (changesMask[135])
                {
                    data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
                }
                if (changesMask[136])
                {
                    data.WriteInt16(DEBUGSoulbindConduitRank);
                }
                if (changesMask[138])
                {
                    data.WriteUInt32(ActiveCombatTraitConfigID);
                }
                if (changesMask[139])
                {
                    data.WriteInt32(ItemUpgradeHighOnehandWeaponItemID);
                }
                if (changesMask[140])
                {
                    data.WriteInt32(ItemUpgradeHighFingerItemID);
                }
                if (changesMask[141])
                {
                    data.WriteFloat(ItemUpgradeHighFingerWatermark);
                }
                if (changesMask[142])
                {
                    data.WriteInt32(ItemUpgradeHighTrinketItemID);
                }
                if (changesMask[143])
                {
                    data.WriteFloat(ItemUpgradeHighTrinketWatermark);
                }
                if (changesMask[144])
                {
                    data.WriteUInt64(LootHistoryInstanceID);
                }
                if (changesMask[146])
                {
                    data.WriteUInt8(RequiredMountCapabilityFlags);
                }
            }
            if (changesMask[102])
            {
                data.WriteBits(QuestSession.HasValue(), 1);
            }
            if (changesMask[134])
            {
                data.WriteBits(PetStable.HasValue(), 1);
                data.WriteBits(WalkInData.HasValue(), 1);
                data.WriteBits(DelveData.HasValue(), 1);
                data.WriteBits(ChallengeModeData.HasValue(), 1);
            }
            data.FlushBits();
            if (changesMask[102])
            {
                if (changesMask[127])
                {
                    ResearchHistory.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[129])
                {
                    if (QuestSession.HasValue())
                    {
                        QuestSession.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
                if (changesMask[128])
                {
                    FrozenPerksVendorItem.GetValue().Write(data);
                }
                if (changesMask[130])
                {
                    Field_1410.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
            if (changesMask[134])
            {
                if (changesMask[137])
                {
                    DungeonScore.GetValue().Write(data);
                }
                if (changesMask[145])
                {
                    if (PetStable.HasValue())
                    {
                        PetStable.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
                if (changesMask[147])
                {
                    if (WalkInData.HasValue())
                    {
                        WalkInData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
                if (changesMask[148])
                {
                    if (DelveData.HasValue())
                    {
                        DelveData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
                if (changesMask[149])
                {
                    if (ChallengeModeData.HasValue())
                    {
                        ChallengeModeData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[150])
            {
                for (int i = 0; i < 105; ++i)
                {
                    if (changesMask[151 + i])
                    {
                        data.WritePackedGuid(InvSlots[i]);
                    }
                }
            }
            if (changesMask[256])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[257 + i])
                    {
                        RestInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[259])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[260 + i])
                    {
                        data.WriteInt32(ModDamageDonePos[i]);
                    }
                    if (changesMask[267 + i])
                    {
                        data.WriteInt32(ModDamageDoneNeg[i]);
                    }
                    if (changesMask[274 + i])
                    {
                        data.WriteFloat(ModDamageDonePercent[i]);
                    }
                    if (changesMask[281 + i])
                    {
                        data.WriteFloat(ModHealingDonePercent[i]);
                    }
                }
            }
            if (changesMask[288])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[289 + i])
                    {
                        data.WriteFloat(WeaponDmgMultipliers[i]);
                    }
                    if (changesMask[292 + i])
                    {
                        data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
                    }
                }
            }
            if (changesMask[295])
            {
                for (int i = 0; i < 12; ++i)
                {
                    if (changesMask[296 + i])
                    {
                        data.WriteUInt32(BuybackPrice[i]);
                    }
                    if (changesMask[308 + i])
                    {
                        data.WriteInt64(BuybackTimestamp[i]);
                    }
                }
            }
            if (changesMask[320])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (changesMask[321 + i])
                    {
                        data.WriteUInt32(CombatRatings[i]);
                    }
                }
            }
            if (changesMask[353])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[354 + i])
                    {
                        data.WriteUInt32(NoReagentCostMask[i]);
                    }
                }
            }
            if (changesMask[358])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[359 + i])
                    {
                        data.WriteUInt32(ProfessionSkillLine[i]);
                    }
                }
            }
            if (changesMask[361])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[362 + i])
                    {
                        data.WriteUInt32(BagSlotFlags[i]);
                    }
                }
            }
            if (changesMask[367])
            {
                for (int i = 0; i < 17; ++i)
                {
                    if (changesMask[368 + i])
                    {
                        data.WriteFloat(ItemUpgradeHighWatermark[i]);
                    }
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BackpackAutoSortDisabled);
            ClearChangesMask(BackpackSellJunkDisabled);
            ClearChangesMask(BankAutoSortDisabled);
            ClearChangesMask(SortBagsRightToLeft);
            ClearChangesMask(InsertItemsLeftToRight);
            ClearChangesMask(HasPerksProgramPendingReward);
            ClearChangesMask(ResearchSites);
            ClearChangesMask(ResearchSiteProgress);
            ClearChangesMask(Research);
            ClearChangesMask(KnownTitles);
            ClearChangesMask(CharacterDataElements);
            ClearChangesMask(AccountDataElements);
            ClearChangesMask(DailyQuestsCompleted);
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
            ClearChangesMask(SpellPctModByLabel);
            ClearChangesMask(SpellFlatModByLabel);
            ClearChangesMask(MawPowers);
            ClearChangesMask(MultiFloorExploration);
            ClearChangesMask(RecipeProgression);
            ClearChangesMask(ReplayedQuests);
            ClearChangesMask(TaskQuests);
            ClearChangesMask(DisabledSpells);
            ClearChangesMask(PersonalCraftingOrderCounts);
            ClearChangesMask(NpcCraftingOrders);
            ClearChangesMask(CategoryCooldownMods);
            ClearChangesMask(WeeklySpellUses);
            ClearChangesMask(TrackedCollectableSources);
            ClearChangesMask(PvpInfo);
            ClearChangesMask(CharacterRestrictions);
            ClearChangesMask(TraitConfigs);
            ClearChangesMask(CraftingOrders);
            ClearChangesMask(CharacterBankTabSettings);
            ClearChangesMask(AccountBankTabSettings);
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
            ClearChangesMask(Field_1410);
            ClearChangesMask(QuestSession);
            ClearChangesMask(UiChromieTimeExpansionID);
            ClearChangesMask(TimerunningSeasonID);
            ClearChangesMask(TransportServerTime);
            ClearChangesMask(WeeklyRewardsPeriodSinceOrigin);
            ClearChangesMask(DEBUGSoulbindConduitRank);
            ClearChangesMask(DungeonScore);
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
            ClearChangesMask(ChallengeModeData);
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

    public struct GameObjectAssistActionData : IEquatable<GameObjectAssistActionData>
    {
        public string PlayerName;
        public string MonsterName;
        public uint VirtualRealmAddress;
        public byte Sex;
        public long Time;
        public int DelveTier;

        public void WriteCreate(WorldPacket data, GameObject owner, Player receiver)
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, GameObject owner, Player receiver)
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

    public class GameObjectFieldData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.GameObject, 26)
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
        public UpdateField<Quaternion> ParentRotation = new(0, 13);
        public UpdateField<uint> FactionTemplate = new(0, 14);
        public UpdateField<sbyte> State = new(0, 15);
        public UpdateField<sbyte> TypeID = new(0, 16);
        public UpdateField<byte> PercentHealth = new(0, 17);
        public UpdateField<uint> ArtKit = new(0, 18);
        public UpdateField<uint> CustomParam = new(0, 19);
        public UpdateField<uint> Level = new(0, 20);
        public UpdateField<uint> AnimGroupInstance = new(0, 21);
        public UpdateField<uint> UiWidgetItemID = new(0, 22);
        public UpdateField<uint> UiWidgetItemQuality = new(0, 23);
        public UpdateField<uint> UiWidgetItemUnknown1000 = new(0, 24);
        public OptionalUpdateField<GameObjectAssistActionData> AssistActionData = new(0, 25);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
        {
            data.WriteUInt32(DisplayID);
            data.WriteUInt32(SpellVisualID);
            data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimID(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimKitID(this, owner, receiver));
            var stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, owner, receiver);
            data.WriteInt32(stateWorldEffectIDs.Count);
            data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, owner, receiver));
            for (int i = 0; i < stateWorldEffectIDs.Count; ++i)
            {
                data.WriteUInt32(stateWorldEffectIDs[i]);
            }
            data.WritePackedGuid(CreatedBy);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GetViewerDependentGameObjectFlags(this, owner, receiver));
            Quaternion rotation = ParentRotation;
            data.WriteFloat(rotation.X);
            data.WriteFloat(rotation.Y);
            data.WriteFloat(rotation.Z);
            data.WriteFloat(rotation.W);
            data.WriteUInt32(FactionTemplate);
            data.WriteInt8(GetViewerDependentGameObjectState(this, owner, receiver));
            data.WriteInt8(TypeID);
            data.WriteUInt8(PercentHealth);
            data.WriteUInt32(ArtKit);
            data.WriteInt32(EnableDoodadSets.Size());
            data.WriteUInt32(CustomParam);
            data.WriteUInt32(Level);
            data.WriteUInt32(AnimGroupInstance);
            data.WriteUInt32(UiWidgetItemID);
            data.WriteUInt32(UiWidgetItemQuality);
            data.WriteUInt32(UiWidgetItemUnknown1000);
            data.WriteInt32(WorldEffects.Size());
            for (int i = 0; i < EnableDoodadSets.Size(); ++i)
            {
                data.WriteInt32(EnableDoodadSets[i]);
            }
            for (int i = 0; i < WorldEffects.Size(); ++i)
            {
                data.WriteInt32(WorldEffects[i]);
            }
            data.WriteBits(AssistActionData.HasValue(), 1);
            data.FlushBits();
            if (AssistActionData.HasValue())
            {
                AssistActionData.GetValue().WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, GameObject owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlock(0), 26);

            List<uint> stateWorldEffectIDs;

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    stateWorldEffectIDs = GetViewerDependentStateWorldEffectIDs(this, owner, receiver);
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
                    data.WriteUInt32(GetViewerDependentStateSpellVisualID(this, owner, receiver));
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimID(this, owner, receiver));
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(GetViewerDependentSpawnTrackingStateAnimKitID(this, owner, receiver));
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(GetViewerDependentStateWorldEffectsQuestObjectiveID(this, owner, receiver));
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
                    data.WriteUInt32(GetViewerDependentGameObjectFlags(this, owner, receiver));
                }
                if (changesMask[13])
                {
                    data.WriteFloat(((Quaternion)ParentRotation).X);
                    data.WriteFloat(((Quaternion)ParentRotation).Y);
                    data.WriteFloat(((Quaternion)ParentRotation).Z);
                    data.WriteFloat(((Quaternion)ParentRotation).W);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(FactionTemplate);
                }
                if (changesMask[15])
                {
                    data.WriteInt8(GetViewerDependentGameObjectState(this, owner, receiver));
                }
                if (changesMask[16])
                {
                    data.WriteInt8(TypeID);
                }
                if (changesMask[17])
                {
                    data.WriteUInt8(PercentHealth);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(ArtKit);
                }
                if (changesMask[19])
                {
                    data.WriteUInt32(CustomParam);
                }
                if (changesMask[20])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[21])
                {
                    data.WriteUInt32(AnimGroupInstance);
                }
                if (changesMask[22])
                {
                    data.WriteUInt32(UiWidgetItemID);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(UiWidgetItemQuality);
                }
                if (changesMask[24])
                {
                    data.WriteUInt32(UiWidgetItemUnknown1000);
                }
                data.WriteBits(AssistActionData.HasValue(), 1);
                data.FlushBits();
                if (changesMask[25])
                {
                    if (AssistActionData.HasValue())
                    {
                        AssistActionData.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
            ClearChangesMask(UiWidgetItemUnknown1000);
            ClearChangesMask(AssistActionData);
            _changesMask.ResetAll();
        }

        uint GetViewerDependentGameObjectFlags(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            uint flags = gameObjectData.Flags;
            if (gameObject.GetGoType() == GameObjectTypes.Chest)
                if (gameObject.GetGoInfo().Chest.usegrouplootrules != 0 && !gameObject.IsLootAllowedFor(receiver))
                    flags |= (uint)(GameObjectFlags.Locked | GameObjectFlags.NotSelectable);

            return flags;
        }

        sbyte GetViewerDependentGameObjectState(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            return (sbyte)gameObject.GetGoStateFor(receiver.GetGUID());
        }

        List<uint> GetViewerDependentStateWorldEffectIDs(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            List<uint> stateWorldEffects = gameObjectData.StateWorldEffectIDs;

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateWorldEffects = spawnTrackingStateData.StateWorldEffects;

            return stateWorldEffects;
        }

        uint GetViewerDependentStateSpellVisualID(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            uint stateSpellVisual = gameObjectData.StateSpellVisualID;

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateSpellVisual = spawnTrackingStateData.StateSpellVisualId.GetValueOrDefault(0);

            return stateSpellVisual;
        }

        uint GetViewerDependentSpawnTrackingStateAnimID(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            uint stateAnimId = Global.DB2Mgr.GetEmptyAnimStateID();

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateAnimId = spawnTrackingStateData.StateAnimId.GetValueOrDefault((ushort)stateAnimId);

            return stateAnimId;
        }

        uint GetViewerDependentSpawnTrackingStateAnimKitID(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            uint stateAnimKitId = gameObjectData.SpawnTrackingStateAnimKitID;

            var spawnTrackingStateData = gameObject.GetSpawnTrackingStateDataForPlayer(receiver);
            if (spawnTrackingStateData != null)
                stateAnimKitId = spawnTrackingStateData.StateAnimKitId.GetValueOrDefault(0);

            return stateAnimKitId;
        }

        uint GetViewerDependentStateWorldEffectsQuestObjectiveID(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
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

    public class DynamicObjectData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.DynamicObject, 7)
    {
        public UpdateField<ObjectGuid> Caster = new(0, 1);
        public UpdateField<byte> Type = new(0, 2);
        public UpdateField<SpellCastVisualField> SpellVisual = new(0, 3);
        public UpdateField<uint> SpellID = new(0, 4);
        public UpdateField<float> Radius = new(0, 5);
        public UpdateField<uint> CastTime = new(0, 6);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
        {
            data.WritePackedGuid(Caster);
            data.WriteUInt8(Type);
            ((SpellCastVisualField)SpellVisual).WriteCreate(data, owner, receiver);
            data.WriteUInt32(SpellID);
            data.WriteFloat(Radius);
            data.WriteUInt32(CastTime);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, DynamicObject owner, Player receiver)
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
                    ((SpellCastVisualField)SpellVisual).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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

    public class CorpseData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Corpse, 33)
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

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Corpse owner, Player receiver)
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
                Customizations[i].WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Corpse owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Corpse owner, Player receiver)
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
                            Customizations[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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

    public class ScaleCurve() : HasChangesMask(7)
    {
        public UpdateField<bool> OverrideActive = new(0, 1);
        public UpdateField<uint> StartTimeOffset = new(0, 2);
        public UpdateField<uint> ParameterCurve = new(0, 3);
        public UpdateFieldArray<Vector2> Points = new(2, 4, 5);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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
            data.FlushBits();
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

    public class VisualAnim() : HasChangesMask(0, TypeId.AreaTrigger, 5)
    {
        public UpdateField<bool> IsDecay = new(0, 1);
        public UpdateField<int> AnimationDataID = new(0, 2);
        public UpdateField<uint> AnimKitID = new(0, 3);
        public UpdateField<uint> AnimProgress = new(0, 4);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteInt32(AnimationDataID);
            data.WriteUInt32(AnimKitID);
            data.WriteUInt32(AnimProgress);
            data.WriteBit(IsDecay);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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
                if (changesMask[2])
                {
                    data.WriteInt32(AnimationDataID);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(AnimKitID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(AnimProgress);
                }
            }
            data.FlushBits();
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

    public struct ForceSetAreaTriggerPositionAndRotation : IEquatable<ForceSetAreaTriggerPositionAndRotation>
    {
        public ObjectGuid TriggerGUID;
        public Vector3 Position;
        public Quaternion Rotation;

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WritePackedGuid(TriggerGUID);
            data.WriteVector3(Position);
            data.WriteFloat(Rotation.X);
            data.WriteFloat(Rotation.Y);
            data.WriteFloat(Rotation.Z);
            data.WriteFloat(Rotation.W);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
        {
            data.WritePackedGuid(TriggerGUID);
            data.WriteVector3(Position);
            data.WriteFloat(Rotation.X);
            data.WriteFloat(Rotation.Y);
            data.WriteFloat(Rotation.Z);
            data.WriteFloat(Rotation.W);
        }

        public bool Equals(ForceSetAreaTriggerPositionAndRotation right)
        {
            return TriggerGUID == right.TriggerGUID
                && Position == right.Position
                && Rotation == right.Rotation;
        }
    }

    public class AreaTriggerSplineCalculator() : HasChangesMask(3)
    {
        public UpdateField<bool> Catmullrom = new(0, 1);
        public DynamicUpdateField<Vector3> Points = new(0, 2);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteBits(Points.Size(), 16);
            data.WriteBit(Catmullrom);
            for (int i = 0; i < Points.Size(); ++i)
            {
                data.WriteVector3(Points[i]);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Catmullrom);
            ClearChangesMask(Points);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerOrbit() : HasChangesMask(7)
    {
        public UpdateField<bool> CounterClockwise = new(0, 1);
        public UpdateField<Vector3> Center = new(0, 2);
        public UpdateField<float> Radius = new(0, 3);
        public UpdateField<float> InitialAngle = new(0, 4);
        public UpdateField<float> BlendFromRadius = new(0, 5);
        public UpdateField<int> ExtraTimeForBlending = new(0, 6);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteVector3(Center);
            data.WriteFloat(Radius);
            data.WriteFloat(InitialAngle);
            data.WriteFloat(BlendFromRadius);
            data.WriteInt32(ExtraTimeForBlending);
            data.WriteBit(CounterClockwise);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerMovementScript() : HasChangesMask(4)
    {
        public UpdateField<int> SpellScriptID = new(0, 1);
        public UpdateField<Vector3> Center = new(0, 2);
        public UpdateField<uint> CreationTime = new(0, 3);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteInt32(SpellScriptID);
            data.WriteVector3(Center);
            data.WriteUInt32(CreationTime);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerSphere() : HasChangesMask(3)
    {
        public UpdateField<float> Radius = new(0, 1);
        public UpdateField<float> RadiusTarget = new(0, 2);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteFloat(Radius);
            data.WriteFloat(RadiusTarget);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerBox() : HasChangesMask(3)
    {
        public UpdateField<Vector3> Extents = new(0, 1);
        public UpdateField<Vector3> ExtentsTarget = new(0, 2);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteVector3(Extents);
            data.WriteVector3(ExtentsTarget);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerPolygon() : HasChangesMask(5)
    {
        public DynamicUpdateField<Vector2> Vertices = new(0, 1);
        public DynamicUpdateField<Vector2> VerticesTarget = new(0, 2);
        public UpdateField<float> Height = new(0, 3);
        public UpdateField<float> HeightTarget = new(0, 4);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerCylinder() : HasChangesMask(7)
    {
        public UpdateField<float> Radius = new(0, 1);
        public UpdateField<float> RadiusTarget = new(0, 2);
        public UpdateField<float> Height = new(0, 3);
        public UpdateField<float> HeightTarget = new(0, 4);
        public UpdateField<float> LocationZOffset = new(0, 5);
        public UpdateField<float> LocationZOffsetTarget = new(0, 6);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteFloat(Radius);
            data.WriteFloat(RadiusTarget);
            data.WriteFloat(Height);
            data.WriteFloat(HeightTarget);
            data.WriteFloat(LocationZOffset);
            data.WriteFloat(LocationZOffsetTarget);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerDisk() : HasChangesMask(9)
    {
        public UpdateField<float> InnerRadius = new(0, 1);
        public UpdateField<float> InnerRadiusTarget = new(0, 2);
        public UpdateField<float> OuterRadius = new(0, 3);
        public UpdateField<float> OuterRadiusTarget = new(0, 4);
        public UpdateField<float> Height = new(0, 5);
        public UpdateField<float> HeightTarget = new(0, 6);
        public UpdateField<float> LocationZOffset = new(0, 7);
        public UpdateField<float> LocationZOffsetTarget = new(0, 8);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
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

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerBoundedPlane() : HasChangesMask(3)
    {
        public UpdateField<Vector2> Extents = new(0, 1);
        public UpdateField<Vector2> ExtentsTarget = new(0, 2);

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteVector2(Extents);
            data.WriteVector2(ExtentsTarget);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, AreaTrigger owner, Player receiver)
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

    public class AreaTriggerFieldData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.AreaTrigger, 36)
    {
        public UpdateField<ScaleCurve> OverrideScaleCurve = new(0, 1);
        public UpdateField<ScaleCurve> ExtraScaleCurve = new(0, 2);
        public UpdateField<ScaleCurve> OverrideMoveCurveX = new(0, 3);
        public UpdateField<ScaleCurve> OverrideMoveCurveY = new(0, 4);
        public UpdateField<ScaleCurve> OverrideMoveCurveZ = new(0, 5);
        public UpdateField<ObjectGuid> Caster = new(0, 6);
        public UpdateField<uint> Duration = new(0, 7);
        public UpdateField<uint> TimeToTarget = new(0, 8);
        public UpdateField<uint> TimeToTargetScale = new(0, 9);
        public UpdateField<uint> TimeToTargetExtraScale = new(0, 10);
        public UpdateField<uint> TimeToTargetPos = new(0, 11);                                 // Linked to m_overrideMoveCurve
        public UpdateField<uint> SpellID = new(0, 12);
        public UpdateField<uint> SpellForVisuals = new(0, 13);
        public UpdateField<SpellCastVisualField> SpellVisual = new(0, 14);
        public UpdateField<float> BoundsRadius2D = new(0, 15);
        public UpdateField<uint> DecalPropertiesID = new(0, 16);
        public UpdateField<ObjectGuid> CreatingEffectGUID = new(0, 17);
        public UpdateField<ObjectGuid> OrbitPathTarget = new(0, 18);
        public UpdateField<Vector3> RollPitchYaw = new(0, 19);
        public UpdateField<int> PositionalSoundKitID = new(0, 20);
        public UpdateField<uint> MovementStartTime = new(0, 21);
        public UpdateField<uint> CreationTime = new(0, 22);
        public UpdateField<float> ZOffset = new(0, 23);
        public OptionalUpdateField<Vector3> TargetRollPitchYaw = new(0, 24);
        public UpdateField<uint> Flags = new(0, 25);
        public UpdateField<VisualAnim> VisualAnim = new(0, 26);
        public UpdateField<uint> ScaleCurveId = new(0, 27);
        public UpdateField<uint> FacingCurveId = new(0, 28);
        public UpdateField<uint> MorphCurveId = new(0, 29);
        public UpdateField<uint> MoveCurveId = new(0, 30);
        public UpdateField<float> Facing = new(0, 31);
        public OptionalUpdateField<ForceSetAreaTriggerPositionAndRotation> ForcedPositionAndRotation = new(32, 33);
        public UpdateField<int> PathType = new(32, 34);
        public UpdateField<byte> ShapeType = new(32, 35);
        public VariantUpdateField PathData = new(32, 34);//, AreaTriggerSplineCalculator, AreaTriggerOrbit, AreaTriggerMovementScript);
        public VariantUpdateField ShapeData = new(32, 35);//, AreaTriggerSphere, AreaTriggerBox, AreaTriggerPolygon, AreaTriggerCylinder, AreaTriggerDisk, AreaTriggerBoundedPlane);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
        {
            OverrideScaleCurve.GetValue().WriteCreate(data, owner, receiver);
            data.WritePackedGuid(Caster);
            data.WriteUInt32(Duration);
            data.WriteUInt32(TimeToTarget);
            data.WriteUInt32(TimeToTargetScale);
            data.WriteUInt32(TimeToTargetExtraScale);
            data.WriteUInt32(TimeToTargetPos);
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellForVisuals);
            SpellVisual.GetValue().WriteCreate(data, owner, receiver);
            data.WriteFloat(BoundsRadius2D);
            data.WriteUInt32(DecalPropertiesID);
            data.WritePackedGuid(CreatingEffectGUID);
            data.WritePackedGuid(OrbitPathTarget);
            data.WriteVector3(RollPitchYaw);
            data.WriteInt32(PositionalSoundKitID);
            data.WriteUInt32(MovementStartTime);
            data.WriteUInt32(CreationTime);
            data.WriteFloat(ZOffset);
            data.WriteUInt32(Flags);
            data.WriteUInt32(ScaleCurveId);
            data.WriteUInt32(FacingCurveId);
            data.WriteUInt32(MorphCurveId);
            data.WriteUInt32(MoveCurveId);
            data.WriteFloat(Facing);
            data.WriteInt32(PathType);
            data.WriteUInt8(ShapeType);
            if (PathType == 3)
            {
                PathData.Get<AreaTriggerMovementScript>().WriteCreate(data, owner, receiver);
            }
            if (ShapeType == 0)
            {
                ShapeData.Get<AreaTriggerSphere>().WriteCreate(data, owner, receiver);
            }
            if (ShapeType == 1)
            {
                ShapeData.Get<AreaTriggerBox>().WriteCreate(data, owner, receiver);
            }
            if (ShapeType == 3)
            {
                ShapeData.Get<AreaTriggerPolygon>().WriteCreate(data, owner, receiver);
            }
            if (ShapeType == 4)
            {
                ShapeData.Get<AreaTriggerCylinder>().WriteCreate(data, owner, receiver);
            }
            if (ShapeType == 7)
            {
                ShapeData.Get<AreaTriggerDisk>().WriteCreate(data, owner, receiver);
            }
            if (ShapeType == 8)
            {
                ShapeData.Get<AreaTriggerBoundedPlane>().WriteCreate(data, owner, receiver);
            }
            ExtraScaleCurve.GetValue().WriteCreate(data, owner, receiver);
            data.FlushBits();
            data.WriteBits(TargetRollPitchYaw.HasValue(), 1);
            data.WriteBits(ForcedPositionAndRotation.HasValue(), 1);
            OverrideMoveCurveX.GetValue().WriteCreate(data, owner, receiver);
            if (TargetRollPitchYaw.HasValue())
            {
                data.WriteVector3(TargetRollPitchYaw);
            }
            if (ForcedPositionAndRotation.HasValue())
            {
                ForcedPositionAndRotation.GetValue().WriteCreate(data, owner, receiver);
            }
            OverrideMoveCurveY.GetValue().WriteCreate(data, owner, receiver);
            OverrideMoveCurveZ.GetValue().WriteCreate(data, owner, receiver);
            VisualAnim.GetValue().WriteCreate(data, owner, receiver);
            if (PathType == 0)
            {
                PathData.Get<AreaTriggerSplineCalculator>().WriteCreate(data, owner, receiver);
            }
            if (PathType == 1)
            {
                PathData.Get<AreaTriggerOrbit>().WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AreaTrigger owner, Player receiver)
        {
            data.WriteUInt32(changesMask.GetBlock(0));
            data.WriteBits(changesMask.GetBlock(1), 4);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    OverrideScaleCurve.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(Caster);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(Duration);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(TimeToTarget);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(TimeToTargetScale);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(TimeToTargetExtraScale);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(TimeToTargetPos);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(SpellID);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(SpellForVisuals);
                }
                if (changesMask[14])
                {
                    SpellVisual.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[15])
                {
                    data.WriteFloat(BoundsRadius2D);
                }
                if (changesMask[16])
                {
                    data.WriteUInt32(DecalPropertiesID);
                }
                if (changesMask[17])
                {
                    data.WritePackedGuid(CreatingEffectGUID);
                }
                if (changesMask[18])
                {
                    data.WritePackedGuid(OrbitPathTarget);
                }
                if (changesMask[19])
                {
                    data.WriteVector3(RollPitchYaw);
                }
                if (changesMask[20])
                {
                    data.WriteInt32(PositionalSoundKitID);
                }
                if (changesMask[21])
                {
                    data.WriteUInt32(MovementStartTime);
                }
                if (changesMask[22])
                {
                    data.WriteUInt32(CreationTime);
                }
                if (changesMask[23])
                {
                    data.WriteFloat(ZOffset);
                }
                if (changesMask[25])
                {
                    data.WriteUInt32(Flags);
                }
                if (changesMask[27])
                {
                    data.WriteUInt32(ScaleCurveId);
                }
                if (changesMask[28])
                {
                    data.WriteUInt32(FacingCurveId);
                }
                if (changesMask[29])
                {
                    data.WriteUInt32(MorphCurveId);
                }
                if (changesMask[30])
                {
                    data.WriteUInt32(MoveCurveId);
                }
                if (changesMask[31])
                {
                    data.WriteFloat(Facing);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[34])
                {
                    data.WriteInt32(PathType);
                }
                if (changesMask[35])
                {
                    data.WriteUInt8(ShapeType);
                }
                if (changesMask[34])
                {
                    if (PathType == 3)
                    {
                        PathData.Get<AreaTriggerMovementScript>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
                if (changesMask[35])
                {
                    if (ShapeType == 0)
                    {
                        ShapeData.Get<AreaTriggerSphere>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                    if (ShapeType == 1)
                    {
                        ShapeData.Get<AreaTriggerBox>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                    if (ShapeType == 3)
                    {
                        ShapeData.Get<AreaTriggerPolygon>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                    if (ShapeType == 4)
                    {
                        ShapeData.Get<AreaTriggerCylinder>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                    if (ShapeType == 7)
                    {
                        ShapeData.Get<AreaTriggerDisk>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                    if (ShapeType == 8)
                    {
                        ShapeData.Get<AreaTriggerBoundedPlane>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    ExtraScaleCurve.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                data.WriteBits(TargetRollPitchYaw.HasValue(), 1);
            }
            if (changesMask[32])
            {
                data.WriteBits(ForcedPositionAndRotation.HasValue(), 1);
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    OverrideMoveCurveX.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[24])
                {
                    if (TargetRollPitchYaw.HasValue())
                    {
                        data.WriteVector3(TargetRollPitchYaw);
                    }
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    if (ForcedPositionAndRotation.HasValue())
                    {
                        ForcedPositionAndRotation.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[0])
            {
                if (changesMask[4])
                {
                    OverrideMoveCurveY.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[5])
                {
                    OverrideMoveCurveZ.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[26])
                {
                    VisualAnim.GetValue().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[34])
                {
                    if (PathType == 0)
                    {
                        PathData.Get<AreaTriggerSplineCalculator>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                    if (PathType == 1)
                    {
                        PathData.Get<AreaTriggerOrbit>().WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
            ClearChangesMask(Caster);
            ClearChangesMask(Duration);
            ClearChangesMask(TimeToTarget);
            ClearChangesMask(TimeToTargetScale);
            ClearChangesMask(TimeToTargetExtraScale);
            ClearChangesMask(TimeToTargetPos);
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
            ClearChangesMask(ShapeType);
            ClearChangesMask(PathData);
            ClearChangesMask(ShapeData);
            _changesMask.ResetAll();
        }
    }

    public class SceneObjectData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.SceneObject, 5)
    {
        public UpdateField<int> ScriptPackageID = new(0, 1);
        public UpdateField<uint> RndSeedVal = new(0, 2);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 3);
        public UpdateField<uint> SceneType = new(0, 4);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            data.WriteInt32(ScriptPackageID);
            data.WriteUInt32(RndSeedVal);
            data.WritePackedGuid(CreatedBy);
            data.WriteUInt32(SceneType);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, WorldObject owner, Player receiver)
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

    public class ConversationLine : IEquatable<ConversationLine>
    {
        public uint ConversationLineID;
        public uint BroadcastTextID;
        public uint StartTime;
        public uint UiCameraID;
        public byte ActorIndex;
        public byte Flags;
        public byte ChatType;

        public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(BroadcastTextID);
            data.WriteUInt32(GetViewerStartTime(this, owner, receiver));
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
            data.WriteUInt8(ChatType);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(BroadcastTextID);
            data.WriteUInt32(GetViewerStartTime(this, owner, receiver));
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
            data.WriteUInt8(ChatType);
        }

        public uint GetViewerStartTime(ConversationLine conversationLine, Conversation conversation, Player receiver)
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

    public class ConversationActorField : IEquatable<ConversationActorField>
    {
        public uint CreatureID;
        public uint CreatureDisplayInfoID;
        public ObjectGuid ActorGUID;
        public int Id;
        public ConversationActorType Type;
        public uint NoActorObject;

        public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.WritePackedGuid(ActorGUID);
            data.WriteInt32(Id);
            data.WriteBits(Type, 1);
            data.WriteBits(NoActorObject, 1);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
        {
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.WritePackedGuid(ActorGUID);
            data.WriteInt32(Id);
            data.WriteBits(Type, 1);
            data.WriteBits(NoActorObject, 1);
            data.FlushBits();
        }

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

    public class ConversationData() : HasChangesMask((int)EntityFragment.CGObject, TypeId.Conversation, 7)
    {
        public UpdateField<bool> DontPlayBroadcastTextSounds = new(0, 1);
        public UpdateField<List<ConversationLine>> Lines = new(0, 2);
        public DynamicUpdateField<ConversationActorField> Actors = new(0, 3);
        public UpdateField<uint> LastLineEndTime = new(0, 4);
        public UpdateField<uint> Progress = new(0, 5);
        public UpdateField<uint> Flags = new(0, 6);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
        {
            data.WriteInt32(Lines.GetValue().Count);
            data.WriteUInt32(GetViewerLastLineEndTime(this, owner, receiver));
            data.WriteUInt32(Progress);
            for (int i = 0; i < Lines.GetValue().Count; ++i)
            {
                Lines.GetValue()[i].WriteCreate(data, owner, receiver);
            }
            data.WriteBit(DontPlayBroadcastTextSounds);
            data.WriteInt32(Actors.Size());
            data.WriteUInt32(Flags);
            for (int i = 0; i < Actors.Size(); ++i)
            {
                Actors[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Conversation owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 6);

            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteBit(DontPlayBroadcastTextSounds);
                }
                if (changesMask[2])
                {
                    List<ConversationLine> list = Lines;
                    data.WriteBits(list.Count, 32);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        list[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[3])
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
                if (_changesMask[3])
                {
                    for (int i = 0; i < Actors.Size(); ++i)
                    {
                        if (Actors.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Actors[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(GetViewerLastLineEndTime(this, owner, receiver));
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(Progress);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(Flags);
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(DontPlayBroadcastTextSounds);
            ClearChangesMask(Lines);
            ClearChangesMask(Actors);
            ClearChangesMask(LastLineEndTime);
            ClearChangesMask(Progress);
            _changesMask.ResetAll();
        }

        public uint GetViewerLastLineEndTime(ConversationData conversationLineData, Conversation conversation, Player receiver)
        {
            Locale locale = receiver.GetSession().GetSessionDbLocaleIndex();
            return (uint)conversation.GetLastLineEndTime(locale).TotalMilliseconds;
        }
    }

    public class VendorData() : HasChangesMask(2)
    {
        public UpdateField<int> Flags = new(0, 1);

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Creature owner, Player receiver)
        {
            data.WriteInt32(Flags);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Creature owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Creature owner, Player receiver)
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
}
