﻿/*
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
using System;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class MailTemplateRecord
    {
        public uint Id;
        public LocalizedString Body;
    }

    public sealed class MapRecord
    {
        public uint Id;
        public string Directory;
        public LocalizedString MapName;
        public string MapDescription0;                               // Horde
        public string MapDescription1;                               // Alliance
        public string PvpShortDescription;
        public string PvpLongDescription;
        public byte MapType;
        public MapTypes InstanceType;
        public byte ExpansionID;
        public ushort AreaTableID;
        public short LoadingScreenID;
        public short TimeOfDayOverride;
        public short ParentMapID;
        public short CosmeticParentMapID;
        public byte TimeOffset;
        public float MinimapIconScale;
        public int RaidOffset;
        public short CorpseMapID;                                              // map_id of entrance map in ghost mode (continent always and in most cases = normal entrance)
        public byte MaxPlayers;
        public short WindSettingsID;
        public int ZmpFileDataID;
        public uint[] Flags = new uint[3];

        // Helpers
        public Expansion Expansion() { return (Expansion)ExpansionID; }

        public bool IsDungeon()
        {
            return (InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Scenario) && !IsGarrison();
        }
        public bool IsNonRaidDungeon() { return InstanceType == MapTypes.Instance; }
        public bool Instanceable() { return InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena || InstanceType == MapTypes.Scenario; }
        public bool IsRaid() { return InstanceType == MapTypes.Raid; }
        public bool IsBattleground() { return InstanceType == MapTypes.Battleground; }
        public bool IsBattleArena() { return InstanceType == MapTypes.Arena; }
        public bool IsBattlegroundOrArena() { return InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena; }
        public bool IsScenario() { return InstanceType == MapTypes.Scenario; }
        public bool IsWorldMap() { return InstanceType == MapTypes.Common; }

        public bool IsContinent()
        {
            switch (Id)
            {
                case 0:
                case 1:
                case 530:
                case 571:
                case 870:
                case 1116:
                case 1220:
                case 1642:
                case 1643:
                case 2222:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsDynamicDifficultyMap() { return GetFlags().HasFlag(MapFlags.DynamicDifficulty); }
        public bool IsFlexLocking() { return GetFlags().HasFlag(MapFlags.FlexibleRaidLocking); }
        public bool IsGarrison() { return GetFlags().HasFlag(MapFlags.Garrison); }
        public bool IsSplitByFaction() { return Id == 609 || Id == 2175; }

        public MapFlags GetFlags() { return (MapFlags)Flags[0]; }
        public MapFlags2 GetFlags2() { return (MapFlags2)Flags[1]; }
    }

    public sealed class MapChallengeModeRecord
    {
        public LocalizedString Name;
        public uint Id;
        public ushort MapID;
        public byte Flags;
        public uint ExpansionLevel;
        public int RequiredWorldStateID; // maybe?
        public short[] CriteriaCount = new short[3];
    }

    public sealed class MapDifficultyRecord
    {
        public uint Id;
        public LocalizedString Message;                               // m_message_lang (text showed when transfer to map failed)
        public uint ItemContextPickerID;
        public int ContentTuningID;
        public byte DifficultyID;
        public byte LockID;
        public MapDifficultyResetInterval ResetInterval;
        public byte MaxPlayers;
        public byte ItemContext;
        public byte Flags;
        public uint MapID;

        public bool HasResetSchedule() { return ResetInterval != MapDifficultyResetInterval.Anytime; }
        public bool IsUsingEncounterLocks() { return GetFlags().HasFlag(MapDifficultyFlags.UseLootBasedLockInsteadOfInstanceLock); }
        public bool IsRestoringDungeonState() { return GetFlags().HasFlag(MapDifficultyFlags.ResumeDungeonProgressBasedOnLockout); }
        public bool IsExtendable() { return !GetFlags().HasFlag(MapDifficultyFlags.DisableLockExtension); }

        public uint GetRaidDuration()
        {
            if (ResetInterval == MapDifficultyResetInterval.Daily)
                return 86400;
            if (ResetInterval == MapDifficultyResetInterval.Weekly)
                return 604800;
            return 0;
        }

        public MapDifficultyFlags GetFlags() { return (MapDifficultyFlags)Flags; }
    }

    public sealed class MapDifficultyXConditionRecord
    {
        public uint Id;
        public LocalizedString FailureDescription;
        public uint PlayerConditionID;
        public int OrderIndex;
        public uint MapDifficultyID;
    }

    public sealed class ModifierTreeRecord
    {
        public uint Id;
        public uint Parent;
        public sbyte Operator;
        public sbyte Amount;
        public uint Type;
        public uint Asset;
        public int SecondaryAsset;
        public sbyte TertiaryAsset;
    }

    public sealed class MountRecord
    {
        public string Name;
        public string SourceText;
        public string Description;
        public uint Id;
        public ushort MountTypeID;
        public MountFlags Flags;
        public sbyte SourceTypeEnum;
        public uint SourceSpellID;
        public uint PlayerConditionID;
        public float MountFlyRideHeight;
        public int UiModelSceneID;

        public bool IsSelfMount() { return (Flags & MountFlags.SelfMount) != 0; }
    }

    public sealed class MountCapabilityRecord
    {
        public uint Id;
        public MountCapabilityFlags Flags;
        public ushort ReqRidingSkill;
        public ushort ReqAreaID;
        public uint ReqSpellAuraID;
        public uint ReqSpellKnownID;
        public uint ModSpellAuraID;
        public short ReqMapID;
    }

    public sealed class MountTypeXCapabilityRecord
    {
        public uint Id;
        public ushort MountTypeID;
        public ushort MountCapabilityID;
        public byte OrderIndex;
    }

    public sealed class MountXDisplayRecord
    {
        public uint Id;
        public uint CreatureDisplayInfoID;
        public uint PlayerConditionID;
        public uint MountID;
    }

    public sealed class MovieRecord
    {
        public uint Id;
        public byte Volume;
        public byte KeyID;
        public uint AudioFileDataID;
        public uint SubtitleFileDataID;
    }
}
