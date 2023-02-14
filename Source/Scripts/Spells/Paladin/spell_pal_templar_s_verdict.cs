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
    [SpellScript(85256)] // 85256 - Templar's Verdict
    internal class spell_pal_templar_s_verdict : SpellScript, IHasSpellEffects
    {
        public List<ISpellEffect> SpellEffects { get; } = new();

        public override bool Validate(SpellInfo spellEntry)
        {
            return ValidateSpellInfo(PaladinSpells.TemplarVerdictDamage);
        }

        public override void Register()
        {
            SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
        }

        private void HandleHitTarget(uint effIndex)
        {
            GetCaster().CastSpell(GetHitUnit(), PaladinSpells.TemplarVerdictDamage, true);
        }
    }
}
