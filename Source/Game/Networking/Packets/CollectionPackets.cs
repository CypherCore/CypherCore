// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets
{
    public enum CollectionType
    {
        None = -1,
        Toybox = 1,
        Appearance = 3,
        TransmogSet = 4
    }

    class CollectionItemSetFavorite : ClientPacket
    {
        public CollectionItemSetFavorite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Type = (CollectionType)_worldPacket.ReadUInt32();
            Id = _worldPacket.ReadUInt32();
            IsFavorite = _worldPacket.HasBit();
        }

        public CollectionType Type;
        public uint Id;
        public bool IsFavorite;
    }
}
