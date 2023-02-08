using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(33110)]
public class spell_pri_prayer_of_mending_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	private void HandleHeal(uint UnnamedParameter)
	{
		Unit caster = GetOriginalCaster();
		if (caster != null)
		{
			AuraEffect aurEff = caster.GetAuraEffect(PriestSpells.SPELL_PRIEST_T9_HEALING_2P, 0);
			if (aurEff != null)
			{
				int heal = GetHitHeal();
				MathFunctions.AddPct(ref heal, aurEff.GetAmount());
				SetHitHeal(heal);
			}
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHeal, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}