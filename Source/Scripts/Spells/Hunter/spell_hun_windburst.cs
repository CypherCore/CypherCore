using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(204147)]
public class spell_hun_windburst : SpellScript, ISpellAfterHit
{
	public void AfterHit()
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			Unit target = GetHitUnit();

			if (target == null)
			{
				return;
			}

			caster.CastSpell(new Position(target.GetPositionX(), target.GetPositionY(), target.GetPositionZ()), 204475, true);
		}
	}
}