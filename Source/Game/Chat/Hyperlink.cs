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

using Framework.Constants;
using Framework.IO;
using Game.DataStorage;
using Game.Spells;
using System;
using System.Collections.Generic;

namespace Game.Chat
{
    class Hyperlink
    {
        public static bool TryConsume(out dynamic value, Type type, StringArguments args)
        {
            value = default;

            HyperlinkInfo info = ParseHyperlink(args.GetString());
            // invalid hyperlinks cannot be consumed
            if (info == null)
                return false;

            // store value
            switch (Type.GetTypeCode(type))
            {
                case TypeCode.UInt32:
                {
                    if (!uint.TryParse(info.Data, out uint tempValue))
                        return false;

                    value = tempValue;
                    return true;
                }
                case TypeCode.UInt64:
                {
                    if (!ulong.TryParse(info.Data, out ulong tempValue))
                        return false;

                    value = tempValue;
                    return true;
                }
                case TypeCode.String:
                {
                    value = info.Data;
                    return true;
                }
                case TypeCode.Object:
                    return TryConsumeObject(out value, type, info);
            }

            return false;
        }

        public static bool TryConsumeObject(out dynamic value, Type type, HyperlinkInfo info)
        {
            value = default;

            switch (type.Name)
            {
                case nameof(AchievementRecord):
                {
                    HyperlinkDataTokenizer t = new(info.Data);
                    if (!t.TryConsumeTo(out uint achievementId))
                        return false;

                    value = CliDB.AchievementStorage.LookupByKey(achievementId);
                    return true;
                }
                case nameof(CurrencyTypesRecord):
                {
                    HyperlinkDataTokenizer t = new(info.Data);
                    if (!t.TryConsumeTo(out uint currencyId))
                        return false;

                    value = CliDB.CurrencyTypesStorage.LookupByKey(currencyId);
                    return true;
                }
                case nameof(GameTele):
                {
                    HyperlinkDataTokenizer t = new(info.Data);
                    if (!t.TryConsumeTo(out uint teleId))
                        return false;

                    value = Global.ObjectMgr.GetGameTele(teleId);
                    return true;
                }
                case nameof(SpellInfo):
                {
                    HyperlinkDataTokenizer t = new(info.Data);
                    if (!t.TryConsumeTo(out uint spellId))
                        return false;

                    value = Global.SpellMgr.GetSpellInfo(spellId, Difficulty.None);

                    return true;
                }
            }

            return false;
        }

        public static bool CheckAllLinks(string str)
        {
            // Step 1: Disallow all control sequences except ||, |H, |h, |c and |r
            {
                int pos = 0;
                while ((pos = str.IndexOf('|', pos)) != -1)
                {
                    char next = str[pos + 1];
                    if (next == 'H' || next == 'h' || next == 'c' || next == 'r' || next == '|')
                        pos += 2;
                    else
                        return false;
                }
            }

            // Step 2: Parse all link sequences
            // They look like this: |c<color>|H<linktag>:<linkdata>|h[<linktext>]|h|r
            // - <color> is 8 hex characters AARRGGBB
            // - <linktag> is arbitrary length [a-z_]
            // - <linkdata> is arbitrary length, no | contained
            // - <linktext> is printable
            {
                int pos = 0;
                while ((pos = str.IndexOf('|', pos)) != -1)
                {
                    if (str[pos + 1] == '|') // this is an escaped pipe character (||)
                    {
                        pos += 2;
                        continue;
                    }

                    HyperlinkInfo info = ParseHyperlink(str.Substring(pos));
                    if (info == null)// todo fix me || !ValidateLinkInfo(info))
                        return false;

                    // tag is fine, find the next one
                    pos = str.Length - info.next.Length;
                }
            }

            // all tags are valid
            return true;
        }

        static byte toHex(char c) { return (byte)((c >= '0' && c <= '9') ? c - '0' + 0x10 : (c >= 'a' && c <= 'f') ? c - 'a' + 0x1a : 0x00); }

        //|color|Henchant:recipe_spell_id|h[prof_name: recipe_name]|h|r
        public static HyperlinkInfo ParseHyperlink(string currentString)
        {
            if (currentString.IsEmpty())
                return null;

            int pos = 0;

            //color tag
            if (currentString[pos++] != '|' || currentString[pos++] != 'c')
                return null;

            uint color = 0;
            for (byte i = 0; i < 8; ++i)
            {
                byte hex = toHex(currentString[pos++]);
                if (hex != 0)
                    color = (uint)((int)(color << 4) | (hex & 0xf));
                else
                    return null;
            }

            // link data start tag
            if (currentString[pos++] != '|' || currentString[pos++] != 'H')
                return null;

            // link tag, find next : or |
            int tagStart = pos;
            int tagLength = 0;
            while (pos < currentString.Length && currentString[pos] != '|' && currentString[pos++] != ':') // we only advance pointer to one past if the last thing is : (not for |), this is intentional!
                ++tagLength;

            // ok, link data, skip to next |
            int dataStart = pos;
            int dataLength = 0;
            while (pos < currentString.Length && currentString[pos++] != '|')
                ++dataLength;

            // ok, next should be link data end tag...
            if (currentString[pos++] != 'h')
                return null;

            // then visible link text, starts with [
            if (currentString[pos++] != '[')
                return null;

            // skip until we hit the next ], abort on unexpected |
            int textStart = pos;
            int textLength = 0;
            while (pos < currentString.Length)
            {
                if (currentString[pos] == '|')
                    return null;

                if (currentString[pos++] == ']')
                    break;

                ++textLength;
            }

            // link end tag
            if (currentString[pos++] != '|' || currentString[pos++] != 'h' || currentString[pos++] != '|' || currentString[pos++] != 'r')
                return null;

            // ok, valid hyperlink, return info
            return new HyperlinkInfo(currentString.Substring(pos), color, currentString.Substring(tagStart, tagLength), currentString.Substring(dataStart, dataLength), currentString.Substring(textStart, textLength));
        }
    }

    class HyperlinkInfo
    {
        public HyperlinkInfo(string n = null, uint c = 0, string tag = null, string data = null, string text = null)
        {
            next = n;
            color = new(c);
            Tag = tag;
            Data = data;
            Text = text;
        }

        public string next;
        public HyperlinkColor color;
        public string Tag;
        public string Data;
        public string Text;
    }

    struct HyperlinkColor
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;

        public HyperlinkColor(uint c)
        {
            r = (byte)(c >> 16);
            g = (byte)(c >> 8);
            b = (byte)c;
            a = (byte)(c >> 24);
        }
    }
}
