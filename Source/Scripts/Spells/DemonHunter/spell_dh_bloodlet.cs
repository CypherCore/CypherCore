// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(206473)]
public class spell_dh_bloodlet : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetSpellInfo().Id == DemonHunterSpells.THROW_GLAIVE)
			return true;

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo eventInfo)
	{
		var caster = GetCaster();
		var target = eventInfo.GetActionTarget();

		if (caster == null || target == null || eventInfo.GetDamageInfo() != null || !GetSpellInfo().GetEffect(0).IsEffect())
			return;

		var basePoints = GetSpellInfo().GetEffect(0).BasePoints;
		var dmg        = (eventInfo.GetDamageInfo().GetDamage() * (double)basePoints) / 100.0f;
		var dmgPerTick = (double)dmg / 5.0f;

		// Any remaining damage must be added
		var dot = target.GetAuraEffect(DemonHunterSpells.BLOODLET_DOT, 0, caster.GetGUID());

		if (dot != null)
			dmgPerTick += (dot.GetAmount() * (dot.GetTotalTicks() - dot.GetTickNumber())) / 5;

		var args = new CastSpellExtraArgs();
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)dmgPerTick);
		args.SetTriggerFlags(TriggerCastFlags.FullMask);
		caster.CastSpell(target, DemonHunterSpells.BLOODLET_DOT, args);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}