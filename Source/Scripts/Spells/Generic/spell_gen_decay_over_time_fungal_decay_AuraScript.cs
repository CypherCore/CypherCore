using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 32065 - Fungal Decay
internal class spell_gen_decay_over_time_fungal_decay_AuraScript : AuraScript, IAuraCheckProc, IAuraOnProc, IHasAuraEffects
{
	public bool CheckProc(ProcEventInfo eventInfo)
	{
		return eventInfo.GetSpellInfo() == GetSpellInfo();
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(ModDuration, 0, AuraType.ModDecreaseSpeed, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
	}

	public void OnProc(ProcEventInfo info)
	{
		PreventDefaultAction();
		ModStackAmount(-1);
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void ModDuration(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		// only on actual reapply, not on stack decay
		if (GetDuration() == GetMaxDuration())
		{
			SetMaxDuration(Misc.AuraDuration);
			SetDuration(Misc.AuraDuration);
		}
	}
}