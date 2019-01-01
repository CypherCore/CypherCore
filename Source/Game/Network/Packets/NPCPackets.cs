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

using Framework.Constants;
using Framework.GameMath;
using Game.Entities;
using System;
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

        public ObjectGuid Unit;
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

                _worldPacket.WriteBits(options.Text.GetByteCount(), 12);
                _worldPacket.WriteBits(options.Confirm.GetByteCount(), 12);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(options.Text);
                _worldPacket.WriteString(options.Confirm);
            }

            foreach (ClientGossipText text in GossipText)
            {
                _worldPacket.WriteInt32(text.QuestID);
                _worldPacket.WriteInt32(text.QuestType);
                _worldPacket.WriteInt32(text.QuestLevel);
                _worldPacket.WriteInt32(text.QuestMaxScalingLevel);
                _worldPacket.WriteInt32(text.QuestFlags);
                _worldPacket.WriteInt32(text.QuestFlagsEx);

                _worldPacket.WriteBit(text.Repeatable);
                _worldPacket.WriteBits(text.QuestTitle.GetByteCount(), 9);
                _worldPacket.FlushBits();

                _worldPacket.WriteString(text.QuestTitle);
            }
        }

        public List<ClientGossipOptions> GossipOptions = new List<ClientGossipOptions>();
        public int FriendshipFactionID = 0;
        public ObjectGuid GossipGUID;
        public List<ClientGossipText> GossipText = new List<ClientGossipText>();
        public int TextID = 0;
        public int GossipID = 0;
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

        public ObjectGuid GossipUnit;
        public uint GossipIndex;
        public uint GossipID;
        public string PromotionCode;
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

        public byte Reason = 0;
        public List<VendorItemPkt> Items = new List<VendorItemPkt>();
        public ObjectGuid Vendor;
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

            _worldPacket.WriteBits(Greeting.GetByteCount(), 11);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Greeting);
        }

        public ObjectGuid TrainerGUID;
        public int TrainerType;
        public int TrainerID = 1;
        public List<TrainerListSpell> Spells = new List<TrainerListSpell>();
        public string Greeting;
    }

    public class ShowBank : ServerPacket
    {
        public ShowBank() : base(ServerOpcodes.ShowBank, ConnectionType.Instance) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Guid);
        }

        public ObjectGuid Guid;
    }

    public class PlayerTabardVendorActivate : ServerPacket
    {
        public PlayerTabardVendorActivate() : base(ServerOpcodes.PlayerTabardVendorActivate) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Vendor);
        }

        public ObjectGuid Vendor;
    }

    class GossipPOI : ServerPacket
    {
        public GossipPOI() : base(ServerOpcodes.GossipPoi) { }

        public override void Write()
        {
            _worldPacket.WriteUInt32(ID);
            _worldPacket.WriteFloat(Pos.X);
            _worldPacket.WriteFloat(Pos.Y);
            _worldPacket.WriteUInt32(Icon);
            _worldPacket.WriteUInt32(Importance);
            _worldPacket.WriteBits(Flags, 14);
            _worldPacket.WriteBits(Name.GetByteCount(), 6);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);
        }

        public uint ID;
        public uint Flags;
        public Vector2 Pos;
        public uint Icon;
        public uint Importance;
        public string Name;
    }

    class SpiritHealerActivate : ClientPacket
    {
        public SpiritHealerActivate(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            Healer = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid Healer;
    }

    public class SpiritHealerConfirm : ServerPacket
    {
        public SpiritHealerConfirm() : base(ServerOpcodes.SpiritHealerConfirm) { }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Unit);
        }

        public ObjectGuid Unit;
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

        public ObjectGuid TrainerGUID;
        public uint TrainerID;
        public uint SpellID;
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

        public ObjectGuid TrainerGUID;
        public uint SpellID;
        public TrainerFailReason TrainerFailedReason;
    }

    class RequestStabledPets : ClientPacket
    {
        public RequestStabledPets(WorldPacket packet) : base(packet) { }

        public override void Read()
        {
            StableMaster = _worldPacket.ReadPackedGuid();
        }

        public ObjectGuid StableMaster;
    }

    //Structs
    public struct ClientGossipOptions
    {
        public int ClientOption;
        public byte OptionNPC;
        public byte OptionFlags;
        public int OptionCost;
        public string Text;
        public string Confirm;
    }

    public class ClientGossipText
    {
        public int QuestID;
        public int QuestType;
        public int QuestLevel;
        public int QuestMaxScalingLevel;
        public bool Repeatable;
        public string QuestTitle;
        public int QuestFlags;
        public int QuestFlagsEx;
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

        public int MuID;
        public int Type;
        public ItemInstance Item = new ItemInstance();
        public int Quantity = -1;
        public ulong Price;
        public int Durability;
        public int StackCount;
        public int ExtendedCostID;
        public int PlayerConditionFailed;
        public bool DoNotFilterOnVendor;
    }

    public class TrainerListSpell
    {
        public uint SpellID;
        public uint MoneyCost;
        public uint ReqSkillLine;
        public uint ReqSkillRank;
        public uint[] ReqAbility = new uint[SharedConst.MaxTrainerspellAbilityReqs];
        public TrainerSpellState Usable;
        public byte ReqLevel;
    }
}
