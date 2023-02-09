using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

[Script] // 51505 - Lava burst
internal class spell_sha_lava_burst : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.PathOfFlamesTalent, ShamanSpells.PathOfFlamesSpread, ShamanSpells.LavaSurge);
	}

	public void AfterCast()
	{
		Unit caster = GetCaster();

		Aura lavaSurge = caster.GetAura(ShamanSpells.LavaSurge);

		if (lavaSurge != null)
			if (!GetSpell().m_appliedMods.Contains(lavaSurge))
			{
				uint chargeCategoryId = GetSpellInfo().ChargeCategoryId;

				// Ensure we have at least 1 usable charge after cast to allow next cast immediately
				if (!caster.GetSpellHistory().HasCharge(chargeCategoryId))
					caster.GetSpellHistory().RestoreCharge(chargeCategoryId);
			}
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleScript, 0, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHitTarget));
	}

	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleScript(uint effIndex)
	{
		Unit caster = GetCaster();

		if (caster)
			if (caster.HasAura(ShamanSpells.PathOfFlamesTalent))
				caster.CastSpell(GetHitUnit(), ShamanSpells.PathOfFlamesSpread, new CastSpellExtraArgs(GetSpell()));
	}
}