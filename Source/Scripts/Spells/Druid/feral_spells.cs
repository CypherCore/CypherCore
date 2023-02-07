using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Druid;

[Script]
public class feral_spells : ScriptObjectAutoAdd, IPlayerOnLogin
{
	public Class PlayerClass { get { return Class.Druid; } }

	public feral_spells() : base("feral_spells")
	{
	}

	public void OnLogin(Player player)
	{
		if (player.GetPrimarySpecialization() != TalentSpecialization.DruidCat)
		{
			return;
		}

		if (player.GetLevel() >= 5 && !player.HasSpell(DruidSpells.SPELL_DRUID_SHRED))
		{
			player.LearnSpell(DruidSpells.SPELL_DRUID_SHRED, false);
		}

		if (player.GetLevel() >= 20 && !player.HasSpell(DruidSpells.SPELL_DRUID_RIP))
		{
			player.LearnSpell(DruidSpells.SPELL_DRUID_RIP, false);
		}

		if (player.GetLevel() >= 24 && !player.HasSpell(DruidSpells.SPELL_DRUID_RAKE))
		{
			player.LearnSpell(DruidSpells.SPELL_DRUID_RAKE, false);
		}

		if (player.GetLevel() >= 32 && !player.HasSpell(DruidSpells.SPELL_DRUID_FEROCIOUS_BITE))
		{
			player.LearnSpell(DruidSpells.SPELL_DRUID_FEROCIOUS_BITE, false);
		}
	}
}