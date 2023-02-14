// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 233490 - Unstable Affliction dispel
	[SpellScript(233490)]
	public class spell_warlock_unstable_affliction_dispel : AuraScript, IHasAuraEffects, IAuraOnDispel
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		public void OnDispel(DispelInfo dispelInfo)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var dispeller = dispelInfo.GetDispeller().ToUnit();

			if (dispeller != null)
			{
				var damage = GetAura().GetEffect(0).GetAmount() * 4;
				var args   = new CastSpellExtraArgs();
				args.AddSpellMod(SpellValueMod.BasePoint0, (int)damage);
				args.SetTriggerFlags(TriggerCastFlags.FullMask);
				caster.CastSpell(dispeller, WarlockSpells.UNSTABLE_AFFLICTION_DISPEL, args);
			}
		}

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();
			var target = GetUnitOwner();

			if (caster == null || target == null || !caster.ToPlayer())
				return;

			if (caster.HasAura(WarlockSpells.UNSTABLE_AFFLICTION_RANK2))
				if (GetTargetApplication() != null && GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Death)
				{
					if (caster.VariableStorage.Exist("_uaLockout"))
						return;

					caster.CastSpell(caster, WarlockSpells.UNSTABLE_AFFLICTION_ENERGIZE, true);

					caster.VariableStorage.Set("_uaLockout", 0);


					caster.m_Events.AddEventAtOffset(() => { caster.VariableStorage.Remove("_uaLockout"); }, TimeSpan.FromMilliseconds(100));
				}

			// When Unstable Affliction expires, it has a 6% chance to reapply itself.
			if (GetTargetApplication() != null && GetTargetApplication().GetRemoveMode() == AuraRemoveMode.Expire)
				if (RandomHelper.randChance(caster.GetAuraEffectAmount(WarlockSpells.FATAL_ECHOES, 0)))
					caster.m_Events.AddEventAtOffset(() => { caster.CastSpell(target, GetSpellInfo().Id, true); }, TimeSpan.FromMilliseconds(100));
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}
}