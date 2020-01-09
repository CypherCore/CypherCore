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

namespace Framework.Constants
{
    [Flags]
    public enum RealmFlags
    {
        None = 0x00,
        VersionMismatch = 0x01,
        Offline = 0x02,
        SpecifyBuild = 0x04,
        Unk1 = 0x08,
        Unk2 = 0x10,
        Recommended = 0x20,
        New = 0x40,
        Full = 0x80
    }

    public enum RealmType
    {
        Normal = 0,
        PVP = 1,
        Normal2 = 4,
        RP = 6,
        RPPVP = 8,

        MaxType = 14,

        FFAPVP = 16                            // custom, free for all pvp mode like arena PvP in all zones except rest activated places and sanctuaries
        // replaced by REALM_PVP in realm list
    }

    public enum RealmZones
    {
        Unknown = 0,                           // Any Language
        Development = 1,                           // Any Language
        UnitedStates = 2,                           // Extended-Latin
        Oceanic = 3,                           // Extended-Latin
        LatinAmerica = 4,                           // Extended-Latin
        Tournament5 = 5,                           // Basic-Latin At Create, Any At Login
        Korea = 6,                           // East-Asian
        Tournament7 = 7,                           // Basic-Latin At Create, Any At Login
        English = 8,                           // Extended-Latin
        German = 9,                           // Extended-Latin
        French = 10,                          // Extended-Latin
        Spanish = 11,                          // Extended-Latin
        Russian = 12,                          // Cyrillic
        Tournament13 = 13,                          // Basic-Latin At Create, Any At Login
        Taiwan = 14,                          // East-Asian
        Tournament15 = 15,                          // Basic-Latin At Create, Any At Login
        China = 16,                          // East-Asian
        Cn1 = 17,                          // Basic-Latin At Create, Any At Login
        Cn2 = 18,                          // Basic-Latin At Create, Any At Login
        Cn3 = 19,                          // Basic-Latin At Create, Any At Login
        Cn4 = 20,                          // Basic-Latin At Create, Any At Login
        Cn5 = 21,                          // Basic-Latin At Create, Any At Login
        Cn6 = 22,                          // Basic-Latin At Create, Any At Login
        Cn7 = 23,                          // Basic-Latin At Create, Any At Login
        Cn8 = 24,                          // Basic-Latin At Create, Any At Login
        Tournament25 = 25,                          // Basic-Latin At Create, Any At Login
        TestServer = 26,                          // Any Language
        Tournament27 = 27,                          // Basic-Latin At Create, Any At Login
        QaServer = 28,                          // Any Language
        Cn9 = 29,                          // Basic-Latin At Create, Any At Login
        TestServer2 = 30,                          // Any Language
        Cn10 = 31,                          // Basic-Latin At Create, Any At Login
        Ctc = 32,
        Cnc = 33,
        Cn14 = 34,                          // Basic-Latin At Create, Any At Login
        Cn269 = 35,                          // Basic-Latin At Create, Any At Login
        Cn37 = 36,                          // Basic-Latin At Create, Any At Login
        Cn58 = 37                           // Basic-Latin At Create, Any At Login
    }
}
