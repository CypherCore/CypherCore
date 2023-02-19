// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(1822)]
public class spell_dru_rake : SpellScript, IHasSpellEffects
{
	private bool _stealthed = false;

	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Load()
	{
		var caster = GetCaster();

		if (caster.HasAuraType(AuraType.ModStealth))
			_stealthed = true;

		return true;
	}

	private void HandleOnHit(int effIndex)
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (caster == null || target == null)
			return;

		// While stealthed or have Incarnation: King of the Jungle aura, deal 100% increased damage
		if (_stealthed || caster.HasAura(ShapeshiftFormSpells.INCARNATION_KING_OF_JUNGLE))
			SetHitDamage(GetHitDamage() * 2);

		// Only stun if the caster was in stealth
		if (_stealthed)
			caster.CastSpell(target, RakeSpells.RAKE_STUN, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}