// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	// Ravager - 152277
	// Ravager - 228920
	[SpellScript(new uint[]
	             {
		             152277, 228920
	             })]
	public class aura_warr_ravager : AuraScript, IHasAuraEffects
	{
		public List<IAuraEffectHandler> AuraEffects => new();

		private void OnApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
		{
			var player = GetTarget().ToPlayer();

			if (player != null)
				if (player.GetPrimarySpecialization() == TalentSpecialization.WarriorProtection)
					player.CastSpell(player, WarriorSpells.RAVAGER_PARRY, true);
		}

		private void OnTick(AuraEffect UnnamedParameter)
		{
			var creature = GetTarget().GetSummonedCreatureByEntry(WarriorSpells.NPC_WARRIOR_RAVAGER);

			if (creature != null)
				GetTarget().CastSpell(creature.GetPosition(), WarriorSpells.RAVAGER_DAMAGE, true);
		}

		public override void Register()
		{
			AuraEffects.Add(new AuraEffectApplyHandler(this.OnApply, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask));
			AuraEffects.Add(new AuraEffectPeriodicHandler(this.OnTick, 2, AuraType.PeriodicDummy));
		}
	}
}