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

using Framework.Constants;
using Framework.GameMath;

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

    public sealed class CharacterFacialHairStylesRecord
    {
        public uint Id;
        public int[] Geoset = new int[5];
        public byte RaceID;
        public byte SexID;
        public byte VariationID;
    }

    public sealed class CharBaseSectionRecord
    {
        public uint Id;
        public byte LayoutResType;
        public CharBaseSectionVariation VariationEnum;
        public byte ResolutionVariationEnum;
    }

    public sealed class CharSectionsRecord
    {
        public uint Id;
        public byte RaceID;
        public byte SexID;
        public sbyte BaseSection;
        public byte VariationIndex;
        public byte ColorIndex;
        public short Flags;
        public int[] MaterialResourcesID = new int[3];
    }

    public sealed class CharStartOutfitRecord
    {
        public uint Id;
        public byte ClassID;
        public byte SexID;
        public byte OutfitID;
        public uint PetDisplayID;                                            // Pet Model ID for starting pet
        public byte PetFamilyID;                                              // Pet Family Entry for starting pet
        public int[] ItemID = new int[24];
        public uint RaceID;
    }

    public sealed class CharTitlesRecord
    {
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Name1;
        public ushort MaskID;
        public sbyte Flags;
    }

    public sealed class ChatChannelsRecord
    {
        public uint Id;
        public LocalizedString Name;
        public string Shortcut;
        public ChannelDBCFlags Flags;
        public sbyte FactionGroup;
    }

    public sealed class ChrClassesRecord
    {
        public LocalizedString Name;
        public string Filename;
        public string NameMale;
        public string NameFemale;
        public string PetNameToken;
        public uint Id;
        public uint CreateScreenFileDataID;
        public uint SelectScreenFileDataID;
        public uint IconFileDataID;
        public uint LowResScreenFileDataID;
        public int StartingLevel;
        public ushort Flags;
        public ushort CinematicSequenceID;
        public ushort DefaultSpec;
        public byte PrimaryStatPriority;
        public PowerType DisplayPower;
        public byte RangedAttackPowerPerAgility;
        public byte AttackPowerPerAgility;
        public byte AttackPowerPerStrength;
        public byte SpellClassSet;
    }

    public sealed class ChrClassesXPowerTypesRecord
    {
        public uint Id;
        public sbyte PowerType;
        public uint ClassID;
    }

    public sealed class ChrRacesRecord
    {
        public string ClientPrefix;
        public string ClientFileString;
        public LocalizedString Name;
        public string NameFemale;
        public string NameLowercase;
        public string NameFemaleLowercase;
        public uint Id;
        public int Flags;
        public uint MaleDisplayId;
        public uint FemaleDisplayId;
        public uint HighResMaleDisplayId;
        public uint HighResFemaleDisplayId;
        public int CreateScreenFileDataID;
        public int SelectScreenFileDataID;
        public float[] MaleCustomizeOffset = new float[3];
        public float[] FemaleCustomizeOffset = new float[3];
        public int LowResScreenFileDataID;
        public uint[] AlteredFormStartVisualKitID = new uint[3];
        public uint[] AlteredFormFinishVisualKitID = new uint[3];
        public int HeritageArmorAchievementID;
        public int StartingLevel;
        public int UiDisplayOrder;
        public int FemaleSkeletonFileDataID;
        public int MaleSkeletonFileDataID;
        public int HelmVisFallbackRaceID;
        public ushort FactionID;
        public ushort CinematicSequenceID;
        public short ResSicknessSpellID;
        public short SplashSoundID;
        public sbyte BaseLanguage;
        public sbyte CreatureType;
        public sbyte Alliance;
        public sbyte RaceRelated;
        public sbyte UnalteredVisualRaceID;
        public sbyte CharComponentTextureLayoutID;
        public sbyte CharComponentTexLayoutHiResID;
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
    }

    public sealed class ChrSpecializationRecord
    {
        public string Name;
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
        public int MinLevel;
        public int MaxLevel;
        public int Flags;
        public int ExpectedStatModID;
        public int DifficultyESMID;
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
        public int[] TextureVariationFileDataID = new int[3];
    }

    public sealed class CreatureDisplayInfoExtraRecord
    {
        public uint Id;
        public sbyte DisplayRaceID;
        public sbyte DisplaySexID;
        public sbyte DisplayClassID;
        public sbyte SkinID;
        public sbyte FaceID;
        public sbyte HairStyleID;
        public sbyte HairColorID;
        public sbyte FacialHairID;
        public sbyte Flags;
        public int BakeMaterialResourcesID;
        public int HDBakeMaterialResourcesID;
        public byte[] CustomDisplayOption = new byte[3];
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
        public CriteriaTypes Type;
        public uint Asset;
        public uint ModifierTreeId;
        public CriteriaTimedTypes StartEvent;
        public uint StartAsset;
        public ushort StartTimer;
        public byte FailEvent;
        public uint FailAsset;
        public byte Flags;
        public ushort EligibilityWorldStateID;
        public byte EligibilityWorldStateValue;
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

    public sealed class CurrencyTypesRecord
    {
        public uint Id;
        public string Name;
        public string Description;
        public byte CategoryID;
        public int InventoryIconFileID;
        public uint SpellWeight;
        public byte SpellCategory;
        public uint MaxQty;
        public uint MaxEarnablePerWeek;
        public uint Flags;
        public sbyte Quality;
        public int FactionID;
    }

    public sealed class CurveRecord
    {
        public uint Id;
        public byte Type;
        public byte Flags;
    }

    public sealed class CurvePointRecord
    {
        public uint Id;
        public Vector2 Pos;
        public ushort CurveID;
        public byte OrderIndex;
    }
}
