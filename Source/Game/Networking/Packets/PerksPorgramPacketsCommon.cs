// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Networking.Packets
{
    public struct PerksVendorItem
    {
        public int VendorItemID;
        public int MountID;
        public int BattlePetSpeciesID;
        public int TransmogSetID;
        public int ItemModifiedAppearanceID;
        public int Field_14;
        public int Field_18;
        public int Price;
        public long AvailableUntil;
        public bool Disabled;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(VendorItemID);
            data.WriteInt32(MountID);
            data.WriteInt32(BattlePetSpeciesID);
            data.WriteInt32(TransmogSetID);
            data.WriteInt32(ItemModifiedAppearanceID);
            data.WriteInt32(Field_14);
            data.WriteInt32(Field_18);
            data.WriteInt32(Price);
            data.WriteInt64(AvailableUntil);
            data.WriteBit(Disabled);
            data.FlushBits();
        }
    }
}
