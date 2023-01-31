﻿// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class Cfg_RegionsRecord
    {
        public uint Id;
        public string Tag;
        public ushort RegionID;
        public uint Raidorigin;                                              // Date of first raid reset, all other resets are calculated as this date plus interval
        public byte RegionGroupMask;
        public uint ChallengeOrigin;
    }

    public sealed class CharTitlesRecord
    {
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Name1;
        public ushort MaskID;
        public sbyte Flags;
    }

    public sealed class CharacterLoadoutRecord
    {
        public uint Id;
        public long RaceMask;
        public sbyte ChrClassID;
        public int Purpose;
        public sbyte ItemContext;

        public bool IsForNewCharacter() { return Purpose == 9; }
    }

    public sealed class CharacterLoadoutItemRecord
    {
        public uint Id;
        public ushort CharacterLoadoutID;
        public uint ItemID;
    }

    public sealed class ChatChannelsRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string Shortcut;
        public ChannelDBCFlags Flags;
        public sbyte FactionGroup;
        public int Ruleset;
    }

    public sealed class ChrClassUIDisplayRecord
    {
        public uint Id;
        public byte ChrClassesID;
        public uint AdvGuidePlayerConditionID;
        public uint SplashPlayerConditionID;
    }

    public sealed class ChrClassesRecord
    {
        public LocalizedString Name;
        public string Filename;
        public string NameMale;
        public string NameFemale;
        public string PetNameToken;
        public string Description;
        public string RoleInfoString;
        public string DisabledString;
        public string HyphenatedNameMale;
        public string HyphenatedNameFemale;
        public uint Id;
        public uint CreateScreenFileDataID;
        public uint SelectScreenFileDataID;
        public uint IconFileDataID;
        public uint LowResScreenFileDataID;
        public int Flags;
        public uint SpellTextureBlobFileDataID;
        public uint RolesMask;
        public uint ArmorTypeMask;
        public int CharStartKitUnknown901;
        public int MaleCharacterCreationVisualFallback;
        public int MaleCharacterCreationIdleVisualFallback;
        public int FemaleCharacterCreationVisualFallback;
        public int FemaleCharacterCreationIdleVisualFallback;
        public int CharacterCreationIdleGroundVisualFallback;
        public int CharacterCreationGroundVisualFallback;
        public int AlteredFormCharacterCreationIdleVisualFallback;
        public int CharacterCreationAnimLoopWaitTimeMsFallback;
        public ushort CinematicSequenceID;
        public ushort DefaultSpec;
        public byte PrimaryStatPriority;
        public PowerType DisplayPower;
        public byte RangedAttackPowerPerAgility;
        public byte AttackPowerPerAgility;
        public byte AttackPowerPerStrength;
        public byte SpellClassSet;
        public byte ClassColorR;
        public byte ClassColorG;
        public byte ClassColorB;
    }

    public sealed class ChrClassesXPowerTypesRecord
    {
        public uint Id;
        public sbyte PowerType;
        public uint ClassID;
    }

    public sealed class ChrCustomizationChoiceRecord
    {
        public LocalizedString Name;
        public uint Id;
        public uint ChrCustomizationOptionID;
        public uint ChrCustomizationReqID;
        public int ChrCustomizationVisReqID;
        public ushort SortOrder;
        public ushort UiOrderIndex;
        public int Flags;
        public int AddedInPatch;
        public int[] SwatchColor = new int[2];
    }

    public sealed class ChrCustomizationDisplayInfoRecord
    {
        public uint Id;
        public int ShapeshiftFormID;
        public uint DisplayID;
        public float BarberShopMinCameraDistance;
        public float BarberShopHeightOffset;
    }

    public sealed class ChrCustomizationElementRecord
    {
        public uint Id;
        public uint ChrCustomizationChoiceID;
        public int RelatedChrCustomizationChoiceID;
        public int ChrCustomizationGeosetID;
        public int ChrCustomizationSkinnedModelID;
        public int ChrCustomizationMaterialID;
        public int ChrCustomizationBoneSetID;
        public int ChrCustomizationCondModelID;
        public int ChrCustomizationDisplayInfoID;
        public int ChrCustItemGeoModifyID;
        public int ChrCustomizationVoiceID;
    }

    public sealed class ChrCustomizationOptionRecord
    {
        public LocalizedString Name;
        public uint Id;
        public ushort SecondaryID;
        public int Flags;
        public uint ChrModelID;
        public int SortIndex;
        public int ChrCustomizationCategoryID;
        public int OptionType;
        public float BarberShopCostModifier;
        public int ChrCustomizationID;
        public int ChrCustomizationReqID;
        public int UiOrderIndex;
        public int AddedInPatch;
    }

    public sealed class ChrCustomizationReqRecord
    {
        public uint Id;
        public string ReqSource;
        public int Flags;
        public int ClassMask;
        public int AchievementID;
        public int QuestID;
        public int OverrideArchive;                                          // -1: allow any, otherwise must match OverrideArchive cvar
        public uint ItemModifiedAppearanceID;

        public ChrCustomizationReqFlag GetFlags() { return (ChrCustomizationReqFlag)Flags; }
    }

    public sealed class ChrCustomizationReqChoiceRecord
    {
        public uint Id;
        public uint ChrCustomizationChoiceID;
        public uint ChrCustomizationReqID;
    }

    public sealed class ChrModelRecord
    {
        public float[] FaceCustomizationOffset = new float[3];
        public float[] CustomizeOffset = new float[3];
        public uint Id;
        public sbyte Sex;
        public uint DisplayID;
        public int CharComponentTextureLayoutID;
        public int Flags;
        public int SkeletonFileDataID;
        public int ModelFallbackChrModelID;
        public int TextureFallbackChrModelID;
        public int HelmVisFallbackChrModelID;
        public float CustomizeScale;
        public float CustomizeFacing;
        public float CameraDistanceOffset;
        public float BarberShopCameraOffsetScale;
        public float BarberShopCameraHeightOffsetScale; // applied after BarberShopCameraOffsetScale
        public float BarberShopCameraRotationOffset;
    }

    public sealed class ChrRaceXChrModelRecord
    {
        public uint Id;
        public int ChrRacesID;
        public int ChrModelID;
        public int Sex;
        public int AllowedTransmogSlots;
    }

    public sealed class ChrRacesRecord
    {
        public uint Id;
        public string ClientPrefix;
        public string ClientFileString;
        public LocalizedString Name;
        public string NameFemale;
        public string NameLowercase;
        public string NameFemaleLowercase;
        public string LoreName;
        public string LoreNameFemale;
        public string LoreNameLower;
        public string LoreNameLowerFemale;
        public string LoreDescription;
        public string ShortName;
        public string ShortNameFemale;
        public string ShortNameLower;
        public string ShortNameLowerFemale;
        public int Flags;
        public int FactionID;
        public uint CinematicSequenceID;
        public uint ResSicknessSpellID;
        public int SplashSoundID;
        public int Alliance;
        public int RaceRelated;
        public int UnalteredVisualRaceID;
        public int DefaultClassID;
        public int CreateScreenFileDataID;
        public int SelectScreenFileDataID;
        public int NeutralRaceID;
        public int LowResScreenFileDataID;
        public int[] AlteredFormStartVisualKitID = new int[3];
        public int[] AlteredFormFinishVisualKitID = new int[3];
        public int HeritageArmorAchievementID;
        public int StartingLevel;
        public int UiDisplayOrder;
        public int MaleModelFallbackRaceID;
        public int FemaleModelFallbackRaceID;
        public int MaleTextureFallbackRaceID;
        public int FemaleTextureFallbackRaceID;
        public int PlayableRaceBit;
        public int HelmetAnimScalingRaceID;
        public int TransmogrifyDisabledSlotMask;
        public int UnalteredVisualCustomizationRaceID;
        public float[] AlteredFormCustomizeOffsetFallback = new float[3];
        public float AlteredFormCustomizeRotationFallback;
        public float[] Unknown910_1 = new float[3];
        public float[] Unknown910_2 = new float[3];
        public int Unknown1000;
        public sbyte BaseLanguage;
        public sbyte CreatureType;
        public sbyte MaleModelFallbackSex;
        public sbyte FemaleModelFallbackSex;
        public sbyte MaleTextureFallbackSex;
        public sbyte FemaleTextureFallbackSex;

        public ChrRacesFlag GetFlags() { return (ChrRacesFlag)Flags; }
    }

    public sealed class ChrSpecializationRecord
    {
        public LocalizedString Name;
        public string FemaleName;
        public string Description;
        public uint Id;
        public byte ClassID;
        public byte OrderIndex;
        public sbyte PetTalentType;
        public sbyte Role;
        public ChrSpecializationFlag Flags;
        public int SpellIconFileID;
        public sbyte PrimaryStatPriority;
        public int AnimReplacements;
        public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];

        public bool IsPetSpecialization()
        {
            return ClassID == 0;
        }
    }

    public sealed class CinematicCameraRecord
    {
        public uint Id;
        public Vector3 Origin;                                   // Position in map used for basis for M2 co-ordinates
        public uint SoundID;                                         // Sound ID       (voiceover for cinematic)
        public float OriginFacing;                                     // Orientation in map used for basis for M2 co
        public uint FileDataID;                                      // Model
        public uint ConversationID;
    }

    public sealed class CinematicSequencesRecord
    {
        public uint Id;
        public uint SoundID;
        public ushort[] Camera = new ushort[8];
    }

    public sealed class ContentTuningRecord
    {
        public uint Id;
        public int Flags;
        public int ExpansionID;
        public int MinLevel;
        public int MaxLevel;
        public int MinLevelType;
        public int MaxLevelType;
        public int TargetLevelDelta;
        public int TargetLevelMaxDelta;
        public int TargetLevelMin;
        public int TargetLevelMax;
        public int MinItemLevel;

        public ContentTuningFlag GetFlags() { return (ContentTuningFlag)Flags; }

        public int GetScalingFactionGroup()
        {
            ContentTuningFlag flags = GetFlags();
            if (flags.HasFlag(ContentTuningFlag.Horde))
                return 5;

            if (flags.HasFlag(ContentTuningFlag.Alliance))
                return 3;

            return 0;
        }
    }

    public sealed class ContentTuningXExpectedRecord
    {
        public uint Id;
        public int ExpectedStatModID;
        public int MinMythicPlusSeasonID;
        public int MaxMythicPlusSeasonID;
        public uint ContentTuningID;
    }

    public sealed class ConversationLineRecord
    {
        public uint Id;
        public uint BroadcastTextID;
        public uint SpellVisualKitID;
        public int AdditionalDuration;
        public ushort NextConversationLineID;
        public ushort AnimKitID;
        public byte SpeechType;
        public byte StartAnimation;
        public byte EndAnimation;
    }

    public sealed class CorruptionEffectsRecord
    {
        public uint Id;
        public float MinCorruption;
        public uint Aura;
        public int PlayerConditionID;
        public int Flags;
    }

    public sealed class CreatureDisplayInfoRecord
    {
        public uint Id;
        public ushort ModelID;
        public ushort SoundID;
        public sbyte SizeClass;
        public float CreatureModelScale;
        public byte CreatureModelAlpha;
        public byte BloodID;
        public int ExtendedDisplayInfoID;
        public ushort NPCSoundID;
        public ushort ParticleColorID;
        public int PortraitCreatureDisplayInfoID;
        public int PortraitTextureFileDataID;
        public ushort ObjectEffectPackageID;
        public ushort AnimReplacementSetID;
        public byte Flags;
        public int StateSpellVisualKitID;
        public float PlayerOverrideScale;
        public float PetInstanceScale;                                         // scale of not own player pets inside dungeons/raids/scenarios
        public sbyte UnarmedWeaponType;
        public int MountPoofSpellVisualKitID;
        public int DissolveEffectID;
        public sbyte Gender;
        public int DissolveOutEffectID;
        public sbyte CreatureModelMinLod;
        public int[] TextureVariationFileDataID = new int[4];
    }

    public sealed class CreatureDisplayInfoExtraRecord
    {
        public uint Id;
        public sbyte DisplayRaceID;
        public sbyte DisplaySexID;
        public sbyte DisplayClassID;
        public sbyte Flags;
        public int BakeMaterialResourcesID;
        public int HDBakeMaterialResourcesID;
    }

    public sealed class CreatureFamilyRecord
    {
        public uint Id;
        public LocalizedString Name;
        public float MinScale;
        public sbyte MinScaleLevel;
        public float MaxScale;
        public sbyte MaxScaleLevel;
        public ushort PetFoodMask;
        public sbyte PetTalentType;
        public int IconFileID;
        public short[] SkillLine = new short[2];
    }

    public sealed class CreatureModelDataRecord
    {
        public uint Id;
        public float[] GeoBox = new float[6];
        public uint Flags;
        public uint FileDataID;
        public uint BloodID;
        public uint FootprintTextureID;
        public float FootprintTextureLength;
        public float FootprintTextureWidth;
        public float FootprintParticleScale;
        public uint FoleyMaterialID;
        public uint FootstepCameraEffectID;
        public uint DeathThudCameraEffectID;
        public uint SoundID;
        public uint SizeClass;
        public float CollisionWidth;
        public float CollisionHeight;
        public float WorldEffectScale;
        public uint CreatureGeosetDataID;
        public float HoverHeight;
        public float AttachedEffectScale;
        public float ModelScale;
        public float MissileCollisionRadius;
        public float MissileCollisionPush;
        public float MissileCollisionRaise;
        public float MountHeight;
        public float OverrideLootEffectScale;
        public float OverrideNameScale;
        public float OverrideSelectionRadius;
        public float TamedPetBaseScale;
        public sbyte Unknown820_1;                                              // scale related
        public float Unknown820_2;                                             // scale related
        public float[] Unknown820_3 = new float[2];                            // scale related

        public CreatureModelDataFlags GetFlags() { return (CreatureModelDataFlags)Flags; }
    }

    public sealed class CreatureTypeRecord
    {
        public uint Id;
        public string Name;
        public byte Flags;
    }

    public sealed class CriteriaRecord
    {
        public uint Id;
        public CriteriaType Type;
        public uint Asset;
        public uint ModifierTreeId;
        public byte StartEvent;
        public uint StartAsset;
        public ushort StartTimer;
        public byte FailEvent;
        public uint FailAsset;
        public byte Flags;
        public ushort EligibilityWorldStateID;
        public byte EligibilityWorldStateValue;

        public CriteriaFlags GetFlags() => (CriteriaFlags)Flags;
    }

    public sealed class CriteriaTreeRecord
    {
        public uint Id;
        public string Description;
        public uint Parent;
        public uint Amount;
        public sbyte Operator;
        public uint CriteriaID;
        public int OrderIndex;
        public CriteriaTreeFlags Flags;
    }

    public sealed class CurrencyContainerRecord
    {
        public uint Id;
        public LocalizedString ContainerName;
        public LocalizedString ContainerDescription;
        public int MinAmount;
        public int MaxAmount;
        public int ContainerIconID;
        public int ContainerQuality;
        public int OnLootSpellVisualKitID;
        public uint CurrencyTypesID;
    }

    public sealed class CurrencyTypesRecord
    {
        public uint Id;
        public string Name;
        public string Description;
        public int CategoryID;
        public int InventoryIconFileID;
        public uint SpellWeight;
        public byte SpellCategory;
        public uint MaxQty;
        public uint MaxEarnablePerWeek;
        public sbyte Quality;
        public int FactionID;
        public int ItemGroupSoundsID;
        public int XpQuestDifficulty;
        public int AwardConditionID;
        public int MaxQtyWorldStateID;
        public uint RechargingAmountPerCycle;
        public uint RechargingCycleDurationMS;
        public int[] Flags = new int[2];
    }

    public sealed class CurveRecord
    {
        public uint Id;
        public byte Type;
        public byte Flags;
    }

    public sealed class CurvePointRecord
    {
        public Vector2 Pos;
        public Vector2 PreSLSquishPos;
        public uint Id;
        public ushort CurveID;
        public byte OrderIndex;
    }
}
