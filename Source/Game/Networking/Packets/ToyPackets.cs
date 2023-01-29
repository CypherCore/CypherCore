// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
{
    internal class AddToy : ClientPacket
    {
        public ObjectGuid Guid;

        public AddToy(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Guid = _worldPacket.ReadPackedGuid();
        }
    }

    internal class UseToy : ClientPacket
    {
        public SpellCastRequest Cast = new();

        public UseToy(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Cast.Read(_worldPacket);
        }
    }

    internal class AccountToyUpdate : ServerPacket
    {
        public bool IsFullUpdate = false;
        public Dictionary<uint, ToyFlags> Toys = new();

        public AccountToyUpdate() : base(ServerOpcodes.AccountToyUpdate, ConnectionType.Instance)
        {
        }

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
    }

    internal class ToyClearFanfare : ClientPacket
    {
        public uint ItemID;

        public ToyClearFanfare(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            ItemID = _worldPacket.ReadUInt32();
        }
    }
}