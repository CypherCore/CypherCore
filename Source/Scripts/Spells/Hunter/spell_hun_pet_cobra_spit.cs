using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Hunter;

[SpellScript(206685)]
public class spell_hun_pet_cobra_spit : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private void HandleDamage(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		Unit owner = caster.GetOwner();
		if (owner == null)
		{
			return;
		}

		Unit target = GetExplTargetUnit();
		if (target == null)
		{
			return;
		}

		// (1 + AP * 0,2)
		var dmg = 1 + owner.m_unitData.RangedAttackPower * 0.2f;

		dmg = caster.SpellDamageBonusDone(target, GetSpellInfo(), dmg, DamageEffectType.Direct, GetEffectInfo(0));
		dmg = target.SpellDamageBonusTaken(caster, GetSpellInfo(), dmg, DamageEffectType.Direct);

		SetHitDamage(dmg);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleDamage, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}