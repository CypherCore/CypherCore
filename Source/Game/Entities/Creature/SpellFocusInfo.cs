// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Game.Spells;

namespace Game.Entities
{
    internal struct SpellFocusInfo
	{
		public Spell Spell;
		public uint Delay;        // ms until the creature's Target should snap back (0 = no snapback scheduled)
		public ObjectGuid Target; // the creature's "real" Target while casting
		public float Orientation; // the creature's "real" orientation while casting
	}
}