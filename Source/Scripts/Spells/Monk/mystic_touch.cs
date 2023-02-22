// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IPlayer;
using Game.Spells;

namespace Scripts.Spells.Monk;

[Script]
public class mystic_touch : ScriptObjectAutoAdd, IPlayerOnDealDamage
{
	public Class PlayerClass => Class.Monk;

	public mystic_touch() : base("mystic_touch")
	{
	}

	public void OnDamage(Player caster, Unit target, ref double damage, SpellInfo spellProto)
	{
		var player = caster.ToPlayer();

		if (player != null)
			if (player.GetClass() != Class.Monk)
				return;

		if (caster == null || target == null)
			return;

		if (target.HasAura(MonkSpells.MYSTIC_TOUCH_TARGET_DEBUFF))
			return;

		if (caster.HasAura(MonkSpells.MYSTIC_TOUCH) && !target.HasAura(MonkSpells.MYSTIC_TOUCH_TARGET_DEBUFF))
			if (caster.IsWithinMeleeRange(target))
				caster.CastSpell(MonkSpells.MYSTIC_TOUCH_TARGET_DEBUFF, true);
	}
}