using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 70808 - Item - Shaman T10 Restoration 4P Bonus
internal class spell_sha_t10_restoration_4p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.ChainedHeal);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		HealInfo healInfo = eventInfo.GetHealInfo();

		if (healInfo == null ||
		    healInfo.GetHeal() == 0)
			return;

		SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ShamanSpells.ChainedHeal, GetCastDifficulty());
		int       amount    = (int)MathFunctions.CalculatePct(healInfo.GetHeal(), aurEff.GetAmount());
		amount /= (int)spellInfo.GetMaxTicks();

		Unit caster = eventInfo.GetActor();
		Unit target = eventInfo.GetProcTarget();

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell(target, ShamanSpells.ChainedHeal, args);
	}
}