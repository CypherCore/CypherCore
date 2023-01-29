// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets
{
    public class InitializeFactions : ServerPacket
    {
        private const ushort FactionCount = 443;
        public ReputationFlags[] FactionFlags = new ReputationFlags[FactionCount];
        public bool[] FactionHasBonus = new bool[FactionCount]; //@todo: implement faction bonus

        public int[] FactionStandings = new int[FactionCount];

        public InitializeFactions() : base(ServerOpcodes.InitializeFactions, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            for (ushort i = 0; i < FactionCount; ++i)
            {
                _worldPacket.WriteUInt16((ushort)((ushort)FactionFlags[i] & 0xFF));
                _worldPacket.WriteInt32(FactionStandings[i]);
            }

            for (ushort i = 0; i < FactionCount; ++i)
                _worldPacket.WriteBit(FactionHasBonus[i]);

            _worldPacket.FlushBits();
        }
    }

    internal class RequestForcedReactions : ClientPacket
    {
        public RequestForcedReactions(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
        }
    }

    internal class SetForcedReactions : ServerPacket
    {
        public List<ForcedReaction> Reactions = new();

        public SetForcedReactions() : base(ServerOpcodes.SetForcedReactions, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteInt32(Reactions.Count);

            foreach (ForcedReaction reaction in Reactions)
                reaction.Write(_worldPacket);
        }
    }

    internal class SetFactionStanding : ServerPacket
    {
        public float BonusFromAchievementSystem;
        public List<FactionStandingData> Faction = new();
        public bool ShowVisual;

        public SetFactionStanding() : base(ServerOpcodes.SetFactionStanding, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteFloat(BonusFromAchievementSystem);

            _worldPacket.WriteInt32(Faction.Count);

            foreach (FactionStandingData factionStanding in Faction)
                factionStanding.Write(_worldPacket);

            _worldPacket.WriteBit(ShowVisual);
            _worldPacket.FlushBits();
        }
    }

    internal struct ForcedReaction
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Faction);
            data.WriteInt32(Reaction);
        }

        public int Faction;
        public int Reaction;
    }

    internal readonly struct FactionStandingData
    {
        public FactionStandingData(int index, int standing)
        {
            Index = index;
            Standing = standing;
        }

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Index);
            data.WriteInt32(Standing);
        }

        private readonly int Index;
        private readonly int Standing;
    }
}