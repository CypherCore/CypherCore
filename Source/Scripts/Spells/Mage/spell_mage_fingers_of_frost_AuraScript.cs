using System.Collections.Generic;
using Framework.Constants;
using Framework.Dynamic;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 112965 - Fingers of Frost
internal class spell_mage_fingers_of_frost_AuraScript : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.FingersOfFrost);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckFrostboltProc, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckFrozenOrbProc, 1, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(Trigger, 0, AuraType.Dummy, AuraScriptHookType.EffectAfterProc));
		AuraEffects.Add(new AuraEffectProcHandler(Trigger, 1, AuraType.Dummy, AuraScriptHookType.EffectAfterProc));
	}

	private bool CheckFrostboltProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Mage, new FlagArray128(0, 0x2000000, 0, 0)) && RandomHelper.randChance(aurEff.GetAmount());
	}

	private bool CheckFrozenOrbProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo() != null && eventInfo.GetSpellInfo().IsAffected(SpellFamilyNames.Mage, new FlagArray128(0, 0, 0x80, 0)) && RandomHelper.randChance(aurEff.GetAmount());
	}

	private void Trigger(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		eventInfo.GetActor().CastSpell(GetTarget(), MageSpells.FingersOfFrost, new CastSpellExtraArgs(aurEff));
	}
}