using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(228260)]
public class spell_pri_void_eruption : SpellScript, IHasSpellEffects, ISpellOnCast, ISpellOnTakePower
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_VOID_ERUPTION, PriestSpells.SPELL_PRIEST_VOID_ERUPTION_DAMAGE);
	}

	private void FilterTargets(List<WorldObject> targets)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		targets.RemoveIf((WorldObject target) =>
		                 {
			                 Unit targ = target.ToUnit();
			                 if (targ == null)
			                 {
				                 return true;
			                 }
			                 return !(targ.HasAura(PriestSpells.SPELL_PRIEST_SHADOW_WORD_PAIN, caster.GetGUID()) || targ.HasAura(PriestSpells.SPELL_PRIEST_VAMPIRIC_TOUCH, caster.GetGUID()));
		                 });
	}

	public void TakePower(SpellPowerCost powerCost)
	{
		powerCost.Amount = 0;
	}

	private void HandleDummy(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		var spellid = RandomHelper.RandShort() % 2; //there are two animations which should be random
		caster.CastSpell(target, PriestSpells.SPELL_PRIEST_VOID_ERUPTION_DAMAGE + spellid, true);
	}

	public void OnCast()
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_VOIDFORM_BUFFS, true);
		if (!caster.HasAura(PriestSpells.SPELL_PRIEST_SHADOWFORM_STANCE))
		{
			caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_SHADOWFORM_STANCE, true);
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 0, Targets.UnitDestAreaEnemy));
		SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}
}