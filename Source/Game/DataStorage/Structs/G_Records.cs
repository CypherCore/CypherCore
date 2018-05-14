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

    public sealed class GameObjectsRecord
    {
        public LocalizedString Name;
        public Vector3 Pos;
        public float[] Rot = new float[4];
        public float Scale;
        public int[] PropValue = new int[8];
        public ushort OwnerID;
        public ushort DisplayID;
        public ushort PhaseID;
        public ushort PhaseGroupID;
        public byte PhaseUseFlags;
        public GameObjectTypes TypeID;
        public uint Id;
    }

    public sealed class GarrAbilityRecord
    {
        public string Name;
        public string Description;
        public uint IconFileDataID;
        public GarrisonAbilityFlags Flags;
        public ushort FactionChangeGarrAbilityID;
        public byte GarrAbilityCategoryID;
        public byte GarrFollowerTypeID;
        public uint Id;
    }

    public sealed class GarrBuildingRecord
    {
        public uint Id;
        public string AllianceName;
        public string HordeName;
        public string Description;
        public string Tooltip;
        public uint HordeGameObjectID;
        public uint AllianceGameObjectID;
        public uint IconFileDataID;
        public ushort CurrencyTypeID;
        public ushort HordeUiTextureKitID;
        public ushort AllianceUiTextureKitID;
        public ushort AllianceSceneScriptPackageID;
        public ushort HordeSceneScriptPackageID;
        public ushort GarrAbilityID;
        public ushort BonusGarrAbilityID;
        public short GoldCost;
        public byte GarrSiteID;
        public byte BuildingType;
        public byte UpgradeLevel;
        public GarrisonBuildingFlags Flags;
        public byte ShipmentCapacity;
        public byte GarrTypeID;
        public ushort BuildSeconds;
        public int CurrencyQty;
        public byte MaxAssignments;
    }

    public sealed class GarrBuildingPlotInstRecord
    {
        public Vector2 MapOffset;
        public ushort UiTextureAtlasMemberID;
        public ushort GarrSiteLevelPlotInstID;
        public byte GarrBuildingID;
        public uint Id;
    }

    public sealed class GarrClassSpecRecord
    {
        public string ClassSpec;
        public string ClassSpecMale;
        public string ClassSpecFemale;
        public ushort UiTextureAtlasMemberID;                                            // UiTextureAtlasMember.db2 ref
        public ushort GarrFollItemSetID;
        public byte FollowerClassLimit;
        public byte Flags;
        public uint Id;
    }

    public sealed class GarrFollowerRecord
    {
        public string HordeSourceText;
        public string AllianceSourceText;
        public string TitleName;
        public uint HordeCreatureID;
        public uint AllianceCreatureID;
        public uint HordeIconFileDataID;
        public uint AllianceIconFileDataID;
        public uint HordeSlottingBroadcastTextID;
        public uint AllySlottingBroadcastTextID;
        public ushort HordeGarrFollItemSetID;
        public ushort AllianceGarrFollItemSetID;
        public ushort ItemLevelWeapon;
        public ushort ItemLevelArmor;
        public ushort HordeUITextureKitID;
        public ushort AllianceUITextureKitID;
        public byte GarrFollowerTypeID;
        public byte HordeGarrFollRaceID;
        public byte AllianceGarrFollRaceID;
        public byte Quality;
        public byte HordeGarrClassSpecID;
        public byte AllianceGarrClassSpecID;
        public byte FollowerLevel;
        public byte Gender;
        public byte Flags;
        public sbyte HordeSourceTypeEnum;
        public sbyte AllianceSourceTypeEnum;
        public byte GarrTypeID;
        public byte Vitality;
        public byte ChrClassID;
        public byte HordeFlavorGarrStringID;
        public byte AllianceFlavorGarrStringID;
        public uint Id;
    }

    public sealed class GarrFollowerXAbilityRecord
    {
        public uint Id;
        public ushort GarrAbilityID;
        public byte FactionIndex;
        public uint GarrFollowerID;
    }

    public sealed class GarrPlotRecord
    {
        public uint Id;
        public string Name;
        public uint AllianceConstructObjID;
        public uint HordeConstructObjID;
        public byte UiCategoryID;
        public byte PlotType;
        public byte Flags;
        public uint[] UpgradeRequirement = new uint[2];
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
        public string Name;
        public byte GarrPlotID;
    }

    public sealed class GarrSiteLevelRecord
    {
        public uint Id;
        public Vector2 TownHallUiPos;
        public ushort MapID;
        public ushort UiTextureKitID;
        public ushort UpgradeMovieID;
        public ushort UpgradeCost;
        public ushort UpgradeGoldCost;
        public byte GarrLevel;
        public byte GarrSiteID;
        public byte MaxBuildingLevel;
    }

    public sealed class GarrSiteLevelPlotInstRecord
    {
        public uint Id;
        public Vector2 UiMarkerPos;
        public ushort GarrSiteLevelID;
        public byte GarrPlotInstanceID;
        public byte UiMarkerSize;
    }

    public sealed class GemPropertiesRecord
    {
        public uint Id;
        public SocketColor Type;
        public ushort EnchantId;
        public ushort MinItemLevel;
    }

    public sealed class GlyphBindableSpellRecord
    {
        public uint Id;
        public uint SpellID;
        public uint GlyphPropertiesID;
    }

    public sealed class GlyphPropertiesRecord
    {
        public uint Id;
        public uint SpellID;
        public ushort SpellIconID;
        public byte GlyphType;
        public byte GlyphExclusiveCategoryID;
    }

    public sealed class GlyphRequiredSpecRecord
    {
        public uint Id;
        public ushort ChrSpecializationID;
        public uint GlyphPropertiesID;
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
