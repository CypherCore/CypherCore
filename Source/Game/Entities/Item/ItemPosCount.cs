// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Game.Entities
{
    public class ItemPosCount
    {
        public ItemPosCount(ushort _pos, uint _count)
        {
            Pos = _pos;
            Count = _count;
        }

        public uint Count { get; set; }

        public ushort Pos { get; set; }

        public bool IsContainedIn(List<ItemPosCount> vec)
        {
            foreach (var posCount in vec)
                if (posCount.Pos == Pos)
                    return true;

            return false;
        }
    }
}