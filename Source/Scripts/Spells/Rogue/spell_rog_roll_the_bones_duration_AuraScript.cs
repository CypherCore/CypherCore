// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[]
             {
	             199603, 193358, 193357, 193359, 199600, 193356
             })]
public class spell_rog_roll_the_bones_duration_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void AfterApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		var rtb = caster.GetAura(RogueSpells.ROLL_THE_BONES);

		if (rtb == null)
		{
			caster.RemoveAura(GetSpellInfo().Id); //sometimes it remains on the caster after relog incorrectly.

			return;
		}

		var aur = caster.GetAura(GetSpellInfo().Id);

		if (aur != null)
		{
			aur.SetMaxDuration(rtb.GetDuration());
			aur.SetDuration(rtb.GetDuration());
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterApply, 0, AuraType.Any, AuraEffectHandleModes.Real));
	}
}