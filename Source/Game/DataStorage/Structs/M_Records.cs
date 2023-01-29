// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class MailTemplateRecord
    {
        public LocalizedString Body;
        public uint Id;
    }

    public sealed class MapRecord
    {
        public ushort AreaTableID;
        public Vector2 Corpse;    // entrance coordinates in ghost mode  (in most cases = normal entrance)
        public short CorpseMapID; // map_id of entrance map in ghost mode (continent always and in most cases = normal entrance)
        public short CosmeticParentMapID;
        public string Directory;
        public byte ExpansionID;
        public uint[] Flags = new uint[3];
        public uint Id;
        public MapTypes InstanceType;
        public short LoadingScreenID;
        public string MapDescription0; // Horde
        public string MapDescription1; // Alliance
        public LocalizedString MapName;
        public byte MapType;
        public byte MaxPlayers;
        public float MinimapIconScale;
        public int NavigationMaxDistance;
        public short ParentMapID;
        public string PvpLongDescription;
        public string PvpShortDescription;
        public short TimeOfDayOverride;
        public byte TimeOffset;
        public int WdtFileDataID;
        public short WindSettingsID;
        public int ZmpFileDataID;

        // Helpers
        public Expansion Expansion()
        {
            return (Expansion)ExpansionID;
        }

        public bool IsDungeon()
        {
            return (InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Scenario) && !IsGarrison();
        }

        public bool IsNonRaidDungeon()
        {
            return InstanceType == MapTypes.Instance;
        }

        public bool Instanceable()
        {
            return InstanceType == MapTypes.Instance || InstanceType == MapTypes.Raid || InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena || InstanceType == MapTypes.Scenario;
        }

        public bool IsRaid()
        {
            return InstanceType == MapTypes.Raid;
        }

        public bool IsBattleground()
        {
            return InstanceType == MapTypes.Battleground;
        }

        public bool IsBattleArena()
        {
            return InstanceType == MapTypes.Arena;
        }

        public bool IsBattlegroundOrArena()
        {
            return InstanceType == MapTypes.Battleground || InstanceType == MapTypes.Arena;
        }

        public bool IsScenario()
        {
            return InstanceType == MapTypes.Scenario;
        }

        public bool IsWorldMap()
        {
            return InstanceType == MapTypes.Common;
        }

        public bool GetEntrancePos(out uint mapid, out float x, out float y)
        {
            mapid = 0;
            x = 0;
            y = 0;

            if (CorpseMapID < 0)
                return false;

            mapid = (uint)CorpseMapID;
            x = Corpse.X;
            y = Corpse.Y;

            return true;
        }

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
                case 2444:
                    return true;
                default:
                    return false;
            }
        }

        public bool IsDynamicDifficultyMap()
        {
            return GetFlags().HasFlag(MapFlags.DynamicDifficulty);
        }

        public bool IsFlexLocking()
        {
            return GetFlags().HasFlag(MapFlags.FlexibleRaidLocking);
        }

        public bool IsGarrison()
        {
            return GetFlags().HasFlag(MapFlags.Garrison);
        }

        public bool IsSplitByFaction()
        {
            return Id == 609 || Id == 2175 || Id == 2570;
        }

        public MapFlags GetFlags()
        {
            return (MapFlags)Flags[0];
        }

        public MapFlags2 GetFlags2()
        {
            return (MapFlags2)Flags[1];
        }
    }

    public sealed class MapChallengeModeRecord
    {
        public short[] CriteriaCount = new short[3];
        public uint ExpansionLevel;
        public byte Flags;
        public uint Id;
        public ushort MapID;
        public LocalizedString Name;
        public int RequiredWorldStateID; // maybe?
    }

    public sealed class MapDifficultyRecord
    {
        public int ContentTuningID;
        public uint DifficultyID;
        public int Flags;
        public uint Id;
        public int ItemContext;
        public uint ItemContextPickerID;
        public int LockID;
        public uint MapID;
        public uint MaxPlayers;
        public LocalizedString Message; // _message_lang (text showed when transfer to map failed)
        public MapDifficultyResetInterval ResetInterval;

        public bool HasResetSchedule()
        {
            return ResetInterval != MapDifficultyResetInterval.Anytime;
        }

        public bool IsUsingEncounterLocks()
        {
            return GetFlags().HasFlag(MapDifficultyFlags.UseLootBasedLockInsteadOfInstanceLock);
        }

        public bool IsRestoringDungeonState()
        {
            return GetFlags().HasFlag(MapDifficultyFlags.ResumeDungeonProgressBasedOnLockout);
        }

        public bool IsExtendable()
        {
            return !GetFlags().HasFlag(MapDifficultyFlags.DisableLockExtension);
        }

        public uint GetRaidDuration()
        {
            if (ResetInterval == MapDifficultyResetInterval.Daily)
                return 86400;

            if (ResetInterval == MapDifficultyResetInterval.Weekly)
                return 604800;

            return 0;
        }

        public MapDifficultyFlags GetFlags()
        {
            return (MapDifficultyFlags)Flags;
        }
    }

    public sealed class MapDifficultyXConditionRecord
    {
        public LocalizedString FailureDescription;
        public uint Id;
        public uint MapDifficultyID;
        public int OrderIndex;
        public uint PlayerConditionID;
    }

    public sealed class MawPowerRecord
    {
        public uint Id;
        public int MawPowerRarityID;
        public int SpellID;
    }

    public sealed class ModifierTreeRecord
    {
        public sbyte Amount;
        public uint Asset;
        public uint Id;
        public sbyte Operator;
        public uint Parent;
        public int SecondaryAsset;
        public int TertiaryAsset;
        public uint Type;
    }

    public sealed class MountRecord
    {
        public string Description;
        public MountFlags Flags;
        public uint Id;
        public float MountFlyRideHeight;
        public int MountSpecialRiderAnimKitID;
        public int MountSpecialSpellVisualKitID;
        public ushort MountTypeID;
        public string Name;
        public uint PlayerConditionID;
        public uint SourceSpellID;
        public string SourceText;
        public sbyte SourceTypeEnum;
        public int UiModelSceneID;

        public bool IsSelfMount()
        {
            return (Flags & MountFlags.SelfMount) != 0;
        }
    }

    public sealed class MountCapabilityRecord
    {
        public MountCapabilityFlags Flags;
        public int FlightCapabilityID;
        public uint Id;
        public uint ModSpellAuraID;
        public int PlayerConditionID;
        public ushort ReqAreaID;
        public short ReqMapID;
        public ushort ReqRidingSkill;
        public uint ReqSpellAuraID;
        public uint ReqSpellKnownID;
    }

    public sealed class MountTypeXCapabilityRecord
    {
        public uint Id;
        public ushort MountCapabilityID;
        public ushort MountTypeID;
        public byte OrderIndex;
    }

    public sealed class MountXDisplayRecord
    {
        public uint CreatureDisplayInfoID;
        public uint Id;
        public uint MountID;
        public uint PlayerConditionID;
    }

    public sealed class MovieRecord
    {
        public uint AudioFileDataID;
        public uint Id;
        public byte KeyID;
        public uint SubtitleFileDataID;
        public byte Volume;
    }
}