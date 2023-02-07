using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(203975)]
public class spell_druid_earthwarden_triggered : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private struct Spells
	{
		public static uint SPELL_DRUID_EARTHWARDEN = 203974;
		public static uint SPELL_DRUID_EARTHWARDEN_TRIGGERED = 203975;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_EARTHWARDEN, Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED);
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		amount = -1;
	}

	private void Absorb(AuraEffect auraEffect, DamageInfo dmgInfo, ref uint absorbAmount)
	{
		if (dmgInfo.GetDamageType() == DamageEffectType.Direct)
		{
			SpellInfo earthwarden = Global.SpellMgr.AssertSpellInfo(Spells.SPELL_DRUID_EARTHWARDEN, Difficulty.None);

			absorbAmount = MathFunctions.CalculatePct(dmgInfo.GetDamage(), earthwarden.GetEffect(0).BasePoints);
			GetCaster().RemoveAurasDueToSpell(Spells.SPELL_DRUID_EARTHWARDEN_TRIGGERED);
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new EffectCalcAmountHandler(CalculateAmount, 0, AuraType.SchoolAbsorb));
		AuraEffects.Add(new EffectAbsorbHandler(Absorb, 0));
	}
}