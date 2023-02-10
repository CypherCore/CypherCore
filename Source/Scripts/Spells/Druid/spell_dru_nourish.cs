using Game.Scripting;

namespace Scripts.Spells.Druid;

[SpellScript(50464)]
public class spell_dru_nourish : SpellScript
{
	private const int SPELL_DRUID_NOURISH_PASSIVE = 203374;
	private const int SPELL_DRUID_REJUVENATION = 774;

	public void OnHit()
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var target = GetHitUnit();

			if (target != null)
				if (caster.HasAura(SPELL_DRUID_NOURISH_PASSIVE))
					caster.CastSpell(target, SPELL_DRUID_REJUVENATION, true);
		}
	}
}