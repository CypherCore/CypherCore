using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script]
public class playerScript_monk_earth_fire_storm : ScriptObjectAutoAdd, IPlayerOnSpellCast
{
	public Class PlayerClass => Class.Monk;
	public playerScript_monk_earth_fire_storm() : base("playerScript_monk_earth_fire_storm")
	{
	}

	public void OnSpellCast(Player player, Spell spell, bool re)
	{
		if (player.GetClass() != Class.Monk)
		{
			return;
		}

		SpellInfo spellInfo = spell.GetSpellInfo();
		if (player.HasAura(StormEarthAndFireSpells.SPELL_MONK_SEF) && !spellInfo.IsPositive())
		{
			Unit target = ObjectAccessor.Instance.GetUnit(player, player.GetTarget());
			if (target != null)
			{
				Creature fireSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);
				if (fireSpirit != null)
				{
					fireSpirit.SetFacingToObject(target, true);
					fireSpirit.CastSpell(target, spellInfo.Id, true);
				}
				Creature earthSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);
				if (earthSpirit != null)
				{
					earthSpirit.SetFacingToObject(target, true);
					earthSpirit.CastSpell(target, spellInfo.Id, true);
				}
			}
		}
		if (player.HasAura(StormEarthAndFireSpells.SPELL_MONK_SEF) && spellInfo.IsPositive())
		{
			Unit GetTarget = player.GetSelectedUnit();
			if (GetTarget != null)
			{
				if (!GetTarget.IsFriendlyTo(player))
				{
					return;
				}

				Creature fireSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);
				if (fireSpirit != null)
				{
					fireSpirit.SetFacingToObject(GetTarget, true);
					fireSpirit.CastSpell(GetTarget, spellInfo.Id, true);
				}
				Creature earthSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);
				if (earthSpirit != null)
				{
					earthSpirit.SetFacingToObject(GetTarget, true);
					earthSpirit.CastSpell(GetTarget, spellInfo.Id, true);
				}
			}
		}
	}
}