using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 26169 - Oracle Healing Bonus
internal class spell_pri_aq_3p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.OracularHeal);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.GetActor();

		if (caster == eventInfo.GetProcTarget())
			return;

		var healInfo = eventInfo.GetHealInfo();

		if (healInfo == null ||
		    healInfo.GetHeal() == 0)
			return;

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(healInfo.GetHeal(), 10));
		caster.CastSpell(caster, PriestSpells.OracularHeal, args);
	}
}