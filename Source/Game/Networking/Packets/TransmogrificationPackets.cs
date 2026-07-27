// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Networking.Packets
{
    class TransmogrifyItems(WorldPacket packet) : ClientPacket(packet)
    {
        public ObjectGuid Npc;
        public Array<TransmogrifyItem> Items = new(13);
        public bool CurrentSpecOnly;

        public override void Read()
        {
            var itemsCount = _worldPacket.ReadUInt32();
            Npc = _worldPacket.ReadPackedGuid();

            for (var i = 0; i < itemsCount; ++i)
            {
                TransmogrifyItem item = new();
                item.Read(_worldPacket);
                Items[i] = item;
            }

            _worldPacket.ResetBitPos();
            CurrentSpecOnly = _worldPacket.HasBit();
        }
    }

    class TransmogOutfitNew(WorldPacket packet) : ClientPacket(packet)
    {
        public ObjectGuid Npc;
        public TransmogOutfitDataInfo Info;
        public TransmogOutfitEntrySource Source;

        public override void Read()
        {
            Npc = _worldPacket.ReadPackedGuid();
            Source = (TransmogOutfitEntrySource)_worldPacket.ReadUInt8();
            Info.Read(_worldPacket);
        }
    }

    class TransmogOutfitNewEntryAdded() : ServerPacket(ServerOpcodes.TransmogOutfitNewEntryAdded, ConnectionType.Instance)
    {
        public uint TransmogOutfitID;

        public override void Write()
        {
            _worldPacket.WriteUInt32(TransmogOutfitID);
        }
    }

    class TransmogOutfitUpdateInfo(WorldPacket packet) : ClientPacket(packet)
    {
        public uint OutfitID;
        public ObjectGuid Npc;
        public TransmogOutfitDataInfo Info;

        public override void Read()
        {
            OutfitID = _worldPacket.ReadUInt32();
            Npc = _worldPacket.ReadPackedGuid();
            Info.Read(_worldPacket);
        }
    }

    class TransmogOutfitInfoUpdated() : ServerPacket(ServerOpcodes.TransmogOutfitInfoUpdated, ConnectionType.Instance)
    {
        public uint TransmogOutfitID;
        public TransmogOutfitDataInfo OutfitInfo;

        public override void Write()
        {
            _worldPacket.WriteUInt32(TransmogOutfitID);
            OutfitInfo.Write(_worldPacket);
        }
    }

    class TransmogOutfitUpdateSituations(WorldPacket packet) : ClientPacket(packet)
    {
        public uint OutfitID;
        public ObjectGuid Npc;
        public bool SituationsEnabled;
        public Array<TransmogOutfitSituationInfo> Situations = new(100);

        public override void Read()
        {
            OutfitID = _worldPacket.ReadUInt32();
            Npc = _worldPacket.ReadPackedGuid();
            int situationsCount = _worldPacket.ReadInt32();
            for (var i = 0; i < situationsCount; ++i)
            {
                TransmogOutfitSituationInfo situation = new();
                situation.Read(_worldPacket);
                Situations.Add(situation);
            }

            _worldPacket.ResetBitPos();
            SituationsEnabled = _worldPacket.HasBit();
        }
    }

    class TransmogOutfitSituationsUpdated() : ServerPacket(ServerOpcodes.TransmogOutfitSituationsUpdated, ConnectionType.Instance)
    {
        public int TransmogOutfitID;
        public bool SituationsEnabled;
        public List<TransmogOutfitSituationInfo> Situations;

        public override void Write()
        {
            _worldPacket.WriteInt32(TransmogOutfitID);
            _worldPacket.WriteInt32(Situations.Count);

            foreach (TransmogOutfitSituationInfo situation in Situations)
                situation.Write(_worldPacket);

            _worldPacket.WriteBit(SituationsEnabled);
            _worldPacket.FlushBits();
        }
    }

    class TransmogOutfitUpdateSlots(WorldPacket packet) : ClientPacket(packet)
    {
        public uint OutfitID;
        public Array<TransmogOutfitSlotData> Slots = new(30);
        public ObjectGuid Npc;
        public ulong Cost;
        public bool UseAvailableDiscount;

        public override void Read()
        {
            OutfitID = _worldPacket.ReadUInt32();
            int slotsCount = _worldPacket.ReadInt32();
            Npc = _worldPacket.ReadPackedGuid();
            Cost = _worldPacket.ReadUInt64();

            for (var i = 0; i < slotsCount; ++i)
            {
                TransmogOutfitSlotData slot = new();
                slot.Read(_worldPacket);
                Slots.Add(slot);
            }

            _worldPacket.ResetBitPos();
            UseAvailableDiscount = _worldPacket.HasBit();

            Slots = new Array<TransmogOutfitSlotData>(30, Slots.OrderBy(p => p.Slot).ThenBy(p => p.SlotOption));
        }
    }

    class TransmogOutfitSlotsUpdated() : ServerPacket(ServerOpcodes.TransmogOutfitSlotsUpdated, ConnectionType.Instance)
    {
        public uint TransmogOutfitID;
        public List<TransmogOutfitSlotData> Slots = [];

        public override void Write()
        {
            _worldPacket.WriteUInt32(TransmogOutfitID);
            _worldPacket.WriteInt32(Slots.Count);

            foreach (TransmogOutfitSlotData slot in Slots)
                slot.Write(_worldPacket);
        }
    }

    class AccountTransmogUpdate() : ServerPacket(ServerOpcodes.AccountTransmogUpdate, ConnectionType.Instance)
    {
        public bool IsFullUpdate;
        public bool IsSetFavorite;
        public List<uint> FavoriteAppearances = new();
        public List<uint> NewAppearances = new();

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.WriteBit(IsSetFavorite);
            _worldPacket.WriteInt32(FavoriteAppearances.Count);
            _worldPacket.WriteInt32(NewAppearances.Count);

            foreach (uint itemModifiedAppearanceId in FavoriteAppearances)
                _worldPacket.WriteUInt32(itemModifiedAppearanceId);

            foreach (var newAppearance in NewAppearances)
                _worldPacket.WriteUInt32(newAppearance);
        }
    }

    struct TransmogrifyItem
    {
        public int ItemModifiedAppearanceID;
        public uint Slot;
        public int SpellItemEnchantmentID;
        public int SecondaryItemModifiedAppearanceID;

        public void Read(WorldPacket data)
        {
            ItemModifiedAppearanceID = data.ReadInt32();
            Slot = data.ReadUInt32();
            SpellItemEnchantmentID = data.ReadInt32();
            SecondaryItemModifiedAppearanceID = data.ReadInt32();
        }
    }

    public struct TransmogOutfitDataInfo
    {
        public TransmogOutfitSetType SetType;
        public bool SituationsEnabled;
        public uint Icon;
        public string Name;

        public void Read(WorldPacket data)
        {
            data.ResetBitPos();
            SetType = (TransmogOutfitSetType)data.ReadUInt8();
            Icon = data.ReadUInt32();
            uint nameSize = data.ReadBits<uint>(8);
            SituationsEnabled = data.HasBit();

            Name = data.ReadString(nameSize);
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt8((byte)SetType);
            data.WriteUInt32(Icon);
            data.WriteInt32(Name.GetByteCount());
            data.WriteBit(SituationsEnabled);
            data.FlushBits();

            data.WriteString(Name);
        }
    }

    public struct TransmogOutfitSituationInfo
    {
        public uint SituationID;
        public uint SpecID;
        public uint LoadoutID;
        public uint EquipmentSetID;

        public void Read(WorldPacket data)
        {
            SituationID = data.ReadUInt32();
            SpecID = data.ReadUInt32();
            LoadoutID = data.ReadUInt32();
            EquipmentSetID = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(SituationID);
            data.WriteUInt32(SpecID);
            data.WriteUInt32(LoadoutID);
            data.WriteUInt32(EquipmentSetID);
        }
    }

    public struct TransmogOutfitSlotData
    {
        public TransmogOutfitSlot Slot;
        public TransmogOutfitSlotOption SlotOption;
        public TransmogOutfitDisplayType AppearanceDisplayType;
        public TransmogOutfitDisplayType IllusionDisplayType;
        public uint ItemModifiedAppearanceID;
        public uint SpellItemEnchantmentID;
        public uint Flags;

        public void Read(WorldPacket data)
        {
            Slot = (TransmogOutfitSlot)data.ReadInt8();
            SlotOption = (TransmogOutfitSlotOption)data.ReadUInt8();
            ItemModifiedAppearanceID = data.ReadUInt32();
            AppearanceDisplayType = (TransmogOutfitDisplayType)data.ReadUInt8();
            SpellItemEnchantmentID = data.ReadUInt32();
            IllusionDisplayType = (TransmogOutfitDisplayType)data.ReadUInt8();
            Flags = data.ReadUInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt8((sbyte)Slot);
            data.WriteUInt8((byte)SlotOption);
            data.WriteUInt32(ItemModifiedAppearanceID);
            data.WriteUInt8((byte)AppearanceDisplayType);
            data.WriteUInt32(SpellItemEnchantmentID);
            data.WriteUInt8((byte)IllusionDisplayType);
            data.WriteUInt32(Flags);
        }
    }

}
