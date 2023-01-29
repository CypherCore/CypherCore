// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Maps
{
    public class LiquidData
    {
        public float Depth_level { get; set; }
        public uint Entry { get; set; }
        public float Level { get; set; }
        public LiquidHeaderTypeFlags Type_flags { get; set; }
    }
}