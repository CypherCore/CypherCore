// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

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

    public sealed class WorldStateExpressionRecord
    {
        public uint Id;
        public string Expression;
    }
}
