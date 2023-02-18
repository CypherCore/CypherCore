// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(205065)]
public class aura_pri_void_torrent : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetTarget().CastSpell(GetTarget(), PriestSpells.VOID_TORRENT_PREVENT_REGEN, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetTarget().RemoveAura(PriestSpells.VOID_TORRENT_PREVENT_REGEN);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}