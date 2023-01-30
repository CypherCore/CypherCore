// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Networking.Packets
{
    public enum CollectionType
    {
        None = -1,
        Toybox = 1,
        Appearance = 3,
        TransmogSet = 4
    }

    internal class CollectionItemSetFavorite : ClientPacket
    {
        public uint Id;
        public bool IsFavorite;

        public CollectionType Type;

        public CollectionItemSetFavorite(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Type = (CollectionType)_worldPacket.ReadUInt32();
            Id = _worldPacket.ReadUInt32();
            IsFavorite = _worldPacket.HasBit();
        }
    }
}