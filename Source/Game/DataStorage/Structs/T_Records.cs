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
        public uint SpellID;
        public uint OverridesSpellID;
        public LocalizedString Description;
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
        public Vector3 Pos;
        public LocalizedString Name;
        public uint[] MountCreatureID = new uint[2];
        public Vector2 MapOffset;
        public float Unk730;
        public Vector2 FlightMapOffset;
        public ushort MapID;
        public ushort ConditionID;
        public ushort LearnableIndex;
        public TaxiNodeFlags Flags;
        public int UiTextureKitPrefixID;
        public uint SpecialAtlasIconPlayerConditionID;
    }

    public sealed class TaxiPathRecord
    {
        public ushort From;
        public ushort To;
        public uint Id;
        public uint Cost;
    }

    public sealed class TaxiPathNodeRecord
    {
        public Vector3 Loc;
        public ushort PathID;
        public ushort MapID;
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
        public uint CategoryMask;
        public byte CategoryType;
    }

    public sealed class ToyRecord
    {
        public uint ItemID;
        public LocalizedString Description;
        public byte Flags;
        public byte CategoryFilter;
        public uint Id;
    }

    public sealed class TransmogHolidayRecord
    {
        public uint Id;
        public int HolidayID;
    }

    public sealed class TransmogSetRecord
    {
        public LocalizedString Name;
        public ushort BaseSetID;
        public ushort UIOrder;
        public byte ExpansionID;
        public uint Id;
        public int Flags;
        public int QuestID;
        public int ClassMask;
        public int ItemNameDescriptionID;
        public uint TransmogSetGroupID;
    }

    public sealed class TransmogSetGroupRecord
    {
        public LocalizedString Label;
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
        public uint TransportID;
        public uint TimeIndex;
        public Vector3 Pos;
        public byte SequenceID;
    }

    public sealed class TransportRotationRecord
    {
        public uint Id;
        public uint TransportID;
        public uint TimeIndex;
        public float X;
        public float Y;
        public float Z;
        public float W;
    }
}
