// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.Warrior
{
	//152278 Anger Management
	[Script]
	public class anger_management : ScriptObjectAutoAdd, IPlayerOnSpellCast,
	                                IClassRescriction,
	                                IPlayerOnLogout,
	                                IPlayerOnMapChanged,
	                                IPlayerOnDeath

	{
		public Class PlayerClass { get; } = Class.Warrior;

		public static Dictionary<Player, int> RageSpent = new();

		public anger_management() : base("anger_management")
		{
		}

		public void OnSpellCast(Player player, Spell spell, bool UnnamedParameter)
		{
			var power = spell.GetPowerCost().FirstOrDefault(p => p.Power == PowerType.Rage);

			if (power == null)
				return;

			if (!RageSpent.TryGetValue(player, out var rageSpent))
				RageSpent[player] = 0;

			RageSpent[player] += power.Amount;
			var anger = player.GetAura(WarriorSpells.ANGER_MANAGEMENT);

			if (anger != null)
			{
				var spec = player.GetPrimarySpecialization();

				//int32 mod = powerCost->Amount * 100 / anger->GetEffect(EFFECT_0).GetAmount();
				//int32 mod = std::max(powerCost->Amount * 100, anger->GetEffect(EFFECT_0).GetAmount()) / 2;
				if (spec == TalentSpecialization.WarriorArms)
				{
					var mod = CalculateCount(player, anger.GetEffect(0).GetAmount());
					var ts  = TimeSpan.FromSeconds(-1 * mod);
					player.GetSpellHistory().ModifyCooldown(262161, ts); // Warbreaker
					player.GetSpellHistory().ModifyCooldown(46924, ts);  // Bladestorm
					player.GetSpellHistory().ModifyCooldown(227847, ts); // Bladestorm
					player.GetSpellHistory().ModifyCooldown(167105, ts); // Colossus Smash
				}
				else if (spec == TalentSpecialization.WarriorFury)
				{
					var mod = CalculateCount(player, anger.GetEffect(2).GetAmount());
					var ts  = TimeSpan.FromSeconds(-1 * mod);
					player.GetSpellHistory().ModifyCooldown(1719, ts);   // Recklessness
					player.GetSpellHistory().ModifyCooldown(152277, ts); // Ravenger
				}
				else if (spec == TalentSpecialization.WarriorProtection)
				{
					var mod = CalculateCount(player, anger.GetEffect(1).GetAmount());
					var ts  = TimeSpan.FromSeconds(-1 * mod);
					player.GetSpellHistory().ModifyCooldown(107574, ts); // Avatar
					player.GetSpellHistory().ModifyCooldown(12975, ts);  // Last Stand
					player.GetSpellHistory().ModifyCooldown(871, ts);    // Shield Wall
					player.GetSpellHistory().ModifyCooldown(1160, ts);   // Demoralizing Shout
				}
			}
		}

		public int CalculateCount(Player p, double rage)
		{
			if (RageSpent.TryGetValue(p, out var spentRage))
			{
				var rageMod = (int)Math.Floor(spentRage / rage);

				if (rageMod > 0)
				{
					RageSpent[p] = (rageMod * (int)rage) - spentRage;

					return rageMod;
				}
			}

			return 0;
		}

		public void OnLogout(Player player)
		{
			RageSpent.Remove(player);
		}

		public void OnMapChanged(Player player)
		{
			if (RageSpent.ContainsKey(player))
				RageSpent[player] = 0;
		}

		public void OnDeath(Player player)
		{
			RageSpent[player] = 0;
		}
	}
}