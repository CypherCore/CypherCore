// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 129250 - Power Word: Solace
internal class spell_pri_power_word_solace : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.POWER_WORD_SOLACE_ENERGIZE);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(RestoreMana, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
	}

	private void RestoreMana(int effIndex)
	{
		GetCaster()
			.CastSpell(GetCaster(),
			           PriestSpells.POWER_WORD_SOLACE_ENERGIZE,
			           new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell())
			                                                                        .AddSpellMod(SpellValueMod.BasePoint0, GetEffectValue() / 100));
	}
}