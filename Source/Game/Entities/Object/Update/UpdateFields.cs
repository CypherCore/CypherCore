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
using Framework.Dynamic;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Entities
{
    public class ObjectFieldData : BaseUpdateData<WorldObject>
    {
        public UpdateField<uint> EntryId = new(0, 1);
        public UpdateField<uint> DynamicFlags = new(0, 2);
        public UpdateField<float> Scale = new(0, 3);

        public ObjectFieldData() : base(0, TypeId.Object, 4) { }

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
            uint unitDynFlags = objectData.DynamicFlags;

            Unit unit = obj.ToUnit();
            if (unit != null)
            {
                Creature creature = obj.ToCreature();
                if (creature != null)
                {
                    if ((unitDynFlags & (uint)UnitDynFlags.Tapped) != 0 && !creature.IsTappedBy(receiver))
                        unitDynFlags &= ~(uint)UnitDynFlags.Tapped;

                    if ((unitDynFlags & (uint)UnitDynFlags.Lootable) != 0 && !receiver.IsAllowedToLoot(creature))
                        unitDynFlags &= ~(uint)UnitDynFlags.Lootable;

                    if ((unitDynFlags & (uint)UnitDynFlags.CanSkin) != 0 && creature.IsSkinnedBy(receiver))
                        unitDynFlags &= ~(uint)UnitDynFlags.CanSkin;
                }

                // unit UNIT_DYNFLAG_TRACK_UNIT should only be sent to caster of SPELL_AURA_MOD_STALKED auras
                if (unitDynFlags.HasAnyFlag((uint)UnitDynFlags.TrackUnit))
                    if (!unit.HasAuraTypeWithCaster(AuraType.ModStalked, receiver.GetGUID()))
                        unitDynFlags &= ~(uint)UnitDynFlags.TrackUnit;
            }
            else
            {
                GameObject gameObject = obj.ToGameObject();
                if (gameObject != null)
                {
                    GameObjectDynamicLowFlags dynFlags = 0;
                    ushort pathProgress = 0xFFFF;
                    switch (gameObject.GetGoType())
                    {
                        case GameObjectTypes.QuestGiver:
                            if (gameObject.ActivateToQuest(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.Activate;
                            break;
                        case GameObjectTypes.Chest:
                            if (gameObject.ActivateToQuest(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight;
                            else if (receiver.IsGameMaster())
                                dynFlags |= GameObjectDynamicLowFlags.Activate;
                            break;
                        case GameObjectTypes.Goober:
                            if (gameObject.ActivateToQuest(receiver))
                            {
                                dynFlags |= GameObjectDynamicLowFlags.Highlight;
                                if (gameObject.GetGoStateFor(receiver.GetGUID()) != GameObjectState.Active)
                                    dynFlags |= GameObjectDynamicLowFlags.Activate;
                            }
                            else if (receiver.IsGameMaster())
                                dynFlags |= GameObjectDynamicLowFlags.Activate;
                            break;
                        case GameObjectTypes.Generic:
                            if (gameObject.ActivateToQuest(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight;
                            break;
                        case GameObjectTypes.Transport:
                        case GameObjectTypes.MapObjTransport:
                            {
                                dynFlags = (GameObjectDynamicLowFlags)((int)unitDynFlags & 0xFFFF);
                                pathProgress = (ushort)((int)unitDynFlags >> 16);
                                break;
                            }
                        case GameObjectTypes.CapturePoint:
                            if (!gameObject.CanInteractWithCapturePoint(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.NoInterract;
                            else
                                dynFlags &= ~GameObjectDynamicLowFlags.NoInterract;
                            break;
                        case GameObjectTypes.GatheringNode:
                            if (gameObject.ActivateToQuest(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle | GameObjectDynamicLowFlags.Highlight;
                            if (gameObject.GetGoStateFor(receiver.GetGUID()) == GameObjectState.Active)
                                dynFlags |= GameObjectDynamicLowFlags.Depleted;
                            break;
                        default:
                            break;
                    }

                    if (!gameObject.MeetsInteractCondition(receiver))
                        dynFlags |= GameObjectDynamicLowFlags.NoInterract;

                    unitDynFlags = ((uint)pathProgress << 16) | (uint)dynFlags;
                }
            }

            return unitDynFlags;
        }
    }

    public class ItemEnchantment : BaseUpdateData<Item>
    {
        public UpdateField<uint> ID = new(0, 1);
        public UpdateField<uint> Duration = new(0, 2);
        public UpdateField<short> Charges = new(0, 3);
        public UpdateField<byte> Unk254 = new(0, 4);
        public UpdateField<byte> Unk254_2 = new(0, 5);

        public ItemEnchantment() : base(5) { }

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(ID);
            data.WriteUInt32(Duration);
            data.WriteInt16(Charges);
            data.WriteUInt8(Unk254);
            data.WriteUInt8(Unk254_2);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 6); //TODO: magic number "6"

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
                    data.WriteUInt8(Unk254);
                }
                if (changesMask[5])
                {
                    data.WriteUInt8(Unk254_2);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ID);
            ClearChangesMask(Duration);
            ClearChangesMask(Charges);
            ClearChangesMask(Unk254);
            ClearChangesMask(Unk254_2);
            _changesMask.ResetAll();
        }
    }

    public class ItemMod
    {
        public uint Value;
        public byte Type;

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(Value);
            data.WriteUInt8(Type);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Item owner, Player receiver)
        {
            data.WriteUInt32(Value);
            data.WriteUInt8(Type);
        }
    }

    public class ItemModList : BaseUpdateData<Item>
    {
        public DynamicUpdateField<ItemMod> Values = new(0, 0);

        public ItemModList() : base(1) { }

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
                if (changesMask[0])
                {
                    if (!ignoreChangesMask)
                        Values.WriteUpdateMask(data, 6);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Values.Size(), data, 6);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
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
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Values);
            _changesMask.ResetAll();
        }
    }

    public class ArtifactPower
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
    }

    public class SocketedGem : BaseUpdateData<Item>
    {
        public UpdateField<uint> ItemId = new(0, 1);
        public UpdateField<byte> Context = new(0, 2);
        public UpdateFieldArray<ushort> BonusListIDs = new(16, 3, 4);

        public SocketedGem() : base(20) { }

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

    public class ItemData : BaseUpdateData<Item>
    {
        public UpdateField<List<uint>> BonusListIDs = new(0, 1);
        public DynamicUpdateField<ArtifactPower> ArtifactPowers = new(0, 2);
        public DynamicUpdateField<SocketedGem> Gems = new(0, 3);
        public UpdateField<ObjectGuid> Owner = new(0, 4);
        public UpdateField<ObjectGuid> ContainedIn = new(0, 5);
        public UpdateField<ObjectGuid> Creator = new(0, 6);
        public UpdateField<ObjectGuid> GiftCreator = new(0, 7);
        public UpdateField<uint> StackCount = new(0, 8);
        public UpdateField<uint> Expiration = new(0, 9);
        public UpdateField<uint> DynamicFlags = new(0, 10);
        public UpdateField<int> PropertySeed = new(0, 11);
        public UpdateField<int> RandomPropertiesID =new(0, 12);
        public UpdateField<uint> Durability = new(0, 13);
        public UpdateField<uint> MaxDurability = new(0, 14);
        public UpdateField<uint> CreatePlayedTime = new(0, 15);
        public UpdateField<int> Context = new(0, 16);
        public UpdateField<long> CreateTime = new(0, 17);
        public UpdateField<ulong> ArtifactXP = new(0, 18);
        public UpdateField<byte> ItemAppearanceModID = new(0, 19);
        public UpdateField<ItemModList> Modifiers = new(0, 20);
        public UpdateField<uint> DynamicFlags2 = new(0, 21);
        public UpdateField<ushort> DEBUGItemLevel = new(0, 22);
        public UpdateFieldArray<int> SpellCharges = new(5, 23, 24);
        public UpdateFieldArray<ItemEnchantment> Enchantment = new(13, 29, 30);

        public ItemData() : base(0, TypeId.Item, 41) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            List<uint> bonusList = BonusListIDs;
            data.WriteInt32(bonusList.Count);
            for (int i = 0; i < bonusList.Count; ++i)
                data.WriteUInt32(bonusList[i]);

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
            data.WriteInt32(PropertySeed);
            data.WriteInt32(RandomPropertiesID);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt32(Durability);
                data.WriteUInt32(MaxDurability);
            }
            data.WriteUInt32(CreatePlayedTime);
            data.WriteInt32(Context);
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
                data.WriteUInt32(DynamicFlags2);
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
            ((ItemModList)Modifiers).WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(41, new uint[] { 0xE0139CFFu, 0x000007FFu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(41, new uint[] { 0x1FEC6300u, 0x00000000u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(41, new[] { 0xE0139CFFu, 0x000007FFu });
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
                    List<uint> bonusList = BonusListIDs;
                    data.WriteBits(bonusList.Count, 32);
                    for (int i = 0; i < bonusList.Count; ++i)
                        data.WriteUInt32(bonusList[i]);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        ArtifactPowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ArtifactPowers.Size(), data);
                }
                if (changesMask[3])
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
                if (changesMask[2])
                {
                    for (int i = 0; i < ArtifactPowers.Size(); ++i)
                    {
                        if (ArtifactPowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ArtifactPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < Gems.Size(); ++i)
                    {
                        if (Gems.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Gems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(Owner);
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(ContainedIn);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(Creator);
                }
                if (changesMask[7])
                {
                    data.WritePackedGuid(GiftCreator);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(StackCount);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(Expiration);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(DynamicFlags);
                }
                if (changesMask[11])
                {
                    data.WriteInt32(PropertySeed);
                }
                if (changesMask[12])
                {
                    data.WriteInt32(RandomPropertiesID);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(Durability);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(MaxDurability);
                }
                if (changesMask[15])
                {
                    data.WriteUInt32(CreatePlayedTime);
                }
                if (changesMask[16])
                {
                    data.WriteInt32(Context);
                }
                if (changesMask[17])
                {
                    data.WriteInt64(CreateTime);
                }
                if (changesMask[18])
                {
                    data.WriteUInt64(ArtifactXP);
                }
                if (changesMask[19])
                {
                    data.WriteUInt8(ItemAppearanceModID);
                }
                if (changesMask[21])
                {
                    data.WriteUInt32(DynamicFlags2);
                }
                if (changesMask[22])
                {
                    data.WriteUInt16(DEBUGItemLevel);
                }
                if (changesMask[20])
                {
                    ((ItemModList)Modifiers).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
            if (changesMask[23])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[24 + i])
                    {
                        data.WriteInt32(SpellCharges[i]);
                    }
                }
            }
            if (changesMask[29])
            {
                for (int i = 0; i < 13; ++i)
                {
                    if (changesMask[30 + i])
                    {
                        Enchantment[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BonusListIDs);
            ClearChangesMask(ArtifactPowers);
            ClearChangesMask(Gems);
            ClearChangesMask(Owner);
            ClearChangesMask(ContainedIn);
            ClearChangesMask(Creator);
            ClearChangesMask(GiftCreator);
            ClearChangesMask(StackCount);
            ClearChangesMask(Expiration);
            ClearChangesMask(DynamicFlags);
            ClearChangesMask(PropertySeed);
            ClearChangesMask(RandomPropertiesID);
            ClearChangesMask(Durability);
            ClearChangesMask(MaxDurability);
            ClearChangesMask(CreatePlayedTime);
            ClearChangesMask(Context);
            ClearChangesMask(CreateTime);
            ClearChangesMask(ArtifactXP);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(Modifiers);
            ClearChangesMask(DynamicFlags2);
            ClearChangesMask(DEBUGItemLevel);
            ClearChangesMask(SpellCharges);
            ClearChangesMask(Enchantment);
            _changesMask.ResetAll();
        }
    }

    public class ContainerData : BaseUpdateData<Bag>
    {
        public UpdateField<uint> NumSlots = new(0, 1);
        public UpdateFieldArray<ObjectGuid> Slots = new(36, 2, 3);

        public ContainerData() : base(0, TypeId.Container, 39) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
        {
            for (int i = 0; i < 36; ++i)
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
            data.WriteBits(_changesMask.GetBlocksMask(0), 2);
            for (uint i = 0; i < 2; ++i)
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
                for (int i = 0; i < 36; ++i)
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

    public class UnitChannel
    {
        public uint SpellID;
        public uint SpellXSpellVisualID;

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellXSpellVisualID);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellXSpellVisualID);
        }
    }

    public class VisibleItem : BaseUpdateData<Unit>
    {
        public UpdateField<uint> ItemID = new(0, 1);
        public UpdateField<ushort> ItemAppearanceModID = new(0, 2);
        public UpdateField<ushort> ItemVisual = new(0, 3);

        public VisibleItem() : base(5) { }

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt16(ItemAppearanceModID);
            data.WriteUInt16(ItemVisual);
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
                    data.WriteUInt32(ItemID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt16(ItemAppearanceModID);
                }
                if (changesMask[3])
                {
                    data.WriteUInt16(ItemVisual);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ItemID);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(ItemVisual);
            _changesMask.ResetAll();
        }
    }

    public class PassiveSpellHistory
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
    }

    public class UnitData : BaseUpdateData<Unit>
    {
        public UpdateField<List<uint>> StateWorldEffectIDs = new(0, 1);
        public DynamicUpdateField<PassiveSpellHistory> PassiveSpells = new(0, 2);
        public DynamicUpdateField<int> WorldEffects = new(0, 3);
        public DynamicUpdateField<ObjectGuid> ChannelObjects = new(0, 4);
        public UpdateField<ulong> Health = new(0, 5);
        public UpdateField<ulong> MaxHealth = new(0, 6);
        public UpdateField<uint> DisplayID = new(0, 7);
        public UpdateField<uint> StateSpellVisualID = new(0, 8);
        public UpdateField<uint> StateAnimID = new(0, 9);
        public UpdateField<uint> StateAnimKitID = new(0, 10);        
        public UpdateField<ObjectGuid> Charm = new(0, 11);
        public UpdateField<ObjectGuid> Summon = new(0, 12);
        public UpdateField<ObjectGuid> Critter = new(0, 13);
        public UpdateField<ObjectGuid> CharmedBy = new(0, 14);
        public UpdateField<ObjectGuid> SummonedBy = new(0, 15);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 16);
        public UpdateField<ObjectGuid> DemonCreator = new(0, 17);
        public UpdateField<ObjectGuid> LookAtControllerTarget = new(0, 18);
        public UpdateField<ObjectGuid> Target = new(0, 19);
        public UpdateField<ObjectGuid> BattlePetCompanionGUID = new(0, 20);
        public UpdateField<ulong> BattlePetDBID = new(0, 21);
        public UpdateField<UnitChannel> ChannelData = new(0, 22);
        public UpdateField<uint> SummonedByHomeRealm = new(0, 23);
        public UpdateField<byte> Race = new(0, 24);
        public UpdateField<byte> ClassId = new(0, 25);
        public UpdateField<byte> PlayerClassId = new(0, 26);
        public UpdateField<byte> Sex = new(0, 27);
        public UpdateField<byte> DisplayPower = new(0, 28);
        public UpdateField<uint> OverrideDisplayPowerID = new(0, 29);
        public UpdateField<uint> Level = new(0, 30);
        public UpdateField<int> EffectiveLevel = new(0, 31);
        public UpdateField<uint> ContentTuningID = new(32, 33);
        public UpdateField<int> ScalingLevelMin = new(32, 34);
        public UpdateField<int> ScalingLevelMax = new(32, 35);
        public UpdateField<int> ScalingLevelDelta = new(32, 36);
        public UpdateField<int> ScalingFactionGroup = new(32, 37);
        public UpdateField<int> ScalingHealthItemLevelCurveID = new(32, 38);
        public UpdateField<int> ScalingDamageItemLevelCurveID = new(32, 39);
        public UpdateField<uint> FactionTemplate = new(32, 40);
        public UpdateField<uint> Flags = new(32, 41);
        public UpdateField<uint> Flags2 = new(32, 42);
        public UpdateField<uint> Flags3 = new(32, 43);
        public UpdateField<uint> AuraState = new(32, 44);
        public UpdateField<uint> RangedAttackRoundBaseTime = new(32, 45);
        public UpdateField<float> BoundingRadius = new(32, 46);
        public UpdateField<float> CombatReach = new(32, 47);
        public UpdateField<float> DisplayScale = new(32, 48);
        public UpdateField<uint> NativeDisplayID = new(32, 49);
        public UpdateField<float> NativeXDisplayScale = new(32, 50);
        public UpdateField<uint> MountDisplayID = new(32, 51);
        public UpdateField<float> MinDamage = new(32, 52);
        public UpdateField<float> MaxDamage = new(32, 53);
        public UpdateField<float> MinOffHandDamage = new(32, 54);
        public UpdateField<float> MaxOffHandDamage = new(32, 55);
        public UpdateField<byte> StandState = new(32, 56);
        public UpdateField<byte> PetTalentPoints = new(32, 57);
        public UpdateField<byte> VisFlags = new(32, 58);
        public UpdateField<byte> AnimTier = new(32, 59);
        public UpdateField<uint> PetNumber = new(32, 60);
        public UpdateField<uint> PetNameTimestamp = new(32, 61);
        public UpdateField<uint> PetExperience = new(32, 62);
        public UpdateField<uint> PetNextLevelExperience = new(32, 63);
        public UpdateField<float> ModCastingSpeed = new(64, 65);
        public UpdateField<float> ModSpellHaste = new(64, 66);
        public UpdateField<float> ModHaste = new(64, 67);
        public UpdateField<float> ModRangedHaste = new(64, 68);
        public UpdateField<float> ModHasteRegen = new(64, 69);
        public UpdateField<float> ModTimeRate = new(64, 70);
        public UpdateField<uint> CreatedBySpell = new(64, 71);
        public UpdateField<int> EmoteState = new(64, 72);
        UpdateField<ushort> TrainingPointsUsed = new(64,73);
        UpdateField<ushort> TrainingPointsTotal = new(64, 74);
        public UpdateField<uint> BaseMana = new(64, 75);
        public UpdateField<uint> BaseHealth = new(64, 76);
        public UpdateField<byte> SheatheState = new(64, 77);
        public UpdateField<byte> PvpFlags = new(64, 78);
        public UpdateField<byte> PetFlags = new(64, 79);
        public UpdateField<byte> ShapeshiftForm = new(64, 80);
        public UpdateField<int> AttackPower = new(64, 81);
        public UpdateField<int> AttackPowerModPos = new(64, 82);
        public UpdateField<int> AttackPowerModNeg = new(64, 83);
        public UpdateField<float> AttackPowerMultiplier = new(64, 84);
        public UpdateField<int> RangedAttackPower = new(64, 85);
        public UpdateField<int> RangedAttackPowerModPos = new(64, 86);
        public UpdateField<int> RangedAttackPowerModNeg = new(64, 87);
        public UpdateField<float> RangedAttackPowerMultiplier = new(64, 88);
        public UpdateField<int> SetAttackSpeedAura = new(64, 89);
        public UpdateField<float> Lifesteal = new(64, 90);
        public UpdateField<float> MinRangedDamage = new(64, 91);
        public UpdateField<float> MaxRangedDamage = new(64, 92);
        public UpdateField<float> MaxHealthModifier = new(64, 93);
        public UpdateField<float> HoverHeight = new(64, 94);
        public UpdateField<uint> MinItemLevelCutoff = new(64, 95);
        public UpdateField<uint> MinItemLevel = new(96, 97);
        public UpdateField<uint> MaxItemLevel = new(96, 98);
        public UpdateField<uint> WildBattlePetLevel = new(96, 99);
        public UpdateField<uint> BattlePetCompanionNameTimestamp = new(96, 100);
        public UpdateField<int> InteractSpellID = new(96, 101);
        public UpdateField<int> ScaleDuration = new(96, 102);
        public UpdateField<int> LooksLikeMountID = new(96, 103);
        public UpdateField<int> LooksLikeCreatureID = new(96, 104);
        public UpdateField<int> LookAtControllerID = new(96, 105);
        public UpdateField<ObjectGuid> GuildGUID = new(96, 106);
        public UpdateField<ObjectGuid> SkinningOwnerGUID = new(96, 107);
        public UpdateField<ObjectGuid> Unk340_3 = new(96, 108);                   // When set, nameplate of this unit will instead appear on that object
        public UpdateFieldArray<uint> NpcFlags = new(2, 109, 110);
        public UpdateFieldArray<float> Unk340 = new(7, 112, 113);
        public UpdateFieldArray<float> Unk340_2 = new(7, 112, 120);
        public UpdateFieldArray<int> Power = new(7, 112, 127);
        public UpdateFieldArray<uint> MaxPower = new(7, 112, 134);
        public UpdateFieldArray<float> PowerRegenFlatModifier = new(7, 112, 141);
        public UpdateFieldArray<VisibleItem> VirtualItems = new(3, 148, 149);
        public UpdateFieldArray<uint> AttackRoundBaseTime = new(2, 152, 153);
        public UpdateFieldArray<int> Stats = new(5, 155, 156);
        public UpdateFieldArray<int> StatPosBuff = new(5, 155, 161);
        public UpdateFieldArray<int> StatNegBuff = new(5, 155, 166);
        public UpdateFieldArray<int> Resistances = new(7, 171, 172);
        public UpdateFieldArray<int> PowerCostModifier = new(7, 171, 179);
        public UpdateFieldArray<float> PowerCostMultiplier = new(7, 171, 186);
        public UpdateFieldArray<int> ResistanceBuffModsPositive = new(7, 171, 193);
        public UpdateFieldArray<int> ResistanceBuffModsNegative = new(7, 171, 200);

        public UnitData() : base(0, TypeId.Unit, 194) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            data.WriteUInt32(GetViewerDependentDisplayId(this, owner, receiver));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(GetViewerDependentNpcFlags(this, i, owner, receiver));

            data.WriteUInt32(StateSpellVisualID);
            data.WriteUInt32(StateAnimID);
            data.WriteUInt32(StateAnimKitID);
            data.WriteInt32(((List<uint>)StateWorldEffectIDs).Count);
            for (int i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
                data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);

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
            ((UnitChannel)ChannelData).WriteCreate(data, owner, receiver);
            data.WriteUInt32(SummonedByHomeRealm);
            data.WriteUInt8(Race);
            data.WriteUInt8(ClassId);
            data.WriteUInt8(PlayerClassId);
            data.WriteUInt8(Sex);
            data.WriteUInt8(DisplayPower);
            data.WriteUInt32(OverrideDisplayPowerID);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
            {
                for (int i = 0; i < 7; ++i)
                {
                    data.WriteFloat(Unk340[i]);
                    data.WriteFloat(Unk340_2[i]);
                }
            }
            for (int i = 0; i < 7; ++i)
            {
                data.WriteInt32(Power[i]);
                data.WriteUInt32(MaxPower[i]); //TODO: may be Int32?
                data.WriteFloat(PowerRegenFlatModifier[i]);
            }
            data.WriteUInt32(Level);
            data.WriteInt32(EffectiveLevel);
            data.WriteUInt32(ContentTuningID);
            data.WriteInt32(ScalingLevelMin);
            data.WriteInt32(ScalingLevelMax);
            data.WriteInt32(ScalingLevelDelta);
            data.WriteInt32(ScalingFactionGroup);
            data.WriteInt32(ScalingHealthItemLevelCurveID);
            data.WriteInt32(ScalingDamageItemLevelCurveID);
            data.WriteUInt32(GetViewerDependentFactionTemplate(this, owner, receiver));
            for (int i = 0; i < 3; ++i)
                VirtualItems[i].WriteCreate(data, owner, receiver);

            data.WriteUInt32(GetViewerDependentFlags(this, owner, receiver));
            data.WriteUInt32(Flags2);
            data.WriteUInt32(GetViewerDependentFlags3(this, owner, receiver));
            data.WriteUInt32(GetViewerDependentAuraState(this, owner, receiver));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(AttackRoundBaseTime[i]);

            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WriteUInt32(RangedAttackRoundBaseTime);

            data.WriteFloat(BoundingRadius);
            data.WriteFloat(CombatReach);
            data.WriteFloat(DisplayScale);
            data.WriteUInt32(NativeDisplayID);
            data.WriteFloat(NativeXDisplayScale);
            data.WriteUInt32(MountDisplayID);
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
            data.WriteFloat(ModSpellHaste);
            data.WriteFloat(ModHaste);
            data.WriteFloat(ModRangedHaste);
            data.WriteFloat(ModHasteRegen);
            data.WriteFloat(ModTimeRate);
            data.WriteUInt32(CreatedBySpell);
            data.WriteInt32(EmoteState);
            data.WriteUInt16(TrainingPointsUsed);
            data.WriteUInt16(TrainingPointsTotal);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                for (int i = 0; i < 5; ++i)
                {
                    data.WriteInt32(Stats[i]);
                    data.WriteInt32(StatPosBuff[i]);
                    data.WriteInt32(StatNegBuff[i]);
                }
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
            {
                for (int i = 0; i < (int)SpellSchools.Max; ++i)
                {
                    data.WriteInt32(Resistances[i]);
                }
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                for (int i = 0; i < 7; ++i)
                {
                    data.WriteInt32(PowerCostModifier[i]);
                    data.WriteFloat(PowerCostMultiplier[i]);
                }
            }
            for (int i = 0; i < 7; ++i)
            {
                data.WriteInt32(ResistanceBuffModsPositive[i]);
                data.WriteInt32(ResistanceBuffModsNegative[i]);
            }
            data.WriteUInt32(BaseMana);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WriteUInt32(BaseHealth);

            data.WriteUInt8(SheatheState);
            data.WriteUInt8((byte)GetViewerDependentPvpFlags(this, owner, receiver));
            data.WriteUInt8(PetFlags);
            data.WriteUInt8(ShapeshiftForm);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteInt32(AttackPower);
                data.WriteInt32(AttackPowerModPos);
                data.WriteInt32(AttackPowerModNeg);
                data.WriteFloat(AttackPowerMultiplier);
                data.WriteInt32(RangedAttackPower);
                data.WriteInt32(RangedAttackPowerModPos);
                data.WriteInt32(RangedAttackPowerModNeg);
                data.WriteFloat(RangedAttackPowerMultiplier);
                data.WriteInt32(SetAttackSpeedAura);
                data.WriteFloat(Lifesteal);
                data.WriteFloat(MinRangedDamage);
                data.WriteFloat(MaxRangedDamage);
                data.WriteFloat(MaxHealthModifier);
            }
            data.WriteFloat(HoverHeight);
            data.WriteUInt32(MinItemLevelCutoff);
            data.WriteUInt32(MinItemLevel);
            data.WriteUInt32(MaxItemLevel);
            data.WriteUInt32(WildBattlePetLevel);
            data.WriteUInt32(BattlePetCompanionNameTimestamp);
            data.WriteInt32(InteractSpellID);
            data.WriteInt32(ScaleDuration);
            data.WriteInt32(LooksLikeMountID);
            data.WriteInt32(LooksLikeCreatureID);
            data.WriteInt32(LookAtControllerID);
            data.WritePackedGuid(GuildGUID);
            data.WriteInt32(PassiveSpells.Size());
            data.WriteInt32(WorldEffects.Size());
            data.WriteInt32(ChannelObjects.Size());
            data.WritePackedGuid(SkinningOwnerGUID);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WritePackedGuid(Unk340_3);
            }

            for (int i = 0; i < PassiveSpells.Size(); ++i)
                PassiveSpells[i].WriteCreate(data, owner, receiver);

            for (int i = 0; i < WorldEffects.Size(); ++i)
                data.WriteInt32(WorldEffects[i]);

            for (int i = 0; i < ChannelObjects.Size(); ++i)
                data.WritePackedGuid(ChannelObjects[i]);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(195, new uint[] { 0xFFFFDFFFu, 0xFF0FDFFFu, 0xC001EFFFu, 0x8001EFFFu, 0x07FFFFFFu, 0x00000800u, 0x00007FFEu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(195, new uint[] { 0x00002000u, 0x00F02000u, 0x3FFE1000u, 0x7FFF1000u, 0xF8000000u, 0xFFFFFFFFu, 0x00000001u }));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
                allowedMaskForTarget.OR(new UpdateMask(195, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x7FFF0000u, 0x00000000u, 0x00000000u, 0x00000000u }));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
                allowedMaskForTarget.OR(new UpdateMask(195, new uint[] { 0x00000000u, 0x00F00000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x0007F800u, 0x00000000u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(195, new[] { 0xFFFFDFFFu, 0xFF0FDFFFu, 0xC001EFFFu, 0x8001EFFFu, 0x07FFFFFFu, 0x00000800u, 0x00007FFEu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Unit owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 7);
            for (uint i = 0; i < 7; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBits(((List<uint>)StateWorldEffectIDs).Count, 32);
                    for (int i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
                    {
                        data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        PassiveSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(PassiveSpells.Size(), data);
                }
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        WorldEffects.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(WorldEffects.Size(), data);
                }
                if (changesMask[4])
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
                if (changesMask[2])
                {
                    for (int i = 0; i < PassiveSpells.Size(); ++i)
                    {
                        if (PassiveSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            PassiveSpells[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                    for (int i = 0; i < ChannelObjects.Size(); ++i)
                    {
                        if (ChannelObjects.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WritePackedGuid(ChannelObjects[i]);
                        }
                    }
                }
                if (changesMask[5])
                {
                    data.WriteUInt64(Health); //TODO: may be Int64? it's in TC
                }
                if (changesMask[6])
                {
                    data.WriteUInt64(MaxHealth); //TODO: may be Int64? it's in TC
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(GetViewerDependentDisplayId(this, owner, receiver));
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(StateSpellVisualID);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(StateAnimID);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(StateAnimKitID);
                }
                if (changesMask[11])
                {
                    data.WritePackedGuid(Charm);
                }
                if (changesMask[12])
                {
                    data.WritePackedGuid(Summon);
                }
                if (changesMask[13])
                {
                    data.WritePackedGuid(Critter);
                }
                if (changesMask[14])
                {
                    data.WritePackedGuid(CharmedBy);
                }
                if (changesMask[15])
                {
                    data.WritePackedGuid(SummonedBy);
                }
                if (changesMask[16])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (changesMask[17])
                {
                    data.WritePackedGuid(DemonCreator);
                }
                if (changesMask[18])
                {
                    data.WritePackedGuid(LookAtControllerTarget);
                }
                if (changesMask[19])
                {
                    data.WritePackedGuid(Target);
                }
                if (changesMask[20])
                {
                    data.WritePackedGuid(BattlePetCompanionGUID);
                }
                if (changesMask[21])
                {
                    data.WriteUInt64(BattlePetDBID);
                }
                if (changesMask[22])
                {
                    ((UnitChannel)ChannelData).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(SummonedByHomeRealm);
                }
                if (changesMask[24])
                {
                    data.WriteUInt8(Race);
                }
                if (changesMask[25])
                {
                    data.WriteUInt8(ClassId);
                }
                if (changesMask[26])
                {
                    data.WriteUInt8(PlayerClassId);
                }
                if (changesMask[27])
                {
                    data.WriteUInt8(Sex);
                }
                if (changesMask[28])
                {
                    data.WriteUInt8(DisplayPower);
                }
                if (changesMask[29])
                {
                    data.WriteUInt32(OverrideDisplayPowerID);
                }
                if (changesMask[30])
                {
                    data.WriteUInt32(Level); //TODO: may be Int32? it's in TC
                }
                if (changesMask[31])
                {
                    data.WriteInt32(EffectiveLevel);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt32(ContentTuningID);//TODO: may be Int32? it's in TC
                }
                if (changesMask[34])
                {
                    data.WriteInt32(ScalingLevelMin);
                }
                if (changesMask[35])
                {
                    data.WriteInt32(ScalingLevelMax);
                }
                if (changesMask[36])
                {
                    data.WriteInt32(ScalingLevelDelta);
                }
                if (changesMask[37])
                {
                    data.WriteInt32(ScalingFactionGroup);
                }
                if (changesMask[38])
                {
                    data.WriteInt32(ScalingHealthItemLevelCurveID);
                }
                if (changesMask[39])
                {
                    data.WriteInt32(ScalingDamageItemLevelCurveID);
                }
                if (changesMask[40])
                {
                    data.WriteUInt32(GetViewerDependentFactionTemplate(this, owner, receiver));
                }
                if (changesMask[41])
                {
                    data.WriteUInt32(GetViewerDependentFlags(this, owner, receiver));
                }
                if (changesMask[42])
                {
                    data.WriteUInt32(Flags2);
                }
                if (changesMask[43])
                {
                    data.WriteUInt32(GetViewerDependentFlags3(this, owner, receiver));
                }
                if (changesMask[44])
                {
                    data.WriteUInt32(GetViewerDependentAuraState(this, owner, receiver));
                }
                if (changesMask[45])
                {
                    data.WriteUInt32(RangedAttackRoundBaseTime);
                }
                if (changesMask[46])
                {
                    data.WriteFloat(BoundingRadius);
                }
                if (changesMask[47])
                {
                    data.WriteFloat(CombatReach);
                }
                if (changesMask[48])
                {
                    data.WriteFloat(DisplayScale);
                }
                if (changesMask[49])
                {
                    data.WriteUInt32(NativeDisplayID);
                }
                if (changesMask[50])
                {
                    data.WriteFloat(NativeXDisplayScale);
                }
                if (changesMask[51])
                {
                    data.WriteUInt32(MountDisplayID);
                }
                if (changesMask[52])
                {
                    data.WriteFloat(MinDamage);
                }
                if (changesMask[53])
                {
                    data.WriteFloat(MaxDamage);
                }
                if (changesMask[54])
                {
                    data.WriteFloat(MinOffHandDamage);
                }
                if (changesMask[55])
                {
                    data.WriteFloat(MaxOffHandDamage);
                }
                if (changesMask[56])
                {
                    data.WriteUInt8(StandState);
                }
                if (changesMask[57])
                {
                    data.WriteUInt8(PetTalentPoints);
                }
                if (changesMask[58])
                {
                    data.WriteUInt8(VisFlags);
                }
                if (changesMask[59])
                {
                    data.WriteUInt8(AnimTier);
                }
                if (changesMask[60])
                {
                    data.WriteUInt32(PetNumber);
                }
                if (changesMask[61])
                {
                    data.WriteUInt32(PetNameTimestamp);
                }
                if (changesMask[62])
                {
                    data.WriteUInt32(PetExperience);
                }
                if (changesMask[63])
                {
                    data.WriteUInt32(PetNextLevelExperience);
                }
            }
            if (changesMask[64])
            {

                if (changesMask[65])
                {
                    data.WriteFloat(ModCastingSpeed);
                }
                if (changesMask[66])
                {
                    data.WriteFloat(ModSpellHaste);
                }
                if (changesMask[67])
                {
                    data.WriteFloat(ModHaste);
                }
                if (changesMask[68])
                {
                    data.WriteFloat(ModRangedHaste);
                }
                if (changesMask[69])
                {
                    data.WriteFloat(ModHasteRegen);
                }
                if (changesMask[70])
                {
                    data.WriteFloat(ModTimeRate);
                }
                if (changesMask[71])
                {
                    data.WriteUInt32(CreatedBySpell); //TODO: may be Int32? it's in TC
                }
                if (changesMask[72])
                {
                    data.WriteInt32(EmoteState);
                }
                if (changesMask[73])
                {
                    data.WriteUInt16(TrainingPointsUsed);
                }
                if (changesMask[74])
                {
                    data.WriteUInt16(TrainingPointsTotal);
                }
                if (changesMask[75])
                {
                    data.WriteUInt32(BaseMana); //TODO: may be Int32? it's in TC
                }
                if (changesMask[76])
                {
                    data.WriteUInt32(BaseHealth); //TODO: may be Int32? it's in TC
                }
                if (changesMask[77])
                {
                    data.WriteUInt8(SheatheState);
                }
                if (changesMask[78])
                {
                    data.WriteUInt8((byte)GetViewerDependentPvpFlags(this, owner, receiver));
                }
                if (changesMask[79])
                {
                    data.WriteUInt8(PetFlags);
                }
                if (changesMask[80])
                {
                    data.WriteUInt8(ShapeshiftForm);
                }
                if (changesMask[81])
                {
                    data.WriteInt32(AttackPower);
                }
                if (changesMask[82])
                {
                    data.WriteInt32(AttackPowerModPos);
                }
                if (changesMask[83])
                {
                    data.WriteInt32(AttackPowerModNeg);
                }
                if (changesMask[84])
                {
                    data.WriteFloat(AttackPowerMultiplier);
                }
                if (changesMask[85])
                {
                    data.WriteInt32(RangedAttackPower);
                }
                if (changesMask[86])
                {
                    data.WriteInt32(RangedAttackPowerModPos);
                }
                if (changesMask[87])
                {
                    data.WriteInt32(RangedAttackPowerModNeg);
                }
                if (changesMask[88])
                {
                    data.WriteFloat(RangedAttackPowerMultiplier);
                }
                if (changesMask[89])
                {
                    data.WriteInt32(SetAttackSpeedAura);
                }
                if (changesMask[90])
                {
                    data.WriteFloat(Lifesteal);
                }
                if (changesMask[91])
                {
                    data.WriteFloat(MinRangedDamage);
                }
                if (changesMask[92])
                {
                    data.WriteFloat(MaxRangedDamage);
                }
                if (changesMask[93])
                {
                    data.WriteFloat(MaxHealthModifier);
                }
                if (changesMask[94])
                {
                    data.WriteFloat(HoverHeight);
                }
                if (changesMask[95])
                {
                    data.WriteUInt32(MinItemLevelCutoff);
                }
            }
            if (changesMask[96])
            {
                if (changesMask[97])
                {
                    data.WriteUInt32(MinItemLevel); //TODO: may be Int32? it's in TC
                }
                if (changesMask[98])
                {
                    data.WriteUInt32(MaxItemLevel); //TODO: may be Int32? it's in TC
                }
                if (changesMask[99])
                {
                    data.WriteUInt32(WildBattlePetLevel);  //TODO: may be Int32? it's in TC
                }
                if (changesMask[100])
                {
                    data.WriteUInt32(BattlePetCompanionNameTimestamp);
                }
                if (changesMask[101])
                {
                    data.WriteInt32(InteractSpellID);
                }
                if (changesMask[102])
                {
                    data.WriteInt32(ScaleDuration);
                }
                if (changesMask[103])
                {
                    data.WriteInt32(LooksLikeMountID);
                }
                if (changesMask[104])
                {
                    data.WriteInt32(LooksLikeCreatureID);
                }
                if (changesMask[105])
                {
                    data.WriteInt32(LookAtControllerID);
                }
                if (changesMask[106])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[107])
                {
                    data.WritePackedGuid(SkinningOwnerGUID);
                }
                if (changesMask[108])
                {
                    data.WritePackedGuid(Unk340_3);
                }
            }
            if (changesMask[109])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[110 + i])
                    {
                        data.WriteUInt32(GetViewerDependentNpcFlags(this, i, owner, receiver));
                    }
                }
            }
            if (changesMask[112])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[113 + i])
                    {
                        data.WriteFloat(Unk340[i]);
                    }
                    if (changesMask[120 + i])
                    {
                        data.WriteFloat(Unk340_2[i]);
                    }
                    if (changesMask[127 + i])
                    {
                        data.WriteInt32(Power[i]);
                    }
                    if (changesMask[134 + i])
                    {
                        data.WriteUInt32(MaxPower[i]);
                    }
                    if (changesMask[141 + i])
                    {
                        data.WriteFloat(PowerRegenFlatModifier[i]);
                    }
                }
            }
            if (changesMask[148])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[149 + i])
                    {
                        VirtualItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[152])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[153 + i])
                    {
                        data.WriteUInt32(AttackRoundBaseTime[i]);
                    }
                }
            }
            if (changesMask[155])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[156 + i])
                    {
                        data.WriteInt32(Stats[i]);
                    }
                    if (changesMask[161 + i])
                    {
                        data.WriteInt32(StatPosBuff[i]);
                    }
                    if (changesMask[166 + i])
                    {
                        data.WriteInt32(StatNegBuff[i]);
                    }
                }
            }
            if (changesMask[171])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[172 + i])
                    {
                        data.WriteInt32(Resistances[i]);
                    }
                    if (changesMask[179 + i])
                    {
                        data.WriteInt32(PowerCostModifier[i]);
                    }
                    if (changesMask[186 + i])
                    {
                        data.WriteFloat(PowerCostMultiplier[i]);
                    }
                }
            }
            if (changesMask[193])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[194 + i])
                    {
                        data.WriteInt32(ResistanceBuffModsPositive[i]);
                    }
                    if (changesMask[201 + i])
                    {
                        data.WriteInt32(ResistanceBuffModsNegative[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(StateWorldEffectIDs);
            ClearChangesMask(PassiveSpells);
            ClearChangesMask(WorldEffects);
            ClearChangesMask(ChannelObjects);
            ClearChangesMask(Health);
            ClearChangesMask(MaxHealth);
            ClearChangesMask(DisplayID);
            ClearChangesMask(StateSpellVisualID);
            ClearChangesMask(StateAnimID);
            ClearChangesMask(StateAnimKitID);
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
            ClearChangesMask(SummonedByHomeRealm);
            ClearChangesMask(Race);
            ClearChangesMask(ClassId);
            ClearChangesMask(PlayerClassId);
            ClearChangesMask(Sex);
            ClearChangesMask(DisplayPower);
            ClearChangesMask(OverrideDisplayPowerID);
            ClearChangesMask(Level);
            ClearChangesMask(EffectiveLevel);
            ClearChangesMask(ContentTuningID);
            ClearChangesMask(ScalingLevelMin);
            ClearChangesMask(ScalingLevelMax);
            ClearChangesMask(ScalingLevelDelta);
            ClearChangesMask(ScalingFactionGroup);
            ClearChangesMask(ScalingHealthItemLevelCurveID);
            ClearChangesMask(ScalingDamageItemLevelCurveID);
            ClearChangesMask(FactionTemplate);
            ClearChangesMask(Flags);
            ClearChangesMask(Flags2);
            ClearChangesMask(Flags3);
            ClearChangesMask(AuraState);
            ClearChangesMask(RangedAttackRoundBaseTime);
            ClearChangesMask(BoundingRadius);
            ClearChangesMask(CombatReach);
            ClearChangesMask(DisplayScale);
            ClearChangesMask(NativeDisplayID);
            ClearChangesMask(NativeXDisplayScale);
            ClearChangesMask(MountDisplayID);
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
            ClearChangesMask(RangedAttackPower);
            ClearChangesMask(RangedAttackPowerModPos);
            ClearChangesMask(RangedAttackPowerModNeg);
            ClearChangesMask(RangedAttackPowerMultiplier);
            ClearChangesMask(SetAttackSpeedAura);
            ClearChangesMask(Lifesteal);
            ClearChangesMask(MinRangedDamage);
            ClearChangesMask(MaxRangedDamage);
            ClearChangesMask(MaxHealthModifier);
            ClearChangesMask(HoverHeight);
            ClearChangesMask(MinItemLevelCutoff);
            ClearChangesMask(MinItemLevel);
            ClearChangesMask(MaxItemLevel);
            ClearChangesMask(WildBattlePetLevel);
            ClearChangesMask(BattlePetCompanionNameTimestamp);
            ClearChangesMask(InteractSpellID);
            ClearChangesMask(ScaleDuration);
            ClearChangesMask(LooksLikeMountID);
            ClearChangesMask(LooksLikeCreatureID);
            ClearChangesMask(LookAtControllerID);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(SkinningOwnerGUID);
            ClearChangesMask(Unk340_3);
            ClearChangesMask(NpcFlags);
            ClearChangesMask(Unk340);
            ClearChangesMask(Unk340_2);
            ClearChangesMask(Power);
            ClearChangesMask(MaxPower);
            ClearChangesMask(PowerRegenFlatModifier);
            ClearChangesMask(VirtualItems);
            ClearChangesMask(AttackRoundBaseTime);
            ClearChangesMask(Stats);
            ClearChangesMask(StatPosBuff);
            ClearChangesMask(StatNegBuff);
            ClearChangesMask(Resistances);
            ClearChangesMask(PowerCostModifier);
            ClearChangesMask(ResistanceBuffModsPositive);
            ClearChangesMask(ResistanceBuffModsNegative);
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
        uint GetViewerDependentNpcFlags(UnitData unitData, int i, Unit unit, Player receiver)
        {
            uint npcFlag = unitData.NpcFlags[i];
            if (i == 0 && unit.IsCreature() && !receiver.CanSeeSpellClickOn(unit.ToCreature()))
                npcFlag &= ~(uint)NPCFlags.SpellClick;

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
        uint GetViewerDependentFlags3(UnitData unitData, Unit unit, Player receiver)
        {
            uint flags = unitData.Flags3;
            if ((flags & (uint)UnitFlags3.AlreadySkinned) != 0 && unit.IsCreature() && !unit.ToCreature().IsSkinnedBy(receiver))
                flags &= ~(uint)UnitFlags3.AlreadySkinned;

            return flags;
        }
        uint GetViewerDependentAuraState(UnitData unitData, Unit unit, Player receiver)
        {
            // Check per caster aura states to not enable using a spell in client if specified aura is not by target
            return unit.BuildAuraStateUpdateForTarget(receiver);
        }
        uint GetViewerDependentPvpFlags(UnitData unitData, Unit unit, Player receiver)
        {
            uint pvpFlags = unitData.PvpFlags;
            if (unit.IsControlledByPlayer() && receiver != unit && WorldConfig.GetBoolValue(WorldCfg.AllowTwoSideInteractionGroup) && unit.IsInRaidWith(receiver))
            {
                FactionTemplateRecord ft1 = unit.GetFactionTemplateEntry();
                FactionTemplateRecord ft2 = receiver.GetFactionTemplateEntry();
                if (ft1 != null && ft2 != null && !ft1.IsFriendlyTo(ft2))
                    // Allow targeting opposite faction in party when enabled in config
                    pvpFlags &= (uint)UnitPVPStateFlags.Sanctuary;
            }

            return pvpFlags;
        }
    }

    public class ChrCustomizationChoice : IComparable<ChrCustomizationChoice>
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

        public int CompareTo(ChrCustomizationChoice other)
        {
            return ChrCustomizationOptionID.CompareTo(other.ChrCustomizationOptionID);
        }
    }

    public class QuestLog : BaseUpdateData<Player>
    {
        public UpdateField<uint> QuestID = new(0, 1);
        public UpdateField<uint> StateFlags = new(0, 2);
        public UpdateField<uint> EndTime = new(0, 3);
        public UpdateField<uint> AcceptTime = new(0, 4);
        public UpdateFieldArray<ushort> ObjectiveProgress = new(24, 5, 6);

        public QuestLog() : base(31) { }

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(QuestID);
            data.WriteUInt32(StateFlags);
            data.WriteUInt32(EndTime);
            data.WriteUInt32(AcceptTime);
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
                    data.WriteUInt32(QuestID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(StateFlags);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(EndTime);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(AcceptTime);
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
            ClearChangesMask(QuestID);
            ClearChangesMask(StateFlags);
            ClearChangesMask(EndTime);
            ClearChangesMask(AcceptTime);
            ClearChangesMask(ObjectiveProgress);
            _changesMask.ResetAll();
        }
    }

    public class ArenaCooldown : BaseUpdateData<Player>
    {
        public UpdateField<int> SpellID = new(0, 1);
        public UpdateField<int> Charges = new(0, 2);
        public UpdateField<int> Unk254 = new(0, 3);
        public UpdateField<uint> Flags = new(0, 4);
        public UpdateField<uint> StartTime = new(0, 5);
        public UpdateField<uint> EndTime = new(0, 6);
        public UpdateField<uint> NextChargeTime = new(0, 7);
        public UpdateField<byte> MaxCharges = new(0, 8);

        public ArenaCooldown() : base(8) { }

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(Charges);
            data.WriteInt32(Unk254);
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

            data.WriteBits(changesMask.GetBlock(0), 9);

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
                    data.WriteInt32(Unk254);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(Flags);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(StartTime);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(EndTime);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(NextChargeTime);
                }
                if (changesMask[8])
                {
                    data.WriteUInt8(MaxCharges);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(SpellID);
            ClearChangesMask(Charges);
            ClearChangesMask(Unk254);
            ClearChangesMask(Flags);
            ClearChangesMask(StartTime);
            ClearChangesMask(EndTime);
            ClearChangesMask(NextChargeTime);
            ClearChangesMask(MaxCharges);
            _changesMask.ResetAll();
        }
    }

    public class PlayerData : BaseUpdateData<Player>
    {
        public DynamicUpdateField<ChrCustomizationChoice> Customizations = new(0, 1);
        public DynamicUpdateField<ArenaCooldown> ArenaCooldowns = new(0, 2);
        public UpdateField<ObjectGuid> DuelArbiter = new(0, 3);
        public UpdateField<ObjectGuid> WowAccount = new(0, 4);
        public UpdateField<ObjectGuid> LootTargetGUID = new(0, 5);
        public UpdateField<uint> PlayerFlags = new(0, 6);
        public UpdateField<uint> PlayerFlagsEx = new(0, 7);
        public UpdateField<uint> GuildRankID = new(0, 8);
        public UpdateField<uint> GuildDeleteDate = new(0, 9);
        public UpdateField<uint> GuildLevel = new(0, 10);
        public UpdateField<byte> PartyType = new(0, 11);
        public UpdateField<byte> NativeSex = new(0, 12);
        public UpdateField<byte> Inebriation = new(0, 13);
        public UpdateField<byte> PvpTitle = new(0, 14);
        public UpdateField<byte> ArenaFaction = new(0, 15);
        public UpdateField<byte> PvpRank = new(0, 16);
        public UpdateField<byte> Unk254 = new(0, 17);
        public UpdateField<uint> DuelTeam = new(0, 18);
        public UpdateField<int> GuildTimeStamp = new(0, 19);
        public UpdateField<uint> PlayerTitle = new(0, 20);
        public UpdateField<int> FakeInebriation = new(0, 21);
        public UpdateField<uint> VirtualPlayerRealm = new(0, 22);
        public UpdateField<uint> CurrentSpecID = new(0, 23);
        public UpdateField<int> TaxiMountAnimKitID = new(0, 24);
        public UpdateField<byte> CurrentBattlePetBreedQuality = new(0, 25);
        public UpdateField<uint> HonorLevel = new(0, 26);
        public UpdateFieldArray<QuestLog> QuestLog = new(25, 27, 28);
        public UpdateFieldArray<VisibleItem> VisibleItems = new(19, 53, 54);
        public UpdateFieldArray<float> AvgItemLevel = new(6, 73, 74);

        public PlayerData() : base(0, TypeId.Player, 188) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            data.WritePackedGuid(DuelArbiter);
            data.WritePackedGuid(WowAccount);
            data.WritePackedGuid(LootTargetGUID);
            data.WriteUInt32(PlayerFlags);
            data.WriteUInt32(PlayerFlagsEx);
            data.WriteUInt32(GuildRankID);
            data.WriteUInt32(GuildDeleteDate);
            data.WriteUInt32(GuildLevel);
            data.WriteInt32(Customizations.Size());
            data.WriteUInt8(PartyType);
            data.WriteUInt8(NativeSex);
            data.WriteUInt8(Inebriation);
            data.WriteUInt8(PvpTitle);
            data.WriteUInt8(ArenaFaction);
            data.WriteUInt8(PvpRank);
            data.WriteUInt8(Unk254);
            data.WriteUInt32(DuelTeam);
            data.WriteInt32(GuildTimeStamp);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                for (int i = 0; i < 25; ++i)
                {
                    QuestLog[i].WriteCreate(data, owner, receiver);
                }
            }
            for (int i = 0; i < 19; ++i)
            {
                VisibleItems[i].WriteCreate(data, owner, receiver);
            }
            data.WriteUInt32(PlayerTitle);
            data.WriteInt32(FakeInebriation);
            data.WriteUInt32(VirtualPlayerRealm);
            data.WriteUInt32(CurrentSpecID);
            data.WriteInt32(TaxiMountAnimKitID);
            for (int i = 0; i < 6; ++i)
            {
                data.WriteFloat(AvgItemLevel[i]);
            }
            data.WriteUInt8(CurrentBattlePetBreedQuality);
            data.WriteUInt32(HonorLevel);
            data.WriteInt32(ArenaCooldowns.Size());
            for (int i = 0; i < Customizations.Size(); ++i)
            {
                Customizations[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < ArenaCooldowns.Size(); ++i)
            {
                ArenaCooldowns[i].WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(188, new[] { 0x07FFFFFFu, 0xFFE00000u, 0x0000FFFFu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
                allowedMaskForTarget.OR(new UpdateMask(188, new[] { 0xF8000000u, 0x001FFFFFu, 0x00000000u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(188, new[] { 0x07FFFFFFu, 0xFFE00000u, 0x0000FFFFu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 3);
            for (uint i = 0; i < 3; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            bool noQuestLogChangesMask = data.WriteBit(IsQuestLogChangesMaskSkipped());
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    if (!ignoreNestedChangesMask)
                        Customizations.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Customizations.Size(), data);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        ArenaCooldowns.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ArenaCooldowns.Size(), data);
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
                    for (int i = 0; i < ArenaCooldowns.Size(); ++i)
                    {
                        if (ArenaCooldowns.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ArenaCooldowns[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[3])
                {
                    data.WritePackedGuid(DuelArbiter);
                }
                if (changesMask[4])
                {
                    data.WritePackedGuid(WowAccount);
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(LootTargetGUID);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(PlayerFlags);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(PlayerFlagsEx);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(GuildRankID);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(GuildDeleteDate);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(GuildLevel);
                }
                if (changesMask[11])
                {
                    data.WriteUInt8(PartyType);
                }
                if (changesMask[12])
                {
                    data.WriteUInt8(NativeSex);
                }
                if (changesMask[13])
                {
                    data.WriteUInt8(Inebriation);
                }
                if (changesMask[14])
                {
                    data.WriteUInt8(PvpTitle);
                }
                if (changesMask[15])
                {
                    data.WriteUInt8(ArenaFaction);
                }
                if (changesMask[16])
                {
                    data.WriteUInt8(PvpRank);
                }
                if (changesMask[17])
                {
                    data.WriteUInt8(Unk254);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(DuelTeam);
                }
                if (changesMask[19])
                {
                    data.WriteInt32(GuildTimeStamp);
                }
                if (changesMask[20])
                {
                    data.WriteUInt32(PlayerTitle);
                }
                if (changesMask[21])
                {
                    data.WriteInt32(FakeInebriation);
                }
                if (changesMask[22])
                {
                    data.WriteUInt32(VirtualPlayerRealm);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(CurrentSpecID);
                }
                if (changesMask[24])
                {
                    data.WriteInt32(TaxiMountAnimKitID);
                }
                if (changesMask[25])
                {
                    data.WriteUInt8(CurrentBattlePetBreedQuality);
                }
                if (changesMask[26])
                {
                    data.WriteUInt32(HonorLevel);
                }
            }
            if (changesMask[27])
            {
                for (int i = 0; i < 25; ++i)
                {
                    if (changesMask[28 + i])
                    {
                        QuestLog[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[53])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[54 + i])
                    {
                        VisibleItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[73])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[74 + i])
                    {
                        data.WriteFloat(AvgItemLevel[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Customizations);
            ClearChangesMask(ArenaCooldowns);
            ClearChangesMask(DuelArbiter);
            ClearChangesMask(WowAccount);
            ClearChangesMask(LootTargetGUID);
            ClearChangesMask(PlayerFlags);
            ClearChangesMask(PlayerFlagsEx);
            ClearChangesMask(GuildRankID);
            ClearChangesMask(GuildDeleteDate);
            ClearChangesMask(GuildLevel);
            ClearChangesMask(PartyType);
            ClearChangesMask(NativeSex);
            ClearChangesMask(Inebriation);
            ClearChangesMask(PvpTitle);
            ClearChangesMask(ArenaFaction);
            ClearChangesMask(PvpRank);
            ClearChangesMask(Unk254);
            ClearChangesMask(DuelTeam);
            ClearChangesMask(GuildTimeStamp);
            ClearChangesMask(PlayerTitle);
            ClearChangesMask(FakeInebriation);
            ClearChangesMask(VirtualPlayerRealm);
            ClearChangesMask(CurrentSpecID);
            ClearChangesMask(TaxiMountAnimKitID);
            ClearChangesMask(CurrentBattlePetBreedQuality);
            ClearChangesMask(HonorLevel);
            ClearChangesMask(QuestLog);
            ClearChangesMask(VisibleItems);
            ClearChangesMask(AvgItemLevel);
            _changesMask.ResetAll();
        }

        bool IsQuestLogChangesMaskSkipped() { return false; } // bandwidth savings aren't worth the cpu time
    }

    public class SkillInfo : BaseUpdateData<Player>
    {
        public UpdateFieldArray<ushort> SkillLineID = new(256, 0, 1);
        public UpdateFieldArray<ushort> SkillStep = new(256, 0, 257);
        public UpdateFieldArray<ushort> SkillRank = new(256, 0, 513);
        public UpdateFieldArray<ushort> SkillStartingRank = new(256, 0, 769);
        public UpdateFieldArray<ushort> SkillMaxRank = new(256, 0, 1025);
        public UpdateFieldArray<ushort> SkillTempBonus = new(256, 0, 1281);
        public UpdateFieldArray<ushort> SkillPermBonus = new(256, 0, 1537);

        public SkillInfo() : base(1793) { }

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

    public class RestInfo : BaseUpdateData<Player>
    {
        public UpdateField<uint> Threshold = new(0, 1);
        public UpdateField<byte> StateID = new(0, 2);

        public RestInfo() : base(3) { }

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

    public class PVPInfo : BaseUpdateData<Player>
    {
        public UpdateField<bool> Disqualified = new(0, 1);
        public UpdateField<uint> WeeklyPlayed = new(0, 2);
        public UpdateField<uint> WeeklyWon = new(0, 3);
        public UpdateField<uint> SeasonPlayed = new(0, 4);
        public UpdateField<uint> SeasonWon = new(0, 5);
        public UpdateField<uint> Rating = new(0, 6);
        public UpdateField<uint> WeeklyBestRating = new(0, 7);
        public UpdateField<uint> SeasonBestRating = new(0, 8);
        public UpdateField<uint> PvpTierID = new(0, 9);
        public UpdateField<uint> WeeklyBestWinPvpTierID = new(0, 10);
        public UpdateField<uint> Field_28 = new(0, 11);
        public UpdateField<uint> Field_2C = new(0, 12);

        public PVPInfo() : base(13) { }

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
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
            data.WriteBit(Disqualified);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 13);

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
                    data.WriteUInt32(WeeklyPlayed);
                }
                if (changesMask[3])
                {
                    data.WriteUInt32(WeeklyWon);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(SeasonPlayed);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(SeasonWon);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(Rating);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(WeeklyBestRating);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(SeasonBestRating);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(PvpTierID);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(WeeklyBestWinPvpTierID);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(Field_28);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(Field_2C);
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Disqualified);
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
            _changesMask.ResetAll();
        }
    }

    public class CharacterRestriction
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
    }

    public class SpellPctModByLabel
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
    }

    public class SpellFlatModByLabel
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
    }

    public class Research
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
    }

    public class GlyphInfo : BaseUpdateData<Player>
    {
        public UpdateField<uint> GlyphSlot = new(0, 1);
        public UpdateField<uint> Glyph = new(0, 2);

        public GlyphInfo() : base(3) { }

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(GlyphSlot);
            data.WriteUInt32(Glyph);
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
                    data.WriteUInt32(GlyphSlot);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(Glyph);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(GlyphSlot);
            ClearChangesMask(Glyph);
            _changesMask.ResetAll();
        }
    }

    public class ActivePlayerData : BaseUpdateData<Player>
    {
        public static int ExploredZonesSize;
        public static int ExploredZonesBits;
        public static int QuestCompletedBitsSize;
        public static int QuestCompletedBitsPerBlock;

        public UpdateField<bool> InsertItemsLeftToRight = new(0, 1);
        public UpdateFieldArray<DynamicUpdateField<Research>> Research = new(1, 17, 18);
        public DynamicUpdateField<ulong> KnownTitles = new(0, 2);
        public DynamicUpdateField<ushort> ResearchSites = new(0, 3);
        public DynamicUpdateField<uint> ResearchSiteProgress = new(0, 4);
        public DynamicUpdateField<uint> DailyQuestsCompleted = new(0, 5);
        public DynamicUpdateField<int> AvailableQuestLineXQuestIDs = new(0, 6);
        public DynamicUpdateField<int> Unk254 = new(0, 7);
        public DynamicUpdateField<uint> Heirlooms = new(0, 8);
        public DynamicUpdateField<uint> HeirloomFlags = new(0, 9);
        public DynamicUpdateField<uint> Toys = new(0, 10);
        public DynamicUpdateField<uint> Transmog = new(0, 11);
        public DynamicUpdateField<uint> ConditionalTransmog = new(0, 12);
        public DynamicUpdateField<uint> SelfResSpells = new(0, 13);
        public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new(0, 15);
        public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new(0, 16);       
        public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new(0, 14);
        public UpdateField<ObjectGuid> FarsightObject = new(0, 19);
        public UpdateField<ObjectGuid> ComboTarget = new(0, 20);
        public UpdateField<ulong> Coinage = new(0, 21);
        public UpdateField<uint> XP = new(0, 22);
        public UpdateField<uint> NextLevelXP = new(0, 23);
        public UpdateField<int> TrialXP = new(0, 24);
        public UpdateField<SkillInfo> Skill = new(0, 25);
        public UpdateField<uint> CharacterPoints = new(0, 26);
        public UpdateField<uint> MaxTalentTiers = new(0, 27);
        public UpdateField<uint> TrackCreatureMask = new(0, 28);
        public UpdateField<float> MainhandExpertise = new(0, 29);
        public UpdateField<float> OffhandExpertise = new(0, 30);
        public UpdateField<float> RangedExpertise = new(0, 31);
        public UpdateField<float> CombatRatingExpertise = new(0, 32);
        public UpdateField<float> BlockPercentage = new(0, 33);
        public UpdateField<float> DodgePercentage = new(34, 35);
        public UpdateField<float> DodgePercentageFromAttribute = new(34, 36);
        public UpdateField<float> ParryPercentage = new(34, 37);
        public UpdateField<float> ParryPercentageFromAttribute = new(34, 38);
        public UpdateField<float> CritPercentage = new(34, 39);
        public UpdateField<float> RangedCritPercentage = new(34, 40);
        public UpdateField<float> OffhandCritPercentage = new(34, 41);
        public UpdateField<uint> ShieldBlock = new(34, 42);
        public UpdateField<float> Mastery = new(34, 43);
        public UpdateField<float> Speed = new(34, 44);
        public UpdateField<float> Avoidance = new(34, 45);
        public UpdateField<float> Sturdiness = new(34, 46);
        public UpdateField<float> Unk340_3 = new(34, 47);
        public UpdateField<int> Versatility = new(34, 48);
        public UpdateField<float> VersatilityBonus = new(34, 49);
        public UpdateField<float> PvpPowerDamage = new(34, 50);
        public UpdateField<float> PvpPowerHealing = new(34, 51);
        public UpdateField<int> ModHealingDonePos = new(34, 52);
        public UpdateField<float> ModHealingPercent = new(34, 53);
        public UpdateField<float> ModHealingDonePercent = new(34, 54);
        public UpdateField<float> ModPeriodicHealingDonePercent = new(34, 55);
        public UpdateField<float> ModSpellPowerPercent = new(34, 56);
        public UpdateField<float> ModResiliencePercent = new(34, 57);
        public UpdateField<float> OverrideSpellPowerByAPPercent = new(34, 58);
        public UpdateField<float> OverrideAPBySpellPowerPercent = new(34, 59);
        public UpdateField<int> ModTargetResistance = new(34, 60);
        public UpdateField<int> ModTargetPhysicalResistance = new(34, 61);
        public UpdateField<uint> LocalFlags = new(34, 62);
        public UpdateField<byte> GrantableLevels = new(34, 63);
        public UpdateField<byte> MultiActionBars = new(34, 64);
        public UpdateField<byte> LifetimeMaxRank = new(34, 65);
        public UpdateField<byte> NumRespecs = new(66, 67);
        public UpdateField<int> AmmoID = new(66, 68);
        public UpdateField<uint> PvpMedals = new(66, 69);
        public UpdateField<ushort> TodayHonorableKills = new (66, 70);
        public UpdateField<ushort> TodayDishonorableKills = new (66, 6719);
        public UpdateField<ushort> YesterdayHonorableKills = new (66, 72);
        public UpdateField<ushort> YesterdayDishonorableKills = new (66, 73);
        public UpdateField<ushort> LastWeekHonorableKills = new (66, 74);
        public UpdateField<ushort> LastWeekDishonorableKills = new (66, 75);
        public UpdateField<ushort> ThisWeekHonorableKills = new (66, 76);
        public UpdateField<ushort> ThisWeekDishonorableKills = new (66, 77);
        public UpdateField<uint> ThisWeekContribution = new(66, 78);
        public UpdateField<uint> LifetimeHonorableKills = new(66, 79);
        public UpdateField<uint> LifetimeDishonorableKills = new(66, 80);
        public UpdateField<uint> YesterdayContribution = new(66, 81);
        public UpdateField<uint> LastWeekContribution = new(66, 82);
        public UpdateField<uint> LastWeekRank = new(66, 83);
        public UpdateField<uint> WatchedFactionIndex = new(66, 84);
        public UpdateField<int> MaxLevel = new(66, 85);
        public UpdateField<int> ScalingPlayerLevelDelta = new(66, 86);
        public UpdateField<int> MaxCreatureScalingLevel = new(66, 87);
        public UpdateField<uint> PetSpellPower = new(66, 88);
        public UpdateField<float> UiHitModifier = new(66, 89);
        public UpdateField<float> UiSpellHitModifier = new(66, 90);
        public UpdateField<int> HomeRealmTimeOffset = new(66, 91);
        public UpdateField<float> ModPetHaste = new(66, 92);
        public UpdateField<byte> LocalRegenFlags = new(66, 93);
        public UpdateField<byte> AuraVision = new(66, 94);
        public UpdateField<byte> NumBackpackSlots = new(66, 95);
        public UpdateField<uint> OverrideSpellsID = new(66, 96);
        public UpdateField<int> LfgBonusFactionID = new(66, 97);
        public UpdateField<ushort> LootSpecID = new(98, 99);
        public UpdateField<uint> OverrideZonePVPType = new(98, 100);
        public UpdateField<uint> Honor = new(98, 101);
        public UpdateField<uint> HonorNextLevel = new(98, 102);
        public UpdateField<int> PvpTierMaxFromWins = new(98, 103);
        public UpdateField<int> PvpLastWeeksTierMaxFromWins = new(98, 104);
        public UpdateField<byte> NumBankSlots = new(98, 105);
        public UpdateField<int> TransportServerTime = new(98, 106);
        public UpdateField<byte> Unk340_2 = new(98, 107); 
        public UpdateFieldArray<ObjectGuid> InvSlots = new(129, 108, 109);
        public UpdateFieldArray<uint> TrackResourceMask = new(2, 238, 239);
        public UpdateFieldArray<float> SpellCritPercentage = new(7, 241, 242);
        public UpdateFieldArray<int> ModDamageDonePos = new(7, 241, 249);
        public UpdateFieldArray<int> ModDamageDoneNeg = new(7, 241, 256);
        public UpdateFieldArray<float> ModDamageDonePercent = new(7, 241, 263);
        public UpdateFieldArray<ulong> ExploredZones = new(240, 270, 271);
        public UpdateFieldArray<RestInfo> RestInfo = new(2, 511, 512);
        public UpdateFieldArray<float> WeaponDmgMultipliers = new(3, 514, 515);
        public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new(3, 514, 518);
        public UpdateFieldArray<uint> BuybackPrice = new(12, 521, 522);
        public UpdateFieldArray<long> BuybackTimestamp = new(12, 521, 534);
        public UpdateFieldArray<uint> CombatRatings = new(32, 546, 547);
        public UpdateFieldArray<PVPInfo> PvpInfo = new(6, 579, 580);
        public UpdateFieldArray<uint> NoReagentCostMask = new(4, 586, 587);
        public UpdateFieldArray<uint> ProfessionSkillLine = new(2, 591, 592);
        public UpdateFieldArray<uint> BagSlotFlags = new(4, 594, 595);
        public UpdateFieldArray<uint> BankBagSlotFlags = new(7, 599, 600);
        public UpdateFieldArray<ulong> QuestCompleted = new(875, 607, 608);
        UpdateFieldArray<GlyphInfo> GlyphInfos = new (6, 1483, 1484);

        public ActivePlayerData() : base(0, TypeId.ActivePlayer, 1554)
        {
            ExploredZonesSize = ExploredZones.GetSize();
            ExploredZonesBits = sizeof(ulong) * 8;

            QuestCompletedBitsSize = QuestCompleted.GetSize();
            QuestCompletedBitsPerBlock = sizeof(ulong) * 8;
        }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            for (int i = 0; i < 129; ++i)
            {
                data.WritePackedGuid(InvSlots[i]);
            }
            data.WritePackedGuid(FarsightObject);
            data.WritePackedGuid(ComboTarget);
            data.WriteInt32(KnownTitles.Size());
            data.WriteUInt64(Coinage);
            data.WriteUInt32(XP);
            data.WriteUInt32(NextLevelXP);
            data.WriteInt32(TrialXP);
            ((SkillInfo)Skill).WriteCreate(data, owner, receiver);
            data.WriteUInt32(CharacterPoints);
            data.WriteUInt32(MaxTalentTiers);
            data.WriteUInt32(TrackCreatureMask);
            for (int i = 0; i < 2; ++i)
            {
                data.WriteUInt32(TrackResourceMask[i]);
            }
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
            for (int i = 0; i < 7; ++i)
            {
                data.WriteFloat(SpellCritPercentage[i]);
                data.WriteInt32(ModDamageDonePos[i]);
                data.WriteInt32(ModDamageDoneNeg[i]);
                data.WriteFloat(ModDamageDonePercent[i]);
            }
            data.WriteUInt32(ShieldBlock);
            data.WriteFloat(Mastery);
            data.WriteFloat(Speed);
            data.WriteFloat(Avoidance);
            data.WriteFloat(Sturdiness);
            data.WriteFloat(Unk340_3);
            data.WriteInt32(Versatility);
            data.WriteFloat(VersatilityBonus);
            data.WriteFloat(PvpPowerDamage);
            data.WriteFloat(PvpPowerHealing);
            for (int i = 0; i < 240; ++i)
            {
                data.WriteUInt64(ExploredZones[i]);
            }
            for (int i = 0; i < 2; ++i)
            {
                RestInfo[i].WriteCreate(data, owner, receiver);
            }
            data.WriteInt32(ModHealingDonePos);
            data.WriteFloat(ModHealingPercent);
            data.WriteFloat(ModHealingDonePercent);
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
            data.WriteInt32(AmmoID);
            data.WriteUInt32(PvpMedals);
            for (int i = 0; i < 12; ++i)
            {
                data.WriteUInt32(BuybackPrice[i]);
                data.WriteInt64(BuybackTimestamp[i]);
            }
            data.WriteUInt16(TodayHonorableKills);
            data.WriteUInt16(TodayDishonorableKills);
            data.WriteUInt16(YesterdayHonorableKills);
            data.WriteUInt16(YesterdayDishonorableKills);
            data.WriteUInt16(LastWeekHonorableKills);
            data.WriteUInt16(LastWeekDishonorableKills);
            data.WriteUInt16(ThisWeekHonorableKills);
            data.WriteUInt16(ThisWeekDishonorableKills);
            data.WriteUInt32(ThisWeekContribution);
            data.WriteUInt32(LifetimeHonorableKills);
            data.WriteUInt32(LifetimeDishonorableKills);
            data.WriteUInt32(YesterdayContribution);
            data.WriteUInt32(LastWeekContribution);
            data.WriteUInt32(LastWeekRank);
            data.WriteUInt32(WatchedFactionIndex);
            for (int i = 0; i < 32; ++i)
            {
                data.WriteUInt32(CombatRatings[i]);
            }
            data.WriteInt32(MaxLevel);
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
            data.WriteUInt8(LocalRegenFlags);
            data.WriteUInt8(AuraVision);
            data.WriteUInt8(NumBackpackSlots);
            data.WriteUInt32(OverrideSpellsID);
            data.WriteInt32(LfgBonusFactionID);
            data.WriteUInt16(LootSpecID);
            data.WriteUInt32(OverrideZonePVPType);
            for (int i = 0; i < 4; ++i)
            {
                data.WriteUInt32(BagSlotFlags[i]);
            }
            for (int i = 0; i < 7; ++i)
            {
                data.WriteUInt32(BankBagSlotFlags[i]);
            }
            for (int i = 0; i < 875; ++i)
            {
                data.WriteUInt64(QuestCompleted[i]);
            }
            data.WriteUInt32(Honor);
            data.WriteUInt32(HonorNextLevel);
            data.WriteInt32(PvpTierMaxFromWins);
            data.WriteInt32(PvpLastWeeksTierMaxFromWins);
            data.WriteUInt8(NumBankSlots);
            data.WriteInt32(ResearchSites.Size());
            data.WriteInt32(ResearchSiteProgress.Size());
            data.WriteInt32(DailyQuestsCompleted.Size());
            data.WriteInt32(AvailableQuestLineXQuestIDs.Size());
            data.WriteInt32(Unk254.Size());
            data.WriteInt32(Heirlooms.Size());
            data.WriteInt32(HeirloomFlags.Size());
            data.WriteInt32(Toys.Size());
            data.WriteInt32(Transmog.Size());
            data.WriteInt32(ConditionalTransmog.Size());
            data.WriteInt32(SelfResSpells.Size());
            data.WriteInt32(CharacterRestrictions.Size());
            data.WriteInt32(SpellPctModByLabel.Size());
            data.WriteInt32(SpellFlatModByLabel.Size());
            for (int i = 0; i < 1; ++i)
            {
                data.WriteInt32(Research[i].Size());
                for (int j = 0; j < Research[i].Size(); ++j)
                {
                    Research[i][j].WriteCreate(data, owner, receiver);
                }
            }
            data.WriteInt32(TransportServerTime);
            for (int i = 0; i < 6; ++i)
            {
                GlyphInfos[i].WriteCreate(data, owner, receiver);
            }
            data.WriteUInt8(Unk340_2);
            for (int i = 0; i < KnownTitles.Size(); ++i)
            {
                data.WriteUInt64(KnownTitles[i]);
            }
            for (int i = 0; i < ResearchSites.Size(); ++i)
            {
                data.WriteUInt16(ResearchSites[i]);
            }
            for (int i = 0; i < ResearchSiteProgress.Size(); ++i)
            {
                data.WriteUInt32(ResearchSiteProgress[i]);
            }
            for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
            {
                data.WriteUInt32(DailyQuestsCompleted[i]);
            }
            for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
            {
                data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
            }
            for (int i = 0; i < Unk254.Size(); ++i)
            {
                data.WriteInt32(Unk254[i]);
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
            for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
            {
                SpellPctModByLabel[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
            {
                SpellFlatModByLabel[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < 6; ++i)
            {
                PvpInfo[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
            data.WriteBit(InsertItemsLeftToRight);
            for (int i = 0; i < CharacterRestrictions.Size(); ++i)
            {
                CharacterRestrictions[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
        {
            for (uint i = 0; i < 1; ++i)
                data.WriteUInt32(changesMask.GetBlocksMask(i));

            data.WriteBits(changesMask.GetBlocksMask(1), 15);
            for (uint i = 0; i < 47; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBit(InsertItemsLeftToRight);
                }
                if (changesMask[2])
                {
                    if (!ignoreNestedChangesMask)
                        KnownTitles.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(KnownTitles.Size(), data);
                }
                if (changesMask[3])
                {
                    if (!ignoreNestedChangesMask)
                        ResearchSites.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ResearchSites.Size(), data);
                }
                if (changesMask[4])
                {
                    if (!ignoreNestedChangesMask)
                        ResearchSiteProgress.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ResearchSiteProgress.Size(), data);
                }
                if (changesMask[5])
                {
                    if (!ignoreNestedChangesMask)
                        DailyQuestsCompleted.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(DailyQuestsCompleted.Size(), data);
                }
                if (changesMask[6])
                {
                    if (!ignoreNestedChangesMask)
                        AvailableQuestLineXQuestIDs.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(AvailableQuestLineXQuestIDs.Size(), data);
                }
                if (changesMask[7])
                {
                    if (!ignoreNestedChangesMask)
                        Unk254.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Unk254.Size(), data);
                }
                if (changesMask[8])
                {
                    if (!ignoreNestedChangesMask)
                        Heirlooms.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Heirlooms.Size(), data);
                }
                if (changesMask[9])
                {
                    if (!ignoreNestedChangesMask)
                        HeirloomFlags.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HeirloomFlags.Size(), data);
                }
                if (changesMask[10])
                {
                    if (!ignoreNestedChangesMask)
                        Toys.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Toys.Size(), data);
                }
                if (changesMask[11])
                {
                    if (!ignoreNestedChangesMask)
                        Transmog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Transmog.Size(), data);
                }
                if (changesMask[12])
                {
                    if (!ignoreNestedChangesMask)
                        ConditionalTransmog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ConditionalTransmog.Size(), data);
                }
                if (changesMask[13])
                {
                    if (!ignoreNestedChangesMask)
                        SelfResSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SelfResSpells.Size(), data);
                }
                if (changesMask[14])
                {
                    if (!ignoreNestedChangesMask)
                        CharacterRestrictions.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterRestrictions.Size(), data);
                }
                if (changesMask[15])
                {
                    if (!ignoreNestedChangesMask)
                        SpellPctModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellPctModByLabel.Size(), data);
                }
                if (changesMask[16])
                {
                    if (!ignoreNestedChangesMask)
                        SpellFlatModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellFlatModByLabel.Size(), data);
                }
            }
            if (changesMask[17])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[18 + i])
                    {
                        if (!ignoreNestedChangesMask)
                            Research[i].WriteUpdateMask(data);
                        else
                            WriteCompleteDynamicFieldUpdateMask(Research[i].Size(), data);
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
            data.FlushBits(); //TODO: is need to twice FlushBits?             
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    for (int i = 0; i < KnownTitles.Size(); ++i)
                    {
                        if (KnownTitles.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt64(KnownTitles[i]);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < ResearchSites.Size(); ++i)
                    {
                        if (ResearchSites.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt16(ResearchSites[i]);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < ResearchSiteProgress.Size(); ++i)
                    {
                        if (ResearchSiteProgress.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ResearchSiteProgress[i]);
                        }
                    }
                }
                if (changesMask[5])
                {
                    for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
                    {
                        if (DailyQuestsCompleted.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(DailyQuestsCompleted[i]);
                        }
                    }
                }
                if (changesMask[6])
                {
                    for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
                    {
                        if (AvailableQuestLineXQuestIDs.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
                        }
                    }
                }
                if (changesMask[7])
                {
                    for (int i = 0; i < Unk254.Size(); ++i)
                    {
                        if (Unk254.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(Unk254[i]);
                        }
                    }
                }
                if (changesMask[8])
                {
                    for (int i = 0; i < Heirlooms.Size(); ++i)
                    {
                        if (Heirlooms.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Heirlooms[i]);
                        }
                    }
                }
                if (changesMask[9])
                {
                    for (int i = 0; i < HeirloomFlags.Size(); ++i)
                    {
                        if (HeirloomFlags.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HeirloomFlags[i]);
                        }
                    }
                }
                if (changesMask[10])
                {
                    for (int i = 0; i < Toys.Size(); ++i)
                    {
                        if (Toys.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Toys[i]);
                        }
                    }
                }
                if (changesMask[11])
                {
                    for (int i = 0; i < Transmog.Size(); ++i)
                    {
                        if (Transmog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Transmog[i]);
                        }
                    }
                }
                if (changesMask[12])
                {
                    for (int i = 0; i < ConditionalTransmog.Size(); ++i)
                    {
                        if (ConditionalTransmog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ConditionalTransmog[i]);
                        }
                    }
                }
                if (changesMask[13])
                {
                    for (int i = 0; i < SelfResSpells.Size(); ++i)
                    {
                        if (SelfResSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(SelfResSpells[i]);
                        }
                    }
                }
                if (changesMask[15])
                {
                    for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
                    {
                        if (SpellPctModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellPctModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[16])
                {
                    for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
                    {
                        if (SpellFlatModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellFlatModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[14])
                {
                    for (int i = 0; i < CharacterRestrictions.Size(); ++i)
                    {
                        if (CharacterRestrictions.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterRestrictions[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[19])
                {
                    data.WritePackedGuid(FarsightObject);
                }
                if (changesMask[20])
                {
                    data.WritePackedGuid(ComboTarget);
                }
                if (changesMask[21])
                {
                    data.WriteUInt64(Coinage);
                }
                if (changesMask[22])
                {
                    data.WriteUInt32(XP); //TODO: may be Int32? this is in TC
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(NextLevelXP); //TODO: may be Int32? this is in TC
                }
                if (changesMask[24])
                {
                    data.WriteInt32(TrialXP);
                }
                if (changesMask[25])
                {
                    ((SkillInfo)Skill).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[26])
                {
                    data.WriteUInt32(CharacterPoints); //TODO: may be Int32? this is in TC
                }
                if (changesMask[27])
                {
                    data.WriteUInt32(MaxTalentTiers); //TODO: may be Int32? this is in TC
                }
                if (changesMask[28])
                {
                    data.WriteUInt32(TrackCreatureMask);
                }
                if (changesMask[29])
                {
                    data.WriteFloat(MainhandExpertise);
                }
                if (changesMask[30])
                {
                    data.WriteFloat(OffhandExpertise);
                }
                if (changesMask[31])
                {
                    data.WriteFloat(RangedExpertise);
                }
                if (changesMask[32])
                {
                    data.WriteFloat(CombatRatingExpertise);
                }
                if (changesMask[33])
                {
                    data.WriteFloat(BlockPercentage);
                }
            }
            if (changesMask[34])
            {
                if (changesMask[35])
                {
                    data.WriteFloat(DodgePercentage);
                }
                if (changesMask[36])
                {
                    data.WriteFloat(DodgePercentageFromAttribute);
                }
                if (changesMask[37])
                {
                    data.WriteFloat(ParryPercentage);
                }
                if (changesMask[38])
                {
                    data.WriteFloat(ParryPercentageFromAttribute);
                }
                if (changesMask[39])
                {
                    data.WriteFloat(CritPercentage);
                }
                if (changesMask[40])
                {
                    data.WriteFloat(RangedCritPercentage);
                }
                if (changesMask[41])
                {
                    data.WriteFloat(OffhandCritPercentage);
                }
                if (changesMask[42])
                {
                    data.WriteUInt32(ShieldBlock);
                }
                if (changesMask[43])
                {
                    data.WriteFloat(Mastery);
                }
                if (changesMask[44])
                {
                    data.WriteFloat(Speed);
                }
                if (changesMask[45])
                {
                    data.WriteFloat(Avoidance);
                }
                if (changesMask[46])
                {
                    data.WriteFloat(Sturdiness);
                }
                if (changesMask[47])
                {
                    data.WriteFloat(Unk340_3);
                }
                if (changesMask[48])
                {
                    data.WriteInt32(Versatility);
                }
                if (changesMask[49])
                {
                    data.WriteFloat(VersatilityBonus);
                }
                if (changesMask[50])
                {
                    data.WriteFloat(PvpPowerDamage);
                }
                if (changesMask[51])
                {
                    data.WriteFloat(PvpPowerHealing);
                }
                if (changesMask[52])
                {
                    data.WriteInt32(ModHealingDonePos);
                }
                if (changesMask[53])
                {
                    data.WriteFloat(ModHealingPercent);
                }
                if (changesMask[54])
                {
                    data.WriteFloat(ModHealingDonePercent);
                }
                if (changesMask[55])
                {
                    data.WriteFloat(ModPeriodicHealingDonePercent);
                }
                if (changesMask[56])
                {
                    data.WriteFloat(ModSpellPowerPercent);
                }
                if (changesMask[57])
                {
                    data.WriteFloat(ModResiliencePercent);
                }
                if (changesMask[58])
                {
                    data.WriteFloat(OverrideSpellPowerByAPPercent);
                }
                if (changesMask[59])
                {
                    data.WriteFloat(OverrideAPBySpellPowerPercent);
                }
                if (changesMask[60])
                {
                    data.WriteInt32(ModTargetResistance);
                }
                if (changesMask[61])
                {
                    data.WriteInt32(ModTargetPhysicalResistance);
                }
                if (changesMask[62])
                {
                    data.WriteUInt32(LocalFlags);
                }
                if (changesMask[63])
                {
                    data.WriteUInt8(GrantableLevels);
                }
                if (changesMask[64])
                {
                    data.WriteUInt8(MultiActionBars);
                }
                if (changesMask[65])
                {
                    data.WriteUInt8(LifetimeMaxRank);
                }
            }
            if (changesMask[66])
            {

                if (changesMask[67])
                {
                    data.WriteUInt8(NumRespecs);
                }
                if (changesMask[68])
                {
                    data.WriteInt32(AmmoID);
                }
                if (changesMask[69])
                {
                    data.WriteUInt32(PvpMedals);
                }
                if (changesMask[70])
                {
                    data.WriteUInt16(TodayHonorableKills);
                }
                if (changesMask[71])
                {
                    data.WriteUInt16(TodayDishonorableKills);
                }
                if (changesMask[72])
                {
                    data.WriteUInt16(YesterdayHonorableKills);
                }
                if (changesMask[73])
                {
                    data.WriteUInt16(YesterdayDishonorableKills);
                }
                if (changesMask[74])
                {
                    data.WriteUInt16(LastWeekHonorableKills);
                }
                if (changesMask[75])
                {
                    data.WriteUInt16(LastWeekDishonorableKills);
                }
                if (changesMask[76])
                {
                    data.WriteUInt16(ThisWeekHonorableKills);
                }
                if (changesMask[77])
                {
                    data.WriteUInt16(ThisWeekDishonorableKills);
                }
                if (changesMask[78])
                {
                    data.WriteUInt32(ThisWeekContribution);
                }
                if (changesMask[79])
                {
                    data.WriteUInt32(LifetimeHonorableKills);
                }
                if (changesMask[80])
                {
                    data.WriteUInt32(LifetimeDishonorableKills);
                }
                if (changesMask[81])
                {
                    data.WriteUInt32(YesterdayContribution);
                }
                if (changesMask[82])
                {
                    data.WriteUInt32(LastWeekContribution);
                }
                if (changesMask[83])
                {
                    data.WriteUInt32(LastWeekRank);
                }
                if (changesMask[84])
                {
                    data.WriteUInt32(WatchedFactionIndex); //TODO: may be Int32? this is in TC
                }
                if (changesMask[85])
                {
                    data.WriteInt32(MaxLevel);
                }
                if (changesMask[86])
                {
                    data.WriteInt32(ScalingPlayerLevelDelta);
                }
                if (changesMask[87])
                {
                    data.WriteInt32(MaxCreatureScalingLevel);
                }
                if (changesMask[88])
                {
                    data.WriteUInt32(PetSpellPower); //TODO: may be Int32? this is in TC
                }
                if (changesMask[89])
                {
                    data.WriteFloat(UiHitModifier);
                }
                if (changesMask[90])
                {
                    data.WriteFloat(UiSpellHitModifier);
                }
                if (changesMask[91])
                {
                    data.WriteInt32(HomeRealmTimeOffset);
                }
                if (changesMask[92])
                {
                    data.WriteFloat(ModPetHaste);
                }
                if (changesMask[93])
                {
                    data.WriteUInt8(LocalRegenFlags);
                }
                if (changesMask[94])
                {
                    data.WriteUInt8(AuraVision);
                }
                if (changesMask[95])
                {
                    data.WriteUInt8(NumBackpackSlots);
                }
                if (changesMask[96])
                {
                    data.WriteUInt32(OverrideSpellsID); //TODO: may be Int32? this is in TC
                }
                if (changesMask[97])
                {
                    data.WriteInt32(LfgBonusFactionID);
                }
            }
            if (changesMask[98])
            {
                if (changesMask[99])
                {
                    data.WriteUInt16(LootSpecID);
                }
                if (changesMask[100])
                {
                    data.WriteUInt32(OverrideZonePVPType);
                }
                if (changesMask[101])
                {
                    data.WriteUInt32(Honor); //TODO: may be Int32? this is in TC
                }
                if (changesMask[102])
                {
                    data.WriteUInt32(HonorNextLevel); //TODO: may be Int32? this is in TC
                }
                if (changesMask[103])
                {
                    data.WriteInt32(PvpTierMaxFromWins);
                }
                if (changesMask[104])
                {
                    data.WriteInt32(PvpLastWeeksTierMaxFromWins);
                }
                if (changesMask[105])
                {
                    data.WriteUInt8(NumBankSlots);
                }
                if (changesMask[106])
                {
                    data.WriteInt32(TransportServerTime);
                }
                if (changesMask[107])
                {
                    data.WriteUInt8(Unk340_2);
                }
            }
            if (changesMask[108])
            {
                for (int i = 0; i < 129; ++i)
                {
                    if (changesMask[109 + i])
                    {
                        data.WritePackedGuid(InvSlots[i]);
                    }
                }
            }
            if (changesMask[238])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[239 + i])
                    {
                        data.WriteUInt32(TrackResourceMask[i]);
                    }
                }
            }
            if (changesMask[241])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[242 + i])
                    {
                        data.WriteFloat(SpellCritPercentage[i]);
                    }
                    if (changesMask[249 + i])
                    {
                        data.WriteInt32(ModDamageDonePos[i]);
                    }
                    if (changesMask[256 + i])
                    {
                        data.WriteInt32(ModDamageDoneNeg[i]);
                    }
                    if (changesMask[263 + i])
                    {
                        data.WriteFloat(ModDamageDonePercent[i]);
                    }
                }
            }
            if (changesMask[270])
            {
                for (int i = 0; i < 240; ++i)
                {
                    if (changesMask[271 + i])
                    {
                        data.WriteUInt64(ExploredZones[i]);
                    }
                }
            }
            if (changesMask[511])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[512 + i])
                    {
                        RestInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[514])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[515 + i])
                    {
                        data.WriteFloat(WeaponDmgMultipliers[i]);
                    }
                    if (changesMask[518 + i])
                    {
                        data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
                    }
                }
            }
            if (changesMask[521])
            {
                for (int i = 0; i < 12; ++i)
                {
                    if (changesMask[522 + i])
                    {
                        data.WriteUInt32(BuybackPrice[i]);
                    }
                    if (changesMask[534 + i])
                    {
                        data.WriteInt64(BuybackTimestamp[i]);
                    }
                }
            }
            if (changesMask[546])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (changesMask[547 + i])
                    {
                        data.WriteUInt32(CombatRatings[i]); //TODO: may be Int32? this is in TC
                    }
                }
            }
            if (changesMask[586])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[587 + i])
                    {
                        data.WriteUInt32(NoReagentCostMask[i]);
                    }
                }
            }
            if (changesMask[591])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[592 + i])
                    {
                        data.WriteUInt32(ProfessionSkillLine[i]); //TODO: may be Int32? this is in TC
                    }
                }
            }
            if (changesMask[594])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[595 + i])
                    {
                        data.WriteUInt32(BagSlotFlags[i]);
                    }
                }
            }
            if (changesMask[599])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[600 + i])
                    {
                        data.WriteUInt32(BankBagSlotFlags[i]);
                    }
                }
            }
            if (changesMask[607])
            {
                for (int i = 0; i < 875; ++i)
                {
                    if (changesMask[608 + i])
                    {
                        data.WriteUInt64(QuestCompleted[i]);
                    }
                }
            }
            if (changesMask[1483])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[1484 + i])
                    {
                        GlyphInfos[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[579])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[580 + i])
                    {
                        PvpInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(InsertItemsLeftToRight);
            ClearChangesMask(Research);
            ClearChangesMask(KnownTitles);
            ClearChangesMask(ResearchSites);
            ClearChangesMask(ResearchSiteProgress);
            ClearChangesMask(DailyQuestsCompleted);
            ClearChangesMask(AvailableQuestLineXQuestIDs);
            ClearChangesMask(Unk254);
            ClearChangesMask(Heirlooms);
            ClearChangesMask(HeirloomFlags);
            ClearChangesMask(Toys);
            ClearChangesMask(Transmog);
            ClearChangesMask(ConditionalTransmog);
            ClearChangesMask(SelfResSpells);
            ClearChangesMask(SpellPctModByLabel);
            ClearChangesMask(SpellFlatModByLabel);
            ClearChangesMask(CharacterRestrictions);
            ClearChangesMask(FarsightObject);
            ClearChangesMask(ComboTarget);
            ClearChangesMask(Coinage);
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
            ClearChangesMask(ShieldBlock);
            ClearChangesMask(Mastery);
            ClearChangesMask(Speed);
            ClearChangesMask(Avoidance);
            ClearChangesMask(Sturdiness);
            ClearChangesMask(Unk340_3);
            ClearChangesMask(Versatility);
            ClearChangesMask(VersatilityBonus);
            ClearChangesMask(PvpPowerDamage);
            ClearChangesMask(PvpPowerHealing);
            ClearChangesMask(ModHealingDonePos);
            ClearChangesMask(ModHealingPercent);
            ClearChangesMask(ModHealingDonePercent);
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
            ClearChangesMask(AmmoID);
            ClearChangesMask(PvpMedals);
            ClearChangesMask(TodayHonorableKills);
            ClearChangesMask(TodayDishonorableKills);
            ClearChangesMask(YesterdayHonorableKills);
            ClearChangesMask(YesterdayDishonorableKills);
            ClearChangesMask(LastWeekHonorableKills);
            ClearChangesMask(LastWeekDishonorableKills);
            ClearChangesMask(ThisWeekHonorableKills);
            ClearChangesMask(ThisWeekDishonorableKills);
            ClearChangesMask(ThisWeekContribution);
            ClearChangesMask(LifetimeHonorableKills);
            ClearChangesMask(LifetimeDishonorableKills);
            ClearChangesMask(YesterdayContribution);
            ClearChangesMask(LastWeekContribution);
            ClearChangesMask(LastWeekRank);
            ClearChangesMask(WatchedFactionIndex);
            ClearChangesMask(MaxLevel);
            ClearChangesMask(ScalingPlayerLevelDelta);
            ClearChangesMask(MaxCreatureScalingLevel);
            ClearChangesMask(PetSpellPower);
            ClearChangesMask(UiHitModifier);
            ClearChangesMask(UiSpellHitModifier);
            ClearChangesMask(HomeRealmTimeOffset);
            ClearChangesMask(ModPetHaste);
            ClearChangesMask(LocalRegenFlags);
            ClearChangesMask(AuraVision);
            ClearChangesMask(NumBackpackSlots);
            ClearChangesMask(OverrideSpellsID);
            ClearChangesMask(LfgBonusFactionID);
            ClearChangesMask(LootSpecID);
            ClearChangesMask(OverrideZonePVPType);
            ClearChangesMask(Honor);
            ClearChangesMask(HonorNextLevel);
            ClearChangesMask(PvpTierMaxFromWins);
            ClearChangesMask(PvpLastWeeksTierMaxFromWins);
            ClearChangesMask(NumBankSlots);
            ClearChangesMask(TransportServerTime);
            ClearChangesMask(Unk340_2);
            ClearChangesMask(InvSlots);
            ClearChangesMask(TrackResourceMask);
            ClearChangesMask(SpellCritPercentage);
            ClearChangesMask(ModDamageDonePos);
            ClearChangesMask(ModDamageDoneNeg);
            ClearChangesMask(ModDamageDonePercent);
            ClearChangesMask(ExploredZones);
            ClearChangesMask(RestInfo);
            ClearChangesMask(WeaponDmgMultipliers);
            ClearChangesMask(WeaponAtkSpeedMultipliers);
            ClearChangesMask(BuybackPrice);
            ClearChangesMask(BuybackTimestamp);
            ClearChangesMask(CombatRatings);
            ClearChangesMask(PvpInfo);
            ClearChangesMask(NoReagentCostMask);
            ClearChangesMask(ProfessionSkillLine);
            ClearChangesMask(BagSlotFlags);
            ClearChangesMask(BankBagSlotFlags);
            ClearChangesMask(QuestCompleted);
            ClearChangesMask(GlyphInfos);
            _changesMask.ResetAll();
        }
    }

    public class GameObjectFieldData : BaseUpdateData<GameObject>
    {
        public UpdateField<List<uint>> StateWorldEffectIDs = new(0, 1);
        public DynamicUpdateField<int> EnableDoodadSets = new(0, 2);
        public UpdateField<uint> DisplayID = new(0, 3);
        public UpdateField<uint> SpellVisualID = new(0, 4);
        public UpdateField<uint> StateSpellVisualID = new(0, 5);
        public UpdateField<uint> SpawnTrackingStateAnimID = new(0, 6);
        public UpdateField<uint> SpawnTrackingStateAnimKitID = new(0, 7);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 8);
        public UpdateField<ObjectGuid> GuildGUID = new(0, 9);
        public UpdateField<uint> Flags = new(0, 10);
        public UpdateField<Quaternion> ParentRotation = new(0, 11);
        public UpdateField<uint> FactionTemplate = new(0, 12);
        public UpdateField<uint> Level = new(0, 13);
        public UpdateField<sbyte> State = new(0, 14);
        public UpdateField<sbyte> TypeID = new(0, 15);
        public UpdateField<byte> PercentHealth = new(0, 16);
        public UpdateField<uint> ArtKit = new(0, 17);
        public UpdateField<uint> CustomParam = new(0, 18);

        public GameObjectFieldData() : base(0, TypeId.GameObject, 19) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
        {
            data.WriteUInt32(DisplayID);
            data.WriteUInt32(SpellVisualID);
            data.WriteUInt32(StateSpellVisualID);
            data.WriteUInt32(SpawnTrackingStateAnimID);
            data.WriteUInt32(SpawnTrackingStateAnimKitID);
            data.WriteInt32(((List<uint>)StateWorldEffectIDs).Count);
            for (int i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
            {
                data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);
            }
            data.WritePackedGuid(CreatedBy);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GetViewerGameObjectFlags(this, owner, receiver));
            Quaternion rotation = ParentRotation;
            data.WriteFloat(rotation.X);
            data.WriteFloat(rotation.Y);
            data.WriteFloat(rotation.Z);
            data.WriteFloat(rotation.W);
            data.WriteUInt32(FactionTemplate);
            data.WriteUInt32(Level); //TODO: may be Int32? this is in TC
            data.WriteInt8(GetViewerGameObjectState(this, owner, receiver));
            data.WriteInt8(TypeID);
            data.WriteUInt8(PercentHealth);
            data.WriteUInt32(ArtKit);
            data.WriteInt32(EnableDoodadSets.Size());
            data.WriteUInt32(CustomParam);
            for (int i = 0; i < EnableDoodadSets.Size(); ++i)
            {
                data.WriteInt32(EnableDoodadSets[i]);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, GameObject owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlock(0), 19);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteBits(((List<uint>)StateWorldEffectIDs).Count, 32);
                    for (int i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
                    {
                        data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);
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
                    data.WriteUInt32(DisplayID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt32(SpellVisualID);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(StateSpellVisualID);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(SpawnTrackingStateAnimID);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(SpawnTrackingStateAnimKitID);
                }
                if (changesMask[8])
                { 
                    data.WritePackedGuid(CreatedBy);
                }
                if (changesMask[9])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(GetViewerGameObjectFlags(this, owner, receiver));
                }
                if (changesMask[11])
                {
                    data.WriteFloat(((Quaternion)ParentRotation).X);
                    data.WriteFloat(((Quaternion)ParentRotation).Y);
                    data.WriteFloat(((Quaternion)ParentRotation).Z);
                    data.WriteFloat(((Quaternion)ParentRotation).W);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(FactionTemplate); //TODO: may be Int32? this is in TC
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(Level); //TODO: may be Int32? this is in TC
                }
                if (changesMask[14])
                {
                    data.WriteInt8(GetViewerGameObjectState(this, owner, receiver));
                }
                if (changesMask[15])
                {
                    data.WriteInt8(TypeID);
                }
                if (changesMask[16])
                {
                    data.WriteUInt8(PercentHealth);
                }
                if (changesMask[17])
                {
                    data.WriteUInt32(ArtKit);
                }
                if (changesMask[18])
                {
                    data.WriteUInt32(CustomParam);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(StateWorldEffectIDs);
            ClearChangesMask(EnableDoodadSets);
            ClearChangesMask(DisplayID);
            ClearChangesMask(SpellVisualID);
            ClearChangesMask(StateSpellVisualID);
            ClearChangesMask(SpawnTrackingStateAnimID);
            ClearChangesMask(SpawnTrackingStateAnimKitID);
            ClearChangesMask(CreatedBy);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(Flags);
            ClearChangesMask(ParentRotation);
            ClearChangesMask(FactionTemplate);
            ClearChangesMask(Level);
            ClearChangesMask(State);
            ClearChangesMask(TypeID);
            ClearChangesMask(PercentHealth);
            ClearChangesMask(ArtKit);
            ClearChangesMask(CustomParam);
            _changesMask.ResetAll();
        }

        uint GetViewerGameObjectFlags(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            uint flags = gameObjectData.Flags;
            if (gameObject.GetGoType() == GameObjectTypes.Chest)
                if (gameObject.GetGoInfo().Chest.usegrouplootrules != 0 && !gameObject.IsLootAllowedFor(receiver))
                    flags |= (uint)(GameObjectFlags.Locked | GameObjectFlags.NotSelectable);

            return flags;
        }

        sbyte GetViewerGameObjectState(GameObjectFieldData gameObjectData, GameObject gameObject, Player receiver)
        {
            return (sbyte)gameObject.GetGoStateFor(receiver.GetGUID());
        }
    }

    public class DynamicObjectData : BaseUpdateData<DynamicObject>
    {
        public UpdateField<ObjectGuid> Caster = new(0, 1);
        public UpdateField<byte> Type = new(0, 2);
        public UpdateField<uint> SpellXSpellVisualID = new(0, 3);
        public UpdateField<uint> SpellID = new(0, 4);
        public UpdateField<float> Radius = new(0, 5);
        public UpdateField<uint> CastTime = new(0, 6);

        public DynamicObjectData() : base(0, TypeId.DynamicObject, 7) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
        {
            data.WritePackedGuid(Caster);
            data.WriteUInt8(Type);
            data.WriteUInt32(SpellXSpellVisualID);
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
                    data.WriteUInt32(SpellXSpellVisualID);
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
            ClearChangesMask(SpellXSpellVisualID);
            ClearChangesMask(SpellID);
            ClearChangesMask(Radius);
            ClearChangesMask(CastTime);
            _changesMask.ResetAll();
        }
    }

    public class CorpseData : BaseUpdateData<Corpse>
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
        public UpdateFieldArray<uint> Items = new(19, 12, 13);

        public CorpseData() : base(0, TypeId.Corpse, 32) { }

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
            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            for (uint i = 0; i < 1; ++i)
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
            }
            if (changesMask[12])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[13 + i])
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
            ClearChangesMask(Items);
            _changesMask.ResetAll();
        }
    }

    public class ScaleCurve : BaseUpdateData<AreaTrigger>
    {
        public UpdateField<bool> OverrideActive = new(0, 1);
        public UpdateField<uint> StartTimeOffset = new(0, 2);
        public UpdateField<uint> ParameterCurve = new(0, 3);
        public UpdateFieldArray<Vector2> Points = new(2, 4, 5);

        public ScaleCurve() : base(7) { }

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

    public class VisualAnim : BaseUpdateData<AreaTrigger>
    {
        public UpdateField<bool> Field_C = new(0, 1);
        public UpdateField<int> AnimationDataID = new(0, 2);
        public UpdateField<uint> AnimKitID = new(0, 3);
        public UpdateField<uint> AnimProgress = new(0, 4);

        public VisualAnim() : base(0, TypeId.AreaTrigger, 5) { }

        public void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteInt32(AnimationDataID);
            data.WriteUInt32(AnimKitID);
            data.WriteUInt32(AnimProgress);
            data.WriteBit(Field_C);
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
                    data.WriteBit(Field_C);
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
            ClearChangesMask(Field_C);
            ClearChangesMask(AnimationDataID);
            ClearChangesMask(AnimKitID);
            ClearChangesMask(AnimProgress);
            _changesMask.ResetAll();
        }
    }

    public class AreaTriggerFieldData : BaseUpdateData<AreaTrigger>
    {
        public UpdateField<ScaleCurve> OverrideScaleCurve = new(0, 1);
        public UpdateField<ScaleCurve> ExtraScaleCurve = new(0, 2);
        public UpdateField<ObjectGuid> Caster = new(0, 3);
        public UpdateField<uint> Duration = new(0, 4);
        public UpdateField<uint> TimeToTarget = new(0, 5);
        public UpdateField<uint> TimeToTargetScale = new(0, 6);
        public UpdateField<uint> TimeToTargetExtraScale = new(0, 7);
        public UpdateField<uint> SpellID = new(0, 8);
        public UpdateField<uint> SpellForVisuals = new(0, 9);
        public UpdateField<uint> SpellXSpellVisualID = new(0, 10);
        public UpdateField<float> BoundsRadius2D = new(0, 11);
        public UpdateField<uint> DecalPropertiesID = new(0, 12);
        public UpdateField<ObjectGuid> CreatingEffectGUID = new(0, 13);
        public UpdateField<ObjectGuid> Field_80 = new(0, 14);
        public UpdateField<VisualAnim> VisualAnim = new(0, 15);

        public AreaTriggerFieldData() : base(0, TypeId.AreaTrigger, 16) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
        {
            ((ScaleCurve)OverrideScaleCurve).WriteCreate(data, owner, receiver);
            data.WritePackedGuid(Caster);
            data.WriteUInt32(Duration);
            data.WriteUInt32(TimeToTarget);
            data.WriteUInt32(TimeToTargetScale);
            data.WriteUInt32(TimeToTargetExtraScale);
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellForVisuals);
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteFloat(BoundsRadius2D);
            data.WriteUInt32(DecalPropertiesID);
            data.WritePackedGuid(CreatingEffectGUID);
            data.WritePackedGuid(Field_80);
            ((ScaleCurve)ExtraScaleCurve).WriteCreate(data, owner, receiver);
            ((VisualAnim)VisualAnim).WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AreaTrigger owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 16);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    ((ScaleCurve)OverrideScaleCurve).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (_changesMask[3])
                {
                    data.WritePackedGuid(Caster);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(Duration);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(TimeToTarget);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt32(TimeToTargetScale);
                }
                if (_changesMask[7])
                {
                    data.WriteUInt32(TimeToTargetExtraScale);
                }
                if (_changesMask[8])
                {
                    data.WriteUInt32(SpellID);
                }
                if (_changesMask[9])
                {
                    data.WriteUInt32(SpellForVisuals);
                }
                if (_changesMask[10])
                {
                    data.WriteUInt32(SpellXSpellVisualID);
                }
                if (_changesMask[11])
                {
                    data.WriteFloat(BoundsRadius2D);
                }
                if (_changesMask[12])
                {
                    data.WriteUInt32(DecalPropertiesID);
                }
                if (_changesMask[13])
                {
                    data.WritePackedGuid(CreatingEffectGUID);
                }
                if (changesMask[14])
                {
                    data.WritePackedGuid(Field_80);
                }
                if (_changesMask[2])
                {
                    ((ScaleCurve)ExtraScaleCurve).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[15])
                {
                    ((VisualAnim)VisualAnim).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(OverrideScaleCurve);
            ClearChangesMask(ExtraScaleCurve);
            ClearChangesMask(Caster);
            ClearChangesMask(Duration);
            ClearChangesMask(TimeToTarget);
            ClearChangesMask(TimeToTargetScale);
            ClearChangesMask(TimeToTargetExtraScale);
            ClearChangesMask(SpellID);
            ClearChangesMask(SpellForVisuals);
            ClearChangesMask(SpellXSpellVisualID);
            ClearChangesMask(BoundsRadius2D);
            ClearChangesMask(DecalPropertiesID);
            ClearChangesMask(CreatingEffectGUID);
            ClearChangesMask(Field_80);
            ClearChangesMask(VisualAnim);
            _changesMask.ResetAll();
        }
    }

    public class SceneObjectData : BaseUpdateData<WorldObject>
    {
        public UpdateField<int> ScriptPackageID = new(0, 1);
        public UpdateField<uint> RndSeedVal = new(0, 2);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 3);
        public UpdateField<uint> SceneType = new(0, 4);

        public SceneObjectData() : base(5) { }

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

    public class ConversationLine
    {
        public uint ConversationLineID;
        public uint StartTime;
        public uint UiCameraID;
        public byte ActorIndex;
        public byte Flags;

        public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(GetViewerStartTime(this, owner, receiver));
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(GetViewerStartTime(this, owner, receiver));
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
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
    }

    public class ConversationActorField
    {
        public ConversationActorType Type;
        public int Id;
        public ObjectGuid ActorGUID;
        public uint CreatureID;
        public uint CreatureDisplayInfoID;

        public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteBits(Type, 1);
            data.WriteInt32(Id);
            data.WritePackedGuid(ActorGUID);
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
        {
            data.WriteBits(Type, 1);
            data.WriteInt32(Id);
            data.WritePackedGuid(ActorGUID);
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.FlushBits();
        }
    }

    public class ConversationData : BaseUpdateData<Conversation>
    {
        public UpdateField<List<ConversationLine>> Lines = new(0, 1);
        public DynamicUpdateField<ConversationActorField> Actors = new(0, 2);
        public UpdateField<uint> LastLineEndTime = new(0, 3);
        public UpdateField<uint> Progress = new(0, 4);

        public ConversationData() : base(0, TypeId.Conversation, 5) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
        {
            data.WriteInt32(((List<ConversationLine>)Lines).Count);
            data.WriteUInt32(GetViewerLastLineEndTime(this, owner, receiver));
            for (int i = 0; i < ((List<ConversationLine>)Lines).Count; ++i)
            {
                ((List<ConversationLine>)Lines)[i].WriteCreate(data, owner, receiver);
            }
            data.WriteUInt32(Progress);
            data.WriteInt32(Actors.Size());
            for (int i = 0; i < Actors.Size(); ++i)
            {
                Actors[i].WriteCreate(data, owner, receiver);
            }
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Conversation owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 5);

            if (_changesMask[0])
            {
                if (_changesMask[1])
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
                if (_changesMask[2])
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
                if (_changesMask[2])
                {
                    for (int i = 0; i < Actors.Size(); ++i)
                    {
                        if (Actors.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            Actors[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(GetViewerLastLineEndTime(this, owner, receiver));
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(Progress);
                }
            }
        }

        public override void ClearChangesMask()
        {
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
}
