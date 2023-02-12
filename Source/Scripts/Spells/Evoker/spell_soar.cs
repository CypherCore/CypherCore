using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(369536)]
public class spell_soar : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(EvokerSpells.SPELL_EVOKER_SOAR_RACIAL, EvokerSpells.SPELL_SKYWARD_ASCENT, EvokerSpells.SPELL_SURGE_FORWARD);
	}

	private void HandleOnHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		// Increase flight speed by 830540%
		caster.SetSpeedRate(UnitMoveType.Flight, 83054.0f);

		var player = GetHitPlayer();
		// Add "Skyward Ascent" and "Surge Forward" to the caster's spellbook
		player.LearnSpell(EvokerSpells.SPELL_SKYWARD_ASCENT, false);
		player.LearnSpell(EvokerSpells.SPELL_SURGE_FORWARD, false);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}