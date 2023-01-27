// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
	internal class UpdateTalentData : ServerPacket
	{
		public TalentInfoUpdate Info = new();

		public UpdateTalentData() : base(ServerOpcodes.UpdateTalentData, ConnectionType.Instance)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteUInt8(Info.ActiveGroup);
			_worldPacket.WriteUInt32(Info.PrimarySpecialization);
			_worldPacket.WriteInt32(Info.TalentGroups.Count);

			foreach (var talentGroupInfo in Info.TalentGroups)
			{
				_worldPacket.WriteUInt32(talentGroupInfo.SpecID);
				_worldPacket.WriteInt32(talentGroupInfo.TalentIDs.Count);
				_worldPacket.WriteInt32(talentGroupInfo.PvPTalents.Count);

				foreach (var talentID in talentGroupInfo.TalentIDs)
					_worldPacket.WriteUInt16(talentID);

				foreach (PvPTalent talent in talentGroupInfo.PvPTalents)
					talent.Write(_worldPacket);
			}
		}

		public class TalentGroupInfo
		{
			public List<PvPTalent> PvPTalents = new();
			public uint SpecID;
			public List<ushort> TalentIDs = new();
		}

		public class TalentInfoUpdate
		{
			public byte ActiveGroup;
			public uint PrimarySpecialization;
			public List<TalentGroupInfo> TalentGroups = new();
		}
	}

	internal class LearnTalents : ClientPacket
	{
		public Array<ushort> Talents = new(PlayerConst.MaxTalentTiers);

		public LearnTalents(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			uint count = _worldPacket.ReadBits<uint>(6);

			for (int i = 0; i < count; ++i)
				Talents[i] = _worldPacket.ReadUInt16();
		}
	}

	internal class RespecWipeConfirm : ServerPacket
	{
		public uint Cost;

		public ObjectGuid RespecMaster;
		public SpecResetType RespecType;

		public RespecWipeConfirm() : base(ServerOpcodes.RespecWipeConfirm)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt8((sbyte)RespecType);
			_worldPacket.WriteUInt32(Cost);
			_worldPacket.WritePackedGuid(RespecMaster);
		}
	}

	internal class ConfirmRespecWipe : ClientPacket
	{
		public ObjectGuid RespecMaster;
		public SpecResetType RespecType;

		public ConfirmRespecWipe(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			RespecMaster = _worldPacket.ReadPackedGuid();
			RespecType   = (SpecResetType)_worldPacket.ReadUInt8();
		}
	}

	internal class LearnTalentFailed : ServerPacket
	{
		public uint Reason;
		public int SpellID;
		public List<ushort> Talents = new();

		public LearnTalentFailed() : base(ServerOpcodes.LearnTalentFailed)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBits(Reason, 4);
			_worldPacket.WriteInt32(SpellID);
			_worldPacket.WriteInt32(Talents.Count);

			foreach (var talent in Talents)
				_worldPacket.WriteUInt16(talent);
		}
	}

	internal class ActiveGlyphs : ServerPacket
	{
		public List<GlyphBinding> Glyphs = new();
		public bool IsFullUpdate;

		public ActiveGlyphs() : base(ServerOpcodes.ActiveGlyphs)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteInt32(Glyphs.Count);

			foreach (GlyphBinding glyph in Glyphs)
				glyph.Write(_worldPacket);

			_worldPacket.WriteBit(IsFullUpdate);
			_worldPacket.FlushBits();
		}
	}

	internal class LearnPvpTalents : ClientPacket
	{
		public Array<PvPTalent> Talents = new(4);

		public LearnPvpTalents(WorldPacket packet) : base(packet)
		{
		}

		public override void Read()
		{
			uint size = _worldPacket.ReadUInt32();

			for (int i = 0; i < size; ++i)
				Talents[i] = new PvPTalent(_worldPacket);
		}
	}

	internal class LearnPvpTalentFailed : ServerPacket
	{
		public uint Reason;
		public uint SpellID;
		public List<PvPTalent> Talents = new();

		public LearnPvpTalentFailed() : base(ServerOpcodes.LearnPvpTalentFailed)
		{
		}

		public override void Write()
		{
			_worldPacket.WriteBits(Reason, 4);
			_worldPacket.WriteUInt32(SpellID);
			_worldPacket.WriteInt32(Talents.Count);

			foreach (var pvpTalent in Talents)
				pvpTalent.Write(_worldPacket);
		}
	}

	//Structs
	public struct PvPTalent
	{
		public ushort PvPTalentID;
		public byte Slot;

		public PvPTalent(WorldPacket data)
		{
			PvPTalentID = data.ReadUInt16();
			Slot        = data.ReadUInt8();
		}

		public void Write(WorldPacket data)
		{
			data.WriteUInt16(PvPTalentID);
			data.WriteUInt8(Slot);
		}
	}

	internal struct GlyphBinding
	{
		public GlyphBinding(uint spellId, ushort glyphId)
		{
			SpellID = spellId;
			GlyphID = glyphId;
		}

		public void Write(WorldPacket data)
		{
			data.WriteUInt32(SpellID);
			data.WriteUInt16(GlyphID);
		}

		private uint SpellID;
		private ushort GlyphID;
	}
}