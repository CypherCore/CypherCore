// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Framework.Dynamic;
using Game.Spells;

namespace Game.Entities
{
    public class SpellProcEntry
    {
        public SpellSchoolMask SchoolMask { get; set; }             // if nonzero - bitmask for matching proc condition based on spell's school
        public SpellFamilyNames SpellFamilyName { get; set; }       // if nonzero - for matching proc condition based on candidate spell's SpellFamilyName
        public FlagArray128 SpellFamilyMask { get; set; } = new(4); // if nonzero - bitmask for matching proc condition based on candidate spell's SpellFamilyFlags
        public ProcFlagsInit ProcFlags { get; set; }                // if nonzero - owerwrite procFlags field for given Spell.dbc entry, bitmask for matching proc condition, see enum ProcFlags
        public ProcFlagsSpellType SpellTypeMask { get; set; }       // if nonzero - bitmask for matching proc condition based on candidate spell's Damage/heal effects, see enum ProcFlagsSpellType
        public ProcFlagsSpellPhase SpellPhaseMask { get; set; }     // if nonzero - bitmask for matching phase of a spellcast on which proc occurs, see enum ProcFlagsSpellPhase
        public ProcFlagsHit HitMask { get; set; }                   // if nonzero - bitmask for matching proc condition based on hit result, see enum ProcFlagsHit
        public ProcAttributes AttributesMask { get; set; }          // bitmask, see ProcAttributes
        public uint DisableEffectsMask { get; set; }                // bitmask
        public float ProcsPerMinute { get; set; }                   // if nonzero - chance to proc is equal to value * aura caster's weapon speed / 60
        public float Chance { get; set; }                           // if nonzero - owerwrite procChance field for given Spell.dbc entry, defines chance of proc to occur, not used if ProcsPerMinute set
        public uint Cooldown { get; set; }                          // if nonzero - cooldown in secs for aura proc, applied to aura
        public uint Charges { get; set; }                           // if nonzero - owerwrite procCharges field for given Spell.dbc entry, defines how many times proc can occur before aura remove, 0 - infinite
    }
}