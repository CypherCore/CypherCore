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
using Framework.GameMath;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;

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
            data.WriteUInt32(EntryId);
            data.WriteUInt32(GetViewerDependentDynamicFlags(DynamicFlags, owner, receiver));
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
                    data.WriteUInt32(EntryId);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(GetViewerDependentDynamicFlags(DynamicFlags, owner, receiver));
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

        uint GetViewerDependentDynamicFlags(uint dynamicFlags, WorldObject obj, Player receiver)
        {
            UnitDynFlags unitDynFlags = (UnitDynFlags)dynamicFlags;

            Unit unit = obj.ToUnit();
            if (unit != null)
            {
                unitDynFlags &= ~UnitDynFlags.Tapped;

                Creature creature = obj.ToCreature();
                if (creature != null)
                {
                    if (creature.HasLootRecipient() && !creature.IsTappedBy(receiver))
                        unitDynFlags |= UnitDynFlags.Tapped;

                    if (!receiver.IsAllowedToLoot(creature))
                        unitDynFlags &= ~UnitDynFlags.Lootable;
                }

                // unit UNIT_DYNFLAG_TRACK_UNIT should only be sent to caster of SPELL_AURA_MOD_STALKED auras
                if (unitDynFlags.HasAnyFlag(UnitDynFlags.TrackUnit))
                    if (!unit.HasAuraTypeWithCaster(AuraType.ModStalked, receiver.GetGUID()))
                        unitDynFlags &= ~UnitDynFlags.TrackUnit;
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
                        case GameObjectTypes.Goober:
                            if (gameObject.ActivateToQuest(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.Activate | GameObjectDynamicLowFlags.Sparkle;
                            else if (receiver.IsGameMaster())
                                dynFlags |= GameObjectDynamicLowFlags.Activate;
                            break;
                        case GameObjectTypes.Generic:
                            if (gameObject.ActivateToQuest(receiver))
                                dynFlags |= GameObjectDynamicLowFlags.Sparkle;
                            break;
                        case GameObjectTypes.Transport:
                        case GameObjectTypes.MapObjTransport:
                            {
                                uint transportPeriod = gameObject.GetTransportPeriod();
                                if (transportPeriod != 0)
                                {
                                    float timer = (float)(gameObject.GetGoValue().Transport.PathProgress % transportPeriod);
                                    pathProgress = (ushort)(timer / (float)transportPeriod * 65535.0f);
                                }
                                break;
                            }
                        default:
                            break;
                    }

                    unitDynFlags = (UnitDynFlags)((pathProgress << 16) | (ushort)dynFlags);
                }
            }

            return (uint)unitDynFlags;
        }
    }

    public class ItemEnchantment : BaseUpdateData<Item>
    {
        public UpdateField<uint> ID = new(0, 1);
        public UpdateField<uint> Duration = new(0, 2);
        public UpdateField<short> Charges = new(0, 3);
        public UpdateField<ushort> Inactive = new(0, 4);

        public ItemEnchantment() : base(5) { }

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
        public UpdateField<uint> Durability = new(0, 11);
        public UpdateField<uint> MaxDurability = new(0, 12);
        public UpdateField<uint> CreatePlayedTime = new(0, 13);
        public UpdateField<int> Context = new(0, 14);
        public UpdateField<long> CreateTime = new(0, 15);
        public UpdateField<ulong> ArtifactXP = new(0, 16);
        public UpdateField<byte> ItemAppearanceModID = new(0, 17);
        public UpdateField<ItemModList> Modifiers = new(0, 18);
        public UpdateField<uint> DynamicFlags2 = new(0, 19);
        public UpdateFieldArray<int> SpellCharges = new(5, 20, 21);
        public UpdateFieldArray<ItemEnchantment> Enchantment = new(13, 26, 27);

        public ItemData() : base(0, TypeId.Item, 40) { }

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
            UpdateMask allowedMaskForTarget = new(40, new uint[] { 0xFC04E4FFu, 0x000000FFu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(40, new uint[] { 0x03FB1B00u, 0x00000000u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(40, new[] { 0xFC04E4FFu, 0x000000FFu });
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
                    data.WriteUInt32(Durability);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(MaxDurability);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(CreatePlayedTime);
                }
                if (changesMask[14])
                {
                    data.WriteInt32(Context);
                }
                if (changesMask[15])
                {
                    data.WriteInt64(CreateTime);
                }
                if (changesMask[16])
                {
                    data.WriteUInt64(ArtifactXP);
                }
                if (changesMask[17])
                {
                    data.WriteUInt8(ItemAppearanceModID);
                }
                if (changesMask[19])
                {
                    data.WriteUInt32(DynamicFlags2);
                }
                if (changesMask[18])
                {
                    ((ItemModList)Modifiers).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
            }
            if (changesMask[20])
            {
                for (int i = 0; i < 5; ++i)
                {
                    if (changesMask[21 + i])
                    {
                        data.WriteInt32(SpellCharges[i]);
                    }
                }
            }
            if (changesMask[26])
            {
                for (int i = 0; i < 13; ++i)
                {
                    if (changesMask[27 + i])
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
            ClearChangesMask(Durability);
            ClearChangesMask(MaxDurability);
            ClearChangesMask(CreatePlayedTime);
            ClearChangesMask(Context);
            ClearChangesMask(CreateTime);
            ClearChangesMask(ArtifactXP);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(Modifiers);
            ClearChangesMask(DynamicFlags2);
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

    public class AzeriteEmpoweredItemData : BaseUpdateData<Item>
    {
        public UpdateFieldArray<int> Selections = new(5, 0, 1);

        public AzeriteEmpoweredItemData() : base(0, TypeId.AzeriteEmpoweredItem, 6) { }

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

    public class UnlockedAzeriteEssence
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
    }

    public class SelectedAzeriteEssences : BaseUpdateData<AzeriteItem>
    {
        public UpdateField<bool> Enabled = new(0, 1);
        public UpdateField<uint> SpecializationID = new(0, 2);
        public UpdateFieldArray<uint> AzeriteEssenceID = new(4, 3, 4);

        public SelectedAzeriteEssences() : base(8) { }

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

    public class AzeriteItemData : BaseUpdateData<AzeriteItem>
    {
        public UpdateField<bool> Enabled = new(0, 1);
        public DynamicUpdateField<UnlockedAzeriteEssence>UnlockedEssences = new(0, 2);
        public DynamicUpdateField<uint>UnlockedEssenceMilestones = new(0, 4);
        public DynamicUpdateField<SelectedAzeriteEssences>SelectedEssences = new(0, 3);
        public UpdateField<ulong> Xp = new(0, 5);
        public UpdateField<uint> Level = new(0, 6);
        public UpdateField<uint> AuraLevel = new(0, 7);
        public UpdateField<uint> KnowledgeLevel = new(0, 8);
        public UpdateField<int> DEBUGknowledgeWeek = new(0, 9);

        public AzeriteItemData() : base(10) { }

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
            UpdateMask allowedMaskForTarget = new(9, new[] { 0x0000001Du });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(9, new[] { 0x000003E2u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(9, new[] { 0x0000001Du });
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

    public class SpellCastVisualField
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
    }

    public class UnitChannel
    {
        public uint SpellID;
        public uint SpellXSpellVisualID;
        public SpellCastVisualField SpellVisual = new();

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            SpellVisual.WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            SpellVisual.WriteUpdate(data, ignoreChangesMask, owner, receiver);
        }
    }

    public class VisibleItem : BaseUpdateData<Unit>
    {
        public UpdateField<uint> ItemID = new(0, 1);
        public UpdateField<uint> SecondaryItemModifiedAppearanceID = new(0, 2);
        public UpdateField<ushort> ItemAppearanceModID = new(0, 3);
        public UpdateField<ushort> ItemVisual = new(0, 4);

        public VisibleItem() : base(5) { }

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt32(SecondaryItemModifiedAppearanceID);
            data.WriteUInt16(ItemAppearanceModID);
            data.WriteUInt16(ItemVisual);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Unit owner, Player receiver)
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
                    data.WriteUInt32(ItemID);
                }
                if (changesMask[2])
                {
                    data.WriteUInt32(SecondaryItemModifiedAppearanceID);
                }
                if (changesMask[3])
                {
                    data.WriteUInt16(ItemAppearanceModID);
                }
                if (changesMask[4])
                {
                    data.WriteUInt16(ItemVisual);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(ItemID);
            ClearChangesMask(SecondaryItemModifiedAppearanceID);
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
        public UpdateField<uint> DisplayID = new(0, 5);
        public UpdateField<uint> StateSpellVisualID = new(0, 6);
        public UpdateField<uint> StateAnimID = new(0, 7);
        public UpdateField<uint> StateAnimKitID = new(0, 8);
        public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new(0, 9);
        public UpdateField<int> SpellOverrideNameID = new(0, 10);
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
        public UpdateField<ulong> Health = new(0, 30);
        public UpdateField<ulong> MaxHealth = new(0, 31);
        public UpdateField<uint> Level = new(32, 33);
        public UpdateField<int> EffectiveLevel = new(32, 34);
        public UpdateField<uint> ContentTuningID = new(32, 35);
        public UpdateField<int> ScalingLevelMin = new(32, 36);
        public UpdateField<int> ScalingLevelMax = new(32, 37);
        public UpdateField<int> ScalingLevelDelta = new(32, 38);
        public UpdateField<int> ScalingFactionGroup = new(32, 39);
        public UpdateField<int> ScalingHealthItemLevelCurveID = new(32, 40);
        public UpdateField<int> ScalingDamageItemLevelCurveID = new(32, 41);
        public UpdateField<uint> FactionTemplate = new(32, 42);
        public UpdateField<uint> Flags = new(32, 43);
        public UpdateField<uint> Flags2 = new(32, 44);
        public UpdateField<uint> Flags3 = new(32, 45);
        public UpdateField<uint> AuraState = new(32, 46);
        public UpdateField<uint> RangedAttackRoundBaseTime = new(32, 47);
        public UpdateField<float> BoundingRadius = new(32, 48);
        public UpdateField<float> CombatReach = new(32, 49);
        public UpdateField<float> DisplayScale = new(32, 50);
        public UpdateField<int> CreatureFamily = new(32, 51);
        public UpdateField<int> CreatureType = new(32, 52);
        public UpdateField<uint> NativeDisplayID = new(32, 53);
        public UpdateField<float> NativeXDisplayScale = new(32, 54);
        public UpdateField<uint> MountDisplayID = new(32, 55);
        public UpdateField<uint> CosmeticMountDisplayID = new(32, 56);
        public UpdateField<float> MinDamage = new(32, 57);
        public UpdateField<float> MaxDamage = new(32, 58);
        public UpdateField<float> MinOffHandDamage = new(32, 59);
        public UpdateField<float> MaxOffHandDamage = new(32, 60);
        public UpdateField<byte> StandState = new(32, 61);
        public UpdateField<byte> PetTalentPoints = new(32, 62);
        public UpdateField<byte> VisFlags = new(32, 63);
        public UpdateField<byte> AnimTier = new(64, 65);
        public UpdateField<uint> PetNumber = new(64, 66);
        public UpdateField<uint> PetNameTimestamp = new(64, 67);
        public UpdateField<uint> PetExperience = new(64, 68);
        public UpdateField<uint> PetNextLevelExperience = new(64, 69);
        public UpdateField<float> ModCastingSpeed = new(64, 70);
        public UpdateField<float> ModCastingSpeedNeg = new(64, 71);
        public UpdateField<float> ModSpellHaste = new(64, 72);
        public UpdateField<float> ModHaste = new(64, 73);
        public UpdateField<float> ModRangedHaste = new(64, 74);
        public UpdateField<float> ModHasteRegen = new(64, 75);
        public UpdateField<float> ModTimeRate = new(64, 76);
        public UpdateField<uint> CreatedBySpell = new(64, 77);
        public UpdateField<int> EmoteState = new(64, 78);
        public UpdateField<uint> BaseMana = new(64, 79);
        public UpdateField<uint> BaseHealth = new(64, 80);
        public UpdateField<byte> SheatheState = new(64, 81);
        public UpdateField<byte> PvpFlags = new(64, 82);
        public UpdateField<byte> PetFlags = new(64, 83);
        public UpdateField<byte> ShapeshiftForm = new(64, 84);
        public UpdateField<int> AttackPower = new(64, 85);
        public UpdateField<int> AttackPowerModPos = new(64, 86);
        public UpdateField<int> AttackPowerModNeg = new(64, 87);
        public UpdateField<float> AttackPowerMultiplier = new(64, 88);
        public UpdateField<int> RangedAttackPower = new(64, 89);
        public UpdateField<int> RangedAttackPowerModPos = new(64, 90);
        public UpdateField<int> RangedAttackPowerModNeg = new(64, 91);
        public UpdateField<float> RangedAttackPowerMultiplier = new(64, 92);
        public UpdateField<int> MainHandWeaponAttackPower = new(64, 93);
        public UpdateField<int> OffHandWeaponAttackPower = new(64, 94);
        public UpdateField<int> RangedWeaponAttackPower = new(64, 95);
        public UpdateField<int> SetAttackSpeedAura = new(96, 97);
        public UpdateField<float> Lifesteal = new(96, 98);
        public UpdateField<float> MinRangedDamage = new(96, 99);
        public UpdateField<float> MaxRangedDamage = new(96, 100);
        public UpdateField<float> ManaCostMultiplier = new(96, 101);
        public UpdateField<float> MaxHealthModifier = new(96, 102);
        public UpdateField<float> HoverHeight = new(96, 103);
        public UpdateField<uint> MinItemLevelCutoff = new(96, 104);
        public UpdateField<uint> MinItemLevel = new(96, 105);
        public UpdateField<uint> MaxItemLevel = new(96, 106);
        public UpdateField<int> AzeriteItemLevel = new(96, 107);
        public UpdateField<uint> WildBattlePetLevel = new(96, 108);
        public UpdateField<uint> BattlePetCompanionNameTimestamp = new(96, 109);
        public UpdateField<int> InteractSpellID = new(96, 110);
        public UpdateField<int> ScaleDuration = new(96, 111);
        public UpdateField<int> LooksLikeMountID = new(96, 112);
        public UpdateField<int> LooksLikeCreatureID = new(96, 113);
        public UpdateField<int> LookAtControllerID = new(96, 114);
        public UpdateField<int> TaxiNodesID = new(96, 115);
        public UpdateField<ObjectGuid> GuildGUID = new(96, 116);
        public UpdateField<ObjectGuid> SkinningOwnerGUID = new(96, 117);
        public UpdateField<uint> SilencedSchoolMask = new(96, 118);
        public UpdateField<ObjectGuid> NameplateAttachToGUID = new(96, 119); // When set, nameplate of this unit will instead appear on that object
        public UpdateFieldArray<uint> NpcFlags = new(2, 120, 121);
        public UpdateFieldArray<int> Power = new(6, 123, 124);
        public UpdateFieldArray<uint> MaxPower = new(6, 123, 130);
        public UpdateFieldArray<float> PowerRegenFlatModifier = new(6, 123, 136);
        public UpdateFieldArray<float> PowerRegenInterruptedFlatModifier = new(6, 123, 142);
        public UpdateFieldArray<VisibleItem> VirtualItems = new(3, 148, 149);
        public UpdateFieldArray<uint> AttackRoundBaseTime = new(2, 152, 153);
        public UpdateFieldArray<int> Stats = new(4, 155, 156);
        public UpdateFieldArray<int> StatPosBuff = new(4, 155, 160);
        public UpdateFieldArray<int> StatNegBuff = new(4, 155, 164);
        public UpdateFieldArray<int> Resistances = new(7, 168, 169);
        public UpdateFieldArray<int> BonusResistanceMods = new(7, 168, 176);
        public UpdateFieldArray<int> ManaCostModifier = new(7, 168, 183);

        public UnitData() : base(0, TypeId.Unit, 190) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            data.WriteUInt32(GetViewerDependentDisplayId(DisplayID, owner, receiver));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(GetViewerDependentNpcFlags(NpcFlags[i], i, owner, receiver));

            data.WriteUInt32(StateSpellVisualID);
            data.WriteUInt32(StateAnimID);
            data.WriteUInt32(StateAnimKitID);
            data.WriteInt32(((List<uint>)StateWorldEffectIDs).Count);
            data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
            data.WriteInt32(SpellOverrideNameID);
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
            data.WriteUInt64(Health);
            for (int i = 0; i < 6; ++i)
            {
                data.WriteInt32(Power[i]);
                data.WriteUInt32(MaxPower[i]);
            }
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner) || fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
            {
                for (int i = 0; i < 6; ++i)
                {
                    data.WriteFloat(PowerRegenFlatModifier[i]);
                    data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
                }
            }
            data.WriteUInt64(MaxHealth);
            data.WriteUInt32(Level);
            data.WriteInt32(EffectiveLevel);
            data.WriteUInt32(ContentTuningID);
            data.WriteInt32(ScalingLevelMin);
            data.WriteInt32(ScalingLevelMax);
            data.WriteInt32(ScalingLevelDelta);
            data.WriteInt32(ScalingFactionGroup);
            data.WriteInt32(ScalingHealthItemLevelCurveID);
            data.WriteInt32(ScalingDamageItemLevelCurveID);
            data.WriteUInt32(GetViewerDependentFactionTemplate(FactionTemplate, owner, receiver));
            for (int i = 0; i < 3; ++i)
                VirtualItems[i].WriteCreate(data, owner, receiver);

            data.WriteUInt32(GetViewerDependentFlags(Flags, owner, receiver));
            data.WriteUInt32(Flags2);
            data.WriteUInt32(Flags3);
            data.WriteUInt32(GetViewerDependentAuraState(AuraState, owner, receiver));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(AttackRoundBaseTime[i]);

            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                data.WriteUInt32(RangedAttackRoundBaseTime);

            data.WriteFloat(BoundingRadius);
            data.WriteFloat(CombatReach);
            data.WriteFloat(DisplayScale);
            data.WriteInt32(CreatureFamily);
            data.WriteInt32(CreatureType);
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
            data.WriteUInt8((byte)GetViewerDependentPvpFlags(PvpFlags, owner, receiver));
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
                data.WriteInt32(MainHandWeaponAttackPower);
                data.WriteInt32(OffHandWeaponAttackPower);
                data.WriteInt32(RangedWeaponAttackPower);
                data.WriteInt32(SetAttackSpeedAura);
                data.WriteFloat(Lifesteal);
                data.WriteFloat(MinRangedDamage);
                data.WriteFloat(MaxRangedDamage);
                data.WriteFloat(ManaCostMultiplier);
                data.WriteFloat(MaxHealthModifier);
            }
            data.WriteFloat(HoverHeight);
            data.WriteUInt32(MinItemLevelCutoff);
            data.WriteUInt32(MinItemLevel);
            data.WriteUInt32(MaxItemLevel);
            data.WriteInt32(AzeriteItemLevel);
            data.WriteUInt32(WildBattlePetLevel);
            data.WriteUInt32(BattlePetCompanionNameTimestamp);
            data.WriteInt32(InteractSpellID);
            data.WriteInt32(ScaleDuration);
            data.WriteInt32(LooksLikeMountID);
            data.WriteInt32(LooksLikeCreatureID);
            data.WriteInt32(LookAtControllerID);
            data.WriteInt32(TaxiNodesID);
            data.WritePackedGuid(GuildGUID);
            data.WriteInt32(PassiveSpells.Size());
            data.WriteInt32(WorldEffects.Size());
            data.WriteInt32(ChannelObjects.Size());
            data.WritePackedGuid(SkinningOwnerGUID);
            data.WriteUInt32(SilencedSchoolMask);
            data.WritePackedGuid(NameplateAttachToGUID);

            for (int i = 0; i < PassiveSpells.Size(); ++i)
                PassiveSpells[i].WriteCreate(data, owner, receiver);

            for (int i = 0; i < WorldEffects.Size(); ++i)
                data.WriteInt32(WorldEffects[i]);

            for (int i = 0; i < ChannelObjects.Size(); ++i)
                data.WritePackedGuid(ChannelObjects[i]);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(192, new uint[] { 0xFFFFDFFFu, 0xE1FF7FFFu, 0x001EFFFFu, 0xFFFFFF81u, 0x07F000FFu, 0x00000000u });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget.OR(new UpdateMask(190, new uint[] { 0x00002000u, 0x1E008000u, 0xFFE10000u, 0x0800007Eu, 0xF80FFF00u, 0x3FFFFFFFu }));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
                allowedMaskForTarget.OR(new UpdateMask(190, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x08000000u, 0x000FFF00u, 0x00000000u }));
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
                allowedMaskForTarget.OR(new UpdateMask(190, new uint[] { 0x00000000u, 0x1E000000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x0000FF00u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(190, new[] { 0xFFFFDFFFu, 0xE1FF7FFFu, 0x001EFFFFu, 0xFFFFFF81u, 0x07F000FFu, 0x00000000u });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Unit owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 6);
            for (uint i = 0; i < 6; ++i)
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
                    data.WriteUInt32(GetViewerDependentDisplayId(DisplayID, owner, receiver));
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(StateSpellVisualID);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(StateAnimID);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(StateAnimKitID);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
                }
                if (changesMask[10])
                {
                    data.WriteInt32(SpellOverrideNameID);
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
                    data.WriteUInt64(Health);
                }
                if (changesMask[31])
                {
                    data.WriteUInt64(MaxHealth);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[34])
                {
                    data.WriteInt32(EffectiveLevel);
                }
                if (changesMask[35])
                {
                    data.WriteUInt32(ContentTuningID);
                }
                if (changesMask[36])
                {
                    data.WriteInt32(ScalingLevelMin);
                }
                if (changesMask[37])
                {
                    data.WriteInt32(ScalingLevelMax);
                }
                if (changesMask[38])
                {
                    data.WriteInt32(ScalingLevelDelta);
                }
                if (changesMask[39])
                {
                    data.WriteInt32(ScalingFactionGroup);
                }
                if (changesMask[40])
                {
                    data.WriteInt32(ScalingHealthItemLevelCurveID);
                }
                if (changesMask[41])
                {
                    data.WriteInt32(ScalingDamageItemLevelCurveID);
                }
                if (changesMask[42])
                {
                    data.WriteUInt32(GetViewerDependentFactionTemplate(FactionTemplate, owner, receiver));
                }
                if (changesMask[43])
                {
                    data.WriteUInt32(GetViewerDependentFlags(Flags, owner, receiver));
                }
                if (changesMask[44])
                {
                    data.WriteUInt32(Flags2);
                }
                if (changesMask[45])
                {
                    data.WriteUInt32(Flags3);
                }
                if (changesMask[46])
                {
                    data.WriteUInt32(GetViewerDependentAuraState(AuraState, owner, receiver));
                }
                if (changesMask[47])
                {
                    data.WriteUInt32(RangedAttackRoundBaseTime);
                }
                if (changesMask[48])
                {
                    data.WriteFloat(BoundingRadius);
                }
                if (changesMask[49])
                {
                    data.WriteFloat(CombatReach);
                }
                if (changesMask[50])
                {
                    data.WriteFloat(DisplayScale);
                }
                if (changesMask[51])
                {
                    data.WriteInt32(CreatureFamily);
                }
                if (changesMask[52])
                {
                    data.WriteInt32(CreatureType);
                }
                if (changesMask[53])
                {
                    data.WriteUInt32(NativeDisplayID);
                }
                if (changesMask[54])
                {
                    data.WriteFloat(NativeXDisplayScale);
                }
                if (changesMask[55])
                {
                    data.WriteUInt32(MountDisplayID);
                }
                if (changesMask[56])
                {
                    data.WriteUInt32(CosmeticMountDisplayID);
                }
                if (changesMask[57])
                {
                    data.WriteFloat(MinDamage);
                }
                if (changesMask[58])
                {
                    data.WriteFloat(MaxDamage);
                }
                if (changesMask[59])
                {
                    data.WriteFloat(MinOffHandDamage);
                }
                if (changesMask[60])
                {
                    data.WriteFloat(MaxOffHandDamage);
                }
                if (changesMask[61])
                {
                    data.WriteUInt8(StandState);
                }
                if (changesMask[62])
                {
                    data.WriteUInt8(PetTalentPoints);
                }
                if (changesMask[63])
                {
                    data.WriteUInt8(VisFlags);
                }
            }
            if (changesMask[64])
            {
                if (changesMask[65])
                {
                    data.WriteUInt8(AnimTier);
                }
                if (changesMask[66])
                {
                    data.WriteUInt32(PetNumber);
                }
                if (changesMask[67])
                {
                    data.WriteUInt32(PetNameTimestamp);
                }
                if (changesMask[68])
                {
                    data.WriteUInt32(PetExperience);
                }
                if (changesMask[69])
                {
                    data.WriteUInt32(PetNextLevelExperience);
                }
                if (changesMask[70])
                {
                    data.WriteFloat(ModCastingSpeed);
                }
                if (changesMask[71])
                {
                    data.WriteFloat(ModCastingSpeedNeg);
                }
                if (changesMask[72])
                {
                    data.WriteFloat(ModSpellHaste);
                }
                if (changesMask[73])
                {
                    data.WriteFloat(ModHaste);
                }
                if (changesMask[74])
                {
                    data.WriteFloat(ModRangedHaste);
                }
                if (changesMask[75])
                {
                    data.WriteFloat(ModHasteRegen);
                }
                if (changesMask[76])
                {
                    data.WriteFloat(ModTimeRate);
                }
                if (changesMask[77])
                {
                    data.WriteUInt32(CreatedBySpell);
                }
                if (changesMask[78])
                {
                    data.WriteInt32(EmoteState);
                }
                if (changesMask[79])
                {
                    data.WriteUInt32(BaseMana);
                }
                if (changesMask[80])
                {
                    data.WriteUInt32(BaseHealth);
                }
                if (changesMask[81])
                {
                    data.WriteUInt8(SheatheState);
                }
                if (changesMask[82])
                {
                    data.WriteUInt8((byte)GetViewerDependentPvpFlags(PvpFlags, owner, receiver));
                }
                if (changesMask[83])
                {
                    data.WriteUInt8(PetFlags);
                }
                if (changesMask[84])
                {
                    data.WriteUInt8(ShapeshiftForm);
                }
                if (changesMask[85])
                {
                    data.WriteInt32(AttackPower);
                }
                if (changesMask[86])
                {
                    data.WriteInt32(AttackPowerModPos);
                }
                if (changesMask[87])
                {
                    data.WriteInt32(AttackPowerModNeg);
                }
                if (changesMask[88])
                {
                    data.WriteFloat(AttackPowerMultiplier);
                }
                if (changesMask[89])
                {
                    data.WriteInt32(RangedAttackPower);
                }
                if (changesMask[90])
                {
                    data.WriteInt32(RangedAttackPowerModPos);
                }
                if (changesMask[91])
                {
                    data.WriteInt32(RangedAttackPowerModNeg);
                }
                if (changesMask[92])
                {
                    data.WriteFloat(RangedAttackPowerMultiplier);
                }
                if (changesMask[93])
                {
                    data.WriteInt32(MainHandWeaponAttackPower);
                }
                if (changesMask[94])
                {
                    data.WriteInt32(OffHandWeaponAttackPower);
                }
                if (changesMask[95])
                {
                    data.WriteInt32(RangedWeaponAttackPower);
                }
            }
            if (changesMask[96])
            {
                if (changesMask[97])
                {
                    data.WriteInt32(SetAttackSpeedAura);
                }
                if (changesMask[98])
                {
                    data.WriteFloat(Lifesteal);
                }
                if (changesMask[99])
                {
                    data.WriteFloat(MinRangedDamage);
                }
                if (changesMask[100])
                {
                    data.WriteFloat(MaxRangedDamage);
                }
                if (changesMask[101])
                {
                    data.WriteFloat(ManaCostMultiplier);
                }
                if (changesMask[102])
                {
                    data.WriteFloat(MaxHealthModifier);
                }
                if (changesMask[103])
                {
                    data.WriteFloat(HoverHeight);
                }
                if (changesMask[104])
                {
                    data.WriteUInt32(MinItemLevelCutoff);
                }
                if (changesMask[105])
                {
                    data.WriteUInt32(MinItemLevel);
                }
                if (changesMask[106])
                {
                    data.WriteUInt32(MaxItemLevel);
                }
                if (changesMask[107])
                {
                    data.WriteInt32(AzeriteItemLevel);
                }
                if (changesMask[108])
                {
                    data.WriteUInt32(WildBattlePetLevel);
                }
                if (changesMask[109])
                {
                    data.WriteUInt32(BattlePetCompanionNameTimestamp);
                }
                if (changesMask[110])
                {
                    data.WriteInt32(InteractSpellID);
                }
                if (changesMask[111])
                {
                    data.WriteInt32(ScaleDuration);
                }
                if (changesMask[112])
                {
                    data.WriteInt32(LooksLikeMountID);
                }
                if (changesMask[113])
                {
                    data.WriteInt32(LooksLikeCreatureID);
                }
                if (changesMask[114])
                {
                    data.WriteInt32(LookAtControllerID);
                }
                if (changesMask[115])
                {
                    data.WriteInt32(TaxiNodesID);
                }
                if (changesMask[116])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[117])
                {
                    data.WritePackedGuid(SkinningOwnerGUID);
                }
                if (changesMask[118])
                {
                    data.WriteUInt32(SilencedSchoolMask);
                }
                if (changesMask[119])
                {
                    data.WritePackedGuid(NameplateAttachToGUID);
                }
            }
            if (changesMask[120])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[121 + i])
                    {
                        data.WriteUInt32(GetViewerDependentNpcFlags(NpcFlags[i], i, owner, receiver));
                    }
                }
            }
            if (changesMask[123])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[124 + i])
                    {
                        data.WriteInt32(Power[i]);
                    }
                    if (changesMask[130 + i])
                    {
                        data.WriteUInt32(MaxPower[i]);
                    }
                    if (changesMask[136 + i])
                    {
                        data.WriteFloat(PowerRegenFlatModifier[i]);
                    }
                    if (changesMask[142 + i])
                    {
                        data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
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
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[156 + i])
                    {
                        data.WriteInt32(Stats[i]);
                    }
                    if (changesMask[160 + i])
                    {
                        data.WriteInt32(StatPosBuff[i]);
                    }
                    if (changesMask[164 + i])
                    {
                        data.WriteInt32(StatNegBuff[i]);
                    }
                }
            }
            if (changesMask[168])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[169 + i])
                    {
                        data.WriteInt32(Resistances[i]);
                    }
                    if (changesMask[176 + i])
                    {
                        data.WriteInt32(BonusResistanceMods[i]);
                    }
                    if (changesMask[183 + i])
                    {
                        data.WriteInt32(ManaCostModifier[i]);
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
            ClearChangesMask(DisplayID);
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
            ClearChangesMask(RangedAttackPower);
            ClearChangesMask(RangedAttackPowerModPos);
            ClearChangesMask(RangedAttackPowerModNeg);
            ClearChangesMask(RangedAttackPowerMultiplier);
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
            ClearChangesMask(BattlePetCompanionNameTimestamp);
            ClearChangesMask(InteractSpellID);
            ClearChangesMask(ScaleDuration);
            ClearChangesMask(LooksLikeMountID);
            ClearChangesMask(LooksLikeCreatureID);
            ClearChangesMask(LookAtControllerID);
            ClearChangesMask(TaxiNodesID);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(SkinningOwnerGUID);
            ClearChangesMask(SilencedSchoolMask);
            ClearChangesMask(NameplateAttachToGUID);
            ClearChangesMask(NpcFlags);
            ClearChangesMask(Power);
            ClearChangesMask(MaxPower);
            ClearChangesMask(PowerRegenFlatModifier);
            ClearChangesMask(PowerRegenInterruptedFlatModifier);
            ClearChangesMask(VirtualItems);
            ClearChangesMask(AttackRoundBaseTime);
            ClearChangesMask(Stats);
            ClearChangesMask(StatPosBuff);
            ClearChangesMask(StatNegBuff);
            ClearChangesMask(Resistances);
            ClearChangesMask(BonusResistanceMods);
            ClearChangesMask(ManaCostModifier);
            _changesMask.ResetAll();
        }

        uint GetViewerDependentDisplayId(uint displayId, Unit unit, Player receiver)
        {
            if (unit.IsCreature())
            {
                CreatureTemplate cinfo = unit.ToCreature().GetCreatureTemplate();

                // this also applies for transform auras
                SpellInfo transform = Global.SpellMgr.GetSpellInfo(unit.GetTransForm(), unit.GetMap().GetDifficultyID());
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
        uint GetViewerDependentNpcFlags(uint npcFlag, int i, Unit unit, Player receiver)
        {
            if (i == 0 && unit.IsCreature() && !receiver.CanSeeSpellClickOn(unit.ToCreature()))
                npcFlag &= ~(uint)NPCFlags.SpellClick;

            return npcFlag;
        }
        uint GetViewerDependentFactionTemplate(uint factionTemplate, Unit unit, Player receiver)
        {
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
        uint GetViewerDependentFlags(uint flags, Unit unit, Player receiver)
        {
            // Gamemasters should be always able to select units - remove not selectable flag
            if (receiver.IsGameMaster())
                flags &= ~(uint)UnitFlags.NotSelectable;

            return flags;
        }
        uint GetViewerDependentAuraState(uint auraState, Unit unit, Player receiver)
        {
            // Check per caster aura states to not enable using a spell in client if specified aura is not by target
            return unit.BuildAuraStateUpdateForTarget(receiver);
        }
        uint GetViewerDependentPvpFlags(uint pvpFlags, Unit unit, Player receiver)
        {
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
        public UpdateField<uint> ObjectiveFlags = new(0, 5);
        public UpdateFieldArray<ushort> ObjectiveProgress = new(24, 6, 7);

        public QuestLog() : base(31) { }

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(QuestID);
            data.WriteUInt32(StateFlags);
            data.WriteUInt32(EndTime);
            data.WriteUInt32(AcceptTime);
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
                if (changesMask[5])
                {
                    data.WriteUInt32(ObjectiveFlags);
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
            ClearChangesMask(AcceptTime);
            ClearChangesMask(ObjectiveFlags);
            ClearChangesMask(ObjectiveProgress);
            _changesMask.ResetAll();
        }
    }

    public class ArenaCooldown : BaseUpdateData<Player>
    {
        public UpdateField<int> SpellID = new(0, 1);
        public UpdateField<int> Charges = new(0, 2);
        public UpdateField<uint> Flags = new(0, 3);
        public UpdateField<uint> StartTime = new(0, 4);
        public UpdateField<uint> EndTime = new(0, 5);
        public UpdateField<uint> NextChargeTime = new(0, 6);
        public UpdateField<byte> MaxCharges = new(0, 7);

        public ArenaCooldown() : base(8) { }

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

    public class CTROptions
    {
        public uint ContentTuningConditionMask;
        public uint Field_4;
        public uint ExpansionLevelMask;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(ContentTuningConditionMask);
            data.WriteUInt32(Field_4);
            data.WriteUInt32(ExpansionLevelMask);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteUInt32(ContentTuningConditionMask);
            data.WriteUInt32(Field_4);
            data.WriteUInt32(ExpansionLevelMask);
        }
    }

    public class PlayerData : BaseUpdateData<Player>
    {
        public UpdateField<bool> HasQuestSession = new(0, 1);
        public UpdateField<bool> HasLevelLink = new(0, 2);
        public DynamicUpdateField<ChrCustomizationChoice>Customizations = new(0, 3);
        public DynamicUpdateField<QuestLog>QuestSessionQuestLog = new(0, 4);
        public DynamicUpdateField<ArenaCooldown>ArenaCooldowns = new(0, 5);
        public UpdateField<ObjectGuid> DuelArbiter = new(0, 6);
        public UpdateField<ObjectGuid> WowAccount = new(0, 7);
        public UpdateField<ObjectGuid> LootTargetGUID = new(0, 8);
        public UpdateField<uint> PlayerFlags = new(0, 9);
        public UpdateField<uint> PlayerFlagsEx = new(0, 10);
        public UpdateField<uint> GuildRankID = new(0, 11);
        public UpdateField<uint> GuildDeleteDate = new(0, 12);
        public UpdateField<uint> GuildLevel = new(0, 13);
        public UpdateField<byte> PartyType = new(0, 14);
        public UpdateField<byte> NativeSex = new(0, 15);
        public UpdateField<byte> Inebriation = new(0, 16);
        public UpdateField<byte> PvpTitle = new(0, 17);
        public UpdateField<byte> ArenaFaction = new(0, 18);
        public UpdateField<uint> DuelTeam = new(0, 19);
        public UpdateField<int> GuildTimeStamp = new(0, 20);
        public UpdateField<uint> PlayerTitle = new(0, 21);
        public UpdateField<int> FakeInebriation = new(0, 22);
        public UpdateField<uint> VirtualPlayerRealm = new(0, 23);
        public UpdateField<uint> CurrentSpecID = new(0, 24);
        public UpdateField<int> TaxiMountAnimKitID = new(0, 25);
        public UpdateField<byte> CurrentBattlePetBreedQuality = new(0, 26);
        public UpdateField<uint> HonorLevel = new(0, 27);
        public UpdateField<int> Field_B0 = new(0, 28);
        public UpdateField<int> Field_B4 = new(0, 29);
        public UpdateField<CTROptions> CtrOptions = new(0, 30);
        public UpdateField<int> CovenantID = new(0, 31);
        public UpdateField<int> SoulbindID = new(32, 33);
        public UpdateField<DungeonScoreSummary> DungeonScore = new(32, 34);
        public UpdateFieldArray<QuestLog>QuestLog = new(125, 35, 36);
        public UpdateFieldArray<VisibleItem>VisibleItems = new(19, 161, 162);
        public UpdateFieldArray<float>AvgItemLevel = new(6, 181, 182);

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
            data.WriteUInt32(DuelTeam);
            data.WriteInt32(GuildTimeStamp);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                for (int i = 0; i < 125; ++i)
                {
                    QuestLog[i].WriteCreate(data, owner, receiver);
                }
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
            data.WriteInt32(TaxiMountAnimKitID);
            for (int i = 0; i < 6; ++i)
            {
                data.WriteFloat(AvgItemLevel[i]);
            }
            data.WriteUInt8(CurrentBattlePetBreedQuality);
            data.WriteUInt32(HonorLevel);
            data.WriteInt32(ArenaCooldowns.Size());
            data.WriteInt32(Field_B0);
            data.WriteInt32(Field_B4);
            ((CTROptions)CtrOptions).WriteCreate(data, owner, receiver);
            data.WriteInt32(CovenantID);
            data.WriteInt32(SoulbindID);
            for (int i = 0; i < Customizations.Size(); ++i)
            {
                Customizations[i].WriteCreate(data, owner, receiver);
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
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
            {
                data.WriteBit(HasQuestSession);
            }
            data.WriteBit(HasLevelLink);
            DungeonScore._value.Write(data);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new(188, new[] { 0xFFFFFFEDu, 0x00000007u, 0x00000000u, 0x00000000u, 0x00000000u, 0x0FFFFFFEu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, false, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
                allowedMaskForTarget.OR(new UpdateMask(188, new[] { 0x00000012u, 0xFFFFFFF8u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x00000001u }));
        }

        public void FilterDisallowedFieldsMaskForFlag(UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags)
        {
            UpdateMask allowedMaskForTarget = new(188, new[] { 0xFFFFFFEDu, 0x00000007u, 0x00000000u, 0x00000000u, 0x00000000u, 0x0FFFFFFEu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            changesMask.AND(allowedMaskForTarget);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, Player owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 6);
            for (uint i = 0; i < 6; ++i)
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
                        QuestSessionQuestLog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(QuestSessionQuestLog.Size(), data);
                }
                if (changesMask[5])
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
                if (changesMask[5])
                {
                    for (int i = 0; i < ArenaCooldowns.Size(); ++i)
                    {
                        if (ArenaCooldowns.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ArenaCooldowns[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(DuelArbiter);
                }
                if (changesMask[7])
                {
                    data.WritePackedGuid(WowAccount);
                }
                if (changesMask[8])
                {
                    data.WritePackedGuid(LootTargetGUID);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(PlayerFlags);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(PlayerFlagsEx);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(GuildRankID);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(GuildDeleteDate);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(GuildLevel);
                }
                if (changesMask[14])
                {
                    data.WriteUInt8(PartyType);
                }
                if (changesMask[15])
                {
                    data.WriteUInt8(NativeSex);
                }
                if (changesMask[16])
                {
                    data.WriteUInt8(Inebriation);
                }
                if (changesMask[17])
                {
                    data.WriteUInt8(PvpTitle);
                }
                if (changesMask[18])
                {
                    data.WriteUInt8(ArenaFaction);
                }
                if (changesMask[19])
                {
                    data.WriteUInt32(DuelTeam);
                }
                if (changesMask[20])
                {
                    data.WriteInt32(GuildTimeStamp);
                }
                if (changesMask[21])
                {
                    data.WriteUInt32(PlayerTitle);
                }
                if (changesMask[22])
                {
                    data.WriteInt32(FakeInebriation);
                }
                if (changesMask[23])
                {
                    data.WriteUInt32(VirtualPlayerRealm);
                }
                if (changesMask[24])
                {
                    data.WriteUInt32(CurrentSpecID);
                }
                if (changesMask[25])
                {
                    data.WriteInt32(TaxiMountAnimKitID);
                }
                if (changesMask[26])
                {
                    data.WriteUInt8(CurrentBattlePetBreedQuality);
                }
                if (changesMask[27])
                {
                    data.WriteUInt32(HonorLevel);
                }
                if (changesMask[28])
                {
                    data.WriteInt32(Field_B0);
                }
                if (changesMask[29])
                {
                    data.WriteInt32(Field_B4);
                }
                if (changesMask[30])
                {
                    ((CTROptions)CtrOptions).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[31])
                {
                    data.WriteInt32(CovenantID);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteInt32(SoulbindID);
                }
                if (changesMask[34])
                {
                    DungeonScore._value.Write(data);
                }
            }
            if (changesMask[35])
            {
                for (int i = 0; i < 125; ++i)
                {
                    if (changesMask[36 + i])
                    {
                        if (noQuestLogChangesMask)
                            QuestLog[i].WriteCreate(data, owner, receiver);
                        else
                            QuestLog[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[161])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[162 + i])
                    {
                        VisibleItems[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[181])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[182 + i])
                    {
                        data.WriteFloat(AvgItemLevel[i]);
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
            ClearChangesMask(QuestSessionQuestLog);
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
            ClearChangesMask(DuelTeam);
            ClearChangesMask(GuildTimeStamp);
            ClearChangesMask(PlayerTitle);
            ClearChangesMask(FakeInebriation);
            ClearChangesMask(VirtualPlayerRealm);
            ClearChangesMask(CurrentSpecID);
            ClearChangesMask(TaxiMountAnimKitID);
            ClearChangesMask(CurrentBattlePetBreedQuality);
            ClearChangesMask(HonorLevel);
            ClearChangesMask(Field_B0);
            ClearChangesMask(Field_B4);
            ClearChangesMask(CtrOptions);
            ClearChangesMask(CovenantID);
            ClearChangesMask(SoulbindID);
            ClearChangesMask(DungeonScore);
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

        public PVPInfo() : base(11) { }

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
            data.WriteBit(Disqualified);
            data.FlushBits();
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlock(0), 11);

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

    public class MawPower
    {
        public int Field_0;
        public int Field_4;
        public int Field_8;

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(Field_4);
            data.WriteInt32(Field_8);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            data.WriteInt32(Field_0);
            data.WriteInt32(Field_4);
            data.WriteInt32(Field_8);
        }
    }

    public class MultiFloorExplore
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
    }

    public class RecipeProgressionInfo
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
    }

    public class ActivePlayerUnk901 : BaseUpdateData<Player>
    {
        public UpdateField<ObjectGuid> Field_0 = new(0, 1 );
        public UpdateField<int> Field_10 = new(0, 2 );

        public ActivePlayerUnk901() : base(3) { }

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

    public class ReplayedQuest : BaseUpdateData<Player>
    {
        public UpdateField<int> QuestID = new(0, 1);
        public UpdateField<uint> ReplayTime = new(0, 2);

        public ReplayedQuest() : base(3) { }

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

    public class QuestSession : BaseUpdateData<Player>
    {
        public UpdateField<ObjectGuid> Owner = new(0, 1);
        public UpdateFieldArray<ulong> QuestCompleted = new(875, 2, 3);

        public QuestSession() : base(878) { }

        public void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WritePackedGuid(Owner);
            for (int i = 0; i < 875; ++i)
            {
                data.WriteUInt64(QuestCompleted[i]);
            }
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Player owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            if (ignoreChangesMask)
                changesMask.SetAll();

            data.WriteBits(changesMask.GetBlocksMask(0), 28);
            for (uint i = 0; i < 28; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WritePackedGuid(Owner);
                }
            }
            if (changesMask[2])
            {
                for (int i = 0; i < 875; ++i)
                {
                    if (changesMask[3 + i])
                    {
                        data.WriteUInt64(QuestCompleted[i]);
                    }
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

    public class ActivePlayerData : BaseUpdateData<Player>
    {
        public UpdateField<bool> BackpackAutoSortDisabled = new(0, 1);
        public UpdateField<bool> BankAutoSortDisabled = new(0, 2);
        public UpdateField<bool> SortBagsRightToLeft = new(0, 3);
        public UpdateField<bool> InsertItemsLeftToRight = new(0, 4);
        public UpdateFieldArray<DynamicUpdateField<Research>> Research = new(1, 27, 28);
        public DynamicUpdateField<ulong> KnownTitles = new(0, 5);
        public DynamicUpdateField<ushort> ResearchSites = new(0, 6);
        public DynamicUpdateField<uint> ResearchSiteProgress = new(0, 7);
        public DynamicUpdateField<uint> DailyQuestsCompleted = new(0, 8);
        public DynamicUpdateField<int> AvailableQuestLineXQuestIDs = new(0, 9);
        public DynamicUpdateField<uint> Heirlooms = new(0, 10);
        public DynamicUpdateField<uint> HeirloomFlags = new(0, 11);
        public DynamicUpdateField<uint> Toys = new(0, 12);
        public DynamicUpdateField<uint> ToyFlags = new(0, 13);
        public DynamicUpdateField<uint> Transmog = new(0, 14);
        public DynamicUpdateField<uint> ConditionalTransmog = new(0, 15);
        public DynamicUpdateField<uint> SelfResSpells = new(0, 16);
        public DynamicUpdateField<uint> RuneforgePowers = new(0, 17);
        public DynamicUpdateField<uint> TransmogIllusions = new(0, 18);
        public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new(0, 20);
        public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new(0, 21);
        public DynamicUpdateField<MawPower> MawPowers = new(0, 22);
        public DynamicUpdateField<MultiFloorExplore> MultiFloorExploration = new(0, 23);
        public DynamicUpdateField<RecipeProgressionInfo> RecipeProgression = new(0, 24);
        public DynamicUpdateField<ReplayedQuest> ReplayedQuests = new(0, 25);
        public DynamicUpdateField<int> DisabledSpells = new(0, 26);
        public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new(0, 19);
        public UpdateField<ObjectGuid> FarsightObject = new(0, 29);
        public UpdateField<ObjectGuid> SummonedBattlePetGUID = new(0, 30);
        public UpdateField<ulong> Coinage = new(0, 31);
        public UpdateField<uint> XP = new(0, 32);
        public UpdateField<uint> NextLevelXP = new(0, 33);
        public UpdateField<int> TrialXP = new(34, 35);
        public UpdateField<SkillInfo> Skill = new(34, 36);
        public UpdateField<uint> CharacterPoints = new(34, 37);
        public UpdateField<uint> MaxTalentTiers = new(34, 38);
        public UpdateField<uint> TrackCreatureMask = new(34, 39);
        public UpdateField<float> MainhandExpertise = new(34, 40);
        public UpdateField<float> OffhandExpertise = new(34, 41);
        public UpdateField<float> RangedExpertise = new(34, 42);
        public UpdateField<float> CombatRatingExpertise = new(34, 43);
        public UpdateField<float> BlockPercentage = new(34, 44);
        public UpdateField<float> DodgePercentage = new(34, 45);
        public UpdateField<float> DodgePercentageFromAttribute = new(34, 46);
        public UpdateField<float> ParryPercentage = new(34, 47);
        public UpdateField<float> ParryPercentageFromAttribute = new(34, 48);
        public UpdateField<float> CritPercentage = new(34, 49);
        public UpdateField<float> RangedCritPercentage = new(34, 50);
        public UpdateField<float> OffhandCritPercentage = new(34, 51);
        public UpdateField<float> SpellCritPercentage = new(34, 52);
        public UpdateField<uint> ShieldBlock = new(34, 53);
        public UpdateField<float> ShieldBlockCritPercentage = new(34, 54);
        public UpdateField<float> Mastery = new(34, 55);
        public UpdateField<float> Speed = new(34, 56);
        public UpdateField<float> Avoidance = new(34, 57);
        public UpdateField<float> Sturdiness = new(34, 58);
        public UpdateField<int> Versatility = new(34, 59);
        public UpdateField<float> VersatilityBonus = new(34, 60);
        public UpdateField<float> PvpPowerDamage = new(34, 61);
        public UpdateField<float> PvpPowerHealing = new(34, 62);
        public UpdateField<int> ModHealingDonePos = new(34, 63);
        public UpdateField<float> ModHealingPercent = new(34, 64);
        public UpdateField<float> ModPeriodicHealingDonePercent = new(34, 65);
        public UpdateField<float> ModSpellPowerPercent = new(66, 67);
        public UpdateField<float> ModResiliencePercent = new(66, 68);
        public UpdateField<float> OverrideSpellPowerByAPPercent = new(66, 69);
        public UpdateField<float> OverrideAPBySpellPowerPercent = new(66, 70);
        public UpdateField<int> ModTargetResistance = new(66, 71);
        public UpdateField<int> ModTargetPhysicalResistance = new(66, 72);
        public UpdateField<uint> LocalFlags = new(66, 73);
        public UpdateField<byte> GrantableLevels = new(66, 74);
        public UpdateField<byte> MultiActionBars = new(66, 75);
        public UpdateField<byte> LifetimeMaxRank = new(66, 76);
        public UpdateField<byte> NumRespecs = new(66, 77);
        public UpdateField<uint> PvpMedals = new(66, 78);
        public UpdateField<ushort> TodayHonorableKills = new(66, 79);
        public UpdateField<ushort> YesterdayHonorableKills = new(66, 80);
        public UpdateField<uint> LifetimeHonorableKills = new(66, 81);
        public UpdateField<uint> WatchedFactionIndex = new(66, 82);
        public UpdateField<int> MaxLevel = new(66, 83);
        public UpdateField<int> ScalingPlayerLevelDelta = new(66, 84);
        public UpdateField<int> MaxCreatureScalingLevel = new(66, 85);
        public UpdateField<uint> PetSpellPower = new(66, 86);
        public UpdateField<float> UiHitModifier = new(66, 87);
        public UpdateField<float> UiSpellHitModifier = new(66, 88);
        public UpdateField<int> HomeRealmTimeOffset = new(66, 89);
        public UpdateField<float> ModPetHaste = new(66, 90);
        public UpdateField<sbyte> JailersTowerLevelMax = new(66, 91);
        public UpdateField<sbyte> JailersTowerLevel = new(66, 92);
        public UpdateField<byte> LocalRegenFlags = new(66, 93);
        public UpdateField<byte> AuraVision = new(66, 94);
        public UpdateField<byte> NumBackpackSlots = new(66, 95);
        public UpdateField<uint> OverrideSpellsID = new(66, 96);
        public UpdateField<ushort> LootSpecID = new(66, 97);
        public UpdateField<uint> OverrideZonePVPType = new(98, 99);
        public UpdateField<ObjectGuid> BnetAccount = new(98, 100);
        public UpdateField<ulong> GuildClubMemberID = new(98, 101);
        public UpdateField<uint> Honor = new(98, 102);
        public UpdateField<uint> HonorNextLevel = new(98, 103);
        public UpdateField<byte>NumBankSlots = new(98, 104);
        public UpdateField<ActivePlayerUnk901> Field_1410 = new(98, 106);
        public UpdateField<Optional<QuestSession>> QuestSession = new(98, 105);
        public UpdateField<int> UiChromieTimeExpansionID = new(98, 107);
        public UpdateField<int> TransportServerTime = new(98, 108);
        public UpdateField<uint> WeeklyRewardsPeriodSinceOrigin = new(98, 109);                // week count since Cfg_RegionsEntry::ChallengeOrigin
        public UpdateField<short> DEBUGSoulbindConduitRank = new(98, 110);
        public UpdateField<DungeonScoreData> DungeonScore = new(98, 111);
        public UpdateFieldArray<ObjectGuid> InvSlots = new(199, 112, 113);
        public UpdateFieldArray<uint> TrackResourceMask = new(2, 312, 313);
        public UpdateFieldArray<ulong> ExploredZones = new(240, 315, 316);
        public UpdateFieldArray<RestInfo> RestInfo = new(2, 556, 557);
        public UpdateFieldArray<int> ModDamageDonePos = new(7, 559, 560);
        public UpdateFieldArray<int> ModDamageDoneNeg = new(7, 559, 567);
        public UpdateFieldArray<float> ModDamageDonePercent = new(7, 559, 574);
        public UpdateFieldArray<float> ModHealingDonePercent = new(7, 559, 581);
        public UpdateFieldArray<float> WeaponDmgMultipliers = new(3, 588, 589);
        public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new(3, 588, 592);
        public UpdateFieldArray<uint> BuybackPrice = new(12, 595, 596);
        public UpdateFieldArray<uint> BuybackTimestamp = new(12, 595, 608);
        public UpdateFieldArray<uint> CombatRatings = new(32, 620, 621);
        public UpdateFieldArray<PVPInfo> PvpInfo = new(6, 653, 654);
        public UpdateFieldArray<uint> NoReagentCostMask = new(4, 660, 661);
        public UpdateFieldArray<uint> ProfessionSkillLine = new(2, 665, 666);
        public UpdateFieldArray<uint> BagSlotFlags = new(4, 668, 669);
        public UpdateFieldArray<uint> BankBagSlotFlags = new(7, 673, 674);
        public UpdateFieldArray<ulong> QuestCompleted = new(875, 681, 682);

        public ActivePlayerData() : base(0, TypeId.ActivePlayer, 1557) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            for (int i = 0; i < 199; ++i)
            {
                data.WritePackedGuid(InvSlots[i]);
            }
            data.WritePackedGuid(FarsightObject);
            data.WritePackedGuid(SummonedBattlePetGUID);
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
            for (int i = 0; i < 240; ++i)
            {
                data.WriteUInt64(ExploredZones[i]);
            }
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
                data.WriteUInt32(BuybackTimestamp[i]);
            }
            data.WriteUInt16(TodayHonorableKills);
            data.WriteUInt16(YesterdayHonorableKills);
            data.WriteUInt32(LifetimeHonorableKills);
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
            data.WriteInt8(JailersTowerLevelMax);
            data.WriteInt8(JailersTowerLevel);
            data.WriteUInt8(LocalRegenFlags);
            data.WriteUInt8(AuraVision);
            data.WriteUInt8(NumBackpackSlots);
            data.WriteUInt32(OverrideSpellsID);
            data.WriteUInt16(LootSpecID);
            data.WriteUInt32(OverrideZonePVPType);
            data.WritePackedGuid(BnetAccount);
            data.WriteUInt64(GuildClubMemberID);
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
            data.WriteUInt8(NumBankSlots);
            data.WriteInt32(ResearchSites.Size());
            data.WriteInt32(ResearchSiteProgress.Size());
            data.WriteInt32(DailyQuestsCompleted.Size());
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
            data.WriteInt32(MawPowers.Size());
            data.WriteInt32(MultiFloorExploration.Size());
            data.WriteInt32(RecipeProgression.Size());
            data.WriteInt32(ReplayedQuests.Size());
            data.WriteInt32(DisabledSpells.Size());
            data.WriteInt32(UiChromieTimeExpansionID);
            data.WriteInt32(TransportServerTime);
            data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
            data.WriteInt16(DEBUGSoulbindConduitRank);
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
            for (int i = 0; i < DisabledSpells.Size(); ++i)
            {
                data.WriteInt32(DisabledSpells[i]);
            }
            for (int i = 0; i < 6; ++i)
            {
                PvpInfo[i].WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
            data.WriteBit(BackpackAutoSortDisabled);
            data.WriteBit(BankAutoSortDisabled);
            data.WriteBit(SortBagsRightToLeft);
            data.WriteBit(InsertItemsLeftToRight);
            data.WriteBits(QuestSession.GetValue().HasValue, 1);
            ((ActivePlayerUnk901)Field_1410).WriteCreate(data, owner, receiver);
            if (QuestSession.GetValue().HasValue)
            {
                QuestSession.GetValue().Value.WriteCreate(data, owner, receiver);
            }
            DungeonScore._value.Write(data);
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

            data.WriteBits(changesMask.GetBlocksMask(1), 17);
            for (uint i = 0; i < 49; ++i)
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
                    data.WriteBit(BankAutoSortDisabled);
                }
                if (changesMask[3])
                {
                    data.WriteBit(SortBagsRightToLeft);
                }
                if (changesMask[4])
                {
                    data.WriteBit(InsertItemsLeftToRight);
                }
                if (changesMask[5])
                {
                    if (!ignoreNestedChangesMask)
                        KnownTitles.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(KnownTitles.Size(), data);
                }
                if (changesMask[6])
                {
                    if (!ignoreNestedChangesMask)
                        ResearchSites.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ResearchSites.Size(), data);
                }
                if (changesMask[7])
                {
                    if (!ignoreNestedChangesMask)
                        ResearchSiteProgress.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ResearchSiteProgress.Size(), data);
                }
                if (changesMask[8])
                {
                    if (!ignoreNestedChangesMask)
                        DailyQuestsCompleted.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(DailyQuestsCompleted.Size(), data);
                }
                if (changesMask[9])
                {
                    if (!ignoreNestedChangesMask)
                        AvailableQuestLineXQuestIDs.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(AvailableQuestLineXQuestIDs.Size(), data);
                }
                if (changesMask[10])
                {
                    if (!ignoreNestedChangesMask)
                        Heirlooms.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Heirlooms.Size(), data);
                }
                if (changesMask[11])
                {
                    if (!ignoreNestedChangesMask)
                        HeirloomFlags.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(HeirloomFlags.Size(), data);
                }
                if (changesMask[12])
                {
                    if (!ignoreNestedChangesMask)
                        Toys.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Toys.Size(), data);
                }
                if (changesMask[13])
                {
                    if (!ignoreNestedChangesMask)
                        ToyFlags.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ToyFlags.Size(), data);
                }
                if (changesMask[14])
                {
                    if (!ignoreNestedChangesMask)
                        Transmog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(Transmog.Size(), data);
                }
                if (changesMask[15])
                {
                    if (!ignoreNestedChangesMask)
                        ConditionalTransmog.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ConditionalTransmog.Size(), data);
                }
                if (changesMask[16])
                {
                    if (!ignoreNestedChangesMask)
                        SelfResSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SelfResSpells.Size(), data);
                }
                if (changesMask[17])
                {
                    if (!ignoreNestedChangesMask)
                        RuneforgePowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(RuneforgePowers.Size(), data);
                }
                if (changesMask[18])
                {
                    if (!ignoreNestedChangesMask)
                        TransmogIllusions.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(TransmogIllusions.Size(), data);
                }
                if (changesMask[19])
                {
                    if (!ignoreNestedChangesMask)
                        CharacterRestrictions.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(CharacterRestrictions.Size(), data);
                }
                if (changesMask[20])
                {
                    if (!ignoreNestedChangesMask)
                        SpellPctModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellPctModByLabel.Size(), data);
                }
                if (changesMask[21])
                {
                    if (!ignoreNestedChangesMask)
                        SpellFlatModByLabel.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(SpellFlatModByLabel.Size(), data);
                }
            }
            if (changesMask[27])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (changesMask[28 + i])
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
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[22])
                {
                    if (!ignoreNestedChangesMask)
                        MawPowers.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MawPowers.Size(), data);
                }
                if (changesMask[23])
                {
                    if (!ignoreNestedChangesMask)
                        MultiFloorExploration.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(MultiFloorExploration.Size(), data);
                }
                if (changesMask[24])
                {
                    if (!ignoreNestedChangesMask)
                        RecipeProgression.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(RecipeProgression.Size(), data);
                }
                if (changesMask[25])
                {
                    if (!ignoreNestedChangesMask)
                        ReplayedQuests.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(ReplayedQuests.Size(), data);
                }
                if (changesMask[26])
                {
                    if (!ignoreNestedChangesMask)
                        DisabledSpells.WriteUpdateMask(data);
                    else
                        WriteCompleteDynamicFieldUpdateMask(DisabledSpells.Size(), data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[5])
                {
                    for (int i = 0; i < KnownTitles.Size(); ++i)
                    {
                        if (KnownTitles.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt64(KnownTitles[i]);
                        }
                    }
                }
                if (changesMask[6])
                {
                    for (int i = 0; i < ResearchSites.Size(); ++i)
                    {
                        if (ResearchSites.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt16(ResearchSites[i]);
                        }
                    }
                }
                if (changesMask[7])
                {
                    for (int i = 0; i < ResearchSiteProgress.Size(); ++i)
                    {
                        if (ResearchSiteProgress.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ResearchSiteProgress[i]);
                        }
                    }
                }
                if (changesMask[8])
                {
                    for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
                    {
                        if (DailyQuestsCompleted.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(DailyQuestsCompleted[i]);
                        }
                    }
                }
                if (changesMask[9])
                {
                    for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
                    {
                        if (AvailableQuestLineXQuestIDs.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
                        }
                    }
                }
                if (changesMask[10])
                {
                    for (int i = 0; i < Heirlooms.Size(); ++i)
                    {
                        if (Heirlooms.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Heirlooms[i]);
                        }
                    }
                }
                if (changesMask[11])
                {
                    for (int i = 0; i < HeirloomFlags.Size(); ++i)
                    {
                        if (HeirloomFlags.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(HeirloomFlags[i]);
                        }
                    }
                }
                if (changesMask[12])
                {
                    for (int i = 0; i < Toys.Size(); ++i)
                    {
                        if (Toys.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Toys[i]);
                        }
                    }
                }
                if (changesMask[13])
                {
                    for (int i = 0; i < ToyFlags.Size(); ++i)
                    {
                        if (ToyFlags.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ToyFlags[i]);
                        }
                    }
                }
                if (changesMask[14])
                {
                    for (int i = 0; i < Transmog.Size(); ++i)
                    {
                        if (Transmog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(Transmog[i]);
                        }
                    }
                }
                if (changesMask[15])
                {
                    for (int i = 0; i < ConditionalTransmog.Size(); ++i)
                    {
                        if (ConditionalTransmog.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(ConditionalTransmog[i]);
                        }
                    }
                }
                if (changesMask[16])
                {
                    for (int i = 0; i < SelfResSpells.Size(); ++i)
                    {
                        if (SelfResSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(SelfResSpells[i]);
                        }
                    }
                }
                if (changesMask[17])
                {
                    for (int i = 0; i < RuneforgePowers.Size(); ++i)
                    {
                        if (RuneforgePowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(RuneforgePowers[i]);
                        }
                    }
                }
                if (changesMask[18])
                {
                    for (int i = 0; i < TransmogIllusions.Size(); ++i)
                    {
                        if (TransmogIllusions.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteUInt32(TransmogIllusions[i]);
                        }
                    }
                }
                if (changesMask[20])
                {
                    for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
                    {
                        if (SpellPctModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellPctModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[21])
                {
                    for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
                    {
                        if (SpellFlatModByLabel.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            SpellFlatModByLabel[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[22])
                {
                    for (int i = 0; i < MawPowers.Size(); ++i)
                    {
                        if (MawPowers.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            MawPowers[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[23])
                {
                    for (int i = 0; i < MultiFloorExploration.Size(); ++i)
                    {
                        if (MultiFloorExploration.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            MultiFloorExploration[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[24])
                {
                    for (int i = 0; i < RecipeProgression.Size(); ++i)
                    {
                        if (RecipeProgression.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            RecipeProgression[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[25])
                {
                    for (int i = 0; i < ReplayedQuests.Size(); ++i)
                    {
                        if (ReplayedQuests.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            ReplayedQuests[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[26])
                {
                    for (int i = 0; i < DisabledSpells.Size(); ++i)
                    {
                        if (DisabledSpells.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            data.WriteInt32(DisabledSpells[i]);
                        }
                    }
                }
                if (changesMask[19])
                {
                    for (int i = 0; i < CharacterRestrictions.Size(); ++i)
                    {
                        if (CharacterRestrictions.HasChanged(i) || ignoreNestedChangesMask)
                        {
                            CharacterRestrictions[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                        }
                    }
                }
                if (changesMask[29])
                {
                    data.WritePackedGuid(FarsightObject);
                }
                if (changesMask[30])
                {
                    data.WritePackedGuid(SummonedBattlePetGUID);
                }
                if (changesMask[31])
                {
                    data.WriteUInt64(Coinage);
                }
                if (changesMask[32])
                {
                    data.WriteUInt32(XP);
                }
                if (changesMask[33])
                {
                    data.WriteUInt32(NextLevelXP);
                }
            }
            if (changesMask[34])
            {
                if (changesMask[35])
                {
                    data.WriteInt32(TrialXP);
                }
                if (changesMask[36])
                {
                    ((SkillInfo)Skill).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[37])
                {
                    data.WriteUInt32(CharacterPoints);
                }
                if (changesMask[38])
                {
                    data.WriteUInt32(MaxTalentTiers);
                }
                if (changesMask[39])
                {
                    data.WriteUInt32(TrackCreatureMask);
                }
                if (changesMask[40])
                {
                    data.WriteFloat(MainhandExpertise);
                }
                if (changesMask[41])
                {
                    data.WriteFloat(OffhandExpertise);
                }
                if (changesMask[42])
                {
                    data.WriteFloat(RangedExpertise);
                }
                if (changesMask[43])
                {
                    data.WriteFloat(CombatRatingExpertise);
                }
                if (changesMask[44])
                {
                    data.WriteFloat(BlockPercentage);
                }
                if (changesMask[45])
                {
                    data.WriteFloat(DodgePercentage);
                }
                if (changesMask[46])
                {
                    data.WriteFloat(DodgePercentageFromAttribute);
                }
                if (changesMask[47])
                {
                    data.WriteFloat(ParryPercentage);
                }
                if (changesMask[48])
                {
                    data.WriteFloat(ParryPercentageFromAttribute);
                }
                if (changesMask[49])
                {
                    data.WriteFloat(CritPercentage);
                }
                if (changesMask[50])
                {
                    data.WriteFloat(RangedCritPercentage);
                }
                if (changesMask[51])
                {
                    data.WriteFloat(OffhandCritPercentage);
                }
                if (changesMask[52])
                {
                    data.WriteFloat(SpellCritPercentage);
                }
                if (changesMask[53])
                {
                    data.WriteUInt32(ShieldBlock);
                }
                if (changesMask[54])
                {
                    data.WriteFloat(ShieldBlockCritPercentage);
                }
                if (changesMask[55])
                {
                    data.WriteFloat(Mastery);
                }
                if (changesMask[56])
                {
                    data.WriteFloat(Speed);
                }
                if (changesMask[57])
                {
                    data.WriteFloat(Avoidance);
                }
                if (changesMask[58])
                {
                    data.WriteFloat(Sturdiness);
                }
                if (changesMask[59])
                {
                    data.WriteInt32(Versatility);
                }
                if (changesMask[60])
                {
                    data.WriteFloat(VersatilityBonus);
                }
                if (changesMask[61])
                {
                    data.WriteFloat(PvpPowerDamage);
                }
                if (changesMask[62])
                {
                    data.WriteFloat(PvpPowerHealing);
                }
                if (changesMask[63])
                {
                    data.WriteInt32(ModHealingDonePos);
                }
                if (changesMask[64])
                {
                    data.WriteFloat(ModHealingPercent);
                }
                if (changesMask[65])
                {
                    data.WriteFloat(ModPeriodicHealingDonePercent);
                }
            }
            if (changesMask[66])
            {
                if (changesMask[67])
                {
                    data.WriteFloat(ModSpellPowerPercent);
                }
                if (changesMask[68])
                {
                    data.WriteFloat(ModResiliencePercent);
                }
                if (changesMask[69])
                {
                    data.WriteFloat(OverrideSpellPowerByAPPercent);
                }
                if (changesMask[70])
                {
                    data.WriteFloat(OverrideAPBySpellPowerPercent);
                }
                if (changesMask[71])
                {
                    data.WriteInt32(ModTargetResistance);
                }
                if (changesMask[72])
                {
                    data.WriteInt32(ModTargetPhysicalResistance);
                }
                if (changesMask[73])
                {
                    data.WriteUInt32(LocalFlags);
                }
                if (changesMask[74])
                {
                    data.WriteUInt8(GrantableLevels);
                }
                if (changesMask[75])
                {
                    data.WriteUInt8(MultiActionBars);
                }
                if (changesMask[76])
                {
                    data.WriteUInt8(LifetimeMaxRank);
                }
                if (changesMask[77])
                {
                    data.WriteUInt8(NumRespecs);
                }
                if (changesMask[78])
                {
                    data.WriteUInt32(PvpMedals);
                }
                if (changesMask[79])
                {
                    data.WriteUInt16(TodayHonorableKills);
                }
                if (changesMask[80])
                {
                    data.WriteUInt16(YesterdayHonorableKills);
                }
                if (changesMask[81])
                {
                    data.WriteUInt32(LifetimeHonorableKills);
                }
                if (changesMask[82])
                {
                    data.WriteUInt32(WatchedFactionIndex);
                }
                if (changesMask[83])
                {
                    data.WriteInt32(MaxLevel);
                }
                if (changesMask[84])
                {
                    data.WriteInt32(ScalingPlayerLevelDelta);
                }
                if (changesMask[85])
                {
                    data.WriteInt32(MaxCreatureScalingLevel);
                }
                if (changesMask[86])
                {
                    data.WriteUInt32(PetSpellPower);
                }
                if (changesMask[87])
                {
                    data.WriteFloat(UiHitModifier);
                }
                if (changesMask[88])
                {
                    data.WriteFloat(UiSpellHitModifier);
                }
                if (changesMask[89])
                {
                    data.WriteInt32(HomeRealmTimeOffset);
                }
                if (changesMask[90])
                {
                    data.WriteFloat(ModPetHaste);
                }
                if (changesMask[91])
                {
                    data.WriteInt8(JailersTowerLevelMax);
                }
                if (changesMask[92])
                {
                    data.WriteInt8(JailersTowerLevel);
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
                    data.WriteUInt32(OverrideSpellsID);
                }
                if (changesMask[97])
                {
                    data.WriteUInt16(LootSpecID);
                }
            }
            if (changesMask[98])
            {
                if (changesMask[99])
                {
                    data.WriteUInt32(OverrideZonePVPType);
                }
                if (changesMask[100])
                {
                    data.WritePackedGuid(BnetAccount);
                }
                if (changesMask[101])
                {
                    data.WriteUInt64(GuildClubMemberID);
                }
                if (changesMask[102])
                {
                    data.WriteUInt32(Honor);
                }
                if (changesMask[103])
                {
                    data.WriteUInt32(HonorNextLevel);
                }
                if (changesMask[104])
                {
                    data.WriteUInt8(NumBankSlots);
                }
                if (changesMask[107])
                {
                    data.WriteInt32(UiChromieTimeExpansionID);
                }
                if (changesMask[108])
                {
                    data.WriteInt32(TransportServerTime);
                }
                if (changesMask[109])
                {
                    data.WriteUInt32(WeeklyRewardsPeriodSinceOrigin);
                }
                if (changesMask[110])
                {
                    data.WriteInt16(DEBUGSoulbindConduitRank);
                }
            }
            if (changesMask[98])
            {
                data.WriteBits(QuestSession.GetValue().HasValue, 1);
                if (changesMask[106])
                {
                    ((ActivePlayerUnk901)Field_1410).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (changesMask[105])
                {
                    if (QuestSession.GetValue().HasValue)
                    {
                        QuestSession.GetValue().Value.WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
                if (changesMask[111])
                {
                    DungeonScore._value.Write(data);
                }
            }
            if (changesMask[112])
            {
                for (int i = 0; i < 199; ++i)
                {
                    if (changesMask[113 + i])
                    {
                        data.WritePackedGuid(InvSlots[i]);
                    }
                }
            }
            if (changesMask[312])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[313 + i])
                    {
                        data.WriteUInt32(TrackResourceMask[i]);
                    }
                }
            }
            if (changesMask[315])
            {
                for (int i = 0; i < 240; ++i)
                {
                    if (changesMask[316 + i])
                    {
                        data.WriteUInt64(ExploredZones[i]);
                    }
                }
            }
            if (changesMask[556])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[557 + i])
                    {
                        RestInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            if (changesMask[559])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[560 + i])
                    {
                        data.WriteInt32(ModDamageDonePos[i]);
                    }
                    if (changesMask[567 + i])
                    {
                        data.WriteInt32(ModDamageDoneNeg[i]);
                    }
                    if (changesMask[574 + i])
                    {
                        data.WriteFloat(ModDamageDonePercent[i]);
                    }
                    if (changesMask[581 + i])
                    {
                        data.WriteFloat(ModHealingDonePercent[i]);
                    }
                }
            }
            if (changesMask[588])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[589 + i])
                    {
                        data.WriteFloat(WeaponDmgMultipliers[i]);
                    }
                    if (changesMask[592 + i])
                    {
                        data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
                    }
                }
            }
            if (changesMask[595])
            {
                for (int i = 0; i < 12; ++i)
                {
                    if (changesMask[596 + i])
                    {
                        data.WriteUInt32(BuybackPrice[i]);
                    }
                    if (changesMask[608 + i])
                    {
                        data.WriteUInt32(BuybackTimestamp[i]);
                    }
                }
            }
            if (changesMask[620])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (changesMask[621 + i])
                    {
                        data.WriteUInt32(CombatRatings[i]);
                    }
                }
            }
            if (changesMask[660])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[661 + i])
                    {
                        data.WriteUInt32(NoReagentCostMask[i]);
                    }
                }
            }
            if (changesMask[665])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[666 + i])
                    {
                        data.WriteUInt32(ProfessionSkillLine[i]);
                    }
                }
            }
            if (changesMask[668])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[669 + i])
                    {
                        data.WriteUInt32(BagSlotFlags[i]);
                    }
                }
            }
            if (changesMask[673])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[674 + i])
                    {
                        data.WriteUInt32(BankBagSlotFlags[i]);
                    }
                }
            }
            if (changesMask[681])
            {
                for (int i = 0; i < 875; ++i)
                {
                    if (changesMask[682 + i])
                    {
                        data.WriteUInt64(QuestCompleted[i]);
                    }
                }
            }
            if (changesMask[653])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[654 + i])
                    {
                        PvpInfo[i].WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                    }
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BackpackAutoSortDisabled);
            ClearChangesMask(BankAutoSortDisabled);
            ClearChangesMask(SortBagsRightToLeft);
            ClearChangesMask(InsertItemsLeftToRight);
            ClearChangesMask(Research);
            ClearChangesMask(KnownTitles);
            ClearChangesMask(ResearchSites);
            ClearChangesMask(ResearchSiteProgress);
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
            ClearChangesMask(SpellPctModByLabel);
            ClearChangesMask(SpellFlatModByLabel);
            ClearChangesMask(MawPowers);
            ClearChangesMask(MultiFloorExploration);
            ClearChangesMask(RecipeProgression);
            ClearChangesMask(ReplayedQuests);
            ClearChangesMask(DisabledSpells);
            ClearChangesMask(CharacterRestrictions);
            ClearChangesMask(FarsightObject);
            ClearChangesMask(SummonedBattlePetGUID);
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
            ClearChangesMask(BnetAccount);
            ClearChangesMask(GuildClubMemberID);
            ClearChangesMask(Honor);
            ClearChangesMask(HonorNextLevel);
            ClearChangesMask(NumBankSlots);
            ClearChangesMask(Field_1410);
            ClearChangesMask(QuestSession);
            ClearChangesMask(UiChromieTimeExpansionID);
            ClearChangesMask(TransportServerTime);
            ClearChangesMask(WeeklyRewardsPeriodSinceOrigin);
            ClearChangesMask(DEBUGSoulbindConduitRank);
            ClearChangesMask(DungeonScore);
            ClearChangesMask(InvSlots);
            ClearChangesMask(TrackResourceMask);
            ClearChangesMask(ExploredZones);
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
            ClearChangesMask(PvpInfo);
            ClearChangesMask(NoReagentCostMask);
            ClearChangesMask(ProfessionSkillLine);
            ClearChangesMask(BagSlotFlags);
            ClearChangesMask(BankBagSlotFlags);
            ClearChangesMask(QuestCompleted);
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
        public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new(0, 8);
        public UpdateField<ObjectGuid> CreatedBy = new(0, 9);
        public UpdateField<ObjectGuid> GuildGUID = new(0, 10);
        public UpdateField<uint> Flags = new(0, 11);
        public UpdateField<Quaternion> ParentRotation = new(0, 12);
        public UpdateField<uint> FactionTemplate = new(0, 13);
        public UpdateField<sbyte> State = new(0, 14);
        public UpdateField<sbyte> TypeID = new(0, 15);
        public UpdateField<byte> PercentHealth = new(0, 16);
        public UpdateField<uint> ArtKit = new(0, 17);
        public UpdateField<uint> CustomParam = new(0, 18);
        public UpdateField<uint> Level = new(0, 19);
        public UpdateField<uint> AnimGroupInstance = new(0, 20);

        public GameObjectFieldData() : base(0, TypeId.GameObject, 21) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
        {
            data.WriteUInt32(DisplayID);
            data.WriteUInt32(SpellVisualID);
            data.WriteUInt32(StateSpellVisualID);
            data.WriteUInt32(SpawnTrackingStateAnimID);
            data.WriteUInt32(SpawnTrackingStateAnimKitID);
            data.WriteInt32(((List<uint>)StateWorldEffectIDs).Count);
            data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
            for (int i = 0; i < ((List<uint>)StateWorldEffectIDs).Count; ++i)
            {
                data.WriteUInt32(((List<uint>)StateWorldEffectIDs)[i]);
            }
            data.WritePackedGuid(CreatedBy);
            data.WritePackedGuid(GuildGUID);
            data.WriteUInt32(GetViewerGameObjectFlags(Flags, owner, receiver));
            Quaternion rotation = ParentRotation;
            data.WriteFloat(rotation.X);
            data.WriteFloat(rotation.Y);
            data.WriteFloat(rotation.Z);
            data.WriteFloat(rotation.W);
            data.WriteUInt32(FactionTemplate);
            data.WriteInt8(GetViewerGameObjectState(State, owner, receiver));
            data.WriteInt8(TypeID);
            data.WriteUInt8(PercentHealth);
            data.WriteUInt32(ArtKit);
            data.WriteInt32(EnableDoodadSets.Size());
            data.WriteUInt32(CustomParam);
            data.WriteUInt32(GetViewerGameObjectLevel(Level, owner, receiver));
            data.WriteUInt32(AnimGroupInstance);
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
            data.WriteBits(changesMask.GetBlock(0), 21);

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
                    data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
                }
                if (changesMask[9])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (changesMask[10])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(GetViewerGameObjectFlags(Flags, owner, receiver));
                }
                if (changesMask[12])
                {
                    data.WriteFloat(((Quaternion)ParentRotation).X);
                    data.WriteFloat(((Quaternion)ParentRotation).Y);
                    data.WriteFloat(((Quaternion)ParentRotation).Z);
                    data.WriteFloat(((Quaternion)ParentRotation).W);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(FactionTemplate);
                }
                if (changesMask[14])
                {
                    data.WriteInt8(GetViewerGameObjectState(State, owner, receiver));
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
                if (changesMask[19])
                {
                    data.WriteUInt32(GetViewerGameObjectLevel(Level, owner, receiver));
                }
                if (changesMask[20])
                {
                    data.WriteUInt32(AnimGroupInstance);
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
            _changesMask.ResetAll();
        }

        uint GetViewerGameObjectFlags(uint flags, GameObject gameObject, Player receiver)
        {
            if (gameObject.GetGoType() == GameObjectTypes.Chest)
                if (gameObject.GetGoInfo().Chest.usegrouplootrules != 0 && !gameObject.IsLootAllowedFor(receiver))
                    flags |= (uint)(GameObjectFlags.Locked | GameObjectFlags.NotSelectable);

            return (uint)flags;
        }

        uint GetViewerGameObjectLevel(uint level, GameObject gameObject, Player receiver)
        {
            bool isStoppableTransport = gameObject.GetGoType() == GameObjectTypes.Transport && !gameObject.GetGoValue().Transport.StopFrames.Empty();
            return isStoppableTransport ? gameObject.GetGoValue().Transport.PathProgress : level;
        }

        sbyte GetViewerGameObjectState(sbyte state, GameObject gameObject, Player receiver)
        {
            bool isStoppableTransport = gameObject.GetGoType() == GameObjectTypes.Transport && !gameObject.GetGoValue().Transport.StopFrames.Empty();
            if (isStoppableTransport && gameObject.GetGoState() == GameObjectState.TransportActive)
                if (((gameObject.GetGoValue().Transport.StateUpdateTimer / 20000) & 1) != 0)
                    state = (sbyte)GameObjectState.TransportStopped;

            return state;
        }
    }

    public class DynamicObjectData : BaseUpdateData<DynamicObject>
    {
        public UpdateField<ObjectGuid> Caster = new(0, 1);
        public UpdateField<SpellCastVisualField> SpellVisual = new(0, 2);
        public UpdateField<uint> SpellID = new(0, 3);
        public UpdateField<float> Radius = new(0, 4);
        public UpdateField<uint> CastTime = new(0, 5);
        public UpdateField<byte> Type = new(0, 6);

        public DynamicObjectData() : base(0, TypeId.DynamicObject, 7) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
        {
            data.WritePackedGuid(Caster);
            ((SpellCastVisualField)SpellVisual).WriteCreate(data, owner, receiver);
            data.WriteUInt32(SpellID);
            data.WriteFloat(Radius);
            data.WriteUInt32(CastTime);
            data.WriteUInt8(Type);
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
                    ((SpellCastVisualField)SpellVisual).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(SpellID);
                }
                if (_changesMask[4])
                {
                    data.WriteFloat(Radius);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(CastTime);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt8(Type);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Caster);
            ClearChangesMask(SpellVisual);
            ClearChangesMask(SpellID);
            ClearChangesMask(Radius);
            ClearChangesMask(CastTime);
            ClearChangesMask(Type);
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
        public UpdateField<uint> StateSpellVisualKitID = new(0, 12);
        public UpdateFieldArray<uint> Items = new(19, 13, 14);

        public CorpseData() : base(0, TypeId.Corpse, 33) { }

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
        public UpdateField<SpellCastVisualField> SpellVisual = new(0, 10);
        public UpdateField<float> BoundsRadius2D = new(0, 11);
        public UpdateField<uint> DecalPropertiesID = new(0, 12);
        public UpdateField<ObjectGuid> CreatingEffectGUID = new(0, 13);

        public AreaTriggerFieldData() : base(0, TypeId.AreaTrigger, 14) { }

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

            ((SpellCastVisualField)SpellVisual).WriteCreate(data, owner, receiver);

            data.WriteFloat(BoundsRadius2D);
            data.WriteUInt32(DecalPropertiesID);
            data.WritePackedGuid(CreatingEffectGUID);
            ((ScaleCurve)ExtraScaleCurve).WriteCreate(data, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
        {
            WriteUpdate(data, _changesMask, false, owner, receiver);
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, bool ignoreNestedChangesMask, AreaTrigger owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 14);

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
                    ((SpellCastVisualField)SpellVisual).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
                if (_changesMask[2])
                {
                    ((ScaleCurve)ExtraScaleCurve).WriteUpdate(data, ignoreNestedChangesMask, owner, receiver);
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
            ClearChangesMask(SpellVisual);
            ClearChangesMask(BoundsRadius2D);
            ClearChangesMask(DecalPropertiesID);
            ClearChangesMask(CreatingEffectGUID);
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
        public byte ChatType;

        public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
            data.WriteUInt8(ChatType);
        }

        public void WriteUpdate(WorldPacket data, bool ignoreChangesMask, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
            data.WriteUInt8(ChatType);
        }
    }

    public class ConversationActorField
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
    }

    public class ConversationData : BaseUpdateData<Conversation>
    {
        public UpdateField<bool> DontPlayBroadcastTextSounds = new(0, 1);
        public UpdateField<List<ConversationLine>> Lines = new(0, 2);
        public DynamicUpdateField<ConversationActorField> Actors = new(0, 3);
        public UpdateField<uint> LastLineEndTime = new(0, 4);
        public UpdateField<uint>Progress = new(0, 5);

        public ConversationData() : base(0, TypeId.Conversation, 6) { }

        public void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
        {
            data.WriteInt32(((List<ConversationLine>)Lines).Count);
            data.WriteUInt32(LastLineEndTime);
            data.WriteUInt32(Progress);
            for (int i = 0; i < ((List<ConversationLine>)Lines).Count; ++i)
            {
                ((List<ConversationLine>)Lines)[i].WriteCreate(data, owner, receiver);
            }
            data.WriteBit(DontPlayBroadcastTextSounds);
            data.WriteInt32(Actors.Size());
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
                    data.WriteUInt32(LastLineEndTime);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(Progress);
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
    }
}
