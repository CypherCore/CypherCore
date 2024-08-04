// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.BattleGrounds;
using Game.Entities;
using Game.Maps;
using Game.Scripting;
using System;
using System.Collections.Generic;

namespace Scripts.Battlegrounds.AlteracValley
{
    enum AlteracValleyMine
    {
        North = 0,
        South
    }

    enum TextIds
    {
        // Herold
        // Towers/Graveyards = 1 - 60
        ColdtoothMineAllianceTaken = 61,
        IrondeepMineAllianceTaken = 62,
        ColdtoothMineHordeTaken = 63,
        IrondeepMineHordeTaken = 64,
        FrostwolfGeneralDead = 65, /// @Todo: Sound Is Missing
        StormpikeGeneralDead = 66, /// @Todo: Sound Is Missing
        AllianceWins = 67, // Nyi /// @Todo: Sound Is Missing
        HordeWins = 68, // Nyi /// @Todo: Sound Is Missing

        // Taskmaster Snivvle
        SnivvleRandom = 0
    }

    enum BroadcastTextIds
    {
        StartOneMinute = 10638,
        StartHalfMinute = 10639,
        BattleHasBegun = 10640,

        AllianceNearLose = 23210,
        HordeNearLose = 23211
    }

    enum Nodes
    {
        FirstaidStation = 0,
        StormpikeGrave = 1,
        StoneheartGrave = 2,
        SnowfallGrave = 3,
        IcebloodGrave = 4,
        FrostwolfGrave = 5,
        FrostwolfHut = 6,
        DunbaldarSouth = 7,
        DunbaldarNorth = 8,
        IcewingBunker = 9,
        StoneheartBunker = 10,
        IcebloodTower = 11,
        TowerPoint = 12,
        FrostwolfEtower = 13,
        FrostwolfWtower = 14,

        Max = 15
    }

    enum BuffIds
    {
        /// @todo: Add all other buffs here
        Armor = 21163,
        ACaptain = 23693, //the buff which the alliance captain does
        HCaptain = 22751 //the buff which the horde captain does
    }

    enum SoundIds
    {
        NearVictory = 8456, /// @todo: Not confirmed yet

        AllianceAssaults = 8212, //tower, grave + enemy boss if someone tries to attack him
        HordeAssaults = 8174,
        AllianceGood = 8173, //if something good happens for the team:  wins(maybe only through killing the boss), captures mine or grave, destroys tower and defends grave
        HordeGood = 8213,
        BothTowerDefend = 8192,

        AllianceCaptain = 8232, //gets called when someone attacks them and at the beginning after 5min+rand(x)*10sec (maybe buff)
        HordeCaptain = 8333
    }

    enum ObjectIds
    {
        //Cause The Mangos-System Is A Bit Different, We Don't Use The Right Go-Ids For Every Node.. If We Want To Be 100% Like Another Big Server, We Must Take One Object For Every Node
        //Snowfall 4flags As Eyecandy 179424 (Alliance Neutral)
        //Banners - Stolen From BattlegroundAb.H ;-)
        BannerA = 178925, // Can Only Be Used By Horde
        BannerH = 178943, // Can Only Be Used By Alliance
        BannerContA = 178940, // Can Only Be Used By Horde
        BannerContH = 179435, // Can Only Be Used By Alliance

        BannerAB = 178365,
        BannerHB = 178364,
        BannerContAB = 179286,
        BannerContHB = 179287,
        BannerSnowfallN = 180418,

        //Snowfall Eyecandy Banner:
        SnowfallCandyA = 179044,
        SnowfallCandyPa = 179424,
        SnowfallCandyH = 179064,
        SnowfallCandyPh = 179425,

        //Banners On Top Of Towers:
        TowerBannerA = 178927, //[Ph] Alliance A1 Tower Banner Big
        TowerBannerH = 178955, //[Ph] Horde H1 Tower Banner Big
        TowerBannerPa = 179446, //[Ph] Alliance H1 Tower Pre-Banner Big
        TowerBannerPh = 179436, //[Ph] Horde A1 Tower Pre-Banner Big

        //Auras
        AuraA = 180421,
        AuraH = 180422,
        AuraN = 180423,
        AuraAS = 180100,
        AuraHS = 180101,
        AuraNS = 180102,

        Gate = 180424,
        GhostGate = 180322,

        //Mine Supplies
        MineN = 178785,
        MineS = 178784,

        Fire = 179065,
        Smoke = 179066,

        // Towers
        SouthBunkerControlledTowerBanner = 178927,
        SouthBunkerControlledBanner = 178925,
        SouthBunkerContestedBanner = 179435,
        SouthBunkerContestedTowerBanner = 179436,

        NorthBunkerControlledTowerBanner = 178932,
        NorthBunkerControlledBanner = 178929,
        NorthBunkerContestedBanner = 179439,
        NorthBunkerContestedTowerBanner = 179440,

        EastTowerControlledTowerBanner = 178956,
        EastTowerControlledBanner = 178944,
        EastTowerContestedBanner = 179449,
        EastTowerContestedTowerBanner = 179450,

        WestTowerControlledTowerBanner = 178955,
        WestTowerControlledBanner = 178943,
        WestTowerContestedBanner = 179445,
        WestTowerContestedTowerBanner = 179446,

        TowerPointControlledTowerBanner = 178957,
        TowerPointControlledBanner = 178945,
        TowerPointContestedBanner = 179453,
        TowerPointContestedTowerBanner = 179454,

        IcebloodTowerControlledTowerBanner = 178958,
        IcebloodTowerControlledBanner = 178946,
        IcebloodTowerContestedBanner = 178940,
        IcebloodTowerContestedTowerBanner = 179458,

        StonehearthBunkerControlledTowerBanner = 178948,
        StonehearthBunkerControlledBanner = 178936,
        StonehearthBunkerContestedBanner = 179443,
        StonehearthBunkerContestedTowerBanner = 179444,

        IcewingBunkerControlledTowerBanner = 178947,
        IcewingBunkerControlledBanner = 178935,
        IcewingBunkerContestedBanner = 179441,
        IcewingBunkerContestedTowerBanner = 179442,

        // Graveyards
        AidStationAllianceControlled = 179465,
        AidStationHordeContested = 179468,
        AidStationHordeControlled = 179467,
        AidStationAllianceContested = 179466,

        StormpikeAllianceControlled = 178389,
        StormpikeHordeContested = 179287,
        StormpikeHordeControlled = 178388,
        StormpikeAllianceContested = 179286,

        StonehearthHordeContested = 179310,
        StonehearthHordeControlled = 179285,
        StonehearthAllianceContested = 179308,
        StonehearthAllianceControlled = 179284,

        SnowfallNeutral = 180418,
        SnowfallHordeContested = 180420,
        SnowfallAllianceContested = 180419,
        SnowfallHordeControlled = 178364,
        SnowfallAllianceControlled = 178365,

        IcebloodHordeControlled = 179483,
        IcebloodAllianceContested = 179482,
        IcebloodAllianceControlled = 179481,
        IcebloodHordeContested = 179484,

        FrostwolfHordeControlled = 178393,
        FrostwolfAllianceContested = 179304,
        FrostwolfAllianceControlled = 178394,
        FrostwolfHordeContested = 179305,

        FrostwolfHutHordeControlled = 179472,
        FrostwolfHutAllianceContested = 179471,
        FrostwolfHutAllianceControlled = 179470,
        FrostwolfHutHordeContested = 179473
    }

    enum WorldStateIds
    {
        AllianceReinforcements = 3127,
        HordeReinforcements = 3128,
        ShowHordeReinforcements = 3133,
        ShowAllianceReinforcements = 3134,
        MaxReinforcements = 3136,

        // Graves
        // Alliance
        //Stormpike First Aid Station
        StormpikeAidStationAllianceControlled = 1325,
        StormpikeAidStationInConflictAllianceAttacking = 1326,
        StormpikeAidStationHordeControlled = 1327,
        StormpikeAidStationInConflictHordeAttacking = 1328,
        //Stormpike Graveyard
        StormpikeGraveyardAllianceControlled = 1333,
        StormpikeGraveyardInConflictAllianceAttacking = 1335,
        StormpikeGraveyardHordeControlled = 1334,
        StormpikeGraveyardInConflictHordeAttacking = 1336,
        //Stoneheart Grave
        StonehearthGraveyardAllianceControlled = 1302,
        StonehearthGraveyardInConflictAllianceAttacking = 1304,
        StonehearthGraveyardHordeControlled = 1301,
        StonehearthGraveyardInConflictHordeAttacking = 1303,
        //Neutral
        //Snowfall Grave
        SnowfallGraveyardUncontrolled = 1966,
        SnowfallGraveyardAllianceControlled = 1341,
        SnowfallGraveyardInConflictAllianceAttacking = 1343,
        SnowfallGraveyardHordeControlled = 1342,
        SnowfallGraveyardInConflictHordeAttacking = 1344,
        //Horde
        //Iceblood Grave
        IcebloodGraveyardAllianceControlled = 1346,
        IcebloodGraveyardInConflictAllianceAttacking = 1348,
        IcebloodGraveyardHordeControlled = 1347,
        IcebloodGraveyardInConflictHordeAttacking = 1349,
        //Frostwolf Grave
        FrostwolfGraveyardAllianceControlled = 1337,
        FrostwolfGraveyardInConflictAllianceAttacking = 1339,
        FrostwolfGraveyardHordeControlled = 1338,
        FrostwolfGraveyardInConflictHordeAttacking = 1340,
        //Frostwolf Hut
        FrostwolfReliefHutAllianceControlled = 1329,
        FrostwolfReliefHutInConflictAllianceAttacking = 1331,
        FrostwolfReliefHutHordeControlled = 1330,
        FrostwolfReliefHutInConflictHordeAttacking = 1332,

        //Towers
        //Alliance
        //Dunbaldar South Bunker
        DunBaldarSouthBunkerOwner = 1181,
        DunBaldarSouthBunkerAllianceControlled = 1361,
        DunBaldarSouthBunkerDestroyed = 1370,
        DunBaldarSouthBunkerInConflictHordeAttacking = 1378,
        DunBaldarSouthBunkerInConflictAllianceAttacking = 1374, // Unused
                                                                //Dunbaldar North Bunker
        DunBaldarNorthBunkerOwner = 1182,
        DunBaldarNorthBunkerAllianceControlled = 1362,
        DunBaldarNorthBunkerDestroyed = 1371,
        DunBaldarNorthBunkerInConflictHordeAttacking = 1379,
        DunBaldarNorthBunkerInConflictAllianceAttacking = 1375, // Unused
                                                                //Icewing Bunker
        IcewingBunkerOwner = 1183,
        IcewingBunkerAllianceControlled = 1363,
        IcewingBunkerDestroyed = 1372,
        IcewingBunkerInConflictHordeAttacking = 1380,
        IcewingBunkerInConflictAllianceAttacking = 1376, // Unused
                                                         //Stoneheart Bunker
        StonehearthBunkerOwner = 1184,
        StonehearthBunkerAllianceControlled = 1364,
        StonehearthBunkerDestroyed = 1373,
        StonehearthBunkerInConflictHordeAttacking = 1381,
        StonehearthBunkerInConflictAllianceAttacking = 1377, // Unused
                                                             //Horde
                                                             //Iceblood Tower
        IcebloodTowerOwner = 1187,
        IcebloodTowerDestroyed = 1368,
        IcebloodTowerHordeControlled = 1385,
        IcebloodTowerInConflictAllianceAttacking = 1390,
        IcebloodTowerInConflictHordeAttacking = 1395, // Unused
                                                      //Tower Point
        TowerPointOwner = 1188,
        TowerPointDestroyed = 1367,
        TowerPointHordeControlled = 1384,
        TowerPointInConflictAllianceAttacking = 1389,
        TowerPointInConflictHordeAttacking = 1394, // Unused
                                                   //Frostwolf West
        WestFrostwolfTowerOwner = 1185,
        WestFrostwolfTowerDestroyed = 1365,
        WestFrostwolfTowerHordeControlled = 1382,
        WestFrostwolfTowerInConflictAllianceAttacking = 1387,
        WestFrostwolfTowerInConflictHordeAttacking = 1392, // Unused
                                                           //Frostwolf East
        EastFrostwolfTowerOwner = 1186,
        EastFrostwolfTowerDestroyed = 1366,
        EastFrostwolfTowerHordeControlled = 1383,
        EastFrostwolfTowerInConflictAllianceAttacking = 1388,
        EastFrostwolfTowerInConflictHordeAttacking = 1393, // Unused

        //Mines
        IrondeepMineOwner = 801,
        IrondeepMineTroggControlled = 1360,
        IrondeepMineAllianceControlled = 1358,
        IrondeepMineHordeControlled = 1359,

        ColdtoothMineOwner = 804,
        ColdtoothMineKoboldControlled = 1357,
        ColdtoothMineAllianceControlled = 1355,
        ColdtoothMineHordeControlled = 1356,

        //Turnins
        IvusStormCrystalCount = 1043,
        IvusStormCrystalMax = 1044,
        LokholarStormpikeSoldiersBloodCount = 923,
        LokholarStormpikeSoldiersBloodMax = 922,

        //Bosses
        DrektharAlive = 601,
        VandaarAlive = 602,

        //Captains
        GalvagarAlive = 1352,
        BalindaAlive = 1351
    }

    enum PointStates
    {
        Neutral = 0,
        Assaulted = 1,
        Destroyed = 2,
        Controled = 3
    }

    struct MiscConst
    {
        public const int ScoreInitialPoints = 700;
        public const uint EventStartBattle = 9166; // Achievement: The Alterac Blitz
        public static TimeSpan ResourceTimer = TimeSpan.FromSeconds(45);


        public const uint DataDefenderTierHorde = 1;
        public const uint DataDefenderTierAlliance = 2;

        public const uint NearLosePoints = 140;


        public static StaticNodeInfo[] BGAVNodeInfo =
        [
            new(Nodes.FirstaidStation,  47, 48, 45, 46,  1325, 1326, 1327, 1328, 0,  "bg_av_herald_stormpike_aid_station_alliance", "bg_av_herald_stormpike_aid_station_horde"), // Stormpike First Aid Station
            new(Nodes.StormpikeGrave,   1,  2,  3,  4,   1333, 1335, 1334, 1336, 0,  "bg_av_herald_stormpike_alliance", "bg_av_herald_stormpike_horde"), // Stormpike Graveyard
            new(Nodes.StoneheartGrave,  55, 56, 53, 54,  1302, 1304, 1301, 1303, 0,  "bg_av_herald_stonehearth_alliance", "bg_av_herald_stonehearth_horde"), // Stoneheart Graveyard
            new(Nodes.SnowfallGrave,    5,  6,  7,  8,   1341, 1343, 1342, 1344, 0,  "bg_av_herald_snowfall_alliance", "bg_av_herald_snowfall_horde"), // Snowfall Graveyard
            new(Nodes.IcebloodGrave,    59, 60, 57, 58,  1346, 1348, 1347, 1349, 0,  "bg_av_herald_iceblood_alliance", "bg_av_herald_iceblood_horde"), // Iceblood Graveyard
            new(Nodes.FrostwolfGrave,   9, 10, 11, 12,   1337, 1339, 1338, 1340, 0,   "bg_av_herald_frostwolf_alliance", "bg_av_herald_frostwolf_horde"), // Frostwolf Graveyard
            new(Nodes.FrostwolfHut,     51, 52, 49, 50,  1329, 1331, 1330, 1332, 0,   "bg_av_herald_frostwolf_hut_alliance", "bg_av_herald_frostwolf_hut_horde"), // Frostwolf Hut
            new(Nodes.DunbaldarSouth,   16, 15, 14, 13,  1361, 1375, 1370, 1378, 1181, "bg_av_herald_south_bunker_defend", "bg_av_herald_south_bunker_attack"), // Dunbaldar South Bunker
            new(Nodes.DunbaldarNorth,   20, 19, 18, 17,  1362, 1374, 1371, 1379, 1182, "bg_av_herald_north_bunker_defend", "bg_av_herald_south_bunker_attack"), // Dunbaldar North Bunker
            new(Nodes.IcewingBunker,    24, 23, 22, 21,  1363, 1376, 1372, 1380, 1183, "bg_av_herald_icewing_bunker_defend", "bg_av_herald_icewing_bunker_attack"), // Icewing Bunker
            new(Nodes.StoneheartBunker, 28, 27, 26, 25,  1364, 1377, 1373, 1381, 1184, "bg_av_herald_stonehearth_bunker_defend", "bg_av_herald_stonehearth_bunker_attack"), // Stoneheart Bunker
            new(Nodes.IcebloodTower,    44, 43, 42, 41,  1368, 1390, 1385, 1395, 1188, "bg_av_herald_iceblood_tower_defend", "bg_av_herald_iceblood_tower_attack"), // Iceblood Tower
            new(Nodes.TowerPoint,       40, 39, 38, 37,  1367, 1389, 1384, 1394, 1187, "bg_av_herald_tower_point_defend", "bg_av_herald_tower_point_attack"), // Tower Point
            new(Nodes.FrostwolfEtower,  36, 35, 34, 33,  1366, 1388, 1383, 1393, 1186, "bg_av_herald_east_tower_defend", "bg_av_herald_east_tower_attack"), // Frostwolf East Tower
            new(Nodes.FrostwolfWtower,  32, 31, 30, 29,  1365, 1387, 1382, 1392, 1185, "bg_av_herald_west_tower_defend", "bg_av_herald_west_tower_attack"), // Frostwolf West Tower
        ];
    }

    class StaticNodeInfo
    {
        public Nodes NodeId;
        public byte AllianceCaptureTextId;
        public byte AllianceAttackTextId;
        public byte HordeCaptureTextId;
        public byte HordeAttackTextId;

        public int AllianceControlWorldStateId;
        public int AllianceAssaultWorldStateId;
        public int HordeControlWorldStateId;
        public int HordeAssaultWorldStateId;
        public int OwnerWorldStateId;

        public string AllianceOrDefendStringId;
        public string HordeOrDestroyStringId;

        public StaticNodeInfo(Nodes nodeId, byte allianceCaptureTextId, byte allianceAttackTextId, byte hordeCaptureTextId, byte hordeAttackTextId, int allianceControlWorldStateId, int allianceAssaultWorldStateId, int hordeControlWorldStateId, int hordeAssaultWorldStateId, int ownerWorldStateId, string allianceOrDefendStringId, string hordeOrDestroyStringId)
        {
            NodeId = nodeId;
            AllianceCaptureTextId = allianceCaptureTextId;
            AllianceAttackTextId = allianceAttackTextId;
            HordeCaptureTextId = hordeCaptureTextId;
            HordeAttackTextId = hordeAttackTextId;
            AllianceControlWorldStateId = allianceControlWorldStateId;
            AllianceAssaultWorldStateId = allianceAssaultWorldStateId;
            HordeControlWorldStateId = hordeControlWorldStateId;
            HordeAssaultWorldStateId = hordeAssaultWorldStateId;
            OwnerWorldStateId = ownerWorldStateId;
            AllianceOrDefendStringId = allianceOrDefendStringId;
            HordeOrDestroyStringId = hordeOrDestroyStringId;
        }
    }

    struct NodeInfo
    {
        public PointStates State;
        public PointStates PrevState;
        public ushort TotalOwner;
        public Team Owner;
        public ushort PrevOwner;
        public bool Tower;
    }

    struct AlteracValleyMineInfo
    {
        public Team Owner;
        public int WorldStateOwner;
        public int WorldStateAllianceControlled;
        public int WorldStateHordeControlled;
        public int WorldStateNeutralControlled;
        public byte TextIdAlliance;
        public byte TextIdHorde;

        public AlteracValleyMineInfo(Team owner, WorldStateIds worldStateOwner, WorldStateIds worldStateAllianceControlled, WorldStateIds worldStateHordeControlled, WorldStateIds worldStateNeutralControlled, TextIds textIdAlliance, TextIds textIdHorde)
        {
            Owner = owner;
            WorldStateOwner = (int)worldStateOwner;
            WorldStateAllianceControlled = (int)worldStateAllianceControlled;
            WorldStateHordeControlled = (int)worldStateHordeControlled;
            WorldStateNeutralControlled = (int)worldStateNeutralControlled;
            TextIdAlliance = (byte)textIdAlliance;
            TextIdHorde = (byte)textIdHorde;
        }
    }

    [Script(nameof(battleground_alterac_valley), 30)]
    class battleground_alterac_valley : BattlegroundScript
    {
        enum QuestIds
        {
            AV_QUEST_A_SCRAPS1 = 7223,
            AV_QUEST_A_SCRAPS2 = 6781,
            AV_QUEST_H_SCRAPS1 = 7224,
            AV_QUEST_H_SCRAPS2 = 6741,
            AV_QUEST_A_COMMANDER1 = 6942, //soldier
            AV_QUEST_H_COMMANDER1 = 6825,
            AV_QUEST_A_COMMANDER2 = 6941, //leutnant
            AV_QUEST_H_COMMANDER2 = 6826,
            AV_QUEST_A_COMMANDER3 = 6943, //commander
            AV_QUEST_H_COMMANDER3 = 6827,
            AV_QUEST_A_BOSS1 = 7386, // 5 cristal/blood
            AV_QUEST_H_BOSS1 = 7385,
            AV_QUEST_A_BOSS2 = 6881, // 1
            AV_QUEST_H_BOSS2 = 6801,
            AV_QUEST_A_NEAR_MINE = 5892, //the mine near start location of team
            AV_QUEST_H_NEAR_MINE = 5893,
            AV_QUEST_A_OTHER_MINE = 6982, //the other mine ;)
            AV_QUEST_H_OTHER_MINE = 6985,
            AV_QUEST_A_RIDER_HIDE = 7026,
            AV_QUEST_H_RIDER_HIDE = 7002,
            AV_QUEST_A_RIDER_TAME = 7027,
            AV_QUEST_H_RIDER_TAME = 7001
        };

        enum DefenderTier
        {
            Defender,
            Seasoned,
            Veteran,
            Champion
        }

        enum PvpStats
        {
            TowersAssulted = 61,
            GraveyardsAssaulted = 63,
            TowersDefended = 64,
            GraveyardsDefended = 65,
            SecondaryObjectives = 82
        }

        enum HonorKillBonus
        {
            Boss = 4,
            Captain = 3,
            SurvivingTower = 2,
            SurvivingCaptain = 2,
            DestroyTower = 3
        }

        enum ReputationGains
        {
            Boss = 350,
            Captain = 125,
            DestroyTower = 12,
            SurvivingTower = 12,
            SurvivingCaptain = 125
        }

        enum ResourceLoss
        {
            Tower = -75,
            Captain = -100
        }

        enum SpellIds
        {
            CompleteAlteracValleyQuest = 23658,
        }

        enum FactionIds
        {
            FrostwolfClan = 729,
            StormpikeGuard = 730,
        }

        int[] _teamResources = new int[SharedConst.PvpTeamsCount];
        uint[][] _teamQuestStatus = new uint[SharedConst.PvpTeamsCount][];//[9] ; //[x][y] x=team y=questcounter

        NodeInfo[] _nodes = new NodeInfo[(int)Nodes.Max];

        TimeTracker _mineResourceTimer; //ticks for both teams

        AlteracValleyMineInfo[] _mineInfo = new AlteracValleyMineInfo[2];

        TimeTracker[] _captainBuffTimer = new TimeTracker[SharedConst.PvpTeamsCount];

        bool[] _isInformedNearVictory = new bool[SharedConst.PvpTeamsCount];
        List<ObjectGuid> _doorGUIDs;
        ObjectGuid _balindaGUID;
        ObjectGuid _galvangarGUID;
        List<ObjectGuid> _heraldGUIDs;

        public battleground_alterac_valley(BattlegroundMap map) : base(map)
        {
            _teamResources = [MiscConst.ScoreInitialPoints, MiscConst.ScoreInitialPoints];
            _isInformedNearVictory = [false, false];

            for (var i = 0; i < 2; i++) //forloop for both teams (it just make 0 == alliance and 1 == horde also for both mines 0=north 1=south
            {
                for (var j = 0; j < 9; j++)
                    _teamQuestStatus[i][j] = 0;

                _captainBuffTimer[i] = new(120000 + RandomHelper.URand(0, 4) * 60); //as far as i could see, the buff is randomly so i make 2minutes (thats the duration of the buff itself) + 0-4minutes @todo get the right times
            }

            _mineInfo[(byte)AlteracValleyMine.North] = new(Team.Other, WorldStateIds.IrondeepMineOwner, WorldStateIds.IrondeepMineAllianceControlled, WorldStateIds.IrondeepMineHordeControlled, WorldStateIds.IrondeepMineTroggControlled, TextIds.IrondeepMineAllianceTaken, TextIds.IrondeepMineHordeTaken);
            _mineInfo[(byte)AlteracValleyMine.South] = new(Team.Other, WorldStateIds.ColdtoothMineOwner, WorldStateIds.ColdtoothMineAllianceControlled, WorldStateIds.ColdtoothMineHordeControlled, WorldStateIds.ColdtoothMineKoboldControlled, TextIds.ColdtoothMineAllianceTaken, TextIds.ColdtoothMineHordeTaken);

            for (Nodes i = Nodes.FirstaidStation; i <= Nodes.StoneheartGrave; ++i) //alliance graves
                InitNode(i, Team.Alliance, false);
            for (Nodes i = Nodes.DunbaldarSouth; i <= Nodes.StoneheartBunker; ++i) //alliance towers
                InitNode(i, Team.Alliance, true);
            for (Nodes i = Nodes.IcebloodGrave; i <= Nodes.FrostwolfHut; ++i) //horde graves
                InitNode(i, Team.Horde, false);
            for (Nodes i = Nodes.IcebloodTower; i <= Nodes.FrostwolfWtower; ++i) //horde towers
                InitNode(i, Team.Horde, true);
            InitNode(Nodes.SnowfallGrave, Team.Other, false); //give snowfall neutral owner

            _mineResourceTimer = new(MiscConst.ResourceTimer);
        }

        public override void OnUpdate(uint diff)
        {
            if (battleground.GetStatus() != BattlegroundStatus.InProgress)
                return;

            _mineResourceTimer.Update(diff);
            if (_mineResourceTimer.Passed())
            {
                foreach (AlteracValleyMineInfo info in _mineInfo)
                {
                    if (info.Owner == Team.Other)
                        continue;

                    UpdateScore(info.Owner, 1);
                }

                _mineResourceTimer.Reset(MiscConst.ResourceTimer);
            }

            for (var i = BattleGroundTeamId.Alliance; i <= BattleGroundTeamId.Horde; i++)
            {
                if (!IsCaptainAlive(i))
                    continue;

                _captainBuffTimer[i].Update(diff);
                if (_captainBuffTimer[i].Passed())
                {
                    if (i == 0)
                    {
                        battleground.CastSpellOnTeam((uint)BuffIds.ACaptain, Team.Alliance);
                        Creature creature = battlegroundMap.GetCreature(_balindaGUID);
                        if (creature != null)
                            creature.GetAI().DoAction((int)SharedActions.BuffYell);
                    }
                    else
                    {
                        battleground.CastSpellOnTeam((uint)BuffIds.HCaptain, Team.Horde);
                        Creature creature = battlegroundMap.GetCreature(_galvangarGUID);
                        if (creature != null)
                            creature.GetAI().DoAction((int)SharedActions.BuffYell);
                    }

                    _captainBuffTimer[i].Reset(120000 + RandomHelper.URand(0, 4) * 60000); //as far as i could see, the buff is randomly so i make 2minutes (thats the duration of the buff itself) + 0-4minutes @todo get the right times
                }
            }
        }

        public override void OnPlayerKilled(Player victim, Player killer)
        {
            UpdateScore(battleground.GetPlayerTeam(victim.GetGUID()), -1);
        }

        public override void OnUnitKilled(Creature victim, Unit killer)
        {
            switch ((CreatureIds)victim.GetEntry())
            {
                case CreatureIds.Vanndar:
                    {
                        UpdateWorldState((int)WorldStateIds.VandaarAlive, 0);
                        battleground.CastSpellOnTeam((uint)SpellIds.CompleteAlteracValleyQuest, Team.Horde); //this is a spell which finishes a quest where a player has to kill the boss
                        battleground.RewardReputationToTeam((uint)FactionIds.FrostwolfClan, (uint)ReputationGains.Boss, Team.Horde);
                        battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill((uint)HonorKillBonus.Boss), Team.Horde);
                        battleground.EndBattleground(Team.Horde);
                        break;
                    }
                case CreatureIds.Drekthar:
                    {
                        UpdateWorldState((int)WorldStateIds.DrektharAlive, 0);
                        battleground.CastSpellOnTeam((uint)SpellIds.CompleteAlteracValleyQuest, Team.Alliance); //this is a spell which finishes a quest where a player has to kill the boss
                        battleground.RewardReputationToTeam((uint)FactionIds.StormpikeGuard, (uint)ReputationGains.Boss, Team.Alliance);
                        battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill((uint)HonorKillBonus.Boss), Team.Alliance);
                        battleground.EndBattleground(Team.Alliance);
                        break;
                    }
                case CreatureIds.Balinda:
                    {
                        UpdateWorldState((int)WorldStateIds.BalindaAlive, 0);
                        battleground.RewardReputationToTeam((uint)FactionIds.FrostwolfClan, (uint)ReputationGains.Captain, Team.Horde);
                        battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill((uint)HonorKillBonus.Captain), Team.Horde);
                        UpdateScore(Team.Alliance, (int)ResourceLoss.Captain);
                        Creature herald = FindHerald("bg_av_herald_horde_win");
                        if (herald != null)
                            herald.GetAI().Talk((uint)TextIds.StormpikeGeneralDead);
                        break;
                    }
                case CreatureIds.Galvangar:
                    {
                        UpdateWorldState((int)WorldStateIds.GalvagarAlive, 0);
                        battleground.RewardReputationToTeam((uint)FactionIds.StormpikeGuard, (uint)ReputationGains.Captain, Team.Alliance);
                        battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill((uint)HonorKillBonus.Captain), Team.Alliance);
                        UpdateScore(Team.Horde, (int)ResourceLoss.Captain);
                        Creature herald = FindHerald("bg_av_herald_alliance_win");
                        if (herald != null)
                            herald.GetAI().Talk((uint)TextIds.FrostwolfGeneralDead);
                        break;
                    }
                case CreatureIds.Morloch:
                    {
                        // if mine is not owned by morloch, then nothing happens
                        if (_mineInfo[(int)AlteracValleyMine.North].Owner != Team.Other)
                            break;

                        Team killerTeam = battleground.GetPlayerTeam((killer.GetCharmerOrOwnerPlayerOrPlayerItself() ?? killer).GetGUID());
                        ChangeMineOwner(AlteracValleyMine.North, killerTeam);
                        break;
                    }
                case CreatureIds.TaskmasterSnivvle:
                    {
                        if (_mineInfo[(int)AlteracValleyMine.South].Owner != Team.Other)
                            break;

                        Team killerTeam = battleground.GetPlayerTeam((killer.GetCharmerOrOwnerPlayerOrPlayerItself() ?? killer).GetGUID());
                        ChangeMineOwner(AlteracValleyMine.South, killerTeam);
                        break;
                    }
                case CreatureIds.UmiThorson:
                case CreatureIds.Keetar:
                    {
                        Team killerTeam = battleground.GetPlayerTeam((killer.GetCharmerOrOwnerPlayerOrPlayerItself() ?? killer).GetGUID());
                        ChangeMineOwner(AlteracValleyMine.North, killerTeam);
                        break;
                    }
                case CreatureIds.AgiRumblestomp:
                case CreatureIds.MashaSwiftcut:
                    {
                        Team killerTeam = battleground.GetPlayerTeam((killer.GetCharmerOrOwnerPlayerOrPlayerItself() ?? killer).GetGUID());
                        ChangeMineOwner(AlteracValleyMine.South, killerTeam);
                        break;
                    }
            }
        }

        bool IsCaptainAlive(int teamId)
        {
            if (teamId == BattleGroundTeamId.Horde)
                return battlegroundMap.GetWorldStateValue((int)WorldStateIds.GalvagarAlive) == 1;

            if (teamId == BattleGroundTeamId.Alliance)
                return battlegroundMap.GetWorldStateValue((int)WorldStateIds.BalindaAlive) == 1;

            return false;
        }

        public override void OnStart()
        {
            UpdateWorldState((int)WorldStateIds.ShowHordeReinforcements, 1);
            UpdateWorldState((int)WorldStateIds.ShowAllianceReinforcements, 1);

            // Achievement: The Alterac Blitz
            TriggerGameEvent(MiscConst.EventStartBattle);

            foreach (ObjectGuid guid in _doorGUIDs)
            {
                GameObject gameObject = battlegroundMap.GetGameObject(guid);
                if (gameObject != null)
                {
                    gameObject.UseDoorOrButton();
                    TimeSpan delay = gameObject.GetEntry() == (uint)ObjectIds.GhostGate ? TimeSpan.Zero : TimeSpan.FromSeconds(3);
                    gameObject.DespawnOrUnsummon(delay);
                }
            }
        }

        public override void OnEnd(Team winner)
        {
            base.OnEnd(winner);
            //calculate bonuskills for both teams:
            //first towers:
            byte[] kills = [0, 0];
            byte[] rep = [0, 0];

            for (int i = (int)Nodes.DunbaldarSouth; i <= (int)Nodes.FrostwolfWtower; ++i)
            {
                if (_nodes[i].State == PointStates.Controled)
                {
                    if (_nodes[i].Owner == Team.Alliance)
                    {
                        rep[BattleGroundTeamId.Alliance] += (byte)ReputationGains.SurvivingTower;
                        kills[BattleGroundTeamId.Alliance] += (byte)HonorKillBonus.SurvivingTower;
                    }
                    else
                    {
                        rep[BattleGroundTeamId.Horde] += (byte)ReputationGains.SurvivingTower;
                        kills[BattleGroundTeamId.Horde] += (byte)HonorKillBonus.SurvivingTower;
                    }
                }
            }

            for (var i = BattleGroundTeamId.Alliance; i <= BattleGroundTeamId.Horde; ++i)
            {
                if (IsCaptainAlive(i))
                {
                    kills[i] += (byte)HonorKillBonus.SurvivingCaptain;
                    rep[i] += (byte)ReputationGains.SurvivingCaptain;
                }
                if (rep[i] != 0)
                    battleground.RewardReputationToTeam(i == 0 ? (uint)FactionIds.StormpikeGuard : (uint)FactionIds.FrostwolfClan, rep[i], i == 0 ? Team.Alliance : Team.Horde);
                if (kills[i] != 0)
                    battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill(kills[i]), i == 0 ? Team.Alliance : Team.Horde);
            }
        }

        public override void OnPlayerLeft(Player player)
        {
            base.OnPlayerLeft(player);
            if (player == null)
                return;

            player.RemoveAurasDueToSpell((uint)BuffIds.Armor);
        }

        void EventPlayerDestroyedPoint(GameObject gameobject)
        {
            if (gameobject == null)
                return;

            Nodes node = GetNodeThroughObject(gameobject.GetEntry());
            DestroyNode(node);
            UpdateNodeWorldState(node);

            Team owner = _nodes[(int)node].Owner;
            if (IsTower(node))
            {
                UpdateScore((owner == Team.Alliance) ? Team.Horde : Team.Alliance, (int)ResourceLoss.Tower);
                battleground.RewardReputationToTeam(owner == Team.Alliance ? (uint)FactionIds.StormpikeGuard : (uint)FactionIds.FrostwolfClan, (uint)ReputationGains.DestroyTower, owner);
                battleground.RewardHonorToTeam(battleground.GetBonusHonorFromKill((uint)HonorKillBonus.DestroyTower), owner);
            }

            StaticNodeInfo nodeInfo = GetStaticNodeInfo(node);
            if (nodeInfo != null)
            {
                Creature herald = FindHerald(nodeInfo.HordeOrDestroyStringId);
                if (herald != null)
                    herald.GetAI().Talk(owner == Team.Alliance ? nodeInfo.AllianceCaptureTextId : nodeInfo.HordeCaptureTextId);
            }

            battlegroundMap.UpdateSpawnGroupConditions();
        }

        public override void DoAction(uint actionId, WorldObject source, WorldObject target)
        {
            Team team = battleground.GetPlayerTeam(source.GetGUID());
            uint teamIndex = (uint)Battleground.GetTeamIndexByTeamId(team);

            switch ((SharedActions)actionId)
            {
                case SharedActions.CaptureCapturableObject:
                    EventPlayerDestroyedPoint(source.ToGameObject());
                    break;
                case SharedActions.InteractCapturableObject:
                    if (target != null && source != null && source.IsPlayer())
                        HandleInteractCapturableObject(source.ToPlayer(), target.ToGameObject());
                    break;
                case SharedActions.TurnInScraps:
                    _teamQuestStatus[teamIndex][0] += 20;
                    break;
                case SharedActions.TurnInCommander1:
                    _teamQuestStatus[teamIndex][1]++;
                    battleground.RewardReputationToTeam(teamIndex, 1, team);
                    if (_teamQuestStatus[teamIndex][1] == 30)
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                    break;
                case SharedActions.TurnInCommander2:
                    _teamQuestStatus[teamIndex][2]++;
                    battleground.RewardReputationToTeam(teamIndex, 1, team);
                    if (_teamQuestStatus[teamIndex][2] == 60)
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                    break;
                case SharedActions.TurnInCommander3:
                    _teamQuestStatus[teamIndex][3]++;
                    battleground.RewardReputationToTeam(teamIndex, 1, team);
                    if (_teamQuestStatus[teamIndex][3] == 120)
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                    break;
                case SharedActions.TurnInBoss1:
                    _teamQuestStatus[teamIndex][4] += 4; //you can turn in 5 or 1 item..
                    goto case SharedActions.TurnInBoss2;
                case SharedActions.TurnInBoss2:
                    _teamQuestStatus[teamIndex][4]++;
                    if (_teamQuestStatus[teamIndex][4] >= 200)
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                    UpdateWorldState((int)(teamIndex == BattleGroundTeamId.Alliance ? WorldStateIds.IvusStormCrystalCount : WorldStateIds.LokholarStormpikeSoldiersBloodCount), (int)_teamQuestStatus[teamIndex][4]);
                    break;
                case SharedActions.TurnInNearMine:
                    _teamQuestStatus[teamIndex][5]++;
                    if (_teamQuestStatus[teamIndex][5] == 28)
                    {
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                        if (_teamQuestStatus[teamIndex][6] == 7)
                            Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here - ground assault ready");
                    }
                    break;
                case SharedActions.TurnInOtherMine:
                    _teamQuestStatus[teamIndex][6]++;
                    if (_teamQuestStatus[teamIndex][6] == 7)
                    {
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                        if (_teamQuestStatus[teamIndex][5] == 20)
                            Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here - ground assault ready");
                    }
                    break;
                case SharedActions.TurnInRiderHide:
                    _teamQuestStatus[teamIndex][7]++;
                    if (_teamQuestStatus[teamIndex][7] == 25)
                    {
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                        if (_teamQuestStatus[teamIndex][8] == 25)
                            Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here - rider assault ready");
                    }
                    break;
                case SharedActions.TurnInRiderTame:
                    _teamQuestStatus[teamIndex][8]++;
                    if (_teamQuestStatus[teamIndex][8] == 25)
                    {
                        Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here");
                        if (_teamQuestStatus[teamIndex][7] == 25)
                            Log.outDebug(LogFilter.Battleground, $"BG_AV action {actionId} completed (need to implement some events here - rider assault ready");
                    }
                    break;
                default:
                    Log.outError(LogFilter.Battleground, $"BattlegroundAV::DoAction: {actionId}. Unhandled action.");
                    break;
            }
        }

        void ChangeMineOwner(AlteracValleyMine mine, Team team, bool initial = false)
        {
            if (team != Team.Alliance && team != Team.Horde)
                team = Team.Other;

            AlteracValleyMineInfo mineInfo = _mineInfo[(int)mine];

            if (mineInfo.Owner == team && !initial)
                return;

            mineInfo.Owner = team;

            SendMineWorldStates(mine);

            byte textId = team == Team.Alliance ? mineInfo.TextIdAlliance : mineInfo.TextIdHorde;

            string stringId = team == Team.Alliance ? "bg_av_herald_mine_alliance" : "bg_av_herald_mine_horde";

            FindHerald(stringId)?.GetAI().Talk(textId);
        }

        static Nodes GetNodeThroughObject(uint obj)
        {
            switch ((ObjectIds)obj)
            {
                case ObjectIds.AidStationAllianceControlled:
                case ObjectIds.AidStationHordeContested:
                case ObjectIds.AidStationHordeControlled:
                case ObjectIds.AidStationAllianceContested:
                    return Nodes.FirstaidStation;
                case ObjectIds.StormpikeAllianceControlled:
                case ObjectIds.StormpikeHordeContested:
                case ObjectIds.StormpikeHordeControlled:
                case ObjectIds.StormpikeAllianceContested:
                    return Nodes.StormpikeGrave;
                case ObjectIds.StonehearthHordeContested:
                case ObjectIds.StonehearthHordeControlled:
                case ObjectIds.StonehearthAllianceContested:
                case ObjectIds.StonehearthAllianceControlled:
                    return Nodes.StoneheartGrave;
                case ObjectIds.SnowfallNeutral:
                case ObjectIds.SnowfallHordeContested:
                case ObjectIds.SnowfallAllianceContested:
                case ObjectIds.SnowfallHordeControlled:
                case ObjectIds.SnowfallAllianceControlled:
                    return Nodes.SnowfallGrave;
                case ObjectIds.IcebloodHordeControlled:
                case ObjectIds.IcebloodAllianceContested:
                case ObjectIds.IcebloodAllianceControlled:
                case ObjectIds.IcebloodHordeContested:
                    return Nodes.IcebloodGrave;
                case ObjectIds.FrostwolfHordeControlled:
                case ObjectIds.FrostwolfAllianceContested:
                case ObjectIds.FrostwolfAllianceControlled:
                case ObjectIds.FrostwolfHordeContested:
                    return Nodes.FrostwolfGrave;
                case ObjectIds.FrostwolfHutHordeControlled:
                case ObjectIds.FrostwolfHutAllianceContested:
                case ObjectIds.FrostwolfHutAllianceControlled:
                case ObjectIds.FrostwolfHutHordeContested:
                    return Nodes.FrostwolfHut;
                case ObjectIds.SouthBunkerControlledTowerBanner:
                case ObjectIds.SouthBunkerControlledBanner:
                case ObjectIds.SouthBunkerContestedBanner:
                case ObjectIds.SouthBunkerContestedTowerBanner:
                    return Nodes.DunbaldarSouth;
                case ObjectIds.NorthBunkerControlledTowerBanner:
                case ObjectIds.NorthBunkerControlledBanner:
                case ObjectIds.NorthBunkerContestedBanner:
                case ObjectIds.NorthBunkerContestedTowerBanner:
                    return Nodes.DunbaldarNorth;
                case ObjectIds.EastTowerControlledTowerBanner:
                case ObjectIds.EastTowerControlledBanner:
                case ObjectIds.EastTowerContestedBanner:
                case ObjectIds.EastTowerContestedTowerBanner:
                    return Nodes.FrostwolfEtower;
                case ObjectIds.WestTowerControlledTowerBanner:
                case ObjectIds.WestTowerControlledBanner:
                case ObjectIds.WestTowerContestedBanner:
                case ObjectIds.WestTowerContestedTowerBanner:
                    return Nodes.FrostwolfWtower;
                case ObjectIds.TowerPointControlledTowerBanner:
                case ObjectIds.TowerPointControlledBanner:
                case ObjectIds.TowerPointContestedBanner:
                case ObjectIds.TowerPointContestedTowerBanner:
                    return Nodes.TowerPoint;
                case ObjectIds.IcebloodTowerControlledTowerBanner:
                case ObjectIds.IcebloodTowerControlledBanner:
                case ObjectIds.IcebloodTowerContestedBanner:
                case ObjectIds.IcebloodTowerContestedTowerBanner:
                    return Nodes.IcebloodTower;
                case ObjectIds.StonehearthBunkerControlledTowerBanner:
                case ObjectIds.StonehearthBunkerControlledBanner:
                case ObjectIds.StonehearthBunkerContestedBanner:
                case ObjectIds.StonehearthBunkerContestedTowerBanner:
                    return Nodes.StoneheartBunker;
                case ObjectIds.IcewingBunkerControlledTowerBanner:
                case ObjectIds.IcewingBunkerControlledBanner:
                case ObjectIds.IcewingBunkerContestedBanner:
                case ObjectIds.IcewingBunkerContestedTowerBanner:
                    return Nodes.IcewingBunker;
                default:
                    Log.outError(LogFilter.Battleground, "BattlegroundAV: ERROR! GetPlace got a wrong object :(");
                    return 0;
            }
        }

        void HandleInteractCapturableObject(Player player, GameObject target)
        {
            if (player == null || target == null)
                return;

            switch ((ObjectIds)target.GetEntry())
            {
                // graveyards
                case ObjectIds.AidStationAllianceControlled:
                case ObjectIds.AidStationHordeControlled:
                case ObjectIds.FrostwolfAllianceControlled:
                case ObjectIds.FrostwolfHordeControlled:
                case ObjectIds.FrostwolfHutAllianceControlled:
                case ObjectIds.FrostwolfHutHordeControlled:
                case ObjectIds.IcebloodAllianceControlled:
                case ObjectIds.IcebloodHordeControlled:
                case ObjectIds.StonehearthAllianceControlled:
                case ObjectIds.StonehearthHordeControlled:
                case ObjectIds.StormpikeAllianceControlled:
                case ObjectIds.StormpikeHordeControlled:
                // Snowfall
                case ObjectIds.SnowfallNeutral:
                case ObjectIds.SnowfallAllianceControlled:
                case ObjectIds.SnowfallHordeControlled:
                // towers
                case ObjectIds.EastTowerControlledBanner:
                case ObjectIds.WestTowerControlledBanner:
                case ObjectIds.TowerPointControlledBanner:
                case ObjectIds.IcebloodTowerControlledBanner:
                case ObjectIds.StonehearthBunkerControlledBanner:
                case ObjectIds.IcewingBunkerControlledBanner:
                case ObjectIds.SouthBunkerControlledBanner:
                case ObjectIds.NorthBunkerControlledBanner:
                    EventPlayerAssaultsPoint(player, target.GetEntry());
                    break;
                // graveyards
                case ObjectIds.AidStationAllianceContested:
                case ObjectIds.AidStationHordeContested:
                case ObjectIds.FrostwolfAllianceContested:
                case ObjectIds.FrostwolfHordeContested:
                case ObjectIds.FrostwolfHutAllianceContested:
                case ObjectIds.FrostwolfHutHordeContested:
                case ObjectIds.IcebloodAllianceContested:
                case ObjectIds.IcebloodHordeContested:
                case ObjectIds.StonehearthAllianceContested:
                case ObjectIds.StonehearthHordeContested:
                case ObjectIds.StormpikeAllianceContested:
                case ObjectIds.StormpikeHordeContested:
                // towers
                case ObjectIds.EastTowerContestedBanner:
                case ObjectIds.WestTowerContestedBanner:
                case ObjectIds.TowerPointContestedBanner:
                case ObjectIds.IcebloodTowerContestedBanner:
                case ObjectIds.StonehearthBunkerContestedBanner:
                case ObjectIds.IcewingBunkerContestedBanner:
                case ObjectIds.SouthBunkerContestedBanner:
                case ObjectIds.NorthBunkerContestedBanner:
                    EventPlayerDefendsPoint(player, target.GetEntry());
                    break;
                // Snowfall special cases (either defend/assault)
                case ObjectIds.SnowfallAllianceContested:
                case ObjectIds.SnowfallHordeContested:
                    {
                        Nodes node = GetNodeThroughObject(target.GetEntry());
                        if (_nodes[(int)node].TotalOwner == (ushort)Team.Other)
                            EventPlayerAssaultsPoint(player, target.GetEntry());
                        else
                            EventPlayerDefendsPoint(player, target.GetEntry());
                        break;
                    }
                default:
                    break;
            }
        }

        void EventPlayerDefendsPoint(Player player, uint obj)
        {
            Nodes node = GetNodeThroughObject(obj);

            Team owner = _nodes[(int)node].Owner;
            Team team = battleground.GetPlayerTeam(player.GetGUID());

            if (owner == team || _nodes[(int)node].State != PointStates.Assaulted)
                return;

            Log.outDebug(LogFilter.Battleground, $"player defends point object: {obj} node: {node}");
            if (_nodes[(int)node].PrevOwner != (ushort)team)
            {
                Log.outError(LogFilter.Battleground, $"BG_AV: player defends point which doesn't belong to his team {node}");
                return;
            }

            DefendNode(node, team);
            UpdateNodeWorldState(node);

            StaticNodeInfo nodeInfo = GetStaticNodeInfo(node);
            if (nodeInfo != null)
            {
                string stringId;

                if (IsTower(node))
                    stringId = nodeInfo.AllianceOrDefendStringId;
                else
                    stringId = team == Team.Alliance ? nodeInfo.AllianceOrDefendStringId : nodeInfo.HordeOrDestroyStringId;

                Creature herald = FindHerald(stringId);
                if (herald != null)
                    herald.GetAI().Talk(team == Team.Alliance ? nodeInfo.AllianceCaptureTextId : nodeInfo.HordeCaptureTextId);
            }

            // update the statistic for the defending player
            battleground.UpdatePvpStat(player, (uint)(IsTower(node) ? PvpStats.TowersDefended : PvpStats.GraveyardsDefended), 1);
            battlegroundMap.UpdateSpawnGroupConditions();
        }

        void EventPlayerAssaultsPoint(Player player, uint obj)
        {
            Nodes node = GetNodeThroughObject(obj);
            Team owner = _nodes[(int)node].Owner; //maybe name it prevowner
            Team team = battleground.GetPlayerTeam(player.GetGUID());

            Log.outDebug(LogFilter.Battleground, $"bg_av: player assaults point object {obj} node {node}");
            if (owner == team || (ushort)team == _nodes[(int)node].TotalOwner)
                return; //surely a gm used this object

            AssaultNode(node, team);
            UpdateNodeWorldState(node);

            StaticNodeInfo nodeInfo = GetStaticNodeInfo(node);
            if (nodeInfo != null)
            {
                string stringId;
                if (IsTower(node))
                    stringId = nodeInfo.HordeOrDestroyStringId;
                else
                    stringId = team == Team.Alliance ? nodeInfo.AllianceOrDefendStringId : nodeInfo.HordeOrDestroyStringId;

                Creature herald = FindHerald(stringId);
                if (herald != null)
                    herald.GetAI().Talk(team == Team.Alliance ? nodeInfo.AllianceAttackTextId : nodeInfo.HordeAttackTextId);
            }

            // update the statistic for the assaulting player
            battleground.UpdatePvpStat(player, (uint)(IsTower(node) ? PvpStats.TowersAssulted : PvpStats.GraveyardsAssaulted), 1);
            battlegroundMap.UpdateSpawnGroupConditions();
        }

        void UpdateNodeWorldState(Nodes node)
        {
            StaticNodeInfo nodeInfo = GetStaticNodeInfo(node);
            if (nodeInfo != null)
            {
                var owner = _nodes[(int)node].Owner;
                PointStates state = _nodes[(int)node].State;

                UpdateWorldState(nodeInfo.AllianceAssaultWorldStateId, owner == Team.Alliance && state == PointStates.Assaulted ? 1 : 0);
                UpdateWorldState(nodeInfo.AllianceControlWorldStateId, owner == Team.Alliance && state >= PointStates.Destroyed ? 1 : 0);
                UpdateWorldState(nodeInfo.HordeAssaultWorldStateId, owner == Team.Horde && state == PointStates.Assaulted ? 1 : 0);
                UpdateWorldState(nodeInfo.HordeControlWorldStateId, owner == Team.Horde && state >= PointStates.Destroyed ? 1 : 0);
                if (nodeInfo.OwnerWorldStateId != 0)
                    UpdateWorldState(nodeInfo.OwnerWorldStateId, owner == Team.Horde ? 2 : owner == Team.Alliance ? 1 : 0);
            }

            if (node == Nodes.SnowfallGrave)
                UpdateWorldState((int)WorldStateIds.SnowfallGraveyardUncontrolled, _nodes[(int)node].Owner == Team.Other ? 1 : 0);
        }

        void SendMineWorldStates(AlteracValleyMine mine)
        {
            AlteracValleyMineInfo mineInfo = _mineInfo[(int)mine];
            UpdateWorldState(mineInfo.WorldStateHordeControlled, mineInfo.Owner == Team.Horde ? 1 : 0);
            UpdateWorldState(mineInfo.WorldStateAllianceControlled, mineInfo.Owner == Team.Alliance ? 1 : 0);
            UpdateWorldState(mineInfo.WorldStateNeutralControlled, mineInfo.Owner == Team.Other ? 1 : 0);
            UpdateWorldState(mineInfo.WorldStateOwner, mineInfo.Owner == Team.Horde ? 2 : mineInfo.Owner == Team.Alliance ? 1 : 0);
        }

        void AssaultNode(Nodes node, Team team)
        {
            _nodes[(int)node].PrevOwner = (ushort)_nodes[(int)node].Owner;
            _nodes[(int)node].Owner = team;
            _nodes[(int)node].PrevState = _nodes[(int)node].State;
            _nodes[(int)node].State = PointStates.Assaulted;
        }

        void DestroyNode(Nodes node)
        {
            _nodes[(int)node].TotalOwner = (ushort)_nodes[(int)node].Owner;
            _nodes[(int)node].PrevOwner = (ushort)_nodes[(int)node].Owner;
            _nodes[(int)node].PrevState = _nodes[(int)node].State;
            _nodes[(int)node].State = _nodes[(int)node].Tower ? PointStates.Destroyed : PointStates.Controled;
        }

        void InitNode(Nodes node, Team team, bool tower)
        {
            _nodes[(int)node].TotalOwner = (ushort)team;
            _nodes[(int)node].Owner = team;
            _nodes[(int)node].PrevOwner = 0;
            _nodes[(int)node].State = PointStates.Controled;
            _nodes[(int)node].PrevState = _nodes[(int)node].State;
            _nodes[(int)node].State = PointStates.Controled;
            _nodes[(int)node].Tower = tower;
        }

        void DefendNode(Nodes node, Team team)
        {
            _nodes[(int)node].PrevOwner = (ushort)_nodes[(int)node].Owner;
            _nodes[(int)node].Owner = team;
            _nodes[(int)node].PrevState = _nodes[(int)node].State;
            _nodes[(int)node].State = PointStates.Controled;
        }

        public override Team GetPrematureWinner()
        {
            int allianceScore = _teamResources[Battleground.GetTeamIndexByTeamId(Team.Alliance)];
            int hordeScore = _teamResources[Battleground.GetTeamIndexByTeamId(Team.Horde)];

            if (allianceScore > hordeScore)
                return Team.Alliance;
            if (hordeScore > allianceScore)
                return Team.Horde;

            return base.GetPrematureWinner();
        }

        public override void OnGameObjectCreate(GameObject gameObject)
        {
            switch ((ObjectIds)gameObject.GetEntry())
            {
                case ObjectIds.GhostGate:
                case ObjectIds.Gate:
                    _doorGUIDs.Add(gameObject.GetGUID());
                    break;
                default:
                    break;
            }
        }

        public override void OnCreatureCreate(Creature creature)
        {
            switch ((CreatureIds)creature.GetEntry())
            {
                case CreatureIds.Galvangar:
                    _galvangarGUID = creature.GetGUID();
                    break;
                case CreatureIds.Balinda:
                    _balindaGUID = creature.GetGUID();
                    break;
                case CreatureIds.Herald:
                    _heraldGUIDs.Add(creature.GetGUID());
                    break;
                default:
                    break;
            }
        }

        public override uint GetData(uint dataId)
        {
            DefenderTier getDefenderTierForTeam(int teamId)
            {
                if (_teamQuestStatus[teamId][0] < 500)
                    return DefenderTier.Defender;

                if (_teamQuestStatus[teamId][0] < 1000)
                    return DefenderTier.Seasoned;

                if (_teamQuestStatus[teamId][0] < 1500)
                    return DefenderTier.Veteran;

                return DefenderTier.Champion;
            };

            switch (dataId)
            {
                case MiscConst.DataDefenderTierAlliance:
                    return (uint)getDefenderTierForTeam(BattleGroundTeamId.Alliance);
                case MiscConst.DataDefenderTierHorde:
                    return (uint)getDefenderTierForTeam(BattleGroundTeamId.Horde);
                default:
                    return base.GetData(dataId);
            }
        }

        Creature FindHerald(string stringId)
        {
            foreach (ObjectGuid guid in _heraldGUIDs)
            {
                Creature creature = battlegroundMap.GetCreature(guid);
                if (creature != null)
                    if (creature.HasStringId(stringId))
                        return creature;
            }

            return null;
        }

        static StaticNodeInfo GetStaticNodeInfo(Nodes node)
        {
            foreach (var nodeInfo in MiscConst.BGAVNodeInfo)
                if (nodeInfo.NodeId == node)
                    return nodeInfo;

            return null;
        }

        bool IsTower(Nodes node) { return _nodes[(int)node].Tower; }

        void UpdateScore(Team team, short points)
        {
            Cypher.Assert(team == Team.Alliance || team == Team.Horde);
            int teamindex = Battleground.GetTeamIndexByTeamId(team);
            _teamResources[teamindex] += points;

            UpdateWorldState((int)(teamindex == BattleGroundTeamId.Horde ? WorldStateIds.HordeReinforcements : WorldStateIds.AllianceReinforcements), _teamResources[teamindex]);
            if (points < 0)
            {
                if (_teamResources[teamindex] < 1)
                {
                    _teamResources[teamindex] = 0;
                    battleground.EndBattleground(teamindex == BattleGroundTeamId.Horde ? Team.Alliance : Team.Horde);
                }
                else if (!_isInformedNearVictory[teamindex] && _teamResources[teamindex] < MiscConst.NearLosePoints)
                {
                    if (teamindex == BattleGroundTeamId.Alliance)
                        battleground.SendBroadcastText((uint)BroadcastTextIds.AllianceNearLose, ChatMsg.BgSystemAlliance);
                    else
                        battleground.SendBroadcastText((uint)BroadcastTextIds.HordeNearLose, ChatMsg.BgSystemHorde);
                    battleground.PlaySoundToAll((uint)SoundIds.NearVictory);
                    _isInformedNearVictory[teamindex] = true;
                }
            }
        }
    }
}
