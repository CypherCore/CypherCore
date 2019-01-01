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
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
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

        public SpellCastRequest Cast = new SpellCastRequest();
    }

    class AccountToysUpdate : ServerPacket
    {
        public AccountToysUpdate() : base(ServerOpcodes.AccountToysUpdate, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.FlushBits();

            // both lists have to have the same size
            _worldPacket.WriteUInt32(Toys.Count);
            _worldPacket.WriteUInt32(Toys.Count);

            foreach (var item in Toys)
                _worldPacket.WriteUInt32(item.Key);

            foreach (var favourite in Toys)
                _worldPacket.WriteBit(favourite.Value);

            _worldPacket.FlushBits();
        }

        public bool IsFullUpdate = false;
        public Dictionary<uint, bool> Toys = new Dictionary<uint, bool>();
    }
}
