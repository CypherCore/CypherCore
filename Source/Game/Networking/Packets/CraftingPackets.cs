// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Entities;
using System.Collections.Generic;

namespace Game.Networking.Packets
{
    struct SpellReducedReagent
    {
        public int ItemID;
        public int Quantity;

        public void Write(WorldPacket data)
        {
            data.WriteInt32(ItemID);
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
        public float field_1C;
        public ulong field_20;
        public bool IsCrit;
        public bool field_29;
        public bool field_2A;
        public bool BonusCraft;
        public List<SpellReducedReagent> ResourcesReturned = new();
        public uint OperationID;
        public ObjectGuid ItemGUID;
        public int Quantity;
        public ItemInstance OldItem = new();
        public ItemInstance NewItem = new();
        public int EnchantID;

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
            data.WriteFloat(field_1C);
            data.WriteUInt64(field_20);
            data.WriteInt32(ResourcesReturned.Count);
            data.WriteUInt32(OperationID);
            data.WritePackedGuid(ItemGUID);
            data.WriteInt32(Quantity);
            data.WriteInt32(EnchantID);

            foreach (SpellReducedReagent spellReducedReagent in ResourcesReturned)
                spellReducedReagent.Write(data);

            data.WriteBit(IsCrit);
            data.WriteBit(field_29);
            data.WriteBit(field_2A);
            data.WriteBit(BonusCraft);
            data.FlushBits();

            OldItem.Write(data);
            NewItem.Write(data);
        }
    }
}
