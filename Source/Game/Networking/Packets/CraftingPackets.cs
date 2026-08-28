// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    public struct CraftingReagentBase
    {
        public int? ItemID;
        public int? CurrencyID;

        public void Read(WorldPacket data)
        {
            data.ResetBitPos();
            bool HasItemID = data.HasBit();
            bool HasCurrencyID = data.HasBit();

            if (HasItemID)
                ItemID = data.ReadInt32();

            if (HasCurrencyID)
                CurrencyID = data.ReadInt32();
        }

        public void Write(WorldPacket data)
        {
            data.WriteBit(ItemID.HasValue);
            data.WriteBit(CurrencyID.HasValue);
            data.FlushBits();

            if (ItemID.HasValue)
                data.WriteInt32(ItemID.Value);

            if (CurrencyID.HasValue)
                data.WriteInt32(CurrencyID.Value);
        }
    }

    struct SpellReducedReagent
    {
        public CraftingReagentBase Reagent;
        public int Quantity;

        public void Write(WorldPacket data)
        {
            Reagent.Write(data);
            data.WriteInt32(Quantity);
        }
    }

    class CraftingData
    {
        public int CraftingQualityID;
        public float QualityProgress;
        public int SkillLineAbilityID;
        public int CraftingDataID;
        public int Multicraft;
        public int SkillFromReagents;
        public int Skill;
        public int CritBonusSkill;
        public float ModSkillGain;
        public ulong OrderID;
        public bool IsCrit;
        public bool IsRecraft;
        public bool IsInitialRecraft;
        public bool IsFirstCraft;
        public List<SpellReducedReagent> ResourcesReturned = new();
        public uint OperationID;
        public ObjectGuid ItemGUID;
        public int Quantity;
        public ItemInstance OldItem = new();
        public ItemInstance NewItem = new();
        public int EnchantID;
        public int ConcentrationCurrencyID;
        public int ConcentrationSpent;
        public int IngenuityRefund;
        public bool HasIngenuityProc;
        public bool ApplyConcentration;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(CraftingQualityID);
            data.WriteFloat(QualityProgress);
            data.WriteInt32(SkillLineAbilityID);
            data.WriteInt32(CraftingDataID);
            data.WriteInt32(Multicraft);
            data.WriteInt32(SkillFromReagents);
            data.WriteInt32(Skill);
            data.WriteInt32(CritBonusSkill);
            data.WriteFloat(ModSkillGain);
            data.WriteUInt64(OrderID);
            data.WriteInt32(ResourcesReturned.Count);
            data.WriteUInt32(OperationID);
            data.WritePackedGuid(ItemGUID);
            data.WriteInt32(Quantity);
            OldItem.Write(data);
            NewItem.Write(data);
            data.WriteInt32(EnchantID);
            data.WriteInt32(ConcentrationCurrencyID);
            data.WriteInt32(ConcentrationSpent);
            data.WriteInt32(IngenuityRefund);

            foreach (SpellReducedReagent spellReducedReagent in ResourcesReturned)
                spellReducedReagent.Write(data);

            data.WriteBit(IsCrit);
            data.WriteBit(IsRecraft);
            data.WriteBit(IsInitialRecraft);
            data.WriteBit(IsFirstCraft);
            data.WriteBit(HasIngenuityProc);
            data.WriteBit(ApplyConcentration);
            data.FlushBits();
        }
    }
}
