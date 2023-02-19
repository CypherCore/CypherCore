// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(new uint[]
             {
	             47750, 47666
             })]
public class spell_pri_penance_heal_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.POWER_OF_THE_DARK_SIDE_MARKER, PriestSpells.PENANCE_HEAL);
	}

	private void HandleDummy(int effIndex)
	{
		if (GetCaster().GetAuraEffect(PriestSpells.CONTRITION, 0) != null)
			foreach (var auApp in GetCaster().GetAppliedAuras().LookupByKey(PriestSpells.ATONEMENT_AURA))
				GetCaster().CastSpell(auApp.GetTarget(), PriestSpells.CONTRITION_HEAL, true);

		var powerOfTheDarkSide = GetCaster().GetAuraEffect(PriestSpells.POWER_OF_THE_DARK_SIDE_MARKER, 0);

		if (powerOfTheDarkSide != null)
		{
			if (GetSpellInfo().Id == PriestSpells.PENANCE_HEAL)
			{
				var heal = GetHitHeal();
				MathFunctions.AddPct(ref heal, powerOfTheDarkSide.GetAmount());
				SetHitHeal(heal);
			}
			else
			{
				var damage = GetHitDamage();
				MathFunctions.AddPct(ref damage, powerOfTheDarkSide.GetAmount());
				SetHitDamage(damage);
			}
		}
	}

	public override void Register()
	{
		if (ScriptSpellId == PriestSpells.PENANCE_HEAL)
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));

		if (ScriptSpellId == PriestSpells.PENANCE_DAMAGE)
			SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}