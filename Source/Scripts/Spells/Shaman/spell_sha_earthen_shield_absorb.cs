// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman
{
	//201633 - Earthen Shield
	[SpellScript(201633)]
	public class spell_sha_earthen_shield_absorb : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void CalcAbsorb(AuraEffect UnnamedParameter, ref double amount, ref bool UnnamedParameter2)
		{
			if (!GetCaster())
				return;

			amount = (int)GetCaster().GetHealth();
		}

		private void HandleAbsorb(AuraEffect UnnamedParameter, DamageInfo dmgInfo, ref double absorbAmount)
		{
			var caster = GetCaster();

			if (caster == null || !caster.IsTotem())
				return;

			var owner = caster.GetOwner();

			if (owner == null)
				return;

			if (dmgInfo.GetDamage() - owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true) > 0)
				absorbAmount = owner.GetTotalSpellPowerValue(SpellSchoolMask.All, true);
			else
				absorbAmount = dmgInfo.GetDamage();

			//201657 - The damager
			caster.CastSpell(caster, 201657, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)absorbAmount));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcAbsorb, 0, AuraType.SchoolAbsorb));
			AuraEffects.Add(new AuraEffectAbsorbHandler(HandleAbsorb, 0));
		}
	}
}