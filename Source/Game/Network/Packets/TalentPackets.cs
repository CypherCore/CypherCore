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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    class UpdateTalentData : ServerPacket
    {
        public UpdateTalentData() : base(ServerOpcodes.UpdateTalentData, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteUInt8(Info.ActiveGroup);
            _worldPacket.WriteUInt32(Info.PrimarySpecialization);
            _worldPacket.WriteUInt32(Info.TalentGroups.Count);

            foreach (var talentGroupInfo in Info.TalentGroups)
            {
                _worldPacket.WriteUInt32(talentGroupInfo.SpecID);
                _worldPacket.WriteUInt32(talentGroupInfo.TalentIDs.Count);
                _worldPacket.WriteUInt32(talentGroupInfo.PvPTalentIDs.Count);

                foreach (var talentID in talentGroupInfo.TalentIDs)
                    _worldPacket.WriteUInt16(talentID);

                foreach (ushort talentID in talentGroupInfo.PvPTalentIDs)
                    _worldPacket.WriteUInt16(talentID);
            }
        }

        public TalentInfoUpdate Info { get; set; } = new TalentInfoUpdate();

        public class TalentGroupInfo
        {
            public uint SpecID { get; set; }
            public List<ushort> TalentIDs { get; set; } = new List<ushort>();
            public List<ushort> PvPTalentIDs { get; set; } = new List<ushort>();
        }

        public class TalentInfoUpdate
        {
            public byte ActiveGroup { get; set; }
            public uint PrimarySpecialization { get; set; }
            public List<TalentGroupInfo> TalentGroups { get; set; } = new List<TalentGroupInfo>();
        }
    }

    class LearnTalents : ClientPacket
    {
        public LearnTalents(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            uint count = _worldPacket.ReadBits<uint>(6);

            for (uint i = 0; i < count; ++i)
                Talents.Add(_worldPacket.ReadUInt16());
        }

        public Array<ushort> Talents { get; set; } = new Array<ushort>(PlayerConst.MaxTalentTiers);
    }

    class RespecWipeConfirm : ServerPacket
    {
        public RespecWipeConfirm() : base(ServerOpcodes.RespecWipeConfirm) { }

        public override void Write()
        {
            _worldPacket.WriteInt8(RespecType);
            _worldPacket.WriteUInt32(Cost);
            _worldPacket.WritePackedGuid(RespecMaster);
        }

        public ObjectGuid RespecMaster { get; set; }
        public uint Cost { get; set; }
        public SpecResetType RespecType { get; set; }
    }

    class ConfirmRespecWipe : ClientPacket
    {
        public ConfirmRespecWipe(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            RespecMaster = _worldPacket.ReadPackedGuid();
            RespecType = (SpecResetType)_worldPacket.ReadUInt8();
        }

        public ObjectGuid RespecMaster { get; set; }
        public SpecResetType RespecType { get; set; }
    }

    class LearnTalentsFailed : ServerPacket
    {
        public LearnTalentsFailed() : base(ServerOpcodes.LearnTalentsFailed) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Reason, 4);
            _worldPacket.WriteInt32(SpellID);
            _worldPacket.WriteUInt32(Talents.Count);

            foreach (var talent in Talents)
                _worldPacket.WriteUInt16(talent);
        }

        public uint Reason { get; set; }
        public int SpellID;
        public List<ushort> Talents { get; set; } = new List<ushort>();
    }

    class ActiveGlyphs : ServerPacket
    {
        public ActiveGlyphs() : base(ServerOpcodes.ActiveGlyphs) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Glyphs.Count);
            foreach (GlyphBinding glyph in Glyphs)
                glyph.Write(_worldPacket);

            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();
        }

        public List<GlyphBinding> Glyphs { get; set; } = new List<GlyphBinding>();
        public bool IsFullUpdate { get; set; }
    }

    //Structs
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
