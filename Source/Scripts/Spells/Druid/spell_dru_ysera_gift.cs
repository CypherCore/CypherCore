using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(145108)]
public class spell_dru_ysera_gift : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandlePeriodic(AuraEffect aurEff)
	{
		var caster = GetCaster();

		if (caster == null || !caster.IsAlive())
			return;

		var amount = MathFunctions.CalculatePct(caster.GetMaxHealth(), aurEff.GetBaseAmount());
		var values = new CastSpellExtraArgs(TriggerCastFlags.FullMask);
		values.AddSpellMod(SpellValueMod.MaxTargets, 1);
		values.AddSpellMod(SpellValueMod.BasePoint0, (int)amount);

		if (caster.IsFullHealth())
			caster.CastSpell(caster, DruidSpells.SPELL_DRUID_YSERA_GIFT_RAID_HEAL, values);
		else
			caster.CastSpell(caster, DruidSpells.SPELL_DRUID_YSERA_GIFT_CASTER_HEAL, values);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(HandlePeriodic, 0, AuraType.PeriodicDummy));
	}
}