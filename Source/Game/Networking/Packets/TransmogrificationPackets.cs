﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

namespace Game.Networking.Packets
{
    internal class TransmogrifyItems : ClientPacket
    {
        public TransmogrifyItems(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            var itemsCount = _worldPacket.ReadUInt32();
            Npc = _worldPacket.ReadPackedGuid();

            for (var i = 0; i < itemsCount; ++i)
            {
                var item = new TransmogrifyItem();
                item.Read(_worldPacket);
                Items[i] = item;
            }

            CurrentSpecOnly = _worldPacket.HasBit();
        }

        public ObjectGuid Npc;
        public Array<TransmogrifyItem> Items = new Array<TransmogrifyItem>(13);
        public bool CurrentSpecOnly;
    }

    internal class AccountTransmogUpdate : ServerPacket
    {
        public AccountTransmogUpdate() : base(ServerOpcodes.AccountTransmogUpdate) { }

        public override void Write()
        {
            _worldPacket.WriteBit(IsFullUpdate);
            _worldPacket.WriteBit(IsSetFavorite);
            _worldPacket.WriteInt32(FavoriteAppearances.Count);
            _worldPacket.WriteInt32(NewAppearances.Count);

            foreach (var itemModifiedAppearanceId in FavoriteAppearances)
                _worldPacket.WriteUInt32(itemModifiedAppearanceId);

            foreach (var newAppearance in NewAppearances)
                _worldPacket.WriteUInt32(newAppearance);
        }

        public bool IsFullUpdate;
        public bool IsSetFavorite;
        public List<uint> FavoriteAppearances = new List<uint>();
        public List<uint> NewAppearances = new List<uint>();
    }

    internal class TransmogrifyNPC : ServerPacket
    {
        public TransmogrifyNPC(ObjectGuid guid) : base(ServerOpcodes.TransmogrifyNpc, ConnectionType.Instance)
        {
            Guid = guid;
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        private ObjectGuid Guid;
    }

    internal struct TransmogrifyItem
    {
        public void Read(WorldPacket data)
        {
            ItemModifiedAppearanceID = data.ReadInt32();
            Slot = data.ReadUInt32();
            SpellItemEnchantmentID = data.ReadInt32();
            SecondaryItemModifiedAppearanceID = data.ReadInt32();
        }

        public int ItemModifiedAppearanceID;
        public uint Slot;
        public int SpellItemEnchantmentID;
        public int SecondaryItemModifiedAppearanceID;
    }
}
