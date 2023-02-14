// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 186257 - Aspect of the Cheetah
internal class spell_hun_aspect_cheetah : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.AspectCheetahSlow);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.ModIncreaseSpeed, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
			GetTarget().CastSpell(GetTarget(), HunterSpells.AspectCheetahSlow, true);
	}
}