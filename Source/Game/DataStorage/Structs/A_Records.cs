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
    public sealed class AchievementRecord
    {
        public string Description;
        public string Title;
        public string Reward;
        public uint Id;
        public short InstanceID;
        public AchievementFaction Faction;
        public ushort Supercedes;
        public ushort Category;
        public byte MinimumCriteria;
        public byte Points;
        public AchievementFlags Flags;
        public ushort UiOrder;
        public uint IconFileID;
        public uint CriteriaTree;
        public ushort SharesCriteria;
    }

    public sealed class AchievementCategoryRecord
    {
        public LocalizedString Name;
        public uint Id;
        public short Parent;
        public sbyte UiOrder;
    }

    public sealed class AdventureJournalRecord
    {
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Description;
        public LocalizedString ButtonText;
        public LocalizedString RewardDescription;
        public LocalizedString ContinueDescription;
        public byte Type;
        public uint PlayerConditionID;
        public int Flags;
        public byte ButtonActionType;
        public int TextureFileDataID;
        public ushort LfgDungeonID;
        public uint QuestID;
        public ushort BattleMasterListID;
        public byte PriorityMin;
        public byte PriorityMax;
        public int ItemID;
        public uint ItemQuantity;
        public ushort CurrencyType;
        public byte CurrencyQuantity;
        public ushort UiMapID;
        public uint[] BonusPlayerConditionID = new uint[2];
        public byte[] BonusValue = new byte[2];
    }

    public sealed class AdventureMapPOIRecord
    {
        public uint Id;
        public LocalizedString Title;
        public string Description;
        public Vector2 WorldPosition;
        public sbyte Type;
        public uint PlayerConditionID;
        public uint QuestID;
        public uint LfgDungeonID;
        public int RewardItemID;
        public uint UiTextureAtlasMemberID;
        public uint UiTextureKitID;
        public int MapID;
        public uint AreaTableID;
    }

    public sealed class AnimationDataRecord
    {
        public uint Id;
        public ushort Fallback;
        public byte BehaviorTier;
        public int BehaviorID;
        public int[] Flags = new int[2];
    }

    public sealed class AnimKitRecord
    {
        public uint Id;
        public uint OneShotDuration;
        public ushort OneShotStopAnimKitID;
        public ushort LowDefAnimKitID;
    }

    public sealed class AreaGroupMemberRecord
    {
        public uint Id;
        public ushort AreaID;
        public uint AreaGroupID;
    }

    public sealed class AreaTableRecord
    {
        public uint Id;
        public string ZoneName;
        public LocalizedString AreaName;
        public ushort ContinentID;
        public ushort ParentAreaID;
        public short AreaBit;
        public byte SoundProviderPref;
        public byte SoundProviderPrefUnderwater;
        public ushort AmbienceID;
        public ushort UwAmbience;
        public ushort ZoneMusic;
        public ushort UwZoneMusic;
        public byte ExplorationLevel;
        public ushort IntroSound;
        public uint UwIntroSound;
        public byte FactionGroupMask;
        public float AmbientMultiplier;
        public byte MountFlags;
        public short PvpCombatWorldStateID;
        public byte WildBattlePetLevelMin;
        public byte WildBattlePetLevelMax;
        public byte WindSettingsID;
        public uint[] Flags = new uint[2];
        public ushort[] LiquidTypeID = new ushort[4];

        public bool IsSanctuary()
        {
            return HasFlag(AreaFlags.Sanctuary);
        }

        public bool IsFlyable()
        {
            if (HasFlag(AreaFlags.Outland))
            {
                if (!HasFlag(AreaFlags.NoFlyZone))
                    return true;
            }

            return false;
        }

        public bool HasFlag(AreaFlags flag) { return Flags[0].HasAnyFlag((uint)flag); }

        public bool HasFlag2(AreaFlags2 flag) { return Flags[1].HasAnyFlag((uint)flag); }
    }

    public sealed class AreaTriggerRecord
    {
        public LocalizedString Message;
        public Vector3 Pos;
        public uint Id;
        public ushort ContinentID;
        public sbyte PhaseUseFlags;
        public ushort PhaseID;
        public ushort PhaseGroupID;
        public float Radius;
        public float BoxLength;
        public float BoxWidth;
        public float BoxHeight;
        public float BoxYaw;
        public sbyte ShapeType;
        public short ShapeID;
        public short AreaTriggerActionSetID;
        public sbyte Flags;
    }

    public sealed class ArmorLocationRecord
    {
        public uint Id;
        public float Clothmodifier;
        public float Leathermodifier;
        public float Chainmodifier;
        public float Platemodifier;
        public float Modifier;
    }

    public sealed class ArtifactRecord
    {
        public string Name;
        public uint Id;
        public ushort UiTextureKitID;
        public int UiNameColor;
        public int UiBarOverlayColor;
        public int UiBarBackgroundColor;
        public ushort ChrSpecializationID;
        public byte Flags;
        public byte ArtifactCategoryID;
        public uint UiModelSceneID;
        public uint SpellVisualKitID;
    }

    public sealed class ArtifactAppearanceRecord
    {
        public string Name;
        public uint Id;
        public ushort ArtifactAppearanceSetID;
        public byte DisplayIndex;
        public uint UnlockPlayerConditionID;
        public byte ItemAppearanceModifierID;
        public int UiSwatchColor;
        public float UiModelSaturation;
        public float UiModelOpacity;
        public byte OverrideShapeshiftFormID;
        public uint OverrideShapeshiftDisplayID;
        public uint UiItemAppearanceID;
        public uint UiAltItemAppearanceID;
        public byte Flags;
        public ushort UiCameraID;
    }

    public sealed class ArtifactAppearanceSetRecord
    {
        public string Name;
        public string Description;
        public uint Id;
        public byte DisplayIndex;
        public ushort UiCameraID;
        public ushort AltHandUICameraID;
        public sbyte ForgeAttachmentOverride;
        public byte Flags;
        public uint ArtifactID;
    }

    public sealed class ArtifactCategoryRecord
    {
        public uint Id;
        public short XpMultCurrencyID;
        public short XpMultCurveID;
    }

    public sealed class ArtifactPowerRecord
    {
        public Vector2 DisplayPos;
        public uint Id;
        public byte ArtifactID;
        public byte MaxPurchasableRank;
        public int Label;
        public ArtifactPowerFlag Flags;
        public byte Tier;
    }

    public sealed class ArtifactPowerLinkRecord
    {
        public uint Id;
        public ushort PowerA;
        public ushort PowerB;
    }

    public sealed class ArtifactPowerPickerRecord
    {
        public uint Id;
        public uint PlayerConditionID;
    }

    public sealed class ArtifactPowerRankRecord
    {
        public uint Id;
        public byte RankIndex;
        public uint SpellID;
        public ushort ItemBonusListID;
        public float AuraPointsOverride;
        public uint ArtifactPowerID;
    }

    public sealed class ArtifactQuestXPRecord
    {
        public uint Id;
        public uint[] Difficulty = new uint[10];
    }

    public sealed class ArtifactTierRecord
    {
        public uint Id;
        public uint ArtifactTier;
        public uint MaxNumTraits;
        public uint MaxArtifactKnowledge;
        public uint KnowledgePlayerCondition;
        public uint MinimumEmpowerKnowledge;
    }

    public sealed class ArtifactUnlockRecord
    {
        public uint Id;
        public uint PowerID;
        public byte PowerRank;
        public ushort ItemBonusListID;
        public uint PlayerConditionID;
        public uint ArtifactID;
    }

    public sealed class AuctionHouseRecord
    {
        public uint Id;
        public string Name;
        public ushort FactionID;                                               // id of faction.dbc for player factions associated with city
        public byte DepositRate;
        public byte ConsignmentRate;
    }

    public sealed class AzeriteEmpoweredItemRecord
    {
        public uint Id;
        public uint ItemID;
        public uint AzeriteTierUnlockSetID;
        public uint AzeritePowerSetID;
    }

    public sealed class AzeriteEssenceRecord
    {
        public uint Id;
        public string Name;
        public string Description;
        public int SpecSetID;
    }

    public sealed class AzeriteEssencePowerRecord
    {
        public uint Id;
        public string SourceAlliance;
        public string SourceHorde;
        public int AzeriteEssenceID;
        public byte Tier;
        public uint MajorPowerDescription;
        public uint MinorPowerDescription;
        public uint MajorPowerActual;
        public uint MinorPowerActual;
    }

    public sealed class AzeriteItemRecord
    {
        public uint Id;
        public uint ItemID;
    }

    public sealed class AzeriteItemMilestonePowerRecord
    {
        public uint Id;
        public int RequiredLevel;
        public int AzeritePowerID;
        public int Type;
        public int AutoUnlock;
    }

    public sealed class AzeriteKnowledgeMultiplierRecord
    {
        public uint Id;
        public float Multiplier;
    }

    public sealed class AzeriteLevelInfoRecord
    {
        public uint Id;
        public ulong BaseExperienceToNextLevel;
        public ulong MinimumExperienceToNextLevel;
        public uint ItemLevel;
    }

    public sealed class AzeritePowerRecord
    {
        public uint Id;
        public uint SpellID;
        public uint ItemBonusListID;
        public int SpecSetID;
        public int Flags;
    }

    public sealed class AzeritePowerSetMemberRecord
    {
        public uint Id;
        public uint AzeritePowerSetID;
        public int AzeritePowerID;
        public int Class;
        public byte Tier;
        public uint OrderIndex;
    }

    public sealed class AzeriteTierUnlockRecord
    {
        public uint Id;
        public byte ItemCreationContext;
        public byte Tier;
        public byte AzeriteLevel;
        public uint AzeriteTierUnlockSetID;
    }

    public sealed class AzeriteTierUnlockSetRecord
    {
        public uint Id;
        public AzeriteTierUnlockSetFlags Flags;
    }
}
