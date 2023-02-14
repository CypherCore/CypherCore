// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script] // 117952 - Crackling Jade Lightning
internal class spell_monk_crackling_jade_lightning : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MonkSpells.StanceOfTheSpiritedCrane, MonkSpells.CracklingJadeLightningChiProc);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnTick, 0, AuraType.PeriodicDamage));
	}

	private void OnTick(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster)
			if (caster.HasAura(MonkSpells.StanceOfTheSpiritedCrane))
				caster.CastSpell(caster, MonkSpells.CracklingJadeLightningChiProc, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}