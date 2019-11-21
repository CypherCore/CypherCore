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

using System;
using System.Collections.Generic;
using System.Text;
using Game.Network;
using Framework.Constants;
using Framework.Database;
using Game.Network.Packets;
using Game.DataStorage;

namespace Game.Entities
{
    public class AzeriteItem : Item
    {
        public AzeriteItemData m_azeriteItemData;

        public AzeriteItem()
        {
            m_azeriteItemData = new AzeriteItemData();

            ObjectTypeMask |= TypeMask.AzeriteItem;
            ObjectTypeId = TypeId.AzeriteItem;

            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.DEBUGknowledgeWeek), -1);
        }

        public override bool Create(ulong guidlow, uint itemId, Player owner)
        {
            if (!base.Create(guidlow, itemId, owner))
                return false;

            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.Level), 1u);
            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.KnowledgeLevel), GetCurrentKnowledgeLevel());
            return true;
        }

        public override void SaveToDB(SQLTransaction trans)
        {
            base.SaveToDB(trans);

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, m_azeriteItemData.Xp);
            stmt.AddValue(2, m_azeriteItemData.Level);
            stmt.AddValue(3, m_azeriteItemData.KnowledgeLevel);
            trans.Append(stmt);
        }

        public void LoadAzeriteItemData(AzeriteData azeriteData)
        {
            bool needSave = false;

            if (!CliDB.AzeriteLevelInfoStorage.ContainsKey(azeriteData.Level))
            {
                azeriteData.Xp = 0;
                azeriteData.Level = 1;
                azeriteData.KnowledgeLevel = GetCurrentKnowledgeLevel();
                needSave = true;
            }
            else if (azeriteData.Level > PlayerConst.MaxAzeriteItemLevel)
            {
                azeriteData.Xp = 0;
                azeriteData.Level = PlayerConst.MaxAzeriteItemLevel;
                needSave = true;
            }

            if (azeriteData.KnowledgeLevel != GetCurrentKnowledgeLevel())
            {
                // rescale XP to maintain same progress %
                ulong oldMax = CalcTotalXPToNextLevel(azeriteData.Level, azeriteData.KnowledgeLevel);
                azeriteData.KnowledgeLevel = GetCurrentKnowledgeLevel();
                ulong newMax = CalcTotalXPToNextLevel(azeriteData.Level, azeriteData.KnowledgeLevel);
                azeriteData.Xp = (ulong)(azeriteData.Xp / (double)oldMax * newMax);
                needSave = true;
            }
            else if (azeriteData.KnowledgeLevel > PlayerConst.MaxAzeriteItemKnowledgeLevel)
            {
                azeriteData.KnowledgeLevel = PlayerConst.MaxAzeriteItemKnowledgeLevel;
                needSave = true;
            }

            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.Xp), azeriteData.Xp);
            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.Level), azeriteData.Level);
            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.KnowledgeLevel), azeriteData.KnowledgeLevel);

            if (needSave)
            {
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_ON_LOAD);
                stmt.AddValue(0, azeriteData.Xp);
                stmt.AddValue(1, azeriteData.KnowledgeLevel);
                stmt.AddValue(2, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public override void DeleteFromDB(SQLTransaction trans)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            base.DeleteFromDB(trans);
        }

        public override uint GetItemLevel(Player owner)
        {
            return CliDB.AzeriteLevelInfoStorage.LookupByKey(m_azeriteItemData.Level).ItemLevel;
        }

        uint GetLevel() { return m_azeriteItemData.Level; }
        uint GetEffectiveLevel()
        {
            uint level = m_azeriteItemData.AuraLevel;
            if (level == 0)
                level = m_azeriteItemData.Level;

            return level;
        }

        uint GetCurrentKnowledgeLevel()
        {
            // count weeks from 26.06.2019
            DateTime now = GameTime.GetDateAndTime();
            DateTime beginDate = new DateTime(2019, 6, 26);
            uint knowledge = 0;
            while (beginDate < now && knowledge < PlayerConst.MaxAzeriteItemKnowledgeLevel)
            {
                ++knowledge;
                beginDate.AddDays(7);
            }
            return knowledge;
        }

        ulong CalcTotalXPToNextLevel(uint level, uint knowledgeLevel)
        {
            AzeriteLevelInfoRecord levelInfo = CliDB.AzeriteLevelInfoStorage.LookupByKey(level);
            ulong totalXp = levelInfo.BaseExperienceToNextLevel * (ulong)CliDB.AzeriteKnowledgeMultiplierStorage.LookupByKey(knowledgeLevel).Multiplier;
            return Math.Max(totalXp, levelInfo.MinimumExperienceToNextLevel);
        }

        public void GiveXP(ulong xp)
        {
            Player owner = GetOwner();
            uint level = m_azeriteItemData.Level;
            if (level < PlayerConst.MaxAzeriteItemLevel)
            {
                ulong currentXP = m_azeriteItemData.Xp;
                ulong remainingXP = xp;
                do
                {
                    ulong totalXp = CalcTotalXPToNextLevel(level, m_azeriteItemData.KnowledgeLevel);
                    if (currentXP + remainingXP >= totalXp)
                    {
                        // advance to next level
                        ++level;
                        remainingXP -= totalXp - currentXP;
                        currentXP = 0;
                    }
                    else
                    {
                        currentXP += remainingXP;
                        remainingXP = 0;
                    }
                } while (remainingXP > 0 && level < PlayerConst.MaxAzeriteItemLevel);

                SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.Xp), currentXP);

                owner.UpdateCriteria(CriteriaTypes.HeartOfAzerothArtifactPowerEarned, xp);

                // changing azerite level changes item level, need to update stats
                if (m_azeriteItemData.Level != level)
                {
                    if (IsEquipped())
                        owner._ApplyItemBonuses(this, GetSlot(), false);

                    SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData. Level), level);
                    owner.UpdateCriteria(CriteriaTypes.HeartOfAzerothLevelReached, level);

                    if (IsEquipped())
                        owner._ApplyItemBonuses(this, GetSlot(), true);
                }

                SetState(ItemUpdateState.Changed, owner);
            }

            AzeriteXpGain xpGain = new AzeriteXpGain();
            xpGain.ItemGUID = GetGUID();
            xpGain.XP = xp;
            owner.SendPacket(xpGain);
        }

        public SelectedAzeriteEssences GetSelectedAzeriteEssences()
        {
            foreach (SelectedAzeriteEssences essences in m_azeriteItemData.SelectedEssences)
                if (essences.Enabled != 0)
                    return essences;

            return null;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_itemData.WriteCreate(buffer, flags, this, target);
            m_azeriteItemData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new WorldPacket();

            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.Item))
                m_itemData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.AzeriteItem))
                m_azeriteItemData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new UpdateMask(14);
            valuesMask.Set((int)TypeId.Item);
            valuesMask.Set((int)TypeId.AzeriteItem);

            WorldPacket buffer = new WorldPacket();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            UpdateMask mask = new UpdateMask(40);
            m_itemData.AppendAllowedFieldsMaskForFlag(mask, flags);
            m_itemData.WriteUpdate(buffer, mask, flags, this, target);

            UpdateMask mask2 = new UpdateMask(9);
            m_azeriteItemData.AppendAllowedFieldsMaskForFlag(mask2, flags);
            m_azeriteItemData.WriteUpdate(buffer, mask2, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_azeriteItemData);
            base.ClearUpdateMask(remove);
        }
    }

    public class AzeriteData
    {
        public ulong Xp;
        public uint Level;
        public uint KnowledgeLevel;
    }
}
