﻿/*
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

                foreach (var talentID in talentGroupInfo.TalentIDs)
                    _worldPacket.WriteUInt16(talentID);

                foreach (var talent in talentGroupInfo.PvPTalents)
                    talent.Write(_worldPacket);
            }
        }

        public TalentInfoUpdate Info = new TalentInfoUpdate();

        public class TalentGroupInfo
        {
            public uint SpecID;
            public List<ushort> TalentIDs = new List<ushort>();
            public List<PvPTalent> PvPTalents = new List<PvPTalent>();
        }

        public class TalentInfoUpdate
        {
            public byte ActiveGroup;
            public uint PrimarySpecialization;
            public List<TalentGroupInfo> TalentGroups = new List<TalentGroupInfo>();
        }
    }

    class LearnTalents : ClientPacket
    {
        public LearnTalents(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var count = _worldPacket.ReadBits<uint>(6);
            for (var i = 0; i < count; ++i)
                Talents[i] = _worldPacket.ReadUInt16();
        }

        public Array<ushort> Talents = new Array<ushort>(PlayerConst.MaxTalentTiers);
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
        public List<ushort> Talents = new List<ushort>();
    }

    class ActiveGlyphs : ServerPacket
    {
        public ActiveGlyphs() : base(ServerOpcodes.ActiveGlyphs) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Glyphs.Count);
            foreach (var glyph in Glyphs)
                glyph.Write(_worldPacket);

            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();
        }

        public List<GlyphBinding> Glyphs = new List<GlyphBinding>();
        public bool IsFullUpdate;
    }

    class LearnPvpTalents : ClientPacket
    {
        public LearnPvpTalents(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var size = _worldPacket.ReadUInt32();
            for (var i = 0; i < size; ++i)
                Talents[i] = new PvPTalent(_worldPacket);
        }

        public Array<PvPTalent> Talents = new Array<PvPTalent>(4);
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
        public List<PvPTalent> Talents = new List<PvPTalent>();
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

        uint SpellID;
        ushort GlyphID;
    }
}
