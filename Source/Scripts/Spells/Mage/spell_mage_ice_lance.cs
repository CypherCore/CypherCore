using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Mage;

[Script] // Ice Lance - 30455
internal class spell_mage_ice_lance : SpellScript, IHasSpellEffects
{
	private readonly List<ObjectGuid> _orderedTargets = new();
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(MageSpells.IceLanceTrigger, MageSpells.ThermalVoid, MageSpells.IcyVeins, MageSpells.ChainReactionDummy, MageSpells.ChainReaction, MageSpells.FingersOfFrost);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(IndexTarget, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.LaunchTarget));
		SpellEffects.Add(new EffectHandler(HandleOnHit, 0, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
	}

	private void IndexTarget(uint effIndex)
	{
		_orderedTargets.Add(GetHitUnit().GetGUID());
	}

	private void HandleOnHit(uint effIndex)
	{
		var caster = GetCaster();
		var target = GetHitUnit();

		var index = _orderedTargets.IndexOf(target.GetGUID());

		if (index == 0 // only primary Target triggers these benefits
		    &&
		    target.HasAuraState(AuraStateType.Frozen, GetSpellInfo(), caster))
		{
			// Thermal Void
			var thermalVoid = caster.GetAura(MageSpells.ThermalVoid);

			if (!thermalVoid.GetSpellInfo().GetEffects().Empty())
			{
				var icyVeins = caster.GetAura(MageSpells.IcyVeins);

				icyVeins?.SetDuration(icyVeins.GetDuration() + thermalVoid.GetSpellInfo().GetEffect(0).CalcValue(caster) * Time.InMilliseconds);
			}

			// Chain Reaction
			if (caster.HasAura(MageSpells.ChainReactionDummy))
				caster.CastSpell(caster, MageSpells.ChainReaction, true);
		}

		// put Target index for chain value Multiplier into EFFECT_1 base points, otherwise triggered spell doesn't know which Damage Multiplier to apply
		CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
		args.AddSpellMod(SpellValueMod.BasePoint1, index);
		caster.CastSpell(target, MageSpells.IceLanceTrigger, args);
	}
}