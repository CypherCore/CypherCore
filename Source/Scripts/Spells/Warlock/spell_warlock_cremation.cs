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
	// 212282 -
	[SpellScript(212282)]
	public class spell_warlock_cremation : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
		{
			PreventDefaultAction();
			var caster = GetCaster();
			var target = eventInfo.GetActionTarget();

			if (caster == null || target == null)
				return;

			switch (eventInfo.GetDamageInfo().GetSpellInfo().Id)
			{
				case WarlockSpells.SHADOWBURN:
				case WarlockSpells.CONFLAGRATE:
					caster.CastSpell(target, GetSpellInfo().GetEffect(0).TriggerSpell, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)aurEff.GetAmount()));

					break;
				case WarlockSpells.INCINERATE:
					caster.CastSpell(target, WarlockSpells.IMMOLATE_DOT, true);

					break;
			}
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
		}
	}
}