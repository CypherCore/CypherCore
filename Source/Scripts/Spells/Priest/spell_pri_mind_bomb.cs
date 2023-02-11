﻿using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 205369 - Mind Bomb
internal class spell_pri_mind_bomb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.MindBombStun);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death ||
		    GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
			GetCaster()?.CastSpell(GetTarget().GetPosition(), PriestSpells.MindBombStun, new CastSpellExtraArgs(true));
	}
}