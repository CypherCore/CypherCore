using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // 33510 - Health Restore
internal class spell_item_mark_of_conquest : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.MarkOfConquestEnergize);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.PeriodicTriggerSpell, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		if (eventInfo.GetTypeMask().HasFlag(ProcFlags.DealRangedAttack | ProcFlags.DealRangedAbility))
		{
			// in that case, do not cast heal spell
			PreventDefaultAction();
			// but mana instead
			eventInfo.GetActor().CastSpell((Unit)null, ItemSpellIds.MarkOfConquestEnergize, new CastSpellExtraArgs(aurEff));
		}
	}
}