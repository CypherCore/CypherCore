using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(116680)]
public class bfa_spell_focused_thunder_talent_thunder_focus_tea : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster().ToPlayer();
		if (caster == null)
		{
			return;
		}

		if (caster.HasAura(MonkSpells.SPELL_FOCUSED_THUNDER_TALENT))
		{
			Aura thunder = caster.GetAura(MonkSpells.SPELL_MONK_THUNDER_FOCUS_TEA);
			if (thunder != null)
			{
				thunder.SetStackAmount(2);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.AddFlatModifier, AuraEffectHandleModes.Real));
	}
}