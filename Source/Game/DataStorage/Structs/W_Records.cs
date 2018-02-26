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
        public string AreaName;
        public int WMOGroupID;                                               //  used in group WMO
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
        public uint Id;
        public byte UWZoneMusic;
        public uint WMOID;                                                    //  used in root WMO
    }

    public sealed class WorldEffectRecord
    {
        public uint ID;
        public uint TargetAsset;
        public ushort CombatConditionID;
        public byte TargetType;
        public byte WhenToDisplay;
        public uint QuestFeedbackEffectID;
        public ushort PlayerConditionID;
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
        public string TextureName;
        public uint Id;
        public ushort TextureWidth;
        public ushort TextureHeight;
        public ushort MapAreaID;                                               // idx in WorldMapArea.dbc
        public ushort OffsetX;
        public uint OffsetY;
        public ushort HitRectTop;
        public ushort HitRectLeft;
        public ushort HitRectBottom;
        public ushort HitRectRight;
        public ushort PlayerConditionID;
        public byte Flags;
        public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea]; // needs checked

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
        public string AreaName;
        public Vector3 Loc;
        public float Facing;
        public ushort MapID;
    }
}
