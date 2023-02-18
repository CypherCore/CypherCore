// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(49821)]
public class spell_pri_mind_sear : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(PriestSpells.MIND_SEAR_INSANITY, Difficulty.None) != null)
			return false;

		return true;
	}

	private void HandleInsanity(uint UnnamedParameter)
	{
		GetCaster().CastSpell(GetCaster(), PriestSpells.MIND_SEAR_INSANITY, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleInsanity, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}