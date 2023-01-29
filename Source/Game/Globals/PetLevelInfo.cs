// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game
{
    public class PetLevelInfo
    {
        public uint Armor { get; set; }
        public uint Health { get; set; }
        public uint Mana { get; set; }

        public uint[] Stats { get; set; } = new uint[(int)Framework.Constants.Stats.Max];

        public PetLevelInfo()
        {
            Health = 0;
            Mana = 0;
        }
    }
}