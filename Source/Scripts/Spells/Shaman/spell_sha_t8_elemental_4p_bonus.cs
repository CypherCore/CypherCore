using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 64928 - Item - Shaman T8 Elemental 4P Bonus
internal class spell_sha_t8_elemental_4p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.Electrified);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		PreventDefaultAction();

		DamageInfo damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo == null ||
		    damageInfo.GetDamage() == 0)
			return;

		SpellInfo spellInfo = Global.SpellMgr.GetSpellInfo(ShamanSpells.Electrified, GetCastDifficulty());
		int       amount    = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
		amount /= (int)spellInfo.GetMaxTicks();

		Unit caster = eventInfo.GetActor();
		Unit target = eventInfo.GetProcTarget();

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell(target, ShamanSpells.Electrified, args);
	}
}