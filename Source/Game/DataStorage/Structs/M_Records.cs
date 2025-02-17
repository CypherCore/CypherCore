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
        public int PreloadFileDataID;
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

        public bool IsDynamicDifficultyMap() { return HasFlag(MapFlags.DynamicDifficulty); }
        public bool IsFlexLocking() { return HasFlag(MapFlags.FlexibleRaidLocking); }
        public bool IsGarrison() { return HasFlag(MapFlags.Garrison); }
        public bool IsSplitByFaction()
        {
            return Id == 609 || // Acherus (DeathKnight Start)
            Id == 1265 ||   // Assault on the Dark Portal (WoD Intro)
            Id == 1481 ||   // Mardum (DH Start)
            Id == 2175 ||   // Exiles Reach - NPE
            Id == 2570;     // Forbidden Reach (Dracthyr/Evoker Start)
        }

        public bool HasFlag(MapFlags mapFlags) { return (Flags[0] & (uint)mapFlags) != 0; }
        public bool HasFlag(MapFlags2 mapFlags2) { return (Flags[1] & (uint)mapFlags2) != 0; }
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
        public byte ItemContext;
        public uint ItemContextPickerID;
        public int Flags;
        public int ContentTuningID;
        public int WorldStateExpressionID;
        public uint MapID;

        public bool HasResetSchedule() { return ResetInterval != MapDifficultyResetInterval.Anytime; }
        public bool IsUsingEncounterLocks() { return HasFlag(MapDifficultyFlags.UseLootBasedLockInsteadOfInstanceLock); }
        public bool IsRestoringDungeonState() { return HasFlag(MapDifficultyFlags.ResumeDungeonProgressBasedOnLockout); }
        public bool IsExtendable() { return !HasFlag(MapDifficultyFlags.DisableLockExtension); }

        public uint GetRaidDuration()
        {
            if (ResetInterval == MapDifficultyResetInterval.Daily)
                return 86400;
            if (ResetInterval == MapDifficultyResetInterval.Weekly)
                return 604800;
            return 0;
        }

        public bool HasFlag(MapDifficultyFlags mapFlags) { return (Flags & (int)mapFlags) != 0; }
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
        public uint SpellID;
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
        public int Flags;
        public sbyte SourceTypeEnum;
        public uint SourceSpellID;
        public uint PlayerConditionID;
        public float MountFlyRideHeight;
        public int UiModelSceneID;
        public int MountSpecialRiderAnimKitID;
        public int MountSpecialSpellVisualKitID;

        public bool HasFlag(MountFlags mountFlags) { return (Flags & (int)mountFlags) != 0; }
    }

    public sealed class MountCapabilityRecord
    {
        public uint Id;
        public int Flags;
        public ushort ReqRidingSkill;
        public ushort ReqAreaID;
        public uint ReqSpellAuraID;
        public uint ReqSpellKnownID;
        public uint ModSpellAuraID;
        public short ReqMapID;
        public int PlayerConditionID;
        public int FlightCapabilityID;

        public bool HasFlag(MountCapabilityFlags mountCapabilityFlags) { return (Flags & (int)mountCapabilityFlags) != 0; }
    }

    public sealed class MountEquipmentRecord
    {
        public uint Id;
        public int Item;
        public int BuffSpell;
        public int Unknown820;
        public uint LearnedBySpell;
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
        public ushort Unknown1100;
        public uint MountID;
    }

    public sealed class MovieRecord
    {
        public uint Id;
        public byte Volume;
        public byte KeyID;
        public uint AudioFileDataID;
        public uint SubtitleFileDataID;
        public uint SubtitleFileFormat;
    }

    public sealed class MythicPlusSeasonRecord
    {
        public uint Id;
        public int MilestoneSeason;
        public int StartTimeEvent;
        public int ExpansionLevel;
        public int HeroicLFGDungeonMinGear;
    }
}
