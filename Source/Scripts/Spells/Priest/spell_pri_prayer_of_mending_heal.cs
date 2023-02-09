using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Priest;

[SpellScript(33110)]
public class spell_pri_prayer_of_mending_heal : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new();

	private void HandleHeal(int UnnamedParameter)
	{
		var caster = GetOriginalCaster();

		if (caster != null)
		{
			var aurEff = caster.GetAuraEffect(PriestSpells.SPELL_PRIEST_T9_HEALING_2P, 0);

			if (aurEff != null)
			{
				var heal = GetHitHeal();
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