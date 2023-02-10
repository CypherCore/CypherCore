using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(26679)]
public class spell_rog_deadly_throw_SpellScript : SpellScript, ISpellOnHit
{
	public void OnHit()
	{
		var target = GetHitUnit();

		if (target != null)
		{
			var caster = GetCaster().ToPlayer();

			if (caster != null)
				if (caster.GetPower(PowerType.ComboPoints) >= 5)
					caster.CastSpell(target, 137576, true);
		}
	}
}