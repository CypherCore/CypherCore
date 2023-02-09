using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 67228 - Item - Shaman T9 Elemental 4P Bonus (Lava Burst)
internal class spell_sha_t9_elemental_4p_bonus : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.LavaBurstBonusDamage);
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

		var spellInfo = Global.SpellMgr.GetSpellInfo(ShamanSpells.LavaBurstBonusDamage, GetCastDifficulty());
		var amount    = (int)MathFunctions.CalculatePct(damageInfo.GetDamage(), aurEff.GetAmount());
		amount /= (int)spellInfo.GetMaxTicks();

		var caster = eventInfo.GetActor();
		var target = eventInfo.GetProcTarget();

		CastSpellExtraArgs args = new(aurEff);
		args.AddSpellMod(SpellValueMod.BasePoint0, amount);
		caster.CastSpell(target, ShamanSpells.LavaBurstBonusDamage, args);
	}
}