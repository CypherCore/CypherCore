// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets
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
        public ObjectGuid Unit;

        public Hello(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Unit = _worldPacket.ReadPackedGuid();
        }
    }

    public class NPCInteractionOpenResult : ServerPacket
    {
        public PlayerInteractionType InteractionType;

        public ObjectGuid Npc;
        public bool Success = true;

        public NPCInteractionOpenResult() : base(ServerOpcodes.NpcInteractionOpenResult)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Npc);
            _worldPacket.WriteInt32((int)InteractionType);
            _worldPacket.WriteBit(Success);
            _worldPacket.FlushBits();
        }
    }

    public class GossipMessagePkt : ServerPacket
    {
        public int FriendshipFactionID;
        public ObjectGuid GossipGUID;
        public uint GossipID;

        public List<ClientGossipOptions> GossipOptions = new();
        public List<ClientGossipText> GossipText = new();
        public int? TextID;
        public int? TextID2;

        public GossipMessagePkt() : base(ServerOpcodes.GossipMessage)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GossipGUID);
            _worldPacket.WriteUInt32(GossipID);
            _worldPacket.WriteInt32(FriendshipFactionID);
            _worldPacket.WriteInt32(GossipOptions.Count);
            _worldPacket.WriteInt32(GossipText.Count);
            _worldPacket.WriteBit(TextID.HasValue);
            _worldPacket.WriteBit(TextID2.HasValue);
            _worldPacket.FlushBits();

            foreach (ClientGossipOptions options in GossipOptions)
                options.Write(_worldPacket);

            if (TextID.HasValue)
                _worldPacket.WriteInt32(TextID.Value);

            if (TextID2.HasValue)
                _worldPacket.WriteInt32(TextID2.Value);

            foreach (ClientGossipText text in GossipText)
                text.Write(_worldPacket);
        }
    }

    public class GossipSelectOption : ClientPacket
    {
        public uint GossipID;
        public int GossipOptionID;

        public ObjectGuid GossipUnit;
        public string PromotionCode;

        public GossipSelectOption(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            GossipUnit = _worldPacket.ReadPackedGuid();
            GossipID = _worldPacket.ReadUInt32();
            GossipOptionID = _worldPacket.ReadInt32();

            uint length = _worldPacket.ReadBits<uint>(8);
            PromotionCode = _worldPacket.ReadString(length);
        }
    }

    internal class GossipOptionNPCInteraction : ServerPacket
    {
        public int? FriendshipFactionID;

        public ObjectGuid GossipGUID;
        public int GossipNpcOptionID;

        public GossipOptionNPCInteraction() : base(ServerOpcodes.GossipOptionNpcInteraction)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(GossipGUID);
            _worldPacket.WriteInt32(GossipNpcOptionID);
            _worldPacket.WriteBit(FriendshipFactionID.HasValue);
            _worldPacket.FlushBits();

            if (FriendshipFactionID.HasValue)
                _worldPacket.WriteInt32(FriendshipFactionID.Value);
        }
    }

    public class GossipComplete : ServerPacket
    {
        public bool SuppressSound;

        public GossipComplete() : base(ServerOpcodes.GossipComplete)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteBit(SuppressSound);
            _worldPacket.FlushBits();
        }
    }

    public class VendorInventory : ServerPacket
    {
        public List<VendorItemPkt> Items = new();

        public byte Reason = 0;
        public ObjectGuid Vendor;

        public VendorInventory() : base(ServerOpcodes.VendorInventory, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(Vendor);
            _worldPacket.WriteUInt8(Reason);
            _worldPacket.WriteInt32(Items.Count);

            foreach (VendorItemPkt item in Items)
                item.Write(_worldPacket);
        }
    }

    public class TrainerList : ServerPacket
    {
        public string Greeting;
        public List<TrainerListSpell> Spells = new();

        public ObjectGuid TrainerGUID;
        public int TrainerID = 1;
        public int TrainerType;

        public TrainerList() : base(ServerOpcodes.TrainerList, ConnectionType.Instance)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TrainerGUID);
            _worldPacket.WriteInt32(TrainerType);
            _worldPacket.WriteInt32(TrainerID);

            _worldPacket.WriteInt32(Spells.Count);

            foreach (TrainerListSpell spell in Spells)
            {
                _worldPacket.WriteUInt32(spell.SpellID);
                _worldPacket.WriteUInt32(spell.MoneyCost);
                _worldPacket.WriteUInt32(spell.ReqSkillLine);
                _worldPacket.WriteUInt32(spell.ReqSkillRank);

                for (uint i = 0; i < SharedConst.MaxTrainerspellAbilityReqs; ++i)
                    _worldPacket.WriteUInt32(spell.ReqAbility[i]);

                _worldPacket.WriteUInt8((byte)spell.Usable);
                _worldPacket.WriteUInt8(spell.ReqLevel);
            }

            _worldPacket.WriteBits(Greeting.GetByteCount(), 11);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Greeting);
        }
    }

    internal class GossipPOI : ServerPacket
    {
        public uint Flags;
        public uint Icon;

        public uint Id;
        public uint Importance;
        public string Name;
        public Vector3 Pos;
        public uint WMOGroupID;

        public GossipPOI() : base(ServerOpcodes.GossipPoi)
        {
        }

        public override void Write()
        {
            _worldPacket.WriteUInt32(Id);
            _worldPacket.WriteVector3(Pos);
            _worldPacket.WriteUInt32(Icon);
            _worldPacket.WriteUInt32(Importance);
            _worldPacket.WriteUInt32(WMOGroupID);
            _worldPacket.WriteBits(Flags, 14);
            _worldPacket.WriteBits(Name.GetByteCount(), 6);
            _worldPacket.FlushBits();
            _worldPacket.WriteString(Name);
        }
    }

    internal class SpiritHealerActivate : ClientPacket
    {
        public ObjectGuid Healer;

        public SpiritHealerActivate(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            Healer = _worldPacket.ReadPackedGuid();
        }
    }

    internal class TrainerBuySpell : ClientPacket
    {
        public uint SpellID;

        public ObjectGuid TrainerGUID;
        public uint TrainerID;

        public TrainerBuySpell(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            TrainerGUID = _worldPacket.ReadPackedGuid();
            TrainerID = _worldPacket.ReadUInt32();
            SpellID = _worldPacket.ReadUInt32();
        }
    }

    internal class TrainerBuyFailed : ServerPacket
    {
        public uint SpellID;
        public TrainerFailReason TrainerFailedReason;

        public ObjectGuid TrainerGUID;

        public TrainerBuyFailed() : base(ServerOpcodes.TrainerBuyFailed)
        {
        }

        public override void Write()
        {
            _worldPacket.WritePackedGuid(TrainerGUID);
            _worldPacket.WriteUInt32(SpellID);
            _worldPacket.WriteUInt32((uint)TrainerFailedReason);
        }
    }

    internal class RequestStabledPets : ClientPacket
    {
        public ObjectGuid StableMaster;

        public RequestStabledPets(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            StableMaster = _worldPacket.ReadPackedGuid();
        }
    }

    internal class SetPetSlot : ClientPacket
    {
        public byte DestSlot;
        public uint PetNumber;

        public ObjectGuid StableMaster;

        public SetPetSlot(WorldPacket packet) : base(packet)
        {
        }

        public override void Read()
        {
            PetNumber = _worldPacket.ReadUInt32();
            DestSlot = _worldPacket.ReadUInt8();
            StableMaster = _worldPacket.ReadPackedGuid();
        }
    }

    //Structs
    public struct TreasureItem
    {
        public GossipOptionRewardType Type;
        public int ID;
        public int Quantity;

        public void Write(WorldPacket data)
        {
            data.WriteBits((byte)Type, 1);
            data.WriteInt32(ID);
            data.WriteInt32(Quantity);
        }
    }

    public class TreasureLootList
    {
        public List<TreasureItem> Items = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(Items.Count);

            foreach (TreasureItem treasureItem in Items)
                treasureItem.Write(data);
        }
    }

    public class ClientGossipOptions
    {
        public string Confirm = "";
        public GossipOptionFlags Flags;
        public int GossipOptionID;
        public int OptionCost;
        public byte OptionFlags;
        public uint OptionLanguage;
        public GossipOptionNpc OptionNPC;
        public int OrderIndex;
        public int? OverrideIconID;
        public int? SpellID;
        public GossipOptionStatus Status;
        public string Text = "";
        public TreasureLootList Treasure = new();

        public void Write(WorldPacket data)
        {
            data.WriteInt32(GossipOptionID);
            data.WriteUInt8((byte)OptionNPC);
            data.WriteInt8((sbyte)OptionFlags);
            data.WriteInt32(OptionCost);
            data.WriteUInt32(OptionLanguage);
            data.WriteInt32((int)Flags);
            data.WriteInt32(OrderIndex);
            data.WriteBits(Text.GetByteCount(), 12);
            data.WriteBits(Confirm.GetByteCount(), 12);
            data.WriteBits((byte)Status, 2);
            data.WriteBit(SpellID.HasValue);
            data.WriteBit(OverrideIconID.HasValue);
            data.FlushBits();

            Treasure.Write(data);

            data.WriteString(Text);
            data.WriteString(Confirm);

            if (SpellID.HasValue)
                data.WriteInt32(SpellID.Value);

            if (OverrideIconID.HasValue)
                data.WriteInt32(OverrideIconID.Value);
        }
    }

    public class ClientGossipText
    {
        public uint ContentTuningID;
        public uint QuestFlags;
        public uint QuestFlagsEx;
        public uint QuestID;
        public string QuestTitle;
        public int QuestType;
        public bool Repeatable;

        public void Write(WorldPacket data)
        {
            data.WriteUInt32(QuestID);
            data.WriteUInt32(ContentTuningID);
            data.WriteInt32(QuestType);
            data.WriteUInt32(QuestFlags);
            data.WriteUInt32(QuestFlagsEx);

            data.WriteBit(Repeatable);
            data.WriteBits(QuestTitle.GetByteCount(), 9);
            data.FlushBits();

            data.WriteString(QuestTitle);
        }
    }

    public class VendorItemPkt
    {
        public bool DoNotFilterOnVendor;
        public int Durability;
        public int ExtendedCostID;
        public ItemInstance Item = new();
        public bool Locked;

        public int MuID;
        public int PlayerConditionFailed;
        public ulong Price;
        public int Quantity = -1;
        public bool Refundable;
        public int StackCount;
        public int Type;

        public void Write(WorldPacket data)
        {
            data.WriteUInt64(Price);
            data.WriteInt32(MuID);
            data.WriteInt32(Durability);
            data.WriteInt32(StackCount);
            data.WriteInt32(Quantity);
            data.WriteInt32(ExtendedCostID);
            data.WriteInt32(PlayerConditionFailed);
            data.WriteBits(Type, 3);
            data.WriteBit(Locked);
            data.WriteBit(DoNotFilterOnVendor);
            data.WriteBit(Refundable);
            data.FlushBits();

            Item.Write(data);
        }
    }

    public class TrainerListSpell
    {
        public uint MoneyCost;
        public uint[] ReqAbility = new uint[SharedConst.MaxTrainerspellAbilityReqs];
        public byte ReqLevel;
        public uint ReqSkillLine;
        public uint ReqSkillRank;
        public uint SpellID;
        public TrainerSpellState Usable;
    }
}