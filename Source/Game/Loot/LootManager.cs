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

using Framework.Constants;
using Framework.Database;
using Game.Conditions;
using Game.DataStorage;
using Game.Entities;
using System;
using System.Collections.Generic;

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

        public static void LoadLootTemplates_Creature()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading creature loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet, lootIdSetUsed = new List<uint>();
            uint count = Creature.LoadAndCollectLootIds(out lootIdSet);

            // Remove real entries and check loot existence
            var ctc = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var pair in ctc)
            {
                uint lootid = pair.Value.LootId;
                if (lootid != 0)
                {
                    if (!lootIdSet.Contains(lootid))
                        Creature.ReportNonExistingId(lootid, pair.Value.Entry);
                    else
                        lootIdSetUsed.Add(lootid);
                }
            }

            foreach (var id in lootIdSetUsed)
                lootIdSet.Remove(id);

            // output error for any still listed (not referenced from appropriate table) ids
            Creature.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} creature loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 creature loot templates. DB table `creature_loot_template` is empty");
        }

        public static void LoadLootTemplates_Disenchant()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading disenchanting loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet, lootIdSetUsed = new List<uint>();
            uint count = Disenchant.LoadAndCollectLootIds(out lootIdSet);

            foreach (var disenchant in CliDB.ItemDisenchantLootStorage.Values)
            {
                uint lootid = disenchant.Id;
                if (!lootIdSet.Contains(lootid))
                    Disenchant.ReportNonExistingId(lootid, disenchant.Id);
                else
                    lootIdSetUsed.Add(lootid);
            }

            foreach (var id in lootIdSetUsed)
                lootIdSet.Remove(id);

            // output error for any still listed (not referenced from appropriate table) ids
            Disenchant.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} disenchanting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 disenchanting loot templates. DB table `disenchant_loot_template` is empty");
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
                Log.outError(LogFilter.ServerLoading, "Loaded 0 fishing loot templates. DB table `fishing_loot_template` is empty");
        }

        public static void LoadLootTemplates_Gameobject()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading gameobject loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet, lootIdSetUsed = new List<uint>();
            uint count = Gameobject.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var gotc = Global.ObjectMgr.GetGameObjectTemplates();
            foreach (var go in gotc)
            {
                uint lootid = go.Value.GetLootId();
                if (lootid != 0)
                {
                    if (!lootIdSet.Contains(lootid))
                        Gameobject.ReportNonExistingId(lootid, go.Value.entry);
                    else
                        lootIdSetUsed.Add(lootid);
                }
            }

            foreach (var id in lootIdSetUsed)
                lootIdSet.Remove(id);

            // output error for any still listed (not referenced from appropriate table) ids
            Gameobject.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} gameobject loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 gameobject loot templates. DB table `gameobject_loot_template` is empty");
        }

        public static void LoadLootTemplates_Item()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading item loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Items.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var pair in its)
                if (lootIdSet.Contains(pair.Value.GetId()) && pair.Value.GetFlags().HasAnyFlag(ItemFlags.HasLoot))
                    lootIdSet.Remove(pair.Value.GetId());

            // output error for any still listed (not referenced from appropriate table) ids
            Items.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} item loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 item loot templates. DB table `item_loot_template` is empty");
        }

        public static void LoadLootTemplates_Milling()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading milling loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Milling.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var pair in its)
            {
                if (!pair.Value.GetFlags().HasAnyFlag(ItemFlags.IsMillable))
                    continue;

                if (lootIdSet.Contains(pair.Value.GetId()))
                    lootIdSet.Remove(pair.Value.GetId());
            }

            // output error for any still listed (not referenced from appropriate table) ids
            Milling.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} milling loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 milling loot templates. DB table `milling_loot_template` is empty");
        }

        public static void LoadLootTemplates_Pickpocketing()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading pickpocketing loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            List<uint> lootIdSetUsed = new List<uint>();
            uint count = Pickpocketing.LoadAndCollectLootIds(out lootIdSet);

            // Remove real entries and check loot existence
            var ctc = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var pair in ctc)
            {
                uint lootid = pair.Value.PickPocketId;
                if (lootid != 0)
                {
                    if (!lootIdSet.Contains(lootid))
                        Pickpocketing.ReportNonExistingId(lootid, pair.Value.Entry);
                    else
                        lootIdSetUsed.Add(lootid);
                }
            }

            foreach (var id in lootIdSetUsed)
                lootIdSet.Remove(id);

            // output error for any still listed (not referenced from appropriate table) ids
            Pickpocketing.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} pickpocketing loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 pickpocketing loot templates. DB table `pickpocketing_loot_template` is empty");
        }

        public static void LoadLootTemplates_Prospecting()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading prospecting loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Prospecting.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var its = Global.ObjectMgr.GetItemTemplates();
            foreach (var pair in its)
            {
                if (!pair.Value.GetFlags().HasAnyFlag(ItemFlags.IsProspectable))
                    continue;

                if (lootIdSet.Contains(pair.Value.GetId()))
                    lootIdSet.Remove(pair.Value.GetId());
            }

            // output error for any still listed (not referenced from appropriate table) ids
            Prospecting.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} prospecting loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 prospecting loot templates. DB table `prospecting_loot_template` is empty");
        }

        public static void LoadLootTemplates_Mail()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading mail loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Mail.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            foreach (var mail in CliDB.MailTemplateStorage.Values)
                if (lootIdSet.Contains(mail.Id))
                    lootIdSet.Remove(mail.Id);

            // output error for any still listed (not referenced from appropriate table) ids
            Mail.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} mail loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 mail loot templates. DB table `mail_loot_template` is empty");
        }

        public static void LoadLootTemplates_Skinning()
        {
            Log.outInfo(LogFilter.ServerLoading, "Loading skinning loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            List<uint> lootIdSetUsed = new List<uint>();
            uint count = Skinning.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            var ctc = Global.ObjectMgr.GetCreatureTemplates();
            foreach (var pair in ctc)
            {
                uint lootid = pair.Value.SkinLootId;
                if (lootid != 0)
                {
                    if (!lootIdSet.Contains(lootid))
                        Skinning.ReportNonExistingId(lootid, pair.Value.Entry);
                    else
                        lootIdSetUsed.Add(lootid);
                }
            }

            foreach (var id in lootIdSetUsed)
                lootIdSet.Remove(id);

            // output error for any still listed (not referenced from appropriate table) ids
            Skinning.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} skinning loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 skinning loot templates. DB table `skinning_loot_template` is empty");
        }

        public static void LoadLootTemplates_Spell()
        {
            // TODO: change this to use MiscValue from spell effect as id instead of spell id
            Log.outInfo(LogFilter.ServerLoading, "Loading spell loot templates...");

            uint oldMSTime = Time.GetMSTime();

            List<uint> lootIdSet;
            uint count = Spell.LoadAndCollectLootIds(out lootIdSet);

            // remove real entries and check existence loot
            foreach (var spellInfo in Global.SpellMgr.GetSpellInfoStorage().Values)
            {
                // possible cases
                if (!spellInfo.IsLootCrafting())
                    continue;

                if (!lootIdSet.Contains(spellInfo.Id))
                {
                    // not report about not trainable spells (optionally supported by DB)
                    // ignore 61756 (Northrend Inscription Research (FAST QA VERSION) for example
                    if (!spellInfo.HasAttribute(SpellAttr0.NotShapeshift) || spellInfo.HasAttribute(SpellAttr0.Tradespell))
                    {
                        Spell.ReportNonExistingId(spellInfo.Id, spellInfo.Id);
                    }
                }
                else
                    lootIdSet.Remove(spellInfo.Id);
            }

            // output error for any still listed (not referenced from appropriate table) ids
            Spell.ReportUnusedIds(lootIdSet);

            if (count != 0)
                Log.outInfo(LogFilter.ServerLoading, "Loaded {0} spell loot templates in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
            else
                Log.outError(LogFilter.ServerLoading, "Loaded 0 spell loot templates. DB table `spell_loot_template` is empty");
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

            Log.outInfo(LogFilter.ServerLoading, "Loaded refence loot templates in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }
    }

    public class LootStoreItem
    {
        public uint itemid;                 // id of the item
        public uint reference;              // referenced TemplateleId
        public float chance;                // chance to drop for both quest and non-quest items, chance to be used for refs
        public ushort lootmode;
        public bool needs_quest;            // quest drop (negative ChanceOrQuestChance in DB)
        public byte groupid;
        public byte mincount;               // mincount for drop items
        public byte maxcount;               // max drop count for the item mincount or Ref multiplicator
        public List<Condition> conditions;  // additional loot condition

        public LootStoreItem(uint _itemid, uint _reference, float _chance, bool _needs_quest, ushort _lootmode, byte _groupid, byte _mincount, byte _maxcount)
        {
            itemid = _itemid;
            reference = _reference;
            chance = _chance;
            lootmode = _lootmode;
            needs_quest = _needs_quest;
            groupid = _groupid;
            mincount = _mincount;
            maxcount = _maxcount;
            conditions = new List<Condition>();
        }

        public bool Roll(bool rate)
        {
            if (chance >= 100.0f)
                return true;

            if (reference > 0)                                   // reference case
                return RandomHelper.randChance(chance * (rate ? WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferenced) : 1.0f));

            ItemTemplate pProto = Global.ObjectMgr.GetItemTemplate(itemid);

            float qualityModifier = pProto != null && rate ? WorldConfig.GetFloatValue(qualityToRate[(int)pProto.GetQuality()]) : 1.0f;

            return RandomHelper.randChance(chance * qualityModifier);
        }
        public bool IsValid(LootStore store, uint entry)
        {
            if (mincount == 0)
            {
                Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: wrong mincount ({3}) - skipped", store.GetName(), entry, itemid, reference);
                return false;
            }

            if (reference == 0)                                  // item (quest or non-quest) entry, maybe grouped
            {
                ItemTemplate proto = Global.ObjectMgr.GetItemTemplate(itemid);
                if (proto == null)
                {
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: item entry not listed in `item_template` - skipped", store.GetName(), entry, itemid);
                    return false;
                }

                if (chance == 0 && groupid == 0)                      // Zero chance is allowed for grouped entries only
                {
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: equal-chanced grouped entry, but group not defined - skipped", store.GetName(), entry, itemid);
                    return false;
                }

                if (chance != 0 && chance < 0.000001f)             // loot with low chance
                {
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: low chance ({3}) - skipped",
                        store.GetName(), entry, itemid, chance);
                    return false;
                }

                if (maxcount < mincount)                       // wrong max count
                {
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: max count ({3}) less that min count ({4}) - skipped", store.GetName(), entry, itemid, maxcount, reference);
                    return false;
                }
            }
            else                                                    // mincountOrRef < 0
            {
                if (needs_quest)
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: quest chance will be treated as non-quest chance", store.GetName(), entry, itemid);
                else if (chance == 0)                              // no chance for the reference
                {
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: zero chance is specified for a reference, skipped", store.GetName(), entry, itemid);
                    return false;
                }
            }
            return true;                                            // Referenced template existence is checked at whole store level
        }

        public static WorldCfg[] qualityToRate = new WorldCfg[7]
        {
            WorldCfg.RateDropItemPoor,                                    // ITEM_QUALITY_POOR
            WorldCfg.RateDropItemNormal,                                  // ITEM_QUALITY_NORMAL
            WorldCfg.RateDropItemUncommon,                                // ITEM_QUALITY_UNCOMMON
            WorldCfg.RateDropItemRare,                                    // ITEM_QUALITY_RARE
            WorldCfg.RateDropItemEpic,                                    // ITEM_QUALITY_EPIC
            WorldCfg.RateDropItemLegendary,                               // ITEM_QUALITY_LEGENDARY
            WorldCfg.RateDropItemArtifact,                                // ITEM_QUALITY_ARTIFACT
        };
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
            foreach (var i in m_LootTemplates)
                i.Value.Verify(this, i.Key);
        }

        public uint LoadAndCollectLootIds(out List<uint> lootIdSet)
        {
            uint count = LoadLootTable();
            lootIdSet = new List<uint>();

            foreach (var tab in m_LootTemplates)
                lootIdSet.Add(tab.Key);

            return count;
        }
        public void CheckLootRefs(List<uint> ref_set = null)
        {
            foreach (var pair in m_LootTemplates)
                pair.Value.CheckLootRefs(m_LootTemplates, ref_set);
        }
        public void ReportUnusedIds(List<uint> lootIdSet)
        {
            // all still listed ids isn't referenced
            foreach (var id in lootIdSet)
                Log.outError( LogFilter.Sql, "Table '{0}' entry {1} isn't {2} and not referenced from loot, and then useless.", GetName(), id, GetEntryName());
        }
        public void ReportNonExistingId(uint lootId, uint ownerId)
        {
            Log.outError( LogFilter.Sql, "Table '{0}' Entry {1} does not exist but it is used by {2} {3}", GetName(), lootId, GetEntryName(), ownerId);
        }

        public bool HaveLootFor(uint loot_id) { return m_LootTemplates.LookupByKey(loot_id) != null; }
        public bool HaveQuestLootFor(uint loot_id)
        {
            var lootTemplate = m_LootTemplates.LookupByKey(loot_id);
            if (lootTemplate == null)
                return false;

            // scan loot for quest items
            return lootTemplate.HasQuestDrop(m_LootTemplates);
        }
        public bool HaveQuestLootForPlayer(uint loot_id, Player player)
        {
            var tab = m_LootTemplates.LookupByKey(loot_id);
            if (tab != null)
                if (tab.HasQuestDropForPlayer(m_LootTemplates, player))
                    return true;

            return false;
        }

        public LootTemplate GetLootFor(uint loot_id)
        {
            var tab = m_LootTemplates.LookupByKey(loot_id);

            if (tab == null)
                return null;

            return tab;
        }
        public void ResetConditions()
        {
            foreach (var pair in m_LootTemplates)
            {
                List<Condition> empty = new List<Condition>();
                pair.Value.CopyConditions(empty);
            }
        }
        public LootTemplate GetLootForConditionFill(uint loot_id)
        {
            var tab = m_LootTemplates.LookupByKey(loot_id);

            if (tab == null)
                return null;

            return tab;
        }

        public string GetName() { return m_name; }
        string GetEntryName() { return m_entryName; }
        public bool IsRatesAllowed() { return m_ratesAllowed; }

        uint LoadLootTable()
        {
            // Clearing store (for reloading case)
            Clear();

            //                                            0     1      2        3         4             5          6        7         8
            SQLResult result = DB.World.Query("SELECT Entry, Item, Reference, Chance, QuestRequired, LootMode, GroupId, MinCount, MaxCount FROM {0}", GetName());
            if (result.IsEmpty())
                return 0;

            uint count = 0;
            do
            {
                uint entry = result.Read<uint>(0);
                uint item = result.Read<uint>(1);
                uint reference = result.Read<uint>(2);
                float chance = result.Read<float>(3);
                bool needsquest = result.Read<bool>(4);
                ushort lootmode = result.Read<ushort>(5);
                byte groupid = result.Read<byte>(6);
                byte mincount = result.Read<byte>(7);
                byte maxcount = result.Read<byte>(8);

                if (groupid >= 1 << 7)                                     // it stored in 7 bit field
                {
                    Log.outError(LogFilter.Sql, "Table '{0}' entry {1} item {2}: group ({3}) must be less {4} - skipped", GetName(), entry, item, groupid, 1 << 7);
                    return 0;
                }

                LootStoreItem storeitem = new LootStoreItem(item, reference, chance, needsquest, lootmode, groupid, mincount, maxcount);

                if (!storeitem.IsValid(this, entry))            // Validity checks
                    continue;

                // Looking for the template of the entry
                // often entries are put together
                if (m_LootTemplates.Empty() || !m_LootTemplates.ContainsKey(entry))
                    m_LootTemplates.Add(entry, new LootTemplate());

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

        LootTemplateMap m_LootTemplates = new LootTemplateMap();
        string m_name;
        string m_entryName;
        bool m_ratesAllowed;
    }

    public class LootTemplate
    {
        public void AddEntry(LootStoreItem item)
        {
            if (item.groupid > 0 && item.reference == 0)         // Group
            {
                if (!Groups.ContainsKey(item.groupid - 1))
                    Groups[item.groupid - 1] = new LootGroup();

                Groups[item.groupid - 1].AddEntry(item);              // Adds new entry to the group
            }
            else                                                    // Non-grouped entries and references are stored together
                Entries.Add(item);
        }

        public void Process(Loot loot, bool rate, ushort lootMode, byte groupId = 0)
        {
            if (groupId != 0)                                            // Group reference uses own processing of the group
            {
                if (groupId > Groups.Count)
                    return;                                         // Error message already printed at loading stage

                if (Groups[groupId - 1] == null)
                    return;

                Groups[groupId - 1].Process(loot, lootMode);
                return;
            }

            // Rolling non-grouped items
            foreach (var item in Entries)
            {
                if (!Convert.ToBoolean(item.lootmode & lootMode))                       // Do not add if mode mismatch
                    continue;

                if (!item.Roll(rate))
                    continue;                                           // Bad luck for the entry

                if (item.reference > 0)                            // References processing
                {
                    LootTemplate Referenced = LootStorage.Reference.GetLootFor(item.reference);
                    if (Referenced == null)
                        continue;                                       // Error message already printed at loading stage

                    uint maxcount = (uint)(item.maxcount * WorldConfig.GetFloatValue(WorldCfg.RateDropItemReferencedAmount));
                    for (uint loop = 0; loop < maxcount; ++loop)      // Ref multiplicator
                        Referenced.Process(loot, rate, lootMode, item.groupid);
                }
                else                                                    // Plain entries (not a reference, not grouped)
                    loot.AddItem(item);                                // Chance is already checked, just add
            }

            // Now processing groups
            foreach (var group in Groups.Values)
            {
                if (group != null)
                    group.Process(loot, lootMode);
            }
        }
        public void CopyConditions(List<Condition> conditions)
        {
            foreach (var i in Entries)
                i.conditions.Clear();

            foreach (var group in Groups.Values)
                group.CopyConditions(conditions);
        }
        public void CopyConditions(LootItem li)
        {
            // Copies the conditions list from a template item to a LootItem
            foreach (var item in Entries)
            {
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
                if (item.reference > 0)                        // References
                {
                    var Referenced = store.LookupByKey(item.reference);
                    if (Referenced == null)
                        continue;                                   // Error message [should be] already printed at loading stage
                    if (Referenced.HasQuestDrop(store, item.groupid))
                        return true;
                }
                else if (item.needs_quest)
                    return true;                                    // quest drop found
            }

            // Now processing groups
            foreach (var group in Groups.Values)
                if (group.HasQuestDrop())
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
                if (item.reference > 0)                        // References processing
                {
                    var Referenced = store.LookupByKey(item.reference);
                    if (Referenced == null)
                        continue;                                   // Error message already printed at loading stage
                    if (Referenced.HasQuestDropForPlayer(store, player, item.groupid))
                        return true;
                }
                else if (player.HasQuestForItem(item.itemid))
                    return true;                                    // active quest drop found
            }

            // Now checking groups
            foreach (var group in Groups.Values)
                if (group.HasQuestDropForPlayer(player))
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
                if (item.reference > 0)
                {
                    if (LootStorage.Reference.GetLootFor(item.reference) == null)
                        LootStorage.Reference.ReportNonExistingId(item.reference, item.itemid);
                    else if (ref_set != null)
                        ref_set.Remove(item.reference);
                }
            }

            foreach (var group in Groups.Values)
                group.CheckLootRefs(store, ref_set);
        }
        public bool addConditionItem(Condition cond)
        {
            if (cond == null || !cond.isLoaded())//should never happen, checked at loading
            {
                Log.outError(LogFilter.Loot, "LootTemplate.addConditionItem: condition is null");
                return false;
            }

            if (!Entries.Empty())
            {
                foreach (var i in Entries)
                {
                    if (i.itemid == cond.SourceEntry)
                    {
                        i.conditions.Add(cond);
                        return true;
                    }
                }
            }

            if (!Groups.Empty())
            {
                foreach (var group in Groups.Values)
                {
                    if (group == null)
                        continue;

                    LootStoreItemList itemList = group.GetExplicitlyChancedItemList();
                    if (!itemList.Empty())
                    {
                        foreach (var i in itemList)
                        {
                            if (i.itemid == cond.SourceEntry)
                            {
                                i.conditions.Add(cond);
                                return true;
                            }
                        }
                    }

                    itemList = group.GetEqualChancedItemList();
                    if (!itemList.Empty())
                    {
                        foreach (var i in itemList)
                        {
                            if (i.itemid == cond.SourceEntry)
                            {
                                i.conditions.Add(cond);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }
        public bool isReference(uint id)
        {
            foreach (var storeItem in Entries)
                if (storeItem.itemid == id && storeItem.reference > 0)
                    return true;

            return false;//not found or not reference
        }

        LootStoreItemList Entries = new LootStoreItemList();                          // not grouped only
        Dictionary<int, LootGroup> Groups = new Dictionary<int,LootGroup>();                           // groups have own (optimised) processing, grouped entries go there

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
                foreach (var i in ExplicitlyChanced)
                    if (i.needs_quest)
                        return true;

                foreach (var i in EqualChanced)
                    if (i.needs_quest)
                        return true;

                return false;
            }

            public bool HasQuestDropForPlayer(Player player)
            {
                foreach (var i in ExplicitlyChanced)
                    if (player.HasQuestForItem(i.itemid))
                        return true;

                foreach (var i in EqualChanced)
                    if (player.HasQuestForItem(i.itemid))
                        return true;

                return false;
            }

            public void Process(Loot loot, ushort lootMode)
            {
                LootStoreItem item = Roll(loot, lootMode);
                if (item != null)
                    loot.AddItem(item);
            }
            float RawTotalChance()
            {
                float result = 0;

                foreach (var i in ExplicitlyChanced)
                    if (!i.needs_quest)
                        result += i.chance;

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
                    if (item.reference > 0)
                    {
                        if (LootStorage.Reference.GetLootFor(item.reference) == null)
                            LootStorage.Reference.ReportNonExistingId(item.reference, item.itemid);
                        else if (ref_set != null)
                            ref_set.Remove(item.reference);
                    }
                }

                foreach (var item in EqualChanced)
                {
                    if (item.reference > 0)
                    {
                        if (LootStorage.Reference.GetLootFor(item.reference) == null)
                            LootStorage.Reference.ReportNonExistingId(item.reference, item.itemid);
                        else if (ref_set != null)
                            ref_set.Remove(item.reference);
                    }
                }
            }
            public LootStoreItemList GetExplicitlyChancedItemList() { return ExplicitlyChanced; }
            public LootStoreItemList GetEqualChancedItemList() { return EqualChanced; }
            public void CopyConditions(List<Condition> conditions)
            {
                foreach (var i in ExplicitlyChanced)
                    i.conditions.Clear();

                foreach (var i in EqualChanced)
                    i.conditions.Clear();
            }

            LootStoreItemList ExplicitlyChanced = new LootStoreItemList();                // Entries with chances defined in DB
            LootStoreItemList EqualChanced = new LootStoreItemList();                     // Zero chances - every entry takes the same chance

            LootStoreItem Roll(Loot loot, ushort lootMode)
            {
                LootStoreItemList possibleLoot = ExplicitlyChanced;
                possibleLoot.RemoveAll(new LootGroupInvalidSelector(loot, lootMode).Check);

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

                possibleLoot = EqualChanced;
                possibleLoot.RemoveAll(new LootGroupInvalidSelector(loot, lootMode).Check);
                if (!possibleLoot.Empty())                              // If nothing selected yet - an item is taken from equal-chanced part
                    return possibleLoot.SelectRandom();

                return null;                                            // Empty drop from the group
            }
        }
    }

    public struct LootGroupInvalidSelector
    {
        public LootGroupInvalidSelector(Loot loot, ushort lootMode)
        {
            _loot = loot;
            _lootMode = lootMode;
        }

        public bool Check(LootStoreItem item)
        {
            if (!Convert.ToBoolean(item.lootmode & _lootMode))
                return true;

            byte foundDuplicates = 0;
            foreach (var lootItem in _loot.items)
                if (lootItem.itemid == item.itemid)
                    if (++foundDuplicates == _loot.maxDuplicates)
                        return true;

            return false;
        }

        Loot _loot;
        ushort _lootMode;
    }
}
