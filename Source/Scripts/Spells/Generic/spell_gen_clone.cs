// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class spell_gen_clone : SpellScript, IHasSpellEffects
{
	public List<ISpellEffect> SpellEffects { get; } = new();

	public override void Register()
	{
		if (ScriptSpellId == GenericSpellIds.NightmareFigmentMirrorImage)
		{
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
		}
		else
		{
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 1, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
			SpellEffects.Add(new EffectHandler(HandleScriptEffect, 2, SpellEffectName.ScriptEffect, SpellScriptHookType.EffectHitTarget));
		}
	}

	private void HandleScriptEffect(int effIndex)
	{
		PreventHitDefaultEffect(effIndex);
		GetHitUnit().CastSpell(GetCaster(), (uint)GetEffectValue(), true);
	}
}