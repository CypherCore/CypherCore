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
	//157504 - Cloudburst Totem
	[SpellScript(157504)]
	public class spell_sha_cloudburst_effect : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		private void OnProc(AuraEffect p_AurEff, ProcEventInfo p_EventInfo)
		{
			PreventDefaultAction();

			var l_HealInfo = p_EventInfo.GetHealInfo();

			if (l_HealInfo == null)
				return;

			if (Global.SpellMgr.GetSpellInfo(TotemSpells.TOTEM_CLOUDBURST, Difficulty.None) != null)
			{
				var l_SpellInfo = Global.SpellMgr.GetSpellInfo(TotemSpells.TOTEM_CLOUDBURST, Difficulty.None);
				GetEffect((byte)p_AurEff.GetEffIndex()).SetAmount(p_AurEff.GetAmount() + (int)MathFunctions.CalculatePct(l_HealInfo.GetHeal(), l_SpellInfo.GetEffect(0).BasePoints));
			}
		}

		private void OnRemove(AuraEffect p_AurEff, AuraEffectHandleModes UnnamedParameter)
		{
			var l_Owner = GetOwner().ToUnit();

			if (l_Owner != null)
			{
				double l_Amount = p_AurEff.GetAmount();

				if (p_AurEff.GetAmount() != 0)
				{
					l_Owner.CastSpell(l_Owner, TotemSpells.TOTEM_CLOUDBURST, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)l_Amount));
					GetEffect((byte)p_AurEff.GetEffIndex()).SetAmount(0);
				}
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
			AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}
}