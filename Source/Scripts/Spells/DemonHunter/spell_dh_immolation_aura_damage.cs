using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(258922)]
public class spell_dh_immolation_aura_damage : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	readonly uint[] _hit = new uint[]
	                       {
		                       DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER
	                       };


	public override bool Validate(SpellInfo UnnamedParameter)
	{
		return ValidateSpellInfo(DemonHunterSpells.SPELL_DH_CHARRED_FLESH, DemonHunterSpells.SPELL_DH_FIERY_BRAND_DOT, DemonHunterSpells.SPELL_DH_FIERY_BRAND_MARKER);
	}

	private void HandleHit(uint UnnamedParameter)
	{
		var target = GetHitUnit();

		if (target != null)
			if (GetCaster().HasAura(DemonHunterSpells.SPELL_DH_CHARRED_FLESH))
				foreach (var spellId in _hit)
				{
					var fieryBrand = target.GetAura(spellId);

					if (fieryBrand != null)
					{
						var durationMod = GetCaster().GetAuraEffectAmount(DemonHunterSpells.SPELL_DH_CHARRED_FLESH, 0);
						fieryBrand.ModDuration(durationMod);
					}
				}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
	}
}