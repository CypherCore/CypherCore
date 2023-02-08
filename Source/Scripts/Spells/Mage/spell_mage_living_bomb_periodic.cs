using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 217694 - Living Bomb
internal class spell_mage_living_bomb_periodic : AuraScript, IHasAuraEffects
{
	public List<IAuraEffectHandler> AuraEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.LivingBombExplosion);
	}

	public override void Register()
	{
		AuraEffects.Add(new AuraEffectApplyHandler(AfterRemove, 2, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
	}

	private void AfterRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
	{
		if (GetTargetApplication().GetRemoveMode() != AuraRemoveMode.Expire)
			return;

		Unit caster = GetCaster();

		if (caster)
			caster.CastSpell(GetTarget(), MageSpells.LivingBombExplosion, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, aurEff.GetAmount()));
	}
}