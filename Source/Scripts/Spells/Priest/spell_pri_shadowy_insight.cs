using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(162452)]
public class spell_pri_shadowy_insight : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(PriestSpells.SPELL_PRIEST_MIND_BLAST);
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		GetTarget().GetSpellHistory().ResetCharges(Global.SpellMgr.AssertSpellInfo(PriestSpells.SPELL_PRIEST_MIND_BLAST, Difficulty.None).ChargeCategoryId);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}