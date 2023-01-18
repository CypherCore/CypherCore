// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

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
