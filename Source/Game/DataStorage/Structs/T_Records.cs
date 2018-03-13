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
    public sealed class TactKeyRecord
    {
        public uint Id;
        public byte[] Key = new byte[16];
    }

    public sealed class TalentRecord
    {
        public uint Id;
        public string Description;
        public uint SpellID;
        public uint OverridesSpellID;
        public ushort SpecID;
        public byte TierID;
        public byte ColumnIndex;
        public byte Flags;
        public byte[] CategoryMask = new byte[2];
        public byte ClassID;
    }

    public sealed class TaxiNodesRecord
    {
        public uint Id;
        public LocalizedString Name;
        public Vector3 Pos;
        public uint[] MountCreatureID = new uint[2];
        public Vector2 MapOffset;
        public float Facing;
        public Vector2 FlightMapOffset;
        public ushort ContinentID;
        public ushort ConditionID;
        public ushort CharacterBitNumber;
        public TaxiNodeFlags Flags;
        public int UiTextureKitID;
        public uint SpecialIconConditionID;
    }

    public sealed class TaxiPathRecord
    {
        public ushort FromTaxiNode;
        public ushort ToTaxiNode;
        public uint Id;
        public uint Cost;
    }

    public sealed class TaxiPathNodeRecord
    {
        public Vector3 Loc;
        public ushort PathID;
        public ushort ContinentID;
        public byte NodeIndex;
        public uint Id;
        public TaxiPathNodeFlags Flags;
        public uint Delay;
        public ushort ArrivalEventID;
        public ushort DepartureEventID;
    }

    public sealed class TotemCategoryRecord
    {
        public uint Id;
        public LocalizedString Name;
        public uint TotemCategoryMask;
        public byte TotemCategoryType;
    }

    public sealed class ToyRecord
    {
        public LocalizedString SourceText;
        public uint ItemID;
        public byte Flags;
        public byte SourceTypeEnum;
        public uint Id;
    }

    public sealed class TransmogHolidayRecord
    {
        public uint Id;
        public int RequiredTransmogHoliday;
    }

    public sealed class TransmogSetRecord
    {
        public LocalizedString Name;
        public ushort ParentTransmogSetID;
        public ushort UIOrder;
        public byte ExpansionID;
        public uint Id;
        public byte Flags;
        public int TrackingQuestID;
        public int ClassMask;
        public int ItemNameDescriptionID;
        public byte TransmogSetGroupID;
    }

    public sealed class TransmogSetGroupRecord
    {
        public LocalizedString Name;
        public uint Id;
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
        public uint TimeIndex;
        public Vector3 Pos;
        public byte SequenceID;
        public uint TransportID;
    }

    public sealed class TransportRotationRecord
    {
        public uint Id;
        public uint TimeIndex;
        public float[] Rot = new float[4];
        public uint GameObjectsID;
    }
}
