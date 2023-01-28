// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Networking;
using Game.Networking.Packets;

namespace Game.Entities
{
	public class AzeriteEmpoweredItem : Item
	{
		private AzeriteEmpoweredItemData _azeriteEmpoweredItemData;
		private List<AzeritePowerSetMemberRecord> _azeritePowers;
		private int _maxTier;

		public AzeriteEmpoweredItem()
		{
			ObjectTypeMask |= TypeMask.AzeriteEmpoweredItem;
			ObjectTypeId   =  TypeId.AzeriteEmpoweredItem;

			_azeriteEmpoweredItemData = new AzeriteEmpoweredItemData();
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
			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
			stmt.AddValue(0, GetGUID().GetCounter());
			trans.Append(stmt);

			switch (GetState())
			{
				case ItemUpdateState.New:
				case ItemUpdateState.Changed:
					stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_AZERITE_EMPOWERED);
					stmt.AddValue(0, GetGUID().GetCounter());

					for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
						stmt.AddValue(1 + i, _azeriteEmpoweredItemData.Selections[i]);

					trans.Append(stmt);

					break;
			}

			base.SaveToDB(trans);
		}

		public void LoadAzeriteEmpoweredItemData(Player owner, AzeriteEmpoweredData azeriteEmpoweredItem)
		{
			InitAzeritePowerData();
			bool needSave = false;

			if (_azeritePowers != null)
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
			else
				needSave = true;

			if (needSave)
			{
				PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_AZERITE_EMPOWERED);

				for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
					stmt.AddValue(i, _azeriteEmpoweredItemData.Selections[i]);

				stmt.AddValue(5, GetGUID().GetCounter());
				DB.Characters.Execute(stmt);
			}
		}

		public new static void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
		{
			PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_AZERITE_EMPOWERED);
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
			var azeritePowerItr = _azeritePowers.Find(power => { return power.AzeritePowerID == azeritePowerId && power.Class == (int)playerClass; });

			if (azeritePowerItr != null)
				return azeritePowerItr.Tier;

			return SharedConst.MaxAzeriteEmpoweredTier;
		}

		public void SetSelectedAzeritePower(int tier, int azeritePowerId)
		{
			SetUpdateFieldValue(ref Values.ModifyValue(_azeriteEmpoweredItemData).ModifyValue(_azeriteEmpoweredItemData.Selections, tier), azeritePowerId);

			// Not added to UF::ItemData::BonusListIDs, client fakes it on its own too
			_bonusData.AddBonusList(CliDB.AzeritePowerStorage.LookupByKey(azeritePowerId).ItemBonusListID);
		}

		private void ClearSelectedAzeritePowers()
		{
			for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
				SetUpdateFieldValue(ref Values.ModifyValue(_azeriteEmpoweredItemData).ModifyValue(_azeriteEmpoweredItemData.Selections, i), 0);

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
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			buffer.WriteUInt8((byte)flags);
			ObjectData.WriteCreate(buffer, flags, this, target);
			_itemData.WriteCreate(buffer, flags, this, target);
			_azeriteEmpoweredItemData.WriteCreate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteBytes(buffer);
		}

		public override void BuildValuesUpdate(WorldPacket data, Player target)
		{
			UpdateFieldFlag flags  = GetUpdateFieldFlagsFor(target);
			WorldPacket     buffer = new();

			if (Values.HasChanged(TypeId.Object))
				ObjectData.WriteUpdate(buffer, flags, this, target);

			if (Values.HasChanged(TypeId.Item))
				_itemData.WriteUpdate(buffer, flags, this, target);

			if (Values.HasChanged(TypeId.AzeriteEmpoweredItem))
				_azeriteEmpoweredItemData.WriteUpdate(buffer, flags, this, target);

			data.WriteUInt32(buffer.GetSize());
			data.WriteUInt32(Values.GetChangedObjectTypeMask());
			data.WriteBytes(buffer);
		}

		private void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, UpdateMask requestedAzeriteEmpoweredItemMask, Player target)
		{
			UpdateFieldFlag flags      = GetUpdateFieldFlagsFor(target);
			UpdateMask      valuesMask = new((int)TypeId.Max);

			if (requestedObjectMask.IsAnySet())
				valuesMask.Set((int)TypeId.Object);

			_itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);

			if (requestedItemMask.IsAnySet())
				valuesMask.Set((int)TypeId.Item);

			if (requestedAzeriteEmpoweredItemMask.IsAnySet())
				valuesMask.Set((int)TypeId.AzeriteEmpoweredItem);

			WorldPacket buffer = new();
			buffer.WriteUInt32(valuesMask.GetBlock(0));

			if (valuesMask[(int)TypeId.Object])
				ObjectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

			if (valuesMask[(int)TypeId.Item])
				_itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

			if (valuesMask[(int)TypeId.AzeriteEmpoweredItem])
				_azeriteEmpoweredItemData.WriteUpdate(buffer, requestedAzeriteEmpoweredItemMask, true, this, target);

			WorldPacket buffer1 = new();
			buffer1.WriteUInt8((byte)UpdateType.Values);
			buffer1.WritePackedGuid(GetGUID());
			buffer1.WriteUInt32(buffer.GetSize());
			buffer1.WriteBytes(buffer.GetData());

			data.AddUpdateBlock(buffer1);
		}

		public override void ClearUpdateMask(bool remove)
		{
			Values.ClearChangesMask(_azeriteEmpoweredItemData);
			base.ClearUpdateMask(remove);
		}

		private void InitAzeritePowerData()
		{
			_azeritePowers = Global.DB2Mgr.GetAzeritePowers(GetEntry());

			if (_azeritePowers != null)
				_maxTier = _azeritePowers.Aggregate((a1, a2) => a1.Tier < a2.Tier ? a2 : a1).Tier;
		}

		public int GetMaxAzeritePowerTier()
		{
			return _maxTier;
		}

		public uint GetSelectedAzeritePower(int tier)
		{
			return (uint)_azeriteEmpoweredItemData.Selections[tier];
		}

		private class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
		{
			private AzeriteEmpoweredItemData AzeriteEmpoweredItemMask = new();
			private ItemData ItemMask = new();
			private ObjectFieldData ObjectMask = new();
			private AzeriteEmpoweredItem Owner;

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