// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    class CollectionItemSetFavorite : ClientPacket
    {
        public CollectionItemSetFavorite(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Type = (ItemCollectionType)_worldPacket.ReadUInt8();
            Id = _worldPacket.ReadUInt32();
            IsFavorite = _worldPacket.HasBit();
        }

        public ItemCollectionType Type;
        public uint Id;
        public bool IsFavorite;
    }

    class AccountItemCollectionData : ServerPacket
    {
        public AccountItemCollectionData() : base(ServerOpcodes.AccountItemCollectionData) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Unknown1110_1);
            _worldPacket.WriteUInt8((byte)Type);
            _worldPacket.WriteInt32(Items.Count);

            foreach (ItemCollectionItemData item in Items)
                item.Write(_worldPacket);

            _worldPacket.WriteBit(Unknown1110_2);
            _worldPacket.FlushBits();
        }

        public uint Unknown1110_1;
        public ItemCollectionType Type;
        public bool Unknown1110_2;
        public List<ItemCollectionItemData> Items = new();
    }

    struct ItemCollectionItemData
    {
        public int Id;
        public ItemCollectionType Type;
        public long Unknown1110;
        public int Flags;

        public void Write(WorldPacket packet)
        {
            packet.WriteInt32(Id);
            packet.WriteUInt8((byte)Type);
            packet.WriteInt64(Unknown1110);
            packet.WriteInt32(Flags);
        }
    }
}