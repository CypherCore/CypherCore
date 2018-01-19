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
    public sealed class GameObjectsRecord
    {
        public Vector3 Position;
        public float RotationX;
        public float RotationY;
        public float RotationZ;
        public float RotationW;
        public float Size;
        public int[] Data = new int[8];
        public LocalizedString Name;
        public ushort MapID;
        public ushort DisplayID;
        public ushort PhaseID;
        public ushort PhaseGroupID;
        public byte PhaseUseFlags;
        public GameObjectTypes Type;
        public uint Id;
    }

    public sealed class GameObjectDisplayInfoRecord
    {
        public uint Id;
        public uint FileDataID;
        public Vector3 GeoBoxMin;
        public Vector3 GeoBoxMax;
        public float OverrideLootEffectScale;
        public float OverrideNameScale;
        public ushort ObjectEffectPackageID;
    }

    public sealed class GarrAbilityRecord
    {
        public LocalizedString Name;
        public LocalizedString Description;
        public uint IconFileDataID;
        public GarrisonAbilityFlags Flags;
        public ushort OtherFactionGarrAbilityID;
        public byte GarrAbilityCategoryID;
        public byte FollowerTypeID;
        public uint Id;
    }

    public sealed class GarrBuildingRecord
    {
        public uint Id;
        public uint HordeGameObjectID;
        public uint AllianceGameObjectID;
        public LocalizedString NameAlliance;
        public LocalizedString NameHorde;
        public LocalizedString Description;
        public LocalizedString Tooltip;
        public uint IconFileDataID;
        public ushort CostCurrencyID;
        public ushort HordeTexPrefixKitID;
        public ushort AllianceTexPrefixKitID;
        public ushort AllianceActivationScenePackageID;
        public ushort HordeActivationScenePackageID;
        public ushort FollowerRequiredGarrAbilityID;
        public ushort FollowerGarrAbilityEffectID;
        public short CostMoney;
        public byte Unknown;
        public byte Type;
        public byte Level;
        public GarrisonBuildingFlags Flags;
        public byte MaxShipments;
        public byte GarrTypeID;
        public int BuildDuration;
        public int CostCurrencyAmount;
        public int BonusAmount;
    }

    public sealed class GarrBuildingPlotInstRecord
    {
        public Vector2 LandmarkOffset;
        public ushort UiTextureAtlasMemberID;
        public ushort GarrSiteLevelPlotInstID;
        public byte GarrBuildingID;
        public uint Id;
    }

    public sealed class GarrClassSpecRecord
    {
        public LocalizedString NameMale;
        public LocalizedString NameFemale;
        public LocalizedString NameGenderless;
        public ushort ClassAtlasID;                                            // UiTextureAtlasMember.db2 ref
        public ushort GarrFollItemSetID;
        public byte Limit;
        public byte Flags;
        public uint Id;
    }

    public sealed class GarrFollowerRecord
    {
        public uint HordeCreatureID;
        public uint AllianceCreatureID;
        public LocalizedString HordeSourceText;
        public LocalizedString AllianceSourceText;
        public uint HordePortraitIconID;
        public uint AlliancePortraitIconID;
        public uint HordeAddedBroadcastTextID;
        public uint AllianceAddedBroadcastTextID;
        public LocalizedString Name;
        public ushort HordeGarrFollItemSetID;
        public ushort AllianceGarrFollItemSetID;
        public ushort ItemLevelWeapon;
        public ushort ItemLevelArmor;
        public ushort HordeListPortraitTextureKitID;
        public ushort AllianceListPortraitTextureKitID;
        public byte FollowerTypeID;
        public byte HordeUiAnimRaceInfoID;
        public byte AllianceUiAnimRaceInfoID;
        public byte Quality;
        public byte HordeGarrClassSpecID;
        public byte AllianceGarrClassSpecID;
        public byte Level;
        public byte Unknown1;
        public byte Flags;
        public sbyte Unknown2;
        public sbyte Unknown3;
        public byte GarrTypeID;
        public byte MaxDurability;
        public byte Class;
        public byte HordeFlavorTextGarrStringID;
        public byte AllianceFlavorTextGarrStringID;
        public uint Id;
    }

    public sealed class GarrFollowerXAbilityRecord
    {
        public uint Id;
        public ushort GarrFollowerID;
        public ushort GarrAbilityID;
        public byte FactionIndex;
    }

    public sealed class GarrPlotRecord
    {
        public uint Id;
        public LocalizedString Name;
        public uint AllianceConstructionGameObjectID;
        public uint HordeConstructionGameObjectID;
        public byte GarrPlotUICategoryID;
        public byte PlotType;
        public byte Flags;
        public uint MinCount;
        public uint MaxCount;
    }

    public sealed class GarrPlotBuildingRecord
    {
        public uint Id;
        public byte GarrPlotID;
        public byte GarrBuildingID;
    }

    public sealed class GarrPlotInstanceRecord
    {
        public uint Id;
        public LocalizedString Name;
        public byte GarrPlotID;
    }

    public sealed class GarrSiteLevelRecord
    {
        public uint Id;
        public Vector2 TownHall;
        public ushort MapID;
        public ushort SiteID;
        public ushort MovieID;
        public ushort UpgradeResourceCost;
        public ushort UpgradeMoneyCost;
        public byte Level;
        public byte UITextureKitID;
        public byte Level2;
    }

    public sealed class GarrSiteLevelPlotInstRecord
    {
        public uint Id;
        public Vector2 Landmark;
        public ushort GarrSiteLevelID;
        public byte GarrPlotInstanceID;
        public byte Unknown;
    }

    public sealed class GemPropertiesRecord
    {
        public uint Id;
        public SocketColor Type;
        public ushort EnchantID;
        public ushort MinItemLevel;
    }

    public sealed class GlyphBindableSpellRecord
    {
        public uint Id;
        public uint SpellID;
        public ushort GlyphPropertiesID;
    }

    public sealed class GlyphPropertiesRecord
    {
        public uint Id;
        public uint SpellID;
        public ushort SpellIconID;
        public byte Type;
        public byte GlyphExclusiveCategoryID;
    }

    public sealed class GlyphRequiredSpecRecord
    {
        public uint Id;
        public ushort GlyphPropertiesID;
        public ushort ChrSpecializationID;
    }

    public sealed class GuildColorBackgroundRecord
    {
        public uint Id;

        public byte Red;
        public byte Green;
        public byte Blue;
    }

    public sealed class GuildColorBorderRecord
    {
        public uint Id;
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    public sealed class GuildColorEmblemRecord
    {
        public uint Id;
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    public sealed class GuildPerkSpellsRecord
    {
        public uint Id;
        public uint SpellID;
    }
}
