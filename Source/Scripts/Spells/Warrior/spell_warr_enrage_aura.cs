// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// Enrage Aura - 184362
	[SpellScript(184362)]
	public class spell_warr_enrage_aura : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();

			if (caster != null)
				if (caster.HasAura(WarriorSpells.ENDLESS_RAGE))
					caster.CastSpell(null, WarriorSpells.ENDLESS_RAGE_GIVE_POWER, true);
		}

		private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var caster = GetCaster();
			caster.RemoveAurasDueToSpell(WarriorSpells.UNCHACKLED_FURY);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real));
			AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		}
	}
}