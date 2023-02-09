using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Items;

[Script] // Frozen Shadoweave set 3p bonus
internal class spell_item_frozen_shadoweave : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ItemSpellIds.Shadowmend);
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
		args.AddSpellMod(SpellValueMod.BasePoint0, (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount()));
		caster.CastSpell((Unit)null, ItemSpellIds.Shadowmend, args);
	}
}