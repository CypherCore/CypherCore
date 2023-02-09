using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[SpellScript(205708)]
public class spell_mage_chilled : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private void HandleApply(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (caster.HasAura(MageSpells.SPELL_MAGE_BONE_CHILLING))
			//@TODO REDUCE BONE CHILLING DAMAGE PER STACK TO 0.5% from 1%
			caster.CastSpell(caster, MageSpells.SPELL_MAGE_BONE_CHILLING_BUFF, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleApply, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask));
	}
}