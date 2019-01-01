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
using Framework.GameMath;
using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class UiMapRecord
    {
        public LocalizedString Name;
        public uint Id;
        public int ParentUiMapID;
        public int Flags;
        public int System;
        public UiMapType Type;
        public uint LevelRangeMin;
        public uint LevelRangeMax;
        public int BountySetID;
        public uint BountyDisplayLocation;
        public int VisibilityPlayerConditionID;
        public sbyte HelpTextPosition;
        public int BkgAtlasID;
    }

    public sealed class UiMapAssignmentRecord
    {
        public Vector2 UiMin;
        public Vector2 UiMax;
        public Vector3[] Region = new Vector3[2];
        public uint Id;
        public int UiMapID;
        public int OrderIndex;
        public int MapID;
        public int AreaID;
        public int WmoDoodadPlacementID;
        public int WmoGroupID;
    }

    public sealed class UiMapLinkRecord
    {
        public Vector2 UiMin;
        public Vector2 UiMax;
        public uint Id;
        public int ParentUiMapID;
        public int OrderIndex;
        public int ChildUiMapID;
    }

    public sealed class UiMapXMapArtRecord
    {
        public uint Id;
        public int PhaseID;
        public int UiMapArtID;
        public uint UiMapID;
    }

    public sealed class UnitPowerBarRecord
    {
        public uint Id;
        public string Name;
        public string Cost;
        public string OutOfError;
        public string ToolTip;
        public uint MinPower;
        public uint MaxPower;
        public ushort StartPower;
        public byte CenterPower;
        public float RegenerationPeace;
        public float RegenerationCombat;
        public byte BarType;
        public ushort Flags;
        public float StartInset;
        public float EndInset;
        public uint[] FileDataID = new uint[6];
        public uint[] Color = new uint[6];
    }
}
