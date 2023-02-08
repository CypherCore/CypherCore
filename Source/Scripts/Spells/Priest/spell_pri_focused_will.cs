using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Priest;

[SpellScript(45243)]
public class spell_pri_focused_will : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new List<IAuraEffectHandler>();

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		if (Global.SpellMgr.GetSpellInfo(PriestSpells.SPELL_PRIEST_FOCUSED_WILL_BUFF, Difficulty.None) != null)
		{
			return false;
		}
		return true;
	}

	private bool HandleProc(ProcEventInfo eventInfo)
	{
		Unit caster = GetCaster();
		if (caster == null)
		{
			return false;
		}

		if (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack || eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.OffAttack)
		{
			caster.CastSpell(caster, PriestSpells.SPELL_PRIEST_FOCUSED_WILL_BUFF, true);
			return true;
		}

		return false;
	}

	private void PreventAction(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		PreventDefaultAction();
	}

	public override void Register()
	{

		AuraEffects.Add(new AuraEffectProcHandler(PreventAction, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}
}