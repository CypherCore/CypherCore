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

namespace Game.Network.Packets
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
            ID = _worldPacket.ReadUInt32();
            IsFavorite = _worldPacket.HasBit();
        }

        public CollectionType Type;
        public uint ID;
        public bool IsFavorite;
    }
}
