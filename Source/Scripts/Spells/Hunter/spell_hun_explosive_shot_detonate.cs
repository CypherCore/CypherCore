// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(212679)]
public class spell_hun_explosive_shot_detonate : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleDummy(uint UnnamedParameter)
	{
		var at = GetCaster().GetAreaTrigger(HunterSpells.EXPLOSIVE_SHOT);

		if (at != null)
		{
			GetCaster().RemoveAura(HunterSpells.EXPLOSIVE_SHOT);
			GetCaster().CastSpell(at.GetPosition(), HunterSpells.EXPLOSIVE_SHOT_DAMAGE, true);
			at.Remove();
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}