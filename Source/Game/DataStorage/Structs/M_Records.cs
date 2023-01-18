// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
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
        public Vector2 Corpse;                                           // entrance coordinates in ghost mode  (in most cases = normal entrance)
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
        public short CorpseMapID;                                              // map_id of entrance map in ghost mode (continent always and in most cases = normal entrance)
        public byte MaxPlayers;
        public short WindSettingsID;
        public int ZmpFileDataID;
        public int WdtFileDataID;
        public int NavigationMaxDistance;
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

        public bool IsDynamicDifficultyMap() { return GetFlags().HasFlag(MapFlags.DynamicDifficulty); }
        public bool IsFlexLocking() { return GetFlags().HasFlag(MapFlags.FlexibleRaidLocking); }
        public bool IsGarrison() { return GetFlags().HasFlag(MapFlags.Garrison); }
        public bool IsSplitByFaction() { return Id == 609 || Id == 2175 || Id == 2570; }

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
        public uint DifficultyID;
        public int LockID;
        public MapDifficultyResetInterval ResetInterval;
        public uint MaxPlayers;
        public int ItemContext;
        public uint ItemContextPickerID;
        public int Flags;
        public int ContentTuningID;
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

    public sealed class MawPowerRecord
    {
        public uint Id;
        public int SpellID;
        public int MawPowerRarityID;
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
        public int TertiaryAsset;
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
        public int MountSpecialRiderAnimKitID;
        public int MountSpecialSpellVisualKitID;

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
        public int PlayerConditionID;
        public int FlightCapabilityID;
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
