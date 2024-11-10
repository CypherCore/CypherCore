// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using System;
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
        public int Flags;
        public int UiTextureKitID;
        public int MinimapAtlasMemberID;
        public float Facing;
        public uint SpecialIconConditionID;
        public uint VisibilityConditionID;
        public uint[] MountCreatureID = new uint[2];

        public bool HasFlag(TaxiNodeFlags flag) { return (Flags & (int)flag) != 0; }

        public bool IsPartOfTaxiNetwork()
        {
            return HasFlag(TaxiNodeFlags.ShowOnAllianceMap | TaxiNodeFlags.ShowOnHordeMap)
                // manually whitelisted nodes
                || Id == 1985   // [Hidden] Argus Ground Points Hub (Ground TP out to here, TP to Vindicaar from here)
                || Id == 1986   // [Hidden] Argus Vindicaar Ground Hub (Vindicaar TP out to here, TP to ground from here)
                || Id == 1987   // [Hidden] Argus Vindicaar No Load Hub (Vindicaar No Load transition goes through here)
                || Id == 2627   // [Hidden] 9.0 Bastion Ground Points Hub (Ground TP out to here, TP to Sanctum from here)
                || Id == 2628   // [Hidden] 9.0 Bastion Ground Hub (Sanctum TP out to here, TP to ground from here)
                || Id == 2732   // [HIDDEN] 9.2 Resonant Peaks - Teleport Network - Hidden Hub (Connects all Nodes to each other without unique paths)
                || Id == 2835   // [Hidden] 10.0 Travel Network - Destination Input
                || Id == 2843;   // [Hidden] 10.0 Travel Network - Destination Output
        }
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
        public int Flags;
        public uint Delay;
        public uint ArrivalEventID;
        public uint DepartureEventID;

        public bool HasFlag(TaxiPathNodeFlags taxiPathNodeFlags) { return (Flags & (int)taxiPathNodeFlags) != 0; }
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
        public uint TraitTreeID;
        public int GrantedRanks;
        public uint QuestID;
        public uint AchievementID;
        public int SpecSetID;
        public int TraitNodeGroupID;
        public int TraitNodeID;
        public int TraitNodeEntryID;
        public int TraitCurrencyID;
        public int SpentAmountRequired;
        public int Flags;
        public int RequiredLevel;
        public int FreeSharedStringID;
        public int SpendMoreSharedStringID;
        public int TraitCondAccountElementID;

        public TraitConditionType GetCondType() { return (TraitConditionType)CondType; }
        public bool HasFlag(TraitCondFlags flag) => Flags.HasAnyFlag((int)flag);
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
        public uint TraitCurrencyID;
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
        public uint TraitDefinitionID;
        public int EffectIndex;
        public int OperationType;
        public int CurveID;

        public TraitPointsOperationType GetOperationType() { return (TraitPointsOperationType)OperationType; }
    }

    public sealed class TraitEdgeRecord
    {
        public uint Id;
        public int VisualStyle;
        public uint LeftTraitNodeID;
        public int RightTraitNodeID;
        public int Type;
    }

    public sealed class TraitNodeRecord
    {
        public uint Id;
        public uint TraitTreeID;
        public int PosX;
        public int PosY;
        public byte Type;
        public int Flags;
        public int TraitSubTreeID;

        public TraitNodeType GetNodeType() { return (TraitNodeType)Type; }
    }

    public sealed class TraitNodeEntryRecord
    {
        public uint Id;
        public int TraitDefinitionID;
        public int MaxRanks;
        public byte NodeEntryType;
        public int TraitSubTreeID;

        public TraitNodeEntryType GetNodeEntryType() { return (TraitNodeEntryType)NodeEntryType; }
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
        public uint TraitNodeEntryID;
        public int TraitCostID;
    }

    public sealed class TraitNodeGroupRecord
    {
        public uint Id;
        public uint TraitTreeID;
        public int Flags;
    }

    public sealed class TraitNodeGroupXTraitCondRecord
    {
        public uint Id;
        public int TraitCondID;
        public uint TraitNodeGroupID;
    }

    public sealed class TraitNodeGroupXTraitCostRecord
    {
        public uint Id;
        public uint TraitNodeGroupID;
        public int TraitCostID;
    }

    public sealed class TraitNodeGroupXTraitNodeRecord
    {
        public uint Id;
        public uint TraitNodeGroupID;
        public int TraitNodeID;
        public int Index;
    }

    public sealed class TraitNodeXTraitCondRecord
    {
        public uint Id;
        public int TraitCondID;
        public uint TraitNodeID;
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
        public uint TraitNodeID;
        public int TraitNodeEntryID;
        public int Index;
    }

    public sealed class TraitSubTreeRecord
    {
        public LocalizedString Name;
        public LocalizedString Description;
        public uint ID;
        public int UiTextureAtlasElementID;
        public uint TraitTreeID;             // Parent tree
    }

    public sealed class TraitTreeRecord
    {
        public uint Id;
        public uint TraitSystemID;
        public int Unused1000_1;
        public int FirstTraitNodeID;
        public int PlayerConditionID;
        public int Flags;
        public float Unused1000_2;
        public float Unused1000_3;

        public bool HasFlag(TraitTreeFlag traitTreeFlag) { return (Flags & (int)traitTreeFlag) != 0; }
    }

    public sealed class TraitTreeLoadoutRecord
    {
        public uint Id;
        public uint TraitTreeID;
        public int ChrSpecializationID;
    }

    public sealed class TraitTreeLoadoutEntryRecord
    {
        public uint Id;
        public uint TraitTreeLoadoutID;
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
        public uint TraitTreeID;
        public int TraitCurrencyID;
    }

    public sealed class TransmogIllusionRecord
    {
        public uint Id;
        public int UnlockConditionID;
        public int TransmogCost;
        public int SpellItemEnchantmentID;
        public int Flags;

        public bool HasFlag(TransmogIllusionFlags transmogIllusionFlags) { return (Flags & (int)transmogIllusionFlags) != 0; }
    }

    public sealed class TransmogSetRecord
    {
        public string Name;
        public uint Id;
        public int ClassMask;
        public int TrackingQuestID;
        public int Flags;
        public uint TransmogSetGroupID;
        public int ItemNameDescriptionID;
        public uint ParentTransmogSetID;
        public int Unknown810;
        public int ExpansionID;
        public int PatchID;
        public int UiOrder;
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