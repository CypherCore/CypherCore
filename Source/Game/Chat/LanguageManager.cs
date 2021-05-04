/*
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


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.DataStorage;
using Framework.Constants;
using Game.Spells;
using Framework.Dynamic;
using Framework.Collections;

namespace Game.Chat
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        Dictionary<uint, LanguageDesc> _langsMap = new();
        MultiMap<Tuple<uint, byte>, string> _wordsMap = new();

        LanguageManager() { }

        public void LoadSpellEffectLanguage(SpellEffectRecord spellEffect)
        {
            Cypher.Assert(spellEffect != null && spellEffect.Effect == (uint)SpellEffectName.Language);

            uint languageId = (uint)spellEffect.EffectMiscValue[0];
            if (!_langsMap.TryGetValue(languageId, out LanguageDesc desc))
            {
                Log.outWarn(LogFilter.Spells, $"LoadSpellEffectLanguage called on Spell {spellEffect.SpellID} with language {languageId} which does not exist in Language.db2!");
                return;
            }

            desc.SpellId = spellEffect.SpellID;
        }

        uint GetSpellLanguage(uint spellId)
        {
            SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);
            if (spellInfo != null)
            {
                var effects = spellInfo.GetEffects();
                if (effects.Length != 1 || effects[0].Effect != SpellEffectName.Language)
                    Log.outWarn(LogFilter.Spells, $"Invalid language spell {spellId}. Expected 1 effect with SPELL_EFFECT_LANGUAGE");
                else
                    return (uint)effects[0].MiscValue;
            }
            return 0;
        }

        bool IsRelevantLanguageSkill(SkillLineRecord skillLineEntry)
        {
            if (skillLineEntry == null)
                return false;

            SkillRaceClassInfoRecord entry = Global.DB2Mgr.GetAvailableSkillRaceClassInfo(skillLineEntry.Id);
            return entry != null;
        }

        public void LoadLanguages()
        {
            uint oldMSTime = Time.GetMSTime();

            // Load languages from Languages.db2. Just the id, we don't need the name
            foreach (LanguagesRecord langEntry in CliDB.LanguagesStorage.Values)
                _langsMap.Add(langEntry.Id, new LanguageDesc());

            // Add the languages used in code in case they don't exist
            _langsMap.Add((uint)Language.Universal, new LanguageDesc());
            _langsMap.Add((uint)Language.Addon, new LanguageDesc());
            _langsMap.Add((uint)Language.AddonLogged, new LanguageDesc());

            // Log load time
            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_langsMap.Count} languages in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void LoadLanguagesSkills()
        {
            uint oldMSTime = Time.GetMSTime();

            uint count = 0;
            foreach (SkillLineRecord skillLineEntry in CliDB.SkillLineStorage.Values)
            {
                if (skillLineEntry.CategoryID != SkillCategory.Languages)
                    continue;

                if (!IsRelevantLanguageSkill(skillLineEntry))
                    continue;

                var skills = Global.DB2Mgr.GetSkillLineAbilitiesBySkill(skillLineEntry.Id);

                // We're expecting only 1 skill
                if (skills.Count != 1)
                    Log.outWarn(LogFilter.ServerLoading, $"Found language skill line with {skills.Count} spells. Expected 1. Will use 1st if available");

                SkillLineAbilityRecord ability = skills.Empty() ? null : skills[0];
                if (ability != null)
                {
                    uint languageId = GetSpellLanguage(ability.Spell);
                    if (languageId != 0)
                    {
                        if (!_langsMap.TryGetValue(languageId, out LanguageDesc desc))
                            Log.outWarn(LogFilter.ServerLoading, $"Spell {ability.Spell} has language {languageId}, which doesn't exist in Languages.db2");
                        else
                        {
                            desc.SpellId = ability.Spell;
                            desc.SkillId = skillLineEntry.Id;
                            ++count;
                        }
                    }
                }
            }

            // Languages that don't have skills will be added in SpellMgr::LoadSpellInfoStore() (e.g. LANG_ZOMBIE, LANG_SHATH_YAR)

            // Log load time
            Log.outInfo(LogFilter.ServerLoading, $"Loaded {count} languages skills in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        public void LoadLanguagesWords()
        {
            uint oldMSTime = Time.GetMSTime();

            uint wordsNum = 0;
            foreach (LanguageWordsRecord wordEntry in CliDB.LanguageWordsStorage.Values)
            {
                byte length = (byte)Math.Min(18, wordEntry.Word.Length);

                var key = Tuple.Create(wordEntry.LanguageID, length);

                _wordsMap.Add(key, wordEntry.Word);
                ++wordsNum;
            }

            // log load time
            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_wordsMap.Count} word groups from {wordsNum} words in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
        }

        List<string> FindWordGroup(uint language, uint wordLen)
        {
            return _wordsMap.LookupByKey(Tuple.Create(language, wordLen));
        }

        public string Translate(string msg, uint sourcePlayerLanguage)
        {
            StringBuilder result = new StringBuilder();
            StringArray tokens = new(msg, ' ');

            bool first = true;
            foreach (string str in tokens)
            {
                string nextPart = str;
                uint wordLen = (uint)Math.Min(18U, str.Length);
                var wordGroup = FindWordGroup(sourcePlayerLanguage, wordLen);
                if (wordGroup.Empty())
                    nextPart = "";
                else
                {
                    uint wordHash = SStrHash(str, true);
                    byte idxInsideGroup = (byte)(wordHash % wordGroup.Count);
                    nextPart = wordGroup[idxInsideGroup];
                }

                if (first)
                    first = false;
                else
                    result.Append(" ");

                result.Append(nextPart);
            }
            return result.ToString();
        }

        static char upper_backslash(char c) { return c == '/' ? '\\' : char.ToUpper(c); }

        static uint[] s_hashtable =
        {
            0x486E26EE, 0xDCAA16B3, 0xE1918EEF, 0x202DAFDB,
            0x341C7DC7, 0x1C365303, 0x40EF2D37, 0x65FD5E49,
            0xD6057177, 0x904ECE93, 0x1C38024F, 0x98FD323B,
            0xE3061AE7, 0xA39B0FA1, 0x9797F25F, 0xE4444563,
        };

        uint SStrHash(string str, bool caseInsensitive, uint seed = 0x7FED7FED)
        {
            uint shift = 0xEEEEEEEE;
            for (var i = 0; i < str.Length; ++i)
            {
                var c = str[i];
                if (caseInsensitive)
                    c = upper_backslash(c);

                seed = (s_hashtable[c >> 4] - s_hashtable[c & 0xF]) ^ (shift + seed);
                shift = c + seed + 33 * shift + 3;
            }

            return seed != 0 ? seed : 1;
        }

        public bool IsLanguageExist(Language languageId)
        {
            return _langsMap.ContainsKey((uint)languageId);
        }

        public LanguageDesc GetLanguageDescById(Language languageId)
        {
            return _langsMap.LookupByKey((uint)languageId);
        }

        public bool ForEachLanguage(Func<uint, LanguageDesc, bool> callback)
        {
            foreach (var pair in _langsMap)
                if (!callback(pair.Key, pair.Value))
                    return false;
            return true;
        }
    }
}
