// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Entities
{
    public class SpellThreatEntry
    {
        public float ApPctMod { get; set; } // Pct of AP that is added as Threat - default: 0.0f
        public int FlatMod { get; set; }    // flat threat-value for this Spell  - default: 0
        public float PctMod { get; set; }   // threat-Multiplier for this Spell  - default: 1.0f
    }
}