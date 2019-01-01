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
using System;

namespace Framework.Constants
{
    public class MapConst
    {
        //Grids
        public const int MaxGrids = 64;
        public const float SizeofGrids = 533.33333f;
        public const int CenterGridCellId = (MaxCells * MaxGrids / 2);
        public const int CenterGridId = (MaxGrids / 2);
        public const float CenterGridOffset = (SizeofGrids / 2);
        public const float CenterGridCellOffset = (SizeofCells / 2);

        //Cells
        public const int MaxCells = 8;
        public const float SizeofCells = (SizeofGrids / MaxCells);
        public const int TotalCellsPerMap = (MaxGrids * MaxCells);
        public const float MapSize = (SizeofGrids * MaxGrids);
        public const float MapHalfSize = (MapSize / 2);

        public const uint MaxGroupSize = 5;
        public const uint MaxRaidSize = 40;
        public const uint MaxRaidSubGroups = MaxRaidSize / MaxGroupSize;
        public const uint TargetIconsCount = 8;
        public const uint RaidMarkersCount = 8;
        public const uint ReadycheckDuration = 35000;

        //Liquid
        public const uint MapLiquidTypeNoWater = 0x00;
        public const uint MapLiquidTypeWater = 0x01;
        public const uint MapLiquidTypeOcean = 0x02;
        public const uint MapLiquidTypeMagma = 0x04;
        public const uint MapLiquidTypeSlime = 0x08;
        public const uint MapLiquidTypeDarkWater = 0x10;
        public const uint MapAllLiquidTypes = (MapLiquidTypeWater | MapLiquidTypeOcean | MapLiquidTypeMagma | MapLiquidTypeSlime);
        public const float LiquidTileSize = (533.333f / 128.0f);

        public const int MinMapUpdateDelay = 50;
        public const int MinGridDelay = (Time.Minute * Time.InMilliseconds);

        public const int MapResolution = 128;
        public const float DefaultHeightSearch = 50.0f;
        public const float InvalidHeight = -100000.0f;
        public const float MaxHeight = 100000.0f;
        public const float MaxFallDistance = 250000.0f;

        public const uint MapMagic = 0x5350414D; //"MAPS";
        public const uint MapVersionMagic = 0x392E3176; //"v1.9";
        public const uint MapAreaMagic = 0x41455241; //"AREA";
        public const uint MapHeightMagic = 0x5447484D; //"MHGT";
        public const uint MapLiquidMagic = 0x51494C4D; //"MLIQ";

        public const uint mmapMagic = 0x4D4D4150; // 'MMAP'
        public const int mmapVersion = 9;

        public const string VMapMagic = "VMAP_4.8";
        public const float VMAPInvalidHeightValue = -200000.0f;
    }

    public enum NewWorldReason
    {
        Normal = 16,   // Normal map change
        Seamless = 21,   // Teleport to another map without a loading screen, used for outdoor scenarios
    }

    public enum InstanceResetWarningType
    {
        WarningHours = 1,                    // WARNING! %s is scheduled to reset in %d hour(s).
        WarningMin = 2,                    // WARNING! %s is scheduled to reset in %d minute(s)!
        WarningMinSoon = 3,                    // WARNING! %s is scheduled to reset in %d minute(s). Please exit the zone or you will be returned to your bind location!
        Welcome = 4,                    // Welcome to %s. This raid instance is scheduled to reset in %s.
        Expired = 5
    }

    public enum InstanceResetMethod
    {
        All,
        ChangeDifficulty,
        Global,
        GroupDisband,
        GroupJoin,
        RespawnDelay
    }

    [Flags]
    public enum GridMapTypeMask
    {
        None = 0x00,
        Corpse = 0x01,
        Creature = 0x02,
        DynamicObject = 0x04,
        GameObject = 0x08,
        Player = 0x10,
        AreaTrigger = 0x20,
        Conversation = 0x40,
        All = 0x7F,
        
        //GameObjects, Creatures(except pets), DynamicObject, Corpse(Bones), AreaTrigger
        AllGrid = GameObject | Creature | DynamicObject | Corpse | AreaTrigger | Conversation,

        //Player, Pets, Corpse(resurrectable), DynamicObject(farsight)
        AllWorld = Player | Creature | Corpse | DynamicObject
    }

    public enum ZLiquidStatus
    {
        NoWater = 0x00,
        AboveWater = 0x01,
        WaterWalk = 0x02,
        InWater = 0x04,
        UnderWater = 0x08
    }

    public enum EncounterFrameType
    {
        SetCombatResLimit = 0,
        ResetCombatResLimit = 1,
        Engage = 2,
        Disengage = 3,
        UpdatePriority = 4,
        AddTimer = 5,
        EnableObjective = 6,
        UpdateObjective = 7,
        DisableObjective = 8,
        Unk7 = 9,    // Seems To Have Something To Do With Sorting The Encounter Units
        AddCombatResLimit = 10
    }

    public enum EncounterState
    {
        NotStarted = 0,
        InProgress = 1,
        Fail = 2,
        Done = 3,
        Special = 4,
        ToBeDecided = 5
    }

    public enum EncounterCreditType
    {
        KillCreature = 0,
        CastSpell = 1
    }

    public enum DoorType
    {
        Room = 0,    // Door can open if encounter is not in progress
        Passage = 1,    // Door can open if encounter is done
        SpawnHole = 2,    // Door can open if encounter is in progress, typically used for spawning places
        Max
    }

    public enum EnterState
    {
        CanEnter = 0,
        CannotEnterAlreadyInMap = 1, // Player Is Already In The Map
        CannotEnterNoEntry, // No Map Entry Was Found For The Target Map Id
        CannotEnterUninstancedDungeon, // No Instance Template Was Found For Dungeon Map
        CannotEnterDifficultyUnavailable, // Requested Instance Difficulty Is Not Available For Target Map
        CannotEnterNotInRaid, // Target Instance Is A Raid Instance And The Player Is Not In A Raid Group
        CannotEnterCorpseInDifferentInstance, // Player Is Dead And Their Corpse Is Not In Target Instance
        CannotEnterInstanceBindMismatch, // Player'S Permanent Instance Save Is Not Compatible With Their Group'S Current Instance Bind
        CannotEnterTooManyInstances, // Player Has Entered Too Many Instances Recently
        CannotEnterMaxPlayers, // Target Map Already Has The Maximum Number Of Players Allowed
        CannotEnterZoneInCombat, // A Boss Encounter Is Currently In Progress On The Target Map
        CannotEnterUnspecifiedReason
    }

    public enum ModelIgnoreFlags
    {
        Nothing = 0x00,
        M2 = 0x01
    }
}
