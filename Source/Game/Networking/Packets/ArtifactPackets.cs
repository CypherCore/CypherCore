// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
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
                ArtifactPowerChoice artifactPowerChoice;
                artifactPowerChoice.ArtifactPowerID = _worldPacket.ReadUInt32();
                artifactPowerChoice.Rank = _worldPacket.ReadUInt8();
                PowerChoices[i] = artifactPowerChoice;
            }
        }

        public ObjectGuid ArtifactGUID;
        public ObjectGuid ForgeGUID;
        public Array<ArtifactPowerChoice> PowerChoices = new(1);

        public struct ArtifactPowerChoice
        {
            public uint ArtifactPowerID;
            public byte Rank;
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

        public ObjectGuid ArtifactGUID;
        public ObjectGuid ForgeGUID;
        public int ArtifactAppearanceID;
    }

    class ConfirmArtifactRespec : ClientPacket
    {
        public ConfirmArtifactRespec(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ArtifactGUID = _worldPacket.ReadPackedGuid();
            NpcGUID = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid ArtifactGUID;
        public ObjectGuid NpcGUID;
    }

    class OpenArtifactForge : ServerPacket
    {
        public OpenArtifactForge() : base(ServerOpcodes.OpenArtifactForge) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WritePackedGuid(ForgeGUID);
        }

        public ObjectGuid ArtifactGUID;
        public ObjectGuid ForgeGUID;
    }

    class ArtifactRespecPrompt : ServerPacket
    {
        public ArtifactRespecPrompt() : base(ServerOpcodes.ArtifactRespecPrompt) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WritePackedGuid(NpcGUID);
        }

        public ObjectGuid ArtifactGUID;
        public ObjectGuid NpcGUID;
    }

    class ArtifactXpGain : ServerPacket
    {
        public ArtifactXpGain() : base(ServerOpcodes.ArtifactXpGain) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WriteUInt64(Amount);
        }

        public ObjectGuid ArtifactGUID;
        public ulong Amount;
    }
}
