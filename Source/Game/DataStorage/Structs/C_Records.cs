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
        public uint[] Geoset = new uint[5];
        public byte RaceID;
        public byte SexID;
        public byte VariationID;
    }

    public sealed class CharBaseSectionRecord
    {
        public uint ID;
        public CharBaseSectionVariation Variation;
        public byte ResolutionVariation;
        public byte Resolution;
    }

    public sealed class CharSectionsRecord
    {
        public uint Id;
        public uint[] TextureFileDataID = new uint[3];
        public ushort Flags;
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
        public byte GenderID;
        public byte OutfitID;
        public byte PetFamilyID;
        public uint RaceID;
    }

    public sealed class CharTitlesRecord
    {
        public uint Id;
        public LocalizedString NameMale;
        public LocalizedString NameFemale;
        public ushort MaskID;
        public byte Flags;
    }

    public sealed class ChatChannelsRecord
    {
        public uint Id;
        public LocalizedString Name;
        public LocalizedString Shortcut;
        public ChannelDBCFlags Flags;
        public byte FactionGroup;
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
        public uint IconFileDataID;
        public uint LowResScreenFileDataID;
        public uint StartingLevel;
        public ushort Flags;
        public ushort CinematicSequenceID;
        public ushort DefaultSpec;
        public PowerType PowerType;
        public byte SpellClassSet;
        public byte AttackPowerPerStrength;
        public byte AttackPowerPerAgility;
        public byte RangedAttackPowerPerAgility;
        public byte Unk1;
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
        public string LowercaseName;
        public string LowercaseNameFemale;
        public uint Flags;
        public uint MaleDisplayID;
        public uint FemaleDisplayID;
        public uint CreateScreenFileDataID;
        public uint SelectScreenFileDataID;
        public uint[] MaleCustomizeOffset = new uint[3];
        public uint[] FemaleCustomizeOffset = new uint[3];
        public uint LowResScreenFileDataID;
        public uint StartingLevel;
        public uint UIDisplayOrder;
        public ushort FactionID;
        public ushort ResSicknessSpellID;
        public ushort SplashSoundID;
        public ushort CinematicSequenceID;
        public byte BaseLanguage;
        public byte CreatureType;
        public byte TeamID;
        public byte RaceRelated;
        public byte UnalteredVisualRaceID;
        public byte CharComponentTextureLayoutID;
        public byte DefaultClassID;
        public byte NeutralRaceID;
        public byte ItemAppearanceFrameRaceID;
        public byte CharComponentTexLayoutHiResID;
        public uint Id;
        public uint HighResMaleDisplayID;
        public uint HighResFemaleDisplayID;
        public uint HeritageArmorAchievementID;
        public uint MaleCorpseBonesModelFileDataID;
        public uint FemaleCorpseBonesModelFileDataID;
        public uint[] AlteredFormTransitionSpellVisualID = new uint[3];
        public uint[] AlteredFormTransitionSpellVisualKitID = new uint[3];
    }

    public sealed class ChrSpecializationRecord
    {
        public string Name;
        public string Name2;
        public string Description;
        public uint[] MasterySpellID = new uint[PlayerConst.MaxMasterySpells];
        public byte ClassID;
        public byte OrderIndex;
        public byte PetTalentType;
        public byte Role;
        public byte PrimaryStatOrder;
        public uint Id;
        public uint IconFileDataID;
        public ChrSpecializationFlag Flags;
        public uint AnimReplacementSetID;

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
        public uint ModelFileDataID;
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
        public uint Duration;
        public ushort NextLineID;
        public ushort Unk1;
        public byte Yell;
        public byte Unk2;
        public byte Unk3;
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
        public float PlayerModelScale;
        public uint PortraitCreatureDisplayInfoID;
        public byte BloodID;
        public ushort ParticleColorID;
        public uint CreatureGeosetData;
        public ushort ObjectEffectPackageID;
        public ushort AnimReplacementSetID;
        public sbyte UnarmedWeaponSubclass;
        public uint StateSpellVisualKitID;
        public float InstanceOtherPlayerPetScale;                              // scale of not own player pets inside dungeons/raids/scenarios
        public uint MountSpellVisualKitID;
        public uint[] TextureVariation = new uint[3];
    }

    public sealed class CreatureDisplayInfoExtraRecord
    {
        public uint Id;
        public uint FileDataID;
        public uint HDFileDataID;
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
        public uint IconFileDataID;
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
        public float[] GeoBoxMin = new float[3];
        public float[] GeoBoxMax = new float[3];
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
        public byte FootstepEffectID;
        public byte DeathThudEffectID;
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
        public uint InventoryIconFileDataID;
        public uint SpellWeight;
    }

    public sealed class CurveRecord
    {
        public uint ID;
        public byte Type;
        public byte Unused;
    }

    public sealed class CurvePointRecord
    {
        public uint Id;
        public float X;
        public float Y;
        public ushort CurveID;
        public byte Index;
    }
}
