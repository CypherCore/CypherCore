using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(257538)]
public class spell_mage_ebonbolt_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects => new List<ISpellEffect>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(MageSpells.SPELL_MAGE_SPLITTING_ICE);
	}

	private void DoEffectHitTarget(uint UnnamedParameter)
	{
		Unit       hitUnit       = GetHitUnit();
		ObjectGuid primaryTarget = GetCaster().VariableStorage.GetValue<ObjectGuid>("explTarget", default);
		int        damage        = GetHitDamage();
		if (hitUnit == null || primaryTarget == default)
		{
			return;
		}

		int eff1 = Global.SpellMgr.GetSpellInfo(MageSpells.SPELL_MAGE_SPLITTING_ICE, Difficulty.None).GetEffect(1).CalcValue();
		if (eff1 != 0)
		{
			if (hitUnit.GetGUID() != primaryTarget)
			{
				SetHitDamage(MathFunctions.CalculatePct(damage, eff1));
			}
		}

	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}