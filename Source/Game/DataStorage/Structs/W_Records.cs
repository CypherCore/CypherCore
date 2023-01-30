// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.DataStorage
{
    public sealed class WMOAreaTableRecord
    {
        public ushort AmbienceID;
        public string AreaName;
        public ushort AreaTableID;
        public byte Flags;
        public uint Id;
        public ushort IntroSound;
        public byte NameSetID; //  used in adt file
        public byte SoundProviderPref;
        public byte SoundProviderPrefUnderwater;
        public ushort UwAmbience;
        public ushort UwIntroSound;
        public uint UwZoneMusic;
        public int WmoGroupID; //  used in group WMO
        public ushort WmoID;   //  used in root WMO
        public ushort ZoneMusic;
    }

    public sealed class WorldEffectRecord
    {
        public ushort CombatConditionID;
        public uint Id;
        public uint PlayerConditionID;
        public uint QuestFeedbackEffectID;
        public int TargetAsset;
        public byte TargetType;
        public byte WhenToDisplay;
    }

    public sealed class WorldMapOverlayRecord
    {
        public uint[] AreaID = new uint[SharedConst.MaxWorldMapOverlayArea];
        public uint Flags;
        public int HitRectBottom;
        public int HitRectLeft;
        public int HitRectRight;
        public int HitRectTop;
        public uint Id;
        public int OffsetX;
        public int OffsetY;
        public uint PlayerConditionID;
        public ushort TextureHeight;
        public ushort TextureWidth;
        public uint UiMapArtID;
    }

    public sealed class WorldStateExpressionRecord
    {
        public string Expression;
        public uint Id;
    }
}