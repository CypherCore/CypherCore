// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DemonHunter;

[SpellScript(209651)]
public class spell_dh_shattered_souls_missile : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	private void HandleHit(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		var caster = GetCaster();

		if (caster == null)
			return;

		var spellToCast = GetSpellValue().EffectBasePoints[0];

		var dest = GetHitDest();

		if (dest != null)
			caster.CastSpell(new Position(dest.GetPositionX(), dest.GetPositionY(), dest.GetPositionZ()), (uint)spellToCast, true);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.TriggerMissile, SpellScriptHookType.EffectHit));
	}
}