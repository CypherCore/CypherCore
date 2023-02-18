// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Hunter;

[Script]
public class PlayerScript_black_arrow : ScriptObjectAutoAdd, IPlayerOnCreatureKill, IPlayerOnPVPKill
{
	public PlayerScript_black_arrow() : base("PlayerScript_black_arrow")
	{
	}

	public void OnCreatureKill(Player Player, Creature UnnamedParameter)
	{
		if (Player.HasSpell(HunterSpells.BLACK_ARROW))
			if (Player.GetSpellHistory().HasCooldown(HunterSpells.BLACK_ARROW))
				Player.GetSpellHistory().ResetCooldown(HunterSpells.BLACK_ARROW, true);
	}

	public void OnPVPKill(Player killer, Player UnnamedParameter)
	{
		if (killer.HasSpell(HunterSpells.BLACK_ARROW))
			if (killer.GetSpellHistory().HasCooldown(HunterSpells.BLACK_ARROW))
				killer.GetSpellHistory().ResetCooldown(HunterSpells.BLACK_ARROW, true);
	}
}