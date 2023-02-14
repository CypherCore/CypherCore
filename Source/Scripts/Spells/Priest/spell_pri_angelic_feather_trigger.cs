// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.Priest;

[Script] // 121536 - Angelic Feather talent
internal class spell_pri_angelic_feather_trigger : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override bool Validate(SpellInfo spellInfo)
	{
		return ValidateSpellInfo(PriestSpells.AngelicFeatherAreatrigger);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleEffectDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHit));
	}

	private void HandleEffectDummy(uint effIndex)
	{
		var destPos = GetHitDest().GetPosition();
		var radius  = GetEffectInfo().CalcRadius();

		// Caster is prioritary
		if (GetCaster().IsWithinDist2d(destPos, radius))
		{
			GetCaster().CastSpell(GetCaster(), PriestSpells.AngelicFeatherAura, true);
		}
		else
		{
			CastSpellExtraArgs args = new(TriggerCastFlags.FullMask);
			args.CastDifficulty = GetCastDifficulty();
			GetCaster().CastSpell(destPos, PriestSpells.AngelicFeatherAreatrigger, args);
		}
	}
}