// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(102793)]
public class spell_dru_ursols_vortex : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private struct Spells
	{
		public static readonly uint URSOLS_VORTEX_SLOW = 127797;
	}


	private void HandleHit(int effIndex)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.AddAura(Spells.URSOLS_VORTEX_SLOW, GetHitUnit());
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}