// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script] // 117959 - Crackling Jade Lightning
internal class spell_monk_crackling_jade_lightning_knockback_proc_aura : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MonkSpells.CracklingJadeLightningKnockback, MonkSpells.CracklingJadeLightningKnockbackCd);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (GetTarget().HasAura(MonkSpells.CracklingJadeLightningKnockbackCd))
			return false;

		if (eventInfo.GetActor().HasAura(MonkSpells.CracklingJadeLightningChannel, GetTarget().GetGUID()))
			return false;

		var currentChanneledSpell = GetTarget().GetCurrentSpell(CurrentSpellTypes.Channeled);

		if (!currentChanneledSpell ||
		    currentChanneledSpell.GetSpellInfo().Id != MonkSpells.CracklingJadeLightningChannel)
			return false;

		return true;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		GetTarget().CastSpell(eventInfo.GetActor(), MonkSpells.CracklingJadeLightningKnockback, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		GetTarget().CastSpell(GetTarget(), MonkSpells.CracklingJadeLightningKnockbackCd, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}