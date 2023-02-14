// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(115191)]
public class spell_rog_stealth_with_subterfuge_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (!GetCaster())
			return;

		GetCaster().RemoveAura(115191);
		GetCaster().RemoveAura(115192);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}