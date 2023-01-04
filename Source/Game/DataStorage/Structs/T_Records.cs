/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
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
using System.Numerics;

namespace Game.DataStorage
{
    public sealed class TactKeyRecord
    {
        public uint Id;
        public byte[] Key = new byte[16];
    }

    public sealed class TalentRecord
    {
        public uint Id;
        public string Description;
        public byte TierID;
        public byte Flags;
        public byte ColumnIndex;
        public byte ClassID;
        public ushort SpecID;
        public uint SpellID;
        public uint OverridesSpellID;
        public byte[] CategoryMask = new byte[2];
    }

    public sealed class TaxiNodesRecord
    {
        public LocalizedString Name;
        public Vector3 Pos;
        public Vector2 MapOffset;
        public Vector2 FlightMapOffset;
        public uint Id;
        public ushort ContinentID;
        public uint ConditionID;
        public ushort CharacterBitNumber;
        public TaxiNodeFlags Flags;
        public int UiTextureKitID;
        public int MinimapAtlasMemberID;
        public float Facing;
        public uint SpecialIconConditionID;
        public uint VisibilityConditionID;
        public uint[] MountCreatureID = new uint[2];
    }

    public sealed class TaxiPathRecord
    {
        public uint Id;
        public ushort FromTaxiNode;
        public ushort ToTaxiNode;
        public uint Cost;
    }

    public sealed class TaxiPathNodeRecord
    {
        public Vector3 Loc;
        public uint Id;
        public ushort PathID;
        public int NodeIndex;
        public ushort ContinentID;
        public TaxiPathNodeFlags Flags;
        public uint Delay;
        public uint ArrivalEventID;
        public uint DepartureEventID;
    }

    public sealed class TotemCategoryRecord
    {
        public uint Id;
        public string Name;
        public byte TotemCategoryType;
        public int TotemCategoryMask;
    }

    public sealed class ToyRecord
    {
        public string SourceText;
        public uint Id;
        public uint ItemID;
        public byte Flags;
        public sbyte SourceTypeEnum;
    }

    public sealed class TransmogHolidayRecord
    {
        public uint Id;
        public int RequiredTransmogHoliday;
    }

    public sealed class TraitCondRecord
    {
        public uint Id;
        public int CondType;
        public int TraitTreeID;
        public int GrantedRanks;
        public uint QuestID;
        public uint AchievementID;
        public int SpecSetID;
        public int TraitNodeGroupID;
        public int TraitNodeID;
        public int TraitCurrencyID;
        public int SpentAmountRequired;
        public int Flags;
        public int RequiredLevel;
        public int FreeSharedStringID;
        public int SpendMoreSharedStringID;

        public TraitConditionType GetCondType() { return (TraitConditionType)CondType; }
    }

    public sealed class TraitCostRecord
    {
        public string InternalName;
        public uint Id;
        public int Amount;
        public int TraitCurrencyID;
    }

    public sealed class TraitCurrencyRecord
    {
        public uint Id;
        public int Type;
        public int CurrencyTypesID;
        public int Flags;
        public int Icon;

        public TraitCurrencyType GetCurrencyType() { return (TraitCurrencyType)Type; }
    }

    public sealed class TraitCurrencySourceRecord
    {
        public LocalizedString Requirement;
        public uint Id;
        public int TraitCurrencyID;
        public int Amount;
        public uint QuestID;
        public uint AchievementID;
        public uint PlayerLevel;
        public int TraitNodeEntryID;
        public int OrderIndex;
    }

    public sealed class TraitDefinitionRecord
    {
        public LocalizedString OverrideName;
        public LocalizedString OverrideSubtext;
        public LocalizedString OverrideDescription;
        public uint Id;
        public uint SpellID;
        public int OverrideIcon;
        public uint OverridesSpellID;
        public uint VisibleSpellID;
    }

    public sealed class TraitDefinitionEffectPointsRecord
    {
        public uint Id;
        public int TraitDefinitionID;
        public int EffectIndex;
        public int OperationType;
        public int CurveID;

        public TraitPointsOperationType GetOperationType() { return (TraitPointsOperationType)OperationType; }
    }

    public sealed class TraitEdgeRecord
    {
        public uint Id;
        public int VisualStyle;
        public int LeftTraitNodeID;
        public int RightTraitNodeID;
        public int Type;
    }

    public sealed class TraitNodeRecord
    {
        public uint Id;
        public int TraitTreeID;
        public int PosX;
        public int PosY;
        public sbyte Type;
        public int Flags;

        public TraitNodeType GetNodeType() { return (TraitNodeType)Type; }
    }

    public sealed class TraitNodeEntryRecord
    {
        public uint Id;
        public int TraitDefinitionID;
        public int MaxRanks;
        public byte NodeRecordType;
    }

    public sealed class TraitNodeEntryXTraitCondRecord
    {
        public uint Id;
        public int TraitCondID;
        public uint TraitNodeEntryID;
    }

    public sealed class TraitNodeEntryXTraitCostRecord
    {
        public uint Id;
        public int TraitNodeEntryID;
        public int TraitCostID;
    }

    public sealed class TraitNodeGroupRecord
    {
        public uint Id;
        public int TraitTreeID;
        public int Flags;
    }

    public sealed class TraitNodeGroupXTraitCondRecord
    {
        public uint Id;
        public int TraitCondID;
        public int TraitNodeGroupID;
    }

    public sealed class TraitNodeGroupXTraitCostRecord
    {
        public uint Id;
        public int TraitNodeGroupID;
        public int TraitCostID;
    }

    public sealed class TraitNodeGroupXTraitNodeRecord
    {
        public uint Id;
        public int TraitNodeGroupID;
        public int TraitNodeID;
        public int Index;
    }

    public sealed class TraitNodeXTraitCondRecord
    {
        public uint Id;
        public int TraitCondID;
        public int TraitNodeID;
    }

    public sealed class TraitNodeXTraitCostRecord
    {
        public uint Id;
        public uint TraitNodeID;
        public int TraitCostID;
    }

    public sealed class TraitNodeXTraitNodeEntryRecord
    {
        public uint Id;
        public int TraitNodeID;
        public int TraitNodeEntryID;
        public int Index;
    }

    public sealed class TraitTreeRecord
    {
        public uint Id;
        public int TraitSystemID;
        public int Unused1000_1;
        public int FirstTraitNodeID;
        public int PlayerConditionID;
        public int Flags;
        public float Unused1000_2;
        public float Unused1000_3;

        public TraitTreeFlag GetFlags() { return (TraitTreeFlag)Flags; }
    }

    public sealed class TraitTreeLoadoutRecord
    {
        public uint Id;
        public int TraitTreeID;
        public int ChrSpecializationID;
    }

    public sealed class TraitTreeLoadoutEntryRecord
    {
        public uint Id;
        public int TraitTreeLoadoutID;
        public int SelectedTraitNodeID;
        public int SelectedTraitNodeEntryID;
        public int NumPoints;
        public int OrderIndex;
    }

    public sealed class TraitTreeXTraitCostRecord
    {
        public uint Id;
        public uint TraitTreeID;
        public int TraitCostID;
    }

    public sealed class TraitTreeXTraitCurrencyRecord
    {
        public uint Id;
        public int Index;
        public int TraitTreeID;
        public int TraitCurrencyID;
    }

    public sealed class TransmogIllusionRecord
    {
        public uint Id;
        public int UnlockConditionID;
        public int TransmogCost;
        public int SpellItemEnchantmentID;
        public int Flags;

        public TransmogIllusionFlags GetFlags() { return (TransmogIllusionFlags)Flags; }
    }

    public sealed class TransmogSetRecord
    {
        public string Name;
        public uint Id;
        public int ClassMask;
        public uint TrackingQuestID;
        public int Flags;
        public uint TransmogSetGroupID;
        public int ItemNameDescriptionID;
        public ushort ParentTransmogSetID;
        public byte Unknown810;
        public byte ExpansionID;
        public int PatchID;
        public short UiOrder;
        public uint PlayerConditionID;
    }

    public sealed class TransmogSetGroupRecord
    {
        public uint Id;
        public string Name;
    }

    public sealed class TransmogSetItemRecord
    {
        public uint Id;
        public uint TransmogSetID;
        public uint ItemModifiedAppearanceID;
        public int Flags;
    }

    public sealed class TransportAnimationRecord
    {
        public uint Id;
        public Vector3 Pos;
        public byte SequenceID;
        public uint TimeIndex;
        public uint TransportID;
    }

    public sealed class TransportRotationRecord
    {
        public uint Id;
        public float[] Rot = new float[4];
        public uint TimeIndex;
        public uint GameObjectsID;
    }
}