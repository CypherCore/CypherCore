// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;

namespace Game.Entities
{
    public class PetSpell
    {
        public ActiveStates Active { get; set; }
        public PetSpellState State { get; set; }
        public PetSpellType Type { get; set; }
    }
}