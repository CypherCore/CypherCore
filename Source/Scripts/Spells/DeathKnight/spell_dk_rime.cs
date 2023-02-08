using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 59057 - Rime
internal class spell_dk_rime : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return spellInfo.GetEffects().Count > 1 && ValidateSpellInfo(DeathKnightSpells.FrostScythe);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraCheckEffectProcHandler(CheckProc, 0, AuraType.ProcTriggerSpell));
	}

	private bool CheckProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		float chance = (float)GetSpellInfo().GetEffect(1).CalcValue(GetTarget());

		if (eventInfo.GetSpellInfo().Id == DeathKnightSpells.FrostScythe)
			chance /= 2.0f;

		return RandomHelper.randChance(chance);
	}
}