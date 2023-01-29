// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Collision
{
    public class AreaAndLiquidData
    {
        public AreaInfo? AreaInfoData;

        public float FloorZ = MapConst.VMAPInvalidHeightValue;
        public LiquidInfo? LiquidInfoData;

        public struct AreaInfo
        {
            public int AdtId;
            public int RootId;
            public int GroupId;
            public uint MogpFlags;

            public AreaInfo(int adtId, int rootId, int groupId, uint flags)
            {
                AdtId = adtId;
                RootId = rootId;
                GroupId = groupId;
                MogpFlags = flags;
            }
        }

        public struct LiquidInfo
        {
            public uint LiquidType;
            public float Level;

            public LiquidInfo(uint type, float level)
            {
                LiquidType = type;
                Level = level;
            }
        }
    }
}