using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201453)]
public class spell_dh_metamorphosis_immunity : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	private void HandleRemove(AuraEffect UnnamedParameter, AuraEffectHandleModes UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		caster.CastSpell(caster, DemonHunterSpells.SPELL_DH_METAMORPHOSIS_STUN, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 1, AuraType.AbilityIgnoreAurastate, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}
}