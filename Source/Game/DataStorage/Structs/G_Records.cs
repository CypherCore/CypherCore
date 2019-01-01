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
    public sealed class GameObjectDisplayInfoRecord
    {
        public uint Id;
        public float[] GeoBox = new float[6];
        public int FileDataID;
        public short ObjectEffectPackageID;
        public float OverrideLootEffectScale;
        public float OverrideNameScale;

        public Vector3 GeoBoxMin
        {
            get { return new Vector3(GeoBox[0], GeoBox[1], GeoBox[2]); }
            set { GeoBox[0] = value.X; GeoBox[1] = value.Y; GeoBox[2] = value.Z; }
        }
        public Vector3 GeoBoxMax
        {
            get { return new Vector3(GeoBox[3], GeoBox[4], GeoBox[5]); }
            set { GeoBox[3] = value.X; GeoBox[4] = value.Y; GeoBox[5] = value.Z; }
        }
    }

    public sealed class GameObjectsRecord
    {
        public LocalizedString Name;
        public Vector3 Pos;
        public float[] Rot = new float[4];
        public uint Id;
        public ushort OwnerID;
        public ushort DisplayID;
        public float Scale;
        public GameObjectTypes TypeID;
        public byte PhaseUseFlags;
        public ushort PhaseID;
        public ushort PhaseGroupID;
        public int[] PropValue = new int[8];
    }

    public sealed class GarrAbilityRecord
    {
        public string Name;
        public string Description;
        public uint Id;
        public byte GarrAbilityCategoryID;
        public byte GarrFollowerTypeID;
        public int IconFileDataID;
        public ushort FactionChangeGarrAbilityID;
        public GarrisonAbilityFlags Flags;
    }

    public sealed class GarrBuildingRecord
    {
        public uint Id;
        public string HordeName;
        public string AllianceName;
        public string Description;
        public string Tooltip;
        public byte GarrTypeID;
        public byte BuildingType;
        public uint HordeGameObjectID;
        public uint AllianceGameObjectID;
        public byte GarrSiteID;
        public byte UpgradeLevel;
        public int BuildSeconds;
        public ushort CurrencyTypeID;
        public int CurrencyQty;
        public ushort HordeUiTextureKitID;
        public ushort AllianceUiTextureKitID;
        public int IconFileDataID;
        public ushort AllianceSceneScriptPackageID;
        public ushort HordeSceneScriptPackageID;
        public int MaxAssignments;
        public byte ShipmentCapacity;
        public ushort GarrAbilityID;
        public ushort BonusGarrAbilityID;
        public ushort GoldCost;
        public GarrisonBuildingFlags Flags;
    }

    public sealed class GarrBuildingPlotInstRecord
    {
        public Vector2 MapOffset;
        public uint Id;
        public byte GarrBuildingID;
        public ushort GarrSiteLevelPlotInstID;
        public ushort UiTextureAtlasMemberID;
    }

    public sealed class GarrClassSpecRecord
    {
        public string ClassSpec;
        public string ClassSpecMale;
        public string ClassSpecFemale;
        public uint Id;
        public ushort UiTextureAtlasMemberID;
        public ushort GarrFollItemSetID;
        public byte FollowerClassLimit;
        public byte Flags;
    }

    public sealed class GarrFollowerRecord
    {
        public string HordeSourceText;
        public string AllianceSourceText;
        public string TitleName;
        public uint Id;
        public byte GarrTypeID;
        public byte GarrFollowerTypeID;
        public int HordeCreatureID;
        public int AllianceCreatureID;
        public byte HordeGarrFollRaceID;
        public byte AllianceGarrFollRaceID;
        public byte HordeGarrClassSpecID;
        public byte AllianceGarrClassSpecID;
        public byte Quality;
        public byte FollowerLevel;
        public ushort ItemLevelWeapon;
        public ushort ItemLevelArmor;
        public sbyte HordeSourceTypeEnum;
        public sbyte AllianceSourceTypeEnum;
        public int HordeIconFileDataID;
        public int AllianceIconFileDataID;
        public ushort HordeGarrFollItemSetID;
        public ushort AllianceGarrFollItemSetID;
        public ushort HordeUITextureKitID;
        public ushort AllianceUITextureKitID;
        public byte Vitality;
        public byte HordeFlavorGarrStringID;
        public byte AllianceFlavorGarrStringID;
        public uint HordeSlottingBroadcastTextID;
        public uint AllySlottingBroadcastTextID;
        public byte ChrClassID;
        public byte Flags;
        public byte Gender;
    }

    public sealed class GarrFollowerXAbilityRecord
    {
        public uint Id;
        public byte OrderIndex;
        public byte FactionIndex;
        public ushort GarrAbilityID;
        public uint GarrFollowerID;
    }

    public sealed class GarrPlotRecord
    {
        public uint Id;
        public string Name;
        public byte PlotType;
        public uint HordeConstructObjID;
        public uint AllianceConstructObjID;
        public byte Flags;
        public byte UiCategoryID;
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
        public uint GarrSiteID;
        public byte GarrLevel;
        public ushort MapID;
        public ushort UpgradeMovieID;
        public ushort UiTextureKitID;
        public byte MaxBuildingLevel;
        public ushort UpgradeCost;
        public ushort UpgradeGoldCost;
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
        public ushort EnchantId;
        public SocketColor Type;
        public ushort MinItemLevel;
    }

    public sealed class GlyphBindableSpellRecord
    {
        public uint Id;
        public int SpellID;
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
        public byte Blue;
        public byte Green;
    }

    public sealed class GuildColorBorderRecord
    {
        public uint Id;
        public byte Red;
        public byte Blue;
        public byte Green;
    }

    public sealed class GuildColorEmblemRecord
    {
        public uint Id;
        public byte Red;
        public byte Blue;
        public byte Green;
    }

    public sealed class GuildPerkSpellsRecord
    {
        public uint Id;
        public uint SpellID;
    }
}
