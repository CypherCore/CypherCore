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
        public ushort ConditionID;
        public ushort CharacterBitNumber;
        public TaxiNodeFlags Flags;
        public int UiTextureKitID;
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
        public uint NodeIndex;
        public ushort ContinentID;
        public TaxiPathNodeFlags Flags;
        public uint Delay;
        public ushort ArrivalEventID;
        public ushort DepartureEventID;
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
        public byte ExpansionID;
        public short UiOrder;
    }

    public sealed class TransmogSetGroupRecord
    {
        public string Name;
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
