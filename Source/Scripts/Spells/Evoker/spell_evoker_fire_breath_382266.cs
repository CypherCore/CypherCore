// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Evoker;

[SpellScript(382266)]
public class spell_evoker_fire_breath_382266 : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private enum eSpells
	{
		FireBreath = 357209
	}

	private void HandleAfterRemove(AuraEffect p_AuraEff, AuraEffectHandleModes p_Mode)
	{
		var l_Caster = GetCaster();

		if (l_Caster == null || GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Cancel)
			return;

		l_Caster.CastSpell(l_Caster, eSpells.FireBreath, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}