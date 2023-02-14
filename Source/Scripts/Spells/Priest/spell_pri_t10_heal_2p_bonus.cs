// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 70770 - Item - Priest T10 Healer 2P Bonus
internal class spell_pri_t10_heal_2p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.BlessedHealing);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		var healInfo = eventInfo.GetHealInfo();

		if (healInfo == null ||
		    healInfo.GetHeal() == 0)
			return;

		var spellInfo = Global.SpellMgr.GetSpellInfo(PriestSpells.BlessedHealing, GetCastDifficulty());
		var amount    = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
		amount /= (int)spellInfo.GetMaxTicks();

		var caster = eventInfo.GetActor();
		var target = eventInfo.GetProcTarget();

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell(target, PriestSpells.BlessedHealing, args);
	}
}