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
using Game.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Game.Spells
{
    public class SkillDiscovery
    {
        public static void LoadSkillDiscoveryTable()
        {
            uint oldMSTime = Time.GetMSTime();

            SkillDiscoveryStorage.Clear();                            // need for reload

            //                                                0        1         2              3
            SQLResult result = DB.World.Query("SELECT spellId, reqSpell, reqSkillValue, chance FROM skill_discovery_template");

            if (result.IsEmpty())
            {
                Log.outError(LogFilter.ServerLoading, "Loaded 0 skill discovery definitions. DB table `skill_discovery_template` is empty.");
                return;
            }

            uint count = 0;

            StringBuilder ssNonDiscoverableEntries = new StringBuilder();
            List<uint> reportedReqSpells = new List<uint>();

            do
            {
                uint spellId = result.Read<uint>(0);
                int reqSkillOrSpell = result.Read<int>(1);
                uint reqSkillValue = result.Read<uint>(2);
                float chance = result.Read<float>(3);

                if (chance <= 0)                                    // chance
                {
                    ssNonDiscoverableEntries.AppendFormat("spellId = {0} reqSkillOrSpell = {1} reqSkillValue = {2} chance = {3} (chance problem)\n", spellId, reqSkillOrSpell, reqSkillValue, chance);
                    continue;
                }

                if (reqSkillOrSpell > 0)                            // spell case
                {
                    uint absReqSkillOrSpell = (uint)reqSkillOrSpell;
                    SpellInfo reqSpellInfo = Global.SpellMgr.GetSpellInfo(absReqSkillOrSpell);
                    if (reqSpellInfo == null)
                    {
                        if (!reportedReqSpells.Contains(absReqSkillOrSpell))
                        {
                            Log.outError(LogFilter.Sql, "Spell (ID: {0}) have not existed spell (ID: {1}) in `reqSpell` field in `skill_discovery_template` table", spellId, reqSkillOrSpell);
                            reportedReqSpells.Add(absReqSkillOrSpell);
                        }
                        continue;
                    }

                    // mechanic discovery
                    if (reqSpellInfo.Mechanic != Mechanics.Discovery &&
                        // explicit discovery ability
                        !reqSpellInfo.IsExplicitDiscovery())
                    {
                        if (!reportedReqSpells.Contains(absReqSkillOrSpell))
                        {
                            Log.outError(LogFilter.Sql, "Spell (ID: {0}) not have MECHANIC_DISCOVERY (28) value in Mechanic field in spell.dbc" +
                                " and not 100%% chance random discovery ability but listed for spellId {1} (and maybe more) in `skill_discovery_template` table",
                                absReqSkillOrSpell, spellId);
                            reportedReqSpells.Add(absReqSkillOrSpell);
                        }
                        continue;
                    }

                    SkillDiscoveryStorage.Add(reqSkillOrSpell, new SkillDiscoveryEntry(spellId, reqSkillValue, chance));
                }
                else if (reqSkillOrSpell == 0)                      // skill case
                {
                    var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);

                    if (bounds.Empty())
                    {
                        Log.outError(LogFilter.Sql, "Spell (ID: {0}) not listed in `SkillLineAbility.dbc` but listed with `reqSpell`=0 in `skill_discovery_template` table", spellId);
                        continue;
                    }

                    foreach (var _spell_idx in bounds)
                        SkillDiscoveryStorage.Add(-(int)_spell_idx.SkillLine, new SkillDiscoveryEntry(spellId, reqSkillValue, chance));
                }
                else
                {
                    Log.outError(LogFilter.Sql, "Spell (ID: {0}) have negative value in `reqSpell` field in `skill_discovery_template` table", spellId);
                    continue;
                }

                ++count;
            }
            while (result.NextRow());

            if (ssNonDiscoverableEntries.Length != 0)
                Log.outError(LogFilter.Sql, "Some items can't be successfully discovered: have in chance field value < 0.000001 in `skill_discovery_template` DB table . List:\n{0}", ssNonDiscoverableEntries.ToString());

            // report about empty data for explicit discovery spells
            foreach (var spellEntry in Global.SpellMgr.GetSpellInfoStorage().Values)
            {
                // skip not explicit discovery spells
                if (!spellEntry.IsExplicitDiscovery())
                    continue;

                if (!SkillDiscoveryStorage.ContainsKey((int)spellEntry.Id))
                    Log.outError(LogFilter.Sql, "Spell (ID: {0}) is 100% chance random discovery ability but not have data in `skill_discovery_template` table", spellEntry.Id);
            }

            Log.outInfo(LogFilter.ServerLoading, "Loaded {0} skill discovery definitions in {1} ms", count, Time.GetMSTimeDiffToNow(oldMSTime));
        }

        public static uint GetExplicitDiscoverySpell(uint spellId, Player player)
        {
            // explicit discovery spell chances (always success if case exist)
            // in this case we have both skill and spell
            var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);
            if (tab.Empty())
                return 0;

            var bounds = Global.SpellMgr.GetSkillLineAbilityMapBounds(spellId);
            uint skillvalue = !bounds.Empty() ? (uint)player.GetSkillValue((SkillType)bounds.FirstOrDefault().SkillLine) : 0;

            float full_chance = 0;
            foreach (var item_iter in tab)
                if (item_iter.reqSkillValue <= skillvalue)
                    if (!player.HasSpell(item_iter.spellId))
                        full_chance += item_iter.chance;

            float rate = full_chance / 100.0f;
            float roll = (float)RandomHelper.randChance() * rate;                      // roll now in range 0..full_chance

            foreach (var item_iter in tab)
            {
                if (item_iter.reqSkillValue > skillvalue)
                    continue;

                if (player.HasSpell(item_iter.spellId))
                    continue;

                if (item_iter.chance > roll)
                    return item_iter.spellId;

                roll -= item_iter.chance;
            }

            return 0;
        }

        public static bool HasDiscoveredAllSpells(uint spellId, Player player)
        {
            var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);
            if (tab.Empty())
                return true;

            foreach (var item_iter in tab)
                if (!player.HasSpell(item_iter.spellId))
                    return false;

            return true;
        }

        public static uint GetSkillDiscoverySpell(uint skillId, uint spellId, Player player)
        {
            uint skillvalue = skillId != 0 ? (uint)player.GetSkillValue((SkillType)skillId) : 0;

            // check spell case
            var tab = SkillDiscoveryStorage.LookupByKey((int)spellId);

            if (!tab.Empty())
            {
                foreach (var item_iter in tab)
                {
                    if (RandomHelper.randChance(item_iter.chance * WorldConfig.GetFloatValue(WorldCfg.RateSkillDiscovery)) &&
                        item_iter.reqSkillValue <= skillvalue &&
                        !player.HasSpell(item_iter.spellId))
                        return item_iter.spellId;
                }

                return 0;
            }

            if (skillId == 0)
                return 0;

            // check skill line case
            tab = SkillDiscoveryStorage.LookupByKey(-(int)skillId);
            if (!tab.Empty())
            {
                foreach (var item_iter in tab)
                {
                    if (RandomHelper.randChance(item_iter.chance * WorldConfig.GetFloatValue(WorldCfg.RateSkillDiscovery)) &&
                        item_iter.reqSkillValue <= skillvalue &&
                        !player.HasSpell(item_iter.spellId))
                        return item_iter.spellId;
                }

                return 0;
            }

            return 0;
        }

        static MultiMap<int, SkillDiscoveryEntry> SkillDiscoveryStorage = new MultiMap<int, SkillDiscoveryEntry>();
    }

    public class SkillDiscoveryEntry
    {
        public SkillDiscoveryEntry(uint _spellId = 0, uint req_skill_val = 0, float _chance = 0)
        {
            spellId = _spellId;
            reqSkillValue = req_skill_val;
            chance = _chance;
        }

        public uint spellId;                                        // discavered spell
        public uint reqSkillValue;                                  // skill level limitation
        public float chance;                                         // chance
    }
}
