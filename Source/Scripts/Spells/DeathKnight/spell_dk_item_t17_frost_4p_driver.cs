// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(167655)]
public class spell_dk_item_t17_frost_4p_driver : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private struct eSpells
	{
		public const uint FrozenRuneblade = 170202;
	}

	private void OnProc(AuraEffect UnnamedParameter, ProcEventInfo p_EventInfo)
	{
		PreventDefaultAction();

		var l_Caster = GetCaster();

		if (l_Caster == null)
			return;

		var l_ProcSpell = p_EventInfo.GetDamageInfo().GetSpellInfo();

		if (l_ProcSpell == null)
			return;

		var l_Target = p_EventInfo.GetActionTarget();

		if (l_Target == null || l_Target == l_Caster)
			return;

		/// While Pillar of Frost is active, your special attacks trap a soul in your rune weapon.
		l_Caster.CastSpell(l_Target, eSpells.FrozenRuneblade, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}