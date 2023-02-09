using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(204021)]
public class spell_dh_fiery_brand : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER);
	}

	private void HandleDamage(int UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			GetCaster().CastSpell(target, DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 1, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}