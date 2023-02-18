// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(18562)]
public class spell_dru_swiftmend : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();


	private struct Spells
	{
		public static readonly uint SOUL_OF_THE_FOREST = 158478;
		public static readonly uint SOUL_OF_THE_FOREST_TRIGGERED = 114108;
	}


	private void HandleHit(uint UnnamedParameter)
	{
		var caster = GetCaster();

		if (caster != null)
			if (caster.HasAura(Spells.SOUL_OF_THE_FOREST))
				caster.AddAura(Spells.SOUL_OF_THE_FOREST_TRIGGERED, caster);
	}

	public override void Register()
	{
		SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.Heal, SpellScriptHookType.EffectHitTarget));
	}
}