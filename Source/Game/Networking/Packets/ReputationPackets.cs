// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    public class InitializeFactions : ServerPacket
    {
        const ushort FactionCount = 1000;

        public InitializeFactions() : base(ServerOpcodes.InitializeFactions, ConnectionType.Instance) { }

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

        public int[] FactionStandings = new int[FactionCount];
        public bool[] FactionHasBonus = new bool[FactionCount]; //@todo: implement faction bonus
        public ReputationFlags[] FactionFlags = new ReputationFlags[FactionCount];
    }

    class RequestForcedReactions : ClientPacket
    {
        public RequestForcedReactions(WorldPacket packet) : base(packet) { }

        public override void Read() { }
    }

    class SetForcedReactions : ServerPacket
    {
        public SetForcedReactions() : base(ServerOpcodes.SetForcedReactions, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteInt32(Reactions.Count);
            foreach (ForcedReaction reaction in Reactions)
                reaction.Write(_worldPacket);
        }

        public List<ForcedReaction> Reactions = new();
    }

    class SetFactionStanding : ServerPacket
    {
        public SetFactionStanding() : base(ServerOpcodes.SetFactionStanding, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteFloat(BonusFromAchievementSystem);

            _worldPacket.WriteInt32(Faction.Count);
            foreach (FactionStandingData factionStanding in Faction)
                factionStanding.Write(_worldPacket);

            _worldPacket.WriteBit(ShowVisual);
            _worldPacket.FlushBits();
        }

        public float BonusFromAchievementSystem;
        public List<FactionStandingData> Faction = new();
        public bool ShowVisual;
    }

    struct ForcedReaction
    {
        public void Write(WorldPacket data)
        {
            data.WriteInt32(Faction);
            data.WriteInt32(Reaction);
        }

        public int Faction;
        public int Reaction;
    }

    struct FactionStandingData
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

        int Index;
        int Standing;
    }
}
