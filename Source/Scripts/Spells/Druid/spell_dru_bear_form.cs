// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(5487)]
public class spell_dru_bear_form : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(caster, BearFormSpells.BEARFORM_OVERRIDE, true);

		if (caster.HasSpell(BearFormSpells.STAMPEDING_ROAR))
			caster.CastSpell(caster, BearFormSpells.STAMPEDING_ROAR_BEAR_OVERRIDE, true);
	}

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.RemoveAura(BearFormSpells.BEARFORM_OVERRIDE);

		if (caster.HasSpell(BearFormSpells.STAMPEDING_ROAR))
			caster.RemoveAura(BearFormSpells.STAMPEDING_ROAR_BEAR_OVERRIDE);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}