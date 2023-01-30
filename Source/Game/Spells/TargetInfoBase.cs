// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

namespace Game.Spells
{
    public class TargetInfoBase
    {
        public uint EffectMask { get; set; }

        public virtual void PreprocessTarget(Spell spell)
        {
        }

        public virtual void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
        }

        public virtual void DoDamageAndTriggers(Spell spell)
        {
        }
    }
}