// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_mark_of_kazrogal_hellfire_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spell)
	{
		return ValidateSpellInfo(GenericSpellIds.MarkOfKazrogalDamageHellfire);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodic, 0, AuraType.PowerBurn));
	}

	private void OnPeriodic(AuraEffect aurEff)
	{
		var target = GetTarget();

		if (target.GetPower(PowerType.Mana) == 0)
		{
			target.CastSpell(target, GenericSpellIds.MarkOfKazrogalDamageHellfire, new CastSpellExtraArgs(aurEff));
			// Remove aura
			SetDuration(0);
		}
	}
}