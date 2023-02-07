using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Groups;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 190336 - Conjure Refreshment
internal class spell_mage_conjure_refreshment : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.ConjureRefreshment, MageSpells.ConjureRefreshmentTable);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleDummy(uint effIndex)
	{
		Player caster = GetCaster().ToPlayer();

		if (caster)
		{
			Group group = caster.GetGroup();

			if (group)
				caster.CastSpell(caster, MageSpells.ConjureRefreshmentTable, true);
			else
				caster.CastSpell(caster, MageSpells.ConjureRefreshment, true);
		}
	}
}