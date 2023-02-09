using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Hunter;

[Script] // 67151 - Item - Hunter T9 4P Bonus (Steady Shot)
internal class spell_hun_t9_4p_bonus : AuraScript, IAuraCheckProc, IHasAuraEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(HunterSpells.T94PGreatness);
	}

	public bool CheckProc(ProcEventInfo eventInfo)
	{
		if (eventInfo.GetActor().IsTypeId(TypeId.Player) &&
		    eventInfo.GetActor().ToPlayer().GetPet())
			return true;

		return false;
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
	}

	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var caster = eventInfo.GetActor();

		caster.CastSpell(caster.ToPlayer().GetPet(), HunterSpells.T94PGreatness, new CastSpellExtraArgs(aurEff));
	}
}