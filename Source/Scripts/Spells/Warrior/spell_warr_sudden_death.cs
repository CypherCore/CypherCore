// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// 52437 - Sudden Death
	[Script]
	internal class spell_warr_sudden_death : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects { get; } = new();

		public override bool Validate(SpellInfo spellInfo)
		{
			return ValidateSpellInfo(WarriorSpells.COLOSSUS_SMASH);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply)); // correct?
		}

		private void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
		{
			// Remove cooldown on Colossus Smash
			var player = GetTarget().ToPlayer();

			if (player)
				player.GetSpellHistory().ResetCooldown(WarriorSpells.COLOSSUS_SMASH, true);
		}
	}
}