// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	internal class DismissCritter : ClientPacket
	{
		public ObjectGuid CritterGUID;

		public DismissCritter(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			CritterGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class RequestPetInfo : ClientPacket
	{
		public RequestPetInfo(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
		}
	}

	internal class PetAbandon : ClientPacket
	{
		public ObjectGuid Pet;

		public PetAbandon(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			Pet = _worldPacket.ReadPackedGuid();
		}
	}

	internal class PetStopAttack : ClientPacket
	{
		public ObjectGuid PetGUID;

		public PetStopAttack(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PetGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class PetSpellAutocast : ClientPacket
	{
		public bool AutocastEnabled;

		public ObjectGuid PetGUID;
		public uint SpellID;

		public PetSpellAutocast(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PetGUID         = _worldPacket.ReadPackedGuid();
			SpellID         = _worldPacket.ReadUInt32();
			AutocastEnabled = _worldPacket.HasBit();
		}
	}

	public class PetSpells : ServerPacket
	{
		public uint[] ActionButtons = new uint[10];

		public List<uint> Actions = new();
		public CommandStates CommandState;
		public List<PetSpellCooldown> Cooldowns = new();
		public ushort CreatureFamily;
		public byte Flag;

		public ObjectGuid PetGUID;
		public ReactStates ReactState;
		public ushort Specialization;
		public List<PetSpellHistory> SpellHistory = new();
		public uint TimeLimit;

		public PetSpells() : base(ServerOpcodes.PetSpellsMessage, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(PetGUID);
			_worldPacket.WriteUInt16(CreatureFamily);
			_worldPacket.WriteUInt16(Specialization);
			_worldPacket.WriteUInt32(TimeLimit);
			_worldPacket.WriteUInt16((ushort)((byte)CommandState | (Flag << 16)));
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
	}

	internal class PetStableList : ServerPacket
	{
		public List<PetStableInfo> Pets = new();

		public ObjectGuid StableMaster;

		public PetStableList() : base(ServerOpcodes.PetStableList, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(StableMaster);

			_worldPacket.WriteInt32(Pets.Count);

			foreach (PetStableInfo pet in Pets)
			{
				_worldPacket.WriteUInt32(pet.PetSlot);
				_worldPacket.WriteUInt32(pet.PetNumber);
				_worldPacket.WriteUInt32(pet.CreatureID);
				_worldPacket.WriteUInt32(pet.DisplayID);
				_worldPacket.WriteUInt32(pet.ExperienceLevel);
				_worldPacket.WriteUInt8((byte)pet.PetFlags);
				_worldPacket.WriteBits(pet.PetName.GetByteCount(), 8);
				_worldPacket.WriteString(pet.PetName);
			}
		}
	}

	internal class PetStableResult : ServerPacket
	{
		public StableResult Result;

		public PetStableResult() : base(ServerOpcodes.PetStableResult, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8((byte)Result);
		}
	}

	internal class PetLearnedSpells : ServerPacket
	{
		public List<uint> Spells = new();

		public PetLearnedSpells() : base(ServerOpcodes.PetLearnedSpells, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Spells.Count);

			foreach (uint spell in Spells)
				_worldPacket.WriteUInt32(spell);
		}
	}

	internal class PetUnlearnedSpells : ServerPacket
	{
		public List<uint> Spells = new();

		public PetUnlearnedSpells() : base(ServerOpcodes.PetUnlearnedSpells, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Spells.Count);

			foreach (uint spell in Spells)
				_worldPacket.WriteUInt32(spell);
		}
	}

	internal class PetNameInvalid : ServerPacket
	{
		public PetRenameData RenameData;
		public PetNameInvalidReason Result;

		public PetNameInvalid() : base(ServerOpcodes.PetNameInvalid)
		{
		}

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
	}

	internal class PetRename : ClientPacket
	{
		public PetRenameData RenameData;

		public PetRename(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			RenameData.PetGUID   = _worldPacket.ReadPackedGuid();
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
	}

	internal class PetAction : ClientPacket
	{
		public uint Action;
		public Vector3 ActionPosition;

		public ObjectGuid PetGUID;
		public ObjectGuid TargetGUID;

		public PetAction(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PetGUID = _worldPacket.ReadPackedGuid();

			Action     = _worldPacket.ReadUInt32();
			TargetGUID = _worldPacket.ReadPackedGuid();

			ActionPosition = _worldPacket.ReadVector3();
		}
	}

	internal class PetSetAction : ClientPacket
	{
		public uint Action;
		public uint Index;

		public ObjectGuid PetGUID;

		public PetSetAction(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PetGUID = _worldPacket.ReadPackedGuid();

			Index  = _worldPacket.ReadUInt32();
			Action = _worldPacket.ReadUInt32();
		}
	}

	internal class CancelModSpeedNoControlAuras : ClientPacket
	{
		public ObjectGuid TargetGUID;

		public CancelModSpeedNoControlAuras(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			TargetGUID = _worldPacket.ReadPackedGuid();
		}
	}

	internal class PetCancelAura : ClientPacket
	{
		public ObjectGuid PetGUID;
		public uint SpellID;

		public PetCancelAura(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			PetGUID = _worldPacket.ReadPackedGuid();
			SpellID = _worldPacket.ReadUInt32();
		}
	}

	internal class SetPetSpecialization : ServerPacket
	{
		public ushort SpecID;

		public SetPetSpecialization() : base(ServerOpcodes.SetPetSpecialization)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt16(SpecID);
		}
	}

	internal class PetActionFeedbackPacket : ServerPacket
	{
		public PetActionFeedback Response;

		public uint SpellID;

		public PetActionFeedbackPacket() : base(ServerOpcodes.PetStableResult)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt32(SpellID);
			_worldPacket.WriteUInt8((byte)Response);
		}
	}

	internal class PetActionSound : ServerPacket
	{
		public PetTalk Action;

		public ObjectGuid UnitGUID;

		public PetActionSound() : base(ServerOpcodes.PetStableResult)
		{
		}

		public override void Write()
		{
			_worldPacket.WritePackedGuid(UnitGUID);
			_worldPacket.WriteUInt32((uint)Action);
		}
	}

	internal class PetTameFailure : ServerPacket
	{
		public byte Result;

		public PetTameFailure() : base(ServerOpcodes.PetTameFailure)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8(Result);
		}
	}

	//Structs
	public class PetSpellCooldown
	{
		public ushort Category;
		public uint CategoryDuration;
		public uint Duration;
		public float ModRate = 1.0f;
		public uint SpellID;
	}

	public class PetSpellHistory
	{
		public uint CategoryID;
		public float ChargeModRate = 1.0f;
		public sbyte ConsumedCharges;
		public uint RecoveryTime;
	}

	internal struct PetStableInfo
	{
		public uint PetSlot;
		public uint PetNumber;
		public uint CreatureID;
		public uint DisplayID;
		public uint ExperienceLevel;
		public PetStableinfo PetFlags;
		public string PetName;
	}

	internal struct PetRenameData
	{
		public ObjectGuid PetGUID;
		public int PetNumber;
		public string NewName;
		public bool HasDeclinedNames;
		public DeclinedName DeclinedNames;
	}
}