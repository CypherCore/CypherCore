// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Game.Networking;
using Framework.Constants;
using Framework.Database;
using Game.Networking.Packets;
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

        public override bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
        {
            if (!base.Create(guidlow, itemId, context, owner))
                return false;

            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.Level), 1u);
            SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.KnowledgeLevel), GetCurrentKnowledgeLevel());
            UnlockDefaultMilestones();
            return true;
        }

        public override void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            switch (GetState())
            {
                case ItemUpdateState.New:
                case ItemUpdateState.Changed:
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    stmt.AddValue(1, m_azeriteItemData.Xp);
                    stmt.AddValue(2, m_azeriteItemData.Level);
                    stmt.AddValue(3, m_azeriteItemData.KnowledgeLevel);

                    int specIndex = 0;
                    for (; specIndex < m_azeriteItemData.SelectedEssences.Size(); ++specIndex)
                    {
                        stmt.AddValue(4 + specIndex * 5, m_azeriteItemData.SelectedEssences[specIndex].SpecializationID);
                        for (int j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                            stmt.AddValue(5 + specIndex * 5 + j, m_azeriteItemData.SelectedEssences[specIndex].AzeriteEssenceID[j]);
                    }
                    for (; specIndex < 4; ++specIndex)
                    {
                        stmt.AddValue(4 + specIndex * 5, 0);
                        for (int j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                            stmt.AddValue(5 + specIndex * 5 + j, 0);
                    }

                    trans.Append(stmt);

                    foreach (uint azeriteItemMilestonePowerId in m_azeriteItemData.UnlockedEssenceMilestones)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, azeriteItemMilestonePowerId);
                        trans.Append(stmt);
                    }

                    foreach (var azeriteEssence in m_azeriteItemData.UnlockedEssences)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, azeriteEssence.AzeriteEssenceID);
                        stmt.AddValue(2, azeriteEssence.Rank);
                        trans.Append(stmt);
                    }
                    break;
            }

            base.SaveToDB(trans);
        }

        public void LoadAzeriteItemData(Player owner, AzeriteData azeriteData)
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

            foreach (uint azeriteItemMilestonePowerId in azeriteData.AzeriteItemMilestonePowers)
                AddUnlockedEssenceMilestone(azeriteItemMilestonePowerId);

            UnlockDefaultMilestones();

            foreach (AzeriteEssencePowerRecord unlockedAzeriteEssence in azeriteData.UnlockedAzeriteEssences)
                SetEssenceRank((uint)unlockedAzeriteEssence.AzeriteEssenceID, unlockedAzeriteEssence.Tier);

            foreach (AzeriteItemSelectedEssencesData selectedEssenceData in azeriteData.SelectedAzeriteEssences)
            {
                if (selectedEssenceData.SpecializationId == 0)
                    continue;

                var selectedEssences = new SelectedAzeriteEssences();
                selectedEssences.ModifyValue(selectedEssences.SpecializationID).SetValue(selectedEssenceData.SpecializationId);
                for (int i = 0; i < SharedConst.MaxAzeriteEssenceSlot; ++i)
                {
                    // Check if essence was unlocked
                    if (GetEssenceRank(selectedEssenceData.AzeriteEssenceId[i]) == 0)
                        continue;

                    selectedEssences.ModifyValue(selectedEssences.AzeriteEssenceID, i) = selectedEssenceData.AzeriteEssenceId[i];
                }

                if (owner != null && owner.GetPrimarySpecialization() == (ChrSpecialization)selectedEssenceData.SpecializationId)
                    selectedEssences.ModifyValue(selectedEssences.Enabled).SetValue(true);

                AddDynamicUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.SelectedEssences), selectedEssences);
            }

            // add selected essences for current spec
            if (owner != null && GetSelectedAzeriteEssences() == null)
                CreateSelectedAzeriteEssences((uint)owner.GetPrimarySpecialization());

            if (needSave)
            {
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_ON_LOAD);
                stmt.AddValue(0, azeriteData.Xp);
                stmt.AddValue(1, azeriteData.KnowledgeLevel);
                stmt.AddValue(2, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public override void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(trans, GetGUID().GetCounter());
            base.DeleteFromDB(trans);
        }

        public new static void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_MILESTONE_POWER);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_UNLOCKED_ESSENCE);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public uint GetLevel() { return m_azeriteItemData.Level; }

        public uint GetEffectiveLevel()
        {
            uint level = m_azeriteItemData.AuraLevel;
            if (level == 0)
                level = m_azeriteItemData.Level;

            return level;
        }

        uint GetCurrentKnowledgeLevel()
        {
            // count weeks from 14.01.2020
            DateTime now = GameTime.GetDateAndTime();
            DateTime beginDate = new(2020, 1, 14);
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

                owner.UpdateCriteria(CriteriaType.EarnArtifactXPForAzeriteItem, xp);

                // changing azerite level changes item level, need to update stats
                if (m_azeriteItemData.Level != level)
                {
                    if (IsEquipped())
                        owner._ApplyItemBonuses(this, GetSlot(), false);

                    SetUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.Level), level);
                    UnlockDefaultMilestones();
                    owner.UpdateCriteria(CriteriaType.AzeriteLevelReached, level);

                    if (IsEquipped())
                        owner._ApplyItemBonuses(this, GetSlot(), true);
                }

                SetState(ItemUpdateState.Changed, owner);
            }

            PlayerAzeriteItemGains xpGain = new();
            xpGain.ItemGUID = GetGUID();
            xpGain.XP = xp;
            owner.SendPacket(xpGain);
        }

        public static GameObject FindHeartForge(Player owner)
        {
            GameObject forge = owner.FindNearestGameObjectOfType(GameObjectTypes.ItemForge, 40.0f);
            if (forge != null)
                if (forge.GetGoInfo().ItemForge.ForgeType == 2)
                    return forge;

            return null;
        }

        public bool CanUseEssences()
        {
            return ConditionManager.IsPlayerMeetingCondition(GetOwner(), PlayerConst.PlayerConditionIdUnlockedAzeriteEssences);
        }

        public bool HasUnlockedEssenceSlot(byte slot)
        {
            AzeriteItemMilestonePowerRecord milestone = Global.DB2Mgr.GetAzeriteItemMilestonePower(slot);
            return m_azeriteItemData.UnlockedEssenceMilestones.FindIndex(milestone.Id) != -1;
        }

        public bool HasUnlockedEssenceMilestone(uint azeriteItemMilestonePowerId) { return m_azeriteItemData.UnlockedEssenceMilestones.FindIndex(azeriteItemMilestonePowerId) != -1; }

        public void AddUnlockedEssenceMilestone(uint azeriteItemMilestonePowerId)
        {
            AddDynamicUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.UnlockedEssenceMilestones), azeriteItemMilestonePowerId);
        }

        public uint GetEssenceRank(uint azeriteEssenceId)
        {
            int index = m_azeriteItemData.UnlockedEssences.FindIndexIf(essence =>
            {
                return essence.AzeriteEssenceID == azeriteEssenceId;
            });

            if (index < 0)
                return 0;

            return m_azeriteItemData.UnlockedEssences[index].Rank;
        }

        public void SetEssenceRank(uint azeriteEssenceId, uint rank)
        {
            int index = m_azeriteItemData.UnlockedEssences.FindIndexIf(essence =>
            {
                return essence.AzeriteEssenceID == azeriteEssenceId;
            });

            if (rank == 0 && index >= 0)
            {
                RemoveDynamicUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.UnlockedEssences), index);
                return;
            }

            if (Global.DB2Mgr.GetAzeriteEssencePower(azeriteEssenceId, rank) == null)
                return;

            if (index < 0)
            {
                UnlockedAzeriteEssence unlockedEssence = new();
                unlockedEssence.AzeriteEssenceID = azeriteEssenceId;
                unlockedEssence.Rank = rank;
                AddDynamicUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.UnlockedEssences), unlockedEssence);
            }
            else
            {
                UnlockedAzeriteEssence actorField = m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.UnlockedEssences, index);
                SetUpdateFieldValue(ref actorField.Rank, rank);
            }
        }

        public SelectedAzeriteEssences GetSelectedAzeriteEssences()
        {
            foreach (SelectedAzeriteEssences essences in m_azeriteItemData.SelectedEssences)
                if (essences.Enabled)
                    return essences;

            return null;
        }

        public void CreateSelectedAzeriteEssences(uint specializationId)
        {
            SelectedAzeriteEssences selectedEssences = new();
            selectedEssences.ModifyValue(selectedEssences.SpecializationID).SetValue(specializationId);
            selectedEssences.ModifyValue(selectedEssences.Enabled).SetValue(true);
            AddDynamicUpdateFieldValue(m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.SelectedEssences), selectedEssences);
        }

        public void SetSelectedAzeriteEssences(uint specializationId)
        {
            int index = m_azeriteItemData.SelectedEssences.FindIndexIf(essences => { return essences.Enabled; });
            if (index >= 0)
            {
                SelectedAzeriteEssences selectedEssences = m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.SelectedEssences, index);
                SetUpdateFieldValue(selectedEssences.ModifyValue(selectedEssences.Enabled), false);
            }

            index = m_azeriteItemData.SelectedEssences.FindIndexIf(essences =>
            {
                return essences.SpecializationID == specializationId;
            });

            if (index >= 0)
            {
                SelectedAzeriteEssences selectedEssences = m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.SelectedEssences, index);
                SetUpdateFieldValue(selectedEssences.ModifyValue(selectedEssences.Enabled), true);
            }
            else
                CreateSelectedAzeriteEssences(specializationId);
        }

        public void SetSelectedAzeriteEssence(int slot, uint azeriteEssenceId)
        {
            //ASSERT(slot < MAX_AZERITE_ESSENCE_SLOT);
            int index = m_azeriteItemData.SelectedEssences.FindIndexIf(essences => { return essences.Enabled; });
            //ASSERT(index >= 0);
            SelectedAzeriteEssences selectedEssences = m_values.ModifyValue(m_azeriteItemData).ModifyValue(m_azeriteItemData.SelectedEssences, index);
            SetUpdateFieldValue(ref selectedEssences.ModifyValue(selectedEssences.AzeriteEssenceID, slot), azeriteEssenceId);
        }

        public override void BuildValuesCreate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            m_objectData.WriteCreate(data, flags, this, target);
            m_itemData.WriteCreate(data, flags, this, target);
            m_azeriteItemData.WriteCreate(data, flags, this, target);
        }

        public override void BuildValuesUpdate(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.Item))
                m_itemData.WriteUpdate(data, flags, this, target);

            if (m_values.HasChanged(TypeId.AzeriteItem))
                m_azeriteItemData.WriteUpdate(data, flags, this, target);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            valuesMask.Set((int)TypeId.Item);
            valuesMask.Set((int)TypeId.AzeriteItem);

            data.WriteUInt32(valuesMask.GetBlock(0));

            UpdateMask mask = m_itemData.GetStaticUpdateMask();
            m_itemData.AppendAllowedFieldsMaskForFlag(mask, flags);
            m_itemData.WriteUpdate(data, mask, true, this, target);

            UpdateMask mask2 = m_azeriteItemData.GetStaticUpdateMask();
            m_azeriteItemData.AppendAllowedFieldsMaskForFlag(mask2, flags);
            m_azeriteItemData.WriteUpdate(data, mask2, true, this, target);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedAzeriteItemMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            m_itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);
            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            m_azeriteItemData.FilterDisallowedFieldsMaskForFlag(requestedAzeriteItemMask, flags);
            if (requestedAzeriteItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.AzeriteItem);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                m_itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            if (valuesMask[(int)TypeId.AzeriteItem])
                m_azeriteItemData.WriteUpdate(buffer, requestedAzeriteItemMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_azeriteItemData);
            base.ClearUpdateMask(remove);
        }

        void UnlockDefaultMilestones()
        {
            bool hasPreviousMilestone = true;
            foreach (AzeriteItemMilestonePowerRecord milestone in Global.DB2Mgr.GetAzeriteItemMilestonePowers())
            {
                if (!hasPreviousMilestone)
                    break;

                if (milestone.RequiredLevel > GetLevel())
                    break;

                if (HasUnlockedEssenceMilestone(milestone.Id))
                    continue;

                if (milestone.AutoUnlock != 0)
                {
                    AddUnlockedEssenceMilestone(milestone.Id);
                    hasPreviousMilestone = true;
                }
                else
                    hasPreviousMilestone = false;
            }
        }

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            AzeriteItem Owner;
            ObjectFieldData ObjectMask = new();
            ItemData ItemMask = new();
            AzeriteItemData AzeriteItemMask = new();

            public ValuesUpdateForPlayerWithMaskSender(AzeriteItem owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ItemMask.GetUpdateMask(), AzeriteItemMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }

    public class AzeriteItemSelectedEssencesData
    {
        public uint SpecializationId;
        public uint[] AzeriteEssenceId = new uint[SharedConst.MaxAzeriteEssenceSlot];
    }

    public class AzeriteData
    {
        public ulong Xp;
        public uint Level;
        public uint KnowledgeLevel;
        public List<uint> AzeriteItemMilestonePowers = new();
        public List<AzeriteEssencePowerRecord> UnlockedAzeriteEssences = new();
        public AzeriteItemSelectedEssencesData[] SelectedAzeriteEssences = new AzeriteItemSelectedEssencesData[4];
    }
}
