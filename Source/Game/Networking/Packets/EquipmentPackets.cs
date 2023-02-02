// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    public class EquipmentSetID : ServerPacket
    {
        public EquipmentSetID() : base(ServerOpcodes.EquipmentSetId, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(GUID);
            _worldPacket.WriteInt32(Type);
            _worldPacket.WriteUInt32(SetID);
        }

        public ulong GUID; // Set Identifier
        public int Type;
        public uint SetID; // Index
    }

    public class LoadEquipmentSet : ServerPacket
    {
        public LoadEquipmentSet() : base(ServerOpcodes.LoadEquipmentSet, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(SetData.Count);

            foreach (var equipSet in SetData)
            {
                _worldPacket.WriteInt32((int)equipSet.Type);
                _worldPacket.WriteUInt64(equipSet.Guid);
                _worldPacket.WriteUInt32(equipSet.SetID);
                _worldPacket.WriteUInt32(equipSet.IgnoreMask);

                for (int i = 0; i < EquipmentSlot.End; ++i)
                {
                    _worldPacket.WritePackedGuid(equipSet.Pieces[i]);
                    _worldPacket.WriteInt32(equipSet.Appearances[i]);
                }

                foreach (var id in equipSet.Enchants)
                    _worldPacket.WriteInt32(id);

                _worldPacket.WriteInt32(equipSet.SecondaryShoulderApparanceID);
                _worldPacket.WriteInt32(equipSet.SecondaryShoulderSlot);
                _worldPacket.WriteInt32(equipSet.SecondaryWeaponAppearanceID);
                _worldPacket.WriteInt32(equipSet.SecondaryWeaponSlot);

                _worldPacket.WriteBit(equipSet.AssignedSpecIndex != -1);
                _worldPacket.WriteBits(equipSet.SetName.GetByteCount(), 8);
                _worldPacket.WriteBits(equipSet.SetIcon.GetByteCount(), 9);

                if (equipSet.AssignedSpecIndex != -1)
                    _worldPacket.WriteInt32(equipSet.AssignedSpecIndex);

                _worldPacket.WriteString(equipSet.SetName);
                _worldPacket.WriteString(equipSet.SetIcon);
            }
        }

        public List<EquipmentSetInfo.EquipmentSetData> SetData = new();
    }

    public class SaveEquipmentSet : ClientPacket
    {
        public SaveEquipmentSet(WorldPacket packet) : base(packet)
        {
            Set = new EquipmentSetInfo.EquipmentSetData();
        }

        public override void Read()
        {
            Set.Type = (EquipmentSetInfo.EquipmentSetType)_worldPacket.ReadInt32();
            Set.Guid = _worldPacket.ReadUInt64();
            Set.SetID = _worldPacket.ReadUInt32();
            Set.IgnoreMask = _worldPacket.ReadUInt32();

            for (byte i = 0; i < EquipmentSlot.End; ++i)
            {
                Set.Pieces[i] = _worldPacket.ReadPackedGuid();
                Set.Appearances[i] = _worldPacket.ReadInt32();
            }

            Set.Enchants[0] = _worldPacket.ReadInt32();
            Set.Enchants[1] = _worldPacket.ReadInt32();

            Set.SecondaryShoulderApparanceID = _worldPacket.ReadInt32();
            Set.SecondaryShoulderSlot = _worldPacket.ReadInt32();
            Set.SecondaryWeaponAppearanceID = _worldPacket.ReadInt32();
            Set.SecondaryWeaponSlot = _worldPacket.ReadInt32();

            bool hasSpecIndex = _worldPacket.HasBit();

            uint setNameLength = _worldPacket.ReadBits<uint>(8);
            uint setIconLength = _worldPacket.ReadBits<uint>(9);

            if (hasSpecIndex)
                Set.AssignedSpecIndex = _worldPacket.ReadInt32();

            Set.SetName = _worldPacket.ReadString(setNameLength);
            Set.SetIcon = _worldPacket.ReadString(setIconLength);
        }

        public EquipmentSetInfo.EquipmentSetData Set;
    }

    class DeleteEquipmentSet : ClientPacket
    {
        public DeleteEquipmentSet(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ID = _worldPacket.ReadUInt64();
        }

        public ulong ID;
    }

    class UseEquipmentSet : ClientPacket
    {
        public UseEquipmentSet(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);

            for (byte i = 0; i < EquipmentSlot.End; ++i)
            {
                Items[i].Item = _worldPacket.ReadPackedGuid();
                Items[i].ContainerSlot = _worldPacket.ReadUInt8();
                Items[i].Slot = _worldPacket.ReadUInt8();
            }

            GUID = _worldPacket.ReadUInt64();
        }

        public InvUpdate Inv;
        public EquipmentSetItem[] Items = new EquipmentSetItem[EquipmentSlot.End];
        public ulong GUID; //Set Identifier

        public struct EquipmentSetItem
        {
            public ObjectGuid Item;
            public byte ContainerSlot;
            public byte Slot;
        }
    }

    class UseEquipmentSetResult : ServerPacket
    {
        public UseEquipmentSetResult() : base(ServerOpcodes.UseEquipmentSetResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt64(GUID);
            _worldPacket.WriteUInt8(Reason);
        }

        public ulong GUID; //Set Identifier
        public byte Reason;
    }
}
