// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.


using System;
using System.Collections.Generic;
using System.Linq;
using Game.DataStorage;
using Framework.Constants;
using Framework.Collections;

namespace Game.Chat
{
    public class LanguageManager : Singleton<LanguageManager>
    {
        MultiMap<uint, LanguageDesc> _langsMap = new();
        MultiMap<Tuple<uint, byte>, string> _wordsMap = new();

        LanguageManager() { }

        public void LoadSpellEffectLanguage(SpellEffectRecord spellEffect)
        {
            Cypher.Assert(spellEffect != null && spellEffect.Effect == (uint)SpellEffectName.Language);

            uint languageId = (uint)spellEffect.EffectMiscValue[0];
            _langsMap.Add(languageId, new LanguageDesc(spellEffect.SpellID, 0)); // register without a skill id for now
        }

        public void LoadLanguages()
        {
            uint oldMSTime = Time.GetMSTime();

            // Load languages from Languages.db2. Just the id, we don't need the name
            foreach (LanguagesRecord langEntry in CliDB.LanguagesStorage.Values)
            {
                var spellsRange = _langsMap.LookupByKey(langEntry.Id);
                if (spellsRange.Empty())
                    _langsMap.Add(langEntry.Id, new LanguageDesc());
                else
                {
                    List<LanguageDesc> langsWithSkill = new();
                    foreach (var spellItr in spellsRange)
                        foreach (var skillPair in Global.SpellMgr.GetSkillLineAbilityMapBounds(spellItr.SpellId))
                            langsWithSkill.Add(new LanguageDesc(spellItr.SpellId, (uint)skillPair.SkillLine));

                    foreach (var langDesc in langsWithSkill)
                    {
                        // erase temporary assignment that lacked skill
                        _langsMap.Remove(langEntry.Id, new LanguageDesc(langDesc.SpellId, 0));
                        _langsMap.Add(langEntry.Id, langDesc);
                    }
                }
            }

            // Add the languages used in code in case they don't exist
            _langsMap.Add((uint)Language.Universal, new LanguageDesc());
            _langsMap.Add((uint)Language.Addon, new LanguageDesc());
            _langsMap.Add((uint)Language.AddonLogged, new LanguageDesc());

            // Log load time
            Log.outInfo(LogFilter.ServerLoading, $"Loaded {_langsMap.Count} languages in {Time.GetMSTimeDiffToNow(oldMSTime)} ms");
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
            return _wordsMap.LookupByKey(Tuple.Create(language, (byte)wordLen));
        }

        public string Translate(string msg, uint language, Locale locale)
        {
            string textToTranslate = "";
            StripHyperlinks(msg, ref textToTranslate);
            ReplaceUntranslatableCharactersWithSpace(ref textToTranslate);

            string result = "";
            StringArray tokens = new(textToTranslate, ' ');
            foreach (string str in tokens)
            {
                uint wordLen = (uint)Math.Min(18, str.Length);
                var wordGroup = FindWordGroup(language, wordLen);
                if (!wordGroup.Empty())
                {
                    uint wordHash = SStrHash(str, true);
                    byte idxInsideGroup = (byte)(wordHash % wordGroup.Count());

                    string replacementWord = wordGroup[idxInsideGroup];

                    switch (locale)
                    {
                        case Locale.koKR:
                        case Locale.zhCN:
                        case Locale.zhTW:
                            {
                                int length = Math.Min(str.Length, replacementWord.Length);
                                for (int i = 0; i < length; ++i)
                                {
                                    if (str[i] >= 'A' && str[i] <= 'Z')
                                        result += char.ToUpper(replacementWord[i]);
                                    else
                                        result += replacementWord[i];
                                }
                                break;
                            }
                        default:
                            {
                                int length = Math.Min(str.Length, replacementWord.Length);
                                for (int i = 0; i < length; ++i)
                                {
                                    if (char.IsUpper(str[i]))
                                        result += char.ToUpper(replacementWord[i]);
                                    else
                                        result += char.ToLower(replacementWord[i]);
                                }
                                break;
                            }
                    }
                }

                result += ' ';
            }

            if (!result.IsEmpty())
                result.Remove(result.Length - 1);

            return result;
        }

        public bool IsLanguageExist(Language languageId)
        {
            return CliDB.LanguagesStorage.HasRecord((uint)languageId);
        }

        public List<LanguageDesc> GetLanguageDescById(Language languageId)
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

        void StripHyperlinks(string source, ref string dest)
        {
            char[] destChar = new char[source.Length];

            int destSize = 0;
            bool skipSquareBrackets = false;
            for (int i = 0; i < source.Length; ++i)
            {
                char c = source[i];
                if (c != '|')
                {
                    if (!skipSquareBrackets || (c != '[' && c != ']'))
                        destChar[destSize++] = source[i];

                    continue;
                }

                if (i + 1 >= source.Length)
                    break;

                switch (source[i + 1])
                {
                    case 'c':
                    case 'C':
                        // skip color
                        if (i + 2 >= source.Length)
                            break;

                        if (source[i + 2] == 'n')
                            i = source.IndexOf(':', i); // numeric color id
                        else
                            i += 9;
                        break;
                    case 'r':
                        ++i;
                        break;
                    case 'H':
                        // skip just past first |h
                        i = source.IndexOf("|h", i);
                        if (i != -1)
                            i += 2;
                        skipSquareBrackets = true;
                        break;
                    case 'h':
                        ++i;
                        skipSquareBrackets = false;
                        break;
                    case 'T':
                        // skip just past closing |t
                        i = source.IndexOf("|t", i);
                        if (i != -1)
                            i += 2;
                        break;
                    default:
                        break;
                }
            }

            dest = new string(destChar, 0, destSize);
        }

        void ReplaceUntranslatableCharactersWithSpace(ref string text)
        {
            var chars = text.ToCharArray();
            for (var i = 0; i < text.Length; ++i)
            {
                var w = chars[i];
                if (!Extensions.isExtendedLatinCharacter(w) && !char.IsNumber(w) && w <= 0xFF && w != '\\')
                    chars[i] = ' ';
            }

            text = new string(chars);
        }

        static char upper_backslash(char c)
        {
            if (c == '/')
                return '\\';
            if (c >= 'a' && c <= 'z')
                return (char)('A' + (char)(c - 'a'));
            else
                return c;
        }

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
    }
}
