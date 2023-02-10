using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 47758 - Penance (Channel Damage), 47757 - Penance (Channel Healing)
internal class spell_pri_penance_channeled : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.PowerOfTheDarkSide);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(HandleOnRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
	}

	private void HandleOnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		var caster = GetCaster();

		caster?.RemoveAura(PriestSpells.PowerOfTheDarkSide);
	}
}