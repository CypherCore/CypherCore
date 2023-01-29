// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class ArtifactAddPower : ClientPacket
    {
        public struct ArtifactPowerChoice
        {
            public uint ArtifactPowerID;
            public byte Rank;
        }

        public ObjectGuid ArtifactGUID;
        public ObjectGuid ForgeGUID;
        public Array<ArtifactPowerChoice> PowerChoices = new(1);

        public ArtifactAddPower(WorldPacket packet) : base(packet)
        {
        }

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
    }

    internal class ArtifactSetAppearance : ClientPacket
    {
        public int ArtifactAppearanceID;

        public ObjectGuid ArtifactGUID;
        public ObjectGuid ForgeGUID;

        public ArtifactSetAppearance(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ArtifactGUID = _worldPacket.ReadPackedGuid();
            ForgeGUID = _worldPacket.ReadPackedGuid();
            ArtifactAppearanceID = _worldPacket.ReadInt32();
        }
    }

    internal class ConfirmArtifactRespec : ClientPacket
    {
        public ObjectGuid ArtifactGUID;
        public ObjectGuid NpcGUID;

        public ConfirmArtifactRespec(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ArtifactGUID = _worldPacket.ReadPackedGuid();
            NpcGUID = _worldPacket.ReadPackedGuid();
        }
    }

    internal class OpenArtifactForge : ServerPacket
    {
        public ObjectGuid ArtifactGUID;
        public ObjectGuid ForgeGUID;

        public OpenArtifactForge() : base(ServerOpcodes.OpenArtifactForge)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WritePackedGuid(ForgeGUID);
        }
    }

    internal class ArtifactRespecPrompt : ServerPacket
    {
        public ObjectGuid ArtifactGUID;
        public ObjectGuid NpcGUID;

        public ArtifactRespecPrompt() : base(ServerOpcodes.ArtifactRespecPrompt)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WritePackedGuid(NpcGUID);
        }
    }

    internal class ArtifactXpGain : ServerPacket
    {
        public ulong Amount;

        public ObjectGuid ArtifactGUID;

        public ArtifactXpGain() : base(ServerOpcodes.ArtifactXpGain)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(ArtifactGUID);
            _worldPacket.WriteUInt64(Amount);
        }
    }
}