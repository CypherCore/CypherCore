using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script("spell_item_unstable_power", ItemSpellIds.UnstablePowerAuraStack)]
[Script("spell_item_restless_strength", ItemSpellIds.RestlessStrengthAuraStack)]
internal class spell_item_zandalarian_charm : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	private readonly uint _spellId;

	public spell_item_zandalarian_charm(uint SpellId)
	{
		_spellId = SpellId;
	}

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(_spellId);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		SpellInfo spellInfo = eventInfo.GetSpellInfo();

		if (spellInfo != null)
			if (spellInfo.Id != ScriptSpellId)
				return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleStackDrop, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleStackDrop(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		GetTarget().RemoveAuraFromStack(_spellId);
	}
}