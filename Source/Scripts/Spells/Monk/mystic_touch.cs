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

	public void OnDamage(Player caster, Unit target, ref uint damage, SpellInfo spellProto)
	{
		Player player = caster.ToPlayer();
		if (player != null)
		{
			if (player.GetClass() != Class.Monk)
			{
				return;
			}
		}

		if (caster == null || target == null)
		{
			return;
		}

		if (target.HasAura(MonkSpells.SPELL_MONK_MYSTIC_TOUCH_TARGET_DEBUFF))
		{
			return;
		}

		if (caster.HasAura(MonkSpells.SPELL_MONK_MYSTIC_TOUCH) && !target.HasAura(MonkSpells.SPELL_MONK_MYSTIC_TOUCH_TARGET_DEBUFF))
		{
			if (caster.IsWithinMeleeRange(target))
			{
				caster.CastSpell(MonkSpells.SPELL_MONK_MYSTIC_TOUCH_TARGET_DEBUFF, true);
			}
		}
	}
}