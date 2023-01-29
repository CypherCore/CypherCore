// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class EnchantDuration
    {
        public Item Item { get; set; }
        public uint Leftduration { get; set; }
        public EnchantmentSlot Slot;

        public EnchantDuration(Item _item = null, EnchantmentSlot _slot = EnchantmentSlot.Max, uint _leftduration = 0)
        {
            Item = _item;
            Slot = _slot;
            Leftduration = _leftduration;
        }
    }
}