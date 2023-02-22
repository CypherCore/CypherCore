// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

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
	public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

	private void CalculateAmount(AuraEffect aurEff, ref double amount, ref bool UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
		{
			var effInfo = GetAura().GetSpellInfo().GetEffect(2).CalcValue();

			if (GetAura().GetSpellInfo().GetEffect(2).CalcValue() != 0)
			{
				amount = caster.CountPctFromMaxHealth(effInfo);

				aurEff.SetAmount(amount);
			}
		}
	}

	private void OnAbsorb(AuraEffect aurEff, DamageInfo dmgInfo, ref double UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		foreach (var aurApp in caster.GetAppliedAurasQuery().HasSpellId(MonkSpells.TOUCH_OF_KARMA).GetResults())
			if (aurApp.GetTarget() != caster)
			{
				var periodicDamage = dmgInfo.GetDamage() / Global.SpellMgr.GetSpellInfo(MonkSpells.TOUCH_OF_KARMA_DAMAGE, Difficulty.None).GetMaxTicks();
				//  periodicDamage += int32(aurApp->GetTarget()->GetRemainingPeriodicAmount(GetCasterGUID(), TOUCH_OF_KARMA_DAMAGE, AuraType.PeriodicDamage));
				caster.CastSpell(aurApp.GetTarget(), MonkSpells.TOUCH_OF_KARMA_DAMAGE, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, periodicDamage).SetTriggeringAura(aurEff));

				if (caster.HasAura(MonkSpells.GOOD_KARMA_TALENT))
					caster.CastSpell(caster, MonkSpells.GOOD_KARMA_TALENT_HEAL, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, periodicDamage).SetTriggeringAura(aurEff));
			}
	}

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		GetCaster().CastSpell(GetCaster(), MonkSpells.TOUCH_OF_KARMA_BUFF, true);
	}

	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.RemoveAura(MonkSpells.TOUCH_OF_KARMA_BUFF);

		foreach (var aurApp in caster.GetAppliedAurasQuery().HasSpellId(MonkSpells.TOUCH_OF_KARMA).GetResults())
		{
			var targetAura = aurApp.GetBase();

			if (targetAura != null)
				targetAura.Remove();
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