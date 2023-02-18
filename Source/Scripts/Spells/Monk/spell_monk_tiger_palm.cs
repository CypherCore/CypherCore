// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Monk;

[SpellScript(100780)]
public class spell_monk_tiger_palm : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(uint UnnamedParameter)
	{
		var powerStrikes = GetCaster().GetAura(MonkSpells.POWER_STRIKES_AURA);

		if (powerStrikes != null)
		{
			SetEffectValue(GetEffectValue() + powerStrikes.GetEffect(0).GetBaseAmount());
			powerStrikes.Remove();
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
	}
}