// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    //183218
    [SpellScript(183218)]
    public class spell_pal_hand_of_hindrance : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new();

        private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes mode)
        {
            if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.EnemySpell)
            {
                Unit caster = GetCaster();
                if (caster != null)
                {
                    if (caster.HasAura(PaladinSpells.LAW_AND_ORDER))
                    {
                        caster.GetSpellHistory().ModifyCooldown(PaladinSpells.HAND_OF_HINDRANCE, TimeSpan.FromSeconds(-15));
                    }
                }
            }
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        }
    }
}
