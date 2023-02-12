using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(8936)]
public class spell_dru_regrowth : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DruidSpells.SPELL_DRU_REGROWTH, DruidSpells.SPELL_DRU_BLOODTALONS, DruidSpells.SPELL_DRU_BLOODTALONS_TRIGGERED, DruidSpells.SPELL_DRU_MOMENT_OF_CLARITY, DruidSpells.SPELL_DRU_CLEARCASTING);
	}

	private void HandleHealEffect(uint UnnamedParameter)
	{
		if (GetCaster().HasAura(DruidSpells.SPELL_DRU_BLOODTALONS))
			GetCaster().AddAura(DruidSpells.SPELL_DRU_BLOODTALONS_TRIGGERED, GetCaster());

		var clearcasting = GetCaster().GetAura(DruidSpells.SPELL_DRU_CLEARCASTING);

		if (clearcasting != null)
		{
			if (GetCaster().HasAura(DruidSpells.SPELL_DRU_MOMENT_OF_CLARITY))
			{
				var amount = clearcasting.GetEffect(0).GetAmount();
				clearcasting.GetEffect(0).SetAmount(amount - 1);

				if (amount == -102)
					GetCaster().RemoveAurasDueToSpell(DruidSpells.SPELL_DRU_CLEARCASTING);
			}
			else
			{
				GetCaster().RemoveAurasDueToSpell(DruidSpells.SPELL_DRU_CLEARCASTING);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHealEffect, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}