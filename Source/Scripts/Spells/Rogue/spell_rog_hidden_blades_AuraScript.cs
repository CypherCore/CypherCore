using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(270061)]
public class spell_rog_hidden_blades_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private byte _stacks;

	private void HandleEffectPeriodic(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			if (_stacks != 20)
			{
				caster.AddAura(RogueSpells.SPELL_ROGUE_HIDDEN_BLADES_BUFF, caster);
				_stacks++;
			}

			if (_stacks >= 20)
				return;
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
	}
}