// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Numerics;
using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class TactKeyRecord
    {
        public uint Id;
        public byte[] Key = new byte[16];
    }

    public sealed class TalentRecord
    {
        public byte[] CategoryMask = new byte[2];
        public byte ClassID;
        public byte ColumnIndex;
        public string Description;
        public byte Flags;
        public uint Id;
        public uint OverridesSpellID;
        public ushort SpecID;
        public uint SpellID;
        public byte TierID;
    }

    public sealed class TaxiNodesRecord
    {
        public ushort CharacterBitNumber;
        public uint ConditionID;
        public ushort ContinentID;
        public float Facing;
        public TaxiNodeFlags Flags;
        public Vector2 FlightMapOffset;
        public uint Id;
        public Vector2 MapOffset;
        public int MinimapAtlasMemberID;
        public uint[] MountCreatureID = new uint[2];
        public LocalizedString Name;
        public Vector3 Pos;
        public uint SpecialIconConditionID;
        public int UiTextureKitID;
        public uint VisibilityConditionID;
    }

    public sealed class TaxiPathRecord
    {
        public uint Cost;
        public ushort FromTaxiNode;
        public uint Id;
        public ushort ToTaxiNode;
    }

    public sealed class TaxiPathNodeRecord
    {
        public uint ArrivalEventID;
        public ushort ContinentID;
        public uint Delay;
        public uint DepartureEventID;
        public TaxiPathNodeFlags Flags;
        public uint Id;
        public Vector3 Loc;
        public int NodeIndex;
        public ushort PathID;
    }

    public sealed class TotemCategoryRecord
    {
        public uint Id;
        public string Name;
        public int TotemCategoryMask;
        public byte TotemCategoryType;
    }

    public sealed class ToyRecord
    {
        public byte Flags;
        public uint Id;
        public uint ItemID;
        public string SourceText;
        public sbyte SourceTypeEnum;
    }

    public sealed class TransmogHolidayRecord
    {
        public uint Id;
        public int RequiredTransmogHoliday;
    }

    public sealed class TraitCondRecord
    {
        public uint AchievementID;
        public int CondType;
        public int Flags;
        public int FreeSharedStringID;
        public int GrantedRanks;
        public uint Id;
        public uint QuestID;
        public int RequiredLevel;
        public int SpecSetID;
        public int SpendMoreSharedStringID;
        public int SpentAmountRequired;
        public int TraitCurrencyID;
        public int TraitNodeGroupID;
        public int TraitNodeID;
        public int TraitTreeID;

        public TraitConditionType GetCondType()
        {
            return (TraitConditionType)CondType;
        }
    }

    public sealed class TraitCostRecord
    {
        public int Amount;
        public uint Id;
        public string InternalName;
        public int TraitCurrencyID;
    }

    public sealed class TraitCurrencyRecord
    {
        public int CurrencyTypesID;
        public int Flags;
        public int Icon;
        public uint Id;
        public int Type;

        public TraitCurrencyType GetCurrencyType()
        {
            return (TraitCurrencyType)Type;
        }
    }

    public sealed class TraitCurrencySourceRecord
    {
        public uint AchievementID;
        public int Amount;
        public uint Id;
        public int OrderIndex;
        public uint PlayerLevel;
        public uint QuestID;
        public LocalizedString Requirement;
        public int TraitCurrencyID;
        public int TraitNodeEntryID;
    }

    public sealed class TraitDefinitionRecord
    {
        public uint Id;
        public LocalizedString OverrideDescription;
        public int OverrideIcon;
        public LocalizedString OverrideName;
        public uint OverridesSpellID;
        public LocalizedString OverrideSubtext;
        public uint SpellID;
        public uint VisibleSpellID;
    }

    public sealed class TraitDefinitionEffectPointsRecord
    {
        public int CurveID;
        public int EffectIndex;
        public uint Id;
        public int OperationType;
        public int TraitDefinitionID;

        public TraitPointsOperationType GetOperationType()
        {
            return (TraitPointsOperationType)OperationType;
        }
    }

    public sealed class TraitEdgeRecord
    {
        public uint Id;
        public int LeftTraitNodeID;
        public int RightTraitNodeID;
        public int Type;
        public int VisualStyle;
    }

    public sealed class TraitNodeRecord
    {
        public int Flags;
        public uint Id;
        public int PosX;
        public int PosY;
        public int TraitTreeID;
        public sbyte Type;

        public TraitNodeType GetNodeType()
        {
            return (TraitNodeType)Type;
        }
    }

    public sealed class TraitNodeEntryRecord
    {
        public uint Id;
        public int MaxRanks;
        public byte NodeRecordType;
        public int TraitDefinitionID;
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
        public int TraitCostID;
        public int TraitNodeEntryID;
    }

    public sealed class TraitNodeGroupRecord
    {
        public int Flags;
        public uint Id;
        public int TraitTreeID;
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
        public int TraitCostID;
        public int TraitNodeGroupID;
    }

    public sealed class TraitNodeGroupXTraitNodeRecord
    {
        public uint Id;
        public int Index;
        public int TraitNodeGroupID;
        public int TraitNodeID;
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
        public int TraitCostID;
        public uint TraitNodeID;
    }

    public sealed class TraitNodeXTraitNodeEntryRecord
    {
        public uint Id;
        public int Index;
        public int TraitNodeEntryID;
        public int TraitNodeID;
    }

    public sealed class TraitTreeRecord
    {
        public int FirstTraitNodeID;
        public int Flags;
        public uint Id;
        public int PlayerConditionID;
        public int TraitSystemID;
        public int Unused1000_1;
        public float Unused1000_2;
        public float Unused1000_3;

        public TraitTreeFlag GetFlags()
        {
            return (TraitTreeFlag)Flags;
        }
    }

    public sealed class TraitTreeLoadoutRecord
    {
        public int ChrSpecializationID;
        public uint Id;
        public int TraitTreeID;
    }

    public sealed class TraitTreeLoadoutEntryRecord
    {
        public uint Id;
        public int NumPoints;
        public int OrderIndex;
        public int SelectedTraitNodeEntryID;
        public int SelectedTraitNodeID;
        public int TraitTreeLoadoutID;
    }

    public sealed class TraitTreeXTraitCostRecord
    {
        public uint Id;
        public int TraitCostID;
        public uint TraitTreeID;
    }

    public sealed class TraitTreeXTraitCurrencyRecord
    {
        public uint Id;
        public int Index;
        public int TraitCurrencyID;
        public int TraitTreeID;
    }

    public sealed class TransmogIllusionRecord
    {
        public int Flags;
        public uint Id;
        public int SpellItemEnchantmentID;
        public int TransmogCost;
        public int UnlockConditionID;

        public TransmogIllusionFlags GetFlags()
        {
            return (TransmogIllusionFlags)Flags;
        }
    }

    public sealed class TransmogSetRecord
    {
        public int ClassMask;
        public byte ExpansionID;
        public int Flags;
        public uint Id;
        public int ItemNameDescriptionID;
        public string Name;
        public ushort ParentTransmogSetID;
        public int PatchID;
        public uint PlayerConditionID;
        public uint TrackingQuestID;
        public uint TransmogSetGroupID;
        public short UiOrder;
        public byte Unknown810;
    }

    public sealed class TransmogSetGroupRecord
    {
        public uint Id;
        public string Name;
    }

    public sealed class TransmogSetItemRecord
    {
        public int Flags;
        public uint Id;
        public uint ItemModifiedAppearanceID;
        public uint TransmogSetID;
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
        public uint GameObjectsID;
        public uint Id;
        public float[] Rot = new float[4];
        public uint TimeIndex;
    }
}