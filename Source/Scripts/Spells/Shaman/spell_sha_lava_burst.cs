// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Shaman;

// 51505 - Lava burst
[SpellScript(51505)]
internal class spell_sha_lava_burst : SpellScript, ISpellAfterCast, IHasSpellEffects
{
	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(ShamanSpells.PathOfFlamesTalent, ShamanSpells.PathOfFlamesSpread, ShamanSpells.LavaSurge);
	}

	public void AfterCast()
	{
		var caster = GetCaster();

		var lavaSurge = caster.GetAura(ShamanSpells.LavaSurge);

		if (lavaSurge != null)
			if (!GetSpell().m_appliedMods.Contains(lavaSurge))
			{
				var chargeCategoryId = GetSpellInfo().ChargeCategoryId;

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

	private void HandleScript(int effIndex)
	{
		var caster = GetCaster();

		if (caster)
			if (caster.HasAura(ShamanSpells.PathOfFlamesTalent))
				caster.CastSpell(GetHitUnit(), ShamanSpells.PathOfFlamesSpread, new CastSpellExtraArgs(GetSpell()));
	}
}