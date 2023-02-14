// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(115151)]
public class spell_monk_renewing_mist : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MonkSpells.SPELL_MONK_RENEWING_MIST_HOT);
	}

	private void HandleDummy(uint effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var target = GetExplTargetUnit();

		if (target != null)
			GetCaster().CastSpell(target, MonkSpells.SPELL_MONK_RENEWING_MIST_HOT, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.Launch));
	}
}