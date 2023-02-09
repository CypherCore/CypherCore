using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(122470)]
public class spell_monk_touch_of_karma : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect aurEff, ref int amount, ref bool UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster != null)
		{
			int effInfo = GetAura().GetSpellInfo().GetEffect(2).CalcValue();
			if (GetAura().GetSpellInfo().GetEffect(2).CalcValue() != 0)
			{
				amount = (int)caster.CountPctFromMaxHealth(effInfo);

				aurEff.SetAmount(amount);
			}
		}
	}

	private void OnAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref uint UnnamedParameter)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		foreach (AuraApplication aurApp in caster.GetAppliedAuras().LookupByKey(MonkSpells.SPELL_MONK_TOUCH_OF_KARMA))
		{
			if (aurApp.GetTarget() != caster)
			{
				var periodicDamage = dmgInfo.GetDamage() / Global.SpellMgr.GetSpellInfo(MonkSpells.SPELL_MONK_TOUCH_OF_KARMA_DAMAGE, Difficulty.None).GetMaxTicks();
				//  periodicDamage += int32(aurApp->GetTarget()->GetRemainingPeriodicAmount(GetCasterGUID(), SPELL_MONK_TOUCH_OF_KARMA_DAMAGE, AuraType.PeriodicDamage));
				caster.CastSpell(aurApp.GetTarget(), MonkSpells.SPELL_MONK_TOUCH_OF_KARMA_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)periodicDamage).SetTriggeringAura(aurEff));
				if (caster.HasAura(MonkSpells.SPELL_GOOD_KARMA_TALENT))
				{
					caster.CastSpell(caster, MonkSpells.SPELL_GOOD_KARMA_TALENT_HEAL, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, (int)periodicDamage).SetTriggeringAura(aurEff));
				}
			}
		}
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetCaster().CastSpell(GetCaster(), MonkSpells.SPELL_MONK_TOUCH_OF_KARMA_BUFF, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		caster.RemoveAura(MonkSpells.SPELL_MONK_TOUCH_OF_KARMA_BUFF);
		foreach (AuraApplication aurApp in caster.GetAppliedAuras().LookupByKey(MonkSpells.SPELL_MONK_TOUCH_OF_KARMA))
		{
			Aura targetAura = aurApp.GetBase();
			if (targetAura != null)
			{
				targetAura.Remove();
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
		AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 1));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}