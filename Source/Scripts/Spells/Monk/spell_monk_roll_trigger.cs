using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(107427)]
public class spell_monk_roll_trigger : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private void CalcSpeed(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (caster.HasAura(MonkSpells.SPELL_MONK_ENHANCED_ROLL))
		{
			amount = 277;
		}
	}

	private void CalcSpeed2(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (!caster.HasAura(MonkSpells.SPELL_MONK_ENHANCED_ROLL))
		{
			return;
		}

		amount = 377;
	}

	private void SendAmount(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return;
		}

		if (!caster.HasAura(MonkSpells.SPELL_MONK_ENHANCED_ROLL))
		{
			return;
		}

		Aura aur = GetAura();
		if (aur == null)
		{
			return;
		}

		aur.SetMaxDuration(600);
		aur.SetDuration(600);

		AuraApplication aurApp = GetAura().GetApplicationOfTarget(caster.GetGUID());
		if (aurApp != null)
		{
			aurApp.ClientUpdate();
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed, 0, AuraType.ModSpeedNoControl));
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalcSpeed2, 2, AuraType.ModMinimumSpeed));
		AuraEffects.Add(new AuraEffectApplyHandler(SendAmount, 4, AuraType.UseNormalMovementSpeed, AuraEffectHandleModes.Real));
	}
}