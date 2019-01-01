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
    public sealed class WMOAreaTableRecord
    {
        public string AreaName;
        public uint Id;
        public ushort WmoID;                                                   //  used in root WMO
        public byte NameSetID;                                                //  used in adt file
        public int WmoGroupID;                                               //  used in group WMO
        public byte SoundProviderPref;
        public byte SoundProviderPrefUnderwater;
        public ushort AmbienceID;
        public ushort UwAmbience;
        public ushort ZoneMusic;
        public uint UwZoneMusic;
        public ushort IntroSound;
        public ushort UwIntroSound;
        public ushort AreaTableID;
        public byte Flags;
    }

    public sealed class WorldEffectRecord
    {
        public uint Id;
        public uint QuestFeedbackEffectID;
        public byte WhenToDisplay;
        public byte TargetType;
        public int TargetAsset;
        public uint PlayerConditionID;
        public ushort CombatConditionID;
    }

    public sealed class WorldMapOverlayRecord
    {
        public uint Id;
        public uint UiMapArtID;
        public ushort TextureWidth;
        public ushort TextureHeight;
        public int OffsetX;
        public int OffsetY;
        public int HitRectTop;
        public int HitRectBottom;
        public int HitRectLeft;
        public int HitRectRight;
        public uint PlayerConditionID;
        public uint Flags;
        public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea];
    }

    public sealed class WorldSafeLocsRecord
    {
        public uint Id;
        public string AreaName;
        public Vector3 Loc;
        public ushort MapID;
        public float Facing;
    }
}
