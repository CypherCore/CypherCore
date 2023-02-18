// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 603 - Doom
	public class spell_warlock_doom : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void PeriodicTick(AuraEffect aurEff)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			caster.CastSpell(caster, WarlockSpells.DOOM_ENERGIZE, true);

			if (caster.HasAura(WarlockSpells.IMPENDING_DOOM))
				caster.CastSpell(GetTarget(), WarlockSpells.WILD_IMP_SUMMON, true);

			if (caster.HasAura(WarlockSpells.DOOM_DOUBLED) && RandomHelper.randChance(25))
				GetEffect(0).SetAmount(aurEff.GetAmount() * 2);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.PeriodicDamage));
		}
	}
}