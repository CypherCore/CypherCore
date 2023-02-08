using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(53209)]
public class spell_hun_chimera_shot : SpellScript, ISpellOnHit
{
	public override bool Load()
	{
		return GetCaster().GetTypeId() == TypeId.Player;
	}

	public void OnHit()
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (RandomHelper.URand(0, 1) == 1)
		{
			caster.CastSpell(target, 171454, true);
		}
		else
		{
			caster.CastSpell(target, 171457, true);
		}
	}
}