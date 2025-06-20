﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Game.Networking.Packets
{
    class DismissCritter : ClientPacket
    {
        public DismissCritter(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            CritterGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid CritterGUID;
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

        public ObjectGuid Pet;
    }

    class PetAbandonByNumber : ClientPacket
    {
        public uint PetNumber;

        public PetAbandonByNumber(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetNumber = _worldPacket.ReadUInt32();
        }
    }

    class PetStopAttack : ClientPacket
    {
        public PetStopAttack(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid PetGUID;
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

        public ObjectGuid PetGUID;
        public uint SpellID;
        public bool AutocastEnabled;
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
            _worldPacket.WriteUInt8((byte)CommandState);
            _worldPacket.WriteUInt8(Flag);
            _worldPacket.WriteUInt8((byte)ReactState);

            foreach (uint actionButton in ActionButtons)
                _worldPacket.WriteUInt32(actionButton);

            _worldPacket.WriteInt32(Actions.Count);
            _worldPacket.WriteInt32(Cooldowns.Count);
            _worldPacket.WriteInt32(SpellHistory.Count);

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
                _worldPacket.WriteUInt32(history.CategoryID);
                _worldPacket.WriteUInt32(history.RecoveryTime);
                _worldPacket.WriteFloat(history.ChargeModRate);
                _worldPacket.WriteInt8(history.ConsumedCharges);
            }
        }

        public ObjectGuid PetGUID;
        public ushort CreatureFamily;
        public ushort Specialization;
        public uint TimeLimit;
        public ReactStates ReactState;
        public CommandStates CommandState;
        public byte Flag;

        public uint[] ActionButtons = new uint[10];

        public List<uint> Actions = new();
        public List<PetSpellCooldown> Cooldowns = new();
        public List<PetSpellHistory> SpellHistory = new();
    }

    class PetStableResult : ServerPacket
    {
        public PetStableResult() : base(ServerOpcodes.PetStableResult, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Result);
        }

        public StableResult Result;
    }

    class PetLearnedSpells : ServerPacket
    {
        public PetLearnedSpells() : base(ServerOpcodes.PetLearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Spells.Count);
            foreach (uint spell in Spells)
                _worldPacket.WriteUInt32(spell);
        }

        public List<uint> Spells = new();
    }

    class PetUnlearnedSpells : ServerPacket
    {
        public PetUnlearnedSpells() : base(ServerOpcodes.PetUnlearnedSpells, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Spells.Count);
            foreach (uint spell in Spells)
                _worldPacket.WriteUInt32(spell);
        }

        public List<uint> Spells = new();
    }

    class PetNameInvalid : ServerPacket
    {
        public PetNameInvalid() : base(ServerOpcodes.PetNameInvalid) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8((byte)Result);
            _worldPacket.WritePackedGuid(RenameData.PetGUID);
            _worldPacket.WriteInt32(RenameData.PetNumber);

            _worldPacket.WriteUInt8((byte)RenameData.NewName.GetByteCount());

            _worldPacket.WriteBit(RenameData.HasDeclinedNames);

            if (RenameData.HasDeclinedNames)
            {
                for (int i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                    _worldPacket.WriteBits(RenameData.DeclinedNames.name[i].GetByteCount(), 7);

                for (int i = 0; i < SharedConst.MaxDeclinedNameCases; i++)
                    _worldPacket.WriteString(RenameData.DeclinedNames.name[i]);
            }

            _worldPacket.WriteString(RenameData.NewName);
        }

        public PetRenameData RenameData;
        public PetNameInvalidReason Result;
    }

    class PetRename : ClientPacket
    {
        public PetRename(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RenameData.PetGUID = _worldPacket.ReadPackedGuid();
            RenameData.PetNumber = _worldPacket.ReadInt32();

            uint nameLen = _worldPacket.ReadBits<uint>(8);

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

        public ObjectGuid PetGUID;
        public uint Action;
        public ObjectGuid TargetGUID;
        public Vector3 ActionPosition;
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

        public ObjectGuid PetGUID;
        public uint Index;
        public uint Action;
    }

    class CancelModSpeedNoControlAuras : ClientPacket
    {
        public ObjectGuid TargetGUID;

        public CancelModSpeedNoControlAuras(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TargetGUID = _worldPacket.ReadPackedGuid();
        }
    }

    class PetCancelAura : ClientPacket
    {
        public PetCancelAura(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            PetGUID = _worldPacket.ReadPackedGuid();
            SpellID = _worldPacket.ReadUInt32();
        }

        public ObjectGuid PetGUID;
        public uint SpellID;
    }

    class SetPetSpecialization : ServerPacket
    {
        public SetPetSpecialization() : base(ServerOpcodes.SetPetSpecialization) { }

        public override void Write()
        {
            _worldPacket.WriteUInt16(SpecID);
        }

        public ushort SpecID;
    }

    class PetActionFeedbackPacket : ServerPacket
    {
        public PetActionFeedbackPacket() : base(ServerOpcodes.PetStableResult) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt8((byte)Response);
        }

        public uint SpellID;
        public PetActionFeedback Response;
    }

    class PetActionSound : ServerPacket
    {
        public PetActionSound() : base(ServerOpcodes.PetStableResult) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(UnitGUID);
            _worldPacket.WriteUInt32((uint)Action);
        }

        public ObjectGuid UnitGUID;
        public PetTalk Action;
    }

    class PetTameFailure : ServerPacket
    {
        public PetTameFailure() : base(ServerOpcodes.PetTameFailure) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Result);
        }

        public byte Result;
    }

    class PetMode : ServerPacket
    {
        public ObjectGuid PetGUID;
        public ReactStates ReactState;
        public CommandStates CommandState;
        public byte Flag;

        public PetMode() : base(ServerOpcodes.PetMode, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(PetGUID);
            _worldPacket.WriteUInt8((byte)CommandState);
            _worldPacket.WriteUInt8(Flag);
            _worldPacket.WriteUInt8((byte)ReactState);
        }
    }

    //Structs
    public class PetSpellCooldown
    {
        public uint SpellID;
        public uint Duration;
        public uint CategoryDuration;
        public float ModRate = 1.0f;
        public ushort Category;
    }

    public class PetSpellHistory
    {
        public uint CategoryID;
        public uint RecoveryTime;
        public float ChargeModRate = 1.0f;
        public sbyte ConsumedCharges;
    }

    struct PetRenameData
    {
        public ObjectGuid PetGUID;
        public int PetNumber;
        public string NewName;
        public bool HasDeclinedNames;
        public DeclinedName DeclinedNames;
    }
}
