// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Scripting.Interfaces.ISpell;
using Game.Scripting.Interfaces;
using Game.Scripting;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Scripts.Spells.Paladin
{
    [SpellScript(114165)] // 114165 - Holy Prism
    internal class spell_pal_holy_prism : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellInfo)
        {
            return ValidateSpellInfo(PaladinSpells.HolyPrismTargetAlly, PaladinSpells.HolyPrismTargetEnemy, PaladinSpells.HolyPrismTargetBeamVisual);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleDummy(int effIndex)
        {
            if (GetCaster().IsFriendlyTo(GetHitUnit()))
                GetCaster().CastSpell(GetHitUnit(), PaladinSpells.HolyPrismTargetAlly, true);
            else
                GetCaster().CastSpell(GetHitUnit(), PaladinSpells.HolyPrismTargetEnemy, true);

            GetCaster().CastSpell(GetHitUnit(), PaladinSpells.HolyPrismTargetBeamVisual, true);
        }
    }
}
