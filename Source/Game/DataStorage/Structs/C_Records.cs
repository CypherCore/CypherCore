/*
 * Copyright (C) 2012-2018 CypherCore <http://github.com/CypherCore>
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
    public sealed class CharacterFacialHairStylesRecord
    {
        public uint ID;
        public int[] Geoset = new int[5];
        public byte RaceID;
        public byte SexID;
        public byte VariationID;
    }

    public sealed class CharBaseSectionRecord
    {
        public uint ID;
        public CharBaseSectionVariation VariationEnum;
        public byte ResolutionVariationEnum;
        public byte LayoutResType;
    }

    public sealed class CharSectionsRecord
    {
        public uint Id;
        public uint[] MaterialResourcesID = new uint[3];
        public short Flags;
        public byte RaceID;
        public byte SexID;
        public byte BaseSection;
        public byte VariationIndex;
        public byte ColorIndex;
    }

    public sealed class CharStartOutfitRecord
    {
        public uint Id;
        public int[] ItemID = new int[24];
        public uint PetDisplayID;
        public byte ClassID;
        public byte SexID;
        public byte OutfitID;
        public byte PetFamilyID;
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
        public LocalizedString Shortcut;
        public ChannelDBCFlags Flags;
        public sbyte FactionGroup;
    }

    public sealed class ChrClassesRecord
    {
        public string PetNameToken;
        public LocalizedString Name;
        public LocalizedString NameFemale;
        public LocalizedString NameMale;
        public uint FileName;
        public uint CreateScreenFileDataID;
        public uint SelectScreenFileDataID;
        public uint LowResScreenFileDataID;
        public uint IconFileDataID;
        public int StartingLevel;
        public ushort Flags;
        public ushort CinematicSequenceID;
        public ushort DefaultSpec;
        public PowerType DisplayPower;
        public byte SpellClassSet;
        public byte AttackPowerPerStrength;
        public byte AttackPowerPerAgility;
        public byte RangedAttackPowerPerAgility;
        public byte PrimaryStatPriority;
        public uint Id;
    }

    public sealed class ChrClassesXPowerTypesRecord
    {
        public uint Id;
        public byte PowerType;
        public uint ClassID;
    }

    public sealed class ChrRacesRecord
    {
        public uint ClientPrefix;
        public uint ClientFileString;
        public LocalizedString Name;
        public string NameFemale;
        public string NameLowercase;
        public string NameFemaleLowercase;
        public int Flags;
        public uint MaleDisplayId;
        public uint FemaleDisplayId;
        public int CreateScreenFileDataID;
        public int SelectScreenFileDataID;
        public float[] MaleCustomizeOffset = new float[3];
        public float[] FemaleCustomizeOffset = new float[3];
        public int LowResScreenFileDataID;
        public int StartingLevel;
        public int UiDisplayOrder;
        public ushort FactionID;
        public short ResSicknessSpellID;
        public short SplashSoundID;
        public ushort CinematicSequenceID;
        public sbyte BaseLanguage;
        public sbyte CreatureType;
        public sbyte Alliance;
        public sbyte RaceRelated;
        public sbyte UnalteredVisualRaceID;
        public sbyte CharComponentTextureLayoutID;
        public sbyte DefaultClassID;
        public sbyte NeutralRaceID;
        public sbyte DisplayRaceID;
        public sbyte CharComponentTexLayoutHiResID;
        public uint Id;
        public uint HighResMaleDisplayId;
        public uint HighResFemaleDisplayId;
        public int HeritageArmorAchievementID;
        public int MaleSkeletonFileDataID;
        public int FemaleSkeletonFileDataID;
        public uint[] AlteredFormStartVisualKitID = new uint[3];
        public uint[] AlteredFormFinishVisualKitID = new uint[3];
    }

    public sealed class ChrSpecializationRecord
    {
        public string Name;
        public string FemaleName;
        public string Description;
        public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];
        public byte ClassID;
        public byte OrderIndex;
        public byte PetTalentType;
        public byte Role;
        public byte PrimaryStatPriority;
        public uint Id;
        public int SpellIconFileID;
        public ChrSpecializationFlag Flags;
        public int AnimReplacements;

        public bool IsPetSpecialization()
        {
            return ClassID == 0;
        }
    }

    public sealed class CinematicCameraRecord
    {
        public uint ID;
        public uint SoundID;
        public Vector3 Origin;
        public float OriginFacing;
        public uint FileDataID;
    }

    public sealed class CinematicSequencesRecord
    {
        public uint Id;
        public uint SoundID;
        public ushort[] Camera = new ushort[8];
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
        public float CreatureModelScale;
        public ushort ModelID;
        public ushort NPCSoundID;
        public byte SizeClass;
        public byte Flags;
        public sbyte Gender;
        public uint ExtendedDisplayInfoID;
        public uint PortraitTextureFileDataID;
        public byte CreatureModelAlpha;
        public ushort SoundID;
        public float PlayerOverrideScale;
        public uint PortraitCreatureDisplayInfoID;
        public byte BloodID;
        public ushort ParticleColorID;
        public uint CreatureGeosetData;
        public ushort ObjectEffectPackageID;
        public ushort AnimReplacementSetID;
        public sbyte UnarmedWeaponType;
        public uint StateSpellVisualKitID;
        public float PetInstanceScale;                              // scale of not own player pets inside dungeons/raids/scenarios
        public uint MountPoofSpellVisualKitID;
        public uint[] TextureVariationFileDataID = new uint[3];
    }

    public sealed class CreatureDisplayInfoExtraRecord
    {
        public uint Id;
        public uint BakeMaterialResourcesID;
        public uint HDBakeMaterialResourcesID;
        public byte DisplayRaceID;
        public byte DisplaySexID;
        public byte DisplayClassID;
        public byte SkinID;
        public byte FaceID;
        public byte HairStyleID;
        public byte HairColorID;
        public byte FacialHairID;
        public byte[] CustomDisplayOption = new byte[3];
        public byte Flags;
    }

    public sealed class CreatureFamilyRecord
    {
        public uint Id;
        public LocalizedString Name;
        public float MinScale;
        public float MaxScale;
        public uint IconFileID;
        public ushort[] SkillLine = new ushort[2];
        public ushort PetFoodMask;
        public byte MinScaleLevel;
        public byte MaxScaleLevel;
        public byte PetTalentType;
    }

    public sealed class CreatureModelDataRecord
    {
        public uint Id;
        public float ModelScale;
        public float FootprintTextureLength;
        public float FootprintTextureWidth;
        public float FootprintParticleScale;
        public float CollisionWidth;
        public float CollisionHeight; 
        public float MountHeight;
        public float[] GeoBox = new float[6];
        public float WorldEffectScale;
        public float AttachedEffectScale;
        public float MissileCollisionRadius;
        public float MissileCollisionPush;
        public float MissileCollisionRaise;
        public float OverrideLootEffectScale;
        public float OverrideNameScale;
        public float OverrideSelectionRadius;
        public float TamedPetBaseScale;
        public float HoverHeight;
        public uint Flags;
        public uint FileDataID;
        public byte SizeClass;
        public uint BloodID;
        public byte FootprintTextureID;
        public byte FoleyMaterialID;
        public byte FootstepCameraEffectID;
        public byte DeathThudCameraEffectID;
        public uint SoundID;
        public uint CreatureGeosetDataID;
    }

    public sealed class CreatureTypeRecord
    {
        public uint Id;
        public LocalizedString Name;
        public byte Flags;
    }

    public sealed class CriteriaRecord
    {
        public uint Id;
        public uint Asset;
        public uint StartAsset;
        public uint FailAsset;
        public uint ModifierTreeId;
        public ushort StartTimer;
        public ushort EligibilityWorldStateID;
        public CriteriaTypes Type;
        public CriteriaTimedTypes StartEvent;
        public byte FailEvent;
        public byte Flags;
        public byte EligibilityWorldStateValue;
    }

    public sealed class CriteriaTreeRecord
    {
        public uint Id;
        public string Description;
        public uint Amount;
        public CriteriaTreeFlags Flags;
        public byte Operator;
        public ushort CriteriaID;
        public ushort Parent;
        public int OrderIndex;
    }

    public sealed class CurrencyTypesRecord
    {
        public uint Id;
        public string Name;
        public string Description;
        public uint MaxQty;
        public uint MaxEarnablePerWeek;
        public CurrencyFlags Flags;
        public byte CategoryID;
        public byte SpellCategory;
        public byte Quality;
        public uint InventoryIconFileID;
        public uint SpellWeight;
    }

    public sealed class CurveRecord
    {
        public uint ID;
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
