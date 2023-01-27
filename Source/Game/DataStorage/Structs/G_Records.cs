// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
	public sealed class GameObjectArtKitRecord
	{
		public int AttachModelFileID;
		public uint Id;
		public int[] TextureVariationFileID = new int[3];
	}

	public sealed class GameObjectDisplayInfoRecord
	{
		public int FileDataID;
		public float[] GeoBox = new float[6];
		public uint Id;
		public short ObjectEffectPackageID;
		public float OverrideLootEffectScale;
		public float OverrideNameScale;

		public Vector3 GeoBoxMin
		{
			get => new Vector3(GeoBox[0], GeoBox[1], GeoBox[2]);
			set
			{
				GeoBox[0] = value.X;
				GeoBox[1] = value.Y;
				GeoBox[2] = value.Z;
			}
		}

		public Vector3 GeoBoxMax
		{
			get => new Vector3(GeoBox[3], GeoBox[4], GeoBox[5]);
			set
			{
				GeoBox[3] = value.X;
				GeoBox[4] = value.Y;
				GeoBox[5] = value.Z;
			}
		}
	}

	public sealed class GameObjectsRecord
	{
		public uint DisplayID;
		public uint Id;
		public LocalizedString Name;
		public uint OwnerID;
		public int PhaseGroupID;
		public int PhaseID;
		public int PhaseUseFlags;
		public Vector3 Pos;
		public int[] PropValue = new int[8];
		public float[] Rot = new float[4];
		public float Scale;
		public GameObjectTypes TypeID;
	}

	public sealed class GarrAbilityRecord
	{
		public string Description;
		public ushort FactionChangeGarrAbilityID;
		public GarrisonAbilityFlags Flags;
		public byte GarrAbilityCategoryID;
		public sbyte GarrFollowerTypeID;
		public int IconFileDataID;
		public uint Id;
		public string Name;
	}

	public sealed class GarrBuildingRecord
	{
		public uint AllianceGameObjectID;
		public string AllianceName;
		public ushort AllianceSceneScriptPackageID;
		public ushort AllianceUiTextureKitID;
		public ushort BonusGarrAbilityID;
		public sbyte BuildingType;
		public int BuildSeconds;
		public int CurrencyQty;
		public ushort CurrencyTypeID;
		public string Description;
		public GarrisonBuildingFlags Flags;
		public ushort GarrAbilityID;
		public int GarrSiteID;
		public byte GarrTypeID;
		public ushort GoldCost;
		public uint HordeGameObjectID;
		public string HordeName;
		public ushort HordeSceneScriptPackageID;
		public ushort HordeUiTextureKitID;
		public int IconFileDataID;
		public uint Id;
		public int MaxAssignments;
		public byte ShipmentCapacity;
		public string Tooltip;
		public byte UpgradeLevel;
	}

	public sealed class GarrBuildingPlotInstRecord
	{
		public byte GarrBuildingID;
		public ushort GarrSiteLevelPlotInstID;
		public uint Id;
		public Vector2 MapOffset;
		public ushort UiTextureAtlasMemberID;
	}

	public sealed class GarrClassSpecRecord
	{
		public string ClassSpec;
		public string ClassSpecFemale;
		public string ClassSpecMale;
		public int Flags;
		public byte FollowerClassLimit;
		public ushort GarrFollItemSetID;
		public uint Id;
		public ushort UiTextureAtlasMemberID;
	}

	public sealed class GarrFollowerRecord
	{
		public int AllianceCreatureID;
		public byte AllianceFlavorGarrStringID;
		public uint AllianceGarrClassSpecID;
		public ushort AllianceGarrFollItemSetID;
		public byte AllianceGarrFollRaceID;
		public int AllianceIconFileDataID;
		public string AllianceSourceText;
		public sbyte AllianceSourceTypeEnum;
		public ushort AllianceUITextureKitID;
		public uint AllySlottingBroadcastTextID;
		public int AutoCombatantID;
		public byte ChrClassID;
		public int CovenantID;
		public int Flags;
		public byte FollowerLevel;
		public sbyte GarrFollowerTypeID;
		public byte GarrTypeID;
		public byte Gender;
		public int HordeCreatureID;
		public byte HordeFlavorGarrStringID;
		public uint HordeGarrClassSpecID;
		public ushort HordeGarrFollItemSetID;
		public byte HordeGarrFollRaceID;
		public int HordeIconFileDataID;
		public uint HordeSlottingBroadcastTextID;
		public string HordeSourceText;
		public sbyte HordeSourceTypeEnum;
		public ushort HordeUITextureKitID;
		public uint Id;
		public ushort ItemLevelArmor;
		public ushort ItemLevelWeapon;
		public sbyte Quality;
		public string TitleName;
		public byte Vitality;
	}

	public sealed class GarrFollowerXAbilityRecord
	{
		public byte FactionIndex;
		public ushort GarrAbilityID;
		public uint GarrFollowerID;
		public uint Id;
		public byte OrderIndex;
	}

	public sealed class GarrMissionRecord
	{
		public uint AreaID;
		public int AutoCombatantEnvCasterID;
		public float AutoMissionScalar;
		public int AutoMissionScalarCurveID;
		public byte BaseCompletionChance;
		public uint BaseFollowerXP;
		public LocalizedString Description;
		public uint EnvGarrMechanicID;
		public int EnvGarrMechanicTypeID;
		public int Flags;
		public byte FollowerDeathChance;
		public sbyte GarrFollowerTypeID;
		public int GarrMissionSetID;
		public byte GarrMissionTypeID;
		public byte GarrTypeID;
		public uint Id;
		public LocalizedString Location;
		public Vector2 MapPos;
		public byte MaxFollowers;
		public uint MissionCost;
		public ushort MissionCostCurrencyTypesID;
		public int MissionDuration;
		public LocalizedString Name;
		public uint OfferDuration;
		public byte OfferedGarrMissionTextureID;
		public uint OvermaxRewardPackID;
		public uint PlayerConditionID;
		public ushort TargetItemLevel;
		public sbyte TargetLevel;
		public int TravelDuration;
		public ushort UiTextureKitID;
		public Vector2 WorldPos;
	}

	public sealed class GarrPlotRecord
	{
		public uint AllianceConstructObjID;
		public byte Flags;
		public uint HordeConstructObjID;
		public uint Id;
		public string Name;
		public byte PlotType;
		public byte UiCategoryID;
		public uint[] UpgradeRequirement = new uint[2];
	}

	public sealed class GarrPlotBuildingRecord
	{
		public byte GarrBuildingID;
		public byte GarrPlotID;
		public uint Id;
	}

	public sealed class GarrPlotInstanceRecord
	{
		public byte GarrPlotID;
		public uint Id;
		public string Name;
	}

	public sealed class GarrSiteLevelRecord
	{
		public byte GarrLevel;
		public uint GarrSiteID;
		public uint Id;
		public ushort MapID;
		public byte MaxBuildingLevel;
		public Vector2 TownHallUiPos;
		public ushort UiTextureKitID;
		public ushort UpgradeCost;
		public ushort UpgradeGoldCost;
		public ushort UpgradeMovieID;
	}

	public sealed class GarrSiteLevelPlotInstRecord
	{
		public byte GarrPlotInstanceID;
		public ushort GarrSiteLevelID;
		public uint Id;
		public Vector2 UiMarkerPos;
		public byte UiMarkerSize;
	}

	public sealed class GarrTalentTreeRecord
	{
		public int ClassID;
		public int CurrencyID;
		public sbyte FeatureSubtypeIndex;
		public byte FeatureTypeIndex;
		public int Flags;
		public int GarrTalentTreeType;
		public byte GarrTypeID;
		public uint Id;
		public sbyte MaxTiers;
		public string Name;
		public int PlayerConditionID;
		public sbyte UiOrder;
		public ushort UiTextureKitID;
	}

	public sealed class GemPropertiesRecord
	{
		public ushort EnchantId;
		public uint Id;
		public SocketColor Type;
	}

	public sealed class GlobalCurveRecord
	{
		public uint CurveID;
		public uint Id;
		public GlobalCurve Type;
	}

	public sealed class GlyphBindableSpellRecord
	{
		public uint GlyphPropertiesID;
		public uint Id;
		public int SpellID;
	}

	public sealed class GlyphPropertiesRecord
	{
		public byte GlyphExclusiveCategoryID;
		public byte GlyphType;
		public uint Id;
		public uint SpellIconID;
		public uint SpellID;
	}

	public sealed class GlyphRequiredSpecRecord
	{
		public ushort ChrSpecializationID;
		public uint GlyphPropertiesID;
		public uint Id;
	}

	public sealed class GossipNPCOptionRecord
	{
		public int CharShipmentID;
		public int CovenantID;
		public int GarrFollowerTypeID;
		public int GarrTalentTreeID;
		public int GossipNpcOption;
		public int GossipOptionID;
		public uint Id;
		public int LFGDungeonsID;
		public int ProfessionID;
		public int TrainerID;
		public int TraitTreeID;
		public int UiItemInteractionID;
		public int UiMapID;
		public int Unknown_1000_8;
		public int Unknown_1000_9;
		public int Unknown_1002_14;
	}

	public sealed class GuildColorBackgroundRecord
	{
		public byte Blue;
		public byte Green;
		public uint Id;
		public byte Red;
	}

	public sealed class GuildColorBorderRecord
	{
		public byte Blue;
		public byte Green;
		public uint Id;
		public byte Red;
	}

	public sealed class GuildColorEmblemRecord
	{
		public byte Blue;
		public byte Green;
		public uint Id;
		public byte Red;
	}

	public sealed class GuildPerkSpellsRecord
	{
		public uint Id;
		public uint SpellID;
	}
}