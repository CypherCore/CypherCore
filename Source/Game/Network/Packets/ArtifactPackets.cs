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
    class ArtifactAddPower : ClientPacket
    {
        public ArtifactAddPower(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ArtifactGUID = _worldPacket.ReadPackedGuid();
            ForgeGUID = _worldPacket.ReadPackedGuid();

            var powerCount = _worldPacket.ReadUInt32();
            for (var i = 0; i < powerCount; ++i)
            {
                ArtifactPowerChoice artifactPowerChoice = new ArtifactPowerChoice();
                artifactPowerChoice.ArtifactPowerID = _worldPacket.ReadUInt32();
                artifactPowerChoice.Rank = _worldPacket.ReadUInt8();
                PowerChoices.Add(artifactPowerChoice);
            }
        }

        public ObjectGuid ArtifactGUID { get; set; }
        public ObjectGuid ForgeGUID { get; set; }
        public Array<ArtifactPowerChoice> PowerChoices { get; set; } = new Array<ArtifactPowerChoice>(1);

        public struct ArtifactPowerChoice
        {
            public uint ArtifactPowerID { get; set; }
            public byte Rank { get; set; }
        }
    }

    class ArtifactSetAppearance : ClientPacket
    {
        public ArtifactSetAppearance(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ArtifactGUID = _worldPacket.ReadPackedGuid();
            ForgeGUID = _worldPacket.ReadPackedGuid();
            ArtifactAppearanceID = _worldPacket.ReadInt32();
        }

        public ObjectGuid ArtifactGUID { get; set; }
        public ObjectGuid ForgeGUID { get; set; }
        public int ArtifactAppearanceID { get; set; }
    }

    class ConfirmArtifactRespec : ClientPacket
    {
        public ConfirmArtifactRespec(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ArtifactGUID = _worldPacket.ReadPackedGuid();
            NpcGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ArtifactGUID { get; set; }
        public ObjectGuid NpcGUID { get; set; }
    }

    class ArtifactForgeOpened : ServerPacket
    {
        public ArtifactForgeOpened() : base(ServerOpcodes.ArtifactForgeOpened) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WritePackedGuid(ForgeGUID);
        }

        public ObjectGuid ArtifactGUID { get; set; }
        public ObjectGuid ForgeGUID { get; set; }
    }

    class ArtifactRespecConfirm : ServerPacket
    {
        public ArtifactRespecConfirm() : base(ServerOpcodes.ArtifactRespecConfirm) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WritePackedGuid(NpcGUID);
        }

        public ObjectGuid ArtifactGUID { get; set; }
        public ObjectGuid NpcGUID { get; set; }
    }

    class ArtifactXpGain : ServerPacket
    {
        public ArtifactXpGain() : base(ServerOpcodes.ArtifactXpGain) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WriteUInt64(Amount);
        }

        public ObjectGuid ArtifactGUID { get; set; }
        public ulong Amount { get; set; }
    }
}
