// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script]
internal class spell_mage_cauterize_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 2 && ValidateSpellInfo(MageSpells.CauterizeDot, MageSpells.Cauterized, spellInfo.GetEffect(2).TriggerSpell);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, false, AuraScriptHookType.EffectAbsorb));
	}

	private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
	{
		var effectInfo = GetEffect(1);

		if (effectInfo == null ||
		    !GetTargetApplication().HasEffect(1) ||
		    dmgInfo.GetDamage() < GetTarget().GetHealth() ||
		    dmgInfo.GetDamage() > GetTarget().GetMaxHealth() * 2 ||
		    GetTarget().HasAura(MageSpells.Cauterized))
		{
			PreventDefaultAction();

			return;
		}

		GetTarget().SetHealth(GetTarget().CountPctFromMaxHealth(effectInfo.GetAmount()));
		GetTarget().CastSpell(GetTarget(), GetEffectInfo(2).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		GetTarget().CastSpell(GetTarget(), MageSpells.CauterizeDot, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
		GetTarget().CastSpell(GetTarget(), MageSpells.Cauterized, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
	}
}