// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public struct HitTriggerSpell
    {
        public HitTriggerSpell(SpellInfo spellInfo, SpellInfo auraSpellInfo, int procChance)
        {
            TriggeredSpell = spellInfo;
            TriggeredByAura = auraSpellInfo;
            Chance = procChance;
        }

        public SpellInfo TriggeredSpell;

        public SpellInfo TriggeredByAura;

        // ubyte triggeredByEffIdx          This might be needed at a later stage - No need known for now
        public int Chance;
    }
}