using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Rogue;

[SpellScript(196912)]
public class spell_rog_shadow_techniques_AuraScript : AuraScript, IHasAuraEffects, IAuraCheckProc
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetDamageInfo().GetAttackType() == WeaponAttackType.BaseAttack)
			return true;

		return false;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (RandomHelper.randChance(40))
			caster.CastSpell(caster, RogueSpells.SPELL_ROGUE_SHADOW_TENCHNIQUES_POWER, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}