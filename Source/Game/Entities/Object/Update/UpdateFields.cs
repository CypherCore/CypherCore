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
using Game.Network;
using System;
using System.Collections.Generic;
using Framework.GameMath;
using Game.Spells;
using Game.DataStorage;
using Framework.Dynamic;

namespace Game.Entities
{
    public class ObjectFieldData : BaseUpdateData<WorldObject>
    {
        public UpdateField<uint> EntryId = new UpdateField<uint>(0, 1);
        public UpdateField<uint> DynamicFlags = new UpdateField<uint>(0, 2);
        public UpdateField<float> Scale = new UpdateField<float>(0, 3);

        public ObjectFieldData() : base(0, TypeId.Object, 4) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            data.WriteUInt32(EntryId);
            data.WriteUInt32(GetViewerDependentDynamicFlags(DynamicFlags, owner, receiver));
            data.WriteFloat(Scale);
        }

        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(EntryId);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt32(GetViewerDependentDynamicFlags(DynamicFlags, owner, receiver));
                }
                if (_changesMask[3])
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
        public UpdateField<uint> ID = new UpdateField<uint>(0, 1);
        public UpdateField<uint> Duration = new UpdateField<uint>(0, 2);
        public UpdateField<short> Charges = new UpdateField<short>(0, 3);
        public UpdateField<ushort> Inactive = new UpdateField<ushort>(0, 4);

        public ItemEnchantment() : base(5) { }

        public override void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(ID);
            data.WriteUInt32(Duration);
            data.WriteInt16(Charges);
            data.WriteUInt16(Inactive);
        }

        public override void WriteUpdate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 5);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(ID);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt32(Duration);
                }
                if (_changesMask[3])
                {
                    data.WriteInt16(Charges);
                }
                if (_changesMask[4])
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
        public void WriteUpdate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt16(ArtifactPowerId);
            data.WriteUInt8(PurchasedRank);
            data.WriteUInt8(CurrentRankWithBonus);
        }
    }

    public class SocketedGem : BaseUpdateData<Item>
    {
        public UpdateField<uint> ItemId = new UpdateField<uint>(0, 1);
        public UpdateField<byte> Context = new UpdateField<byte>(0, 2);
        public UpdateFieldArray<ushort> BonusListIDs = new UpdateFieldArray<ushort>(16, 3, 4);

        public SocketedGem() : base(20) { }

        public override void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(ItemId);
            for (int i = 0; i < 16; ++i)
                data.WriteUInt16(BonusListIDs[i]);

            data.WriteUInt8(Context);
        }

        public override void WriteUpdate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlocksMask(0), 1);
            if (_changesMask.GetBlock(0) != 0)
                data.WriteBits(_changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(ItemId);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt8(Context);
                }
            }
            if (_changesMask[3])
            {
                for (int i = 0; i < 16; ++i)
                {
                    if (_changesMask[4 + i])
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
        public UpdateField<List<uint>> BonusListIDs = new UpdateField<List<uint>>(0, 1);
        public DynamicUpdateField<uint> Modifiers = new DynamicUpdateField<uint>(0, 2);
        public DynamicUpdateField<ArtifactPower> ArtifactPowers = new DynamicUpdateField<ArtifactPower>(0, 3);
        public DynamicUpdateField<SocketedGem> Gems = new DynamicUpdateField<SocketedGem>(0, 4);
        public UpdateField<ObjectGuid> Owner = new UpdateField<ObjectGuid>(0, 5);
        public UpdateField<ObjectGuid> ContainedIn = new UpdateField<ObjectGuid>(0, 6);
        public UpdateField<ObjectGuid> Creator = new UpdateField<ObjectGuid>(0, 7);
        public UpdateField<ObjectGuid> GiftCreator = new UpdateField<ObjectGuid>(0, 8);
        public UpdateField<uint> StackCount = new UpdateField<uint>(0, 9);
        public UpdateField<uint> Expiration = new UpdateField<uint>(0, 10);
        public UpdateField<uint> DynamicFlags = new UpdateField<uint>(0, 11);
        public UpdateField<uint> Durability = new UpdateField<uint>(0, 12);
        public UpdateField<uint> MaxDurability = new UpdateField<uint>(0, 13);
        public UpdateField<uint> CreatePlayedTime = new UpdateField<uint>(0, 14);
        public UpdateField<uint> ModifiersMask = new UpdateField<uint>(0, 15);
        public UpdateField<int> Context = new UpdateField<int>(0, 16);
        public UpdateField<ulong> ArtifactXP = new UpdateField<ulong>(0, 17);
        public UpdateField<byte> ItemAppearanceModID = new UpdateField<byte>(0, 18);
        public UpdateField<uint> Field_130 = new UpdateField<uint>(0, 19);
        public UpdateFieldArray<int> SpellCharges = new UpdateFieldArray<int>(5, 20, 21);
        public UpdateFieldArray<ItemEnchantment> Enchantment = new UpdateFieldArray<ItemEnchantment>(13, 26, 27);

        public ItemData() : base(0, TypeId.Item, 40) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
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
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt32(ModifiersMask);
            }
            data.WriteInt32(Context);
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt64(ArtifactXP);
                data.WriteUInt8(ItemAppearanceModID);
            }
            data.WriteInt32(Modifiers.Size());
            data.WriteInt32(ArtifactPowers.Size());
            data.WriteInt32(Gems.Size());
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
            {
                data.WriteUInt32(Field_130);
            }
            for (int i = 0; i < Modifiers.Size(); ++i)
            {
                data.WriteUInt32(Modifiers[i]);
            }
            for (int i = 0; i < ArtifactPowers.Size(); ++i)
            {
                ArtifactPowers[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < Gems.Size(); ++i)
            {
                Gems[i].WriteCreate(data, owner, receiver);
            }
        }

        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new UpdateMask(40, new uint[] { 0xFC0149FFu, 0x000000FFu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, fieldVisibilityFlags, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget |= new UpdateMask(40, new uint[] { 0x03FEB600u, 0x00000000u });
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
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
                    Modifiers.WriteUpdateMask(data);
                }
                if (changesMask[3])
                {
                    ArtifactPowers.WriteUpdateMask(data);
                }
                if (changesMask[4])
                {
                    Gems.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    for (int i = 0; i < Modifiers.Size(); ++i)
                    {
                        if (Modifiers.HasChanged(i))
                        {
                            data.WriteUInt32(Modifiers[i]);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < ArtifactPowers.Size(); ++i)
                    {
                        if (ArtifactPowers.HasChanged(i))
                        {
                            ArtifactPowers[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < Gems.Size(); ++i)
                    {
                        if (Gems.HasChanged(i))
                        {
                            Gems[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(Owner);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(ContainedIn);
                }
                if (changesMask[7])
                {
                    data.WritePackedGuid(Creator);
                }
                if (changesMask[8])
                {
                    data.WritePackedGuid(GiftCreator);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(StackCount);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(Expiration);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(DynamicFlags);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(Durability);
                }
                if (changesMask[13])
                {
                    data.WriteUInt32(MaxDurability);
                }
                if (changesMask[14])
                {
                    data.WriteUInt32(CreatePlayedTime);
                }
                if (changesMask[15])
                {
                    data.WriteUInt32(ModifiersMask);
                }
                if (changesMask[16])
                {
                    data.WriteInt32(Context);
                }
                if (changesMask[17])
                {
                    data.WriteUInt64(ArtifactXP);
                }
                if (changesMask[18])
                {
                    data.WriteUInt8(ItemAppearanceModID);
                }
                if (changesMask[19])
                {
                    data.WriteUInt32(Field_130);
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
                        Enchantment[i].WriteUpdate(data, owner, receiver);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(BonusListIDs);
            ClearChangesMask(Modifiers);
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
            ClearChangesMask(ModifiersMask);
            ClearChangesMask(Context);
            ClearChangesMask(ArtifactXP);
            ClearChangesMask(ItemAppearanceModID);
            ClearChangesMask(Field_130);
            ClearChangesMask(SpellCharges);
            ClearChangesMask(Enchantment);
            _changesMask.ResetAll();
        }
    }

    public class ContainerData : BaseUpdateData<Bag>
    {
        public UpdateField<uint> NumSlots = new UpdateField<uint>(0, 1);
        public UpdateFieldArray<ObjectGuid> Slots = new UpdateFieldArray<ObjectGuid>(36, 2, 3);

        public ContainerData() : base(0, TypeId.Container, 39) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
        {
            for (int i = 0; i < 36; ++i)
            {
                data.WritePackedGuid(Slots[i]);
            }
            data.WriteUInt32(NumSlots);
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Bag owner, Player receiver)
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
        public UpdateFieldArray<int> Selections = new UpdateFieldArray<int>(5, 0, 1);

        public AzeriteEmpoweredItemData() : base(6) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            for (int i = 0; i < 5; ++i)
            {
                data.WriteInt32(Selections[i]);
            }
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
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
        uint AzeriteEssenceID;
        uint Rank;

        public void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(AzeriteEssenceID);
            data.WriteUInt32(Rank);
        }
        public void WriteUpdate(WorldPacket data, Item owner, Player receiver)
        {
            data.WriteUInt32(AzeriteEssenceID);
            data.WriteUInt32(Rank);
        }
    }

    public class SelectedAzeriteEssences : BaseUpdateData<Item>
    {
        UpdateField<uint> SpecializationID = new UpdateField<uint>(0, 1);
        UpdateField<uint> Enabled = new UpdateField<uint>(0, 2);
        UpdateFieldArray<uint> AzeriteEssenceID = new UpdateFieldArray<uint>(3, 3, 4);

        public SelectedAzeriteEssences() : base(7) { }

        public override void WriteCreate(WorldPacket data, Item owner, Player receiver)
        {
            for (int i = 0; i < 3; ++i)
            {
                data.WriteUInt32(AzeriteEssenceID[i]);
            }
            data.WriteUInt32(SpecializationID);
            data.WriteBits(Enabled, 1);
            data.FlushBits();
        }
        public override void WriteUpdate(WorldPacket data, Item owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
            data.WriteBits(changesMask.GetBlocksMask(0), 1);
            if (changesMask.GetBlock(0) != 0)
                data.WriteBits(changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    data.WriteUInt32(SpecializationID);
                }
                if (changesMask[2])
                {
                    data.WriteBits(Enabled, 1);
                }
            }
            if (changesMask[3])
            {
                for (int i = 0; i < 3; ++i)
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
            ClearChangesMask(SpecializationID);
            ClearChangesMask(Enabled);
            ClearChangesMask(AzeriteEssenceID);
            _changesMask.ResetAll();
        }
    }

    public class AzeriteItemData : BaseUpdateData<Item>
    {
        public DynamicUpdateField<UnlockedAzeriteEssence> UnlockedEssences = new DynamicUpdateField<UnlockedAzeriteEssence>(0, 1);
        public DynamicUpdateField<SelectedAzeriteEssences> SelectedEssences = new DynamicUpdateField<SelectedAzeriteEssences>(0, 2);
        public DynamicUpdateField<uint> UnlockedEssenceMilestones = new DynamicUpdateField<uint>(0, 3);
        public UpdateField<ulong> Xp = new UpdateField<ulong>(0, 4);
        public UpdateField<uint> Level = new UpdateField<uint>(0, 5);
        public UpdateField<uint> AuraLevel = new UpdateField<uint>(0, 6);
        public UpdateField<uint> KnowledgeLevel = new UpdateField<uint>(0, 7);
        public UpdateField<int> DEBUGknowledgeWeek = new UpdateField<int>(0, 8);

        public AzeriteItemData() : base(9) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
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
            for (int i = 0; i < SelectedEssences.Size(); ++i)
            {
                SelectedEssences[i].WriteCreate(data, owner, receiver);
            }
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new UpdateMask(9, new[] { 0x0000000Fu });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, fieldVisibilityFlags, owner, receiver);
        }
        void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget |= new UpdateMask(9, new[] { 0x000001F0u });
        }
        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags, Item owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlock(0), 9);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    UnlockedEssences.WriteUpdateMask(data);
                }
                if (changesMask[2])
                {
                    SelectedEssences.WriteUpdateMask(data);
                }
                if (changesMask[3])
                {
                    UnlockedEssenceMilestones.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    for (int i = 0; i < UnlockedEssences.Size(); ++i)
                    {
                        if (UnlockedEssences.HasChanged(i))
                        {
                            UnlockedEssences[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[2])
                {
                    for (int i = 0; i < SelectedEssences.Size(); ++i)
                    {
                        if (SelectedEssences.HasChanged(i))
                        {
                            SelectedEssences[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < UnlockedEssenceMilestones.Size(); ++i)
                    {
                        if (UnlockedEssenceMilestones.HasChanged(i))
                        {
                            data.WriteUInt32(UnlockedEssenceMilestones[i]);
                        }
                    }
                }
                if (changesMask[4])
                {
                    data.WriteUInt64(Xp);
                }
                if (changesMask[5])
                {
                    data.WriteUInt32(Level);
                }
                if (changesMask[6])
                {
                    data.WriteUInt32(AuraLevel);
                }
                if (changesMask[7])
                {
                    data.WriteUInt32(KnowledgeLevel);
                }
                if (changesMask[8])
                {
                    data.WriteInt32(DEBUGknowledgeWeek);
                }
            }
        }

        public override void ClearChangesMask()
        {
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

    public class UnitChannel
    {
        public uint SpellID;
        public uint SpellXSpellVisualID;

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellXSpellVisualID);
        }
        public void WriteUpdate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(SpellID);
            data.WriteUInt32(SpellXSpellVisualID);
        }
    }

    public class VisibleItem : BaseUpdateData<Unit>
    {
        public UpdateField<uint> ItemID = new UpdateField<uint>(0, 1);
        public UpdateField<ushort> ItemAppearanceModID = new UpdateField<ushort>(0, 2);
        public UpdateField<ushort> ItemVisual = new UpdateField<ushort>(0, 3);

        public VisibleItem() : base(4) { }

        public override void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteUInt32(ItemID);
            data.WriteUInt16(ItemAppearanceModID);
            data.WriteUInt16(ItemVisual);
        }
        public override void WriteUpdate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 4);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(ItemID);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt16(ItemAppearanceModID);
                }
                if (_changesMask[3])
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
        int SpellID;
        int AuraSpellID;

        public void WriteCreate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(AuraSpellID);
        }
        public void WriteUpdate(WorldPacket data, Unit owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(AuraSpellID);
        }
    }

    public class UnitData : BaseUpdateData<Unit>
    {
        public UpdateField<List<uint>> StateWorldEffectIDs = new UpdateField<List<uint>>(0, 1);
        public DynamicUpdateField<PassiveSpellHistory> PassiveSpells = new DynamicUpdateField<PassiveSpellHistory>(0, 2);
        public DynamicUpdateField<int> WorldEffects = new DynamicUpdateField<int>(0, 3);
        public DynamicUpdateField<ObjectGuid> ChannelObjects = new DynamicUpdateField<ObjectGuid>(0, 4);
        public UpdateField<uint> DisplayID = new UpdateField<uint>(0, 5);
        public UpdateField<uint> StateSpellVisualID = new UpdateField<uint>(0, 6);
        public UpdateField<uint> StateAnimID = new UpdateField<uint>(0, 7);
        public UpdateField<uint> StateAnimKitID = new UpdateField<uint>(0, 8);
        public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new UpdateField<uint>(0, 9);
        public UpdateField<ObjectGuid> Charm = new UpdateField<ObjectGuid>(0, 10);
        public UpdateField<ObjectGuid> Summon = new UpdateField<ObjectGuid>(0, 11);
        public UpdateField<ObjectGuid> Critter = new UpdateField<ObjectGuid>(0, 12);
        public UpdateField<ObjectGuid> CharmedBy = new UpdateField<ObjectGuid>(0, 13);
        public UpdateField<ObjectGuid> SummonedBy = new UpdateField<ObjectGuid>(0, 14);
        public UpdateField<ObjectGuid> CreatedBy = new UpdateField<ObjectGuid>(0, 15);
        public UpdateField<ObjectGuid> DemonCreator = new UpdateField<ObjectGuid>(0, 16);
        public UpdateField<ObjectGuid> LookAtControllerTarget = new UpdateField<ObjectGuid>(0, 17);
        public UpdateField<ObjectGuid> Target = new UpdateField<ObjectGuid>(0, 18);
        public UpdateField<ObjectGuid> BattlePetCompanionGUID = new UpdateField<ObjectGuid>(0, 19);
        public UpdateField<ulong> BattlePetDBID = new UpdateField<ulong>(0, 20);
        public UpdateField<UnitChannel> ChannelData = new UpdateField<UnitChannel>(0, 21);
        public UpdateField<uint> SummonedByHomeRealm = new UpdateField<uint>(0, 22);
        public UpdateField<byte> Race = new UpdateField<byte>(0, 23);
        public UpdateField<byte> ClassId = new UpdateField<byte>(0, 24);
        public UpdateField<byte> PlayerClassId = new UpdateField<byte>(0, 25);
        public UpdateField<byte> Sex = new UpdateField<byte>(0, 26);
        public UpdateField<byte> DisplayPower = new UpdateField<byte>(0, 27);
        public UpdateField<uint> OverrideDisplayPowerID = new UpdateField<uint>(0, 28);
        public UpdateField<ulong> Health = new UpdateField<ulong>(0, 29);
        public UpdateField<ulong> MaxHealth = new UpdateField<ulong>(0, 30);
        public UpdateField<uint> Level = new UpdateField<uint>(0, 31);
        public UpdateField<int> EffectiveLevel = new UpdateField<int>(32, 33);
        public UpdateField<int> ContentTuningID = new UpdateField<int>(32, 34);
        public UpdateField<int> ScalingLevelMin = new UpdateField<int>(32, 35);
        public UpdateField<int> ScalingLevelMax = new UpdateField<int>(32, 36);
        public UpdateField<int> ScalingLevelDelta = new UpdateField<int>(32, 37);
        public UpdateField<int> ScalingFactionGroup = new UpdateField<int>(32, 38);
        public UpdateField<int> ScalingHealthItemLevelCurveID = new UpdateField<int>(32, 39);
        public UpdateField<int> ScalingDamageItemLevelCurveID = new UpdateField<int>(32, 40);
        public UpdateField<uint> FactionTemplate = new UpdateField<uint>(32, 41);
        public UpdateField<uint> Flags = new UpdateField<uint>(32, 42);
        public UpdateField<uint> Flags2 = new UpdateField<uint>(32, 43);
        public UpdateField<uint> Flags3 = new UpdateField<uint>(32, 44);
        public UpdateField<uint> AuraState = new UpdateField<uint>(32, 45);
        public UpdateField<uint> RangedAttackRoundBaseTime = new UpdateField<uint>(32, 46);
        public UpdateField<float> BoundingRadius = new UpdateField<float>(32, 47);
        public UpdateField<float> CombatReach = new UpdateField<float>(32, 48);
        public UpdateField<float> DisplayScale = new UpdateField<float>(32, 49);
        public UpdateField<uint> NativeDisplayID = new UpdateField<uint>(32, 50);
        public UpdateField<float> NativeXDisplayScale = new UpdateField<float>(32, 51);
        public UpdateField<uint> MountDisplayID = new UpdateField<uint>(32, 52);
        public UpdateField<int> CosmeticMountDisplayID = new UpdateField<int>(32, 53);
        public UpdateField<float> MinDamage = new UpdateField<float>(32, 54);
        public UpdateField<float> MaxDamage = new UpdateField<float>(32, 55);
        public UpdateField<float> MinOffHandDamage = new UpdateField<float>(32, 56);
        public UpdateField<float> MaxOffHandDamage = new UpdateField<float>(32, 57);
        public UpdateField<byte> StandState = new UpdateField<byte>(32, 58);
        public UpdateField<byte> PetTalentPoints = new UpdateField<byte>(32, 59);
        public UpdateField<byte> VisFlags = new UpdateField<byte>(32, 60);
        public UpdateField<byte> AnimTier = new UpdateField<byte>(32, 61);
        public UpdateField<uint> PetNumber = new UpdateField<uint>(32, 62);
        public UpdateField<uint> PetNameTimestamp = new UpdateField<uint>(32, 63);
        public UpdateField<uint> PetExperience = new UpdateField<uint>(64, 65);
        public UpdateField<uint> PetNextLevelExperience = new UpdateField<uint>(64, 66);
        public UpdateField<float> ModCastingSpeed = new UpdateField<float>(64, 67);
        public UpdateField<float> ModSpellHaste = new UpdateField<float>(64, 68);
        public UpdateField<float> ModHaste = new UpdateField<float>(64, 69);
        public UpdateField<float> ModRangedHaste = new UpdateField<float>(64, 70);
        public UpdateField<float> ModHasteRegen = new UpdateField<float>(64, 71);
        public UpdateField<float> ModTimeRate = new UpdateField<float>(64, 72);
        public UpdateField<uint> CreatedBySpell = new UpdateField<uint>(64, 73);
        public UpdateField<int> EmoteState = new UpdateField<int>(64, 74);
        public UpdateField<uint> BaseMana = new UpdateField<uint>(64, 75);
        public UpdateField<uint> BaseHealth = new UpdateField<uint>(64, 76);
        public UpdateField<byte> SheatheState = new UpdateField<byte>(64, 77);
        public UpdateField<byte> PvpFlags = new UpdateField<byte>(64, 78);
        public UpdateField<byte> PetFlags = new UpdateField<byte>(64, 79);
        public UpdateField<byte> ShapeshiftForm = new UpdateField<byte>(64, 80);
        public UpdateField<int> AttackPower = new UpdateField<int>(64, 81);
        public UpdateField<int> AttackPowerModPos = new UpdateField<int>(64, 82);
        public UpdateField<int> AttackPowerModNeg = new UpdateField<int>(64, 83);
        public UpdateField<float> AttackPowerMultiplier = new UpdateField<float>(64, 84);
        public UpdateField<int> RangedAttackPower = new UpdateField<int>(64, 85);
        public UpdateField<int> RangedAttackPowerModPos = new UpdateField<int>(64, 86);
        public UpdateField<int> RangedAttackPowerModNeg = new UpdateField<int>(64, 87);
        public UpdateField<float> RangedAttackPowerMultiplier = new UpdateField<float>(64, 88);
        public UpdateField<int> MainHandWeaponAttackPower = new UpdateField<int>(64, 89);
        public UpdateField<int> OffHandWeaponAttackPower = new UpdateField<int>(64, 90);
        public UpdateField<int> RangedWeaponAttackPower = new UpdateField<int>(64, 91);
        public UpdateField<int> SetAttackSpeedAura = new UpdateField<int>(64, 92);
        public UpdateField<float> Lifesteal = new UpdateField<float>(64, 93);
        public UpdateField<float> MinRangedDamage = new UpdateField<float>(64, 94);
        public UpdateField<float> MaxRangedDamage = new UpdateField<float>(64, 95);
        public UpdateField<float> ManaCostModifierModifier = new UpdateField<float>(96, 97);
        public UpdateField<float> MaxHealthModifier = new UpdateField<float>(96, 98);
        public UpdateField<float> HoverHeight = new UpdateField<float>(96, 99);
        public UpdateField<uint> MinItemLevelCutoff = new UpdateField<uint>(96, 100);
        public UpdateField<uint> MinItemLevel = new UpdateField<uint>(96, 101);
        public UpdateField<uint> MaxItemLevel = new UpdateField<uint>(96, 102);
        public UpdateField<int> AzeriteItemLevel = new UpdateField<int>(96, 103);
        public UpdateField<int> WildBattlePetLevel = new UpdateField<int>(96, 104);
        public UpdateField<uint> BattlePetCompanionNameTimestamp = new UpdateField<uint>(96, 105);
        public UpdateField<int> InteractSpellID = new UpdateField<int>(96, 106);
        public UpdateField<int> ScaleDuration = new UpdateField<int>(96, 107);
        public UpdateField<int> SpellOverrideNameID = new UpdateField<int>(96, 108);
        public UpdateField<int> LooksLikeMountID = new UpdateField<int>(96, 109);
        public UpdateField<int> LooksLikeCreatureID = new UpdateField<int>(96, 110);
        public UpdateField<int> LookAtControllerID = new UpdateField<int>(96, 111);
        public UpdateField<int> TaxiNodesID = new UpdateField<int>(96, 112);
        public UpdateField<ObjectGuid> GuildGUID = new UpdateField<ObjectGuid>(96, 113);
        public UpdateFieldArray<uint> NpcFlags = new UpdateFieldArray<uint>(2, 114, 115);
        public UpdateFieldArray<int> Power = new UpdateFieldArray<int>(6, 117, 118);
        public UpdateFieldArray<uint> MaxPower = new UpdateFieldArray<uint>(6, 117, 124);
        public UpdateFieldArray<float> PowerRegenFlatModifier = new UpdateFieldArray<float>(6, 117, 130);
        public UpdateFieldArray<float> PowerRegenInterruptedFlatModifier = new UpdateFieldArray<float>(6, 117, 136);
        public UpdateFieldArray<VisibleItem> VirtualItems = new UpdateFieldArray<VisibleItem>(3, 142, 143);
        public UpdateFieldArray<uint> AttackRoundBaseTime = new UpdateFieldArray<uint>(2, 146, 147);
        public UpdateFieldArray<int> Stats = new UpdateFieldArray<int>(4, 149, 150);
        public UpdateFieldArray<int> StatPosBuff = new UpdateFieldArray<int>(4, 149, 154);
        public UpdateFieldArray<int> StatNegBuff = new UpdateFieldArray<int>(4, 149, 158);
        public UpdateFieldArray<int> Resistances = new UpdateFieldArray<int>(7, 162, 163);
        public UpdateFieldArray<int> BonusResistanceMods = new UpdateFieldArray<int>(7, 162, 170);
        public UpdateFieldArray<int> PowerCostModifier = new UpdateFieldArray<int>(7, 162, 177);
        public UpdateFieldArray<float> PowerCostMultiplier = new UpdateFieldArray<float>(7, 162, 184);

        public UnitData() : base(0, TypeId.Unit, 191) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            data.WriteUInt32(GetViewerDependentDisplayId(DisplayID, owner, receiver));
            for (int i = 0; i < 2; ++i)
                data.WriteUInt32(GetViewerDependentNpcFlags(NpcFlags[i], i, owner, receiver));

            data.WriteUInt32(StateSpellVisualID);
            data.WriteUInt32(StateAnimID);
            data.WriteUInt32(StateAnimKitID);
            data.WriteInt32(((List<uint>)StateWorldEffectIDs).Count);
            data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
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
            data.WriteInt32(ContentTuningID);
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
            data.WriteUInt32(NativeDisplayID);
            data.WriteFloat(NativeXDisplayScale);
            data.WriteUInt32(MountDisplayID);
            data.WriteInt32(CosmeticMountDisplayID);
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
                    data.WriteInt32(PowerCostModifier[i]);
                    data.WriteFloat(PowerCostMultiplier[i]);
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
                data.WriteFloat(ManaCostModifierModifier);
                data.WriteFloat(MaxHealthModifier);
            }
            data.WriteFloat(HoverHeight);
            data.WriteUInt32(MinItemLevelCutoff);
            data.WriteUInt32(MinItemLevel);
            data.WriteUInt32(MaxItemLevel);
            data.WriteInt32(AzeriteItemLevel);
            data.WriteInt32(WildBattlePetLevel);
            data.WriteUInt32(BattlePetCompanionNameTimestamp);
            data.WriteInt32(InteractSpellID);
            data.WriteInt32(ScaleDuration);
            data.WriteInt32(SpellOverrideNameID);
            data.WriteInt32(LooksLikeMountID);
            data.WriteInt32(LooksLikeCreatureID);
            data.WriteInt32(LookAtControllerID);
            data.WriteInt32(TaxiNodesID);
            data.WritePackedGuid(GuildGUID);
            data.WriteInt32(PassiveSpells.Size());
            data.WriteInt32(WorldEffects.Size());
            data.WriteInt32(ChannelObjects.Size());
            for (int i = 0; i < PassiveSpells.Size(); ++i)
                PassiveSpells[i].WriteCreate(data, owner, receiver);

            for (int i = 0; i < WorldEffects.Size(); ++i)
                data.WriteInt32(WorldEffects[i]);

            for (int i = 0; i < ChannelObjects.Size(); ++i)
                data.WritePackedGuid(ChannelObjects[i]);
        }

        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new UpdateMask(191, new uint[] { 0xFFFFEFFFu, 0xFC3FBFFFu, 0x0001EFFFu, 0xFFDFFFF9u, 0x001FC003u, 0x00000000u });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, fieldVisibilityFlags, owner, receiver);
        }

        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Owner))
                allowedMaskForTarget |= new UpdateMask(191, new uint[] { 0x00001000u, 0x03C04000u, 0xFFFE1000u, 0x00200006u, 0xFFE03FFCu, 0x7FFFFFFFu });
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.UnitAll))
                allowedMaskForTarget |= new UpdateMask(191, new uint[] { 0x00000000u, 0x00000000u, 0x00000000u, 0x00200000u, 0x00003FFCu, 0x00000000u });
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.Empath))
                allowedMaskForTarget |= new UpdateMask(191, new uint[] { 0x00000000u, 0x03C00000u, 0x00000000u, 0x00000000u, 0x00000000u, 0x000003FCu });
        }

        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags, Unit owner, Player receiver)
        {
            data.WriteBits(changesMask.GetBlocksMask(0), 6);
            for (uint i = 0; i < 6; ++i)
                if (changesMask.GetBlock(i) != 0)
                    data.WriteBits(changesMask.GetBlock(i), 32);

            if (changesMask[0])
            {
                if (changesMask[1])
                {
                    List<uint> list = StateWorldEffectIDs;
                    data.WriteBits(list.Count, 32);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        data.WriteUInt32(list[i]);
                    }
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    PassiveSpells.WriteUpdateMask(data);
                }
                if (changesMask[3])
                {
                    WorldEffects.WriteUpdateMask(data);
                }
                if (changesMask[4])
                {
                    ChannelObjects.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[2])
                {
                    for (int i = 0; i < PassiveSpells.Size(); ++i)
                    {
                        if (PassiveSpells.HasChanged(i))
                        {
                            PassiveSpells[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[3])
                {
                    for (int i = 0; i < WorldEffects.Size(); ++i)
                    {
                        if (WorldEffects.HasChanged(i))
                        {
                            data.WriteInt32(WorldEffects[i]);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < ChannelObjects.Size(); ++i)
                    {
                        if (ChannelObjects.HasChanged(i))
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
                    data.WritePackedGuid(Charm);
                }
                if (changesMask[11])
                {
                    data.WritePackedGuid(Summon);
                }
                if (changesMask[12])
                {
                    data.WritePackedGuid(Critter);
                }
                if (changesMask[13])
                {
                    data.WritePackedGuid(CharmedBy);
                }
                if (changesMask[14])
                {
                    data.WritePackedGuid(SummonedBy);
                }
                if (changesMask[15])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (changesMask[16])
                {
                    data.WritePackedGuid(DemonCreator);
                }
                if (changesMask[17])
                {
                    data.WritePackedGuid(LookAtControllerTarget);
                }
                if (changesMask[18])
                {
                    data.WritePackedGuid(Target);
                }
                if (changesMask[19])
                {
                    data.WritePackedGuid(BattlePetCompanionGUID);
                }
                if (changesMask[20])
                {
                    data.WriteUInt64(BattlePetDBID);
                }
                if (changesMask[21])
                {
                    ((UnitChannel)ChannelData).WriteUpdate(data, owner, receiver);
                }
                if (changesMask[22])
                {
                    data.WriteUInt32(SummonedByHomeRealm);
                }
                if (changesMask[23])
                {
                    data.WriteUInt8(Race);
                }
                if (changesMask[24])
                {
                    data.WriteUInt8(ClassId);
                }
                if (changesMask[25])
                {
                    data.WriteUInt8(PlayerClassId);
                }
                if (changesMask[26])
                {
                    data.WriteUInt8(Sex);
                }
                if (changesMask[27])
                {
                    data.WriteUInt8(DisplayPower);
                }
                if (changesMask[28])
                {
                    data.WriteUInt32(OverrideDisplayPowerID);
                }
                if (changesMask[29])
                {
                    data.WriteUInt64(Health);
                }
                if (changesMask[30])
                {
                    data.WriteUInt64(MaxHealth);
                }
                if (changesMask[31])
                {
                    data.WriteUInt32(Level);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteInt32(EffectiveLevel);
                }
                if (changesMask[34])
                {
                    data.WriteInt32(ContentTuningID);
                }
                if (changesMask[35])
                {
                    data.WriteInt32(ScalingLevelMin);
                }
                if (changesMask[36])
                {
                    data.WriteInt32(ScalingLevelMax);
                }
                if (changesMask[37])
                {
                    data.WriteInt32(ScalingLevelDelta);
                }
                if (changesMask[38])
                {
                    data.WriteInt32(ScalingFactionGroup);
                }
                if (changesMask[39])
                {
                    data.WriteInt32(ScalingHealthItemLevelCurveID);
                }
                if (changesMask[40])
                {
                    data.WriteInt32(ScalingDamageItemLevelCurveID);
                }
                if (changesMask[41])
                {
                    data.WriteUInt32(GetViewerDependentFactionTemplate(FactionTemplate, owner, receiver));
                }
                if (changesMask[42])
                {
                    data.WriteUInt32(GetViewerDependentFlags(Flags, owner, receiver));
                }
                if (changesMask[43])
                {
                    data.WriteUInt32(Flags2);
                }
                if (changesMask[44])
                {
                    data.WriteUInt32(Flags3);
                }
                if (changesMask[45])
                {
                    data.WriteUInt32(GetViewerDependentAuraState(AuraState, owner, receiver));
                }
                if (changesMask[46])
                {
                    data.WriteUInt32(RangedAttackRoundBaseTime);
                }
                if (changesMask[47])
                {
                    data.WriteFloat(BoundingRadius);
                }
                if (changesMask[48])
                {
                    data.WriteFloat(CombatReach);
                }
                if (changesMask[49])
                {
                    data.WriteFloat(DisplayScale);
                }
                if (changesMask[50])
                {
                    data.WriteUInt32(NativeDisplayID);
                }
                if (changesMask[51])
                {
                    data.WriteFloat(NativeXDisplayScale);
                }
                if (changesMask[52])
                {
                    data.WriteUInt32(MountDisplayID);
                }
                if (changesMask[53])
                {
                    data.WriteInt32(CosmeticMountDisplayID);
                }
                if (changesMask[54])
                {
                    data.WriteFloat(MinDamage);
                }
                if (changesMask[55])
                {
                    data.WriteFloat(MaxDamage);
                }
                if (changesMask[56])
                {
                    data.WriteFloat(MinOffHandDamage);
                }
                if (changesMask[57])
                {
                    data.WriteFloat(MaxOffHandDamage);
                }
                if (changesMask[58])
                {
                    data.WriteUInt8(StandState);
                }
                if (changesMask[59])
                {
                    data.WriteUInt8(PetTalentPoints);
                }
                if (changesMask[60])
                {
                    data.WriteUInt8(VisFlags);
                }
                if (changesMask[61])
                {
                    data.WriteUInt8(AnimTier);
                }
                if (changesMask[62])
                {
                    data.WriteUInt32(PetNumber);
                }
                if (changesMask[63])
                {
                    data.WriteUInt32(PetNameTimestamp);
                }
            }
            if (changesMask[64])
            {
                if (changesMask[65])
                {
                    data.WriteUInt32(PetExperience);
                }
                if (changesMask[66])
                {
                    data.WriteUInt32(PetNextLevelExperience);
                }
                if (changesMask[67])
                {
                    data.WriteFloat(ModCastingSpeed);
                }
                if (changesMask[68])
                {
                    data.WriteFloat(ModSpellHaste);
                }
                if (changesMask[69])
                {
                    data.WriteFloat(ModHaste);
                }
                if (changesMask[70])
                {
                    data.WriteFloat(ModRangedHaste);
                }
                if (changesMask[71])
                {
                    data.WriteFloat(ModHasteRegen);
                }
                if (changesMask[72])
                {
                    data.WriteFloat(ModTimeRate);
                }
                if (changesMask[73])
                {
                    data.WriteUInt32(CreatedBySpell);
                }
                if (changesMask[74])
                {
                    data.WriteInt32(EmoteState);
                }
                if (changesMask[75])
                {
                    data.WriteUInt32(BaseMana);
                }
                if (changesMask[76])
                {
                    data.WriteUInt32(BaseHealth);
                }
                if (changesMask[77])
                {
                    data.WriteUInt8(SheatheState);
                }
                if (changesMask[78])
                {
                    data.WriteUInt8((byte)GetViewerDependentPvpFlags(PvpFlags, owner, receiver));
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
                    data.WriteInt32(MainHandWeaponAttackPower);
                }
                if (changesMask[90])
                {
                    data.WriteInt32(OffHandWeaponAttackPower);
                }
                if (changesMask[91])
                {
                    data.WriteInt32(RangedWeaponAttackPower);
                }
                if (changesMask[92])
                {
                    data.WriteInt32(SetAttackSpeedAura);
                }
                if (changesMask[93])
                {
                    data.WriteFloat(Lifesteal);
                }
                if (changesMask[94])
                {
                    data.WriteFloat(MinRangedDamage);
                }
                if (changesMask[95])
                {
                    data.WriteFloat(MaxRangedDamage);
                }
            }
            if (changesMask[96])
            {
                if (changesMask[97])
                {
                    data.WriteFloat(ManaCostModifierModifier);
                }
                if (changesMask[98])
                {
                    data.WriteFloat(MaxHealthModifier);
                }
                if (changesMask[99])
                {
                    data.WriteFloat(HoverHeight);
                }
                if (changesMask[100])
                {
                    data.WriteUInt32(MinItemLevelCutoff);
                }
                if (changesMask[101])
                {
                    data.WriteUInt32(MinItemLevel);
                }
                if (changesMask[102])
                {
                    data.WriteUInt32(MaxItemLevel);
                }
                if (changesMask[103])
                {
                    data.WriteInt32(AzeriteItemLevel);
                }
                if (changesMask[104])
                {
                    data.WriteInt32(WildBattlePetLevel);
                }
                if (changesMask[105])
                {
                    data.WriteUInt32(BattlePetCompanionNameTimestamp);
                }
                if (changesMask[106])
                {
                    data.WriteInt32(InteractSpellID);
                }
                if (changesMask[107])
                {
                    data.WriteInt32(ScaleDuration);
                }
                if (changesMask[108])
                {
                    data.WriteInt32(SpellOverrideNameID);
                }
                if (changesMask[109])
                {
                    data.WriteInt32(LooksLikeMountID);
                }
                if (changesMask[110])
                {
                    data.WriteInt32(LooksLikeCreatureID);
                }
                if (changesMask[111])
                {
                    data.WriteInt32(LookAtControllerID);
                }
                if (changesMask[112])
                {
                    data.WriteInt32(TaxiNodesID);
                }
                if (changesMask[113])
                {
                    data.WritePackedGuid(GuildGUID);
                }
            }
            if (changesMask[114])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[115 + i])
                    {
                        data.WriteUInt32(GetViewerDependentNpcFlags(NpcFlags[i], i, owner, receiver));
                    }
                }
            }
            if (changesMask[117])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (changesMask[118 + i])
                    {
                        data.WriteInt32(Power[i]);
                    }
                    if (changesMask[124 + i])
                    {
                        data.WriteUInt32(MaxPower[i]);
                    }
                    if (changesMask[130 + i])
                    {
                        data.WriteFloat(PowerRegenFlatModifier[i]);
                    }
                    if (changesMask[136 + i])
                    {
                        data.WriteFloat(PowerRegenInterruptedFlatModifier[i]);
                    }
                }
            }
            if (changesMask[142])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[143 + i])
                    {
                        VirtualItems[i].WriteUpdate(data, owner, receiver);
                    }
                }
            }
            if (changesMask[146])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (changesMask[147 + i])
                    {
                        data.WriteUInt32(AttackRoundBaseTime[i]);
                    }
                }
            }
            if (changesMask[149])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[150 + i])
                    {
                        data.WriteInt32(Stats[i]);
                    }
                    if (changesMask[154 + i])
                    {
                        data.WriteInt32(StatPosBuff[i]);
                    }
                    if (changesMask[158 + i])
                    {
                        data.WriteInt32(StatNegBuff[i]);
                    }
                }
            }
            if (changesMask[162])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (changesMask[163 + i])
                    {
                        data.WriteInt32(Resistances[i]);
                    }
                    if (changesMask[170 + i])
                    {
                        data.WriteInt32(BonusResistanceMods[i]);
                    }
                    if (changesMask[177 + i])
                    {
                        data.WriteInt32(PowerCostModifier[i]);
                    }
                    if (changesMask[184 + i])
                    {
                        data.WriteFloat(PowerCostMultiplier[i]);
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
            ClearChangesMask(ManaCostModifierModifier);
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
            ClearChangesMask(SpellOverrideNameID);
            ClearChangesMask(LooksLikeMountID);
            ClearChangesMask(LooksLikeCreatureID);
            ClearChangesMask(LookAtControllerID);
            ClearChangesMask(TaxiNodesID);
            ClearChangesMask(GuildGUID);
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
            ClearChangesMask(PowerCostModifier);
            ClearChangesMask(PowerCostMultiplier);
            _changesMask.ResetAll();
        }

        uint GetViewerDependentDisplayId(uint displayId, Unit unit, Player receiver)
        {
            if (unit.IsCreature())
            {
                CreatureTemplate cinfo = unit.ToCreature().GetCreatureTemplate();

                // this also applies for transform auras
                SpellInfo transform = Global.SpellMgr.GetSpellInfo(unit.GetTransForm());
                if (transform != null)
                {
                    foreach (SpellEffectInfo effect in transform.GetEffectsForDifficulty(unit.GetMap().GetDifficultyID()))
                    {
                        if (effect != null && effect.IsAura(AuraType.Transform))
                        {
                            CreatureTemplate transformInfo = Global.ObjectMgr.GetCreatureTemplate((uint)effect.MiscValue);
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

    public class QuestLog : BaseUpdateData<Player>
    {
        public UpdateField<uint> QuestID = new UpdateField<uint>(0, 1);
        public UpdateField<uint> StateFlags = new UpdateField<uint>(0, 2);
        public UpdateField<uint> EndTime = new UpdateField<uint>(0, 3);
        public UpdateField<uint> AcceptTime = new UpdateField<uint>(0, 4);
        public UpdateField<uint> Field_10 = new UpdateField<uint>(0, 5);
        public UpdateFieldArray<ushort> ObjectiveProgress = new UpdateFieldArray<ushort>(24, 6, 7);

        public QuestLog() : base(31) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(QuestID);
            data.WriteUInt32(StateFlags);
            data.WriteUInt32(EndTime);
            data.WriteUInt32(AcceptTime);
            data.WriteUInt32(Field_10);
            for (int i = 0; i < 24; ++i)
            {
                data.WriteUInt16(ObjectiveProgress[i]);
            }
        }
        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlocksMask(0), 1);
            if (_changesMask.GetBlock(0) != 0)
                data.WriteBits(_changesMask.GetBlock(0), 32);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(QuestID);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt32(StateFlags);
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(EndTime);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(AcceptTime);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(Field_10);
                }
            }
            if (_changesMask[6])
            {
                for (int i = 0; i < 24; ++i)
                {
                    if (_changesMask[7 + i])
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
            ClearChangesMask(Field_10);
            ClearChangesMask(ObjectiveProgress);
            _changesMask.ResetAll();
        }
    }

    public class ArenaCooldown : BaseUpdateData<Player>
    {
        public UpdateField<int> SpellID = new UpdateField<int>(0, 1);
        public UpdateField<int> Charges = new UpdateField<int>(0, 2);
        public UpdateField<uint> Flags = new UpdateField<uint>(0, 3);
        public UpdateField<uint> StartTime = new UpdateField<uint>(0, 4);
        public UpdateField<uint> EndTime = new UpdateField<uint>(0, 5);
        public UpdateField<uint> NextChargeTime = new UpdateField<uint>(0, 6);
        public UpdateField<byte> MaxCharges = new UpdateField<byte>(0, 7);

        public ArenaCooldown() : base(8) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(SpellID);
            data.WriteInt32(Charges);
            data.WriteUInt32(Flags);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(EndTime);
            data.WriteUInt32(NextChargeTime);
            data.WriteUInt8(MaxCharges);
        }
        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 8);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteInt32(SpellID);
                }
                if (_changesMask[2])
                {
                    data.WriteInt32(Charges);
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(Flags);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(StartTime);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(EndTime);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt32(NextChargeTime);
                }
                if (_changesMask[7])
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

    public class PlayerData : BaseUpdateData<Player>
    {
        public UpdateField<bool> HasQuestSession = new UpdateField<bool>(0, 1);
        public UpdateField<bool> HasLevelLink = new UpdateField<bool>(0, 2);
        public DynamicUpdateField<QuestLog> QuestSessionQuestLog = new DynamicUpdateField<QuestLog>(0, 3);
        public DynamicUpdateField<ArenaCooldown> ArenaCooldowns = new DynamicUpdateField<ArenaCooldown>(0, 4);
        public UpdateField<ObjectGuid> DuelArbiter = new UpdateField<ObjectGuid>(0, 5);
        public UpdateField<ObjectGuid> WowAccount = new UpdateField<ObjectGuid>(0, 6);
        public UpdateField<ObjectGuid> LootTargetGUID = new UpdateField<ObjectGuid>(0, 7);
        public UpdateField<uint> PlayerFlags = new UpdateField<uint>(0, 8);
        public UpdateField<uint> PlayerFlagsEx = new UpdateField<uint>(0, 9);
        public UpdateField<uint> GuildRankID = new UpdateField<uint>(0, 10);
        public UpdateField<uint> GuildDeleteDate = new UpdateField<uint>(0, 11);
        public UpdateField<uint> GuildLevel = new UpdateField<uint>(0, 12);
        public UpdateField<byte> SkinID = new UpdateField<byte>(0, 13);
        public UpdateField<byte> FaceID = new UpdateField<byte>(0, 14);
        public UpdateField<byte> HairStyleID = new UpdateField<byte>(0, 15);
        public UpdateField<byte> HairColorID = new UpdateField<byte>(0, 16);
        public UpdateField<byte> FacialHairStyleID = new UpdateField<byte>(0, 17);
        public UpdateField<byte> PartyType = new UpdateField<byte>(0, 18);
        public UpdateField<byte> NativeSex = new UpdateField<byte>(0, 19);
        public UpdateField<byte> Inebriation = new UpdateField<byte>(0, 20);
        public UpdateField<byte> PvpTitle = new UpdateField<byte>(0, 21);
        public UpdateField<byte> ArenaFaction = new UpdateField<byte>(0, 22);
        public UpdateField<uint> DuelTeam = new UpdateField<uint>(0, 23);
        public UpdateField<int> GuildTimeStamp = new UpdateField<int>(0, 24);
        public UpdateField<uint> PlayerTitle = new UpdateField<uint>(0, 25);
        public UpdateField<int> FakeInebriation = new UpdateField<int>(0, 26);
        public UpdateField<uint> VirtualPlayerRealm = new UpdateField<uint>(0, 27);
        public UpdateField<uint> CurrentSpecID = new UpdateField<uint>(0, 28);
        public UpdateField<int> TaxiMountAnimKitID = new UpdateField<int>(0, 29);
        public UpdateField<byte> CurrentBattlePetBreedQuality = new UpdateField<byte>(0, 30);
        public UpdateField<uint> HonorLevel = new UpdateField<uint>(0, 31);
        public UpdateField<int> Field_B0 = new UpdateField<int>(32, 33);
        public UpdateField<int> Field_B4 = new UpdateField<int>(32, 34);
        public UpdateField<ObjectGuid> Field_F8 = new UpdateField<ObjectGuid>(32, 35);
        public UpdateField<int> Field_108 = new UpdateField<int>(32, 36);
        public UpdateFieldArray<byte> CustomDisplayOption = new UpdateFieldArray<byte>(3, 37, 38);
        public UpdateFieldArray<QuestLog> QuestLog = new UpdateFieldArray<QuestLog>(125, 41, 42);
        public UpdateFieldArray<VisibleItem> VisibleItems = new UpdateFieldArray<VisibleItem>(19, 167, 168);
        public UpdateFieldArray<float> AvgItemLevel = new UpdateFieldArray<float>(4, 187, 188);

        public PlayerData() : base(0, TypeId.Player, 192) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            data.WritePackedGuid(DuelArbiter);
            data.WritePackedGuid(WowAccount);
            data.WritePackedGuid(LootTargetGUID);
            data.WriteUInt32(PlayerFlags);
            data.WriteUInt32(PlayerFlagsEx);
            data.WriteUInt32(GuildRankID);
            data.WriteUInt32(GuildDeleteDate);
            data.WriteUInt32(GuildLevel);
            data.WriteUInt8(SkinID);
            data.WriteUInt8(FaceID);
            data.WriteUInt8(HairStyleID);
            data.WriteUInt8(HairColorID);
            for (int i = 0; i < 3; ++i)
            {
                data.WriteUInt8(CustomDisplayOption[i]);
            }
            data.WriteUInt8(FacialHairStyleID);
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
            for (int i = 0; i < 4; ++i)
            {
                data.WriteFloat(AvgItemLevel[i]);
            }
            data.WriteUInt8(CurrentBattlePetBreedQuality);
            data.WriteUInt32(HonorLevel);
            data.WriteInt32(ArenaCooldowns.Size());
            data.WriteInt32(Field_B0);
            data.WriteInt32(Field_B4);
            data.WritePackedGuid(Field_F8);
            data.WriteInt32(Field_108);
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
                data.WriteBit((bool)HasQuestSession);
            }
            data.WriteBit((bool)HasLevelLink);
            data.FlushBits();
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            UpdateMask allowedMaskForTarget = new UpdateMask(192, new[] { 0xFFFFFFF5u, 0x000001FFu, 0x00000000u, 0x00000000u, 0x00000000u, 0xFFFFFF80u });
            AppendAllowedFieldsMaskForFlag(allowedMaskForTarget, fieldVisibilityFlags);
            WriteUpdate(data, _changesMask & allowedMaskForTarget, fieldVisibilityFlags, owner, receiver);
        }
        public void AppendAllowedFieldsMaskForFlag(UpdateMask allowedMaskForTarget, UpdateFieldFlag fieldVisibilityFlags)
        {
            if (fieldVisibilityFlags.HasFlag(UpdateFieldFlag.PartyMember))
                allowedMaskForTarget |= new UpdateMask(192, new[] { 0x0000000Au, 0xFFFFFE00u, 0xFFFFFFFFu, 0xFFFFFFFFu, 0xFFFFFFFFu, 0x0000007Fu });
        }
        public void WriteUpdate(WorldPacket data, UpdateMask changesMask, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
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
                    QuestSessionQuestLog.WriteUpdateMask(data);
                }
                if (changesMask[4])
                {
                    ArenaCooldowns.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (changesMask[0])
            {
                if (changesMask[3])
                {
                    for (int i = 0; i < QuestSessionQuestLog.Size(); ++i)
                    {
                        if (QuestSessionQuestLog.HasChanged(i))
                        {
                            if (noQuestLogChangesMask)
                                QuestSessionQuestLog[i].WriteCreate(data, owner, receiver);
                            else
                                QuestSessionQuestLog[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[4])
                {
                    for (int i = 0; i < ArenaCooldowns.Size(); ++i)
                    {
                        if (ArenaCooldowns.HasChanged(i))
                        {
                            ArenaCooldowns[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (changesMask[5])
                {
                    data.WritePackedGuid(DuelArbiter);
                }
                if (changesMask[6])
                {
                    data.WritePackedGuid(WowAccount);
                }
                if (changesMask[7])
                {
                    data.WritePackedGuid(LootTargetGUID);
                }
                if (changesMask[8])
                {
                    data.WriteUInt32(PlayerFlags);
                }
                if (changesMask[9])
                {
                    data.WriteUInt32(PlayerFlagsEx);
                }
                if (changesMask[10])
                {
                    data.WriteUInt32(GuildRankID);
                }
                if (changesMask[11])
                {
                    data.WriteUInt32(GuildDeleteDate);
                }
                if (changesMask[12])
                {
                    data.WriteUInt32(GuildLevel);
                }
                if (changesMask[13])
                {
                    data.WriteUInt8(SkinID);
                }
                if (changesMask[14])
                {
                    data.WriteUInt8(FaceID);
                }
                if (changesMask[15])
                {
                    data.WriteUInt8(HairStyleID);
                }
                if (changesMask[16])
                {
                    data.WriteUInt8(HairColorID);
                }
                if (changesMask[17])
                {
                    data.WriteUInt8(FacialHairStyleID);
                }
                if (changesMask[18])
                {
                    data.WriteUInt8(PartyType);
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
                    data.WriteInt32(TaxiMountAnimKitID);
                }
                if (changesMask[30])
                {
                    data.WriteUInt8(CurrentBattlePetBreedQuality);
                }
                if (changesMask[31])
                {
                    data.WriteUInt32(HonorLevel);
                }
            }
            if (changesMask[32])
            {
                if (changesMask[33])
                {
                    data.WriteInt32(Field_B0);
                }
                if (changesMask[34])
                {
                    data.WriteInt32(Field_B4);
                }
                if (changesMask[35])
                {
                    data.WritePackedGuid(Field_F8);
                }
                if (changesMask[36])
                {
                    data.WriteInt32(Field_108);
                }
            }
            if (changesMask[37])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (changesMask[38 + i])
                    {
                        data.WriteUInt8(CustomDisplayOption[i]);
                    }
                }
            }
            if (changesMask[41])
            {
                for (int i = 0; i < 125; ++i)
                {
                    if (changesMask[42 + i])
                    {
                        if (noQuestLogChangesMask)
                            QuestLog[i].WriteCreate(data, owner, receiver);
                        else
                            QuestLog[i].WriteUpdate(data, owner, receiver);
                    }
                }
            }
            if (changesMask[167])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (changesMask[168 + i])
                    {
                        VisibleItems[i].WriteUpdate(data, owner, receiver);
                    }
                }
            }
            if (changesMask[187])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (changesMask[188 + i])
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
            ClearChangesMask(SkinID);
            ClearChangesMask(FaceID);
            ClearChangesMask(HairStyleID);
            ClearChangesMask(HairColorID);
            ClearChangesMask(FacialHairStyleID);
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
            ClearChangesMask(Field_F8);
            ClearChangesMask(Field_108);
            ClearChangesMask(CustomDisplayOption);
            ClearChangesMask(QuestLog);
            ClearChangesMask(VisibleItems);
            ClearChangesMask(AvgItemLevel);
            _changesMask.ResetAll();
        }

        bool IsQuestLogChangesMaskSkipped() { return false; } // bandwidth savings aren't worth the cpu time
    }

    public class SkillInfo : BaseUpdateData<Player>
    {
        public UpdateFieldArray<ushort> SkillLineID = new UpdateFieldArray<ushort>(256, 0, 1);
        public UpdateFieldArray<ushort> SkillStep = new UpdateFieldArray<ushort>(256, 0, 257);
        public UpdateFieldArray<ushort> SkillRank = new UpdateFieldArray<ushort>(256, 0, 513);
        public UpdateFieldArray<ushort> SkillStartingRank = new UpdateFieldArray<ushort>(256, 0, 769);
        public UpdateFieldArray<ushort> SkillMaxRank = new UpdateFieldArray<ushort>(256, 0, 1025);
        public UpdateFieldArray<ushort> SkillTempBonus = new UpdateFieldArray<ushort>(256, 0, 1281);
        public UpdateFieldArray<ushort> SkillPermBonus = new UpdateFieldArray<ushort>(256, 0, 1537);

        public SkillInfo() : base(1793) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
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
        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            for (uint i = 0; i < 1; ++i)
                data.WriteUInt32(_changesMask.GetBlocksMask(i));
            data.WriteBits(_changesMask.GetBlocksMask(1), 25);
            for (uint i = 0; i < 57; ++i)
                if (_changesMask.GetBlock(i) != 0)
                    data.WriteBits(_changesMask.GetBlock(i), 32);

            data.FlushBits();
            if (_changesMask[0])
            {
                for (int i = 0; i < 256; ++i)
                {
                    if (_changesMask[1 + i])
                    {
                        data.WriteUInt16(SkillLineID[i]);
                    }
                    if (_changesMask[257 + i])
                    {
                        data.WriteUInt16(SkillStep[i]);
                    }
                    if (_changesMask[513 + i])
                    {
                        data.WriteUInt16(SkillRank[i]);
                    }
                    if (_changesMask[769 + i])
                    {
                        data.WriteUInt16(SkillStartingRank[i]);
                    }
                    if (_changesMask[1025 + i])
                    {
                        data.WriteUInt16(SkillMaxRank[i]);
                    }
                    if (_changesMask[1281 + i])
                    {
                        data.WriteUInt16(SkillTempBonus[i]);
                    }
                    if (_changesMask[1537 + i])
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
        public UpdateField<uint> Threshold = new UpdateField<uint>(0, 1);
        public UpdateField<byte> StateID = new UpdateField<byte>(0, 2);

        public RestInfo() : base(3) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(Threshold);
            data.WriteUInt8(StateID);
        }
        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(Threshold);
                }
                if (_changesMask[2])
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
        public UpdateField<uint> Field_0 = new UpdateField<uint>(0, 1);
        public UpdateField<uint> Field_4 = new UpdateField<uint>(0, 2);
        public UpdateField<uint> Field_8 = new UpdateField<uint>(0, 3);
        public UpdateField<uint> Field_C = new UpdateField<uint>(0, 4);
        public UpdateField<uint> Rating = new UpdateField<uint>(0, 5);
        public UpdateField<uint> Field_14 = new UpdateField<uint>(0, 6);
        public UpdateField<uint> Field_18 = new UpdateField<uint>(0, 7);
        public UpdateField<uint> PvpTierID = new UpdateField<uint>(0, 8);
        public UpdateField<uint> Field_20 = new UpdateField<uint>(0, 9);

        public PVPInfo() : base(10) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteUInt32(Field_0);
            data.WriteUInt32(Field_4);
            data.WriteUInt32(Field_8);
            data.WriteUInt32(Field_C);
            data.WriteUInt32(Rating);
            data.WriteUInt32(Field_14);
            data.WriteUInt32(Field_18);
            data.WriteUInt32(PvpTierID);
            data.WriteBits((uint)Field_20, 1);
            data.FlushBits();
        }
        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 10);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteUInt32(Field_0);
                }
                if (_changesMask[2])
                {
                    data.WriteUInt32(Field_4);
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(Field_8);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(Field_C);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(Rating);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt32(Field_14);
                }
                if (_changesMask[7])
                {
                    data.WriteUInt32(Field_18);
                }
                if (_changesMask[8])
                {
                    data.WriteUInt32(PvpTierID);
                }
                if (_changesMask[9])
                {
                    data.WriteBits(Field_20, 1);
                }
            }
            data.FlushBits();
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Field_0);
            ClearChangesMask(Field_4);
            ClearChangesMask(Field_8);
            ClearChangesMask(Field_C);
            ClearChangesMask(Rating);
            ClearChangesMask(Field_14);
            ClearChangesMask(Field_18);
            ClearChangesMask(PvpTierID);
            ClearChangesMask(Field_20);
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
        public void WriteUpdate(WorldPacket data, Player owner, Player receiver)
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
        public void WriteUpdate(WorldPacket data, Player owner, Player receiver)
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
        public void WriteUpdate(WorldPacket data, Player owner, Player receiver)
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
        public void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt16(ResearchProjectID);
        }
    }

    public class ReplayedQuest : BaseUpdateData<Player>
    {
        public UpdateField<int> QuestID = new UpdateField<int>(0, 1);
        public UpdateField<uint> ReplayTime = new UpdateField<uint>(0, 2);

        public ReplayedQuest() : base(3) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteInt32(QuestID);
            data.WriteUInt32(ReplayTime);
        }

        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 3);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteInt32(QuestID);
                }
                if (_changesMask[2])
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
        public UpdateField<ObjectGuid> Owner = new UpdateField<ObjectGuid>(0, 1);
        public UpdateFieldArray<ulong> QuestCompleted = new UpdateFieldArray<ulong>(875, 2, 3);

        public QuestSession() : base(878) { }

        public override void WriteCreate(WorldPacket data, Player owner, Player receiver)
        {
            data.WritePackedGuid(Owner);
            for (int i = 0; i < 875; ++i)
            {
                data.WriteUInt64(QuestCompleted[i]);
            }
        }

        public override void WriteUpdate(WorldPacket data, Player owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlocksMask(0), 28);
            for (uint i = 0; i < 28; ++i)
                if (_changesMask.GetBlock(i) != 0)
                    data.WriteBits(_changesMask.GetBlock(i), 32);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WritePackedGuid(Owner);
                }
            }
            if (_changesMask[2])
            {
                for (int i = 0; i < 875; ++i)
                {
                    if (_changesMask[3 + i])
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
        public UpdateField<bool> BackpackAutoSortDisabled = new UpdateField<bool>(0, 1);
        public UpdateField<bool> BankAutoSortDisabled = new UpdateField<bool>(0, 2);
        public UpdateField<bool> SortBagsRightToLeft = new UpdateField<bool>(0, 3);
        public UpdateField<bool> InsertItemsLeftToRight = new UpdateField<bool>(0, 4);
        public UpdateFieldArray<DynamicUpdateField<Research>> Research = new UpdateFieldArray<DynamicUpdateField<Research>>(1, 22, 23);//-1-1
        public DynamicUpdateField<ulong> KnownTitles = new DynamicUpdateField<ulong>(0, 5);
        public DynamicUpdateField<ushort> ResearchSites = new DynamicUpdateField<ushort>(0, 6);
        public DynamicUpdateField<uint> ResearchSiteProgress = new DynamicUpdateField<uint>(0, 7);
        public DynamicUpdateField<uint> DailyQuestsCompleted = new DynamicUpdateField<uint>(0, 8);
        public DynamicUpdateField<int> AvailableQuestLineXQuestIDs = new DynamicUpdateField<int>(0, 9);
        public DynamicUpdateField<uint> Heirlooms = new DynamicUpdateField<uint>(0, 10);
        public DynamicUpdateField<uint> HeirloomFlags = new DynamicUpdateField<uint>(0, 11);
        public DynamicUpdateField<uint> Toys = new DynamicUpdateField<uint>(0, 12);
        public DynamicUpdateField<uint> ToyFlags = new DynamicUpdateField<uint>(0, 13);
        public DynamicUpdateField<uint> Transmog = new DynamicUpdateField<uint>(0, 14);
        public DynamicUpdateField<uint> ConditionalTransmog = new DynamicUpdateField<uint>(0, 15);
        public DynamicUpdateField<uint> SelfResSpells = new DynamicUpdateField<uint>(0, 16);
        public DynamicUpdateField<SpellPctModByLabel> SpellPctModByLabel = new DynamicUpdateField<SpellPctModByLabel>(0, 18);
        public DynamicUpdateField<SpellFlatModByLabel> SpellFlatModByLabel = new DynamicUpdateField<SpellFlatModByLabel>(0, 19);
        public DynamicUpdateField<ReplayedQuest> ReplayedQuests = new DynamicUpdateField<ReplayedQuest>(0, 20);
        public DynamicUpdateField<int> DisabledSpells = new DynamicUpdateField<int>(0, 21);
        public DynamicUpdateField<CharacterRestriction> CharacterRestrictions = new DynamicUpdateField<CharacterRestriction>(0, 17);
        public UpdateField<ObjectGuid> FarsightObject = new UpdateField<ObjectGuid>(0, 24);
        public UpdateField<ObjectGuid> SummonedBattlePetGUID = new UpdateField<ObjectGuid>(0, 25);
        public UpdateField<ulong> Coinage = new UpdateField<ulong>(0, 26);
        public UpdateField<uint> XP = new UpdateField<uint>(0, 27);
        public UpdateField<uint> NextLevelXP = new UpdateField<uint>(0, 28);
        public UpdateField<int> TrialXP = new UpdateField<int>(0, 29);
        public UpdateField<SkillInfo> Skill = new UpdateField<SkillInfo>(0, 30);
        public UpdateField<uint> CharacterPoints = new UpdateField<uint>(0, 31);
        public UpdateField<uint> MaxTalentTiers = new UpdateField<uint>(0, 32);
        public UpdateField<uint> TrackCreatureMask = new UpdateField<uint>(0, 33);
        public UpdateField<float> MainhandExpertise = new UpdateField<float>(34, 35);
        public UpdateField<float> OffhandExpertise = new UpdateField<float>(34, 36);
        public UpdateField<float> RangedExpertise = new UpdateField<float>(34, 37);
        public UpdateField<float> CombatRatingExpertise = new UpdateField<float>(34, 38);
        public UpdateField<float> BlockPercentage = new UpdateField<float>(34, 39);
        public UpdateField<float> DodgePercentage = new UpdateField<float>(34, 40);
        public UpdateField<float> DodgePercentageFromAttribute = new UpdateField<float>(34, 41);
        public UpdateField<float> ParryPercentage = new UpdateField<float>(34, 42);
        public UpdateField<float> ParryPercentageFromAttribute = new UpdateField<float>(34, 43);
        public UpdateField<float> CritPercentage = new UpdateField<float>(34, 44);
        public UpdateField<float> RangedCritPercentage = new UpdateField<float>(34, 45);
        public UpdateField<float> OffhandCritPercentage = new UpdateField<float>(34, 46);
        public UpdateField<float> SpellCritPercentage = new UpdateField<float>(34, 47);
        public UpdateField<uint> ShieldBlock = new UpdateField<uint>(34, 48);
        public UpdateField<float> ShieldBlockCritPercentage = new UpdateField<float>(34, 49);
        public UpdateField<float> Mastery = new UpdateField<float>(34, 50);
        public UpdateField<float> Speed = new UpdateField<float>(34, 51);
        public UpdateField<float> Avoidance = new UpdateField<float>(34, 52);
        public UpdateField<float> Sturdiness = new UpdateField<float>(34, 53);
        public UpdateField<int> Versatility = new UpdateField<int>(34, 54);
        public UpdateField<float> VersatilityBonus = new UpdateField<float>(34, 55);
        public UpdateField<float> PvpPowerDamage = new UpdateField<float>(34, 56);
        public UpdateField<float> PvpPowerHealing = new UpdateField<float>(34, 57);
        public UpdateField<int> ModHealingDonePos = new UpdateField<int>(34, 58);
        public UpdateField<float> ModHealingPercent = new UpdateField<float>(34, 59);
        public UpdateField<float> ModHealingDonePercent = new UpdateField<float>(34, 60);
        public UpdateField<float> ModPeriodicHealingDonePercent = new UpdateField<float>(34, 61);
        public UpdateField<float> ModSpellPowerPercent = new UpdateField<float>(34, 62);
        public UpdateField<float> ModResiliencePercent = new UpdateField<float>(34, 63);
        public UpdateField<float> OverrideSpellPowerByAPPercent = new UpdateField<float>(34, 64);
        public UpdateField<float> OverrideAPBySpellPowerPercent = new UpdateField<float>(34, 65);
        public UpdateField<int> ModTargetResistance = new UpdateField<int>(66, 67);
        public UpdateField<int> ModTargetPhysicalResistance = new UpdateField<int>(66, 68);
        public UpdateField<int> LocalFlags = new UpdateField<int>(66, 69);
        public UpdateField<byte> GrantableLevels = new UpdateField<byte>(66, 70);
        public UpdateField<byte> MultiActionBars = new UpdateField<byte>(66, 71);
        public UpdateField<byte> LifetimeMaxRank = new UpdateField<byte>(66, 72);
        public UpdateField<byte> NumRespecs = new UpdateField<byte>(66, 73);
        public UpdateField<uint> PvpMedals = new UpdateField<uint>(66, 74);
        public UpdateField<ushort> TodayHonorableKills = new UpdateField<ushort>(66, 75);
        public UpdateField<ushort> YesterdayHonorableKills = new UpdateField<ushort>(66, 76);
        public UpdateField<uint> LifetimeHonorableKills = new UpdateField<uint>(66, 77);
        public UpdateField<uint> WatchedFactionIndex = new UpdateField<uint>(66, 78);
        public UpdateField<int> MaxLevel = new UpdateField<int>(66, 79);
        public UpdateField<int> ScalingPlayerLevelDelta = new UpdateField<int>(66, 80);
        public UpdateField<int> MaxCreatureScalingLevel = new UpdateField<int>(66, 81);
        public UpdateField<uint> PetSpellPower = new UpdateField<uint>(66, 82);
        public UpdateField<float> UiHitModifier = new UpdateField<float>(66, 83);
        public UpdateField<float> UiSpellHitModifier = new UpdateField<float>(66, 84);
        public UpdateField<int> HomeRealmTimeOffset = new UpdateField<int>(66, 85);
        public UpdateField<float> ModPetHaste = new UpdateField<float>(66, 86);
        public UpdateField<byte> LocalRegenFlags = new UpdateField<byte>(66, 87);
        public UpdateField<byte> AuraVision = new UpdateField<byte>(66, 88);
        public UpdateField<byte> NumBackpackSlots = new UpdateField<byte>(66, 89);
        public UpdateField<uint> OverrideSpellsID = new UpdateField<uint>(66, 90);
        public UpdateField<int> LfgBonusFactionID = new UpdateField<int>(66, 91);
        public UpdateField<ushort> LootSpecID = new UpdateField<ushort>(66, 92);
        public UpdateField<uint> OverrideZonePVPType = new UpdateField<uint>(66, 93);
        public UpdateField<uint> Honor = new UpdateField<uint>(66, 94);
        public UpdateField<uint> HonorNextLevel = new UpdateField<uint>(66, 95);
        public UpdateField<int> PvpRewardAchieved = new UpdateField<int>(66, 96);
        public UpdateField<int> PvpTierMaxFromWins = new UpdateField<int>(66, 97);
        public UpdateField<int> PvpLastWeeksRewardAchieved = new UpdateField<int>(98, 99);
        public UpdateField<int> PvpLastWeeksTierMaxFromWins = new UpdateField<int>(98, 100);
        public UpdateField<int> PvpLastWeeksRewardClaimed = new UpdateField<int>(98, 101);
        public UpdateField<byte> NumBankSlots = new UpdateField<byte>(98, 102);
        public UpdateField<Optional<QuestSession>> QuestSession = new UpdateField<Optional<QuestSession>>(98, 103);
        public UpdateFieldArray<ObjectGuid> InvSlots = new UpdateFieldArray<ObjectGuid>(195, 104, 105);
        public UpdateFieldArray<uint> TrackResourceMask = new UpdateFieldArray<uint>(2, 300, 301);
        public UpdateFieldArray<ulong> ExploredZones = new UpdateFieldArray<ulong>(192, 303, 304);
        public UpdateFieldArray<RestInfo> RestInfo = new UpdateFieldArray<RestInfo>(2, 496, 497);
        public UpdateFieldArray<int> ModDamageDonePos = new UpdateFieldArray<int>(7, 499, 500);
        public UpdateFieldArray<int> ModDamageDoneNeg = new UpdateFieldArray<int>(7, 499, 507);
        public UpdateFieldArray<float> ModDamageDonePercent = new UpdateFieldArray<float>(7, 499, 514);
        public UpdateFieldArray<float> WeaponDmgMultipliers = new UpdateFieldArray<float>(3, 521, 522);
        public UpdateFieldArray<float> WeaponAtkSpeedMultipliers = new UpdateFieldArray<float>(3, 521, 525);
        public UpdateFieldArray<uint> BuybackPrice = new UpdateFieldArray<uint>(12, 528, 529);
        public UpdateFieldArray<uint> BuybackTimestamp = new UpdateFieldArray<uint>(12, 528, 541);
        public UpdateFieldArray<uint> CombatRatings = new UpdateFieldArray<uint>(32, 553, 554);
        public UpdateFieldArray<PVPInfo> PvpInfo = new UpdateFieldArray<PVPInfo>(6, 586, 587);
        public UpdateFieldArray<uint> NoReagentCostMask = new UpdateFieldArray<uint>(4, 593, 594);
        public UpdateFieldArray<uint> ProfessionSkillLine = new UpdateFieldArray<uint>(2, 598, 599);
        public UpdateFieldArray<uint> BagSlotFlags = new UpdateFieldArray<uint>(4, 601, 602);
        public UpdateFieldArray<uint> BankBagSlotFlags = new UpdateFieldArray<uint>(7, 606, 607);
        public UpdateFieldArray<ulong> QuestCompleted = new UpdateFieldArray<ulong>(875, 614, 615);



        public ActivePlayerData() : base(0, TypeId.ActivePlayer, 1487) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            for (int i = 0; i < 195; ++i)
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
            for (int i = 0; i < 192; ++i)
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
            data.WriteInt32(LocalFlags);
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
            data.WriteInt32(PvpRewardAchieved);
            data.WriteInt32(PvpTierMaxFromWins);
            data.WriteInt32(PvpLastWeeksRewardAchieved);
            data.WriteInt32(PvpLastWeeksTierMaxFromWins);
            data.WriteInt32(PvpLastWeeksRewardClaimed);
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
            data.WriteInt32(ReplayedQuests.Size());
            data.WriteInt32(DisabledSpells.Size());
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
            for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
            {
                SpellPctModByLabel[i].WriteCreate(data, owner, receiver);
            }
            for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
            {
                SpellFlatModByLabel[i].WriteCreate(data, owner, receiver);
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
            data.WriteBit((bool)BackpackAutoSortDisabled);
            data.WriteBit((bool)BankAutoSortDisabled);
            data.WriteBit((bool)SortBagsRightToLeft);
            data.WriteBit((bool)InsertItemsLeftToRight);
            data.WriteBit(QuestSession.GetValue().HasValue);
            for (int i = 0; i < CharacterRestrictions.Size(); ++i)
            {
                CharacterRestrictions[i].WriteCreate(data, owner, receiver);
            }
            if (QuestSession.GetValue().HasValue)
            {
                QuestSession.GetValue().Value.WriteCreate(data, owner, receiver);
            }
            data.FlushBits();
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Player owner, Player receiver)
        {
            for (uint i = 0; i < 1; ++i)
                data.WriteUInt32(_changesMask.GetBlocksMask(i));
            data.WriteBits(_changesMask.GetBlocksMask(1), 15);
            for (uint i = 0; i < 47; ++i)
                if (_changesMask.GetBlock(i) != 0)
                    data.WriteBits(_changesMask.GetBlock(i), 32);

            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    data.WriteBit(BackpackAutoSortDisabled);
                }
                if (_changesMask[2])
                {
                    data.WriteBit(BankAutoSortDisabled);
                }
                if (_changesMask[3])
                {
                    data.WriteBit(SortBagsRightToLeft);
                }
                if (_changesMask[4])
                {
                    data.WriteBit(InsertItemsLeftToRight);
                }
                if (_changesMask[5])
                {
                    KnownTitles.WriteUpdateMask(data);
                }
                if (_changesMask[6])
                {
                    ResearchSites.WriteUpdateMask(data);
                }
                if (_changesMask[7])
                {
                    ResearchSiteProgress.WriteUpdateMask(data);
                }
                if (_changesMask[8])
                {
                    DailyQuestsCompleted.WriteUpdateMask(data);
                }
                if (_changesMask[9])
                {
                    AvailableQuestLineXQuestIDs.WriteUpdateMask(data);
                }
                if (_changesMask[10])
                {
                    Heirlooms.WriteUpdateMask(data);
                }
                if (_changesMask[11])
                {
                    HeirloomFlags.WriteUpdateMask(data);
                }
                if (_changesMask[12])
                {
                    Toys.WriteUpdateMask(data);
                }
                if (_changesMask[13])
                {
                    ToyFlags.WriteUpdateMask(data);
                }
                if (_changesMask[14])
                {
                    Transmog.WriteUpdateMask(data);
                }
                if (_changesMask[15])
                {
                    ConditionalTransmog.WriteUpdateMask(data);
                }
                if (_changesMask[16])
                {
                    SelfResSpells.WriteUpdateMask(data);
                }
                if (_changesMask[17])
                {
                    CharacterRestrictions.WriteUpdateMask(data);
                }
                if (_changesMask[18])
                {
                    SpellPctModByLabel.WriteUpdateMask(data);
                }
                if (_changesMask[19])
                {
                    SpellFlatModByLabel.WriteUpdateMask(data);
                }
            }
            if (_changesMask[22])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (_changesMask[23 + i])
                    {
                        Research[i].WriteUpdateMask(data);
                    }
                }
            }
            if (_changesMask[22])
            {
                for (int i = 0; i < 1; ++i)
                {
                    if (_changesMask[23 + i])
                    {
                        for (int j = 0; j < Research[i].Size(); ++j)
                        {
                            if (Research[i].HasChanged(j))
                            {
                                Research[i][j].WriteUpdate(data, owner, receiver);
                            }
                        }
                    }
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[20])
                {
                    ReplayedQuests.WriteUpdateMask(data);
                }
                if (_changesMask[21])
                {
                    DisabledSpells.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[5])
                {
                    for (int i = 0; i < KnownTitles.Size(); ++i)
                    {
                        if (KnownTitles.HasChanged(i))
                        {
                            data.WriteUInt64(KnownTitles[i]);
                        }
                    }
                }
                if (_changesMask[6])
                {
                    for (int i = 0; i < ResearchSites.Size(); ++i)
                    {
                        if (ResearchSites.HasChanged(i))
                        {
                            data.WriteUInt16(ResearchSites[i]);
                        }
                    }
                }
                if (_changesMask[7])
                {
                    for (int i = 0; i < ResearchSiteProgress.Size(); ++i)
                    {
                        if (ResearchSiteProgress.HasChanged(i))
                        {
                            data.WriteUInt32(ResearchSiteProgress[i]);
                        }
                    }
                }
                if (_changesMask[8])
                {
                    for (int i = 0; i < DailyQuestsCompleted.Size(); ++i)
                    {
                        if (DailyQuestsCompleted.HasChanged(i))
                        {
                            data.WriteUInt32(DailyQuestsCompleted[i]);
                        }
                    }
                }
                if (_changesMask[9])
                {
                    for (int i = 0; i < AvailableQuestLineXQuestIDs.Size(); ++i)
                    {
                        if (AvailableQuestLineXQuestIDs.HasChanged(i))
                        {
                            data.WriteInt32(AvailableQuestLineXQuestIDs[i]);
                        }
                    }
                }
                if (_changesMask[10])
                {
                    for (int i = 0; i < Heirlooms.Size(); ++i)
                    {
                        if (Heirlooms.HasChanged(i))
                        {
                            data.WriteUInt32(Heirlooms[i]);
                        }
                    }
                }
                if (_changesMask[11])
                {
                    for (int i = 0; i < HeirloomFlags.Size(); ++i)
                    {
                        if (HeirloomFlags.HasChanged(i))
                        {
                            data.WriteUInt32(HeirloomFlags[i]);
                        }
                    }
                }
                if (_changesMask[12])
                {
                    for (int i = 0; i < Toys.Size(); ++i)
                    {
                        if (Toys.HasChanged(i))
                        {
                            data.WriteUInt32(Toys[i]);
                        }
                    }
                }
                if (_changesMask[13])
                {
                    for (int i = 0; i < ToyFlags.Size(); ++i)
                    {
                        if (ToyFlags.HasChanged(i))
                        {
                            data.WriteUInt32(ToyFlags[i]);
                        }
                    }
                }
                if (_changesMask[14])
                {
                    for (int i = 0; i < Transmog.Size(); ++i)
                    {
                        if (Transmog.HasChanged(i))
                        {
                            data.WriteUInt32(Transmog[i]);
                        }
                    }
                }
                if (_changesMask[15])
                {
                    for (int i = 0; i < ConditionalTransmog.Size(); ++i)
                    {
                        if (ConditionalTransmog.HasChanged(i))
                        {
                            data.WriteUInt32(ConditionalTransmog[i]);
                        }
                    }
                }
                if (_changesMask[16])
                {
                    for (int i = 0; i < SelfResSpells.Size(); ++i)
                    {
                        if (SelfResSpells.HasChanged(i))
                        {
                            data.WriteUInt32(SelfResSpells[i]);
                        }
                    }
                }
                if (_changesMask[17])
                {
                    for (int i = 0; i < CharacterRestrictions.Size(); ++i)
                    {
                        if (CharacterRestrictions.HasChanged(i))
                        {
                            CharacterRestrictions[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (_changesMask[18])
                {
                    for (int i = 0; i < SpellPctModByLabel.Size(); ++i)
                    {
                        if (SpellPctModByLabel.HasChanged(i))
                        {
                            SpellPctModByLabel[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (_changesMask[19])
                {
                    for (int i = 0; i < SpellFlatModByLabel.Size(); ++i)
                    {
                        if (SpellFlatModByLabel.HasChanged(i))
                        {
                            SpellFlatModByLabel[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (_changesMask[20])
                {
                    for (int i = 0; i < ReplayedQuests.Size(); ++i)
                    {
                        if (ReplayedQuests.HasChanged(i))
                        {
                            ReplayedQuests[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (_changesMask[21])
                {
                    for (int i = 0; i < DisabledSpells.Size(); ++i)
                    {
                        if (DisabledSpells.HasChanged(i))
                        {
                            data.WriteInt32(DisabledSpells[i]);
                        }
                    }
                }
                if (_changesMask[22])
                {
                    data.WritePackedGuid(FarsightObject);
                }
                if (_changesMask[23])
                {
                    data.WritePackedGuid(SummonedBattlePetGUID);
                }
                if (_changesMask[24])
                {
                    data.WritePackedGuid(FarsightObject);
                }
                if (_changesMask[25])
                {
                    data.WritePackedGuid(SummonedBattlePetGUID);
                }
                if (_changesMask[26])
                {
                    data.WriteUInt64(Coinage);
                }
                if (_changesMask[27])
                {
                    data.WriteUInt32(XP);
                }
                if (_changesMask[28])
                {
                    data.WriteUInt32(NextLevelXP);
                }
                if (_changesMask[29])
                {
                    data.WriteInt32(TrialXP);
                }
                if (_changesMask[30])
                {
                    ((SkillInfo)Skill).WriteUpdate(data, owner, receiver);
                }
                if (_changesMask[31])
                {
                    data.WriteUInt32(CharacterPoints);
                }
                if (_changesMask[32])
                {
                    data.WriteFloat(MaxTalentTiers);
                }
                if (_changesMask[33])
                {
                    data.WriteFloat(TrackCreatureMask);
                }
            }
            if (_changesMask[34])
            {
                if (_changesMask[35])
                {
                    data.WriteFloat(MainhandExpertise);
                }
                if (_changesMask[36])
                {
                    data.WriteFloat(OffhandExpertise);
                }
                if (_changesMask[37])
                {
                    data.WriteFloat(RangedExpertise);
                }
                if (_changesMask[38])
                {
                    data.WriteFloat(CombatRatingExpertise);
                }
                if (_changesMask[39])
                {
                    data.WriteFloat(BlockPercentage);
                }
                if (_changesMask[40])
                {
                    data.WriteFloat(DodgePercentage);
                }
                if (_changesMask[41])
                {
                    data.WriteFloat(DodgePercentageFromAttribute);
                }
                if (_changesMask[42])
                {
                    data.WriteFloat(ParryPercentage);
                }
                if (_changesMask[43])
                {
                    data.WriteFloat(ParryPercentageFromAttribute);
                }
                if (_changesMask[44])
                {
                    data.WriteFloat(CritPercentage);
                }
                if (_changesMask[45])
                {
                    data.WriteFloat(RangedCritPercentage);
                }
                if (_changesMask[46])
                {
                    data.WriteFloat(OffhandCritPercentage);
                }
                if (_changesMask[47])
                {
                    data.WriteFloat(SpellCritPercentage);
                }
                if (_changesMask[48])
                {
                    data.WriteFloat(ShieldBlock);
                }
                if (_changesMask[49])
                {
                    data.WriteFloat(ShieldBlockCritPercentage);
                }
                if (_changesMask[50])
                {
                    data.WriteFloat(Mastery);
                }
                if (_changesMask[51])
                {
                    data.WriteFloat(Speed);
                }
                if (_changesMask[52])
                {
                    data.WriteFloat(Avoidance);
                }
                if (_changesMask[53])
                {
                    data.WriteFloat(Sturdiness);
                }
                if (_changesMask[54])
                {
                    data.WriteFloat(Versatility);
                }
                if (_changesMask[55])
                {
                    data.WriteFloat(VersatilityBonus);
                }
                if (_changesMask[56])
                {
                    data.WriteFloat(PvpPowerDamage);
                }
                if (_changesMask[57])
                {
                    data.WriteFloat(PvpPowerHealing);
                }
                if (_changesMask[58])
                {
                    data.WriteFloat(ModHealingDonePos);
                }
                if (_changesMask[59])
                {
                    data.WriteFloat(ModHealingPercent);
                }
                if (_changesMask[60])
                {
                    data.WriteFloat(ModHealingDonePercent);
                }
                if (_changesMask[61])
                {
                    data.WriteFloat(ModPeriodicHealingDonePercent);
                }
                if (_changesMask[62])
                {
                    data.WriteFloat(ModSpellPowerPercent);
                }
                if (_changesMask[63])
                {
                    data.WriteFloat(ModResiliencePercent);
                }
                if (_changesMask[64])
                {
                    data.WriteFloat(OverrideSpellPowerByAPPercent);
                }
                if (_changesMask[65])
                {
                    data.WriteFloat(OverrideAPBySpellPowerPercent);
                }
            }
            if (_changesMask[66])
            {
                if (_changesMask[67])
                {
                    data.WriteInt32(ModTargetResistance);
                }
                if (_changesMask[68])
                {
                    data.WriteInt32(ModTargetPhysicalResistance);
                }
                if (_changesMask[69])
                {
                    data.WriteInt32(LocalFlags);
                }
                if (_changesMask[70])
                {
                    data.WriteUInt8(GrantableLevels);
                }
                if (_changesMask[71])
                {
                    data.WriteUInt8(MultiActionBars);
                }
                if (_changesMask[72])
                {
                    data.WriteUInt32(LifetimeMaxRank);
                }
                if (_changesMask[73])
                {
                    data.WriteUInt16(NumRespecs);
                }
                if (_changesMask[74])
                {
                    data.WriteUInt32(PvpMedals);
                }
                if (_changesMask[75])
                {
                    data.WriteUInt32(TodayHonorableKills);
                }
                if (_changesMask[76])
                {
                    data.WriteUInt32(YesterdayHonorableKills);
                }
                if (_changesMask[77])
                {
                    data.WriteUInt32(LifetimeHonorableKills);
                }
                if (_changesMask[78])
                {
                    data.WriteUInt32(WatchedFactionIndex);
                }
                if (_changesMask[79])
                {
                    data.WriteInt32(MaxLevel);
                }
                if (_changesMask[80])
                {
                    data.WriteInt32(ScalingPlayerLevelDelta);
                }
                if (_changesMask[81])
                {
                    data.WriteFloat(MaxCreatureScalingLevel);
                }
                if (_changesMask[82])
                {
                    data.WriteFloat(PetSpellPower);
                }
                if (_changesMask[83])
                {
                    data.WriteFloat(UiHitModifier);
                }
                if (_changesMask[84])
                {
                    data.WriteFloat(UiSpellHitModifier);
                }
                if (_changesMask[85])
                {
                    data.WriteInt32(HomeRealmTimeOffset);
                }
                if (_changesMask[86])
                {
                    data.WriteFloat(ModPetHaste);
                }
                if (_changesMask[87])
                {
                    data.WriteUInt8(LocalRegenFlags);
                }
                if (_changesMask[88])
                {
                    data.WriteUInt32(AuraVision);
                }
                if (_changesMask[89])
                {
                    data.WriteInt32(NumBackpackSlots);
                }
                if (_changesMask[90])
                {
                    data.WriteUInt32(OverrideSpellsID);
                }
                if (_changesMask[91])
                {
                    data.WriteInt32(LfgBonusFactionID);
                }
                if (_changesMask[92])
                {
                    data.WriteUInt32(LootSpecID);
                }
                if (_changesMask[93])
                {
                    data.WriteUInt32(OverrideZonePVPType);
                }
                if (_changesMask[94])
                {
                    data.WriteUInt32(Honor);
                }
                if (_changesMask[95])
                {
                    data.WriteUInt32(HonorNextLevel);
                }
                if (_changesMask[96])
                {
                    data.WriteInt32(PvpRewardAchieved);
                }
                if (_changesMask[97])
                {
                    data.WriteInt32(PvpTierMaxFromWins);
                }
            }
            if (_changesMask[98])
            {
                if (_changesMask[99])
                {
                    data.WriteInt32(PvpLastWeeksRewardAchieved);
                }
                if (_changesMask[100])
                {
                    data.WriteInt32(PvpLastWeeksTierMaxFromWins);
                }
                if (_changesMask[101])
                {
                    data.WriteInt32(PvpLastWeeksRewardClaimed);
                }
                if (_changesMask[102])
                {
                    data.WriteUInt8(NumBankSlots);
                }
            }
            data.FlushBits();
            if (_changesMask[98])
            {
                data.WriteBit(QuestSession.GetValue().HasValue);
                if (_changesMask[103])
                {
                    if (QuestSession.GetValue().HasValue)
                    {
                        QuestSession.GetValue().Value.WriteUpdate(data, owner, receiver);
                    }
                }
            }
            if (_changesMask[104])
            {
                for (int i = 0; i < 195; ++i)
                {
                    if (_changesMask[105 + i])
                    {
                        data.WritePackedGuid(InvSlots[i]);
                    }
                }
            }
            if (_changesMask[300])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (_changesMask[301 + i])
                    {
                        data.WriteUInt32(TrackResourceMask[i]);
                    }
                }
            }
            if (_changesMask[303])
            {
                for (int i = 0; i < 192; ++i)
                {
                    if (_changesMask[304 + i])
                    {
                        data.WriteUInt64(ExploredZones[i]);
                    }
                }
            }
            if (_changesMask[496])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (_changesMask[497 + i])
                    {
                        RestInfo[i].WriteUpdate(data, owner, receiver);
                    }
                }
            }
            if (_changesMask[499])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (_changesMask[500 + i])
                    {
                        data.WriteInt32(ModDamageDonePos[i]);
                    }
                    if (_changesMask[507 + i])
                    {
                        data.WriteInt32(ModDamageDoneNeg[i]);
                    }
                    if (_changesMask[514 + i])
                    {
                        data.WriteFloat(ModDamageDonePercent[i]);
                    }
                }
            }
            if (_changesMask[521])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (_changesMask[522 + i])
                    {
                        data.WriteFloat(WeaponDmgMultipliers[i]);
                    }
                    if (_changesMask[525 + i])
                    {
                        data.WriteFloat(WeaponAtkSpeedMultipliers[i]);
                    }
                }
            }
            if (_changesMask[528])
            {
                for (int i = 0; i < 12; ++i)
                {
                    if (_changesMask[529 + i])
                    {
                        data.WriteUInt32(BuybackPrice[i]);
                    }
                    if (_changesMask[541 + i])
                    {
                        data.WriteUInt32(BuybackTimestamp[i]);
                    }
                }
            }
            if (_changesMask[553])
            {
                for (int i = 0; i < 32; ++i)
                {
                    if (_changesMask[554 + i])
                    {
                        data.WriteUInt32(CombatRatings[i]);
                    }
                }
            }
            if (_changesMask[593])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (_changesMask[594 + i])
                    {
                        data.WriteUInt32(NoReagentCostMask[i]);
                    }
                }
            }
            if (_changesMask[598])
            {
                for (int i = 0; i < 2; ++i)
                {
                    if (_changesMask[599 + i])
                    {
                        data.WriteUInt32(ProfessionSkillLine[i]);
                    }
                }
            }
            if (_changesMask[601])
            {
                for (int i = 0; i < 4; ++i)
                {
                    if (_changesMask[602 + i])
                    {
                        data.WriteUInt32(BagSlotFlags[i]);
                    }
                }
            }
            if (_changesMask[606])
            {
                for (int i = 0; i < 7; ++i)
                {
                    if (_changesMask[607 + i])
                    {
                        data.WriteUInt32(BankBagSlotFlags[i]);
                    }
                }
            }
            if (_changesMask[614])
            {
                for (int i = 0; i < 875; ++i)
                {
                    if (_changesMask[615 + i])
                    {
                        data.WriteUInt64(QuestCompleted[i]);
                    }
                }
            }
            if (_changesMask[586])
            {
                for (int i = 0; i < 6; ++i)
                {
                    if (_changesMask[587 + i])
                    {
                        PvpInfo[i].WriteUpdate(data, owner, receiver);
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
            ClearChangesMask(SpellPctModByLabel);
            ClearChangesMask(SpellFlatModByLabel);
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
            ClearChangesMask(LocalRegenFlags);
            ClearChangesMask(AuraVision);
            ClearChangesMask(NumBackpackSlots);
            ClearChangesMask(OverrideSpellsID);
            ClearChangesMask(LfgBonusFactionID);
            ClearChangesMask(LootSpecID);
            ClearChangesMask(OverrideZonePVPType);
            ClearChangesMask(Honor);
            ClearChangesMask(HonorNextLevel);
            ClearChangesMask(PvpRewardAchieved);
            ClearChangesMask(PvpTierMaxFromWins);
            ClearChangesMask(PvpLastWeeksRewardAchieved);
            ClearChangesMask(PvpLastWeeksTierMaxFromWins);
            ClearChangesMask(PvpLastWeeksRewardClaimed);
            ClearChangesMask(NumBankSlots);
            ClearChangesMask(QuestSession);
            ClearChangesMask(InvSlots);
            ClearChangesMask(TrackResourceMask);
            ClearChangesMask(ExploredZones);
            ClearChangesMask(RestInfo);
            ClearChangesMask(ModDamageDonePos);
            ClearChangesMask(ModDamageDoneNeg);
            ClearChangesMask(ModDamageDonePercent);
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
        public UpdateField<List<uint>> StateWorldEffectIDs = new UpdateField<List<uint>>(0, 1);
        public DynamicUpdateField<int> EnableDoodadSets = new DynamicUpdateField<int>(0, 2);
        public UpdateField<uint> DisplayID = new UpdateField<uint>(0, 3);
        public UpdateField<uint> SpellVisualID = new UpdateField<uint>(0, 4);
        public UpdateField<uint> StateSpellVisualID = new UpdateField<uint>(0, 5);
        public UpdateField<uint> SpawnTrackingStateAnimID = new UpdateField<uint>(0, 6);
        public UpdateField<uint> SpawnTrackingStateAnimKitID = new UpdateField<uint>(0, 7);
        public UpdateField<uint> StateWorldEffectsQuestObjectiveID = new UpdateField<uint>(0, 8);
        public UpdateField<ObjectGuid> CreatedBy = new UpdateField<ObjectGuid>(0, 9);
        public UpdateField<ObjectGuid> GuildGUID = new UpdateField<ObjectGuid>(0, 10);
        public UpdateField<uint> Flags = new UpdateField<uint>(0, 11);
        public UpdateField<Quaternion> ParentRotation = new UpdateField<Quaternion>(0, 12);
        public UpdateField<uint> FactionTemplate = new UpdateField<uint>(0, 13);
        public UpdateField<uint> Level = new UpdateField<uint>(0, 14);
        public UpdateField<sbyte> State = new UpdateField<sbyte>(0, 15);
        public UpdateField<sbyte> TypeID = new UpdateField<sbyte>(0, 16);
        public UpdateField<byte> PercentHealth = new UpdateField<byte>(0, 17);
        public UpdateField<uint> ArtKit = new UpdateField<uint>(0, 18);
        public UpdateField<uint> CustomParam = new UpdateField<uint>(0, 19);

        public GameObjectFieldData() : base(0, TypeId.GameObject, 20) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
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
            data.WriteUInt32(GetViewerGameObjectLevel(Level, owner, receiver));
            data.WriteInt8((sbyte)GetViewerGameObjectState(State, owner, receiver));
            data.WriteInt8((sbyte)TypeID);
            data.WriteUInt8(PercentHealth);
            data.WriteUInt32(ArtKit);
            data.WriteInt32(EnableDoodadSets.Size());
            data.WriteUInt32(CustomParam);
            for (int i = 0; i < EnableDoodadSets.Size(); ++i)
            {
                data.WriteInt32(EnableDoodadSets[i]);
            }
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, GameObject owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 20);

            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    List<uint> list = StateWorldEffectIDs;
                    data.WriteBits(list.Count, 32);
                    for (int i = 0; i < list.Count; ++i)
                    {
                        data.WriteUInt32(list[i]);
                    }
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[2])
                {
                    EnableDoodadSets.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[2])
                {
                    for (int i = 0; i < EnableDoodadSets.Size(); ++i)
                    {
                        if (EnableDoodadSets.HasChanged(i))
                        {
                            data.WriteInt32(EnableDoodadSets[i]);
                        }
                    }
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(DisplayID);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(SpellVisualID);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(StateSpellVisualID);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt32(SpawnTrackingStateAnimID);
                }
                if (_changesMask[7])
                {
                    data.WriteUInt32(SpawnTrackingStateAnimKitID);
                }
                if (_changesMask[8])
                {
                    data.WriteUInt32(StateWorldEffectsQuestObjectiveID);
                }
                if (_changesMask[9])
                {
                    data.WritePackedGuid(CreatedBy);
                }
                if (_changesMask[10])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (_changesMask[11])
                {
                    data.WriteUInt32(GetViewerGameObjectFlags(Flags, owner, receiver));
                }
                if (_changesMask[12])
                {
                    Quaternion rotation = ParentRotation;
                    data.WriteFloat(rotation.X);
                    data.WriteFloat(rotation.Y);
                    data.WriteFloat(rotation.Z);
                    data.WriteFloat(rotation.W);
                }
                if (_changesMask[13])
                {
                    data.WriteUInt32(FactionTemplate);
                }
                if (_changesMask[14])
                {
                    data.WriteUInt32(GetViewerGameObjectLevel(Level, owner, receiver));
                }
                if (_changesMask[15])
                {
                    data.WriteInt8((sbyte)GetViewerGameObjectState(State, owner, receiver));
                }
                if (_changesMask[16])
                {
                    data.WriteInt8((sbyte)TypeID);
                }
                if (_changesMask[17])
                {
                    data.WriteUInt8(PercentHealth);
                }
                if (_changesMask[18])
                {
                    data.WriteUInt32(ArtKit);
                }
                if (_changesMask[19])
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
            ClearChangesMask(StateWorldEffectsQuestObjectiveID);
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
        public UpdateField<ObjectGuid> Caster = new UpdateField<ObjectGuid>(0, 1);
        public UpdateField<uint> SpellXSpellVisualID = new UpdateField<uint>(0, 2);
        public UpdateField<uint> SpellID = new UpdateField<uint>(0, 3);
        public UpdateField<float> Radius = new UpdateField<float>(0, 4);
        public UpdateField<uint> CastTime = new UpdateField<uint>(0, 5);
        public UpdateField<byte> Type = new UpdateField<byte>(0, 6);

        public DynamicObjectData() : base(0, TypeId.DynamicObject, 7) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
        {
            data.WritePackedGuid(Caster);
            data.WriteUInt32(SpellXSpellVisualID);
            data.WriteUInt32(SpellID);
            data.WriteFloat(Radius);
            data.WriteUInt32(CastTime);
            data.WriteUInt8(Type);
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, DynamicObject owner, Player receiver)
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
                    data.WriteUInt32(SpellXSpellVisualID);
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
            ClearChangesMask(SpellXSpellVisualID);
            ClearChangesMask(SpellID);
            ClearChangesMask(Radius);
            ClearChangesMask(CastTime);
            ClearChangesMask(Type);
            _changesMask.ResetAll();
        }
    }

    public class CorpseData : BaseUpdateData<Corpse>
    {
        public UpdateField<uint> DynamicFlags = new UpdateField<uint>(0, 1);
        public UpdateField<ObjectGuid> Owner = new UpdateField<ObjectGuid>(0, 2);
        public UpdateField<ObjectGuid> PartyGUID = new UpdateField<ObjectGuid>(0, 3);
        public UpdateField<ObjectGuid> GuildGUID = new UpdateField<ObjectGuid>(0, 4);
        public UpdateField<uint> DisplayID = new UpdateField<uint>(0, 5);
        public UpdateField<byte> Unused = new UpdateField<byte>(0, 6);
        public UpdateField<byte> RaceID = new UpdateField<byte>(0, 7);
        public UpdateField<byte> Sex = new UpdateField<byte>(0, 8);
        public UpdateField<byte> SkinID = new UpdateField<byte>(0, 9);
        public UpdateField<byte> FaceID = new UpdateField<byte>(0, 10);
        public UpdateField<byte> HairStyleID = new UpdateField<byte>(0, 11);
        public UpdateField<byte> HairColorID = new UpdateField<byte>(0, 12);
        public UpdateField<byte> FacialHairStyleID = new UpdateField<byte>(0, 13);
        public UpdateField<uint> Flags = new UpdateField<uint>(0, 14);
        public UpdateField<int> FactionTemplate = new UpdateField<int>(0, 15);
        public UpdateFieldArray<uint> Items = new UpdateFieldArray<uint>(19, 16, 17);
        public UpdateFieldArray<byte> CustomDisplayOption = new UpdateFieldArray<byte>(3, 36, 37);

        public CorpseData() : base(0, TypeId.Corpse, 40) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Corpse owner, Player receiver)
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
            data.WriteUInt8(Unused);
            data.WriteUInt8(RaceID);
            data.WriteUInt8(Sex);
            data.WriteUInt8(SkinID);
            data.WriteUInt8(FaceID);
            data.WriteUInt8(HairStyleID);
            data.WriteUInt8(HairColorID);
            data.WriteUInt8(FacialHairStyleID);
            data.WriteUInt32(Flags);
            data.WriteInt32(FactionTemplate);
            for (int i = 0; i < 3; ++i)
            {
                data.WriteUInt8(CustomDisplayOption[i]);
            }
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Corpse owner, Player receiver)
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
                    data.WriteUInt32(DynamicFlags);
                }
                if (_changesMask[2])
                {
                    data.WritePackedGuid(Owner);
                }
                if (_changesMask[3])
                {
                    data.WritePackedGuid(PartyGUID);
                }
                if (_changesMask[4])
                {
                    data.WritePackedGuid(GuildGUID);
                }
                if (_changesMask[5])
                {
                    data.WriteUInt32(DisplayID);
                }
                if (_changesMask[6])
                {
                    data.WriteUInt8(Unused);
                }
                if (_changesMask[7])
                {
                    data.WriteUInt8(RaceID);
                }
                if (_changesMask[8])
                {
                    data.WriteUInt8(Sex);
                }
                if (_changesMask[9])
                {
                    data.WriteUInt8(SkinID);
                }
                if (_changesMask[10])
                {
                    data.WriteUInt8(FaceID);
                }
                if (_changesMask[11])
                {
                    data.WriteUInt8(HairStyleID);
                }
                if (_changesMask[12])
                {
                    data.WriteUInt8(HairColorID);
                }
                if (_changesMask[13])
                {
                    data.WriteUInt8(FacialHairStyleID);
                }
                if (_changesMask[14])
                {
                    data.WriteUInt32(Flags);
                }
                if (_changesMask[15])
                {
                    data.WriteInt32(FactionTemplate);
                }
            }
            if (_changesMask[16])
            {
                for (int i = 0; i < 19; ++i)
                {
                    if (_changesMask[17 + i])
                    {
                        data.WriteUInt32(Items[i]);
                    }
                }
            }
            if (_changesMask[36])
            {
                for (int i = 0; i < 3; ++i)
                {
                    if (_changesMask[37 + i])
                    {
                        data.WriteUInt8(CustomDisplayOption[i]);
                    }
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(DynamicFlags);
            ClearChangesMask(Owner);
            ClearChangesMask(PartyGUID);
            ClearChangesMask(GuildGUID);
            ClearChangesMask(DisplayID);
            ClearChangesMask(Unused);
            ClearChangesMask(RaceID);
            ClearChangesMask(Sex);
            ClearChangesMask(SkinID);
            ClearChangesMask(FaceID);
            ClearChangesMask(HairStyleID);
            ClearChangesMask(HairColorID);
            ClearChangesMask(FacialHairStyleID);
            ClearChangesMask(Flags);
            ClearChangesMask(FactionTemplate);
            ClearChangesMask(Items);
            ClearChangesMask(CustomDisplayOption);
            _changesMask.ResetAll();
        }
    }

    public class ScaleCurve : BaseUpdateData<AreaTrigger>
    {
        public UpdateField<bool> OverrideActive = new UpdateField<bool>(0, 1);
        public UpdateField<uint> StartTimeOffset = new UpdateField<uint>(0, 2);
        public UpdateField<uint> ParameterCurve = new UpdateField<uint>(0, 3);
        public UpdateFieldArray<Vector2> Points = new UpdateFieldArray<Vector2>(2, 4, 5);

        public ScaleCurve() : base(7) { }

        public override void WriteCreate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            data.WriteUInt32(StartTimeOffset);
            for (int i = 0; i < 2; ++i)
            {
                data.WriteVector2(Points[i]);
            }
            data.WriteUInt32(ParameterCurve);
            data.WriteBit(OverrideActive);
            data.FlushBits();
        }
        public override void WriteUpdate(WorldPacket data, AreaTrigger owner, Player receiver)
        {
            UpdateMask changesMask = _changesMask;
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
        public UpdateField<ScaleCurve> OverrideScaleCurve = new UpdateField<ScaleCurve>(0, 1);
        public UpdateField<ScaleCurve> ExtraScaleCurve = new UpdateField<ScaleCurve>(0, 2);
        public UpdateField<ObjectGuid> Caster = new UpdateField<ObjectGuid>(0, 3);
        public UpdateField<uint> Duration = new UpdateField<uint>(0, 4);
        public UpdateField<uint> TimeToTarget = new UpdateField<uint>(0, 5);
        public UpdateField<uint> TimeToTargetScale = new UpdateField<uint>(0, 6);
        public UpdateField<uint> TimeToTargetExtraScale = new UpdateField<uint>(0, 7);
        public UpdateField<uint> SpellID = new UpdateField<uint>(0, 8);
        public UpdateField<uint> SpellForVisuals = new UpdateField<uint>(0, 9);
        public UpdateField<uint> SpellXSpellVisualID = new UpdateField<uint>(0, 10);
        public UpdateField<float> BoundsRadius2D = new UpdateField<float>(0, 11);
        public UpdateField<uint> DecalPropertiesID = new UpdateField<uint>(0, 12);
        public UpdateField<ObjectGuid> CreatingEffectGUID = new UpdateField<ObjectGuid>(0, 13);

        public AreaTriggerFieldData() : base(0, TypeId.AreaTrigger, 14) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
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
            ((ScaleCurve)ExtraScaleCurve).WriteCreate(data, owner, receiver);
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, AreaTrigger owner, Player receiver)
        {
            data.WriteBits(_changesMask.GetBlock(0), 14);

            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[1])
                {
                    ((ScaleCurve)OverrideScaleCurve).WriteUpdate(data, owner, receiver);
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
                if (_changesMask[2])
                {
                    ((ScaleCurve)ExtraScaleCurve).WriteUpdate(data, owner, receiver);
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
            _changesMask.ResetAll();
        }
    }

    public class SceneObjectData : BaseUpdateData<WorldObject>
    {
        public UpdateField<int> ScriptPackageID = new UpdateField<int>(0, 1);
        public UpdateField<uint> RndSeedVal = new UpdateField<uint>(0, 2);
        public UpdateField<ObjectGuid> CreatedBy = new UpdateField<ObjectGuid>(0, 3);
        public UpdateField<uint> SceneType = new UpdateField<uint>(0, 4);

        public SceneObjectData() : base(5) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
        {
            data.WriteInt32(ScriptPackageID);
            data.WriteUInt32(RndSeedVal);
            data.WritePackedGuid(CreatedBy);
            data.WriteUInt32(SceneType);
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, WorldObject owner, Player receiver)
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
            data.WriteUInt32(StartTime);
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
        }
        public void WriteUpdate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(ConversationLineID);
            data.WriteUInt32(StartTime);
            data.WriteUInt32(UiCameraID);
            data.WriteUInt8(ActorIndex);
            data.WriteUInt8(Flags);
        }
    }

    public class ConversationActor
    {
        public uint CreatureID;
        public uint CreatureDisplayInfoID;
        public ObjectGuid ActorGUID;
        public int Field_18;
        public ConversationActorType Type;

        public void WriteCreate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.WritePackedGuid(ActorGUID);
            data.WriteInt32(Field_18);
            data.WriteBits(Type, 1);
            data.FlushBits();
        }
        public void WriteUpdate(WorldPacket data, Conversation owner, Player receiver)
        {
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CreatureDisplayInfoID);
            data.WritePackedGuid(ActorGUID);
            data.WriteInt32(Field_18);
            data.WriteBits(Type, 1);
            data.FlushBits();
        }
    }

    public class ConversationData : BaseUpdateData<Conversation>
    {
        public UpdateField<List<ConversationLine>> Lines = new UpdateField<List<ConversationLine>>(0, 1);
        public DynamicUpdateField<ConversationActor> Actors = new DynamicUpdateField<ConversationActor>(0, 2);
        public UpdateField<uint> LastLineEndTime = new UpdateField<uint>(0, 3);
        public UpdateField<uint> Field_1C = new UpdateField<uint>(0, 4);

        public ConversationData() : base(0, TypeId.Conversation, 5) { }

        public override void WriteCreate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
        {
            data.WriteInt32(((List<ConversationLine>)Lines).Count);
            data.WriteUInt32(LastLineEndTime);
            data.WriteUInt32(Field_1C);
            for (int i = 0; i < ((List<ConversationLine>)Lines).Count; ++i)
            {
                ((List<ConversationLine>)Lines)[i].WriteCreate(data, owner, receiver);
            }
            data.WriteInt32(Actors.Size());
            for (int i = 0; i < Actors.Size(); ++i)
            {
                Actors[i].WriteCreate(data, owner, receiver);
            }
        }
        public override void WriteUpdate(WorldPacket data, UpdateFieldFlag fieldVisibilityFlags, Conversation owner, Player receiver)
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
                        list[i].WriteUpdate(data, owner, receiver);
                    }
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[2])
                {
                    Actors.WriteUpdateMask(data);
                }
            }
            data.FlushBits();
            if (_changesMask[0])
            {
                if (_changesMask[2])
                {
                    for (int i = 0; i < Actors.Size(); ++i)
                    {
                        if (Actors.HasChanged(i))
                        {
                            Actors[i].WriteUpdate(data, owner, receiver);
                        }
                    }
                }
                if (_changesMask[3])
                {
                    data.WriteUInt32(LastLineEndTime);
                }
                if (_changesMask[4])
                {
                    data.WriteUInt32(Field_1C);
                }
            }
        }

        public override void ClearChangesMask()
        {
            ClearChangesMask(Lines);
            ClearChangesMask(Actors);
            ClearChangesMask(LastLineEndTime);
            ClearChangesMask(Field_1C);
            _changesMask.ResetAll();
        }
    }
}
