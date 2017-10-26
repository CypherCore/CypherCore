/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class BattlePetJournal : ServerPacket
    {
        public BattlePetJournal() : base(ServerOpcodes.BattlePetJournal) { }

        public override void Write()
        {
            _worldPacket.WriteUInt16(Trap);
            _worldPacket.WriteInt32(Slots.Count);
            _worldPacket.WriteInt32(Pets.Count);
            _worldPacket.WriteInt32(MaxPets);
            _worldPacket.WriteBit(HasJournalLock);
            _worldPacket.FlushBits();

            foreach (var slot in Slots)
                slot.Write(_worldPacket);

            foreach (var pet in Pets)
                pet.Write(_worldPacket);
        }

        public ushort Trap { get; set; }
        bool HasJournalLock = true;
        public List<BattlePetSlot> Slots { get; set; } = new List<BattlePetSlot>();
        public List<BattlePetStruct> Pets { get; set; } = new List<BattlePetStruct>();
        int MaxPets = 1000;
    }

    class BattlePetJournalLockAcquired : ServerPacket
    {
        public BattlePetJournalLockAcquired() : base(ServerOpcodes.BattlePetJournalLockAcquired) { }

        public override void Write() { }
    }

    class BattlePetRequestJournal : ClientPacket
    {
        public BattlePetRequestJournal(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class BattlePetUpdates : ServerPacket
    {
        public BattlePetUpdates() : base(ServerOpcodes.BattlePetUpdates) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Pets.Count);
            _worldPacket.WriteBit(PetAdded);
            _worldPacket.FlushBits();

            foreach (var pet in Pets)
                pet.Write(_worldPacket);
        }

        public List<BattlePetStruct> Pets { get; set; } = new List<BattlePetStruct>();
        public bool PetAdded { get; set; }
    }

    class PetBattleSlotUpdates : ServerPacket
    {
        public PetBattleSlotUpdates() : base(ServerOpcodes.PetBattleSlotUpdates) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Slots.Count);
            _worldPacket.WriteBit(NewSlot);
            _worldPacket.WriteBit(AutoSlotted);
            _worldPacket.FlushBits();

            foreach (var slot in Slots)
                slot.Write(_worldPacket);
        }

        public List<BattlePetSlot> Slots { get; set; } = new List<BattlePetSlot>();
        public bool AutoSlotted { get; set; }
        public bool NewSlot { get; set; }
    }

    class BattlePetSetBattleSlot : ClientPacket
    {
        public BattlePetSetBattleSlot(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
            Slot = _worldPacket.ReadUInt8();
        }

        public ObjectGuid PetGuid { get; set; }
        public byte Slot { get; set; }
    }

    class BattlePetModifyName : ClientPacket
    {
        public BattlePetModifyName(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
            uint nameLength = _worldPacket.ReadBits<uint>(7);

            if (_worldPacket.HasBit())
            {
                Declined = new DeclinedName();

                byte[] declinedNameLengths = new byte[SharedConst.MaxDeclinedNameCases];

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    declinedNameLengths[i] = _worldPacket.ReadBits<byte>(7);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    Declined.name[i] = _worldPacket.ReadString(declinedNameLengths[i]);
            }

            Name = _worldPacket.ReadString(nameLength);
        }

        public ObjectGuid PetGuid { get; set; }
        public string Name { get; set; }
        public DeclinedName Declined { get; set; }
    }

    class BattlePetDeletePet : ClientPacket
    {
        public BattlePetDeletePet(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid { get; set; }
    }

    class BattlePetSetFlags : ClientPacket
    {
        public BattlePetSetFlags(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
            Flags = _worldPacket.ReadUInt32();
            ControlType = (FlagsControlType)_worldPacket.ReadBits<byte>(2);
        }

        public ObjectGuid PetGuid { get; set; }
        public uint Flags { get; set; }
        public FlagsControlType ControlType { get; set; }
    }

    class CageBattlePet : ClientPacket
    {
        public CageBattlePet(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid { get; set; }
    }

    class BattlePetDeleted : ServerPacket
    {
        public BattlePetDeleted() : base(ServerOpcodes.BattlePetDeleted) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PetGuid);
        }

        public ObjectGuid PetGuid { get; set; }
    }

    class BattlePetErrorPacket : ServerPacket
    {
        public BattlePetErrorPacket() : base(ServerOpcodes.BattlePetError) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Result, 3);
            _worldPacket.WriteUInt32(CreatureID);
        }

        public BattlePetError Result { get; set; }
        public uint CreatureID { get; set; }
    }

    class BattlePetSummon : ClientPacket
    {
        public BattlePetSummon(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid { get; set; }
    }


    //Structs
    public struct BattlePetStruct
    {
        public void Write(WorldPacket data)
        {
            data .WritePackedGuid( Guid);
            data.WriteUInt32(Species);
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(CollarID);
            data.WriteUInt16(Breed);
            data.WriteUInt16(Level);
            data.WriteUInt16(Exp);
            data.WriteUInt16(Flags);
            data.WriteUInt32(Power);
            data.WriteUInt32(Health);
            data.WriteUInt32(MaxHealth);
            data .WriteUInt32( Speed);
            data .WriteUInt8( Quality);
            data.WriteBits(Name.Length, 7);
            data.WriteBit(OwnerInfo.HasValue); // HasOwnerInfo
            data.WriteBit(Name.IsEmpty()); // NoRename
            data.FlushBits();

            data.WriteString(Name);

            if (OwnerInfo.HasValue)
            {
                data.WritePackedGuid(OwnerInfo.Value.Guid);
                data.WriteUInt32(OwnerInfo.Value.PlayerVirtualRealm); // Virtual
                data.WriteUInt32(OwnerInfo.Value.PlayerNativeRealm); // Native
            }
        }

        public struct BattlePetOwnerInfo
        {
            public ObjectGuid Guid { get; set; }
            public uint PlayerVirtualRealm { get; set; }
            public uint PlayerNativeRealm { get; set; }
        }

        public ObjectGuid Guid { get; set; }
        public uint Species { get; set; }
        public uint CreatureID { get; set; }
        public uint CollarID; // what's this?
        public ushort Breed { get; set; }
        public ushort Level { get; set; }
        public ushort Exp { get; set; }
        public ushort Flags { get; set; }
        public uint Power { get; set; }
        public uint Health { get; set; }
        public uint MaxHealth { get; set; }
        public uint Speed { get; set; }
        public byte Quality { get; set; }
        public Optional<BattlePetOwnerInfo> OwnerInfo { get; set; }
        public string Name { get; set; }
    }

    public class BattlePetSlot
    {
        public void Write(WorldPacket data)
        {
            data .WritePackedGuid(Pet.Guid.IsEmpty() ? ObjectGuid.Create(HighGuid.BattlePet, 0) : Pet.Guid);
            data .WriteUInt32( CollarID);
            data .WriteUInt8( Index);
            data.WriteBit(Locked);
            data.FlushBits();
        }

        public BattlePetStruct Pet { get; set; }
        public uint CollarID; // what's this?
        public byte Index { get; set; }
        public bool Locked { get; set; } = true;
    }
}
