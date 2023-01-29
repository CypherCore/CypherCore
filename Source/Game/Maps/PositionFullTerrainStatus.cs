// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public class PositionFullTerrainStatus
    {
        public struct AreaInfoData
        {
            public int AdtId;
            public int RootId;
            public int GroupId;
            public uint MogpFlags;

            public AreaInfoData(int adtId, int rootId, int groupId, uint flags)
            {
                AdtId = adtId;
                RootId = rootId;
                GroupId = groupId;
                MogpFlags = flags;
            }
        }

        public uint AreaId { get; set; }
        public AreaInfoData? AreaInfo { get; set; }
        public float FloorZ { get; set; }
        public LiquidData LiquidInfo { get; set; }
        public ZLiquidStatus LiquidStatus { get; set; }
        public bool Outdoors { get; set; } = true;
    }
}