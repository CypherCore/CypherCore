// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(5143)]
public class spell_mage_arcane_missiles : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		//@TODO: Remove when proc system can handle arcane missiles.....
		caster.RemoveAura(MageSpells.SPELL_MAGE_CLEARCASTING_BUFF);
		caster.RemoveAura(MageSpells.SPELL_MAGE_CLEARCASTING_EFFECT);
		var pvpClearcast = caster.GetAura(MageSpells.SPELL_MAGE_CLEARCASTING_PVP_STACK_EFFECT);

		if (pvpClearcast != null)
			pvpClearcast.ModStackAmount(-1);

		caster.RemoveAura(MageSpells.SPELL_MAGE_RULE_OF_THREES_BUFF);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 1, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real));
	}
}