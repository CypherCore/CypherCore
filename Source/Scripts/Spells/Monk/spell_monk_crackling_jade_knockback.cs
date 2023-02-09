using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(117962)]
public class spell_monk_crackling_jade_knockback : SpellScript, ISpellAfterHit
{


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return Global.SpellMgr.GetSpellInfo(CracklingJade.SPELL_MONK_CRACKLING_JADE_LIGHTNING_KNOCK_BACK, Difficulty.None) != null;
	}

	public void AfterHit()
	{
		Unit target = GetHitUnit();
		Unit caster = GetCaster();
		if (caster != null && target != null && caster.HasAura(CracklingJade.SPELL_MONK_CRACKLING_JADE_LIGHTNING_TALENT))
		{
			caster.CastSpell(target, CracklingJade.SPELL_MONK_CRACKLING_JAD_LIGHTNING_TALENT_SPEED, true);
		}
	}
}