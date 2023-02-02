// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;
using System.Collections.Generic;
using System;

namespace Game.Networking.Packets
{
    class AddToy : ClientPacket
    {
        public AddToy(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Guid;
    }

    class UseToy : ClientPacket
    {
        public UseToy(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Cast.Read(_worldPacket);
        }

        public SpellCastRequest Cast = new();
    }

    class AccountToyUpdate : ServerPacket
    {
        public AccountToyUpdate() : base(ServerOpcodes.AccountToyUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();

            // all lists have to have the same size
            _worldPacket.WriteInt32(Toys.Count);
            _worldPacket.WriteInt32(Toys.Count);
            _worldPacket.WriteInt32(Toys.Count);

            foreach (var pair in Toys)
                _worldPacket.WriteUInt32(pair.Key);

            foreach (var pair in Toys)
                _worldPacket.WriteBit(pair.Value.HasAnyFlag(ToyFlags.Favorite));

            foreach (var pair in Toys)
                _worldPacket.WriteBit(pair.Value.HasAnyFlag(ToyFlags.HasFanfare));

            _worldPacket.FlushBits();
        }

        public bool IsFullUpdate = false;
        public Dictionary<uint, ToyFlags> Toys = new();
    }

    class ToyClearFanfare : ClientPacket
    {
        public ToyClearFanfare(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            ItemID = _worldPacket.ReadUInt32();
        }

        public uint ItemID;
    }
}
