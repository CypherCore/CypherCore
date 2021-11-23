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
using Game.Entities;
using System;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class BattlePetJournal : ServerPacket
    {
        public BattlePetJournal() : base(ServerOpcodes.BattlePetJournal) { }

        public override void Write()
        {
            _worldPacket.WriteUInt16(Trap);
            _worldPacket.WriteInt32(Slots.Count);
            _worldPacket.WriteInt32(Pets.Count);
            _worldPacket.WriteBit(HasJournalLock);
            _worldPacket.FlushBits();

            foreach (var slot in Slots)
                slot.Write(_worldPacket);

            foreach (var pet in Pets)
                pet.Write(_worldPacket);
        }

        public ushort Trap;
        public bool HasJournalLock = false;
        public List<BattlePetSlot> Slots = new();
        public List<BattlePetStruct> Pets = new();
    }

    class BattlePetJournalLockAcquired : ServerPacket
    {
        public BattlePetJournalLockAcquired() : base(ServerOpcodes.BattlePetJournalLockAcquired) { }

        public override void Write() { }
    }

    class BattlePetJournalLockDenied : ServerPacket
    {
        public BattlePetJournalLockDenied() : base(ServerOpcodes.BattlePetJournalLockDenied) { }

        public override void Write() { }
    }
    
    class BattlePetRequestJournal : ClientPacket
    {
        public BattlePetRequestJournal(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class BattlePetRequestJournalLock : ClientPacket
    {
        public BattlePetRequestJournalLock(WorldPacket packet) : base(packet) { }

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

        public List<BattlePetStruct> Pets = new();
        public bool PetAdded;
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

        public List<BattlePetSlot> Slots = new();
        public bool AutoSlotted;
        public bool NewSlot;
    }

    class BattlePetSetBattleSlot : ClientPacket
    {
        public BattlePetSetBattleSlot(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
            Slot = _worldPacket.ReadUInt8();
        }

        public ObjectGuid PetGuid;
        public byte Slot;
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
                DeclinedName.Set(new DeclinedName());

                byte[] declinedNameLengths = new byte[SharedConst.MaxDeclinedNameCases];

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    declinedNameLengths[i] = _worldPacket.ReadBits<byte>(7);

                for (byte i = 0; i < SharedConst.MaxDeclinedNameCases; ++i)
                    DeclinedName.Value.name[i] = _worldPacket.ReadString(declinedNameLengths[i]);
            }

            Name = _worldPacket.ReadString(nameLength);
        }

        public ObjectGuid PetGuid;
        public string Name;
        public Optional<DeclinedName> DeclinedName;
    }

    class BattlePetDeletePet : ClientPacket
    {
        public BattlePetDeletePet(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid;
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

        public ObjectGuid PetGuid;
        public uint Flags;
        public FlagsControlType ControlType;
    }

    class BattlePetClearFanfare : ClientPacket
    {
        public BattlePetClearFanfare(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid;
    }
    
    class CageBattlePet : ClientPacket
    {
        public CageBattlePet(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid;
    }

    class BattlePetDeleted : ServerPacket
    {
        public BattlePetDeleted() : base(ServerOpcodes.BattlePetDeleted) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PetGuid);
        }

        public ObjectGuid PetGuid;
    }

    class BattlePetErrorPacket : ServerPacket
    {
        public BattlePetErrorPacket() : base(ServerOpcodes.BattlePetError) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Result, 4);
            _worldPacket.WriteUInt32(CreatureID);
        }

        public BattlePetError Result;
        public uint CreatureID;
    }

    class BattlePetSummon : ClientPacket
    {
        public BattlePetSummon(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGuid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGuid;
    }


    //Structs
    public struct BattlePetStruct
    {
        public void Write(WorldPacket data)
        {
            data .WritePackedGuid( Guid);
            data.WriteUInt32(Species);
            data.WriteUInt32(CreatureID);
            data.WriteUInt32(DisplayID);
            data.WriteUInt16(Breed);
            data.WriteUInt16(Level);
            data.WriteUInt16(Exp);
            data.WriteUInt16(Flags);
            data.WriteUInt32(Power);
            data.WriteUInt32(Health);
            data.WriteUInt32(MaxHealth);
            data .WriteUInt32( Speed);
            data .WriteUInt8( Quality);
            data.WriteBits(Name.GetByteCount(), 7);
            data.WriteBit(OwnerInfo.HasValue); // HasOwnerInfo
            data.WriteBit(false); // NoRename
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
            public ObjectGuid Guid;
            public uint PlayerVirtualRealm;
            public uint PlayerNativeRealm;
        }

        public ObjectGuid Guid;
        public uint Species;
        public uint CreatureID;
        public uint DisplayID;
        public ushort Breed;
        public ushort Level;
        public ushort Exp;
        public ushort Flags;
        public uint Power;
        public uint Health;
        public uint MaxHealth;
        public uint Speed;
        public byte Quality;
        public Optional<BattlePetOwnerInfo> OwnerInfo;
        public string Name;
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

        public BattlePetStruct Pet;
        public uint CollarID;
        public byte Index;
        public bool Locked = true;
    }
}
