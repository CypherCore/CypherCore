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
		return ValidateSpellInfo(PriestSpells.PowerWordSolaceEnergize);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(RestoreMana, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
	}

	private void RestoreMana(uint effIndex)
	{
		GetCaster()
			.CastSpell(GetCaster(),
			           PriestSpells.PowerWordSolaceEnergize,
			           new CastSpellExtraArgs(TriggerCastFlags.IgnoreCastInProgress).SetTriggeringSpell(GetSpell())
			                                                                        .AddSpellMod(SpellValueMod.BasePoint0, GetEffectValue() / 100));
	}
}