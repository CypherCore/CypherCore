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
        public int TransmogIllusionID;
        public int ToyID;
        public int WarbandSceneID;
        public int Price;
        public int OriginalPrice;
        public long AvailableUntil;
        public bool Disabled;
        public bool DoesNotExpire;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(VendorItemID);
            data.WriteInt32(MountID);
            data.WriteInt32(BattlePetSpeciesID);
            data.WriteInt32(TransmogSetID);
            data.WriteInt32(ItemModifiedAppearanceID);
            data.WriteInt32(TransmogIllusionID);
            data.WriteInt32(ToyID);
            data.WriteInt32(Price);
            data.WriteInt32(OriginalPrice);
            data.WriteInt64(AvailableUntil);
            data.WriteInt32(WarbandSceneID);
            data.WriteBit(Disabled);
            data.WriteBit(DoesNotExpire);
            data.FlushBits();
        }
    }
}
