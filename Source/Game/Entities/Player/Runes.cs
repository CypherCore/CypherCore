// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Framework.Constants;

namespace Game.Entities
{
    public class Runes
    {
        public uint[] Cooldown { get; set; } = new uint[PlayerConst.MaxRunes];

        public List<byte> CooldownOrder { get; set; } = new();
        public byte RuneState { get; set; } // mask of available runes

        public void SetRuneState(byte index, bool set = true)
        {
            bool foundRune = CooldownOrder.Contains(index);

            if (set)
            {
                RuneState |= (byte)(1 << index); // usable

                if (foundRune)
                    CooldownOrder.Remove(index);
            }
            else
            {
                RuneState &= (byte)~(1 << index); // on cooldown

                if (!foundRune)
                    CooldownOrder.Add(index);
            }
        }
    }
}