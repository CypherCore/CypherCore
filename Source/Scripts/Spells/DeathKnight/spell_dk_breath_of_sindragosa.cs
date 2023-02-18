// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(152279)]
public class spell_dk_breath_of_sindragosa : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnTick(AuraEffect UnnamedParameter)
	{
		var l_Caster = GetCaster();

		if (l_Caster == null)
			return;

		var l_Player = l_Caster.ToPlayer();

		if (l_Player == null)
			return;

		l_Caster.ModifyPower(PowerType.RunicPower, -130);
		/*if (l_Caster->ToPlayer())
                l_Caster->ToPlayer()->SendPowerUpdate(PowerType.RunicPower, l_Caster->GetPower(PowerType.RunicPower));*/

		if (l_Caster.GetPower(PowerType.RunicPower) <= 130)
			l_Caster.RemoveAura(DeathKnightSpells.SPELL_DK_BREATH_OF_SINDRAGOSA);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicTriggerSpell));
	}
}