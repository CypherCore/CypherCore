using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 120 - Cone of Cold
internal class spell_mage_cone_of_cold : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.ConeOfColdSlow);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleSlow, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void HandleSlow(int effIndex)
	{
		GetCaster().CastSpell(GetHitUnit(), MageSpells.ConeOfColdSlow, true);
	}
}