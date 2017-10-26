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
using Framework.GameMath;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class DismissCritter : ClientPacket
    {
        public DismissCritter(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CritterGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid CritterGUID { get; set; }
    }

    class RequestPetInfo : ClientPacket
    {
        public RequestPetInfo(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class PetAbandon : ClientPacket
    {
        public PetAbandon(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Pet = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Pet { get; set; }
    }

    class PetStopAttack : ClientPacket
    {
        public PetStopAttack(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGUID { get; set; }
    }

    class PetSpellAutocast : ClientPacket
    {
        public PetSpellAutocast(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();
            SpellID = _worldPacket.ReadUInt32();
            AutocastEnabled = _worldPacket.HasBit();
        }

        public ObjectGuid PetGUID { get; set; }
        public uint SpellID { get; set; }
        public bool AutocastEnabled { get; set; }
    }

    public class PetSpells : ServerPacket
    {
        public PetSpells() : base(ServerOpcodes.PetSpellsMessage, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PetGUID);
            _worldPacket.WriteUInt16(CreatureFamily);
            _worldPacket.WriteUInt16(Specialization);
            _worldPacket.WriteUInt32(TimeLimit);
            _worldPacket.WriteUInt16((ushort)((byte)CommandState | (Flag << 16)));
            _worldPacket.WriteUInt8(ReactState);

            foreach (uint actionButton in ActionButtons)
                _worldPacket.WriteUInt32(actionButton);

            _worldPacket.WriteUInt32(Actions.Count);
            _worldPacket.WriteUInt32(Cooldowns.Count);
            _worldPacket.WriteUInt32(SpellHistory.Count);

            foreach (uint action in Actions)
                _worldPacket.WriteUInt32(action);

            foreach (PetSpellCooldown cooldown in Cooldowns)
            {
                _worldPacket.WriteUInt32(cooldown.SpellID);
                _worldPacket.WriteUInt32(cooldown.Duration);
                _worldPacket.WriteUInt32(cooldown.CategoryDuration);
                _worldPacket.WriteFloat(cooldown.ModRate);
                _worldPacket.WriteUInt16(cooldown.Category);
            }

            foreach (PetSpellHistory history in SpellHistory)
            {
                _worldPacket.WriteInt32(history.CategoryID);
                _worldPacket.WriteInt32(history.RecoveryTime);
                _worldPacket.WriteFloat(history.ChargeModRate);
                _worldPacket.WriteInt8(history.ConsumedCharges);
            }
        }

        public ObjectGuid PetGUID { get; set; }
        public ushort CreatureFamily { get; set; }
        public ushort Specialization { get; set; }
        public uint TimeLimit { get; set; }
        public ReactStates ReactState { get; set; }
        public CommandStates CommandState { get; set; }
        public byte Flag { get; set; }

        public uint[] ActionButtons { get; set; } = new uint[10];

        public List<uint> Actions { get; set; } = new List<uint>();
        public List<PetSpellCooldown> Cooldowns { get; set; } = new List<PetSpellCooldown>();
        public List<PetSpellHistory> SpellHistory { get; set; } = new List<PetSpellHistory>();
    }

    class PetStableList : ServerPacket
    {
        public PetStableList() : base(ServerOpcodes.PetStableList, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(StableMaster);

            _worldPacket.WriteUInt32(Pets.Count);
            foreach (PetStableInfo pet in Pets)
            {
                _worldPacket.WriteUInt32(pet.PetSlot);
                _worldPacket.WriteUInt32(pet.PetNumber);
                _worldPacket.WriteUInt32(pet.CreatureID);
                _worldPacket.WriteUInt32(pet.DisplayID);
                _worldPacket.WriteUInt32(pet.ExperienceLevel);
                _worldPacket.WriteUInt32(pet.PetFlags);

                _worldPacket.WriteUInt8(pet.PetName.Length);
                _worldPacket.WriteString(pet.PetName);
            }
        }

        public ObjectGuid StableMaster { get; set; }
        public List<PetStableInfo> Pets { get; set; } = new List<PetStableInfo>();
    }

    class PetLearnedSpells : ServerPacket
    {
        public PetLearnedSpells() : base(ServerOpcodes.PetLearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket .WriteUInt32(Spells.Count);
            foreach (uint spell in Spells)
                _worldPacket.WriteUInt32(spell);
        }

        public List<uint> Spells { get; set; } = new List<uint>();
    }

    class PetUnlearnedSpells : ServerPacket
    {
        public PetUnlearnedSpells() : base(ServerOpcodes.PetUnlearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Spells);
            foreach (uint spell in Spells)
                _worldPacket.WriteUInt32(spell);
        }

        public List<uint> Spells { get; set; } = new List<uint>();
    }

    class PetNameInvalid : ServerPacket
    {
        public PetNameInvalid() : base(ServerOpcodes.PetNameInvalid) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(RenameData.PetGUID);
            _worldPacket.WriteInt32(RenameData.PetNumber);

            _worldPacket.WriteUInt8(RenameData.NewName.Length);

            _worldPacket.WriteBit(RenameData.HasDeclinedNames);
            _worldPacket.FlushBits();

            if (RenameData.HasDeclinedNames)
            {
                for (int i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                {
                    _worldPacket.WriteBits(RenameData.DeclinedNames.name[i].Length, 7);
                    _worldPacket.FlushBits();
                }

                for (int i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                    _worldPacket.WriteString(RenameData.DeclinedNames.name[i]);
            }

            _worldPacket.WriteString(RenameData.NewName);
        }

        public PetRenameData RenameData;
        public PetNameInvalidReason Result { get; set; }
    }

    class PetRename : ClientPacket
    {
        public PetRename(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RenameData.PetGUID = _worldPacket.ReadPackedGuid();
            RenameData.PetNumber = _worldPacket.ReadInt32();

            uint nameLen = _worldPacket.ReadUInt8();

            RenameData.HasDeclinedNames = _worldPacket.HasBit();
            if (RenameData.HasDeclinedNames)
            {
                RenameData.DeclinedNames = new DeclinedName();
                uint[] count = new uint[SharedConst.MaxDeclinedNameCases];
                for (int i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                    count[i] = _worldPacket.ReadBits<uint>(7);

                for (int i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                    RenameData.DeclinedNames.name[i] = _worldPacket.ReadString(count[i]);
            }

            RenameData.NewName = _worldPacket.ReadString(nameLen);
        }

        public PetRenameData RenameData;
    }

    class PetAction : ClientPacket
    {
        public PetAction(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();

            Action = _worldPacket.ReadUInt32();
            TargetGUID = _worldPacket.ReadPackedGuid();

            ActionPosition = _worldPacket.ReadVector3();
        }

        public ObjectGuid PetGUID { get; set; }
        public uint Action { get; set; }
        public ObjectGuid TargetGUID { get; set; }
        public Vector3 ActionPosition { get; set; }
    }

    class PetSetAction : ClientPacket
    {
        public PetSetAction(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();

            Index = _worldPacket.ReadUInt32();
            Action = _worldPacket.ReadUInt32();
        }

        public ObjectGuid PetGUID { get; set; }
        public uint Index { get; set; }
        public uint Action { get; set; }
    }

    class PetActionSound : ServerPacket
    {
        public PetActionSound() : base(ServerOpcodes.PetStableResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteUInt32(Action);
        }

        public ObjectGuid UnitGUID { get; set; }
        public PetTalk Action { get; set; }
    }

    class PetActionFeedback : ServerPacket
    {
        public PetActionFeedback() : base(ServerOpcodes.PetStableResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt8(Response);
        }

        public uint SpellID { get; set; }
        public ActionFeedback Response { get; set; }
    }

    class PetCancelAura : ClientPacket
    {
        public PetCancelAura(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();
            SpellID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid PetGUID { get; set; }
        public uint SpellID { get; set; }
    }


    class PetStableResult : ServerPacket
    {
        public PetStableResult(byte result) : base(ServerOpcodes.PetStableResult)
        {
            Result = result;
        }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Result);
        }

        public byte Result { get; set; }
    }

    class SetPetSpecialization : ServerPacket
    {
        public SetPetSpecialization() : base(ServerOpcodes.SetPetSpecialization) { }

        public override void Write()
        {
            _worldPacket.WriteUInt16(SpecID);
        }

        public ushort SpecID { get; set; }
    }

    //Structs
    public class PetSpellCooldown
    {
        public uint SpellID { get; set; }
        public uint Duration { get; set; }
        public uint CategoryDuration { get; set; }
        public float ModRate { get; set; } = 1.0f;
        public ushort Category { get; set; }
    }

    public class PetSpellHistory
    {
        public uint CategoryID { get; set; }
        public uint RecoveryTime { get; set; }
        public float ChargeModRate { get; set; } = 1.0f;
        public sbyte ConsumedCharges { get; set; }
    }

    struct PetStableInfo
    {
        public uint PetSlot { get; set; }
        public uint PetNumber { get; set; }
        public uint CreatureID { get; set; }
        public uint DisplayID { get; set; }
        public uint ExperienceLevel { get; set; }
        public PetStableinfo PetFlags { get; set; }
        public string PetName { get; set; }
    }

    struct PetRenameData
    {
        public ObjectGuid PetGUID { get; set; }
        public int PetNumber { get; set; }
        public string NewName { get; set; }
        public bool HasDeclinedNames { get; set; }
        public DeclinedName DeclinedNames { get; set; }
    }
}
