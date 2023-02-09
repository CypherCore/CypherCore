using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(185314)]
public class spell_rog_deepening_shadows_AuraScript : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();


	private int _cp;

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			int maxcp = caster.HasAura(RogueSpells.SPELL_ROGUE_DEEPER_STRATAGEM) ? 6 : 5;
			_cp = Math.Min(caster.GetPower(PowerType.ComboPoints) + 1, maxcp);
		}
		if (eventInfo.GetSpellInfo().Id == 196819)
		{
			return true;
		}
		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (GetCaster().HasAura(RogueSpells.SPELL_ROGUE_DEEPENING_SHADOWS))
		{
			GetCaster().GetSpellHistory().ModifyCooldown(RogueSpells.SPELL_ROGUE_SHADOW_DANCE, TimeSpan.FromMilliseconds(_cp * -3000));
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}