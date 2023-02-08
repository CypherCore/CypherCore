using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 46584 - Raise Dead
internal class spell_dk_raise_dead : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(DeathKnightSpells.RaiseDeadSummon, DeathKnightSpells.SludgeBelcher, DeathKnightSpells.SludgeBelcherSummon);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		uint spellId = DeathKnightSpells.RaiseDeadSummon;

		if (GetCaster().HasAura(DeathKnightSpells.SludgeBelcher))
			spellId = DeathKnightSpells.SludgeBelcherSummon;

		GetCaster().CastSpell((Unit)null, spellId, true);
	}
}