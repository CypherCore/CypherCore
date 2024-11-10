// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;
using System.Collections.Generic;
using System.Linq;

namespace Game.Entities
{
    public class AzeriteEmpoweredItem : Item
    {
        AzeriteEmpoweredItemData m_azeriteEmpoweredItemData;
        List<AzeritePowerSetMemberRecord> m_azeritePowers;
        int m_maxTier;

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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
            stmt.AddValue(0, GetGUID().GetCounter());
            trans.Append(stmt);

            switch (GetState())
            {
                case ItemUpdateState.New:
                case ItemUpdateState.Changed:
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_EMPOWERED);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                        stmt.AddValue(1 + i, m_azeriteEmpoweredItemData.Selections[i]);

                    trans.Append(stmt);
                    break;
            }

            base.SaveToDB(trans);
        }

        public void LoadAzeriteEmpoweredItemData(Player owner, AzeriteEmpoweredData azeriteEmpoweredItem)
        {
            InitAzeritePowerData();
            bool needSave = false;
            if (m_azeritePowers != null)
            {
                for (int i = SharedConst.MaxAzeriteEmpoweredTier; --i >= 0;)
                {
                    int selection = azeriteEmpoweredItem.SelectedAzeritePowers[i];
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
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_EMPOWERED);
                for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                    stmt.AddValue(i, m_azeriteEmpoweredItemData.Selections[i]);

                stmt.AddValue(5, GetGUID().GetCounter());
                DB.Characters.Execute(stmt);
            }
        }

        public static new void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
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

        void ClearSelectedAzeritePowers()
        {
            for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                SetUpdateFieldValue(ref m_values.ModifyValue(m_azeriteEmpoweredItemData).ModifyValue(m_azeriteEmpoweredItemData.Selections, i), 0);

            _bonusData = new BonusData(GetTemplate());
            foreach (uint bonusListID in GetBonusListIDs())
                _bonusData.AddBonusList(bonusListID);
        }

        public long GetRespecCost()
        {
            Player owner = GetOwner();
            if (owner != null)
                return (long)(MoneyConstants.Gold * Global.DB2Mgr.GetCurveValueAt((uint)Curves.AzeriteEmpoweredItemRespecCost, (float)owner.GetNumRespecs()));

            return (long)PlayerConst.MaxMoneyAmount + 1;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            buffer.WriteUInt8((byte)flags);
            m_objectData.WriteCreate(buffer, flags, this, target);
            m_itemData.WriteCreate(buffer, flags, this, target);
            m_azeriteEmpoweredItemData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

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

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedAzeriteEmpoweredItemMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            m_itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);
            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            if (requestedAzeriteEmpoweredItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.AzeriteEmpoweredItem);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                m_itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            if (valuesMask[(int)TypeId.AzeriteEmpoweredItem])
                m_azeriteEmpoweredItemData.WriteUpdate(buffer, requestedAzeriteEmpoweredItemMask, true, this, target);

            WorldPacket buffer1 = new();
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

        void InitAzeritePowerData()
        {
            m_azeritePowers = Global.DB2Mgr.GetAzeritePowers(GetEntry());
            if (m_azeritePowers != null)
                m_maxTier = m_azeritePowers.Max(p => p.Tier);
        }

        public int GetMaxAzeritePowerTier() { return m_maxTier; }

        public uint GetSelectedAzeritePower(int tier)
        {
            return (uint)m_azeriteEmpoweredItemData.Selections[tier];
        }

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            AzeriteEmpoweredItem Owner;
            ObjectFieldData ObjectMask = new();
            ItemData ItemMask = new();
            AzeriteEmpoweredItemData AzeriteEmpoweredItemMask = new();

            public ValuesUpdateForPlayerWithMaskSender(AzeriteEmpoweredItem owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ItemMask.GetUpdateMask(), AzeriteEmpoweredItemMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }
}
