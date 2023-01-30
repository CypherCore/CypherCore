// Copyright (c) CypherCore <http://github.com/CypherCore> All rights reserved.
// Licensed under the GNU GENERAL PUBLIC LICENSE. See LICENSE file in the project root for full license information.

using Framework.Constants;
using Game.Entities;

namespace Game.Spells
{
    public class ItemTargetInfo : TargetInfoBase
    {
        public Item TargetItem { get; set; }

        public override void DoTargetSpellHit(Spell spell, SpellEffectInfo spellEffectInfo)
        {
            spell.CallScriptBeforeHitHandlers(SpellMissInfo.None);

            spell.HandleEffects(null, TargetItem, null, null, spellEffectInfo, SpellEffectHandleMode.HitTarget);

            spell.CallScriptOnHitHandlers();
            spell.CallScriptAfterHitHandlers();
        }
    }
}