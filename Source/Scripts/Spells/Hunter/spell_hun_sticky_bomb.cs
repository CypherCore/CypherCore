// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[SpellScript(191241)]
public class spell_hun_sticky_bomb : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandleEffectRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		caster.CastSpell(GetTarget(), HunterSpells.SPELL_HUNTER_STICKY_BOMB_PROC, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleEffectRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}