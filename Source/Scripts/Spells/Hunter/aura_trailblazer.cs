using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using Game.Spells.Events;

namespace Scripts.Spells.Hunter;

[SpellScript(199921)]
public class aura_trailblazer : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();
	DelayedCastEvent _event;
	TimeSpan _ts;

	private void EffectApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		RescheduleBuff();

		Player player = GetTarget().ToPlayer();
		if (player != null)
		{
			player.SetSpeed(UnitMoveType.Run, player.GetSpeedRate(UnitMoveType.Run) + 0.15f);
		}
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		RescheduleBuff();
	}

	private void RescheduleBuff()
	{
		Unit caster = GetCaster();
		caster.RemoveAurasDueToSpell(HunterSpells.SPELL_HUNTER_TRAILBLAZER_BUFF);

		if (_event == null)
		{
			_event = new DelayedCastEvent(caster, caster, HunterSpells.SPELL_HUNTER_TRAILBLAZER_BUFF, new CastSpellExtraArgs(true));
			_ts    = TimeSpan.FromSeconds(GetSpellInfo().GetEffect(0).BasePoints);
		}
		else
			caster.m_Events.GetEvents().RemoveFirstMatching(e =>
			                                                {
				                                                if (e.Value is DelayedCastEvent dce)
					                                                return dce.SpellId == HunterSpells.SPELL_HUNTER_TRAILBLAZER_BUFF;

				                                                return false;
			                                                });

		caster.m_Events.AddEventAtOffset(_event, _ts);
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Player player = GetTarget().ToPlayer();
		if (player != null)
		{
			player.SetSpeed(UnitMoveType.Run, player.GetSpeedRate(UnitMoveType.Run) - 0.15f);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(EffectApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}