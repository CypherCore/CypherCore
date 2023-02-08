using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 210824 - Touch of the Magi (Aura)
internal class spell_mage_touch_of_the_magi_aura : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.TouchOfTheMagiExplode);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 0, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void HandleProc(AuraEffect aurEff, ProcEventInfo eventInfo)
	{
		DamageInfo damageInfo = eventInfo.GetDamageInfo();

		if (damageInfo != null)
			if (damageInfo.GetAttacker() == GetCaster() &&
			    damageInfo.GetVictim() == GetTarget())
			{
				uint extra = MathFunctions.CalculatePct(damageInfo.GetDamage(), 25);

				if (extra > 0)
					aurEff.ChangeAmount(aurEff.GetAmount() + (int)extra);
			}
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		int amount = aurEff.GetAmount();

		if (amount == 0 ||
		    GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
			return;

		Unit caster = GetCaster();

		caster?.CastSpell(GetTarget(), MageSpells.TouchOfTheMagiExplode, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, amount));
	}
}