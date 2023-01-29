// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class Cfg_RegionsRecord
    {
        public uint ChallengeOrigin;
        public uint Id;
        public uint Raidorigin; // Date of first raid reset, all other resets are calculated as this date plus interval
        public byte RegionGroupMask;
        public ushort RegionID;
        public string Tag;
    }

    public sealed class CharTitlesRecord
    {
        public sbyte Flags;
        public uint Id;
        public ushort MaskID;
        public LocalizedString Name;
        public LocalizedString Name1;
    }

    public sealed class CharacterLoadoutRecord
    {
        public sbyte ChrClassID;
        public uint Id;
        public sbyte ItemContext;
        public int Purpose;
        public long RaceMask;

        public bool IsForNewCharacter()
        {
            return Purpose == 9;
        }
    }

    public sealed class CharacterLoadoutItemRecord
    {
        public ushort CharacterLoadoutID;
        public uint Id;
        public uint ItemID;
    }

    public sealed class ChatChannelsRecord
    {
        public sbyte FactionGroup;
        public ChannelDBCFlags Flags;
        public uint Id;
        public LocalizedString Name;
        public int Ruleset;
        public string Shortcut;
    }

    public sealed class ChrClassUIDisplayRecord
    {
        public uint AdvGuidePlayerConditionID;
        public byte ChrClassesID;
        public uint Id;
        public uint SplashPlayerConditionID;
    }

    public sealed class ChrClassesRecord
    {
        public int AlteredFormCharacterCreationIdleVisualFallback;
        public uint ArmorTypeMask;
        public byte AttackPowerPerAgility;
        public byte AttackPowerPerStrength;
        public int CharacterCreationAnimLoopWaitTimeMsFallback;
        public int CharacterCreationGroundVisualFallback;
        public int CharacterCreationIdleGroundVisualFallback;
        public int CharStartKitUnknown901;
        public ushort CinematicSequenceID;
        public byte ClassColorB;
        public byte ClassColorG;
        public byte ClassColorR;
        public uint CreateScreenFileDataID;
        public ushort DefaultSpec;
        public string Description;
        public string DisabledString;
        public PowerType DisplayPower;
        public int FemaleCharacterCreationIdleVisualFallback;
        public int FemaleCharacterCreationVisualFallback;
        public string Filename;
        public int Flags;
        public string HyphenatedNameFemale;
        public string HyphenatedNameMale;
        public uint IconFileDataID;
        public uint Id;
        public uint LowResScreenFileDataID;
        public int MaleCharacterCreationIdleVisualFallback;
        public int MaleCharacterCreationVisualFallback;
        public LocalizedString Name;
        public string NameFemale;
        public string NameMale;
        public string PetNameToken;
        public byte PrimaryStatPriority;
        public byte RangedAttackPowerPerAgility;
        public string RoleInfoString;
        public uint RolesMask;
        public uint SelectScreenFileDataID;
        public byte SpellClassSet;
        public uint SpellTextureBlobFileDataID;
    }

    public sealed class ChrClassesXPowerTypesRecord
    {
        public uint ClassID;
        public uint Id;
        public sbyte PowerType;
    }

    public sealed class ChrCustomizationChoiceRecord
    {
        public int AddedInPatch;
        public uint ChrCustomizationOptionID;
        public uint ChrCustomizationReqID;
        public int ChrCustomizationVisReqID;
        public int Flags;
        public uint Id;
        public LocalizedString Name;
        public ushort SortOrder;
        public int[] SwatchColor = new int[2];
        public ushort UiOrderIndex;
    }

    public sealed class ChrCustomizationDisplayInfoRecord
    {
        public float BarberShopHeightOffset;
        public float BarberShopMinCameraDistance;
        public uint DisplayID;
        public uint Id;
        public int ShapeshiftFormID;
    }

    public sealed class ChrCustomizationElementRecord
    {
        public int ChrCustItemGeoModifyID;
        public int ChrCustomizationBoneSetID;
        public uint ChrCustomizationChoiceID;
        public int ChrCustomizationCondModelID;
        public int ChrCustomizationDisplayInfoID;
        public int ChrCustomizationGeosetID;
        public int ChrCustomizationMaterialID;
        public int ChrCustomizationSkinnedModelID;
        public int ChrCustomizationVoiceID;
        public uint Id;
        public int RelatedChrCustomizationChoiceID;
    }

    public sealed class ChrCustomizationOptionRecord
    {
        public int AddedInPatch;
        public float BarberShopCostModifier;
        public int ChrCustomizationCategoryID;
        public int ChrCustomizationID;
        public int ChrCustomizationReqID;
        public uint ChrModelID;
        public int Flags;
        public uint Id;
        public LocalizedString Name;
        public int OptionType;
        public ushort SecondaryID;
        public int SortIndex;
        public int UiOrderIndex;
    }

    public sealed class ChrCustomizationReqRecord
    {
        public int AchievementID;
        public int ClassMask;
        public int Flags;
        public uint Id;
        public uint ItemModifiedAppearanceID;
        public int OverrideArchive; // -1: allow any, otherwise must match OverrideArchive cvar
        public int QuestID;
        public string ReqSource;

        public ChrCustomizationReqFlag GetFlags()
        {
            return (ChrCustomizationReqFlag)Flags;
        }
    }

    public sealed class ChrCustomizationReqChoiceRecord
    {
        public uint ChrCustomizationChoiceID;
        public uint ChrCustomizationReqID;
        public uint Id;
    }

    public sealed class ChrModelRecord
    {
        public float BarberShopCameraHeightOffsetScale; // applied after BarberShopCameraOffsetScale
        public float BarberShopCameraOffsetScale;
        public float BarberShopCameraRotationOffset;
        public float CameraDistanceOffset;
        public int CharComponentTextureLayoutID;
        public float CustomizeFacing;
        public float[] CustomizeOffset = new float[3];
        public float CustomizeScale;
        public uint DisplayID;
        public float[] FaceCustomizationOffset = new float[3];
        public int Flags;
        public int HelmVisFallbackChrModelID;
        public uint Id;
        public int ModelFallbackChrModelID;
        public sbyte Sex;
        public int SkeletonFileDataID;
        public int TextureFallbackChrModelID;
    }

    public sealed class ChrRaceXChrModelRecord
    {
        public int AllowedTransmogSlots;
        public int ChrModelID;
        public int ChrRacesID;
        public uint Id;
        public int Sex;
    }

    public sealed class ChrRacesRecord
    {
        public int Alliance;
        public float[] AlteredFormCustomizeOffsetFallback = new float[3];
        public float AlteredFormCustomizeRotationFallback;
        public int[] AlteredFormFinishVisualKitID = new int[3];
        public int[] AlteredFormStartVisualKitID = new int[3];
        public sbyte BaseLanguage;
        public uint CinematicSequenceID;
        public string ClientFileString;
        public string ClientPrefix;
        public int CreateScreenFileDataID;
        public sbyte CreatureType;
        public int DefaultClassID;
        public int FactionID;
        public int FemaleModelFallbackRaceID;
        public sbyte FemaleModelFallbackSex;
        public int FemaleTextureFallbackRaceID;
        public sbyte FemaleTextureFallbackSex;
        public int Flags;
        public int HelmetAnimScalingRaceID;
        public int HeritageArmorAchievementID;
        public uint Id;
        public string LoreDescription;
        public string LoreName;
        public string LoreNameFemale;
        public string LoreNameLower;
        public string LoreNameLowerFemale;
        public int LowResScreenFileDataID;
        public int MaleModelFallbackRaceID;
        public sbyte MaleModelFallbackSex;
        public int MaleTextureFallbackRaceID;
        public sbyte MaleTextureFallbackSex;
        public LocalizedString Name;
        public string NameFemale;
        public string NameFemaleLowercase;
        public string NameLowercase;
        public int NeutralRaceID;
        public int PlayableRaceBit;
        public int RaceRelated;
        public uint ResSicknessSpellID;
        public int SelectScreenFileDataID;
        public string ShortName;
        public string ShortNameFemale;
        public string ShortNameLower;
        public string ShortNameLowerFemale;
        public int SplashSoundID;
        public int StartingLevel;
        public int TransmogrifyDisabledSlotMask;
        public int UiDisplayOrder;
        public int UnalteredVisualCustomizationRaceID;
        public int UnalteredVisualRaceID;
        public int Unknown1000;
        public float[] Unknown910_1 = new float[3];
        public float[] Unknown910_2 = new float[3];

        public ChrRacesFlag GetFlags()
        {
            return (ChrRacesFlag)Flags;
        }
    }

    public sealed class ChrSpecializationRecord
    {
        public int AnimReplacements;
        public byte ClassID;
        public string Description;
        public string FemaleName;
        public ChrSpecializationFlag Flags;
        public uint Id;
        public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];
        public LocalizedString Name;
        public byte OrderIndex;
        public sbyte PetTalentType;
        public sbyte PrimaryStatPriority;
        public sbyte Role;
        public int SpellIconFileID;

        public bool IsPetSpecialization()
        {
            return ClassID == 0;
        }
    }

    public sealed class CinematicCameraRecord
    {
        public uint ConversationID;
        public uint FileDataID; // Model
        public uint Id;
        public Vector3 Origin;     // Position in map used for basis for M2 co-ordinates
        public float OriginFacing; // Orientation in map used for basis for M2 co
        public uint SoundID;       // Sound ID       (voiceover for cinematic)
    }

    public sealed class CinematicSequencesRecord
    {
        public ushort[] Camera = new ushort[8];
        public uint Id;
        public uint SoundID;
    }

    public sealed class ContentTuningRecord
    {
        public int ExpansionID;
        public int Flags;
        public uint Id;
        public int MaxLevel;
        public int MaxLevelType;
        public int MinItemLevel;
        public int MinLevel;
        public int MinLevelType;
        public int TargetLevelDelta;
        public int TargetLevelMax;
        public int TargetLevelMaxDelta;
        public int TargetLevelMin;

        public ContentTuningFlag GetFlags()
        {
            return (ContentTuningFlag)Flags;
        }

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
        public uint ContentTuningID;
        public int ExpectedStatModID;
        public uint Id;
        public int MaxMythicPlusSeasonID;
        public int MinMythicPlusSeasonID;
    }

    public sealed class ConversationLineRecord
    {
        public int AdditionalDuration;
        public ushort AnimKitID;
        public uint BroadcastTextID;
        public byte EndAnimation;
        public uint Id;
        public ushort NextConversationLineID;
        public byte SpeechType;
        public uint SpellVisualKitID;
        public byte StartAnimation;
    }

    public sealed class CorruptionEffectsRecord
    {
        public uint Aura;
        public int Flags;
        public uint Id;
        public float MinCorruption;
        public int PlayerConditionID;
    }

    public sealed class CreatureDisplayInfoRecord
    {
        public ushort AnimReplacementSetID;
        public byte BloodID;
        public byte CreatureModelAlpha;
        public sbyte CreatureModelMinLod;
        public float CreatureModelScale;
        public int DissolveEffectID;
        public int DissolveOutEffectID;
        public int ExtendedDisplayInfoID;
        public byte Flags;
        public sbyte Gender;
        public uint Id;
        public ushort ModelID;
        public int MountPoofSpellVisualKitID;
        public ushort NPCSoundID;
        public ushort ObjectEffectPackageID;
        public ushort ParticleColorID;
        public float PetInstanceScale; // scale of not own player pets inside dungeons/raids/scenarios
        public float PlayerOverrideScale;
        public int PortraitCreatureDisplayInfoID;
        public int PortraitTextureFileDataID;
        public sbyte SizeClass;
        public ushort SoundID;
        public int StateSpellVisualKitID;
        public int[] TextureVariationFileDataID = new int[4];
        public sbyte UnarmedWeaponType;
    }

    public sealed class CreatureDisplayInfoExtraRecord
    {
        public int BakeMaterialResourcesID;
        public sbyte DisplayClassID;
        public sbyte DisplayRaceID;
        public sbyte DisplaySexID;
        public sbyte Flags;
        public int HDBakeMaterialResourcesID;
        public uint Id;
    }

    public sealed class CreatureFamilyRecord
    {
        public int IconFileID;
        public uint Id;
        public float MaxScale;
        public sbyte MaxScaleLevel;
        public float MinScale;
        public sbyte MinScaleLevel;
        public LocalizedString Name;
        public ushort PetFoodMask;
        public sbyte PetTalentType;
        public short[] SkillLine = new short[2];
    }

    public sealed class CreatureModelDataRecord
    {
        public float AttachedEffectScale;
        public uint BloodID;
        public float CollisionHeight;
        public float CollisionWidth;
        public uint CreatureGeosetDataID;
        public uint DeathThudCameraEffectID;
        public uint FileDataID;
        public uint Flags;
        public uint FoleyMaterialID;
        public float FootprintParticleScale;
        public uint FootprintTextureID;
        public float FootprintTextureLength;
        public float FootprintTextureWidth;
        public uint FootstepCameraEffectID;
        public float[] GeoBox = new float[6];
        public float HoverHeight;
        public uint Id;
        public float MissileCollisionPush;
        public float MissileCollisionRadius;
        public float MissileCollisionRaise;
        public float ModelScale;
        public float MountHeight;
        public float OverrideLootEffectScale;
        public float OverrideNameScale;
        public float OverrideSelectionRadius;
        public uint SizeClass;
        public uint SoundID;
        public float TamedPetBaseScale;
        public sbyte Unknown820_1;                  // scale related
        public float Unknown820_2;                  // scale related
        public float[] Unknown820_3 = new float[2]; // scale related
        public float WorldEffectScale;

        public CreatureModelDataFlags GetFlags()
        {
            return (CreatureModelDataFlags)Flags;
        }
    }

    public sealed class CreatureTypeRecord
    {
        public byte Flags;
        public uint Id;
        public string Name;
    }

    public sealed class CriteriaRecord
    {
        public uint Asset;
        public ushort EligibilityWorldStateID;
        public byte EligibilityWorldStateValue;
        public uint FailAsset;
        public byte FailEvent;
        public byte Flags;
        public uint Id;
        public uint ModifierTreeId;
        public uint StartAsset;
        public byte StartEvent;
        public ushort StartTimer;
        public CriteriaType Type;

        public CriteriaFlags GetFlags()
        {
            return (CriteriaFlags)Flags;
        }
    }

    public sealed class CriteriaTreeRecord
    {
        public uint Amount;
        public uint CriteriaID;
        public string Description;
        public CriteriaTreeFlags Flags;
        public uint Id;
        public sbyte Operator;
        public int OrderIndex;
        public uint Parent;
    }

    public sealed class CurrencyContainerRecord
    {
        public LocalizedString ContainerDescription;
        public int ContainerIconID;
        public LocalizedString ContainerName;
        public int ContainerQuality;
        public uint CurrencyTypesID;
        public uint Id;
        public int MaxAmount;
        public int MinAmount;
        public int OnLootSpellVisualKitID;
    }

    public sealed class CurrencyTypesRecord
    {
        public int AwardConditionID;
        public int CategoryID;
        public string Description;
        public int FactionID;
        public int[] Flags = new int[2];
        public uint Id;
        public int InventoryIconFileID;
        public int ItemGroupSoundsID;
        public uint MaxEarnablePerWeek;
        public uint MaxQty;
        public int MaxQtyWorldStateID;
        public string Name;
        public sbyte Quality;
        public uint RechargingAmountPerCycle;
        public uint RechargingCycleDurationMS;
        public byte SpellCategory;
        public uint SpellWeight;
        public int XpQuestDifficulty;
    }

    public sealed class CurveRecord
    {
        public byte Flags;
        public uint Id;
        public byte Type;
    }

    public sealed class CurvePointRecord
    {
        public ushort CurveID;
        public uint Id;
        public byte OrderIndex;
        public Vector2 Pos;
        public Vector2 PreSLSquishPos;
    }

    public sealed class CharBaseInfo
    {
        public uint Id { get; set; }
        public byte RaceId { get; set; }
        public byte ClassId { get; set; }
        public int FactionXferId { get; set; }
    }
}