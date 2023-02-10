using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[SpellScript(81136)]
public class spell_dk_crimsom_scourge : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		var target = GetTarget();
		target.HasAura(DeathKnightSpells.SPELL_DK_BLOOD_PLAGUE);

		return true;
	}

	private void HandleProc(AuraEffect UnnamedParameter, ProcEventInfo UnnamedParameter2)
	{
		var caster = GetCaster();

		if (caster == null)
			return;

		if (RandomHelper.randChance(40))
			caster.CastSpell(caster, 81141, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}