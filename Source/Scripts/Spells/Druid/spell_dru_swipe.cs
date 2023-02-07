using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(106785)]
public class spell_dru_swipe : SpellScript, IHasSpellEffects
{
	private bool _awardComboPoint = true;

	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();


	private void HandleOnHit(uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		Unit target = GetHitUnit();
		if (caster == null || target == null)
		{
			return;
		}

		int damage      = GetHitDamage();
		var casterLevel = caster.GetLevelForTarget(caster);

		// This prevent awarding multiple Combo Points when multiple targets hit with Swipe AoE
		if (_awardComboPoint)
		{
			// Awards the caster 1 Combo Point (get value from the spell data)
			caster.ModifyPower(PowerType.ComboPoints, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SWIPE_CAT, Difficulty.None).GetEffect(0).BasePoints);
		}

		// If caster is level >= 44 and the target is bleeding, deals 20% increased damage (get value from the spell data)
		if ((casterLevel >= 44) && target.HasAuraState(AuraStateType.Bleed))
		{
			MathFunctions.AddPct(ref damage, Global.SpellMgr.GetSpellInfo(DruidSpells.SPELL_DRUID_SWIPE_CAT, Difficulty.None).GetEffect(1).BasePoints);
		}

		SetHitDamage(damage);
		_awardComboPoint = false;
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleOnHit, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
	}

}