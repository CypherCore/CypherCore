// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(197923)]
public class spell_dh_fel_rush_dash_SpellScript : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void PreventTrigger(uint effIndex)
	{
		PreventHitEffect(effIndex);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(PreventTrigger, 6, SpellEffectName.TriggerSpell, SpellScriptHookType.Launch));
		SpellEffects.Add(new EffectHandler(PreventTrigger, 6, SpellEffectName.TriggerSpell, SpellScriptHookType.EffectHit));
	}
}