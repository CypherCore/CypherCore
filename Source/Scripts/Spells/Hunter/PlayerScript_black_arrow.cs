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
		if (Player.HasSpell(HunterSpells.SPELL_HUNTER_BLACK_ARROW))
			if (Player.GetSpellHistory().HasCooldown(HunterSpells.SPELL_HUNTER_BLACK_ARROW))
				Player.GetSpellHistory().ResetCooldown(HunterSpells.SPELL_HUNTER_BLACK_ARROW, true);
	}

	public void OnPVPKill(Player killer, Player UnnamedParameter)
	{
		if (killer.HasSpell(HunterSpells.SPELL_HUNTER_BLACK_ARROW))
			if (killer.GetSpellHistory().HasCooldown(HunterSpells.SPELL_HUNTER_BLACK_ARROW))
				killer.GetSpellHistory().ResetCooldown(HunterSpells.SPELL_HUNTER_BLACK_ARROW, true);
	}
}