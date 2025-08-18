// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class Cfg_CategoriesRecord
    {
        public uint Id;
        public LocalizedString Name;
        public ushort LocaleMask;
        public byte CreateCharsetMask;
        public byte ExistingCharsetMask;
        public byte Flags;
        public sbyte Order;

        public CfgCategoriesCharsets GetCreateCharsetMask() { return (CfgCategoriesCharsets)CreateCharsetMask; }
        public CfgCategoriesCharsets GetExistingCharsetMask() { return (CfgCategoriesCharsets)ExistingCharsetMask; }
        public bool HasFlag(CfgCategoriesFlags cfgCategoriesFlags) { return (Flags & (byte)cfgCategoriesFlags) != 0; }
    }

    public sealed class Cfg_RegionsRecord
    {
        public uint Id;
        public string Tag;
        public ushort RegionID;
        public uint Raidorigin;                                              // Date of first raid reset, all other resets are calculated as this date plus interval
        public byte RegionGroupMask;
        public uint ChallengeOrigin;
    }

    public sealed class ChallengeModeItemBonusOverrideRecord
    {
        public uint Id;
        public int ItemBonusTreeGroupID;
        public int DstItemBonusTreeID;
        public int Value;
        public int RequiredTimeEventPassed;
        public int RequiredTimeEventNotPassed;
        public uint SrcItemBonusTreeID;
    }

    public sealed class CharBaseInfoRecord
    {
        public uint Id;
        public sbyte RaceID;
        public sbyte ClassID;
        public sbyte OtherFactionRaceID;
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
        public byte ItemContext;

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
        public int Flags;
        public byte FactionGroup;
        public int Ruleset;

        public bool HasFlag(ChatChannelFlags chatChannelFlags) { return (Flags & (int)chatChannelFlags) != 0; }
        public ChatChannelRuleset GetRuleset() { return (ChatChannelRuleset)Ruleset; }
    }

    public sealed class ChrClassUIDisplayRecord
    {
        public uint Id;
        public sbyte ChrClassesID;
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
        public uint CreateScreenFileDataID;
        public uint SelectScreenFileDataID;
        public uint IconFileDataID;
        public uint LowResScreenFileDataID;
        public int Flags;
        public uint SpellTextureBlobFileDataID;
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
        public uint Id;
        public sbyte PrimaryStatPriority;
        public PowerType DisplayPower;
        public byte RangedAttackPowerPerAgility;
        public byte AttackPowerPerAgility;
        public byte AttackPowerPerStrength;
        public byte SpellClassSet;
        public byte ClassColorR;
        public byte ClassColorG;
        public byte ClassColorB;
        public byte RolesMask;
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
        public int SoundKitID;
        public int[] SwatchColor = new int[2];
    }

    public sealed class ChrCustomizationDisplayInfoRecord
    {
        public uint Id;
        public int ShapeshiftFormID;
        public uint DisplayID;
        public float BarberShopMinCameraDistance;
        public float BarberShopHeightOffset;
        public float BarberShopCameraZoomOffset;
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
        public int AnimKitID;
        public int ParticleColorID;
        public int ChrCustGeoComponentLinkID;
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
        public long RaceMask;
        public string ReqSource;
        public int Flags;
        public int ClassMask;
        public int RegionGroupMask;
        public int AchievementID;
        public int QuestID;
        public int OverrideArchive;                                          // -1: allow any, otherwise must match OverrideArchive cvar
        public uint ItemModifiedAppearanceID;

        public bool HasFlag(ChrCustomizationReqFlag chrCustomizationReqFlag) { return (Flags & (int)chrCustomizationReqFlag) != 0; }
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
        public byte ChrRacesID;
        public int ChrModelID;
        public sbyte Sex;
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
        public int CreateScreenFileDataID;
        public int SelectScreenFileDataID;
        public int LowResScreenFileDataID;
        public int[] AlteredFormStartVisualKitID = new int[3];
        public int[] AlteredFormFinishVisualKitID = new int[3];
        public int HeritageArmorAchievementID;
        public int StartingLevel;
        public int UiDisplayOrder;
        public int PlayableRaceBit;
        public int TransmogrifyDisabledSlotMask;
        public float[] AlteredFormCustomizeOffsetFallback = new float[3];
        public float AlteredFormCustomizeRotationFallback;
        public float[] Unknown910_1 = new float[3];
        public float[] Unknown910_2 = new float[3];
        public sbyte BaseLanguage;
        public byte CreatureType;
        public sbyte Alliance;
        public sbyte RaceRelated;
        public sbyte UnalteredVisualRaceID;
        public sbyte DefaultClassID;
        public sbyte NeutralRaceID;
        public sbyte MaleModelFallbackRaceID;
        public sbyte MaleModelFallbackSex;
        public sbyte FemaleModelFallbackRaceID;
        public sbyte FemaleModelFallbackSex;
        public sbyte MaleTextureFallbackRaceID;
        public sbyte MaleTextureFallbackSex;
        public sbyte FemaleTextureFallbackRaceID;
        public sbyte FemaleTextureFallbackSex;
        public sbyte HelmetAnimScalingRaceID;
        public sbyte UnalteredVisualCustomizationRaceID;

        public bool HasFlag(ChrRacesFlag chrRacesFlag) { return (Flags & (int)chrRacesFlag) != 0; }
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
        public uint Flags;
        public int SpellIconFileID;
        public sbyte PrimaryStatPriority;
        public int AnimReplacements;
        public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];

        public bool HasFlag(ChrSpecializationFlag chrSpecializationFlag) { return (Flags & (uint)chrSpecializationFlag) != 0; }
        public ChrSpecializationRole GetRole() { return (ChrSpecializationRole)Role; }

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

    public sealed class ConditionalChrModelRecord
    {
        public uint Id;
        public int ChrModelID;
        public int ChrCustomizationReqID;
        public int PlayerConditionID;
        public int Flags;
        public int ChrCustomizationCategoryID;
    }

    public sealed class ConditionalContentTuningRecord
    {
        public uint Id;
        public int OrderIndex;
        public int RedirectContentTuningID;
        public int RedirectFlag;
        public uint ParentContentTuningID;
    }

    public sealed class ContentTuningRecord
    {
        public uint Id;
        public int Flags;
        public int ExpansionID;
        public int HealthItemLevelCurveID;
        public int DamageItemLevelCurveID;
        public int HealthPrimaryStatCurveID;
        public int DamagePrimaryStatCurveID;
        public int MinLevel;
        public int MaxLevel;
        public int MinLevelType;
        public int MaxLevelType;
        public int TargetLevelDelta;
        public int TargetLevelMaxDelta;
        public int TargetLevelMin;
        public int TargetLevelMax;
        public int MinItemLevel;
        public float QuestXpMultiplier;

        public bool HasFlag(ContentTuningFlag contentTuningFlag) { return (Flags & (int)contentTuningFlag) != 0; }

        public int GetScalingFactionGroup()
        {
            if (HasFlag(ContentTuningFlag.Horde))
                return 5;

            if (HasFlag(ContentTuningFlag.Alliance))
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

    public sealed class ContentTuningXLabelRecord
    {
        public uint Id;
        public int LabelID;
        public uint ContentTuningID;
    }

    public sealed class ConversationLineRecord
    {
        public uint Id;
        public uint BroadcastTextID;
        public uint Unused1020;
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
        public int Flags;
        public int StateSpellVisualKitID;
        public float PlayerOverrideScale;
        public float PetInstanceScale;                                         // scale of not own player pets inside dungeons/raids/scenarios
        public sbyte UnarmedWeaponType;
        public int MountPoofSpellVisualKitID;
        public int DissolveEffectID;
        public sbyte Gender;
        public int DissolveOutEffectID;
        public sbyte CreatureModelMinLod;
        public ushort ConditionalCreatureModelID;
        public float Unknown_1100_1;
        public ushort Unknown_1100_2;
        public int[] TextureVariationFileDataID = new int[4];
    }

    public sealed class CreatureDisplayInfoExtraRecord
    {
        public uint Id;
        public sbyte DisplayRaceID;
        public sbyte DisplaySexID;
        public sbyte DisplayClassID;
        public int Flags;
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

    public sealed class CreatureLabelRecord
    {
        public uint Id;
        public int LabelID;
        public uint CreatureDifficultyID;
    }

    public sealed class CreatureModelDataRecord
    {
        public uint Id;
        public float[] GeoBox = new float[6];
        public int Flags;
        public uint FileDataID;
        public float WalkSpeed;
        public float RunSpeed;
        public uint BloodID;
        public uint FootprintTextureID;
        public float FootprintTextureLength;
        public float FootprintTextureWidth;
        public float FootprintParticleScale;
        public uint FoleyMaterialID;
        public uint FootstepCameraEffectID;
        public uint DeathThudCameraEffectID;
        public uint SoundID;
        public sbyte SizeClass;
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
        public sbyte MountScaleOtherIndex;
        public float MountScaleSelf;
        public ushort Unknown1100;
        public float[] MountScaleOther = new float[2];

        public bool HasFlag(CreatureModelDataFlags creatureModelDataFlags) { return (Flags & (uint)creatureModelDataFlags) != 0; }
    }

    public sealed class CreatureTypeRecord
    {
        public uint Id;
        public string Name;
        public int Flags;
    }

    public sealed class CriteriaRecord
    {
        public uint Id;
        public CriteriaType Type;
        public uint Asset;
        public uint ModifierTreeId;
        public int StartEvent;
        public uint StartAsset;
        public ushort StartTimer;
        public int FailEvent;
        public uint FailAsset;
        public int Flags;
        public ushort EligibilityWorldStateID;
        public byte EligibilityWorldStateValue;

        public bool HasFlag(CriteriaFlags criteriaFlags) { return (Flags & (int)criteriaFlags) != 0; }
    }

    public sealed class CriteriaTreeRecord
    {
        public uint Id;
        public string Description;
        public uint Parent;
        public uint Amount;
        public int Operator;
        public uint CriteriaID;
        public int OrderIndex;
        public int Flags;

        public bool HasFlag(CriteriaTreeFlags criteriaTreeFlags) { return (Flags & (int)criteriaTreeFlags) != 0; }
    }

    public sealed class CurrencyContainerRecord
    {
        public uint Id;
        public LocalizedString ContainerName;
        public LocalizedString ContainerDescription;
        public int MinAmount;
        public int MaxAmount;
        public int ContainerIconID;
        public sbyte ContainerQuality;
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
        public float AccountTransferPercentage;
        public byte OrderIndex;
        public int[] Flags = new int[2];

        public bool HasFlag(CurrencyTypesFlags currencyTypesFlags) { return (Flags[0] & (int)currencyTypesFlags) != 0; }
        public bool HasFlag(CurrencyTypesFlagsB currencyTypesFlagsB) { return (Flags[1] & (int)currencyTypesFlagsB) != 0; }

        // Helpers
        public int GetScaler()
        {
            return HasFlag(CurrencyTypesFlags._100_Scaler) ? 100 : 1;
        }

        public bool HasMaxEarnablePerWeek()
        {
            return MaxEarnablePerWeek != 0 || HasFlag(CurrencyTypesFlags.ComputedWeeklyMaximum);
        }

        public bool HasMaxQuantity(bool onLoad = false, bool onUpdateVersion = false)
        {
            if (onLoad && HasFlag(CurrencyTypesFlags.IgnoreMaxQtyOnLoad))
                return false;

            if (onUpdateVersion && HasFlag(CurrencyTypesFlags.UpdateVersionIgnoreMax))
                return false;

            return MaxQty != 0 || MaxQtyWorldStateID != 0 || HasFlag(CurrencyTypesFlags.DynamicMaximum);
        }

        public bool HasTotalEarned()
        {
            return HasFlag(CurrencyTypesFlagsB.UseTotalEarnedForEarned);
        }

        public bool IsAlliance()
        {
            return HasFlag(CurrencyTypesFlags.IsAllianceOnly);
        }

        public bool IsHorde()
        {
            return HasFlag(CurrencyTypesFlags.IsHordeOnly);
        }

        public bool IsSuppressingChatLog(bool onUpdateVersion = false)
        {
            if ((onUpdateVersion && HasFlag(CurrencyTypesFlags.SuppressChatMessageOnVersionChange)) ||
                HasFlag(CurrencyTypesFlags.SuppressChatMessages))
                return true;

            return false;
        }

        public bool IsTrackingQuantity()
        {
            return HasFlag(CurrencyTypesFlags.TrackQuantity);
        }
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
        public uint CurveID;
        public byte OrderIndex;
    }
}
