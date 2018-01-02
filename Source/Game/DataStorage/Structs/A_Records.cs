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
        public LocalizedString Title;
        public LocalizedString Description;
        public AchievementFlags Flags;
        public LocalizedString Reward;
        public short MapID;
        public ushort Supercedes;
        public ushort Category;
        public ushort UiOrder;
        public ushort SharesCriteria;
        public AchievementFaction Faction;
        public byte Points;
        public byte MinimumCriteria;
        public uint Id;
        public uint IconFileDataID;
        public uint CriteriaTree;
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
        public ushort AreaGroupID;
        public ushort AreaID;
    }

    public sealed class AreaTableRecord
    {
        public uint Id;
        public AreaFlags[] Flags = new AreaFlags[2];
        public string ZoneName;
        public float AmbientMultiplier;
        public LocalizedString AreaName;
        public ushort MapId;
        public ushort ParentAreaID;
        public short AreaBit;
        public ushort AmbienceID;
        public ushort ZoneMusic;
        public ushort IntroSound;
        public ushort[] LiquidTypeID = new ushort[4];
        public ushort UWZoneMusic;
        public ushort UWAmbience;
        public ushort PvPCombastWorldStateID;
        public byte SoundProviderPref;
        public byte SoundProviderPrefUnderwater;
        public byte ExplorationLevel;
        public byte FactionGroupMask;
        public byte MountFlags;
        public byte WildBattlePetLevelMin;
        public byte WildBattlePetLevelMax;
        public byte WindSettingsID;
        public byte[] UWIntroSound = new byte[2];

        public bool IsSanctuary()
        {
            if (MapId == 609)
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
        public ushort MapID;
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
        public float[] Modifier = new float[5];
    }

    public sealed class ArtifactRecord
    {
        public uint Id;
        public LocalizedString Name;
        public uint BarConnectedColor;
        public uint BarDisconnectedColor;
        public uint TitleColor;
        public ushort ClassUiTextureKitID;
        public ushort SpecID;
        public byte ArtifactCategoryID;
        public byte Flags;
        public uint UiModelSceneID;
        public uint SpellVisualKitID;
    }

    public sealed class ArtifactAppearanceRecord
    {
        public LocalizedString Name;
        public uint SwatchColor;
        public float ModelDesaturation;
        public float ModelAlpha;
        public uint ShapeshiftDisplayID;
        public ushort ArtifactAppearanceSetID;
        public ushort Unknown;
        public byte DisplayIndex;
        public byte AppearanceModID;
        public byte Flags;
        public byte ModifiesShapeshiftFormDisplay;
        public uint Id;
        public uint PlayerConditionID;
        public uint ItemAppearanceID;
        public uint AltItemAppearanceID;
    }

    public sealed class ArtifactAppearanceSetRecord
    {
        public LocalizedString Name;
        public LocalizedString Name2;
        public ushort UiCameraID;
        public ushort AltHandUICameraID;
        public byte ArtifactID;
        public byte DisplayIndex;
        public byte AttachmentPoint;
        public byte Flags;
        public uint Id;
    }

    public sealed class ArtifactCategoryRecord
    {
        public uint Id;
        public ushort ArtifactKnowledgeCurrencyID;
        public ushort ArtifactKnowledgeMultiplierCurveID;
    }

    public sealed class ArtifactPowerRecord
    {
        public Vector2 Pos;
        public byte ArtifactID;
        public ArtifactPowerFlag Flags;
        public byte MaxRank;
        public byte ArtifactTier;
        public uint Id;
        public int RelicType;
    }

    public sealed class ArtifactPowerLinkRecord
    {
        public uint Id;
        public ushort FromArtifactPowerID;
        public ushort ToArtifactPowerID;
    }

    public sealed class ArtifactPowerPickerRecord
    {
        public uint Id;
        public uint PlayerConditionID;
    }

    public sealed class ArtifactPowerRankRecord
    {
        public uint Id;
        public uint SpellID;
        public float Value;
        public ushort ArtifactPowerID;
        public ushort Unknown;
        public byte Rank;
    }

    public sealed class ArtifactQuestXPRecord
    {
        public uint Id;
        public uint[] Exp = new uint[10];
    }

    public sealed class AuctionHouseRecord
    {
        public uint Id;
        public LocalizedString Name;
        public ushort FactionID;
        public byte DepositRate;
        public byte ConsignmentRate;
    }
}
