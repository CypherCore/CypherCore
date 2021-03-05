﻿/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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

using System.Collections.Generic;
using Game.Networking;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using System.Linq;

namespace Game.Entities
{
    public class AzeriteEmpoweredItem : Item
    {
        private AzeriteEmpoweredItemData m_azeriteEmpoweredItemData;
        private List<AzeritePowerSetMemberRecord> m_azeritePowers;
        private int m_maxTier;

        public AzeriteEmpoweredItem()
        {
            ObjectTypeMask |= TypeMask.AzeriteEmpoweredItem;
            ObjectTypeId = TypeId.AzeriteEmpoweredItem;

            m_azeriteEmpoweredItemData = new AzeriteEmpoweredItemData();
        }

        public override bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
        {
            if (!base.Create(guidlow, itemId, context, owner))
                return false;

            InitAzeritePowerData();
            return true;
        }

        public override void SaveToDB(SQLTransaction trans)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            switch (GetState())
            {
                case ItemUpdateState.New:
                case ItemUpdateState.Changed:
                    stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_EMPOWERED);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                        stmt.AddValue(1 + i, m_azeriteEmpoweredItemData.Selections[i]);

                    trans.Append(stmt);
                    break;
            }

            base.SaveToDB(trans);
        }

        public void LoadAzeriteEmpoweredItemData(Player owner, AzeriteEmpoweredData azeriteEmpoweredItem)
        {
            InitAzeritePowerData();
            var needSave = false;
            if (m_azeritePowers != null)
            {
                for (var i = SharedConst.MaxAzeriteEmpoweredTier; --i >= 0;)
                {
                    var selection = azeriteEmpoweredItem.SelectedAzeritePowers[i];
                    if (GetTierForAzeritePower(owner.GetClass(), selection) != i)
                    {
                        needSave = true;
                        break;
                    }

                    SetSelectedAzeritePower(i, selection);
                }
            }
            else
                needSave = true;

            if (needSave)
            {
                var stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_EMPOWERED);
                for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                    stmt.AddValue(i, m_azeriteEmpoweredItemData.Selections[i]);

                stmt.AddValue(5, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public static new void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
        {
            var stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public override void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(trans, GetGUID().GetCounter());
            base.DeleteFromDB(trans);
        }

        public uint GetRequiredAzeriteLevelForTier(uint tier)
        {
            return Global.DB2Mgr.GetRequiredAzeriteLevelForAzeritePowerTier(_bonusData.AzeriteTierUnlockSetId, GetContext(), tier);
        }

        public int GetTierForAzeritePower(Class playerClass, int azeritePowerId)
        {
            var azeritePowerItr = m_azeritePowers.Find(power =>
            {
                return power.AzeritePowerID == azeritePowerId && power.Class == (int)playerClass;
            });

            if (azeritePowerItr != null)
                return azeritePowerItr.Tier;

            return SharedConst.MaxAzeriteEmpoweredTier;
        }

        public void SetSelectedAzeritePower(int tier, int azeritePowerId)
        {
            SetUpdateFieldValue(ref m_values.ModifyValue(m_azeriteEmpoweredItemData).ModifyValue(m_azeriteEmpoweredItemData.Selections, tier), azeritePowerId);

            // Not added to UF::ItemData::BonusListIDs, client fakes it on its own too
            _bonusData.AddBonusList(CliDB.AzeritePowerStorage.LookupByKey(azeritePowerId).ItemBonusListID);
        }

        private void ClearSelectedAzeritePowers()
        {
            for (var i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                SetUpdateFieldValue(ref m_values.ModifyValue(m_azeriteEmpoweredItemData).ModifyValue(m_azeriteEmpoweredItemData.Selections, i), 0);

            _bonusData = new BonusData(GetTemplate());
            foreach (var bonusListID in (List<uint>)m_itemData.BonusListIDs)
                _bonusData.AddBonusList(bonusListID);
        }

        public long GetRespecCost()
        {
            var owner = GetOwner();
            if (owner != null)
                return (long)(MoneyConstants.Gold * Global.DB2Mgr.GetCurveValueAt((uint)Curves.AzeriteEmpoweredItemRespecCost, (float)owner.GetNumRespecs()));

            return (long)PlayerConst.MaxMoneyAmount + 1;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var buffer = new WorldPacket();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_itemData.WriteCreate(buffer, flags, this, target);
            m_azeriteEmpoweredItemData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var buffer = new WorldPacket();

            if (m_values.HasChanged(TypeId.Object))
                m_objectData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.Item))
                m_itemData.WriteUpdate(buffer, flags, this, target);

            if (m_values.HasChanged(TypeId.AzeriteEmpoweredItem))
                m_azeriteEmpoweredItemData.WriteUpdate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            data.WriteBytes(buffer);
        }

        private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedAzeriteEmpoweredItemMask, Player target)
        {
            var flags = GetUpdateFieldFlagsFor(target);
            var valuesMask = new UpdateMask((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            m_itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);
            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            if (requestedAzeriteEmpoweredItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.AzeriteEmpoweredItem);

            var buffer = new WorldPacket();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                m_itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            if (valuesMask[(int)TypeId.AzeriteEmpoweredItem])
                m_azeriteEmpoweredItemData.WriteUpdate(buffer, requestedAzeriteEmpoweredItemMask, true, this, target);

            var buffer1 = new WorldPacket();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_azeriteEmpoweredItemData);
            base.ClearUpdateMask(remove);
        }

        private void InitAzeritePowerData()
        {
            m_azeritePowers = Global.DB2Mgr.GetAzeritePowers(GetEntry());
            if (m_azeritePowers != null)
                m_maxTier = m_azeritePowers.Aggregate((a1, a2) => a1.Tier < a2.Tier ? a2 : a1).Tier;
        }

        public int GetMaxAzeritePowerTier() { return m_maxTier; }
        public uint GetSelectedAzeritePower(int tier)
        {
            return (uint)m_azeriteEmpoweredItemData.Selections[tier];
        }
    }
}
