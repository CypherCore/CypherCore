// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(116888)]
public class spell_dk_purgatory : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var _player = GetTarget().ToPlayer();

		if (_player != null)
		{
			var removeMode = GetTargetApplication().GetRemoveMode();

			if (removeMode == AuraRemoveMode.Expire)
				_player.CastSpell(_player, DeathKnightSpells.PURGATORY_INSTAKILL, true);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.SchoolHealAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}