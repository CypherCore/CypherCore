// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public struct VisibleMapIdRef
    {
        public VisibleMapIdRef(int references, TerrainSwapInfo visibleMapInfo)
        {
            References = references;
            VisibleMapInfo = visibleMapInfo;
        }

        public int References { get; set; }
        public TerrainSwapInfo VisibleMapInfo { get; set; }
    }
}