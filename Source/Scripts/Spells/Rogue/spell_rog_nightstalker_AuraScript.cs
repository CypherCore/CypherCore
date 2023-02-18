// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(14062)]
public class spell_rog_nightstalker_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster)
		{
			if (caster.HasAura(RogueSpells.NIGHTSTALKER_DAMAGE_DONE))
				caster.RemoveAura(RogueSpells.NIGHTSTALKER_DAMAGE_DONE);

			if (caster.HasAura(RogueSpells.SHADOW_FOCUS_EFFECT))
				caster.RemoveAura(RogueSpells.SHADOW_FOCUS_EFFECT);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.ModShapeshift, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}