// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(137639)]
public class spell_monk_storm_earth_and_fire : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var target = GetTarget();
		target.CastSpell(target, StormEarthAndFireSpells.SPELL_MONK_SEF_STORM_VISUAL, true);
		target.CastSpell(target, StormEarthAndFireSpells.SPELL_MONK_SEF_SUMMON_EARTH, true);
		target.CastSpell(target, StormEarthAndFireSpells.SPELL_MONK_SEF_SUMMON_FIRE, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetTarget().RemoveAurasDueToSpell(StormEarthAndFireSpells.SPELL_MONK_SEF_STORM_VISUAL);

		var fireSpirit = GetTarget().GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);

		if (fireSpirit != null)
			fireSpirit.ToTempSummon().DespawnOrUnsummon();

		var earthSpirit = GetTarget().GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);

		if (earthSpirit != null)
			earthSpirit.ToTempSummon().DespawnOrUnsummon();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.AddPctModifier, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}