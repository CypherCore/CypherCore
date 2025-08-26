// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class UpdateTalentData : ServerPacket
    {
        public UpdateTalentData() : base(ServerOpcodes.UpdateTalentData, ConnectionType.Instance) { }

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
                _worldPacket.WriteInt32(talentGroupInfo.GlyphIDs.Count);

                foreach (var talentID in talentGroupInfo.TalentIDs)
                    _worldPacket.WriteUInt16(talentID);

                foreach (PvPTalent talent in talentGroupInfo.PvPTalents)
                    talent.Write(_worldPacket);

                foreach (uint talent in talentGroupInfo.GlyphIDs)
                    _worldPacket.WriteUInt16((ushort)talent);
            }
        }

        public TalentInfoUpdate Info = new();

        public class TalentGroupInfo
        {
            public uint SpecID;
            public List<ushort> TalentIDs = new();
            public List<PvPTalent> PvPTalents = new();
            public List<uint> GlyphIDs = new();
        }

        public class TalentInfoUpdate
        {
            public byte ActiveGroup;
            public uint PrimarySpecialization;
            public List<TalentGroupInfo> TalentGroups = new();
        }
    }

    class LearnTalents : ClientPacket
    {
        public LearnTalents(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint count = _worldPacket.ReadBits<uint>(6);
            for (int i = 0; i < count; ++i)
                Talents[i] = _worldPacket.ReadUInt16();
        }

        public Array<ushort> Talents = new(PlayerConst.MaxTalentTiers);
    }

    class RespecWipeConfirm : ServerPacket
    {
        public RespecWipeConfirm() : base(ServerOpcodes.RespecWipeConfirm) { }

        public override void Write()
        {
            _worldPacket.WriteInt8((sbyte)RespecType);
            _worldPacket.WriteUInt32(Cost);
            _worldPacket.WritePackedGuid(RespecMaster);
        }

        public ObjectGuid RespecMaster;
        public uint Cost;
        public SpecResetType RespecType;
    }

    class ConfirmRespecWipe : ClientPacket
    {
        public ConfirmRespecWipe(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RespecMaster = _worldPacket.ReadPackedGuid();
            RespecType = (SpecResetType)_worldPacket.ReadUInt8();
        }

        public ObjectGuid RespecMaster;
        public SpecResetType RespecType;
    }

    class LearnTalentFailed : ServerPacket
    {
        public LearnTalentFailed() : base(ServerOpcodes.LearnTalentFailed) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Reason, 4);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteInt32(Talents.Count);

            foreach (var talent in Talents)
                _worldPacket.WriteUInt16(talent);
        }

        public uint Reason;
        public int SpellID;
        public List<ushort> Talents = new();
    }

    class ActiveGlyphs : ServerPacket
    {
        public ActiveGlyphs() : base(ServerOpcodes.ActiveGlyphs) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Glyphs.Count);
            foreach (GlyphBinding glyph in Glyphs)
                glyph.Write(_worldPacket);

            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();
        }

        public List<GlyphBinding> Glyphs = new();
        public bool IsFullUpdate;
    }

    class LearnPvpTalents : ClientPacket
    {
        public LearnPvpTalents(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint size = _worldPacket.ReadUInt32();
            for (int i = 0; i < size; ++i)
                Talents[i] = new PvPTalent(_worldPacket);
        }

        public Array<PvPTalent> Talents = new(4);
    }

    class LearnPvpTalentFailed : ServerPacket
    {
        public LearnPvpTalentFailed() : base(ServerOpcodes.LearnPvpTalentFailed) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Reason, 4);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteInt32(Talents.Count);

            foreach (var pvpTalent in Talents)
                pvpTalent.Write(_worldPacket);
        }

        public uint Reason;
        public uint SpellID;
        public List<PvPTalent> Talents = new();
    }

    //Structs
    public struct PvPTalent
    {
        public ushort PvPTalentID;
        public byte Slot;

        public PvPTalent(WorldPacket data)
        {
            PvPTalentID = data.ReadUInt16();
            Slot = data.ReadUInt8();
        }

        public void Write(WorldPacket data)
        {
            data.WriteUInt16(PvPTalentID);
            data.WriteUInt8(Slot);
        }
    }

    struct GlyphBinding
    {
        uint SpellID;
        ushort GlyphID;

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
    }

    public struct ClassicTalentEntry
    {
        public int TalentID;
        public int Rank;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(TalentID);
            data.WriteInt32(Rank);
        }
    }

    public class ClassicTalentGroupInfo
    {
        public byte NumTalents;
        public List<ClassicTalentEntry> Talents = new();
        public byte NumGlyphs;
        public List<ushort> GlyphIDs = new();
        public sbyte Role;
        public int PrimarySpecialization;

        public void Write(WorldPacket data)
        {
            data.WriteUInt8(NumTalents);
            data.WriteInt32(Talents.Count);

            data.WriteUInt8(NumGlyphs);
            data.WriteInt32(GlyphIDs.Count);

            data.WriteInt8(Role);
            data.WriteInt32(PrimarySpecialization);

            foreach (ClassicTalentEntry talentEntry in Talents)
                talentEntry.Write(data);

            foreach (ushort id in GlyphIDs)
                data.WriteUInt16(id);
        }
    }

    public class ClassicTalentInfoUpdate
    {
        public int UnspentTalentPoints;
        public byte ActiveGroup;
        public bool IsPetTalents;
        public List<ClassicTalentGroupInfo> Talents = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(UnspentTalentPoints);
            data.WriteUInt8(ActiveGroup);
            data.WriteInt32(Talents.Count);

            foreach (ClassicTalentGroupInfo talents in Talents)
                talents.Write(data);

            data.WriteBit(IsPetTalents);
            data.FlushBits();
        }
    }
}
