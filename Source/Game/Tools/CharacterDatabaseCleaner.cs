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
using Game.DataStorage;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Text;

namespace Game
{
    class CharacterDatabaseCleaner
    {
        public static void CleanDatabase()
        {
            // config to disable
            if (!WorldConfig.GetBoolValue(WorldCfg.CleanCharacterDb))
                return;

            Log.outInfo(LogFilter.Server, "Cleaning character database...");

            uint oldMSTime = Time.GetMSTime();

            // check flags which clean ups are necessary
            SQLResult result = DB.Characters.Query("SELECT value FROM worldstates WHERE entry = {0}", (uint)WorldStates.CleaningFlags);
            if (result.IsEmpty())
                return;

            CleaningFlags flags = (CleaningFlags)result.Read<uint>(0);

            // clean up
            if (flags.HasAnyFlag(CleaningFlags.AchievementProgress))
                CleanCharacterAchievementProgress();

            if (flags.HasAnyFlag(CleaningFlags.Skills))
                CleanCharacterSkills();

            if (flags.HasAnyFlag(CleaningFlags.Spells))
                CleanCharacterSpell();

            if (flags.HasAnyFlag(CleaningFlags.Talents))
                CleanCharacterTalent();

            if (flags.HasAnyFlag(CleaningFlags.Queststatus))
                CleanCharacterQuestStatus();

            // NOTE: In order to have persistentFlags be set in worldstates for the next cleanup,
            // you need to define them at least once in worldstates.
            flags &= (CleaningFlags)WorldConfig.GetIntValue(WorldCfg.PersistentCharacterCleanFlags);
            DB.Characters.DirectExecute("UPDATE worldstates SET value = {0} WHERE entry = {1}", flags, (uint)WorldStates.CleaningFlags);

            Global.WorldMgr.SetCleaningFlags(flags);

            Log.outInfo(LogFilter.ServerLoading, "Cleaned character database in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
        }

        delegate bool CheckFor(uint id);

        static void CheckUnique(string column, string table, CheckFor check)
        {
            SQLResult result = DB.Characters.Query("SELECT DISTINCT {0} FROM {1}", column, table);
            if (result.IsEmpty())
            {
                Log.outInfo(LogFilter.Sql, "Table {0} is empty.", table);
                return;
            }

            bool found = false;
            StringBuilder ss = new StringBuilder();
            do
            {
                uint id = result.Read<uint>(0);
                if (!check(id))
                {
                    if (!found)
                    {
                        ss.AppendFormat("DELETE FROM {0} WHERE {1} IN(", table, column);
                        found = true;
                    }
                    else
                        ss.Append(',');

                    ss.Append(id);
                }
            }
            while (result.NextRow());

            if (found)
            {
                ss.Append(')');
                DB.Characters.Execute(ss.ToString());
            }
        }

        static bool AchievementProgressCheck(uint criteria)
        {
            return Global.CriteriaMgr.GetCriteria(criteria) != null;
        }

        static void CleanCharacterAchievementProgress()
        {
            CheckUnique("criteria", "character_achievement_progress", AchievementProgressCheck);
        }

        static bool SkillCheck(uint skill)
        {
            return CliDB.SkillLineStorage.ContainsKey(skill);
        }

        static void CleanCharacterSkills()
        {
            CheckUnique("skill", "character_skills", SkillCheck);
        }

        static bool SpellCheck(uint spell_id)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id);
            return spellInfo != null && !spellInfo.HasAttribute(SpellCustomAttributes.IsTalent);
        }

        static void CleanCharacterSpell()
        {
            CheckUnique("spell", "character_spell", SpellCheck);
        }

        static bool TalentCheck(uint talent_id)
        {
            TalentRecord talentInfo = CliDB.TalentStorage.LookupByKey(talent_id);
            if (talentInfo == null)
                return false;

            return CliDB.ChrSpecializationStorage.ContainsKey(talentInfo.SpecID);
        }

        static void CleanCharacterTalent()
        {
            DB.Characters.DirectExecute("DELETE FROM character_talent WHERE talentGroup > {0}", PlayerConst.MaxSpecializations);
            CheckUnique("talentId", "character_talent", TalentCheck);
        }

        static void CleanCharacterQuestStatus()
        {
            DB.Characters.DirectExecute("DELETE FROM character_queststatus WHERE status = 0");
        }
    }

    [Flags]
    public enum CleaningFlags
    {
        AchievementProgress = 0x1,
        Skills = 0x2,
        Spells = 0x4,
        Talents = 0x8,
        Queststatus = 0x10
    }
}
