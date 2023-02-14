// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.DeathKnight;

[Script]
public class spell_dk_runic_empowerment : ScriptObjectAutoAdd, IPlayerOnModifyPower
{
	public spell_dk_runic_empowerment() : base("spell_dk_runic_empowerment")
	{
	}

	public struct eSpells
	{
		public const uint RunicEmpowerment = 81229;
	}

	public void OnModifyPower(Player p_Player, PowerType p_Power, int p_OldValue, ref int p_NewValue, bool p_Regen)
	{
		if (p_Player.GetClass() != Class.Deathknight || p_Power != PowerType.RunicPower || p_Regen || p_NewValue > p_OldValue)
			return;

		var l_RunicEmpowerment = p_Player.GetAuraEffect(eSpells.RunicEmpowerment, 0);

		if (l_RunicEmpowerment != null)
		{
			/// 1.00% chance per Runic Power spent
			var l_Chance = (l_RunicEmpowerment.GetAmount() / 100.0f);

			if (RandomHelper.randChance(l_Chance))
			{
				var l_LstRunesUsed = new List<byte>();

				for (byte i = 0; i < PlayerConst.MaxRunes; ++i)
					if (p_Player.GetRuneCooldown(i) != 0)
						l_LstRunesUsed.Add(i);

				if (l_LstRunesUsed.Count == 0)
					return;

				var l_RuneRandom = l_LstRunesUsed.SelectRandom();

				p_Player.SetRuneCooldown(l_RuneRandom, 0);
				p_Player.ResyncRunes();
			}
		}
	}
}