// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Paladin
{
    // 205290 - Wake of Ashes
    [SpellScript(205290)]
    public class spell_pal_wake_of_ashes : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        private void HandleDamages(int effIndex)
        {
            Creature target = GetHitCreature();
            if (target != null)
            {
                CreatureTemplate creTemplate = target.GetCreatureTemplate();

                if (creTemplate != null)
			    {
                    if (creTemplate.CreatureType == CreatureType.Demon || creTemplate.CreatureType == CreatureType.Undead)
                    {
                        GetCaster().CastSpell(target, PaladinSpells.WAKE_OF_ASHES_STUN, true);
                    }
                }
            }
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDamages, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
        }
    }
}
