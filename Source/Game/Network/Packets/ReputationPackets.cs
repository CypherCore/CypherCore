/*
 * Copyright (C) 2012-2019 CypherCore <http://github.com/CypherCore>
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
using System.Collections.Generic;

namespace Game.Network.Packets
{
    public class InitializeFactions : ServerPacket
    {
        const ushort FactionCount = 300;

        public InitializeFactions() : base(ServerOpcodes.InitializeFactions, ConnectionType.Instance) { }

        public override void Write()
        {
            for (ushort i = 0; i < FactionCount; ++i)
            {
                _worldPacket.WriteUInt8(FactionFlags[i]);
                _worldPacket.WriteInt32(FactionStandings[i]);
            }

            for (ushort i = 0; i < FactionCount; ++i)
                _worldPacket.WriteBit(FactionHasBonus[i]);

            _worldPacket.FlushBits();
        }

        public int[] FactionStandings = new int[FactionCount];
        public bool[] FactionHasBonus = new bool[FactionCount]; //@todo: implement faction bonus
        public FactionFlags[] FactionFlags = new FactionFlags[FactionCount];
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
            _worldPacket.WriteUInt32(Reactions.Count);
            foreach (ForcedReaction reaction in Reactions)
                reaction.Write(_worldPacket);
        }

        public List<ForcedReaction> Reactions = new List<ForcedReaction>();
    }

    class SetFactionStanding : ServerPacket
    {
        public SetFactionStanding() : base(ServerOpcodes.SetFactionStanding, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteFloat(ReferAFriendBonus);
            _worldPacket.WriteFloat(BonusFromAchievementSystem);

            _worldPacket.WriteUInt32(Faction.Count);
            foreach (FactionStandingData factionStanding in Faction)
                factionStanding.Write(_worldPacket);

            _worldPacket.WriteBit(ShowVisual);
            _worldPacket.FlushBits();
        }

        public float ReferAFriendBonus;
        public float BonusFromAchievementSystem;
        public List<FactionStandingData> Faction = new List<FactionStandingData>();
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
