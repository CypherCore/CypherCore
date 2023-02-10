using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 285466 - Lava Burst Overload
internal class spell_sha_mastery_elemental_overload_proc : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.MasteryElementalOverload);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(ApplyDamageModifier, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}

	private void ApplyDamageModifier(uint effIndex)
	{
		var elementalOverload = GetCaster().GetAuraEffect(ShamanSpells.MasteryElementalOverload, 1);

		if (elementalOverload != null)
			SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), elementalOverload.GetAmount()));
	}
}