// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior
{
	// Bladestorm - 227847, 46924
	public class spell_warr_bladestorm : SpellScript, ISpellOnCast, IHasSpellEffects
	{
		public List<ISpellEffect> SpellEffects { get; } = new();

		private void HandleOnHit(int effIndex)
		{
			PreventHitAura();
			PreventHitDamage();
			PreventHitDefaultEffect(effIndex);
			PreventHitEffect(effIndex);
			PreventHitHeal();
		}

		public void OnCast()
		{
			GetCaster().CastSpell(GetCaster(), WarriorSpells.NEW_BLADESTORM, true);
		}

		public override void Register()
		{
			SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleOnHit, 2, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
		}
	}
}