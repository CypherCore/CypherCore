// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(208796)]
public class spell_dh_jagged_spikes : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();
		var target = eventInfo.GetActor();

		if (caster == null || eventInfo.GetDamageInfo() != null)
			return;

		if (caster.IsFriendlyTo(target))
			return;

		var pct    = caster.GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_JAGGED_SPIKES, 0);
		var damage = eventInfo.GetDamageInfo().GetDamage();
		MathFunctions.ApplyPct(ref damage, pct);

		caster.CastSpell(target, DemonHunterSpells.SPELL_DH_JAGGED_SPIKES_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)damage));
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}