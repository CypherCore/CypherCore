// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
			return;

		var spellInfo = spell.GetSpellInfo();

		if (player.HasAura(StormEarthAndFireSpells.SEF) && !spellInfo.IsPositive())
		{
			var target = ObjectAccessor.Instance.GetUnit(player, player.GetTarget());

			if (target != null)
			{
				var fireSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);

				if (fireSpirit != null)
				{
					fireSpirit.SetFacingToObject(target, true);
					fireSpirit.CastSpell(target, spellInfo.Id, true);
				}

				var earthSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);

				if (earthSpirit != null)
				{
					earthSpirit.SetFacingToObject(target, true);
					earthSpirit.CastSpell(target, spellInfo.Id, true);
				}
			}
		}

		if (player.HasAura(StormEarthAndFireSpells.SEF) && spellInfo.IsPositive())
		{
			var GetTarget = player.GetSelectedUnit();

			if (GetTarget != null)
			{
				if (!GetTarget.IsFriendlyTo(player))
					return;

				var fireSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_FIRE_SPIRIT);

				if (fireSpirit != null)
				{
					fireSpirit.SetFacingToObject(GetTarget, true);
					fireSpirit.CastSpell(GetTarget, spellInfo.Id, true);
				}

				var earthSpirit = player.GetSummonedCreatureByEntry(StormEarthAndFireSpells.NPC_EARTH_SPIRIT);

				if (earthSpirit != null)
				{
					earthSpirit.SetFacingToObject(GetTarget, true);
					earthSpirit.CastSpell(GetTarget, spellInfo.Id, true);
				}
			}
		}
	}
}