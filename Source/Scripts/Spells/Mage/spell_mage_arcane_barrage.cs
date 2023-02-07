using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // 44425 - Arcane Barrage
internal class spell_mage_arcane_barrage : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	private ObjectGuid _primaryTarget;

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.ArcaneBarrageR3, MageSpells.ArcaneBarrageEnergize) && spellInfo.GetEffects().Count > 1;
	}

	public void AfterCast()
	{
		Unit caster = GetCaster();

		// Consume all arcane charges
		int arcaneCharges = -caster.ModifyPower(PowerType.ArcaneCharges, -caster.GetMaxPower(PowerType.ArcaneCharges), false);

		if (arcaneCharges != 0)
		{
			AuraEffect auraEffect = caster.GetAuraEffect(MageSpells.ArcaneBarrageR3, 0, caster.GetGUID());

			if (auraEffect != null)
				caster.CastSpell(caster, MageSpells.ArcaneBarrageEnergize, new CastSpellExtraArgs(SpellValueMod.BasePoint0, arcaneCharges * auraEffect.GetAmount() / 100));
		}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
		SpellEffects.Add(new EffectHandler(MarkPrimaryTarget, 1, SpellEffectName.Dummy, SpellScriptHookType.LaunchTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleEffectHitTarget(uint effIndex)
	{
		if (GetHitUnit().GetGUID() != _primaryTarget)
			SetHitDamage(MathFunctions.CalculatePct(GetHitDamage(), GetEffectInfo(1).CalcValue(GetCaster())));
	}

	private void MarkPrimaryTarget(uint effIndex)
	{
		_primaryTarget = GetHitUnit().GetGUID();
	}
}