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
using System;

namespace Game.DataStorage
{
    public class AchievementRecord
    {
        public string Title;
        public string Description;
        public string Reward;
        public AchievementFlags Flags;
        public short InstanceID;
        public ushort Supercedes;
        public ushort Category;
        public ushort UiOrder;
        public ushort SharesCriteria;
        public AchievementFaction Faction;
        public byte Points;
        public byte MinimumCriteria;
        public uint Id;
        public uint IconFileID;
        public ushort CriteriaTree;
    }

    public sealed class AnimKitRecord
    {
        public uint Id;
        public uint OneShotDuration;
        public ushort OneShotStopAnimKitID;
        public ushort LowDefAnimKitID;
    }

    public sealed class AreaGroupMemberRecord
    {
        public uint Id;
        public ushort AreaID;
        public uint AreaGroupID;
    }

    public sealed class AreaTableRecord
    {
        public uint Id;
        public string ZoneName;
        public LocalizedString AreaName;
        public AreaFlags[] Flags = new AreaFlags[2];
        public float AmbientMultiplier;
        public ushort ContinentID;
        public ushort ParentAreaID;
        public short AreaBit;
        public ushort AmbienceID;
        public ushort ZoneMusic;
        public ushort IntroSound;
        public ushort[] LiquidTypeID = new ushort[4];
        public ushort UwZoneMusic;
        public ushort UwAmbience;
        public ushort PvpCombastWorldStateID;
        public byte SoundProviderPref;
        public byte SoundProviderPrefUnderwater;
        public byte ExplorationLevel;
        public byte FactionGroupMask;
        public byte MountFlags;
        public byte WildBattlePetLevelMin;
        public byte WildBattlePetLevelMax;
        public byte WindSettingsID;
        public byte UwIntroSound;

        public bool IsSanctuary()
        {
            if (ContinentID == 609)
                return true;

            return Flags[0].HasAnyFlag(AreaFlags.Sanctuary);
        }
    }

    public sealed class AreaTriggerRecord
    {
        public Vector3 Pos;
        public float Radius;
        public float BoxLength;
        public float BoxWidth;
        public float BoxHeight;
        public float BoxYaw;
        public ushort ContinentID;
        public ushort PhaseID;
        public ushort PhaseGroupID;
        public ushort ShapeID;
        public ushort AreaTriggerActionSetID;
        public byte PhaseUseFlags;
        public byte ShapeType;
        public byte Flags;
        public uint Id;
    }

    public sealed class ArmorLocationRecord
    {
        public uint Id;
        public float Clothmodifier;
        public float Leathermodifier;
        public float Chainmodifier;
        public float Platemodifier;
        public float Modifier;
    }

    public sealed class ArtifactRecord
    {
        public uint Id;
        public string Name;
        public uint UiBarOverlayColor;
        public uint UiBarBackgroundColor;
        public uint UiNameColor;
        public ushort UiTextureKitID;
        public ushort ChrSpecializationID;
        public byte ArtifactCategoryID;
        public byte Flags;
        public byte UiModelSceneID;
        public uint SpellVisualKitID;
    }

    public sealed class ArtifactAppearanceRecord
    {
        public string Name;
        public uint UiSwatchColor;
        public float UiModelSaturation;
        public float UiModelOpacity;
        public uint OverrideShapeshiftDisplayID;
        public ushort ArtifactAppearanceSetID;
        public ushort UiCameraID;
        public byte DisplayIndex;
        public byte ItemAppearanceModifierID;
        public byte Flags;
        public byte OverrideShapeshiftFormID;
        public uint Id;
        public ushort UnlockPlayerConditionID;
        public byte UiItemAppearanceID;
        public byte UiAltItemAppearanceID;
    }

    public sealed class ArtifactAppearanceSetRecord
    {
        public string Name;
        public string Description;
        public ushort UiCameraID;
        public ushort AltHandUICameraID;
        public byte DisplayIndex;
        public byte ForgeAttachmentOverride;
        public byte Flags;
        public uint Id;
        public uint ArtifactID;
    }

    public sealed class ArtifactCategoryRecord
    {
        public uint Id;
        public ushort XpMultCurrencyID;
        public ushort XpMultCurveID;
    }

    public sealed class ArtifactPowerRecord
    {
        public Vector2 Pos;
        public byte ArtifactID;
        public ArtifactPowerFlag Flags;
        public byte MaxPurchasableRank;
        public byte Tier;
        public uint Id;
        public byte Label;
    }

    public sealed class ArtifactPowerLinkRecord
    {
        public uint Id;
        public ushort PowerA;
        public ushort PowerB;
    }

    public sealed class ArtifactPowerPickerRecord
    {
        public uint Id;
        public ushort PlayerConditionID;
    }

    public sealed class ArtifactPowerRankRecord
    {
        public uint Id;
        public uint SpellID;
        public float AuraPointsOverride;
        public ushort ItemBonusListID;
        public byte RankIndex;
        public uint ArtifactPowerID;
    }

    public sealed class ArtifactQuestXPRecord
    {
        public uint Id;
        public uint[] Difficulty = new uint[10];
    }

    public class ArtifactTierRecord
    {
        public uint ID;
        public byte ArtifactTier;
        public byte MaxNumTraits;
        public byte MaxArtifactKnowledge;
        public byte KnowledgePlayerCondition;
        public byte MinimumEmpowerKnowledge;
    }

    public class ArtifactUnlockRecord
    {
        public uint ID;
        public ushort ItemBonusListID;
        public byte PowerRank;
        public uint PowerID;
        public ushort PlayerConditionID;
        public uint ArtifactID;
    }

    public sealed class AuctionHouseRecord
    {
        public uint Id;
        public string Name;
        public ushort FactionID;
        public byte DepositRate;
        public byte ConsignmentRate;
    }
}
