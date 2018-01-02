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
    public sealed class WMOAreaTableRecord
    {
        public int WMOGroupID;                                               //  used in group WMO
        public LocalizedString AreaName;
        public short WMOID;                                                    //  used in root WMO
        public ushort AmbienceID;
        public ushort ZoneMusic;
        public ushort IntroSound;
        public ushort AreaTableID;
        public ushort UWIntroSound;
        public ushort UWAmbience;
        public sbyte NameSet;                                                   //  used in adt file
        public byte SoundProviderPref;
        public byte SoundProviderPrefUnderwater;
        public byte Flags;
        public uint I;
        public uint UWZoneMusic;
    }

    public sealed class WorldEffectRecord
    {
        public uint ID;
        public uint TargetAsset;
        public ushort CombatConditionID;
        public byte TargetType;
        public byte WhenToDisplay;
        public uint QuestFeedbackEffectID;
        public uint PlayerConditionID;
    }

    public sealed class WorldMapAreaRecord
    {
        public string AreaName;
        public float LocLeft;
        public float LocRight;
        public float LocTop;
        public float LocBottom;
        public uint Flags;
        public ushort MapID;
        public ushort AreaID;
        public short DisplayMapID;
        public short DefaultDungeonFloor;
        public ushort ParentWorldMapID;
        public byte LevelRangeMin;
        public byte LevelRangeMax;
        public byte BountySetID;
        public byte BountyBoardLocation;
        public uint Id;
        public uint PlayerConditionID;
    }

    public sealed class WorldMapOverlayRecord
    {
        public uint Id;
        public string TextureName;
        public ushort TextureWidth;
        public ushort TextureHeight;
        public uint MapAreaID;                                               // idx in WorldMapArea.dbc
        public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea];
        public int OffsetX;
        public int OffsetY;
        public int HitRectTop;
        public int HitRectLeft;
        public int HitRectBottom;
        public int HitRectRight;
        public uint PlayerConditionID;
        public uint Flags;
    }

    public sealed class WorldMapTransformsRecord
    {
        public uint Id;
        public Vector3 RegionMin;
        public Vector3 RegionMax;
        public Vector2 RegionOffset;
        public float RegionScale;
        public ushort MapID;
        public ushort AreaID;
        public ushort NewMapID;
        public ushort NewDungeonMapID;
        public ushort NewAreaID;
        public byte Flags;
        public int Priority;
    }

    public sealed class WorldSafeLocsRecord
    {
        public uint Id;
        public Vector3 Loc;
        public float Facing;
        public LocalizedString AreaName;
        public ushort MapID;
    }
}
