// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
	// 205178 - Soul Effigy target
	[SpellScript(205178)]
	public class spell_warlock_soul_effigy_target : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void PeriodicTick(AuraEffect UnnamedParameter)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			if (!caster.VariableStorage.Exist("Spells.SoulEffigyGuid"))
			{
				Remove();

				return;
			}

			var guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyGuid", ObjectGuid.Empty);

			if (!ObjectAccessor.Instance.GetUnit(caster, guid))
				Remove();
		}

		private void OnRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster == null)
				return;

			var guid = caster.VariableStorage.GetValue<ObjectGuid>("Spells.SoulEffigyGuid", ObjectGuid.Empty);

			var effigy = ObjectAccessor.Instance.GetUnit(caster, guid);

			if (effigy != null)
				effigy.ToTempSummon().DespawnOrUnsummon();
		}

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();
			var target = GetTarget();

			if (caster == null || target == null)
				return;

			caster.VariableStorage.Set("Spells.SoulEffigyTargetGuid", target.GetGUID());
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
			AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 0, AuraType.Dummy));
		}
	}
}