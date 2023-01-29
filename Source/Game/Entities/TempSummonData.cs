// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class TempSummonData
    {
        public uint Entry { get; set; }   // Entry of summoned creature
        public Position Pos { get; set; } // Position, where should be creature spawned
        public uint Time { get; set; }    // Despawn Time, usable only with certain temp summon types
        public TempSummonType Type { get; set; } // Summon Type, see TempSummonType for available types
    }
}