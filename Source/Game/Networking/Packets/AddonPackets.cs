/*
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
using System;
using System.Collections.Generic;
using Framework.Dynamic;

namespace Game.Networking.Packets
{
    public struct AddOnInfo
    {
        public string Name;
        public string Version;
        public bool Loaded;
        public bool Disabled;

        public void Read(WorldPacket data)
        {
            data.ResetBitPos();

            uint nameLength = data.ReadBits<uint>(10);
            uint versionLength = data.ReadBits<uint>(10);
            Loaded = data.HasBit();
            Disabled = data.HasBit();
            if (nameLength > 1)
            {
                Name = data.ReadString(nameLength - 1);
                data.ReadUInt8(); // null terminator
            }
            if (versionLength > 1)
            {
                Version = data.ReadString(versionLength - 1);
                data.ReadUInt8(); // null terminator
            }
        }
    }
}
