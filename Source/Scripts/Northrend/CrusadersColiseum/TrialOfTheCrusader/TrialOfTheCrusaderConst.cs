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
using Game.Entities;
using Game.Maps;

namespace Scripts.Northrend.CrusadersColiseum.TrialOfTheCrusader
{
    struct DataTypes
    {
        public const uint BossBeasts = 0;
        public const uint BossJaraxxus = 1;
        public const uint BossCrusaders = 2;
        public const uint BossValkiries = 3;
        public const uint BossLichKing = 4;    // Not Really A Boss But Oh Well
        public const uint BossAnubarak = 5;
        public const uint MaxEncounters = 6;

        public const uint Counter = 8;
        public const uint Event = 9;

        public const uint EventTimer = 101;
        public const uint EventNpc = 102;
        public const uint NorthrendBeasts = 103;

        public const uint SnoboldCount = 301;
        public const uint MistressOfPainCount = 302;
        public const uint TributeToImmortalityEligible = 303;

        public const uint Increase = 501;
        public const uint Decrease = 502;
    }

    struct Spells
    {
        public const uint WilfredPortal = 68424;
        public const uint JaraxxusChains = 67924;
        public const uint CorpseTeleport = 69016;
        public const uint DestroyFloorKnockup = 68193;
    }

    struct WorldStateIds
    {
        public const uint Show = 4390;
        public const uint Count = 4389;
    }

    enum AnnouncerMessages
    {
        Beasts = 724001,
        Jaraxxus = 724002,
        Crusaders = 724003,
        Valkiries = 724004,
        LichKing = 724005,
        Anubarak = 724006
    }

    public struct CreatureIds
    {
        public const uint Barrent = 34816;
        public const uint Tirion = 34996;
        public const uint TirionFordring = 36095;
        public const uint ArgentMage = 36097;
        public const uint Fizzlebang = 35458;
        public const uint Garrosh = 34995;
        public const uint Varian = 34990;
        public const uint LichKing = 35877;

        public const uint Thrall = 34994;
        public const uint Proudmoore = 34992;
        public const uint WilfredPortal = 17965;
        public const uint Trigger = 35651;

        public const uint Icehowl = 34797;
        public const uint Gormok = 34796;
        public const uint Dreadscale = 34799;
        public const uint Acidmaw = 35144;

        public const uint Jaraxxus = 34780;

        public const uint ChampionsController = 34781;

        public const uint AllianceDeathKnight = 34461;
        public const uint AllianceDruidBalance = 34460;
        public const uint AllianceDruidRestoration = 34469;
        public const uint AllianceHunter = 34467;
        public const uint AllianceMage = 34468;
        public const uint AlliancePaladinHoly = 34465;
        public const uint AlliancePaladinRetribution = 34471;
        public const uint AlliancePriestDiscipline = 34466;
        public const uint AlliancePriestShadow = 34473;
        public const uint AllianceRogue = 34472;
        public const uint AllianceShamanEnhancement = 34463;
        public const uint AllianceShamanRestoration = 34470;
        public const uint AllianceWarlock = 34474;
        public const uint AllianceWarrior = 34475;

        public const uint HordeDeathKnight = 34458;
        public const uint HordeDruidBalance = 34451;
        public const uint HordeDruidRestoration = 34459;
        public const uint HordeHunter = 34448;
        public const uint HordeMage = 34449;
        public const uint HordePaladinHoly = 34445;
        public const uint HordePaladinRetribution = 34456;
        public const uint HordePriestDiscipline = 34447;
        public const uint HordePriestShadow = 34441;
        public const uint HordeRogue = 34454;
        public const uint HordeShamanEnhancement = 34455;
        public const uint HordeShamanRestoration = 34444;
        public const uint HordeWarlock = 34450;
        public const uint HordeWarrior = 34453;

        public const uint Lightbane = 34497;
        public const uint Darkbane = 34496;

        public const uint DarkEssence = 34567;
        public const uint LightEssence = 34568;

        public const uint Anubarak = 34564;
    }

    struct GameObjectIds
    {
        public const uint CrusadersCache10 = 195631;
        public const uint CrusadersCache25 = 195632;
        public const uint CrusadersCache10H = 195633;
        public const uint CrusadersCache25H = 195635;

        // Tribute Chest (Heroic)
        // 10-Man Modes
        public const uint TributeChest10h25 = 195668; // 10man 01-24 Attempts
        public const uint TributeChest10h45 = 195667; // 10man 25-44 Attempts
        public const uint TributeChest10h50 = 195666; // 10man 45-49 Attempts
        public const uint TributeChest10h99 = 195665; // 10man 50 Attempts
                                                      // 25-Man Modes
        public const uint TributeChest25h25 = 195672; // 25man 01-24 Attempts
        public const uint TributeChest25h45 = 195671; // 25man 25-44 Attempts
        public const uint TributeChest25h50 = 195670; // 25man 45-49 Attempts
        public const uint TributeChest25h99 = 195669; // 25man 50 Attempts

        public const uint ArgentColiseumFloor = 195527; //20943
        public const uint MainGateDoor = 195647;
        public const uint EastPortcullis = 195648;
        public const uint WebDoor = 195485;
        public const uint PortalToDalaran = 195682;
    }

    struct AchievementData
    {
        // Northrend Beasts
        public const uint UpperBackPain10Player = 11779;
        public const uint UpperBackPain10PlayerHeroic = 11802;
        public const uint UpperBackPain25Player = 11780;
        public const uint UpperBackPain25PlayerHeroic = 11801;
        // Lord Jaraxxus
        public const uint ThreeSixtyPainSpike10Player = 11838;
        public const uint ThreeSixtyPainSpike10PlayerHeroic = 11861;
        public const uint ThreeSixtyPainSpike25Player = 11839;
        public const uint ThreeSixtyPainSpike25PlayerHeroic = 11862;
        // Tribute
        public const uint ATributeToSkill10Player = 12344;
        public const uint ATributeToSkill25Player = 12338;
        public const uint ATributeToMadSkill10Player = 12347;
        public const uint ATributeToMadSkill25Player = 12341;
        public const uint ATributeToInsanity10Player = 12349;
        public const uint ATributeToInsanity25Player = 12343;
        public const uint ATributeToImmortalityHorde = 12358;
        public const uint ATributeToImmortalityAlliance = 12359;
        public const uint ATributeToDedicatedInsanity = 12360;
        public const uint RealmFirstGrandCrusader = 12350;

        // Dummy Spells - Not Existing In Dbc But We Don'T Need That
        public const uint SpellWormsKilledIn10Seconds = 68523;
        public const uint SpellChampionsKilledInMinute = 68620;
        public const uint SpellDefeatFactionChampions = 68184;
        public const uint SpellTraitorKing10 = 68186;
        public const uint SpellTraitorKing25 = 68515;

        // Timed Events
        public const uint EventStartTwinsFight = 21853;
    }

    struct NorthrendBeasts
    {
        public const uint GormokInProgress = 1000;
        public const uint GormokDone = 1001;
        public const uint SnakesInProgress = 2000;
        public const uint DreadscaleSubmerged = 2001;
        public const uint AcidmawSubmerged = 2002;
        public const uint SnakesSpecial = 2003;
        public const uint SnakesDone = 2004;
        public const uint IcehowlInProgress = 3000;
        public const uint IcehowlDone = 3001;
    }

    struct Texts
    {
        // Highlord Tirion Fordring - 34996
        public const uint Stage_0_01 = 0;
        public const uint Stage_0_02 = 1;
        public const uint Stage_0_04 = 2;
        public const uint Stage_0_05 = 3;
        public const uint Stage_0_06 = 4;
        public const uint Stage_0_Wipe = 5;
        public const uint Stage_1_01 = 6;
        public const uint Stage_1_07 = 7;
        public const uint Stage_1_08 = 8;
        public const uint Stage_1_11 = 9;
        public const uint Stage_2_01 = 10;
        public const uint Stage_2_03 = 11;
        public const uint Stage_2_06 = 12;
        public const uint Stage_3_01 = 13;
        public const uint Stage_3_02 = 14;
        public const uint Stage_4_01 = 15;
        public const uint Stage_4_03 = 16;

        // Varian Wrynn
        public const uint Stage_0_03a = 0;
        public const uint Stage_1_10 = 1;
        public const uint Stage_2_02a = 2;
        public const uint Stage_2_04a = 3;
        public const uint Stage_2_05a = 4;
        public const uint Stage_3_03a = 5;

        // Garrosh
        public const uint Stage_0_03h = 0;
        public const uint Stage_1_09 = 1;
        public const uint Stage_2_02h = 2;
        public const uint Stage_2_04h = 3;
        public const uint Stage_2_05h = 4;
        public const uint Stage_3_03h = 5;

        // Wilfred Fizzlebang
        public const uint Stage_1_02 = 0;
        public const uint Stage_1_03 = 1;
        public const uint Stage_1_04 = 2;
        public const uint Stage_1_06 = 3;

        // Lord Jaraxxus
        public const uint Stage_1_05 = 0;

        //  The Lich King
        public const uint Stage_4_02 = 0;
        public const uint Stage_4_05 = 1;
        public const uint Stage_4_04 = 2;

        // Highlord Tirion Fordring - 36095
        public const uint Stage_4_06 = 0;
        public const uint Stage_4_07 = 1;
    }

    struct MiscData
    {
        public const uint DespawnTime = 300000;

        public const uint DisplayIdDestroyedFloor = 9060;

        public static BossBoundaryEntry[] boundaries =
        {
            new BossBoundaryEntry(DataTypes.BossBeasts, new CircleBoundary(new Position(563.26f, 139.6f), 75.0)),
            new BossBoundaryEntry(DataTypes.BossJaraxxus, new CircleBoundary(new Position(563.26f, 139.6f), 75.0)),
            new BossBoundaryEntry(DataTypes.BossCrusaders, new CircleBoundary(new Position(563.26f, 139.6f), 75.0)),
            new BossBoundaryEntry(DataTypes.BossValkiries, new CircleBoundary(new Position(563.26f, 139.6f), 75.0)),
            new BossBoundaryEntry(DataTypes.BossAnubarak, new EllipseBoundary(new Position(746.0f, 135.0f), 100.0, 75.0))
        };

        public static _Messages[] _GossipMessage =
        {
            new _Messages(AnnouncerMessages.Beasts, eTradeskill.GossipActionInfoDef + 1, false, DataTypes.BossBeasts),
            new _Messages(AnnouncerMessages.Jaraxxus, eTradeskill.GossipActionInfoDef + 2, false, DataTypes.BossJaraxxus),
            new _Messages(AnnouncerMessages.Crusaders, eTradeskill.GossipActionInfoDef + 3, false, DataTypes.BossCrusaders),
            new _Messages(AnnouncerMessages.Valkiries, eTradeskill.GossipActionInfoDef + 4, false, DataTypes.BossValkiries),
            new _Messages(AnnouncerMessages.LichKing, eTradeskill.GossipActionInfoDef + 5, false, DataTypes.BossAnubarak),
            new _Messages(AnnouncerMessages.Anubarak, eTradeskill.GossipActionInfoDef + 6, true, DataTypes.BossAnubarak)
        };

        public static Position[] ToCSpawnLoc =
        {
            new Position(563.912f, 261.625f, 394.73f, 4.70437f),  //  0 Center
            new Position( 575.451f, 261.496f, 394.73f,  4.6541f),  //  1 Left
            new Position( 549.951f,  261.55f, 394.73f, 4.74835f)   //  2 Right
        };

        public static Position[] ToCCommonLoc =
        {
            new Position(559.257996f, 90.266197f, 395.122986f, 0),  //  0 Barrent

            new Position(563.672974f, 139.571f, 393.837006f, 0),    //  1 Center
            new Position(563.833008f, 187.244995f, 394.5f, 0),      //  2 Backdoor
            new Position(577.347839f, 195.338888f, 395.14f, 0),     //  3 - Right
            new Position(550.955933f, 195.338888f, 395.14f, 0),     //  4 - Left
            new Position(563.833008f, 195.244995f, 394.585561f, 0), //  5 - Center
            new Position(573.5f, 180.5f, 395.14f, 0),               //  6 Move 0 Right
            new Position(553.5f, 180.5f, 395.14f, 0),               //  7 Move 0 Left
            new Position(573.0f, 170.0f, 395.14f, 0),               //  8 Move 1 Right
            new Position(555.5f, 170.0f, 395.14f, 0),               //  9 Move 1 Left
            new Position(563.8f, 216.1f, 395.1f, 0),                // 10 Behind the door

            new Position(575.042358f, 195.260727f, 395.137146f, 0), // 5
            new Position(552.248901f, 195.331955f, 395.132658f, 0), // 6
            new Position(573.342285f, 195.515823f, 395.135956f, 0), // 7
            new Position(554.239929f, 195.825577f, 395.137909f, 0), // 8
            new Position(571.042358f, 195.260727f, 395.137146f, 0), // 9
            new Position(556.720581f, 195.015472f, 395.132658f, 0), // 10
            new Position(569.534119f, 195.214478f, 395.139526f, 0), // 11
            new Position(569.231201f, 195.941071f, 395.139526f, 0), // 12
            new Position(558.811610f, 195.985779f, 394.671661f, 0), // 13
            new Position(567.641724f, 195.351501f, 394.659943f, 0), // 14
            new Position(560.633972f, 195.391708f, 395.137543f, 0), // 15
            new Position(565.816956f, 195.477921f, 395.136810f, 0)  // 16
        };

        public static Position[] JaraxxusLoc =
        {
            new Position(508.104767f, 138.247345f, 395.128052f, 0), // 0 - Fizzlebang start location
            new Position(548.610596f, 139.807800f, 394.321838f, 0), // 1 - fizzlebang end
            new Position(581.854187f, 138.0f, 394.319f, 0),         // 2 - Portal Right
            new Position(550.558838f, 138.0f, 394.319f, 0)          // 3 - Portal Left
        };

        public static Position[] FactionChampionLoc =
        {
            new Position(514.231f, 105.569f, 418.234f, 0),               //  0 - Horde Initial Pos 0
            new Position(508.334f, 115.377f, 418.234f, 0),               //  1 - Horde Initial Pos 1
            new Position(506.454f, 126.291f, 418.234f, 0),               //  2 - Horde Initial Pos 2
            new Position(506.243f, 106.596f, 421.592f, 0),               //  3 - Horde Initial Pos 3
            new Position(499.885f, 117.717f, 421.557f, 0),               //  4 - Horde Initial Pos 4

            new Position(613.127f, 100.443f, 419.74f, 0),                //  5 - Ally Initial Pos 0
            new Position(621.126f, 128.042f, 418.231f, 0),               //  6 - Ally Initial Pos 1
            new Position(618.829f, 113.606f, 418.232f, 0),               //  7 - Ally Initial Pos 2
            new Position(625.845f, 112.914f, 421.575f, 0),               //  8 - Ally Initial Pos 3
            new Position(615.566f, 109.653f, 418.234f, 0),               //  9 - Ally Initial Pos 4

            new Position(535.469f, 113.012f, 394.66f, 0),                // 10 - Horde Final Pos 0
            new Position(526.417f, 137.465f, 394.749f, 0),               // 11 - Horde Final Pos 1
            new Position(528.108f, 111.057f, 395.289f, 0),               // 12 - Horde Final Pos 2
            new Position(519.92f, 134.285f, 395.289f, 0),                // 13 - Horde Final Pos 3
            new Position(533.648f, 119.148f, 394.646f, 0),               // 14 - Horde Final Pos 4
            new Position(531.399f, 125.63f, 394.708f, 0),                // 15 - Horde Final Pos 5
            new Position(528.958f, 131.47f, 394.73f, 0),                 // 16 - Horde Final Pos 6
            new Position(526.309f, 116.667f, 394.833f, 0),               // 17 - Horde Final Pos 7
            new Position(524.238f, 122.411f, 394.819f, 0),               // 18 - Horde Final Pos 8
            new Position(521.901f, 128.488f, 394.832f, 0)                // 19 - Horde Final Pos 9
        };

        public static Position[] TwinValkyrsLoc =
        {
            new Position(586.060242f, 117.514809f, 394.41f, 0), // 0 - Dark essence 1
            new Position(541.602112f, 161.879837f, 394.41f, 0), // 1 - Dark essence 2
            new Position(541.021118f, 117.262932f, 394.41f, 0), // 2 - Light essence 1
            new Position(586.200562f, 162.145523f, 394.41f, 0)  // 3 - Light essence 2
        };

        public static Position[] LichKingLoc =
        {
            new Position(563.549f, 152.474f, 394.393f, 0),          // 0 - Lich king start
            new Position(563.547f, 141.613f, 393.908f, 0)           // 1 - Lich king end
        };

        public static Position[] AnubarakLoc =
        {
            new Position(787.932556f, 133.289780f, 142.612152f, 0),  // 0 - Anub'arak start location
            new Position(695.240051f, 137.834824f, 142.200000f, 0),  // 1 - Anub'arak move point location
            new Position(694.886353f, 102.484665f, 142.119614f, 0),  // 3 - Nerub Spawn
            new Position(694.500671f, 185.363968f, 142.117905f, 0),  // 5 - Nerub Spawn
            new Position(731.987244f, 83.3824690f, 142.119614f, 0),  // 2 - Nerub Spawn
            new Position(740.184509f, 193.443390f, 142.117584f, 0)   // 4 - Nerub Spawn
        };

        public static Position[] EndSpawnLoc =
        {
            new Position(648.9167f, 131.0208f, 141.6161f, 0), // 0 - Highlord Tirion Fordring
            new Position(649.1614f, 142.0399f, 141.3057f, 0), // 1 - Argent Mage
            new Position(644.6250f, 149.2743f, 140.6015f, 0)  // 2 - Portal to Dalaran
        };
    }

    struct _Messages
    {
        public _Messages(AnnouncerMessages _msg, uint _id, bool _state, uint _encounter)
        {
            msgnum = _msg;
            id = _id;
            state = _state;
            encounter = _encounter;
        }

        public AnnouncerMessages msgnum;
        public uint id;
        public bool state;
        public uint encounter;
    }
}
