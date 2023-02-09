using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Generic;

[Script] // 60501 - Vampiric Touch
internal class spell_gen_vampiric_touch : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(GenericSpellIds.VampiricTouchHeal);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();
		var damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null ||
		    damageInfo.GetDamage() == 0)
			return;

		var                caster = eventInfo.GetActor();
		CastSpellExtraArgs args   = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)damageInfo.GetDamage() / 2);
		caster.CastSpell(caster, GenericSpellIds.VampiricTouchHeal, args);
	}
}