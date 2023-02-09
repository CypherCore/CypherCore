using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 77756 - Lava Surge
internal class spell_sha_lava_surge : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LavaSurge, ShamanSpells.IgneousPotential);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProcChance, 0, AuraType.Dummy));
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private bool CheckProcChance(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		var procChance       = aurEff.GetAmount();
		var igneousPotential = GetTarget().GetAuraEffect(ShamanSpells.IgneousPotential, 0);

		if (igneousPotential != null)
			procChance += igneousPotential.GetAmount();

		return RandomHelper.randChance(procChance);
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(GetTarget(), ShamanSpells.LavaSurge, true);
	}
}