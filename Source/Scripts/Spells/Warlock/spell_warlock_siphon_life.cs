// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock
{
	// 63106 - Siphon Life @ Glyph of Siphon Life
	[SpellScript(63106)]
	public class spell_warlock_siphon_life : SpellScript, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleHit(int effIndex)
		{
			var caster = GetCaster();
			var heal   = caster.SpellHealingBonusDone(caster, GetSpellInfo(), caster.CountPctFromMaxHealth(GetSpellInfo().GetEffect(effIndex).BasePoints), DamageEffectType.Heal, GetEffectInfo(), 1, GetSpell());
			heal /= 100; // 0.5%
			heal =  caster.SpellHealingBonusTaken(caster, GetSpellInfo(), heal, DamageEffectType.Heal);
			SetHitHeal((int)heal);
			PreventHitDefaultEffect(effIndex);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
		}
	}
}