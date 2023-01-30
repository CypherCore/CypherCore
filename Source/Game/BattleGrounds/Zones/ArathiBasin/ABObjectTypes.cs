// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.BattleGrounds.Zones.ArathiBasin
{
    internal struct ABObjectTypes
    {
        // for all 5 node points 8*5=40 objects
        public const int BANNER_NEUTRAL = 0;
        public const int BANNER_CONT_A = 1;
        public const int BANNER_CONT_H = 2;
        public const int BANNER_ALLY = 3;
        public const int BANNER_HORDE = 4;
        public const int AURA_ALLY = 5;
        public const int AURA_HORDE = 6;

        public const int AURA_CONTESTED = 7;

        //Gates
        public const int GATE_A = 40;

        public const int GATE_H = 41;

        //Buffs
        public const int SPEEDBUFF_STABLES = 42;
        public const int REGENBUFF_STABLES = 43;
        public const int BERSERKBUFF_STABLES = 44;
        public const int SPEEDBUFF_BLACKSMITH = 45;
        public const int REGENBUFF_BLACKSMITH = 46;
        public const int BERSERKBUFF_BLACKSMITH = 47;
        public const int SPEEDBUFF_FARM = 48;
        public const int REGENBUFF_FARM = 49;
        public const int BERSERKBUFF_FARM = 50;
        public const int SPEEDBUFF_LUMBER_MILL = 51;
        public const int REGENBUFF_LUMBER_MILL = 52;
        public const int BERSERKBUFF_LUMBER_MILL = 53;
        public const int SPEEDBUFF_GOLD_MINE = 54;
        public const int REGENBUFF_GOLD_MINE = 55;
        public const int BERSERKBUFF_GOLD_MINE = 56;
        public const int MAX = 57;
    }
}