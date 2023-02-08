using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(27827)]
public class spell_pri_spirit_of_redemption_form : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	private struct eSpells
	{
		public const uint SpiritOfRedemptionImmunity = 62371;
		public const uint SpiritOfRedemptionForm = 27795;
	}

	private void AfterRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		Unit l_Target = GetTarget();

		l_Target.RemoveAura(eSpells.SpiritOfRedemptionForm);
		l_Target.RemoveAura(eSpells.SpiritOfRedemptionImmunity);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.WaterBreathing, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}
}