// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
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
		return ValidateSpellInfo(PriestSpells.MIND_BOMB_STUN);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(RemoveEffect, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void RemoveEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death ||
		    GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
			GetCaster()?.CastSpell(GetTarget().GetPosition(), PriestSpells.MIND_BOMB_STUN, new CastSpellExtraArgs(true));
	}
}