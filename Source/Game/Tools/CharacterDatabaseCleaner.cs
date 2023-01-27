// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Framework.Constants;
using Framework.Database;
using Game.DataStorage;
using Game.Spells;

namespace Game
{
	internal class CharacterDatabaseCleaner
	{
		public static void CleanDatabase()
		{
			// config to disable
			if (!WorldConfig.GetBoolValue(WorldCfg.CleanCharacterDb))
				return;

			Log.outInfo(LogFilter.Server, "Cleaning character database...");

			uint oldMSTime = Time.GetMSTime();

			CleaningFlags flags = (CleaningFlags)Global.WorldMgr.GetPersistentWorldVariable(WorldManager.CharacterDatabaseCleaningFlagsVarId);

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
			Global.WorldMgr.SetPersistentWorldVariable(WorldManager.CharacterDatabaseCleaningFlagsVarId, (int)flags);

			Global.WorldMgr.SetCleaningFlags(flags);

			Log.outInfo(LogFilter.ServerLoading, "Cleaned character database in {0} ms", Time.GetMSTimeDiffToNow(oldMSTime));
		}

		private static void CheckUnique(string column, string table, CheckFor check)
		{
			SQLResult result = DB.Characters.Query("SELECT DISTINCT {0} FROM {1}", column, table);

			if (result.IsEmpty())
			{
				Log.outInfo(LogFilter.Sql, "Table {0} is empty.", table);

				return;
			}

			bool          found = false;
			StringBuilder ss    = new();

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
					{
						ss.Append(',');
					}

					ss.Append(id);
				}
			} while (result.NextRow());

			if (found)
			{
				ss.Append(')');
				DB.Characters.Execute(ss.ToString());
			}
		}

		private static bool AchievementProgressCheck(uint criteria)
		{
			return Global.CriteriaMgr.GetCriteria(criteria) != null;
		}

		private static void CleanCharacterAchievementProgress()
		{
			CheckUnique("criteria", "character_achievement_progress", AchievementProgressCheck);
		}

		private static bool SkillCheck(uint skill)
		{
			return CliDB.SkillLineStorage.ContainsKey(skill);
		}

		private static void CleanCharacterSkills()
		{
			CheckUnique("skill", "character_skills", SkillCheck);
		}

		private static bool SpellCheck(uint spell_id)
		{
			SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spell_id, Difficulty.None);

			return spellInfo != null && !spellInfo.HasAttribute(SpellCustomAttributes.IsTalent);
		}

		private static void CleanCharacterSpell()
		{
			CheckUnique("spell", "character_spell", SpellCheck);
		}

		private static bool TalentCheck(uint talent_id)
		{
			TalentRecord talentInfo = CliDB.TalentStorage.LookupByKey(talent_id);

			if (talentInfo == null)
				return false;

			return CliDB.ChrSpecializationStorage.ContainsKey(talentInfo.SpecID);
		}

		private static void CleanCharacterTalent()
		{
			DB.Characters.DirectExecute("DELETE FROM character_talent WHERE talentGroup > {0}", PlayerConst.MaxSpecializations);
			CheckUnique("talentId", "character_talent", TalentCheck);
		}

		private static void CleanCharacterQuestStatus()
		{
			DB.Characters.DirectExecute("DELETE FROM character_queststatus WHERE status = 0");
		}

		private delegate bool CheckFor(uint id);
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