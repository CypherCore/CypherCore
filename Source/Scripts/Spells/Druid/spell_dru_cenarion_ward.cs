using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Druid;

[SpellScript(102351)]
public class spell_dru_cenarion_ward : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects => new();

	private struct Spells
	{
		public static readonly uint SPELL_DRUID_CENARION_WARD_TRIGGERED = 102352;
	}

	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(Spells.SPELL_DRUID_CENARION_WARD_TRIGGERED);
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		if (!GetCaster() || !eventInfo.GetActionTarget())
			return;

		GetCaster().CastSpell(eventInfo.GetActionTarget(), Spells.SPELL_DRUID_CENARION_WARD_TRIGGERED, true);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}
}