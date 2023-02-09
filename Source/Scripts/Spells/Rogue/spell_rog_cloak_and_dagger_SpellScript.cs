using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(new uint[] { 703, 1833, 8676 })]
public class spell_rog_cloak_and_dagger_SpellScript : SpellScript, ISpellCheckCast
{
	public SpellCastResult CheckCast()
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			if (caster.HasAuraType(AuraType.ModStealth))
			{
				if (caster.HasAura(138106))
				{
					Unit target = GetExplTargetUnit();
					if (target != null)
					{
						caster.CastSpell(target, 138916, true);
					}
				}
			}
		}

		return SpellCastResult.SpellCastOk;
	}
}