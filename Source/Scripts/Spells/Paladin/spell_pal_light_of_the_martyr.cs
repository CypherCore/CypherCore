// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting.BaseScripts;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Game.Scripting.Interfaces;

namespace Scripts.Spells.Paladin
{
    // Light of the Martyr - 183998
    [SpellScript(183998)]
    public class spell_pal_light_of_the_martyr : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo UnnamedParameter)
        {
            if (Global.SpellMgr.GetSpellInfo(PaladinSpells.LIGHT_OF_THE_MARTYR_DAMAGE, Difficulty.None) != null)
            {
                return false;
            }
            return true;
        }

        private void HandleOnHit(int effIndex)
        {
            Unit caster = GetCaster();

            double dmg = (GetHitHeal() * 50.0f) / 100.0f;
            caster.CastSpell(caster, PaladinSpells.LIGHT_OF_THE_MARTYR_DAMAGE, (int)dmg);

            if (caster.HasAura(PaladinSpells.FERVENT_MARTYR_BUFF))
                caster.RemoveAura(PaladinSpells.FERVENT_MARTYR_BUFF);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
        }
    }
}
