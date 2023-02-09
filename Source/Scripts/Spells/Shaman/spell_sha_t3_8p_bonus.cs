using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 28820 - Lightning Shield
internal class spell_sha_t3_8p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicTick, 1, AuraType.PeriodicTriggerSpell));
	}

	private void PeriodicTick(AuraEffect aurEff)
	{
		PreventDefaultAction();

		// Need remove self if Lightning Shield not active
		if (GetTarget().GetAuraEffect(AuraType.ProcTriggerSpell, SpellFamilyNames.Shaman, new FlagArray128(0x400), GetCaster().GetGUID()) == null)
			Remove();
	}
}