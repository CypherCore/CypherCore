using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 170374 - Earthen Rage (Passive)
[SpellScript(170374)]
public class spell_sha_earthen_rage_passive : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	private ObjectGuid _procTargetGuid;

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.EarthenRagePeriodic, ShamanSpells.EarthenRageDamage);
	}

	public bool CheckProc(ProcEventInfo procInfo)
	{
		return procInfo.GetSpellInfo() != null && procInfo.GetSpellInfo().Id != ShamanSpells.EarthenRageDamage;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleEffectProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public ObjectGuid GetProcTargetGuid()
	{
		return _procTargetGuid;
	}

	private void HandleEffectProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		_procTargetGuid = eventInfo.GetProcTarget().GetGUID();
		eventInfo.GetActor().CastSpell(eventInfo.GetActor(), ShamanSpells.EarthenRagePeriodic, true);
	}
}