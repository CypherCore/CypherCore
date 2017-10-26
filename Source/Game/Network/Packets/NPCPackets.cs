/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
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
using Framework.GameMath;
using Game.Entities;
using System.Collections.Generic;

namespace Game.Network.Packets
{
    // CMSG_BANKER_ACTIVATE
    // CMSG_BINDER_ACTIVATE
    // CMSG_BINDER_CONFIRM
    // CMSG_GOSSIP_HELLO
    // CMSG_LIST_INVENTORY
    // CMSG_TRAINER_LIST
    // CMSG_BATTLEMASTER_HELLO
    public class Hello : ClientPacket
    {
        public Hello(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Unit { get; set; }
    }

    public class GossipMessagePkt : ServerPacket
    {
        public GossipMessagePkt() : base(ServerOpcodes.GossipMessage) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GossipGUID);
            _worldPacket.WriteInt32(GossipID);
            _worldPacket.WriteInt32(FriendshipFactionID);
            _worldPacket.WriteInt32(TextID);

            _worldPacket.WriteInt32(GossipOptions.Count);
            _worldPacket.WriteInt32(GossipText.Count);

            foreach (ClientGossipOptions options in GossipOptions)
            {
                _worldPacket.WriteInt32(options.ClientOption);
                _worldPacket.WriteInt8(options.OptionNPC);
                _worldPacket.WriteInt8(options.OptionFlags);
                _worldPacket.WriteInt32(options.OptionCost);

                _worldPacket.WriteBits(options.Text.Length, 12);
                _worldPacket.WriteBits(options.Confirm.Length, 12);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(options.Text);
                _worldPacket.WriteString(options.Confirm);
            }

            foreach (ClientGossipText text in GossipText)
            {
                _worldPacket.WriteInt32(text.QuestID);
                _worldPacket.WriteInt32(text.QuestType);
                _worldPacket.WriteInt32(text.QuestLevel);
                _worldPacket.WriteInt32(text.QuestFlags);
                _worldPacket.WriteInt32(text.QuestFlagsEx);

                _worldPacket.WriteBit(text.Repeatable);
                _worldPacket.WriteBits(text.QuestTitle.Length, 9);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(text.QuestTitle);
            }
        }

        public List<ClientGossipOptions> GossipOptions { get; set; } = new List<ClientGossipOptions>();
        public int FriendshipFactionID { get; set; } = 0;
        public ObjectGuid GossipGUID { get; set; }
        public List<ClientGossipText> GossipText { get; set; } = new List<ClientGossipText>();
        public int TextID { get; set; } = 0;
        public int GossipID { get; set; } = 0;
    }

    public class GossipSelectOption : ClientPacket
    {
        public GossipSelectOption(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            GossipUnit = _worldPacket.ReadPackedGuid();
            GossipID = _worldPacket.ReadUInt32();
            GossipIndex = _worldPacket.ReadUInt32();

            uint length = _worldPacket.ReadBits<uint>(8);
            PromotionCode = _worldPacket.ReadString(length);
        }

        public ObjectGuid GossipUnit { get; set; }
        public uint GossipIndex { get; set; }
        public uint GossipID { get; set; }
        public string PromotionCode { get; set; }
    }

    public class GossipComplete : ServerPacket
    {
        public GossipComplete() : base(ServerOpcodes.GossipComplete) { }

        public override void Write() { }
    }

    public class VendorInventory : ServerPacket
    {
        public VendorInventory() : base(ServerOpcodes.VendorInventory, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Vendor);
            _worldPacket.WriteUInt8(Reason);
            _worldPacket.WriteInt32(Items.Count);

            foreach (VendorItemPkt item in Items)
                item.Write(_worldPacket);
        }

        public byte Reason { get; set; } = 0;
        public List<VendorItemPkt> Items { get; set; } = new List<VendorItemPkt>();
        public ObjectGuid Vendor { get; set; }
    }

    public class TrainerList : ServerPacket
    {
        public TrainerList() : base(ServerOpcodes.TrainerList, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TrainerGUID);
            _worldPacket.WriteInt32(TrainerType);
            _worldPacket.WriteInt32(TrainerID);

            _worldPacket.WriteInt32(Spells.Count);
            foreach (TrainerListSpell spell in Spells)
            {
                _worldPacket.WriteInt32(spell.SpellID);
                _worldPacket.WriteInt32(spell.MoneyCost);
                _worldPacket.WriteInt32(spell.ReqSkillLine);
                _worldPacket.WriteInt32(spell.ReqSkillRank);

                for (uint i = 0; i < SharedConst.MaxTrainerspellAbilityReqs; ++i)
                    _worldPacket.WriteInt32(spell.ReqAbility[i]);

                _worldPacket.WriteInt8(spell.Usable);
                _worldPacket.WriteInt8(spell.ReqLevel);
            }

            _worldPacket.WriteBits(Greeting.Length, 11);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Greeting);
        }

        public ObjectGuid TrainerGUID { get; set; }
        public int TrainerType { get; set; }
        public int TrainerID { get; set; } = 1;
        public List<TrainerListSpell> Spells { get; set; } = new List<TrainerListSpell>();
        public string Greeting { get; set; }
    }

    public class ShowBank : ServerPacket
    {
        public ShowBank() : base(ServerOpcodes.ShowBank, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid { get; set; }
    }

    public class PlayerTabardVendorActivate : ServerPacket
    {
        public PlayerTabardVendorActivate() : base(ServerOpcodes.PlayerTabardVendorActivate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Vendor);
        }

        public ObjectGuid Vendor { get; set; }
    }

    class GossipPOI : ServerPacket
    {
        public GossipPOI() : base(ServerOpcodes.GossipPoi) { }

        public override void Write()
        {
            _worldPacket.WriteBits(Flags, 14);
            _worldPacket.WriteBits(Name.Length, 6);
            _worldPacket.WriteFloat(Pos.X);
            _worldPacket.WriteFloat(Pos.Y);
            _worldPacket.WriteUInt32(Icon);
            _worldPacket.WriteUInt32(Importance);
            _worldPacket.WriteString(Name);
        }

        public uint Flags { get; set; }
        public Vector2 Pos { get; set; }
        public uint Icon { get; set; }
        public uint Importance { get; set; }
        public string Name;
    }

    class SpiritHealerActivate : ClientPacket
    {
        public SpiritHealerActivate(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Healer = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Healer { get; set; }
    }

    public class SpiritHealerConfirm : ServerPacket
    {
        public SpiritHealerConfirm() : base(ServerOpcodes.SpiritHealerConfirm) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
        }

        public ObjectGuid Unit { get; set; }
    }

    class TrainerBuySpell : ClientPacket
    {
        public TrainerBuySpell(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            TrainerGUID = _worldPacket.ReadPackedGuid();
            TrainerID = _worldPacket.ReadUInt32();
            SpellID= _worldPacket.ReadUInt32();
        }

        public ObjectGuid TrainerGUID { get; set; }
        public uint TrainerID { get; set; }
        public uint SpellID { get; set; }
    }

    class TrainerBuyFailed : ServerPacket
    {
        public TrainerBuyFailed() : base(ServerOpcodes.TrainerBuyFailed) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TrainerGUID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32(TrainerFailedReason);
        }

        public ObjectGuid TrainerGUID { get; set; }
        public uint SpellID { get; set; }
        public TrainerFailReason TrainerFailedReason { get; set; }
    }

    class RequestStabledPets : ClientPacket
    {
        public RequestStabledPets(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            StableMaster = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid StableMaster { get; set; }
    }

    //Structs
    public struct ClientGossipOptions
    {
        public int ClientOption { get; set; }
        public byte OptionNPC { get; set; }
        public byte OptionFlags { get; set; }
        public int OptionCost { get; set; }
        public string Text { get; set; }
        public string Confirm { get; set; }
    }

    public class ClientGossipText
    {
        public int QuestID { get; set; }
        public int QuestType { get; set; }
        public int QuestLevel { get; set; }
        public bool Repeatable { get; set; }
        public string QuestTitle;
        public int QuestFlags { get; set; }
        public int QuestFlagsEx { get; set; }
    }

    public class VendorItemPkt
    {
        public void Write(WorldPacket data)
        {
            data.WriteUInt32(MuID);
            data.WriteInt32(Type);
            data.WriteInt32(Quantity);
            data.WriteUInt64(Price);
            data.WriteInt32(Durability);
            data.WriteInt32(StackCount);
            data.WriteInt32(ExtendedCostID);
            data.WriteInt32(PlayerConditionFailed);
            Item.Write(data);
            data.WriteBit(DoNotFilterOnVendor);
            data.FlushBits();
        }

        public int MuID { get; set; }
        public int Type { get; set; }
        public ItemInstance Item { get; set; } = new ItemInstance();
        public int Quantity { get; set; } = -1;
        public ulong Price { get; set; }
        public int Durability { get; set; }
        public int StackCount { get; set; }
        public int ExtendedCostID { get; set; }
        public int PlayerConditionFailed { get; set; }
        public bool DoNotFilterOnVendor { get; set; }
    }

    public class TrainerListSpell
    {
        public uint SpellID { get; set; }
        public uint MoneyCost { get; set; }
        public uint ReqSkillLine { get; set; }
        public uint ReqSkillRank { get; set; }
        public uint[] ReqAbility { get; set; } = new uint[SharedConst.MaxTrainerspellAbilityReqs];
        public TrainerSpellState Usable { get; set; }
        public byte ReqLevel { get; set; }
    }
}
