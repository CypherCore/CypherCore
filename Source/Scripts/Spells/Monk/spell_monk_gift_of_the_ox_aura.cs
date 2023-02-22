// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;

namespace Scripts.Spells.Monk;

[Script]
public class spell_monk_gift_of_the_ox_aura : ScriptObjectAutoAdd, IPlayerOnTakeDamage
{
    public Class PlayerClass { get; } = Class.Monk;
    public spell_monk_gift_of_the_ox_aura() : base("spell_monk_gift_of_the_ox_aura")
	{
	}

	public enum UsedSpells
	{
		HEALING_SPHERE_COOLDOWN = 224863
	}

	public List<uint> spellsToCast = new()
	                                 {
		                                 MonkSpells.GIFT_OF_THE_OX_AT_RIGHT,
		                                 MonkSpells.GIFT_OF_THE_OX_AT_LEFT
	                                 };

	public void OnPlayerTakeDamage(Player victim, double damage, SpellSchoolMask UnnamedParameter)
	{
		if (damage == 0 || victim == null)
			return;

		if (!victim.HasAura(MonkSpells.GIFT_OF_THE_OX_AURA))
			return;

		var spellToCast = spellsToCast[RandomHelper.IRand(0, (spellsToCast.Count - 1))];

		if (RandomHelper.randChance((0.75 * damage / victim.GetMaxHealth()) * (3 - 2 * (victim.GetHealthPct() / 100)) * 100))
			if (!victim.HasAura(UsedSpells.HEALING_SPHERE_COOLDOWN))
			{
				victim.CastSpell(victim, UsedSpells.HEALING_SPHERE_COOLDOWN, true);
				victim.CastSpell(victim, spellToCast, true);
			}
	}
}