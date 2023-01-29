// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class AreaTriggerStruct
    {
        public uint PortLocId { get; set; }
        public uint Target_mapId { get; set; }
        public float Target_Orientation { get; set; }
        public float Target_X { get; set; }
        public float Target_Y { get; set; }
        public float Target_Z { get; set; }
    }
}