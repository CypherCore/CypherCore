// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(198013)]
public class spell_dh_eye_beam : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();


	private bool _firstTick = true;

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (!Global.SpellMgr.HasSpellInfo(DemonHunterSpells.EYE_BEAM, Difficulty.None) || !Global.SpellMgr.HasSpellInfo(DemonHunterSpells.EYE_BEAM_DAMAGE, Difficulty.None))
			return false;

		return true;
	}

	private void HandlePeriodic(AuraEffect UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			if (!_firstTick)
			{
				caster.CastSpell(caster, DemonHunterSpells.EYE_BEAM_DAMAGE, true);
				var energize = caster.GetAuraEffectAmount(DemonHunterSpells.BLIND_FURY, 2);

				if (energize != 0)
					caster.ModifyPower(PowerType.Fury, energize * 2.0f / 50.0f);
			}

		_firstTick = false;
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
			caster.RemoveAura(DemonHunterSpells.EYE_BEAM_VISUAL);
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			if (!caster.HasAura(DemonHunterSpells.DEMONIC))
				caster.CastSpell(caster, DemonHunterSpells.EYE_BEAM_VISUAL, true);

			if (caster.HasAura(DemonHunterSpells.DEMONIC))
			{
				var aur = caster.GetAura(DemonHunterSpells.METAMORPHOSIS_HAVOC);

				if (aur != null)
					aur.ModDuration(8 * Time.InMilliseconds);
				else
					aur = caster.AddAura(DemonHunterSpells.METAMORPHOSIS_HAVOC, caster);

				if (aur != null)
					aur.SetDuration(10 * Time.InMilliseconds);
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicTriggerSpell));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 2, AuraType.Dummy, AuraEffectHandleModes.Real));
	}
}