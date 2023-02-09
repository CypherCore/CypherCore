using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[Script] // 212283 - Symbols of Death
internal class spell_rog_symbols_of_death : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(RogueSpells.SymbolsOfDeathRank2, RogueSpells.SymbolsOfDeathCritAura);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleEffectHitTarget(uint effIndex)
	{
		if (GetCaster().HasAura(RogueSpells.SymbolsOfDeathRank2))
			GetCaster().CastSpell(GetCaster(), RogueSpells.SymbolsOfDeathCritAura, true);
	}
}