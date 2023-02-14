// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
