using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(28305)]
public class spell_pri_mana_leech : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public spell_pri_mana_leech()
	{
		_procTarget = null;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(PriestSpells.SPELL_PRIEST_MANA_LEECH_PROC, Difficulty.None) != null)
		{
			return false;
		}
		return true;
	}

	public bool CheckProc(ProcEventInfo UnnamedParameter)
	{
		_procTarget = GetTarget().GetOwner();
		return _procTarget != null;
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo UnnamedParameter)
	{
		PreventDefaultAction();
		GetTarget().CastSpell(_procTarget, PriestSpells.SPELL_PRIEST_MANA_LEECH_PROC, aurEff);
	}

	public override void Register()
	{

		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private Unit _procTarget;
}