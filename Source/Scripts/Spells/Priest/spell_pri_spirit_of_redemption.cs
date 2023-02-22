// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 20711 - Spirit of Redemption
internal class spell_pri_spirit_of_redemption : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.SPIRIT_OF_REDEMPTION);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0, true, AuraScriptHookType.EffectAbsorb));
	}

	private void HandleAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double absorbAmount)
	{
		var target = GetTarget();
		target.CastSpell(target, PriestSpells.SPIRIT_OF_REDEMPTION, new CastSpellExtraArgs(aurEff));
		target.SetFullHealth();

		absorbAmount = dmgInfo.GetDamage();
	}
}