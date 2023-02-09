using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(125883)]
public class spell_monk_zen_flight_check_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Load()
	{
		return GetCaster() && GetCaster().GetTypeId() == TypeId.Player;
	}

	private void CalculateAmount(AuraEffect UnnamedParameter, ref int amount, ref bool UnnamedParameter2)
	{
		if (!GetCaster())
		{
			return;
		}

		Player caster = GetCaster().ToPlayer();
		if (caster != null)
		{
			if (caster.GetSkillValue(SkillType.Riding) >= 375)
			{
				amount = 310;
			}
		}
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.ModIncreaseVehicleFlightSpeed));
	}
}