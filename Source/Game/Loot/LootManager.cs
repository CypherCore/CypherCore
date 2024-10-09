// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Database;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Loots
{
    using LootStoreItemList = List<LootStoreItem>;
    using LootTemplateMap = Dictionary<uint, LootTemplate>;

    public class LootManager : LootStorage
    {
        static void Initialize()
        {
            Creature = new LootStore("creature_loot_template", "creature entry");
            Disenchant = new LootStore("disenchant_loot_template", "item disenchant id");
            Fishing = new LootStore("fishing_loot_template", "area id");
            Gameobject = new LootStore("gameobject_loot_template", "gameobject entry");
            Items = new LootStore("item_loot_template", "item entry");
            Mail = new LootStore("mail_loot_template", "mail template id", false);
            Milling = new LootStore("milling_loot_template", "item entry (herb)");
            Pickpocketing = new LootStore("pickpocketing_loot_template", "creature pickpocket lootid");
            Prospecting = new LootStore("prospecting_loot_template", "item entry (ore)");
            Reference = new LootStore("reference_loot_template", "reference id", false);
            Skinning = new LootStore("skinning_loot_template", "creature skinning id");
            Spell = new LootStore("spell_loot_template", "spell id (random item creating)", false);
        }

        public static void LoadLootTables()
        {
            Initialize();
            LoadLootTemplates_Creature();
            LoadLootTemplates_Fishing();
            LoadLootTemplates_Gameobject();
            LoadLootTemplates_Item();
            LoadLootTemplates_Mail();
            LoadLootTemplates_Milling();
            LoadLootTemplates_Pickpocketing();
            LoadLootTemplates_Skinning();
            LoadLootTemplates_Disenchant();
            LoadLootTemplates_Prospecting();
            LoadLootTemplates_Spell();

            LoadLootTemplates_Reference();
        }

        public static Dictionary<ObjectGuid, Loot> GenerateDungeonEncounterPersonalLoot(uint dungeonEncounterId, uint lootId, LootStore store,
            LootType type, WorldObject lootOwner, uint minMoney, uint maxMoney, ushort lootMode, MapDifficultyRecord mapDifficulty, List<Player> tappers)
        {
            Dictionary<Player, Loot> tempLoot = new();

            foreach (Player tapper in tappers)
            {
                if (tapper.IsLockedToDungeonEncounter(dungeonEncounterId))
                    continue;

                Loot loot = new(lootOwner.GetMap(), lootOwner.GetGUID(), type, null);
                loot.SetItemContext(ItemBonusMgr.GetContextForPlayer(mapDifficulty, tapper));
                loot.SetDungeonEncounterId(dungeonEncounterId);
                loot.GenerateMoneyLoot(minMoney, maxMoney);

                tempLoot[tapper] = loot;
            }

            LootTemplate tab = store.GetLootFor(lootId);
            if (tab != null)
                tab.ProcessPersonalLoot(tempLoot, store.IsRatesAllowed(), lootMode);

            Dictionary<ObjectGuid, Loot> personalLoot = new();
            foreach (var (looter, loot) in tempLoot)
            {
                loot.FillNotNormalLootFor(looter);

                if (loot.IsLooted())
                    continue;

                personalLoot[looter.GetGUID()] = loot;
            }

            return personalLoot;
        }

        public static void LoadLootTemplates_Creature()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading creature loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet, lootIdSetUsed = new();
            uint count = Creature.LoadAndCollectLootIds(out lootIdSet);

            // Remove real entries and check loot existence
            var templates = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var (creatureId, creatureTemplate) in templates)
            {
                foreach (var (_, creatureDifficulty) in creatureTemplate.difficultyStorage)
                {
                    if (creatureDifficulty.LootID != 0)
                    {
                        if (!lootIdSet.Contains(creatureDifficulty.LootID))
                            Creature.ReportNonExistingId(creatureDifficulty.LootID, creatureId);
                        else
                            lootIdSetUsed.Add(creatureDifficulty.LootID);
                    }
                }
            }

            foreach (var lootId in lootIdSetUsed)
                lootIdSet.Remove(lootId);

            // 1 means loot for player corpse
            lootIdSet.Remove(SharedConst.PlayerCorpseLootEntry);

            // output error for any still listed (not referenced from appropriate table) ids
            Creature.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creature loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 creature loot templates. DB table `creature_loot_template` is empty");
        }

        public static void LoadLootTemplates_Disenchant()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading disenchanting loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet, lootIdSetUsed = new();
            uint count = Disenchant.LoadAndCollectLootIds(out lootIdSet);

            foreach (var (_, disenchant) in CliDB.ItemDisenchantLootStorage)
            {
                uint lootid = disenchant.Id;
                if (!lootIdSet.Contains(lootid))
                    Disenchant.ReportNonExistingId(lootid, disenchant.Id);
                else
                    lootIdSetUsed.Add(lootid);
            }

            foreach (var lootId in lootIdSetUsed)
                lootIdSet.Remove(lootId);

            // output error for any still listed (not referenced from appropriate table) ids
            Disenchant.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} disenchanting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 disenchanting loot templates. DB table `disenchant_loot_template` is empty");
        }

        public static void LoadLootTemplates_Fishing()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading fishing loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Fishing.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            foreach (var areaEntry in CliDB.AreaTableStorage.Values)
                if (lootIdSet.Contains(areaEntry.Id))
                    lootIdSet.Remove(areaEntry.Id);

            // output error for any still listed (not referenced from appropriate table) ids
            Fishing.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} fishing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 fishing loot templates. DB table `fishing_loot_template` is empty");
        }

        public static void LoadLootTemplates_Gameobject()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading gameobject loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet, lootIdSetUsed = new();
            uint count = Gameobject.LoadAndCollectLootIds(out lootIdSet);

            void checkLootId(uint lootId, uint gameObjectId)
            {
                if (!lootIdSet.Contains(lootId))
                    Gameobject.ReportNonExistingId(lootId, gameObjectId);
                else
                    lootIdSetUsed.Add(lootId);
            }

            // remove real entries and check existence loot
            var gotc = Global.ObjectMgr.GetGameObjectTemplates();
            foreach (var (gameObjectId, gameObjectTemplate) in gotc)
            {
                uint lootid = gameObjectTemplate.GetLootId();
                if (lootid != 0)
                    checkLootId(lootid, gameObjectId);

                if (gameObjectTemplate.type == GameObjectTypes.Chest)
                {
                    if (gameObjectTemplate.Chest.chestPersonalLoot != 0)
                        checkLootId(gameObjectTemplate.Chest.chestPersonalLoot, gameObjectId);

                    if (gameObjectTemplate.Chest.chestPushLoot != 0)
                        checkLootId(gameObjectTemplate.Chest.chestPushLoot, gameObjectId);
                }
            }

            foreach (var lootId in lootIdSetUsed)
                lootIdSet.Remove(lootId);

            // output error for any still listed (not referenced from appropriate table) ids
            Gameobject.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} gameobject loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 gameobject loot templates. DB table `gameobject_loot_template` is empty");
        }

        public static void LoadLootTemplates_Item()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading item loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Items.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var (itemId, itemTemplate) in its)
                if (itemTemplate.HasFlag(ItemFlags.HasLoot))
                    if (!lootIdSet.Remove(itemId))
                        Items.ReportNonExistingId(itemId, itemId);

            // output error for any still listed (not referenced from appropriate table) ids
            Items.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} item loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 item loot templates. DB table `item_loot_template` is empty");
        }

        public static void LoadLootTemplates_Milling()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading milling loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Milling.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var (itemId, itemTemplate) in its)
                if (!itemTemplate.HasFlag(ItemFlags.IsMillable))
                    if (!lootIdSet.Remove(itemId))
                        Milling.ReportNonExistingId(itemId, itemId);

            // output error for any still listed (not referenced from appropriate table) ids
            Milling.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} milling loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 milling loot templates. DB table `milling_loot_template` is empty");
        }

        public static void LoadLootTemplates_Pickpocketing()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading pickpocketing loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            List<uint> lootIdSetUsed = new();
            uint count = Pickpocketing.LoadAndCollectLootIds(out lootIdSet);

            // Remove real entries and check loot existence
            var templates = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var (creatureId, creatureTemplate) in templates)
            {
                foreach (var (_, creatureDifficulty) in creatureTemplate.difficultyStorage)
                {
                    if (creatureDifficulty.PickPocketLootID != 0)
                    {
                        if (!lootIdSet.Contains(creatureDifficulty.PickPocketLootID))
                            Pickpocketing.ReportNonExistingId(creatureDifficulty.PickPocketLootID, creatureId);
                        else
                            lootIdSetUsed.Add(creatureDifficulty.PickPocketLootID);
                    }
                }
            }

            foreach (var lootId in lootIdSetUsed)
                lootIdSet.Remove(lootId);

            // output error for any still listed (not referenced from appropriate table) ids
            Pickpocketing.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pickpocketing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 pickpocketing loot templates. DB table `pickpocketing_loot_template` is empty");
        }

        public static void LoadLootTemplates_Prospecting()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading prospecting loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Prospecting.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var (itemId, itemTemplate) in its)
                if (itemTemplate.HasFlag(ItemFlags.IsProspectable))
                    if (!lootIdSet.Remove(itemId))
                        Prospecting.ReportNonExistingId(itemId, itemId);

            // output error for any still listed (not referenced from appropriate table) ids
            Prospecting.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} prospecting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 prospecting loot templates. DB table `prospecting_loot_template` is empty");
        }

        public static void LoadLootTemplates_Mail()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading mail loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Mail.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            foreach (var (_, mailTemplate) in CliDB.MailTemplateStorage)
                lootIdSet.Remove(mailTemplate.Id);

            // output error for any still listed (not referenced from appropriate table) ids
            Mail.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} mail loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 mail loot templates. DB table `mail_loot_template` is empty");
        }

        public static void LoadLootTemplates_Skinning()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading skinning loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            List<uint> lootIdSetUsed = new();
            uint count = Skinning.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var templates = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var (creatureId, creatureTemplate) in templates)
            {
                foreach (var (_, creatureDifficulty) in creatureTemplate.difficultyStorage)
                {
                    if (creatureDifficulty.SkinLootID != 0)
                    {
                        if (!lootIdSet.Contains(creatureDifficulty.SkinLootID))
                            Skinning.ReportNonExistingId(creatureDifficulty.SkinLootID, creatureId);
                        else
                            lootIdSetUsed.Add(creatureDifficulty.SkinLootID);
                    }
                }
            }

            foreach (var lootId in lootIdSetUsed)
                lootIdSet.Remove(lootId);

            // output error for any still listed (not referenced from appropriate table) ids
            Skinning.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} skinning loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 skinning loot templates. DB table `skinning_loot_template` is empty");
        }

        public static void LoadLootTemplates_Spell()
        {
            // TODO: change this to use MiscValue from spell effect as id instead of spell id
            Log.outInfo(LogFilter.ServerLoading, "Loading spell loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            List<uint> lootIdSetUsed = new();
            uint count = Spell.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            Global.SpellMgr.ForEachSpellInfo(spellInfo =>
            {
                // possible cases
                if (!spellInfo.IsLootCrafting())
                    return;

                if (!lootIdSet.Contains(spellInfo.Id))
                {
                    // not report about not trainable spells (optionally supported by DB)
                    // ignore 61756 (Northrend Inscription Research (FAST QA VERSION) for example
                    if (!spellInfo.HasAttribute(SpellAttr0.NotShapeshifted) || spellInfo.HasAttribute(SpellAttr0.IsTradeskill))
                    {
                        Spell.ReportNonExistingId(spellInfo.Id, spellInfo.Id);
                    }
                }
                else
                    lootIdSetUsed.Add(spellInfo.Id);
            });

            foreach (uint lootId in lootIdSetUsed)
                lootIdSet.Remove(lootId);

            // output error for any still listed (not referenced from appropriate table) ids
            Spell.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outInfo(LogFilter.ServerLoading, "Loaded 0 spell loot templates. DB table `spell_loot_template` is empty");
        }

        public static void LoadLootTemplates_Reference()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading reference loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            Reference.LoadAndCollectLootIds(out lootIdSet);

            // check references and remove used
            Creature.CheckLootRefs(lootIdSet);
            Fishing.CheckLootRefs(lootIdSet);
            Gameobject.CheckLootRefs(lootIdSet);
            Items.CheckLootRefs(lootIdSet);
            Milling.CheckLootRefs(lootIdSet);
            Pickpocketing.CheckLootRefs(lootIdSet);
            Skinning.CheckLootRefs(lootIdSet);
            Disenchant.CheckLootRefs(lootIdSet);
            Prospecting.CheckLootRefs(lootIdSet);
            Mail.CheckLootRefs(lootIdSet);
            Reference.CheckLootRefs(lootIdSet);

            // output error for any still listed ids (not referenced from any loot table)
            Reference.ReportUnusedIds(lootIdSet);

            Log.outInfo(LogFilter.ServerLoading, "Loaded reference loot templates in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }
    }

    public class LootStoreItem
    {
        public uint itemid;                 // id of the item
        public LootStoreItemType type;
        public float chance;                // chance to drop for both quest and non-quest items, chance to be used for refs
        public ushort lootmode;
        public bool needs_quest;            // quest drop (negative ChanceOrQuestChance in DB)
        public byte groupid;
        public byte mincount;               // mincount for drop items
        public byte maxcount;               // max drop count for the item mincount or Ref multiplicator
        public ConditionsReference conditions;  // additional loot condition

        public LootStoreItem(uint _itemid, LootStoreItemType _type, float _chance, bool _needs_quest, ushort _lootmode, byte _groupid, byte _mincount, byte _maxcount)
        {
            itemid = _itemid;
            type = _type;
            chance = _chance;
            lootmode = _lootmode;
            needs_quest = _needs_quest;
            groupid = _groupid;
            mincount = _mincount;
            maxcount = _maxcount;
        }

        public bool Roll(bool rate)
        {
            if (chance >= 100.0f)
                return true;

            switch (type)
            {
                case LootStoreItemType.Item:
                {
                    ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);
                    float qualityModifier = pProto != null && rate && LootStoreItem.QualityToRate[(int)pProto.GetQuality()] != WorldCfg.Max ? WorldConfig.GetFloatValue(LootStoreItem.QualityToRate[(int)pProto.GetQuality()]) : 1.0f;
                    return RandomHelper.randChance(chance * qualityModifier);
                }
                case LootStoreItemType.Reference:
                    return RandomHelper.randChance(chance * (rate ? WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferenced) : 1.0f));
                case LootStoreItemType.Currency:
                {
                    CurrencyTypesRecord currency = CliDB.CurrencyTypesStorage.LookupByKey(itemid);

                    float qualityModifier = currency != null && rate && QualityToRate[currency.Quality] != WorldCfg.Max ? WorldConfig.GetFloatValue(QualityToRate[currency.Quality]) : 1.0f;

                    return RandomHelper.randChance(chance * qualityModifier);
                }
                default:
                    break;
            }

            return false;
        }

        public bool IsValid(LootStore store, uint entry)
        {
            if (mincount == 0)
            {
                Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: wrong mincount ({mincount}) - skipped");
                return false;
            }

            switch (type)
            {
                case LootStoreItemType.Item:
                    ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemid);
                    if (proto == null)
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: item does not exist - skipped");
                        return false;
                    }

                    if (chance == 0 && groupid == 0)                      // Zero chance is allowed for grouped entries only
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: equal-chanced grouped entry, but group not defined - skipped");
                        return false;
                    }

                    if (chance != 0 && chance < 0.000001f)             // loot with low chance
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: low chance ({chance}) - skipped");
                        return false;
                    }

                    if (maxcount < mincount)                       // wrong max count
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: max count ({maxcount}) less that min count ({mincount}) - skipped");
                        return false;
                    }
                    break;
                case LootStoreItemType.Reference:
                    if (needs_quest)
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: quest chance will be treated as non-quest chance");
                    else if (chance == 0)                              // no chance for the reference
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: zero chance is specified for a reference, skipped");
                        return false;
                    }
                    break;
                case LootStoreItemType.Currency:
                {
                    if (!CliDB.CurrencyTypesStorage.HasRecord(itemid))
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: currency does not exist - skipped");
                        return false;
                    }

                    if (chance == 0 && groupid == 0)                // Zero chance is allowed for grouped entries only
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: equal-chanced grouped entry, but group not defined - skipped");
                        return false;
                    }

                    if (chance != 0 && chance < 0.0001f)            // loot with low chance
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: low chance ({chance}) - skipped");
                        return false;
                    }

                    if (maxcount < mincount)                        // wrong max count
                    {
                        Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} ItemType {type} Item {itemid}: MaxCount ({maxcount}) less that MinCount ({mincount}) - skipped");
                        return false;
                    }
                    break;
                }
                default:
                    Log.outError(LogFilter.Sql, $"Table '{store.GetName()}' Entry {entry} Item {itemid}: invalid ItemType {type}, skipped");
                    return false;
            }
            return true;                                            // Referenced template existence is checked at whole store level
        }

        public static WorldCfg[] QualityToRate =
        [
            WorldCfg.RateDropItemPoor,                                    // ITEM_QUALITY_POOR
            WorldCfg.RateDropItemNormal,                                  // ITEM_QUALITY_NORMAL
            WorldCfg.RateDropItemUncommon,                                // ITEM_QUALITY_UNCOMMON
            WorldCfg.RateDropItemRare,                                    // ITEM_QUALITY_RARE
            WorldCfg.RateDropItemEpic,                                    // ITEM_QUALITY_EPIC
            WorldCfg.RateDropItemLegendary,                               // ITEM_QUALITY_LEGENDARY
            WorldCfg.RateDropItemArtifact,                                // ITEM_QUALITY_ARTIFACT
            WorldCfg.Max,                                                 // ITEM_QUALITY_HEIRLOOM
            WorldCfg.Max,                                                 // ITEM_QUALITY_WOW_TOKEN
        ];
    }

    public class LootStore
    {
        public LootStore(string name, string entryName, bool ratesAllowed = true)
        {
            m_name = name;
            m_entryName = entryName;
            m_ratesAllowed = ratesAllowed;
        }

        void Verify()
        {
            foreach (var (lootId, lootTemplate) in m_LootTemplates)
                lootTemplate.Verify(this, lootId);
        }

        public uint LoadAndCollectLootIds(out List<uint> lootIdSet)
        {
            uint count = LoadLootTable();
            lootIdSet = new(m_LootTemplates.Select(tab => tab.Key));

            return count;
        }

        public void CheckLootRefs(List<uint> ref_set = null)
        {
            foreach (var (_, lootTemplate) in m_LootTemplates)
                lootTemplate.CheckLootRefs(m_LootTemplates, ref_set);
        }

        public void ReportUnusedIds(List<uint> lootIdSet)
        {
            // all still listed ids isn't referenced
            foreach (var lootId in lootIdSet)
                Log.outError(LogFilter.Sql, $"Table '{GetName()}' entry {lootId} isn't {GetEntryName()} and not referenced from loot, and then useless.");
        }

        public void ReportNonExistingId(uint lootId, uint ownerId)
        {
            Log.outError(LogFilter.Sql, "Table '{0}' Entry {1} does not exist but it is used by {2} {3}", GetName(), lootId, GetEntryName(), ownerId);
        }

        public bool HaveLootFor(uint loot_id) { return m_LootTemplates.ContainsKey(loot_id); }

        public bool HaveQuestLootFor(uint loot_id)
        {
            // scan loot for quest items
            LootTemplate lootTemplate = m_LootTemplates.LookupByKey(loot_id);
            if (lootTemplate != null)
                return lootTemplate.HasQuestDrop(m_LootTemplates);

            return false;
        }

        public bool HaveQuestLootForPlayer(uint loot_id, Player player)
        {
            LootTemplate lootTemplate = m_LootTemplates.LookupByKey(loot_id);
            if (lootTemplate != null && lootTemplate.HasQuestDropForPlayer(m_LootTemplates, player))
                return true;

            return false;
        }

        public LootTemplate GetLootFor(uint loot_id)
        {
            return m_LootTemplates.LookupByKey(loot_id);
        }

        public LootTemplate GetLootForConditionFill(uint loot_id)
        {
            return m_LootTemplates.LookupByKey(loot_id);
        }

        public string GetName() { return m_name; }
        string GetEntryName() { return m_entryName; }
        public bool IsRatesAllowed() { return m_ratesAllowed; }

        uint LoadLootTable()
        {
            // Clearing store (for reloading case)
            Clear();

            //                                          0      1         2     3       4              5         6        7         8
            SQLResult result = DB.World.Query($"SELECT Entry, ItemType, Item, Chance, QuestRequired, LootMode, GroupId, MinCount, MaxCount FROM {GetName()}");
            if (result.IsEmpty())
                return 0;

            uint count = 0;
            do
            {
                uint entry = result.Read<uint>(0);
                LootStoreItemType type = (LootStoreItemType)result.Read<sbyte>(1);
                uint item = result.Read<uint>(2);
                float chance = result.Read<float>(3);
                bool needsquest = result.Read<bool>(4);
                ushort lootmode = result.Read<ushort>(5);
                byte groupid = result.Read<byte>(6);
                byte mincount = result.Read<byte>(7);
                byte maxcount = result.Read<byte>(8);

                LootStoreItem storeitem = new(item, type, chance, needsquest, lootmode, groupid, mincount, maxcount);

                if (!storeitem.IsValid(this, entry))            // Validity checks
                    continue;

                // Looking for the template of the entry
                // often entries are put together
                if (m_LootTemplates.Empty() || !m_LootTemplates.ContainsKey(entry))
                    m_LootTemplates.TryAdd(entry, new LootTemplate());

                // Adds current row to the template
                m_LootTemplates[entry].AddEntry(storeitem);
                ++count;
            }
            while (result.NextRow());

            Verify();                                           // Checks validity of the loot store

            return count;
        }
        void Clear()
        {
            m_LootTemplates.Clear();
        }

        LootTemplateMap m_LootTemplates = new();
        string m_name;
        string m_entryName;
        bool m_ratesAllowed;
    }

    public class LootTemplate
    {
        public void AddEntry(LootStoreItem item)
        {
            if (item.groupid > 0 && item.type != LootStoreItemType.Reference)  // Group
            {
                if (!Groups.ContainsKey(item.groupid - 1))
                    Groups[item.groupid - 1] = new LootGroup();

                Groups[item.groupid - 1].AddEntry(item);              // Adds new entry to the group
            }
            else                                                    // Non-grouped entries and references are stored together
                Entries.Add(item);
        }

        public void Process(Loot loot, bool rate, ushort lootMode, byte groupId, Player personalLooter = null)
        {
            if (groupId != 0)                                            // Group reference uses own processing of the group
            {
                if (groupId > Groups.Count)
                    return;                                         // Error message already printed at loading stage

                if (Groups[groupId - 1] == null)
                    return;

                Groups[groupId - 1].Process(loot, lootMode, personalLooter);
                return;
            }

            // Rolling non-grouped items
            foreach (var item in Entries)
            {
                if ((item.lootmode & lootMode) == 0)                       // Do not add if mode mismatch
                    continue;

                if (!item.Roll(rate))
                    continue;                                           // Bad luck for the entry

                switch (item.type)
                {
                    case LootStoreItemType.Item:
                    case LootStoreItemType.Currency:
                        // Plain entries (not a reference, not grouped)
                        // Chance is already checked, just add
                        if (personalLooter == null || LootItem.AllowedForPlayer(personalLooter, item, true))
                            loot.AddItem(item);
                        break;
                    case LootStoreItemType.Reference:
                        LootTemplate Referenced = LootStorage.Reference.GetLootFor(item.itemid);
                        if (Referenced == null)
                            continue;                                       // Error message already printed at loading stage

                        uint maxcount = (uint)(item.maxcount * WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferencedAmount));
                        for (uint loop = 0; loop < maxcount; ++loop)      // Ref multiplicator
                            Referenced.Process(loot, rate, lootMode, item.groupid, personalLooter);

                        break;
                    default:
                        break;
                }
            }

            // Now processing groups
            foreach (var (_, group) in Groups)
                group?.Process(loot, lootMode, personalLooter);
        }

        public void ProcessPersonalLoot(Dictionary<Player, Loot> personalLoot, bool rate, ushort lootMode)
        {
            List<Player> getLootersForItem(Func<Player, bool> predicate)
            {
                List<Player> lootersForItem = new();
                foreach (var (looter, loot) in personalLoot)
                {
                    if (predicate(looter))
                        lootersForItem.Add(looter);
                }
                return lootersForItem;
            }

            // Rolling non-grouped items
            foreach (LootStoreItem item in Entries)
            {
                if ((item.lootmode & lootMode) == 0)                       // Do not add if mode mismatch
                    continue;

                if (!item.Roll(rate))
                    continue;                                           // Bad luck for the entry

                switch (item.type)
                {
                    case LootStoreItemType.Item:
                    {
                        // Plain entries (not a reference, not grouped)
                        // Chance is already checked, just add
                        var lootersForItem = getLootersForItem(looter => LootItem.AllowedForPlayer(looter, item, true));
                        if (!lootersForItem.Empty())
                        {
                            Player chosenLooter = lootersForItem.SelectRandom();
                            personalLoot[chosenLooter].AddItem(item);
                        }
                        break;
                    }
                    case LootStoreItemType.Reference:
                    {
                        LootTemplate referenced = LootStorage.Reference.GetLootFor(item.itemid);
                        if (referenced == null)
                            continue;                                       // Error message already printed at loading stage

                        uint maxcount = (uint)((float)item.maxcount * WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferencedAmount));
                        List<Player> gotLoot = new();
                        for (uint loop = 0; loop < maxcount; ++loop)      // Ref multiplicator
                        {
                            var lootersForItem = getLootersForItem(looter => referenced.HasDropForPlayer(looter, item.groupid, true));

                            // nobody can loot this, skip it
                            if (lootersForItem.Empty())
                                break;

                            var newEnd = lootersForItem.RemoveAll(looter => gotLoot.Contains(looter));

                            if (lootersForItem.Count == newEnd)
                            {
                                // if we run out of looters this means that there are more items dropped than players
                                // start a new cycle adding one item to everyone
                                gotLoot.Clear();
                            }
                            else
                                lootersForItem.RemoveRange(newEnd, lootersForItem.Count - newEnd);

                            Player chosenLooter = lootersForItem.SelectRandom();
                            referenced.Process(personalLoot[chosenLooter], rate, lootMode, item.groupid, chosenLooter);
                            gotLoot.Add(chosenLooter);
                        }
                        break;
                    }
                    case LootStoreItemType.Currency:
                    {
                        // Plain entries (not a reference, not grouped)
                        // Chance is already checked, just add
                        var lootersForItem = getLootersForItem(looter => LootItem.AllowedForPlayer(looter, item, true));

                        foreach (Player looter in lootersForItem)
                            personalLoot[looter].AddItem(item);
                        break;
                    }
                    default:
                        break;
                }

                // Now processing groups
                foreach (LootGroup group in Groups.Values)
                {
                    if (group != null)
                    {
                        var lootersForGroup = getLootersForItem(looter => group.HasDropForPlayer(looter, true));

                        if (!lootersForGroup.Empty())
                        {
                            Player chosenLooter = lootersForGroup.SelectRandom();
                            group.Process(personalLoot[chosenLooter], lootMode);
                        }
                    }
                }
            }
        }

        // True if template includes at least 1 drop for the player
        bool HasDropForPlayer(Player player, byte groupId, bool strictUsabilityCheck)
        {
            if (groupId != 0)                                            // Group reference
            {
                if (groupId > Groups.Count)
                    return false;                                   // Error message already printed at loading stage

                if (Groups[groupId - 1] == null)
                    return false;

                return Groups[groupId - 1].HasDropForPlayer(player, strictUsabilityCheck);
            }

            // Checking non-grouped entries
            foreach (LootStoreItem lootStoreItem in Entries)
            {
                switch (lootStoreItem.type)
                {
                    case LootStoreItemType.Item:
                        if (LootItem.AllowedForPlayer(player, lootStoreItem, strictUsabilityCheck))
                            return true;                                    // active quest drop found
                        break;
                    case LootStoreItemType.Reference:
                        LootTemplate referenced = LootStorage.Reference.GetLootFor(lootStoreItem.itemid);
                        if (referenced == null)
                            continue;                                   // Error message already printed at loading stage
                        if (referenced.HasDropForPlayer(player, lootStoreItem.groupid, strictUsabilityCheck))
                            return true;
                        break;
                    default:
                        break;
                }
            }

            // Now checking groups
            foreach (var (_, group) in Groups)
                if (group != null && group.HasDropForPlayer(player, strictUsabilityCheck))
                    return true;

            return false;
        }

        public void CopyConditions(LootItem li)
        {
            // Copies the conditions list from a template item to a LootItem
            foreach (var item in Entries)
            {
                switch (item.type)
                {
                    case LootStoreItemType.Item:
                        if (li.type != LootItemType.Item)
                            continue;
                        break;
                    case LootStoreItemType.Reference:
                        continue;
                    case LootStoreItemType.Currency:
                        if (li.type != LootItemType.Currency)
                            continue;
                        break;
                    default:
                        break;
                }

                if (item.itemid != li.itemid)
                    continue;

                li.conditions = item.conditions;
                break;
            }
        }

        public bool HasQuestDrop(LootTemplateMap store, byte groupId = 0)
        {
            if (groupId != 0)                                            // Group reference
            {
                if (groupId > Groups.Count)
                    return false;                                   // Error message [should be] already printed at loading stage

                if (Groups[groupId - 1] == null)
                    return false;

                return Groups[groupId - 1].HasQuestDrop();
            }

            foreach (var item in Entries)
            {
                switch (item.type)
                {
                    case LootStoreItemType.Item:
                    case LootStoreItemType.Currency:
                        if (item.needs_quest)
                            return true;                                    // quest drop found
                        break;
                    case LootStoreItemType.Reference:
                        var Referenced = store.LookupByKey(item.itemid);
                        if (Referenced == null)
                            continue;                                   // Error message [should be] already printed at loading stage
                        if (Referenced.HasQuestDrop(store, item.groupid))
                            return true;
                        break;
                    default:
                        break;
                }
            }

            // Now processing groups
            foreach (var (_, group) in Groups)
                if (group != null && group.HasQuestDrop())
                    return true;

            return false;
        }

        public bool HasQuestDropForPlayer(LootTemplateMap store, Player player, byte groupId = 0)
        {
            if (groupId != 0)                                            // Group reference
            {
                if (groupId > Groups.Count)
                    return false;                                   // Error message already printed at loading stage

                if (Groups[groupId - 1] == null)
                    return false;

                return Groups[groupId - 1].HasQuestDropForPlayer(player);
            }

            // Checking non-grouped entries
            foreach (var item in Entries)
            {
                switch (item.type)
                {
                    case LootStoreItemType.Item:
                        if (player.HasQuestForItem(item.itemid))
                            return true;                                    // active quest drop found
                        break;
                    case LootStoreItemType.Reference:
                        var Referenced = store.LookupByKey(item.itemid);
                        if (Referenced == null)
                            continue;                                   // Error message already printed at loading stage
                        if (Referenced.HasQuestDropForPlayer(store, player, item.groupid))
                            return true;
                        break;
                    case LootStoreItemType.Currency:
                        if (player.HasQuestForCurrency(item.itemid))
                            return true;                            // active quest drop found
                        break;
                    default:
                        break;
                }
            }

            // Now checking groups
            foreach (var (_, group) in Groups)
                if (group != null && group.HasQuestDropForPlayer(player))
                    return true;

            return false;
        }

        public void Verify(LootStore lootstore, uint id)
        {
            // Checking group chances
            foreach (var group in Groups)
                group.Value.Verify(lootstore, id, (byte)(group.Key + 1));

            // @todo References validity checks
        }
        public void CheckLootRefs(LootTemplateMap store, List<uint> ref_set)
        {
            foreach (var item in Entries)
            {
                if (item.type == LootStoreItemType.Reference)
                {
                    if (LootStorage.Reference.GetLootFor(item.itemid) == null)
                        LootStorage.Reference.ReportNonExistingId(item.itemid, item.itemid);
                    else if (ref_set != null)
                        ref_set.Remove(item.itemid);
                }
            }

            foreach (var (_, group) in Groups)
                group?.CheckLootRefs(store, ref_set);
        }

        public bool LinkConditions(ConditionId id, ConditionsReference reference)
        {
            if (!Entries.Empty())
            {
                foreach (var item in Entries)
                {
                    if (item.itemid == id.SourceEntry)
                    {
                        item.conditions = reference;
                        return true;
                    }
                }
            }

            if (!Groups.Empty())
            {
                foreach (var (_, group) in Groups)
                {
                    if (group == null)
                        continue;

                    LootStoreItemList itemList = group.GetExplicitlyChancedItemList();
                    if (!itemList.Empty())
                    {
                        foreach (var item in itemList)
                        {
                            if (item.itemid == id.SourceEntry)
                            {
                                item.conditions = reference;
                                return true;
                            }
                        }
                    }

                    itemList = group.GetEqualChancedItemList();
                    if (!itemList.Empty())
                    {
                        foreach (var item in itemList)
                        {
                            if (item.itemid == id.SourceEntry)
                            {
                                item.conditions = reference;
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        LootStoreItemList Entries = new();                          // not grouped only
        Dictionary<int, LootGroup> Groups = new();                           // groups have own (optimised) processing, grouped entries go there

        public class LootGroup                               // A set of loot definitions for items (refs are not allowed)
        {
            public void AddEntry(LootStoreItem item)
            {
                if (item.chance != 0)
                    ExplicitlyChanced.Add(item);
                else
                    EqualChanced.Add(item);
            }

            public bool HasQuestDrop()
            {
                if (ExplicitlyChanced.Any(item => item.needs_quest))
                    return true;

                if (EqualChanced.Any(item => item.needs_quest))
                    return true;

                return false;
            }

            public bool HasQuestDropForPlayer(Player player)
            {
                var hasQuestForLootItem = (LootStoreItem item) =>
                {
                    switch (item.type)
                    {
                        case LootStoreItemType.Item:
                            return player.HasQuestForItem(item.itemid);
                        case LootStoreItemType.Currency:
                            return player.HasQuestForCurrency(item.itemid);
                        default:
                            break;
                    }
                    return false;
                };

                if (ExplicitlyChanced.Any(hasQuestForLootItem))
                    return true;

                if (EqualChanced.Any(hasQuestForLootItem))
                    return true;

                return false;
            }

            public void Process(Loot loot, ushort lootMode, Player personalLooter = null)
            {
                LootStoreItem item = Roll(lootMode, personalLooter);
                if (item != null)
                    loot.AddItem(item);
            }

            float RawTotalChance()
            {
                float result = 0;

                foreach (var item in ExplicitlyChanced)
                    if (!item.needs_quest)
                        result += item.chance;

                return result;
            }

            float TotalChance()
            {
                float result = RawTotalChance();

                if (!EqualChanced.Empty() && result < 100.0f)
                    return 100.0f;

                return result;
            }

            public void Verify(LootStore lootstore, uint id, byte group_id = 0)
            {
                float chance = RawTotalChance();
                if (chance > 101.0f)                                    // @todo replace with 100% when DBs will be ready
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} group {2} has total chance > 100% ({3})", lootstore.GetName(), id, group_id, chance);

                if (chance >= 100.0f && !EqualChanced.Empty())
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} group {2} has items with chance=0% but group total chance >= 100% ({3})", lootstore.GetName(), id, group_id, chance);

            }

            public void CheckLootRefs(LootTemplateMap store, List<uint> ref_set)
            {
                foreach (var item in ExplicitlyChanced)
                {
                    if (item.type == LootStoreItemType.Reference)
                    {
                        if (LootStorage.Reference.GetLootFor(item.itemid) == null)
                            LootStorage.Reference.ReportNonExistingId(item.itemid, item.itemid);
                        else if (ref_set != null)
                            ref_set.Remove(item.itemid);
                    }
                }

                foreach (var item in EqualChanced)
                {
                    if (item.type == LootStoreItemType.Reference)
                    {
                        if (LootStorage.Reference.GetLootFor(item.itemid) == null)
                            LootStorage.Reference.ReportNonExistingId(item.itemid, item.itemid);
                        else if (ref_set != null)
                            ref_set.Remove(item.itemid);
                    }
                }
            }

            public LootStoreItemList GetExplicitlyChancedItemList() { return ExplicitlyChanced; }

            public LootStoreItemList GetEqualChancedItemList() { return EqualChanced; }

            LootStoreItemList ExplicitlyChanced = new();                // Entries with chances defined in DB
            LootStoreItemList EqualChanced = new();                     // Zero chances - every entry takes the same chance

            LootStoreItem Roll(ushort lootMode, Player personalLooter = null)
            {
                LootStoreItemList getValidLoot(LootStoreItemList items, ushort lootMode, Player personalLooter)
                {
                    LootStoreItemList possibleLoot = new(items);
                    possibleLoot.RemoveAll(new LootGroupInvalidSelector(lootMode, personalLooter).Check);
                    return possibleLoot;
                }

                var possibleLoot = getValidLoot(ExplicitlyChanced, lootMode, personalLooter);
                if (!possibleLoot.Empty())                             // First explicitly chanced entries are checked
                {
                    float roll = (float)RandomHelper.randChance();

                    foreach (var item in possibleLoot)   // check each explicitly chanced entry in the template and modify its chance based on quality.
                    {
                        if (item.chance >= 100.0f)
                            return item;

                        roll -= item.chance;
                        if (roll < 0)
                            return item;
                    }
                }

                possibleLoot = getValidLoot(EqualChanced, lootMode, personalLooter);
                if (!possibleLoot.Empty())                              // If nothing selected yet - an item is taken from equal-chanced part
                    return possibleLoot.SelectRandom();

                return null;                                            // Empty drop from the group
            }

            public bool HasDropForPlayer(Player player, bool strictUsabilityCheck)
            {
                foreach (LootStoreItem lootStoreItem in ExplicitlyChanced)
                    if (LootItem.AllowedForPlayer(player, lootStoreItem, strictUsabilityCheck))
                        return true;

                foreach (LootStoreItem lootStoreItem in EqualChanced)
                    if (LootItem.AllowedForPlayer(player, lootStoreItem, strictUsabilityCheck))
                        return true;

                return false;
            }
        }
    }

    public struct LootGroupInvalidSelector
    {
        public LootGroupInvalidSelector(ushort lootMode, Player personalLooter)
        {
            _lootMode = lootMode;
            _personalLooter = personalLooter;
        }

        public bool Check(LootStoreItem item)
        {
            if ((item.lootmode & _lootMode) == 0)
                return true;

            if (_personalLooter != null && !LootItem.AllowedForPlayer(_personalLooter, item, true))
                return true;

            return false;
        }

        ushort _lootMode;
        Player _personalLooter;
    }
}
