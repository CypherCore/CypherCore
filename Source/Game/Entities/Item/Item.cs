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

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Framework.IO;
using Game.DataStorage;
using Game.Loots;
using Game.Network;
using Game.Network.Packets;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Game.Entities
{
    public class Item : WorldObject
    {
        public Item() : base(false)
        {
            objectTypeMask |= TypeMask.Item;
            objectTypeId = TypeId.Item;

            valuesCount = (int)ItemFields.End;
            _dynamicValuesCount = (int)ItemDynamicFields.End;
            uState = ItemUpdateState.New;
            uQueuePos = -1;
            m_lastPlayedTimeUpdate = Time.UnixTime;

            loot = new Loot();
        }

        public virtual bool Create(ulong guidlow, uint itemid, Player owner)
        {
            _Create(ObjectGuid.Create(HighGuid.Item, guidlow));

            SetEntry(itemid);
            SetObjectScale(1.0f);

            if (owner)
            {
                SetOwnerGUID(owner.GetGUID());
                SetGuidValue(ItemFields.Contained, owner.GetGUID());
            }

            ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(itemid);
            if (itemProto == null)
                return false;

            _bonusData = new BonusData(itemProto);
            SetUInt32Value(ItemFields.StackCount, 1);
            SetUInt32Value(ItemFields.MaxDurability, itemProto.MaxDurability);
            SetUInt32Value(ItemFields.Durability, itemProto.MaxDurability);

            for (var i = 0; i < itemProto.Effects.Count; ++i)
            {
                if (i < 5)
                    SetSpellCharges(i, itemProto.Effects[i].Charges);
            }

            SetUInt32Value(ItemFields.Duration, itemProto.GetDuration());
            SetUInt32Value(ItemFields.CreatePlayedTime, 0);

            if (itemProto.GetArtifactID() != 0)
            {
                InitArtifactPowers(itemProto.GetArtifactID(), 0);
                foreach (ArtifactAppearanceRecord artifactAppearance in CliDB.ArtifactAppearanceStorage.Values)
                {
                    ArtifactAppearanceSetRecord artifactAppearanceSet = CliDB.ArtifactAppearanceSetStorage.LookupByKey(artifactAppearance.ArtifactAppearanceSetID);
                    if (artifactAppearanceSet != null)
                    {
                        if (itemProto.GetArtifactID() != artifactAppearanceSet.ArtifactID)
                            continue;

                        PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactAppearance.UnlockPlayerConditionID);
                        if (playerCondition != null)
                            if (!owner || !ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                                continue;

                        SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearance.Id);
                        SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);
                        break;
                    }
                }

                CheckArtifactRelicSlotUnlock(owner != null ? owner : GetOwner());
            }
            return true;
        }

        public bool IsNotEmptyBag()
        {
            Bag bag = ToBag();
            if (bag != null)
                return !bag.IsEmpty();

            return false;
        }

        public void UpdateDuration(Player owner, uint diff)
        {
            uint dur = GetUInt32Value(ItemFields.Duration);

            Log.outDebug(LogFilter.Player, "Item.UpdateDuration Item (Entry: {0} Duration {1} Diff {2})", GetEntry(), dur, diff);

            if (dur <= diff)
            {
                Global.ScriptMgr.OnItemExpire(owner, GetTemplate());
                owner.DestroyItem(GetBagSlot(), GetSlot(), true);
                return;
            }

            SetUInt32Value(ItemFields.Duration, dur - diff);
            SetState(ItemUpdateState.Changed, owner);                          // save new time in database
        }

        public virtual void SaveToDB(SQLTransaction trans)
        {
            PreparedStatement stmt;
            switch (uState)
            {
                case ItemUpdateState.New:
                case ItemUpdateState.Changed:
                    {
                        byte index = 0;
                        stmt = DB.Characters.GetPreparedStatement(uState == ItemUpdateState.New ? CharStatements.REP_ITEM_INSTANCE : CharStatements.UPD_ITEM_INSTANCE);
                        stmt.AddValue(index, GetEntry());
                        stmt.AddValue(++index, GetOwnerGUID().GetCounter());
                        stmt.AddValue(++index, GetGuidValue(ItemFields.Creator).GetCounter());
                        stmt.AddValue(++index, GetGuidValue(ItemFields.GiftCreator).GetCounter());
                        stmt.AddValue(++index, GetCount());
                        stmt.AddValue(++index, GetUInt32Value(ItemFields.Duration));

                        StringBuilder ss = new StringBuilder();
                        for (byte i = 0; i < ItemConst.MaxSpells; ++i)
                            ss.AppendFormat("{0} ", GetSpellCharges(i));

                        stmt.AddValue(++index, ss.ToString());
                        stmt.AddValue(++index, GetUInt32Value(ItemFields.Flags));

                        ss.Clear();
                        for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                            ss.AppendFormat("{0} {1} {2} ", GetEnchantmentId(slot), GetEnchantmentDuration(slot), GetEnchantmentCharges(slot));

                        stmt.AddValue(++index, ss.ToString());
                        stmt.AddValue(++index, (byte)GetItemRandomEnchantmentId().Type);
                        stmt.AddValue(++index, GetItemRandomEnchantmentId().Id);
                        stmt.AddValue(++index, GetUInt32Value(ItemFields.Durability));
                        stmt.AddValue(++index, GetUInt32Value(ItemFields.CreatePlayedTime));
                        stmt.AddValue(++index, m_text);
                        stmt.AddValue(++index, GetModifier(ItemModifier.UpgradeId));
                        stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetSpeciesId));
                        stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetBreedData));
                        stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetLevel));
                        stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetDisplayId));
                        stmt.AddValue(++index, (byte)GetUInt32Value(ItemFields.Context));

                        ss.Clear();
                        foreach (uint bonusListID in GetDynamicValues(ItemDynamicFields.BonusListIds))
                            ss.Append(bonusListID + ' ');

                        stmt.AddValue(++index, ss.ToString());
                        stmt.AddValue(++index, GetGUID().GetCounter());

                        DB.Characters.Execute(stmt);

                        if ((uState == ItemUpdateState.Changed) && HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_GIFT_OWNER);
                            stmt.AddValue(0, GetOwnerGUID().GetCounter());
                            stmt.AddValue(1, GetGUID().GetCounter());
                            DB.Characters.Execute(stmt);
                        }

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        if (!GetGems().Empty())
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_GEMS);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            int i = 0;
                            int gemFields = 4;
                            foreach (ItemDynamicFieldGems gemData in GetGems())
                            {
                                if (gemData.ItemId != 0)
                                {
                                    stmt.AddValue(1 + i * gemFields, gemData.ItemId);
                                    StringBuilder gemBonusListIDs = new StringBuilder();
                                    foreach (ushort bonusListID in gemData.BonusListIDs)
                                    {
                                        if (bonusListID != 0)
                                            gemBonusListIDs.AppendFormat("{0} ", bonusListID);
                                    }

                                    stmt.AddValue(2 + i * gemFields, gemBonusListIDs.ToString());
                                    stmt.AddValue(3 + i * gemFields, gemData.Context);
                                    stmt.AddValue(4 + i * gemFields, m_gemScalingLevels[i]);
                                }
                                else
                                {
                                    stmt.AddValue(1 + i * gemFields, 0);
                                    stmt.AddValue(2 + i * gemFields, "");
                                    stmt.AddValue(3 + i * gemFields, 0);
                                    stmt.AddValue(4 + i * gemFields, 0);
                                }
                                ++i;
                            }
                            for (; i < ItemConst.MaxGemSockets; ++i)
                            {
                                stmt.AddValue(1 + i * gemFields, 0);
                                stmt.AddValue(2 + i * gemFields, "");
                                stmt.AddValue(3 + i * gemFields, 0);
                                stmt.AddValue(4 + i * gemFields, 0);
                            }
                            trans.Append(stmt);
                        }

                        ItemModifier[] transmogMods =
                        {
                            ItemModifier.TransmogAppearanceAllSpecs,
                            ItemModifier.TransmogAppearanceSpec1,
                            ItemModifier.TransmogAppearanceSpec2,
                            ItemModifier.TransmogAppearanceSpec3,
                            ItemModifier.TransmogAppearanceSpec4,

                            ItemModifier.EnchantIllusionAllSpecs,
                            ItemModifier.EnchantIllusionSpec1,
                            ItemModifier.EnchantIllusionSpec2,
                            ItemModifier.EnchantIllusionSpec3,
                            ItemModifier.EnchantIllusionSpec4,
                        };

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        if (transmogMods.Any(modifier => GetModifier(modifier) != 0))
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_TRANSMOG);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                            stmt.AddValue(2, GetModifier(ItemModifier.TransmogAppearanceSpec1));
                            stmt.AddValue(3, GetModifier(ItemModifier.TransmogAppearanceSpec2));
                            stmt.AddValue(4, GetModifier(ItemModifier.TransmogAppearanceSpec3));
                            stmt.AddValue(5, GetModifier(ItemModifier.TransmogAppearanceSpec4));
                            stmt.AddValue(6, GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                            stmt.AddValue(7, GetModifier(ItemModifier.EnchantIllusionSpec1));
                            stmt.AddValue(8, GetModifier(ItemModifier.EnchantIllusionSpec2));
                            stmt.AddValue(9, GetModifier(ItemModifier.EnchantIllusionSpec3));
                            stmt.AddValue(10, GetModifier(ItemModifier.EnchantIllusionSpec4));
                            trans.Append(stmt);
                        }

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        if (GetTemplate().GetArtifactID() != 0)
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, GetUInt64Value(ItemFields.ArtifactXp));
                            stmt.AddValue(2, GetModifier(ItemModifier.ArtifactAppearanceId));
                            stmt.AddValue(3, GetModifier(ItemModifier.ArtifactTier));
                            trans.Append(stmt);

                            foreach (ItemDynamicFieldArtifactPowers artifactPower in GetArtifactPowers())
                            {
                                stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT_POWERS);
                                stmt.AddValue(0, GetGUID().GetCounter());
                                stmt.AddValue(1, artifactPower.ArtifactPowerId);
                                stmt.AddValue(2, artifactPower.PurchasedRank);
                                trans.Append(stmt);
                            }
                        }

                        ItemModifier[] modifiersTable =
                        {
                            ItemModifier.ScalingStatDistributionFixedLevel,
                            ItemModifier.ArtifactKnowledgeLevel
                        };

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        if (modifiersTable.Any(modifier => GetModifier(modifier) != 0))
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_MODIFIERS);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, GetModifier(ItemModifier.ScalingStatDistributionFixedLevel));
                            stmt.AddValue(2, GetModifier(ItemModifier.ArtifactKnowledgeLevel));
                            trans.Append(stmt);
                        }
                        break;
                    }
                case ItemUpdateState.Removed:
                    {
                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);

                        if (HasFlag(ItemFields.Flags, ItemFieldFlags.Wrapped))
                        {
                            stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_GIFT);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            trans.Append(stmt);
                        }

                        // Delete the items if this is a container
                        if (!loot.isLooted())
                            ItemContainerDeleteLootMoneyAndLootItemsFromDB();

                        Dispose();
                        return;
                    }
                case ItemUpdateState.Unchanged:
                    break;
            }

            SetState(ItemUpdateState.Unchanged);
        }

        public virtual bool LoadFromDB(ulong guid, ObjectGuid ownerGuid, SQLFields fields, uint entry)
        {
            // create item before any checks for store correct guid
            // and allow use "FSetState(ITEM_REMOVED); SaveToDB();" for deleting item from DB
            _Create(ObjectGuid.Create(HighGuid.Item, guid));

            SetEntry(entry);
            SetObjectScale(1.0f);

            ItemTemplate proto = GetTemplate();
            if (proto == null)
                return false;

            _bonusData = new BonusData(proto);

            // set owner (not if item is only loaded for gbank/auction/mail
            if (!ownerGuid.IsEmpty())
                SetOwnerGUID(ownerGuid);

            uint itemFlags = fields.Read<uint>(7);
            bool need_save = false;
            ulong creator = fields.Read<ulong>(2);
            if (creator != 0)
            {
                if (!Convert.ToBoolean(itemFlags & (int)ItemFieldFlags.Child))
                    SetGuidValue(ItemFields.Creator, ObjectGuid.Create(HighGuid.Player, creator));
                else
                    SetGuidValue(ItemFields.Creator, ObjectGuid.Create(HighGuid.Item, creator));
            }

            ulong giftCreator = fields.Read<ulong>(3);
            if (giftCreator != 0)
                SetGuidValue(ItemFields.GiftCreator, ObjectGuid.Create(HighGuid.Player, giftCreator));
            SetCount(fields.Read<uint>(4));

            uint duration = fields.Read<uint>(5);
            SetUInt32Value(ItemFields.Duration, duration);
            // update duration if need, and remove if not need
            if (proto.GetDuration() != duration)
            {
                SetUInt32Value(ItemFields.Duration, proto.GetDuration());
                need_save = true;
            }

            var tokens = new StringArray(fields.Read<string>(6), ' ');
            if (tokens.Length == ItemConst.MaxProtoSpells)
            {
                for (byte i = 0; i < ItemConst.MaxProtoSpells; ++i)
                {
                    if (int.TryParse(tokens[i], out int value))
                        SetSpellCharges(i, value);
                }
            }

            SetUInt32Value(ItemFields.Flags, itemFlags);

            uint durability = fields.Read<uint>(11);
            SetUInt32Value(ItemFields.Durability, durability);
            // update max durability (and durability) if need
            SetUInt32Value(ItemFields.MaxDurability, proto.MaxDurability);
            if (durability > proto.MaxDurability)
            {
                SetUInt32Value(ItemFields.Durability, proto.MaxDurability);
                need_save = true;
            }

            SetUInt32Value(ItemFields.CreatePlayedTime, fields.Read<uint>(12));
            SetText(fields.Read<string>(13));

            uint upgradeId = fields.Read<uint>(14);
            ItemUpgradeRecord rulesetUpgrade = CliDB.ItemUpgradeStorage.LookupByKey(Global.DB2Mgr.GetRulesetItemUpgrade(entry));
            ItemUpgradeRecord upgrade = CliDB.ItemUpgradeStorage.LookupByKey(upgradeId);
            if (rulesetUpgrade == null || upgrade == null || rulesetUpgrade.ItemUpgradePathID != upgrade.ItemUpgradePathID)
            {
                upgradeId = 0;
                need_save = true;
            }

            if (rulesetUpgrade != null && upgradeId == 0)
            {
                upgradeId = rulesetUpgrade.Id;
                need_save = true;
            }

            SetModifier(ItemModifier.UpgradeId, upgradeId);
            SetModifier(ItemModifier.BattlePetSpeciesId, fields.Read<uint>(15));
            SetModifier(ItemModifier.BattlePetBreedData, fields.Read<uint>(16));
            SetModifier(ItemModifier.BattlePetLevel, fields.Read<ushort>(17));
            SetModifier(ItemModifier.BattlePetDisplayId, fields.Read<uint>(18));

            SetUInt32Value(ItemFields.Context, fields.Read<byte>(19));

            var bonusListIDs = new StringArray(fields.Read<string>(20), ' ');
            if (!tokens.IsEmpty())
            {
                for (var i = 0; i < bonusListIDs.Length; ++i)
                {
                    if (uint.TryParse(tokens[i], out uint bonusListID))
                        AddBonuses(bonusListID);
                }
            }

            SetModifier(ItemModifier.TransmogAppearanceAllSpecs, fields.Read<uint>(21));
            SetModifier(ItemModifier.TransmogAppearanceSpec1, fields.Read<uint>(22));
            SetModifier(ItemModifier.TransmogAppearanceSpec2, fields.Read<uint>(23));
            SetModifier(ItemModifier.TransmogAppearanceSpec3, fields.Read<uint>(24));
            SetModifier(ItemModifier.TransmogAppearanceSpec4, fields.Read<uint>(25));

            SetModifier(ItemModifier.EnchantIllusionAllSpecs, fields.Read<uint>(26));
            SetModifier(ItemModifier.EnchantIllusionSpec1, fields.Read<uint>(27));
            SetModifier(ItemModifier.EnchantIllusionSpec2, fields.Read<uint>(28));
            SetModifier(ItemModifier.EnchantIllusionSpec3, fields.Read<uint>(29));
            SetModifier(ItemModifier.EnchantIllusionSpec4, fields.Read<uint>(30));

            int gemFields = 4;
            ItemDynamicFieldGems[] gemData = new ItemDynamicFieldGems[ItemConst.MaxGemSockets];
            for (int i = 0; i < ItemConst.MaxGemSockets; ++i)
            {
                gemData[i] = new ItemDynamicFieldGems();
                gemData[i].ItemId = fields.Read<uint>(31 + i * gemFields);
                var gemBonusListIDs = new StringArray(fields.Read<string>(32 + i * gemFields), ' ');
                if (!gemBonusListIDs.IsEmpty())
                {
                    uint b = 0;
                    foreach (string token in gemBonusListIDs)
                    {
                        if (uint.TryParse(token, out uint bonusListID) && bonusListID != 0)
                            gemData[i].BonusListIDs[b++] = (ushort)bonusListID;
                    }
                }

                gemData[i].Context = fields.Read<byte>(33 + i * gemFields);
                if (gemData[i].ItemId != 0)
                    SetGem((ushort)i, gemData[i], fields.Read<uint>(34 + i * gemFields));
            }

            SetModifier(ItemModifier.ScalingStatDistributionFixedLevel, fields.Read<uint>(43));
            SetModifier(ItemModifier.ArtifactKnowledgeLevel, fields.Read<uint>(44));

            // Enchants must be loaded after all other bonus/scaling data
            _LoadIntoDataField(fields.Read<string>(8), (uint)ItemFields.Enchantment, (uint)EnchantmentSlot.Max * (uint)EnchantmentOffset.Max);
            m_randomEnchantment.Type = (ItemRandomEnchantmentType)fields.Read<byte>(9);
            m_randomEnchantment.Id = fields.Read<uint>(10);
            if (m_randomEnchantment.Type == ItemRandomEnchantmentType.Property)
                SetUInt32Value(ItemFields.RandomPropertiesId, m_randomEnchantment.Id);
            else if (m_randomEnchantment.Type == ItemRandomEnchantmentType.Suffix)
            {
                SetInt32Value(ItemFields.RandomPropertiesId, -(int)m_randomEnchantment.Id);
                // recalculate suffix factor
                UpdateItemSuffixFactor();
            }

            // Remove bind flag for items vs NO_BIND set
            if (IsSoulBound() && GetBonding() == ItemBondingType.None)
            {
                ApplyModFlag(ItemFields.Flags, ItemFieldFlags.Soulbound, false);
                need_save = true;
            }

            if (need_save)                                           // normal item changed state set not work at loading
            {
                byte index = 0;
                PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_ON_LOAD);
                stmt.AddValue(index++, GetUInt32Value(ItemFields.Duration));
                stmt.AddValue(index++, GetUInt32Value(ItemFields.Flags));
                stmt.AddValue(index++, GetUInt32Value(ItemFields.Durability));
                stmt.AddValue(index++, GetModifier(ItemModifier.UpgradeId));
                stmt.AddValue(index++, guid);
                DB.Characters.Execute(stmt);
            }
            return true;
        }

        public void LoadArtifactData(Player owner, ulong xp, uint artifactAppearanceId, uint artifactTier, List<ItemDynamicFieldArtifactPowers> powers)
        {
            for (byte i = 0; i <= artifactTier; ++i)
                InitArtifactPowers(GetTemplate().GetArtifactID(), i);

            SetUInt64Value(ItemFields.ArtifactXp, xp);
            SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearanceId);
            SetModifier(ItemModifier.ArtifactTier, artifactTier);

            ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifactAppearanceId);
            if (artifactAppearance != null)
                SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);

            byte totalPurchasedRanks = 0;
            foreach (ItemDynamicFieldArtifactPowers power in powers)
            {
                power.CurrentRankWithBonus += power.PurchasedRank;
                totalPurchasedRanks += power.PurchasedRank;

                ArtifactPowerRecord artifactPower = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);
                for (var e = EnchantmentSlot.Sock1; e <= EnchantmentSlot.Sock3; ++e)
                {
                    SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetEnchantmentId(e));
                    if (enchant != null)
                    {
                        for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                        {
                            switch (enchant.Effect[i])
                            {
                                case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                                    if (artifactPower.Label == enchant.EffectArg[i])
                                        power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                    break;
                                case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                                    if (artifactPower.Id == enchant.EffectArg[i])
                                        power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                    break;
                                case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                                    if (_bonusData.GemRelicType[e - EnchantmentSlot.Sock1] != -1)
                                    {
                                        ArtifactPowerPickerRecord artifactPowerPicker = CliDB.ArtifactPowerPickerStorage.LookupByKey(enchant.EffectArg[i]);
                                        if (artifactPowerPicker != null)
                                        {
                                            PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactPowerPicker.PlayerConditionID);
                                            if (playerCondition == null || ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                                                if (artifactPower.Label == _bonusData.GemRelicType[e - EnchantmentSlot.Sock1])
                                                    power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                SetArtifactPower(power);
            }

            foreach (ItemDynamicFieldArtifactPowers power in powers)
            {
                ArtifactPowerRecord scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);
                if (!scaledArtifactPowerEntry.Flags.HasAnyFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                    continue;

                power.CurrentRankWithBonus = (byte)(totalPurchasedRanks + 1);
                SetArtifactPower(power);
            }

            CheckArtifactRelicSlotUnlock(owner != null ? owner : GetOwner());
        }

        public void CheckArtifactRelicSlotUnlock(Player owner)
        {
            if (!owner)
                return;

            byte artifactId = GetTemplate().GetArtifactID();
            if (artifactId == 0)
                return;

            foreach (ArtifactUnlockRecord artifactUnlock in CliDB.ArtifactUnlockStorage.Values)
                if (artifactUnlock.ArtifactID == artifactId)
                    if (owner.MeetPlayerCondition(artifactUnlock.PlayerConditionID))
                        AddBonuses(artifactUnlock.ItemBonusListID);
        }

        public static void DeleteFromDB(SQLTransaction trans, ulong itemGuid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
            stmt.AddValue(0, itemGuid);
            trans.Append(stmt);
        }

        public virtual void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(trans, GetGUID().GetCounter());

            // Delete the items if this is a container
            if (!loot.isLooted())
                ItemContainerDeleteLootMoneyAndLootItemsFromDB();
        }

        public static void DeleteFromInventoryDB(SQLTransaction trans, ulong itemGuid)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
            stmt.AddValue(0, itemGuid);
            trans.Append(stmt);
        }

        public void DeleteFromInventoryDB(SQLTransaction trans)
        {
            DeleteFromInventoryDB(trans, GetGUID().GetCounter());
        }

        public ItemTemplate GetTemplate()
        {
            return Global.ObjectMgr.GetItemTemplate(GetEntry());
        }

        public Player GetOwner()
        {
            return Global.ObjAccessor.FindPlayer(GetOwnerGUID());
        }

        public SkillType GetSkill()
        {
            ItemTemplate proto = GetTemplate();
            return proto.GetSkill();
        }

        public void SetItemRandomProperties(ItemRandomEnchantmentId randomPropId)
        {
            if (randomPropId.Id == 0)
                return;

            switch (randomPropId.Type)
            {
                case ItemRandomEnchantmentType.Property:
                    {
                        ItemRandomPropertiesRecord item_rand = CliDB.ItemRandomPropertiesStorage.LookupByKey(randomPropId.Id);
                        if (item_rand != null)
                        {
                            if (GetUInt32Value(ItemFields.RandomPropertiesId) != randomPropId.Id)
                            {
                                SetUInt32Value(ItemFields.RandomPropertiesId, randomPropId.Id);
                                SetState(ItemUpdateState.Changed, GetOwner());
                            }
                            for (EnchantmentSlot i = EnchantmentSlot.Prop0; i <= EnchantmentSlot.Prop4; ++i)
                                SetEnchantment(i, item_rand.Enchantment[i - EnchantmentSlot.Prop0], 0, 0);
                        }
                    }
                    break;
                case ItemRandomEnchantmentType.Suffix:
                    {
                        ItemRandomSuffixRecord item_rand = CliDB.ItemRandomSuffixStorage.LookupByKey(randomPropId.Id);
                        if (item_rand != null)
                        {
                            if (GetInt32Value(ItemFields.RandomPropertiesId) != -(int)randomPropId.Id || GetItemSuffixFactor() == 0)
                            {
                                SetInt32Value(ItemFields.RandomPropertiesId, -(int)randomPropId.Id);
                                UpdateItemSuffixFactor();
                                SetState(ItemUpdateState.Changed, GetOwner());
                            }

                            for (var i = EnchantmentSlot.Prop0; i <= EnchantmentSlot.Prop4; ++i)
                                SetEnchantment(i, item_rand.Enchantment[i - EnchantmentSlot.Prop0], 0, 0);
                        }
                    }
                    break;
                case ItemRandomEnchantmentType.BonusList:
                    AddBonuses(randomPropId.Id);
                    break;
                default:
                    break;
            }
        }

        void UpdateItemSuffixFactor()
        {
            if (GetTemplate().GetRandomSuffix() == 0)
                return;

            uint suffixFactor = 0;
            Player owner = GetOwner();
            if (owner)
                suffixFactor = ItemEnchantment.GetRandomPropertyPoints(GetItemLevel(owner), GetQuality(), GetTemplate().GetInventoryType(), GetTemplate().GetSubClass());
            else
                suffixFactor = ItemEnchantment.GenerateEnchSuffixFactor(GetEntry());

            if (GetItemSuffixFactor() == suffixFactor)
                return;

            SetUInt32Value(ItemFields.PropertySeed, suffixFactor);
        }

        public void SetState(ItemUpdateState state, Player forplayer = null)
        {
            if (uState == ItemUpdateState.New && state == ItemUpdateState.Removed)
            {
                // pretend the item never existed
                if (forplayer)
                {
                    RemoveItemFromUpdateQueueOf(this, forplayer);
                    forplayer.DeleteRefundReference(GetGUID());
                }
                return;
            }
            if (state != ItemUpdateState.Unchanged)
            {
                // new items must stay in new state until saved
                if (uState != ItemUpdateState.New)
                    uState = state;

                if (forplayer)
                    AddItemToUpdateQueueOf(this, forplayer);
            }
            else
            {
                // unset in queue
                // the item must be removed from the queue manually
                uQueuePos = -1;
                uState = ItemUpdateState.Unchanged;
            }
        }

        static void AddItemToUpdateQueueOf(Item item, Player player)
        {
            if (item.IsInUpdateQueue())
                return;

            Cypher.Assert(player != null);

            if (player.GetGUID() != item.GetOwnerGUID())
            {
                Log.outError(LogFilter.Player, "Item.AddToUpdateQueueOf - Owner's guid ({0}) and player's guid ({1}) don't match!", item.GetOwnerGUID(), player.GetGUID().ToString());
                return;
            }

            if (player.m_itemUpdateQueueBlocked)
                return;

            player.ItemUpdateQueue.Add(item);
            item.uQueuePos = player.ItemUpdateQueue.Count - 1;
        }

        public static void RemoveItemFromUpdateQueueOf(Item item, Player player)
        {
            if (!item.IsInUpdateQueue())
                return;

            Cypher.Assert(player != null);

            if (player.GetGUID() != item.GetOwnerGUID())
            {
                Log.outError(LogFilter.Player, "Item.RemoveFromUpdateQueueOf - Owner's guid ({0}) and player's guid ({1}) don't match!", item.GetOwnerGUID().ToString(), player.GetGUID().ToString());
                return;
            }

            if (player.m_itemUpdateQueueBlocked)
                return;

            player.ItemUpdateQueue[item.uQueuePos] = null;
            item.uQueuePos = -1;
        }

        public byte GetBagSlot()
        {
            return m_container != null ? m_container.GetSlot() : InventorySlots.Bag0;
        }

        public bool IsEquipped() { return !IsInBag() && m_slot < EquipmentSlot.End; }

        public bool CanBeTraded(bool mail = false, bool trade = false)
        {
            if (m_lootGenerated)
                return false;

            if ((!mail || !IsBoundAccountWide()) && (IsSoulBound() && (!HasFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable) || !trade)))
                return false;

            if (IsBag() && (Player.IsBagPos(GetPos()) || !ToBag().IsEmpty()))
                return false;

            Player owner = GetOwner();
            if (owner != null)
            {
                if (owner.CanUnequipItem(GetPos(), false) != InventoryResult.Ok)
                    return false;
                if (owner.GetLootGUID() == GetGUID())
                    return false;
            }

            if (IsBoundByEnchant())
                return false;

            return true;
        }

        public void SetCount(uint value)
        {
            SetUInt32Value(ItemFields.StackCount, value);

            Player player = GetOwner();
            if (player)
            {
                TradeData tradeData = player.GetTradeData();
                if (tradeData != null)
                {
                    TradeSlots slot = tradeData.GetTradeSlotForItem(GetGUID());

                    if (slot != TradeSlots.Invalid)
                        tradeData.SetItem(slot, this, true);
                }
            }
        }

        bool HasEnchantRequiredSkill(Player player)
        {
            // Check all enchants for required skill
            for (var enchant_slot = EnchantmentSlot.Perm; enchant_slot < EnchantmentSlot.Max; ++enchant_slot)
            {
                uint enchant_id = GetEnchantmentId(enchant_slot);
                if (enchant_id != 0)
                {
                    SpellItemEnchantmentRecord enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                    if (enchantEntry != null)
                        if (enchantEntry.RequiredSkillID != 0 && player.GetSkillValue((SkillType)enchantEntry.RequiredSkillID) < enchantEntry.RequiredSkillRank)
                            return false;
                }
            }

            return true;
        }

        uint GetEnchantRequiredLevel()
        {
            uint level = 0;

            // Check all enchants for required level
            for (var enchant_slot = EnchantmentSlot.Perm; enchant_slot < EnchantmentSlot.Max; ++enchant_slot)
            {
                uint enchant_id = GetEnchantmentId(enchant_slot);
                if (enchant_id != 0)
                {
                    var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                    if (enchantEntry != null)
                        if (enchantEntry.MinLevel > level)
                            level = enchantEntry.MinLevel;
                }
            }

            return level;
        }

        bool IsBoundByEnchant()
        {
            // Check all enchants for soulbound
            for (var enchant_slot = EnchantmentSlot.Perm; enchant_slot < EnchantmentSlot.Max; ++enchant_slot)
            {
                uint enchant_id = GetEnchantmentId(enchant_slot);
                if (enchant_id != 0)
                {
                    var enchantEntry = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchant_id);
                    if (enchantEntry != null)
                        if (enchantEntry.Flags.HasAnyFlag(EnchantmentSlotMask.CanSouldBound))
                            return true;
                }
            }

            return false;
        }

        public InventoryResult CanBeMergedPartlyWith(ItemTemplate proto)
        {
            // not allow merge looting currently items
            if (m_lootGenerated)
                return InventoryResult.LootGone;

            // check item type
            if (GetEntry() != proto.GetId())
                return InventoryResult.CantStack;

            // check free space (full stacks can't be target of merge
            if (GetCount() >= proto.GetMaxStackSize())
                return InventoryResult.CantStack;

            return InventoryResult.Ok;
        }

        public bool IsFitToSpellRequirements(SpellInfo spellInfo)
        {
            ItemTemplate proto = GetTemplate();

            bool isEnchantSpell = spellInfo.HasEffect(SpellEffectName.EnchantItem) || spellInfo.HasEffect(SpellEffectName.EnchantItemTemporary) || spellInfo.HasEffect(SpellEffectName.EnchantItemPrismatic);
            if ((int)spellInfo.EquippedItemClass != -1)                 // -1 == any item class
            {
                if (isEnchantSpell && proto.GetFlags3().HasAnyFlag(ItemFlags3.CanStoreEnchants))
                    return true;

                if (spellInfo.EquippedItemClass != proto.GetClass())
                    return false;                                   //  wrong item class

                if (spellInfo.EquippedItemSubClassMask != 0)        // 0 == any subclass
                {
                    if ((spellInfo.EquippedItemSubClassMask & (1 << (int)proto.GetSubClass())) == 0)
                        return false;                               // subclass not present in mask
                }
            }

            if (isEnchantSpell && spellInfo.EquippedItemInventoryTypeMask != 0)       // 0 == any inventory type
            {
                // Special case - accept weapon type for main and offhand requirements
                if (proto.GetInventoryType() == InventoryType.Weapon &&
                    Convert.ToBoolean(spellInfo.EquippedItemInventoryTypeMask & (1 << (int)InventoryType.WeaponMainhand)) ||
                     Convert.ToBoolean(spellInfo.EquippedItemInventoryTypeMask & (1 << (int)InventoryType.WeaponOffhand)))
                    return true;
                else if ((spellInfo.EquippedItemInventoryTypeMask & (1 << (int)proto.GetInventoryType())) == 0)
                    return false;                                   // inventory type not present in mask
            }

            return true;
        }

        public void SetEnchantment(EnchantmentSlot slot, uint id, uint duration, uint charges, ObjectGuid caster = default(ObjectGuid))
        {
            // Better lost small time at check in comparison lost time at item save to DB.
            if ((GetEnchantmentId(slot) == id) && (GetEnchantmentDuration(slot) == duration) && (GetEnchantmentCharges(slot) == charges))
                return;

            Player owner = GetOwner();
            if (slot < EnchantmentSlot.MaxInspected)
            {
                uint oldEnchant = GetEnchantmentId(slot);
                if (oldEnchant != 0)
                    owner.GetSession().SendEnchantmentLog(GetOwnerGUID(), ObjectGuid.Empty, GetGUID(), GetEntry(), oldEnchant, (uint)slot);

                if (id != 0)
                    owner.GetSession().SendEnchantmentLog(GetOwnerGUID(), caster, GetGUID(), GetEntry(), id, (uint)slot);
            }

            ApplyArtifactPowerEnchantmentBonuses(slot, GetEnchantmentId(slot), false, owner);
            ApplyArtifactPowerEnchantmentBonuses(slot, id, true, owner);

            SetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Id, id);
            SetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Duration, duration);
            SetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Charges, charges);
            SetState(ItemUpdateState.Changed, owner);
        }

        public void SetEnchantmentDuration(EnchantmentSlot slot, uint duration, Player owner)
        {
            if (GetEnchantmentDuration(slot) == duration)
                return;

            SetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Duration, duration);
            SetState(ItemUpdateState.Changed, owner);
            // Cannot use GetOwner() here, has to be passed as an argument to avoid freeze due to hashtable locking
        }

        public void SetEnchantmentCharges(EnchantmentSlot slot, uint charges)
        {
            if (GetEnchantmentCharges(slot) == charges)
                return;

            SetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Charges, charges);
            SetState(ItemUpdateState.Changed, GetOwner());
        }

        public void ClearEnchantment(EnchantmentSlot slot)
        {
            if (GetEnchantmentId(slot) == 0)
                return;

            for (byte x = 0; x < ItemConst.MaxItemEnchantmentEffects; ++x)
                SetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + x, 0);
            SetState(ItemUpdateState.Changed, GetOwner());
        }

        public List<ItemDynamicFieldGems> GetGems()
        {
            return GetDynamicStructuredValues<ItemDynamicFieldGems>(ItemDynamicFields.Gems);
        }

        public ItemDynamicFieldGems GetGem(ushort slot)
        {
            //ASSERT(slot < MAX_GEM_SOCKETS);
            return GetDynamicStructuredValue<ItemDynamicFieldGems>(ItemDynamicFields.Gems, slot);
        }

        public void SetGem(ushort slot, ItemDynamicFieldGems gem, uint gemScalingLevel)
        {
            //ASSERT(slot < MAX_GEM_SOCKETS);
            m_gemScalingLevels[slot] = gemScalingLevel;
            _bonusData.GemItemLevelBonus[slot] = 0;
            ItemTemplate gemTemplate = Global.ObjectMgr.GetItemTemplate(gem.ItemId);
            if (gemTemplate != null)
            {
                GemPropertiesRecord gemProperties = CliDB.GemPropertiesStorage.LookupByKey(gemTemplate.GetGemProperties());
                if (gemProperties != null)
                {
                    SpellItemEnchantmentRecord gemEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(gemProperties.EnchantId);
                    if (gemEnchant != null)
                    {
                        BonusData gemBonus = new BonusData(gemTemplate);
                        foreach (var bonusListId in gem.BonusListIDs)
                        {

                            var bonuses = Global.DB2Mgr.GetItemBonusList(bonusListId);
                            if (bonuses != null)
                            {

                                foreach (ItemBonusRecord itemBonus in bonuses)
                                    gemBonus.AddBonus(itemBonus.Type, itemBonus.Value);
                            }

                            uint gemBaseItemLevel = gemTemplate.GetBaseItemLevel();
                            ScalingStatDistributionRecord ssd = CliDB.ScalingStatDistributionStorage.LookupByKey(gemBonus.ScalingStatDistribution);
                            if (ssd != null)
                            {
                                uint scaledIlvl = (uint)Global.DB2Mgr.GetCurveValueAt(ssd.PlayerLevelToItemLevelCurveID, gemScalingLevel);
                                if (scaledIlvl != 0)
                                    gemBaseItemLevel = scaledIlvl;
                            }

                            _bonusData.GemRelicType[slot] = gemBonus.RelicType;

                            for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                            {
                                switch (gemEnchant.Effect[i])
                                {
                                    case ItemEnchantmentType.BonusListID:
                                        {
                                            var bonusesEffect = Global.DB2Mgr.GetItemBonusList(gemEnchant.EffectArg[i]);
                                            if (bonusesEffect != null)
                                            {
                                                foreach (ItemBonusRecord itemBonus in bonusesEffect)
                                                    if (itemBonus.Type == ItemBonusType.ItemLevel)

                                                        _bonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];
                                            }
                                            break;
                                        }
                                    case ItemEnchantmentType.BonusListCurve:
                                        {
                                            uint artifactrBonusListId = Global.DB2Mgr.GetItemBonusListForItemLevelDelta((short)Global.DB2Mgr.GetCurveValueAt((uint)Curves.ArtifactRelicItemLevelBonus, gemBaseItemLevel + gemBonus.ItemLevelBonus));
                                            if (artifactrBonusListId != 0)
                                            {
                                                var bonusesEffect = Global.DB2Mgr.GetItemBonusList(artifactrBonusListId);
                                                if (bonusesEffect != null)
                                                    foreach (ItemBonusRecord itemBonus in bonusesEffect)
                                                        if (itemBonus.Type == ItemBonusType.ItemLevel)
                                                            _bonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];
                                            }
                                            break;
                                        }
                                    default:
                                        break;
                                }
                            }
                        }
                    }
                }
            }

            SetDynamicStructuredValue(ItemDynamicFields.Gems, slot, gem);
        }

        public bool GemsFitSockets()
        {
            uint gemSlot = 0;
            foreach (ItemDynamicFieldGems gemData in GetGems())
            {
                SocketColor SocketColor = GetTemplate().GetSocketColor(gemSlot);
                if (SocketColor == 0) // no socket slot
                    continue;

                SocketColor GemColor = 0;

                ItemTemplate gemProto = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);
                if (gemProto != null)
                {
                    GemPropertiesRecord gemProperty = CliDB.GemPropertiesStorage.LookupByKey(gemProto.GetGemProperties());
                    if (gemProperty != null)
                        GemColor = gemProperty.Type;
                }

                if (!GemColor.HasAnyFlag(ItemConst.SocketColorToGemTypeMask[(int)SocketColor])) // bad gem color on this socket
                    return false;
            }
            return true;
        }

        public byte GetGemCountWithID(uint GemID)
        {
            return (byte)GetGems().Count(gemData => gemData.ItemId == GemID);
        }

        public byte GetGemCountWithLimitCategory(uint limitCategory)
        {
            return (byte)GetGems().Count(gemData =>
            {
                ItemTemplate gemProto = Global.ObjectMgr.GetItemTemplate(gemData.ItemId);
                if (gemProto == null)
                    return false;

                return gemProto.GetItemLimitCategory() == limitCategory;
            });
        }

        public bool IsLimitedToAnotherMapOrZone(uint cur_mapId, uint cur_zoneId)
        {
            ItemTemplate proto = GetTemplate();
            return proto != null && ((proto.GetMap() != 0 && proto.GetMap() != cur_mapId) || (proto.GetArea() != 0 && proto.GetArea() != cur_zoneId));
        }

        public void SendUpdateSockets()
        {
            SocketGemsResult socketGems = new SocketGemsResult();
            socketGems.Item = GetGUID();

            GetOwner().SendPacket(socketGems);
        }

        public void SendTimeUpdate(Player owner)
        {
            uint duration = GetUInt32Value(ItemFields.Duration);
            if (duration == 0)
                return;

            ItemTimeUpdate itemTimeUpdate = new ItemTimeUpdate();
            itemTimeUpdate.ItemGuid = GetGUID();
            itemTimeUpdate.DurationLeft = duration;
            owner.SendPacket(itemTimeUpdate);
        }

        public static Item CreateItem(uint item, uint count, Player player = null)
        {
            if (count < 1)
                return null;                                        //don't create item at zero count

            var pProto = Global.ObjectMgr.GetItemTemplate(item);
            if (pProto != null)
            {
                if (count > pProto.GetMaxStackSize())
                    count = pProto.GetMaxStackSize();

                Item pItem = Bag.NewItemOrBag(pProto);
                if (pItem.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), item, player))
                {
                    pItem.SetCount(count);
                    return pItem;
                }
            }

            return null;
        }

        public Item CloneItem(uint count, Player player = null)
        {
            Item newItem = CreateItem(GetEntry(), count, player);
            if (newItem == null)
                return null;

            newItem.SetUInt32Value(ItemFields.Creator, GetUInt32Value(ItemFields.Creator));
            newItem.SetUInt32Value(ItemFields.GiftCreator, GetUInt32Value(ItemFields.GiftCreator));
            newItem.SetUInt32Value(ItemFields.Flags, GetUInt32Value(ItemFields.Flags));
            newItem.SetUInt32Value(ItemFields.Duration, GetUInt32Value(ItemFields.Duration));
            // player CAN be NULL in which case we must not update random properties because that accesses player's item update queue
            if (player != null)
                newItem.SetItemRandomProperties(GetItemRandomEnchantmentId());
            return newItem;
        }

        public bool IsBindedNotWith(Player player)
        {
            // not binded item
            if (!IsSoulBound())
                return false;

            // own item
            if (GetOwnerGUID() == player.GetGUID())
                return false;

            if (HasFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable))
                if (allowedGUIDs.Contains(player.GetGUID()))
                    return false;

            // BOA item case
            if (IsBoundAccountWide())
                return false;

            return true;
        }

        public override void BuildUpdate(Dictionary<Player, UpdateData> data)
        {
            Player owner = GetOwner();
            if (owner != null)
                BuildFieldsUpdate(owner, data);
            ClearUpdateMask(false);
        }

        public override void BuildDynamicValuesUpdate(UpdateType updateType, WorldPacket data, Player target)
        {
            if (!target)
                return;

            ByteBuffer fieldBuffer = new ByteBuffer();
            UpdateMask fieldMask = new UpdateMask(_dynamicValuesCount);

            uint[] flags = null;
            uint visibleFlag = GetDynamicUpdateFieldData(target, out flags);

            for (ushort index = 0; index < _dynamicValuesCount; ++index)
            {
                var values = _dynamicValues[index];
                if (_fieldNotifyFlags.HasAnyFlag(flags[index]) ||
                    ((updateType == UpdateType.Values ? _dynamicChangesMask[index] != DynamicFieldChangeType.Unchanged : !values.Empty()) && flags[index].HasAnyFlag(visibleFlag)))
                {
                    ByteBuffer arrayValuesBuffer = new ByteBuffer();
                    fieldMask.SetBit(index);

                    DynamicUpdateMask arrayValuesMask = new DynamicUpdateMask((uint)values.Length);
                    arrayValuesMask.EncodeDynamicFieldChangeType(_dynamicChangesMask[index], updateType);

                    if (updateType == UpdateType.Values && _dynamicChangesMask[index] == DynamicFieldChangeType.ValueAndSizeChanged)
                        arrayValuesMask.SetCount(values.Length);

                    if (index != (int)ItemDynamicFields.Modifiers)
                    {
                        for (var v = 0; v < values.Length; ++v)
                        {
                            if (updateType != UpdateType.Values || _dynamicChangesArrayMask[index].Get(v))
                            {
                                arrayValuesMask.SetBit(v);
                                arrayValuesBuffer.WriteUInt32(values[v]);
                            }
                        }
                    }
                    else
                    {
                        int m = 0;

                        if (updateType == UpdateType.Values && _dynamicChangesMask[index] != DynamicFieldChangeType.ValueAndSizeChanged && _changesMask.Get((int)ItemFields.ModifiersMask))
                        {
                            arrayValuesMask.DynamicFieldChangeType |= (int)DynamicFieldChangeType.ValueAndSizeChanged;
                            arrayValuesMask.SetCount(m);
                        }

                        // in case of ITEM_DYNAMIC_FIELD_MODIFIERS it is ITEM_FIELD_MODIFIERS_MASK that controls index of each value, not updatemask
                        // so we just have to write this starting from 0 index
                        for (var v = 0; v < values.Length; ++v)
                        {
                            if (values[v] != 0)
                            {
                                arrayValuesMask.SetBit(m++);
                                arrayValuesBuffer.WriteUInt32(values[v]);
                            }
                        }

                        if (updateType == UpdateType.Values && _changesMask.Get((int)ItemFields.ModifiersMask))
                            arrayValuesMask.SetCount(m);
                    }

                    arrayValuesMask.AppendToPacket(fieldBuffer);
                    fieldBuffer.WriteBytes(arrayValuesBuffer);
                }
            }

            fieldMask.AppendToPacket(data);
            data.WriteBytes(fieldBuffer);
        }

        public override void AddToObjectUpdate()
        {
            Player owner = GetOwner();
            if (owner)
                owner.GetMap().AddUpdateObject(this);
        }

        public override void RemoveFromObjectUpdate()
        {
            Player owner = GetOwner();
            if (owner)
                owner.GetMap().RemoveUpdateObject(this);
        }

        public void SaveRefundDataToDB()
        {
            DeleteRefundDataFromDB();

            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEM_REFUND_INSTANCE);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, GetRefundRecipient().GetCounter());
            stmt.AddValue(2, GetPaidMoney());
            stmt.AddValue(3, (ushort)GetPaidExtendedCost());
            DB.Characters.Execute(stmt);
        }

        public void DeleteRefundDataFromDB(SQLTransaction trans = null)
        {
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
            stmt.AddValue(0, GetGUID().GetCounter());
            if (trans != null)
                trans.Append(stmt);
            else
                DB.Characters.Execute(stmt);
        }

        public void SetNotRefundable(Player owner, bool changestate = true, SQLTransaction trans = null, bool addToCollection = true)
        {
            if (!HasFlag(ItemFields.Flags, ItemFieldFlags.Refundable))
                return;

            ItemExpirePurchaseRefund itemExpirePurchaseRefund = new ItemExpirePurchaseRefund();
            itemExpirePurchaseRefund.ItemGUID = GetGUID();
            owner.SendPacket(itemExpirePurchaseRefund);

            RemoveFlag(ItemFields.Flags, ItemFieldFlags.Refundable);
            // Following is not applicable in the trading procedure
            if (changestate)
                SetState(ItemUpdateState.Changed, owner);

            SetRefundRecipient(ObjectGuid.Empty);
            SetPaidMoney(0);
            SetPaidExtendedCost(0);
            DeleteRefundDataFromDB(trans);

            owner.DeleteRefundReference(GetGUID());
            if (addToCollection)
                owner.GetSession().GetCollectionMgr().AddItemAppearance(this);
        }

        public void UpdatePlayedTime(Player owner)
        {
            // Get current played time
            uint current_playtime = GetUInt32Value(ItemFields.CreatePlayedTime);
            // Calculate time elapsed since last played time update
            long curtime = Time.UnixTime;
            uint elapsed = (uint)(curtime - m_lastPlayedTimeUpdate);
            uint new_playtime = current_playtime + elapsed;
            // Check if the refund timer has expired yet
            if (new_playtime <= 2 * Time.Hour)
            {
                // No? Proceed.
                // Update the data field
                SetUInt32Value(ItemFields.CreatePlayedTime, new_playtime);
                // Flag as changed to get saved to DB
                SetState(ItemUpdateState.Changed, owner);
                // Speaks for itself
                m_lastPlayedTimeUpdate = curtime;
                return;
            }
            // Yes
            SetNotRefundable(owner);
        }

        public uint GetPlayedTime()
        {
            long curtime = Time.UnixTime;
            uint elapsed = (uint)(curtime - m_lastPlayedTimeUpdate);
            return GetUInt32Value(ItemFields.CreatePlayedTime) + elapsed;
        }

        public bool IsRefundExpired()
        {
            return (GetPlayedTime() > 2 * Time.Hour);
        }

        public void SetSoulboundTradeable(List<ObjectGuid> allowedLooters)
        {
            SetFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable);
            allowedGUIDs = allowedLooters;
        }

        public void ClearSoulboundTradeable(Player currentOwner)
        {
            RemoveFlag(ItemFields.Flags, ItemFieldFlags.BopTradeable);
            if (allowedGUIDs.Empty())
                return;

            currentOwner.GetSession().GetCollectionMgr().AddItemAppearance(this);
            allowedGUIDs.Clear();
            SetState(ItemUpdateState.Changed, currentOwner);
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEM_BOP_TRADE);
            stmt.AddValue(0, GetGUID().GetCounter());
            DB.Characters.Execute(stmt);
        }

        public bool CheckSoulboundTradeExpire()
        {
            // called from owner's update - GetOwner() MUST be valid
            if (GetUInt32Value(ItemFields.CreatePlayedTime) + 2 * Time.Hour < GetOwner().GetTotalPlayedTime())
            {
                ClearSoulboundTradeable(GetOwner());
                return true; // remove from tradeable list
            }

            return false;
        }

        bool IsValidTransmogrificationTarget()
        {
            ItemTemplate proto = GetTemplate();
            if (proto == null)
                return false;

            if (proto.GetClass() != ItemClass.Armor &&
                proto.GetClass() != ItemClass.Weapon)
                return false;

            if (proto.GetClass() == ItemClass.Weapon && proto.GetSubClass() == (uint)ItemSubClassWeapon.FishingPole)
                return false;

            if (proto.GetFlags2().HasAnyFlag(ItemFlags2.NoAlterItemVisual))
                return false;

            if (!HasStats())
                return false;

            return true;
        }

        bool HasStats()
        {
            if (GetItemRandomPropertyId() != 0)
                return true;

            ItemTemplate proto = GetTemplate();
            Player owner = GetOwner();
            for (byte i = 0; i < ItemConst.MaxStats; ++i)
            {
                if ((owner ? GetItemStatValue(i, owner) : proto.GetItemStatAllocation(i)) != 0)
                    return true;
            }

            return false;
        }

        static bool HasStats(ItemInstance itemInstance, BonusData bonus)
        {
            if (itemInstance.RandomPropertiesID != 0)
                return true;

            for (byte i = 0; i < ItemConst.MaxStats; ++i)
            {
                if (bonus.ItemStatAllocation[i] != 0)
                    return true;
            }

            return false;
        }

        static ItemTransmogrificationWeaponCategory GetTransmogrificationWeaponCategory(ItemTemplate proto)
        {
            if (proto.GetClass() == ItemClass.Weapon)
            {
                switch ((ItemSubClassWeapon)proto.GetSubClass())
                {
                    case ItemSubClassWeapon.Axe2:
                    case ItemSubClassWeapon.Mace2:
                    case ItemSubClassWeapon.Sword2:
                    case ItemSubClassWeapon.Staff:
                    case ItemSubClassWeapon.Polearm:
                        return ItemTransmogrificationWeaponCategory.Melee2H;
                    case ItemSubClassWeapon.Bow:
                    case ItemSubClassWeapon.Gun:
                    case ItemSubClassWeapon.Crossbow:
                        return ItemTransmogrificationWeaponCategory.Ranged;
                    case ItemSubClassWeapon.Axe:
                    case ItemSubClassWeapon.Mace:
                    case ItemSubClassWeapon.Sword:
                    case ItemSubClassWeapon.Warglaives:
                        return ItemTransmogrificationWeaponCategory.AxeMaceSword1H;
                    case ItemSubClassWeapon.Dagger:
                        return ItemTransmogrificationWeaponCategory.Dagger;
                    case ItemSubClassWeapon.Fist:
                        return ItemTransmogrificationWeaponCategory.Fist;
                    default:
                        break;
                }
            }

            return ItemTransmogrificationWeaponCategory.Invalid;
        }

        public static int[] ItemTransmogrificationSlots =
        {
            -1,                                                     // INVTYPE_NON_EQUIP
            EquipmentSlot.Head,                                    // INVTYPE_HEAD
            -1,                                                    // INVTYPE_NECK
            EquipmentSlot.Shoulders,                               // INVTYPE_SHOULDERS
            EquipmentSlot.Shirt,                                    // INVTYPE_BODY
            EquipmentSlot.Chest,                                   // INVTYPE_CHEST
            EquipmentSlot.Waist,                                   // INVTYPE_WAIST
            EquipmentSlot.Legs,                                    // INVTYPE_LEGS
            EquipmentSlot.Feet,                                    // INVTYPE_FEET
            EquipmentSlot.Wrist,                                  // INVTYPE_WRISTS
            EquipmentSlot.Hands,                                   // INVTYPE_HANDS
            -1,                                                     // INVTYPE_FINGER
            -1,                                                     // INVTYPE_TRINKET
            -1,                                                     // INVTYPE_WEAPON
            EquipmentSlot.OffHand,                                 // INVTYPE_SHIELD
            EquipmentSlot.MainHand,                                // INVTYPE_RANGED
            EquipmentSlot.Cloak,                                    // INVTYPE_CLOAK
            EquipmentSlot.MainHand,                                 // INVTYPE_2HWEAPON
            -1,                                                     // INVTYPE_BAG
            EquipmentSlot.Tabard,                                  // INVTYPE_TABARD
            EquipmentSlot.Chest,                                   // INVTYPE_ROBE
            EquipmentSlot.MainHand,                                // INVTYPE_WEAPONMAINHAND
            EquipmentSlot.MainHand,                                 // INVTYPE_WEAPONOFFHAND
            EquipmentSlot.OffHand,                                 // INVTYPE_HOLDABLE
            -1,                                                     // INVTYPE_AMMO
            -1,                                                     // INVTYPE_THROWN
            EquipmentSlot.MainHand,                                // INVTYPE_RANGEDRIGHT
            -1,                                                     // INVTYPE_QUIVER
            -1                                                      // INVTYPE_RELIC
        };

        public static bool CanTransmogrifyItemWithItem(Item item, ItemModifiedAppearanceRecord itemModifiedAppearance)
        {
            ItemTemplate source = Global.ObjectMgr.GetItemTemplate(itemModifiedAppearance.ItemID); // source
            ItemTemplate target = item.GetTemplate(); // dest

            if (source == null || target == null)
                return false;

            if (itemModifiedAppearance == item.GetItemModifiedAppearance())
                return false;

            if (!item.IsValidTransmogrificationTarget())
                return false;

            if (source.GetClass() != target.GetClass())
                return false;

            if (source.GetInventoryType() == InventoryType.Bag ||
                source.GetInventoryType() == InventoryType.Relic ||
                source.GetInventoryType() == InventoryType.Finger ||
                source.GetInventoryType() == InventoryType.Trinket ||
                source.GetInventoryType() == InventoryType.Ammo ||
                source.GetInventoryType() == InventoryType.Quiver)
                return false;

            if (source.GetSubClass() != target.GetSubClass())
            {
                switch (source.GetClass())
                {
                    case ItemClass.Weapon:
                        if (GetTransmogrificationWeaponCategory(source) != GetTransmogrificationWeaponCategory(target))
                            return false;
                        break;
                    case ItemClass.Armor:
                        if ((ItemSubClassArmor)source.GetSubClass() != ItemSubClassArmor.Cosmetic)
                            return false;
                        if (source.GetInventoryType() != target.GetInventoryType())
                            if (ItemTransmogrificationSlots[(int)source.GetInventoryType()] != ItemTransmogrificationSlots[(int)target.GetInventoryType()])
                                return false;
                        break;
                    default:
                        return false;
                }
            }

            return true;
        }

        uint GetBuyPrice(Player owner, out bool standardPrice)
        {
            return GetBuyPrice(GetTemplate(), (uint)GetQuality(), GetItemLevel(owner), out standardPrice);
        }

        static uint GetBuyPrice(ItemTemplate proto, uint quality, uint itemLevel, out bool standardPrice)
        {
            standardPrice = true;

            if (proto.GetFlags2().HasAnyFlag(ItemFlags2.OverrideGoldCost))
                return proto.GetBuyPrice();

            var qualityPrice = CliDB.ImportPriceQualityStorage.LookupByKey(quality + 1);
            if (qualityPrice == null)
                return 0;

            var basePrice = CliDB.ItemPriceBaseStorage.LookupByKey(proto.GetBaseItemLevel());
            if (basePrice == null)
                return 0;

            float qualityFactor = qualityPrice.Data;
            float baseFactor = 0.0f;

            var inventoryType = proto.GetInventoryType();

            if (inventoryType == InventoryType.Weapon ||
                inventoryType == InventoryType.Weapon2Hand ||
                inventoryType == InventoryType.WeaponMainhand ||
                inventoryType == InventoryType.WeaponOffhand ||
                inventoryType == InventoryType.Ranged ||
                inventoryType == InventoryType.Thrown ||
                inventoryType == InventoryType.RangedRight)
                baseFactor = basePrice.Weapon;
            else
                baseFactor = basePrice.Armor;

            if (inventoryType == InventoryType.Robe)
                inventoryType = InventoryType.Chest;

            if (proto.GetClass() == ItemClass.Gem && (ItemSubClassGem)proto.GetSubClass() == ItemSubClassGem.ArtifactRelic)
            {
                inventoryType = InventoryType.Weapon;
                baseFactor = basePrice.Weapon / 3.0f;
            }


            float typeFactor = 0.0f;
            sbyte weapType = -1;

            switch (inventoryType)
            {
                case InventoryType.Head:
                case InventoryType.Neck:
                case InventoryType.Shoulders:
                case InventoryType.Chest:
                case InventoryType.Waist:
                case InventoryType.Legs:
                case InventoryType.Feet:
                case InventoryType.Wrists:
                case InventoryType.Hands:
                case InventoryType.Finger:
                case InventoryType.Trinket:
                case InventoryType.Cloak:
                case InventoryType.Holdable:
                    {
                        var armorPrice = CliDB.ImportPriceArmorStorage.LookupByKey(inventoryType);
                        if (armorPrice == null)
                            return 0;

                        switch ((ItemSubClassArmor)proto.GetSubClass())
                        {
                            case ItemSubClassArmor.Miscellaneous:
                            case ItemSubClassArmor.Cloth:
                                typeFactor = armorPrice.ClothModifier;
                                break;
                            case ItemSubClassArmor.Leather:
                                typeFactor = armorPrice.LeatherModifier;
                                break;
                            case ItemSubClassArmor.Mail:
                                typeFactor = armorPrice.ChainModifier;
                                break;
                            case ItemSubClassArmor.Plate:
                                typeFactor = armorPrice.PlateModifier;
                                break;
                            default:
                                typeFactor = 1.0f;
                                break;
                        }

                        break;
                    }
                case InventoryType.Shield:
                    {
                        var shieldPrice = CliDB.ImportPriceShieldStorage.LookupByKey(2); // it only has two rows, it's unclear which is the one used
                        if (shieldPrice == null)
                            return 0;

                        typeFactor = shieldPrice.Data;
                        break;
                    }
                case InventoryType.WeaponMainhand:
                    weapType = 0;
                    break;
                case InventoryType.WeaponOffhand:
                    weapType = 1;
                    break;
                case InventoryType.Weapon:
                    weapType = 2;
                    break;
                case InventoryType.Weapon2Hand:
                    weapType = 3;
                    break;
                case InventoryType.Ranged:
                case InventoryType.RangedRight:
                case InventoryType.Relic:
                    weapType = 4;
                    break;
                default:
                    return proto.GetBuyPrice();
            }

            if (weapType != -1)
            {
                var weaponPrice = CliDB.ImportPriceWeaponStorage.LookupByKey(weapType + 1);
                if (weaponPrice == null)
                    return 0;

                typeFactor = weaponPrice.Data;
            }

            standardPrice = false;
            return (uint)(proto.GetPriceVariance() * typeFactor * baseFactor * qualityFactor * proto.GetPriceRandomValue());
        }

        public uint GetSellPrice(Player owner)
        {
            return GetSellPrice(GetTemplate(), (uint)GetQuality(), GetItemLevel(owner));
        }

        public static uint GetSellPrice(ItemTemplate proto, uint quality, uint itemLevel)
        {
            if (proto.GetFlags2().HasAnyFlag(ItemFlags2.OverrideGoldCost))
                return proto.GetSellPrice();

            bool standardPrice;
            uint cost = GetBuyPrice(proto, quality, itemLevel, out standardPrice);

            if (standardPrice)
            {
                ItemClassRecord classEntry = Global.DB2Mgr.GetItemClassByOldEnum(proto.GetClass());
                if (classEntry != null)
                {
                    uint buyCount = Math.Max(proto.GetBuyCount(), 1u);
                    return (uint)(cost * classEntry.PriceModifier / buyCount);
                }

                return 0;
            }
            else
                return proto.GetSellPrice();
        }

        public void ItemContainerSaveLootToDB()
        {
            // Saves the money and item loot associated with an openable item to the DB
            if (loot.isLooted()) // no money and no loot
                return;

            SQLTransaction trans = new SQLTransaction();

            loot.containerID = GetGUID(); // Save this for when a LootItem is removed

            // Save money
            if (loot.gold > 0)
            {
                PreparedStatement stmt_money = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
                stmt_money.AddValue(0, loot.containerID.GetCounter());
                trans.Append(stmt_money);

                stmt_money = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_MONEY);
                stmt_money.AddValue(0, loot.containerID.GetCounter());
                stmt_money.AddValue(1, loot.gold);
                trans.Append(stmt_money);
            }

            // Save items
            if (!loot.isLooted())
            {
                PreparedStatement stmt_items = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
                stmt_items.AddValue(0, loot.containerID.GetCounter());
                trans.Append(stmt_items);

                // Now insert the items
                foreach (var _li in loot.items)
                {
                    // When an item is looted, it doesn't get removed from the items collection
                    //  but we don't want to resave it.
                    if (!_li.canSave)
                        continue;

                    Player guid = GetOwner();
                    if (!_li.AllowedForPlayer(guid))
                        continue;

                    stmt_items = DB.Characters.GetPreparedStatement(CharStatements.INS_ITEMCONTAINER_ITEMS);

                    // container_id, item_id, item_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_prop, rnd_suffix
                    stmt_items.AddValue(0, loot.containerID.GetCounter());
                    stmt_items.AddValue(1, _li.itemid);
                    stmt_items.AddValue(2, _li.count);
                    stmt_items.AddValue(3, _li.follow_loot_rules);
                    stmt_items.AddValue(4, _li.freeforall);
                    stmt_items.AddValue(5, _li.is_blocked);
                    stmt_items.AddValue(6, _li.is_counted);
                    stmt_items.AddValue(7, _li.is_underthreshold);
                    stmt_items.AddValue(8, _li.needs_quest);
                    stmt_items.AddValue(9, (byte)_li.randomPropertyId.Type);
                    stmt_items.AddValue(10, _li.randomPropertyId.Id);
                    stmt_items.AddValue(11, _li.randomSuffix);
                    stmt_items.AddValue(12, _li.context);

                    string bonusListIDs = "";
                    foreach (int bonusListID in _li.BonusListIDs)
                        bonusListIDs += bonusListID + ' ';

                    stmt_items.AddValue(13, bonusListIDs);
                    trans.Append(stmt_items);
                }
            }
            DB.Characters.CommitTransaction(trans);
        }

        public bool ItemContainerLoadLootFromDB()
        {
            // Loads the money and item loot associated with an openable item from the DB
            // Default. If there are no records for this item then it will be rolled for in Player.SendLoot()
            m_lootGenerated = false;

            // Save this for later use
            loot.containerID = GetGUID();

            // First, see if there was any money loot. This gets added directly to the container.
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, loot.containerID.GetCounter());
            SQLResult money_result = DB.Characters.Query(stmt);

            if (!money_result.IsEmpty())
            {
                loot.gold = money_result.Read<uint>(0);
            }

            // Next, load any items that were saved
            stmt = DB.Characters.GetPreparedStatement(CharStatements.SEL_ITEMCONTAINER_ITEMS);
            stmt.AddValue(0, loot.containerID.GetCounter());
            SQLResult item_result = DB.Characters.Query(stmt);

            if (!item_result.IsEmpty())
            {
                // Get a LootTemplate for the container item. This is where
                //  the saved loot was originally rolled from, we will copy conditions from it
                LootTemplate lt = LootStorage.Items.GetLootFor(GetEntry());
                if (lt != null)
                {
                    do
                    {
                        // Create an empty LootItem
                        LootItem loot_item = new LootItem();

                        // item_id, itm_count, follow_rules, ffa, blocked, counted, under_threshold, needs_quest, rnd_prop, rnd_suffix
                        loot_item.itemid = item_result.Read<uint>(0);
                        loot_item.count = item_result.Read<byte>(1);
                        loot_item.follow_loot_rules = item_result.Read<bool>(2);
                        loot_item.freeforall = item_result.Read<bool>(3);
                        loot_item.is_blocked = item_result.Read<bool>(4);
                        loot_item.is_counted = item_result.Read<bool>(5);
                        loot_item.canSave = true;
                        loot_item.is_underthreshold = item_result.Read<bool>(6);
                        loot_item.needs_quest = item_result.Read<bool>(7);
                        loot_item.randomPropertyId = new ItemRandomEnchantmentId((ItemRandomEnchantmentType)item_result.Read<byte>(8), item_result.Read<uint>(9));
                        loot_item.randomSuffix = item_result.Read<uint>(10);
                        loot_item.context = item_result.Read<byte>(11);

                        StringArray bonusLists = new StringArray(item_result.Read<string>(12), ' ');
                        if (!bonusLists.IsEmpty())
                        {
                            foreach (string line in bonusLists)
                            {
                                if (uint.TryParse(line, out uint id))
                                    loot_item.BonusListIDs.Add(id);
                            }
                        }

                        // Copy the extra loot conditions from the item in the loot template
                        lt.CopyConditions(loot_item);

                        // If container item is in a bag, add that player as an allowed looter
                        if (GetBagSlot() != 0)
                            loot_item.AddAllowedLooter(GetOwner());

                        // Finally add the LootItem to the container
                        loot.items.Add(loot_item);

                        // Increment unlooted count
                        loot.unlootedCount++;
                    }
                    while (item_result.NextRow());
                }
            }

            // Mark the item if it has loot so it won't be generated again on open
            m_lootGenerated = !loot.isLooted();

            return m_lootGenerated;
        }

        void ItemContainerDeleteLootItemsFromDB()
        {
            // Deletes items associated with an openable item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEMS);
            stmt.AddValue(0, GetGUID().GetCounter());
            DB.Characters.Execute(stmt);
        }

        void ItemContainerDeleteLootItemFromDB(uint itemID)
        {
            // Deletes a single item associated with an openable item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_ITEM);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, itemID);
            DB.Characters.Execute(stmt);
        }

        void ItemContainerDeleteLootMoneyFromDB()
        {
            // Deletes the money loot associated with an openable item from the DB
            PreparedStatement stmt = DB.Characters.GetPreparedStatement(CharStatements.DEL_ITEMCONTAINER_MONEY);
            stmt.AddValue(0, GetGUID().GetCounter());
            DB.Characters.Execute(stmt);
        }

        public void ItemContainerDeleteLootMoneyAndLootItemsFromDB()
        {
            // Deletes money and items associated with an openable item from the DB
            ItemContainerDeleteLootMoneyFromDB();
            ItemContainerDeleteLootItemsFromDB();
        }

        public uint GetItemLevel(Player owner)
        {
            uint minItemLevel = owner.GetUInt32Value(UnitFields.MinItemLevel);
            uint minItemLevelCutoff = owner.GetUInt32Value(UnitFields.MinItemLevelCutoff);
            uint maxItemLevel = GetTemplate().GetFlags3().HasAnyFlag(ItemFlags3.IgnoreItemLevelCapInPvp) ? 0 : owner.GetUInt32Value(UnitFields.MaxItemlevel);
            bool pvpBonus = owner.IsUsingPvpItemLevels();
            return GetItemLevel(GetTemplate(), _bonusData, owner.getLevel(), GetModifier(ItemModifier.ScalingStatDistributionFixedLevel), GetModifier(ItemModifier.UpgradeId),
                minItemLevel, minItemLevelCutoff, maxItemLevel, pvpBonus);
        }

        public static uint GetItemLevel(ItemTemplate itemTemplate, BonusData bonusData, uint level, uint fixedLevel, uint upgradeId,
            uint minItemLevel, uint minItemLevelCutoff, uint maxItemLevel, bool pvpBonus)
        {
            if (itemTemplate == null)
                return 1;

            uint itemLevel = itemTemplate.GetBaseItemLevel();
            ScalingStatDistributionRecord ssd = CliDB.ScalingStatDistributionStorage.LookupByKey(bonusData.ScalingStatDistribution);
            if (ssd != null)
            {
                if (fixedLevel != 0)
                    level = fixedLevel;
                else
                    level = (uint)Math.Min(Math.Max(level, ssd.MinLevel), ssd.MaxLevel);

                ContentTuningRecord contentTuning = CliDB.ContentTuningStorage.LookupByKey(bonusData.ContentTuningId);
                if (contentTuning != null)
                    if ((Convert.ToBoolean(contentTuning.Flags & 2) || contentTuning.MinLevel != 0 || contentTuning.MaxLevel != 0) && !Convert.ToBoolean(contentTuning.Flags & 4))
                        level = (uint)Math.Min(Math.Max(level, contentTuning.MinLevel), contentTuning.MaxLevel);

                uint heirloomIlvl = (uint)Global.DB2Mgr.GetCurveValueAt(ssd.PlayerLevelToItemLevelCurveID, level);
                if (heirloomIlvl != 0)
                    itemLevel = heirloomIlvl;
            }

            itemLevel += (uint)bonusData.ItemLevelBonus;

            for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
                itemLevel += bonusData.GemItemLevelBonus[i];

            uint itemLevelBeforeUpgrades = itemLevel;
            ItemUpgradeRecord upgrade = CliDB.ItemUpgradeStorage.LookupByKey(upgradeId);
            if (upgrade != null)
                itemLevel += upgrade.ItemLevelIncrement;

            if (pvpBonus)
                itemLevel += Global.DB2Mgr.GetPvpItemLevelBonus(itemTemplate.GetId());

            if (itemTemplate.GetInventoryType() != InventoryType.NonEquip)
            {
                if (minItemLevel != 0 && (minItemLevelCutoff == 0 || itemLevelBeforeUpgrades >= minItemLevelCutoff) && itemLevel < minItemLevel)
                    itemLevel = minItemLevel;

                if (maxItemLevel != 0 && itemLevel > maxItemLevel)
                    itemLevel = maxItemLevel;
            }

            return Math.Min(Math.Max(itemLevel, 1), 1300);
        }

        public int GetItemStatValue(uint index, Player owner)
        {
            Cypher.Assert(index < ItemConst.MaxStats);
            uint itemLevel = GetItemLevel(owner);
            uint randomPropPoints = ItemEnchantment.GetRandomPropertyPoints(itemLevel, GetQuality(), GetTemplate().GetInventoryType(), GetTemplate().GetSubClass());
            if (randomPropPoints != 0)
            {
                float statValue = (_bonusData.ItemStatAllocation[index] * randomPropPoints) * 0.0001f;
                GtItemSocketCostPerLevelRecord gtCost = CliDB.ItemSocketCostPerLevelGameTable.GetRow(itemLevel);
                if (gtCost != null)
                    statValue -= (_bonusData.ItemStatSocketCostMultiplier[index] * gtCost.SocketCost);

                return (int)(Math.Floor(statValue + 0.5f));
            }

            return 0;
        }

        public ItemDisenchantLootRecord GetDisenchantLoot(Player owner)
        {
            return GetDisenchantLoot(GetTemplate(), (uint)GetQuality(), GetItemLevel(owner));
        }

        public static ItemDisenchantLootRecord GetDisenchantLoot(ItemTemplate itemTemplate, uint quality, uint itemLevel)
        {
            if (itemTemplate.GetFlags().HasAnyFlag(ItemFlags.Conjured | ItemFlags.NoDisenchant) || itemTemplate.GetBonding() == ItemBondingType.Quest)
                return null;

            if (itemTemplate.GetArea() != 0 || itemTemplate.GetMap() != 0 || itemTemplate.GetMaxStackSize() > 1)
                return null;

            if (GetSellPrice(itemTemplate, quality, itemLevel) == 0 && !Global.DB2Mgr.HasItemCurrencyCost(itemTemplate.GetId()))
                return null;

            byte itemClass = (byte)itemTemplate.GetClass();
            uint itemSubClass = itemTemplate.GetSubClass();
            byte expansion = itemTemplate.GetRequiredExpansion();
            foreach (ItemDisenchantLootRecord disenchant in CliDB.ItemDisenchantLootStorage.Values)
            {
                if (disenchant.Class != itemClass)
                    continue;

                if (disenchant.Subclass >= 0 && itemSubClass != 0)
                    continue;

                if (disenchant.Quality != quality)
                    continue;

                if (disenchant.MinLevel > itemLevel || disenchant.MaxLevel < itemLevel)
                    continue;

                if (disenchant.ExpansionID != -2 && disenchant.ExpansionID != expansion)
                    continue;

                return disenchant;
            }

            return null;
        }

        public uint GetDisplayId(Player owner)
        {
            ItemModifier transmogModifier = ItemModifier.TransmogAppearanceAllSpecs;
            if (HasFlag(ItemFields.ModifiersMask, ItemConst.AppearanceModifierMaskSpecSpecific))
                transmogModifier = ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()];

            ItemModifiedAppearanceRecord transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(GetModifier(transmogModifier));
            if (transmog != null)
            {
                ItemAppearanceRecord itemAppearance = CliDB.ItemAppearanceStorage.LookupByKey(transmog.ItemAppearanceID);
                if (itemAppearance != null)
                    return itemAppearance.ItemDisplayInfoID;
            }

            return Global.DB2Mgr.GetItemDisplayId(GetEntry(), GetAppearanceModId());
        }

        public ItemModifiedAppearanceRecord GetItemModifiedAppearance()
        {
            return Global.DB2Mgr.GetItemModifiedAppearance(GetEntry(), _bonusData.AppearanceModID);
        }

        public uint GetModifier(ItemModifier modifier)
        {
            return GetDynamicValue(ItemDynamicFields.Modifiers, (byte)modifier);
        }

        public void SetModifier(ItemModifier modifier, uint value)
        {
            ApplyModFlag(ItemFields.ModifiersMask, 1 << (int)modifier, value != 0);
            SetDynamicValue(ItemDynamicFields.Modifiers, (byte)modifier, value);
        }

        public uint GetVisibleEntry(Player owner)
        {
            ItemModifier transmogModifier = ItemModifier.TransmogAppearanceAllSpecs;
            if (HasFlag(ItemFields.ModifiersMask, ItemConst.AppearanceModifierMaskSpecSpecific))
                transmogModifier = ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()];

            ItemModifiedAppearanceRecord transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(GetModifier(transmogModifier));
            if (transmog != null)
                return transmog.ItemID;

            return GetEntry();
        }

        public ushort GetVisibleAppearanceModId(Player owner)
        {
            ItemModifier transmogModifier = ItemModifier.TransmogAppearanceAllSpecs;
            if (HasFlag(ItemFields.ModifiersMask, ItemConst.AppearanceModifierMaskSpecSpecific))
                transmogModifier = ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()];

            ItemModifiedAppearanceRecord transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(GetModifier(transmogModifier));
            if (transmog != null)
                return transmog.ItemAppearanceModifierID;

            return (ushort)GetAppearanceModId();
        }

        public uint GetVisibleEnchantmentId(Player owner)
        {
            ItemModifier illusionModifier = ItemModifier.EnchantIllusionAllSpecs;
            if (HasFlag(ItemFields.ModifiersMask, ItemConst.IllusionModifierMaskSpecSpecific))
                illusionModifier = ItemConst.IllusionModifierSlotBySpec[owner.GetActiveTalentGroup()];

            uint enchantIllusion = GetModifier(illusionModifier);
            if (enchantIllusion != 0)
                return enchantIllusion;

            return GetEnchantmentId(EnchantmentSlot.Perm);
        }

        public ushort GetVisibleItemVisual(Player owner)
        {
            SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetVisibleEnchantmentId(owner));
            if (enchant != null)
                return enchant.ItemVisual;

            return 0;
        }

        public void AddBonuses(uint bonusListID)
        {
            if (HasDynamicValue(ItemDynamicFields.BonusListIds, bonusListID))
                return;

            var bonuses = Global.DB2Mgr.GetItemBonusList(bonusListID);
            if (bonuses != null)
            {
                AddDynamicValue(ItemDynamicFields.BonusListIds, bonusListID);
                foreach (ItemBonusRecord bonus in bonuses)
                    _bonusData.AddBonus((ItemBonusType)bonus.Type, bonus.Value);

                SetUInt32Value(ItemFields.AppearanceModId, _bonusData.AppearanceModID);
            }
        }

        public List<ItemDynamicFieldArtifactPowers> GetArtifactPowers()
        {
            return GetDynamicStructuredValues<ItemDynamicFieldArtifactPowers>(ItemDynamicFields.ArtifactPowers);
        }

        public ItemDynamicFieldArtifactPowers GetArtifactPower(uint artifactPowerId)
        {
            var index = m_artifactPowerIdToIndex.LookupByKey(artifactPowerId);
            if (index != 0)
                return GetDynamicStructuredValue<ItemDynamicFieldArtifactPowers>(ItemDynamicFields.ArtifactPowers, index);

            return null;
        }

        public void SetArtifactPower(ItemDynamicFieldArtifactPowers artifactPower, bool createIfMissing = false)
        {
            var foundIndex = m_artifactPowerIdToIndex.LookupByKey(artifactPower.ArtifactPowerId);
            ushort index;
            if (foundIndex != 0)
                index = foundIndex;
            else
            {
                if (!createIfMissing)
                    return;

                index = (ushort)m_artifactPowerIdToIndex.Count;
                m_artifactPowerIdToIndex[artifactPower.ArtifactPowerId] = index;
            }

            SetDynamicStructuredValue(ItemDynamicFields.ArtifactPowers, index, artifactPower);
        }

        public void InitArtifactPowers(byte artifactId, byte artifactTier)
        {
            foreach (ArtifactPowerRecord artifactPower in Global.DB2Mgr.GetArtifactPowers(artifactId))
            {
                if (artifactPower.Tier != artifactTier)
                    continue;

                if (m_artifactPowerIdToIndex.ContainsKey(artifactPower.Id))
                    continue;

                ItemDynamicFieldArtifactPowers powerData = new ItemDynamicFieldArtifactPowers();
                powerData.ArtifactPowerId = artifactPower.Id;
                powerData.PurchasedRank = 0;
                powerData.CurrentRankWithBonus = (byte)((artifactPower.Flags & ArtifactPowerFlag.First) == ArtifactPowerFlag.First ? 1 : 0);
                SetArtifactPower(powerData, true);
            }
        }

        public uint GetTotalPurchasedArtifactPowers()
        {
            uint purchasedRanks = 0;
            foreach (ItemDynamicFieldArtifactPowers power in GetArtifactPowers())
                purchasedRanks += power.PurchasedRank;

            return purchasedRanks;
        }

        void ApplyArtifactPowerEnchantmentBonuses(EnchantmentSlot slot, uint enchantId, bool apply, Player owner)
        {
            SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(enchantId);
            if (enchant != null)
            {
                for (uint i = 0; i < ItemConst.MaxItemEnchantmentEffects; ++i)
                {
                    switch (enchant.Effect[i])
                    {
                        case ItemEnchantmentType.ArtifactPowerBonusRankByType:
                            {
                                foreach (ItemDynamicFieldArtifactPowers artifactPower in GetArtifactPowers())
                                {
                                    if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label == enchant.EffectArg[i])
                                    {
                                        ItemDynamicFieldArtifactPowers newPower = artifactPower;
                                        if (apply)
                                            newPower.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                        else
                                            newPower.CurrentRankWithBonus -= (byte)enchant.EffectPointsMin[i];

                                        if (IsEquipped())
                                        {
                                            ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newPower.CurrentRankWithBonus != 0 ? newPower.CurrentRankWithBonus - 1 : 0));
                                            if (artifactPowerRank != null)
                                                owner.ApplyArtifactPowerRank(this, artifactPowerRank, newPower.CurrentRankWithBonus != 0);
                                        }

                                        SetArtifactPower(newPower);
                                    }
                                }
                            }
                            break;
                        case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                            {
                                ItemDynamicFieldArtifactPowers artifactPower = GetArtifactPower(enchant.EffectArg[i]);
                                if (artifactPower != null)
                                {
                                    ItemDynamicFieldArtifactPowers newPower = artifactPower;
                                    if (apply)
                                        newPower.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                    else
                                        newPower.CurrentRankWithBonus -= (byte)enchant.EffectPointsMin[i];

                                    if (IsEquipped())
                                    {
                                        ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newPower.CurrentRankWithBonus != 0 ? newPower.CurrentRankWithBonus - 1 : 0));
                                        if (artifactPowerRank != null)
                                            owner.ApplyArtifactPowerRank(this, artifactPowerRank, newPower.CurrentRankWithBonus != 0);
                                    }

                                    SetArtifactPower(newPower);
                                }
                            }
                            break;
                        case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                            if (slot >= EnchantmentSlot.Sock1 && slot <= EnchantmentSlot.Sock3 && _bonusData.GemRelicType[slot - EnchantmentSlot.Sock1] != -1)
                            {
                                ArtifactPowerPickerRecord artifactPowerPicker = CliDB.ArtifactPowerPickerStorage.LookupByKey(enchant.EffectArg[i]);
                                if (artifactPowerPicker != null)
                                {
                                    PlayerConditionRecord playerCondition = CliDB.PlayerConditionStorage.LookupByKey(artifactPowerPicker.PlayerConditionID);
                                    if (playerCondition == null || ConditionManager.IsPlayerMeetingCondition(owner, playerCondition))
                                    {
                                        foreach (ItemDynamicFieldArtifactPowers artifactPower in GetArtifactPowers())
                                        {
                                            if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label == _bonusData.GemRelicType[slot - EnchantmentSlot.Sock1])
                                            {
                                                ItemDynamicFieldArtifactPowers newPower = artifactPower;
                                                if (apply)
                                                    newPower.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                                else
                                                    newPower.CurrentRankWithBonus -= (byte)enchant.EffectPointsMin[i];

                                                if (IsEquipped())
                                                {
                                                    ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newPower.CurrentRankWithBonus != 0 ? newPower.CurrentRankWithBonus - 1 : 0));
                                                    if (artifactPowerRank != null)
                                                        owner.ApplyArtifactPowerRank(this, artifactPowerRank, newPower.CurrentRankWithBonus != 0);
                                                }

                                                SetArtifactPower(newPower);
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public void CopyArtifactDataFromParent(Item parent)
        {
            Array.Copy(parent.GetBonus().GemItemLevelBonus, _bonusData.GemItemLevelBonus, _bonusData.GemItemLevelBonus.Length);
            SetModifier(ItemModifier.ArtifactAppearanceId, parent.GetModifier(ItemModifier.ArtifactAppearanceId));
            SetAppearanceModId(parent.GetAppearanceModId());
        }

        public void GiveArtifactXp(ulong amount, Item sourceItem, ArtifactCategory artifactCategoryId)
        {
            Player owner = GetOwner();
            if (!owner)
                return;

            if (artifactCategoryId != 0)
            {
                uint artifactKnowledgeLevel = 1;
                if (sourceItem != null && sourceItem.GetModifier(ItemModifier.ArtifactKnowledgeLevel) != 0)
                    artifactKnowledgeLevel = sourceItem.GetModifier(ItemModifier.ArtifactKnowledgeLevel);

                GtArtifactKnowledgeMultiplierRecord artifactKnowledge = CliDB.ArtifactKnowledgeMultiplierGameTable.GetRow(artifactKnowledgeLevel);
                if (artifactKnowledge != null)
                    amount = (ulong)(amount * artifactKnowledge.Multiplier);

                if (amount >= 5000)
                    amount = 50 * (amount / 50);
                else if (amount >= 1000)
                    amount = 25 * (amount / 25);
                else if (amount >= 50)
                    amount = 5 * (amount / 5);
            }

            SetUInt64Value(ItemFields.ArtifactXp, GetUInt64Value(ItemFields.ArtifactXp) + amount);

            ArtifactXpGain artifactXpGain = new ArtifactXpGain();
            artifactXpGain.ArtifactGUID = GetGUID();
            artifactXpGain.Amount = amount;
            owner.SendPacket(artifactXpGain);

            SetState(ItemUpdateState.Changed, owner);
        }

        public void SetFixedLevel(uint level)
        {
            if (!_bonusData.HasFixedLevel || GetModifier(ItemModifier.ScalingStatDistributionFixedLevel) != 0)
                return;

            ScalingStatDistributionRecord ssd = CliDB.ScalingStatDistributionStorage.LookupByKey(_bonusData.ScalingStatDistribution);
            if (ssd != null)
            {
                level = (uint)Math.Min(Math.Max(level, ssd.MinLevel), ssd.MaxLevel);

                ContentTuningRecord contentTuning = CliDB.ContentTuningStorage.LookupByKey(_bonusData.ContentTuningId);
                if (contentTuning != null)
                    if ((contentTuning.Flags.HasAnyFlag(2) || contentTuning.MinLevel != 0 || contentTuning.MaxLevel != 0) && !contentTuning.Flags.HasAnyFlag(4))
                        level = (uint)Math.Min(Math.Max(level, contentTuning.MinLevel), contentTuning.MaxLevel);

                SetModifier(ItemModifier.ScalingStatDistributionFixedLevel, level);
            }
        }

        public int GetRequiredLevel()
        {
            if (_bonusData.RequiredLevelOverride != 0)
                return _bonusData.RequiredLevelOverride;
            else if (_bonusData.HasFixedLevel)
                return (int)GetModifier(ItemModifier.ScalingStatDistributionFixedLevel);
            else
                return _bonusData.RequiredLevel;
        }

        public static void AddItemsSetItem(Player player, Item item)
        {
            ItemTemplate proto = item.GetTemplate();
            uint setid = proto.GetItemSet();

            ItemSetRecord set = CliDB.ItemSetStorage.LookupByKey(setid);
            if (set == null)
            {
                Log.outError(LogFilter.Sql, "Item set {0} for item (id {1}) not found, mods not applied.", setid, proto.GetId());
                return;
            }

            if (set.RequiredSkill != 0 && player.GetSkillValue((SkillType)set.RequiredSkill) < set.RequiredSkillRank)
                return;

            if (set.SetFlags.HasAnyFlag(ItemSetFlags.LegacyInactive))
                return;

            ItemSetEffect eff = null;
            for (int x = 0; x < player.ItemSetEff.Count; ++x)
            {
                if (player.ItemSetEff[x]?.ItemSetID == setid)
                {
                    eff = player.ItemSetEff[x];
                    break;
                }
            }

            if (eff == null)
            {
                eff = new ItemSetEffect();
                eff.ItemSetID = setid;

                int x = 0;
                for (; x < player.ItemSetEff.Count; ++x)
                    if (player.ItemSetEff[x] == null)
                        break;

                if (x < player.ItemSetEff.Count)
                    player.ItemSetEff[x] = eff;
                else
                    player.ItemSetEff.Add(eff);
            }

            ++eff.EquippedItemCount;

            List<ItemSetSpellRecord> itemSetSpells = Global.DB2Mgr.GetItemSetSpells(setid);
            foreach (var itemSetSpell in itemSetSpells)
            {
                //not enough for  spell
                if (itemSetSpell.Threshold > eff.EquippedItemCount)
                    continue;

                if (eff.SetBonuses.Contains(itemSetSpell))
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Player, "WORLD: unknown spell id {0} in items set {1} effects", itemSetSpell.SpellID, setid);
                    continue;
                }

                eff.SetBonuses.Add(itemSetSpell);
                // spell cast only if fit form requirement, in other case will cast at form change
                if (itemSetSpell.ChrSpecID == 0 || itemSetSpell.ChrSpecID == player.GetUInt32Value(PlayerFields.CurrentSpecId))
                    player.ApplyEquipSpell(spellInfo, null, true);
            }
        }

        public static void RemoveItemsSetItem(Player player, ItemTemplate proto)
        {
            uint setid = proto.GetItemSet();

            ItemSetRecord set = CliDB.ItemSetStorage.LookupByKey(setid);
            if (set == null)
            {
                Log.outError(LogFilter.Sql, "Item set {0} for item {1} not found, mods not removed.", setid, proto.GetId());
                return;
            }

            ItemSetEffect eff = null;
            int setindex = 0;
            for (; setindex < player.ItemSetEff.Count; setindex++)
            {
                if (player.ItemSetEff[setindex] != null && player.ItemSetEff[setindex].ItemSetID == setid)
                {
                    eff = player.ItemSetEff[setindex];
                    break;
                }
            }

            // can be in case now enough skill requirement for set appling but set has been appliend when skill requirement not enough
            if (eff == null)
                return;

            --eff.EquippedItemCount;

            List<ItemSetSpellRecord> itemSetSpells = Global.DB2Mgr.GetItemSetSpells(setid);
            foreach (ItemSetSpellRecord itemSetSpell in itemSetSpells)
            {
                // enough for spell
                if (itemSetSpell.Threshold <= eff.EquippedItemCount)
                    continue;

                if (!eff.SetBonuses.Contains(itemSetSpell))
                    continue;

                player.ApplyEquipSpell(Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID), null, false);
                eff.SetBonuses.Remove(itemSetSpell);
            }

            if (eff.EquippedItemCount == 0)                                    //all items of a set were removed
            {
                Cypher.Assert(eff == player.ItemSetEff[setindex]);
                player.ItemSetEff[setindex] = null;
            }
        }

        public BonusData GetBonus() { return _bonusData; }

        public ObjectGuid GetOwnerGUID() { return GetGuidValue(ItemFields.Owner); }
        public void SetOwnerGUID(ObjectGuid guid) { SetGuidValue(ItemFields.Owner, guid); }

        public ItemBondingType GetBonding() { return _bonusData.Bonding; }
        public void SetBinding(bool val) { ApplyModFlag(ItemFields.Flags, (uint)ItemFieldFlags.Soulbound, val); }
        public bool IsSoulBound() { return HasFlag(ItemFields.Flags, ItemFieldFlags.Soulbound); }
        public bool IsBoundAccountWide() { return GetTemplate().GetFlags().HasAnyFlag(ItemFlags.IsBoundToAccount); }
        bool IsBattlenetAccountBound() { return GetTemplate().GetFlags2().HasAnyFlag(ItemFlags2.BnetAccountTradeOk); }

        public Bag ToBag()
        {
            if (IsBag())
                return (this as Bag);
            else
                return null;
        }

        public bool IsLocked() { return !HasFlag(ItemFields.Flags, ItemFieldFlags.Unlocked); }
        public bool IsBag() { return GetTemplate().GetInventoryType() == InventoryType.Bag; }
        public bool IsCurrencyToken() { return GetTemplate().IsCurrencyToken(); }
        public bool IsBroken() { return GetUInt32Value(ItemFields.MaxDurability) > 0 && GetUInt32Value(ItemFields.Durability) == 0; }
        public void SetInTrade(bool b = true) { mb_in_trade = b; }
        public bool IsInTrade() { return mb_in_trade; }

        public uint GetCount() { return GetUInt32Value(ItemFields.StackCount); }
        public uint GetMaxStackCount() { return GetTemplate().GetMaxStackSize(); }

        public byte GetSlot() { return m_slot; }
        public Bag GetContainer() { return m_container; }
        public void SetSlot(byte slot) { m_slot = slot; }
        public ushort GetPos() { return (ushort)(GetBagSlot() << 8 | GetSlot()); }
        public void SetContainer(Bag container) { m_container = container; }

        bool IsInBag() { return m_container != null; }

        public int GetItemRandomPropertyId() { return GetInt32Value(ItemFields.RandomPropertiesId); }
        public uint GetItemSuffixFactor() { return GetUInt32Value(ItemFields.PropertySeed); }
        public ItemRandomEnchantmentId GetItemRandomEnchantmentId() { return m_randomEnchantment; }
        public uint GetEnchantmentId(EnchantmentSlot slot)
        {
            return GetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Id);
        }
        public uint GetEnchantmentDuration(EnchantmentSlot slot)
        {
            return GetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Duration);
        }
        public uint GetEnchantmentCharges(EnchantmentSlot slot)
        {
            return GetUInt32Value(ItemFields.Enchantment + (int)slot * (int)EnchantmentOffset.Max + (int)EnchantmentOffset.Charges);
        }

        public string GetText() { return m_text; }
        public void SetText(string text) { m_text = text; }

        public int GetSpellCharges(int index = 0) { return GetInt32Value(ItemFields.SpellCharges + index); }
        public void SetSpellCharges(int index, int value) { SetInt32Value(ItemFields.SpellCharges + index, value); }

        public ItemUpdateState GetState() { return uState; }

        public bool IsInUpdateQueue() { return uQueuePos != -1; }
        public int GetQueuePos() { return uQueuePos; }
        public void FSetState(ItemUpdateState state)// forced
        {
            uState = state;
        }

        public override bool hasQuest(uint quest_id) { return GetTemplate().GetStartQuest() == quest_id; }
        public override bool hasInvolvedQuest(uint quest_id) { return false; }
        public bool IsPotion() { return GetTemplate().IsPotion(); }
        public bool IsVellum() { return GetTemplate().IsVellum(); }
        public bool IsConjuredConsumable() { return GetTemplate().IsConjuredConsumable(); }
        public bool IsRangedWeapon() { return GetTemplate().IsRangedWeapon(); }
        public ItemQuality GetQuality() { return _bonusData.Quality; }
        public int GetItemStatType(uint index)
        {
            Cypher.Assert(index < ItemConst.MaxStats);
            return _bonusData.ItemStatType[index];
        }
        public SocketColor GetSocketColor(uint index)
        {
            Cypher.Assert(index < ItemConst.MaxGemSockets);
            return _bonusData.socketColor[index];
        }
        public uint GetAppearanceModId() { return GetUInt32Value(ItemFields.AppearanceModId); }
        public void SetAppearanceModId(uint appearanceModId) { SetUInt32Value(ItemFields.AppearanceModId, appearanceModId); }
        public uint GetArmor(Player owner) { return GetTemplate().GetArmor(GetItemLevel(owner)); }
        public void GetDamage(Player owner, out float minDamage, out float maxDamage) { GetTemplate().GetDamage(GetItemLevel(owner), out minDamage, out maxDamage); }
        public float GetRepairCostMultiplier() { return _bonusData.RepairCostMultiplier; }
        public uint GetScalingStatDistribution() { return _bonusData.ScalingStatDistribution; }

        public void SetRefundRecipient(ObjectGuid guid) { m_refundRecipient = guid; }
        public void SetPaidMoney(ulong money) { m_paidMoney = money; }
        public void SetPaidExtendedCost(uint iece) { m_paidExtendedCost = iece; }

        public ObjectGuid GetRefundRecipient() { return m_refundRecipient; }
        public ulong GetPaidMoney() { return m_paidMoney; }
        public uint GetPaidExtendedCost() { return m_paidExtendedCost; }

        public uint GetScriptId() { return GetTemplate().ScriptId; }

        public ObjectGuid GetChildItem() { return m_childItem; }
        public void SetChildItem(ObjectGuid childItem) { m_childItem = childItem; }

        //Static
        public static bool ItemCanGoIntoBag(ItemTemplate pProto, ItemTemplate pBagProto)
        {
            if (pProto == null || pBagProto == null)
                return false;

            switch (pBagProto.GetClass())
            {
                case ItemClass.Container:
                    switch ((ItemSubClassContainer)pBagProto.GetSubClass())
                    {
                        case ItemSubClassContainer.Container:
                            return true;
                        case ItemSubClassContainer.SoulContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.SoulShards))
                                return false;
                            return true;
                        case ItemSubClassContainer.HerbContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.Herbs))
                                return false;
                            return true;
                        case ItemSubClassContainer.EnchantingContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.EnchantingSupp))
                                return false;
                            return true;
                        case ItemSubClassContainer.MiningContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.MiningSupp))
                                return false;
                            return true;
                        case ItemSubClassContainer.EngineeringContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.EngineeringSupp))
                                return false;
                            return true;
                        case ItemSubClassContainer.GemContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.Gems))
                                return false;
                            return true;
                        case ItemSubClassContainer.LeatherworkingContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.LeatherworkingSupp))
                                return false;
                            return true;
                        case ItemSubClassContainer.InscriptionContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.InscriptionSupp))
                                return false;
                            return true;
                        case ItemSubClassContainer.TackleContainer:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.FishingSupp))
                                return false;
                            return true;
                        case ItemSubClassContainer.CookingContainer:
                            if (!pProto.GetBagFamily().HasAnyFlag(BagFamilyMask.CookingSupp))
                                return false;
                            return true;
                        default:
                            return false;
                    }
                //can remove?
                case ItemClass.Quiver:
                    switch ((ItemSubClassQuiver)pBagProto.GetSubClass())
                    {
                        case ItemSubClassQuiver.Quiver:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.Arrows))
                                return false;
                            return true;
                        case ItemSubClassQuiver.AmmoPouch:
                            if (!Convert.ToBoolean(pProto.GetBagFamily() & BagFamilyMask.Bullets))
                                return false;
                            return true;
                        default:
                            return false;
                    }
            }
            return false;
        }

        public static uint ItemSubClassToDurabilityMultiplierId(ItemClass ItemClass, uint ItemSubClass)
        {
            switch (ItemClass)
            {
                case ItemClass.Weapon: return ItemSubClass;
                case ItemClass.Armor: return ItemSubClass + 21;
            }
            return 0;
        }

        #region Fields
        public bool m_lootGenerated;
        public Loot loot;
        internal BonusData _bonusData;

        ItemUpdateState uState;
        uint m_paidExtendedCost;
        ulong m_paidMoney;
        ObjectGuid m_refundRecipient;
        byte m_slot;
        Bag m_container;
        int uQueuePos;
        string m_text;
        bool mb_in_trade;
        long m_lastPlayedTimeUpdate;
        List<ObjectGuid> allowedGUIDs = new List<ObjectGuid>();
        ItemRandomEnchantmentId m_randomEnchantment;        // store separately to easily find which bonus list is the one randomly given for stat rerolling
        ObjectGuid m_childItem;
        Dictionary<uint, ushort> m_artifactPowerIdToIndex = new Dictionary<uint, ushort>();
        Array<uint> m_gemScalingLevels = new Array<uint>(ItemConst.MaxGemSockets);
        #endregion
    }

    public class ItemPosCount
    {
        public ItemPosCount(ushort _pos, uint _count)
        {
            pos = _pos;
            count = _count;
        }

        public bool isContainedIn(List<ItemPosCount> vec)
        {
            foreach (var posCount in vec)
                if (posCount.pos == pos)
                    return true;
            return false;
        }

        public ushort pos;
        public uint count;
    }

    public enum EnchantmentOffset
    {
        Id = 0,
        Duration = 1,
        Charges = 2,                         // now here not only charges, but something new in wotlk
        Max = 3
    }

    public class ItemSetEffect
    {
        public uint ItemSetID;
        public uint EquippedItemCount;
        public List<ItemSetSpellRecord> SetBonuses = new List<ItemSetSpellRecord>();
    }

    public class BonusData
    {
        public BonusData(ItemTemplate proto)
        {
            if (proto == null)
                return;

            Quality = proto.GetQuality();
            ItemLevelBonus = 0;
            RequiredLevel = proto.GetBaseRequiredLevel();
            for (uint i = 0; i < ItemConst.MaxStats; ++i)
                ItemStatType[i] = proto.GetItemStatType(i);

            for (uint i = 0; i < ItemConst.MaxStats; ++i)
                ItemStatAllocation[i] = proto.GetItemStatAllocation(i);

            for (uint i = 0; i < ItemConst.MaxStats; ++i)
                ItemStatSocketCostMultiplier[i] = proto.GetItemStatSocketCostMultiplier(i);

            for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
            {
                socketColor[i] = proto.GetSocketColor(i);
                GemItemLevelBonus[i] = 0;
                GemRelicType[i] = -1;
                GemRelicRankBonus[i] = 0;
            }

            Bonding = proto.GetBonding();

            AppearanceModID = 0;
            RepairCostMultiplier = 1.0f;
            ScalingStatDistribution = proto.GetScalingStatDistribution();
            RelicType = -1;
            HasItemLevelBonus = false;
            HasFixedLevel = false;
            RequiredLevelOverride = 0;

            _state.AppearanceModPriority = int.MaxValue;
            _state.ScalingStatDistributionPriority = int.MaxValue;
            _state.HasQualityBonus = false;
        }

        public BonusData(ItemInstance itemInstance) : this(Global.ObjectMgr.GetItemTemplate(itemInstance.ItemID))
        {
            if (itemInstance.ItemBonus.HasValue)
            {
                foreach (uint bonusListID in itemInstance.ItemBonus.Value.BonusListIDs)
                {
                    var bonuses = Global.DB2Mgr.GetItemBonusList(bonusListID);
                    if (bonuses != null)
                    {
                        foreach (ItemBonusRecord bonus in bonuses)
                            AddBonus(bonus.Type, bonus.Value);
                    }
                }
            }
        }

        public void AddBonus(ItemBonusType type, int[] values)
        {
            switch (type)
            {
                case ItemBonusType.ItemLevel:
                    ItemLevelBonus += values[0];
                    HasItemLevelBonus = true;
                    break;
                case ItemBonusType.Stat:
                    {
                        uint statIndex = 0;
                        for (statIndex = 0; statIndex < ItemConst.MaxStats; ++statIndex)
                            if (ItemStatType[statIndex] == values[0] || ItemStatType[statIndex] == -1)
                                break;

                        if (statIndex < ItemConst.MaxStats)
                        {
                            ItemStatType[statIndex] = values[0];
                            ItemStatAllocation[statIndex] += values[1];
                        }
                        break;
                    }
                case ItemBonusType.Quality:
                    if (!_state.HasQualityBonus)
                    {
                        Quality = (ItemQuality)values[0];
                        _state.HasQualityBonus = true;
                    }
                    else if ((uint)Quality < values[0])
                        Quality = (ItemQuality)values[0];
                    break;
                case ItemBonusType.Socket:
                    {
                        uint socketCount = (uint)values[0];
                        for (uint i = 0; i < ItemConst.MaxGemSockets && socketCount != 0; ++i)
                        {
                            if (socketColor[i] == 0)
                            {
                                socketColor[i] = (SocketColor)values[1];
                                --socketCount;
                            }
                        }
                        break;
                    }
                case ItemBonusType.Appearance:
                    if (values[1] < _state.AppearanceModPriority)
                    {
                        AppearanceModID = Convert.ToUInt32(values[0]);
                        _state.AppearanceModPriority = values[1];
                    }
                    break;
                case ItemBonusType.RequiredLevel:
                    RequiredLevel += values[0];
                    break;
                case ItemBonusType.RepairCostMuliplier:
                    RepairCostMultiplier *= Convert.ToSingle(values[0]) * 0.01f;
                    break;
                case ItemBonusType.ScalingStatDistribution:
                case ItemBonusType.ScalingStatDistributionFixed:
                    if (values[1] < _state.ScalingStatDistributionPriority)
                    {
                        ScalingStatDistribution = (uint)values[0];
                        ContentTuningId = (uint)values[2];
                        _state.ScalingStatDistributionPriority = values[1];
                        HasFixedLevel = type == ItemBonusType.ScalingStatDistributionFixed;
                    }
                    break;
                case ItemBonusType.Bounding:
                    Bonding = (ItemBondingType)values[0];
                    break;
                case ItemBonusType.RelicType:
                    RelicType = values[0];
                    break;
                case ItemBonusType.OverrideRequiredLevel:
                    RequiredLevelOverride = values[0];
                    break;
            }
        }

        public ItemQuality Quality;
        public int ItemLevelBonus;
        public int RequiredLevel;
        public int[] ItemStatType = new int[ItemConst.MaxStats];
        public int[] ItemStatAllocation = new int[ItemConst.MaxStats];
        public float[] ItemStatSocketCostMultiplier = new float[ItemConst.MaxStats];
        public SocketColor[] socketColor = new SocketColor[ItemConst.MaxGemSockets];
        public ItemBondingType Bonding;
        public uint AppearanceModID;
        public float RepairCostMultiplier;
        public uint ScalingStatDistribution;
        public uint ContentTuningId;
        public uint DisenchantLootId;
        public uint[] GemItemLevelBonus = new uint[ItemConst.MaxGemSockets];
        public int[] GemRelicType = new int[ItemConst.MaxGemSockets];
        public ushort[] GemRelicRankBonus = new ushort[ItemConst.MaxGemSockets];
        public int RelicType;
        public int RequiredLevelOverride;
        public bool HasItemLevelBonus;
        public bool HasFixedLevel;
        State _state;

        struct State
        {
            public int AppearanceModPriority;
            public int ScalingStatDistributionPriority;
            public bool HasQualityBonus;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ItemDynamicFieldArtifactPowers
    {
        public uint ArtifactPowerId;
        public byte PurchasedRank;
        public byte CurrentRankWithBonus;
        public ushort Padding;
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ItemDynamicFieldGems
    {
        public uint ItemId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ushort[] BonusListIDs = new ushort[16];
        public byte Context;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public byte[] Padding = new byte[3];
    }
}
