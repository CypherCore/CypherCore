// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Networking.Packets.Bpay
{
    public class SyncWowEntitlements : ServerPacket
    {
        public SyncWowEntitlements() : base(ServerOpcodes.SyncWowEntitlements)
        {
        }


        /*void WorldPackets::BattlePay::PurchaseProduct::Read()
        {
            _worldPacket >> ClientToken;
            _worldPacket >> ProductID;
            _worldPacket >> TargetCharacter;

            uint32 strlen1 = _worldPacket.ReadBits(6);
            uint32 strlen2 = _worldPacket.ReadBits(12);
            WowSytem = _worldPacket.ReadString(strlen1);
            PublicKey = _worldPacket.ReadString(strlen2);
        }*/

        public override void Write()
        {
            Log.outInfo(LogFilter.BattlePay, "SyncWowEntitlements");
            _worldPacket.WriteUInt32((uint)PurchaseCount.Count);
            _worldPacket.WriteUInt32((uint)Product.Count);

            foreach (var purchases in PurchaseCount)
            {
                _worldPacket.WriteUInt32(0); // productID ?
                _worldPacket.WriteUInt32(0); // flags?
                _worldPacket.WriteUInt32(0); // idem to flags?
                _worldPacket.WriteUInt32(0); // always 0
                _worldPacket.WriteBits(0, 7); // always 0
                _worldPacket.WriteBit(false); // always false
            }

            foreach (var product in Product)
            {
                _worldPacket.Write(product.ProductId);
                _worldPacket.Write(product.Type);
                _worldPacket.Write(product.Flags);
                _worldPacket.Write(product.Unk1);
                _worldPacket.Write(product.DisplayId);
                _worldPacket.Write(product.ItemId);
                _worldPacket.WriteUInt32(0);
                _worldPacket.WriteUInt32(2);
                _worldPacket.WriteUInt32(0);
                _worldPacket.WriteUInt32(0);
                _worldPacket.WriteUInt32(0);
                _worldPacket.WriteUInt32(0);

                _worldPacket.WriteBits((uint)product.UnkString.Length, 8);
                _worldPacket.WriteBit(product.UnkBits != 0);
                _worldPacket.WriteBit(product.UnkBit);
                _worldPacket.WriteBits((uint)product.Items.Count, 7);
                _worldPacket.WriteBit(product.Display != null);
                _worldPacket.WriteBit(false); // unk

                if (product.UnkBits != 0)
                {
                    _worldPacket.WriteBits(product.UnkBits, 4);
                }

                _worldPacket.FlushBits();

                foreach (var productItem in product.Items)
                {
                    _worldPacket.WriteUInt32(productItem.ID);
                    _worldPacket.WriteUInt8(productItem.UnkByte);
                    _worldPacket.WriteUInt32(productItem.ItemID);
                    _worldPacket.WriteUInt32(productItem.Quantity);
                    _worldPacket.WriteUInt32(productItem.UnkInt1);
                    _worldPacket.WriteUInt32(productItem.UnkInt2);

                    _worldPacket.WriteBit(productItem.IsPet);
                    _worldPacket.WriteBit(productItem.PetResult != 0);
                    _worldPacket.WriteBit(productItem.Display != null);

                    if (productItem.PetResult != 0)
                    {
                        _worldPacket.WriteBits(productItem.PetResult, 4);
                    }

                    _worldPacket.FlushBits();

                    if (productItem.Display != null)
                        productItem.Display.Write(_worldPacket);
                }

                _worldPacket.WriteString(product.UnkString);

                if (product.Display != null)
                {
                    product.Display.Write(_worldPacket);
                }
            }


        }

        public List<uint> PurchaseCount { get; set; } = new List<uint>();
        public List<uint> ProductCount { get; set; } = new List<uint>();
        public List<BpayProduct> Product { get; set; } = new List<BpayProduct>();
    }

}
