using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(703)]
public class spell_rog_garrote_SpellScript : SpellScript, ISpellOnHit
{
	private bool _stealthed;

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(RogueSpells.SPELL_ROGUE_GARROTE_DOT, RogueSpells.SPELL_ROGUE_GARROTE_SILENCE);
	}

	public override bool Load()
	{
		if (GetCaster().HasAuraType(AuraType.ModStealth))
			_stealthed = true;

		return true;
	}

	public void OnHit()
	{
		var caster = GetCaster();
		var target = GetExplTargetUnit();

		if (_stealthed)
			caster.CastSpell(target, RogueSpells.SPELL_ROGUE_GARROTE_SILENCE, true);
	}
}