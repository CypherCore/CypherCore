// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using Framework.Constants;

namespace Game.Networking.Packets
{
    public class AutoBankItem : ClientPacket
    {
        public InvUpdate Inv;
        public BankType BankType;
        public byte Bag;
        public byte Slot;

        public AutoBankItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            BankType = (BankType)_worldPacket.ReadInt8();
            Bag = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    public class AutoStoreBankItem : ClientPacket
    {
        public InvUpdate Inv;
        public byte Bag;
        public byte Slot;

        public AutoStoreBankItem(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Inv = new InvUpdate(_worldPacket);
            Bag = _worldPacket.ReadUInt8();
            Slot = _worldPacket.ReadUInt8();
        }
    }

    public class BuyBankTab : ClientPacket
    {
        public ObjectGuid Banker;
        public BankType BankType;

        public BuyBankTab(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            BankType = (BankType)_worldPacket.ReadInt8();
        }
    }

    class AutoDepositCharacterBank : ClientPacket
    {
        public ObjectGuid Banker;

        public AutoDepositCharacterBank(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
        }
    }

    class BankerActivate : ClientPacket
    {
        public ObjectGuid Banker;
        public PlayerInteractionType InteractionType;

        public BankerActivate(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            InteractionType = (PlayerInteractionType)_worldPacket.ReadInt32();
        }
    }

    class UpdateBankTabSettings : ClientPacket
    {
        public ObjectGuid Banker;
        public BankType BankType;
        public byte Tab;
        public BankTabSettings Settings;

        public UpdateBankTabSettings(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Banker = _worldPacket.ReadPackedGuid();
            BankType = (BankType)_worldPacket.ReadInt8();
            Tab = _worldPacket.ReadUInt8();
            Settings.Read(_worldPacket);
        }
    }

    public struct BankTabSettings
    {
        public string Name;
        public string Icon;
        public string Description;
        public BagSlotFlags DepositFlags;

        public void Read(WorldPacket data)
        {
            data.ResetBitPos();
            var nameLength = data.ReadBits<uint>(7);
            var iconLength = data.ReadBits<uint>(9);
            var descriptionLength = data.ReadBits<uint>(14);
            DepositFlags = (BagSlotFlags)data.ReadInt32();

            Name = data.ReadString(nameLength);
            Icon = data.ReadString(iconLength);
            Description = data.ReadString(descriptionLength);
        }
    }
}
