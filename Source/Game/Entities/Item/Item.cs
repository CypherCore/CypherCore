// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Collections;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Loots;
using Game.Networking;
using Game.Networking.Packets;
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
            ObjectTypeMask |= TypeMask.Item;
            ObjectTypeId = TypeId.Item;

            m_itemData = new ItemData();

            uState = ItemUpdateState.New;
            uQueuePos = -1;
        }

        public virtual bool Create(ulong guidlow, uint itemId, ItemContext context, Player owner)
        {
            _Create(ObjectGuid.Create(HighGuid.Item, guidlow));

            SetEntry(itemId);
            SetObjectScale(1.0f);

            if (owner != null)
            {
                SetOwnerGUID(owner.GetGUID());
                SetContainedIn(owner.GetGUID());
            }

            ItemTemplate itemProto = Global.ObjectMgr.GetItemTemplate(itemId);
            if (itemProto == null)
                return false;

            _bonusData = new BonusData(itemProto);
            SetCount(1);
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.MaxDurability), itemProto.MaxDurability);
            SetDurability(itemProto.MaxDurability);

            for (int i = 0; i < itemProto.Effects.Count; ++i)
                if (itemProto.Effects[i].LegacySlotIndex < 5)
                    SetSpellCharges(itemProto.Effects[i].LegacySlotIndex, itemProto.Effects[i].Charges);

            SetExpiration(itemProto.GetDuration());
            SetCreatePlayedTime(0);
            SetCreateTime(GameTime.GetGameTime());
            SetContext(context);

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

                        if (owner == null || !ConditionManager.IsPlayerMeetingCondition(owner, artifactAppearance.UnlockPlayerConditionID))
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

        public override string GetName(Locale locale = Locale.enUS)
        {
            ItemTemplate itemTemplate = GetTemplate();
            var suffix = CliDB.ItemNameDescriptionStorage.LookupByKey(_bonusData.Suffix);
            if (suffix != null)
                return $"{itemTemplate.GetName(locale)} {suffix.Description[locale]}";

            return itemTemplate.GetName(locale);
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
            uint duration = m_itemData.Expiration;
            if (duration == 0)
                return;

            Log.outDebug(LogFilter.Player, "Item.UpdateDuration Item (Entry: {0} Duration {1} Diff {2})", GetEntry(), duration, diff);

            if (duration <= diff)
            {
                Global.ScriptMgr.OnItemExpire(owner, GetTemplate());
                owner.DestroyItem(GetBagSlot(), GetSlot(), true);
                return;
            }

            SetExpiration(duration - diff);
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
                    stmt = CharacterDatabase.GetPreparedStatement(uState == ItemUpdateState.New ? CharStatements.REP_ITEM_INSTANCE : CharStatements.UPD_ITEM_INSTANCE);
                    stmt.AddValue(index, GetEntry());
                    stmt.AddValue(++index, GetOwnerGUID().GetCounter());
                    stmt.AddValue(++index, GetCreator().GetCounter());
                    stmt.AddValue(++index, GetGiftCreator().GetCounter());
                    stmt.AddValue(++index, GetCount());
                    stmt.AddValue(++index, (uint)m_itemData.Expiration);

                    StringBuilder ss = new();
                    for (byte i = 0; i < m_itemData.SpellCharges.GetSize() && i < _bonusData.EffectCount; ++i)
                        ss.AppendFormat("{0} ", GetSpellCharges(i));

                    stmt.AddValue(++index, ss.ToString());
                    stmt.AddValue(++index, (uint)m_itemData.DynamicFlags);

                    ss.Clear();
                    for (EnchantmentSlot slot = 0; slot < EnchantmentSlot.Max; ++slot)
                    {
                        var enchantment = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetEnchantmentId(slot));
                        if (enchantment != null && !enchantment.HasFlag(SpellItemEnchantmentFlags.DoNotSaveToDB))
                            ss.Append($"{GetEnchantmentId(slot)} {GetEnchantmentDuration(slot)} {GetEnchantmentCharges(slot)} ");
                        else
                            ss.Append("0 0 0 ");
                    }

                    stmt.AddValue(++index, ss.ToString());
                    stmt.AddValue(++index, m_randomBonusListId);
                    stmt.AddValue(++index, (uint)m_itemData.Durability);
                    stmt.AddValue(++index, (uint)m_itemData.CreatePlayedTime);
                    stmt.AddValue(++index, m_itemData.CreateTime);
                    stmt.AddValue(++index, m_text);
                    stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetSpeciesId));
                    stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetBreedData));
                    stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetLevel));
                    stmt.AddValue(++index, GetModifier(ItemModifier.BattlePetDisplayId));
                    stmt.AddValue(++index, (byte)m_itemData.Context);

                    ss.Clear();

                    foreach (int bonusListID in GetBonusListIDs())
                        ss.Append($"{bonusListID} ");

                    stmt.AddValue(++index, ss.ToString());
                    stmt.AddValue(++index, GetGUID().GetCounter());

                    DB.Characters.Execute(stmt);

                    if ((uState == ItemUpdateState.Changed) && IsWrapped())
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_GIFT_OWNER);
                        stmt.AddValue(0, GetOwnerGUID().GetCounter());
                        stmt.AddValue(1, GetGUID().GetCounter());
                        DB.Characters.Execute(stmt);
                    }

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (m_itemData.Gems.Size() != 0)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_GEMS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        int i = 0;
                        int gemFields = 4;

                        foreach (SocketedGem gemData in m_itemData.Gems)
                        {
                            if (gemData.ItemId != 0)
                            {
                                stmt.AddValue(1 + i * gemFields, (uint)gemData.ItemId);
                                StringBuilder gemBonusListIDs = new();
                                foreach (ushort bonusListID in gemData.BonusListIDs)
                                {
                                    if (bonusListID != 0)
                                        gemBonusListIDs.AppendFormat("{0} ", bonusListID);
                                }

                                stmt.AddValue(2 + i * gemFields, gemBonusListIDs.ToString());
                                stmt.AddValue(3 + i * gemFields, (byte)gemData.Context);
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
                        ItemModifier.TransmogAppearanceSpec5,

                        ItemModifier.EnchantIllusionAllSpecs,
                        ItemModifier.EnchantIllusionSpec1,
                        ItemModifier.EnchantIllusionSpec2,
                        ItemModifier.EnchantIllusionSpec3,
                        ItemModifier.EnchantIllusionSpec4,
                        ItemModifier.EnchantIllusionSpec5,

                        ItemModifier.TransmogSecondaryAppearanceAllSpecs,
                        ItemModifier.TransmogSecondaryAppearanceSpec1,
                        ItemModifier.TransmogSecondaryAppearanceSpec2,
                        ItemModifier.TransmogSecondaryAppearanceSpec3,
                        ItemModifier.TransmogSecondaryAppearanceSpec4,
                        ItemModifier.TransmogSecondaryAppearanceSpec5
                    };

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (transmogMods.Any(modifier => GetModifier(modifier) != 0))
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_TRANSMOG);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, GetModifier(ItemModifier.TransmogAppearanceAllSpecs));
                        stmt.AddValue(2, GetModifier(ItemModifier.TransmogAppearanceSpec1));
                        stmt.AddValue(3, GetModifier(ItemModifier.TransmogAppearanceSpec2));
                        stmt.AddValue(4, GetModifier(ItemModifier.TransmogAppearanceSpec3));
                        stmt.AddValue(5, GetModifier(ItemModifier.TransmogAppearanceSpec4));
                        stmt.AddValue(6, GetModifier(ItemModifier.TransmogAppearanceSpec5));
                        stmt.AddValue(7, GetModifier(ItemModifier.EnchantIllusionAllSpecs));
                        stmt.AddValue(8, GetModifier(ItemModifier.EnchantIllusionSpec1));
                        stmt.AddValue(9, GetModifier(ItemModifier.EnchantIllusionSpec2));
                        stmt.AddValue(10, GetModifier(ItemModifier.EnchantIllusionSpec3));
                        stmt.AddValue(11, GetModifier(ItemModifier.EnchantIllusionSpec4));
                        stmt.AddValue(12, GetModifier(ItemModifier.EnchantIllusionSpec5));
                        stmt.AddValue(13, GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs));
                        stmt.AddValue(14, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1));
                        stmt.AddValue(15, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2));
                        stmt.AddValue(16, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3));
                        stmt.AddValue(17, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4));
                        stmt.AddValue(18, GetModifier(ItemModifier.TransmogSecondaryAppearanceSpec5));
                        trans.Append(stmt);
                    }

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (GetTemplate().GetArtifactID() != 0)
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, (ulong)m_itemData.ArtifactXP);
                        stmt.AddValue(2, GetModifier(ItemModifier.ArtifactAppearanceId));
                        stmt.AddValue(3, GetModifier(ItemModifier.ArtifactTier));
                        trans.Append(stmt);

                        foreach (ArtifactPower artifactPower in m_itemData.ArtifactPowers)
                        {
                            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_ARTIFACT_POWERS);
                            stmt.AddValue(0, GetGUID().GetCounter());
                            stmt.AddValue(1, artifactPower.ArtifactPowerId);
                            stmt.AddValue(2, artifactPower.PurchasedRank);
                            trans.Append(stmt);
                        }
                    }

                    ItemModifier[] modifiersTable =
                    {
                            ItemModifier.TimewalkerLevel,
                            ItemModifier.ArtifactKnowledgeLevel
                        };

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (modifiersTable.Any(modifier => GetModifier(modifier) != 0))
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_INSTANCE_MODIFIERS);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        stmt.AddValue(1, GetModifier(ItemModifier.TimewalkerLevel));
                        stmt.AddValue(2, GetModifier(ItemModifier.ArtifactKnowledgeLevel));
                        trans.Append(stmt);
                    }
                    break;
                }
                case ItemUpdateState.Removed:
                {
                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
                    stmt.AddValue(0, GetGUID().GetCounter());
                    trans.Append(stmt);

                    if (IsWrapped())
                    {
                        stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
                        stmt.AddValue(0, GetGUID().GetCounter());
                        trans.Append(stmt);
                    }

                    // Delete the items if this is a container
                    if (loot != null && !loot.IsLooted())
                        Global.LootItemStorage.RemoveStoredLootForContainer(GetGUID().GetCounter());

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
                    SetCreator(ObjectGuid.Create(HighGuid.Player, creator));
                else
                    SetCreator(ObjectGuid.Create(HighGuid.Item, creator));
            }

            ulong giftCreator = fields.Read<ulong>(3);
            if (giftCreator != 0)
                SetGiftCreator(ObjectGuid.Create(HighGuid.Player, giftCreator));

            SetCount(fields.Read<uint>(4));

            uint duration = fields.Read<uint>(5);
            SetExpiration(duration);
            // update duration if need, and remove if not need
            if (proto.GetDuration() != duration)
            {
                SetExpiration(proto.GetDuration());
                need_save = true;
            }

            ReplaceAllItemFlags((ItemFieldFlags)itemFlags);

            uint durability = fields.Read<uint>(10);
            SetDurability(durability);
            // update max durability (and durability) if need
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.MaxDurability), proto.MaxDurability);

            // do not overwrite durability for wrapped items
            if (durability > proto.MaxDurability && !IsWrapped())
            {
                SetDurability(proto.MaxDurability);
                need_save = true;
            }

            SetCreatePlayedTime(fields.Read<uint>(11));
            SetCreateTime(fields.Read<long>(12));
            SetText(fields.Read<string>(13));

            SetModifier(ItemModifier.BattlePetSpeciesId, fields.Read<uint>(14));
            SetModifier(ItemModifier.BattlePetBreedData, fields.Read<uint>(15));
            SetModifier(ItemModifier.BattlePetLevel, fields.Read<ushort>(16));
            SetModifier(ItemModifier.BattlePetDisplayId, fields.Read<uint>(17));

            SetContext((ItemContext)fields.Read<byte>(18));

            var bonusListString = new StringArray(fields.Read<string>(19), ' ');
            List<uint> bonusListIDs = new();
            for (var i = 0; i < bonusListString.Length; ++i)
            {
                if (uint.TryParse(bonusListString[i], out uint bonusListID))
                    bonusListIDs.Add(bonusListID);
            }
            SetBonuses(bonusListIDs);

            // load charges after bonuses, they can add more item effects
            var tokens = new StringArray(fields.Read<string>(6), ' ');
            for (byte i = 0; i < m_itemData.SpellCharges.GetSize() && i < _bonusData.EffectCount && i < tokens.Length; ++i)
            {
                if (int.TryParse(tokens[i], out int value))
                    SetSpellCharges(i, value);
            }

            SetModifier(ItemModifier.TransmogAppearanceAllSpecs, fields.Read<uint>(20));
            SetModifier(ItemModifier.TransmogAppearanceSpec1, fields.Read<uint>(21));
            SetModifier(ItemModifier.TransmogAppearanceSpec2, fields.Read<uint>(22));
            SetModifier(ItemModifier.TransmogAppearanceSpec3, fields.Read<uint>(23));
            SetModifier(ItemModifier.TransmogAppearanceSpec4, fields.Read<uint>(24));
            SetModifier(ItemModifier.TransmogAppearanceSpec5, fields.Read<uint>(25));

            SetModifier(ItemModifier.EnchantIllusionAllSpecs, fields.Read<uint>(26));
            SetModifier(ItemModifier.EnchantIllusionSpec1, fields.Read<uint>(27));
            SetModifier(ItemModifier.EnchantIllusionSpec2, fields.Read<uint>(28));
            SetModifier(ItemModifier.EnchantIllusionSpec3, fields.Read<uint>(29));
            SetModifier(ItemModifier.EnchantIllusionSpec4, fields.Read<uint>(30));
            SetModifier(ItemModifier.EnchantIllusionSpec4, fields.Read<uint>(31));

            SetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs, fields.Read<uint>(32));
            SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec1, fields.Read<uint>(33));
            SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec2, fields.Read<uint>(34));
            SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec3, fields.Read<uint>(35));
            SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec4, fields.Read<uint>(36));
            SetModifier(ItemModifier.TransmogSecondaryAppearanceSpec5, fields.Read<uint>(37));

            int gemFields = 4;
            ItemDynamicFieldGems[] gemData = new ItemDynamicFieldGems[ItemConst.MaxGemSockets];
            for (int i = 0; i < ItemConst.MaxGemSockets; ++i)
            {
                gemData[i] = new ItemDynamicFieldGems();
                gemData[i].ItemId = fields.Read<uint>(38 + i * gemFields);
                var gemBonusListIDs = new StringArray(fields.Read<string>(39 + i * gemFields), ' ');
                if (!gemBonusListIDs.IsEmpty())
                {
                    uint b = 0;
                    foreach (string token in gemBonusListIDs)
                    {
                        if (uint.TryParse(token, out uint bonusListID) && bonusListID != 0)
                            gemData[i].BonusListIDs[b++] = (ushort)bonusListID;
                    }
                }

                gemData[i].Context = fields.Read<byte>(40 + i * gemFields);
                if (gemData[i].ItemId != 0)
                    SetGem((ushort)i, gemData[i], fields.Read<uint>(41 + i * gemFields));
            }

            SetModifier(ItemModifier.TimewalkerLevel, fields.Read<uint>(50));
            SetModifier(ItemModifier.ArtifactKnowledgeLevel, fields.Read<uint>(51));

            // Enchants must be loaded after all other bonus/scaling data
            var enchantmentTokens = new StringArray(fields.Read<string>(8), ' ');
            if (enchantmentTokens.Length == (int)EnchantmentSlot.Max * 3)
            {
                for (int i = 0; i < (int)EnchantmentSlot.Max; ++i)
                {
                    ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, i);
                    SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), uint.Parse(enchantmentTokens[i * 3 + 0]));
                    SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), uint.Parse(enchantmentTokens[i * 3 + 1]));
                    SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), short.Parse(enchantmentTokens[i * 3 + 2]));
                }
            }

            m_randomBonusListId = fields.Read<uint>(9);

            // Remove bind flag for items vs NO_BIND set
            if (IsSoulBound() && GetBonding() == ItemBondingType.None)
            {
                RemoveItemFlag(ItemFieldFlags.Soulbound);
                need_save = true;
            }

            if (need_save)                                           // normal item changed state set not work at loading
            {
                byte index = 0;
                PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.UPD_ITEM_INSTANCE_ON_LOAD);
                stmt.AddValue(index++, (uint)m_itemData.Expiration);
                stmt.AddValue(index++, (uint)m_itemData.DynamicFlags);
                stmt.AddValue(index++, (uint)m_itemData.Durability);
                stmt.AddValue(index++, guid);
                DB.Characters.Execute(stmt);
            }
            return true;
        }

        public void LoadArtifactData(Player owner, ulong xp, uint artifactAppearanceId, uint artifactTier, List<ArtifactPowerData> powers)
        {
            for (byte i = 0; i <= artifactTier; ++i)
                InitArtifactPowers(GetTemplate().GetArtifactID(), i);

            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactXP), xp);
            SetModifier(ItemModifier.ArtifactAppearanceId, artifactAppearanceId);
            SetModifier(ItemModifier.ArtifactTier, artifactTier);

            ArtifactAppearanceRecord artifactAppearance = CliDB.ArtifactAppearanceStorage.LookupByKey(artifactAppearanceId);
            if (artifactAppearance != null)
                SetAppearanceModId(artifactAppearance.ItemAppearanceModifierID);

            byte totalPurchasedRanks = 0;
            foreach (ArtifactPowerData power in powers)
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
                                        if (artifactPowerPicker != null && owner != null && ConditionManager.IsPlayerMeetingCondition(owner, artifactPowerPicker.PlayerConditionID))
                                            if (artifactPower.Label == _bonusData.GemRelicType[e - EnchantmentSlot.Sock1])
                                                power.CurrentRankWithBonus += (byte)enchant.EffectPointsMin[i];
                                    }
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                SetArtifactPower((ushort)power.ArtifactPowerId, power.PurchasedRank, power.CurrentRankWithBonus);
            }

            foreach (ArtifactPowerData power in powers)
            {
                ArtifactPowerRecord scaledArtifactPowerEntry = CliDB.ArtifactPowerStorage.LookupByKey(power.ArtifactPowerId);
                if (!scaledArtifactPowerEntry.HasFlag(ArtifactPowerFlag.ScalesWithNumPowers))
                    continue;

                SetArtifactPower((ushort)power.ArtifactPowerId, power.PurchasedRank, (byte)(totalPurchasedRanks + 1));
            }

            CheckArtifactRelicSlotUnlock(owner);
        }

        public void CheckArtifactRelicSlotUnlock(Player owner)
        {
            if (owner == null)
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
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_GEMS);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_TRANSMOG);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_ARTIFACT_POWERS);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_INSTANCE_MODIFIERS);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);

            stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_GIFT);
            stmt.AddValue(0, itemGuid);
            DB.Characters.ExecuteOrAppend(trans, stmt);
        }

        public virtual void DeleteFromDB(SQLTransaction trans)
        {
            DeleteFromDB(trans, GetGUID().GetCounter());

            // Delete the items if this is a container
            if (loot != null && !loot.IsLooted())
                Global.LootItemStorage.RemoveStoredLootForContainer(GetGUID().GetCounter());
        }

        public static void DeleteFromInventoryDB(SQLTransaction trans, ulong itemGuid)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_CHAR_INVENTORY_BY_ITEM);
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

        public override Player GetOwner()
        {
            return Global.ObjAccessor.FindPlayer(GetOwnerGUID());
        }

        public SkillType GetSkill()
        {
            ItemTemplate proto = GetTemplate();
            return proto.GetSkill();
        }

        public void SetItemRandomBonusList(uint bonusListId)
        {
            if (bonusListId == 0)
                return;

            AddBonuses(bonusListId);
        }

        public void SetState(ItemUpdateState state, Player forplayer = null)
        {
            if (uState == ItemUpdateState.New && state == ItemUpdateState.Removed)
            {
                // pretend the item never existed
                if (forplayer != null)
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

                if (forplayer != null)
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

        public bool IsEquipped()
        {
            return !IsInBag() && (m_slot < EquipmentSlot.End
                || (m_slot >= ProfessionSlots.Start && m_slot < ProfessionSlots.End));
        }

        public bool CanBeTraded(bool mail = false, bool trade = false)
        {
            if (m_lootGenerated)
                return false;

            if ((!mail || !IsBoundAccountWide()) && (IsSoulBound() && (!IsBOPTradeable() || !trade)))
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
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.StackCount), value);

            Player player = GetOwner();
            if (player != null)
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

        public ulong CalculateDurabilityRepairCost(float discount)
        {
            uint maxDurability = m_itemData.MaxDurability;
            if (maxDurability == 0)
                return 0;

            uint curDurability = m_itemData.Durability;
            Cypher.Assert(maxDurability >= curDurability);

            uint lostDurability = maxDurability - curDurability;
            if (lostDurability == 0)
                return 0;

            ItemTemplate itemTemplate = GetTemplate();

            var durabilityCost = CliDB.DurabilityCostsStorage.LookupByKey(GetItemLevel(GetOwner()));
            if (durabilityCost == null)
                return 0;

            uint durabilityQualityEntryId = ((uint)GetQuality() + 1) * 2;
            var durabilityQualityEntry = CliDB.DurabilityQualityStorage.LookupByKey(durabilityQualityEntryId);
            if (durabilityQualityEntry == null)
                return 0;

            uint dmultiplier = 0;
            if (itemTemplate.GetClass() == ItemClass.Weapon)
                dmultiplier = durabilityCost.WeaponSubClassCost[itemTemplate.GetSubClass()];
            else if (itemTemplate.GetClass() == ItemClass.Armor)
                dmultiplier = durabilityCost.ArmorSubClassCost[itemTemplate.GetSubClass()];

            ulong cost = (ulong)Math.Round(lostDurability * dmultiplier * durabilityQualityEntry.Data * GetRepairCostMultiplier());
            cost = (ulong)(cost * discount * WorldConfig.GetFloatValue(WorldCfg.RateRepaircost));

            if (cost == 0) // Fix for ITEM_QUALITY_ARTIFACT
                cost = 1;

            return cost;
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
                        if (enchantEntry.HasFlag(SpellItemEnchantmentFlags.Soulbound))
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
                if (isEnchantSpell && proto.HasFlag(ItemFlags3.CanStoreEnchants))
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

        public void SetEnchantment(EnchantmentSlot slot, uint id, uint duration, uint charges, ObjectGuid caster = default)
        {
            // Better lost small time at check in comparison lost time at item save to DB.
            if ((GetEnchantmentId(slot) == id) && (GetEnchantmentDuration(slot) == duration) && (GetEnchantmentCharges(slot) == charges))
                return;

            Player owner = GetOwner();
            if (slot < EnchantmentSlot.MaxInspected)
            {
                var oldEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetEnchantmentId(slot));
                if (oldEnchant != null && !oldEnchant.HasFlag(SpellItemEnchantmentFlags.DoNotLog))
                    owner.GetSession().SendEnchantmentLog(GetOwnerGUID(), ObjectGuid.Empty, GetGUID(), GetEntry(), oldEnchant.Id, (uint)slot);

                var newEnchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(id);
                if (newEnchant != null && !newEnchant.HasFlag(SpellItemEnchantmentFlags.DoNotLog))
                    owner.GetSession().SendEnchantmentLog(GetOwnerGUID(), caster, GetGUID(), GetEntry(), id, (uint)slot);
            }

            ApplyArtifactPowerEnchantmentBonuses(slot, GetEnchantmentId(slot), false, owner);
            ApplyArtifactPowerEnchantmentBonuses(slot, id, true, owner);

            ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, (int)slot);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), id);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), duration);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), (short)charges);
            SetState(ItemUpdateState.Changed, owner);
        }

        public void SetEnchantmentDuration(EnchantmentSlot slot, uint duration, Player owner)
        {
            if (GetEnchantmentDuration(slot) == duration)
                return;

            ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, (int)slot);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), duration);
            SetState(ItemUpdateState.Changed, owner);
            // Cannot use GetOwner() here, has to be passed as an argument to avoid freeze due to hashtable locking
        }

        public void SetEnchantmentCharges(EnchantmentSlot slot, uint charges)
        {
            if (GetEnchantmentCharges(slot) == charges)
                return;

            ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, (int)slot);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), (short)charges);
            SetState(ItemUpdateState.Changed, GetOwner());
        }

        public void ClearEnchantment(EnchantmentSlot slot)
        {
            if (GetEnchantmentId(slot) == 0)
                return;

            ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, (int)slot);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), 0u);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), 0u);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Charges), (short)0);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Inactive), (ushort)0);
            SetState(ItemUpdateState.Changed, GetOwner());
        }

        public SocketedGem GetGem(ushort slot)
        {
            //ASSERT(slot < MAX_GEM_SOCKETS);
            return slot < m_itemData.Gems.Size() ? m_itemData.Gems[slot] : null;
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
                        BonusData gemBonus = new(gemTemplate);
                        foreach (var bonusListId in gem.BonusListIDs)
                            gemBonus.AddBonusList(bonusListId);

                        uint gemBaseItemLevel = gemTemplate.GetBaseItemLevel();
                        if (gemBonus.PlayerLevelToItemLevelCurveId != 0)
                        {
                            uint scaledIlvl = (uint)Global.DB2Mgr.GetCurveValueAt(gemBonus.PlayerLevelToItemLevelCurveId, gemScalingLevel);
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
                                    foreach (var itemBonus in ItemBonusMgr.GetItemBonuses(gemEnchant.EffectArg[i]))
                                        if (itemBonus.BonusType == ItemBonusType.ItemLevel)
                                            _bonusData.GemItemLevelBonus[slot] += (uint)itemBonus.Value[0];
                                    break;
                                }
                                case ItemEnchantmentType.BonusListCurve:
                                {
                                    uint bonusListId = ItemBonusMgr.GetItemBonusListForItemLevelDelta((short)Global.DB2Mgr.GetCurveValueAt((uint)Curves.ArtifactRelicItemLevelBonus, gemBaseItemLevel + gemBonus.ItemLevelBonus));
                                    if (bonusListId != 0)
                                    {
                                        foreach (var itemBonus in ItemBonusMgr.GetItemBonuses(bonusListId))
                                            if (itemBonus.BonusType == ItemBonusType.ItemLevel)
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

            SocketedGem gemField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Gems, slot);
            SetUpdateFieldValue(gemField.ModifyValue(gemField.ItemId), gem.ItemId);
            SetUpdateFieldValue(gemField.ModifyValue(gemField.Context), gem.Context);
            for (int i = 0; i < 16; ++i)
                SetUpdateFieldValue(ref gemField.ModifyValue(gemField.BonusListIDs, i), gem.BonusListIDs[i]);
        }

        public bool GemsFitSockets()
        {
            for (uint gemSlot = 0; gemSlot < ItemConst.MaxGemSockets; ++gemSlot)
            {
                SocketColor socketColor = GetTemplate().GetSocketColor(gemSlot);
                if (socketColor == 0) // no socket slot
                    continue;

                if (gemSlot >= m_itemData.Gems.Size()) // no gems on this socket
                    return false;

                SocketColor GemColor = 0;

                uint gemid = m_itemData.Gems[(int)gemSlot].ItemId;
                if (gemid != 0)
                {
                    ItemTemplate gemProto = Global.ObjectMgr.GetItemTemplate(gemid);
                    if (gemProto != null)
                    {
                        GemPropertiesRecord gemProperty = CliDB.GemPropertiesStorage.LookupByKey(gemProto.GetGemProperties());
                        if (gemProperty != null)
                            GemColor = gemProperty.Type;
                    }
                }

                if (!GemColor.HasAnyFlag(ItemConst.SocketColorToGemTypeMask[(int)socketColor])) // bad gem color on this socket
                    return false;
            }
            return true;
        }

        public byte GetGemCountWithID(uint GemID)
        {
            var list = (List<SocketedGem>)m_itemData.Gems;
            return (byte)list.Count(gemData => gemData.ItemId == GemID);
        }

        public byte GetGemCountWithLimitCategory(uint limitCategory)
        {
            var list = (List<SocketedGem>)m_itemData.Gems;
            return (byte)list.Count(gemData =>
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
            return proto != null && ((proto.GetMap() != 0 && proto.GetMap() != cur_mapId) ||
                ((proto.GetArea(0) != 0 && proto.GetArea(0) != cur_zoneId) && (proto.GetArea(1) != 0 && proto.GetArea(1) != cur_zoneId)));
        }

        public void SendUpdateSockets()
        {
            SocketGemsSuccess socketGems = new();
            socketGems.Item = GetGUID();

            GetOwner().SendPacket(socketGems);
        }

        public void SendTimeUpdate(Player owner)
        {
            uint duration = m_itemData.Expiration;
            if (duration == 0)
                return;

            ItemTimeUpdate itemTimeUpdate = new();
            itemTimeUpdate.ItemGuid = GetGUID();
            itemTimeUpdate.DurationLeft = duration;
            owner.SendPacket(itemTimeUpdate);
        }

        public static Item CreateItem(uint itemEntry, uint count, ItemContext context, Player player = null, bool addDefaultBonuses = true)
        {
            if (count < 1)
                return null;                                        //don't create item at zero count

            var pProto = Global.ObjectMgr.GetItemTemplate(itemEntry);
            if (pProto != null)
            {
                if (count > pProto.GetMaxStackSize())
                    count = pProto.GetMaxStackSize();

                Item item = Bag.NewItemOrBag(pProto);
                if (item.Create(Global.ObjectMgr.GetGenerator(HighGuid.Item).Generate(), itemEntry, context, player))
                {
                    item.SetCount(count);
                    if (addDefaultBonuses)
                        item.SetBonuses(ItemBonusMgr.GetBonusListsForItem(itemEntry, new(context)));

                    return item;
                }
            }

            return null;
        }

        public Item CloneItem(uint count, Player player = null)
        {
            Item newItem = CreateItem(GetEntry(), count, GetContext(), player, false);
            if (newItem == null)
                return null;

            newItem.SetCreator(GetCreator());
            newItem.SetGiftCreator(GetGiftCreator());
            newItem.ReplaceAllItemFlags((ItemFieldFlags)(m_itemData.DynamicFlags & ~(uint)(ItemFieldFlags.Refundable | ItemFieldFlags.BopTradeable)));
            newItem.SetExpiration(m_itemData.Expiration);
            newItem.SetBonuses(m_itemData.ItemBonusKey.GetValue().BonusListIDs);
            newItem.SetFixedLevel(GetModifier(ItemModifier.TimewalkerLevel));
            // player CAN be NULL in which case we must not update random properties because that accesses player's item update queue
            if (player != null)
                newItem.SetItemRandomBonusList(m_randomBonusListId);
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

            if (IsBOPTradeable())
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

        public override UpdateFieldFlag GetUpdateFieldFlagsFor(Player target)
        {
            if (target.GetGUID() == GetOwnerGUID())
                return UpdateFieldFlag.Owner;

            return UpdateFieldFlag.None;
        }

        public override void BuildValuesCreate(WorldPacket data, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            WorldPacket buffer = new();

            m_objectData.WriteCreate(buffer, flags, this, target);
            m_itemData.WriteCreate(buffer, flags, this, target);

            data.WriteUInt32(buffer.GetSize() + 1);
            data.WriteUInt8((byte)flags);
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


            data.WriteUInt32(buffer.GetSize());
            data.WriteUInt32(m_values.GetChangedObjectTypeMask());
            data.WriteBytes(buffer);
        }

        public override void BuildValuesUpdateWithFlag(WorldPacket data, UpdateFieldFlag flags, Player target)
        {
            UpdateMask valuesMask = new((int)TypeId.Max);
            valuesMask.Set((int)TypeId.Item);

            WorldPacket buffer = new();
            UpdateMask mask = m_itemData.GetStaticUpdateMask();

            buffer.WriteUInt32(valuesMask.GetBlock(0));
            m_itemData.AppendAllowedFieldsMaskForFlag(mask, flags);
            m_itemData.WriteUpdate(buffer, mask, true, this, target);

            data.WriteUInt32(buffer.GetSize());
            data.WriteBytes(buffer);
        }

        void BuildValuesUpdateForPlayerWithMask(UpdateData data, UpdateMask requestedObjectMask, UpdateMask requestedItemMask, Player target)
        {
            UpdateFieldFlag flags = GetUpdateFieldFlagsFor(target);
            UpdateMask valuesMask = new((int)TypeId.Max);
            if (requestedObjectMask.IsAnySet())
                valuesMask.Set((int)TypeId.Object);

            m_itemData.FilterDisallowedFieldsMaskForFlag(requestedItemMask, flags);
            if (requestedItemMask.IsAnySet())
                valuesMask.Set((int)TypeId.Item);

            WorldPacket buffer = new();
            buffer.WriteUInt32(valuesMask.GetBlock(0));

            if (valuesMask[(int)TypeId.Object])
                m_objectData.WriteUpdate(buffer, requestedObjectMask, true, this, target);

            if (valuesMask[(int)TypeId.Item])
                m_itemData.WriteUpdate(buffer, requestedItemMask, true, this, target);

            WorldPacket buffer1 = new();
            buffer1.WriteUInt8((byte)UpdateType.Values);
            buffer1.WritePackedGuid(GetGUID());
            buffer1.WriteUInt32(buffer.GetSize());
            buffer1.WriteBytes(buffer.GetData());

            data.AddUpdateBlock(buffer1);
        }

        public override void ClearUpdateMask(bool remove)
        {
            m_values.ClearChangesMask(m_itemData);
            base.ClearUpdateMask(remove);
        }

        public override bool AddToObjectUpdate()
        {
            Player owner = GetOwner();
            if (owner != null)
            {
                owner.GetMap().AddUpdateObject(this);
                return true;
            }

            return false;
        }

        public override void RemoveFromObjectUpdate()
        {
            Player owner = GetOwner();
            if (owner != null)
                owner.GetMap().RemoveUpdateObject(this);
        }

        public void SaveRefundDataToDB()
        {
            DeleteRefundDataFromDB();

            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.INS_ITEM_REFUND_INSTANCE);
            stmt.AddValue(0, GetGUID().GetCounter());
            stmt.AddValue(1, GetRefundRecipient().GetCounter());
            stmt.AddValue(2, GetPaidMoney());
            stmt.AddValue(3, (ushort)GetPaidExtendedCost());
            DB.Characters.Execute(stmt);
        }

        public void DeleteRefundDataFromDB(SQLTransaction trans = null)
        {
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_REFUND_INSTANCE);
            stmt.AddValue(0, GetGUID().GetCounter());
            if (trans != null)
                trans.Append(stmt);
            else
                DB.Characters.Execute(stmt);
        }

        public void SetNotRefundable(Player owner, bool changestate = true, SQLTransaction trans = null, bool addToCollection = true)
        {
            if (!IsRefundable())
                return;

            ItemExpirePurchaseRefund itemExpirePurchaseRefund = new();
            itemExpirePurchaseRefund.ItemGUID = GetGUID();
            owner.SendPacket(itemExpirePurchaseRefund);

            RemoveItemFlag(ItemFieldFlags.Refundable);
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

        public uint GetPlayedTime()
        {
            return m_itemData.CreatePlayedTime;
        }

        public bool IsRefundExpired()
        {
            return m_itemData.CreateTime + 2 * Time.Hour <= GameTime.GetGameTime();
        }

        public void SetSoulboundTradeable(List<ObjectGuid> allowedLooters)
        {
            SetItemFlag(ItemFieldFlags.BopTradeable);
            allowedGUIDs = allowedLooters;
        }

        public void ClearSoulboundTradeable(Player currentOwner)
        {
            RemoveItemFlag(ItemFieldFlags.BopTradeable);
            if (allowedGUIDs.Empty())
                return;

            currentOwner.GetSession().GetCollectionMgr().AddItemAppearance(this);
            allowedGUIDs.Clear();
            SetState(ItemUpdateState.Changed, currentOwner);
            PreparedStatement stmt = CharacterDatabase.GetPreparedStatement(CharStatements.DEL_ITEM_BOP_TRADE);
            stmt.AddValue(0, GetGUID().GetCounter());
            DB.Characters.Execute(stmt);
        }

        public bool CheckSoulboundTradeExpire()
        {
            // called from owner's update - GetOwner() MUST be valid
            if (m_itemData.CreatePlayedTime + 4 * Time.Hour < GetOwner().GetTotalPlayedTime())
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

            if (proto.HasFlag(ItemFlags2.NoAlterItemVisual))
                return false;

            if (!HasStats())
                return false;

            return true;
        }

        bool HasStats()
        {
            ItemTemplate proto = GetTemplate();
            Player owner = GetOwner();
            for (byte i = 0; i < ItemConst.MaxStats; ++i)
            {
                if ((owner != null ? GetItemStatValue(i, owner) : proto.GetStatPercentEditor(i)) != 0)
                    return true;
            }

            return false;
        }

        static bool HasStats(ItemInstance itemInstance, BonusData bonus)
        {
            for (byte i = 0; i < ItemConst.MaxStats; ++i)
            {
                if (bonus.StatPercentEditor[i] != 0)
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
            -1,                                                      // INVTYPE_RELIC
            -1,                                                     // INVTYPE_PROFESSION_TOOL
            -1,                                                     // INVTYPE_PROFESSION_GEAR
            -1,                                                     // INVTYPE_EQUIPABLE_SPELL_OFFENSIVE
            -1,                                                     // INVTYPE_EQUIPABLE_SPELL_UTILITY
            -1,                                                     // INVTYPE_EQUIPABLE_SPELL_DEFENSIVE
            -1                                                      // INVTYPE_EQUIPABLE_SPELL_MOBILITY
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

            if (proto.HasFlag(ItemFlags2.OverrideGoldCost))
                return proto.GetBuyPrice();

            var qualityPrice = CliDB.ImportPriceQualityStorage.LookupByKey(quality + 1);
            if (qualityPrice == null)
                return 0;

            var basePrice = CliDB.ItemPriceBaseStorage.LookupByKey(proto.GetBaseItemLevel());
            if (basePrice == null)
                return 0;

            float qualityFactor = qualityPrice.Data;
            float baseFactor;

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

            standardPrice = true;
            return (uint)(proto.GetPriceVariance() * typeFactor * baseFactor * qualityFactor * proto.GetPriceRandomValue());
        }

        public uint GetSellPrice(Player owner)
        {
            return GetSellPrice(GetTemplate(), (uint)GetQuality(), GetItemLevel(owner));
        }

        public static uint GetSellPrice(ItemTemplate proto, uint quality, uint itemLevel)
        {
            if (proto.HasFlag(ItemFlags2.OverrideGoldCost))
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

        public uint GetItemLevel(Player owner)
        {
            ItemTemplate itemTemplate = GetTemplate();
            uint minItemLevel = owner.m_unitData.MinItemLevel;
            uint minItemLevelCutoff = owner.m_unitData.MinItemLevelCutoff;
            bool pvpBonus = owner.IsUsingPvpItemLevels();
            uint maxItemLevel = pvpBonus && itemTemplate.HasFlag(ItemFlags3.IgnoreItemLevelCapInPvp) ? 0u : owner.m_unitData.MaxItemLevel;

            uint azeriteLevel = 0;
            AzeriteItem azeriteItem = ToAzeriteItem();
            if (azeriteItem != null)
                azeriteLevel = azeriteItem.GetEffectiveLevel();

            return GetItemLevel(itemTemplate, _bonusData, owner.GetLevel(), GetModifier(ItemModifier.TimewalkerLevel),
                minItemLevel, minItemLevelCutoff, maxItemLevel, pvpBonus, azeriteLevel);
        }

        public static uint GetItemLevel(ItemTemplate itemTemplate, BonusData bonusData, uint level, uint fixedLevel, uint minItemLevel, uint minItemLevelCutoff, uint maxItemLevel, bool pvpBonus, uint azeriteLevel)
        {
            if (itemTemplate == null)
                return 1;

            uint itemLevel = itemTemplate.GetBaseItemLevel();
            AzeriteLevelInfoRecord azeriteLevelInfo = CliDB.AzeriteLevelInfoStorage.LookupByKey(azeriteLevel);
            if (azeriteLevelInfo != null)
                itemLevel = azeriteLevelInfo.ItemLevel;

            if (bonusData.PlayerLevelToItemLevelCurveId != 0)
            {
                if (fixedLevel != 0)
                    level = fixedLevel;
                else
                {
                    var levels = Global.DB2Mgr.GetContentTuningData(bonusData.ContentTuningId, 0, true);
                    if (levels.HasValue)
                        level = (uint)Math.Min(Math.Max((ushort)level, levels.Value.MinLevel), levels.Value.MaxLevel);
                }

                itemLevel = (uint)Global.DB2Mgr.GetCurveValueAt(bonusData.PlayerLevelToItemLevelCurveId, level);
            }

            itemLevel += (uint)bonusData.ItemLevelBonus;

            for (uint i = 0; i < ItemConst.MaxGemSockets; ++i)
                itemLevel += bonusData.GemItemLevelBonus[i];

            uint itemLevelBeforeUpgrades = itemLevel;

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

        public float GetItemStatValue(uint index, Player owner)
        {
            Cypher.Assert(index < ItemConst.MaxStats);
            switch ((ItemModType)GetItemStatType(index))
            {
                case ItemModType.Corruption:
                case ItemModType.CorruptionResistance:
                    return _bonusData.StatPercentEditor[index];
                default:
                    break;
            }

            uint itemLevel = GetItemLevel(owner);
            float randomPropPoints = ItemEnchantmentManager.GetRandomPropertyPoints(itemLevel, GetQuality(), GetTemplate().GetInventoryType(), GetTemplate().GetSubClass());
            if (randomPropPoints != 0)
            {
                float statValue = _bonusData.StatPercentEditor[index] * randomPropPoints * 0.0001f;
                GtItemSocketCostPerLevelRecord gtCost = CliDB.ItemSocketCostPerLevelGameTable.GetRow(itemLevel);
                if (gtCost != null)
                    statValue -= _bonusData.ItemStatSocketCostMultiplier[index] * gtCost.SocketCost;

                return statValue;
            }

            return 0f;
        }

        public uint? GetDisenchantLootId()
        {
            if (!_bonusData.CanDisenchant)
                return null;

            if (_bonusData.DisenchantLootId != 0)
                return _bonusData.DisenchantLootId;

            // ignore temporary item level scaling (pvp or timewalking)
            uint itemLevel = GetItemLevel(GetTemplate(), _bonusData, (uint)_bonusData.RequiredLevel, GetModifier(ItemModifier.TimewalkerLevel), 0, 0, 0, false, 0);

            var disenchantLoot = GetBaseDisenchantLoot(GetTemplate(), (uint)GetQuality(), itemLevel);
            if (disenchantLoot == null)
                return null;

            return disenchantLoot.Id;
        }

        public ushort? GetDisenchantSkillRequired()
        {
            if (!_bonusData.CanDisenchant)
                return null;

            // ignore temporary item level scaling (pvp or timewalking)
            uint itemLevel = GetItemLevel(GetTemplate(), _bonusData, (uint)_bonusData.RequiredLevel, GetModifier(ItemModifier.TimewalkerLevel), 0, 0, 0, false, 0);

            var disenchantLoot = GetBaseDisenchantLoot(GetTemplate(), (uint)GetQuality(), itemLevel);
            if (disenchantLoot == null)
                return null;

            return disenchantLoot.SkillRequired;
        }

        public static ItemDisenchantLootRecord GetBaseDisenchantLoot(ItemTemplate itemTemplate, uint quality, uint itemLevel)
        {
            if (itemTemplate.HasFlag(ItemFlags.Conjured) || itemTemplate.HasFlag(ItemFlags.NoDisenchant) || itemTemplate.GetBonding() == ItemBondingType.Quest)
                return null;

            if (itemTemplate.GetArea(0) != 0 || itemTemplate.GetArea(1) != 0 || itemTemplate.GetMap() != 0 || itemTemplate.GetMaxStackSize() > 1)
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
            uint itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);
            if (itemModifiedAppearanceId == 0)
                itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

            ItemModifiedAppearanceRecord transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);
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
            int modifierIndex = m_itemData.Modifiers._value.Values.FindIndexIf(mod =>
            {
                return mod.Type == (byte)modifier;
            });

            if (modifierIndex != -1)
                return m_itemData.Modifiers._value.Values[modifierIndex].Value;

            return 0;
        }

        public void SetModifier(ItemModifier modifier, uint value)
        {
            int modifierIndex = m_itemData.Modifiers._value.Values.FindIndexIf(mod =>
            {
                return mod.Type == (byte)modifier;
            });

            if (value != 0)
            {
                if (modifierIndex == -1)
                {
                    ItemMod mod = new();
                    mod.Value = value;
                    mod.Type = (byte)modifier;

                    AddDynamicUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Modifiers)._value.ModifyValue(m_itemData.Modifiers._value.Values), mod);
                }
                else
                {
                    ItemModList itemModList = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Modifiers);
                    itemModList.ModifyValue(itemModList.Values, modifierIndex);
                    SetUpdateFieldValue(ref itemModList.ModifyValue(itemModList.Values, modifierIndex).GetValue().Value, value);
                }
            }
            else
            {
                if (modifierIndex == -1)
                    return;

                RemoveDynamicUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Modifiers)._value.ModifyValue(m_itemData.Modifiers._value.Values), modifierIndex);
            }
        }

        public uint GetVisibleEntry(Player owner)
        {
            uint itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);
            if (itemModifiedAppearanceId == 0)
                itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

            ItemModifiedAppearanceRecord transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);
            if (transmog != null)
                return transmog.ItemID;

            return GetEntry();
        }

        public ushort GetVisibleAppearanceModId(Player owner)
        {
            uint itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);
            if (itemModifiedAppearanceId == 0)
                itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

            ItemModifiedAppearanceRecord transmog = CliDB.ItemModifiedAppearanceStorage.LookupByKey(itemModifiedAppearanceId);
            if (transmog != null)
                return (ushort)transmog.ItemAppearanceModifierID;

            return (ushort)GetAppearanceModId();
        }

        uint GetVisibleModifiedAppearanceId(Player owner)
        {
            uint itemModifiedAppearanceId = GetModifier(ItemConst.AppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);
            if (itemModifiedAppearanceId == 0)
                itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogAppearanceAllSpecs);

            if (itemModifiedAppearanceId == 0)
            {
                var itemModifiedAppearance = GetItemModifiedAppearance();
                if (itemModifiedAppearance != null)
                    itemModifiedAppearanceId = itemModifiedAppearance.Id;
            }

            return itemModifiedAppearanceId;
        }

        public uint GetVisibleSecondaryModifiedAppearanceId(Player owner)
        {
            uint itemModifiedAppearanceId = GetModifier(ItemConst.SecondaryAppearanceModifierSlotBySpec[owner.GetActiveTalentGroup()]);
            if (itemModifiedAppearanceId == 0)
                itemModifiedAppearanceId = GetModifier(ItemModifier.TransmogSecondaryAppearanceAllSpecs);

            return itemModifiedAppearanceId;
        }

        public uint GetVisibleEnchantmentId(Player owner)
        {
            uint enchantmentId = GetModifier(ItemConst.IllusionModifierSlotBySpec[owner.GetActiveTalentGroup()]);
            if (enchantmentId == 0)
                enchantmentId = GetModifier(ItemModifier.EnchantIllusionAllSpecs);

            if (enchantmentId == 0)
                enchantmentId = GetEnchantmentId(EnchantmentSlot.Perm);

            return enchantmentId;
        }

        public ushort GetVisibleItemVisual(Player owner)
        {
            SpellItemEnchantmentRecord enchant = CliDB.SpellItemEnchantmentStorage.LookupByKey(GetVisibleEnchantmentId(owner));
            if (enchant != null)
                return enchant.ItemVisual;

            return 0;
        }

        public List<uint> GetBonusListIDs() { return m_itemData.ItemBonusKey.GetValue().BonusListIDs; }

        public void AddBonuses(uint bonusListID)
        {
            var bonusListIDs = GetBonusListIDs();
            if (bonusListIDs.Contains(bonusListID))
                return;

            ItemBonusKey itemBonusKey = new();
            itemBonusKey.ItemID = GetEntry();
            itemBonusKey.BonusListIDs = GetBonusListIDs();
            itemBonusKey.BonusListIDs.Add(bonusListID);
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemBonusKey), itemBonusKey);
            foreach (var bonus in ItemBonusMgr.GetItemBonuses(bonusListID))
                _bonusData.AddBonus(bonus.BonusType, bonus.Value);
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemAppearanceModID), (byte)_bonusData.AppearanceModID);
        }

        public void SetBonuses(List<uint> bonusListIDs)
        {
            if (bonusListIDs == null)
                bonusListIDs = new List<uint>();

            ItemBonusKey itemBonusKey = new();
            itemBonusKey.ItemID = GetEntry();
            itemBonusKey.BonusListIDs = bonusListIDs;
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemBonusKey), itemBonusKey);

            foreach (uint bonusListID in GetBonusListIDs())
                _bonusData.AddBonusList(bonusListID);

            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemAppearanceModID), (byte)_bonusData.AppearanceModID);
        }

        public void ClearBonuses()
        {
            ItemBonusKey itemBonusKey = new();
            itemBonusKey.ItemID = GetEntry();
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemBonusKey), itemBonusKey);
            _bonusData = new BonusData(GetTemplate());
            SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemAppearanceModID), (byte)_bonusData.AppearanceModID);
        }

        public bool IsArtifactDisabled()
        {
            ArtifactRecord artifact = CliDB.ArtifactStorage.LookupByKey(GetTemplate().GetArtifactID());
            if (artifact != null)
                return artifact.ArtifactCategoryID != 2; // fishing artifact

            return true;
        }

        public ArtifactPower GetArtifactPower(uint artifactPowerId)
        {
            var index = m_artifactPowerIdToIndex.LookupByKey(artifactPowerId);
            if (index != 0)
                return m_itemData.ArtifactPowers[index];

            return null;
        }

        void AddArtifactPower(ArtifactPowerData artifactPower)
        {
            int index = m_artifactPowerIdToIndex.Count;
            m_artifactPowerIdToIndex[artifactPower.ArtifactPowerId] = (ushort)index;

            ArtifactPower powerField = new();
            powerField.ArtifactPowerId = (ushort)artifactPower.ArtifactPowerId;
            powerField.PurchasedRank = artifactPower.PurchasedRank;
            powerField.CurrentRankWithBonus = artifactPower.CurrentRankWithBonus;

            AddDynamicUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactPowers), powerField);
        }

        public void SetArtifactPower(ushort artifactPowerId, byte purchasedRank, byte currentRankWithBonus)
        {
            var foundIndex = m_artifactPowerIdToIndex.LookupByKey(artifactPowerId);
            if (foundIndex != 0)
            {
                ArtifactPower artifactPower = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactPowers, foundIndex);
                SetUpdateFieldValue(ref artifactPower.PurchasedRank, purchasedRank);
                SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, currentRankWithBonus);
            }
        }

        public void InitArtifactPowers(byte artifactId, byte artifactTier)
        {
            foreach (ArtifactPowerRecord artifactPower in Global.DB2Mgr.GetArtifactPowers(artifactId))
            {
                if (artifactPower.Tier != artifactTier)
                    continue;

                if (m_artifactPowerIdToIndex.ContainsKey(artifactPower.Id))
                    continue;

                ArtifactPowerData powerData = new();
                powerData.ArtifactPowerId = artifactPower.Id;
                powerData.PurchasedRank = 0;
                powerData.CurrentRankWithBonus = (byte)((artifactPower.Flags & (byte)ArtifactPowerFlag.First) == (byte)ArtifactPowerFlag.First ? 1 : 0);
                AddArtifactPower(powerData);
            }
        }

        public uint GetTotalUnlockedArtifactPowers()
        {
            uint purchased = GetTotalPurchasedArtifactPowers();
            ulong artifactXp = m_itemData.ArtifactXP;
            uint currentArtifactTier = GetModifier(ItemModifier.ArtifactTier);
            uint extraUnlocked = 0;
            do
            {
                ulong xpCost = 0;
                var cost = CliDB.ArtifactLevelXPGameTable.GetRow(purchased + extraUnlocked + 1);
                if (cost != null)
                    xpCost = (ulong)(currentArtifactTier == PlayerConst.MaxArtifactTier ? cost.XP2 : cost.XP);

                if (artifactXp < xpCost)
                    break;

                artifactXp -= xpCost;
                ++extraUnlocked;

            } while (true);

            return purchased + extraUnlocked;
        }

        public uint GetTotalPurchasedArtifactPowers()
        {
            uint purchasedRanks = 0;
            foreach (ArtifactPower power in m_itemData.ArtifactPowers)
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
                            for (int artifactPowerIndex = 0; artifactPowerIndex < m_itemData.ArtifactPowers.Size(); ++artifactPowerIndex)
                            {
                                ArtifactPower artifactPower = m_itemData.ArtifactPowers[artifactPowerIndex];
                                if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label == enchant.EffectArg[i])
                                {
                                    byte newRank = artifactPower.CurrentRankWithBonus;
                                    if (apply)
                                        newRank += (byte)enchant.EffectPointsMin[i];
                                    else
                                        newRank -= (byte)enchant.EffectPointsMin[i];

                                    artifactPower = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactPowers, artifactPowerIndex);
                                    SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                                    if (IsEquipped())
                                    {
                                        ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));
                                        if (artifactPowerRank != null)
                                            owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                                    }
                                }
                            }
                        }
                        break;
                        case ItemEnchantmentType.ArtifactPowerBonusRankByID:
                        {
                            ushort artifactPowerIndex = m_artifactPowerIdToIndex.LookupByKey(enchant.EffectArg[i]);
                            if (artifactPowerIndex != 0)
                            {
                                byte newRank = m_itemData.ArtifactPowers[artifactPowerIndex].CurrentRankWithBonus;
                                if (apply)
                                    newRank += (byte)enchant.EffectPointsMin[i];
                                else
                                    newRank -= (byte)enchant.EffectPointsMin[i];

                                ArtifactPower artifactPower = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactPowers, artifactPowerIndex);
                                SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                                if (IsEquipped())
                                {
                                    ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(m_itemData.ArtifactPowers[artifactPowerIndex].ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));
                                    if (artifactPowerRank != null)
                                        owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                                }
                            }
                        }
                        break;
                        case ItemEnchantmentType.ArtifactPowerBonusRankPicker:
                            if (slot >= EnchantmentSlot.Sock1 && slot <= EnchantmentSlot.Sock3 && _bonusData.GemRelicType[slot - EnchantmentSlot.Sock1] != -1)
                            {
                                ArtifactPowerPickerRecord artifactPowerPicker = CliDB.ArtifactPowerPickerStorage.LookupByKey(enchant.EffectArg[i]);
                                if (artifactPowerPicker != null)
                                {
                                    if (ConditionManager.IsPlayerMeetingCondition(owner, artifactPowerPicker.PlayerConditionID))
                                    {
                                        for (int artifactPowerIndex = 0; artifactPowerIndex < m_itemData.ArtifactPowers.Size(); ++artifactPowerIndex)
                                        {
                                            ArtifactPower artifactPower = m_itemData.ArtifactPowers[artifactPowerIndex];
                                            if (CliDB.ArtifactPowerStorage.LookupByKey(artifactPower.ArtifactPowerId).Label == _bonusData.GemRelicType[slot - EnchantmentSlot.Sock1])
                                            {
                                                byte newRank = artifactPower.CurrentRankWithBonus;
                                                if (apply)
                                                    newRank += (byte)enchant.EffectPointsMin[i];
                                                else
                                                    newRank -= (byte)enchant.EffectPointsMin[i];

                                                artifactPower = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactPowers, artifactPowerIndex);
                                                SetUpdateFieldValue(ref artifactPower.CurrentRankWithBonus, newRank);

                                                if (IsEquipped())
                                                {
                                                    ArtifactPowerRankRecord artifactPowerRank = Global.DB2Mgr.GetArtifactPowerRank(artifactPower.ArtifactPowerId, (byte)(newRank != 0 ? newRank - 1 : 0));
                                                    if (artifactPowerRank != null)
                                                        owner.ApplyArtifactPowerRank(this, artifactPowerRank, newRank != 0);
                                                }
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

        public void SetArtifactXP(ulong xp) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ArtifactXP), xp); }

        public void GiveArtifactXp(ulong amount, Item sourceItem, ArtifactCategory artifactCategoryId)
        {
            Player owner = GetOwner();
            if (owner == null)
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

            SetArtifactXP(m_itemData.ArtifactXP + amount);

            ArtifactXpGain artifactXpGain = new();
            artifactXpGain.ArtifactGUID = GetGUID();
            artifactXpGain.Amount = amount;
            owner.SendPacket(artifactXpGain);

            SetState(ItemUpdateState.Changed, owner);

            owner.UpdateCriteria(CriteriaType.EarnArtifactXP, amount);
        }

        public ItemContext GetContext() { return (ItemContext)(int)m_itemData.Context; }
        public void SetContext(ItemContext context) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Context), (byte)context); }

        public void SetPetitionId(uint petitionId)
        {
            ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, 0);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.ID), petitionId);
        }
        public void SetPetitionNumSignatures(uint signatures)
        {
            ItemEnchantment enchantmentField = m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Enchantment, 0);
            SetUpdateFieldValue(enchantmentField.ModifyValue(enchantmentField.Duration), signatures);
        }

        public void SetFixedLevel(uint level)
        {
            if (!_bonusData.HasFixedLevel || GetModifier(ItemModifier.TimewalkerLevel) != 0)
                return;

            if (_bonusData.PlayerLevelToItemLevelCurveId != 0)
            {
                var levels = Global.DB2Mgr.GetContentTuningData(_bonusData.ContentTuningId, 0, true);
                if (levels.HasValue)
                    level = (uint)Math.Min(Math.Max((short)level, levels.Value.MinLevel), levels.Value.MaxLevel);

                SetModifier(ItemModifier.TimewalkerLevel, level);
            }
        }

        public int GetRequiredLevel()
        {
            int fixedLevel = (int)GetModifier(ItemModifier.TimewalkerLevel);
            if (_bonusData.RequiredLevelCurve != 0)
                return (int)Global.DB2Mgr.GetCurveValueAt(_bonusData.RequiredLevelCurve, fixedLevel);
            if (_bonusData.RequiredLevelOverride != 0)
                return _bonusData.RequiredLevelOverride;
            if (_bonusData.HasFixedLevel && _bonusData.PlayerLevelToItemLevelCurveId != 0)
                return fixedLevel;
            return _bonusData.RequiredLevel;
        }

        public override string GetDebugInfo()
        {
            return $"{base.GetDebugInfo()}\nOwner: {GetOwnerGUID()} Count: {GetCount()} BagSlot: {GetBagSlot()} Slot: {GetSlot()} Equipped: {IsEquipped()}";
        }

        public static Item NewItemOrBag(ItemTemplate proto)
        {
            if (proto.GetInventoryType() == InventoryType.Bag)
                return new Bag();

            if (Global.DB2Mgr.IsAzeriteItem(proto.GetId()))
                return new AzeriteItem();

            if (Global.DB2Mgr.GetAzeriteEmpoweredItem(proto.GetId()) != null)
                return new AzeriteEmpoweredItem();

            return new Item();
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

            if (set.HasFlag(ItemSetFlags.LegacyInactive))
                return;

            // Check player level for heirlooms
            if (Global.DB2Mgr.GetHeirloomByItemId(item.GetEntry()) != null)
            {
                if (item.GetBonus().PlayerLevelToItemLevelCurveId != 0)
                {
                    uint maxLevel = (uint)Global.DB2Mgr.GetCurveXAxisRange(item.GetBonus().PlayerLevelToItemLevelCurveId).Item2;

                    var contentTuning = Global.DB2Mgr.GetContentTuningData(item.GetBonus().ContentTuningId, player.m_playerData.CtrOptions._value.ContentTuningConditionMask, true);
                    if (contentTuning.HasValue)
                        maxLevel = Math.Min(maxLevel, (uint)contentTuning.Value.MaxLevel);

                    if (player.GetLevel() > maxLevel)
                        return;
                }
            }

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

            eff.EquippedItems.Add(item);

            List<ItemSetSpellRecord> itemSetSpells = Global.DB2Mgr.GetItemSetSpells(setid);
            foreach (var itemSetSpell in itemSetSpells)
            {
                //not enough for  spell
                if (itemSetSpell.Threshold > eff.EquippedItems.Count)
                    continue;

                if (eff.SetBonuses.Contains(itemSetSpell))
                    continue;

                SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID, Difficulty.None);
                if (spellInfo == null)
                {
                    Log.outError(LogFilter.Player, "WORLD: unknown spell id {0} in items set {1} effects", itemSetSpell.SpellID, setid);
                    continue;
                }

                eff.SetBonuses.Add(itemSetSpell);
                // spell cast only if fit form requirement, in other case will cast at form change
                if (itemSetSpell.ChrSpecID == 0 || (ChrSpecialization)itemSetSpell.ChrSpecID == player.GetPrimarySpecialization())
                    player.ApplyEquipSpell(spellInfo, null, true);
            }
        }

        public static void RemoveItemsSetItem(Player player, Item item)
        {
            uint setid = item.GetTemplate().GetItemSet();

            ItemSetRecord set = CliDB.ItemSetStorage.LookupByKey(setid);
            if (set == null)
            {
                Log.outError(LogFilter.Sql, $"Item set {setid} for item {item.GetEntry()} not found, mods not removed.");
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

            eff.EquippedItems.Remove(item);

            List<ItemSetSpellRecord> itemSetSpells = Global.DB2Mgr.GetItemSetSpells(setid);
            foreach (ItemSetSpellRecord itemSetSpell in itemSetSpells)
            {
                // enough for spell
                if (itemSetSpell.Threshold <= eff.EquippedItems.Count)
                    continue;

                if (!eff.SetBonuses.Contains(itemSetSpell))
                    continue;

                player.ApplyEquipSpell(Global.SpellMgr.GetSpellInfo(itemSetSpell.SpellID, Difficulty.None), null, false);
                eff.SetBonuses.Remove(itemSetSpell);
            }

            if (eff.EquippedItems.Empty())                                    //all items of a set were removed
            {
                Cypher.Assert(eff == player.ItemSetEff[setindex]);
                player.ItemSetEff[setindex] = null;
            }
        }

        public BonusData GetBonus() { return _bonusData; }

        public override ObjectGuid GetOwnerGUID() { return m_itemData.Owner; }
        public void SetOwnerGUID(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Owner), guid); }
        public ObjectGuid GetContainedIn() { return m_itemData.ContainedIn; }
        public void SetContainedIn(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ContainedIn), guid); }
        public ObjectGuid GetCreator() { return m_itemData.Creator; }
        public void SetCreator(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Creator), guid); }
        public ObjectGuid GetGiftCreator() { return m_itemData.GiftCreator; }
        public void SetGiftCreator(ObjectGuid guid) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.GiftCreator), guid); }

        void SetExpiration(uint expiration) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Expiration), expiration); }

        public ItemBondingType GetBonding() { return _bonusData.Bonding; }
        public void SetBinding(bool val)
        {
            if (val)
                SetItemFlag(ItemFieldFlags.Soulbound);
            else
                RemoveItemFlag(ItemFieldFlags.Soulbound);
        }

        public bool IsSoulBound() { return HasItemFlag(ItemFieldFlags.Soulbound); }
        public bool IsBoundAccountWide() { return GetTemplate().HasFlag(ItemFlags.IsBoundToAccount); }
        public bool IsBattlenetAccountBound() { return GetTemplate().HasFlag(ItemFlags2.BnetAccountTradeOk); }

        public bool HasItemFlag(ItemFieldFlags flag) { return (m_itemData.DynamicFlags & (uint)flag) != 0; }
        public void SetItemFlag(ItemFieldFlags flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.DynamicFlags), (uint)flags); }
        public void RemoveItemFlag(ItemFieldFlags flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.DynamicFlags), (uint)flags); }
        public void ReplaceAllItemFlags(ItemFieldFlags flags) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.DynamicFlags), (uint)flags); }
        public bool HasItemFlag2(ItemFieldFlags2 flag) { return (m_itemData.DynamicFlags2 & (uint)flag) != 0; }
        public void SetItemFlag2(ItemFieldFlags2 flags) { SetUpdateFieldFlagValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.DynamicFlags2), (uint)flags); }
        public void RemoveItemFlag2(ItemFieldFlags2 flags) { RemoveUpdateFieldFlagValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.DynamicFlags2), (uint)flags); }
        public void ReplaceAllItemFlags2(ItemFieldFlags2 flags) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.DynamicFlags2), (uint)flags); }

        public Bag ToBag() { return IsBag() ? this as Bag : null; }
        public AzeriteItem ToAzeriteItem() { return IsAzeriteItem() ? this as AzeriteItem : null; }
        public AzeriteEmpoweredItem ToAzeriteEmpoweredItem() { return IsAzeriteEmpoweredItem() ? this as AzeriteEmpoweredItem : null; }

        public bool IsRefundable() { return HasItemFlag(ItemFieldFlags.Refundable); }
        public bool IsBOPTradeable() { return HasItemFlag(ItemFieldFlags.BopTradeable); }
        public bool IsWrapped() { return HasItemFlag(ItemFieldFlags.Wrapped); }
        public bool IsLocked() { return !HasItemFlag(ItemFieldFlags.Unlocked); }
        public bool IsBag() { return GetTemplate().GetInventoryType() == InventoryType.Bag; }
        public bool IsAzeriteItem() { return GetTypeId() == TypeId.AzeriteItem; }
        public bool IsAzeriteEmpoweredItem() { return GetTypeId() == TypeId.AzeriteEmpoweredItem; }
        public bool IsCurrencyToken() { return GetTemplate().IsCurrencyToken(); }
        public bool IsBroken() { return m_itemData.MaxDurability > 0 && m_itemData.Durability == 0; }
        public void SetDurability(uint durability) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.Durability), durability); }
        public void SetMaxDurability(uint maxDurability) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.MaxDurability), maxDurability); }
        public void SetInTrade(bool b = true) { mb_in_trade = b; }
        public bool IsInTrade() { return mb_in_trade; }

        public uint GetCount() { return m_itemData.StackCount; }
        public uint GetMaxStackCount() { return GetTemplate().GetMaxStackSize(); }

        public byte GetSlot() { return m_slot; }
        public Bag GetContainer() { return m_container; }
        public void SetSlot(byte slot) { m_slot = slot; }
        public ushort GetPos() { return (ushort)(GetBagSlot() << 8 | GetSlot()); }
        public void SetContainer(Bag container) { m_container = container; }

        bool IsInBag() { return m_container != null; }

        public uint GetItemRandomBonusListId() { return m_randomBonusListId; }
        public uint GetEnchantmentId(EnchantmentSlot slot) { return m_itemData.Enchantment[(int)slot].ID; }
        public uint GetEnchantmentDuration(EnchantmentSlot slot) { return m_itemData.Enchantment[(int)slot].Duration; }
        public int GetEnchantmentCharges(EnchantmentSlot slot) { return m_itemData.Enchantment[(int)slot].Charges; }

        public void SetCreatePlayedTime(uint createPlayedTime) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.CreatePlayedTime), createPlayedTime); }
        public void SetCreateTime(long createTime) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.CreateTime), createTime); }

        public string GetText() { return m_text; }
        public void SetText(string text) { m_text = text; }

        public int GetSpellCharges(int index = 0) { return m_itemData.SpellCharges[index]; }
        public void SetSpellCharges(int index, int value) { SetUpdateFieldValue(ref m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.SpellCharges, index), value); }

        public ItemUpdateState GetState() { return uState; }

        public bool IsInUpdateQueue() { return uQueuePos != -1; }
        public int GetQueuePos() { return uQueuePos; }
        public void FSetState(ItemUpdateState state)// forced
        {
            uState = state;
        }

        public override bool HasQuest(uint quest_id) { return GetTemplate().GetStartQuest() == quest_id; }
        public override bool HasInvolvedQuest(uint quest_id) { return false; }
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
        public uint GetAppearanceModId() { return m_itemData.ItemAppearanceModID; }
        public void SetAppearanceModId(uint appearanceModId) { SetUpdateFieldValue(m_values.ModifyValue(m_itemData).ModifyValue(m_itemData.ItemAppearanceModID), (byte)appearanceModId); }
        public float GetRepairCostMultiplier() { return _bonusData.RepairCostMultiplier; }
        public uint GetScalingContentTuningId() { return _bonusData.ContentTuningId; }

        public void SetRefundRecipient(ObjectGuid guid) { m_refundRecipient = guid; }
        public void SetPaidMoney(ulong money) { m_paidMoney = money; }
        public void SetPaidExtendedCost(uint iece) { m_paidExtendedCost = iece; }

        public ObjectGuid GetRefundRecipient() { return m_refundRecipient; }
        public ulong GetPaidMoney() { return m_paidMoney; }
        public uint GetPaidExtendedCost() { return m_paidExtendedCost; }

        public uint GetScriptId() { return GetTemplate().ScriptId; }

        public ObjectGuid GetChildItem() { return m_childItem; }
        public void SetChildItem(ObjectGuid childItem) { m_childItem = childItem; }

        public ItemEffectRecord[] GetEffects() { return _bonusData.Effects[0.._bonusData.EffectCount]; }

        public override Loot GetLootForPlayer(Player player) { return loot; }

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
                        case ItemSubClassContainer.ReagentContainer:
                            return pProto.IsCraftingReagent();
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
        public ItemData m_itemData;

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
        List<ObjectGuid> allowedGUIDs = new();
        uint m_randomBonusListId;        // store separately to easily find which bonus list is the one randomly given for stat rerolling
        ObjectGuid m_childItem;
        Dictionary<uint, ushort> m_artifactPowerIdToIndex = new();
        Array<uint> m_gemScalingLevels = new(ItemConst.MaxGemSockets);
        #endregion

        class ValuesUpdateForPlayerWithMaskSender : IDoWork<Player>
        {
            Item Owner;
            ObjectFieldData ObjectMask = new();
            ItemData ItemMask = new();

            public ValuesUpdateForPlayerWithMaskSender(Item owner)
            {
                Owner = owner;
            }

            public void Invoke(Player player)
            {
                UpdateData udata = new(Owner.GetMapId());

                Owner.BuildValuesUpdateForPlayerWithMask(udata, ObjectMask.GetUpdateMask(), ItemMask.GetUpdateMask(), player);

                udata.BuildPacket(out UpdateObject packet);
                player.SendPacket(packet);
            }
        }
    }

    public class ItemPosCount
    {
        public ItemPosCount(ushort _pos, uint _count)
        {
            pos = _pos;
            count = _count;
        }

        public bool IsContainedIn(List<ItemPosCount> vec)
        {
            foreach (var posCount in vec)
                if (posCount.pos == pos)
                    return true;
            return false;
        }

        public ushort pos;
        public uint count;
    }

    public class ItemSetEffect
    {
        public uint ItemSetID;
        public List<Item> EquippedItems = new();
        public List<ItemSetSpellRecord> SetBonuses = new();
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
            {
                ItemStatType[i] = proto.GetStatModifierBonusStat(i);
                StatPercentEditor[i] = proto.GetStatPercentEditor(i);
                ItemStatSocketCostMultiplier[i] = proto.GetStatPercentageOfSocket(i);
            }

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
            ContentTuningId = proto.GetScalingStatContentTuning();
            PlayerLevelToItemLevelCurveId = proto.GetPlayerLevelToItemLevelCurveId();
            RelicType = -1;
            HasFixedLevel = false;
            RequiredLevelOverride = 0;
            AzeriteTierUnlockSetId = 0;

            AzeriteEmpoweredItemRecord azeriteEmpoweredItem = Global.DB2Mgr.GetAzeriteEmpoweredItem(proto.GetId());
            if (azeriteEmpoweredItem != null)
                AzeriteTierUnlockSetId = azeriteEmpoweredItem.AzeriteTierUnlockSetID;

            EffectCount = 0;
            foreach (ItemEffectRecord itemEffect in proto.Effects)
                Effects[EffectCount++] = itemEffect;

            for (int i = EffectCount; i < Effects.Length; ++i)
                Effects[i] = null;

            CanDisenchant = !proto.HasFlag(ItemFlags.NoDisenchant);
            CanScrap = proto.HasFlag(ItemFlags4.Scrapable);

            _state.SuffixPriority = int.MaxValue;
            _state.AppearanceModPriority = int.MaxValue;
            _state.DisenchantLootPriority = int.MaxValue;
            _state.ScalingStatDistributionPriority = int.MaxValue;
            _state.AzeriteTierUnlockSetPriority = int.MaxValue;
            _state.RequiredLevelCurvePriority = int.MaxValue;
            _state.HasQualityBonus = false;
        }

        public BonusData(ItemInstance itemInstance) : this(Global.ObjectMgr.GetItemTemplate(itemInstance.ItemID))
        {
            if (itemInstance.ItemBonus != null)
            {
                foreach (uint bonusListID in itemInstance.ItemBonus.BonusListIDs)
                    AddBonusList(bonusListID);
            }
        }

        public void AddBonusList(uint bonusListId)
        {
            foreach (var bonus in ItemBonusMgr.GetItemBonuses(bonusListId))
                AddBonus(bonus.BonusType, bonus.Value);
        }

        public void AddBonus(ItemBonusType type, int[] values)
        {
            switch (type)
            {
                case ItemBonusType.ItemLevel:
                    ItemLevelBonus += values[0];
                    break;
                case ItemBonusType.Stat:
                {
                    uint statIndex;
                    for (statIndex = 0; statIndex < ItemConst.MaxStats; ++statIndex)
                        if (ItemStatType[statIndex] == values[0] || ItemStatType[statIndex] == -1)
                            break;

                    if (statIndex < ItemConst.MaxStats)
                    {
                        ItemStatType[statIndex] = values[0];
                        StatPercentEditor[statIndex] += values[1];
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
                case ItemBonusType.Suffix:
                    if (values[1] < _state.SuffixPriority)
                    {
                        Suffix = (uint)values[0];
                        _state.SuffixPriority = values[1];
                    }
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
                        ContentTuningId = (uint)values[2];
                        PlayerLevelToItemLevelCurveId = (uint)values[3];
                        _state.ScalingStatDistributionPriority = values[1];
                        HasFixedLevel = type == ItemBonusType.ScalingStatDistributionFixed;
                    }
                    break;
                case ItemBonusType.DisenchantLootId:
                    if (values[1] < _state.DisenchantLootPriority)
                    {
                        DisenchantLootId = (uint)values[0];
                        _state.DisenchantLootPriority = values[1];
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
                case ItemBonusType.AzeriteTierUnlockSet:
                    if (values[1] < _state.AzeriteTierUnlockSetPriority)
                    {
                        AzeriteTierUnlockSetId = (uint)values[0];
                        _state.AzeriteTierUnlockSetPriority = values[1];
                    }
                    break;
                case ItemBonusType.OverrideCanDisenchant:
                    CanDisenchant = values[0] != 0;
                    break;
                case ItemBonusType.OverrideCanScrap:
                    CanScrap = values[0] != 0;
                    break;
                case ItemBonusType.ItemEffectId:
                    ItemEffectRecord itemEffect = CliDB.ItemEffectStorage.LookupByKey(values[0]);
                    if (itemEffect != null)
                        Effects[EffectCount++] = itemEffect;
                    break;
                case ItemBonusType.RequiredLevelCurve:
                    if (values[2] < _state.RequiredLevelCurvePriority)
                    {
                        RequiredLevelCurve = (uint)values[0];
                        _state.RequiredLevelCurvePriority = values[2];
                        if (values[1] != 0)
                            ContentTuningId = (uint)values[1];
                    }
                    break;
            }
        }

        public ItemQuality Quality;
        public int ItemLevelBonus;
        public int RequiredLevel;
        public int[] ItemStatType = new int[ItemConst.MaxStats];
        public int[] StatPercentEditor = new int[ItemConst.MaxStats];
        public float[] ItemStatSocketCostMultiplier = new float[ItemConst.MaxStats];
        public SocketColor[] socketColor = new SocketColor[ItemConst.MaxGemSockets];
        public ItemBondingType Bonding;
        public uint AppearanceModID;
        public float RepairCostMultiplier;
        public uint ContentTuningId;
        public uint PlayerLevelToItemLevelCurveId;
        public uint DisenchantLootId;
        public uint[] GemItemLevelBonus = new uint[ItemConst.MaxGemSockets];
        public int[] GemRelicType = new int[ItemConst.MaxGemSockets];
        public ushort[] GemRelicRankBonus = new ushort[ItemConst.MaxGemSockets];
        public int RelicType;
        public int RequiredLevelOverride;
        public uint AzeriteTierUnlockSetId;
        public uint Suffix;
        public uint RequiredLevelCurve;
        public ItemEffectRecord[] Effects = new ItemEffectRecord[13];
        public int EffectCount;
        public bool CanDisenchant;
        public bool CanScrap;
        public bool HasFixedLevel;
        State _state;

        struct State
        {
            public int SuffixPriority;
            public int AppearanceModPriority;
            public int DisenchantLootPriority;
            public int ScalingStatDistributionPriority;
            public int AzeriteTierUnlockSetPriority;
            public int RequiredLevelCurvePriority;
            public bool HasQualityBonus;
        }
    }

    public class ArtifactPowerData
    {
        public uint ArtifactPowerId;
        public byte PurchasedRank;
        public byte CurrentRankWithBonus;
    }

    class ArtifactData
    {
        public ulong Xp;
        public uint ArtifactAppearanceId;
        public uint ArtifactTierId;
        public List<ArtifactPowerData> ArtifactPowers = new();
    }

    public class AzeriteEmpoweredData
    {
        public int[] SelectedAzeritePowers = new int[SharedConst.MaxAzeriteEmpoweredTier];
    }

    class ItemAdditionalLoadInfo
    {
        public ArtifactData Artifact;
        public AzeriteData AzeriteItem;
        public AzeriteEmpoweredData AzeriteEmpoweredItem;

        public static void Init(Dictionary<ulong, ItemAdditionalLoadInfo> loadInfo, SQLResult artifactResult, SQLResult azeriteItemResult, SQLResult azeriteItemMilestonePowersResult,
            SQLResult azeriteItemUnlockedEssencesResult, SQLResult azeriteEmpoweredItemResult)
        {
            ItemAdditionalLoadInfo GetOrCreateLoadInfo(ulong guid)
            {
                if (!loadInfo.ContainsKey(guid))
                    loadInfo[guid] = new ItemAdditionalLoadInfo();

                return loadInfo[guid];
            }

            if (!artifactResult.IsEmpty())
            {
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(artifactResult.Read<ulong>(0));
                    if (info.Artifact == null)
                        info.Artifact = new ArtifactData();

                    info.Artifact.Xp = artifactResult.Read<ulong>(1);
                    info.Artifact.ArtifactAppearanceId = artifactResult.Read<uint>(2);
                    info.Artifact.ArtifactTierId = artifactResult.Read<uint>(3);

                    ArtifactPowerData artifactPowerData = new();
                    artifactPowerData.ArtifactPowerId = artifactResult.Read<uint>(4);
                    artifactPowerData.PurchasedRank = artifactResult.Read<byte>(5);

                    ArtifactPowerRecord artifactPower = CliDB.ArtifactPowerStorage.LookupByKey(artifactPowerData.ArtifactPowerId);
                    if (artifactPower != null)
                    {
                        uint maxRank = artifactPower.MaxPurchasableRank;
                        // allow ARTIFACT_POWER_FLAG_FINAL to overflow maxrank here - needs to be handled in Item::CheckArtifactUnlock (will refund artifact power)
                        if (artifactPower.HasFlag(ArtifactPowerFlag.MaxRankWithTier) && artifactPower.Tier < info.Artifact.ArtifactTierId)
                            maxRank += info.Artifact.ArtifactTierId - artifactPower.Tier;

                        if (artifactPowerData.PurchasedRank > maxRank)
                            artifactPowerData.PurchasedRank = (byte)maxRank;

                        artifactPowerData.CurrentRankWithBonus = (byte)((artifactPower.Flags & (byte)ArtifactPowerFlag.First) == (byte)ArtifactPowerFlag.First ? 1 : 0);

                        info.Artifact.ArtifactPowers.Add(artifactPowerData);
                    }

                } while (artifactResult.NextRow());
            }

            if (!azeriteItemResult.IsEmpty())
            {
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteItemResult.Read<ulong>(0));
                    if (info.AzeriteItem == null)
                        info.AzeriteItem = new AzeriteData();

                    info.AzeriteItem.Xp = azeriteItemResult.Read<ulong>(1);
                    info.AzeriteItem.Level = azeriteItemResult.Read<uint>(2);
                    info.AzeriteItem.KnowledgeLevel = azeriteItemResult.Read<uint>(3);
                    for (int i = 0; i < info.AzeriteItem.SelectedAzeriteEssences.Length; ++i)
                    {
                        info.AzeriteItem.SelectedAzeriteEssences[i] = new();

                        uint specializationId = azeriteItemResult.Read<uint>(4 + i * 4);
                        if (!CliDB.ChrSpecializationStorage.ContainsKey(specializationId))
                            continue;

                        info.AzeriteItem.SelectedAzeriteEssences[i].SpecializationId = specializationId;
                        for (int j = 0; j < SharedConst.MaxAzeriteEssenceSlot; ++j)
                        {
                            AzeriteEssenceRecord azeriteEssence = CliDB.AzeriteEssenceStorage.LookupByKey(azeriteItemResult.Read<uint>(5 + i * 5 + j));
                            if (azeriteEssence == null || !Global.DB2Mgr.IsSpecSetMember(azeriteEssence.SpecSetID, specializationId))
                                continue;

                            info.AzeriteItem.SelectedAzeriteEssences[i].AzeriteEssenceId[j] = azeriteEssence.Id;
                        }
                    }

                } while (azeriteItemResult.NextRow());
            }

            if (!azeriteItemMilestonePowersResult.IsEmpty())
            {
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteItemMilestonePowersResult.Read<ulong>(0));
                    if (info.AzeriteItem == null)
                        info.AzeriteItem = new AzeriteData();

                    info.AzeriteItem.AzeriteItemMilestonePowers.Add(azeriteItemMilestonePowersResult.Read<uint>(1));
                }
                while (azeriteItemMilestonePowersResult.NextRow());
            }

            if (!azeriteItemUnlockedEssencesResult.IsEmpty())
            {
                do
                {
                    AzeriteEssencePowerRecord azeriteEssencePower = Global.DB2Mgr.GetAzeriteEssencePower(azeriteItemUnlockedEssencesResult.Read<uint>(1), azeriteItemUnlockedEssencesResult.Read<uint>(2));
                    if (azeriteEssencePower != null)
                    {
                        ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteItemUnlockedEssencesResult.Read<ulong>(0));
                        if (info.AzeriteItem == null)
                            info.AzeriteItem = new AzeriteData();

                        info.AzeriteItem.UnlockedAzeriteEssences.Add(azeriteEssencePower);
                    }
                }
                while (azeriteItemUnlockedEssencesResult.NextRow());
            }

            if (!azeriteEmpoweredItemResult.IsEmpty())
            {
                do
                {
                    ItemAdditionalLoadInfo info = GetOrCreateLoadInfo(azeriteEmpoweredItemResult.Read<ulong>(0));
                    if (info.AzeriteEmpoweredItem == null)
                        info.AzeriteEmpoweredItem = new AzeriteEmpoweredData();

                    for (int i = 0; i < SharedConst.MaxAzeriteEmpoweredTier; ++i)
                        if (CliDB.AzeritePowerStorage.ContainsKey(azeriteEmpoweredItemResult.Read<int>(1 + i)))
                            info.AzeriteEmpoweredItem.SelectedAzeritePowers[i] = azeriteEmpoweredItemResult.Read<int>(1 + i);

                } while (azeriteEmpoweredItemResult.NextRow());
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public class ItemDynamicFieldGems
    {
        public uint ItemId;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public ushort[] BonusListIDs = new ushort[16];
        public byte Context;
    }
}
